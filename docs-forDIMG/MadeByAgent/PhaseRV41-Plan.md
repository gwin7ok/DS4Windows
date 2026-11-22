# PhaseRV41 計画書: 出力レポート・IMU の校正・XInput の値の範囲の突き合わせ

作成日: 2026-11-22（実施のあとに、記録として作成）
状態: **完了（2026-11-22）**
動き: テストと文書の追加だけ。本体は変えない。26 件のテストはすべて仕様と一致し、新しい食い違いはなかった。

| Step | 内容 | 状態 |
|---|---|---|
| RV41-1 | `OutputReportLayoutSpecConformanceTests`: DS4（USB・Bluetooth）と DualSense（USB・Bluetooth）の出力レポートの各フィールドの位置、有効フラグ、プレイヤー LED のマスク、ミュート LED の値を、カーネルと SDL と突き合わせる | 完了 |
| RV41-2 | `ImuCalibrationSpecConformanceTests`: DS4・DualSense のジャイロ・加速度計の校正（フィーチャーレポート 0x02・0x05 の 2 つの並び）と、Switch Pro の IMU の校正（SPI 0x6020〜。軸の読み替え） | 完了 |
| RV41-3 | `XInputValueRangeSpecConformanceTests`: スティックの 16 ビットの範囲と Y 軸の反転、トリガーの 0〜255、電池の種類・段階（`xinput.h`・Microsoft の文書） | 完了 |

## 記録
- トリガーの効果のモードのバイトは、カーネルにも SDL にもなく、確かめられない。

## 履歴
- 2026-11-22: 実施。記録は、git の履歴（`PhaseRV41:` で始まるコミット）と、この計画書。
