# PhaseRV38 計画書: 独立した仕様との突き合わせと、仮想コントローラーの拡張

作成日: 2026-11-22
状態: **完了（2026-11-22）**
動き: テストと文書の追加だけ。本体・保存の形・ログは変えない（食い違いは記録のみ）。

| Step | 内容 | 状態 |
|---|---|---|
| RV38-1 | Linux カーネルの `hid-playstation.c`・`hid-nintendo.c` から、レポートのオフセット・ビットを取得し、パーサーと突き合わせる（`PhaseRV38-Spec-Crosscheck.md`） | 完了 |
| RV38-2 | `ReportSpecConformanceTests`: カーネルの仕様から台本を組み立て、独立実装の CRC（`PsReportCrc`）で封をして、パーサー・デバイスへ流す | 完了 |
| RV38-3 | `VirtualPadFailureTests`: 全機種で、読み取りの失敗・機器の取り外しが、切断 1 回だけになること | 完了 |
| RV38-4 | `VirtualPadRumbleRoundTripTests`: ゲームの振動が、本番の経路を通って、DS4・DualSense の出力レポート（カーネルのオフセット）まで届くこと | 完了 |
| RV38-5 | `VirtualPad` に DS4・DualSense の Bluetooth を追加（契約・出力までの通し・失敗のテストに自動で載る） | 完了 |

## 範囲外（今後）
- 押す→離す→押す の複数フレームの台本（`VirtualPad` を、入力の列を受ける形へ）。
- Switch Pro・Xbox の振動の往復、DS4 の仮想出力、ライトバー、Switch Pro の Bluetooth。
- 食い違い 2 件（DualSense の電池）の修正（実機確認のあと）。

## 履歴
- 2026-11-22: 計画書を作成し、RV38-1〜5 を実施。DualSense の電池に食い違いを 2 件記録（`Known-Issues-Ledger.md` §3）。
