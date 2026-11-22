# PhaseRV31 計画書（開始・停止の順序を挙動へ）

作成日: 2026-11-22。承認: 「次の Phase へ」。

## 実施（完了）
- `ServiceLifecycleOrderTests`（4 件）。`RecordingConnectDevice` に `StopUpdate` の記録を追加。負の確認 済み。`LifecycleGuardTests` の文字列検査 2 件を撤去。Debug ALL OK（3772 件）。再採点 `scorecard/RV31.json`。挙動は変えていない（実機確認の追加なし）。
