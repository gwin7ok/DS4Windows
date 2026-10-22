# クラス図 / インターフェース関連図
**（Class & Interface Specification）**

本ドキュメントは、巨大ファイルの解体、ステートレスパイプライン、パケット解析の分離、およびUIのMediator化を反映した、DS4Windows-Vader4Pro のインターフェース構成とクラス関連性を定義する。

---

## 1. 主要クラス・インターフェース関連図

```mermaid
classDiagram
    %% ==========================================
    %% 1. 入力監視層 (Ingress: 通信と解析の分離)
    %% ==========================================
    class IDs4DeviceRegistry {
        <<interface>>
        +getDevices() IEnumerable~IInputDevice~
        +findDeviceByMac(string mac) IInputDevice
        +startScan()
        +stopScan()
    }
    class IDeviceHotplugMonitor {
        <<interface>>
        +StartMonitoring()
        +StopMonitoring()
        +DeviceArrived event
        +DeviceRemoved event
    }
    class IHidTransport {
        <<interface>>
        +ReadReport(byte[] buffer) int
        +WriteReport(byte[] buffer) bool
        +IsConnected bool
    }
    class IInputReportParser {
        <<interface>>
        +ParseInputReport(byte[] rawReport, ref RawInputState outState) bool
    }
    class IOutputReportEncoder {
        <<interface>>
        +BuildOutputReport(FeedbackState feedback, byte[] outBuffer) int
    }

    class Ds4DeviceRegistryAdapter {
        -List~IInputDevice~ _activeDevices
    }
    class DS4ReportParser {
        +ParseInputReport(byte[] rawReport, ref RawInputState outState) bool
    }
    class Vader4ProReportParser {
        +ParseInputReport(byte[] rawReport, ref RawInputState outState) bool
    }

    IDs4DeviceRegistry <|.. Ds4DeviceRegistryAdapter : implements
    IInputReportParser <|.. DS4ReportParser : implements
    IInputReportParser <|.. Vader4ProReportParser : implements
    Ds4DeviceRegistryAdapter --> IDeviceHotplugMonitor : uses
    Ds4DeviceRegistryAdapter --> IHidTransport : uses

    %% ==========================================
    %% 2. 信号変換層 (Mapping Pipeline & Context)
    %% ==========================================
    class MappingPipelineContext {
        +int SlotIndex
        +RawInputState CurrentRaw
        +SlotHistoryState History
        +TransformedOutput Output
        +ResetForTick()
    }

    class IInputMappingPipeline {
        <<interface>>
        +ProcessPipeline(MappingPipelineContext ctx, ProfileEntity profile)
    }
    class IButtonProcessor {
        <<interface>>
        +ProcessButtons(MappingPipelineContext ctx, ButtonMappingConfig cfg)
    }
    class IStickProcessor {
        <<interface>>
        +ProcessStick(MappingPipelineContext ctx, StickConfig cfg)
    }
    class ITriggerProcessor {
        <<interface>>
        +ProcessTriggers(MappingPipelineContext ctx, TriggerConfig cfg)
    }
    class ITouchGyroProcessor {
        <<interface>>
        +ProcessTouchAndGyro(MappingPipelineContext ctx, MotionConfig cfg)
    }
    class IMouseEngine {
        <<interface>>
        +ProcessMousePointer(MappingPipelineContext ctx, MouseConfig cfg)
    }

    class InputMappingPipeline {
        -IButtonProcessor _btnProc
        -IStickProcessor _stickProc
        -ITriggerProcessor _trigProc
        -ITouchGyroProcessor _touchGyroProc
        -IMouseEngine _mouseEngine
        +ProcessPipeline(MappingPipelineContext ctx, ProfileEntity profile)
    }
    IInputMappingPipeline <|.. InputMappingPipeline : implements
    InputMappingPipeline o-- IButtonProcessor
    InputMappingPipeline o-- IStickProcessor
    InputMappingPipeline o-- ITriggerProcessor
    InputMappingPipeline o-- ITouchGyroProcessor
    InputMappingPipeline o-- IMouseEngine
    InputMappingPipeline ..> MappingPipelineContext : mutates

    class ControlService {
        -IDs4DeviceRegistry _deviceRegistry
        -IInputLoopCoordinator _loopCoordinator
        -IInputMappingPipeline _pipeline
        -IOutputSlotService _outputSlotService
        -IVirtualKBM _virtualKBM
        -IVirtualKBMLifecycle _kbmLifecycle
        -IProfileSlotApplier _profileSlotApplier
        -IProfileSettingsService _profileSettings
        -IAppSettingsService _appSettings
        -IEnvironmentService _environmentService
        -MappingPipelineContext[] _contextPool
        +Start()
        +Stop()
    }
    ControlService --> IDs4DeviceRegistry : uses
    ControlService --> IInputMappingPipeline : uses
    ControlService o-- MappingPipelineContext : pools per slot

    %% ==========================================
    %% 3. 信号出力層 (Egress)
    %% ==========================================
    class IOutputSlotService {
        <<interface>>
        +AttachDevice(int slot, OutContType type) bool
        +DetachDevice(int slot) bool
        +GetOutDevice(int slot) OutputDevice
        +SyncSlotPersist()
    }
    class IVirtualKBM {
        <<interface>>
        +PerformKeyPress(uint key, bool useScan)
        +PerformKeyRelease(uint key, bool useScan)
        +MoveMouse(int dx, int dy)
    }
    class IVirtualKBMLifecycle {
        <<interface>>
        +ReleaseHandler()
        +DetermineHandler(string identifier)
        +SwitchToFallbackHandler()
        +ApplyFakerInputVersion()
        +InitializeMapping(string identifier)
    }
    class IProfileSlotApplier {
        <<interface>>
        +ApplyToSlot(int slotIndex, string profileName, ProfileChangeSource source) bool
    }
    class ProfileSlotApplier
    IProfileSlotApplier <|.. ProfileSlotApplier : implements
    class OutputKBMHandlerAdapter {
        +VirtualKBMBase Handler
        +PerformKeyPress(uint key)
        +Sync()
    }
    class OutputKBMHandlerLifecycle {
        -IEnvironmentService _environment
        -OutputKBMHandlerAdapter _handlerOwner
        -IProfileSettingsService _profileSettings
    }
    IVirtualKBM <|.. OutputKBMHandlerAdapter : implements and owns the handler
    IVirtualKBMLifecycle <|.. OutputKBMHandlerLifecycle : implements
    OutputKBMHandlerLifecycle --> IEnvironmentService : reads FakerInput version
    OutputKBMHandlerLifecycle --> OutputKBMHandlerAdapter : sets Handler
    OutputKBMHandlerLifecycle --> IProfileSettingsService : sets OutputKBMMapping
    class IMapping {
        <<interface>>
        +SetCurveAndDeadzone(int device, DS4State cState, DS4State dState, IProfileSettingsService settings) DS4State
        +MapCustom(int device, DS4State cState, DS4State mapped, DS4StateExposed eState, Mouse tp, ControlService ctrl)
        +TempMouseJoystick(int device, DS4State mapped)
        +Commit(int device, IProfileSettingsService settings)
        +GetBoolMappingExternal(int device, DS4Controls control, DS4State cState, DS4StateExposed eState, Mouse tp) bool
        +HandleDeviceDisconnect(int device)
        +CaptureProfileUntrigger(int device) ProfileUntriggerSnapshot
        +RestoreProfileUntrigger(int device, ProfileUntriggerSnapshot snapshot)
        +PlayMacroDirect(int device, SpecialAction action)
        +EndMacroDirect(int device)
    }
    class Mapping {
        -SyntheticInputState _syntheticInput
        -MacroRuntimeState _macroRuntime
        -StickMotionState _stickMotion
    }
    class IKeyButtonControllerRegistry {
        <<interface>>
        +GetOrCreate(int device, Mode mode, string actionName) KeyButtonActionController
        +GetOrCreate(int device, SpecialAction sa) KeyButtonActionController
        +GetOrCreateForAction(int device, SpecialAction sa) IActionController
        +ClearForDevice(int device)
    }
    class KeyButtonControllerRegistry
    class ISyntheticDispatcher {
        <<interface>>
        +ResolveNativeKey(ushort kvpKey) uint
        +SendPress(int device, ushort kvpKey, uint nativeKey, bool useScanCode, IVirtualKBM handler)
        +SendRelease(int device, ushort kvpKey, uint nativeKey, bool useScanCode, IVirtualKBM handler)
        +ResetKeyTiming(int device, ushort kvpKey)
    }
    class SyntheticDispatcherService
    class SyntheticInputState
    class MacroRuntimeState
    class StickMotionState
    %% Phase9 9-A（2026-10-22）: Mapping から切り出した、状態を持たない補正処理（Singleton。値の特性化はゴールデンテストが守る）
    class IStickDeadZoneProcessor {
        <<interface>>
        +Process(StickDeadZoneInfo mod, byte rawX, byte rawY, ref byte x, ref byte y, ref byte outer)
    }
    class IOutputCurveProcessor {
        <<interface>>
        +ProcessSquareStick(double roundness, ref byte x, ref byte y)
        +ProcessStickCurve(int mode, DeadZoneType type, BezierCurve bezier, ref byte x, ref byte y)
        +ProcessTriggerCurve(int mode, BezierCurve bezier, ref byte value)
    }
    class ITriggerDeadZoneProcessor {
        <<interface>>
        +Process(TriggerDeadZoneZInfo mod, byte rawValue, ref byte value)
    }
    class IGyroControlsProcessor {
        <<interface>>
        +Correct(int accel, int deadZone, double maxZone, double antiDead, double sens) int
        +ApplyCurve(int mode, BezierCurve bezier, int outputAccel) int
    }
    class IStickInputConditioner {
        <<interface>>
        +ApplyDrift(int device, DS4State state, IProfileSettingsService settings) DS4State
        +AntiSnapback(Queue history, double delta, long timeoutMs, long timestamp, ref byte x, ref byte y)
        +Fuzz(byte[] last, int stickId, int delta, ref byte x, ref byte y)
    }
    class IFlickStickProcessor {
        <<interface>>
        +Process(FlickStickMappingData data, FlickStickSettings settings, double elapsedTime, byte stickX, byte stickY, byte prevStickX, byte prevStickY, ref double mouseDeltaX)
    }
    class IButtonMouseMovementCalculator {
        <<interface>>
        +Calculate(int device, DS4Controls control, int mnum, DS4State cState, DS4StateFieldMapping fieldMapping, ButtonMouseInfo info, int lsDeadZone, int rsDeadZone, DeltaSettingsProcessorGroup accel, double timeElapsed) double
    }
    class IMouseMotionQuantizer {
        <<interface>>
        +Quantize(int device, ref double rawMouseX, ref double rawMouseY, out int mouseX, out int mouseY)
    }
    class IGyroMouseStickBlender {
        <<interface>>
        +Blend(GyroMouseStickInfo msinfo, byte gyroX, byte gyroY, DS4State mappedState, PostMapStickData data)
    }
    class IControlActivityReader {
        <<interface>>
        +IsActive(IProfileSettingsService settings, int device, DS4Controls control, DS4State cState, DS4StateExposed eState, Mouse tp) bool
    }
    class IMacroStepExecutor {
        <<interface>>
        +Execute(int device, DS4KeyType keyType, int macroCodeValue, bool[] keydown) bool
    }
    class IMacroPlayback {
        <<interface>>
        +Play(int device, string macroStr, List macroLst, int[] macroArr, DS4KeyType keyType, SpecialAction action, ActionInstanceState state)
        +ClearDevice(int device)
    }
    class IOscService {
        <<interface>>
        +HasListener bool
        +HasSender bool
        +ChangeListenerStatus(bool state)
        +ChangeSenderStatus(bool state)
        +SendPlug(int index, bool plugged)
        +ResetDeviceState(int index)
        +HandleMessage(string address, IList arguments)
        +PreMappingStep(int ind, DS4State cState, DS4State tempMapState)
        +PostMappingStep(int ind, DS4State tempMapState)
    }
    class IOscTransportFactory {
        <<interface>>
        +CreateListener(int port, Action onMessage) IOscListenerHandle
        +CreateSender(string address, int port) IOscMessageSender
    }
    class IHidHideService {
        <<interface>>
        +CheckPresence(string ExePath, string ExeName, bool AddExe)
        +UpdateAttributes()
        +IsAffected(DS4Device dev) bool
        +MarkAffected(DS4Device dev)
    }
    class IHidHideApiFactory {
        <<interface>>
        +Open() IHidHideApi
    }
    class IVirtualPadService {
        <<interface>>
        +OutputSlotManager OutputSlotManager
        +OutputDevices OutputDevice[]
        +HasViGEmClient bool
        +StartViGEm()
        +StopViGEm()
        +PluginOutDev(int index, DS4Device device)
        +UnplugOutDev(int index, DS4Device device, bool immediate, bool force)
        +AttachUnboundOutDev(OutSlotDevice slotDevice, OutContType contType)
        +DetachUnboundOutDev(OutSlotDevice slotDevice)
        +EstablishOutFeedback(int index, OutContType contType, OutputDevice outDevice, DS4Device device)
        +RemoveOutFeedback(OutContType contType, OutputDevice outDevice, int inIdx)
        +QueueOnEventThread(Action action)
    }
    class IVirtualPadHost {
        <<interface>>
        +SetDevRumble(DS4Device device, byte heavyMotor, byte lightMotor, int deviceNum)
        +QueueOnEventThread(Action action)
    }
    class VirtualPadService
    class ILatencyMonitor {
        <<interface>>
        +OnReport(DS4Device device, int ind)
        +Reset(int ind)
    }
    class ITouchpadToggleService {
        <<interface>>
        +CheckForToggle(int deviceID, DS4State cState, DS4State pState)
        +StartTouchpadOff(int deviceID)
        +Slide(int ind, DS4State cState, Mouse touchPad, bool connected) string
    }
    class IActiveInputProbe {
        <<interface>>
        +GetActive(DS4State cState, Mouse tp, bool connected) DS4Controls
    }
    class IConnectionProfileSelector {
        <<interface>>
        +ApplyOnConnect(int index, bool useAutoProfile, bool isValidSerial, string macAddress) bool
    }
    class ConnectionProfileSelector
    class IDeviceConnectionSetup {
        <<interface>>
        +ApplyProfileOptions(int ind, DS4Device device, Mouse touchPad, bool startUp)
        +WireInitialHooks(int ind, DS4Device device)
        +AttachTouchpad(int ind, DS4Device device, Mouse[] touchPads)
    }
    class DeviceConnectionSetup
    class IInputDeviceModule {
        <<interface>>
        +Handles(InputDeviceType type) bool
        +IsEnabled(ControlServiceDeviceOptions options) bool
        +GetKnownExtraButtons(DS4Device device) List
        +OnDeviceInit(DS4Device device, ControlServiceDeviceOptions options)
        +HookSettings(DS4Device device, Func activeControllers)
        +ApplyProfileOptions(DS4Device device, int ind, IProfileSettingsService settings)
    }
    class IInputDeviceModuleRegistry {
        <<interface>>
        +Resolve(InputDeviceType type) IInputDeviceModule
    }
    class InputDeviceModuleRegistry
    class Ds4DeviceModule
    class DualSenseDeviceModule
    class SwitchProDeviceModule
    class JoyConDeviceModule
    class Ds3DeviceModule
    class LatencyMonitor
    class TouchpadToggleService
    class ActiveInputProbe
    class IUdpMotionService {
        <<interface>>
        +HasServer bool
        +IsChangingStatus bool
        +SlotCount int
        +ChangeStatus(bool state, bool openPort)
        +StartListeningIfCreated()
        +ChangeMotionEventStatus(bool state)
        +PrepareDeviceMotion(DS4Device device, int index)
        +ResetSmoothingFilters(int idx)
    }
    class IUdpMotionHost {
        <<interface>>
        +GetPadDetailForIdx(int padIdx, ref DualShockPadMeta meta)
        +GetCurrentState(int ind) DS4State
        +GetTempState(int ind) DS4State
        +QueueOnEventThread(Action action)
    }
    class IUdpMotionServerFactory {
        <<interface>>
        +Create(UdpPadDetailProvider getPadDetail) IUdpMotionServer
    }
    class UdpMotionService
    class UdpMotionServerFactory
    class HidHideService
    class HidHideApiFactory
    class OscService
    class SharpOscTransportFactory
    class MacroStepExecutor
    class MacroPlayback
    class ButtonMouseMovementCalculator
    class MouseMotionQuantizer
    class GyroMouseStickBlender
    class ControlActivityReader
    class StickDeadZoneProcessor
    class OutputCurveProcessor
    class TriggerDeadZoneProcessor
    class GyroControlsProcessor
    class StickInputConditioner
    class FlickStickProcessor
    IMapping <|.. Mapping : implements
    IKeyButtonControllerRegistry <|.. KeyButtonControllerRegistry : implements
    ISyntheticDispatcher <|.. SyntheticDispatcherService : implements
    Mapping --> SyntheticInputState : ctor injection
    Mapping --> MacroRuntimeState : ctor injection
    Mapping --> StickMotionState : ctor injection
    Mapping --> IProfileSettingsService
    Mapping --> IProfileApplicationService
    Mapping --> IVirtualKBM
    Mapping --> IKeyButtonControllerRegistry
    Mapping --> ISyntheticDispatcher
    IStickDeadZoneProcessor <|.. StickDeadZoneProcessor : implements
    IOutputCurveProcessor <|.. OutputCurveProcessor : implements
    ITriggerDeadZoneProcessor <|.. TriggerDeadZoneProcessor : implements
    IGyroControlsProcessor <|.. GyroControlsProcessor : implements
    IStickInputConditioner <|.. StickInputConditioner : implements
    IFlickStickProcessor <|.. FlickStickProcessor : implements
    IButtonMouseMovementCalculator <|.. ButtonMouseMovementCalculator : implements
    IMouseMotionQuantizer <|.. MouseMotionQuantizer : implements
    IGyroMouseStickBlender <|.. GyroMouseStickBlender : implements
    IControlActivityReader <|.. ControlActivityReader : implements
    IMacroStepExecutor <|.. MacroStepExecutor : implements
    IOscService <|.. OscService : implements
    IHidHideService <|.. HidHideService : implements
    IUdpMotionService <|.. UdpMotionService : implements
    IVirtualPadService <|.. VirtualPadService : implements
    ControlService ..|> IVirtualPadHost : implements（Phase9 9-E E-4）
    VirtualPadService --> IVirtualPadHost : Func による遅延取得（循環を避ける）
    VirtualPadService --> OutputSlotManager : 所有（旧: ControlService）
    OutputSlotService --> IVirtualPadService : 抜き差しの窓口（Phase9 9-E E-4。旧: ControlService への逆依存）
    ILatencyMonitor <|.. LatencyMonitor : implements
    ITouchpadToggleService <|.. TouchpadToggleService : implements
    IActiveInputProbe <|.. ActiveInputProbe : implements
    IConnectionProfileSelector <|.. ConnectionProfileSelector : implements
    IDeviceConnectionSetup <|.. DeviceConnectionSetup : implements
    IInputDeviceModuleRegistry <|.. InputDeviceModuleRegistry : implements
    IInputDeviceModule <|.. Ds4DeviceModule : implements（デバイス種類のカートリッジ）
    IInputDeviceModule <|.. DualSenseDeviceModule : implements
    IInputDeviceModule <|.. SwitchProDeviceModule : implements
    IInputDeviceModule <|.. JoyConDeviceModule : implements
    IInputDeviceModule <|.. Ds3DeviceModule : implements
    InputDeviceModuleRegistry --> IInputDeviceModule : 全部を受け取り、種類から引き当てる
    DeviceConnectionSetup --> IInputDeviceModuleRegistry : 種類ごとの設定の反映
    ControlService --> IInputDeviceModuleRegistry : ctor injection（Phase9 9-E E-6c。種類ごとの判定・初期化・追加ボタン）
    ControlService --> IDeviceConnectionSetup : ctor injection（Phase9 9-E E-6b。接続時の設定の反映とイベントの配線）
    ControlService --> IConnectionProfileSelector : ctor injection（Phase9 9-E E-6a。接続時のプロファイルの選択と適用）
    ControlService --> ILatencyMonitor : ctor injection（Phase9 9-E E-5。On_Report の遅延の監視）
    ControlService --> ITouchpadToggleService : ctor injection（Phase9 9-E E-5。タッチパッドの切り替え・スライド）
    ControlService --> IActiveInputProbe : ctor injection（Phase9 9-E E-5。押されている入力の探索）
    ControlService --> IVirtualPadService : ctor injection（PluginOutDev／UnplugOutDev／OutputslotMan／outputDevices は窓口）
    IUdpMotionServerFactory <|.. UdpMotionServerFactory : implements
    ControlService ..|> IUdpMotionHost : implements（Phase9 9-E E-3）
    UdpMotionService --> IUdpMotionHost : Func による遅延取得（循環を避ける）
    UdpMotionService --> IUdpMotionServerFactory : UdpServer（internal）の窓口
    ControlService --> IUdpMotionService : ctor injection（ChangeUDPStatus／ChangeMotionEventStatus は薄い委譲）
    IHidHideApiFactory <|.. HidHideApiFactory : implements
    HidHideService --> IHidHideApiFactory : ドライバー（HidHideAPIDevice）の窓口（Phase9 9-E E-2）
    ControlService --> IHidHideService : ctor injection（CheckHidHidePresence／UpdateHidHiddenAttributes は薄い委譲）
    IOscTransportFactory <|.. SharpOscTransportFactory : implements
    OscService --> IOscTransportFactory : SharpOSC の型は SharpOsc* の中に閉じる（Phase9 9-E E-1）
    ControlService --> IOscService : ctor injection（OSC 系の本体。ChangeOSC*Status は薄い委譲）
    IMacroPlayback <|.. MacroPlayback : implements
    Mapping --> IMacroPlayback : ctor injection (Phase9 9-C)
    MacroPlayback --> IMacroStepExecutor
    MacroPlayback --> MacroRuntimeState : TaskQueue（デバイスごと）
    MacroStepExecutor --> MacroRuntimeState : MacroControl／MacroCount（デバイスごと。Phase9 9-C Q2）
    Mapping --> IButtonMouseMovementCalculator : ctor injection (Phase9 9-B。加速の状態はデバイスごと)
    Mapping --> IMouseMotionQuantizer : ctor injection (端数はデバイスごと)
    Mapping --> IGyroMouseStickBlender : ctor injection
    Mapping --> IControlActivityReader : ctor injection
    Mapping --> IStickDeadZoneProcessor : ctor injection (Phase9 9-A)
    Mapping --> IOutputCurveProcessor : ctor injection
    Mapping --> ITriggerDeadZoneProcessor : ctor injection
    Mapping --> IGyroControlsProcessor : ctor injection
    Mapping --> IStickInputConditioner : ctor injection
    Mapping --> IFlickStickProcessor : ctor injection
    IStickInputConditioner ..> StickMotionState : 履歴・前回の値は呼び出し側が持ち、引数で渡す
    ProfileApplicationService --> IKeyButtonControllerRegistry : ClearForDevice
    SyntheticDispatcherService --> SyntheticInputState
    SyntheticDispatcherService --> OutputKBMHandlerAdapter : default output (Handler)
    SyntheticDispatcherService --> IProfileSettingsService : OutputKBMMapping
    ControlService --> IMapping : MappingService
    class IMacroPlayer {
        <<interface>>
        +PlayMacro(int slot, MacroStep[] steps)
        +StopMacro(int slot)
    }
    class ILightbarService {
        <<interface>>
        +ComputeColor(int slot, BatteryStatus battery, LightbarConfig cfg) DS4Color
    }
    InputMappingPipeline --> IOutputSlotService : outputs pad
    IMouseEngine --> IVirtualKBM : outputs mouse
    ControlService --> ILightbarService : updates LED
    ControlService --> IVirtualKBM : identify / Sync
    ControlService --> IVirtualKBMLifecycle : manages KBM handler lifecycle
    ControlService --> IProfileSlotApplier : applies connect-time profile

    %% ==========================================
    %% 4. UI層 (Sub-ViewModels Mediator構成)
    %% ==========================================
    class ProfileSettingsViewModel {
        +StickSettingsSubViewModel StickSubVM
        +TriggerSettingsSubViewModel TriggerSubVM
        +ButtonMappingSubViewModel ButtonSubVM
        +SpecialActionsSubViewModel ActionSubVM
        -IProfileRepository _profileRepo
        -IEnvironmentService _environmentService
        -IPathService _pathService
        +SaveProfile(string profileName) bool
        +LoadBlankProfile(ControlService control)
        +ApplyPreset(PresetOption opt)
    }
    class StickSettingsSubViewModel {
        -IProfileSettingsService _profileSettings
        +LoadSettings(StickConfig cfg)
        +ApplyTo(StickConfig cfg)
    }
    class TriggerSettingsSubViewModel {
        -IProfileSettingsService _profileSettings
        +LoadSettings(TriggerConfig cfg)
        +ApplyTo(TriggerConfig cfg)
    }
    class ButtonMappingSubViewModel {
        -IProfileSettingsService _profileSettings
        +LoadSettings(ButtonMappingConfig cfg)
        +ApplyTo(ButtonMappingConfig cfg)
    }
    class SpecialActionsSubViewModel {
        -ISpecialActionRepository _actionRepo
        -IProfileRepository _profileRepo
        +LoadActions(string profileName)
        +GetActionOrPlaceholder(string actionName) SpecialAction
        +CommitEnabledActionsToProfile()
    }

    class MainWindowsViewModel {
        -IEnvironmentService _environmentService
        -IPathService _pathService
        +CheckDrivers()
    }
    class SettingsViewModel {
        -IAppSettingsService _appSettings
        -IAppearanceSettingsService _appearanceSettings
        -IEnvironmentService _environmentService
        -IPathService _pathService
        -ILogSettingsApplier _logSettingsApplier
    }
    class TrayIconViewModel {
        -IAppearanceSettingsService _appearanceSettings
        -IProfileRepository _profileRepository
        -IPathService _pathService
        -Dispatcher uiDispatcher
    }
    class ILogSettingsApplier {
        <<interface>>
        +UpdateLogSettings()
        +UpdateNLogConfig(int maxArchiveFiles)
        +RestoreArchiveSetting()
    }
    class LogSettingsApplierProxy {
        +Attach(ILogSettingsApplier applier)
    }
    class LoggerHolder
    ILogSettingsApplier <|.. LogSettingsApplierProxy : implements
    ILogSettingsApplier <|.. LoggerHolder : implements
    LogSettingsApplierProxy --> LoggerHolder : Attach 後に転送
    SettingsViewModel --> ILogSettingsApplier : ログ設定の反映

    class IProfilePresetLoader {
        <<interface>>
        +LoadBlank(deviceIndex, launchProgram, control, xinputChange, postLoad)
        +LoadBlankDS4(...)
        +LoadDefaultGamepadGyro(...)
        +LoadDefaultDS4GamepadGyro(...)
        +LoadDefaultMixedControls(...)
        +LoadDefaultDS4MixedControls(...)
        +LoadDefaultMixedGyroMouse(...)
        +LoadDefaultDS4MixedGyroMouse(...)
        +LoadDefaultKBM(...)
        +LoadDefaultKBMGyroMouse(...)
    }
    class ProfilePresetLoader
    IProfilePresetLoader <|.. ProfilePresetLoader : implements（プリセットの読込。旧 Global.Load…Profile への委譲は Phase7-Step10d で Global ごと削除済み）
    class IEditorWindowFactory {
        <<interface>>
        +CreateBindingWindow(deviceNum, settings, targetDevice, expose)
        +CreateRecordBox(deviceNum, controlSettings, shift, targetDevice, showscan, repeatable)
        +CreateRecordBoxWindow(deviceNum, settings, targetDevice, repeatable)
        +CreatePresetOptionWindow()
        +CreateStickCalibrationWindow(stick, device, profileSettingsViewModel)
        +CreateSpecialActionEditor(deviceNum, profileList, targetDevice, specialAction)
        +CreateControlActionEditor(deviceNum, controlSettings, profileList, targetDevice)
    }
    class EditorWindowFactory {
        -IProfileSettingsService _profileSettings
        -IOutputSlotService _outputSlotService
        -IPathService _pathService
        -IViewModelFactory _viewModelFactory
        -IProfilePresetLoader _presetLoader
        -Func~ControlService~ _controlServiceAccessor
    }
    IEditorWindowFactory <|.. EditorWindowFactory : implements
    EditorWindowFactory --> IProfilePresetLoader : プリセットを PresetOptionWindow へ渡す

    ProfileSettingsViewModel *-- StickSettingsSubViewModel : mediates
    ProfileSettingsViewModel *-- TriggerSettingsSubViewModel : mediates
    ProfileSettingsViewModel *-- ButtonMappingSubViewModel : mediates
    ProfileSettingsViewModel *-- SpecialActionsSubViewModel : mediates

    class IViewModelFactory {
        <<interface>>
        +CreateProfileSettingsVM(string profileName, int targetDevice) ProfileSettingsViewModel
        +CreateRecordBoxViewModel(device, controlSettings, recordMacro, extraHold, targetDevice) RecordBoxViewModel
        +CreateSpecialActEditorViewModel(device, action) SpecialActEditorViewModel
        +CreateAutoProfilesViewModel(autoProfileHolder, profileList) AutoProfilesViewModel
        +CreateSpecialActionsListViewModel(device) SpecialActionsListViewModel
    }
    class ViewModelFactory {
        -IProfileSettingsService _profileSettings
        -IProfileRepository _profileRepo
        -ISpecialActionRepository _actionRepo
        -IOutputSlotService _outputSlotService
        -IEnvironmentService _environmentService
        -IPathService _pathService
        -IAppSettingsService _appSettings
        -Func~IAutoProfileService~ _autoProfileServiceAccessor
        -Func~ControlService~ _controlServiceAccessor
    }
    IViewModelFactory <|.. ViewModelFactory : implements
    class ISpecialActionRepository {
        <<interface>>
        +ActionList List~SpecialAction~
        +AddAction(action) bool
        +RemoveAction(actionName) bool「メモリ上のリストのみ」
        +SaveAction(name, controls, mode, details, edit, delayTime, extras)「Actions.xml へ保存・読み直し・状態リセット」
        +RemoveActionAndSave(actionName) bool「Actions.xml へ保存・読み直し」
        +GetActionOrPlaceholder(actionName) SpecialAction
    }
    ViewModelFactory ..> ProfileSettingsViewModel : creates

    %% ==========================================
    %% 横断基盤・永続化層 (Storage)
    %% ==========================================
    class IProfileXmlStore {
        <<interface>>
        +LoadProfileXml(int deviceIndex, bool launchProgram, ControlService control, ...) bool
        +SaveProfileXml(int deviceIndex, string profileName) bool
        +LoadAppSettingsXml() bool
        +SaveAppSettingsXml() bool
        +LoadControllerConfigsForDevice(DS4Device device) bool
        +SaveControllerConfigsForDevice(DS4Device device) bool
    }
    class IDeviceOptionRepository {
        <<interface>>
        +GetDeviceOptions~T~(string mac) T
        +SaveDeviceOptions~T~(string mac, T opts) bool
    }
    class XmlIoLock {
        -ReaderWriterLockSlim _lock
        +AcquireReadLock()
        +AcquireWriteLock()
    }
    class ProfileXmlStore {
        -XmlIoLock _lock
    }
    IProfileXmlStore <|.. ProfileXmlStore : implements
    ProfileXmlStore --> XmlIoLock : uses

    %% ==========================================
    %% 統一トリガーディスパッチ（Phase8・目標像。未実装。2026-10-19 追加［段階 A］）
    %% 設計: docs-forDIMG/MadeByAgent/Phase8-Step2-Design.md。Step7 の各 PR で実装済みになる。
    %% 撤去済み（Phase8-Step8 8c・8d）: IActionFactory／DefaultActionFactory／ActionFactory、Actions.Action／SpecialActionBase／*ActionAdapter、BindingMigrationOptions、UntriggerPolicy.Buttons。残る: ActionEntry（状態の入れ物）、ActionManager（静的。Phase9 で整理）
    %% IMapping は、Step7.5（Profile）で CaptureProfileUntrigger／RestoreProfileUntrigger を削除して 8 メンバーになり、ITriggerDispatchEngine を使う
    %% ==========================================
    class ITriggerInput {
        <<interface>>
        +Device int
        +IsActive(DS4Controls control) bool
        +WasActive(DS4Controls control) bool
        +Delay DelayClock
        +UtcNow DateTime
        +TickCount64 long
    }
    class TriggerInput
    class TriggerState {
        <<struct>>
        +IsEstablished bool
        +Gesture int
    }
    class IInputAction {
        <<interface>>
        +Name string
        +Triggers IReadOnlyList~DS4Controls~
        +Evaluate(ITriggerInput input) TriggerState
        +Reset()
    }
    class ComboTriggerInputAction
    class MultiTapInputAction
    class IOutputAction {
        <<interface>>
        +Id string
        +Execute(IOutputContext ctx)
        +Stop(IOutputContext ctx)
    }
    class IFrameOutputAction {
        <<interface>>
        +ApplyFrame(IOutputContext ctx, bool active, bool risingEdge)
    }
    class IHeldOutputAction {
        <<interface>>
        +WhileHeld(IOutputContext ctx)
    }
    class IOutputContext {
        <<interface>>
        +Device int
        +OutputHandler IVirtualKBM
        +Frame IFrameOutput
        +RequestEndFrame()
    }
    class IFrameOutput {
        <<interface>>
        +ResetTriggerControl(DS4Controls control)
        +PressX360(X360Controls control)
    }
    class IActionBinding {
        <<interface>>
        +Input IInputAction
        +Outputs IReadOnlyList~IOutputAction~
        +Policy BindingPolicy
        +Source SpecialAction
        +Index int
    }
    class TriggerBinding
    class BindingPolicy {
        +RespectsFreshPressGuard bool
        +BlocksWhileExecuting bool
        +Untrigger UntriggerPolicy
        +ResetsTriggerOutput bool
        +StopOnRelease bool
        +ExclusionGroup string
        +ErasesOutputEveryFrame bool
        +LogsRisingEdge bool
        +LogsReleaseEdge bool
    }
    class FreshPressGuardTable {
        <<static>>
        +ForSpecialAction(type, toggle) bool
        +ForControls(kind) bool
    }
    class IProfileIdentityService {
        <<interface>>
        +EnsureAllPersisted() IReadOnlyList~string~
        +Refresh()
        +GetId(string profileName) string
        +GetName(string profileId) string
        +EnsurePersisted(string profileName) string
        +TryResolve(string profileId, string nameHint, out string profileName) bool
    }
    class ProfileIdentityService
    class ProfileSlotNames {
        +Length int
        +this[int index] string
        +IdAt(int index) string
        +AttachIdentity(IProfileIdentityService identity)
    }
    class LinkedProfileMap {
        +this[string serial] string
        +AttachIdentity(IProfileIdentityService identity)
    }
    IProfileIdentityService <|.. ProfileIdentityService : implements
    ProfileSlotNames ..> IProfileIdentityService : name by ID
    LinkedProfileMap ..> ProfileSlotNames : value cells
    class IActionRegistry {
        <<interface>>
        +GetBindings(int device) IReadOnlyList~IActionBinding~
        +Replace(int device, IReadOnlyList~IActionBinding~ bindings)
        +Clear(int device)
    }
    class ActionBindingRegistry
    class IActionBindingBuilder {
        <<interface>>
        +Build(int device) IReadOnlyList~IActionBinding~
    }
    class ActionBindingBuilder
    class ITriggerDispatchEngine {
        <<interface>>
        +ProcessTriggers(int device, ITriggerInput input, IFrameOutput frame)
        +ApplyFrameOutputs(int device, ITriggerInput input, IFrameOutput frame)
        +IsSuppressed(int device, DS4Controls control) bool
        +OnProfileApplied(int device)
        +OnDeviceDisconnected(int device)
    }
    class TriggerDispatchEngine
    class ButtonOutputAction
    class KeyOutputAction
    class MacroAction
    class ProfileSwitchAction
    class LaunchProcessAction
    ITriggerInput <|.. TriggerInput : implements
    IInputAction <|.. ComboTriggerInputAction : implements
    IInputAction <|.. MultiTapInputAction : implements
    IActionBinding <|.. TriggerBinding : implements
    IActionRegistry <|.. ActionBindingRegistry : implements
    IActionBindingBuilder <|.. ActionBindingBuilder : implements
    ITriggerDispatchEngine <|.. TriggerDispatchEngine : implements
    IOutputAction <|-- IFrameOutputAction
    IOutputAction <|-- IHeldOutputAction
    IFrameOutputAction <|.. ButtonOutputAction : implements
    IOutputAction <|.. KeyOutputAction : implements
    IHeldOutputAction <|.. MacroAction : implements
    IOutputAction <|.. ProfileSwitchAction : implements
    IOutputAction <|.. LaunchProcessAction : implements
    TriggerBinding --> IInputAction : Input
    TriggerBinding --> IOutputAction : Outputs
    TriggerBinding --> BindingPolicy : Policy
    TriggerDispatchEngine --> IActionRegistry : GetBindings
    class IManagedActionManager {
        <<interface>>
        +GetStateFor(SpecialAction action, int device) ActionInstanceState
    }
    class IProfileApplicationService {
        <<interface>>
    }
    TriggerDispatchEngine --> IManagedActionManager : ActionInstanceState
    ActionBindingBuilder ..> TriggerBinding : builds
    class ControlBindingBuilder
    class AssignmentDefinition {
        +Id string
        +Origin AssignmentOrigin
        +Kind ActionTypeId
        +Triggers IReadOnlyList~DS4Controls~
        +HoldSeconds double
        +Item ControlSpecialItem
        +Key KeyBindingValues
        +Button ButtonBindingValues
        +Extras ExtrasBindingValues
        +Macro IReadOnlyList~int~
        +KeyType DS4KeyType
    }
    class AssignmentOrigin {
        <<enumeration>>
        Controls
        SpecialAction
    }
    class SpecialActionAssignmentReader {
        <<static>>
        +Read(SpecialAction sa, AssignmentOrigin origin) AssignmentDefinition
    }
    class ControlsAssignmentReader {
        <<static>>
        +TryReadSpecial(...) bool
        +TryReadKey(...) bool
        +TryReadMacro(...) bool
    }
    class SpecialActionItemMapper {
        <<static>>
        +ToItem(SpecialAction sa) ControlSpecialItem
        +ToSpecialAction(string name, triggers, ControlSpecialItem item) SpecialAction
    }
    class ISpecialActionOutputFactory {
        <<interface>>
        +Create(int device, AssignmentDefinition definition, SpecialAction source, int index) SpecialActionOutputs
    }
    class SpecialActionOutputFactory
    class AssignmentBindingBuilder {
        +Build(int device, AssignmentDefinition definition, SpecialAction source, int index) TriggerBinding
        +BuildExtras(int device, AssignmentDefinition definition, SpecialAction source, BindingPolicy mainPolicy) TriggerBinding
    }
    class ControlsSpecialTypeTable {
        <<static>>
        +IsSupported(ActionTypeId type) bool
    }
    class ControlOutputNatureTable {
        <<static>>
    }
    class FreshPressGuardTable {
        <<static>>
        +For(AssignmentOrigin origin, ActionTypeId type, bool toggle) bool
    }
    ISpecialActionOutputFactory <|.. SpecialActionOutputFactory : implements
    ActionBindingBuilder ..> SpecialActionAssignmentReader : SA → definition
    ControlBindingBuilder ..> ControlsAssignmentReader : Controls → definition
    SpecialActionAssignmentReader ..> SpecialActionItemMapper : typed item
    ControlsAssignmentReader ..> SpecialActionItemMapper : synthetic SA
    ActionBindingBuilder ..> AssignmentBindingBuilder : assembles bindings
    ControlBindingBuilder ..> AssignmentBindingBuilder : assembles bindings
    AssignmentBindingBuilder --> ISpecialActionOutputFactory : outputs + policy
    SpecialActionOutputFactory ..> FreshPressGuardTable : policy by kind x origin
    ControlBindingBuilder ..> ControlsSpecialTypeTable : supported kinds
    AssignmentBindingBuilder ..> ControlOutputNatureTable : input judgement
    AssignmentDefinition --> AssignmentOrigin : Origin
    IMapping ..> ITriggerDispatchEngine : calls every frame
    IProfileApplicationService ..> IActionBindingBuilder : after profile applied
```

