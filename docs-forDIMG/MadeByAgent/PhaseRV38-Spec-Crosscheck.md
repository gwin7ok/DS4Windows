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

## 対応（PhaseRV40。2026-11-22。ユーザーの承認で、食い違い 1〜3 を仕様側へ直した）
- 1: 電池 ＝ `min(容量 × 10 + 5, 100)`。2: 状態は byte 53 の上位 4 ビット（1 充電中、2 満充電、0xA 以上は異常＝0％）。byte 54 の 0x08 は見ない。3: Bluetooth の出力は、[2]＝0x10、効果の構造体は [3] から（USB の添字 + 2）。初期レポート・空レポートも同じ。
- **実機（DualSense）では未確認**。確かめる項目: Bluetooth で振動・ライトバー・トリガー効果・プレイヤー LED が効くこと、USB・Bluetooth の電池の表示（充電中・満充電を含む）。
- 以下の「食い違い」は、対応前の記録。

## 追補（PhaseRV39。2026-11-22。2 つ目の出どころ SDL を加えた）
出どころ: SDL の `SDL_hidapi_ps4.c`・`SDL_hidapi_ps5.c`・`SDL_hidapi_switch.c`、Microsoft の XINPUT_GAMEPAD の文書。テスト: `OutputReportSpecConformanceTests`、`ReportSpecConformanceTests`（Switch Pro の校正・XInput の表を追加）。

| 項目 | 結果 |
|---|---|
| DS4 Bluetooth の出力レポート（ID 0x11、data[1] の 0xC0、有効フラグ data[3]、振動 data[6]・data[7]、CRC は種 0xA2） | 一致（独立実装の CRC で検算） |
| DualSense Bluetooth の出力レポートの CRC（種 0xA2、78 バイト） | 一致 |
| DS4・DualSense のタッチパッド（1 点 4 バイト、12 ビットの詰め方、2 点目は +4。DualSense の BT は +1） | 一致 |
| Switch Pro の校正（左は 最大・中心・最小、右は 中心・最小・最大の順。ユーザー校正の印 0xB2 0xA1。SPI の番地・無振動の 00 01 40 40 をコードで確認） | 一致 |
| XInput のボタンのビット（0x0001〜0x8000） | 一致 |

**食い違い 3（新規。SDL とカーネルの 2 つの出どころが一致）**: DualSense の Bluetooth の出力レポートの枠が 1 バイト早い。仕様は ID 0x31、data[1]＝タグ・連番、data[2]＝0x10、有効フラグ data[3]・data[4]、振動は右 data[5]・左 data[6]。現在の実装は、data[1]＝0x02、有効フラグ data[2]・data[3]、振動は右 data[4]・左 data[5]。実機では、Bluetooth 接続の DualSense で振動・ライトバー・トリガー効果が無視される疑いがある（USB は影響なし）。現在の形は `OutputReportSpecConformanceTests` が固定している。
**食い違い 1・2 の補強**: SDL も、DualSense の電池は `min(容量 × 10 + 5, 100)`、充電は byte 53 の上位 4 ビット（byte 54 の 0x08 は USB 接続の意味）で、カーネルと一致。
**メモ**: SDL の DS4 の電池は `容量 × 10 + 5`（11＝満充電）で、カーネルとこのリポジトリの 8／11 の換算とは異なる（出どころの間で割れている。直さない）。

## 追補（PhaseRV41。2026-11-22。出力レポートの中身・IMU・校正・XInput）
出どころ: カーネル `hid-playstation.c`・`hid-nintendo.c`、SDL `SDL_hidapi_ps4.c`・`_ps5.c`・`_switch.c`、Microsoft の XINPUT_GAMEPAD・XINPUT_BATTERY_INFORMATION（`xinput.h` の定数）。テスト: `OutputReportLayoutSpecConformanceTests`、`ImuCalibrationSpecConformanceTests`、`XInputValueRangeSpecConformanceTests`（26 件。すべて仕様どおりで合格。**新しい食い違いはなし**）。

| 項目 | 結果 |
|---|---|
| DualSense 出力の構造体（motor_right 2・left 3・mute LED 8・右トリガー 10〜・左トリガー 21〜・valid_flag2 38・プレイヤー LED 43・色 44〜46）と USB／Bluetooth の同一性 | 一致 |
| DualSense の valid_flag0（0x0F）・valid_flag1（0x01・0x04・0x10）・valid_flag2（0x02・0x04）、プレイヤー LED の 0〜3 番（0x04・0x0A・0x15・0x1B）、ミュート LED（0／1／2） | 一致 |
| DS4 の USB 0x05（flag 1、モーター 4・5、色 6〜8、点滅 9・10）と BT 0x11（hw_control 0xC0＋間隔、flag 3、モーター 6・7、色 8〜10、点滅 11・12） | 一致 |
| DS4・DualSense のジャイロ・加速度の校正（フィーチャーレポートの位置、USB と BT の + − の並び、バイアス、1/16 度毎秒、8192 LSB／G、加速度は幅の半分を中心にする） | 一致（校正点で ±速度・±1 G になることをテスト） |
| Switch Pro の IMU 校正の配置（カーネルの X・Y・Z をこのアプリの DS4 向きの Y・X・Z へ読み替え）と係数（加速度 4÷幅、ジャイロ 936÷幅）、0x30 のサンプルの位置・単位（4096 LSB／G、約 14.25 LSB／度毎秒） | 一致（デバイスの読み取り位置はコードを読んで確認） |
| XInput のスティック（符号付き 16 ビット→0〜255、Y 反転）、トリガー（0〜255）、電池の種類（1・2・3・0xFF）・残量（0〜3） | 一致 |

**食い違いではないが、出どころと違う点（変更しない）**
- DualSense の lightbar_setup（0x02）と led_brightness（0x02）を毎回書く。カーネルは初期化のときだけ。実機で動いている値なので触らない。
- DualSense のプレイヤー LED の 5 番目以降（0x11、0x0E、0x1F）の並びは、SDL（0x1F、0x11、0x0E）と違う。このアプリ独自の割り当て。
- DualSense の旧ファームウェア（2.21 未満）の振動方式の切替は 2.21（カーネル）。SDL は 2.24 と書いている。
- Switch Pro の工場校正のスティックは、範囲を内側へ 0.96／1.04 に詰める（カーネルは詰めない）。
- DS4 のジャイロ校正の分母は `plus − minus`（SDL・カーネルの DualSense は `|plus − bias| + |minus − bias|`。通常は同じ値）。SDL の校正の妥当性検査（バイアス > 1024 など）はない。
- DS4 の Bluetooth の最小レポート（ID 0x01、10 バイト）: このアプリは 0x11 だけを受ける（0x01 は捨てる。`OnlyInputData0x01` の機種だけ 0x01 を使う）。カーネルは 0x01 を最小の入力として読むが、センサー・タッチ・電池は読まない。SDL は 0x11 へ切り替える信号を送る。
- XInput のデッドゾーン（7849・8689・30）は、文書に数値がなく、このアプリも使わない（確認できず、対象外）。
- トリガー効果のモード値（0x01・0x02・0x21 など）は、カーネルにも SDL にも実装がなく、仕様として確認できなかった。

## 見ていないもの
- Switch Pro の校正後の値の計算、IMU（ジャイロ・加速度。オフセットはコードの読みだけ）。
- DS4 の Bluetooth の最小レポート（ID 0x01、10 バイト）。このリポジトリは 0x11 を前提にしている。
