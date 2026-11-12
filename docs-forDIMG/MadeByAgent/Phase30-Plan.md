# Phase30 計画書: 入力機種の読み取りの共通化と、機種の名指しの解消（コードレビュー 2026-11 の 4-2・4-3・5-2）

作成日: 2026-11-11
状態: **未着手。承認待ち**
出典: `docs-forDIMG/Code-Review-2026-11/04-Duplication-and-Abstraction.md`、`05-Extensibility.md`
前提: Phase29 の完了（同じファイルを触らないが、入力の経路の確認をまとめるため）

## 目的
1. DS4 と DualSense にある、読み取り・CRC の検証・停止の手順の重複を、`InputLoopDevice`（骨組み）へ寄せる。
2. 全機種の基底（`InputDeviceCore`）、第 2 層、`Core` に残る、機種の名指しをなくす。

機種を足す人が、基底や第 2 層の `if` を触らずに済む状態にする。

## 現状（調査）
- 重複（4-2）: `DualShock4Device.ReceiveFrame`（`Input/Devices/DualShock4Device.cs:539`〜`644`）と `DualSenseDevice.ReceiveFrame`（`Input/Devices/DualSenseDevice.cs:508`〜`598`）。CRC の検証、読み取りの失敗のログ、停止の手順が同じ形。ずれ: 失敗の上限（定数 `CRC32_NUM_ATTEMPTS` と、直書きの `10`）、ループの終え方（`Terminate` と、`exitInputThread`＋`Skip`）、ログ（リソースのキーと、旧い文言 `Strings.CRC32Fail`）。
- 重複（4-3）: `StartUpdate` のスレッドの起動が 4 機種に（`DualShock4Device.cs:229`、`DualSenseDevice.cs:430`、`SwitchProDevice.cs:310`、`ProtocolDevice.cs:101`）。`TimeoutTestThread` が 2 機種に（`DualShock4Device.cs:169`、`DualSenseDevice.cs:469`）。
- 名指し（5-2）:
  - `InputDeviceCore.WriteOutputOffOnce`（`Input/InputDeviceCore.cs:262`）と `PrimeBatteryReading`（同 `542`）が、DS4 かどうかで分岐する。
  - `ControllerSlotCommands.ApplyDualSenseRumbleOptions`（`Processing/Services/ControllerSlotCommands.cs:52`）が `is DualSenseDevice` で分岐する。
  - `ProfileSettingsService.GetRumbleBoost`（`Core/Settings/ProfileSettingsService.cs:256`）が、DualSense かどうかで分岐する。

## 方針（決定事項の案）
1. 動作は変えない。DS4 と DualSense のずれ（上限の数、ループの終え方、ログ）は、**今のまま**差し込み口の値として残す（揃えるのは、別に承認を得てから）。
2. DualSense・Switch Pro は実機がない。差分テスト（`FakeHidTransport` に台本を与え、状態・出力レポート・切断の順序を、変更の前後で比べる）を先に足してから動かす。
3. 骨組みに足す差し込み口は、機種が 2 つ以上使うものだけにする（共通化しすぎない）。
4. 機種ごとの自前のループを足さない（`DeviceLoopStructureTests` を守る）。

## 決めること（着手前にユーザーへ確認）
| # | 決めること | 選択肢（先頭が推奨） |
|---|---|---|
| D1 | DS4 と DualSense のずれを揃えるか | (A) 揃えない（今の動作を固定する）／(B) DualSense を DS4 に合わせる（ログの文言が変わるため、規則 2.3 の承認が要る。実機なし） |
| D2 | 4-3（スレッドの起動）を含めるか | (A) 含める（30-3）／(B) 見送る（重複は小さく、Switch Pro は起動の前に初期化があり、形が違う） |
| D3 | DualSense の振動の設定の置き場所 | (A) `IInputDeviceModule` に「機種だけの出力の設定を反映する」差し込み口を足す／(B) `IConfigurableDevice` に操作を足す |
| D5 | 出力レポートの組み立てと、校正の読み出しの切り出し（30-9〜30-11。2026-11-12 追加）を行うか | (A) 30-9（DualSense の出力レポート。313 行で一番大きい）だけ行う／(B) 3 つとも行う／(C) 見送る（実機がなく、差分テストだけが頼りになる） |
| D4 | 解析器の補助の共有（30-8）を行うか | (A) 見送る（解析器は 78 行と 95 行で小さく、差分テストで守られている。3 つ目の Sony 系の機種を足すときに行う）／(B) 行う |

