# 拡張の手順書（機種・出力の種類を足す）

最終更新: 2026-11-10（Phase17-0。現状の手順を書いた。Phase17-1〜17-5 で手順が短くなるたびに、この文書を更新する。最終形は Phase17-4 で確定する）。

---

## 1. 入力機種（コントローラー）を足す

現状は、1 系統あたり **約 28 ファイル** を触る（Switch Pro の実測。Phase17-Plan.md §2.1）。機種ごとに同じ形のクラスを書き足す作業になっている。

| 区分 | 足すもの（`Xxx` は系統名） | 参考にする既存の例 |
|---|---|---|
| 第1層 | `Input/Devices/XxxDevice.cs`（`DS4Device` の派生）、`XxxDeviceCartridge.cs`（VID/PID の表・生成）、`Input/Parsers/XxxReportParser.cs` | `SwitchProDevice`、`SwitchProDeviceCartridge`、`SwitchProReportParser` |
| Core/Model | `InputDeviceFamily` と `InputDeviceType` に値を足し、`InputDeviceType` の値に `[InputDeviceFamily(...)]` を付ける（17-1 で表は不要になった） | |
| 第2層 | `Processing/Services/XxxDeviceModule.cs`、`ServiceRegistration.Processing.cs` に `AddInputDevice<XxxDeviceCartridge, XxxDeviceModule>()` を 1 行 | `SwitchProDeviceModule` |
| 設定 | `Core/Settings/XxxDeviceOptions.cs`、`XxxControllerOptions.cs`、`ControlServiceDeviceOptions` に系統を足す | |
| 保存（17-2 で簡素化） | `XxxDeviceOptions`（節の名前を `base(…)` に渡し、機種固有の項目は `ExtraSettings` に `ControllerSetting` で書く）、`XxxControllerOptions`（`SectionName` と `Schema` に `ControllerSetting` を並べる）。DTO・シリアライザー・`AppSettingsDTO` は変えない | 保存の黄金テスト（`appsettings-varied.xml`）が形を守る |
| UI | `XxxControllerOptionsWrapper`、`ControllerRegDeviceOptsViewModel`、`FirstLauchUtilViewModel`、`SettingsViewModel`、各 XAML、`Strings.*.resx` | |
| テスト | カートリッジ・モジュール・解析器の差分テスト | `DeviceModuleTests` ほか |

### 単純な機種（ボタン・スティック・トリガーと、任意の電池・出力）の最短経路（17-3）
`IDeviceProtocol`（`Parser`、`TryReadBattery`、`TryBuildOutputReport`、接続方式の判定）と、`ProtocolDeviceCartridge` の派生（`Family`、`KnownDevices`、`Protocol`）を書けば、読み取りループ・`Report`・出力の書き込み・切断は共通の `ProtocolDevice` が行う。そのうえで、(1) `InputDeviceFamily`・`InputDeviceType`（`[InputDeviceFamily]` つき）に値を足し、(2) `ControlServiceDeviceOptions` の系統の表に 1 行、(3) 接続フック（なければ `NullInputDeviceModule` 相当）を作り、(4) `AddInputDevice<…>()` を 1 行。保存は設定クラスの `ControllerSetting` の定義だけ（17-2）。見本は `ProtocolDeviceTests` の偽の機種。ジャイロ・タッチパッド・複数の接続方式を持つ機種は、従来どおり `DS4Device` の派生で書く。

### 今後（Phase17 の進行で更新する）
- 17-1（済）: 種類と系統の対応は `InputDeviceType` の値に付ける `[InputDeviceFamily]` 属性だけ（`InputDeviceTypeInfo` の表は不要）。カートリッジは `Family` を持ち、登録はカートリッジとモジュールを 1 行（`AddInputDevice<TCartridge, TModule>()`）で行う。
- 17-2（保存は済、画面は未）: 保存は設定の定義から作られる（DTO が不要になった）。画面（Wrapper・XAML）の自動生成は未着手。
- 17-3〜17-5: 読み取りループ・出力・切断が `ControllerSession` に集まり、機種は `IDeviceProtocol` を書くだけになる。

## 2. 仮想パッドの出力の種類を足す

| 足すもの | 場所 |
|---|---|
| カートリッジ（`IOutputDeviceCartridge` の実装） | `Output/Devices/XxxOutputCartridge.cs`（例: `Ds4OutputCartridge`、`Xbox360OutputCartridge`） |
| 保存キー・選択肢の番号 | `Core/Model/OutContTypeInfo`（種類ごとの `if`／`switch` を増やさない） |
| DI 登録 | `ServiceRegistration.Output.cs` |
| バックエンドの型 | ViGEm の型は `Output/Backend` の中だけ（L9） |

## 3. アクション（特殊アクション）を足す

`Processing/Actions/` に、`Action*` と `*ActionController` の組で足す（規約 3.4 R4）。判定と実行は `TriggerDispatchEngine` が担う。

## 4. 足したあとの確認

1. `dotnet build`／`dotnet test DS4WindowsTests/DS4Windows.Actions.Tests.csproj -c Debug -p:platform=x64`（警告 0・エラー 0）。
2. `LayerDependencyRuleTests`（依存の向き）、`ConstructorArityRuleTests`（引数 15 以下）、`Phase14NamespaceFolderGuardTests`（名前空間＝フォルダ）。
3. 保存形式を触ったら、保存の黄金テストが不変であること。
4. 実機がない機種は、差分テスト（`FakeHidTransport` に台本を与え、出力レポート・状態・切断の順序を確かめる）で守り、「実機未確認」を台帳に残す。
