using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step12: 呼び出し元 0 件の旧静的シムを削除した結果の回帰ガード。
    /// (1) 削除済みのメンバーが <c>Global.cs</c> に再び現れないこと、
    /// (2) 削除禁止の防壁（定数、Pre-Host、ViGEm クライアント依存ほか）にあたるメンバーが残っていること、を固定する。
    /// 削除した名前は Wave ごとに <see cref="RemovedFromGlobal"/> へ追記する（名簿は Phase6-Step12-Plan.md §0A.8）。
    /// ソースが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class Step12DeadShimRemovalGuardTests
    {
        private static readonly string[] RemovedFromGlobal =
        {
            // Wave 1（孤立コード）
            "m_IdleTimeout", "exeFileName", "exeversionLong", "fullExeVersionLong", "localAppDataPpath", "linkedProfileCheck",
            "DS4Mapping", "DownloadLang", "CheckForDevice",
            "ControllerStatusChange", "ControllerStatusChanged", "BatteryStatusChange", "OnBatteryStatusChange",
            "ControllerRemoved", "OnControllerRemoved", "DeviceStatusChange", "OnDeviceStatusChanged",
            "getUseExclusiveMode", "getQuickCharge", "GetMinToTaskbar", "getFlashWhenLate", "getFlashWhenLateAt",
            "IsUsingUDPServerSmoothing", "NormalizeActionName",
            // Wave 2（ProfileSettings 系の互換アクセサ。TouchActive／GetTouchActive は touchpadActive の参照元のため含む）
            "BTPollRate", "ButtonMouseInfos", "DinputOnly", "DoubleTap", "DualSenseHapticPowerLevel", "DualSenseRumbleEmulationMode",
            "EnableOutputDataToDS4", "EnableTouchToggle", "getBTPollRate", "getChargingColor", "getCustomColor", "getDInputOnly",
            "getDoubleTap", "GetDS4CSetting", "getDS4CSettings", "getEnableOutputDataToDS4", "getEnableTouchToggle", "getFlashColor",
            "GetGyroControlsInfo", "getGyroInvert", "GetGyroMouseDeadZone", "getGyroMouseHorizontalAxis",
            "getGyroMouseStickHorizontalAxis", "GetGyroMouseStickInfo", "GetGyroMouseStickTriggerTurns", "GetGyroOutMode",
            "getGyroSensitivity", "getGyroSensVerticalScale", "GetGyroSwipeInfo", "getGyroTriggerTurns", "getL2OutCurveMode",
            "getLowColor", "getLsOutCurveMode", "getR2OutCurveMode", "getRsOutCurveMode", "getRumbleAutostopTime", "getRumbleBoost",
            "GetSAMouseStickTriggerCond", "GetSAMouseStickTriggers", "GetSASteeringWheelEmulationRange", "getSATriggerCond",
            "getSATriggers", "getScrollSensitivity", "getSXDeadzone", "getSXMaxzone", "getSXOutCurveMode", "getSXSens", "getSZDeadzone",
            "getSZMaxzone", "getSZOutCurveMode", "getSZSens", "GetTouchActive", "getTrackballFriction", "getTrackballMode",
            "getUseCustomLed", "GyroControlsInf", "GyroInvert", "GyroMouseDeadZone", "GyroMouseHorizontalAxis", "GyroMouseInfo",
            "GyroMouseStickHorizontalAxis", "GyroMouseStickInf", "GyroMouseStickToggle", "GyroMouseStickTriggerTurns", "GyroMouseToggle",
            "GyroOutputMode", "GyroSensitivity", "GyroSensVerticalScale", "GyroSwipeInf", "GyroTriggerTurns", "IdleDisconnectTimeout",
            "InverseRumbleMotors", "L2ModInfo", "l2OutBezierCurveObj", "L2OutputSettings", "L2Sens", "LaunchProgram",
            "LeftStickDriftXAxis", "LeftStickDriftYAxis", "LightbarSettingsInfo", "LowerRCOn", "LSAntiSnapbackInfo", "LSModInfo",
            "lsOutBezierCurveObj", "LSOutputSettings", "LSRotation", "LSSens", "OutputDS4TriggerMode", "OutputVirtualTriggerButton",
            "R2ModInfo", "r2OutBezierCurveObj", "R2OutputSettings", "R2Sens", "RightStickDriftXAxis", "RightStickDriftYAxis",
            "RSAntiSnapbackInfo", "RSModInfo", "rsOutBezierCurveObj", "RSOutputSettings", "RSRotation", "RSSens", "RumbleBoost",
            "SAMouseStickTriggerCond", "SAMousestickTriggers", "SASteeringWheelEmulationAxis", "SASteeringWheelEmulationRange",
            "SATriggerCond", "SATriggers", "SAWheelFuzzValues", "ScrollSensitivity", "SetGyroControlsToggle", "SetGyroMouseDeadZone",
            "SetGyroMouseStickToggle", "SetGyroMouseToggle", "setL2OutCurveMode", "setLsOutCurveMode", "setR2OutCurveMode",
            "setRsOutCurveMode", "setRumbleAutostopTime", "SetSaMouseStickTriggerCond", "SetSaTriggerCond", "setSXOutCurveMode",
            "setSZOutCurveMode", "SquStickInfo", "StartTouchpadOff", "SXDeadzone", "SXMaxzone", "sxOutBezierCurveObj", "SXSens",
            "SZDeadzone", "SZMaxzone", "szOutBezierCurveObj", "SZSens", "TapSensitivity", "tempprofilename", "TouchActive",
            "TouchClickPassthru", "TouchDisInvertTriggers", "TouchMouseStickInf", "TouchOutMode", "touchpadActive", "TouchpadButtonMode",
            "TouchpadInvert", "TouchpadJitterCompensation", "TouchRelMouse", "TouchSensitivity", "TrackballFriction", "TrackballMode",
            "UseGenericRumbleStrRescaleForDualSenses", "WheelSmoothInfo",
            // Wave 3（*Instance のフォールバック群）
            "CreateFallbackOutputSlotService", "deviceStateService", "DeviceStateServiceInstance", "environmentService", "EnvironmentServiceInstance", "fallbackDeviceStateService", "fallbackEnvironmentService", "fallbackOutputSlotService", "fallbackPathService", "fallbackProfileRepository", "fallbackSpecialActionRepository", "outputSlotService", "OutputSlotServiceInstance", "pathService", "PathServiceInstance", "profileRepository", "ProfileRepositoryInstance", "specialActionRepository", "SpecialActionRepositoryInstance",
            // Wave 4（BackingStore 直転送ラッパー。getDefaultX360ControlBinding を含む）
            "CalculateProfileActionCount", "CalculateProfileActionDicts", "containsCustomAction", "containsCustomExtras", "GetActions",
            "GetControlSettingsGroup", "getDefaultX360ControlBinding", "GetDS4Action", "GetDS4Extra", "GetDS4KeyType", "GetDS4STrigger",
            "getL2AntiDeadzone", "getL2Deadzone", "getL2Maxzone", "GetL2ModInfo", "getL2Sens", "getLSAntiDeadzone",
            "GetLSAntiSnapbackInfo", "GetLSDeadInfo", "getLSDeadzone", "getLSMaxzone", "getLSRotation", "getLSSens",
            "GetOutputDS4TriggerMode", "getProfileActionCount", "GetProfileActionIndexOf", "getR2AntiDeadzone", "getR2Deadzone",
            "getR2Maxzone", "GetR2ModInfo", "getR2Sens", "getRSAntiDeadzone", "GetRSAntiSnapbackInfo", "GetRSDeadInfo", "getRSDeadzone",
            "getRSMaxzone", "getRSRotation", "getRSSens", "GetSquareStickInfo", "getSXAntiDeadzone", "getSZAntiDeadzone",
            "getTapSensitivity", "getTouchDisInvertTriggers", "GetTouchMouseStickInfo", "getTouchpadInvert",
            "getTouchpadJitterCompensation", "getTouchSensitivity", "HasCustomActions", "HasCustomExtras", "IsUsingSAForControls",
            "IsUsingTouchpadForControls", "Save", "SaveAsProfile", "SaveProfile", "SXAntiDeadzone", "SZAntiDeadzone", "UpdateDS4CSetting",
            "UpdateDS4Extra",
            // Wave 6（プロファイル用 DTO の削除で、呼び出し元が 0 件になった名前引きの転送）
            "getDS4ControlsByName", "getX360ControlsByName",
            // デバウンス時間の通知経路（案 A の是正後に不要）
            "DebouncingMsHasChanged", "DebouncingMsChanged",
            // Phase7-Step10a W4（定数 5 つ・資源パスの定数 2 つ・Clamp。移し先は ControllerLimits／ResourcePaths／NumericUtil）
            "MAX_DS4_CONTROLLER_COUNT", "TEST_PROFILE_ITEM_COUNT", "TEST_PROFILE_INDEX", "OLD_XINPUT_CONTROLLER_COUNT",
            "DS4_STICK_AXIS_MIDPOINT", "ASSEMBLY_RESOURCE_PREFIX", "RESOURCES_PREFIX", "Clamp",
            // Phase7-Step10b W1（表と変換。移し先は X360ControlTables／DS4ControlNames）
            "defaultButtonMapping", "reverseX360ButtonMapping", "xboxDefaultNames", "ds4DefaultNames", "ds4inputNames", "macroDS4Values", "getX360ControlString",
            "getTransitionedColor", "HuetoRGB", "applyRatio", "iconChoiceResources",
            // Phase7-Step10c W2（Actions.xml の保存・読込のシム。移し先は SpecialActionsXmlSerializer／SpecialActionRepository）
            "SaveAction", "SaveActions", "RemoveAction", "ReloadActionsAndVerify", "LoadActions", "CreateStdActions",
            // Phase7-Step10c W3（バージョンの数値化。移し先は VersionUtil）
            "CompileVersionNumberFromString", "CompileVersionNumber",
        };

        private readonly ITestOutputHelper _output;

        public Step12DeadShimRemovalGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Wave5_RemovedApiSurface_DoesNotReappear()
        {
            string pathServiceInterface = SourceFileLocator.Find("DS4Windows/Core/Abstractions/IPathService.cs");
            if (pathServiceInterface == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            // IPathService.AppDataPath のセッターを再導入しない（Global.appdatapath と連動しない孤立した実装だった）
            string pathText = SourceText.StripComments(File.ReadAllText(pathServiceInterface));
            Assert.Matches(@"string\s+AppDataPath\s*\{\s*get;\s*\}", pathText);

            // 互換ラッパー DS4Windows.AppHost は Phase7-Step9 W1 で削除した（再導入の防止は Phase7Step9W1CompatAppHostRemovedGuardTests）
        }
    }
}
