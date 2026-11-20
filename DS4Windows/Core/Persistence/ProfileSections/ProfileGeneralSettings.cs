#nullable enable
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.ProfileSections
{
    /// <summary>
    /// 全般・ライトバー・振動・バッテリー・タッチパッドのジッター補正（<c>touchToggle</c>〜<c>touchpadJitterCompensation</c>）の定義の並び。
    /// 並びの順が、保存する要素の順序。色とライトバーのモードは、読み込みが節に手書きのため、保存だけの定義にしている。
    /// </summary>
    internal static class ProfileGeneralSettings
    {
        public static readonly ProfileSetting[] All =
        {
            ProfileSetting.Bool("touchToggle", c => c.Store.basicSlots[c.Device].EnableTouchToggle, (c, v) => c.Store.basicSlots[c.Device].EnableTouchToggle = v),
            // idleDisconnectTimeout がないときは、欠けた設定の印を付けない（従来の動き）
            ProfileSetting.Int("idleDisconnectTimeout", c => c.Store.basicSlots[c.Device].IdleDisconnectTimeout, (c, v) => c.Store.basicSlots[c.Device].IdleDisconnectTimeout = v, marksMissing: false),
            ProfileSetting.Bool("outputDataToDS4", c => c.Store.basicSlots[c.Device].EnableOutputDataToDS4, (c, v) => c.Store.basicSlots[c.Device].EnableOutputDataToDS4 = v),
            ProfileSetting.SaveOnly("Color", c => Rgb(c.LightInfo.m_Led)),
            ProfileSetting.Byte("RumbleBoost", c => c.Store.rumbleSlots[c.Device].Boost, (c, v) => c.Store.rumbleSlots[c.Device].Boost = v),
            ProfileSetting.Int("RumbleAutostopTime", c => c.Store.rumbleSlots[c.Device].AutostopTime, (c, v) => c.Store.rumbleSlots[c.Device].AutostopTime = v),
            ProfileSetting.SaveOnly("LightbarMode", c => c.LightbarSettings.mode == LightbarMode.Passthru ? "Passthru" : "DS4Win"),
            ProfileSetting.Bool("ledAsBatteryIndicator", c => c.LightInfo.ledAsBattery, (c, v) => c.LightInfo.ledAsBattery = v),
            ProfileSetting.Byte("FlashType", c => c.LightInfo.flashType, (c, v) => c.LightInfo.flashType = v),
            ProfileSetting.Int("flashBatteryAt", c => c.LightInfo.flashAt, (c, v) => c.LightInfo.flashAt = v),
            ProfileSetting.Byte("touchSensitivity", c => c.Store.touchpadSlots[c.Device].TouchSensitivity, (c, v) => c.Store.touchpadSlots[c.Device].TouchSensitivity = v),
            ProfileSetting.SaveOnly("LowColor", c => Rgb(c.LightInfo.m_LowLed)),
            ProfileSetting.SaveOnly("ChargingColor", c => Rgb(c.LightInfo.m_ChargingLed)),
            ProfileSetting.SaveOnly("FlashColor", c => Rgb(c.LightInfo.m_FlashLed)),
            ProfileSetting.Bool("touchpadJitterCompensation", c => c.Store.touchpadSlots[c.Device].JitterCompensation, (c, v) => c.Store.touchpadSlots[c.Device].JitterCompensation = v),
        };

        /// <summary>定義のある要素を読み込む。欠けた設定として印を付けるべき要素があれば true を返す。</summary>
        public static bool LoadAll(ProfileLoadContext context)
        {
            bool missing = false;
            foreach (ProfileSetting setting in All)
            {
                missing |= setting.Load(context);
            }

            return missing;
        }

        private static string Rgb(DS4Color color) => color.red + "," + color.green + "," + color.blue;
    }
}
