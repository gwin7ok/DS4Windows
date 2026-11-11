# Phase22 追補計画書: Xbox コントローラーの動作を DS4 に近づける（接続種別・電池・BT 切断・BT 検出）

作成日: 2026-11-11
前提: `Phase22-Plan.md`（22-2 の XInput 経路、22-4 の HidHide）、`Model-Diagram/02-Extension-Guide.md`、`.github/copilot-instructions.md`（1 ファイル 1 型、層の依存、ログは `LogUser`＋リソース、テストは機能名）。
状態: **承認待ち（コードは未変更）**。

## 1. 背景（実機で見つかった違い）

| 番号 | 現象 | 原因（2026-11-11 にコードで確認） |
|---|---|---|
| 1 | BT 接続でも、一覧の状態列が USB のマークになる | `XboxProtocol.DetermineConnectionType` と `XInputTransportSource` の `checkConnection` が、常に `ConnectionType.USB` を返す |
| 2 | 電池が 99%。値が正しいか検証されていない | `XboxProtocol.TryReadBattery` が常に false を返し、`ProtocolDevice` が 99% 固定にする。XInput の電池 API（`XInputGetBatteryInformation`）は呼んでいない |
| 3・4 | 割り当て編集画面の Disconnect from Bluetooth、トレイの Disconnect、一覧の右クリックの Disconnect wireless で、BT が切れない | 3 つとも `InputDeviceCore.DisconnectBT`／`DisconnectWireless` へ行き着く。Xbox が使う `ProtocolDevice` が、3 つとも「何もせず false」に上書きしている。さらに `DisconnectWireless` の基底は `conType == BT` を見るが、いまは常に USB |
| 5 | BT 接続の Xbox は、サービス（または DS4Windows）の再起動をしないと一覧に出ない（USB は不要） | **仮説**: BT では、XUSB のインターフェースができた直後に、XInput の番号（LED）が読めず、`XInputTransportSource` が「まだ使えない」として返さない。その後、再検出のきっかけがない。**未確認** |
| 6 | 入力側の画像・ボタン名が DS4 のもの | 割り当て編集画面（`ProfileEditor`）が配置表 `DualShock4Editor` に固定、名前は `DS4ControlNames` の DS4 用の表に固定。DualShock 4・DualSense・Switch Pro も同じ（調べ済み）。本計画書の範囲外（§7） |

### Xbox 用のカートリッジについて

カートリッジは**ある**: `XboxDeviceCartridge`（生成）、`XboxProtocol`（接続方式・電池・出力）、`XboxDeviceModule`（有効判定・HidHide）。ただし、切断の手順はカートリッジにもプロトコルにも差し込み口がなく、**デバイスの基底**（`InputDeviceCore.DisconnectBT`。DS4・DualSense・Switch Pro が共有）にある。`ProtocolDevice` が `false` を返す上書きを持っているため、Xbox だけ使えない。つまり、共通部品は既にあるので、**新しい切断の実装は作らず、共通部品を使えるようにする**（§4）。

## 2. 設計の決定（承認を求める）

