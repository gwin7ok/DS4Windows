# Phase28 計画書: 工場・窓口の太りの整理（Structure-Review の G）

作成日: 2026-11-11

## 現状（調査）
- `IViewModelFactory` は 30 メンバーの 1 つの窓口で、実体は `ViewModelFactory`（用途別の 3 つの工場へ委譲するだけ）だった。画面は自分が使わないメンバーまで知っていた。
- `ShellViewModelFactory` は 14 引数、`SpecialActionOutputFactory` は 14 引数（どちらも `ConstructorArityBaseline` に凍結）。

## Step 一覧
| Step | 内容 | 状態 |
|---|---|---|
| 28-1 | `IViewModelFactory` を、用途別の 3 つの窓口（`IShellViewModelFactory` 12、`IProfileEditViewModelFactory` 9、`IActionViewModelFactory` 9）へ分け、窓口の `ViewModelFactory` を削除。利用側は使う窓口だけを受ける（`AssignmentEditor`・`ProfileEditor`・`AssignmentWindowFactory`・`ProfileEditorFactory` は 2 つ） | 完了（2026-11-11） |
| 28-2a | `MainWindow`（15 引数）の整理: 子画面（更新・変更履歴・名前変更・コントローラー登録など）の生成を、窓の工場（`EditorWindowFactory` の系統）へ出し、`MainWindow` が受ける引数を減らす。`ConstructorArityBaseline` も同じコミットで更新する | 未着手（28-3 の後） |
| 28-2b | 28-2a の後、`ShellViewModelFactory`（14 引数）から自動プロファイルの生成を別の工場（`IAutoProfilesViewModelFactory`）へ出し、12 引数にする。`MainWindow` が追加の窓口を受けても、28-2a で減らした分に収まること | 未着手（28-2a の後） |
| 28-3 | `SpecialActionOutputFactory`（14 引数、種別ごとの `switch`）を、種別ごとの組み立て部品の列へ分ける。`ISpecialActionOutputBuilder`（`Processing/Actions/OutputBuilders`）を種別ごとに 10 個、工場は `IEnumerable` を受けて種別で選ぶ（14 → 1 引数）。テストは `TestOutputFactory.Create` で一部の窓口だけから作る | 完了（2026-11-11） |

## 28-2a・28-2b の理由
自動プロファイルの ViewModel を別の工場へ出せば 12 引数になるが、その窓口を `MainWindow`（すでに 15 引数で凍結。`autoProfControl` の初期化で必要）が追加で受けることになり、画面側の引数が増える。先に `MainWindow` の引数を減らす整理（子画面の生成を窓の工場へ出す。28-2a）を行い、そのあとで分ける（28-2b）。

## 履歴
- 2026-11-11: 28-1 を完了。全 3,291 テスト合格。画面の起動・各画面の表示は実機で未確認（Known-Issues-Ledger に追記）。
- 2026-11-11: 28-3 を完了。全 3,291 テスト合格。`ConstructorArityBaseline` から `SpecialActionOutputFactory` を削除。動作（種別ごとの出力・方針）は移動のみで変えていない。
