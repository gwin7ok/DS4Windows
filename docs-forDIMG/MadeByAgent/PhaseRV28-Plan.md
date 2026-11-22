# PhaseRV28 計画書（出力ピッカーの窓を抽象化）

作成日: 2026-11-22。承認: 「窓を抽象化する」（ユーザー指示）。

## 実施（完了）
- `UI/Abstractions/IBindingDialog`（`DefaultPicked`、`ShowModal`）。`BindingWindow` が実装し、`ShowModal` が親（メイン画面）の設定と `ShowDialog` を行う。`IEditorWindowFactory.CreateBindingDialog` を追加（`CreateBindingWindow` はマークアップ読み込みのテストが使うため残す）。
- `AssignmentEditorSession.PickOutput` は `IBindingDialog` だけを使う。
- `AssignmentEditorPickOutputTests`（11 件）、負の確認 済み。文字列検査 4 件を撤去。Debug ALL OK（3767 件）。再採点 `scorecard/RV28.json`。
