#nullable enable
using System;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.ProfileSections
{
    /// <summary>
    /// 出力カーブ・アンチスナップバック・フリックスティック・トリガー・DualSense の保存する要素の定義の並び。並びの順が、保存する要素の順序（XML の要素の順序は、黄金テストが固定している）。
    /// </summary>
    internal static class ProfileOutputCurveSettings
    {
        public static readonly ProfileSetting[] All =
        {
            ProfileSetting.Text("BTPollRate", (store, device, c) => store.basicSlots[device].BTPollRate.ToString()),
            ProfileSetting.Text("LSOutputCurveMode", (store, device, c) => store.StickOutputCurveString(store.stickSlots[device].LSCurveMode)),
            ProfileSetting.Text("LSOutputCurveCustom", (store, device, c) => store.stickSlots[device].LsBezier.ToString()),
            ProfileSetting.Text("RSOutputCurveMode", (store, device, c) => store.StickOutputCurveString(store.stickSlots[device].RSCurveMode)),
            ProfileSetting.Text("RSOutputCurveCustom", (store, device, c) => store.stickSlots[device].RsBezier.ToString()),
            ProfileSetting.Text("LSSquareStick", (store, device, c) => store.stickSlots[device].SquStick.lsMode.ToString()),
            ProfileSetting.Text("RSSquareStick", (store, device, c) => store.stickSlots[device].SquStick.rsMode.ToString()),
            ProfileSetting.Text("SquareStickRoundness", (store, device, c) => store.stickSlots[device].SquStick.lsRoundness.ToString()),
            ProfileSetting.Text("SquareRStickRoundness", (store, device, c) => store.stickSlots[device].SquStick.rsRoundness.ToString()),
            ProfileSetting.Text("LSAntiSnapback", (store, device, c) => store.stickSlots[device].LSAntiSnapback.enabled.ToString()),
            ProfileSetting.Text("RSAntiSnapback", (store, device, c) => store.stickSlots[device].RSAntiSnapback.enabled.ToString()),
            ProfileSetting.Text("LSAntiSnapbackDelta", (store, device, c) => store.stickSlots[device].LSAntiSnapback.delta.ToString()),
            ProfileSetting.Text("RSAntiSnapbackDelta", (store, device, c) => store.stickSlots[device].RSAntiSnapback.delta.ToString()),
            ProfileSetting.Text("LSAntiSnapbackTimeout", (store, device, c) => store.stickSlots[device].LSAntiSnapback.timeout.ToString()),
            ProfileSetting.Text("RSAntiSnapbackTimeout", (store, device, c) => store.stickSlots[device].RSAntiSnapback.timeout.ToString()),
            ProfileSetting.Text("LSOutputMode", (store, device, c) => store.stickSlots[device].LSOutput.mode.ToString()),
            ProfileSetting.Text("RSOutputMode", (store, device, c) => store.stickSlots[device].RSOutput.mode.ToString()),
            ProfileSetting.Group("LSOutputSettings",
                ProfileSetting.Group("FlickStickSettings",
                    ProfileSetting.Text("RealWorldCalibration", (store, device, c) => store.stickSlots[device].LSOutput.outputSettings.flickSettings.realWorldCalibration.ToString()),
                    ProfileSetting.Text("FlickThreshold", (store, device, c) => store.stickSlots[device].LSOutput.outputSettings.flickSettings.flickThreshold.ToString()),
                    ProfileSetting.Text("FlickTime", (store, device, c) => store.stickSlots[device].LSOutput.outputSettings.flickSettings.flickTime.ToString()),
                    ProfileSetting.Text("MinAngleThreshold", (store, device, c) => store.stickSlots[device].LSOutput.outputSettings.flickSettings.minAngleThreshold.ToString()))),
            ProfileSetting.Group("RSOutputSettings",
                ProfileSetting.Group("FlickStickSettings",
                    ProfileSetting.Text("RealWorldCalibration", (store, device, c) => store.stickSlots[device].RSOutput.outputSettings.flickSettings.realWorldCalibration.ToString()),
                    ProfileSetting.Text("FlickThreshold", (store, device, c) => store.stickSlots[device].RSOutput.outputSettings.flickSettings.flickThreshold.ToString()),
                    ProfileSetting.Text("FlickTime", (store, device, c) => store.stickSlots[device].RSOutput.outputSettings.flickSettings.flickTime.ToString()),
                    ProfileSetting.Text("MinAngleThreshold", (store, device, c) => store.stickSlots[device].RSOutput.outputSettings.flickSettings.minAngleThreshold.ToString()))),
            ProfileSetting.Text("L2OutputCurveMode", (store, device, c) => store.StickOutputCurveString(store.triggerSlots[device].L2CurveMode)),
            ProfileSetting.Text("L2OutputCurveCustom", (store, device, c) => store.triggerSlots[device].L2Bezier.ToString()),
            ProfileSetting.Text("L2TwoStageMode", (store, device, c) => store.triggerSlots[device].L2Output.twoStageMode.ToString()),
            ProfileSetting.Text("R2TwoStageMode", (store, device, c) => store.triggerSlots[device].R2Output.twoStageMode.ToString()),
            ProfileSetting.Text("L2HipFireTime", (store, device, c) => store.triggerSlots[device].L2Output.hipFireMS.ToString()),
            ProfileSetting.Text("R2HipFireTime", (store, device, c) => store.triggerSlots[device].R2Output.hipFireMS.ToString()),
            ProfileSetting.Text("L2TriggerEffect", (store, device, c) => store.triggerSlots[device].L2Output.triggerEffect.ToString()),
            ProfileSetting.Text("R2TriggerEffect", (store, device, c) => store.triggerSlots[device].R2Output.triggerEffect.ToString()),
            ProfileSetting.Text("R2OutputCurveMode", (store, device, c) => store.StickOutputCurveString(store.triggerSlots[device].R2CurveMode)),
            ProfileSetting.Text("R2OutputCurveCustom", (store, device, c) => store.triggerSlots[device].R2Bezier.ToString()),
            ProfileSetting.Text("SXOutputCurveMode", (store, device, c) => store.StickOutputCurveString(store.sixAxisSlots[device].SXCurveMode)),
            ProfileSetting.Text("SXOutputCurveCustom", (store, device, c) => store.sixAxisSlots[device].SXCurve.ToString()),
            ProfileSetting.Text("SZOutputCurveMode", (store, device, c) => store.StickOutputCurveString(store.sixAxisSlots[device].SZCurveMode)),
            ProfileSetting.Text("SZOutputCurveCustom", (store, device, c) => store.sixAxisSlots[device].SZCurve.ToString()),
            ProfileSetting.Text("TrackballMode", (store, device, c) => store.touchpadSlots[device].Trackball.ToString()),
            ProfileSetting.Text("TrackballFriction", (store, device, c) => store.touchpadSlots[device].TrackballFriction.ToString()),
            ProfileSetting.Text("TouchRelMouseRotation", (store, device, c) => Convert.ToInt32(store.touchpadSlots[device].RelMouse.rotation * 180.0 / Math.PI).ToString()),
            ProfileSetting.Text("TouchRelMouseMinThreshold", (store, device, c) => store.touchpadSlots[device].RelMouse.minThreshold.ToString()),
            // Isolate as a group. More readable for this???
            //if (false)
            ProfileSetting.Group("TouchpadMouseStick",
                ProfileSetting.Text("DeadZone", (store, device, c) => store.touchpadSlots[device].MouseStick.deadZone.ToString()),
                ProfileSetting.Text("MaxZone", (store, device, c) => store.touchpadSlots[device].MouseStick.maxZone.ToString()),
                ProfileSetting.Text("OutputStick", (store, device, c) => store.touchpadSlots[device].MouseStick.outputStick.ToString()),
                ProfileSetting.Text("OutputStickAxes", (store, device, c) => store.touchpadSlots[device].MouseStick.outputStickDir.ToString()),
                ProfileSetting.Text("AntiDeadX", (store, device, c) => store.touchpadSlots[device].MouseStick.antiDeadX.ToString()),
                ProfileSetting.Text("AntiDeadY", (store, device, c) => store.touchpadSlots[device].MouseStick.antiDeadY.ToString()),
                ProfileSetting.Text("Invert", (store, device, c) => store.touchpadSlots[device].MouseStick.inverted.ToString()),
                ProfileSetting.Text("MaxOutput", (store, device, c) => store.touchpadSlots[device].MouseStick.maxOutput.ToString()),
                ProfileSetting.Text("MaxOutputEnabled", (store, device, c) => store.touchpadSlots[device].MouseStick.maxOutputEnabled.ToString()),
                ProfileSetting.Text("VerticalScale", (store, device, c) => store.touchpadSlots[device].MouseStick.vertScale.ToString()),
                ProfileSetting.Text("OutputCurve", (store, device, c) => store.touchpadSlots[device].MouseStick.outputCurve.ToString()),
                ProfileSetting.Text("Rotation", (store, device, c) => Convert.ToInt32(store.touchpadSlots[device].MouseStick.rotationRad * 180.0 / Math.PI).ToString()),
                ProfileSetting.Group("SmoothingSettings",
                    ProfileSetting.Text("SmoothingMethod", (store, device, c) => store.touchpadSlots[device].MouseStick.smoothingMethod.ToString()),
                    ProfileSetting.Text("SmoothingMinCutoff", (store, device, c) => store.touchpadSlots[device].MouseStick.minCutoff.ToString()),
                    ProfileSetting.Text("SmoothingBeta", (store, device, c) => store.touchpadSlots[device].MouseStick.beta.ToString()))),
            ProfileSetting.Text("TouchpadButtonMode", (store, device, c) => store.touchpadSlots[device].ButtonMode.ToString()),
            // Start of DualSense specific settings
            // xmlDSRumbleGroupElement.AppendChild();
            ProfileSetting.Group("DualSenseControllerSettings",
                ProfileSetting.Group("RumbleSettings",
                    ProfileSetting.Text("EmulationMode", (store, device, c) => store.dualSenseSlots[device].RumbleEmulationMode.ToString()),
                    ProfileSetting.Text("EnableGenericRumbleRescale", (store, device, c) => store.dualSenseSlots[device].UseGenericRumbleRescale.ToString()),
                    ProfileSetting.Text("HapticPowerLevel", (store, device, c) => store.dualSenseSlots[device].HapticPowerLevel.ToString()))),
            //
            // End of DualSense specific settings
        };
    }
}
