#nullable enable

using System.Xml;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.ProfileSections
{
    /// <summary>
    /// 雑多の設定（ChargingType、MouseAcceleration、LaunchProgram、DinputOnly、StartTouchpadOff、UseTPforControls、UseSAforMouse）の読込。UseTPforControls の有無は、後の節が使うため文脈に書き戻す。
    /// <c>LegacyProfileXmlSerializer.LoadProfile</c> から移したもの。移動のみで、要素の順序・既定値・例外の扱いは変えない。
    /// </summary>
    internal sealed class ProfileMiscLoadSection : IProfileLoadSection
    {
        public void Load(ProfileLoadContext context)
        {
            BackingStore store = context.Store;
            XmlDocument m_Xdoc = context.Xdoc;
            string rootname = context.RootName;
            int device = context.Device;
            LightbarDS4WinInfo lightInfo = context.LightInfo;
            DS4Windows.Core.Abstractions.IProfileSettingsService _profileSettings = context.ProfileSettings;
            IProfileLoadTarget control = context.Control;
            bool missingSetting = context.MissingSetting;
            XmlNode Item;

            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ChargingType"); int.TryParse(Item.InnerText, out lightInfo.chargingType); }
            catch { missingSetting = true; }
            try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/MouseAcceleration"); bool.TryParse(Item.InnerText, out store.buttonMouseInfos[device].mouseAccel); }
            catch { missingSetting = true; }


            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LaunchProgram");
                store.basicSlots[device].LaunchProgram = Item.InnerText;
            }
            catch { store.basicSlots[device].LaunchProgram = string.Empty; missingSetting = true; }

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/DinputOnly");
                bool.TryParse(Item.InnerText, out bool dinputOnly);
                store.basicSlots[device].DinputOnly = dinputOnly;
            }
            catch { store.basicSlots[device].DinputOnly = false; missingSetting = true; }

            bool oldUseDInputOnly = _profileSettings.UseDInputOnlyArray[device];

            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/StartTouchpadOff");
                { bool.TryParse(Item.InnerText, out bool parsedValue); store.touchpadSlots[device].StartOff = parsedValue; };
                if (store.touchpadSlots[device].StartOff == true) control.StartTPOff(device);
            }
            catch { store.touchpadSlots[device].StartOff = false; missingSetting = true; }

            // Fallback lookup if TouchpadOutMode is not set
            bool tpForControlsPresent = false;
            XmlNode xmlUseTPForControlsElement =
                m_Xdoc.SelectSingleNode("/" + rootname + "/UseTPforControls");
            tpForControlsPresent = xmlUseTPForControlsElement != null;
            if (tpForControlsPresent)
            {
                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/UseTPforControls");
                    if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                    {
                        if (temp) store.touchpadSlots[device].OutputMode = TouchpadOutMode.Controls;
                    }
                }
                catch { store.touchpadSlots[device].OutputMode = TouchpadOutMode.Mouse; }
            }

            // Fallback lookup if GyroOutMode is not set
            try
            {
                Item = m_Xdoc.SelectSingleNode("/" + rootname + "/UseSAforMouse");
                if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                {
                    if (temp) store.gyroSlots[device].OutputMode = GyroOutMode.Mouse;
                }
            }
            catch { store.gyroSlots[device].OutputMode = GyroOutMode.Controls; }

            context.TpForControlsPresent = tpForControlsPresent;
            context.MissingSetting = missingSetting;
        }
    }
}