---

## 2. 適用された主要アーキテクチャ・デザインパターン

1. **Pipeline & In-place Mutation パターン（信号変換層）：**
   * `IInputMappingPipeline` を起点に、各プロセッサが常駐バッファ `MappingPipelineContext` を順次更新（インプレースミューテーション）。参照型のアロケーションを完全に抑制し、Zero-GCホットパスを実現。
2. **Mediator パターン（UI層）：**
   * 親の `ProfileSettingsViewModel` が Mediator となり、分割された各サブViewModel（`StickSubVM`, `TriggerSubVM` 等）の設定値収集・反映・永続化呼び出しを一元管理。サブVM同士の結合度をゼロにする。
3. **Transport / Parser 分離パターン（入力監視層）：**
   * `IHidTransport`（OS依存の通信）と `IInputReportParser`（OS非依存のパケット解析）を切り離し、Vader 4 Pro や DS4 の生バイト列解析ロジックを実機なしで単体テスト可能にする。
4. **Repository & IoLock パターン（横断基盤層）：**
   * XMLファイルの入出力を `XmlIoLock` で完全にラップし、複数スレッドからの同時アクセスやプロファイル自動保存時のデッドロックを防止。

5. **送出とライフサイクルの分離（3-b KBM出力）：**
   * キー・マウスの送出（`IVirtualKBM`）と、出力ハンドラの生成・破棄・フォールバック切替・マッピング初期化（`IVirtualKBMLifecycle`）を別インターフェースに分ける。送出側（Actions・`IMouseEngine`）はライフサイクル操作を知らず、`ControlService` だけが両方を使う。

