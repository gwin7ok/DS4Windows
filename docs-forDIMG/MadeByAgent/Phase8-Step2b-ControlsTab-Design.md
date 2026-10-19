# Phase8-Step2b 設計書: Controls タブ（単一ボタンの割り当て）の統一エンジンへの取り込み（Step7.8）

作成日: 2026-10-20  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Step7-Plan.md`（§4 の 7.8）、`Phase8-Step2-Design.md`（§6 のシフト、§24）、`Phase8-Unified-Trigger-Dispatch-Plan.md`（D2＝B1）、`Phase8-Step1-Trigger-Spec-Ledger.md`、`Phase8-Step1-Static-Access-Inventory.md`  
状態: **ユーザーの決定を反映済み（2026-10-20）: Q4＝B（機能変更。共有をやめる）、Q8＝B（実機確認は Step9 でまとめて行う）、Q9＝A、他は推奨（A）。実装はまだ行わない。次は 7.8a の計画書**

---

## 1. 目的と、この設計書が答える問い

D2＝B1 の「Controls タブとスペシャルアクションの統一」の最後の部分。特殊アクション（7.1〜7.7）は統一エンジンへ切り替え済みで、残るのは Controls タブの割り当て `Mapping.ProcessControlSettingAction`（`Mapping.cs` 3759〜4162 行、約 400 行）。

特殊アクションと違い、**Controls タブは「エッジで 1 回実行する」種類の機能ではなく、「毎フレーム、ボタンの状態を別の出力に写す」機能が中心**である。そのため、7.1〜7.7 と同じ方法（入力アクション＋出力アクション＋束縛）を、そのまま当てはめられない部分がある。この設計書は、次の 3 つに答える。

1. 何が Controls タブの機能か（現状の棚卸し。§2）。
2. どこまでをエンジンに載せるのが妥当か、どう分けて進めるか（§3〜§5）。
3. 決めるべきこと（§8 の Q1〜Q8）。

## 2. 現状の調査

### 2.1 呼び出しの構造（`Mapping.MapCustom`、2270〜2680 行）

`MapCustomAction`（特殊アクション。エンジンの `ProcessOwned` を含む）のあと、次の順で `ProcessControlSettingAction(dcs, …)` を呼ぶ。

| 順 | 対象 | 備考 |
|---|---|---|
| 1 | LS（`LSOuter`、`LXNeg`〜`LYPos`） | スティックのモードが `Controls` のときだけ。それ以外（`FlickStick` など）は別の処理 |
| 2 | RS | 同上。`deltaAccelSettings` が有効なら、先に加速の処理 |
| 3 | L2（と `L2FullPull`） | 2 段トリガー（`twoStageMode`）が無効なら 1 回。有効なら `ProcessTwoStageTrigger` が、ソフト・フルのどちらを実行するかを決め、それぞれ実行 |
| 4 | R2（と `R2FullPull`） | 同上 |
| 5 | 標準のボタン（`ControlButtons`） | L1、L3、R1、R3 ほか |
| 6 | 拡張デバイスのボタン（`ExtraDeviceButtons`） | DualSense Edge などの追加ボタン |
| 7 | ジャイロスワイプ（`GyroOutMode.DirectionalSwipe` のとき） | 離れた方向のボタンを先に処理してから、現在の方向を処理 |
| 8 | `customMapQueue` の消化 | 「ボタン → X360 のボタン」への写しを、フレームの最後にまとめて反映 |

つまり、**「どの `DS4ControlSettings`（以降 dcs）を今フレームで処理するか」の選択**（スティックのモード、2 段トリガー、ジャイロスワイプ）と、**「その dcs を処理する」本体**（`ProcessControlSettingAction`）は分かれている。

### 2.2 `ProcessControlSettingAction` が行うこと（dcs 1 つにつき、毎フレーム）

| 区分 | 内容 | 性質 |
|---|---|---|
| **A. 抑制** | `CheckForSpecialActionSuppression`（＝`_engine.IsSuppressed`）が真なら、通常の出力を消して終了 | 特殊アクションとの連携。エンジンが集合を持つ（Step4 で実装済み） |
| **B. シフト** | `shiftActionType` が Default でなく、`ShiftTrigger(shiftTrigger)` が真なら、シフト側の割り当て（`shiftAction`／`shiftKeyType`）を使う。そうでなければ通常の割り当て | レベル判定（毎フレーム）。副ボタンは `shiftTriggerMapping` の添字（26 はタッチ 1 本指の特別扱い） |
| **C. Extras** | 振動（`setRumble`）、ライトバーの強制色・点滅（`DS4LightBar.forced*`）、ボタン感度の一時変更（`tempButtonSensitivity`）。ボタンが押されている間だけ有効。離れたら元に戻す（`held[device]` で管理） | 押している間の副作用＋離したときの復元。**状態がある**（`held`、`extrasRumbleActive`） |
| **D. Lightbar マクロ** | `dcs.LightbarMacro.Active` なら、ボタンの押下または解放のエッジで、背景のタスクで実行（取り消し可能） | エッジ型。`DS4State` のフィールドをリフレクションで読む（前回のレポートと比較） |
| **E. Macro** | 押している間 `PlayMacro`、離したら `EndMacro`。通常の出力は消す | レベル型（押している間再生し続ける仕組みを `PlayMacro` が持つ） |
| **F. Key** | `Key` の出力。論理キー・ネイティブキー（`GetRealEventKey`）・ScanCode・Toggle を求め、**合成の SpecialAction**（`FindSpecialActionForLogicalKey` ?? `GetOrCreateSyntheticKeyAction`）を、**旧エッジディスパッチャ**（`DispatchOrSetBeingTriggered`→`ActionManager.DispatchTriggerEdge`）に投入。通常の出力は消す | エッジ型。**エンジンを通らず、旧系統 B［Phase8 で廃止予定の `ActionManager`／`KeyAction` 系］を通る** |
| **G. Button（X360）** | ボタン → X360 のボタン、タッチパッドクリック、マウスのボタン（左・右・中・第 4・第 5）、ホイール（上・下。アナログなら量で）、マウスの移動（上・下・左・右。`getMouseMapping`）、Toggle。通常の出力は消す | **レベル型・アナログ量・累積**（`customMapQueue`、`currentClicks.*Count`、`tempMouseDeltaX/Y`、`stickWheelRemainder`）。出力先がフレームの共有の入れ物 |
| **H. Default** | 既定のままの dcs は、`AxisDir`（スティックの方向）だけ `customMapQueue` に自分自身への写しを積む | 通常の出力の維持 |

### 2.3 重要な事実

1. **Key（F）は、すでに部分的に統一済みの経路を通っている**: 合成 SpecialAction（`Synthetic_Key_{device}_{control}`）を作り、旧ディスパッチャへ入れる。つまり、「Controls タブの Key を、特殊アクションの Key と同じ仕組みで動かす」ことは、旧系統 B を通して実現されている。**エンジンに載せるとは、この合成 SpecialAction を、`ActionBindingBuilder` が作る Key の束縛（`KeyOutputAction`）に置き換えること**。
2. **`FindSpecialActionForLogicalKey` の癖**: 同じ論理キーを出す特殊アクション（Key 種別）が登録されていると、Controls タブの Key はその特殊アクションの状態（`ActionInstanceState`）を**共有**する（合成を作らない）。意図的な設計か偶然かは不明。移行では、この共有を再現するか、分離（動作の変更）するか、決める必要がある（Q4）。
3. **Button（G）は、出力が「フレームの共有の入れ物」に累積される**（クリックの数、ホイールの残り、マウスの移動量、X360 のボタン配列）。エンジンの `IFrameOutputAction`（Step4 で作った、Button 特殊アクション用の毎フレームの出力）は、同じ入れ物に書くため、**構造としては載せられる**。ただし、アナログ量（スティックでマウスを動かす、スティックでホイール）は、現在の `IFrameOutputTrace`／`IOutputContext` が扱っていない（真偽の押下だけ）。
4. **ほぼ全ての dcs で毎フレーム実行される**: L1、R1 などの 20〜40 個の dcs が、毎フレーム `ProcessControlSettingAction` を通る。**割り当て 0（ホットパスでの GC なし）を保つ**ことが、特殊アクションより厳しく要求される（既定のまま［Default］の dcs では、何もしないで抜けるのが理想）。
5. **シフトは「別の割り当てに切り替わる」意味**: Step2 設計書 §6 の案は、通常用とシフト用の 2 束縛を作り、`ExclusionGroup` を同じにする（先に成立した方だけ実行）。`Phase8Step4` で `ExclusionGroup`（先頭で成立した束縛がグループを占有する）は実装・テスト済みで、使われるのはここ（7.8）。

## 3. 方針

### 3.1 基本方針

**「dcs の選択」は Mapping に残し、「選んだ dcs の処理」をエンジンに載せる。** 理由:

- 選択（スティックのモード、2 段トリガー、ジャイロスワイプ）は、Controls タブの割り当て固有で、特殊アクションと共有しない。統一する価値が小さく、変更の範囲が広がる。
- 処理の本体（抑制、シフト、Key、Macro、Button、Extras）は、特殊アクションと同種の概念（入力の成立→出力）を持つため、統一の対象。

### 3.2 層の対応

| 層 | この機能での担当 |
|---|---|
| 1. 入力監視層 | ボタンの押下状態、前フレーム、2 段トリガーの領域、ジャイロスワイプ（`DS4State`、`Mouse`、`ITriggerInput`） |
| 2. 信号変換層 | 「どの dcs を処理するか」の選択（残す）、シフトの判定（排他グループ）、ボタンの AND 条件（要素数 1 の `ComboTriggerInputAction`）、抑制、割り当て（dcs → 束縛）の構築（ビルダー） |
| 3. 出力層 | Key の送出、Macro の再生、X360 のボタン・マウス・ホイールの書き込み、Extras の副作用（振動・ライトバー・感度） |

### 3.3 種別ごとの載せ方（案）

| 区分 | 載せ方 | 難度 |
|---|---|---|
| **F. Key** | dcs ごとに Key の束縛（`ComboTriggerInputAction`［1 ボタン］＋`KeyOutputAction`）を作る。`respectsFreshPressGuard`＝偽（旧 Controls タブの経路はガードを見ない）、Toggle は `keyType`。合成 SpecialAction と、旧ディスパッチャへの投入を置き換える | 中（`FindSpecialActionForLogicalKey` の共有の扱いが論点） |
| **E. Macro** | 押している間 `PlayMacro`、離したら `EndMacro`（`IHeldOutputAction`）。通常の出力は消す（エンジンの共通の方針） | 中 |
| **B. シフト** | 通常用とシフト用の 2 束縛を `ExclusionGroup` で束ね、シフトの副ボタンを条件に含める。通常側は、シフトが成立している間は実行しない | 中〜大（排他の意味が、旧経路と一致するかの特性化が要る） |
| **C. Extras／D. Lightbar マクロ** | 押している間の副作用＋離したときの復元の出力（`IHeldOutputAction`＋`Stop`）。`held`／`extrasRumbleActive` はインスタンスに持つ | 中（振動・ライトバー・感度の各ポートが要る。7.6 のポート方式 Q2＝A） |
| **G. Button（X360・マウス・ホイール）** | `IFrameOutputAction` で、フレームの入れ物へ書く。アナログ（マウス移動、ホイール）は、出力の文脈にアナログ量を足すか、旧経路に残す | **大**（最も範囲が広く、動作の差が出やすい） |
| **H. Default** | 束縛を作らない（何もしないで抜ける）。`AxisDir` の passthrough は、`customMapQueue` を使う旧経路に残すか、Button と同時に移す | 小（ただし、割り当て 0 の維持が要る） |
| **A. 抑制** | すでにエンジンの `IsSuppressed` を使っている。変更なし | なし |

### 3.4 進め方（1 種別 ＝ 1 PR。7.1〜7.7 と同じ）

| PR | 内容 | 前提 |
|---|---|---|
| **7.8a 土台** | dcs → 束縛のビルダー（`IControlSettingBindingBuilder` 相当）と、エンジンの入口（dcs 単位の処理）。束縛は**まだ作らない**（空）。割り当て 0 と、Default の dcs を何もしないで抜けることの特性化テスト | なし |
| **7.8b Key** | §3.3 の F | 7.8a |
| **7.8c Macro** | §3.3 の E | 7.8a |
| **7.8d シフト** | §3.3 の B（Key・Macro がシフトで切り替わる） | 7.8b、7.8c |
| **7.8e Extras／Lightbar マクロ** | §3.3 の C、D | 7.8a（ポート） |
| **7.8f Button（X360・マウス・ホイール）** | §3.3 の G、H | 7.8a、アナログ量の出力（Q3） |
| **Step8 へ** | 旧コード（`ProcessControlSettingAction` の各枝、合成 SpecialAction、`DispatchOrSetBeingTriggered`、`FindSpecialActionForLogicalKey`）の削除 | 7.8b〜f |

各 PR とも、**ロールバックは、その種別の登録の 1 行を外す**（特殊アクションと同じ。`BindingMigrationOptions` に、Controls タブ用の移行済みの種別の集合を足す）。旧コードは Step8 まで残す。

### 3.5 旧経路との共存（最重要の論点）

Controls タブの各 dcs について、**「新経路が所有するか、旧経路が処理するか」を、dcs（または dcs の区分）ごとに決める**必要がある。特殊アクションは「種別」で所有を決めたが（`Owns(device, sa)`）、Controls タブは「dcs の `actionType`」と「シフトの有無」で決まる。

- 案: `ProcessControlSettingAction` の先頭で、`_engine.OwnsControl(device, dcs)`（その dcs の通常・シフトの両方が移行済みの種別だけで構成されているときだけ真）を調べ、真なら旧経路の本体を**素通り**（抑制だけは共通）。片方だけ移行済みで片方が未移行の dcs（例: 通常が Key、シフトが Button）は、**旧経路が全体を処理**する（動作の一貫性のため）。
- これにより、7.8b〜f は、それぞれの種別だけで構成される dcs から順に新経路へ移り、混在する dcs は最後の PR（7.8f）まで旧経路に残る。

### 3.6 層の割り振りの点検（2026-10-20。ユーザーの指摘を受けて追加）: 出力アクションが「SA タブの設定の解釈」を持っている

7.7b W2b で、「どのジェスチャーで何をするか」の決定を出力層から信号変換層（ビルダー）へ移し、出力を汎用の `MacroStringAction`（文字列を再生するだけ）にした。同じ観点で既存の出力を点検した結果は次のとおり。

| 出力 | 受け取るもの | 設定の解釈を出力が持っているか |
|---|---|---|
| `MacroStringAction`（W2b） | マクロの文字列 | **持たない（理想形）** |
| `KeyOutputAction` | `SpecialAction` | **持つ**: `details` を論理キーに解釈、`keyType` から Toggle を判定、`trigger`／`uTrigger` をログに使う |
| `ButtonOutputAction` | `SpecialAction` | **持つ**: `details` を `X360Controls` に解釈 |
| `LaunchProcessAction` | `SpecialAction` | **持つ**: `details` をパスとして解釈 |
| `MacroAction` | `SpecialAction` | **持つ**: `pressRelease`、`details` |
| `ProfileSwitchAction` | `SpecialAction` | **持つ**: `details` を切替先として解釈 |
| `BatteryCheckAction` | `SpecialAction` | **持つ**: `details` を「\|」または「,」で分けて色・閾値に解釈 |
| `GyroCalibrateAction`／`DisconnectBTAction`／`SteeringWheelCalibrateAction` | `SpecialAction` | 解釈する値は少ない（名前・種別をログに使う程度。`SteeringWheelCalibrateAction` は `firstTap` を状態として使う） |

**問題**: 7.1〜7.6 の出力は、SA タブの設定モデル（`SpecialAction`）に結びついている。Controls タブの割り当て（`DS4ControlSettings`）は `SpecialAction` ではないため、このままでは Key／Macro／Button の出力を **再利用できない**（旧経路は、そのために合成の SpecialAction を作って通している）。§3.3 の「Key の束縛を作る」は、この点を考慮していなかった。

**方針（案）**: 「設定 → 何をするか」の解釈は変換層（ビルダー）に集め、出力は**解釈済みの値**だけを受け取る形にする。

- `KeyOutputAction`: 論理キー、ネイティブキー、Toggle／ScanCode の有無を受け取る。
- `ButtonOutputAction`: `X360Controls` と Toggle を受け取る（マウス・ホイールも同じ列挙）。
- `MacroAction`: 「再生する内容」（文字列または整数配列）と、押している間／離したときの種別を受け取る。
- ログに使う名前・詳細の文字列は、ビルダーが渡す「識別情報」（名前、種別、詳細の表示用の文字列）で足りる。
- ビルダーは 2 種類の入口を持つ: SA 定義（`SpecialAction`）→ 束縛、Controls の割り当て（`DS4ControlSettings`）→ 束縛。どちらも同じ出力アクションを作る。
- **動作とログは変えない**（解釈の場所が移るだけ）。既存の 7.1〜7.7 のテストが、そのまま期待値を保つ。

この作業は、7.8b（Key）より前に、**7.8a の一部として**行う（出力の値渡し化。動作の変更なし）。

- 出力の結果（キー、マクロ、X360 のボタン、マウス、ホイール、振動、ライトバー）と、ログの文言・順序。
- 特殊アクションとの順序（`MapCustomAction` の後に Controls タブ、最後に `customMapQueue`、Stage3、`ApplyFrameOutputs`）。
- 抑制（特殊アクションがトリガーしているボタンの通常の割り当ての無効化）。
- Fresh-press ガード・実行中の再実行防止は、Controls タブの経路では**見ない**（旧経路が見ていないため）。変えない。
- 割り当て 0（毎フレームの GC なし）。

## 5. リスク

| リスク | 対応 |
|---|---|
| 毎フレーム・全 dcs を通るため、遅延・割り当ての増加が体感に響く | 7.8a で「Default の dcs は何もしない」「束縛の配列を事前構築（プロファイル適用時）」を確立してから、種別を載せる。割り当ての計測テスト（特殊アクションの基準と同じ方式）を各 PR に入れる |
| シフトの排他の意味が旧経路と食い違う | 7.8d の前に、旧経路の特性化（`Phase8Step1ShiftTriggerCharacterizationTests` は 3 件のみ。通常／シフトの Key・Macro・Button の組み合わせを追加）を行う |
| `FindSpecialActionForLogicalKey` の共有の癖 | Q4 で決める。再現する場合は、移行後も同じ論理キーの特殊アクションと状態を共有するテストを入れる |
| アナログ量の出力（マウス移動、ホイール） | Q3 で決める。範囲が大きいときは、7.8f を分割するか、Phase9 以降にする |
| 2 段トリガー・ジャイロスワイプの選択ロジック | 選択は Mapping に残す（§3.1）。dcs 単位の処理だけを載せるため、影響を受けない |
| 実機確認が多い（Key、Macro、シフト、Extras、マウス） | 特殊アクション同様、PR ごとに最小限の実機確認を作る。Step9 の総合確認で重複を整理する |
| 巨大な `Mapping.cs` への変更 | 3.2 の方針（ピンポイントの置換）。`ProcessControlSettingAction` の先頭に所有の判定を 1 か所足すだけにする |

## 6. ログ

既存のログ（`LogSpecialActionTrace` など、Controls タブの経路が出すもの）は維持する。新しい経路にも、標準化ログ接頭辞 `[DI]` を付けるのは、DI 境界を通る重要操作に限る（Key／Macro の実行ごとの Trace は、旧経路と同じ文言）。

## 7. 質問（ユーザーの決定が要るもの）

- **Q1（範囲）**【承認: A。2026-10-20】: A（推奨）: 3.4 の 7.8a〜f のすべてを Step7 の範囲とする。ただし 7.8f（Button／マウス）は、7.8e までの結果を見て、続けるか判断する。B: Key・Macro・シフト・Extras（7.8a〜e）までを Step7.8 とし、Button／マウス／ホイール（7.8f）は Phase9 以降に回す（旧経路が残る。D2＝B1 の最後の一部が未完になる）。C: Controls タブの取り込みを Phase8 の範囲外とする。
- **Q2（所有の判定の単位）**【承認: A。2026-10-20】: A（推奨）: dcs 単位（通常・シフトの両方が移行済みの種別のときだけ新経路。片方が未移行なら旧経路が全体を処理。§3.5）。B: 区分（通常／シフト）ごとに分けて所有する（混在できるが、シフトの切り替えが 2 経路にまたがり、リスクが高い）。
- **Q3（アナログ量の出力）**【承認: A。2026-10-20】: A（推奨）: 7.8f の中で、出力の文脈にアナログ量（0〜255 と方向）を足す。B: マウス移動・アナログのホイールは旧経路に残し、ボタン・クリック・X360 のボタンだけを載せる（7.8f を縮める）。
- **Q4（`FindSpecialActionForLogicalKey` の共有の癖）**【承認: B。2026-10-20。動作の変更を、ユーザーが明示的に承認（規約 2.2）。理由: 共有は出力側（キー）でなく、共有するなら入力側（Controls タブの入力ボタン、SA タブのトリガーキー）であるべき】: A（推奨）: 現状を維持する（同じ論理キーの Key 特殊アクションがあれば、Controls タブの Key は、その状態を共有する）。移行後もテストで固定し、別件として記録する。B: 分離する（Controls タブの Key は常に自分専用の状態）。動作の変更になるため、ユーザーの承認が要る（規約 2.2）。
- **Q5（Extras／Lightbar マクロのポート）**【承認: A。2026-10-20】: A（推奨）: 7.6 と同じ方式（`IRumblePort`、`ILightbarForcePort`、`IButtonSensitivityPort` のような狭いインターフェースと `Default*Port`）。B: `ControlService`／`DS4LightBar` を、出力アクションが直接呼ぶ（テストしにくい）。
- **Q6（シフトの扱いの特性化）**【承認: A。2026-10-20】: A（推奨）: 7.8d の前に、通常／シフトの組み合わせ（Key・Macro・Button の 9 通り）の特性化テストを追加してから、実装する。B: 現状の 3 件のみで進める。
- **Q7（進め方）**【承認: A。2026-10-20】: A（推奨）: 1 種別 ＝ 1 PR（7.8a→b→c→d→e→f）。各 PR の計画書は、`Phase8-Step7-8a-Plan.md` のように別に作る。B: Key と Macro を 1 PR にまとめる。
- **Q8（実機確認）**【承認: B。2026-10-20。各 PR の実機確認は作らず、Step9 でまとめて行う】: A（推奨）: 各 PR で最小限の実機確認を作り、Step7.6〜7.7 と同様、ユーザーの判断で先送りできる。まとめの確認は Step9。B: すべて実装後に 1 回だけ確認する。

- **Q9（出力の値渡し化。§3.6）【承認: A。2026-10-20】**: A（推奨）: 7.8a で、Key／Button／Macro の出力を「解釈済みの値を受け取る形」に直す（動作・ログは変えない）。SA と Controls の両方が同じ出力を使う。残りの出力（Program、Profile、BatteryCheck ほか）は、Controls タブで使わないため、今回は直さない。B: 直さず、Controls タブ用に合成の SpecialAction を作って通す（旧経路と同じ。作業は小さいが、設定モデルへの結びつきが残り、Step8 で合成 SA を消せない）。

## 8. 次

1. この設計書の Q1〜Q8 を、ユーザーが承認する。
2. 承認後、`Phase8-Step7-8a-Plan.md`（土台）を作成する。
3. 7.8a が合格してから、7.8b（Key）以降を 1 つずつ進める。

## 9. 参照

- `Mapping.cs`: `MapCustom`（2270〜2680）、`ProcessControlSettingAction`（3759〜4162）、`ShiftTrigger`（2222）、`CheckForSpecialActionSuppression`（6627）、`GetOrCreateSyntheticKeyAction`（53）、`FindSpecialActionForLogicalKey`（3225）、`DispatchOrSetBeingTriggered`（629）、`ResetToDefaultValue`（6930）、`GetBoolActionMapping`（6696）。
- `DS4ControlSettings.cs`、`ControlSettingsGroup.cs`。
- テスト: `Phase8Step1ShiftTriggerCharacterizationTests`（シフト 3 件）。

## 10. 決定の影響（2026-10-20）

- **Q4＝B**: Controls タブの Key は、同じ論理キーの Key 特殊アクションと状態を共有しない（常に自分専用の状態）。`FindSpecialActionForLogicalKey` の共有は、新経路では再現しない。旧経路は Step8 まで残るため、共有は旧経路が処理するボタンにだけ残る。これは**動作の変更**であり、7.8b で、変更前後の違いのテストと、完了報告書への記録（規約 2.2 の承認済みの変更として）を行う。入力側での共有が必要になったら、別件で扱う。
- **Q8＝B**: 7.8a〜f の各計画書に、実機確認の項目は作らない。Step9 の総合確認で、Controls タブ全体（Key、Macro、シフト、Extras、Button、マウス）をまとめて確認する。そのため、各 PR の安全網は、単体テスト（旧・新の両経路の比較）、ログの一致、割り当て 0、ロールバック（登録の 1 行）が中心になる。
- **Q9＝A**: 7.8a に、Key／Button／Macro の出力の値渡し化を含める（動作・ログは変えない）。
