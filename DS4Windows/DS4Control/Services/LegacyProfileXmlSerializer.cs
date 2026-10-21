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
        // プロファイル XML の設定バージョンと拡張子（旧: ConfigVersion／XML_EXTENSION。Global の同名の定数は、この値を指す別名）
        public const int ConfigVersion = 5;
        public const string XmlExtension = ".xml";

        private readonly BackingStore store;
        private readonly DS4Windows.DI.IPathService _pathService;
        private readonly DS4Windows.DI.IProfileSettingsService _profileSettings;
        private readonly DS4Windows.DI.IEnvironmentService _environment;
        private readonly XmlDocument m_Xdoc = new XmlDocument(); // 旧 BackingStore.m_Xdoc と同じ使い方（排他は呼び出し側のロック）
        // Phase8-Step4b W2: LoadProfile の節（Q3＝A。DI 登録せず、直列化の中で使う状態を持たない）
        private readonly IProfileLoadSection _generalSection = new ProfileGeneralLoadSection();
        private readonly IProfileLoadSection _touchpadBasicSection = new ProfileTouchpadBasicLoadSection();
        private readonly IProfileLoadSection _triggerStickBasicSection = new ProfileTriggerStickBasicLoadSection();
        private readonly IProfileLoadSection _stickAxialSection = new ProfileStickAxialLoadSection();
        private readonly IProfileLoadSection _motionSensitivitySection = new ProfileMotionSensitivityLoadSection();
        private readonly IProfileLoadSection _miscSection = new ProfileMiscLoadSection();
        private readonly IProfileLoadSection _gyroControlsSection = new ProfileGyroControlsLoadSection();
        private readonly IProfileLoadSection _gyroMouseSection = new ProfileGyroMouseLoadSection();
        private readonly IProfileLoadSection _stickOutputSection = new ProfileStickOutputLoadSection();
        private readonly IProfileLoadSection _triggerCurveSection = new ProfileTriggerCurveLoadSection();
        private readonly IProfileLoadSection _touchpadMouseSection = new ProfileTouchpadMouseLoadSection();
        private readonly IProfileLoadSection _outputControllerSection = new ProfileOutputControllerLoadSection();
        private readonly IProfileLoadSection _profileActionsSection = new ProfileActionsLoadSection();
        private readonly IProfileLoadSection _buttonControlSection = new ProfileButtonControlLoadSection();
        private readonly IProfileLoadSection _buttonShiftSection = new ProfileButtonShiftLoadSection();
        // Phase8-Step4b W6: SaveProfile の節（保存の順序）
        private readonly IProfileSaveSection _generalSaveSection = new ProfileGeneralSaveSection();
        private readonly IProfileSaveSection _triggerStickSaveSection = new ProfileTriggerStickSaveSection();
        private readonly IProfileSaveSection _motionSaveSection = new ProfileMotionSaveSection();
        private readonly IProfileSaveSection _profileActionsSaveSection = new ProfileActionsSaveSection();
        private readonly ProfileIdFile _profileIdFile = new ProfileIdFile();   // Phase10-Step4d W1: プロファイルの固定 ID
        private readonly IProfileSaveSection _outputCurveSaveSection = new ProfileOutputCurveSaveSection();
        private readonly IProfileSaveSection _outputControllerSaveSection = new ProfileOutputControllerSaveSection();
        private readonly IProfileSaveSection _buttonSaveSection = new ProfileButtonSaveSection();


        // Phase10-Step4d W3: Controls の Load Profile の切り替え先の ID と名前を結ぶ。省略可（登録簿がなければ、従来どおり名前だけ）
        private readonly DS4Windows.DI.IProfileIdentityService _profileIdentity;

        public LegacyProfileXmlSerializer(BackingStore store, DS4Windows.DI.IPathService pathService, DS4Windows.DI.IProfileSettingsService profileSettings,
            DS4Windows.DI.IEnvironmentService environment, DS4Windows.DI.IProfileIdentityService profileIdentity = null)
        {
            _profileIdentity = profileIdentity;
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
        }

        public bool LoadProfile(int device, bool launchprogram, ControlService control,
                string propath = "", bool xinputChange = true, bool postLoad = true)
        {
            bool Loaded = true;
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
                var context = new ProfileLoadContext(store, m_Xdoc, rootname, device, control, lightbarSettings, lightInfo)
                {
                    LaunchProgram = launchprogram,
                    ProfileSettings = _profileSettings,
                    Environment = _environment,
                    XinputChange = xinputChange,
                    OldContType = oldContType,
                    MissingSetting = missingSetting,
                };

                // Make sure to reset currently set profile values before parsing
                store.ResetProfile(device);
                store.ResetMouseProperties(device, control);
                // Reset some Mapping properties before attempting to load different
                // profile
                control.PreLoadReset(device);

                _generalSection.Load(context);
                // Phase6-Step12: 旧形式の読込で欠けていた 10 要素。要素が無い（古いプロファイル）、または値が不正なときは、
                // ResetProfile で設定済みの既定値のままにする（missingSetting は立てない）。
                LoadLegacyExtraSettings(device, rootname);

                _touchpadBasicSection.Load(context);

                _triggerStickBasicSection.Load(context);

                _stickAxialSection.Load(context);

                _motionSensitivitySection.Load(context);

                _miscSection.Load(context);

                _gyroControlsSection.Load(context);

                _gyroMouseSection.Load(context);

                _stickOutputSection.Load(context);

                _triggerCurveSection.Load(context);

                _touchpadMouseSection.Load(context);

                _outputControllerSection.Load(context);

                _profileActionsSection.Load(context);

                _buttonControlSection.Load(context);
                ResolveControlProfileTargets(device);   // Phase10-Step4d W3

                _buttonShiftSection.Load(context);

                // 節が文脈へ書き戻した値を、読込の最後（保存し直し、PostLoadSnippet）が使う
                missingSetting = context.MissingSetting;
                xinputPlug = context.XinputPlug;
                xinputStatus = context.XinputStatus;
            }
            else
            {
                Loaded = false;
                store.ResetProfile(device);
                store.ResetMouseProperties(device, control);

                // Unplug existing output device if requested profile does not exist
                OutputDevice tempOutDev = device < _environment.ControllerSlotLimit ?
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
                SaveProfile(device, proName);
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

        public bool SaveAsProfile(int device, string proName)
        {
            bool Saved = true;
            store.ResetProfile(device);
            Saved = SaveProfile(device, proName);
            return Saved;
        }

        public bool SaveProfile(int device, string proName)
        {
            bool Saved = true;
            //string path = _pathService.AppDataPath + @"\Profiles\" + Path.GetFileNameWithoutExtension(proName) + ".xml";
            if (proName.EndsWith(XmlExtension))
            {
                proName = proName.Remove(proName.LastIndexOf(XmlExtension));
            }

            string path = $@"{_pathService.AppDataPath}\Profiles\{proName}{XmlExtension}";
            try
            {
                XmlNode tmpNode;
                m_Xdoc.RemoveAll();

                tmpNode = m_Xdoc.CreateXmlDeclaration("1.0", "utf-8", string.Empty);
                m_Xdoc.AppendChild(tmpNode);

                tmpNode = m_Xdoc.CreateComment(string.Format(" DS4Windows Configuration Data. {0} ", DateTime.Now));
                m_Xdoc.AppendChild(tmpNode);

                tmpNode = m_Xdoc.CreateComment(string.Format(" Made with DS4Windows version {0} ", _environment.ApplicationVersion));
                m_Xdoc.AppendChild(tmpNode);

                tmpNode = m_Xdoc.CreateWhitespace("\r\n");
                m_Xdoc.AppendChild(tmpNode);

                XmlElement rootElement = m_Xdoc.CreateElement("DS4Windows", null);
                rootElement.SetAttribute("app_version", _environment.ApplicationVersion);
                rootElement.SetAttribute("config_version", ConfigVersion.ToString());

                LightbarSettingInfo lightbarSettings = store.lightbarSettingInfo[device];
                LightbarDS4WinInfo lightInfo = lightbarSettings.ds4winSettings;
                var context = new ProfileSaveContext(store, m_Xdoc, rootElement, device, lightbarSettings, lightInfo);

                // Phase10-Step4d W1（Phase10-Step4d-Plan.md §3.1）: プロファイルの固定 ID を、ルート直下の先頭に書く。保存先のファイルがすでに ID を持つなら、それを保つ
                // （上書き保存は ID を変えない）。保存先が新しいファイル（別名で保存、新規作成）なら、新しい ID を振る。古いバージョンは、この要素を読み飛ばす
                XmlNode xmlProfileId = m_Xdoc.CreateNode(XmlNodeType.Element, ProfileIdFile.ElementName, null);
                xmlProfileId.InnerText = _profileIdFile.ReadIdOrNew(path);
                rootElement.AppendChild(xmlProfileId);

                FillControlProfileTargetIds(device);   // Phase10-Step4d W3

                _generalSaveSection.Save(context);
                // Phase6-Step12: 旧形式（3.9.9 互換）の保存で欠けていた 10 要素（要素名は ProfileDTO と同じ。旧版は未知の要素を無視する）
                SaveLegacyExtraSettings(rootElement, device);
                _triggerStickSaveSection.Save(context);
                _motionSaveSection.Save(context);
                _profileActionsSaveSection.Save(context);
                _outputCurveSaveSection.Save(context);

                _outputControllerSaveSection.Save(context);

                _buttonSaveSection.Save(context);

                m_Xdoc.AppendChild(rootElement);
                m_Xdoc.Save(path);
            }
            catch { Saved = false; }
            return Saved;
        }

        /// <summary>
        /// Phase10-Step4d W3: Controls の Load Profile の切り替え先を、ID で解決する。ID が見つかれば、現在の名前に置き換える（名前変更への追従。ID が勝つ）。
        /// ID がない・見つからないときは、名前のまま。
        /// </summary>
        private void ResolveControlProfileTargets(int device)
        {
            if (_profileIdentity == null) return;

            foreach (DS4ControlSettings dcs in store.ds4settings[device])
            {
                ControlSpecialItem special = dcs.special;
                if (special == null || special.Type != SpecialAction.ActionTypeId.Profile || string.IsNullOrEmpty(special.ProfileId)) continue;

                string name = _profileIdentity.GetName(special.ProfileId);
                if (name != null) special.ProfileName = name;
            }
        }

        /// <summary>
        /// Phase10-Step4d W3: 保存の前に、Controls の Load Profile の切り替え先の ID を、名前から引いて更新する（ID が確定していなければ、ここで確定する）。
        /// プロファイルが見つからなければ（削除済み）、そのままにする。
        /// </summary>
        private void FillControlProfileTargetIds(int device)
        {
            if (_profileIdentity == null) return;

            foreach (DS4ControlSettings dcs in store.ds4settings[device])
            {
                ControlSpecialItem special = dcs.special;
                if (special == null || special.Type != SpecialAction.ActionTypeId.Profile || string.IsNullOrEmpty(special.ProfileName)) continue;

                string id = _profileIdentity.EnsurePersisted(special.ProfileName);
                if (id != null) special.ProfileId = id;
            }
        }

        public void SaveLegacyExtraSettings(XmlNode rootElement, int device)
        {
            AppendLegacyElement(rootElement, "DebouncingMs", store.debouncingMs[device].ToString());
            AppendLegacyElement(rootElement, "InverseRumbleMotors", store.inverseRumbleMotors[device].ToString());
            AppendLegacyElement(rootElement, "UseDs3PitchRollSim", store.useDs3PitchRollSim.ToString());
            AppendLegacyElement(rootElement, "LeftStickDriftXAxis", store.leftStickDriftXAxis[device].ToString());
            AppendLegacyElement(rootElement, "LeftStickDriftYAxis", store.leftStickDriftYAxis[device].ToString());
            AppendLegacyElement(rootElement, "RightStickDriftXAxis", store.rightStickDriftXAxis[device].ToString());
            AppendLegacyElement(rootElement, "RightStickDriftYAxis", store.rightStickDriftYAxis[device].ToString());
            AppendLegacyElement(rootElement, "DS4OutputTriggerMode", store.outputDS4TriggerMode[device].ToString());
            AppendLegacyElement(rootElement, "GyroMouseJitterCompensation", store.gyroMouseInfo[device].jitterCompensation.ToString());
            AppendLegacyElement(rootElement, "GyroMouseStickJitterCompensation", store.gyroMStickInfo[device].jitterCompensation.ToString());
        }

        private void AppendLegacyElement(XmlNode parent, string name, string value)
        {
            XmlNode node = m_Xdoc.CreateNode(XmlNodeType.Element, name, null);
            node.InnerText = value;
            parent.AppendChild(node);
        }
    }
}
