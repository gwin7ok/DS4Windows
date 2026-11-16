# Phase34 V2: AssignmentEditor の業務の参照の解消（計画書）

作成日: 2026-11-16 / 対象: `DS4Windows/UI/AssignmentEditor.xaml.cs`（693 行）/ 規則: V2（View は業務のサービス・設定の型を直接参照しない）

## 1. 目的と現状

`ViewDirectServiceReferenceBaseline` に残る `AssignmentEditor` の 8 件を、画面の動作を変えずに 0 件にする。

| 参照 | 使っている箇所 |
|---|---|
| IEnvironmentService | `HasTargetDevice`（スロット数の上限） |
| IProfileSettingsService | Controls のモードの編集対象（`ControlSettingsEditTarget`）の生成 |
| ISpecialActionRepository | 名前変更時の旧名の削除、Extras の保存 |
| IWindowSystem | FocusWindow のウィンドウ選択（`WindowTargetPicker`）の生成 |
| DS4ControlSettings / ControlActionData / SpecialAction / ActionTypeId | 保存・検証・記録画面・出力ピッカーへの受け渡し |

## 2. 方針（決定事項）

型の名前空間を動かして隠すことはしない。業務の判断と受け渡しを、画面ではない部品へ移す。

1. 新設 `UI/Services/AssignmentEditorSession`（V1 は ViewModel だけが対象のため、ダイアログを開く部品はここに置ける）。
   - 編集の状態を持つ: 種別ごとの ViewModel 一式、`ActionTypeEditorSet`、`ControlSettingsEditTarget`、`saExtras`、モード、実機のスロット番号。
   - 操作を持つ: SA の保存（旧名の削除、種別ごとの保存、Extras の保存）、Controls の保存、マクロ記録 4 種（通常・Tap・Hold・DoubleTap）、出力ピッカーの起動と結果の取り込み。
   - 結果は列挙型で返す（`Saved`／`Invalid`／`ExistingName`）。画面はメッセージの表示位置だけを決める。
2. `AssignmentEditor`（View）のコンストラクターは `AssignmentEditorSession` 1 つを受ける。画面に残すのは、タブの表示切替・色選択ダイアログ・名前重複メッセージの位置決め・データコンテキストの結び直し。
3. `AssignmentWindowFactory` が `AssignmentEditorSession` を組み立てる（依存は工場が持つ）。
4. データコンテキストを外す順序（保存の前）、旧名の削除→保存→Extras の順序、無効時は結びつきを外さない挙動は、従来のまま。ログ文言の変更なし。
5. 公開の `ControlSaveTypeId`（呼び出し元なし）は削除。`IsControlMode` はセッション経由。

## 3. 手順

1. セッション型を作り、画面から呼び出しを移す。
2. 工場を差し替える。
3. ソース文字列を検査している既存テスト（`SaExtrasEditorGuardTests` ほか）の検査対象を、移した先へ更新する（検査の意図は変えない）。
4. 基準 8 件を削除、DI 計画書 §9.3 の件数、台帳を更新。Debug ビルド 0/0、全テスト合格。

## 4. 実機確認（先延ばし。台帳へ記載）

SA の新規・編集・保存、名前の重複の警告、Controls の割り当て保存・キャンセル、マクロ記録（通常・Multi Action の 3 種）、出力ピッカー、FocusWindow のウィンドウ選択、バッテリー確認の色プレビュー。
