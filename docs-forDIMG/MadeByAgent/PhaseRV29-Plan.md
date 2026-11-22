# PhaseRV29 計画書（適用後の掃除の順序を挙動へ）

作成日: 2026-11-22。承認: 「次の Phase へ」。

## 実施（完了）
- `ApplyProfileCleanupOrderTests`（3 件）。負の確認 済み。旧ガードを撤去。Debug ALL OK（3769 件）。再採点 `scorecard/RV29.json`。挙動は変えていないため、実機確認の追加はなし。

## 見送り
- `DeviceConnector.PrepareConnected` の順序（電池の取り込み → 適用 → StartUpdate）、`DeviceLifecycleHooks` の購読順: 実機の基底（`InputDeviceCore` の非仮想メンバー）を偽にできない。機器の部品化（RV14 の流れ）の先で扱う。
