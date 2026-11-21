# PhaseRV27 計画書（割り当て編集の ViewModel を挙動へ）

作成日: 2026-11-22。承認: 「次の Phase へ」。

## 実施（完了）
- `DefaultUnboundTypeTests` に、保存ボタン（`CanSave`）と未選択の検証の挙動のテスト 2 件を追加し、ViewModel のソースを読む検査 3 つを撤去。負の確認 済み。Debug ALL OK（3759 件）。再採点 `scorecard/RV27.json`（等級変化なし）。

## 見送り
- `ControlDefaultAndExtrasTests.TheEditor_OpensThePickerInTheControlMode_...` と `DefaultUnboundTypeTests` の `PickOutput` の検査: `BindingWindow`（具象の WPF の窓）が要り、偽物にできない。窓を `IEditorWindow` のような抽象にする変更が前提（得るものに対して大きい）。
- XAML の字句検査（`x:Name`、`Visibility` の結びつけ、`OpenLeftPaneEditor` の回数）は、画面の構造の検査として残す。

## 残り
文字列走査 106 ファイル。大半は XAML・名前空間・行数などの字句検査か、移行の完了を守る撤去の検査（`GlobalRemovedGuardTests` など）。これ以上の置き換えは、本体の抽象化を伴う。