| 番号 | 項目 | 提案 | 理由 |
|---|---|---|---|
| D1 | 接続種別の判定 | 文字列だけで決める純粋関数 `XusbConnectionClassifier.Classify(devicePath)`（`Input/Xbox`）。パスに BLE の HID（`{00001812-0000-1000-8000-00805f9b34fb}`）か `bthenum`・`bthle` を含めば `BT`、それ以外は `USB`。`XInputTransportSource` の `checkConnection` と `XboxProtocol.DetermineConnectionType` の両方が、これ 1 つを使う | OS への問い合わせが要らず、テストしやすい。2 か所の判定が食い違わない。実機の BT のパス（`hid#{00001812-…}&dev&vid_045e&pid_0b22…`）は 22-4 で確認済み |
| D2 | 電池の読み方 | `IXInputApi` に `TryGetBattery(playerIndex, out XInputBattery)` を足し、`XInputTransport` が約 1 秒ごとに読み、合成レポートの末尾 2 バイト（種類・残量）に入れる。`XboxProtocol.TryReadBattery` がそこから % と充電中を返す（読み取りの流れは `ProtocolDevice` のまま） | 4 ms ごとに呼ぶと無駄。合成レポートに載せれば、`ProtocolDevice` と `IDeviceProtocol` を変えずに済む |
| D3 | 電池の値の割り当て | 種類が「有線」→ 99%（今までどおり、警告を出さない）。種類が電池（アルカリ・ニッケル水素）で、残量が 空／低／中／満 → **0／33／66／100%**。種類が不明、または読めない → 99%。充電中は XInput では取れないため false | XInput の残量は 4 段階だけ。「空」は 0%（2026-11-11 承認）。0% は電池が空に近い状態で、低電池の警告が出るのは正しい動作 |
| D4 | BT 切断の部品 | **基底の `DisconnectBT` をそのまま使う**。`ProtocolDevice` の上書きを、「接続が BT のときは基底を呼ぶ。それ以外は false」に変える。基底は `Mac`（BT アドレス）が必要なので、`IDeviceProtocol` に `string TryGetBluetoothAddress(IHidTransport)`（既定は null。C# の既定実装）を足し、`ProtocolDevice.PostInit` で `Mac` に使う（null なら今までの擬似シリアル）。Xbox は、機器 ID（パスにも含まれる `…&3cfa0617287c&…`）から、`3C:FA:06:17:28:7C` の形へ | DS4 と同じ手順（`IOCTL_BTH_DISCONNECT_DEVICE`）を重複せず使える。USB の Xbox は変わらない。`DisconnectDongle` は、Xbox にドングルがないので false のまま |
| D5 | BT の切断が BLE で効くか | **実機で確かめる**。効かなければ、DS4Windows 側で BT の接続を切る別の手段（デバイスの無効化と有効化など）の要否を、利用者に相談して決める | BLE の機器に、この IOCTL が効くかは文書で確認できていない |
| D6 | BT 検出の対処 | まず、調べるための診断ログ（Debug）を足して実機で原因を確かめる（5-0）。仮説どおりなら、番号が読めない XUSB インターフェースがあるとき、**再検出を少し待って数回（2 秒間隔、最大 5 回）繰り返す**。設計はそのときの結果で追記し、承認を得る | 仮説を確かめずに直すと、別の原因を見逃す |

## 3. 段階（1 → 2 → 3・4 → 5 の順）

各段階は、テスト（`dotnet test -c Debug -p:platform=x64`、遅い・性能のテストを除く）が全件合格してからコミットし、**実機の確認は 1 項目ずつ**行う。

| 段階 | 内容 | テスト | 実機の確認 |
|---|---|---|---|
| 22-7a（項目 1） | D1: `XusbConnectionClassifier`、`XInputTransportSource`・`XboxProtocol` の差し替え。**あわせて 5-0 の診断ログ**（XUSB 列挙の結果と、番号が読めず見送った件数を、Debug で）を足す | 分類の単体テスト（USB の Xbox 360・Xbox One 世代、BT の BLE、空）。偽の XUSB 供給元で、BT のパスの機器の接続種別が BT になる | BT 接続で、一覧の状態列が BT のマークになる。USB は USB のまま |
| 22-7b（項目 2） | D2・D3: `IXInputApi.TryGetBattery`、`XInputNativeApi`、合成レポートの拡張、`XboxProtocol.TryReadBattery`。電池の読みの間隔 | 偽の API で、各種類・各残量の % の変換、有線は 99、読めないとき 99。レポートの長さの変更に合わせた既存テストの更新 | BT で、Windows の設定の Bluetooth の電池の表示、または Xbox アクセサリーアプリの値と、DS4Windows の表示が妥当に対応する。USB は 99% のまま |
| 22-7c（項目 3・4） | D4: `IDeviceProtocol.TryGetBluetoothAddress`（既定 null）、`XboxProtocol` の実装（機器 ID から）、`ProtocolDevice` の `PostInit` と `DisconnectBT`／`DisconnectWireless` | アドレスの取り出し（実機の ID の例）、BT のときだけ基底が呼ばれる、USB のときは false、既存の `ProtocolDevice` を使う機種のテストが不変 | 割り当て編集画面の Disconnect from Bluetooth、トレイの Disconnect、一覧の右クリックの Disconnect wireless のそれぞれで、BT が切れる（D5） |
| 22-7d（項目 5） | 5-0 の診断ログで、原因を確かめ、D6 の設計を追記・承認ののち、再検出の繰り返しを実装 | 偽の供給元（初回は番号が読めず、2 回目に読める）で、再検出の繰り返しが機器を拾う。繰り返しの上限 | BT で接続したとき、再起動なしで一覧に出る。USB は影響なし |
| 22-7e | 文書（`Model-Diagram/01`・`02` の Xbox の記述、`IDeviceProtocol` の差し込み口の追加）、`Phase22-Plan.md` の進捗、台帳 | 既存の `ModelDiagramGuardTests` が合格 | |

