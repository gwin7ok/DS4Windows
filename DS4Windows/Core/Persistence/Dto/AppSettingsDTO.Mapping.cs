using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.Dto;

/// <summary>BackingStore との対応（読み込み・書き出し）。XML の要素の順序は、プロパティの宣言の順で決まるため、プロパティは本体のファイルに残す。</summary>
public partial class AppSettingsDTO
{
    public void MapFrom(BackingStore source)
    {
        // ProfileEditorレイアウト情報の同期
        ProfileEditorLeftWidth = source.Layout.profileEditorLeftWidth > 0 ? source.Layout.profileEditorLeftWidth : BackingStore.DEFAULT_PROFILE_EDITOR_LEFT_WIDTH;
        ProfileEditorRightWidth = source.Layout.profileEditorRightWidth > 0 ? source.Layout.profileEditorRightWidth : BackingStore.DEFAULT_PROFILE_EDITOR_RIGHT_WIDTH;
        SpecialActionNameColWidth = source.Layout.specialActionNameColWidth > 0 ? source.Layout.specialActionNameColWidth : BackingStore.DEFAULT_SPECIAL_ACTION_NAME_COL_WIDTH;
        SpecialActionTriggerColWidth = source.Layout.specialActionTriggerColWidth > 0 ? source.Layout.specialActionTriggerColWidth : BackingStore.DEFAULT_SPECIAL_ACTION_TRIGGER_COL_WIDTH;
        SpecialActionDetailColWidth = source.Layout.specialActionDetailColWidth > 0 ? source.Layout.specialActionDetailColWidth : BackingStore.DEFAULT_SPECIAL_ACTION_DETAIL_COL_WIDTH;

        // Controller tab column widths
        ControllerIndexColWidth = source.Layout.controllerIndexColWidth > 0 ? source.Layout.controllerIndexColWidth : BackingStore.DEFAULT_CONTROLLER_INDEX_COL_WIDTH;
        ControllerIdColWidth = source.Layout.controllerIdColWidth > 0 ? source.Layout.controllerIdColWidth : BackingStore.DEFAULT_CONTROLLER_ID_COL_WIDTH;
        ControllerStatusColWidth = source.Layout.controllerStatusColWidth > 0 ? source.Layout.controllerStatusColWidth : BackingStore.DEFAULT_CONTROLLER_STATUS_COL_WIDTH;
        ControllerExclusiveColWidth = source.Layout.controllerExclusiveColWidth > 0 ? source.Layout.controllerExclusiveColWidth : BackingStore.DEFAULT_CONTROLLER_EXCLUSIVE_COL_WIDTH;
        ControllerBatteryColWidth = source.Layout.controllerBatteryColWidth > 0 ? source.Layout.controllerBatteryColWidth : BackingStore.DEFAULT_CONTROLLER_BATTERY_COL_WIDTH;
        ControllerSelectProfileColWidth = source.Layout.controllerSelectProfileColWidth > 0 ? source.Layout.controllerSelectProfileColWidth : BackingStore.DEFAULT_CONTROLLER_SELECTPROFILE_COL_WIDTH;
        ControllerEditColWidth = source.Layout.controllerEditColWidth > 0 ? source.Layout.controllerEditColWidth : BackingStore.DEFAULT_CONTROLLER_EDIT_COL_WIDTH;
        ControllerLinkedProfileColWidth = source.Layout.controllerLinkedProfileColWidth > 0 ? source.Layout.controllerLinkedProfileColWidth : BackingStore.DEFAULT_CONTROLLER_LINKED_PROFILE_COL_WIDTH;
        ControllerLinkProfIdColWidth = source.Layout.controllerLinkProfIdColWidth > 0 ? source.Layout.controllerLinkProfIdColWidth : BackingStore.DEFAULT_CONTROLLER_LINK_PROF_ID_COL_WIDTH;
        ControllerCustomColorColWidth = source.Layout.controllerCustomColorColWidth > 0 ? source.Layout.controllerCustomColorColWidth : BackingStore.DEFAULT_CONTROLLER_CUSTOMCOLOR_COL_WIDTH;
        OutputSlotNumColWidth = source.Layout.outputSlotNumColWidth > 0 ? source.Layout.outputSlotNumColWidth : BackingStore.DEFAULT_OUTPUT_SLOT_NUM_COL_WIDTH;
        OutputSlotInputNumColWidth = source.Layout.outputSlotInputNumColWidth > 0 ? source.Layout.outputSlotInputNumColWidth : BackingStore.DEFAULT_OUTPUT_SLOT_INPUT_NUM_COL_WIDTH;
        OutputSlotInputIdColWidth = source.Layout.outputSlotInputIdColWidth > 0 ? source.Layout.outputSlotInputIdColWidth : BackingStore.DEFAULT_OUTPUT_SLOT_INPUT_ID_COL_WIDTH;
        OutputSlotCurrentTypeColWidth = source.Layout.outputSlotCurrentTypeColWidth > 0 ? source.Layout.outputSlotCurrentTypeColWidth : BackingStore.DEFAULT_OUTPUT_SLOT_CURRENT_TYPE_COL_WIDTH;
        OutputSlotReservationColWidth = source.Layout.outputSlotReservationColWidth > 0 ? source.Layout.outputSlotReservationColWidth : BackingStore.DEFAULT_OUTPUT_SLOT_RESERVATION_COL_WIDTH;
        AutoProfileNameColWidth = source.Layout.autoProfileNameColWidth > 0 ? source.Layout.autoProfileNameColWidth : BackingStore.DEFAULT_AUTO_PROFILE_NAME_COL_WIDTH;
        AutoProfilePathColWidth = source.Layout.autoProfilePathColWidth > 0 ? source.Layout.autoProfilePathColWidth : BackingStore.DEFAULT_AUTO_PROFILE_PATH_COL_WIDTH;
        AutoProfileTitleColWidth = source.Layout.autoProfileTitleColWidth > 0 ? source.Layout.autoProfileTitleColWidth : BackingStore.DEFAULT_AUTO_PROFILE_TITLE_COL_WIDTH;
        LogTimeColWidth = source.Layout.logTimeColWidth > 0 ? source.Layout.logTimeColWidth : BackingStore.DEFAULT_LOG_TIME_COL_WIDTH;
        LogDataColWidth = source.Layout.logDataColWidth > 0 ? source.Layout.logDataColWidth : BackingStore.DEFAULT_LOG_DATA_COL_WIDTH;

        LogMaxArchiveFiles = source.Log.logMaxArchiveFiles > 0 ? source.Log.logMaxArchiveFiles : BackingStore.DEFAULT_LOG_MAX_ARCHIVE_FILES;
        LogMinLevel = !string.IsNullOrEmpty(source.Log.logMinLevel) ? source.Log.logMinLevel : BackingStore.DEFAULT_LOG_MIN_LEVEL;

        // ルート属性に現在のアプリ/設定バージョンをセット（シリアライザで属性として出力される）
        AppVersion = DS4Windows.Core.Platform.PathService.ExecutableVersionState;
        ConfigVersion = AppConfigVersion.ToString();
        UseExclusiveMode = source.Behavior.useExclusiveMode;
        StartMinimized = source.Behavior.startMinimized;
        MinimizeToTaskbar = source.Behavior.minToTaskbar;
        ProcessPriority = source.Behavior.processPriority;
        FormWidth = source.Layout.formWidth;
        FormHeight = source.Layout.formHeight;
        FormLocationX = source.Layout.formLocationX;
        FormLocationY = source.Layout.formLocationY;
        // Save currently selected profiles to Controller1-8
        Controller1CurrentProfile = !string.IsNullOrEmpty(source.GetPersistedProfileName(0)) ? source.GetPersistedProfileName(0) : string.Empty;
        Controller2CurrentProfile = !string.IsNullOrEmpty(source.GetPersistedProfileName(1)) ? source.GetPersistedProfileName(1) : string.Empty;
        Controller3CurrentProfile = !string.IsNullOrEmpty(source.GetPersistedProfileName(2)) ? source.GetPersistedProfileName(2) : string.Empty;
        Controller4CurrentProfile = !string.IsNullOrEmpty(source.GetPersistedProfileName(3)) ? source.GetPersistedProfileName(3) : string.Empty;
        Controller5CurrentProfile = !string.IsNullOrEmpty(source.GetPersistedProfileName(4)) ? source.GetPersistedProfileName(4) : string.Empty;
        Controller6CurrentProfile = !string.IsNullOrEmpty(source.GetPersistedProfileName(5)) ? source.GetPersistedProfileName(5) : string.Empty;
        Controller7CurrentProfile = !string.IsNullOrEmpty(source.GetPersistedProfileName(6)) ? source.GetPersistedProfileName(6) : string.Empty;
        Controller8CurrentProfile = !string.IsNullOrEmpty(source.GetPersistedProfileName(7)) ? source.GetPersistedProfileName(7) : string.Empty;

        // 選択しているプロファイルの固定 ID（名前の要素と同じ並び。ないスロットは空）
        if (ProfileIdOf != null)
        {
            var ids = new string[8];
            bool anyId = false;
            for (int i = 0; i < ids.Length; i++)
            {
                string selected = source.GetPersistedProfileName(i);
                string? id = !string.IsNullOrEmpty(selected) ? ProfileIdOf(selected) : null;
                ids[i] = id ?? string.Empty;
                anyId |= ids[i].Length > 0;
            }

            ControllerProfileIds = anyId ? string.Join("/", ids) : string.Empty;
        }

        LastChecked = source.UpdateCheck.lastChecked;
        // Map legacy CheckWhen into new fields if present in source profiles.
        // Prefer explicit fields when available (checkEveryValue/checkEveryUnit).
        CheckEveryValue = source.UpdateCheck.checkEveryValue;
        CheckEveryUnit = source.UpdateCheck.checkEveryUnit;
        // Map new explicit startup-check settings when available
        CheckUpdateStartup = source.UpdateCheck.checkUpdateStartup;
        CheckEveryValue = source.UpdateCheck.checkEveryValue;
        CheckEveryUnit = source.UpdateCheck.checkEveryUnit;
        LastVersionChecked = source.UpdateCheck.lastVersionChecked;
        Notifications = source.Behavior.notifications;
        ProfileChangedNotification = source.Behavior.profileChangedNotification;
        DisconnectBTAtStop = source.Behavior.disconnectBTAtStop;
        SwipeProfiles = source.Behavior.swipeProfiles;
        QuickCharge = source.Behavior.quickCharge;
        UseMoonlight = source.Behavior.useMoonlight;
        UseAdvancedMoonlight = source.Behavior.useAdvancedMoonlight;
        CloseMinimizes = source.Behavior.closeMini;
        UseLang = source.Appearance.useLang;
        DownloadLang = source.Appearance.downloadLang;
        FlashWhenLate = source.Behavior.flashWhenLate;
        FlashWhenLateAt = source.Behavior.flashWhenLateAt;
        AppIcon = source.Appearance.useIconChoice;
        AppTheme = source.Appearance.useCurrentTheme;
        UseOSCServer = source.Network.useOSCServ;
        OSCServerPort = source.Network.oscServPort;
        InterpretingOscMonitoring = source.Network.interpretingOscMonitoring;
        UseOSCSender = source.Network.useOSCSend;
        OSCSenderPort = source.Network.oscSendPort;
        OSCSenderAddress = source.Network.oscSendAddress;
        UseUDPServer = source.Network.useUDPServ;
        UDPServerPort = source.Network.udpServPort;
        UDPServerListenAddress = source.Network.udpServListenAddress;
        UDPServerSmoothingOptions = new UDPSrvSmoothingOptionsGroup()
        {
            UseSmoothing = source.Network.useUdpSmoothing,
            UdpSmoothMinCutoff = source.Network.udpSmoothingMincutoff,
            UdpSmoothBeta = source.Network.udpSmoothingBeta,
        };
        UseCustomSteamFolder = source.Network.useCustomSteamFolder;
        CustomSteamFolder = source.Network.customSteamFolder;
        AutoProfileRevertDefaultProfile = source.Behavior.autoProfileRevertDefaultProfile;
        AutoProfileSwitchNotifyChoice = source.autoProfileSwitchNotifyChoice;

        DeviceOptions = new InputDeviceOptions();
        DeviceOptions.Options.CopyValuesFrom(source.deviceOptions);

        LightbarDS4WinInfo[] tempLightArray = new LightbarDS4WinInfo[]
        {
                LightbarInfo1, LightbarInfo2, LightbarInfo3, LightbarInfo4,
                LightbarInfo5, LightbarInfo6, LightbarInfo7, LightbarInfo8,
        };

        for (int i = 0; i < ControllerLimits.MaxControllerCount; i++)
        {
            LightbarDS4WinInfo lightbarDS4Win = source.ObtainLightbarDS4WinInfo(i);
            LightbarDS4WinInfo tempInstance = tempLightArray[i];
            tempInstance.useCustomLed = lightbarDS4Win.useCustomLed;
            tempInstance.m_CustomLed = lightbarDS4Win.m_CustomLed;
        }
    }

