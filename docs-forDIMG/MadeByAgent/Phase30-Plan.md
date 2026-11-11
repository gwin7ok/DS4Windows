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

## 確認の方法
- 各 Step のあと、差分テスト（30-0）と `DeviceLoopStructureTests`、`LayerDependencyRuleTests`。
- 実機（DS4、Xbox）: 接続・切断（USB、BT）、BT の電波切れでの切断、接続時の電池の表示、振動。DualSense・Switch Pro は「実機未確認」を `Known-Issues-Ledger.md` §3 へ足す。

## リスク
- 30-1・30-2: 中。切断の手順は、ガードレール（切断時クリーンアップ、ドライバーの破棄順序）に近い。順序を 1 行も入れ替えない。
- 30-3: 中。スレッドの起動の時機が変わると、初期化の競合が出る（DS4 は `postInitDone` を待つ処理がある）。
- 30-5・30-6: 低〜中。DualSense は実機で確かめられない。

## 履歴
- 2026-11-11: 計画書を作成。コードは未着手。
