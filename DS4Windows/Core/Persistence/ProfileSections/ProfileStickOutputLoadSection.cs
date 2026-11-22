using System;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.ProfileSections
{
    /// <summary>
    /// BT のポーリング・スティックの出力（曲線、四角形、アンチスナップバック、出力モード、出力設定）の読込。
    /// 要素の順序・既定値・欠けた設定の扱いは、読み込みの変異ゴールデン（<c>ProfileLoadMutationGoldenTests</c>）が固定している。
    /// </summary>
    internal sealed class ProfileStickOutputLoadSection : IProfileLoadSection
    {
        public void Load(ProfileLoadContext context)
        {
            BackingStore store = context.Store;
            int device = context.Device;
            string path(string name) => "/" + context.RootName + "/" + name;

            context.ReadLegacy(path("BTPollRate"), t =>
            {
                int.TryParse(t, out int temp);
                store.basicSlots[device].BTPollRate = (temp >= 0 && temp <= 16) ? temp : 4;
            }, () => store.basicSlots[device].BTPollRate = 4);

            // xxOutputCurveCustom は、曲線のモードが 6（カスタム）のときのため、xxOutputCurveMode より先に読む
            context.ReadLegacy(path("LSOutputCurveCustom"), t => store.stickSlots[device].LsBezier.CustomDefinition = t);
            context.ReadLegacy(path("LSOutputCurveMode"), t => store.stickSlots[device].ApplyLSCurveMode(store.StickOutputCurveId(t)), () => store.stickSlots[device].ApplyLSCurveMode(0));

            context.ReadLegacy(path("RSOutputCurveCustom"), t => store.stickSlots[device].RsBezier.CustomDefinition = t);
            context.ReadLegacy(path("RSOutputCurveMode"), t => store.stickSlots[device].ApplyRSCurveMode(store.StickOutputCurveId(t)), () => store.stickSlots[device].ApplyRSCurveMode(0));

            context.ReadLegacy(path("LSSquareStick"), t => bool.TryParse(t, out store.stickSlots[device].SquStick.lsMode), () => store.stickSlots[device].SquStick.lsMode = false);

            context.ReadLegacy(path("SquareStickRoundness"), t => double.TryParse(t, out store.stickSlots[device].SquStick.lsRoundness), () => store.stickSlots[device].SquStick.lsRoundness = 5.0);
            context.ReadLegacy(path("SquareRStickRoundness"), t => double.TryParse(t, out store.stickSlots[device].SquStick.rsRoundness), () => store.stickSlots[device].SquStick.rsRoundness = 5.0);

            context.ReadLegacy(path("RSSquareStick"), t => bool.TryParse(t, out store.stickSlots[device].SquStick.rsMode), () => store.stickSlots[device].SquStick.rsMode = false);

            context.ReadLegacy(path("LSAntiSnapback"), t => bool.TryParse(t, out store.stickSlots[device].LSAntiSnapback.enabled), () => store.stickSlots[device].LSAntiSnapback.enabled = StickAntiSnapbackInfo.DEFAULT_ENABLED);
            context.ReadLegacy(path("RSAntiSnapback"), t => bool.TryParse(t, out store.stickSlots[device].RSAntiSnapback.enabled), () => store.stickSlots[device].RSAntiSnapback.enabled = StickAntiSnapbackInfo.DEFAULT_ENABLED);

            context.ReadLegacy(path("LSAntiSnapbackDelta"), t => double.TryParse(t, out store.stickSlots[device].LSAntiSnapback.delta), () => store.stickSlots[device].LSAntiSnapback.delta = StickAntiSnapbackInfo.DEFAULT_DELTA);
            context.ReadLegacy(path("RSAntiSnapbackDelta"), t => double.TryParse(t, out store.stickSlots[device].RSAntiSnapback.delta), () => store.stickSlots[device].RSAntiSnapback.delta = StickAntiSnapbackInfo.DEFAULT_DELTA);

            context.ReadLegacy(path("LSAntiSnapbackTimeout"), t => int.TryParse(t, out store.stickSlots[device].LSAntiSnapback.timeout), () => store.stickSlots[device].LSAntiSnapback.timeout = StickAntiSnapbackInfo.DEFAULT_TIMEOUT);
            context.ReadLegacy(path("RSAntiSnapbackTimeout"), t => int.TryParse(t, out store.stickSlots[device].RSAntiSnapback.timeout), () => store.stickSlots[device].RSAntiSnapback.timeout = StickAntiSnapbackInfo.DEFAULT_TIMEOUT);

            context.ReadLegacy(path("LSOutputMode"), t => Enum.TryParse(t, out store.stickSlots[device].LSOutput.mode));
            context.ReadLegacy(path("RSOutputMode"), t => Enum.TryParse(t, out store.stickSlots[device].RSOutput.mode));

            // フリックスティックの設定。グループ（xxOutputSettings、FlickStickSettings）がないときは、中の 4 要素もないので、どれも「欠けた設定」になる
            ReadFlick(context, path("LSOutputSettings"), () => store.stickSlots[device].LSOutput.outputSettings.flickSettings);
            ReadFlick(context, path("RSOutputSettings"), () => store.stickSlots[device].RSOutput.outputSettings.flickSettings);
        }

        private static void ReadFlick(ProfileLoadContext context, string settingsPath, Func<FlickStickSettings> settings)
        {
            string flick = settingsPath + "/FlickStickSettings/";
            context.ReadLegacy(flick + "RealWorldCalibration", t => { double.TryParse(t, out double v); settings().realWorldCalibration = v; });
            context.ReadLegacy(flick + "FlickThreshold", t => { double.TryParse(t, out double v); settings().flickThreshold = v; });
            context.ReadLegacy(flick + "FlickTime", t => { double.TryParse(t, out double v); settings().flickTime = v; });
            context.ReadLegacy(flick + "MinAngleThreshold", t => { double.TryParse(t, out double v); settings().minAngleThreshold = v; });
        }
    }
}
