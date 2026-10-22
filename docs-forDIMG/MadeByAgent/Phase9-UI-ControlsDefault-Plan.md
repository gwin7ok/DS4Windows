# Phase9 UI の仕様変更 2 点（Default の編集画面の表示、Controls タブの「すべて Default に戻す」）

作成日: 2026-10-22
状態: ユーザー承認済み（2026-10-22。表示形式＝「Default (A)」、範囲＝Controls タブの全ての割り当て［Extras、マクロを含む］、確認ダイアログあり）。実装済み。ビルド・テスト・実機確認は待ち

## 1. 変更 1: Default のボタンの編集画面
- 旧: 割り当てが Default のボタンの編集画面を開くと、種別のプルダウンは「Default」の項目が選ばれていた。
- 新: 種別のプルダウンは「Output Key/Mouse/Button」（Press Key）が選ばれ、出力の欄には「Default (A)」のように、そのボタンの既定の出力名を添えて表示する。
- 保存: 変更せずに Save しても、Default のまま（`ControlSettingsEditTarget.WriteFrom` の Key の枝は、Press Key の状態が Default なら `WriteDefault` を呼ぶ）。種別の一覧の「Default」の項目は、これまでどおり選べる。
- 実装: `ControlSettingsEditTarget.InitialTypeIndex`（Default → Press Key の添字）、`PressKeyViewModel`（Default の表示に既定の出力名を添える。名前は `IProfileSettingsService.GetDefaultButtonMapping` と `X360ControlTables`）。
- テスト: `Phase10Step2W4ControlSettingsEditTargetTests`（初期の添字 9 → 4）、`Phase10Step3W2ControlDefaultAndExtrasTests`（表示）。

## 2. 変更 2: Controls タブの「すべて Default に戻す」ボタン
- コントローラーの画像（`conCanvas`）の右下に、「Reset all to Default」（日本語: すべて Default に戻す）のボタンを置く。
- 押すと確認ダイアログを出し、「はい」のときだけ、一覧（`MappingListViewModel.Mappings`）の全項目［L2／R2 Full Pull、LS／RS Outer、Touch Started／Ended を含む］の `DS4ControlSettings.Reset()` を呼ぶ（Extras、マクロ、SA 専用の項目も初期化）。Special Actions タブ、スティック・トリガーの調整値は変えない。
- プロファイルは、これまでの編集と同じく、Save するまで保存されない（Cancel で元に戻る）。
- 文言: `Strings.resx`／`Strings.ja.resx` に `ResetAllControls`、`ResetAllControlsConfirm` を追加（`Strings.Designer.cs` にも追記）。
- ログ: `[DI] Controls tab: reset all assignments to Default`。

## 3. 実機確認
- 変更 1: Controls タブで Default のボタンをダブルクリック → 種別が「Output Key/Mouse/Button」、出力の欄が「Default (A)」形式。変更せずに Save → Default のまま。別の出力を選んで Save → 反映。種別を「Default」へ戻して Save → Default。
- 変更 2: いくつかのボタンに割り当てを設定 → ボタンを押す → 確認ダイアログ → 「いいえ」で変化なし、「はい」で全て Default（一覧が「Default」表示）。Save して再度開いても Default。
