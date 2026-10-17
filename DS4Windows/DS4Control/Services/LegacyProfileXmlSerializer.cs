using DS4Windows.DS4Control;
using DS4Windows.InputDevices;
using DS4Windows.StickModifiers;
using DS4WinWPF.DS4Control.DTOXml;
using DS4WinWPF.DS4Forms.ViewModels;
using Sensorit.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;
using DS4WinWPF;
using DS4WinWPF.ApiDTO;
using WpfScreenHelper;
using static DS4Windows.Mouse;
using static DS4Windows.Util;
using LightbarMacro = DS4WinWPF.DS4Forms.ViewModels.LightbarMacro;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase7-Step10c W4・W5（Q1＝A）: プロファイル（旧形式の XML）の保存・読込。
    /// 旧: <c>BackingStore</c> の <c>LoadProfile</c>／<c>LoadLegacyExtraSettings</c>／<c>ReadLegacyElement</c>／<c>axisOutputCurveId</c>／<c>OutContDeviceId</c>／
    /// <c>GetLightbarModeType</c>／<c>PortOldGyroSettings</c>（W4。読込側）と、保存側（W5）。移動のみで、ロジック・ログ・要素の順序は変えない。
    /// <c>BackingStore</c> の公開状態を <c>store.</c> で読み書きする。プロファイルの置き場所は <see cref="DS4Windows.DI.IPathService"/>、
    /// 実行時の「DInput のみ」の配列は <see cref="DS4Windows.DI.IProfileSettingsService"/> から得る。
    /// 排他は呼び出し側（<c>ProfileXmlStore.XmlIoLock</c>）が行う。
    /// </summary>
    public class LegacyProfileXmlSerializer
    {
        private readonly BackingStore store;
        private readonly DS4Windows.DI.IPathService _pathService;
        private readonly DS4Windows.DI.IProfileSettingsService _profileSettings;
        private readonly XmlDocument m_Xdoc = new XmlDocument(); // 旧 BackingStore.m_Xdoc と同じ使い方（排他は呼び出し側のロック）

        public LegacyProfileXmlSerializer(BackingStore store, DS4Windows.DI.IPathService pathService, DS4Windows.DI.IProfileSettingsService profileSettings)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
        }

        private int axisOutputCurveId(string name)
        {
            return store.stickOutputCurveId(name);
        }

        private OutContType OutContDeviceId(string name)
        {
            OutContType id = OutContType.X360;
            switch (name)
            {
                case "None":
                case "X360": id = OutContType.X360; break;
                case "DS4": id = OutContType.DS4; break;
                default: break;
            }

            return id;
        }

        private void PortOldGyroSettings(int device)
        {
            if (store.gyroOutMode[device] == GyroOutMode.None)
            {
                store.gyroOutMode[device] = GyroOutMode.Controls;
            }
        }

        private LightbarMode GetLightbarModeType(string modeString)
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

        public bool LoadProfile(int device, bool launchprogram, ControlService control,
                string propath = "", bool xinputChange = true, bool postLoad = true)
        {
            bool Loaded = true;
            Dictionary<DS4Controls, DS4KeyType> customMapKeyTypes = new Dictionary<DS4Controls, DS4KeyType>();
            Dictionary<DS4Controls, UInt16> customMapKeys = new Dictionary<DS4Controls, UInt16>();
            Dictionary<DS4Controls, X360Controls> customMapButtons = new Dictionary<DS4Controls, X360Controls>();
            Dictionary<DS4Controls, String> customMapMacros = new Dictionary<DS4Controls, String>();
            Dictionary<DS4Controls, String> customMapExtras = new Dictionary<DS4Controls, String>();
            Dictionary<DS4Controls, DS4KeyType> shiftCustomMapKeyTypes = new Dictionary<DS4Controls, DS4KeyType>();
            Dictionary<DS4Controls, UInt16> shiftCustomMapKeys = new Dictionary<DS4Controls, UInt16>();
            Dictionary<DS4Controls, X360Controls> shiftCustomMapButtons = new Dictionary<DS4Controls, X360Controls>();
            Dictionary<DS4Controls, String> shiftCustomMapMacros = new Dictionary<DS4Controls, String>();
            Dictionary<DS4Controls, String> shiftCustomMapExtras = new Dictionary<DS4Controls, String>();
            string rootname = "DS4Windows";
            bool missingSetting = false;
            string profilepath;
            if (propath == "")
                profilepath = _pathService.AppDataPath + @"\Profiles\" + store.profilePath[device] + ".xml";
            else
                profilepath = propath;

            bool xinputPlug = false;
            bool xinputStatus = false;

            if (File.Exists(profilepath))
            {
                XmlNode Item;

                using FileStream fileStream = new FileStream(profilepath, FileMode.Open, FileAccess.Read);
                ProfileMigration tmpMigration = new ProfileMigration(fileStream);
                // Do not perform automatic migrations at startup. Load profile text
                // as-is via the migration helper's reader if available.
                if (tmpMigration.ProfileReader != null)
                {
                    m_Xdoc.Load(tmpMigration.ProfileReader);
                }
                else
                {
                    Loaded = false;
                }
                tmpMigration.Close();

                if (m_Xdoc.SelectSingleNode(rootname) == null)
                {
                    rootname = "DS4Windows";
                    missingSetting = true;
                }

                if (device < ControllerLimits.MaxControllerCount)
                {
                    DS4LightBar.forcelight[device] = false;
                    DS4LightBar.forcedFlash[device] = 0;
                }

                OutContType oldContType = store.ActiveOutDevType[device];
                LightbarSettingInfo lightbarSettings = store.lightbarSettingInfo[device];
                LightbarDS4WinInfo lightInfo = lightbarSettings.ds4winSettings;

                // Make sure to reset currently set profile values before parsing
                store.ResetProfile(device);
                store.ResetMouseProperties(device, control);
                // Reset some Mapping properties before attempting to load different
                // profile
                control.PreLoadReset(device);

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
                // Phase6-Step12: 旧形式の読込で欠けていた 10 要素。要素が無い（古いプロファイル）、または値が不正なときは、
                // ResetProfile で設定済みの既定値のままにする（missingSetting は立てない）。
                LoadLegacyExtraSettings(device, rootname);

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/lowerRCOn"); bool.TryParse(Item.InnerText, out store.lowerRCOn[device]); }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/tapSensitivity"); byte.TryParse(Item.InnerText, out store.tapSensitivity[device]); }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/doubleTap"); bool.TryParse(Item.InnerText, out store.doubleTap[device]); }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/scrollSensitivity"); int.TryParse(Item.InnerText, out store.scrollSensitivity[device]); }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchpadInvert"); int temp = 0; int.TryParse(Item.InnerText, out temp); store.touchpadInvert[device] = Math.Min(Math.Max(temp, 0), 3); }
                catch { store.touchpadInvert[device] = 0; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchpadClickPassthru"); bool.TryParse(Item.InnerText, out store.touchClickPassthru[device]); }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LeftTriggerMiddle"); byte.TryParse(Item.InnerText, out store.l2ModInfo[device].deadZone); }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RightTriggerMiddle"); byte.TryParse(Item.InnerText, out store.r2ModInfo[device].deadZone); }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2AntiDeadZone"); int.TryParse(Item.InnerText, out store.l2ModInfo[device].antiDeadZone); }
                catch { store.l2ModInfo[device].antiDeadZone = 0; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2AntiDeadZone"); int.TryParse(Item.InnerText, out store.r2ModInfo[device].antiDeadZone); }
                catch { store.r2ModInfo[device].antiDeadZone = 0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2MaxZone"); int temp = 100;
                    int.TryParse(Item.InnerText, out temp);
                    store.l2ModInfo[device].maxZone = Math.Min(Math.Max(temp, 0), 100);
                }
                catch { store.l2ModInfo[device].maxZone = 100; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2MaxZone"); int temp = 100;
                    int.TryParse(Item.InnerText, out temp);
                    store.r2ModInfo[device].maxZone = Math.Min(Math.Max(temp, 0), 100);
                }
                catch { store.r2ModInfo[device].maxZone = 100; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2MaxOutput"); double temp = 100.0;
                    temp = double.Parse(Item.InnerText);
                    store.l2ModInfo[device].maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                }
                catch { store.l2ModInfo[device].maxOutput = 100.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2MaxOutput"); double temp = 100.0;
                    temp = double.Parse(Item.InnerText);
                    store.r2ModInfo[device].maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                }
                catch { store.r2ModInfo[device].maxOutput = 100.0; missingSetting = true; }

                try
                {
                    // Take a rotation angle (degrees) and convert to radians
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSRotation"); int tempDegrees = 0;
                    int.TryParse(Item.InnerText, out tempDegrees);
                    tempDegrees = Math.Min(Math.Max(tempDegrees, -180), 180);
                    store.LSRotation[device] = tempDegrees * Math.PI / 180.0;
                }
                catch { store.LSRotation[device] = 0.0; missingSetting = true; }

                try
                {
                    // Take a rotation angle (degrees) and convert to radians
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSRotation"); int tempDegrees = 0;
                    int.TryParse(Item.InnerText, out tempDegrees);
                    tempDegrees = Math.Min(Math.Max(tempDegrees, -180), 180);
                    store.RSRotation[device] = tempDegrees * Math.PI / 180.0;
                }
                catch { store.RSRotation[device] = 0.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSFuzz"); int temp = 0;
                    int.TryParse(Item.InnerText, out temp);
                    temp = Math.Min(Math.Max(temp, 0), 100);
                    store.lsModInfo[device].fuzz = temp;
                }
                catch { store.lsModInfo[device].fuzz = StickDeadZoneInfo.DEFAULT_FUZZ; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSFuzz"); int temp = 0;
                    int.TryParse(Item.InnerText, out temp);
                    temp = Math.Min(Math.Max(temp, 0), 100);
                    store.rsModInfo[device].fuzz = temp;
                }
                catch { store.rsModInfo[device].fuzz = StickDeadZoneInfo.DEFAULT_FUZZ; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ButtonMouseSensitivity"); int.TryParse(Item.InnerText, out store.buttonMouseInfos[device].buttonSensitivity); }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ButtonMouseOffset"); double.TryParse(Item.InnerText, out store.buttonMouseInfos[device].mouseVelocityOffset); }
                catch { missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ButtonMouseVerticalScale");
                    int.TryParse(Item.InnerText, out int temp);
                    store.buttonMouseInfos[device].buttonVerticalScale = Math.Min(Math.Max(temp, 0), 500) * 0.01;
                }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/Rainbow"); double.TryParse(Item.InnerText, out lightInfo.rainbow); }
                catch { lightInfo.rainbow = 0; missingSetting = true; }
                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/MaxSatRainbow");
                    int.TryParse(Item.InnerText, out int temp);
                    lightInfo.maxRainbowSat = Math.Max(0, Math.Min(100, temp)) / 100.0;
                }
                catch { lightInfo.maxRainbowSat = 1.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSDeadZone");
                    int.TryParse(Item.InnerText, out int temp);
                    temp = Math.Min(Math.Max(temp, 0), 127);
                    store.lsModInfo[device].deadZone = temp;
                }
                catch
                {
                    store.lsModInfo[device].deadZone = 10; missingSetting = true;
                }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSDeadZone");
                    int.TryParse(Item.InnerText, out int temp);
                    temp = Math.Min(Math.Max(temp, 0), 127);
                    store.rsModInfo[device].deadZone = temp;
                }
                catch
                {
                    store.rsModInfo[device].deadZone = 10; missingSetting = true;
                }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSAntiDeadZone"); int.TryParse(Item.InnerText, out store.lsModInfo[device].antiDeadZone); }
                catch { store.lsModInfo[device].antiDeadZone = 20; missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSAntiDeadZone"); int.TryParse(Item.InnerText, out store.rsModInfo[device].antiDeadZone); }
                catch { store.rsModInfo[device].antiDeadZone = 20; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSMaxZone"); int temp = 100;
                    int.TryParse(Item.InnerText, out temp);
                    store.lsModInfo[device].maxZone = Math.Min(Math.Max(temp, 0), 100);
                }
                catch { store.lsModInfo[device].maxZone = 100; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSMaxZone"); int temp = 100;
                    int.TryParse(Item.InnerText, out temp);
                    store.rsModInfo[device].maxZone = Math.Min(Math.Max(temp, 0), 100);
                }
                catch { store.rsModInfo[device].maxZone = 100; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSVerticalScale");
                    if (double.TryParse(Item?.InnerText ?? "", out double temp))
                    {
                        store.lsModInfo[device].verticalScale = Math.Min(Math.Max(temp, 0.0), 200.0);
                    }
                }
                catch { store.lsModInfo[device].verticalScale = StickDeadZoneInfo.DEFAULT_VERTICAL_SCALE; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSMaxOutput"); double temp = 100.0;
                    temp = double.Parse(Item.InnerText);
                    store.lsModInfo[device].maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                }
                catch { store.lsModInfo[device].maxOutput = 100.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSMaxOutputForce");
                    if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                    {
                        store.lsModInfo[device].maxOutputForce = temp;
                    }
                }
                catch { store.lsModInfo[device].maxOutputForce = StickDeadZoneInfo.DEFAULT_MAXOUTPUT_FORCE; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSMaxOutput"); double temp = 100.0;
                    temp = double.Parse(Item.InnerText);
                    store.rsModInfo[device].maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                }
                catch { store.rsModInfo[device].maxOutput = 100; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSMaxOutputForce");
                    if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                    {
                        store.rsModInfo[device].maxOutputForce = temp;
                    }
                }
                catch { store.rsModInfo[device].maxOutputForce = StickDeadZoneInfo.DEFAULT_MAXOUTPUT_FORCE; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOuterBindDead");
                    if (int.TryParse(Item?.InnerText ?? "", out int temp))
                    {
                        temp = Math.Min(Math.Max(temp, 0), 100);
                        store.lsModInfo[device].outerBindDeadZone = temp;
                    }
                }
                catch { store.lsModInfo[device].outerBindDeadZone = StickDeadZoneInfo.DEFAULT_OUTER_BIND_DEAD; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOuterBindDead");
                    if (int.TryParse(Item?.InnerText ?? "", out int temp))
                    {
                        temp = Math.Min(Math.Max(temp, 0), 100);
                        store.rsModInfo[device].outerBindDeadZone = temp;
                    }
                }
                catch { store.rsModInfo[device].outerBindDeadZone = StickDeadZoneInfo.DEFAULT_OUTER_BIND_DEAD; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOuterBindInvert");
                    if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                    {
                        store.lsModInfo[device].outerBindInvert = temp;
                    }
                }
                catch { store.lsModInfo[device].outerBindInvert = StickDeadZoneInfo.DEFAULT_OUTER_BIND_INVERT; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOuterBindInvert");
                    if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                    {
                        store.rsModInfo[device].outerBindInvert = temp;
                    }
                }
                catch { store.rsModInfo[device].outerBindInvert = StickDeadZoneInfo.DEFAULT_OUTER_BIND_INVERT; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSVerticalScale");
                    if (double.TryParse(Item?.InnerText ?? "", out double temp))
                    {
                        store.rsModInfo[device].verticalScale = Math.Min(Math.Max(temp, 0.0), 200.0);
                    }
                }
                catch { store.rsModInfo[device].verticalScale = StickDeadZoneInfo.DEFAULT_VERTICAL_SCALE; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSDeadZoneType");
                    if (Enum.TryParse(Item?.InnerText ?? "", out StickDeadZoneInfo.DeadZoneType temp))
                    {
                        store.lsModInfo[device].deadzoneType = temp;
                    }
                }
                catch { }

                bool lsAxialDeadGroup = false;
                XmlNode lsAxialDeadElement =
                    m_Xdoc.SelectSingleNode("/" + rootname + "/LSAxialDeadOptions");
                lsAxialDeadGroup = lsAxialDeadElement != null;

                if (lsAxialDeadGroup)
                {
                    try
                    {
                        Item = lsAxialDeadElement.SelectSingleNode("DeadZoneX");
                        int.TryParse(Item.InnerText, out int temp);
                        temp = Math.Min(Math.Max(temp, 0), 127);
                        store.lsModInfo[device].xAxisDeadInfo.deadZone = temp;
                    }
                    catch { }

                    try
                    {
                        Item = lsAxialDeadElement.SelectSingleNode("DeadZoneY");
                        int.TryParse(Item.InnerText, out int temp);
                        temp = Math.Min(Math.Max(temp, 0), 127);
                        store.lsModInfo[device].yAxisDeadInfo.deadZone = temp;
                    }
                    catch { }

                    try
                    {
                        Item = lsAxialDeadElement.SelectSingleNode("MaxZoneX");
                        int.TryParse(Item.InnerText, out int temp);
                        store.lsModInfo[device].xAxisDeadInfo.maxZone = Math.Min(Math.Max(temp, 0), 100);
                    }
                    catch { }

                    try
                    {
                        Item = lsAxialDeadElement.SelectSingleNode("MaxZoneY");
                        int.TryParse(Item.InnerText, out int temp);
                        store.lsModInfo[device].yAxisDeadInfo.maxZone = Math.Min(Math.Max(temp, 0), 100);
                    }
                    catch { }

                    try
                    {
                        Item = lsAxialDeadElement.SelectSingleNode("AntiDeadZoneX");
                        int.TryParse(Item.InnerText, out int temp);
                        store.lsModInfo[device].xAxisDeadInfo.antiDeadZone = Math.Min(Math.Max(temp, 0), 100);
                    }
                    catch { }

                    try
                    {
                        Item = lsAxialDeadElement.SelectSingleNode("AntiDeadZoneY");
                        int.TryParse(Item.InnerText, out int temp);
                        store.lsModInfo[device].yAxisDeadInfo.antiDeadZone = Math.Min(Math.Max(temp, 0), 100);
                    }
                    catch { }

                    try
                    {
                        Item = lsAxialDeadElement.SelectSingleNode("MaxOutputX");
                        double.TryParse(Item.InnerText, out double temp);
                        store.lsModInfo[device].xAxisDeadInfo.maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                    }
                    catch { }

                    try
                    {
                        Item = lsAxialDeadElement.SelectSingleNode("MaxOutputY");
                        double.TryParse(Item.InnerText, out double temp);
                        store.lsModInfo[device].yAxisDeadInfo.maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                    }
                    catch { }
                }

                XmlNode lsDeltaAccelElement =
                    m_Xdoc.SelectSingleNode($"/{rootname}/LSDeltaAccelSettings");
                if (lsDeltaAccelElement != null)
                {
                    try
                    {
                        Item = lsDeltaAccelElement.SelectSingleNode("Enabled");
                        if (bool.TryParse(Item.InnerText ?? "", out bool temp))
                        {
                            store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.enabled
                                = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = lsDeltaAccelElement.SelectSingleNode("Multiplier");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.multiplier =
                                Math.Clamp(temp, 0.0, 10.0);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = lsDeltaAccelElement.SelectSingleNode("MaxTravel");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.maxTravel =
                                Math.Clamp(temp, 0.0, 1.0);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = lsDeltaAccelElement.SelectSingleNode("MinTravel");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.minTravel =
                                Math.Clamp(temp, 0.0, 1.0);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = lsDeltaAccelElement.SelectSingleNode("EasingDuration");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.easingDuration =
                                Math.Clamp(temp, 0.0, 600.0);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = lsDeltaAccelElement.SelectSingleNode("MinFactor");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            store.lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.minfactor =
                                Math.Clamp(temp, 1.0, 10.0);
                        }
                    }
                    catch { }
                }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSDeadZoneType");
                    if (Enum.TryParse(Item?.InnerText ?? "", out StickDeadZoneInfo.DeadZoneType temp))
                    {
                        store.rsModInfo[device].deadzoneType = temp;
                    }
                }
                catch { }

                bool rsAxialDeadGroup = false;
                XmlNode rsAxialDeadElement =
                    m_Xdoc.SelectSingleNode("/" + rootname + "/RSAxialDeadOptions");
                rsAxialDeadGroup = rsAxialDeadElement != null;

                if (rsAxialDeadGroup)
                {
                    try
                    {
                        Item = rsAxialDeadElement.SelectSingleNode("DeadZoneX");
                        int.TryParse(Item.InnerText, out int temp);
                        temp = Math.Min(Math.Max(temp, 0), 127);
                        store.rsModInfo[device].xAxisDeadInfo.deadZone = temp;
                    }
                    catch { }

                    try
                    {
                        Item = rsAxialDeadElement.SelectSingleNode("DeadZoneY");
                        int.TryParse(Item.InnerText, out int temp);
                        temp = Math.Min(Math.Max(temp, 0), 127);
                        store.rsModInfo[device].yAxisDeadInfo.deadZone = temp;
                    }
                    catch { }

                    try
                    {
                        Item = rsAxialDeadElement.SelectSingleNode("MaxZoneX");
                        int.TryParse(Item.InnerText, out int temp);
                        store.rsModInfo[device].xAxisDeadInfo.maxZone = Math.Min(Math.Max(temp, 0), 100);
                    }
                    catch { }

                    try
                    {
                        Item = rsAxialDeadElement.SelectSingleNode("MaxZoneY");
                        int.TryParse(Item.InnerText, out int temp);
                        store.rsModInfo[device].yAxisDeadInfo.maxZone = Math.Min(Math.Max(temp, 0), 100);
                    }
                    catch { }

                    try
                    {
                        Item = rsAxialDeadElement.SelectSingleNode("AntiDeadZoneX");
                        int.TryParse(Item.InnerText, out int temp);
                        store.rsModInfo[device].xAxisDeadInfo.antiDeadZone = Math.Min(Math.Max(temp, 0), 100);
                    }
                    catch { }

                    try
                    {
                        Item = rsAxialDeadElement.SelectSingleNode("AntiDeadZoneY");
                        int.TryParse(Item.InnerText, out int temp);
                        store.rsModInfo[device].yAxisDeadInfo.antiDeadZone = Math.Min(Math.Max(temp, 0), 100);
                    }
                    catch { }

                    try
                    {
                        Item = rsAxialDeadElement.SelectSingleNode("MaxOutputX");
                        double.TryParse(Item.InnerText, out double temp);
                        store.rsModInfo[device].xAxisDeadInfo.maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                    }
                    catch { }

                    try
                    {
                        Item = rsAxialDeadElement.SelectSingleNode("MaxOutputY");
                        double.TryParse(Item.InnerText, out double temp);
                        store.rsModInfo[device].yAxisDeadInfo.maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                    }
                    catch { }
                }

                XmlNode rsDeltaAccelElement =
                    m_Xdoc.SelectSingleNode($"/{rootname}/RSDeltaAccelSettings");
                if (lsDeltaAccelElement != null)
                {
                    try
                    {
                        Item = rsDeltaAccelElement.SelectSingleNode("Enabled");
                        if (bool.TryParse(Item.InnerText ?? "", out bool temp))
                        {
                            store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.enabled
                                = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = rsDeltaAccelElement.SelectSingleNode("Multiplier");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.multiplier =
                                Math.Clamp(temp, 0.0, 10.0);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = rsDeltaAccelElement.SelectSingleNode("MaxTravel");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.maxTravel =
                                Math.Clamp(temp, 0.0, 1.0);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = rsDeltaAccelElement.SelectSingleNode("MinTravel");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.minTravel =
                                Math.Clamp(temp, 0.0, 1.0);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = rsDeltaAccelElement.SelectSingleNode("EasingDuration");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.easingDuration =
                                Math.Clamp(temp, 0.0, 600.0);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = rsDeltaAccelElement.SelectSingleNode("MinFactor");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            store.rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.minfactor =
                                Math.Clamp(temp, 1.0, 10.0);
                        }
                    }
                    catch { }
                }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXDeadZone");
                    double.TryParse(Item.InnerText, out store.SXDeadzone[device]);
                }
                catch { store.SXDeadzone[device] = BackingStore.DEFAULT_SX_TILT_DEADZONE; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZDeadZone");
                    double.TryParse(Item.InnerText, out store.SZDeadzone[device]);
                }
                catch { store.SZDeadzone[device] = BackingStore.DEFAULT_SX_TILT_DEADZONE; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXMaxZone");
                    int temp = 0;
                    int.TryParse(Item.InnerText, out temp);
                    store.SXMaxzone[device] = Math.Min(Math.Max(temp * 0.01, 0.0), 1.0);
                }
                catch { store.SXMaxzone[device] = 1.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZMaxZone");
                    int temp = 0;
                    int.TryParse(Item.InnerText, out temp);
                    store.SZMaxzone[device] = Math.Min(Math.Max(temp * 0.01, 0.0), 1.0);
                }
                catch { store.SZMaxzone[device] = 1.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXAntiDeadZone");
                    int temp = 0;
                    int.TryParse(Item.InnerText, out temp);
                    store.SXAntiDeadzone[device] = Math.Min(Math.Max(temp * 0.01, 0.0), 1.0);
                }
                catch { store.SXAntiDeadzone[device] = 0.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZAntiDeadZone");
                    int temp = 0;
                    int.TryParse(Item.InnerText, out temp);
                    store.SZAntiDeadzone[device] = Math.Min(Math.Max(temp * 0.01, 0.0), 1.0);
                }
                catch { store.SZAntiDeadzone[device] = 0.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/Sensitivity");
                    string[] s = Item.InnerText.Split('|');
                    if (s.Length == 1)
                        s = Item.InnerText.Split(',');

                    if (double.TryParse(s[0], out store.LSSens[device]))
                    {
                        store.LSSens[device] = Math.Clamp(store.LSSens[device], 0.1, 5.0);
                    }
                    else
                    {
                        store.LSSens[device] = 1;
                    }

                    if (double.TryParse(s[1], out store.RSSens[device]))
                    {
                        store.RSSens[device] = Math.Clamp(store.RSSens[device], 0.1, 5.0);
                    }
                    else
                    {
                        store.RSSens[device] = 1;
                    }

                    if (double.TryParse(s[2], out store.l2Sens[device]))
                    {
                        store.l2Sens[device] = Math.Clamp(store.l2Sens[device], 0.1, 10.0);
                    }
                    else
                    {
                        store.l2Sens[device] = 1;
                    }

                    if (double.TryParse(s[3], out store.r2Sens[device]))
                    {
                        store.r2Sens[device] = Math.Clamp(store.r2Sens[device], 0.1, 10.0);
                    }
                    else
                    {
                        store.r2Sens[device] = 1;
                    }

                    if (double.TryParse(s[4], out store.SXSens[device]))
                    {
                        store.SXSens[device] = Math.Clamp(store.SXSens[device], 0.0, 5.0);
                    }
                    else
                    {
                        store.SXSens[device] = 1;
                    }

                    if (double.TryParse(s[5], out store.SZSens[device]))
                    {
                        store.SZSens[device] = Math.Clamp(store.SZSens[device], 0.0, 5.0);
                    }
                    else
                    {
                        store.SZSens[device] = 1;
                    }
                }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ChargingType"); int.TryParse(Item.InnerText, out lightInfo.chargingType); }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/MouseAcceleration"); bool.TryParse(Item.InnerText, out store.buttonMouseInfos[device].mouseAccel); }
                catch { missingSetting = true; }

                int shiftM = 0;
                if (m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftModifier") != null)
                    int.TryParse(m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftModifier").InnerText, out shiftM);

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LaunchProgram");
                    store.launchProgram[device] = Item.InnerText;
                }
                catch { store.launchProgram[device] = string.Empty; missingSetting = true; }

                if (launchprogram == true && store.launchProgram[device] != string.Empty)
                {
                    string programPath = store.launchProgram[device];
                    bool procFound = false;
                    bool handled = false;
                    try
                    {
                        var inspector = store.ProcessInspector;
                        if (inspector != null)
                        {
                            procFound = inspector.IsProcessRunning(programPath);
                            handled = true;
                        }
                    }
                    catch { }

                    if (!handled)
                    {
                        System.Diagnostics.Process[] localAll = System.Diagnostics.Process.GetProcesses();
                        for (int procInd = 0, procsLen = localAll.Length; !procFound && procInd < procsLen; procInd++)
                        {
                            try
                            {
                                string temp = localAll[procInd].MainModule.FileName;
                                if (temp == programPath)
                                {
                                    procFound = true;
                                }
                            }
                            // Ignore any process for which this information
                            // is not exposed
                            catch { }
                        }
                    }

                    if (!procFound)
                    {
                        Task processTask = new Task(() =>
                        {
                            Thread.Sleep(5000);
                            System.Diagnostics.Process tempProcess = new System.Diagnostics.Process();
                            tempProcess.StartInfo.FileName = programPath;
                            tempProcess.StartInfo.WorkingDirectory = new FileInfo(programPath).Directory.ToString();
                            //tempProcess.StartInfo.UseShellExecute = false;
                            try { tempProcess.Start(); }
                            catch { }
                        });

                        processTask.Start();
                    }
                }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/DinputOnly");
                    bool.TryParse(Item.InnerText, out store.dinputOnly[device]);
                }
                catch { store.dinputOnly[device] = false; missingSetting = true; }

                bool oldUseDInputOnly = _profileSettings.UseDInputOnlyArray[device];

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/StartTouchpadOff");
                    bool.TryParse(Item.InnerText, out store.startTouchpadOff[device]);
                    if (store.startTouchpadOff[device] == true) control.StartTPOff(device);
                }
                catch { store.startTouchpadOff[device] = false; missingSetting = true; }

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
                            if (temp) store.touchOutMode[device] = TouchpadOutMode.Controls;
                        }
                    }
                    catch { store.touchOutMode[device] = TouchpadOutMode.Mouse; }
                }

                // Fallback lookup if GyroOutMode is not set
                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/UseSAforMouse");
                    if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                    {
                        if (temp) store.gyroOutMode[device] = GyroOutMode.Mouse;
                    }
                }
                catch { store.gyroOutMode[device] = GyroOutMode.Controls; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SATriggers"); store.sATriggers[device] = Item.InnerText; }
                catch { store.sATriggers[device] = "-1"; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SATriggerCond"); store.sATriggerCond[device] = BackingStore.SaTriggerCondValue(Item.InnerText); }
                catch { store.sATriggerCond[device] = true; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelEmulationAxis"); SASteeringWheelEmulationAxisType.TryParse(Item.InnerText, out store.sASteeringWheelEmulationAxis[device]); }
                catch { store.sASteeringWheelEmulationAxis[device] = SASteeringWheelEmulationAxisType.None; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelEmulationRange"); int.TryParse(Item.InnerText, out store.sASteeringWheelEmulationRange[device]); }
                catch { store.sASteeringWheelEmulationRange[device] = 360; missingSetting = true; }

                bool sASteeringWheelSmoothingGroup = false;
                XmlNode xmlSASteeringWheelSmoothElement =
                    m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelSmoothingOptions");
                sASteeringWheelSmoothingGroup = xmlSASteeringWheelSmoothElement != null;

                if (sASteeringWheelSmoothingGroup)
                {
                    try
                    {
                        Item = xmlSASteeringWheelSmoothElement.SelectSingleNode("SASteeringWheelUseSmoothing");
                        //Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelUseSmoothing");
                        bool.TryParse(Item.InnerText, out bool temp);
                        store.wheelSmoothInfo[device].Enabled = temp;
                    }
                    catch { store.wheelSmoothInfo[device].Reset(); missingSetting = true; }

                    try
                    {
                        Item = xmlSASteeringWheelSmoothElement.SelectSingleNode("SASteeringWheelSmoothMinCutoff");
                        //Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelSmoothMinCutoff");
                        double.TryParse(Item.InnerText, out double temp);
                        store.wheelSmoothInfo[device].MinCutoff = temp;
                    }
                    catch { store.wheelSmoothInfo[device].MinCutoff = OneEuroFilterPair.DEFAULT_WHEEL_CUTOFF; missingSetting = true; }

                    try
                    {
                        Item = xmlSASteeringWheelSmoothElement.SelectSingleNode("SASteeringWheelSmoothBeta");
                        //Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelSmoothBeta");
                        double.TryParse(Item.InnerText, out double temp);
                        store.wheelSmoothInfo[device].Beta = temp;
                    }
                    catch { store.wheelSmoothInfo[device].Beta = OneEuroFilterPair.DEFAULT_WHEEL_BETA; missingSetting = true; }
                }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelFuzz");
                    int.TryParse(Item.InnerText, out int temp);
                    store.saWheelFuzzValues[device] = temp >= 0 && temp <= 100 ? temp : 0;
                }
                catch { store.saWheelFuzzValues[device] = 0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroOutputMode");
                    string tempMode = Item.InnerText;
                    //gyroOutMode[device] = GetGyroOutModeType(tempMode);
                    Enum.TryParse(tempMode, out store.gyroOutMode[device]);
                }
                catch { PortOldGyroSettings(device); missingSetting = true; }

                XmlNode xmlGyroControlsElement =
                    m_Xdoc.SelectSingleNode("/" + rootname + "/GyroControlsSettings");
                if (xmlGyroControlsElement != null)
                {
                    try
                    {
                        Item = xmlGyroControlsElement.SelectSingleNode("Triggers");
                        if (Item != null)
                        {
                            store.gyroControlsInf[device].triggers = Item.InnerText;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlGyroControlsElement.SelectSingleNode("TriggerCond");
                        if (Item != null)
                        {
                            store.gyroControlsInf[device].triggerCond = BackingStore.SaTriggerCondValue(Item.InnerText);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlGyroControlsElement.SelectSingleNode("TriggerTurns");
                        if (bool.TryParse(Item?.InnerText ?? "", out bool tempTurns))
                        {
                            store.gyroControlsInf[device].triggerTurns = tempTurns;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlGyroControlsElement.SelectSingleNode("Toggle");
                        if (bool.TryParse(Item?.InnerText ?? "", out bool tempToggle))
                        {
                            store.SetGyroControlsToggle(device, tempToggle, control);
                        }
                    }
                    catch { }
                }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickTriggers"); store.sAMouseStickTriggers[device] = Item.InnerText; }
                catch { store.sAMouseStickTriggers[device] = "-1"; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickTriggerCond"); store.sAMouseStickTriggerCond[device] = BackingStore.SaTriggerCondValue(Item.InnerText); }
                catch { store.sAMouseStickTriggerCond[device] = true; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickTriggerTurns"); bool.TryParse(Item.InnerText, out store.gyroMouseStickTriggerTurns[device]); }
                catch { store.gyroMouseStickTriggerTurns[device] = true; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickHAxis"); int temp = 0; int.TryParse(Item.InnerText, out temp); store.gyroMouseStickHorizontalAxis[device] = Math.Min(Math.Max(0, temp), 1); }
                catch { store.gyroMouseStickHorizontalAxis[device] = 0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickDeadZone"); int.TryParse(Item.InnerText, out int temp);
                    store.gyroMStickInfo[device].deadZone = temp;
                }
                catch { store.gyroMStickInfo[device].deadZone = 30; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickMaxZone"); int.TryParse(Item.InnerText, out int temp);
                    store.gyroMStickInfo[device].maxZone = Math.Max(temp, 1);
                }
                catch { store.gyroMStickInfo[device].maxZone = 830; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickOutputStick");
                    if (Enum.TryParse(Item?.InnerText ?? "", out GyroMouseStickInfo.OutputStick temp))
                    {
                        store.gyroMStickInfo[device].outputStick = temp;
                    }
                }
                catch { }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickOutputStickAxes");
                    if (Enum.TryParse(Item?.InnerText ?? "", out GyroMouseStickInfo.OutputStickAxes temp))
                    {
                        store.gyroMStickInfo[device].outputStickDir = temp;
                    }
                }
                catch { }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickAntiDeadX"); double.TryParse(Item.InnerText, out double temp);
                    store.gyroMStickInfo[device].antiDeadX = temp;
                }
                catch { store.gyroMStickInfo[device].antiDeadX = 0.4; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickAntiDeadY"); double.TryParse(Item.InnerText, out double temp);
                    store.gyroMStickInfo[device].antiDeadY = temp;
                }
                catch { store.gyroMStickInfo[device].antiDeadY = 0.4; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickInvert"); uint.TryParse(Item.InnerText, out store.gyroMStickInfo[device].inverted); }
                catch { store.gyroMStickInfo[device].inverted = 0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickToggle");
                    bool.TryParse(Item.InnerText, out bool temp);
                    store.SetGyroMouseStickToggle(device, temp, control);
                }
                catch
                {
                    store.SetGyroMouseStickToggle(device, false, control);
                    missingSetting = true;
                }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickMaxOutput"); double temp = 100.0;
                    temp = double.Parse(Item.InnerText);
                    store.gyroMStickInfo[device].maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                }
                catch { store.gyroMStickInfo[device].maxOutput = 100.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickMaxOutputEnabled");
                    bool.TryParse(Item.InnerText, out bool temp);
                    store.gyroMStickInfo[device].maxOutputEnabled = temp;
                }
                catch { store.gyroMStickInfo[device].maxOutputEnabled = false; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickVerticalScale"); int.TryParse(Item.InnerText, out store.gyroMStickInfo[device].vertScale); }
                catch { store.gyroMStickInfo[device].vertScale = 100; missingSetting = true; }

                bool gyroMStickSmoothingGroup = false;
                XmlNode xmlGyroMStickSmoothingElement =
                    m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickSmoothingSettings");
                gyroMStickSmoothingGroup = xmlGyroMStickSmoothingElement != null;

                if (gyroMStickSmoothingGroup)
                {
                    try
                    {
                        Item = xmlGyroMStickSmoothingElement.SelectSingleNode("UseSmoothing");
                        bool.TryParse(Item.InnerText, out bool tempSmoothing);
                        store.gyroMStickInfo[device].useSmoothing = tempSmoothing;
                    }
                    catch { store.gyroMStickInfo[device].useSmoothing = false; missingSetting = true; }

                    try
                    {
                        Item = xmlGyroMStickSmoothingElement.SelectSingleNode("SmoothingMethod");
                        string temp = Item?.InnerText ?? GyroMouseStickInfo.DEFAULT_SMOOTH_TECHNIQUE;
                        store.gyroMStickInfo[device].DetermineSmoothMethod(temp);
                    }
                    catch { store.gyroMStickInfo[device].ResetSmoothing(); missingSetting = true; }

                    try
                    {
                        Item = xmlGyroMStickSmoothingElement.SelectSingleNode("SmoothingWeight");
                        int temp = 0; int.TryParse(Item.InnerText, out temp);
                        store.gyroMStickInfo[device].smoothWeight = Math.Min(Math.Max(0.0, Convert.ToDouble(temp * 0.01)), 1.0);
                    }
                    catch { store.gyroMStickInfo[device].smoothWeight = 0.5; missingSetting = true; }

                    try
                    {
                        Item = xmlGyroMStickSmoothingElement.SelectSingleNode("SmoothingMinCutoff");
                        double.TryParse(Item.InnerText, out double temp);
                        store.gyroMStickInfo[device].minCutoff = Math.Min(Math.Max(0.0, temp), 100.0);
                    }
                    catch { store.gyroMStickInfo[device].minCutoff = GyroMouseStickInfo.DEFAULT_MINCUTOFF; missingSetting = true; }

                    try
                    {
                        Item = xmlGyroMStickSmoothingElement.SelectSingleNode("SmoothingBeta");
                        double.TryParse(Item.InnerText, out double temp);
                        store.gyroMStickInfo[device].beta = Math.Min(Math.Max(0.0, temp), 1.0);
                    }
                    catch { store.gyroMStickInfo[device].beta = GyroMouseStickInfo.DEFAULT_BETA; missingSetting = true; }
                }
                else
                {
                    missingSetting = true;
                }

                XmlNode xmlGyroSwipeElement =
                    m_Xdoc.SelectSingleNode("/" + rootname + "/GyroSwipeSettings");
                if (xmlGyroSwipeElement != null)
                {
                    try
                    {
                        Item = xmlGyroSwipeElement.SelectSingleNode("DeadZoneX");
                        if (int.TryParse(Item?.InnerText ?? "", out int tempDead))
                        {
                            store.gyroSwipeInfo[device].deadzoneX = tempDead;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlGyroSwipeElement.SelectSingleNode("DeadZoneY");
                        if (int.TryParse(Item?.InnerText ?? "", out int tempDead))
                        {
                            store.gyroSwipeInfo[device].deadzoneY = tempDead;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlGyroSwipeElement.SelectSingleNode("Triggers");
                        if (Item != null)
                        {
                            store.gyroSwipeInfo[device].triggers = Item.InnerText;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlGyroSwipeElement.SelectSingleNode("TriggerCond");
                        if (Item != null)
                        {
                            store.gyroSwipeInfo[device].triggerCond = BackingStore.SaTriggerCondValue(Item.InnerText);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlGyroSwipeElement.SelectSingleNode("TriggerTurns");
                        if (bool.TryParse(Item?.InnerText ?? "", out bool tempTurns))
                        {
                            store.gyroSwipeInfo[device].triggerTurns = tempTurns;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlGyroSwipeElement.SelectSingleNode("XAxis");
                        if (Enum.TryParse(Item?.InnerText ?? "", out GyroDirectionalSwipeInfo.XAxisSwipe tempX))
                        {
                            store.gyroSwipeInfo[device].xAxis = tempX;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlGyroSwipeElement.SelectSingleNode("DelayTime");
                        if (int.TryParse(Item?.InnerText ?? "", out int tempDelay))
                        {
                            store.gyroSwipeInfo[device].delayTime = tempDelay;
                        }
                    }
                    catch { }
                }

                // Check for TouchpadOutputMode if UseTPforControls is not present in profile
                if (!tpForControlsPresent)
                {
                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchpadOutputMode");
                        string tempMode = Item.InnerText;
                        Enum.TryParse(tempMode, out store.touchOutMode[device]);
                    }
                    catch { store.touchOutMode[device] = TouchpadOutMode.Mouse; missingSetting = true; }
                }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchDisInvTriggers");
                    string[] triggers = Item.InnerText.Split(',');
                    int temp = -1;
                    List<int> tempIntList = new List<int>();
                    for (int i = 0, arlen = triggers.Length; i < arlen; i++)
                    {
                        if (int.TryParse(triggers[i], out temp))
                            tempIntList.Add(temp);
                    }

                    store.touchDisInvertTriggers[device] = tempIntList.ToArray();
                }
                catch { store.touchDisInvertTriggers[device] = new int[1] { -1 }; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroSensitivity"); int.TryParse(Item.InnerText, out store.gyroSensitivity[device]); }
                catch { store.gyroSensitivity[device] = 100; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroSensVerticalScale"); int.TryParse(Item.InnerText, out store.gyroSensVerticalScale[device]); }
                catch { store.gyroSensVerticalScale[device] = 100; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroInvert"); int.TryParse(Item.InnerText, out store.gyroInvert[device]); }
                catch { store.gyroInvert[device] = 0; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroTriggerTurns"); bool.TryParse(Item.InnerText, out store.gyroTriggerTurns[device]); }
                catch { store.gyroTriggerTurns[device] = true; missingSetting = true; }

                /*try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroSmoothing"); bool.TryParse(Item.InnerText, out gyroSmoothing[device]); }
                catch { gyroSmoothing[device] = false; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroSmoothingWeight"); int temp = 0; int.TryParse(Item.InnerText, out temp); gyroSmoothWeight[device] = Math.Min(Math.Max(0.0, Convert.ToDouble(temp * 0.01)), 1.0); }
                catch { gyroSmoothWeight[device] = 0.5; missingSetting = true; }
                */

                bool gyroSmoothingGroup = false;
                XmlNode xmlGyroSmoothingElement =
                    m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseSmoothingSettings");
                gyroSmoothingGroup = xmlGyroSmoothingElement != null;

                if (gyroSmoothingGroup)
                {
                    try
                    {
                        Item = xmlGyroSmoothingElement.SelectSingleNode("UseSmoothing");
                        bool.TryParse(Item.InnerText, out bool temp);
                        store.gyroMouseInfo[device].enableSmoothing = temp;
                    }
                    catch { store.gyroMouseInfo[device].enableSmoothing = false; missingSetting = true; }

                    try
                    {
                        Item = xmlGyroSmoothingElement.SelectSingleNode("SmoothingMethod");
                        string temp = Item?.InnerText ?? GyroMouseInfo.DEFAULT_SMOOTH_TECHNIQUE;
                        store.gyroMouseInfo[device].DetermineSmoothMethod(temp);
                    }
                    catch { store.gyroMouseInfo[device].ResetSmoothing(); missingSetting = true; }

                    try
                    {
                        Item = xmlGyroSmoothingElement.SelectSingleNode("SmoothingWeight");
                        int.TryParse(Item.InnerText, out int temp);
                        store.gyroMouseInfo[device].smoothingWeight = Math.Min(Math.Max(0.0, Convert.ToDouble(temp * 0.01)), 1.0);
                    }
                    catch { store.gyroMouseInfo[device].smoothingWeight = 0.5; missingSetting = true; }

                    try
                    {
                        Item = xmlGyroSmoothingElement.SelectSingleNode("SmoothingMinCutoff");
                        double.TryParse(Item.InnerText, out double temp);
                        store.gyroMouseInfo[device].minCutoff = Math.Min(Math.Max(0.0, temp), 100.0);
                    }
                    catch { store.gyroMouseInfo[device].minCutoff = GyroMouseInfo.DEFAULT_MINCUTOFF; missingSetting = true; }

                    try
                    {
                        Item = xmlGyroSmoothingElement.SelectSingleNode("SmoothingBeta");
                        double.TryParse(Item.InnerText, out double temp);
                        store.gyroMouseInfo[device].beta = Math.Min(Math.Max(0.0, temp), 1.0);
                    }
                    catch { store.gyroMouseInfo[device].beta = GyroMouseInfo.DEFAULT_BETA; missingSetting = true; }
                }
                else
                {
                    missingSetting = true;
                }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseHAxis"); int temp = 0; int.TryParse(Item.InnerText, out temp); store.gyroMouseHorizontalAxis[device] = Math.Min(Math.Max(0, temp), 1); }
                catch { store.gyroMouseHorizontalAxis[device] = 0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseDeadZone"); int.TryParse(Item.InnerText, out int temp);
                    store.SetGyroMouseDZ(device, temp, control);
                }
                catch { store.SetGyroMouseDZ(device, MouseCursor.GYRO_MOUSE_DEADZONE, control); missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseMinThreshold");
                    double.TryParse(Item.InnerText, out double temp);
                    temp = Math.Min(Math.Max(temp, 1.0), 40.0);
                    store.gyroMouseInfo[device].minThreshold = temp;
                }
                catch { store.gyroMouseInfo[device].minThreshold = GyroMouseInfo.DEFAULT_MIN_THRESHOLD; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseToggle");
                    bool.TryParse(Item.InnerText, out bool temp);
                    store.SetGyroMouseToggle(device, temp, control);
                }
                catch
                {
                    store.SetGyroMouseToggle(device, false, control);
                    missingSetting = true;
                }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/BTPollRate");
                    int temp = 0;
                    int.TryParse(Item.InnerText, out temp);
                    store.btPollRate[device] = (temp >= 0 && temp <= 16) ? temp : 4;
                }
                catch { store.btPollRate[device] = 4; missingSetting = true; }

                // Note! xxOutputCurveCustom property needs to be read before xxOutputCurveMode property in case the curveMode is value 6
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOutputCurveCustom"); store.lsOutBezierCurveObj[device].CustomDefinition = Item.InnerText; }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOutputCurveMode"); store.setLsOutCurveMode(device, store.stickOutputCurveId(Item.InnerText)); }
                catch { store.setLsOutCurveMode(device, 0); missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOutputCurveCustom"); store.rsOutBezierCurveObj[device].CustomDefinition = Item.InnerText; }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOutputCurveMode"); store.setRsOutCurveMode(device, store.stickOutputCurveId(Item.InnerText)); }
                catch { store.setRsOutCurveMode(device, 0); missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSSquareStick"); bool.TryParse(Item.InnerText, out store.squStickInfo[device].lsMode); }
                catch { store.squStickInfo[device].lsMode = false; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SquareStickRoundness"); double.TryParse(Item.InnerText, out store.squStickInfo[device].lsRoundness); }
                catch { store.squStickInfo[device].lsRoundness = 5.0; missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SquareRStickRoundness"); double.TryParse(Item.InnerText, out store.squStickInfo[device].rsRoundness); }
                catch { store.squStickInfo[device].rsRoundness = 5.0; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSSquareStick"); bool.TryParse(Item.InnerText, out store.squStickInfo[device].rsMode); }
                catch { store.squStickInfo[device].rsMode = false; missingSetting = true; }


                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSAntiSnapback"); bool.TryParse(Item.InnerText, out store.lsAntiSnapbackInfo[device].enabled); }
                catch { store.lsAntiSnapbackInfo[device].enabled = StickAntiSnapbackInfo.DEFAULT_ENABLED; missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSAntiSnapback"); bool.TryParse(Item.InnerText, out store.rsAntiSnapbackInfo[device].enabled); }
                catch { store.rsAntiSnapbackInfo[device].enabled = StickAntiSnapbackInfo.DEFAULT_ENABLED; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSAntiSnapbackDelta"); double.TryParse(Item.InnerText, out store.lsAntiSnapbackInfo[device].delta); }
                catch { store.lsAntiSnapbackInfo[device].delta = StickAntiSnapbackInfo.DEFAULT_DELTA; missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSAntiSnapbackDelta"); double.TryParse(Item.InnerText, out store.rsAntiSnapbackInfo[device].delta); }
                catch { store.rsAntiSnapbackInfo[device].delta = StickAntiSnapbackInfo.DEFAULT_DELTA; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSAntiSnapbackTimeout"); int.TryParse(Item.InnerText, out store.lsAntiSnapbackInfo[device].timeout); }
                catch { store.lsAntiSnapbackInfo[device].timeout = StickAntiSnapbackInfo.DEFAULT_TIMEOUT; missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSAntiSnapbackTimeout"); int.TryParse(Item.InnerText, out store.rsAntiSnapbackInfo[device].timeout); }
                catch { store.rsAntiSnapbackInfo[device].timeout = StickAntiSnapbackInfo.DEFAULT_TIMEOUT; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOutputMode"); Enum.TryParse(Item.InnerText, out store.lsOutputSettings[device].mode); }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOutputMode"); Enum.TryParse(Item.InnerText, out store.rsOutputSettings[device].mode); }
                catch { missingSetting = true; }

                XmlNode xmlLSOutputSettingsElement =
                    m_Xdoc.SelectSingleNode("/" + rootname + "/LSOutputSettings");
                bool lsOutputGroup = xmlLSOutputSettingsElement != null;
                if (lsOutputGroup)
                {
                    bool flickStickLSGroup = false;
                    XmlNode xmlFlickStickLSElement =
                        xmlLSOutputSettingsElement.SelectSingleNode("FlickStickSettings");
                    flickStickLSGroup = xmlFlickStickLSElement != null;

                    if (flickStickLSGroup)
                    {
                        try
                        {
                            Item = xmlFlickStickLSElement.SelectSingleNode("RealWorldCalibration");
                            double.TryParse(Item.InnerText, out double temp);
                            store.lsOutputSettings[device].outputSettings.flickSettings.realWorldCalibration = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlFlickStickLSElement.SelectSingleNode("FlickThreshold");
                            double.TryParse(Item.InnerText, out double temp);
                            store.lsOutputSettings[device].outputSettings.flickSettings.flickThreshold = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlFlickStickLSElement.SelectSingleNode("FlickTime");
                            double.TryParse(Item.InnerText, out double temp);
                            store.lsOutputSettings[device].outputSettings.flickSettings.flickTime = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlFlickStickLSElement.SelectSingleNode("MinAngleThreshold");
                            double.TryParse(Item.InnerText, out double temp);
                            store.lsOutputSettings[device].outputSettings.flickSettings.minAngleThreshold = temp;
                        }
                        catch { missingSetting = true; }

                    }
                    else
                    {
                        missingSetting = true;
                    }
                }
                else
                {
                    missingSetting = true;
                }

                XmlNode xmlRSOutputSettingsElement =
                    m_Xdoc.SelectSingleNode("/" + rootname + "/RSOutputSettings");
                bool rsOutputGroup = xmlRSOutputSettingsElement != null;
                if (rsOutputGroup)
                {
                    bool flickStickRSGroup = false;
                    XmlNode xmlFlickStickRSElement =
                        xmlRSOutputSettingsElement.SelectSingleNode("FlickStickSettings");
                    flickStickRSGroup = xmlFlickStickRSElement != null;

                    if (flickStickRSGroup)
                    {
                        try
                        {
                            Item = xmlFlickStickRSElement.SelectSingleNode("RealWorldCalibration");
                            double.TryParse(Item.InnerText, out double temp);
                            store.rsOutputSettings[device].outputSettings.flickSettings.realWorldCalibration = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlFlickStickRSElement.SelectSingleNode("FlickThreshold");
                            double.TryParse(Item.InnerText, out double temp);
                            store.rsOutputSettings[device].outputSettings.flickSettings.flickThreshold = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlFlickStickRSElement.SelectSingleNode("FlickTime");
                            double.TryParse(Item.InnerText, out double temp);
                            store.rsOutputSettings[device].outputSettings.flickSettings.flickTime = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlFlickStickRSElement.SelectSingleNode("MinAngleThreshold");
                            double.TryParse(Item.InnerText, out double temp);
                            store.rsOutputSettings[device].outputSettings.flickSettings.minAngleThreshold = temp;
                        }
                        catch { missingSetting = true; }
                    }
                    else
                    {
                        missingSetting = true;
                    }
                }
                else
                {
                    missingSetting = true;
                }


                // Start of DualSense specific profile load
                //
                XmlNode xmlDualSenseControllerSettingsElement =
                    m_Xdoc.SelectSingleNode("/" + rootname + "/DualSenseControllerSettings");
                bool dSControllerSettingsGroup = xmlDualSenseControllerSettingsElement != null;
                if (dSControllerSettingsGroup)
                {
                    XmlNode xmlDSRumbleGroupElement =
                        xmlDualSenseControllerSettingsElement.SelectSingleNode("RumbleSettings");
                    bool dSRumbleGroup = xmlDSRumbleGroupElement != null;

                    if (dSRumbleGroup)
                    {
                        try
                        {
                            Item = xmlDSRumbleGroupElement.SelectSingleNode("EmulationMode");
                            DualSenseDevice.RumbleEmulationMode.TryParse(Item.InnerText, out DualSenseDevice.RumbleEmulationMode temp);
                            store.dualSenseRumbleEmulationMode[device] = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlDSRumbleGroupElement.SelectSingleNode("EnableGenericRumbleRescale");
                            bool.TryParse(Item.InnerText, out bool temp);
                            store.useGenericRumbleRescaleForDualSenses[device] = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlDSRumbleGroupElement.SelectSingleNode("HapticPowerLevel");
                            byte.TryParse(Item.InnerText, out byte temp);
                            store.dualSenseHapticPowerLevel[device] = temp;
                        }
                        catch { missingSetting = true; }

                    }
                    else
                    {
                        missingSetting = true;
                    }
                }
                else
                {
                    missingSetting = true;
                }
                //
                // End of DualSense specific profile load

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2OutputCurveCustom"); store.l2OutBezierCurveObj[device].CustomDefinition = Item.InnerText; }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2OutputCurveMode"); store.setL2OutCurveMode(device, axisOutputCurveId(Item.InnerText)); }
                catch { store.setL2OutCurveMode(device, 0); missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2TwoStageMode");
                    if (Enum.TryParse(Item?.InnerText, out TwoStageTriggerMode tempMode))
                    {
                        store.l2OutputSettings[device].TwoStageMode = tempMode;
                    }
                }
                catch { }


                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2HipFireTime");
                    if (int.TryParse(Item?.InnerText, out int temp))
                    {
                        store.l2OutputSettings[device].hipFireMS = Math.Clamp(temp, 0, 5000);
                    }
                }
                catch { }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2TriggerEffect");
                    if (Enum.TryParse(Item?.InnerText, out InputDevices.TriggerEffects tempMode))
                    {
                        store.l2OutputSettings[device].TriggerEffect = tempMode;
                    }
                }
                catch { }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2OutputCurveCustom"); store.r2OutBezierCurveObj[device].CustomDefinition = Item.InnerText; }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2OutputCurveMode"); store.setR2OutCurveMode(device, axisOutputCurveId(Item.InnerText)); }
                catch { store.setR2OutCurveMode(device, 0); missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2TwoStageMode");
                    if (Enum.TryParse(Item?.InnerText, out TwoStageTriggerMode tempMode))
                    {
                        store.r2OutputSettings[device].TwoStageMode = tempMode;
                    }
                }
                catch { }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2HipFireTime");
                    if (int.TryParse(Item?.InnerText, out int temp))
                    {
                        store.r2OutputSettings[device].hipFireMS = Math.Clamp(temp, 0, 5000);
                    }
                }
                catch { }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2TriggerEffect");
                    if (Enum.TryParse(Item?.InnerText, out InputDevices.TriggerEffects tempMode))
                    {
                        store.r2OutputSettings[device].TriggerEffect = tempMode;
                    }
                }
                catch { }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXOutputCurveCustom"); store.sxOutBezierCurveObj[device].CustomDefinition = Item.InnerText; }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXOutputCurveMode"); store.setSXOutCurveMode(device, axisOutputCurveId(Item.InnerText)); }
                catch { store.setSXOutCurveMode(device, 0); missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZOutputCurveCustom"); store.szOutBezierCurveObj[device].CustomDefinition = Item.InnerText; }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZOutputCurveMode"); store.setSZOutCurveMode(device, axisOutputCurveId(Item.InnerText)); }
                catch { store.setSZOutCurveMode(device, 0); missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TrackballMode"); bool.TryParse(Item.InnerText, out store.trackballMode[device]); }
                catch { store.trackballMode[device] = false; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TrackballFriction"); double.TryParse(Item.InnerText, out store.trackballFriction[device]); }
                catch { store.trackballFriction[device] = 10.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchRelMouseRotation");
                    int.TryParse(Item.InnerText, out int temp);
                    temp = Math.Min(Math.Max(temp, -180), 180);
                    store.touchpadRelMouse[device].rotation = temp * Math.PI / 180.0;
                }
                catch { store.touchpadRelMouse[device].rotation = 0.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchRelMouseMinThreshold");
                    double.TryParse(Item.InnerText, out double temp);
                    temp = Math.Min(Math.Max(temp, 1.0), 40.0);
                    store.touchpadRelMouse[device].minThreshold = temp;
                }
                catch { store.touchpadRelMouse[device].minThreshold = TouchpadRelMouseSettings.DEFAULT_MIN_THRESHOLD; missingSetting = true; }


                bool touchMStickGroup = false;
                XmlNode xmlTouchMStickSmoothingElement =
                    m_Xdoc.SelectSingleNode("/" + rootname + "/TouchpadMouseStick");
                touchMStickGroup = xmlTouchMStickSmoothingElement != null;

                if (touchMStickGroup && xmlTouchMStickSmoothingElement.HasChildNodes)
                {
                    //try
                    //{
                    //    Item = xmlGyroMStickSmoothingElement.SelectSingleNode("UseSmoothing");
                    //    if (bool.TryParse(Item?.InnerText ?? "", out bool tempSmoothing))
                    //    {
                    //        gyroMStickInfo[device].useSmoothing = tempSmoothing;
                    //    }
                    //}
                    //catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("DeadZone");
                        if (int.TryParse(Item?.InnerText ?? "", out int temp))
                        {
                            store.touchMStickInfo[device].deadZone = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("MaxZone");
                        if (int.TryParse(Item?.InnerText ?? "", out int temp))
                        {
                            store.touchMStickInfo[device].maxZone = Math.Max(temp, 1);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("OutputStick");
                        if (Enum.TryParse(Item?.InnerText ?? "",
                            out TouchMouseStickInfo.OutputStick temp))
                        {
                            store.touchMStickInfo[device].outputStick = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("OutputStickAxes");
                        if (Item == null)
                        {
                            // Phase6-Step12: 互換。以前の保存（上流の 2022-10-20 以来の不具合）は、軸を 2 つ目の <OutputStick> として書いていた
                            XmlNodeList legacyOutputStickNodes = xmlTouchMStickSmoothingElement.SelectNodes("OutputStick");
                            if (legacyOutputStickNodes != null && legacyOutputStickNodes.Count > 1)
                                Item = legacyOutputStickNodes[1];
                        }
                        if (Enum.TryParse(Item?.InnerText ?? "",
                            out TouchMouseStickInfo.OutputStickAxes temp))
                        {
                            store.touchMStickInfo[device].outputStickDir = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("AntiDeadX");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            store.touchMStickInfo[device].antiDeadX = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("AntiDeadY");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            store.touchMStickInfo[device].antiDeadY = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("Invert");
                        if (uint.TryParse(Item?.InnerText ?? "",
                            out uint temp))
                        {
                            store.touchMStickInfo[device].inverted = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("MaxOutput");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            store.touchMStickInfo[device].maxOutput = Math.Clamp(temp, 0.0, 100.0);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("MaxOutputEnabled");
                        if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                        {
                            store.touchMStickInfo[device].maxOutputEnabled = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("VerticalScale");
                        if (int.TryParse(Item?.InnerText ?? "", out int temp))
                        {
                            store.touchMStickInfo[device].vertScale = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("OutputCurve");
                        if (Enum.TryParse(Item?.InnerText ?? "", out StickOutCurve.Curve temp))
                        {
                            store.touchMStickInfo[device].outputCurve = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("Rotation");
                        if (int.TryParse(Item?.InnerText ?? "", out int temp))
                        {
                            temp = Math.Clamp(temp, -180, 180);
                            store.touchMStickInfo[device].rotationRad = temp * Math.PI / 180.0;
                        }
                    }
                    catch { }


                    bool stickSmoothingGroup = false;
                    XmlNode xmlStickSmoothingElement =
                        xmlTouchMStickSmoothingElement.SelectSingleNode("SmoothingSettings");
                    stickSmoothingGroup = xmlStickSmoothingElement != null;

                    if (stickSmoothingGroup && xmlStickSmoothingElement.HasChildNodes)
                    {
                        //try
                        //{
                        //    Item = xmlGyroMStickSmoothingElement.SelectSingleNode("UseSmoothing");
                        //    bool.TryParse(Item.InnerText, out bool tempSmoothing);
                        //    gyroMStickInfo[device].useSmoothing = tempSmoothing;
                        //}
                        //catch { gyroMStickInfo[device].useSmoothing = false; missingSetting = true; }

                        try
                        {
                            Item = xmlTouchMStickSmoothingElement.SelectSingleNode("SmoothingMethod");
                            if (Enum.TryParse(Item?.InnerText ?? "",
                                out TouchMouseStickInfo.SmoothingMethod temp))
                            {
                                store.touchMStickInfo[device].smoothingMethod = temp;
                            }
                        }
                        catch { }

                        try
                        {
                            Item = xmlTouchMStickSmoothingElement.SelectSingleNode("SmoothingMinCutoff");
                            if (double.TryParse(Item?.InnerText ?? "", out double temp))
                            {
                                store.touchMStickInfo[device].minCutoff = Math.Clamp(temp, 0.0, 100.0);
                            }
                        }
                        catch { }

                        try
                        {
                            Item = xmlTouchMStickSmoothingElement.SelectSingleNode("SmoothingBeta");
                            if (double.TryParse(Item?.InnerText ?? "", out double temp))
                            {
                                store.touchMStickInfo[device].beta = Math.Clamp(temp, 0.0, 1.0);
                            }
                        }
                        catch { }
                    }
                }


                try
                {
                    Item = m_Xdoc.SelectSingleNode($"/{rootname}/TouchpadButtonMode");
                    if (Enum.TryParse(Item?.InnerText ?? "", out TouchButtonActivationMode tempMode))
                    {
                        store.touchpadButtonMode[device] = tempMode;
                    }
                }
                catch { store.touchpadButtonMode[device] = TouchButtonActivationMode.Click; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/OutputContDevice"); store.outputDevType[device] = OutContDeviceId(Item.InnerText); }
                catch { store.outputDevType[device] = OutContType.X360; missingSetting = true; }

                // Only change xinput devices under certain conditions. Avoid
                // performing this upon program startup before loading devices.
                if (xinputChange && device < ControlService.CURRENT_DS4_CONTROLLER_LIMIT)
                {
                    store.CheckOldDevicestatus(device, control, oldContType,
                        out xinputPlug, out xinputStatus);
                }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ProfileActions");
                    store.profileActions[device].Clear();
                    if (!string.IsNullOrEmpty(Item.InnerText))
                    {
                        string[] actionNames = Item.InnerText.Split('/');
                        for (int actIndex = 0, actLen = actionNames.Length; actIndex < actLen; actIndex++)
                        {
                            string tempActionName = actionNames[actIndex];
                            if (!store.profileActions[device].Contains(tempActionName))
                            {
                                store.profileActions[device].Add(tempActionName);
                            }
                        }
                    }
                }
                catch { store.profileActions[device].Clear(); missingSetting = true; }

                foreach (DS4ControlSettings dcs in store.ds4settings[device])
                    dcs.Reset();

                store.containsCustomAction[device] = false;
                store.containsCustomExtras[device] = false;
                store.profileActionCount[device] = store.profileActions[device].Count;
                store.profileActionDict[device].Clear();
                store.profileActionIndexDict[device].Clear();
                foreach (string actionname in store.profileActions[device])
                {
                    store.profileActionDict[device][actionname] = store.GetAction(actionname);
                    store.profileActionIndexDict[device][actionname] = store.GetActionIndexOf(actionname);
                }

                DS4KeyType keyType;
                ushort wvk;

                {
                    XmlNode ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/Control/Button");
                    if (ParentItem != null)
                    {
                        foreach (XmlNode item in ParentItem.ChildNodes)
                        {
                            if (Enum.TryParse(item.Name, out DS4Controls currentControl))
                            {
                                store.UpdateDS4CSetting(device, item.Name, false, store.getX360ControlsByName(item.InnerText), "", DS4KeyType.None, 0);
                                customMapButtons.Add(store.getDS4ControlsByName(item.Name), store.getX360ControlsByName(item.InnerText));
                            }
                        }
                    }

                    ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/Control/Macro");
                    if (ParentItem != null)
                    {
                        foreach (XmlNode item in ParentItem.ChildNodes)
                        {
                            customMapMacros.Add(store.getDS4ControlsByName(item.Name), item.InnerText);
                            string[] skeys;
                            int[] keys;
                            if (!string.IsNullOrEmpty(item.InnerText))
                            {
                                skeys = item.InnerText.Split('/');
                                keys = new int[skeys.Length];
                            }
                            else
                            {
                                skeys = new string[0];
                                keys = new int[0];
                            }

                            for (int i = 0, keylen = keys.Length; i < keylen; i++)
                                keys[i] = int.Parse(skeys[i]);

                            if (Enum.TryParse(item.Name, out DS4Controls currentControl))
                            {
                                store.UpdateDS4CSetting(device, item.Name, false, keys, "", DS4KeyType.None, 0);
                            }
                        }
                    }

                    ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/Control/Key");
                    if (ParentItem != null)
                    {
                        foreach (XmlNode item in ParentItem.ChildNodes)
                        {
                            if (ushort.TryParse(item.InnerText, out wvk) && Enum.TryParse(item.Name, out DS4Controls currentControl))
                            {
                                store.UpdateDS4CSetting(device, item.Name, false, wvk, "", DS4KeyType.None, 0);
                                customMapKeys.Add(store.getDS4ControlsByName(item.Name), wvk);
                            }
                        }
                    }

                    ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/Control/Extras");
                    if (ParentItem != null)
                    {
                        foreach (XmlNode item in ParentItem.ChildNodes)
                        {
                            if (item.InnerText != string.Empty && Enum.TryParse(item.Name, out DS4Controls currentControl))
                            {
                                store.UpdateDS4CExtra(device, item.Name, false, item.InnerText);
                                customMapExtras.Add(store.getDS4ControlsByName(item.Name), item.InnerText);
                            }
                            else
                                ParentItem.RemoveChild(item);
                        }
                    }

                    ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/Control/KeyType");
                    if (ParentItem != null)
                    {
                        foreach (XmlNode item in ParentItem.ChildNodes)
                        {
                            if (item != null)
                            {
                                keyType = DS4KeyType.None;
                                if (item.InnerText.Contains(DS4KeyType.ScanCode.ToString()))
                                    keyType |= DS4KeyType.ScanCode;
                                if (item.InnerText.Contains(DS4KeyType.Toggle.ToString()))
                                    keyType |= DS4KeyType.Toggle;
                                if (item.InnerText.Contains(DS4KeyType.Macro.ToString()))
                                    keyType |= DS4KeyType.Macro;
                                if (item.InnerText.Contains(DS4KeyType.HoldMacro.ToString()))
                                    keyType |= DS4KeyType.HoldMacro;
                                if (item.InnerText.Contains(DS4KeyType.Unbound.ToString()))
                                    keyType |= DS4KeyType.Unbound;

                                if (keyType != DS4KeyType.None && Enum.TryParse(item.Name, out DS4Controls currentControl))
                                {
                                    store.UpdateDS4CKeyType(device, item.Name, false, keyType);
                                    customMapKeyTypes.Add(store.getDS4ControlsByName(item.Name), keyType);
                                }
                            }
                        }
                    }

                    ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftControl/Button");
                    if (ParentItem != null)
                    {
                        foreach (XmlElement item in ParentItem.ChildNodes)
                        {
                            int shiftT = shiftM;
                            if (item.HasAttribute("Trigger"))
                                int.TryParse(item.Attributes["Trigger"].Value, out shiftT);

                            if (Enum.TryParse(item.Name, out DS4Controls currentControl))
                            {
                                store.UpdateDS4CSetting(device, item.Name, true, store.getX360ControlsByName(item.InnerText), "", DS4KeyType.None, shiftT);
                                shiftCustomMapButtons.Add(store.getDS4ControlsByName(item.Name), store.getX360ControlsByName(item.InnerText));
                            }
                        }
                    }

                    ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftControl/Macro");
                    if (ParentItem != null)
                    {
                        foreach (XmlElement item in ParentItem.ChildNodes)
                        {
                            shiftCustomMapMacros.Add(store.getDS4ControlsByName(item.Name), item.InnerText);
                            string[] skeys;
                            int[] keys;
                            if (!string.IsNullOrEmpty(item.InnerText))
                            {
                                skeys = item.InnerText.Split('/');
                                keys = new int[skeys.Length];
                            }
                            else
                            {
                                skeys = new string[0];
                                keys = new int[0];
                            }

                            for (int i = 0, keylen = keys.Length; i < keylen; i++)
                                keys[i] = int.Parse(skeys[i]);

                            int shiftT = shiftM;
                            if (item.HasAttribute("Trigger"))
                                int.TryParse(item.Attributes["Trigger"].Value, out shiftT);

                            if (Enum.TryParse(item.Name, out DS4Controls currentControl))
                            {
                                store.UpdateDS4CSetting(device, item.Name, true, keys, "", DS4KeyType.None, shiftT);
                            }
                        }
                    }

                    ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftControl/Key");
                    if (ParentItem != null)
                    {
                        foreach (XmlElement item in ParentItem.ChildNodes)
                        {
                            if (ushort.TryParse(item.InnerText, out wvk))
                            {
                                int shiftT = shiftM;
                                if (item.HasAttribute("Trigger"))
                                    int.TryParse(item.Attributes["Trigger"].Value, out shiftT);

                                if (Enum.TryParse(item.Name, out DS4Controls currentControl))
                                {
                                    store.UpdateDS4CSetting(device, item.Name, true, wvk, "", DS4KeyType.None, shiftT);
                                    shiftCustomMapKeys.Add(store.getDS4ControlsByName(item.Name), wvk);
                                }
                            }
                        }
                    }

                    ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftControl/Extras");
                    if (ParentItem != null)
                    {
                        foreach (XmlElement item in ParentItem.ChildNodes)
                        {
                            if (item.InnerText != string.Empty)
                            {
                                if (Enum.TryParse(item.Name, out DS4Controls currentControl))
                                {
                                    store.UpdateDS4CExtra(device, item.Name, true, item.InnerText);
                                    shiftCustomMapExtras.Add(store.getDS4ControlsByName(item.Name), item.InnerText);
                                }
                            }
                            else
                                ParentItem.RemoveChild(item);
                        }
                    }

                    ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftControl/KeyType");
                    if (ParentItem != null)
                    {
                        foreach (XmlElement item in ParentItem.ChildNodes)
                        {
                            if (item != null)
                            {
                                keyType = DS4KeyType.None;
                                if (item.InnerText.Contains(DS4KeyType.ScanCode.ToString()))
                                    keyType |= DS4KeyType.ScanCode;
                                if (item.InnerText.Contains(DS4KeyType.Toggle.ToString()))
                                    keyType |= DS4KeyType.Toggle;
                                if (item.InnerText.Contains(DS4KeyType.Macro.ToString()))
                                    keyType |= DS4KeyType.Macro;
                                if (item.InnerText.Contains(DS4KeyType.HoldMacro.ToString()))
                                    keyType |= DS4KeyType.HoldMacro;
                                if (item.InnerText.Contains(DS4KeyType.Unbound.ToString()))
                                    keyType |= DS4KeyType.Unbound;

                                if (keyType != DS4KeyType.None && Enum.TryParse(item.Name, out DS4Controls currentControl))
                                {
                                    store.UpdateDS4CKeyType(device, item.Name, true, keyType);
                                    shiftCustomMapKeyTypes.Add(store.getDS4ControlsByName(item.Name), keyType);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Loaded = false;
                store.ResetProfile(device);
                store.ResetMouseProperties(device, control);

                // Unplug existing output device if requested profile does not exist
                OutputDevice tempOutDev = device < ControlService.CURRENT_DS4_CONTROLLER_LIMIT ?
                    control.outputDevices[device] : null;
                if (tempOutDev != null)
                {
                    tempOutDev = null;
                    //Global.activeOutDevType[device] = OutContType.None;
                    DS4Device tempDev = control.DS4Controllers[device];
                    if (tempDev != null)
                    {
                        tempDev.queueEvent(() =>
                        {
                            control.UnplugOutDev(device, tempDev);
                        });
                    }
                }
            }

            // Only add missing settings if the actual load was graceful
            if (missingSetting && Loaded)// && buttons != null)
            {
                string proName = Path.GetFileName(profilepath);
                store.SaveProfile(device, proName);
            }

            if (Loaded)
            {
                store.CacheProfileCustomsFlags(device);
                store.buttonMouseInfos[device].activeButtonSensitivity =
                    store.buttonMouseInfos[device].buttonSensitivity;

                //if (device < Global.MAX_DS4_CONTROLLER_COUNT && control.touchPad[device] != null)
                //{
                //    control.touchPad[device]?.ResetToggleGyroModes();
                //    GyroOutMode currentGyro = gyroOutMode[device];
                //    if (currentGyro == GyroOutMode.Mouse)
                //    {
                //        control.touchPad[device].ToggleGyroMouse =
                //            gyroMouseToggle[device];
                //    }
                //    else if (currentGyro == GyroOutMode.MouseJoystick)
                //    {
                //        control.touchPad[device].ToggleGyroMouse =
                //            gyroMouseStickToggle[device];
                //    }
                //}

                // If a device exists, make sure to transfer relevant profile device
                // options to device instance
                if (postLoad && device < ControllerLimits.MaxControllerCount)
                {
                    store.PostLoadSnippet(device, control, xinputStatus, xinputPlug);
                }
            }

            return Loaded;
        }

        private string ReadLegacyElement(string rootname, string name)
        {
            return m_Xdoc.SelectSingleNode("/" + rootname + "/" + name)?.InnerText;
        }

        public void LoadLegacyExtraSettings(int device, string rootname)
        {
            if (int.TryParse(ReadLegacyElement(rootname, "DebouncingMs"), out int debounceMs))
                store.debouncingMs[device] = Math.Min(Math.Max(debounceMs, 0), 1000);

            if (bool.TryParse(ReadLegacyElement(rootname, "InverseRumbleMotors"), out bool inverseRumble))
                store.inverseRumbleMotors[device] = inverseRumble;

            // useDs3PitchRollSim は全体で 1 つの値（ProfileDTO.MapTo と同じ扱い）。要素があるときだけ代入する
            if (bool.TryParse(ReadLegacyElement(rootname, "UseDs3PitchRollSim"), out bool ds3Sim))
                store.useDs3PitchRollSim = ds3Sim;

            if (sbyte.TryParse(ReadLegacyElement(rootname, "LeftStickDriftXAxis"), out sbyte value))
                store.leftStickDriftXAxis[device] = value;
            if (sbyte.TryParse(ReadLegacyElement(rootname, "LeftStickDriftYAxis"), out value))
                store.leftStickDriftYAxis[device] = value;
            if (sbyte.TryParse(ReadLegacyElement(rootname, "RightStickDriftXAxis"), out value))
                store.rightStickDriftXAxis[device] = value;
            if (sbyte.TryParse(ReadLegacyElement(rootname, "RightStickDriftYAxis"), out value))
                store.rightStickDriftYAxis[device] = value;

            string triggerMode = ReadLegacyElement(rootname, "DS4OutputTriggerMode");
            if (triggerMode != null && Enum.TryParse(triggerMode, out DS4TriggerOutputMode mode) &&
                Enum.IsDefined(typeof(DS4TriggerOutputMode), mode))
            {
                store.outputDS4TriggerMode[device] = mode;
            }

            if (bool.TryParse(ReadLegacyElement(rootname, "GyroMouseJitterCompensation"), out bool gyroMouseJitter))
                store.gyroMouseInfo[device].jitterCompensation = gyroMouseJitter;
            if (bool.TryParse(ReadLegacyElement(rootname, "GyroMouseStickJitterCompensation"), out bool gyroStickJitter))
                store.gyroMStickInfo[device].jitterCompensation = gyroStickJitter;
        }
    }
}