## Step 一覧
| Step | 内容 | テスト | 状態 |
|---|---|---|---|
| 30-0 | 差分テストの準備: DS4（BT・USB）と DualSense（BT・USB）の `ReceiveFrame` の台本（成功、CRC の失敗が上限まで、タイムアウト、読み取りの失敗）を作り、今の結果（状態、`Removal` の回数、ログのキー、スレッドの終わり方）を固定する | 新規（機種ごと） | 未着手 |
| 30-1 | `InputLoopDevice` に、Bluetooth の CRC の検証（受信値の組み立てと比較、失敗の数え上げ）の保護メソッドを足し、2 機種から呼ぶ。上限の数と、上限に達したときの処理は、機種が渡す | 30-0 が不変 | 未着手 |
| 30-2 | 「読み取りの失敗 → ログ → 停止」の手順を保護メソッドへ出す。機器を起こす処理（DS4 の出力レポートの送信）と、ループの終え方は、機種が渡す | 30-0 が不変 | 未着手 |
| 30-3 | （D2 が A のとき）読み取りスレッドの起動と、Bluetooth の監視スレッドを基底へ出す。差し込み口は「スレッドの名前」と「機器を起こす処理」 | スレッドの名前・優先度・起動の順序を固定するテスト | 未着手 |
| 30-4 | `InputDeviceCore.WriteOutputOffOnce`／`PrimeBatteryReading` の DS4 の分岐を、仮想メソッドにする（基底は何もしない／既定の読み方。DS4 が中身を持つ） | 既存の電池・出力停止のテストが不変 | 未着手 |
| 30-5 | DualSense の振動の設定（`ApplyDualSenseRumbleOptions`）を、D3 の場所へ移し、第 2 層の `is DualSenseDevice` をなくす。UI の窓口（`IControllerSlotCommands`）の名前は変えない | 偽の機種で、設定が機種へ届くこと | 未着手 |
| 30-6 | `ProfileSettingsService.GetRumbleBoost` の DualSense の特例を、機種が返す値（振動の強さの調整を受けるか）で判定する | 既存の振動のテストが不変 | 未着手 |
| 30-7 | 文書: `02-Extension-Guide.md` §1（差し込み口の一覧）、`01-Structure.md`、規則 3.1 を更新する。`Code-Review-2026-11/00-Summary.md` の該当行を「是正済み」へ | — | 未着手 |
| 30-8 | （追加。4-7。D4 が A のときだけ）DS4 と DualSense の解析器・タッチの取り込みで、位置だけ違う同じ形の 2 つ（方向キーの 4 ビット → 4 つの bool、タッチ 1 本の 4 バイト → 生値）を、`Input/Parsers` の静的な補助へ出す。解析器の全体は 1 つにまとめない | 旧実装との差分テスト（既存）が不変。割り当て 0 | 未着手 |

### 追加（2026-11-12。外部レビューの照合 `Code-Review-2026-11/07-External-Review-2026-11-12.md` から統合）: 出力レポートの組み立てと、校正の読み出しの切り出し
解析（`Input/Parsers`）と読み取りの流れ（`InputLoopDevice`）は、すでにデバイスの型の外にある。デバイスの型に残る大きな塊は、次の 3 つである。差分テストの準備（30-0）を共有できるため、本 Phase に統合する。行うかどうかは D5 で決める。

