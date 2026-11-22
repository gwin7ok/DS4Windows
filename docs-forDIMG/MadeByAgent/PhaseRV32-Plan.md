# PhaseRV32 計画書（接続・開始の残りの順序を挙動へ）

作成日: 2026-11-22。承認: 「次の Phase へ」。

## 実施（完了）
- `DeviceConnectionOrderTests`（5 件）、`ServiceLifecycleOrderTests`（5 件）。負の確認 済み。文字列検査を撤去。Debug ALL OK（3773 件）。再採点 `scorecard/RV32.json`。挙動は変えていない（実機確認の追加なし）。
