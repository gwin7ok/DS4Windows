using System;
using System.Xml.Serialization;
using System.Globalization;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.Dto;
/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

public partial class AppSettingsDTO
{
    // 旧 Global.APP_CONFIG_VERSION（利用はこのクラスのみ）。
    public const int AppConfigVersion = 2;

    // ここに既存の全てのプロパティ・メソッド・フィールド・コンストラクタを収める

    [XmlElement("formWidth")]
    public int FormWidth { get; set; } = BackingStore.DEFAULT_FORM_WIDTH;

    [XmlElement("formHeight")]
    public int FormHeight { get; set; } = BackingStore.DEFAULT_FORM_HEIGHT;

    [XmlElement("formLocationX")]
    public int FormLocationX { get; set; } = BackingStore.DEFAULT_FORM_LOCATION_X;

    [XmlElement("formLocationY")]
    public int FormLocationY { get; set; } = BackingStore.DEFAULT_FORM_LOCATION_Y;

    // ウィンドウ状態管理プロパティ
    public bool UseExclusiveMode { get; set; }
    public bool StartMinimized { get; set; }
    public bool MinimizeToTaskbar { get; set; }

    // プロフィール編集画面 Splitter位置・列幅
    [XmlElement("profileEditorLeftWidth")]
    public int ProfileEditorLeftWidth { get; set; } = BackingStore.DEFAULT_PROFILE_EDITOR_LEFT_WIDTH;

    [XmlElement("profileEditorRightWidth")]
    public int ProfileEditorRightWidth { get; set; } = BackingStore.DEFAULT_PROFILE_EDITOR_RIGHT_WIDTH;

    [XmlElement("specialActionNameColWidth")]
    public int SpecialActionNameColWidth { get; set; } = BackingStore.DEFAULT_SPECIAL_ACTION_NAME_COL_WIDTH;

    [XmlElement("specialActionTriggerColWidth")]
    public int SpecialActionTriggerColWidth { get; set; } = BackingStore.DEFAULT_SPECIAL_ACTION_TRIGGER_COL_WIDTH;

    [XmlElement("specialActionDetailColWidth")]
    public int SpecialActionDetailColWidth { get; set; } = BackingStore.DEFAULT_SPECIAL_ACTION_DETAIL_COL_WIDTH;

    // Controller tab column widths
    [XmlElement("controllerIndexColWidth")]
    public int ControllerIndexColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_INDEX_COL_WIDTH;

    [XmlElement("controllerIdColWidth")]
    public int ControllerIdColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_ID_COL_WIDTH;

    [XmlElement("controllerStatusColWidth")]
    public int ControllerStatusColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_STATUS_COL_WIDTH;

    [XmlElement("controllerExclusiveColWidth")]
    public int ControllerExclusiveColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_EXCLUSIVE_COL_WIDTH;

    [XmlElement("controllerBatteryColWidth")]
    public int ControllerBatteryColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_BATTERY_COL_WIDTH;

    [XmlElement("controllerSelectProfileColWidth")]
    public int ControllerSelectProfileColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_SELECTPROFILE_COL_WIDTH;

    [XmlElement("controllerEditColWidth")]
    public int ControllerEditColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_EDIT_COL_WIDTH;

    [XmlElement("controllerLinkedProfileColWidth")]
    public int ControllerLinkedProfileColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_LINKED_PROFILE_COL_WIDTH;

    [XmlElement("controllerLinkProfIdColWidth")]
    public int ControllerLinkProfIdColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_LINK_PROF_ID_COL_WIDTH;

    [XmlElement("controllerCustomColorColWidth")]
    public int ControllerCustomColorColWidth { get; set; } = BackingStore.DEFAULT_CONTROLLER_CUSTOMCOLOR_COL_WIDTH;

    // Output Slots tab column widths
    [XmlElement("outputSlotNumColWidth")]
    public int OutputSlotNumColWidth { get; set; } = BackingStore.DEFAULT_OUTPUT_SLOT_NUM_COL_WIDTH;

    [XmlElement("outputSlotInputNumColWidth")]
    public int OutputSlotInputNumColWidth { get; set; } = BackingStore.DEFAULT_OUTPUT_SLOT_INPUT_NUM_COL_WIDTH;

    [XmlElement("outputSlotInputIdColWidth")]
    public int OutputSlotInputIdColWidth { get; set; } = BackingStore.DEFAULT_OUTPUT_SLOT_INPUT_ID_COL_WIDTH;