| Step | 内容 | テスト | 状態 |
|---|---|---|---|
| 30-9 | `DualSenseDevice.PrepareOutReport`（313 行。`Input/Devices/DualSenseDevice.cs:779`〜）の、出力レポートの組み立て（振動、ライトバー、プレイヤー LED、ミュート LED、トリガーの効果、USB と BT の書式の違い）を、状態を持たない部品へ出す。入力は「いまの出力の状態」と「前回送った内容」、出力はバイト列と「変化があったか」。書き込みと CRC の付与は、デバイスに残す | 30-0 に、出力レポートの台本（各設定の組み合わせ → バイト列）を足し、変更の前後で不変。割り当て 0 | 未着手 |
| 30-10 | `SwitchProDevice.CalibrationData`（216 行。`Input/Devices/SwitchProDevice.cs:826`〜）の、校正の値の解釈（SPI から読んだバイト列 → スティックと 6 軸の校正値）を、状態を持たない部品へ出す。読み出しのやりとり（`Subcommand`）は、デバイスに残す | 校正のバイト列 → 校正値の対応を固定する | 未着手 |
| 30-11 | `SwitchProDevice` の振動の表と組み立て（`PrepareRumbleData`、`fixedRumbleTable`／`compiledRumbleTable`）を、状態を持たない部品へ出す。あわせて、同ファイルに残るコメントアウトの塊（`Thread.Sleep` の行など）を、設計の意図と TODO を残して整理する（規約 3.3 原則 5。範囲はこの Step に限る） | 振動の強さ → バイト列の対応を固定する | 未着手 |

- 置き場所の案: `Input/Parsers` の隣に、出力レポートの組み立ての置き場（例: `Input/Reports`）を作る。規則 R1 と `02-Extension-Guide.md` を更新する。
- 外部レビューの「新しいコントローラー（Vader 4 Pro など）の追加が楽になる」について: Flydigi Vader 4 Pro のデバイスサポートは、承認のうえ廃止済みである。単純な機種は、すでに `IDeviceProtocol`＋`ProtocolDevice` で足せる。この追加の効果は、機種の追加よりも、DualSense・Switch Pro の出力をテストできるようにすることにある。
- `InputDeviceCore`（1,148 行）は、メソッド 65 個で最長 67 行と、長いメソッドがない。30-4（DS4 だけの分岐の解消）のほかには分割しない。

## 確認の方法
- 各 Step のあと、差分テスト（30-0）と `DeviceLoopStructureTests`、`LayerDependencyRuleTests`。
- 実機（DS4、Xbox）: 接続・切断（USB、BT）、BT の電波切れでの切断、接続時の電池の表示、振動。DualSense・Switch Pro は「実機未確認」を `Known-Issues-Ledger.md` §3 へ足す。

## リスク
- 30-1・30-2: 中。切断の手順は、ガードレール（切断時クリーンアップ、ドライバーの破棄順序）に近い。順序を 1 行も入れ替えない。
- 30-3: 中。スレッドの起動の時機が変わると、初期化の競合が出る（DS4 は `postInitDone` を待つ処理がある）。
- 30-5・30-6: 低〜中。DualSense は実機で確かめられない。

## 履歴
- 2026-11-11: 計画書を作成。コードは未着手。
- 2026-11-11: 追加調査の結果を反映。解析器どうしの重複（4-7。低）を、任意の Step 30-8 と、決めること D4 として足した。`ProcessFrame` の全体は、時刻・電池・同期の扱いが機種ごとに違い（DS4 の 110 行と DualSense の 93 行で、違う行が 85 行）、共通化の対象にしない。なお、動き・タッチのイベント引数の割り当て（6-11）は、第 1 層のファイルを触るが、性能の是正として Phase29（29-9）に置いた。
- 2026-11-12: 外部レビューの照合の結果を統合。出力レポートの組み立てと校正の読み出しの切り出し（30-9〜30-11）と、決めること D5 を足した。
