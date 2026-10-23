# R-2 マクロの「繰り返しをトグルする」再生モード 実装計画書

作成日: 2026-10-23
状態: **承認済み（2026-10-23）。W1 完了、W2 実装済み（ビルド・テストの確認待ち）**
関連: [Issue #1](https://github.com/gwin7ok/DS4Windows/issues/1)、`Release-Backlog.md` R-2

## 1. 目的と仕様案

マクロの Execute Mode に、第 3 の選択肢を追加する。

| Execute Mode | 動作（現行） |
|---|---|
| Play Once | トリガーが成立したとき、1 回だけ再生する |
| Repeat While Held | トリガーを押している間、繰り返す |
| **Repeat (Toggle)**（新規） | トリガーを押すたびに、繰り返しの「開始」と「停止」が切り替わる |

- 開始: トリガーの立ち上がりで、状態を ON にし、マクロを再生する。トリガーを離しても、繰り返しは続く。
- 停止: もう一度トリガーが立ち上がったとき、状態を OFF にする。**実行中の 1 回分は最後まで再生してから止まる**（Repeat While Held でトリガーを離したときと同じ）。
- 対象: SA の Macro タブと Controls の Macro の両方（共通部品 `MacroPlaybackOptionsPanel` を使うため、同時に対応できる）。
- 対象外: Press Key／MultiAction のマクロ（現状どおり、繰り返しを選べない）。

## 2. 現状の調査（コードで確認した事実）

- **再生**: `MacroPlayback.PlayMacroTask`（`DS4Control/Services/MacroPlayback.cs`）が、`keyType` に `RepeatMacro` があるとき、1 回再生した後、`ActionManager.IsBeingTriggered(action, device)`（＝`ActionInstanceState.BeingTriggered`）が真の間、繰り返す。繰り返しの条件はここだけ。
- **トリガー**: `TriggerDispatchEngine` は、立ち上がりで `MacroAction.Execute` → `IMacroPlayer.Play` を呼ぶ。離したとき `Stop` を呼ぶが、現状の `EndMacroDirect` は空実装（マクロの停止は、ループが `BeingTriggered` を見て止まる）。
- **トグルの状態**: `ActionInstanceState.IsToggledOn` と `IManagedActionManager.SetToggledOn` が既にある（Key の Toggle、`ButtonOutputAction` のトグルが使う）。`ClearDeviceState`（切断・プロファイル切替）は `ActionInstanceState` を**新しいオブジェクトに差し替える**ため、状態は自動で OFF に戻る。
- **保存**: `DS4KeyType`（`byte`）。値 1・2・4・8・32 を使用中。16 は欠番、64・128 が空き。
  - Controls: プロファイルの `KeyType` ノードに**名前の文字列**で保存。読込は `Contains` による部分一致（`ProfileButtonControlLoadSection`）。`Toggle` の名前は部分一致で `Toggle` フラグになり、Controls では `ControlBindingBuilder` などが `Toggle` を特別扱いする（Key／Button 用）。
  - SA: Actions.xml の `extras` に `/` 区切りのトークン（`Scan Code`、`RunOnRelease`、`Sync`、`KeepKeyState`、`Repeat`）。読込は `Contains`。
- **画面**: `MacroPlaybackOptions`（ViewModel。`RepeatWhileHeld`、`PlayModeIndex`）と `MacroPlaybackOptionsPanel`（コードだけの部品。ComboBox に 2 項目）。保存は `MacroViewModel.SaveAction`（SA）と `RecordBoxViewModel`／`OutBinding`（Controls）。説明文は `AssignmentDescriber`。

## 3. 設計

### 3.1 保存形式（ユーザー決定: 2026-10-23）

ユーザーの決定:
- **Execute Mode は、独立した設定項目として保存する**（`extras` の文字列や `KeyType` の名前の連なりに入れない）。3 つのモードすべてを移す。
- **新しい版が書いた設定を古い版で読む互換（ダウングレード）は、放棄する**。古い版が書いた設定を新しい版で読む互換は、残す（読込時に新しい形へ変換する）。
- 他のモード的な設定（`extras` などの文字列に複数の値を入れているもの）の独立化は、今回は行わない（§8）。

名称は、画面のラベル「Execute Mode」に合わせて **`MacroExecuteMode`**。値は `Once`／`Held`／`Toggle`。

**SA（Actions.xml）**
- `SpecialActionSerializer` に、独立した要素 `<MacroExecuteMode>` を追加する（`ProfileId`、`OutputExtras` と同じ形。種別が Macro のときだけ、`ShouldSerializeMacroExecuteMode` で書く）。
- 保存: `MacroViewModel.SaveAction` は、`extras` に `Repeat` を書かなくなる。`Scan Code`／`RunOnRelease`／`Sync`／`KeepKeyState` は、今回は `extras` のまま。
- 読込: `<MacroExecuteMode>` があればそれを使う。なければ、古い形式として `extras` の `Repeat` を `Held` に変換する（次に保存したとき、新しい形になる）。
- `Once`（既定）のときは、要素を書かない。新しい形式のファイルの Extras には `Repeat` が出ないため、要素がなければ、古い形式として `Repeat` を見ても、結果は `Once` で矛盾しない（実装: W2。`ActionsDTO`）。
- 保存する値は、`SaveAction` の新しい引数 `MacroExecuteMode`（列挙型。`Once`／`Held`／`Toggle`）で、画面から渡す（`extras` の文字列には混ぜない）。変換は `MacroExecuteModes`。

**Controls（プロファイル XML）**
- `Control` の下に、ボタンごとの要素を持つ新しいノード `<MacroExecuteMode>` を追加する（既存の `<Macro>` ノードと同じ構造。要素名＝ボタン名、値＝`Once`／`Held`／`Toggle`）。
- 保存: `KeyType` ノードには、`RepeatMacro` を書かなくなる（`Macro`、`ScanCode`、`Toggle` などは今回は `KeyType` のまま）。`Once` のボタンは、ノードに書かない。
- 読込: `<MacroExecuteMode>` があれば、それを使う（マクロの割り当てのボタンだけ）。なければ（または値が不明なら）、古い形式として `KeyType` の `RepeatMacro`／`HoldMacro` を `Held` に変換する。

**実行時の表現（変えない）**
- メモリ上の `keyType` のフラグは、`RepeatMacro`（＝`Held`）に加えて、新しいフラグ `RepeatLatchMacro = 64`（＝`Toggle`）を持つ。エンジン（`MacroAction`、`MacroPlayback`）は、フラグを見る（Controls と SA で、実行の経路が共通のため）。保存と読込の層だけが、`MacroExecuteMode` とフラグを変換する。
- `RepeatLatchMacro` は、`Toggle`、`RepeatMacro` の名前を含まないため、読込の部分一致の衝突は起きない（実行時のフラグであり、`KeyType` の文字列には書かない）。

### 3.2 実行（エンジン側）

1. `MacroAction.Execute`（立ち上がり）: `keyType` が `RepeatLatchMacro` のとき、`IManagedActionManager`（`ButtonOutputAction` と同じ依存。`SpecialActionOutputFactory` が持つ）で `IsToggledOn` を反転する。
   - 反転後が ON → `macroPlayer.Play` を呼ぶ。
   - 反転後が OFF → `Play` は呼ばない（実行中のループが、次の確認で止まる）。
2. `MacroPlayback.PlayMacroTask` の繰り返し条件を、モードで切り替える（`RepeatMacro`＝`IsBeingTriggered`、`RepeatLatchMacro`＝**`ActionManager.GetStateFor(action, device).IsToggledOn`**）。
   - 状態は**毎回 `GetStateFor` で取り直す**（`ClearDeviceState` が状態オブジェクトを差し替えるため、最初に持った参照は古くなる）。差し替え後は新しい状態の `IsToggledOn` が偽なので、切断・プロファイル切替でループが止まる。
   - `shouldSync`（同期キュー）の条件に、`RepeatLatchMacro` も含める（`RepeatMacro` と同じ。重なり実行の防止）。
3. 取りこぼしの防止（連続操作）: トグルを OFF→ON と素早く切り替えたとき、前のループがまだ動いていれば、そのまま続く（`IsMacroRunning` の開始時ガードが、新しい `Play` を破棄する）。同期キューで遅れて始まった `Play` は、始まったとき `IsToggledOn` が偽なら、何もせず終わる（`PlayMacroTask` の先頭で確認）。
4. ログ: 既存の `PlayMacroTask ITERATION CHECK` の `DECISION` に、モード（`HELD`／`LATCH`）を足す。トグルの反転は `[DI]` ではなく、既存の `SetToggledOn` のトレースに任せる（追加の接頭辞は不要）。

### 3.3 画面

- `MacroPlaybackOptions`: プロパティ `RepeatLatch` を追加。`RepeatWhileHeld` と排他（どちらかを真にしたら、もう一方を偽にする）。`PlayModeIndex` は 0＝Play Once、1＝Repeat While Held、**2＝Repeat (Toggle)**。`LoadFrom`／`ApplyTo` に `RepeatLatchMacro` を追加（`Repeatable` が偽でも値を消さない現行方針は保つ）。
- `MacroPlaybackOptionsPanel`: ComboBox に 3 項目目を追加（`Strings.RecordBox_RepeatToggle`）。
- 文字列: `Strings.resx`（英語）と `Strings.Designer.cs` に追加。他言語の resx は英語のままにする（未翻訳は既定にフォールバック）。
- `AssignmentDescriber`: 説明文に `Repeat (toggle)` を追加（現状は `Repeat while held`／`Play once` の 2 通り）。
- Controls の `OutBinding`／`RecordBoxViewModel`: `RepeatLatchMacro` の読み書きを追加。

## 4. 作業の手順（マイクロステップ）

| Step | 内容 | 主なファイル |
|---|---|---|
| W1 | `DS4KeyType.RepeatLatchMacro` と `MacroPlaybackOptions.RepeatLatch`（排他、`LoadFrom`／`ApplyTo`）。単体テスト | `DS4KeyType.cs`、`MacroPlaybackOptions.cs` |
| W2 | 保存と読込（SA の `<MacroExecuteMode>`、Controls の `<MacroExecuteMode>` ノード）。往復テスト、古い形式［`extras` の `Repeat`、`KeyType` の `RepeatMacro`／`HoldMacro`］の読込テスト | `SpecialActionSerializer.cs`、`ActionsDTO.cs`、`SpecialAction.cs`、`MacroViewModel.cs`、`ProfileButtonSaveSection`、`ProfileButtonControlLoadSection` |
| W3 | 画面（3 項目目、文字列、説明文、Controls の `OutBinding`）。ViewModel のテスト | `MacroPlaybackOptionsPanel.cs`、`Strings.*`、`AssignmentDescriber.cs`、`OutBinding.cs`、`RecordBoxViewModel.cs` |
| W4 | 実行（`MacroAction` の反転、`MacroPlayback` の繰り返し条件、同期キュー、先頭の確認）。ループの停止（OFF、状態の差し替え）のテスト | `MacroAction.cs`、`SpecialActionOutputFactory.cs`、`MacroPlayback.cs` |
| W5 | 実機確認の手順を `Phase6-Step11-RealDevice-Verification-Checklist.md` に追記。台帳・完了報告・モデル図 03 を更新 | docs |

各 Step で `dotnet test` をグリーンにする（私の環境には dotnet がないため、ビルドとテストはユーザーの環境で行い、結果を貼ってもらう）。

## 5. 実機確認の項目（案）

| ID | 内容 |
|---|---|
| R2-1 | SA の Macro（Repeat (Toggle)）: ボタンを 1 回押す → 繰り返しが始まり、ボタンを離しても続く。もう 1 回押す → 止まる |
| R2-2 | Controls の Macro（同上） |
| R2-3 | 繰り返しの最中にプロファイルを切り替える／コントローラーを切断する → 止まる。再接続後、1 回押して始まる（反転が逆にならない） |
| R2-4 | プロファイルを保存・再読込しても、モードが保たれる |
| R2-5 | 既存の Play Once／Repeat While Held のマクロが、従来どおり動く |
| R2-6 | 素早く連打しても、重なって二重に実行されない |

## 6. リスクと対応

- **既存のマクロの動作**: 条件の分岐は、`RepeatLatchMacro` が立っているときだけ新しい経路に入る。`RepeatMacro` の経路は変えない（既存の `Phase8Step78cW2ControlsMacroTests` ほかが守る）。
- **状態のリーク**: トグルが ON のまま、設定画面でそのマクロを編集・削除したとき。`ActionManager` の登録の入れ替え時に `SetToggledOn(false)` が呼ばれる箇所があり（`ActionManager.cs`／`DefaultActionManager.cs`）、ループは次の確認で止まる。W4 のテストで確認する。
- **LED などの強制状態**: 繰り返しの最後の 1 回の後始末（`keepKeyState` の扱い）は、現行のまま。

## 7. 決定事項と、残りの確認

決定済み（2026-10-23）:
- 保存形式: 独立した設定項目 `MacroExecuteMode`（3 モードすべて）。ダウングレードの互換は放棄、古い形式の読込は残す（§3.1）。

- 表示名: 「Repeat (Toggle)」。
- 停止の挙動: 実行中の 1 回分を最後まで再生してから止める。
- `RunOnRelease`（SA）との組み合わせ: Repeat (Toggle) を選んだら無効にする。
- 状態の表示: 今回は行わない（ログのみ）。

## 8. 今後の候補（今回は行わない）

`extras`（SA）や `KeyType`（Controls）のように、モード的な設定を 1 つの文字列に連ねて保存しているものは、独立した設定項目へ移したい（ユーザーの希望）。今回は、マクロの Execute Mode だけを移す。残り（`Scan Code`、`RunOnRelease`、`Sync`、`KeepKeyState`、Controls の `Toggle`／`ScanCode` など）は、R-2 の後に、別の Step として計画する。
