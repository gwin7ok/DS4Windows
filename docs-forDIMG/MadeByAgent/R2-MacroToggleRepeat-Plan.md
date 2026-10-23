# R-2 マクロの「繰り返しをトグルする」再生モード 実装計画書

作成日: 2026-10-23
状態: **計画（承認待ち）**
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

### 3.1 保存形式（古い版との互換）

`DS4Windows.Toggle` は既存の意味が重い（部分一致の衝突）ため、**新しい名前のフラグ `RepeatLatchMacro = 64`** を `DS4KeyType` に追加する。

- 名前に `Toggle` を含まない（部分一致で `Toggle` フラグが立たない）。`RepeatMacro` を含まない（部分一致で `RepeatMacro` が立たない）。`Macro` は含む（既存の読込で `Macro` フラグが立つ。従来どおり）。
- 「Repeat (Toggle)」は **`RepeatLatchMacro` だけ**で表す（`RepeatMacro` とは同時に立てない。モードは 3 つのうち 1 つ）。
- Controls の保存: `RepeatMacro` → `RepeatLatchMacro` → `Macro` の順に、1 つだけ書く（`ProfileButtonSaveSection`）。読込は `Contains("RepeatLatchMacro")` を追加（`ProfileButtonControlLoadSection`）。
- SA の保存: `extras` に `RepeatLatch` を追加（`MacroViewModel.SaveAction`）。読込は `extras.Contains("RepeatLatch")`（`SpecialAction`）。
  - 注意: `extras.Contains("Repeat")` は `RepeatLatch` にも一致する。**`RepeatLatch` を先に判定し、一致したら `RepeatMacro` を立てない**ように書く。
- 古い版でプロファイルを読んだ場合（ダウングレード）: Controls は「Play Once」、SA は「Repeat While Held」になる（壊れない。トグルの機能が使えないだけ）。

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
| W2 | 保存と読込（Controls の `KeyType`、SA の `extras`）。往復テストと、古い形式の読込テスト | `ProfileButtonSaveSection`、`ProfileButtonControlLoadSection`、`SpecialAction.cs`、`MacroViewModel.cs` |
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

## 7. 決めてほしいこと

1. **名称**: 画面の表示名は「Repeat (Toggle)」でよいか（既存の「Play Once」「Repeat While Held」に合わせた英語）。
2. **停止の挙動**: 停止の操作で、実行中の 1 回分を最後まで再生してから止めるのでよいか（案）。途中で即座に止める（キーも解放する）ほうがよいか。後者は `EndMacroDirect` の実装と、押しっぱなしのキーの解放が必要で、作業が増える。
3. **`RunOnRelease`（SA の「離したときに実行」）との組み合わせ**: 組み合わせを許可するか。案は、`Repeat (Toggle)` を選んだら `RunOnRelease` を無効にする（意味が曖昧になるため）。
4. **状態の表示**: トグルが ON であることを、画面やライトバーで示すか。案は、今回は行わない（ログのみ）。