    [XmlElement("outputSlotCurrentTypeColWidth")]
    public int OutputSlotCurrentTypeColWidth { get; set; } = BackingStore.DEFAULT_OUTPUT_SLOT_CURRENT_TYPE_COL_WIDTH;

    [XmlElement("outputSlotReservationColWidth")]
    public int OutputSlotReservationColWidth { get; set; } = BackingStore.DEFAULT_OUTPUT_SLOT_RESERVATION_COL_WIDTH;

    // Auto Profiles / Log tab column widths
    [XmlElement("autoProfileNameColWidth")]
    public int AutoProfileNameColWidth { get; set; } = BackingStore.DEFAULT_AUTO_PROFILE_NAME_COL_WIDTH;

    [XmlElement("autoProfilePathColWidth")]
    public int AutoProfilePathColWidth { get; set; } = BackingStore.DEFAULT_AUTO_PROFILE_PATH_COL_WIDTH;

    [XmlElement("autoProfileTitleColWidth")]
    public int AutoProfileTitleColWidth { get; set; } = BackingStore.DEFAULT_AUTO_PROFILE_TITLE_COL_WIDTH;

    [XmlElement("logTimeColWidth")]
    public int LogTimeColWidth { get; set; } = BackingStore.DEFAULT_LOG_TIME_COL_WIDTH;

    [XmlElement("logDataColWidth")]
    public int LogDataColWidth { get; set; } = BackingStore.DEFAULT_LOG_DATA_COL_WIDTH;

    [XmlElement("logMaxArchiveFiles")]
    public int LogMaxArchiveFiles { get; set; } = BackingStore.DEFAULT_LOG_MAX_ARCHIVE_FILES;

    [XmlElement("logMinLevel")]
    public string LogMinLevel { get; set; } = BackingStore.DEFAULT_LOG_MIN_LEVEL;

    // ルート属性として保存するアプリ/設定バージョン
    [XmlAttribute("app_version")]
    public string? AppVersion { get; set; }

    [XmlAttribute("config_version")]
    public string? ConfigVersion { get; set; }

    [XmlElement("ProcessPriority")]
    public int ProcessPriority { get; set; }

    [XmlElement("Controller1")]
    public string? Controller1CurrentProfile
    {
        get; set;
    }
    public bool ShouldSerializeController1CurrentProfile()
    {
        return !string.IsNullOrEmpty(Controller1CurrentProfile);
    }

    [XmlElement("Controller2")]
    public string? Controller2CurrentProfile
    {
        get; set;
    }
    public bool ShouldSerializeController2CurrentProfile()
    {
        return !string.IsNullOrEmpty(Controller2CurrentProfile);
    }

    [XmlElement("Controller3")]
    public string? Controller3CurrentProfile
    {
        get; set;
    }
    public bool ShouldSerializeController3CurrentProfile()
    {
        return !string.IsNullOrEmpty(Controller3CurrentProfile);
    }

    [XmlElement("Controller4")]
    public string? Controller4CurrentProfile
    {
        get; set;
    }
    public bool ShouldSerializeController4CurrentProfile()
    {
        return !string.IsNullOrEmpty(Controller4CurrentProfile);
    }

    [XmlElement("Controller5")]
    public string? Controller5CurrentProfile
    {
        get; set;
    }
    public bool ShouldSerializeController5CurrentProfile()
    {
        return !string.IsNullOrEmpty(Controller5CurrentProfile) &&
            ControllerLimits.MaxControllerCount >= 5;
    }

    [XmlElement("Controller6")]
    public string? Controller6CurrentProfile
    {
        get; set;
    }
    public bool ShouldSerializeController6CurrentProfile()
    {
        return !string.IsNullOrEmpty(Controller6CurrentProfile) &&
            ControllerLimits.MaxControllerCount >= 6;
    }

    [XmlElement("Controller7")]
    public string? Controller7CurrentProfile
    {
        get; set;
    }
    public bool ShouldSerializeController7CurrentProfile()
    {
        return !string.IsNullOrEmpty(Controller7CurrentProfile) &&
            ControllerLimits.MaxControllerCount >= 7;
    }

    [XmlElement("Controller8")]
    public string? Controller8CurrentProfile
    {
        get; set;
    }
    public bool ShouldSerializeController8CurrentProfile()
    {
        return !string.IsNullOrEmpty(Controller8CurrentProfile) &&
            ControllerLimits.MaxControllerCount >= 8;
    }

