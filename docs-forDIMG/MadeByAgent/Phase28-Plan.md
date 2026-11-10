# Phase28 計画書: 工場・窓口の太りの整理（Structure-Review の G）

作成日: 2026-11-11

## 現状（調査）
- `IViewModelFactory` は 30 メンバーの 1 つの窓口で、実体は `ViewModelFactory`（用途別の 3 つの工場へ委譲するだけ）だった。画面は自分が使わないメンバーまで知っていた。
- `ShellViewModelFactory` は 14 引数、`SpecialActionOutputFactory` は 14 引数（どちらも `ConstructorArityBaseline` に凍結）。

## Step 一覧
| Step | 内容 | 状態 |
|---|---|---|
| 28-1 | `IViewModelFactory` を、用途別の 3 つの窓口（`IShellViewModelFactory` 12、`IProfileEditViewModelFactory` 9、`IActionViewModelFactory` 9）へ分け、窓口の `ViewModelFactory` を削除。利用側は使う窓口だけを受ける（`AssignmentEditor`・`ProfileEditor`・`AssignmentWindowFactory`・`ProfileEditorFactory` は 2 つ） | 完了（2026-11-11） |
| 28-2 | `ShellViewModelFactory`（14 引数）の整理 | 見送り（下記） |
| 28-3 | `SpecialActionOutputFactory`（14 引数、種別ごとの `switch`）を、種別ごとの組み立て部品の列へ分ける | 未着手 |

## 28-2 を見送る理由
自動プロファイルの ViewModel を別の工場へ出せば 12 引数になるが、その窓口を `MainWindow`（すでに 15 引数で凍結。`autoProfControl` の初期化で必要）が追加で受けることになり、画面側の引数が増える。`MainWindow` の引数を減らす整理（子画面の生成を窓の工場へ出す）と一緒に行う。

## 履歴
- 2026-11-11: 28-1 を完了。全 3,291 テスト合格。画面の起動・各画面の表示は実機で未確認（Known-Issues-Ledger に追記）。
