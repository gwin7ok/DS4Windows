using System;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.ProfileSections
{
    /// <summary>
    /// トリガー（L2/R2）の基本設定、スティックの回転・不感帯・最大域・外周・ボタンのマウス・レインボー（LeftTriggerMiddle〜）の読込。
    /// 要素の順序・既定値・欠けた設定の扱いは、読み込みの変異ゴールデン（<c>ProfileLoadMutationGoldenTests</c>）が固定している。
    /// 「ない」ときに既定値へ戻して欠けた設定の印を付ける項目は <c>ReadLegacy</c>、「ない・解析できない」ときに何もしない項目は <c>ReadInt</c> などで読む。
    /// </summary>
    internal sealed class ProfileTriggerStickBasicLoadSection : IProfileLoadSection
    {
        public void Load(ProfileLoadContext context)
        {
            BackingStore store = context.Store;
            int device = context.Device;
            LightbarDS4WinInfo lightInfo = context.LightInfo;
            string path(string name) => "/" + context.RootName + "/" + name;

            context.ReadLegacy(path("LeftTriggerMiddle"), t => byte.TryParse(t, out store.triggerSlots[device].L2Mod.deadZone));
            context.ReadLegacy(path("RightTriggerMiddle"), t => byte.TryParse(t, out store.triggerSlots[device].R2Mod.deadZone));

            context.ReadLegacy(path("L2AntiDeadZone"), t => int.TryParse(t, out store.triggerSlots[device].L2Mod.antiDeadZone), () => store.triggerSlots[device].L2Mod.antiDeadZone = 0);
            context.ReadLegacy(path("R2AntiDeadZone"), t => int.TryParse(t, out store.triggerSlots[device].R2Mod.antiDeadZone), () => store.triggerSlots[device].R2Mod.antiDeadZone = 0);

            context.ReadLegacy(path("L2MaxZone"), t => { int.TryParse(t, out int v); store.triggerSlots[device].L2Mod.maxZone = Math.Min(Math.Max(v, 0), 100); },
                () => store.triggerSlots[device].L2Mod.maxZone = 100);
            context.ReadLegacy(path("R2MaxZone"), t => { int.TryParse(t, out int v); store.triggerSlots[device].R2Mod.maxZone = Math.Min(Math.Max(v, 0), 100); },
                () => store.triggerSlots[device].R2Mod.maxZone = 100);

            // 実数は double.Parse（解析に失敗すると例外 → 欠けた設定）
            context.ReadLegacy(path("L2MaxOutput"), t => store.triggerSlots[device].L2Mod.maxOutput = Math.Min(Math.Max(double.Parse(t), 0.0), 100.0),
                () => store.triggerSlots[device].L2Mod.maxOutput = 100.0);
            context.ReadLegacy(path("R2MaxOutput"), t => store.triggerSlots[device].R2Mod.maxOutput = Math.Min(Math.Max(double.Parse(t), 0.0), 100.0),
                () => store.triggerSlots[device].R2Mod.maxOutput = 100.0);

            // 回転は度で保存されている。ラジアンへ換える
            context.ReadLegacy(path("LSRotation"), t => { int.TryParse(t, out int deg); store.stickSlots[device].LSRotation = Math.Min(Math.Max(deg, -180), 180) * Math.PI / 180.0; },
                () => store.stickSlots[device].LSRotation = 0.0);
            context.ReadLegacy(path("RSRotation"), t => { int.TryParse(t, out int deg); store.stickSlots[device].RSRotation = Math.Min(Math.Max(deg, -180), 180) * Math.PI / 180.0; },
                () => store.stickSlots[device].RSRotation = 0.0);

            context.ReadLegacy(path("LSFuzz"), t => { int.TryParse(t, out int v); store.stickSlots[device].LSMod.fuzz = Math.Min(Math.Max(v, 0), 100); },
                () => store.stickSlots[device].LSMod.fuzz = StickDeadZoneInfo.DEFAULT_FUZZ);
            context.ReadLegacy(path("RSFuzz"), t => { int.TryParse(t, out int v); store.stickSlots[device].RSMod.fuzz = Math.Min(Math.Max(v, 0), 100); },
                () => store.stickSlots[device].RSMod.fuzz = StickDeadZoneInfo.DEFAULT_FUZZ);

            context.ReadLegacy(path("ButtonMouseSensitivity"), t => int.TryParse(t, out store.buttonMouseInfos[device].buttonSensitivity));
            context.ReadLegacy(path("ButtonMouseOffset"), t => double.TryParse(t, out store.buttonMouseInfos[device].mouseVelocityOffset));
            context.ReadLegacy(path("ButtonMouseVerticalScale"), t => { int.TryParse(t, out int v); store.buttonMouseInfos[device].buttonVerticalScale = Math.Min(Math.Max(v, 0), 500) * 0.01; });

            context.ReadLegacy(path("Rainbow"), t => double.TryParse(t, out lightInfo.rainbow), () => lightInfo.rainbow = 0);
            context.ReadLegacy(path("MaxSatRainbow"), t => { int.TryParse(t, out int v); lightInfo.maxRainbowSat = Math.Max(0, Math.Min(100, v)) / 100.0; },
                () => lightInfo.maxRainbowSat = 1.0);

            context.ReadLegacy(path("LSDeadZone"), t => { int.TryParse(t, out int v); store.stickSlots[device].LSMod.deadZone = Math.Min(Math.Max(v, 0), 127); },
                () => store.stickSlots[device].LSMod.deadZone = 10);
            context.ReadLegacy(path("RSDeadZone"), t => { int.TryParse(t, out int v); store.stickSlots[device].RSMod.deadZone = Math.Min(Math.Max(v, 0), 127); },
                () => store.stickSlots[device].RSMod.deadZone = 10);

            context.ReadLegacy(path("LSAntiDeadZone"), t => int.TryParse(t, out store.stickSlots[device].LSMod.antiDeadZone), () => store.stickSlots[device].LSMod.antiDeadZone = 20);
            context.ReadLegacy(path("RSAntiDeadZone"), t => int.TryParse(t, out store.stickSlots[device].RSMod.antiDeadZone), () => store.stickSlots[device].RSMod.antiDeadZone = 20);

            context.ReadLegacy(path("LSMaxZone"), t => { int.TryParse(t, out int v); store.stickSlots[device].LSMod.maxZone = Math.Min(Math.Max(v, 0), 100); },
                () => store.stickSlots[device].LSMod.maxZone = 100);
            context.ReadLegacy(path("RSMaxZone"), t => { int.TryParse(t, out int v); store.stickSlots[device].RSMod.maxZone = Math.Min(Math.Max(v, 0), 100); },
                () => store.stickSlots[device].RSMod.maxZone = 100);

            // 「ない・解析できない」ときは何もしない（旧来の try/catch の catch は、例外が起きないため、実行されなかった）
            context.ReadDouble(context.Xdoc, path("LSVerticalScale"), 0.0, 200.0, v => store.stickSlots[device].LSMod.verticalScale = v);

            context.ReadLegacy(path("LSMaxOutput"), t => store.stickSlots[device].LSMod.maxOutput = Math.Min(Math.Max(double.Parse(t), 0.0), 100.0),
                () => store.stickSlots[device].LSMod.maxOutput = 100.0);

            context.ReadBool(context.Xdoc, path("LSMaxOutputForce"), v => store.stickSlots[device].LSMod.maxOutputForce = v);

            context.ReadLegacy(path("RSMaxOutput"), t => store.stickSlots[device].RSMod.maxOutput = Math.Min(Math.Max(double.Parse(t), 0.0), 100.0),
                () => store.stickSlots[device].RSMod.maxOutput = 100);

            context.ReadBool(context.Xdoc, path("RSMaxOutputForce"), v => store.stickSlots[device].RSMod.maxOutputForce = v);

            context.ReadInt(context.Xdoc, path("LSOuterBindDead"), 0, 100, v => store.stickSlots[device].LSMod.outerBindDeadZone = v);
            context.ReadInt(context.Xdoc, path("RSOuterBindDead"), 0, 100, v => store.stickSlots[device].RSMod.outerBindDeadZone = v);

            context.ReadBool(context.Xdoc, path("LSOuterBindInvert"), v => store.stickSlots[device].LSMod.outerBindInvert = v);
            context.ReadBool(context.Xdoc, path("RSOuterBindInvert"), v => store.stickSlots[device].RSMod.outerBindInvert = v);

            context.ReadDouble(context.Xdoc, path("RSVerticalScale"), 0.0, 200.0, v => store.stickSlots[device].RSMod.verticalScale = v);

            context.ReadEnum<StickDeadZoneInfo.DeadZoneType>(context.Xdoc, path("LSDeadZoneType"), v => store.stickSlots[device].LSMod.deadzoneType = v);
        }
    }
}