    // 各スロットの選択（Controller1〜8）のプロファイルの固定 ID を、同じ並びで「/」区切りに書く（名前の要素は、古いバージョンのために残す）。
    // 読み込みは、ID を主にして、現在の名前に引き直す（プロファイルの名前変更に追従する。ID が勝つ）。ない・見つからないときは、名前のまま。1 つも ID がなければ、書かない
    [XmlElement("ControllerProfileIds")]
    public string? ControllerProfileIds { get; set; }
    public bool ShouldSerializeControllerProfileIds() => !string.IsNullOrEmpty(ControllerProfileIds);

    /// <summary>名前 → ID（保存で使う）。省略可（登録簿がなければ、ID を書かない）。シリアライザーが設定する。</summary>
    [XmlIgnore]
    public Func<string, string?>? ProfileIdOf { get; set; }

    /// <summary>ID → 現在の名前（読み込みで使う）。見つからなければ null を返す。省略可。シリアライザーが設定する。</summary>
    [XmlIgnore]
    public Func<string, string?>? ProfileNameOf { get; set; }

    // Selected Profile settings
    [XmlElement("SelectedProfile1")]
    public string? SelectedProfile1 { get; set; }
    public bool ShouldSerializeSelectedProfile1() => !string.IsNullOrEmpty(SelectedProfile1);

    [XmlElement("SelectedProfile2")]
    public string? SelectedProfile2 { get; set; }
    public bool ShouldSerializeSelectedProfile2() => !string.IsNullOrEmpty(SelectedProfile2);

    [XmlElement("SelectedProfile3")]
    public string? SelectedProfile3 { get; set; }
    public bool ShouldSerializeSelectedProfile3() => !string.IsNullOrEmpty(SelectedProfile3);

    [XmlElement("SelectedProfile4")]
    public string? SelectedProfile4 { get; set; }
    public bool ShouldSerializeSelectedProfile4() => !string.IsNullOrEmpty(SelectedProfile4);

    [XmlElement("SelectedProfile5")]
    public string? SelectedProfile5 { get; set; }
    public bool ShouldSerializeSelectedProfile5() => !string.IsNullOrEmpty(SelectedProfile5) && ControllerLimits.MaxControllerCount >= 5;

    [XmlElement("SelectedProfile6")]
    public string? SelectedProfile6 { get; set; }
    public bool ShouldSerializeSelectedProfile6() => !string.IsNullOrEmpty(SelectedProfile6) && ControllerLimits.MaxControllerCount >= 6;

    [XmlElement("SelectedProfile7")]
    public string? SelectedProfile7 { get; set; }
    public bool ShouldSerializeSelectedProfile7() => !string.IsNullOrEmpty(SelectedProfile7) && ControllerLimits.MaxControllerCount >= 7;

    [XmlElement("SelectedProfile8")]
    public string? SelectedProfile8 { get; set; }
    public bool ShouldSerializeSelectedProfile8() => !string.IsNullOrEmpty(SelectedProfile8) && ControllerLimits.MaxControllerCount >= 8;

    [XmlIgnore]
    public DateTime LastChecked
    {
        get; private set;
    }

    [XmlElement("LastChecked")]
    public string LastCheckString
    {
        get => LastChecked.ToString("MM/dd/yyyy HH:mm:ss");
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // leave LastChecked as default (DateTime.MinValue) if empty
                return;
            }

            DateTime temp;
            // Try several common formats using invariant culture first to avoid culture-specific parsing issues.
            string[] formats = new[] {
                    "MM/dd/yyyy HH:mm:ss",
                    "M/d/yyyy H:mm:ss",
                    "yyyy-MM-ddTHH:mm:ss",
                    "yyyy-MM-dd HH:mm:ss",
                    "yyyy/MM/dd HH:mm:ss",
                    "yyyy-MM-ddTHH:mm:ssZ",
                    "o", // round-trip ISO 8601
                    "s"  // sortable
                };

