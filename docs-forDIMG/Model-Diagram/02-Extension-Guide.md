# 拡張の手順書（機種・出力の種類を足す）

最終更新: 2026-11-14（Phase31: 共通の差し込み口の一覧を追記）。2026-11-10（Phase17-0。現状の手順を書いた。Phase17-1〜17-5 で手順が短くなるたびに、この文書を更新する。最終形は Phase17-4 で確定する）。

---

## 1. 入力機種（コントローラー）を足す

現状は、1 系統あたり **約 28 ファイル** を触る（Switch Pro の実測。Phase17-Plan.md §2.1）。機種ごとに同じ形のクラスを書き足す作業になっている。

| 区分 | 足すもの（`Xxx` は系統名） | 参考にする既存の例 |
|---|---|---|
| 第1層 | `Input/Devices/XxxDevice.cs`（`InputDeviceCore` の派生）、`XxxDeviceCartridge.cs`（VID/PID の表・生成）、`Input/Parsers/XxxReportParser.cs` | `SwitchProDevice`、`SwitchProDeviceCartridge`、`SwitchProReportParser` |
| Core/Model | `InputDeviceFamily` と `InputDeviceType` に値を足し、`InputDeviceType` の値に `[InputDeviceFamily(...)]` を付ける（17-1 で表は不要になった） | |
| 第2層 | `Processing/Services/XxxDeviceModule.cs`、`ServiceRegistration.Processing.cs` に `AddInputDevice<XxxDeviceCartridge, XxxDeviceModule>()` を 1 行 | `SwitchProDeviceModule` |
| 設定 | `Core/Settings/XxxDeviceOptions.cs`、`XxxControllerOptions.cs`、`ControlServiceDeviceOptions` に系統を足す | |
| 保存（17-2 で簡素化） | `XxxDeviceOptions`（節の名前を `base(…)` に渡し、機種固有の項目は `ExtraSettings` に `ControllerSetting` で書く）、`XxxControllerOptions`（`SectionName` と `Schema` に `ControllerSetting` を並べる）。DTO・シリアライザー・`AppSettingsDTO` は変えない | 保存の黄金テスト（`appsettings-varied.xml`）が形を守る |
| UI（17-2c で自動生成） | `InputDeviceFamily` の値に `[InputDeviceFamilyUi(…)]`（文言のリソースキー）、設定項目に `.WithUi(ラベルのキー, …)`、`Strings.resx`／`Strings.ja.resx` の文言。画面の Wrapper・XAML・ViewModel は足さない | |
| テスト | カートリッジ・モジュール・解析器の差分テスト | `DeviceModuleTests` ほか |

### 単純な機種（ボタン・スティック・トリガーと、任意の電池・出力）の最短経路（17-3）
`IDeviceProtocol`（`Parser`、`TryReadBattery`、`TryBuildOutputReport`、接続方式の判定、Bluetooth のアドレス `TryGetBluetoothAddress`［任意。返すと、共通の Bluetooth の切断が使える］。電池の段階を % にする計算は共通の `BatteryScale.ToPercent`。Bluetooth の切断が効かない機器は `SupportsBluetoothDisconnect` が false を返すと、切断は成功と偽らず失敗になり、警告のログが 1 回出る）と、`ProtocolDeviceCartridge` の派生（`Family`、`KnownDevices`、`Protocol`）を書けば、読み取りループ・`Report`・出力の書き込み・切断は共通の `ProtocolDevice` が行う。そのうえで、(1) `InputDeviceFamily`・`InputDeviceType`（`[InputDeviceFamily]` つき）に値を足し、(2) `ControlServiceDeviceOptions` の系統の表に 1 行、(3) 接続フック（なければ `NullInputDeviceModule` 相当）を作り、(4) `AddInputDevice<…>()` を 1 行。保存は設定クラスの `ControllerSetting` の定義だけ（17-2）。見本は `ProtocolDeviceTests` の偽の機種。ジャイロ・タッチパッド・複数の接続方式を持つ機種は、従来どおり `InputDeviceCore` の派生で書く。

### HID を持たない機器（XInput の Xbox）の足し方（Phase22-2）
通信路が HID でない機種は、`IHidTransport` を実装した通信路（例: `XInputTransport`。状態を合成レポートにして返す）と、機器を見つける `IInputTransportSource`（例: `XInputTransportSource`）を書く。供給元は機器ごとの `VidPidInfo` を返し、`InputDeviceRegistry` が HID の機器と同じ流れ（有効設定の確認、仮想パッドの除外、シリアル、カートリッジによる生成）で扱う。カートリッジの `KnownDevices` は空にする（HID の列挙に載せない）。系統・設定・登録は、上の手順のとおり。着脱をホットプラグで拾うには、その機器のインターフェースのクラスの GUID を `MainWindow` に通知登録する。

