using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Persistence.Dto;
using DS4Windows.Core.Settings;
using DS4Windows.Core.Utilities;

namespace DS4Windows.Core.Persistence
{
    /// <summary>
    /// アプリ設定（<c>Profiles.xml</c>）の保存・読込。
    /// 旧: <c>BackingStore</c> の <c>Load</c>／<c>Save</c>／<c>LoadFakeExeName</c>（移動のみ。ロジック・ログ・要素の順序は変えない。
    /// 旧形式の読込の互換を含む。<c>BackingStore</c> の公開状態を <c>store.</c> で読み書きする）。
    /// <c>PostProcessLoad</c>（読込後の値の丸め）は状態の整形のため <c>BackingStore</c> に残し、<c>store.PostProcessLoad()</c> として呼ぶ。
    /// パス・環境の窓口は、<c>Global</c> ではなく <see cref="DS4Windows.Core.Abstractions.IPathService"/>／<see cref="DS4Windows.Core.Abstractions.IEnvironmentService"/> から得る。
    /// </summary>
    public class AppSettingsXmlSerializer
    {
        private readonly BackingStore store;
        // 偽の exe 名を保存するファイルの名前
        public const string CustomExeConfigFileName = "custom_exe_name.txt";

        private readonly DS4Windows.Core.Abstractions.IPathService _pathService;
        private readonly DS4Windows.Core.Abstractions.IEnvironmentService _environment;
        private readonly XmlDocument m_Xdoc = new XmlDocument(); // 旧 BackingStore.m_Xdoc と同じ使い方（排他は呼び出し側のロック）

        // スロットの選択のプロファイルの ID と名前を結ぶ。省略可（登録簿がなければ、従来どおり名前だけ）
        private readonly DS4Windows.Core.Abstractions.IProfileIdentityService? _profileIdentity;

        public AppSettingsXmlSerializer(BackingStore store, DS4Windows.Core.Abstractions.IPathService pathService, DS4Windows.Core.Abstractions.IEnvironmentService environment,
            DS4Windows.Core.Abstractions.IProfileIdentityService? profileIdentity = null)
        {
            _profileIdentity = profileIdentity;
            ArgumentNullException.ThrowIfNull(store);
            this.store = store;
            ArgumentNullException.ThrowIfNull(pathService);
            _pathService = pathService;
            ArgumentNullException.ThrowIfNull(environment);
            _environment = environment;
        }