%% 注釈補強（2026-09-19 Phase6-Step2 決定O1=B／O4=B-2 反映）
%% - `IProfileSlotApplier`（`ProfileSlotApplier` が実装）を追加。`ControlService` は `IProfileApplicationService` に直接依存せず、自身が定義する出力ポートのみを知る（依存の逆転）。現状の実装は `Global.ApplyProfileToSlot` へ委譲し、`IProfileApplicationService` への委譲は K1 の是正後（フェーズG）に行う。

%% 注釈補強（2026-09-19 Phase6-Step2 実地確認・決定D1〜D3反映）
%% - `IVirtualKBMLifecycle`（`OutputKBMHandlerLifecycle` が実装）を追加（決定D3）。現行の `OutputKBMHandlerAdapter` は `Global.outputKBMHandler` への読み取り専用委譲であり、ハンドラの生成・差し替えの口を持たないため。図中の `IVirtualKBM` のメソッドは概念的な代表例で、実インターフェース（`DS4Windows.Services.IVirtualKBM`）とは一致しない。
%% - `ControlService` のフィールドは Step2 の主な依存のみ記載（全体は Phase6-Step2-Plan.md §3.1）。過渡期は `IOutputSlotService` を `Func<IOutputSlotService>` で遅延解決する（決定D1、04-Service-Lifecycle-Spec.md §4）。目標構造（直接注入）は変更しない。

