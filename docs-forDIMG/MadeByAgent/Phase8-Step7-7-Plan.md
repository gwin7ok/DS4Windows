# Phase8-Step7-7 計画書: XboxGameDVR の削除と、MultiAction の移行

作成日: 2026-10-20  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Step7-Plan.md`（§4 の 7.7）、`Phase8-Step7-6-Plan.md`（進め方）、`Phase8-Step2-Design.md`（§4 の `MultiTapInputAction`、§24）、`Phase8-Step2-Intent-Decisions.md`（項目 7）、`Phase8-Step1-Trigger-Spec-Ledger.md`（XboxGameDVR／MultiAction）  
状態: **ユーザーの決定を反映済み（2026-10-20）: Q1＝A（XboxGameDVR は機能削除を前提）、Q2＝B（MultiAction を独立した入力アクションにする）、Q3〜Q7＝推奨（A）。7.7a（XboxGameDVR の削除）の実装済み［ビルド・テストの結果待ち］。7.7b（MultiAction）は 7.7a の後**

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

**実装（2026-10-20）**: `SpecialAction.cs`（列挙の値と構築の分岐を削除。古い項目は種別が不明の項目になる）、`Mapping.cs`（`XboxGameDVR` の条件と、整数からマクロへの置き換え［3 か所の `switch`］、未使用の変数を削除。MultiAction の枝は `details` の 3 つの文字列をそのままマクロにする）、`ActionBindingBuilder.cs` のコメント、`Phase8Step7W3ButtonSwitchTests` の更新、新規 `Phase8Step77aXboxGameDvrRemovalTests`（列挙にない、古い項目が例外にならず `None` になる、MultiAction の `details` が保たれる、ソースに残っていない）。ビルド・テストの結果待ち。

### 3.2 7.7b MultiAction の移行（Q2＝B: 独立した入力アクション）

- **`MultiTapInputAction`（新設。`IInputAction`）**: `ComboTriggerInputAction`（トリガーの AND、`delayTime`）を中に持ち、`IsEstablished` はそれと同じ（トリガーのボタンの抑制・出力の消去を、エンジンの共通の方針で行うため）。それに加えて、**毎フレーム、`trigger[0]` のエッジ（`IsActive`／`WasActive`）と時刻（`ITriggerInput.UtcNow`）と `DebouncingMs[device]` から、旧経路と同じ状態機械を動かし**、その結果を `TriggerState.Gesture`（タップ／長押し／ダブルタップ）として返す。状態（`tappedOnce` ほか）はこのインスタンスが持つ（Q3＝A）。`Reset()` で状態を戻す。
- **`IGestureOutputAction`（新設。`IOutputAction` の拡張）**: `OnGesture(IOutputContext ctx, int gesture)`。
- **`MultiActionOutput`（新設）**: ジェスチャーに対応するマクロの文字列（`details` を「,」で分けた 3 つ。足りなければ空）が空でなければ、Trace 4 行（旧 `LogSpecialActionTrace`）→ `IMultiActionMacroPlayer.PlayMacroString(device, macro)`（Q4＝A）。Debug はなし（Q5＝A）。
- **エンジンの変更（小さい）**: `ProcessBinding` が `Evaluate` の結果を得た直後（ガードや成立の判定の**前**）に、`Gesture` が 0 でなければ、出力の `IGestureOutputAction.OnGesture` を呼ぶ。`BeingTriggered`・`IsExecuting` は変えない。ガードで止めない（旧経路もガードを見ない）。例外は握りつぶして記録する（既存の方式）。
- **位置**: 旧ループの、そのアクションの位置で動く（`ProcessOwned`）。前の特殊アクションが `return` したフレームでは来ない、という旧経路の性質も保たれる（旧経路との違いなし）。
- **方針（`BindingPolicy`）**: `respectsFreshPressGuard: false`、`blocksWhileExecuting: false`、`stopOnRelease: false`（出力の `Stop` は不要）。トリガーのボタンの抑制・出力の消去は既定。
- **`IMultiActionMacroPlayer`（新設。`Actions/`）**: `PlayMacroString(int device, string macro)`。`Mapping` が実装する（旧 `PlayMacro(…, 文字列, …)` を呼ぶだけ）。DI は、`IMapping` と同じインスタンスを返す（`IMapping` は 10 メンバーのまま）。
- **ビルダー**: `MultiAction` の束縛。窓口（プレーヤー）がなければ作らない。
- **旧経路との違い**: 時刻を入力（`ITriggerInput.UtcNow`。本番は `DateTime.UtcNow`）から取る点、状態が `SpecialAction` のフィールドではなくインスタンスにある点。`details` の足りない部分は、旧経路は例外になるが、新経路は空として扱う。

## 4. 作業

- **7.7a**: W1 削除と、テスト（上記）。ビルド・テスト。
- **7.7b**
  - **W1（特性化）**: `SpecialActionTestScope` に MultiAction。旧経路を、`PlayMacro` の記録（`Mapping` の窓口の差し替え）と、入力の時刻の差し込みで固定する: タップ（遅延後に 1 つ目）、長押し、ダブルタップ、間隔と `DebouncingMs`、マクロが空のとき、トリガーの抑制・出力の消去、前の特殊アクションの `return` のフレームでの見逃し。時刻の足場が足りなければ追加する。
  - **W2（実装）**: 上記の新設と変更、既定の一覧の 1 行。旧・新の両経路で、W1 の期待値を変えずに成功。ログの一致。
  - **W3**: ロールバック、割り当て（新 ≦ 旧）。
  - **W4**: 実機確認 U-39（PS 単押しで地図など）。報告書。

## 5. 質問と決定

- **Q1（XboxGameDVR）**: **A（ユーザーの決定: この機能自体が不要なので、機能削除を前提に計画）**。7.7a で削除する。
- **Q2（設計）**: **B（ユーザーの決定: MultiAction を独立させたい）**。独立した `MultiTapInputAction` と、ジェスチャーを受ける出力を新設し、エンジンに小さいフックを足す（3.2）。`ApplyFrame` に乗せる案は採らない。
- **Q3（状態の置き場）**: A（推奨）。入力アクションのインスタンス。旧 `SpecialAction` のフィールドは、旧経路のために残す。
- **Q4（マクロの実行の窓口）**: A（推奨）。`IMultiActionMacroPlayer` を新設し、`Mapping` が実装する。
- **Q5（ログ）**: A（推奨）。旧経路と同じ（マクロを実行する直前の Trace 4 行。Debug なし）。
- **Q6（時間）**: A（推奨）。150ms／500ms ＋ `DebouncingMs` を、旧経路どおり。
- **Q7（実機確認）**: A（推奨）。既存の MultiAction で、ユーザーが確認する。機材がなければ先送りの記録。

## 6. リスクと対応

- **時間の再現**: 150ms／500ms を待たずに確認するため、`TriggerInput` の時刻の差し替えを使う。
- **エッジの定義**: `ITriggerInput.WasActive` と、旧 `getPreviousStateRef`（デバイスの前回のレポート）が同じ結果になることを、W1 の旧・新の比較で確かめる。違いが出たら記録して決める。
- **`delayTime` との関係**: `ComboTriggerInputAction` が持つ `delayTime`（基準時刻の更新）を、旧経路と同じ回数だけ評価する（1 フレーム 1 回）。
- **エンジンの変更**: 既存の種別に影響しない（`Gesture` が 0 のとき何もしない）。割り当て 0 を保つ。
- **ロールバック**: 7.7b は `ServiceRegistration` の MultiAction の 1 行を外す。7.7a の削除は、コミットを戻す。

## 7. 完了条件

- [ ] 7.7a: ビルド・テストが成功（ユーザーが実行）。全テスト・ラチェットの一致検査・1 ファイル 1 型のガードが成功。
- [ ] 7.7b: W1〜W3 のビルド・テストが成功。旧・新の両経路で、タップ・長押し・ダブルタップの期待値が一致。`IMapping` が 10 メンバーのまま。割り当て（新 ≦ 旧）。ロールバックのテストが成功。
- [ ] 実機確認 U-39 が合格（または先送りの記録）。
- [ ] `Phase8-Step7-Completion-Report.md`、`Phase8-Status.md`、チェックリストの更新。

## 8. 次

7.8（Controls タブ。別の設計書［`Phase8-Step2b-ControlsTab-Design.md`］を先に作る）。その後 Step8（旧コードの削除）。
