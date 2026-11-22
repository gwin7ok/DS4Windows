# PhaseRV38 仕様の突き合わせ記録（DS4・DualSense・Switch Pro の入力レポート）

作成日: 2026-11-22
目的: 既存のパーサーのテストは、旧コードの写しとの差分で、旧コードの誤りを検出できない。独立した出どころ（Linux カーネルの `hid-playstation.c`・`hid-nintendo.c`。master を 2026-11-22 に取得）と、このリポジトリの読み方を突き合わせた。
出どころ: https://raw.githubusercontent.com/torvalds/linux/master/drivers/hid/hid-playstation.c ／ https://raw.githubusercontent.com/torvalds/linux/master/drivers/hid/hid-nintendo.c
テスト: `ReportSpecConformanceTests`（カーネルの構造体のオフセット・ビットマスクから台本を組み立てる。CRC は独立実装の `PsReportCrc`）。

## 一致したもの（テストで固定）
| 項目 | 結果 |
|---|---|
| DS4 USB 0x01: スティック（1〜4）、トリガー（8・9）、buttons[0]（方向キー＋ □×○△）、buttons[1]（L1/R1/L2/R2/Share/Options/L3/R3）、buttons[2]（PS・タッチパッド） | 一致 |
| 方向キーの表（0〜7＝上から時計回り、8＝中立。斜めを含む） | 一致 |
| DS4 Bluetooth 0x11: common ブロックが byte 3 から（USB より +2）、CRC は末尾 4 バイト・種 0xA1・先頭（ID 含む）から 74 バイト | 一致（独立実装の CRC で封をしたフレームが受理され、1 バイト書き換えたフレームは捨てられる） |
| DualSense USB 0x01・Bluetooth 0x31: スティック・トリガー・buttons[0..2] のオフセット（BT は USB より +1）、マスク、方向キー | 一致 |
| Switch Pro 0x30: button_status の 24 ビットの各ボタン（18 個）、方向キー、ZL/ZR | 一致（A/B/X/Y は Circle/Cross/Triangle/Square へ割り当て） |
| Switch Pro: 左右スティックの 12 ビットの詰め方、電池（bat_con の bit5〜7＝段階、bit4＝充電） | 一致（コードの読み方を確認。台本は `VirtualPad` が同じ詰め方で作る） |
| DS4 の電池: status[0]（byte 30）の下位 4 ビット＝容量、bit4＝ケーブル | 一致 |

## 食い違い・疑い（実機で確かめる。**コードは変えていない**）
| # | 内容 | カーネル | このリポジトリ |
|---|---|---|---|
| 1 | DualSense の電池の容量の換算 | status[0] の下位 4 ビットは 0〜10（10％刻み）。`min(容量 × 10 + 5, 100)` ％ | `容量 × 100 / 8` を 100 で頭打ち（`BATTERY_MAX = 8`）。例: 容量 5 → カーネル 55％、本リポジトリ 62％。容量 8 以上は 100％ |
| 2 | DualSense の充電中の判定 | status[0] の上位 4 ビット: 0x1＝充電中、0x2＝満充電、0xA／0xB＝異常（電池 0％扱い） | 充電中は **byte 54 の bit 0x08**（カーネルでは status[1]。HP/MIC の検出用で、bit 0x08 の定義なし）。満充電は byte 53 の 0x20 ビット（0xA／0xB も満充電になる） |

→ 実機（DualSense）で、USB 接続・Bluetooth 接続・充電中・満充電の電池表示を確かめる。実機で食い違いが確かめられたら、読み方を直す（動作の変更なので、別途承認を得る）。

## 見ていないもの
- Switch Pro の校正（SPI）の読み方、スティックの校正後の値、IMU（ジャイロ・加速度）。
- DS4・DualSense のタッチパッド、ジャイロ、加速度。
- DS4 の Bluetooth の最小レポート（ID 0x01、10 バイト）。このリポジトリは 0x11 を前提にしている。
