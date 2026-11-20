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

public class AppSettingsDTO
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
