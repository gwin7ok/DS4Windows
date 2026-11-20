# PhaseRV6-0: 未参照の型の調査（承認待ち）

作成日: 2026-11-20
方法: 名前を単語で検索（`DS4Windows`・`DS4WindowsTests`・`StandaloneTests`・`DS4WindowsCli` の `.cs`・`.xaml`）。`git log -S` で追加の経緯。規則 2.4 の手順 1・2・4。

## 1. 誤検出（宣言のほかに使う箇所がある。残す）
`ViGEmBusInfo`（`EnvironmentService` の private 入れ子で使用）、`InputReportDataBytes`・`InputReportDataBytesUSB`（`DualSenseDevice` が `new InputReportDataBytesUSB()`）、`HapticPowerLevelFriendlyName`（既定値に使用）、`GyroMouseSensDualSense`、`SwitchProSubCmd`（`Subcommand(...)` で使用）、`DeviceInfo`、`RumbleTableData`（44 件）、`MessageType`、`ClientRequestTimes`（`DsuClientRegistry` の入れ子で使用）、`ParsedTriggerList`、`ControlSelection`（23 件）。

## 2. 本当に未参照（宣言 1 件のみ）
| 型 | 場所 | 経緯（`git log -S`） | 判断 | 理由 |
|---|---|---|---|---|
| `TriggerType` | `Core/Settings/TriggerType.cs` | ActionManager の Press 状態管理の改善（5e231d18）で追加。後に使われなくなった | **削除** | 4 層の設計に出てこない。参照 0。Phase の目的（DI・依存の整理）に使う場面がない |
| `MappingDoneEventArgs` | `Processing/MappingDoneEventArgs.cs` | 旧 Mapping の完了イベントの引数（上流からの引継ぎ）。参照は `CodeHealthBaseline` の行数の許可リストのみ | **削除**（許可リストの行も消す） | イベント自体が撤去済み。使うべき場面がない |
| `BTOutputReportMethod` | `Input/InputDeviceCore.cs`（入れ子の enum） | Windows 7 の Bluetooth DS4 の出力方式（b0f5c4c2 ほか。上流のコード） | **削除** | 機種の読み取りは `InputLoopDevice` へ移り、方式の切り替えは使わない |
| `LatencyWarnMode` | `UI/ControllerReadingsControl.xaml.cs`（private enum） | 上流の取り込み（906946a7） | **削除** | private で参照 0 |
| `RumbleType` | `UI/ProfileEditor.xaml.cs`（private enum） | 上流の取り込み（722c561c ほか） | **削除** | private で参照 0 |
| `InputReportDataBytesBT` | `Input/Devices/DualSenseDevice.cs`（入れ子の class） | DualSense の初期実装（99c29b44） | **保留（残す）** | Bluetooth のレポートの形を表す説明の役目があり、DualSense の BT 対応の際に使う見込み。TODO（原則4）を付けて残す |

## 3. 承認をお願いする点
- §2 の 5 件（`TriggerType`・`MappingDoneEventArgs`・`BTOutputReportMethod`・`LatencyWarnMode`・`RumbleType`）の削除（RV6-5。1 型 1 コミット、`RemovedApiRuleTests` の表へ）。
- `InputReportDataBytesBT` は残し、原則4 の TODO を付ける。
- 計画書の「約 28 件」のうち、実際に未参照だったのは 6 件。残りは入れ子の型・同一ファイル内の使用で、誤検出。