### 今後（Phase17 の進行で更新する）
- 17-1（済）: 種類と系統の対応は `InputDeviceType` の値に付ける `[InputDeviceFamily]` 属性だけ（`InputDeviceTypeInfo` の表は不要）。カートリッジは `Family` を持ち、登録はカートリッジとモジュールを 1 行（`AddInputDevice<TCartridge, TModule>()`）で行う。
- 17-2（済）: 保存と画面が、設定の定義から作られる（DTO・Wrapper・XAML が不要）。
- 17-3〜17-5（一部済）: 読み取りの 1 フレームの流れは `InputLoopDevice`（骨組み）に集まった。機種は差し込み口（`ReceiveFrame`／`IsExpectedReport`／`ProcessFrame`／`ProcessIdle`／`WriteOutputFrame`）だけを書く。`ProtocolDevice`・`DualSenseDevice`・`SwitchProDevice`・`DualShock4Device` が載っている。`InputDeviceCore` は全機種の共通の基底（抽象クラス）で、自前のループは持たない（Phase18。`DeviceLoopStructureTests` が検査する）。

### 共通の差し込み口（Phase31）
- **読み取りの骨組み**（`InputLoopDevice`）が持つ共通の部品: Bluetooth の入力レポートの CRC の検証（`VerifyBtInputCrc`。上限は機種が渡す。上限に達したときのログと切断は機種が書く）、停止の後始末（`StopAndRemove`）、Bluetooth の監視スレッド（`TimeoutTestThread`。機器を起こす処理は `KickWindowsToNoticeDisconnect` を override する）、スレッドの起動（`StartBackgroundThread`）。
- **機種だけの処理は、基底の仮想メソッドを override する**（基底・第 2 層・`Core` に機種の判定を書かない）: `WriteOutputOffOnce`（出力を OFF にする直前の消灯・振動ゼロ。既定は何もしない）、`PrimeBatteryReading`（接続直後の電池の取り込み。既定は何もしない）、`ApplyRumbleEmulation`（振動のエミュレーションの方式と触覚の強さ。既定は false）、`UsesGenericRumbleRescaleSetting`（振動の強さの補正を設定で切り替える機種か）。
- **バイト列の組み立て・解釈は、状態を持たない部品に出す**: 入力の解析は `Input/Parsers`（DS4・DualSense が共有する方向キーとタッチは `SonyReportDecoding`、Switch Pro の校正の解釈は `SwitchProCalibrationDecoder`）、出力の組み立ては `Input/Reports`（`DualSenseOutputReportBuilder`、`SwitchProRumbleEncoder`）。実機がない機種は、組み立てたバイト列を記録として固定するテスト（`DualSenseOutputReportTests`、`SwitchProRumbleDataTests`）で守る。

## 2. 仮想パッドの出力の種類を足す

種類ごとの違いは、**値**と**処理**に分けて、それぞれ決まった場所へ置く。`OutContType.X360`／`.DS4` を名指しできるのは許可リストのファイルだけで、`OutputTypeNamingGuardTests` が守る（ここ以外へ書かない）。

| 足すもの | 場所 |
|---|---|
| 種類の値（`OutContType` の項目） | `Core/Model/OutContType` |
| 値の違い: 保存キー、プロファイルの選択肢の並び、IPC の接続の名前（`plug<名前>`）、ボタン名の表 | `Core/Model/OutContTypeInfo` の表（`ProfileChoices`／`ProfileKeys`／`IpcPlugNames`／`ButtonNameTables` の同じ位置へ足す）。既定の種類は `DefaultType` |
| 処理の違い（生成、型の絞り込み、振動・ライトバーの戻りの接続） | カートリッジ（`IOutputDeviceCartridge` の実装）`Output/Devices/XxxOutputCartridge.cs`（例: `Ds4OutputCartridge`、`Xbox360OutputCartridge`） |
| 処理の違い（送出の直前の後処理、XInput のスロット番号、差し込みのログ） | 出力デバイス（`OutputDevice` の仮想メンバー: `ConvertandSendReport`、`SupportsXInputSlotNumber`／`XInputSlotNumber`、`LogPlugged`）。DS4 のトリガーのモードは `DS4OutDevice.ApplyTriggerOutputMode` |
| 出力ピッカーの図 | `UI/Controls/ControllerLayouts.ForOutputType`（種類 → 配置表と背景の画像） |
| DI 登録 | `ServiceRegistration.Output.cs` |
| バックエンドの型 | ViGEm の型は `Output/Backend` の中だけ（L9） |

## 3. アクション（特殊アクション）を足す

`Processing/Actions/` に、`Action*` と `*ActionController` の組で足す（規約 3.4 R4）。判定と実行は `TriggerDispatchEngine` が担う。

### 保存・読み込み（Phase37）

