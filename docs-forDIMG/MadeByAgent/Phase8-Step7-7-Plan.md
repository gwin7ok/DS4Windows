# Phase8-Step7-7 計画書: XboxGameDVR の削除と、MultiAction の移行

作成日: 2026-10-20  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Step7-Plan.md`（§4 の 7.7）、`Phase8-Step7-6-Plan.md`（進め方）、`Phase8-Step2-Design.md`（§4 の `MultiTapInputAction`、§24）、`Phase8-Step2-Intent-Decisions.md`（項目 7）、`Phase8-Step1-Trigger-Spec-Ledger.md`（XboxGameDVR／MultiAction）  
状態: **ユーザーの決定を反映済み（2026-10-20）: Q1＝A（XboxGameDVR は機能削除を前提）、Q2＝B（MultiAction を独立した入力アクションにする）、Q3〜Q7＝推奨（A）。7.7a（XboxGameDVR の削除）はビルド・テスト成功。7.7b（MultiAction）は W1 成功、W2 と、ユーザーの設計見直し（ジェスチャーから出力への対応を変換層に置く。W2b。§3.3）を実装済み［W2 と合わせて、ビルド・テスト成功。Q8＝A、Q9＝A を承認済み］。W3 のロールバックと割り当ては、W2 のテスト［`Phase8Step77bW2MultiTapTests`］に含まれる。次は実機確認 U-39**

---

## 1. 目的と分け方

| PR | 内容 |
|---|---|
| **7.7a XboxGameDVR の削除** | 機能削除（ユーザーが「この機能自体が不要」として、削除を前提にした計画を指示。規約 2.2 の「ユーザーの明示的な承認」にあたる）。旧経路の枝と、種別を削除する |
| **7.7b MultiAction の移行** | 独立した入力アクション `MultiTapInputAction` と、ジェスチャーを受ける出力 `MultiActionOutput` を新設し、エンジンを切り替える。運用は 7.1〜7.6 と同じ（`ServiceRegistration` の既定の一覧に 1 行。外すと旧経路に戻る。旧コードは Step8 まで残す） |

削除を先にするのは、旧経路の MultiAction の枝が `XboxGameDVR ||` と、`XboxGameDVR` 用のマクロの置き換え（整数 → 文字列）を含み、先に消した方が MultiAction の特性化と移行が単純になるため。

## 2. 現状の調査

### 2.1 旧経路の MultiAction（`Mapping.cs` 5132〜5295 行）

MultiAction は、他の種別と**構造が違う**。「トリガーが成立しているとき」ではなく、**毎フレーム、トリガーの 1 つ目（`trigger[0]`）の押下・解放のエッジ**を見て、タップの状態機械を動かす。

- 入力: `trigger[0]` の現在（`getBoolSpecialActionMapping`）と、デバイスの前回のレポート（`d.getPreviousStateRef()`）。
- 状態（`SpecialAction` のフィールド）: `tappedOnce`、`firstTouch`、`secondtouchbegin`、`pastTime`、`firstTap`、`TimeofEnd`。
- 時間: 間隔は **150ms ＋ `DebouncingMs[device]`**、長押しは **500ms ＋ `DebouncingMs[device]`**。時刻は `DateTime.UtcNow`。
  - 押した: 前回のタップの終わりから間隔以内なら、ダブルタップの開始。そうでなければ最初の接触。
  - 離した: ダブルタップの開始中なら状態を戻す。最初の接触なら、押した時間が間隔以内でタップ成立（時刻を保存）。
  - タップ: 終わりから間隔を過ぎたら、`details` の 1 つ目のマクロを実行し、状態を戻す（ダブルタップの余地のための遅延）。長押し: `firstTouch` のまま 500ms ＋ debounce 経過で 2 つ目のマクロ。ダブルタップ: 3 つ目のマクロ。
- マクロの実行: `PlayMacro(device, macroControl, <マクロの文字列>, null, null, DS4Controls.None, DS4KeyType.None)`。`SpecialAction` ではなく文字列を渡す。空なら何もしない。実行の直前に Trace 4 行。Debug はなし。
- ガード: 見ない。`BeingTriggered` を設定しない。トリガーの全部が押されている間のボタンの抑制・出力の消去は、共通の部分。
- 旧ループの位置で動く（前の特殊アクションが `return` したフレームでは、この枝に来ない）。

### 2.2 XboxGameDVR

- 構築時に、`details` の整数をマクロの文字列に置き換え、`type` の文字列を `MultiAction` に書き換える（`typeID` は `XboxGameDVR` のまま）。旧経路の分岐は `details` を整数として読み直すため、読み取りに失敗して**マクロは一度も実行されない**（W1 のテストで確認する）。
- 編集画面の種別の一覧にない（新しく作れない）。`Actions.xml` への保存では、`typeID` が `XboxGameDVR` の項目は**書き出されない**（`ActionsDTO` の分岐にない）。
- 参照箇所（削除の範囲）: `SpecialAction.cs`（列挙の値、構築の分岐）、`Mapping.cs`（5069 行の条件、5132 行の分岐、およびその中の整数からマクロへの置き換え［3 か所の `switch`］、4481 行のコメント）、`ActionBindingBuilder.cs` のコメント、`Phase8Step7W3ButtonSwitchTests` の 1 行。
- 古い `Actions.xml` に `<Type>XboxGameDVR</Type>` の項目が残っている場合: 削除後は種別が不明（`None`）の項目になり、何も実行されず、次の保存で書き出されない（今も保存では書き出されないため、実質同じ）。プロファイルの特殊アクションの一覧に名前だけが残ると「見つからない項目」（灰色）として表示される（既存の動き）。

### 2.3 新経路で足りないもの

- `TriggerState` に `Gesture`（`NoGesture`／`Tap`／`Hold`／`DoubleTap`）と、2 引数のコンストラクターは、Step3 で用意済み。エンジンは `Gesture` を**まだ使わない**。
- `ITriggerInput.WasActive`（前フレームの状態）は用意済み（MultiAction のエッジ判定用）。

## 3. 方針

### 3.1 7.7a XboxGameDVR の削除

- `SpecialAction.ActionTypeId` から `XboxGameDVR` を削除し、`SpecialAction` の構築の `XboxGameDVR` の分岐を削除する（古い項目は、種別が不明の項目になる）。
- `Mapping.cs` から、`XboxGameDVR` の条件と、整数からマクロへの置き換えを削除する（MultiAction の枝は、`details` の 3 つの文字列をそのままマクロにする形に単純化する。動きは変えない）。コメントを更新する。
- テスト: 新規 — 列挙に `XboxGameDVR` がないこと、`Type` が `XboxGameDVR` の項目を作っても例外にならず、種別が `None` になること、MultiAction の旧経路の動き（`details` の文字列がそのままマクロになる）が変わらないこと。既存の更新 — `Phase8Step7W3ButtonSwitchTests`。
- 実機確認は要らない（新しく作れず、動いていない機能の削除のため）。

**実装（2026-10-20）**: `SpecialAction.cs`（列挙の値と構築の分岐を削除。古い項目は種別が不明の項目になる）、`Mapping.cs`（`XboxGameDVR` の条件と、整数からマクロへの置き換え［3 か所の `switch`］、未使用の変数を削除。MultiAction の枝は `details` の 3 つの文字列をそのままマクロにする）、`ActionBindingBuilder.cs` のコメント、`Phase8Step7W3ButtonSwitchTests` の更新、新規 `Phase8Step77aXboxGameDvrRemovalTests`（列挙にない、古い項目が例外にならず `None` になる、MultiAction の `details` が保たれる、ソースに残っていない）。ビルド・テスト成功（2026-10-20）。

### 3.2 7.7b MultiAction の移行（Q2＝B: 独立した入力アクション）

> **注（W2b。2026-10-20）**: 以下の `IGestureOutputAction`／`MultiActionOutput`／エンジンの `OnGesture` の呼び出しは、§3.3 の設計見直しで、`MacroStringAction`（汎用の出力）と `BindingPolicy.RoutesOutputsByGesture`（エンジンが `Outputs[Gesture − 1]` を実行）に置き換わった。`IMultiActionMacroPlayer` は `IMacroStringPlayer`、`DefaultMultiActionMacroPlayer` は `DefaultMacroStringPlayer` に改名した。それ以外（`MultiTapInputAction`、状態の置き場、方針、旧経路との違い）は、そのまま。

- **`MultiTapInputAction`（新設。`IInputAction`）**: `ComboTriggerInputAction`（トリガーの AND、`delayTime`）を中に持ち、`IsEstablished` はそれと同じ（トリガーのボタンの抑制・出力の消去を、エンジンの共通の方針で行うため）。それに加えて、**毎フレーム、`trigger[0]` のエッジ（`IsActive`／`WasActive`）と時刻（`ITriggerInput.UtcNow`）と `DebouncingMs[device]` から、旧経路と同じ状態機械を動かし**、その結果を `TriggerState.Gesture`（タップ／長押し／ダブルタップ）として返す。状態（`tappedOnce` ほか）はこのインスタンスが持つ（Q3＝A）。`Reset()` で状態を戻す。
- **`IGestureOutputAction`（新設。`IOutputAction` の拡張）**: `OnGesture(IOutputContext ctx, int gesture)`。
- **`MultiActionOutput`（新設）**: ジェスチャーに対応するマクロの文字列（`details` を「,」で分けた 3 つ。足りなければ空）が空でなければ、Trace 4 行（旧 `LogSpecialActionTrace`）→ `IMultiActionMacroPlayer.PlayMacroString(device, macro)`（Q4＝A）。Debug はなし（Q5＝A）。
- **エンジンの変更（小さい）**: `ProcessBinding` が `Evaluate` の結果を得た直後（ガードや成立の判定の**前**）に、`Gesture` が 0 でなければ、出力の `IGestureOutputAction.OnGesture` を呼ぶ。`BeingTriggered`・`IsExecuting` は変えない。ガードで止めない（旧経路もガードを見ない）。例外は握りつぶして記録する（既存の方式）。
- **位置**: 旧ループの、そのアクションの位置で動く（`ProcessOwned`）。前の特殊アクションが `return` したフレームでは来ない、という旧経路の性質も保たれる（旧経路との違いなし）。
- **方針（`BindingPolicy`）**: `respectsFreshPressGuard: false`、`blocksWhileExecuting: false`、`stopOnRelease: false`（出力の `Stop` は不要）。トリガーのボタンの抑制・出力の消去は既定。
- **`IMultiActionMacroPlayer`（新設。`Actions/`）**: `PlayMacroString(int device, string macro)`。`Mapping` が実装する（旧 `PlayMacro(…, 文字列, …)` を呼ぶだけ）。DI は、`IMapping` と同じインスタンスを返す（`IMapping` は 10 メンバーのまま）。
- **ビルダー**: `MultiAction` の束縛。窓口（プレーヤー）がなければ作らない。
- **旧経路との違い**: 時刻を入力（`ITriggerInput.UtcNow`。本番は `DateTime.UtcNow`）から取る点、状態が `SpecialAction` のフィールドではなくインスタンスにある点。`details` の足りない部分は、旧経路は例外になるが、新経路は空として扱う。

### 3.3 設計の見直し（2026-10-20。ユーザーの提案）: 「どのジェスチャーで何をするか」の決定は、信号変換層に置く

**背景**: W2（初版）の `MultiActionOutput` は、ジェスチャー（タップ／長押し／ダブルタップ）に対応するマクロの文字列を、出力アクションの中で選んでいる（`details` を「,」で分けた3つの中から選ぶ）。ユーザーから、「どのアクションを行うかの決定は、出力層ではなく信号変換層が担当し、決めたうえで出力層へ渡す形がよい」との提案があった。妥当と判断する。

**層の対応（改訂後）**

| 層 | この機能での担当 |
|---|---|
| 1. 入力監視層 | 押下状態、前フレーム、時計（`DS4Device`、`DS4State`、`ITriggerInput`: `IsActive`／`WasActive`／`UtcNow`）。解釈しない |
| 2. 信号変換層 | トリガーの AND と遅延（`ComboTriggerInputAction`）、エッジと時間によるジェスチャーの判定（`MultiTapInputAction`、状態もここ）、**ジェスチャーから「どの出力を実行するか」への対応（束縛の構成。ビルダーが決める）**、エンジンによる出力の選択・ガード・抑制 |
| 3. 信号・アクション出力層 | 渡された命令の実行だけ: 「このマクロの文字列を再生する」（ログ、`IMacroStringPlayer`、`Mapping.PlayMacro`、キー送出） |

**改訂後の設計（W2 初版との差分）**

- **ビルダー（変換層側）**: `details` を「,」で分けた3つ（足りなければ空）から、**ジェスチャーごとの出力を3つ**（タップ、長押し、ダブルタップの順）作って束縛の `Outputs` に持たせる。対応は、ここで決まる。
- **汎用の出力 `MacroStringAction`（新設。`MultiActionOutput` の置き換え）**: 1つのマクロの文字列を持ち、実行されたとき、空でなければ Trace 4 行（旧 `LogSpecialActionTrace`）→ `IMacroStringPlayer.PlayMacroString(device, macro)`。ジェスチャーも `details` の構造も知らない（MultiAction 専用でなくなる）。
- **`BindingPolicy` に項目を追加**（既定は偽）: 「出力をジェスチャーで選ぶ」。真のとき、エンジンは、(1) 通常の立ち上がり・保持・離したときの出力の実行をしない（抑制とトリガーの出力の消去は、今までどおり行う）。(2) `Gesture` が0でないフレームだけ、`Outputs[Gesture − 1]` を実行する。ガード（Fresh-press、実行中）と `BeingTriggered` の対象外であることは、W2 初版と同じ。
- **削除するもの**: `IGestureOutputAction`、`MultiActionOutput`。エンジンの `is IGestureOutputAction` のループは、上の「出力の選択」に置き換わる。
- **名前の整理**: マクロの文字列を再生する窓口は、MultiAction 専用でなくなるため、`IMultiActionMacroPlayer` → `IMacroStringPlayer`、`DefaultMultiActionMacroPlayer` → `DefaultMacroStringPlayer` に改名する（Q8 に含む）。
- **変わらないもの**: `MultiTapInputAction`（判定と状態）、`TriggerState.Gesture`、旧ループの位置で動くこと、旧経路との動作の差（W1 の期待値、ログ、時刻の取り方）、`Mapping` の窓口の実装（`PlayMacroString`）、既定の一覧の 1 行。
- **旧経路との動作の差は増えない**。出力が汎用になるだけで、再生されるマクロと、ログの文言・順序は同じ。

## 4. 作業

- **7.7a**: W1 削除と、テスト（上記）。ビルド・テスト。
- **7.7b**
  - **W1（特性化。2026-10-20 ビルド・テスト成功）**: `Phase8Step77bW1MultiActionDualPathTests`（新設）と `TestDs4Device`（テスト用のデバイス。前回のレポートだけを返す）。旧経路は `d.getPreviousStateRef()` を使うため、スロットにテスト用のデバイスを置く。時刻は旧経路が `DateTime.UtcNow` を使うため、実際に待つ（待ち時間に余裕を持たせる。全体で約 10 秒）。内容: タップ［離した直後は実行しない、間隔後に 1 つ目を 1 回だけ］、長押し［2 つ目を 1 回。離してもタップは実行しない］、ダブルタップ［3 つ目のみ］、空のマクロ、`DebouncingMs`（200 で、タップの間隔が 350ms になる）、Fresh-press ガードを見ない、押している間トリガーの出力が消える、前の特殊アクション［Profile の切替］のフレームの打ち切りで押下を見逃す。旧・新の両経路（W1 の時点では、新経路の指定でも旧経路が処理する）。計画: `SpecialActionTestScope` に MultiAction。旧経路を、`PlayMacro` の記録（`Mapping` の窓口の差し替え）と、入力の時刻の差し込みで固定する: タップ（遅延後に 1 つ目）、長押し、ダブルタップ、間隔と `DebouncingMs`、マクロが空のとき、トリガーの抑制・出力の消去、前の特殊アクションの `return` のフレームでの見逃し。時刻の足場が足りなければ追加する。
  - **W1 は成功（ビルド・テスト。2026-10-20）。W2（実装。W2b と合わせて、ビルド・テスト成功［2026-10-20］）**: 新設 — `IGestureOutputAction`、`MultiTapInputAction`（`ComboTriggerInputAction` を中に持ち、`trigger[0]` のエッジと時刻と `DebouncingMs` から旧経路と同じ状態機械を動かし `Gesture` を返す）、`MultiActionOutput`、`IMultiActionMacroPlayer`、`DefaultMultiActionMacroPlayer`（`Func<IMapping>` から `Mapping` へ委譲）。変更 — `TriggerDispatchEngine.ProcessBinding`（`Evaluate` の直後に、`Gesture` が 0 でなければ出力の `OnGesture` を呼ぶ。ガード・成立・`BeingTriggered` によらない）、`Mapping`（`IMultiActionMacroPlayer.PlayMacroString` を実装。`IMapping` は 10 メンバーのまま）、`ActionBindingBuilder`（MultiAction の束縛。`respectsFreshPressGuard: false`、`stopOnRelease: false`）、`ServiceRegistration`（窓口の登録、既定の一覧に MultiAction の 1 行）。テスト: `Phase8Step77bW2MultiTapTests`（偽の入力［時刻と押下を差し込める］での状態機械、出力、ビルダー、ロールバック、窓口、旧・新のログの一致、割り当て）。計画: 上記の新設と変更、既定の一覧の 1 行。旧・新の両経路で、W1 の期待値を変えずに成功。ログの一致。
  - **W2b（設計の見直し。§3.3。Q8＝A、Q9＝A 承認済み。2026-10-20 実装済み。W2 と合わせて、ビルド・テスト成功［2026-10-20］）**: 上記のとおり — `BindingPolicy.RoutesOutputsByGesture`（追加。既定は偽）、`TriggerDispatchEngine`（`Gesture` が0でないフレームだけ `Outputs[Gesture − 1]` を実行。この項目が真の束縛は、抑制と出力の消去のあと、通常の出力の実行をしない）、`MacroStringAction`（新設）、`IGestureOutputAction`／`MultiActionOutput`（削除）、`IMacroStringPlayer`／`DefaultMacroStringPlayer`（改名）、`ActionBindingBuilder`（`details` の3つから、ジェスチャーごとの出力を3つそろえて作る）、`Mapping`（実装するインターフェース名）、`ServiceRegistration`（登録名）。テストの更新: `Phase8Step77bW2MultiTapTests`（`MacroStringAction` のテスト、ビルダーが3つの出力を作り、ジェスチャーごとのマクロに対応づけるテスト［空・足りない場合］、`BindingPolicy` の既定値、窓口の改名）。ジェスチャーでの出力の選択は、W1 の旧・新の両経路のテスト［タップ・長押し・ダブルタップがそれぞれ正しいマクロを1回だけ再生する］が確かめる。W1 と、`MultiTapInputAction` の状態機械のテスト、旧・新のログの一致、割り当てのテストは、期待値を変えずに成功すること。
  - **W3（W2 のテストに含めて成功）**: ロールバック［`RemovingTheLine_BringsBackTheLegacyPath_NoBindingIsBuilt`］、割り当て［`TheNewPath_DoesNotAddAllocationsToTheFrame_WhileHeld`］。
  - **W4**: 実機確認 U-39（PS 単押しで地図など）。報告書。

## 5. 質問と決定

- **Q1（XboxGameDVR）**: **A（ユーザーの決定: この機能自体が不要なので、機能削除を前提に計画）**。7.7a で削除する。
- **Q2（設計）**: **B（ユーザーの決定: MultiAction を独立させたい）**。独立した `MultiTapInputAction` と、ジェスチャーを受ける出力を新設し、エンジンに小さいフックを足す（3.2）。`ApplyFrame` に乗せる案は採らない。
- **Q3（状態の置き場）**: A（推奨）。入力アクションのインスタンス。旧 `SpecialAction` のフィールドは、旧経路のために残す。
- **Q4（マクロの実行の窓口）**: A（推奨）。`IMultiActionMacroPlayer` を新設し、`Mapping` が実装する。
- **Q5（ログ）**: A（推奨）。旧経路と同じ（マクロを実行する直前の Trace 4 行。Debug なし）。
- **Q6（時間）**: A（推奨）。150ms／500ms ＋ `DebouncingMs` を、旧経路どおり。
- **Q7（実機確認）**: A（推奨）。既存の MultiAction で、ユーザーが確認する。機材がなければ先送りの記録。
- **Q8（ジェスチャーから出力への対応の置き場。ユーザーの提案）【承認: A】**: A（推奨）: 変換層（ビルダーが作る束縛）に置き、出力層は汎用の「マクロの文字列を再生する」出力だけにする（§3.3）。窓口と実装の名前も汎用にする。B: W2 初版のまま（`MultiActionOutput` が、ジェスチャーに対応するマクロを選ぶ）。
- **Q9（W2b を入れる時期）【承認: A】**: A（推奨）: W2（初版）はまだビルド・テストの結果が出ておらず、コミットもされていないため、**W2b を先に適用して**から、1 回のビルド・テストで確認する（作り直しの往復が減る）。B: W2（初版）のビルド・テストの結果を見てから、W2b を別の波として適用する（初版の成否と、見直しの成否を、切り分けられる）。

### W2 の最初の結果と是正（2026-10-20）

W2 のビルド・テストで、W1 の新経路（`InlineData(true)`）の 5 件と、旧・新のログの比べの 1 件が失敗した（新経路でマクロが実行されない）。原因: `Mapping.MapCustom` が `TriggerInput.Update` に**前フレームの状態を `null` で渡していた**（Step4 の配線。それまで `WasActive` を使う種別がなかった）ため、MultiAction の押下・解放のエッジが見えなかった。是正: `Mapping.MapCustom` が、デバイスの前回のレポート（`ctrl.DS4Controllers[device]?.getPreviousStateRef()`。旧経路と同じ）を渡す。前フレームの表は `WasActive` が最初に呼ばれたときにだけ作る（遅延）ため、MultiAction がなければ費用は増えない。デバイスがなければ `null`（従来どおり）。

## 6. リスクと対応

- **時間の再現**: 150ms／500ms を待たずに確認するため、`TriggerInput` の時刻の差し替えを使う。
- **エッジの定義**: `ITriggerInput.WasActive` と、旧 `getPreviousStateRef`（デバイスの前回のレポート）が同じ結果になることを、W1 の旧・新の比較で確かめる。違いが出たら記録して決める。
- **`delayTime` との関係**: `ComboTriggerInputAction` が持つ `delayTime`（基準時刻の更新）を、旧経路と同じ回数だけ評価する（1 フレーム 1 回）。
- **エンジンの変更**: 既存の種別に影響しない（`Gesture` が 0 のとき何もしない）。割り当て 0 を保つ。
- **ロールバック**: 7.7b は `ServiceRegistration` の MultiAction の 1 行を外す。7.7a の削除は、コミットを戻す。
- **W2b の影響範囲**: `BindingPolicy`（項目の追加。既存の種別は既定の偽で、動きが変わらない）、エンジン（出力の選択。`Gesture` が0のとき、および項目が偽のとき、何もしない）、`MultiAction` の束縛だけ。`Phase8Step4W1ContractTests` などの、`BindingPolicy` の形を確認するテストの更新が要る可能性がある。
- **空のマクロ**: ジェスチャーに対応するマクロが空でも、出力は3つそろえて作る（添字を固定するため）。`MacroStringAction` が、空のとき何もしない。
- **出力の選択の添字**: `Gesture − 1` が `Outputs` の範囲外なら、何もしない（旧経路の「足りない部分は空」と同じ扱い）。

## 7. 完了条件

- [ ] 7.7a: ビルド・テストが成功（ユーザーが実行）。全テスト・ラチェットの一致検査・1 ファイル 1 型のガードが成功。
- [ ] 7.7b: W1〜W3 のビルド・テストが成功。旧・新の両経路で、タップ・長押し・ダブルタップの期待値が一致。`IMapping` が 10 メンバーのまま。割り当て（新 ≦ 旧）。ロールバックのテストが成功。
- [ ] 設計の見直し（W2b。Q8・Q9）が反映され、`IGestureOutputAction`／`MultiActionOutput` がなくなっていること（Q8＝A の場合）。
- [ ] 実機確認 U-39 が合格（または先送りの記録）。
- [ ] `Phase8-Step7-Completion-Report.md`、`Phase8-Status.md`、チェックリストの更新。

## 8. 次

7.8（Controls タブ。別の設計書［`Phase8-Step2b-ControlsTab-Design.md`］を先に作る）。その後 Step8（旧コードの削除）。