    public void MapTo(BackingStore destination)
    {
        // ProfileEditorレイアウト情報の同期
        destination.Layout.profileEditorLeftWidth = ProfileEditorLeftWidth > 0 ? ProfileEditorLeftWidth : BackingStore.DEFAULT_PROFILE_EDITOR_LEFT_WIDTH;
        destination.Layout.profileEditorRightWidth = ProfileEditorRightWidth > 0 ? ProfileEditorRightWidth : BackingStore.DEFAULT_PROFILE_EDITOR_RIGHT_WIDTH;
        destination.Layout.specialActionNameColWidth = SpecialActionNameColWidth > 0 ? SpecialActionNameColWidth : BackingStore.DEFAULT_SPECIAL_ACTION_NAME_COL_WIDTH;
        destination.Layout.specialActionTriggerColWidth = SpecialActionTriggerColWidth > 0 ? SpecialActionTriggerColWidth : BackingStore.DEFAULT_SPECIAL_ACTION_TRIGGER_COL_WIDTH;
        destination.Layout.specialActionDetailColWidth = SpecialActionDetailColWidth > 0 ? SpecialActionDetailColWidth : BackingStore.DEFAULT_SPECIAL_ACTION_DETAIL_COL_WIDTH;

        // Controller tab column widths
        destination.Layout.controllerIndexColWidth = ControllerIndexColWidth > 0 ? ControllerIndexColWidth : BackingStore.DEFAULT_CONTROLLER_INDEX_COL_WIDTH;
        destination.Layout.controllerIdColWidth = ControllerIdColWidth > 0 ? ControllerIdColWidth : BackingStore.DEFAULT_CONTROLLER_ID_COL_WIDTH;
        destination.Layout.controllerStatusColWidth = ControllerStatusColWidth > 0 ? ControllerStatusColWidth : BackingStore.DEFAULT_CONTROLLER_STATUS_COL_WIDTH;
        destination.Layout.controllerExclusiveColWidth = ControllerExclusiveColWidth > 0 ? ControllerExclusiveColWidth : BackingStore.DEFAULT_CONTROLLER_EXCLUSIVE_COL_WIDTH;
        destination.Layout.controllerBatteryColWidth = ControllerBatteryColWidth > 0 ? ControllerBatteryColWidth : BackingStore.DEFAULT_CONTROLLER_BATTERY_COL_WIDTH;
        destination.Layout.controllerSelectProfileColWidth = ControllerSelectProfileColWidth > 0 ? ControllerSelectProfileColWidth : BackingStore.DEFAULT_CONTROLLER_SELECTPROFILE_COL_WIDTH;
        destination.Layout.controllerEditColWidth = ControllerEditColWidth > 0 ? ControllerEditColWidth : BackingStore.DEFAULT_CONTROLLER_EDIT_COL_WIDTH;
        destination.Layout.controllerLinkedProfileColWidth = ControllerLinkedProfileColWidth > 0 ? ControllerLinkedProfileColWidth : BackingStore.DEFAULT_CONTROLLER_LINKED_PROFILE_COL_WIDTH;
        destination.Layout.controllerLinkProfIdColWidth = ControllerLinkProfIdColWidth > 0 ? ControllerLinkProfIdColWidth : BackingStore.DEFAULT_CONTROLLER_LINK_PROF_ID_COL_WIDTH;
        destination.Layout.controllerCustomColorColWidth = ControllerCustomColorColWidth > 0 ? ControllerCustomColorColWidth : BackingStore.DEFAULT_CONTROLLER_CUSTOMCOLOR_COL_WIDTH;
        destination.Layout.outputSlotNumColWidth = OutputSlotNumColWidth > 0 ? OutputSlotNumColWidth : BackingStore.DEFAULT_OUTPUT_SLOT_NUM_COL_WIDTH;
        destination.Layout.outputSlotInputNumColWidth = OutputSlotInputNumColWidth > 0 ? OutputSlotInputNumColWidth : BackingStore.DEFAULT_OUTPUT_SLOT_INPUT_NUM_COL_WIDTH;
        destination.Layout.outputSlotInputIdColWidth = OutputSlotInputIdColWidth > 0 ? OutputSlotInputIdColWidth : BackingStore.DEFAULT_OUTPUT_SLOT_INPUT_ID_COL_WIDTH;
        destination.Layout.outputSlotCurrentTypeColWidth = OutputSlotCurrentTypeColWidth > 0 ? OutputSlotCurrentTypeColWidth : BackingStore.DEFAULT_OUTPUT_SLOT_CURRENT_TYPE_COL_WIDTH;
        destination.Layout.outputSlotReservationColWidth = OutputSlotReservationColWidth > 0 ? OutputSlotReservationColWidth : BackingStore.DEFAULT_OUTPUT_SLOT_RESERVATION_COL_WIDTH;
        destination.Layout.autoProfileNameColWidth = AutoProfileNameColWidth > 0 ? AutoProfileNameColWidth : BackingStore.DEFAULT_AUTO_PROFILE_NAME_COL_WIDTH;
        destination.Layout.autoProfilePathColWidth = AutoProfilePathColWidth > 0 ? AutoProfilePathColWidth : BackingStore.DEFAULT_AUTO_PROFILE_PATH_COL_WIDTH;
        destination.Layout.autoProfileTitleColWidth = AutoProfileTitleColWidth > 0 ? AutoProfileTitleColWidth : BackingStore.DEFAULT_AUTO_PROFILE_TITLE_COL_WIDTH;
        destination.Layout.logTimeColWidth = LogTimeColWidth > 0 ? LogTimeColWidth : BackingStore.DEFAULT_LOG_TIME_COL_WIDTH;
        destination.Layout.logDataColWidth = LogDataColWidth > 0 ? LogDataColWidth : BackingStore.DEFAULT_LOG_DATA_COL_WIDTH;

        destination.Log.logMaxArchiveFiles = LogMaxArchiveFiles > 0 ? LogMaxArchiveFiles : BackingStore.DEFAULT_LOG_MAX_ARCHIVE_FILES;
        destination.Log.logMinLevel = !string.IsNullOrEmpty(LogMinLevel) ? LogMinLevel : BackingStore.DEFAULT_LOG_MIN_LEVEL;

        destination.Behavior.useExclusiveMode = UseExclusiveMode;
        destination.Behavior.startMinimized = StartMinimized;
        destination.Behavior.minToTaskbar = MinimizeToTaskbar;
        destination.Behavior.processPriority = ProcessPriority;
        destination.Layout.formWidth = FormWidth;
        // Map new explicit startup-check settings back into backing store
        destination.UpdateCheck.checkUpdateStartup = CheckUpdateStartup;
        destination.UpdateCheck.checkEveryValue = CheckEveryValue;
        destination.UpdateCheck.checkEveryUnit = CheckEveryUnit;
        destination.Layout.formHeight = FormHeight;
        destination.Layout.formLocationX = FormLocationX;
        destination.Layout.formLocationY = FormLocationY;
        // Load Controller1-8 to both profilePath/olderProfilePath and selectedProfile
        destination.profilePath[0] = destination.olderProfilePath[0] = destination.selectedProfile[0] = !string.IsNullOrEmpty(Controller1CurrentProfile) ? Controller1CurrentProfile : string.Empty;
        destination.profilePath[1] = destination.olderProfilePath[1] = destination.selectedProfile[1] = !string.IsNullOrEmpty(Controller2CurrentProfile) ? Controller2CurrentProfile : string.Empty;
        destination.profilePath[2] = destination.olderProfilePath[2] = destination.selectedProfile[2] = !string.IsNullOrEmpty(Controller3CurrentProfile) ? Controller3CurrentProfile : string.Empty;
        destination.profilePath[3] = destination.olderProfilePath[3] = destination.selectedProfile[3] = !string.IsNullOrEmpty(Controller4CurrentProfile) ? Controller4CurrentProfile : string.Empty;
        destination.profilePath[4] = destination.olderProfilePath[4] = destination.selectedProfile[4] = !string.IsNullOrEmpty(Controller5CurrentProfile) ? Controller5CurrentProfile : string.Empty;
        destination.profilePath[5] = destination.olderProfilePath[5] = destination.selectedProfile[5] = !string.IsNullOrEmpty(Controller6CurrentProfile) ? Controller6CurrentProfile : string.Empty;
        destination.profilePath[6] = destination.olderProfilePath[6] = destination.selectedProfile[6] = !string.IsNullOrEmpty(Controller7CurrentProfile) ? Controller7CurrentProfile : string.Empty;
        destination.profilePath[7] = destination.olderProfilePath[7] = destination.selectedProfile[7] = !string.IsNullOrEmpty(Controller8CurrentProfile) ? Controller8CurrentProfile : string.Empty;

        // ID が見つかれば、現在の名前に置き換える（名前変更に追従。ID が勝つ）。ない・見つからないときは、名前のまま
        if (ProfileNameOf != null && !string.IsNullOrEmpty(ControllerProfileIds))
        {
            string[] ids = ControllerProfileIds.Split('/');
            for (int i = 0; i < ids.Length && i < 8; i++)
            {
                if (ids[i].Length == 0) continue;

                string? resolved = ProfileNameOf(ids[i]);
                if (!string.IsNullOrEmpty(resolved))
                    destination.profilePath[i] = destination.olderProfilePath[i] = destination.selectedProfile[i] = resolved;
            }
        }

        destination.UpdateCheck.lastChecked = LastChecked;
        // Persist new explicit startup-check settings in place of legacy CheckWhen.
        destination.UpdateCheck.checkEveryValue = CheckEveryValue;
        destination.UpdateCheck.checkEveryUnit = CheckEveryUnit;
        destination.UpdateCheck.lastVersionChecked = LastVersionChecked;
        destination.Behavior.notifications = Notifications;
        destination.Behavior.profileChangedNotification = ProfileChangedNotification;
        destination.Behavior.disconnectBTAtStop = DisconnectBTAtStop;
        destination.Behavior.swipeProfiles = SwipeProfiles;
        destination.Behavior.quickCharge = QuickCharge;
        destination.Behavior.useMoonlight = UseMoonlight;
        destination.Behavior.useAdvancedMoonlight = UseAdvancedMoonlight;
        destination.Behavior.closeMini = CloseMinimizes;
        destination.Appearance.useLang = UseLang;
        destination.Appearance.downloadLang = DownloadLang;
        destination.Behavior.flashWhenLate = FlashWhenLate;
        destination.Behavior.flashWhenLateAt = FlashWhenLateAt;
        destination.Appearance.useIconChoice = AppIcon;
        destination.Appearance.useCurrentTheme = AppTheme;
        destination.Network.useOSCServ = UseOSCServer;
        destination.Network.oscServPort = OSCServerPort;
        destination.Network.interpretingOscMonitoring = InterpretingOscMonitoring;
        destination.Network.useOSCSend = UseOSCSender;
        destination.Network.oscSendPort = OSCSenderPort;
        if (!string.IsNullOrEmpty(OSCSenderAddress))
        {
            destination.Network.oscSendAddress = OSCSenderAddress;
        }

        destination.Network.useUDPServ = UseUDPServer;
        destination.Network.udpServPort = UDPServerPort;

        if (!string.IsNullOrEmpty(UDPServerListenAddress))
        {
            destination.Network.udpServListenAddress = UDPServerListenAddress;
        }
        destination.Network.useUdpSmoothing = UDPServerSmoothingOptions.UseSmoothing;
        destination.Network.udpSmoothingMincutoff = UDPServerSmoothingOptions.UdpSmoothMinCutoff;
        destination.Network.udpSmoothingBeta = UDPServerSmoothingOptions.UdpSmoothBeta;

        destination.Network.useCustomSteamFolder = UseCustomSteamFolder;
        destination.Network.customSteamFolder = CustomSteamFolder;
        destination.Behavior.autoProfileRevertDefaultProfile = AutoProfileRevertDefaultProfile;
        destination.autoProfileSwitchNotifyChoice = AutoProfileSwitchNotifyChoice;

        destination.deviceOptions.CopyValuesFrom(DeviceOptions.Options);

        LightbarDS4WinInfo[] tempLightArray = new LightbarDS4WinInfo[]
        {
                LightbarInfo1, LightbarInfo2, LightbarInfo3, LightbarInfo4,
                LightbarInfo5, LightbarInfo6, LightbarInfo7, LightbarInfo8,
        };

        for (int i = 0; i < ControllerLimits.MaxControllerCount; i++)
        {
            LightbarDS4WinInfo tempInstance = tempLightArray[i];
            destination.PopulateLightbarDS4WinInfo(i, tempInstance);
        }
    }
}
