# コードレビュー（2026-11）観点 2: 各層の役割の分担と、クラスの配置

作成日: 2026-11-11（対象のコミットと読み方は `00-Summary.md`）

## 1. 良い点（維持すべき設計）

- UI から第 1 層の型（`InputDeviceCore`、各機種のデバイス、`HidDevice`）の実使用は 0 件だった。UI は `IControllerSlotQuery`／`IControllerSlotCommands`／`IControllerReadingsSource` だけを通す。`using DS4Windows.Input;` が 10 ファイルほどに残るが、型は使っていない（消してよい）。
- `Composition` は組み立てだけで、業務ロジックがない。
- ホットプラグの通知登録は、GUID を `IEnvironmentService` から受ける（`DS4Windows/UI/MainWindow.xaml.cs:1188`、`1197`）。UI が HID の型を直接知らない。

## 2. 問題点

### 2-1（中）永続化の層が、読み込み中にプログラムを起動する
- 場所: `DS4Windows/Core/Persistence/ProfileSections/ProfileMiscLoadSection.cs:59`〜`110`
- 事実: プロファイルの XML を読む節が、プロセスを列挙し、見つからなければ 5 秒待ってプログラムを起動する。
- なぜ問題か: 「読む」と「外へ副作用を起こす」が同居している。読み込みをテストすると、プロセスの列挙が走る（`IProcessInspector` がないとき）。同じ処理が第 2 層にもある（`04-Duplication-and-Abstraction.md` の 4-1）。
- 修正の方向: 読み込みは値（`launchProgram`）を入れるだけにし、起動は第 2 層の 1 か所へ寄せる。

### 2-2（中。決定が要る）第 3 層が第 1 層の具象を直接使う
- 場所: `DS4Windows/Output/` の 15 ファイル。例: `Output/Abstractions/IOutputDeviceCartridge.cs`、`Output/Devices/Ds4OutputCartridge.cs:32`（`AttachFeedback(int index, OutputDevice outDevice, InputDeviceCore device)`）、`Output/Services/DeviceRumbleService.cs`、`HidHideService.cs`、`OscService.cs`、`UdpMotionService.cs`、`VirtualPadService.cs`
- 事実: 規則 L4 は「第 3 層は第 2 層・UI・Composition を参照しない」で、第 1 層への参照は禁じていない。一方、`01-Structure.md` §2 の図は、Output から出る矢印を Core だけにしている。
- なぜ問題か: 図と実態が合わない。出力の種類を足す人は、`InputDeviceCore`（約 1,150 行、公開メンバー約 118）を知る必要がある。
- 修正の方向（どちらかを決める）: (A) 振動・ライトバーの戻りに要る最小の契約を `Core/Abstractions` に置き、第 3 層はそれだけを受ける。(B) 図と規則を実態に合わせ、「第 3 層は第 1 層の基底を使ってよい」と明記する。

### 2-3（低）ファイル名と型名が違う（規則 3.3 原則 2）
- 場所と中身:
  - `DS4Windows/Input/DS4Devices.cs:41` → `InputDeviceRegistry`
  - `DS4Windows/Processing/Services/Ds4DeviceRegistryAdapter.cs:11` → `InputDeviceRegistryAdapter`
  - `DS4Windows/Processing/Abstractions/IDs4DeviceRegistry.cs:7` → `IInputDeviceRegistry`（2026-11-11 追加調査で確認）
- 事実: `Structure-Review-2026-11.md` の F（名前の一般化）で、型名だけを変え、ファイル名を変えなかった。既存のテストは拾っていない。
- 追加調査（2026-11-11）: 照合に引っかかった残りの 2 件は、型を持たないファイルだった。
  - `DS4Windows/Core/Updater/UpdaterInstallTrace.cs`: コメントだけの 7 行（「以前の補助を、同じものを作り直さないために置き場所として残す」という説明）。型がなく、ビルドに何も足さない。削除してよい候補（規則 2.4 の手順で判断する）。
  - `DS4Windows/UI/Themes/_.cs`: 名前空間の宣言だけ（IDE が名前空間を解決できるようにするための置き石、とコメントにある）。そのままでよい。
- 修正の方向: `git mv` で改名し（R7）、ファイル名と型名の照合を型の検査へ足す。

### 2-4（低）UDP（Cemuhook）の型が `Input/` にある
- 場所: `DS4Windows/Input/DualShockPadMeta.cs`、`PadDataRspPacket.cs`、`DsState.cs`、`DsBattery.cs`、`DsConnection.cs`、`DsModel.cs`
- 事実: 使うのは `Output/UdpServer.cs`、`Output/Services/UdpMotionService.cs`、`UdpMotionServerAdapter.cs` と、`Processing/Abstractions/IUdpMotionServer.cs`、`UdpPadDetailProvider.cs`。第 1 層は使っていない。
- 修正の方向: 第 2 層の契約が参照するので、`Core/Model`（層をまたぐ値の型。R2）へ移す。

### 2-5（低）`Processing/Services` に約 70 ファイルが平置き
- 事実: 接続の一生（`DeviceConnector`、`DeviceConnectionSetup`、`DeviceDisconnectHandler`、`ServiceLifecycle`、`RuntimeBootstrapper`）、補正の段（`*Processor`、`*Stage`）、UI 向けの窓口（`ControllerSlotQuery`、`ControllerSlotCommands`、`ControllerReadingsSource`）、機種のモジュール（`*DeviceModule`）、IPC、マクロが 1 フォルダーにある。
- 修正の方向: 機能ごとのフォルダーへ分ける（機械的。R7 に従い、移動と名前空間の書き換えを別コミットに）。`Input/` の直下の 31 ファイルも同様に見直せる。

## 3. 大きいファイル（参考）

| ファイル | 行数 | 見立て |
|---|---|---|
| `UI/MainWindow.xaml.cs` | 1,836 | 既知（Structure-Review の B） |
| `Processing/Touchpad/TouchpadMouse.cs` | 1,809 | 未調査。タッチパッドの出力の種類ごとの処理が 1 つの型にある |
| `UI/ProfileEditor.xaml.cs` | 1,735 | 既知（B） |
| `Input/Devices/DualSenseDevice.cs` | 1,284 | 出力レポートの組み立て（`PrepareOutReport` が約 310 行）が大半 |
| `Input/Devices/SwitchProDevice.cs` | 1,244 | 校正の読み出し（`CalibrationData` が約 215 行）、コメントアウトが多い |
| `Core/Settings/BackingStore.cs` | 1,179 | 既知（Structure-Review の A。公開メンバー約 215） |
| `Input/InputDeviceCore.cs` | 1,148 | 全機種の基底。DS4 だけの処理が残る（`05-Extensibility.md` の 5-2） |
| `App.xaml.cs` | 978 | `01-DI.md` の 1-3 |
