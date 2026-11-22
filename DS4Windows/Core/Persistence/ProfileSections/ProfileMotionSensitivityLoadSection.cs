using System;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.ProfileSections
{
    /// <summary>
    /// SX/SZ（モーションの不感帯）・感度（SXDeadZone〜）の読込。
    /// 要素の順序・既定値・欠けた設定の扱いは、読み込みの変異ゴールデン（<c>ProfileLoadMutationGoldenTests</c>）が固定している。
    /// </summary>
    internal sealed class ProfileMotionSensitivityLoadSection : IProfileLoadSection
    {
        public void Load(ProfileLoadContext context)
        {
            BackingStore store = context.Store;
            int device = context.Device;
            string path(string name) => "/" + context.RootName + "/" + name;

            context.ReadLegacy(path("SXDeadZone"), t => { double.TryParse(t, out double v); store.sixAxisSlots[device].SXDeadZone = v; },
                () => store.sixAxisSlots[device].SXDeadZone = BackingStore.DEFAULT_SX_TILT_DEADZONE);
            context.ReadLegacy(path("SZDeadZone"), t => { double.TryParse(t, out double v); store.sixAxisSlots[device].SZDeadZone = v; },
                () => store.sixAxisSlots[device].SZDeadZone = BackingStore.DEFAULT_SX_TILT_DEADZONE);

            // 最大域・不感帯の内側は、百分率の整数で保存されている（0〜1 の割合へ換える）
            static double Ratio(string text)
            {
                int.TryParse(text, out int v);
                return Math.Min(Math.Max(v * 0.01, 0.0), 1.0);
            }

            context.ReadLegacy(path("SXMaxZone"), t => store.sixAxisSlots[device].SXMaxZone = Ratio(t), () => store.sixAxisSlots[device].SXMaxZone = 1.0);
            context.ReadLegacy(path("SZMaxZone"), t => store.sixAxisSlots[device].SZMaxZone = Ratio(t), () => store.sixAxisSlots[device].SZMaxZone = 1.0);
            context.ReadLegacy(path("SXAntiDeadZone"), t => store.sixAxisSlots[device].SXAntiDeadZone = Ratio(t), () => store.sixAxisSlots[device].SXAntiDeadZone = 0.0);
            context.ReadLegacy(path("SZAntiDeadZone"), t => store.sixAxisSlots[device].SZAntiDeadZone = Ratio(t), () => store.sixAxisSlots[device].SZAntiDeadZone = 0.0);

            // 感度は 6 つの値を「|」（古い形式は「,」）で並べる。要素が足りないと、途中まで代入したところで例外になり、欠けた設定になる（代入した分は残る）
            static double Sens(string text, double min, double max) => double.TryParse(text, out double v) ? Math.Clamp(v, min, max) : 1;

            context.ReadLegacy(path("Sensitivity"), t =>
            {
                string[] s = t.Split('|');
                if (s.Length == 1) s = t.Split(',');

                store.stickSlots[device].LSSens = Sens(s[0], 0.1, 5.0);
                store.stickSlots[device].RSSens = Sens(s[1], 0.1, 5.0);
                store.triggerSlots[device].L2Sens = Sens(s[2], 0.1, 10.0);
                store.triggerSlots[device].R2Sens = Sens(s[3], 0.1, 10.0);
                store.sixAxisSlots[device].SXSens = Sens(s[4], 0.0, 5.0);
                store.sixAxisSlots[device].SZSens = Sens(s[5], 0.0, 5.0);
            });
        }
    }
}
