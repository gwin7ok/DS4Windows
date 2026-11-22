# PhaseRV39 計画書: SDL・Microsoft を 2 つ目の出どころにした仕様の突き合わせ

作成日: 2026-11-22（実施のあとに、記録として作成）
状態: **完了（2026-11-22）**
動き: テストと文書の追加だけ。本体・保存の形・ログは変えない（食い違いは記録のみ）。

| Step | 内容 | 状態 |
|---|---|---|
| RV39-1 | SDL の `SDL_hidapi_ps4.c`・`SDL_hidapi_ps5.c`・`SDL_hidapi_switch.c` を 2 つ目の出どころにして、RV38 で記録した食い違いを再確認（DualSense の電池の換算・充電判定が、カーネルと SDL の 2 つで一致して、本リポジトリと食い違う） | 完了 |
| RV39-2 | `OutputReportSpecConformanceTests`: DS4・DualSense の Bluetooth の出力レポートの CRC の種（0xA2）とタッチのオフセットを、仕様と突き合わせる（`PsReportCrc` に出力側の種を追加） | 完了 |
| RV39-3 | `ReportSpecConformanceTests` に、Switch Pro の校正の並びと、XInput のボタンのマスクを追加 | 完了 |
| RV39-4 | 食い違い 3 を新たに記録: DualSense の Bluetooth の出力レポートの枠が 1 バイト早い（コードは変えない。`Known-Issues-Ledger.md` §3） | 完了 |

## 範囲外
- 食い違いの修正（実機確認と利用者の承認が先。RV40 で承認を得て実施）。

## 履歴
- 2026-11-22: 実施。記録は、git の履歴（`PhaseRV39:` で始まるコミット）と、この計画書。
