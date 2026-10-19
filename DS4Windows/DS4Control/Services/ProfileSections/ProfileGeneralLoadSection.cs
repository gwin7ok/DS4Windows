using System;
using System.Xml;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase8-Step4b W2（Phase8-Step4b-Plan.md §3.2）: 全般・ライトバー・振動・バッテリー・タッチパッドのジッター補正（<c>touchToggle</c>〜<c>touchpadJitterCompensation</c>）の読込。
    /// <c>LegacyProfileXmlSerializer.LoadProfile</c> から移したもの。移動のみで、要素の順序・既定値・例外の扱いは変えない。
    /// </summary>
    internal sealed class ProfileGeneralLoadSection : IProfileLoadSection
    {
        public void Load(ProfileLoadContext context)
        {
            BackingStore store = context.Store;
            XmlDocument m_Xdoc = context.Xdoc;
            string rootname = context.RootName;
            int device = context.Device;
            LightbarSettingInfo lightbarSettings = context.LightbarSettings;
            LightbarDS4WinInfo lightInfo = context.LightInfo;
            bool missingSetting = context.MissingSetting;
            XmlNode Item;

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/touchToggle"); Boolean.TryParse(Item.InnerText, out store.enableTouchToggle[device]); }
            catch { missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/idleDisconnectTimeout");
                if (int.TryParse(Item?.InnerText ?? "", out int temp))
                {
                    store.idleDisconnectTimeout[device] = temp;
                }
            }
            catch { missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/outputDataToDS4"); Boolean.TryParse(Item.InnerText, out store.enableOutputDataToDS4[device]); }
            catch { missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LightbarMode");
                string tempMode = Item.InnerText;
                lightbarSettings.mode = GetLightbarModeType(tempMode);
            }
            catch { lightbarSettings.mode = LightbarMode.DS4Win; missingSetting = true; }

            //New method for saving color
            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/Color");
                string[] colors;
                if (!string.IsNullOrEmpty(Item.InnerText))
                    colors = Item.InnerText.Split(',');
                else
                    colors = new string[0];

                lightInfo.m_Led.red = byte.Parse(colors[0]);
                lightInfo.m_Led.green = byte.Parse(colors[1]);
                lightInfo.m_Led.blue = byte.Parse(colors[2]);
            }
            catch { missingSetting = true; }

            if (m_Xdoc.SelectSingleNode("/" + rootname + "/Color") == null)
            {
                //Old method of color saving
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/Red"); Byte.TryParse(Item.InnerText, out lightInfo.m_Led.red); }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/Green"); Byte.TryParse(Item.InnerText, out lightInfo.m_Led.green); }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/Blue"); Byte.TryParse(Item.InnerText, out lightInfo.m_Led.blue); }
                catch { missingSetting = true; }
            }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RumbleBoost"); Byte.TryParse(Item.InnerText, out store.rumble[device]); }
            catch { missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RumbleAutostopTime"); Int32.TryParse(Item.InnerText, out store.rumbleAutostopTime[device]); }
            catch { missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ledAsBatteryIndicator"); Boolean.TryParse(Item.InnerText, out lightInfo.ledAsBattery); }
            catch { missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/FlashType"); Byte.TryParse(Item.InnerText, out lightInfo.flashType); }
            catch { missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/flashBatteryAt"); Int32.TryParse(Item.InnerText, out lightInfo.flashAt); }
            catch { missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/touchSensitivity"); Byte.TryParse(Item.InnerText, out store.touchSensitivity[device]); }
            catch { missingSetting = true; }

            //New method for saving color
            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LowColor");
                string[] colors;
                if (!string.IsNullOrEmpty(Item.InnerText))
                    colors = Item.InnerText.Split(',');
                else
                    colors = new string[0];

                lightInfo.m_LowLed.red = byte.Parse(colors[0]);
                lightInfo.m_LowLed.green = byte.Parse(colors[1]);
                lightInfo.m_LowLed.blue = byte.Parse(colors[2]);
            }
            catch { missingSetting = true; }

            if (m_Xdoc.SelectSingleNode("/" + rootname + "/LowColor") == null)
            {
                //Old method of color saving
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LowRed"); byte.TryParse(Item.InnerText, out lightInfo.m_LowLed.red); }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LowGreen"); byte.TryParse(Item.InnerText, out lightInfo.m_LowLed.green); }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LowBlue"); byte.TryParse(Item.InnerText, out lightInfo.m_LowLed.blue); }
                catch { missingSetting = true; }
            }

            //New method for saving color
            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ChargingColor");
                string[] colors;
                if (!string.IsNullOrEmpty(Item.InnerText))
                    colors = Item.InnerText.Split(',');
                else
                    colors = new string[0];

                lightInfo.m_ChargingLed.red = byte.Parse(colors[0]);
                lightInfo.m_ChargingLed.green = byte.Parse(colors[1]);
                lightInfo.m_ChargingLed.blue = byte.Parse(colors[2]);
            }
            catch { missingSetting = true; }

            if (m_Xdoc.SelectSingleNode("/" + rootname + "/ChargingColor") == null)
            {
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ChargingRed"); Byte.TryParse(Item.InnerText, out lightInfo.m_ChargingLed.red); }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ChargingGreen"); Byte.TryParse(Item.InnerText, out lightInfo.m_ChargingLed.green); }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ChargingBlue"); Byte.TryParse(Item.InnerText, out lightInfo.m_ChargingLed.blue); }
                catch { missingSetting = true; }
            }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/FlashColor");
                string[] colors;
                if (!string.IsNullOrEmpty(Item.InnerText))
                    colors = Item.InnerText.Split(',');
                else
                    colors = new string[0];
                lightInfo.m_FlashLed.red = byte.Parse(colors[0]);
                lightInfo.m_FlashLed.green = byte.Parse(colors[1]);
                lightInfo.m_FlashLed.blue = byte.Parse(colors[2]);
            }
            catch { missingSetting = true; }

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/touchpadJitterCompensation"); bool.TryParse(Item.InnerText, out store.touchpadJitterCompensation[device]); }
            catch { missingSetting = true; }

            context.MissingSetting = missingSetting;
        }

        private static LightbarMode GetLightbarModeType(string modeString)
        {
            LightbarMode result = LightbarMode.DS4Win;
            switch (modeString)
            {
                case "DS4Win":
                    result = LightbarMode.DS4Win;
                    break;
                case "Passthru":
                    result = LightbarMode.Passthru;
                    break;
                default:
                    break;
            }

            return result;
        }
    }
}