## 4. 部品と変更の範囲

| 部品 | 層・場所 | 変更 |
|---|---|---|
| `XusbConnectionClassifier`（新規） | `Input/Xbox` | 純粋関数。1 ファイル 1 型 |
| `XInputBattery`（新規。値の型） | `Input/Abstractions` | 種類と残量。1 ファイル 1 型 |
| `IXInputApi`／`XInputNativeApi` | `Input/Abstractions`／`Input/Xbox` | `TryGetBattery` を足す（`xinput1_4.dll` の `XInputGetBatteryInformation`、機器の種類は `BATTERY_DEVTYPE_GAMEPAD`） |
| `XInputReportLayout`／`XInputTransport` | `Input/Xbox` | 合成レポートの末尾に電池の 2 バイト。電池は約 1 秒ごとに読み、間は前回の値 |
| `XboxProtocol` | `Input/Xbox` | 接続方式（D1）、電池（D2・D3）、BT アドレス（D4） |
| `IDeviceProtocol` | `Input/Abstractions` | `TryGetBluetoothAddress`（既定実装 null）。他の機種のプロトコルは変更不要 |
| `ProtocolDevice` | `Input/Devices` | `PostInit` の `Mac`、`DisconnectBT`／`DisconnectWireless` の条件付き基底呼び出し。`DisconnectDongle` は false のまま |
| `InputDeviceRegistry`（`findControllers`） | `Input/DS4Devices.cs` | 5 の対処のとき（D6） |

層の依存は、すべて第 1 層の中で閉じる（`LayerDependencyRuleTests` の基準 0 件を保つ）。ログは、新しい文言が要る場合、`LogUser`＋`Strings.resx`／`.ja.resx`。

## 5. 守るもの

- DS4・DualSense・Switch Pro の動作（特に `DisconnectBT` の基底の挙動）は変えない。`ProtocolDevice` を使う他の機種（`ProtocolDevice` を使う単純な機種）への影響は、既存のテストが不変であることで確かめる。
- USB の Xbox の動作（接続種別、電池 99%、切断なし）は変えない。
- 既存のログの文言は変えない。
- 22-4 の HidHide の動作（USB のみ）は変えない。

## 6. リスク

- BLE の Xbox で、`IOCTL_BTH_DISCONNECT_DEVICE` が効かない可能性（D5）。
- XInput の電池が、有線・BT の機種や Windows の版によって、期待した値を返さない可能性。読めないときは 99% に戻す（今までと同じ）ので、悪化はしない。
- 5 の仮説が外れる場合。診断ログで確かめてから直す（D6）。
- 合成レポートの長さの変更は、`XInputReportParser` と既存テストに及ぶ。末尾に足すだけで、先頭の位置は変えない。

## 7. 範囲外（別の Phase 候補）: 入力側の画像・ボタン名（項目 6）

全入力機種（DS4・DualSense・Switch Pro・Xbox）が、入力側の画像（`DualShock4Editor`）と名前（`DS4ControlNames`）に DS4 のものを使っている。機種ごとに画像と名前を変えるには、**入力の機種ごとの配置表と名前の表**を、カートリッジの側に持たせる設計が要る（出力ピッカーが仮想パッドの種類ごとに図を出し分けているのと同じ考え方）。影響が広い（割り当て編集画面、保存する名前［互換］、マクロ記録の表示）ので、別の Phase の計画書で決める。当面は、Xbox の A・B・X・Y が Cross・Circle・Square・Triangle の名前で出ることが、割り当て画面での見え方になる。