- 特殊アクションの保存は 1 本だけ。`SpecialActionRepository.SaveAction` → `SpecialActionsXmlSerializer.SaveActionNew`（種別の数 `mode` ごとに `SpecialAction` を作って一覧へ入れる）→ `SaveActions`（一覧全体を `ActionsDTO` で書く）。読み込みも `ActionsDTO`（`Core/Persistence/Dto`）に集まっている。種別を足すときは、`ActionTypeId`・`SaveActionNew` の種別の分岐・`ActionsDTO` の読み書きを揃える（種別の数は 1〜7、9、10、11。8 は欠番）。
- 保存する XML の形（要素名と順序）は変えない。**全種別を本番の保存の経路で書いた黄金**（`actions-production.xml`／`.state.txt`。`ActionsProduction_*` のテスト）に、新しい種別を足す。

## 4. 足したあとの確認

1. `dotnet build`／`dotnet test DS4WindowsTests/DS4Windows.Actions.Tests.csproj -c Debug -p:platform=x64`（警告 0・エラー 0）。
2. `LayerDependencyRuleTests`（依存の向き）、`ConstructorArityRuleTests`（引数 15 以下）、`Phase14NamespaceFolderGuardTests`（名前空間＝フォルダ）。
3. 保存形式を触ったら、保存の黄金テストが不変であること。
4. 実機がない機種は、差分テスト（`FakeHidTransport` に台本を与え、出力レポート・状態・切断の順序を確かめる）で守り、「実機未確認」を台帳に残す。

## 5. プロファイルの設定の項目を足す（Phase33）

スティックの項目（左右で同じ形）の例。値の入れ物から画面まで、次の順に触る。

| # | 触る場所 | ファイル |
|---|---|---|
| 1 | 値の入れ物 | `Core/Settings`（例: `StickAntiSnapbackInfo`、`StickSlotSettings`） |
| 2 | 読み込みの節 | `Core/Persistence/ProfileSections/` の読み込みの節 |
| 3 | 保存の節 | `Core/Persistence/ProfileSections/` の保存の節（読み込みと別の名前の節にある場合がある） |
| 4 | ViewModel | `UI/ViewModels/StickSideViewModel.cs` に 1 つ。左右の振り分けは型の中にあるので、左右で書き分けない |
| 5 | 画面 | `UI/StickSideSettingsView.xaml` に 1 か所。左右とも同じ部品を使う（`StickSettingsPanel` は部品を 2 つ置くだけ） |

- 領域ごとの持ち主（Phase25・Phase37-5）: 配列ではなく、スロットごとのオブジェクトに足す。`GyroSlotSettings`・`TouchpadSlotSettings`・`StickSlotSettings`・`TriggerSlotSettings`・`SixAxisSlotSettings`・`LightbarSlotSettings`・`DualSenseSlotSettings`（DualSense の振動方式など）・`BasicSlotSettings`（タッチの切り替え、無操作の切断、出力データ、BT のポーリング、起動プログラム、DInput のみ）・`RumbleSlotSettings`（振動の強さ、自動停止、モーターの入れ替え）・`OutputTypeSlotSettings`（仮想コントローラーの種類、DS4 のトリガーの出力方式）。持ち主は `BackingStore` が作り、`IProfileSettingsService` の `DualSenseSlot(slot)`・`BasicSlot(slot)`・`RumbleSlot(slot)`・`OutputType(slot)` から引く。既定へ戻す処理は、持ち主の `ResetForProfileLoad()`（`BackingStore.ResetProfile` が順に呼ぶ）に書く。
- 読み込みの節と保存の節は、領域ごとに `ProfileAreaSection` で対にして `LegacyProfileXmlSerializer` が宣言する（General／TriggerStick／Motion／OutputCurve／OutputController／Actions／Button）。項目を足す節が、どの領域に属するかを、そこで確かめる。
- 状態を丸ごと書き出すゴールデン（`Golden/Phase7Step10c/*.state.txt`）は、項目の経路名で比べる。持ち主を足す・移すと経路名が変わるので、値が同一であることを（並べ替えて）確かめてから、新しい出力を採用する。
- 保存する名前（XML の要素名）は変えない。項目を足したら、`StickSideViewModelTests` が公開プロパティを全部たどって、左右の一致と片側の変更の独立を検査する（検査の書き足しは要らない）。
- 左右で挙動が違う項目は作らない。違いが要るときは、`StickSideViewModel` へ引数を足さず、先にユーザーへ相談する。
- 既定値・リセット・プリセットに入れる場合は、`BackingStore` のリセットと `ProfilePresets` も触る。
- L2・R2（`TriggerSettingsViewModel`／`TriggerSettingsPanel.xaml`）は、左右を横に並べた 1 枚の表のため、まだ左右で別の項目として持つ（`Phase33-Plan.md` の 33-5）。