            if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out temp))
            {
                LastChecked = temp;
                return;
            }

            // Fallback to invariant culture parse, then to current culture.
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out temp) ||
                DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out temp))
            {
                LastChecked = temp;
            }
            // If parsing fails, keep existing LastChecked (no throw/log here to keep DTO lightweight).
        }
    }

    // Removed legacy `CheckWhen`. Use `CheckEveryValue`/`CheckEveryUnit` instead.

    // New separate fields for startup update checking persistence
    [XmlIgnore]
    public bool CheckUpdateStartup { get; private set; } = true;

    [XmlElement("CheckUpdateStartup")]
    public string CheckUpdateStartupString
    {
        get => CheckUpdateStartup.ToString();
        set
        {
            if (bool.TryParse(value, out bool temp))
            {
                CheckUpdateStartup = temp;
            }
        }
    }

    [XmlElement("CheckEveryValue")]
    public int CheckEveryValue { get; set; } = 1;

    // 0 = hours, 1 = days
    [XmlElement("CheckEveryUnit")]
    public int CheckEveryUnit { get; set; } = 1;

    public string LastVersionChecked
    {
        get; set;
    } = string.Empty;
    public bool ShouldSerializeLastVersionChecked()
    {
        return !string.IsNullOrEmpty(LastVersionChecked);
    }

    [XmlIgnore]
    public int Notifications
    {
        get; private set;
    } = BackingStore.DEFAULT_NOTIFICATIONS;

    [XmlElement("Notifications")]
    public string NotificationsString
    {
        get => Notifications.ToString();
        set
        {
            if (int.TryParse(value, out int tempNum))
            {
                Notifications = tempNum;
            }
            else
            {
                if (bool.TryParse(value, out bool temp))
                {
                    Notifications = temp ? 2 : 0;
                }
            }
        }
    }

    [XmlIgnore]
    public bool ProfileChangedNotification
    {
        get; private set;
    }

    [XmlElement("ProfileChangedNotification")]
    public string ProfileChangedNotificationString
    {
        get => ProfileChangedNotification.ToString();
        set
        {
            if (bool.TryParse(value, out var input))
            {
                ProfileChangedNotification = input;
            }
        }
    }

    [XmlIgnore]
    public bool DisconnectBTAtStop
    {
        get; private set;
    }

    [XmlElement("DisconnectBTAtStop")]
    public string DisconnectBTAtStopString
    {
        get => DisconnectBTAtStop.ToString();
        set
        {
            if (bool.TryParse(value, out bool temp))
            {
                DisconnectBTAtStop = temp;
            }
        }
    }

    [XmlIgnore]
    public bool SwipeProfiles
    {
        get; private set;
    } = BackingStore.DEFAULT_SWIPE_PROFILES;

    [XmlElement("SwipeProfiles")]
    public string SwipeProfilesString
    {
        get => SwipeProfiles.ToString();
        set
        {
            if (bool.TryParse(value, out bool temp))
            {
                SwipeProfiles = temp;
            }
        }
    }

    [XmlIgnore]
    public bool QuickCharge
    {
        get; private set;
    }

    [XmlElement("QuickCharge")]
    public string QuickChargeString
    {
        get => QuickCharge.ToString();
        set
        {
            if (bool.TryParse(value, out bool temp))
            {
                QuickCharge = temp;
            }
        }
    }

    [XmlElement("UseMoonlight")]
    public bool UseMoonlight
    {
        get;
        set;
    }

    [XmlElement("UseAdvancedMoonlight")]
    public bool UseAdvancedMoonlight
    {
        get;
        set;
    }

    [XmlIgnore]
    public bool CloseMinimizes
    {
        get; private set;
    }

    [XmlElement("CloseMinimizes")]
    public string CloseMinimizesString
    {
        get => CloseMinimizes.ToString();
        set
        {
            if (bool.TryParse(value, out bool temp))
            {
                CloseMinimizes = temp;
            }
        }
    }

    // Default UseLang to empty string. Not null
    public string UseLang
    {
        get; set;
    } = "";

    [XmlIgnore]
    public bool DownloadLang
    {
        get; private set;
    }

    [XmlElement("DownloadLang")]
    public string DownloadLangString
    {
        get => DownloadLang.ToString();
        set
        {
            if (bool.TryParse(value, out bool temp))
            {
                DownloadLang = temp;
            }
        }
    }

    [XmlIgnore]
    public bool FlashWhenLate
    {
        get; private set;
    } = BackingStore.DEFAULT_FLASH_WHEN_LATE;

    [XmlElement("FlashWhenLate")]
    public string FlashWhenLateString
    {
        get => FlashWhenLate.ToString();
        set
        {
            if (bool.TryParse(value, out bool temp))
            {
                FlashWhenLate = temp;
            }
        }
    }

    public int FlashWhenLateAt
    {
        get; set;
    } = BackingStore.DEFAULT_FLASH_WHEN_LATE_AT;

    [XmlIgnore]
    public TrayIconChoice AppIcon
    {
        get; private set;
    }

    [XmlElement("AppIcon")]
    public string AppIconString
    {
        get => AppIcon.ToString();
        set
        {
            if (Enum.TryParse(value, out TrayIconChoice temp))
            {
                AppIcon = temp;
            }
        }
    }

    [XmlIgnore]
    public AppThemeChoice AppTheme
    {
        get; set;
    }

    [XmlElement("AppTheme")]
    public string AppThemeString
    {
        get => AppTheme.ToString();
        set
        {
            if (Enum.TryParse(value, out AppThemeChoice temp))
            {
                AppTheme = temp;
            }
        }
    }

    [XmlIgnore]
    public bool UseOSCServer
    {
        get; private set;
    }

    [XmlElement("UseOSCServer")]
    public string UseOSCServerString
    {
        get => UseOSCServer.ToString();
        set
        {
            if (bool.TryParse(value, out bool temp))
            {
                UseOSCServer = temp;
            }
        }
    }

    public int OSCServerPort
    {
        get; set;
    } = BackingStore.DEFAULT_OSC_SERV_PORT;

    [XmlIgnore]
    public bool UseOSCSender
    {
        get; private set;
    }

    [XmlIgnore]
    public bool InterpretingOscMonitoring
    {
        get; private set;
    }

    [XmlElement("InterpretingOscMonitoring")]
    public string InterpretingOscMonitoringString
    {
        get => InterpretingOscMonitoring.ToString();
        set
        {
            if (bool.TryParse(value, out bool temp))
            {
                InterpretingOscMonitoring = temp;
            }
        }
    }

    [XmlElement("UseOSCSender")]
    public string UseOSCSenderString
    {
        get => UseOSCSender.ToString();
        set
        {
            if (bool.TryParse(value, out bool temp))
            {
                UseOSCSender = temp;
            }
        }
    }

    public int OSCSenderPort
    {
        get; set;
    } = BackingStore.DEFAULT_OSC_SEND_PORT;

    public string OSCSenderAddress
    {
        get; set;
    } = BackingStore.DEFAULT_OSC_SEND_ADDRESS;

    [XmlIgnore]
    public bool UseUDPServer
    {
        get; private set;
    }

    [XmlElement("UseUDPServer")]
    public string UseUDPServerString
    {
        get => UseUDPServer.ToString();
        set
        {
            if (bool.TryParse(value, out bool temp))
            {
                UseUDPServer = temp;
            }
        }
    }

    public int UDPServerPort
    {
        get; set;
    } = BackingStore.DEFAULT_UDP_SERV_PORT;

    public string UDPServerListenAddress
    {
        get; set;
    } = BackingStore.DEFAULT_UDP_SERV_LISTEN_ADDR;

    public UDPSrvSmoothingOptionsGroup UDPServerSmoothingOptions
    {
        get; set;
    }

    [XmlIgnore]
    public bool UseCustomSteamFolder
    {
        get; private set;
    }

    [XmlElement("UseCustomSteamFolder")]
    public string UseCustomSteamFolderString
    {
        get => UseCustomSteamFolder.ToString();
        set
        {
            if (bool.TryParse(value, out bool temp))
            {
                UseCustomSteamFolder = temp;
            }
        }
    }

    public string? CustomSteamFolder
    {
        get; set;
    } = string.Empty;

    [XmlIgnore]
    public bool AutoProfileRevertDefaultProfile
    {
        get; private set;
    } = BackingStore.DEFAULT_AUTO_PROFILE_REVERT_DEFAULT_PROFILE;

    [XmlElement("AutoProfileRevertDefaultProfile")]
    public string AutoProfileRevertDefaultProfileString
    {
        get => AutoProfileRevertDefaultProfile.ToString();
        set
        {
            if (bool.TryParse(value, out bool temp))
            {
                AutoProfileRevertDefaultProfile = temp;
            }
        }
    }

    [XmlElement("AutoProfileSwitchNotifyChoice")]
    public AutoProfileDisplayProfileSwitchChoices AutoProfileSwitchNotifyChoice
    {
        get; set;
    } = AutoProfileDisplayProfileSwitchChoices.None;
    public bool ShouldSerializeAutoProfileSwitchNotifyChoice()
    {
        return AutoProfileSwitchNotifyChoice != AutoProfileDisplayProfileSwitchChoices.None;
    }

    public InputDeviceOptions DeviceOptions
    {
        get; set;
    }

    [XmlIgnore]
    public LightbarDS4WinInfo LightbarInfo1
    {
        get; private set;
    }

    [XmlElement("CustomLed1")]
    public string CustomLed1String
    {
        get => BackingStore.CompileCustomLedString(LightbarInfo1);
        set
        {
            BackingStore.ParseCustomLedString(value, LightbarInfo1);
        }
    }

    [XmlIgnore]
    public LightbarDS4WinInfo LightbarInfo2
    {
        get; private set;
    }

    [XmlElement("CustomLed2")]
    public string CustomLed2String
    {
        get => BackingStore.CompileCustomLedString(LightbarInfo2);
        set
        {
            BackingStore.ParseCustomLedString(value, LightbarInfo2);
        }
    }

    [XmlIgnore]
    public LightbarDS4WinInfo LightbarInfo3
    {
        get; private set;
    }

    [XmlElement("CustomLed3")]
    public string CustomLed3String
    {
        get => BackingStore.CompileCustomLedString(LightbarInfo3);
        set
        {
            BackingStore.ParseCustomLedString(value, LightbarInfo3);
        }
    }

    [XmlIgnore]
    public LightbarDS4WinInfo LightbarInfo4
    {
        get; private set;
    }

    [XmlElement("CustomLed4")]
    public string CustomLed4String
    {
        get => BackingStore.CompileCustomLedString(LightbarInfo4);
        set
        {
            BackingStore.ParseCustomLedString(value, LightbarInfo4);
        }
    }

    [XmlIgnore]
    public LightbarDS4WinInfo LightbarInfo5
    {
        get; private set;
    }

    [XmlElement("CustomLed5")]
    public string CustomLed5String
    {
        get => BackingStore.CompileCustomLedString(LightbarInfo5);
        set
        {
            BackingStore.ParseCustomLedString(value, LightbarInfo5);
        }
    }
    public bool ShouldSerializeCustomLed5String()
    {
        return ControllerLimits.MaxControllerCount >= 5;
    }

    [XmlIgnore]
    public LightbarDS4WinInfo LightbarInfo6
    {
        get; private set;
    }

    [XmlElement("CustomLed6")]
    public string CustomLed6String
    {
        get => BackingStore.CompileCustomLedString(LightbarInfo6);
        set
        {
            BackingStore.ParseCustomLedString(value, LightbarInfo6);
        }
    }
    public bool ShouldSerializeCustomLed6String()
    {
        return ControllerLimits.MaxControllerCount >= 6;
    }

    [XmlIgnore]
    public LightbarDS4WinInfo LightbarInfo7
    {
        get; private set;
    }

    [XmlElement("CustomLed7")]
    public string CustomLed7String
    {
        get => BackingStore.CompileCustomLedString(LightbarInfo7);
        set
        {
            BackingStore.ParseCustomLedString(value, LightbarInfo7);
        }
    }
    public bool ShouldSerializeCustomLed7String()
    {
        return ControllerLimits.MaxControllerCount > 7;
    }

    [XmlIgnore]
    public LightbarDS4WinInfo LightbarInfo8
    {
        get; private set;
    }

    [XmlElement("CustomLed8")]
    public string CustomLed8String
    {
        get => BackingStore.CompileCustomLedString(LightbarInfo8);
        set
        {
            BackingStore.ParseCustomLedString(value, LightbarInfo8);
        }
    }
    public bool ShouldSerializeCustomLed8String()
    {
        return ControllerLimits.MaxControllerCount >= 8;
    }

    public AppSettingsDTO()
    {
        UDPServerSmoothingOptions = new UDPSrvSmoothingOptionsGroup();

        //Controller1CurrentProfile = string.Empty;
        //Controller2CurrentProfile = string.Empty;
        //Controller3CurrentProfile = string.Empty;
        //Controller4CurrentProfile = string.Empty;
        //Controller5CurrentProfile = string.Empty;
        //Controller6CurrentProfile = string.Empty;
        //Controller7CurrentProfile = string.Empty;
        //Controller8CurrentProfile = string.Empty;

        DeviceOptions = new InputDeviceOptions();
        LightbarInfo1 = new LightbarDS4WinInfo();
        LightbarInfo2 = new LightbarDS4WinInfo();
        LightbarInfo3 = new LightbarDS4WinInfo();
        LightbarInfo4 = new LightbarDS4WinInfo();
        LightbarInfo5 = new LightbarDS4WinInfo();
        LightbarInfo6 = new LightbarDS4WinInfo();
        LightbarInfo7 = new LightbarDS4WinInfo();
        LightbarInfo8 = new LightbarDS4WinInfo();
    }
}