        public bool Load()
        {
            bool Loaded = true;
            bool missingSetting = false;

            try
            {
                if (File.Exists(store.m_Profile))
                {
                    // First try DTO-based deserialization. Support both <AppSettingsDTO> and
                    // <Profile> roots by attempting XmlSerializer with a Profile root
                    // then falling back to the default DTO root.
                    string fileXml = File.ReadAllText(store.m_Profile);
                    bool dtoLoaded = false;
                    try
                    {
                        AppSettingsDTO? dto = null;
                        try
                        {
                            var xr = new XmlRootAttribute("Profile");
                            var ser = new XmlSerializer(typeof(AppSettingsDTO), xr);
                            using (var sr = new StringReader(fileXml))
                            {
                                dto = ser.Deserialize(sr) as AppSettingsDTO;
                            }
                        }
                        catch { dto = null; }

                        if (dto == null)
                        {
                            try
                            {
                                var ser2 = new XmlSerializer(typeof(AppSettingsDTO));
                                using (var sr2 = new StringReader(fileXml))
                                {
                                    dto = ser2.Deserialize(sr2) as AppSettingsDTO;
                                }
                            }
                            catch { dto = null; }
                        }

                        if (dto != null)
                        {
                            // Map DTO into this backing store
                            if (_profileIdentity != null) dto.ProfileNameOf = _profileIdentity.GetName;   // Phase10-Step4d W4
                            dto.MapTo(store);
                            dtoLoaded = true;
                        }
                    }
                    catch { /* fall through to legacy loader below */ }

                    if (dtoLoaded)
                    {
                        // 旧形式の経路と同じ後処理を行う。
                        // これまでは「Post processing same as legacy path」とコメントされていたが、実際には何も実行されず、
                        // 偽の exe 名・スキップしたバージョンの数値・ポートの範囲・Distance プロファイルの判定が欠落していた
                        // （105575a7 〜。移行前から続く不具合で、回帰ではない）。
                        store.PostProcessLoad();
                        LoadFakeExeName();
                        Loaded = true;
                        return Loaded;
                    }

                    // Fall back to legacy DOM-based loader
                    XmlNode? Item;

                    m_Xdoc.Load(store.m_Profile);

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/useExclusiveMode"); Boolean.TryParse(Item.Required().InnerText, out var _p_useExclusiveMode); store.Behavior.useExclusiveMode = _p_useExclusiveMode; } // Ex Mode
                    catch { missingSetting = true; } // Ex Mode
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/startMinimized"); Boolean.TryParse(Item.Required().InnerText, out var _p_startMinimized); store.Behavior.startMinimized = _p_startMinimized; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/minimizeToTaskbar"); Boolean.TryParse(Item.Required().InnerText, out var _p_minToTaskbar); store.Behavior.minToTaskbar = _p_minToTaskbar; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/formWidth"); Int32.TryParse(Item.Required().InnerText, out var _p_formWidth); store.Layout.formWidth = _p_formWidth; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/formHeight"); Int32.TryParse(Item.Required().InnerText, out var _p_formHeight); store.Layout.formHeight = _p_formHeight; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/formLocationX"); Int32.TryParse(Item.Required().InnerText, out var _p_formLocationX); store.Layout.formLocationX = _p_formLocationX; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/formLocationY"); Int32.TryParse(Item.Required().InnerText, out var _p_formLocationY); store.Layout.formLocationY = _p_formLocationY; }
                    catch { missingSetting = true; }

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/profileEditorLeftWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out var _p_profileEditorLeftWidth); store.Layout.profileEditorLeftWidth = _p_profileEditorLeftWidth; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/profileEditorRightWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out var _p_profileEditorRightWidth); store.Layout.profileEditorRightWidth = _p_profileEditorRightWidth; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/specialActionNameColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out var _p_specialActionNameColWidth); store.Layout.specialActionNameColWidth = _p_specialActionNameColWidth; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/specialActionTriggerColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out var _p_specialActionTriggerColWidth); store.Layout.specialActionTriggerColWidth = _p_specialActionTriggerColWidth; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/specialActionDetailColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out var _p_specialActionDetailColWidth); store.Layout.specialActionDetailColWidth = _p_specialActionDetailColWidth; }
                    catch { missingSetting = true; }

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerIndexColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out var _p_controllerIndexColWidth); store.Layout.controllerIndexColWidth = _p_controllerIndexColWidth; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerIdColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out var _p_controllerIdColWidth); store.Layout.controllerIdColWidth = _p_controllerIdColWidth; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerStatusColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out var _p_controllerStatusColWidth); store.Layout.controllerStatusColWidth = _p_controllerStatusColWidth; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerExclusiveColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out var _p_controllerExclusiveColWidth); store.Layout.controllerExclusiveColWidth = _p_controllerExclusiveColWidth; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerBatteryColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out var _p_controllerBatteryColWidth); store.Layout.controllerBatteryColWidth = _p_controllerBatteryColWidth; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerSelectProfileColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out var _p_controllerSelectProfileColWidth); store.Layout.controllerSelectProfileColWidth = _p_controllerSelectProfileColWidth; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerEditColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out var _p_controllerEditColWidth); store.Layout.controllerEditColWidth = _p_controllerEditColWidth; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerLinkedProfileColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out var _p_controllerLinkedProfileColWidth); store.Layout.controllerLinkedProfileColWidth = _p_controllerLinkedProfileColWidth; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerLinkProfIdColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out var _p_controllerLinkProfIdColWidth); store.Layout.controllerLinkProfIdColWidth = _p_controllerLinkProfIdColWidth; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerCustomColorColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out var _p_controllerCustomColorColWidth); store.Layout.controllerCustomColorColWidth = _p_controllerCustomColorColWidth; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/outputSlotNumColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out var _p_outputSlotNumColWidth); store.Layout.outputSlotNumColWidth = _p_outputSlotNumColWidth; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/outputSlotInputNumColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out var _p_outputSlotInputNumColWidth); store.Layout.outputSlotInputNumColWidth = _p_outputSlotInputNumColWidth; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/outputSlotInputIdColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out var _p_outputSlotInputIdColWidth); store.Layout.outputSlotInputIdColWidth = _p_outputSlotInputIdColWidth; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/outputSlotCurrentTypeColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out var _p_outputSlotCurrentTypeColWidth); store.Layout.outputSlotCurrentTypeColWidth = _p_outputSlotCurrentTypeColWidth; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/outputSlotReservationColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out var _p_outputSlotReservationColWidth); store.Layout.outputSlotReservationColWidth = _p_outputSlotReservationColWidth; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/autoProfileNameColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out var _p_autoProfileNameColWidth); store.Layout.autoProfileNameColWidth = _p_autoProfileNameColWidth; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/autoProfilePathColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out var _p_autoProfilePathColWidth); store.Layout.autoProfilePathColWidth = _p_autoProfilePathColWidth; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/autoProfileTitleColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out var _p_autoProfileTitleColWidth); store.Layout.autoProfileTitleColWidth = _p_autoProfileTitleColWidth; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/logTimeColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out var _p_logTimeColWidth); store.Layout.logTimeColWidth = _p_logTimeColWidth; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/logDataColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out var _p_logDataColWidth); store.Layout.logDataColWidth = _p_logDataColWidth; }
                    catch { missingSetting = true; }

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/logMaxArchiveFiles"); Int32.TryParse(Item?.InnerText ?? string.Empty, out var _p_logMaxArchiveFiles); store.Log.logMaxArchiveFiles = _p_logMaxArchiveFiles; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/logMinLevel"); store.Log.logMinLevel = Item?.InnerText ?? BackingStore.DEFAULT_LOG_MIN_LEVEL; }
                    catch { missingSetting = true; }

                    for (int i = 0; i < ControllerLimits.MaxControllerCount; i++)
                    {
                        string contTag = $"/Profile/Controller{i + 1}";
                        try
                        {
                            Item = m_Xdoc.SelectSingleNode(contTag); store.profilePath[i] = Item?.InnerText ?? string.Empty;
                            if (store.profilePath[i].ToLower().Contains("distance"))
                            {
                                store.distanceProfiles[i] = true;
                            }

                            store.olderProfilePath[i] = store.profilePath[i];
                        }
                        catch { store.profilePath[i] = store.olderProfilePath[i] = string.Empty; store.distanceProfiles[i] = false; }
                    }

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/LastChecked"); DateTime.TryParse(Item.Required().InnerText, out var _p_lastChecked); store.UpdateCheck.lastChecked = _p_lastChecked; }
                    catch { missingSetting = true; }
                    // Read new explicit startup-check fields when available. If not present,
                    // fall back to legacy <CheckWhen> value and map it into daily/hourly values.
                    try
                    {
                        XmlNode? everyValNode = m_Xdoc.SelectSingleNode("/Profile/CheckEveryValue");
                        XmlNode? everyUnitNode = m_Xdoc.SelectSingleNode("/Profile/CheckEveryUnit");
                        if (everyValNode != null)
                        {
                            Int32.TryParse(everyValNode.InnerText, out var _p_checkEveryValue); store.UpdateCheck.checkEveryValue = _p_checkEveryValue;
                            Int32.TryParse(everyUnitNode?.InnerText ?? "1", out var _p_checkEveryUnit); store.UpdateCheck.checkEveryUnit = _p_checkEveryUnit;
                        }
                        else
                        {
                            // Legacy fallback: map CheckWhen (hours or days) into checkEveryValue/unit
                            Item = m_Xdoc.SelectSingleNode("/Profile/CheckWhen");
                            if (Item != null && Int32.TryParse(Item.InnerText, out int legacyCheckWhen))
                            {
                                if (legacyCheckWhen > 23)
                                {
                                    // stored in hours; convert to days
                                    store.UpdateCheck.checkEveryValue = legacyCheckWhen / 24;
                                    store.UpdateCheck.checkEveryUnit = 1; // days
                                }
                                else
                                {
                                    store.UpdateCheck.checkEveryValue = legacyCheckWhen;
                                    store.UpdateCheck.checkEveryUnit = 0; // hours
                                }
                            }
                        }
                    }
                    catch { missingSetting = true; }
                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/Profile/LastVersionChecked");
                        string tempVer = Item?.InnerText ?? string.Empty;
                        if (!string.IsNullOrEmpty(tempVer))
                        {
                            store.UpdateCheck.lastVersionCheckedNum = VersionUtil.CompileVersionNumberFromString(tempVer);
                            if (store.UpdateCheck.lastVersionCheckedNum > 0) store.UpdateCheck.lastVersionChecked = tempVer;
                        }
                    }
                    catch { missingSetting = true; }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/Profile/Notifications");
                        bool notificationsParsed = int.TryParse(Item.Required().InnerText, out int notificationsValue);
                        store.Behavior.notifications = notificationsValue; // 失敗時の 0 も、従来どおり代入する
                        if (!notificationsParsed)
                            store.Behavior.notifications = (Boolean.Parse(Item.Required().InnerText) ? 2 : 0);
                    }
                    catch { missingSetting = true; }

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/DisconnectBTAtStop"); Boolean.TryParse(Item.Required().InnerText, out var _p_disconnectBTAtStop); store.Behavior.disconnectBTAtStop = _p_disconnectBTAtStop; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/SwipeProfiles"); Boolean.TryParse(Item.Required().InnerText, out var _p_swipeProfiles); store.Behavior.swipeProfiles = _p_swipeProfiles; }
                    catch { missingSetting = true; }
                    //try { Item = m_Xdoc.SelectSingleNode("/Profile/UseDS4ForMapping"); Boolean.TryParse(Item.InnerText, out ds4Mapping); }
                    //catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/QuickCharge"); Boolean.TryParse(Item.Required().InnerText, out var _p_quickCharge); store.Behavior.quickCharge = _p_quickCharge; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/CloseMinimizes"); Boolean.TryParse(Item.Required().InnerText, out var _p_closeMini); store.Behavior.closeMini = _p_closeMini; }
                    catch { missingSetting = true; }
                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/Profile/UseLang"); store.Appearance.useLang = Item.Required().InnerText;
                        AppLogger.LogDebug($"ScpUtil: loaded UseLang='{store.Appearance.useLang}' from profile (appdatapath={_pathService.AppDataPath})");
                    }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/DownloadLang"); Boolean.TryParse(Item.Required().InnerText, out var _p_downloadLang); store.Appearance.downloadLang = _p_downloadLang; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/FlashWhenLate"); Boolean.TryParse(Item.Required().InnerText, out var _p_flashWhenLate); store.Behavior.flashWhenLate = _p_flashWhenLate; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/FlashWhenLateAt"); int.TryParse(Item.Required().InnerText, out var _p_flashWhenLateAt); store.Behavior.flashWhenLateAt = _p_flashWhenLateAt; }
                    catch { missingSetting = true; }

                    Item = m_Xdoc.SelectSingleNode("/Profile/AppIcon");
                    bool hasIconChoice = Item != null;
                    if (hasIconChoice)
                    {
                        hasIconChoice = Enum.TryParse(Item.Required().InnerText ?? "", out TrayIconChoice iconChoiceValue);
                        store.Appearance.useIconChoice = iconChoiceValue;
                    }

                    if (!hasIconChoice)
                    {
                        missingSetting = true;

                        // Backwards compatible code from when only two icons existed
                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/WhiteIcon");
                            if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                            {
                                store.Appearance.useIconChoice = temp ? TrayIconChoice.White : TrayIconChoice.Default;
                            }
                        }
                        catch { missingSetting = true; }
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/Profile/AppTheme");
                        string temp = Item.Required().InnerText;
                        Enum.TryParse(temp, out AppThemeChoice choice);
                        store.Appearance.useCurrentTheme = choice;
                    }
                    catch { missingSetting = true; store.Appearance.useCurrentTheme = AppThemeChoice.Default; }

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/UseOSCServer"); Boolean.TryParse(Item.Required().InnerText, out var _p_useOSCServ); store.Network.useOSCServ = _p_useOSCServ; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/OSCServerPort"); int temp; int.TryParse(Item.Required().InnerText, out temp); store.Network.oscServPort = Math.Min(Math.Max(temp, 1024), 65535); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/InterpretingOscMonitoring"); Boolean.TryParse(Item.Required().InnerText, out var _p_interpretingOscMonitoring); store.Network.interpretingOscMonitoring = _p_interpretingOscMonitoring; }
                    catch { missingSetting = true; }

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/UseOSCSender"); Boolean.TryParse(Item.Required().InnerText, out var _p_useOSCSend); store.Network.useOSCSend = _p_useOSCSend; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/OSCSenderPort"); int temp; int.TryParse(Item.Required().InnerText, out temp); store.Network.oscSendPort = Math.Min(Math.Max(temp, 1024), 65535); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/OSCSenderAddress"); store.Network.oscSendAddress = Item.Required().InnerText; }
                    catch { missingSetting = true; }

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/UseUDPServer"); Boolean.TryParse(Item.Required().InnerText, out var _p_useUDPServ); store.Network.useUDPServ = _p_useUDPServ; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/UDPServerPort"); int temp; int.TryParse(Item.Required().InnerText, out temp); store.Network.udpServPort = Math.Min(Math.Max(temp, 1024), 65535); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/UDPServerListenAddress"); store.Network.udpServListenAddress = Item.Required().InnerText; }
                    catch { missingSetting = true; }

                    bool udpServerSmoothingGroup = false;
                    XmlNode? xmlUdpServerSmoothElement =
                        m_Xdoc.SelectSingleNode("/Profile/UDPServerSmoothingOptions");
                    udpServerSmoothingGroup = xmlUdpServerSmoothElement != null;
                    if (udpServerSmoothingGroup)
                    {
                        try
                        {
                            Item = xmlUdpServerSmoothElement.Required().SelectSingleNode("UseSmoothing");
                            bool.TryParse(Item.Required().InnerText, out bool temp);
                            store.Network.useUdpSmoothing = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlUdpServerSmoothElement.Required().SelectSingleNode("UdpSmoothMinCutoff");
                            double.TryParse(Item.Required().InnerText, out double temp);
                            temp = Math.Min(Math.Max(temp, 0.00001), 100.0);
                            store.Network.udpSmoothingMincutoff = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlUdpServerSmoothElement.Required().SelectSingleNode("UdpSmoothBeta");
                            double.TryParse(Item.Required().InnerText, out double temp);
                            temp = Math.Min(Math.Max(temp, 0.0), 1.0);
                            store.Network.udpSmoothingBeta = temp;
                        }
                        catch { missingSetting = true; }
                    }

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/UseCustomSteamFolder"); Boolean.TryParse(Item.Required().InnerText, out var _p_useCustomSteamFolder); store.Network.useCustomSteamFolder = _p_useCustomSteamFolder; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/CustomSteamFolder"); store.Network.customSteamFolder = Item.Required().InnerText; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/AutoProfileRevertDefaultProfile"); Boolean.TryParse(Item.Required().InnerText, out var _p_autoProfileRevertDefaultProfile); store.Behavior.autoProfileRevertDefaultProfile = _p_autoProfileRevertDefaultProfile; }
                    catch { missingSetting = true; }

                    XmlNode? xmlDeviceOptions = m_Xdoc.SelectSingleNode("/Profile/DeviceOptions");
                    if (xmlDeviceOptions != null)
                    {
                        // 系統ごとの設定（Enabled と機種固有の項目）。節の名前と項目は DeviceOptionsBase の定義が持つ。
                        // 解析できない値はそのまま（既定値）にする
                        foreach (InputDeviceFamily family in store.deviceOptions.Families)
                        {
                            DeviceOptionsBase options = store.deviceOptions[family];
                            XmlNode? xmlSupport = xmlDeviceOptions.SelectSingleNode(options.SectionName);
                            if (xmlSupport == null) continue;
                            foreach (ControllerSetting setting in options.Settings)
                            {
                                try
                                {
                                    setting.ParseOrKeep(xmlSupport.SelectSingleNode(setting.XmlName)?.InnerText)?.Invoke();
                                }
                                catch { /* 欠けた・壊れた値は、既定値のまま続ける */ }
                            }
                        }
                    }

                    for (int i = 0; i < ControllerLimits.MaxControllerCount; i++)
                    {
                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/CustomLed" + (i + 1));
                            string[] ss = Item.Required().InnerText.Split(':');
                            bool.TryParse(ss[0], out store.lightbarSlots[i].Info.ds4winSettings.useCustomLed);
                            DS4Color.TryParse(ss[1], ref store.lightbarSlots[i].Info.ds4winSettings.m_CustomLed);
                        }
                        catch { store.lightbarSlots[i].Info.ds4winSettings.useCustomLed = false; store.lightbarSlots[i].Info.ds4winSettings.m_CustomLed = new DS4Color(Color.Blue); missingSetting = true; }
                    }
                }
            }
            catch { /* 欠けた・壊れた値は、既定値のまま続ける */ }

            if (missingSetting)
            {
                // Do not auto-save changes discovered during legacy load. Auto-saving
                // at startup may overwrite files and cause conflicts (file locks).
                AppLogger.LogUser(AppLogLevel.Warn, "Log_Xml_ProfilesMissingSettings");
            }

            if (Loaded)
            {
                LoadFakeExeName();
            }

            return Loaded;
        }

        private void LoadFakeExeName()
        {
            string custom_exe_name_path = Path.Combine(_pathService.ProgramDirectory, CustomExeConfigFileName);
            bool fakeExeFileExists = File.Exists(custom_exe_name_path);
            if (fakeExeFileExists)
            {
                string fake_exe_name = File.ReadAllText(custom_exe_name_path).Trim();
                bool valid = !(fake_exe_name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0);
                if (valid)
                {
                    store.Appearance.fakeExeFileName = fake_exe_name;
                }
            }
        }

        public bool Save()
        {
            // Use DTO-based serialization as canonical save format but keep compatibility
            // with older readers by writing a <Profile> root (same as legacy files).
            bool saved = true;
            string output_path = store.m_Profile;
            string testStr = string.Empty;

            XmlSerializer serializer = new XmlSerializer(typeof(AppSettingsDTO), new XmlRootAttribute("Profile"));
            AppSettingsDTO dto = new AppSettingsDTO();
            if (_profileIdentity != null) dto.ProfileIdOf = _profileIdentity.EnsurePersisted;   // Phase10-Step4d W4
            dto.MapFrom(store);
            testStr = XmlDtoText.Serialize(serializer, dto, String.Format(" Profile Configuration Data. {0} ", DateTime.Now));

            try
            {
                // The DTO now contains app_version/config_version as XmlAttributes, so write directly.
                using (StreamWriter sw = new StreamWriter(output_path, false))
                {
                    sw.Write(testStr);
                }
            }
            catch (UnauthorizedAccessException)
            {
                saved = false;
            }
            catch (IOException ex)
            {
                AppLogger.LogUser(AppLogLevel.Warn, "Log_Xml_ProfilesIoError", ex.Message);
                saved = false;
            }

            // Write fake exe config file if needed (same behavior as legacy save)
            bool adminNeeded = _environment.AdminNeeded();
            if (saved &&
                (!adminNeeded || (adminNeeded && _environment.IsAdministrator())))
            {
                string custom_exe_name_path = Path.Combine(_pathService.ProgramDirectory, CustomExeConfigFileName);
                bool fakeExeFileExists = File.Exists(custom_exe_name_path);
                if (!string.IsNullOrEmpty(store.Appearance.fakeExeFileName) || fakeExeFileExists)
                {
                    File.WriteAllText(custom_exe_name_path, store.Appearance.fakeExeFileName);
                }
            }

            return saved;
        }
    }
}