%% 注釈補強（2026-09-18監査結果反映、構造変更なし）
%% - 各巨大ファイル（ScpUtil/Mapping/Mouse/ControlService/ProfileSettingsVM/DS4Device）の解体先（1クラス1責任）は現状監査（762参照、10ファイル網羅、重複ゼロ・漏れゼロ証明済み）と完全一致。
%% - ScpUtil.cs の255件は「参照」ではなく「静的宣言」として区別（§2抽出問題記録済み）。横断基盤層（XmlIoLock, PathService等）へのマッピングは理想構造と一致。
%% - ProfileSettingsViewModel の Mediator 構成（親VM + SubVM群）は現状の ProfileEditor（96件、SpecialAction契約差含む）と一致。契約差（GetAction/LoadActions正規化差、Save/Remove操作境界差）はb要設計として記録済み（ABC-Classification.md 参照）。
%% - Pipeline & In-place Mutation パターン（Zero-GC）は現状のホットパス（ControlService入力ループ、Mappingのreadonly配列含む69件）と一致。readonly配列はc除外として維持（自動除外しない：報告書§5維持）。
%% - Transport/Parser 分離（IHidTransport + IInputReportParser）は DS4Device（87件）の解体と一致。パケット解析の単体テスト担保は理想構造の責務として維持。
%% - Phase5証跡（Step14/15未完了）の前提条件を維持（証跡存在確認済み、内容検証別途必要）。
%% - ガードレール（10項目）の未適用項目はStep2以降の実装時に適用（Phase6-Status.md 参照）。

%% 注釈補強（2026-09-21 Phase6-Step3 実地突き合わせ・論点3決定反映）
%% - `IProfileXmlStore` のメンバ一覧を実装済みの現行シグネチャに合わせて更新（`LoadProfileXml`/`SaveProfileXml` は `(path, ProfileDTO)` ではなく `deviceIndex` 主体、`LoadAppSettingsXml`/`SaveAppSettingsXml`/`LoadControllerConfigsForDevice` を追加）。あわせて `SaveControllerConfigsForDevice`（Step3 で新設予定）を追加。
%% - `IDisplayCoordinateService`（`DisplayCoordinateService` が実装）を新設。`Mapping.cs` の `absUseAllMonitors`／`TranslateCoorToAbsDisplay`（Step1エビデンス §2 分類c）の移行先。既存 `IEnvironmentService.PrepareAbsMonitorBounds` は同じ画面座標状態（`absDisplayBounds`/`fullDesktopBounds`/`absUseAllMonitors`）を更新する処理のため本サービスへ集約し、`IEnvironmentService` 側は薄い委譲として残す（Step3-1 で実装時に確定）。

