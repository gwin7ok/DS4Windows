#nullable enable

using System;
using System.IO;
using System.Xml;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Persistence.ProfileSections;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence
{
    /// <summary>
    /// プロファイルの保存・読込。
    /// 旧: <c>BackingStore</c> の <c>LoadProfile</c>／<c>LoadLegacyExtraSettings</c>／<c>ReadLegacyElement</c>／<c>axisOutputCurveId</c>／<c>OutContDeviceId</c>／
    /// <c>GetLightbarModeType</c>／<c>PortOldGyroSettings</c>（W4。読込側）と、保存側（W5）。移動のみで、ロジック・ログ・要素の順序は変えない。
    /// <c>BackingStore</c> の公開状態を <c>store.</c> で読み書きする。プロファイルの置き場所は <see cref="DS4Windows.Core.Abstractions.IPathService"/>、
    /// 実行時の「DInput のみ」の配列は <see cref="DS4Windows.Core.Abstractions.IProfileSettingsService"/> から得る。
    /// 排他は呼び出し側（<c>ProfileXmlStore.XmlIoLock</c>）が行う。
    /// </summary>
    public class LegacyProfileXmlSerializer
    {
        // プロファイル XML の設定バージョンと拡張子
        public const int ConfigVersion = 5;
        public const string XmlExtension = ".xml";

        private readonly BackingStore store;
        private readonly DS4Windows.Core.Abstractions.IPathService _pathService;
        private readonly DS4Windows.Core.Abstractions.IProfileSettingsService _profileSettings;
        private readonly DS4Windows.Core.Abstractions.IEnvironmentService _environment;
        private readonly XmlDocument m_Xdoc = new XmlDocument(); // 旧 BackingStore.m_Xdoc と同じ使い方（排他は呼び出し側のロック）
        // 読み込みと保存の節を領域ごとに対にして持つ（状態を持たず、DI 登録せず、直列化の中で使う）
        private readonly ProfileAreaSection _general = new ProfileAreaSection("General", new ProfileGeneralSaveSection(), new ProfileGeneralLoadSection());
        private readonly ProfileAreaSection _triggerStick = new ProfileAreaSection("TriggerStick", new ProfileTriggerStickSaveSection(),
            new ProfileTouchpadBasicLoadSection(), new ProfileTriggerStickBasicLoadSection(), new ProfileStickAxialLoadSection());
        private readonly ProfileAreaSection _motion = new ProfileAreaSection("Motion", new ProfileMotionSaveSection(),
            new ProfileMotionSensitivityLoadSection(), new ProfileMiscLoadSection(), new ProfileGyroControlsLoadSection(), new ProfileGyroMouseLoadSection());
        private readonly ProfileAreaSection _outputCurve = new ProfileAreaSection("OutputCurve", new ProfileOutputCurveSaveSection(),
            new ProfileStickOutputLoadSection(), new ProfileTriggerCurveLoadSection(), new ProfileTouchpadMouseLoadSection());
        private readonly ProfileAreaSection _outputController = new ProfileAreaSection("OutputController", new ProfileOutputControllerSaveSection(), new ProfileOutputControllerLoadSection());
        private readonly ProfileAreaSection _actions = new ProfileAreaSection("Actions", new ProfileActionsSaveSection(), new ProfileActionsLoadSection());
        private readonly ProfileAreaSection _button = new ProfileAreaSection("Button", new ProfileButtonSaveSection(),
            new ProfileButtonControlLoadSection(), new ProfileButtonShiftLoadSection());
        private readonly ProfileIdFile _profileIdFile = new ProfileIdFile();   // プロファイルの固定 ID


        // Controls の Load Profile の切り替え先の ID と名前を結ぶ。省略可（登録簿がなければ、従来どおり名前だけ）
        private readonly DS4Windows.Core.Abstractions.IProfileIdentityService? _profileIdentity;

        // ライトバーの強制表示の状態
        private readonly DS4Windows.Core.Model.LightbarOverrides _lightbar;

        public LegacyProfileXmlSerializer(DS4Windows.Core.Model.LightbarOverrides lightbar, BackingStore store, DS4Windows.Core.Abstractions.IPathService pathService, DS4Windows.Core.Abstractions.IProfileSettingsService profileSettings,
            DS4Windows.Core.Abstractions.IEnvironmentService environment, DS4Windows.Core.Abstractions.IProfileIdentityService? profileIdentity = null)
        {
            _lightbar = lightbar ?? throw new System.ArgumentNullException(nameof(lightbar));
            _profileIdentity = profileIdentity;
            ArgumentNullException.ThrowIfNull(environment);
            _environment = environment;
            ArgumentNullException.ThrowIfNull(store);
            this.store = store;
            ArgumentNullException.ThrowIfNull(pathService);
            _pathService = pathService;
            ArgumentNullException.ThrowIfNull(profileSettings);
            _profileSettings = profileSettings;
        }

        public bool LoadProfile(int device, IProfileLoadTarget control,
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
                    _lightbar.forcelight[device] = false;
                    _lightbar.forcedFlash[device] = 0;
                }

                OutContType oldContType = store.ActiveOutDevType[device];
                LightbarSettingInfo lightbarSettings = store.lightbarSlots[device].Info;
                LightbarDS4WinInfo lightInfo = lightbarSettings.ds4winSettings;
                var context = new ProfileLoadContext(store, m_Xdoc, rootname, device, control, lightbarSettings, lightInfo)
                {
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

                _general.Load(context);
                // 旧形式の読込で欠けていた 10 要素。要素が無い（古いプロファイル）、または値が不正なときは、
                // ResetProfile で設定済みの既定値のままにする（missingSetting は立てない）。
                LoadLegacyExtraSettings(device, rootname);

                _triggerStick.Load(context);
                _motion.Load(context);
                _outputCurve.Load(context);
                _outputController.Load(context);
                _actions.Load(context);
                _button.Load(context);
                ResolveControlProfileTargets(device);   // Phase10-Step4d W3

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
                if (device < _environment.ControllerSlotLimit && control.HasOutputDevice(device))
                {
                    //Global.activeOutDevType[device] = OutContType.None;
                    IConfigurableDevice tempDev = control.GetController(device);
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

                //if (device < Global.MAX_DS4_CONTROLLER_COUNT && control.GetTouchPad(device) != null)
                //{
                //    control.GetTouchPad(device)?.ResetToggleGyroModes();
                //    GyroOutMode currentGyro = gyroSlots[device].OutputMode;
                //    if (currentGyro == GyroOutMode.Mouse)
                //    {
                //        control.GetTouchPad(device).ToggleGyroMouse =
                //            gyroSlots[device].MouseToggle;
                //    }
                //    else if (currentGyro == GyroOutMode.MouseJoystick)
                //    {
                //        control.GetTouchPad(device).ToggleGyroMouse =
                //            gyroSlots[device].MouseStickToggle;
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

        private string? ReadLegacyElement(string rootname, string name)
        {
            return m_Xdoc.SelectSingleNode("/" + rootname + "/" + name)?.InnerText;
        }

        public void LoadLegacyExtraSettings(int device, string rootname)
        {
            // <DebouncingMs> は機能廃止で読み捨てる（古いプロファイルの互換のため、タグがあっても無視する）。保存もしない
            if (bool.TryParse(ReadLegacyElement(rootname, "InverseRumbleMotors"), out bool inverseRumble))
                store.rumbleSlots[device].InverseMotors = inverseRumble;


            if (sbyte.TryParse(ReadLegacyElement(rootname, "LeftStickDriftXAxis"), out sbyte value))
                store.stickSlots[device].LeftStickDriftXAxis = value;
            if (sbyte.TryParse(ReadLegacyElement(rootname, "LeftStickDriftYAxis"), out value))
                store.stickSlots[device].LeftStickDriftYAxis = value;
            if (sbyte.TryParse(ReadLegacyElement(rootname, "RightStickDriftXAxis"), out value))
                store.stickSlots[device].RightStickDriftXAxis = value;
            if (sbyte.TryParse(ReadLegacyElement(rootname, "RightStickDriftYAxis"), out value))
                store.stickSlots[device].RightStickDriftYAxis = value;

            string? triggerMode = ReadLegacyElement(rootname, "DS4OutputTriggerMode");
            if (triggerMode != null && Enum.TryParse(triggerMode, out DS4TriggerOutputMode mode) &&
                Enum.IsDefined(typeof(DS4TriggerOutputMode), mode))
            {
                store.outputTypeSlots[device].DS4TriggerMode = mode;
            }

            if (bool.TryParse(ReadLegacyElement(rootname, "GyroMouseJitterCompensation"), out bool gyroMouseJitter))
                store.gyroSlots[device].Mouse.jitterCompensation = gyroMouseJitter;
            if (bool.TryParse(ReadLegacyElement(rootname, "GyroMouseStickJitterCompensation"), out bool gyroStickJitter))
                store.gyroSlots[device].MouseStick.jitterCompensation = gyroStickJitter;
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

                LightbarSettingInfo lightbarSettings = store.lightbarSlots[device].Info;
                LightbarDS4WinInfo lightInfo = lightbarSettings.ds4winSettings;
                var context = new ProfileSaveContext(store, m_Xdoc, rootElement, device, lightbarSettings, lightInfo);

                // プロファイルの固定 ID を、ルート直下の先頭に書く。保存先のファイルがすでに ID を持つなら、それを保つ
                // （上書き保存は ID を変えない）。保存先が新しいファイル（別名で保存、新規作成）なら、新しい ID を振る。古いバージョンは、この要素を読み飛ばす
                XmlNode xmlProfileId = m_Xdoc.CreateNode(XmlNodeType.Element, ProfileIdFile.ElementName, null);
                xmlProfileId.InnerText = _profileIdFile.ReadIdOrNew(path);
                rootElement.AppendChild(xmlProfileId);

                FillControlProfileTargetIds(device);   // Phase10-Step4d W3

                _general.Save(context);
                // 旧形式（3.9.9 互換）の保存で欠けていた 10 要素（要素名は ProfileDTO と同じ。旧版は未知の要素を無視する）
                SaveLegacyExtraSettings(rootElement, device);
                _triggerStick.Save(context);
                _motion.Save(context);
                _actions.Save(context);
                _outputCurve.Save(context);
                _outputController.Save(context);
                _button.Save(context);

                m_Xdoc.AppendChild(rootElement);
                m_Xdoc.Save(path);
            }
            catch { Saved = false; }
            return Saved;
        }

        /// <summary>
        /// Controls の Load Profile の切り替え先を、ID で解決する。ID が見つかれば、現在の名前に置き換える（名前変更への追従。ID が勝つ）。
        /// ID がない・見つからないときは、名前のまま。
        /// </summary>
        private void ResolveControlProfileTargets(int device)
        {
            if (_profileIdentity == null) return;

            foreach (DS4ControlSettings dcs in store.ds4settings[device])
            {
                ControlSpecialItem? special = dcs.special;
                if (special == null || special.Type != SpecialAction.ActionTypeId.Profile || string.IsNullOrEmpty(special.ProfileId)) continue;

                string? name = _profileIdentity.GetName(special.ProfileId);
                if (name != null) special.ProfileName = name;
            }
        }

        /// <summary>
        /// 保存の前に、Controls の Load Profile の切り替え先の ID を、名前から引いて更新する（ID が確定していなければ、ここで確定する）。
        /// プロファイルが見つからなければ（削除済み）、そのままにする。
        /// </summary>
        private void FillControlProfileTargetIds(int device)
        {
            if (_profileIdentity == null) return;

            foreach (DS4ControlSettings dcs in store.ds4settings[device])
            {
                ControlSpecialItem? special = dcs.special;
                if (special == null || special.Type != SpecialAction.ActionTypeId.Profile || string.IsNullOrEmpty(special.ProfileName)) continue;

                string? id = _profileIdentity.EnsurePersisted(special.ProfileName);
                if (id != null) special.ProfileId = id;
            }
        }

        public void SaveLegacyExtraSettings(XmlNode rootElement, int device)
        {
            AppendLegacyElement(rootElement, "InverseRumbleMotors", store.rumbleSlots[device].InverseMotors.ToString());
            AppendLegacyElement(rootElement, "LeftStickDriftXAxis", store.stickSlots[device].LeftStickDriftXAxis.ToString());
            AppendLegacyElement(rootElement, "LeftStickDriftYAxis", store.stickSlots[device].LeftStickDriftYAxis.ToString());
            AppendLegacyElement(rootElement, "RightStickDriftXAxis", store.stickSlots[device].RightStickDriftXAxis.ToString());
            AppendLegacyElement(rootElement, "RightStickDriftYAxis", store.stickSlots[device].RightStickDriftYAxis.ToString());
            AppendLegacyElement(rootElement, "DS4OutputTriggerMode", store.outputTypeSlots[device].DS4TriggerMode.ToString());
            AppendLegacyElement(rootElement, "GyroMouseJitterCompensation", store.gyroSlots[device].Mouse.jitterCompensation.ToString());
            AppendLegacyElement(rootElement, "GyroMouseStickJitterCompensation", store.gyroSlots[device].MouseStick.jitterCompensation.ToString());
        }

        private void AppendLegacyElement(XmlNode parent, string name, string value)
        {
            XmlNode node = m_Xdoc.CreateNode(XmlNodeType.Element, name, null);
            node.InnerText = value;
            parent.AppendChild(node);
        }
    }
}
