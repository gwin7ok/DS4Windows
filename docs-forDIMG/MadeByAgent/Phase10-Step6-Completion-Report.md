# Phase10-Step6 完了報告書（統合 Step）

作成日: 2026-10-22
対象ブランチ: `For-DI-migration-work`
計画書: `Phase10-Step6-Plan.md`、完成形のモデルと差: `Phase10-Integration-Target-Model.md` §3・§10
画面の呼び名: 割り当て編集画面（`AssignmentEditor`。旧 `SpecialActionEditor`）、出力ピッカー（`BindingWindow`）

## 1. 目的と結果

Controls タブと SpecialActions（SA）タブの、どちらで設定したかに依存せず、**割り当ての解釈・出力の作り方・方針が 1 か所**にあり、暫定の構造がない状態にした。ユーザーから見える挙動は、SA の Extras（I5）を除き、変えていない。

- ビルド: 警告 0・エラー 0。テスト: 2086 件と 11 件（Standalone）が成功（Wave ごとに `Debug ALL` を通した）。
- 実機確認: 先送り（台帳 §27 の P10-79〜97、P10-98。回帰の進め方は §28）。

## 2. Wave ごとの結果

| Wave | 内容 | 主な成果物 |
|---|---|---|
| I0 | 確認事項の決定 | 元の出力の消し方は由来で決める。Controls の複数ボタンは許さない。SA の Extras は全種別・成立中のみ有効 |
| I1 | 共通の割り当て定義 | `AssignmentDefinition`、`AssignmentOrigin` |
| I2 | SA の型のある項目（**保存形式は変えず**変換部品） | `SpecialActionItemMapper`（`ToItem`／`ToSpecialAction`）。`ControlSpecialItem` に Battery の通知・ライトバー |
| I3 | 読み込みの部品 2 つ、定義を受ける出力の部品 | `SpecialActionAssignmentReader`、`ControlsAssignmentReader`、`ISpecialActionOutputFactory.Create(definition, source, …)` |
| I4a | 押下判定の置き換え | `ControlJudgement` → `ControlOutputNature`＋`ControlOutputNatureTable` |
| I4b | Controls の Key／Macro も読み込みの部品経由、方針の表 | `FreshPressGuardTable.For`（種別 × 由来）、`forControls` 撤去 |
| I4c | 束縛の組み立てを 1 つに | `AssignmentBindingBuilder`、互換の入口の削除 |
| I5a〜c | SA の Extras | `SpecialAction.outputExtras`、`Actions.xml` の `OutputExtras`、`BuildExtras`、`ISpecialActionRepository.SaveActionExtras`、出力ピッカーの SA のモードに Extras |
| I6a〜c | 暫定の構造の撤去、改名 | `AssignmentEditor`（改名）、`ControlsSpecialTypeTable`、`IActionTypeEditor.LoadControlItem`／`ToControlItem`（`ControlSpecialEditors` 削除）、`ExposeMode` の Full／Keyboard 撤去、旧データ用の「No unload triggers specified」の表示を削除 |
| I7 | 文書 | モデル図 `03`、`Phase10-Integration-Target-Model.md` §10、台帳 §28、本報告書、`Phase10-Status.md` |

そのほか、ユーザー依頼で、起動ログの「This binary was built at … commit=」のハッシュを 8 桁から 7 桁にした（VSCode の表示に合わせる）。

## 3. ユーザー決定の一覧

- Controls の複数ボタンの組み合わせは許さない（保存形式は不変）。
- SA の Extras: 全種別、SA が成立している間だけ有効、重なれば SA が勝つ。SA には Default／Unbound を適用しない。画面から付けられるのは Press Key の SA だけ（Controls と同じ出力ピッカー）。
- 定義に Controls 固有の種別（パッド、マウス、アナログマウス、素通し）を入れない。
- Actions.xml の形式は変えず、変換部品で対応する。
- 立ち上がりのログの 1 か所化は後回し。
- 旧データのエラー表示（No unload triggers specified）を削除。
- 出力ピッカーは、Controls と SA で共通（違いは Default／Unbound の表示だけ）。

## 4. 検証

- 各 Wave で `Debug ALL`（Build Debug → Build Test → Execute Test）を 0 警告・0 エラー・全テスト合格にした。
- 新規テスト: `Phase10Step6I1`〜`I6b` の各ファイル（定義、変換、読み込み、方針の表、組み立て、Extras の保存・実行・画面の接続、規則の表）。既存の Phase8、Phase10 の特性化テストは、意図した変更（Extras を出す、No unload の削除、Full／Keyboard の撤去）以外は変更なしで合格。
- ローカルで一度だけ、`Phase10Step4cW2EdgeLogTests.ASpecialActionMacro_LogsTheReleaseToo` が一過性で失敗し、再実行で成功した（再発したら調べる）。Actions 側では、別のテストプロセスの異常終了が 2 回あったが、ローカルでは再現しなかった。

## 5. 残した技術的負債（意図して残したもの）

| 項目 | 内容 | 対応の予定 |
|---|---|---|
| 立ち上がりのログの重複 | SA の出力アクション 9 クラスに、同じ Trace の 4 行がある | 別の整理（ログの文言・レベルを 1 つずつ守る） |
| 実行時の状態 | `SpecialAction` が状態を持つ（エンジンと出力アクションが受け取る） | 状態を別の型へ移す整理（大きいため、別 Step） |
| 追従のコード | `ReplaceControlProfileSwitchTarget`／`ReplaceActionReference`／`RemoveActionReference`（ID のない古いファイルのため） | 起動時に全ファイルへ ID を書く移行のあと撤去 |
| Extras の画面の入口 | Press Key（Key／Button）の SA だけ | Controls タブも含めた共通の追加として、あとで足す |
| Multi-action の未設定マクロ | 保存を止めるかは、Controls へ移すときに決める | W8 相当 |
| 定義の外の Controls 固有の種別 | パッド、マウス、アナログマウス、素通し | Step5（SA のアナログ出力）で検討 |
| 一覧を歩く builder が 2 つ | Extras の順序と門番の規則が Controls にある | 現状維持（特性化テストが守る） |

## 6. 次の作業

1. （完了 2026-10-22）W7・W8 相当（Check Battery、Multi-action を Controls へ。Q5 を含む）。結果は `Phase10-Integration-Target-Model.md` §11、台帳 §29。
2. Step5（SA 側のアナログ出力）。
3. 先送りの実機確認（台帳 §27、§28）。