%% 注釈補強（2026-09-23 Abs Mouse機能削除・ユーザー承認済み、copilot-instructions.md §2.2 例外規定）
%% - 上記の `IDisplayCoordinateService`／`DisplayCoordinateService` は、その唯一の利用者であったボタン割当のAbs Mouse機能（`Mapping.cs` の `MapCustom`／`GetAbsMouseMapping`）およびタッチパッドのAbsolute Mouseモード（`TouchpadAbsMouseSettings`）が、使用頻度が極めて低いとの判断により削除されたことに伴い、丸ごと撤去した。`IEnvironmentService` 側への影響はない（もともと薄い委譲のみ）。
%% - あわせて `IProfileSettingsService.ButtonAbsMouseInfos`／`TouchAbsMouse`、`IVirtualKBM.MoveAbsoluteMouse`（本図には個別メンバーとして未記載だったため図の変更なし）も削除済み。
%% 注釈補強（2026-09-25 Phase6-Step7-4 決定7＝案H 反映）
%% - Composition Root の補助として `IStartupArguments`（`StartupArguments` が実装）を新設した。アプリの起動引数（`ArgumentParser`）を保持するだけのサービスで、`AppHost.CreateHost(config, parser)` が値を設定し、`ServiceRegistration` が `ControlService` を生成するときに取り出して渡す。`ControlService` のクラス定義（依存）には変更がないため、図本体には追加していない。詳細は `04-Service-Lifecycle-Spec.md` の 2026-09-25 の注釈と `Phase6-Step7-Plan.md` 決定7。
%% 注釈補強（2026-09-26 Phase6-Step7b 決定3＝案M1 反映）
%% - `IViewModelFactory.CreateProfileSettingsVM` の理想形のシグネチャに `int targetDevice` を追加した。プロファイル編集画面では、編集対象（プロファイル。編集内容は作業領域に保持し、保存・適用を押すまで実機へ反映しない）と、ランブルテスト・ライトバーのプレビュー・校正などで使う実機（`targetDevice`。-1 は実機なし）を分けて渡す。現行コードは `CreateProfileSettingsViewModel(int device, int targetDevice = -1)`（`device` は作業スロット `Global.TEST_PROFILE_INDEX`）で、`CreateRecordBoxViewModel` にも同じ `targetDevice` を追加した。
%% - Step8 以降の MVVM 移設でも、編集スロットと実機を同じ番号で兼用しないこと（兼用すると、編集中の変更が保存前にコントローラーへ即時反映される。Phase6-Status.md K4-3）。詳細は `Phase6-Step7b-Plan.md` §2A 決定3、`04-Service-Lifecycle-Spec.md` §1-3。
%% 注釈補強（2026-10-13 Phase6-Step8 反映）
%% - `ProfileSettingsViewModel` に、編集スロットに対するバックエンド操作（`SaveProfile(profileName)`、`LoadBlankProfile(control)`、キャッシュ更新）を持たせた。保存はコントローラーのスロットへの再適用をしない（再適用は保存の通知［`ProfileSaved`］を受けた `MainWindow` の責務。Step7b 決定8）。`SpecialActionsSubViewModel` には、有効なアクション名を編集スロットの ProfileActions へ反映する `CommitEnabledActionsToProfile()`、名前からの取得（未発見は placeholder）の `GetActionOrPlaceholder`、依存の `IProfileRepository` を追加した。クラス名は理想形の名称で、現行コードでは `SpecialActionsListViewModel` が相当する。
%% - `IProfileRepository` に `LoadBlankProfile`（空のプロファイルの読み込み。既定アクション作成・一時プロファイルのクリアを含む）、`ISpecialActionRepository` に `GetActionOrPlaceholder`（前後の空白と大文字小文字を無視し、未発見は `null` ではなく placeholder を返す）を追加した。どちらも現行は `Global` への薄い委譲。本図にはこれらのインターフェースのメンバー一覧がないため、一覧の変更はない。
%% - View（`ProfileEditor`）は、レイアウト・言語の設定（`IAppSettingsService`）と、編集スロットの LS／RS 補正値・出力種別（`IProfileSettingsService`）をコンストラクタ引数で受け取る（Pure DI）。生成は `MainWindow.ShowProfileEditor` の 1 箇所。詳細は `Phase6-Step8-Plan.md` §0A、`Phase6-Step8-Completion-Report.md`。
%% 注釈補強（2026-10-14 Phase6-Step9 反映）
%% - 主要 4 大 ViewModel（`SettingsViewModel`、`MainWindowsViewModel`、`TrayIconViewModel`、`ProfileSettingsViewModel`）の依存を、コンストラクタの必須引数（Pure DI）にした。`AppHost.GetService`／`Global.*Instance`／`Program.rootHub` のフォールバックは削除。図に `MainWindowsViewModel`・`SettingsViewModel`・`TrayIconViewModel` のクラスを追加し、`ProfileSettingsViewModel` に `IEnvironmentService`・`IPathService` を追加した。
%% - `ViewModelFactory` の依存を、実コードに合わせて明示した（`IServiceProvider` の保持ではなく、必須のコンストラクタ引数）。`ControlService` は `Func<ControlService>` で生成のたびに遅延解決する（Step2 決定D1 と同じ形）。`IViewModelFactory` の契約（`CreateProfileSettingsViewModel(int device, int targetDevice = -1)`）は変更なし。
%% - 追加した契約: `IAppSettingsService`（`UseCustomSteamFolder`、`CustomSteamFolder`、`FakeExeName`、`LogMaxArchiveFiles`）、`IEnvironmentService`（`IsViGEmBusInstalled`、`IsRunningSupportedViGEmBus`、`IsUsingMinViGEm117333`、`AdminNeeded`）、`IPathService.ProgramDirectory`（`Global.exedirpath` と同じ値。`ExecutableDirectory`＝`AppContext.BaseDirectory` とは値が異なりうる）、`IAppearanceSettingsService.BatteryChanged`（購読。発行の `InvokeBatteryChanged` と対）、`IProfileSettingsService.RefreshActionAlias`。いずれも `Global` への薄い委譲。
%% - 詳細は `Phase6-Step9-Plan.md` §0A、`Phase6-Step9-Completion-Report.md`。
%% 注釈補強（2026-10-14 Phase6-Step10-1 反映）
%% - グループ1（起動・環境系）の View／ViewModel（`SaveWhere`、`WelcomeDialog`、`LanguageSelectDialog`、`About`、`UpdaterWindow`、`LanguagePackControl`、`LogViewModel`、`UpdaterWindowViewModel`、`LanguagePackViewModel`、`SettingsViewModel`、`TrayIconViewModel`）の依存を、コンストラクタの必須引数（Pure DI）にした。`LanguagePackControl` は XAML から生成されるため、引数なしコンストラクタを残し `Initialize(IAppSettingsService)` で受け取る。
%% - 追加した契約: `IEnvironmentService`（`IsWin10OrGreater`、`IsWin8OrGreater`、`IsFakerInputInstalled`、`IsHidHideInstalled`［その場で確認。キャッシュ値の `HidHideInstalled` とは別物］、`RefreshViGEmBusInfo`）、`IPathService`（`SetSaveLocation`＝`Global.SaveWhere`［`AppDataPath` の setter とは別物］、`CreateEmptyProfilesFile`＝`Global.SaveDefault`）、`IAppSettingsService`（`LastVersionChecked`、`SetCulture`）、`ILogSettingsApplier`（`LoggerHolder` が実装。DI へは遅延接続の `LogSettingsApplierProxy` を登録し、`App` が `LoggerHolder` の生成直後に `Attach` する）。いずれも `Global` への薄い委譲。
%% - `StartupMethods`（静的クラス）は静的のまま、プログラムフォルダと実行ファイルの場所を引数で受け取る形にした。`TrayIconViewModel` は UI スレッドの `Dispatcher` をコンストラクタの引数で受け取る。
%% - 詳細は `Phase6-Step10-1-Plan.md`。
%% 注釈補強（2026-10-14 Phase6-Step10-2 反映）
%% - グループ2（プロファイル・マッピング系）の View／ViewModel（`ControllersViewModel`、`ControllerListViewModel`、`ControllerRegDeviceOptsViewModel`、`ControllerRegisterOptionsWindow`、`AutoProfiles`、`AutoProfilesViewModel`、`DupBox`、`RenameProfileWindow`、`RenameProfileViewModel`、`BindingWindow`、`BindingWindowViewModel`、`RecordBox`、`RecordBoxViewModel`、`StickCalibrationWindow`、`MappingListViewModel`、`TouchButtonUserControl(ViewModel)`、`PresetOption` 一式、`ControllerReadingsControl`）の依存を、コンストラクタの必須引数（XAML から生成される `AutoProfiles`／`DupBox`／`ControllerReadingsControl` は `Initialize`）にした。`AppHost.GetService`／`Global.*Instance`／`Program.rootHub` のフォールバックは削除。
%% - 追加した契約: `IProfilePresetLoader`（`PresetOption` が使うプリセットの読み込み 10 メソッド。`Global.Load…Profile` への薄い委譲）、`IEditorWindowFactory`（`ProfileEditor`／`BindingWindow`／`RecordBoxWindow`／`SpecialActionEditor` が補助ウィンドウを生成する窓口。実装 `EditorWindowFactory` が必要なサービスを必須引数で保持し、`ControlService` は生成のたびに `Func` で遅延解決する）。`IAppSettingsService`（`UseMoonlight`、`UseAdvancedMoonlight`）、`IAutoProfileService`（`CreateAutoProfilesFile`）。`ViewModelFactory` に `IAppSettingsService` と `Func<IAutoProfileService>` を追加した。
%% - 温存: `RESOURCES_PREFIX`／`ASSEMBLY_RESOURCE_PREFIX`（定数）、`ds4inputNames`／`xboxDefaultNames`／`ds4DefaultNames`／`macroDS4Values`（不変の静的表）、`getX360ControlString`（純粋関数）。
%% - 付随の機能廃止: Flydigi Vader 4 Pro のデバイスサポートを削除した（ユーザー承認。`Phase6-Step10-V4P-Removal-Plan.md`）。
%% - 詳細は `Phase6-Step10-2-Plan.md`。
%% 注釈補強（2026-10-14 Phase6-Step10-3 反映）
%% - グループ3（スペシャルアクション系）の View／ViewModel（`SpecialActionEditor`、`SpecialActEditorViewModel`、`SpecialActionsListViewModel`、`SpecialActions/*` の種別ごとの ViewModel 7 クラス）の依存を、コンストラクタの必須引数（Pure DI）にした。`AppHost.GetService`／`Global.*Instance`／`Program.rootHub` のフォールバックは削除。
%% - 追加した契約: `ISpecialActionRepository.SaveAction`（`Global.SaveAction` への薄い委譲。Actions.xml への保存、読み直しと検証、キー種別のランタイム状態のリセットを含む）、`RemoveActionAndSave`（`Global.RemoveAction` への薄い委譲。ファイルへの保存と読み直しを含む）。メモリ上のリストだけを変える `AddAction`／`RemoveAction` とは動作が別のため、別メソッドにした。`IViewModelFactory.CreateSpecialActionsListViewModel`。`EditorWindowFactory` に `ISpecialActionRepository` を追加した。
%% - 削除: `SpecialActionsListViewModel` の未使用の依存（`IManagedActionManager`、`IOutputSlotService`、`IProfileSettingsService`）。
%% - 温存: `Global.configFileDecimalCulture`（不変の静的値）、`Global.getX360ControlString`（純粋関数）。
%% - 先送り: SA の「キー押下」でコントローラーのボタンを割り当てられない件（`Phase6-Step10-3-Plan.md` §12。Step10-3c で調査）。
%% - 詳細は `Phase6-Step10-3-Plan.md`。
%% 注釈補強（2026-10-15 Phase6-Step10-4 反映）
%% - 削除した型: `IProfileActionChainService`／`ProfileActionChainService`、`IMappingActionDispatcher`／`MappingActionDispatcher`（`InputMappingPipeline` の `_dispatcher` を図から除去）、`AutoProfileChecker`。
%% - コンストラクターの変更（依存は全て必須。null は `ArgumentNullException`）:
%%   - `ProfileApplicationService(IProfileSettingsService, IDeviceStateService, Func<ControlService>, IProfileRepository, IEnvironmentService)`（スロット上限は `ControllerSlotLimit`。K1 の是正）。
%%   - `ProfileRepository(IProfileSettingsService, IProfileXmlStore, IPathService, Func<ControlService>)`。
%%   - `AutoProfileService(AutoProfileHolder, IProfileApplicationService, IProfileSettingsService, IProcessInspector, IEnvironmentService, IAppSettingsService, IProfileRepository, Func<ControlService>)`。
%%   - `OutputSlotService(Func<OutputSlotManager>, IOutputSlotStore, Func<ControlService>)`、`OutputSlotStore(IPathService)`、`OutputSlotManager(Func<Version>)`。
%%   - `DefaultActionFactory(IMacroPlayer, IProcessLauncher, IProfileSwitcher)`、`DefaultActionManager(IActionFactory)`、`DefaultMacroPlayer(IVirtualKBM)`。
%%   - `SpecialActionRepository(BackingStore, IPathService)`、`ProfileSettingsService(BackingStore, IEnvironmentService, Func<ControlService>)`、`ProfileActionProvider(BackingStore)`、`AutoProfileHolder(IPathService)`、`ProfileList(IPathService)`、`ProfileEntity(IPathService)`。
%% - 追加した契約: `IEnvironmentService.ViGEmBusVersion`、`IProfileRepository.ReplaceLinkedProfileName`、`AutoProfileHolder.ReplaceProfileName`。`WindowPlacementHelper.ApplyPlacement` は `IAppSettingsService` を引数に取る。
%% - 温存: `Global.ApplyProfile`（`ProfileApplicationService` の実装本体。Phase7）、静的 `ActionFactory`／`ActionManager` の `ServiceProviderHolder`（D4-4。Phase7／Phase8）。
%% - 詳細は `Phase6-Step10-4-Plan.md`。
%%
%% Phase7-Step2（2026-10-17）: DI インターフェースの、本体の呼び出しが 0 件だったメンバー（削除 51 メンバー）を整理した。詳細は `Phase7-Step2-Plan.md` §2・§8。
%% - `IProfileSettingsService`: 色・配列の重複アクセサ、`ProfileSettingChanged`／`Wire/UnwireSubSettingsEvents`、`ResetToDefaults` ほかを削除。
%% - `IDeviceStateService`: デバイス一覧・接続状態・電池残量（常に空の孤立実装）を削除。初回接続判定とシリアル変更通知だけを持つ。`ControllersViewModel` を削除。
%% - `IOutputSlotService`: `OutputDevices`／`GetOutputDevice`／`IsSlotPlugin` を削除。`IProfileRepository`、`ISpecialActionRepository`（メモリ上だけの `AddAction`／`ReplaceAction`／`RemoveAction`）、`IDs4DeviceRegistry`、`IVirtualKBM`、`IUdpServerService`［Phase9 9-E E-3 で、契約ごと削除。後継は `IUdpMotionService`］、`INotificationService`、`IPathService` の孤立・重複メンバーを削除。
%% - 「使うべきだが未接続」として TODO 付きで残した契約: `IEnvironmentService.UsingMaxControllers`、`IDs4DeviceRegistry.ReEnableDevice`、`IOutputSlotService.LoadOutputSlots`／`SaveOutputSlots`、`OutputSlotChanged`（温存）。
%%
%% Phase7-Step3（2026-10-17）: `BackingStore` が DI のシングルトンになり、サービスの構成が変わった（クラス間の関係線は変わらない。依存が `Global` 経由から `BackingStore` の直接注入に変わった）。
%%   - 追加された注入: `ProfileXmlStore(BackingStore)`、`ProfileRepository(BackingStore, IProfileSettingsService, IProfileXmlStore, IPathService, Func<ControlService>)`、`AppSettingsService(BackingStore, IProfileXmlStore, IPathService)`、`AppearanceSettingsService(BackingStore)`、`DeviceStateService(BackingStore)`、`AppNotificationService(BackingStore)`、`AutoProfileService(…, Func<ControlService>, BackingStore)`。
%%   - `IAppSettingsService` の `UDPServerSmoothingMincutoffChanged`／`BetaChanged` の発火元は、`AppSettingsService` のインスタンスになった（`Global` の静的イベントを削除）。
%%   - `Global` から削除した転送メンバー 76 名の一覧は `Phase7Step3RemovedGlobalForwardersGuardTests` を参照。
%%
%% Phase7-Step3c（2026-10-17）: プロファイル適用の構成が変わった（クラス間の関係線の変更は下記のとおり）。
%%   - `ProfileSlotApplier` → `IProfileApplicationService`（新しい依存）。`ProfileApplicationService` → `IProfileXmlStore`／`IProfileActionProvider`／`BackingStore`（新しい依存）。
%%   - `IProfileApplicationService` に `ApplyProfileWithoutHalt` を追加。`IProfileRepository` から `LoadBlankProfile` を削除。`ProfileSettingsViewModel`／`ViewModelFactory` が `IProfilePresetLoader` を受け取る。
%%   - `Global` から、プロファイル適用・プリセット・`SelectedProfileChanged` ほか 24 名を削除した（一覧は `Phase7Step3RemovedGlobalForwardersGuardTests`）。
%%
%% Phase7-Step4（2026-10-17）: 環境・保存場所の構成が変わった。
%%   - `EnvironmentService` が ViGEmBus／HidHide／FakerInput の調べ物（`FindViGEmDeviceInfo`／`CheckForSysDevice` ほか）と導入状態の static 状態を持つ（`Global` の 27 名を削除）。`VirtualKBMFactory` は `EnvironmentService.FakerInputInstalledState` を読む。
%%   - `PathService` が実行ファイルの情報、保存場所（`AppDataPathState`）、初回起動・保存場所が 2 か所の判定、`FindConfigLocation`／`SaveWhere`／空のプロファイル一覧の作成を持つ。`PathService(string appDataPath = null, BackingStore config = null)`（`SaveWhere` が `BackingStore` のファイルパスを更新するため）。`Global` の `appdatapath`／`exedirpath`／`exelocation`／`exeversion` は、`PathService` の状態を指す窓口として残る。
%%   - `AppSettingsService.RunHotPlug` はインスタンスの状態、`FirstRun` は `PathService` の状態。`IDs4DeviceRegistry.ReEnableDevice` は `App`（`-reenableDevice` 分岐）が呼ぶ。`AppHost` は `CreateHost(string[])` を削除し、`CreateHost(config, parser, pathService, environmentService)` でブートストラップのインスタンスを登録する。
%%
%% Phase7-Step5（2026-10-17）: 画面の生成と依存の受け渡しの構成が変わった。
%%   - 新設: `IMainWindowFactory` ← `MainWindowFactory`（`MainWindow` を生成。17 サービスと `Func<ControlService>`／`Func<IAutoProfileService>` を保持）。`IEditorWindowFactory` に `CreateProfileEditor(int)` を追加（`EditorWindowFactory` が `IAppSettingsService`／`IProfileRepository`／`IProfileSwitcher` を保持）。
%%   - `MainWindow`（コンストラクター引数 18）、`ProfileEditor`（コンストラクター引数 8）は `AppHost` を参照しない。`ControlService` は `IElevatedProcessLauncher` を受ける。`BackingStore.ProcessInspector`（`IProcessInspector`）はプロパティ注入。`AppLogger.NotificationService`（`INotificationService`）は静的プロパティ。
%%   - 削除: `Program`（`Program.rootHub`）、`Global.ProfileXmlStoreInstance`／`NotificationServiceInstance`／`SaveControllerConfigs`／`LoadControllerConfigs`、`ControlService.LoadPermanentSlotsConfig`。`ProfileXmlStore` は `BackingStore.Load/SaveControllerConfigsForDevice` を直接呼ぶ。
%%
%% Phase7-Step6（2026-10-17）: 出力デバイス種別の配列の所有権の移動。
%%   - `OutputSlotService` が `OutDevTypeTemp`／`ActiveOutDevType` の配列の実体を持つ（IOutputSlotService の契約は変更なし）。
%%   - `BackingStore` に `ActiveOutDevType`（OutContType[]、プロパティ注入。Step10 でコンストラクター注入へ）を追加。
%%   - 削除: `Global.outDevTypeTemp`、`Global.activeOutDevType`。
%%
%% Phase7-Step7（2026-10-17）: Mapping の状態の保持クラスと、トップレベルへ出した型。
%%   - 新設: SyntheticInputState（globalState／deviceState／syncStateLock を保持）、MacroRuntimeState、StickMotionState。いずれも Shared（暫定の静的な窓口）を持ち、DI の Singleton としても登録される。
%%   - トップレベルへ移した型（旧 Mapping の入れ子）: SyntheticState、PostMapStickData、FlickStickMappingData、TwoStageTriggerMappingData、DeltaSettingsProcessor、DeltaSettingsProcessorGroup、Click。
%%   - 新設: ActionsLoadedRuntimeReset（Actions.xml 読み込み後のリセットの購読者。静的クラス）、BackingStore.ActionsLoaded（イベント）。依存方向: BackingStore → （イベント）→ ActionsLoadedRuntimeReset → 保持クラス。BackingStore から Mapping への参照はない。
%%
%% Phase7-Step8（2026-10-17）: Mapping の instance 化。
%%   - 新設: IMapping（8 メンバーの窓口）、Mapping（IMapping の実装。DI の Singleton）、IKeyButtonControllerRegistry／KeyButtonControllerRegistry（旧 Mapping の静的な登録簿）、ISyntheticDispatcher／SyntheticDispatcherService（旧 静的クラス SyntheticDispatcher の処理）。
%%   - 状態の保持クラス 3 つ（SyntheticInputState／MacroRuntimeState／StickMotionState）の Shared（暫定の静的な窓口）を削除。Mapping／Mouse／ControlService／SyntheticDispatcherService／ActionsLoadedRuntimeReset がコンストラクターで受ける。
%%   - ActionsLoadedRuntimeReset は静的クラスから instance へ（保持クラスをコンストラクターで受ける）。
%%   - OutputKBMHandlerAdapter が出力ハンドラの実体（Handler）を持つ。削除: Global.outputKBMHandler／outputKBMMapping／InitOutputKBMHandler／InitOutputKBMMapping。KBM マッピングは IProfileSettingsService.OutputKBMMapping を使う。
%%   - 静的な SyntheticDispatcher は、Actions サブシステム（Phase8 の範囲）が静的に呼ぶための暫定の橋渡し（状態もフォールバックも持たない。DI が作った instance を Bind で受ける）。Phase8 で削除する。
%%   - DefaultActionManager／DefaultMacroPlayer は Func<IMapping>（遅延）を受ける（ホスト構築時に Mapping を作らない。Phase5-Plan §5.4）。
%%   - 削除: Mapping の AppHost／ServiceProviderHolder／Global のフォールバック、MouseOutputAction の無効な検索、呼び出しが 0 件だった内部メンバー 4 つ（Step8 W1）。
%%   - 詳細は Phase7-Step8-Plan.md、Phase7-Step8-Completion-Report.md、台帳は Phase7-Residual-Ledger.md §11。
%%
%% Phase7-Step9（2026-10-18）: 互換 AppHost の削除と、Mapping の Global 参照の解消。
%%   - 削除: 互換 DS4Windows.AppHost（DI/Compat/AppHost.cs。呼び出し元 0 件）。正本は DS4WinWPF.AppHost のみ。
%%   - 新設: ControllerLimits（コントローラー数・スティックの中点などの定数。静的クラス）、NumericUtil（Clamp。静的クラス）。Mapping と状態の保持クラス 3 つはこれを使い、Global を参照しない。Global の同名の定数は別名、Global.Clamp は委譲するシムとして残す（Step10 で利用箇所を移して削除）。
%%   - MacroRuntimeState: 二重実行防止のロックと確保を TryBeginDispatch／EndDispatch に包んだ（DispatchLock は非公開）。Mapping はフラグの配列もロックも持たない。恒久の再設計（実行層の一本化）は Phase8。
%%   - 詳細は Phase7-Step9-Plan.md、Phase7-Step9-Completion-Report.md、台帳は Phase7-Residual-Ledger.md §12。
%%
%% Phase7-Step10a（2026-10-18）: Global の定数・資源パス・Clamp の再配置。
%%   - Global から削除: MAX_DS4_CONTROLLER_COUNT／TEST_PROFILE_ITEM_COUNT／TEST_PROFILE_INDEX／OLD_XINPUT_CONTROLLER_COUNT／DS4_STICK_AXIS_MIDPOINT（→ ControllerLimits）、ASSEMBLY_RESOURCE_PREFIX／RESOURCES_PREFIX（→ 画面側の ResourcePaths.AssemblyResourcePrefix／ResourcesPrefix）、Clamp（→ NumericUtil.Clamp）。
%%   - 全利用箇所（本体・テスト）を新しい型へ切り替えた。ControlService.MAX_DS4_CONTROLLER_COUNT（公開）と ProfileSettingsService の再宣言は ControllerLimits から値を取る。
%%   - 一時的な参照: Global.iconChoiceResources（トレイアイコンの表）が ResourcePaths の定数を参照している（Step10b で画面側へ移す）。
%%   - 詳細は Phase7-Step10a-Plan.md、Phase7-Step10a-Completion-Report.md、台帳は Phase7-Residual-Ledger.md §13。
%%
%% Phase7-Step10b（2026-10-18）: Global の表・色の関数の再配置と、持ち越しの契約の整理。
%%   - 新設（静的クラス。状態を持たない表・純粋計算）: X360ControlTables（ボタンの既定割り当て・名前の表・getX360ControlString）、DS4ControlNames（DS4 入力の表示名・マクロ記録用の値）、ColorUtil（getTransitionedColor ほか）、TrayIconResources（画面側。トレイアイコンの資源パスの表）。
%%   - Global から削除: 上記の表・関数。BackingStore の 1 引数の getX360ControlString も X360ControlTables へ移した。
%%   - 削除: ProfileSubSettingBase.OnSubPropertyChanged（購読者 0 件）、ProfileApplicationService の未使用の依存 IDeviceStateService、ControlService.USING_MAX_CONTROLLERS（静的シム）、孤立定数 4 件。
%%   - 接続: IEnvironmentService.UsingMaxControllers を AutoProfileHolder（コンストラクター注入）・AutoProfilesViewModel（同）・ProgramItem（生成元が値を渡す）が使う。AutoProfilesDTO.MapFrom は source.UsingMaxControllers を読む。
%%   - 温存: IOutputSlotService.OutputSlotChanged（Phase8 以降の UI 通知の拡張点）。AppearanceSettingsService → TrayIconResources（サービスから画面側の型への参照。Phase8 以降で整理）。
%%   - 詳細は Phase7-Step10b-Plan.md、Phase7-Step10b-Completion-Report.md、台帳は Phase7-Residual-Ledger.md §14。
%%
%% Phase7-Step10c（2026-10-18）: BackingStore の永続化の切り出し（保存形式は変えない）。
%%   - BackingStore は、設定の状態（公開のフィールド・プロパティ）と、名前引き・後処理の補助だけを持つ型に絞った（6,522 行 → 1,660 行）。BackingStore は永続化の型を知らない（依存の向きは、シリアライザー → BackingStore）。
%%   - 新設（DS4Control/Services/、具象クラスの Singleton。BackingStore を store として受ける）: SpecialActionsXmlSerializer（Actions.xml）、LinkedProfilesXmlSerializer（LinkedProfiles.xml）、ControllerConfigsXmlSerializer（ControllerConfigs.xml）、AppSettingsXmlSerializer（Profiles.xml。IPathService・IEnvironmentService）、LegacyProfileXmlSerializer（プロファイル XML の読込・保存。IPathService・IProfileSettingsService・IEnvironmentService）、ProfilePresets（プリセット 10 種）。静的: VersionUtil。
%%   - 呼び出し元: ProfileXmlStore（IProfileXmlStore の実装。排他ロック XmlIoLock を持つ）→ AppSettings／LegacyProfile／ControllerConfigs、ProfileRepository → LinkedProfiles／SpecialActions／LegacyProfile、SpecialActionRepository → SpecialActions、ProfilePresetLoader → ProfilePresets。
%%   - Global のアクション保存系のシム（SaveAction ほか）と CreateStdActions を削除。ActionsLoaded イベントの発行元は BackingStore のまま（SpecialActionsXmlSerializer が RaiseActionsLoaded を呼ぶ）。
%%   - 詳細は Phase7-Step10c-Plan.md、Phase7-Step10c-Completion-Report.md、台帳は Phase7-Residual-Ledger.md §15。
%% Phase7-Step10d（2026-10-18）: Global の削除と BackingStore のコンストラクター注入。
%%   - Global（Global.cs）を削除。BackingStore(IEnvironmentService)。BackingStore.ProfileSettings（ProfileSettingsService が生成時に登録）。
%%   - 新設: IDeviceLayerSettings／DeviceLayerSettings（第 1 層の設定）、AdminCheck、DevicePathProbe、ConfigFileCulture、UiCulture。
%%   - イベントの移行: AppearanceSettingsService.BatteryChanged、DeviceStateService.DeviceSerialChange。出力デバイスは SteeringWheelAxisProvider を受ける（**Phase9 W3［2026-10-22］で削除済み**。ステアリング機能ごと廃止）。
%%   - 詳細は Phase7-Step10d-Plan.md、Phase7-Step10d-Completion-Report.md、台帳は Phase7-Residual-Ledger.md §16。
%% Phase7-Step10e（2026-10-18）: 仕上げ（契約・コンストラクターの変更）。
%%   - 削除したメンバー（呼び出し 0 件）: IAppSettingsService.CheckWhen、IProfileSettingsService.SATriggerCond（GetSATriggerCond は残る）、IAutoProfileService.ClearState、IProfileRepository.GetProfilePath（ProfileRepository の public メソッドは残る。窓口は IPathService.GetProfilePath）。
%%   - 維持（TODO(Phase8) 付き）: IAppSettingsService.SettingChanged、ISpecialActionRepository.ActionsChanged、IProfileSettingsService.UseTempProfileArray。
%%   - コンストラクターの変更: OutputSlotManager(+int slotLimit)、ProfileRepository(+IEnvironmentService)、ProfileSlotApplier(+IEnvironmentService)、EditorWindowFactory(+IEnvironmentService)。ControlService.CURRENT_DS4_CONTROLLER_LIMIT（静的）は削除。
%%   - AppHost: GetService は暗黙にホストを作らない。CreationNote は維持。
%%   - 詳細は Phase7-Step10e-Plan.md、Phase7-Residual-Ledger.md §17。
%% Phase7 完了（2026-10-18）: Step1〜Step10e の変更は、上記の Phase7-Step 別の注釈に記載した。全体の完了報告は Phase7-Completion-Report.md、台帳は Phase7-Residual-Ledger.md。Phase8 への引き継ぎ（Actions の ServiceProviderHolder 17 件、第 1 層の静的な状態、TODO(Phase8) 付きメンバー 3 件）は同報告書 §5.2。
%% Phase7b（2026-10-19）: 持ち越し不具合の是正。IMapping に CaptureProfileUntrigger／RestoreProfileUntrigger を追加（メンバー数 8 → 10。ProfileUntriggerSnapshot を新設）。ActionManager.ClearDeviceState（DefaultActionManager と静的なフォールバック）が、HandleDeviceDisconnect の前後で、プロファイル切替の復帰の合図を退避・復元する。詳細は Phase7b-Completion-Report.md。
%% 注釈補強（2026-10-19 Phase8-Step2 W5［段階 A］: 統一トリガーディスパッチの目標像を、クラス図の末尾に追加した。未実装。構造の目標は変更しない）
%% - 判定（IInputAction＝ComboTriggerInputAction／MultiTapInputAction）と実行（IOutputAction）を TriggerBinding が束ね、ITriggerDispatchEngine が、エッジ検出・Fresh-press ガード・抑制・復帰を一元的に担う。実行時の状態は IManagedActionManager（ActionInstanceState）に残す。
%% - IActionRegistry は、スロットごとの不変リストを参照ごと差し替える（Register／Unregister／GetBindingsForDevice は廃止）。IActionBinding の OnTriggered／OnReleased は廃止（ライフサイクルはエンジン）。
%% - 出力アクションは、既存の MacroAction／ProfileSwitchAction／LaunchProcessAction をそのまま使い、KeyOutputAction は KeyAction へ委譲する形に書き直す。ButtonOutputAction は新設（毎フレームの出力）。デバイスを要する種別（DisconnectBT ほか）は Step7.6 で追加。
%% 注釈補強（2026-10-21 Phase10-Step2 反映: 共通の編集画面の骨格）
%% - 編集画面の ViewModel を、役割ごとに分けた（`DS4Forms/ViewModels/`）。`ActionEditorViewModel`（共通の土台）は、`IActionTriggerSource`（SA: `SpecialActionTriggerSource`＝複数のボタン。Controls: `ControlTriggerSource`＝対象のボタン 1 つで固定）、`IActionEditTarget`（SA: `SpecialActionEditTarget`＝名前の検査つき。Controls: `ControlSettingsEditTarget`＝`DS4ControlSettings` の編集用のコピー。名前はボタン名で固定）、`ActionTypeCatalog`（種別の一覧。`ActionTypeDescriptor` ごとに、使えるモードと、今保存できるモード）、`TriggerCatalog`（トリガーの一覧 49 項目）に委譲する。派生は `SpecialActEditorViewModel`（SA のモード）と `ControlActionEditorViewModel`（Controls のモード）。
%% - 種別ごとの入力（`MacroViewModel`、`PressKeyViewModel` ほか 7 つ）は `IActionTypeEditor` を実装し、`ActionTypeEditorSet` が、種別から対応する入力を選んで、読み込み・検証・保存を中継する。
%% - 未接続: `IEditorWindowFactory.CreateControlActionEditor` と Controls のモードは、Phase10-Step4 まで呼び出し元がない（`Phase10-Step2-Plan.md` §9.3）。`BindingWindow`／`RecordBox` は、Step4〜6 で、共通の編集画面へ統合される。

