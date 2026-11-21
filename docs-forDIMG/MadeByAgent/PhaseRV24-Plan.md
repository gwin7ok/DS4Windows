# PhaseRV24 計画書（残りの文字列テストを挙動へ）

作成日: 2026-11-22。承認: 方向はユーザーが選択（「残りの文字列テストを挙動へ」）。

## 目的
RV23 で残した、置き換えに本体の変更が要る文字列テストを、時計の注入などの小さな本体変更とともに挙動のテストへ置き換える。

## 実施（完了）
- タッチパッド: `TouchButtonDetector` に時計と `TryExpireRelease`（保持 100ms の判定）を持たせ、`TapClickDetector` の `DateTime.Now` も時計経由にした。`MouseReleaseButtonHoldTests` を挙動のテストへ（ソース走査を撤去）。`MouseTapClickHoldTests` の実時計待ちの繰り返しを、手動の時計に置き換え。負の確認: 比較を反転すると失敗する。
- 見送り（本体の大きな組み立ての変更が要り、得るものが小さい）: 起動の順序（`App`）、切断の後始末（`DeviceDisconnectHandler`、実機の型が要る）、画面のホバー（`ProfileEditor`）。行数・URL の走査は、本来の字句検査のため残す。
- 実機確認: `Device-Check-Checklist.md` の RV24（後回し）。
