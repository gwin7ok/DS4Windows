# Phase28 計画書: 工場・窓口の太りの整理（Structure-Review の G）

作成日: 2026-11-11

## 現状（調査）
- `IViewModelFactory` は 30 メンバーの 1 つの窓口で、実体は `ViewModelFactory`（用途別の 3 つの工場へ委譲するだけ）だった。画面は自分が使わないメンバーまで知っていた。
- `ShellViewModelFactory` は 14 引数、`SpecialActionOutputFactory` は 14 引数（どちらも `ConstructorArityBaseline` に凍結）。

## Step 一覧
| Step | 内容 | 状態 |
|---|---|---|
| 28-1 | `IViewModelFactory` を、用途別の 3 つの窓口（`IShellViewModelFactory` 12、`IProfileEditViewModelFactory` 9、`IActionViewModelFactory` 9）へ分け、窓口の `ViewModelFactory` を削除。利用側は使う窓口だけを受ける（`AssignmentEditor`・`ProfileEditor`・`AssignmentWindowFactory`・`ProfileEditorFactory` は 2 つ） | 完了（2026-11-11） |
| 28-2a | `MainWindow`（15 引数）の整理 | 見送り（調べた結果、削れる引数がなかった。下記） |
| 28-2b | `ShellViewModelFactory`（14 引数）を、窓口（`IShellViewModelFactory`）は変えず、設定タブ（`SettingsViewModelFactory`）と自動プロファイル（`AutoProfilesViewModelFactory`）の生成を下位の工場へ委譲する形にして 12 引数にする。`MainWindow` は触らない | 完了（2026-11-11） |
| 28-3 | `SpecialActionOutputFactory`（14 引数、種別ごとの `switch`）を、種別ごとの組み立て部品の列へ分ける。`ISpecialActionOutputBuilder`（`Processing/Actions/OutputBuilders`）を種別ごとに 10 個、工場は `IEnumerable` を受けて種別で選ぶ（14 → 1 引数）。テストは `TestOutputFactory.Create` で一部の窓口だけから作る | 完了（2026-11-11） |

## 28-2a を見送った理由と、28-2b の方式の変更
`MainWindow` の 15 個の引数は、すべて実際に使われている（`ipcProcessor` も `MainWindow.Ipc.cs` が使う）。使い方を別の部品へ出しても、その部品を受ける引数が増えるため、純減にならない。純減には、`TrayAppearanceController` などの既存の協力者への統合が要り、画面の責務の整理（Phase26 の続き）として別に扱う。
28-2b は、窓口を分けるのをやめ、実装の側で下位の工場へ委譲する形にしたため、`MainWindow` の引数は増えない。

## 履歴
- 2026-11-11: 28-1 を完了。全 3,291 テスト合格。画面の起動・各画面の表示は実機で未確認（Known-Issues-Ledger に追記）。
- 2026-11-11: 28-3 を完了。全 3,291 テスト合格。`ConstructorArityBaseline` から `SpecialActionOutputFactory` を削除。動作（種別ごとの出力・方針）は移動のみで変えていない。
- 2026-11-11: 28-2b を完了（方式を変更）。全 3,291 テスト合格。`ConstructorArityBaseline` から `ShellViewModelFactory` を削除。28-2a は見送り。