%% 注釈補強（2026-10-21 Phase10-Step3 反映: Controls タブの割り当てエディターへの接続）
%% - `ProfileEditor` が、Controls の割り当ての編集を、従来の選択ダイアログ（`BindingWindow` の `Full`）ではなく、割り当てエディター（`SpecialActionEditor` の Controls のモード。`IEditorWindowFactory.CreateControlActionEditor`）で行う。共通の入口 `ShowControlAssignmentEditor` が、左ペイン全体（`sidebarTabControl`）の上に重ねたホスト `controlEditorHost` へ表示し、編集中は左ペインのタブを隠す。SA の新規・編集も、同じ `OpenLeftPaneEditor`／`CloseLeftPaneEditor` を使う。二重に開かない判定（`leftPaneEditor`）は、コントローラーの入力（タイマー）経由の安全装置としても働く。
%% - 出力ピッカー（`BindingWindow`）は、割り当てエディターの「Select a Key」から開く。表示モードは `SpecialAction`（Default／Unbound・Extras を出さない）と `Control`（Default／Unbound と Extras を出す）。どちらも、Record A Macro・Toggle・Scan Code は出さない（割り当てエディターで設定する）。`Full`・`Keyboard` は使われない（撤去は Phase10-Step6）。
%% - 種別の一覧（`ActionTypeCatalog`）は、Controls のモードだけ、末尾に Default（添字 10）と Unbound（添字 11）を足す（`includeControlOnlyItems`）。モードごとの入力の制限は、一覧の項目の表示・非表示だけで行う（Controls は添字 0 を隠し、SA は足した 2 項目を隠す）。`ControlSettingsEditTarget` は、`Extras` を持ち（出力ピッカーの結果を取り込む。Save で反映、Cancel で破棄）、Default・Unbound を書き出す。
%% - Controls の空のマクロは、SA と同じく `ControlBindingBuilder` が所有する。「押している間繰り返す」の空のマクロは、`Mapping.PlayMacroTask` が 50ms の間隔を置いて繰り返す。

