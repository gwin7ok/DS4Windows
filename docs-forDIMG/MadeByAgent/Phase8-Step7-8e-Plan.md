# Phase8-Step7-8e 計画書: Controls タブの Extras と Lightbar マクロの新経路への移行

作成日: 2026-10-20  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Step2b-ControlsTab-Design.md`（§3.3 の C・D、§10。Q5＝A［ポート方式］、Q8＝B［実機確認は Step9］、Q9＝A［値渡し］）、`Phase8-Step7-8a-Plan.md`（土台）、`Phase8-Step7-8b-Plan.md`（Key）、`Phase8-Step7-8c-Plan.md`（Macro）、`Phase8-Step7-8d-Plan.md`（シフトの廃止）、`Phase8-Step7-6-Plan.md`（ポート方式の前例）  
状態: **Q1〜Q6 を承認済み（2026-10-20。ユーザー。Q1・Q2・Q5＝A、Q3＝B、Q4＝C、Q6＝B。§6）。W0（調査）完了［§5.1］。次は W1（旧経路の特性化テスト）。コードは未着手**

---

## 1. 目的と範囲

Controls タブの「Extras」（振動・ライトバーの強制色と点滅・ボタン感度の一時変更）と、「Lightbar マクロ」を、旧経路（`Mapping.ProcessControlSettingAction`）から統一エンジンへ切り替える。運用は 7.8b・7.8c と同じ: `MigratedControlKinds` に区分を足す 1 行で有効化し、外すと旧経路に戻る。旧コードは Step8 まで残す。

**Extras と Key／Macro は直交する**: 旧経路では、同じ dcs（`DS4ControlSettings`）が「Key＋Extras」「Macro＋Lightbar マクロ」のように、複数の部品を同時に持てる。7.8b・7.8c は「Extras なし、Lightbar マクロなし」の dcs だけを所有したので、Extras か Lightbar マクロを持つ dcs は、すべて旧経路が処理している。7.8e で、これらを新経路に取り込む。

**範囲（ボタン単位の所有。移行中の仕組み）**: dcs が次のすべてを満たすとき、新経路が dcs 全体を処理する（部品の一部だけを新経路にすることはしない）。

| 通常の割り当て | Extras | Lightbar マクロ | 担当 |
|---|---|---|---|
| Default（割り当てなし） | あり | あり／なし | **新経路（7.8e）**。Extras と Lightbar マクロの束縛だけを作る。通常の出力は消さない（Default は消さない規則） |
| Default | なし | あり | **新経路（7.8e）**。Lightbar マクロの束縛だけ |
| Key／Macro | あり／なし | あり／なし | **新経路（7.8b／7.8c の条件を満たすもの）**。部品ごとに束縛を作る（§3.2） |
| Button | あり | あり／なし | **旧経路のまま**（7.8f で Button と一緒に移行する） |

- Extras の値が読めない dcs（9 個未満など）は、所有せず、旧経路が処理する（7.8b の「キー番号が範囲外」と同じ方針。旧経路の例外の挙動を保つ）。
- Lightbar マクロは、`BoolDS4Controls`（ボタン 24 種）以外のボタンでは、旧経路も何もしない。新経路も束縛を作らない。
- ジャイロスワイプの割り当ては対象外（7.8b と同じ）。

**やらないこと**: Button／マウス／ホイール（7.8f）、旧コード（`Mapping` の Extras と Lightbar マクロの枝、`held`、`extrasRumbleActive`、`threadCts`）の削除（Step8）、動作の改善（§4 の承認なしには変えない）。

## 2. 現状の調査（2026-10-20。`Mapping.cs` 3759〜3879 行、4122〜4161 行）

### 2.1 旧経路の Extras

- **対象**: `extras` が空でない dcs（`IsExtrasEmpty`: null か `0,0,0,0,0,0,0,0,0` が空）。通常の割り当て（Key／Button／Macro／Default）とは無関係に、同じ dcs で処理される。
- **押下の判定**: `GetBoolActionMapping`（Controls の Key／Button と同じ判定。新経路の `ControlJudgement.Assignment`／`IsControlActive` の元）。
- **値**（カンマ区切り 9 個。`int.TryParse` が失敗した欄は 0）: [0] 強い振動、[1] 弱い振動、[2] ライトバーの強制色（1 で有効）、[3][4][5] 色（R,G,B）、[6] 点滅、[7] 感度の変更（1 で有効）、[8] 感度。
- **押している間、毎フレーム**: `held[device] = control`。振動が両方 0 でなければ `ctrl.setRumble(byte, byte, device)` と `extrasRumbleActive[device] = true`。ライトバーが有効で、かつ**この dcs の Lightbar マクロが有効でない**とき、`DS4LightBar.forcedColor／forcedFlash／forcelight` を設定。感度が有効で、`tempButtonSensitivity == -1` のときだけ、一時感度を設定（1 回だけ）。全体を `try { } catch { }` で囲む（9 個未満だと途中で例外になり、そこまでの効果が残る）。
- **離した最初のフレーム**（`held[device] == control` のときだけ）: `forcelight = false`、`forcedFlash = 0`、一時感度を元に戻す、`extrasRumbleActive` なら振動を 0,0、`held = None`。
- **癖（再現する対象）**:
  1. **`held` はデバイスにつき 1 つ**: Extras のボタンを 2 つ同時に押すと、最後に処理したほうだけが `held`。先に離したボタンでは、元に戻す処理が走らない。
  2. 離したときの元に戻す処理は、ライトバーが有効でなかった dcs でも `forcelight = false` にする（ほかの強制色の使い手と競合しうる）。
  3. 押したままプロファイルを適用・切断しても、元に戻す処理は走らない可能性がある（W1 で実測。§5）。
  4. Fresh-press ガードはない。

### 2.2 旧経路の Lightbar マクロ

- **対象**: `dcs.LightbarMacro.Active` の dcs のうち、`BoolDS4Controls`（ボタン 24 種）のもの。
- **判定**: **Controls の判定ではなく**、デバイスの生の状態（`GetRawPreviousStateRef`／`GetRawCurrentStateRef`。コメント「`cState` を使うと入力が重複することがある」）の、1 フレーム前と現在の変化。`Trigger` が Press なら押した瞬間、Release なら離した瞬間。
- **実行**: 変化の瞬間に、`Task.Run(RunLightbarMacro)`。`CancelCurrent` なら実行中のタスクを取り消して待ち、新しく始める。`CancelCurrent` でなければ、前のタスクが終わっているか、一度も動いていないときだけ始める。
- **共有状態（癖）**: 取り消しの元（`threadCts`）とタスク（`lightbarMacroTask`）は、**`Mapping` に 1 つ**。デバイスにもボタンにも依らない（複数台で、ある台のマクロが別の台のマクロを取り消す）。
- **`RunLightbarMacro`**: `lock (forcedColor) lock (forcelight)` の中で、`forcelight[device] = true` → 各要素の色を、長さ（ms）ずつ → `forcelight[device] = false`。10ms 刻みで取り消しを見る。

### 2.3 新経路の土台（すでにあるもの）

- 束縛の作り方、値渡し（`Q9=A`）、ボタン単位の所有（`ControlBindingBuilder`）、判定（`ControlButtonInputAction`＋`ControlJudgement`）、消去の規則（`BindingPolicy.ErasesOutputEveryFrame`）、押している間の出力（`IHeldOutputAction.WhileHeld`）、離した瞬間（`Stop`）、ポートの前例（`IBatteryCheckPort`／`DefaultBatteryCheckPort`）。
- **足りないもの（§3）**: ① 1 つのボタンに複数の束縛を持たせること（エンジンの `ControlIndex` は、今は 1 ボタン＝1 束縛）、② Extras の出力とポート、③ Lightbar マクロの出力・ポート・生の状態の変化の判定、④ `held`／`extrasRumbleActive` の置き場。

## 3. 設計

### 3.1 層の割り振り

| 層 | 担当 |
|---|---|
| 入力監視層 | `TriggerInput`（既存の `IsControlActive`。生の状態の前後 `IsRawPressed`／`WasRawPressed` を足す） |
| 信号変換層 | `ControlBindingBuilder`（所有する dcs の選別、Extras の値の解釈、Lightbar マクロの値の解釈、部品ごとの束縛と識別用 SA）、`ControlButtonInputAction`（判定の種類を増やす）、エンジン（1 ボタン複数束縛） |
| 出力層 | `ControlExtrasOutputAction`（Extras）、`ControlLightbarMacroOutputAction`（Lightbar マクロ）。実機・静的な窓口は、ポート（`IControlExtrasPort`、`ILightbarMacroPort`）越し |

解釈（9 個の値、Lightbar マクロの要素・トリガー・取り消し）は変換層が行い、出力には解釈済みの値を渡す（Q9＝A）。

### 3.2 追加・変更するもの

- **エンジン（1 ボタン複数束縛）**: `ControlIndex[control]` は先頭の束縛の添字とし、束縛ごとに「同じボタンの次の束縛」の添字（`ControlNext`）を持つ。`ProcessControl` は、鎖をたどって順に処理する。`OwnsControl` は先頭の束縛で判定する（ビルダーが、dcs の部品を「全部作る」か「1 つも作らない」かにするため、鎖の中で所有がまちまちにはならない）。
- **束縛の順序（旧経路と同じ）**: Extras → Lightbar マクロ → 通常の割り当て（Key／Macro）。
- **識別用 SA（状態の鍵）**: `Synthetic_Extras_{device}_{control}`、`Synthetic_LightbarMacro_{device}_{control}`（既存の `Synthetic_Key_…`／`Synthetic_Macro_…` と同じ形）。
- **Extras の束縛**: 入力 `ControlButtonInputAction(…, Kind=Extras, Judgement=Assignment)`。出力 `ControlExtrasOutputAction`（`IHeldOutputAction`）: 立ち上がりと押している間の毎フレームに同じ「適用」（振動、ライトバー、感度。感度は一時感度が -1 のときだけ）、`Stop` で元に戻す。方針は `respectsFreshPressGuard: false`、`erasesOutputEveryFrame: false`（Extras は通常の出力を消さない。消すのは通常の割り当ての束縛）。値は `ExtrasBindingValues`（新設の構造体。強い振動、弱い振動、ライトバーの有無、色、点滅、感度の有無、感度、**この dcs の Lightbar マクロが有効か**）。
- **Lightbar マクロの束縛**: 入力は新しい判定 `ControlJudgement.RawPress`／`RawRelease`（デバイスの生の状態の前後の変化。成立は変化したフレームの 1 回だけ。ボタンの読み取りは、旧経路のリフレクションではなく、起動時に作る表で行う）。出力 `ControlLightbarMacroOutputAction`（立ち上がりで `ILightbarMacroPort.Start(device, 要素, 取り消すか)`）。値は `LightbarMacroBindingValues`（要素の配列、取り消すか）。方針は Extras と同じ。
- **ポート**: `IControlExtrasPort`（`SetRumble`、`SetForcedLightbar`、`ClearForcedLightbar`、`TrySetTemporarySensitivity`、`RestoreSensitivity`）と `DefaultControlExtrasPort`（`ControlService.setRumble`、`DS4LightBar` の静的配列、`ButtonMouseInfo` を、窓口の内側で使う。`ControlService` は遅延で受ける。前例: `DefaultBatteryCheckPort`）。`ILightbarMacroPort`（`Start`）と `DefaultLightbarMacroPort`（**デバイスごと**に、取り消しの元とタスクを持つ［Q4＝C］。旧 `RunLightbarMacro` の二重の `lock (forcedColor) lock (forcelight)` は、持たない: 静的配列の別の要素への書き込みで、デバイス間の待ち合いを避けるため）。
- **Extras の状態（Q3＝B）**: 旧 `held`／`extrasRumbleActive`（デバイスに 1 つ）は移さない。`ControlExtrasOutputAction` が、自分の分（振動を出したか）を持ち、離したときに自分の分を元に戻す。`Mapping` の旧 `held`／`extrasRumbleActive` は、旧経路が処理する割り当て（Button＋Extras）のために、Step8 まで残す。
- **押したままの適用・切断の後始末（Q6＝B）**: 出力に任意の契約 `IResettableOutputAction`（`OnReset(IOutputContext)`）を足す。エンジンは、`OnProfileApplied` のあとの最初の処理（`ResetState`）と `OnDeviceDisconnected` で、束縛のうち状態が成立中（`BeingTriggered`）のものについて、出力の `OnReset` を呼ぶ。`ControlExtrasOutputAction` は `Stop` と同じ元に戻す処理をする。ほかの出力は実装しない（Key などの押したままの適用の動きは変えない）。適用後も同じボタンが押されたままなら、次のフレームで新しい束縛が立ち上がりとして再び適用する。旧経路で処理する割り当て（Button＋Extras）は、この修正の対象外（7.8f で新経路に入ると直る）。
- **`ControlBindingBuilder`**: 部品ごとの条件の判定（`TryInterpretExtras`、`TryInterpretLightbarMacro`）と、dcs 全体の所有の条件（§1 の表）。`ControlBindingKind` に `LightbarMacro` を足す（`Extras` と別のスイッチ。Q1）。
- **`Mapping`**: 先頭の所有の判定は 7.8b のまま（`OwnsControl`）。**旧経路の Extras と Lightbar マクロの枝は変えない**（Step8 で削除）。変更は、`TriggerInput.Update` に、デバイスの生の現在の状態を渡す引数を足すことのみ。
- **`ServiceRegistration`**: `MigratedControlKinds` に `Extras` と `LightbarMacro` の 1 行ずつ。2 つのポートの登録。

## 4. 動作が変わる点

**変えないことを原則とする**（規約 2.2／2.3）。承認済み（2026-10-20）の変更と、変えないものを分ける。

**承認済みの動作変更**

1. **Extras のボタンを 2 つ同時に押したとき（Q3＝B）**: 旧経路は、デバイスにつき 1 つの記録（`held`）しかなく、先に押したほうを先に離しても元に戻す処理が走らない。新経路は、ボタンごとに元に戻す。一方を離すと、そのボタンの元に戻す処理（ライトバーの強制色の解除、振動 0、感度の復元）が走り、押したままのもう一方は次のフレームで再び適用する（一瞬の途切れが起こりうる）。
2. **Lightbar マクロの実行状態（Q4＝C）**: 旧経路の、取り消しとタスクが `Mapping` に 1 つだけの癖をやめ、デバイスごとにする。複数台のとき、ある台のマクロが別の台のマクロを取り消さない。あわせて、静的配列の二重の `lock` を持たない（台の間で待ち合わない）。旧経路で処理する割り当て（Button＋Lightbar マクロ）は、7.8f まで旧経路の共有のまま。
3. **押したままの適用・切断（Q6＝B）**: 旧経路は、元に戻す処理が走らず、振動・強制色が残りうる（W1 で実測）。新経路は、適用・切断のときに元に戻す。

**変えないもの**

- Extras の判定（Controls の判定）、値の効き方、毎フレーム適用、感度は 1 回だけ（一時感度が -1 のときのみ）、離したときの元に戻す処理（`forcelight = false`、`forcedFlash = 0` を含む）、Lightbar マクロが有効な dcs では Extras の強制色を出さない。
- Lightbar マクロの判定（デバイスの生の状態の前後の変化）、Press／Release、`CancelCurrent` の意味、`BoolDS4Controls` 以外では動かない。
- 値が読めない Extras（9 個未満など）は、新経路は所有せず、旧経路が処理する。
- ログ: Extras と Lightbar マクロに旧経路のログはなく、新経路も出さない。エンジンの例外ログは従来どおり。

## 5. 作業（Wave）

- **W0（調査。完了。結果は §5.1）**: ① 観察の方法: 旧経路の Extras は、振動を `ControlService.setRumble`（実機がないと見えない）、ライトバーを `DS4LightBar` の静的配列、感度を `ButtonMouseInfo` に書く。テストで何を観察するか（`extrasRumbleActive`、静的配列、`tempButtonSensitivity`、ポートの記録）を決める。② 生の状態（`GetRawPreviousStateRef`／`GetRawCurrentStateRef`）を、`TriggerInput` が引ける方法（`IDeviceStateAccessor` など）と、`cState` との違い。③ 2 段トリガー（L2／R2）の dcs で、`ProcessTwoStageTrigger` が `fieldMapping` を 0 にして Extras を「無効にする」処理と、新経路の `IsControlActive`（`fieldMapping` を見る）が同じに働くか。④ `ExtrasRuntimeState` への移行で影響する参照（`Mapping.held`、`extrasRumbleActive` の公開フィールドの参照元）。⑤ `LightbarMacro` の編集画面が保存する値の形。
- **W1（特性化。旧・新の両経路の Theory。W1 の時点では新経路の指定でも旧経路が処理する）**: `ControlsKeyTestScope` に Extras と Lightbar マクロの設定を足す。`Phase8Step78eW1ControlsExtrasCharacterizationTests`: 押下で振動・強制色・点滅・感度が設定される、押し続けても感度は 1 回、離すと元に戻る、`held` の 1 つしかない癖（2 つ同時に押して、先に離す／後に離す）、Lightbar マクロが有効な dcs では Extras の強制色を出さない、スティックの扇形・L2 の 0 超の判定、値が空・読めないとき、押したままの適用・切断（実測。Q6＝B の修正前後の差が分かるように、旧経路の値を記録する）、Default＋Extras で通常の出力が消えないこと、Key＋Extras で両方動くこと。2 つ同時のときは、旧経路の値と、承認済みの変更（§4 の 1）を、`migrated` で期待値を分ける。`Phase8Step78eW1ControlsLightbarMacroCharacterizationTests`: Press／Release の変化で始まる、`CancelCurrent` あり・なし、実行中の再要求、デバイス間での取り消しの共有、`BoolDS4Controls` 以外では動かないこと。**実測値は、この計画書に追記する**（8c と同じ）。
- **W2（実装）**: §3.2。エンジンの 1 ボタン複数束縛と `IResettableOutputAction` から始め、Extras、Lightbar マクロの順に足す。W1 のテストが新経路も走る。期待値の変更点は §4 のとおり（承認後）。
- **W3**: 割り当て（新 ≦ 旧）、ロールバック（`Extras`／`LightbarMacro` の 1 行を外す）、ソースのガード（`Mapping` が `held` などを自分で持たない、Lightbar マクロの枝がポート呼び出し）。
- **W4**: 完了報告書（7.8e）、`Phase8-Status.md`、チェックリスト（**U-44**: Extras と Lightbar マクロの実機確認。Step9 でまとめて行う。Q8＝B）。

### 5.1 W0 の結果（2026-10-20）

1. **観察の方法**: 旧経路で、振動は `ControlService.setRumble`（デバイスがいないと何もしない）を呼ぶ前後に `Mapping.extrasRumbleActive[device]`（公開フィールド）が変わる。ライトバーは `DS4LightBar.forcedColor／forcedFlash／forcelight`（静的な公開配列）、感度は `ProfileSettingsService.ButtonMouseInfos[device].tempButtonSensitivity` で観察できる。したがって、W1 の旧経路は「`extrasRumbleActive`＋静的配列＋`tempButtonSensitivity`」、新経路は「ポートの記録」で比べる。静的配列はテスト間で共有されるため、テストは直列に実行し、前後で初期化する。
2. **生の状態**: 新経路の `TriggerInput` が持つ「前フレーム」（`getPreviousStateRef()`）は、旧経路の Lightbar マクロが使う `GetRawPreviousStateRef()` と同じオブジェクト（`pState`）。現在の状態は、旧経路が `GetRawCurrentStateRef()`（デバイスの `cState`）、`TriggerInput` が持つのは `MapCustom` の引数の `cState`（曲線・デッドゾーン処理後のコピー）で、別。したがって、`Update` に生の現在の状態の引数を足す（§3.2）。
3. **2 段トリガー**: `IsControlActive` は、`MapCustom` が展開した `fieldMapping`（`_fields`）を読む。2 段トリガーの処理が `fieldMapping` を 0 にして Extras を無効にする仕組みは、同じ表を共有するため、新経路でも効く。W1 の Theory に L2 の 2 段（ソフト・フル）を含めて確かめる。
4. **`held`／`extrasRumbleActive` の参照**: `Mapping` の中だけ（Extras の枝）。Q3＝B で、旧経路のものは動かさない。
5. **Lightbar マクロの保存形**: `LightbarMacro`（`DS4WinWPF.DS4Forms.ViewModels`）は、`Active`、`Macro`（色＋長さ ms の要素の列）、`Trigger`（Press／Release）、`CancelCurrent`。プロファイルの文字列は `True/255,255,255:100;255,0,0:50;/Press/False`。ビルダーは、`Active` のとき、要素を配列に写して値にする（`LightbarMacroBindingValues`）。
6. **ついでに分かったこと**: 旧経路の Lightbar マクロの判定は、毎フレーム、リフレクション（`typeof(DS4State).GetField(…)`）で値を引く（割り当てが生じる）。新経路は、表で引き、割り当てを増やさない。

## 6. 質問と決定

承認: **2026-10-20（ユーザー）。Q1・Q2・Q5＝A（推奨）、Q3＝B、Q4＝C、Q6＝B。**

- **Q1（範囲とスイッチ）【承認: A】**: Extras と Lightbar マクロの両方を 7.8e の範囲とし、スイッチは `Extras` と新設の `LightbarMacro` に分ける。
- **Q2（束縛の構成）【承認: A】**: Extras と Lightbar マクロは、通常の割り当てとは別の束縛にし、エンジンを「1 ボタン複数束縛」にする。
- **Q3（`held` の置き場）【承認: B。推奨の A から変更】**: 束縛ごとに持つ。旧経路の「デバイスに 1 つ」の癖は再現しない。2 つ同時に押したときの動きが変わる（§4 の 1）。
- **Q4（Lightbar マクロの実行状態）【承認: C。推奨の A から変更】**: デバイスごとに持つ。複数台の干渉がなくなる（§4 の 2）。
- **Q5（ポート）【承認: A】**: `IControlExtrasPort` を 1 つと、`ILightbarMacroPort`。
- **Q6（押したままの適用・切断）【承認: B。W1 の実測を待たずに決定】**: 適用・切断のときに元に戻す（§3.2 の `IResettableOutputAction`）。旧経路が処理する割り当て（Button＋Extras）は対象外で、7.8f で直る。

## 7. 完了条件

- [x] Q1〜Q6 をユーザーが承認（2026-10-20）。W0 の結果を追記（§5.1）。
- [ ] W1 の実測値を追記。
- [ ] ビルド・テストが成功（ユーザーが実行）。割り当て（新 ≦ 旧）、ロールバック、全ガード（1 ファイル 1 型、ラチェットの一致検査）。
- [ ] 実機確認は Step9（U-44）。
- [ ] `Phase8-Step7-Completion-Report.md`、`Phase8-Status.md`、`Phase8-Step2b-ControlsTab-Design.md` の更新。

## 8. 次

7.8f（Button／マウス／ホイール／アナログ。Default の扱い、`customMapQueue`。Button＋Extras の dcs も、ここで新経路に入る）。そのあと Step8（旧コードの削除）、Step9（総合確認）。
