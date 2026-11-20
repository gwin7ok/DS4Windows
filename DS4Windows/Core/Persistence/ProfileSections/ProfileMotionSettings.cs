#nullable enable
using System;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.ProfileSections
{
    /// <summary>
    /// ジャイロ・モーション・ボタンマウス・タッチパッドの出力 の保存する要素の定義の並び。並びの順が、保存する要素の順序（XML の要素の順序は、黄金テストが固定している）。
    /// </summary>
    internal static class ProfileMotionSettings
    {
        public static readonly ProfileSetting[] All =
        {
            ProfileSetting.Text("MouseAcceleration", (store, device, c) => store.buttonMouseInfos[device].mouseAccel.ToString()),
            ProfileSetting.Text("ButtonMouseVerticalScale", (store, device, c) => Convert.ToInt32(store.buttonMouseInfos[device].buttonVerticalScale * 100).ToString()),
            //XmlNode xmlShiftMod = m_Xdoc.CreateNode(XmlNodeType.Element, "ShiftModifier", null); xmlShiftMod.InnerText = shiftModifier[device].ToString(); rootElement.AppendChild(xmlShiftMod);
            ProfileSetting.Text("LaunchProgram", (store, device, c) => store.basicSlots[device].LaunchProgram.ToString()),
            ProfileSetting.Text("DinputOnly", (store, device, c) => store.basicSlots[device].DinputOnly.ToString()),
            ProfileSetting.Text("StartTouchpadOff", (store, device, c) => store.touchpadSlots[device].StartOff.ToString()),
            ProfileSetting.Text("TouchpadOutputMode", (store, device, c) => store.touchpadSlots[device].OutputMode.ToString()),
            ProfileSetting.Text("SATriggers", (store, device, c) => store.gyroSlots[device].MouseTriggers.ToString()),
            ProfileSetting.Text("SATriggerCond", (store, device, c) => BackingStore.SaTriggerCondString(store.gyroSlots[device].MouseTriggerCondAnd)),
            // Phase9 Steering 削除 W4: 旧「SASteeringWheelEmulationAxis／SASteeringWheelEmulationRange／SASteeringWheelFuzz／SASteeringWheelSmoothingOptions」（ステアリングの設定）の書き込みは、機能ごと削除した。
            ProfileSetting.Text("TouchDisInvTriggers", (store, device, c) => string.Join(",", store.touchpadSlots[device].DisInvertTriggers)),
            ProfileSetting.Text("GyroSensitivity", (store, device, c) => store.gyroSlots[device].Sensitivity.ToString()),
            ProfileSetting.Text("GyroSensVerticalScale", (store, device, c) => store.gyroSlots[device].SensVerticalScale.ToString()),
            ProfileSetting.Text("GyroInvert", (store, device, c) => store.gyroSlots[device].Invert.ToString()),
            ProfileSetting.Text("GyroTriggerTurns", (store, device, c) => store.gyroSlots[device].TriggerTurns.ToString()),
            ProfileSetting.Group("GyroControlsSettings",
                ProfileSetting.Text("Triggers", (store, device, c) => store.gyroSlots[device].Controls.triggers),
                ProfileSetting.Text("TriggerCond", (store, device, c) => BackingStore.SaTriggerCondString(store.gyroSlots[device].Controls.triggerCond)),
                ProfileSetting.Text("TriggerTurns", (store, device, c) => store.gyroSlots[device].Controls.triggerTurns.ToString()),
                ProfileSetting.Text("Toggle", (store, device, c) => store.gyroSlots[device].Controls.triggerToggle.ToString())),
            ProfileSetting.Group("GyroMouseSmoothingSettings",
                ProfileSetting.Text("UseSmoothing", (store, device, c) => store.gyroSlots[device].Mouse.enableSmoothing.ToString()),
                ProfileSetting.Text("SmoothingMethod", (store, device, c) => store.gyroSlots[device].Mouse.SmoothMethodIdentifier()),
                ProfileSetting.Text("SmoothingWeight", (store, device, c) => Convert.ToInt32(store.gyroSlots[device].Mouse.smoothingWeight * 100).ToString()),
                ProfileSetting.Text("SmoothingMinCutoff", (store, device, c) => store.gyroSlots[device].Mouse.minCutoff.ToString()),
                ProfileSetting.Text("SmoothingBeta", (store, device, c) => store.gyroSlots[device].Mouse.beta.ToString())),
            ProfileSetting.Text("GyroMouseHAxis", (store, device, c) => store.gyroSlots[device].MouseHorizontalAxis.ToString()),
            ProfileSetting.Text("GyroMouseDeadZone", (store, device, c) => store.gyroSlots[device].MouseDeadZone.ToString()),
            ProfileSetting.Text("GyroMouseMinThreshold", (store, device, c) => store.gyroSlots[device].Mouse.minThreshold.ToString()),
            ProfileSetting.Text("GyroMouseToggle", (store, device, c) => store.gyroSlots[device].MouseToggle.ToString()),
            ProfileSetting.Text("GyroOutputMode", (store, device, c) => store.gyroSlots[device].RemovedSwipe.GyroOutputModeTextForSave(store.gyroSlots[device].OutputMode.ToString())),
            ProfileSetting.Text("GyroMouseStickTriggers", (store, device, c) => store.gyroSlots[device].MouseStickTriggers.ToString()),
            ProfileSetting.Text("GyroMouseStickTriggerCond", (store, device, c) => BackingStore.SaTriggerCondString(store.gyroSlots[device].MouseStickTriggerCondAnd)),
            ProfileSetting.Text("GyroMouseStickTriggerTurns", (store, device, c) => store.gyroSlots[device].MouseStickTriggerTurns.ToString()),
            ProfileSetting.Text("GyroMouseStickHAxis", (store, device, c) => store.gyroSlots[device].MouseStickHorizontalAxis.ToString()),
            ProfileSetting.Text("GyroMouseStickDeadZone", (store, device, c) => store.gyroSlots[device].MouseStick.deadZone.ToString()),
            ProfileSetting.Text("GyroMouseStickMaxZone", (store, device, c) => store.gyroSlots[device].MouseStick.maxZone.ToString()),
            ProfileSetting.Text("GyroMouseStickOutputStick", (store, device, c) => store.gyroSlots[device].MouseStick.outputStick.ToString()),
            ProfileSetting.Text("GyroMouseStickOutputStickAxes", (store, device, c) => store.gyroSlots[device].MouseStick.outputStickDir.ToString()),
            ProfileSetting.Text("GyroMouseStickAntiDeadX", (store, device, c) => store.gyroSlots[device].MouseStick.antiDeadX.ToString()),
            ProfileSetting.Text("GyroMouseStickAntiDeadY", (store, device, c) => store.gyroSlots[device].MouseStick.antiDeadY.ToString()),
            ProfileSetting.Text("GyroMouseStickInvert", (store, device, c) => store.gyroSlots[device].MouseStick.inverted.ToString()),
            ProfileSetting.Text("GyroMouseStickToggle", (store, device, c) => store.gyroSlots[device].MouseStickToggle.ToString()),
            ProfileSetting.Text("GyroMouseStickMaxOutput", (store, device, c) => store.gyroSlots[device].MouseStick.maxOutput.ToString()),
            ProfileSetting.Text("GyroMouseStickMaxOutputEnabled", (store, device, c) => store.gyroSlots[device].MouseStick.maxOutputEnabled.ToString()),
            ProfileSetting.Text("GyroMouseStickVerticalScale", (store, device, c) => store.gyroSlots[device].MouseStick.vertScale.ToString()),
            ProfileSetting.Group("GyroMouseStickSmoothingSettings",
                ProfileSetting.Text("UseSmoothing", (store, device, c) => store.gyroSlots[device].MouseStick.useSmoothing.ToString()),
                ProfileSetting.Text("SmoothingMethod", (store, device, c) => store.gyroSlots[device].MouseStick.SmoothMethodIdentifier()),
                ProfileSetting.Text("SmoothingWeight", (store, device, c) => Convert.ToInt32(store.gyroSlots[device].MouseStick.smoothWeight * 100).ToString()),
                ProfileSetting.Text("SmoothingMinCutoff", (store, device, c) => store.gyroSlots[device].MouseStick.minCutoff.ToString()),
                ProfileSetting.Text("SmoothingBeta", (store, device, c) => store.gyroSlots[device].MouseStick.beta.ToString())),
        };
    }
}