%% 注釈補強（2026-10-21 Phase10-Step3b 反映: 割り当てエディターの入口を、すべて左ペインのタブへ）
%% - `ProfileEditor` の画面構成: 左ペイン（`sidebarTabControl`）は、Controls、Special Actions、Touchpad、ジャイロ、Controller Readings。右ペイン（`profileSettingsTabCon`）は、Axis Config、Lightbar、Other。割り当て操作の入口（Controls の絵のボタン・一覧、Tilt、Swipe、スティックの Outer Bind、トリガーの Full Pull）は、すべて左ペインのタブにあり、編集中は左ペイン全体が割り当てエディターで覆われるため、二重に開けない（`leftPaneEditor` の判定は、コントローラーの入力［タイマー］経由の安全装置）。
%% - `MappingListViewModel` の一覧は、スティックの Outer Bind（LS／RS）とトリガーの Full Pull（L2／R2）の 4 項目を、末尾（添字 34〜37）に含む（以前の別扱い `extraControls` は廃止）。専用のボタンのクリックのハンドラーは、なくなった。

%% 注釈補強（2026-10-21 Phase10-Step4 反映: Controls での SA 専用の種別。W6 で締め）
%% - `DS4ControlSettings` に `ActionType.Special` と `special`（`ControlSpecialItem`。型のある項目: 種別、Hold 秒数、Macro の 3 項目、Program、Profile、BatteryCheck、MultiAction の項目）を足した。`Profile.xml` の `<Control><Special>` に、ボタンごとの要素（`Type` 属性）として保存する（`ControlSpecialItemXml`）。既存の節の形式・順序は不変。
%% - `ISpecialActionOutputFactory`／`SpecialActionOutputFactory`／`SpecialActionOutputs`（`Actions/`）が、SA の種別ごとの出力アクション・方針・入力の種類を作る。`ActionBindingBuilder` は、これを使う。`ControlBindingBuilder` は、Special の割り当て（Disconnect BT、Calibrate gyro、Calibrate sixaxis wheel、Launch Program、Load Profile）で、`forControls: true`（抑制せず・出力を消さず・割り当てたボタンの出力を毎フレーム消す）で使う。Macro の 3 項目は、`AddMacro` が合成 `SpecialAction` へ写す。
%% - `ControlButtonInputAction` は、離したとき成立（`invertOnRelease`）と押し続ける秒数（`delaySeconds`）を持つ。`ControlBindingKind.Special` を追加。
%% - 画面: `ControlSettingsEditTarget` が、`ControlSpecialEditors`（秒数の入力、Launch Program、Load Profile）と型のある項目を往復する。`CanWrite` が、Load Profile の未選択の Save を止める。`IProfileRepository.ReplaceControlProfileSwitchTarget` が、プロファイルの名前変更で、全プロファイルの `<Special>` の切り替え先を書き換える。
%% - 暫定（統合の Step で整理）: `forControls`、`ControlJudgement`、`ControlBindingBuilder.BuiltSpecialTypes`、`ControlSpecialEditors`。Check Battery と Multi-action は、保存形式のみ（画面・実行は統合後）。

%% 注釈補強（2026-10-21 Phase10-Step4c 反映: Controls と SA の挙動の差を減らす仕様変更。W0〜W5。完成形は Phase10-Integration-Target-Model.md）
%% - Fresh-press ガード: 「見るか」の規則は `FreshPressGuardTable`（`Actions/`、静的）の 1 か所（`ForSpecialAction`／`ForControls`）。`SpecialActionOutputFactory.Policy` と `ControlBindingBuilder` が引く。挙動は不変。
%% - ログ: 離したエッジのログ（Trace `Trigger released for SA '…'`、Debug `SpecialAction <種別> released: …`）は、`TriggerDispatchEngine` が全種別で出す（`BindingPolicy.LogsReleaseEdge`。既定は真。Default の軸の素通しと Unbound の消すだけの束縛は偽）。立ち上がりのログは、出力アクションが出す種別（SA の全種別、Controls の Key／Macro／Special）と、エンジンが出す種別（Controls の Pad／Mouse／AnalogMouse／Extras。`BindingPolicy.LogsRisingEdge`）がある。Controls の Key の `suppressSpecialActionLogs` は解除。`AppLogger.IsDebugEnabled` を追加。
%% - 保存の可否: 種別ごとの入力（`IActionTypeEditor`）の `IsValid` は、必須の値がないとき（キー・ボタン未選択、Launch Program のパスが空、Load Profile 未選択、Check Battery の方法が未選択・2 色が同じ、Hold が範囲外）に偽を返し、保存を止める。「ファイルが存在しない」（警告）、Toggle の「解除トリガーなし」（機能廃止済み）、Multi-action の未設定のマクロは、表示のみ。対象の入力は赤い枠（`ValidatesOnNotifyDataErrors`）。Controls の Save（`SaveControlMode`）も `typeEditors.IsValid` を呼ぶ。`ControlSettingsEditTarget.CanWrite` は削除。
%% - 押下判定: デジタルな出力（Key、Extras、パッドのボタン、マウスのボタン・タッチパッドクリック・デジタルのホイール［Toggle つきを含む］）は SA の閾値（`ControlJudgement.Trigger`）。アナログな出力（パッドの軸・トリガー、マウス移動、アナログのホイール、素通し）は従来の判定（`Assignment`）。判定は `ControlBindingBuilder` が束縛ごとに選ぶ。
%% - 入力の集合: SA のトリガー一覧は 51 項目（末尾に LS Outer／RS Outer を追加。`SpecialAction.getDS4ControlsByName` にも）。Controls の一覧は 51 項目（末尾の後ろに Touch Started／Touch Ended を追加。`ControlSettingsGroup.ControlButtons` にも）。

%% 注釈補強（2026-10-21 Phase10-Step4d 反映: プロファイルと SA の固定 ID。W0〜W7。完成形は Phase10-Integration-Target-Model.md §9）
%% - 固定 ID（GUID の N 形式）: プロファイル XML のルート直下の先頭の `<ProfileId>`（ファイルの中にあるため、名前［ファイル名］を変えても変わらない。保存は、保存先のファイルの ID を保ち、新しいファイルには新しい ID を振る）と、`Actions.xml` の `Action` の `Id` 属性（`SpecialAction.id`。編集・名前変更で `SaveAction` の `actionId` で引き継ぐ）。起動時に、ID のない（または重複・不正な）ファイル・SA へ一括で割り当てる。
%% - 登録簿 `IProfileIdentityService`／`ProfileIdentityService`（Singleton）: ID ↔ 名前。メモリの索引（プロファイルのフォルダーの最終更新時刻で鮮度を判定し、1 秒以内に新しいあいだは毎回作り直す）。DI の登録が、作るときに `BackingStore`（`AttachProfileIdentity`）と `ProfileSettingsService` に結ぶ。
%% - 保存する参照は、ID を主・名前を併記（ID が勝つ。見つからなければ名前）: `<ProfileActions>` ＋ `<ProfileActionIds>`、SA の Load Profile（`Actions.xml` の `ProfileId`）、Controls の Load Profile（`<Control><Special>` の `ProfileId`）、スロットの選択（`Profiles.xml` の `<ControllerProfileIds>`）、リンク（`LinkedProfiles.xml` の `MAC…` の `Id` 属性）、自動プロファイル（`Auto Profiles.xml` の `<ControllerProfileIds>`）。
%% - 実行時の保持（プロファイル）は、保管が ID で、名前を読み書きする窓口: `ProfileSlotNames`（`BackingStore` の `profilePath`／`olderProfilePath`／`selectedProfile`／`linkedProfileUI`、`ProfileSettingsService` の一時プロファイルの名前、`AutoProfileEntity.ProfileNames`）、`LinkedProfileMap`（`linkedProfiles`）。名前を変えても、読み出す名前が現在の名前になる。Load Profile は、実行の直前に `SpecialAction.profileId` から現在の名前を引く（`ProfileApplicationService.ResolveTarget`。Controls の合成 SA も `profileId` を持つ）。
%% - SA の実行時の状態（`profileActions`、`ActionManager` の鍵）は名前のまま（W6 の決定）。追従のコードは、実行時に不要になったものを撤去し（`ReplaceLinkedProfileName`、`AutoProfileHolder.ReplaceProfileName`、スロットの配列の書き換え）、ID のない古いファイルのために `ReplaceControlProfileSwitchTarget`／`ReplaceActionReference`／`RemoveActionReference` を残す（TODO 付き）。名前変更の直後に、保存先のファイルを保存し直す（`SaveLinkedProfiles`、`AutoProfileHolder.Save`、アプリ設定、`RefreshProfileSwitchTargets`）。


%% 注釈補強（2026-10-22 Phase10-Step6 反映: 統合 Step。共通の割り当て定義と、束縛の組み立ての 1 本化。I1〜I7。詳細は Phase10-Step6-Completion-Report.md、Phase10-Integration-Target-Model.md §10）
%% - 共通の割り当て定義 `AssignmentDefinition`（`Actions/`。不変）: 設定の静的な内容（ID、名前、由来 `AssignmentOrigin`［Controls／SpecialAction］、種別、トリガー、押し続ける秒数、型のある項目 `ControlSpecialItem`、Key／Button の解釈済みの値、Extras、Macro の手順・keyType）。Controls 固有の種別（パッドへの出力、マウス、アナログマウス、素通し）は定義に入れず、`ControlBindingBuilder` に残る。
%% - 読み込みの部品: `SpecialActionAssignmentReader`（SA → 定義）、`ControlsAssignmentReader`（Controls の Special／Key／Macro → 定義と、実行時の状態の入れ物［合成の `SpecialAction`］）。文字列（`details`／`extra`）の解釈は `SpecialActionItemMapper`（`SpecialAction` ⇄ 型のある項目）の 1 か所。`Actions.xml` と `SpecialAction` の形式は変えていない（旧版との互換を保つ）。
%% - 束縛の組み立ては `AssignmentBindingBuilder` の 1 つ（出力と方針 = `ISpecialActionOutputFactory`［種別 × 由来の表］、入力 = 由来と種別から、`TriggerBinding` への包み）。一覧を歩く部分は `ActionBindingBuilder`（SA のリスト）と `ControlBindingBuilder`（Controls の dcs。Extras を前に置く順序と、扱えなければ dcs 全体を作らない門番）の 2 つのまま。実行時の状態は、引き続き `SpecialAction`（`TriggerBinding.Source`）が持つ（SA 由来は実物、Controls 由来は合成）。
%% - 規則の表: `FreshPressGuardTable.For`（種別 × 由来）、`ControlOutputNatureTable`（出力の種類 = デジタル／アナログから、押下の判定の性質 `ControlOutputNature`）、`ControlsSpecialTypeTable`（Controls で実行時に束縛を作る SA 専用の種別。種別の一覧の「Controls で保存できる」も同じ表に従う）。
%% - SA の Extras: `SpecialAction.outputExtras`（Controls の Extras と同じ書式）。`Actions.xml` の新要素 `OutputExtras`（あるときだけ）。`AssignmentBindingBuilder.BuildExtras` が別の束縛（`Synthetic_SaExtras_{device}_{SA の ID}`）を作り、SA の束縛の前に並べる。SA が成立している間だけ有効（SA のトリガー・押し続ける秒数と同じ押している間の入力。Fresh-press ガードは SA 本体と同じ）。SA の保存は SA を作り直すため、割り当て編集画面が、保存のあとに `ISpecialActionRepository.SaveActionExtras` を呼ぶ。
%% - 画面: 割り当て編集画面（`AssignmentEditor`。旧 `SpecialActionEditor`）と出力ピッカー（`BindingWindow`）は、Controls のモードと SA のモードで共通。2 つのモードの違いは Default／Unbound の表示だけ（`ExposeMode` は SpecialAction と Control の 2 つ。Full と Keyboard は撤去）。Controls のモードで型のある項目と往復する種別ごとの入力は、`IActionTypeEditor.LoadControlItem`／`ToControlItem`（`ControlSpecialEditors` は削除）。
%% - 撤去した暫定の構造: `forControls`、`ControlJudgement`、`ControlBindingBuilder.BuiltSpecialTypes`、`ControlSpecialEditors`、`ISpecialActionOutputFactory` の互換の入口。
%% - 残した技術的負債: SA の出力アクション 9 クラスに重複する立ち上がりのログ（後回しを承認済み）。`SpecialAction` が実行時の状態を持つこと。ID のない古いファイルのための追従のコード。Extras を画面から付けられる SA は Press Key（Key／Button）だけ。

%% 注釈補強（2026-10-22 W7・W8 相当 反映: Check Battery、Multi-action を Controls へ。詳細は Phase10-Integration-Target-Model.md §11）
%% - `ControlsSpecialTypeTable` に Check Battery と Multi-action を追加（実行時の builder、種別の一覧の「Controls で保存できる」、`ControlSettingsEditTarget.WriteFrom` の分岐が、この表に従う）。`ControlSpecialItem` の Check Battery の通知・ライトバーを `<Control><Special>` に保存する（`ControlSpecialItemXml`）。
%% - 入力の印 `IControlBoundInput`（`Actions/`）を新設。`ControlButtonInputAction` と、新しい `ControlMultiTapInputAction`（`MultiTapInputAction` に判定を任せる、Controls 由来の Multi-action の入力）が実装する。`TriggerDispatchEngine` は、この印で、Controls の束縛をボタンごとの処理の表（`ProcessControl`／`OwnsControl`）に登録する（以前は `ControlButtonInputAction` の型で見分けていた）。
%% - 方針: Controls の Multi-action は、元のボタンの出力を常に消す（待ち時間のあいだだけでなく）。未設定のマクロは、保存を止めず（赤い枠）、実行自体を行わない（`MacroStringAction` は空文字を実行しない）。
%% - 画面: `CheckBatteryViewModel`／`MultiActButtonViewModel` が `IActionTypeEditor.LoadControlItem`／`ToControlItem` を実装。

%% 注釈補強（2026-10-22 Step5 反映: SA 側のアナログ出力。詳細は Phase10-Step5-Plan.md、Phase10-Integration-Target-Model.md §12）
%% - SA の Button の出力先がパッドの軸・トリガー、マウス移動のとき、強さ（0〜100%。既定 100）の固定値で出す。`SpecialAction.analogStrength`（`Actions.xml` の Button の `Strength`。100 以外のときだけ）、`ButtonBindingValues.Strength`。
%% - 出力先の分類は `AnalogOutputTargets.KindOf`（`AnalogOutputKind`: PadAxis、PadTrigger、MouseMove、None）の 1 か所。`ButtonOutputAction` がアナログの分岐を持つ（軸は中立 128 から端へ、トリガーは 255 × 強さ、マウス移動は `IFixedMouseInput`［`TriggerInput` が実装。`Mapping.SampleFixedMouseMovement` が、ボタンが元のときの量 × 強さを返す］）。100% は、Controls のボタンが元のときの出力と同じ。ホイールは、今のまま（デジタル）。
%% - 画面: 割り当て編集画面（SA のモード）の Press Key のタブに、出力先がアナログのときだけ「Strength」を出す（`PressKeyViewModel.ShowAnalogStrength`）。保存は、SA の保存のあとに `ISpecialActionRepository.SaveActionAnalogStrength`。Controls のモードには出さない。
