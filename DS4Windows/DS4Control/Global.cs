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
namespace DS4Windows
{

    public class Global
    {
        static Global()
        {
            // Static initializer retained for future Global initializations.
        }
        // プロファイル切り替え時の無効アクション名エラーログ抑制用
        public static HashSet<string> loggedInvalidActions = new HashSet<string>();
        public static int LogMaxArchiveFiles
        {
            set { m_Config.logMaxArchiveFiles = value; }
            get { return m_Config.logMaxArchiveFiles; }
        }

        public static string LogMinLevel
        {
            set { m_Config.logMinLevel = value; }
            get { return m_Config.logMinLevel; }
        }
        public const int MAX_DS4_CONTROLLER_COUNT = 8;
        public const int TEST_PROFILE_ITEM_COUNT = MAX_DS4_CONTROLLER_COUNT + 1;
        public const int TEST_PROFILE_INDEX = TEST_PROFILE_ITEM_COUNT - 1;
        public const int OLD_XINPUT_CONTROLLER_COUNT = 4;
        public const byte DS4_STICK_AXIS_MIDPOINT = 128;

        public static CultureInfo configFileDecimalCulture = new CultureInfo("en-US"); // Loading and Saving decimal values in configuration files should always use en-US decimal format (ie. dot char as decimal separator char, not comma char)

        protected static BackingStore m_Config = new BackingStore();
        public static BackingStore store => m_Config;
        // Need to perform extra steps to check if DS4Windows is installed in a junction
        // directory (done with Scoop). Use real path when available
        public static string exelocation = new Func<string>(() =>
        {
            string filePath = Process.GetCurrentProcess().MainModule.FileName;
            DirectoryInfo dirInfo = new DirectoryInfo(Path.GetDirectoryName(filePath));
            // Check if exe is placed in a junction symlink directory (done with Scoop).
            // Good enough
            if (dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint) &&
                dirInfo.LinkTarget != null)
            {
                // App directory is a junction. Find real directory and get proper path
                // for inserting into HidHide
                filePath = Path.Combine(dirInfo.LinkTarget, Path.GetFileName(filePath));
            }

            return filePath;
        })();
        public static string exedirpath = Directory.GetParent(exelocation).FullName;
        public static FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(exelocation);
        public static string exeversion = fileVersion.FileVersion;
        public static bool IsWin8OrGreater()
        {
            bool result = false;
            if (Environment.OSVersion.Version.Major > 6)
            {
                result = true;
            }
            else if (Environment.OSVersion.Version.Major == 6 &&
                Environment.OSVersion.Version.Minor >= 2)
            {
                result = true;
            }

            return result;
        }

        public static bool IsWin10OrGreater()
        {
            bool result = false;
            if (Environment.OSVersion.Version.Major >= 10)
            {
                result = true;
            }

            return result;
        }

        public static string appdatapath;
        public static bool firstRun = false;
        public static bool multisavespots = false;
        public static string appDataPpath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\DS4Windows";
        public static bool runHotPlug = false;
        // =========================================================================
        // Phase4-Step1: IProfileSettingsService DI シム (Strangler Fig 移行用)
        // =========================================================================
        // Phase4-Step1: IProfileSettingsService DI シム (Strangler Fig 移行用)
        // =========================================================================
        private static DS4Windows.DI.IProfileSettingsService profileSettingsService = null;
        private static readonly DS4Windows.DI.IProfileSettingsService fallbackProfileSettingsService = new ProfileSettingsService(m_Config, new EnvironmentService(), () => Program.rootHub);

        public static DS4Windows.DI.IProfileSettingsService ProfileSettingsServiceInstance
        {
            get
            {
                if (profileSettingsService != null)
                    return profileSettingsService;

                try
                {
                    var service = DS4WinWPF.AppHost.GetService<DS4Windows.DI.IProfileSettingsService>();
                    if (service != null)
                    {
                        profileSettingsService = service;
                        return profileSettingsService;
                    }
                }
                catch
                {
                    // DIコンテナ未初期化時の安全なフォールバック
                }

                AppLogger.LogTrace("[Legacy] Global.ProfileSettingsServiceInstance: Fallback instance used");
                AppLogger.LogToGui("[Legacy] Global.ProfileSettingsServiceInstance: Fallback instance used", false, true);
                return fallbackProfileSettingsService;
            }
            set => profileSettingsService = value;
        }

        // =========================================================================
        // Phase5-Step2: IProfileXmlStore DI シム (Strangler Fig 移行用)
        // Global.LoadProfile/SaveProfileのフォールバック(ProfileRepository経由)より前に
        // 静的フィールド初期化される必要があるため、Phase4-Step2ブロックの直前に配置する。
        // =========================================================================
        private static DS4Windows.DI.IProfileXmlStore profileXmlStore = null;
        private static readonly DS4Windows.DI.IProfileXmlStore fallbackProfileXmlStore = new ProfileXmlStore(m_Config);

        public static DS4Windows.DI.IProfileXmlStore ProfileXmlStoreInstance
        {
            get
            {
                if (profileXmlStore != null) return profileXmlStore;
                try
                {
                    var service = DS4WinWPF.AppHost.GetService<DS4Windows.DI.IProfileXmlStore>();
                    if (service != null)
                    {
                        profileXmlStore = service;
                        return profileXmlStore;
                    }
                }
                catch { }

                AppLogger.LogTrace("[Legacy] Global.ProfileXmlStoreInstance: Fallback instance used");
                AppLogger.LogToGui("[Legacy] Global.ProfileXmlStoreInstance: Fallback instance used", false, true);
                return fallbackProfileXmlStore;
            }
            set => profileXmlStore = value;
        }

        // =========================================================================
        // Phase4-Step5: INotificationService DI シム（IPathService／IEnvironmentService の静的フォールバックは Phase6-Step12 Wave 3 で削除）
        // =========================================================================
        private static DS4Windows.DI.INotificationService notificationService = null;
        private static readonly DS4Windows.DI.INotificationService fallbackNotificationService = new AppNotificationService(m_Config);

        public static DS4Windows.DI.INotificationService NotificationServiceInstance
        {
            get
            {
                if (notificationService != null) return notificationService;
                try
                {
                    var service = DS4WinWPF.AppHost.GetService<DS4Windows.DI.INotificationService>();
                    if (service != null) { notificationService = service; return notificationService; }
                }
                catch { }

                AppLogger.LogTrace("[Legacy] Global.NotificationServiceInstance: Fallback instance used");
                AppLogger.LogToGui("[Legacy] Global.NotificationServiceInstance: Fallback instance used", false, true);
                return fallbackNotificationService;
            }
            set => notificationService = value;
        }

        public static bool[] useTempProfile
        {
            get
            {
                return ProfileSettingsServiceInstance.UseTempProfileArray;
            }
            set
            {
                AppLogger.LogTrace("[Legacy] Global.useTempProfile setter accessed via static shim");
                AppLogger.LogToGui("[Legacy] Global.useTempProfile setter accessed via static shim", false, true);
                ProfileSettingsServiceInstance.UseTempProfileArray = value;
            }
        }
        public static bool[] tempprofileDistance
        {
            get
            {
                return ProfileSettingsServiceInstance.TempProfileDistanceArray;
            }
            set
            {
                AppLogger.LogTrace("[Legacy] Global.tempprofileDistance setter accessed via static shim");
                AppLogger.LogToGui("[Legacy] Global.tempprofileDistance setter accessed via static shim", false, true);
                ProfileSettingsServiceInstance.TempProfileDistanceArray = value;
            }
        }
        public static bool[] useDInputOnly
        {
            get
            {
                return ProfileSettingsServiceInstance.UseDInputOnlyArray;
            }
            set
            {
                AppLogger.LogTrace("[Legacy] Global.useDInputOnly setter accessed via static shim");
                AppLogger.LogToGui("[Legacy] Global.useDInputOnly setter accessed via static shim", false, true);
                ProfileSettingsServiceInstance.UseDInputOnlyArray = value;
            }
        }
        // (Removed) Previously used to hold pending removed invalid special actions
        // collected by UI-time calls. This mechanism was refactored so that
        // removal detection and logging happen at save/apply time; the array has
        // therefore been removed to avoid dead code.
        // Used to hold device type desired from Profile Editor
        public static OutContType[] outDevTypeTemp = new OutContType[TEST_PROFILE_ITEM_COUNT] { DS4Windows.OutContType.X360, DS4Windows.OutContType.X360,
            DS4Windows.OutContType.X360, DS4Windows.OutContType.X360,
            DS4Windows.OutContType.X360, DS4Windows.OutContType.X360,
            DS4Windows.OutContType.X360, DS4Windows.OutContType.X360,
            DS4Windows.OutContType.X360};
        // Used to hold the currently active controller output type in use for a slot
        public static OutContType[] activeOutDevType = new OutContType[TEST_PROFILE_ITEM_COUNT] { DS4Windows.OutContType.None, DS4Windows.OutContType.None,
            DS4Windows.OutContType.None, DS4Windows.OutContType.None,
            DS4Windows.OutContType.None, DS4Windows.OutContType.None,
            DS4Windows.OutContType.None, DS4Windows.OutContType.None,
            DS4Windows.OutContType.None};

        public const string BLANK_VIGEMBUS_VERSION = "0.0.0.0";
        public const string MIN_SUPPORTED_VIGEMBUS_VERSION = "1.16.112.0";
        public const string MIN_TOUCHPAD_PASSTHRU_VIGEMBUS_VERSION = "1.17.333.0";

        //public static bool vigemInstalled = IsViGEmBusInstalled();
        public static bool vigemInstalled = false;
        //public static string vigembusVersion = ViGEmBusVersion();
        public static string vigembusVersion = BLANK_VIGEMBUS_VERSION;
        public static Version vigemBusVersionInfo =
            new Version(!string.IsNullOrEmpty(vigembusVersion) ? vigembusVersion :
                BLANK_VIGEMBUS_VERSION);
        public static Version minSupportedViGEmBusVersionInfo = new Version(MIN_SUPPORTED_VIGEMBUS_VERSION);
        public static bool hidHideInstalled = IsHidHideInstalled();
        public static bool fakerInputInstalled = IsFakerInputInstalled();
        public const string BLANK_FAKERINPUT_VERSION = "0.0.0.0";
        public static string fakerInputVersion = FakerInputVersion();

        public static VirtualKBMBase outputKBMHandler = null;
        public static VirtualKBMMapping outputKBMMapping
        {
            get => ProfileSettingsServiceInstance.OutputKBMMapping;
            set => ProfileSettingsServiceInstance.OutputKBMMapping = value;
        }

        public const int CONFIG_VERSION = 5;
        public const int APP_CONFIG_VERSION = 2;
        public const string ASSEMBLY_RESOURCE_PREFIX = "pack://application:,,,/DS4Windows;";
        public const string RESOURCES_PREFIX = "/DS4Windows;component/Resources";
        // Need to add additional probing path in code starting with .NET 6.
        public const string PROBING_PATH = "Lang";
        public const string LANGUAGE_ASSEMBLY_NAME = "DS4Windows.resources.dll";
        public const string CUSTOM_EXE_CONFIG_FILENAME = "custom_exe_name.txt";
        public const string XML_EXTENSION = ".xml";

        public static X360Controls[] defaultButtonMapping = {
            X360Controls.None, // DS4Controls.None
            X360Controls.LXNeg, // DS4Controls.LXNeg
            X360Controls.LXPos, // DS4Controls.LXPos
            X360Controls.LYNeg, // DS4Controls.LYNeg
            X360Controls.LYPos, // DS4Controls.LYPos
            X360Controls.RXNeg, // DS4Controls.RXNeg
            X360Controls.RXPos, // DS4Controls.RXPos
            X360Controls.RYNeg, // DS4Controls.RYNeg
            X360Controls.RYPos, // DS4Controls.RYPos
            X360Controls.LB, // DS4Controls.L1
            X360Controls.LT, // DS4Controls.L2
            X360Controls.LS, // DS4Controls.L3
            X360Controls.RB, // DS4Controls.R1
            X360Controls.RT, // DS4Controls.R2
            X360Controls.RS, // DS4Controls.R3
            X360Controls.X, // DS4Controls.Square
            X360Controls.Y, // DS4Controls.Triangle
            X360Controls.B, // DS4Controls.Circle
            X360Controls.A, // DS4Controls.Cross
            X360Controls.DpadUp, // DS4Controls.DpadUp
            X360Controls.DpadRight, // DS4Controls.DpadRight
            X360Controls.DpadDown, // DS4Controls.DpadDown
            X360Controls.DpadLeft, // DS4Controls.DpadLeft
            X360Controls.Guide, // DS4Controls.PS
            X360Controls.LeftMouse, // DS4Controls.TouchLeft
            X360Controls.MiddleMouse, // DS4Controls.TouchUpper
            X360Controls.RightMouse, // DS4Controls.TouchMulti
            X360Controls.LeftMouse, // DS4Controls.TouchRight
            X360Controls.Back, // DS4Controls.Share
            X360Controls.Start, // DS4Controls.Options
            X360Controls.None, // DS4Controls.Mute
            X360Controls.None, // DS4Controls.FnL
            X360Controls.None, // DS4Controls.FnR
            X360Controls.None, // DS4Controls.BLP
            X360Controls.None, // DS4Controls.BRP
            X360Controls.None, // DS4Controls.GyroXPos
            X360Controls.None, // DS4Controls.GyroXNeg
            X360Controls.None, // DS4Controls.GyroZPos
            X360Controls.None, // DS4Controls.GyroZNeg
            X360Controls.None, // DS4Controls.SwipeLeft
            X360Controls.None, // DS4Controls.SwipeRight
            X360Controls.None, // DS4Controls.SwipeUp
            X360Controls.None, // DS4Controls.SwipeDown
            X360Controls.None, // DS4Controls.L2FullPull
            X360Controls.None, // DS4Controls.R2FullPull
            X360Controls.None, // DS4Controls.GyroSwipeLeft
            X360Controls.None, // DS4Controls.GyroSwipeRight
            X360Controls.None, // DS4Controls.GyroSwipeUp
            X360Controls.None, // DS4Controls.GyroSwipeDown
            X360Controls.None, // DS4Controls.Capture
            X360Controls.None, // DS4Controls.SideL
            X360Controls.None, // DS4Controls.SideR
            X360Controls.None, // DS4Controls.LSOuter
            X360Controls.None, // DS4Controls.RSOuter
            X360Controls.None, // DS4Controls.TouchStarted
            X360Controls.None, // DS4Controls.TouchEnded
        };

        // Create mapping array at runtime
        public static DS4Controls[] reverseX360ButtonMapping = new Func<DS4Controls[]>(() =>
        {
            DS4Controls[] temp = new DS4Controls[defaultButtonMapping.Length];
            for (int i = 0, arlen = defaultButtonMapping.Length; i < arlen; i++)
            {
                X360Controls mapping = defaultButtonMapping[i];
                if (mapping != X360Controls.None)
                {
                    temp[(int)mapping] = (DS4Controls)i;
                }
            }

            return temp;
        })();

        public static Dictionary<X360Controls, string> xboxDefaultNames = new Dictionary<X360Controls, string>()
        {
            [X360Controls.LXNeg] = "Left X-Axis-",
            [X360Controls.LXPos] = "Left X-Axis+",
            [X360Controls.LYNeg] = "Left Y-Axis-",
            [X360Controls.LYPos] = "Left Y-Axis+",
            [X360Controls.RXNeg] = "Right X-Axis-",
            [X360Controls.RXPos] = "Right X-Axis+",
            [X360Controls.RYNeg] = "Right Y-Axis-",
            [X360Controls.RYPos] = "Right Y-Axis+",
            [X360Controls.LB] = "Left Bumper",
            [X360Controls.LT] = "Left Trigger",
            [X360Controls.LS] = "Left Stick",
            [X360Controls.RB] = "Right Bumper",
            [X360Controls.RT] = "Right Trigger",
            [X360Controls.RS] = "Right Stick",
            [X360Controls.X] = "X Button",
            [X360Controls.Y] = "Y Button",
            [X360Controls.B] = "B Button",
            [X360Controls.A] = "A Button",
            [X360Controls.DpadUp] = "Up Button",
            [X360Controls.DpadRight] = "Right Button",
            [X360Controls.DpadDown] = "Down Button",
            [X360Controls.DpadLeft] = "Left Button",
            [X360Controls.Guide] = "Guide",
            [X360Controls.Back] = "Back",
            [X360Controls.Start] = "Start",
            [X360Controls.TouchpadClick] = "Touchpad Click",
            [X360Controls.LeftMouse] = "Left Mouse Button",
            [X360Controls.RightMouse] = "Right Mouse Button",
            [X360Controls.MiddleMouse] = "Middle Mouse Button",
            [X360Controls.FourthMouse] = "4th Mouse Button",
            [X360Controls.FifthMouse] = "5th Mouse Button",
            [X360Controls.WUP] = "Mouse Wheel Up",
            [X360Controls.WDOWN] = "Mouse Wheel Down",
            [X360Controls.MouseUp] = "Mouse Up",
            [X360Controls.MouseDown] = "Mouse Down",
            [X360Controls.MouseLeft] = "Mouse Left",
            [X360Controls.MouseRight] = "Mouse Right",
            [X360Controls.Unbound] = "Unbound",
            [X360Controls.None] = "Unassigned",
        };

        public static Dictionary<X360Controls, string> ds4DefaultNames = new Dictionary<X360Controls, string>()
        {
            [X360Controls.LXNeg] = "Left X-Axis-",
            [X360Controls.LXPos] = "Left X-Axis+",
            [X360Controls.LYNeg] = "Left Y-Axis-",
            [X360Controls.LYPos] = "Left Y-Axis+",
            [X360Controls.RXNeg] = "Right X-Axis-",
            [X360Controls.RXPos] = "Right X-Axis+",
            [X360Controls.RYNeg] = "Right Y-Axis-",
            [X360Controls.RYPos] = "Right Y-Axis+",
            [X360Controls.LB] = "L1",
            [X360Controls.LT] = "L2",
            [X360Controls.LS] = "L3",
            [X360Controls.RB] = "R1",
            [X360Controls.RT] = "R2",
            [X360Controls.RS] = "R3",
            [X360Controls.X] = "Square",
            [X360Controls.Y] = "Triangle",
            [X360Controls.B] = "Circle",
            [X360Controls.A] = "Cross",
            [X360Controls.DpadUp] = "Dpad Up",
            [X360Controls.DpadRight] = "Dpad Right",
            [X360Controls.DpadDown] = "Dpad Down",
            [X360Controls.DpadLeft] = "Dpad Left",
            [X360Controls.Guide] = "PS",
            [X360Controls.Back] = "Share",
            [X360Controls.Start] = "Options",
            [X360Controls.TouchpadClick] = "Touchpad Click",
            [X360Controls.LeftMouse] = "Left Mouse Button",
            [X360Controls.RightMouse] = "Right Mouse Button",
            [X360Controls.MiddleMouse] = "Middle Mouse Button",
            [X360Controls.FourthMouse] = "4th Mouse Button",
            [X360Controls.FifthMouse] = "5th Mouse Button",
            [X360Controls.WUP] = "Mouse Wheel Up",
            [X360Controls.WDOWN] = "Mouse Wheel Down",
            [X360Controls.MouseUp] = "Mouse Up",
            [X360Controls.MouseDown] = "Mouse Down",
            [X360Controls.MouseLeft] = "Mouse Left",
            [X360Controls.MouseRight] = "Mouse Right",
            [X360Controls.Unbound] = "Unbound",
        };

        public static string getX360ControlString(X360Controls key, OutContType conType)
        {
            string result = string.Empty;
            if (conType == DS4Windows.OutContType.X360)
            {
                xboxDefaultNames.TryGetValue(key, out result);
            }
            else if (conType == DS4Windows.OutContType.DS4)
            {
                ds4DefaultNames.TryGetValue(key, out result);
            }

            return result;
        }

        public static Dictionary<DS4Controls, string> ds4inputNames = new Dictionary<DS4Controls, string>()
        {
            [DS4Controls.LXNeg] = "Left X-Axis-",
            [DS4Controls.LXPos] = "Left X-Axis+",
            [DS4Controls.LYNeg] = "Left Y-Axis-",
            [DS4Controls.LYPos] = "Left Y-Axis+",
            [DS4Controls.RXNeg] = "Right X-Axis-",
            [DS4Controls.RXPos] = "Right X-Axis+",
            [DS4Controls.RYNeg] = "Right Y-Axis-",
            [DS4Controls.RYPos] = "Right Y-Axis+",
            [DS4Controls.L1] = "L1",
            [DS4Controls.L2] = "L2",
            [DS4Controls.L3] = "L3",
            [DS4Controls.R1] = "R1",
            [DS4Controls.R2] = "R2",
            [DS4Controls.R3] = "R3",
            [DS4Controls.Square] = "Square",
            [DS4Controls.Triangle] = "Triangle",
            [DS4Controls.Circle] = "Circle",
            [DS4Controls.Cross] = "Cross",
            [DS4Controls.DpadUp] = "Dpad Up",
            [DS4Controls.DpadRight] = "Dpad Right",
            [DS4Controls.DpadDown] = "Dpad Down",
            [DS4Controls.DpadLeft] = "Dpad Left",
            [DS4Controls.PS] = "PS",
            [DS4Controls.Share] = "Share",
            [DS4Controls.Options] = "Options",
            [DS4Controls.Mute] = "Mute",
            [DS4Controls.FnL] = "Function Left",
            [DS4Controls.FnR] = "Function Right",
            [DS4Controls.BLP] = "Bottom Left Paddle",
            [DS4Controls.BRP] = "Bottom Right Paddle",
            [DS4Controls.Capture] = "Capture",
            [DS4Controls.SideL] = "Side L",
            [DS4Controls.SideR] = "Side R",
            [DS4Controls.TouchLeft] = "Left Touch",
            [DS4Controls.TouchUpper] = "Upper Touch",
            [DS4Controls.TouchMulti] = "Multitouch",
            [DS4Controls.TouchRight] = "Right Touch",

            [DS4Controls.GyroXPos] = "Gyro X+",
            [DS4Controls.GyroXNeg] = "Gyro X-",
            [DS4Controls.GyroZPos] = "Gyro Z+",
            [DS4Controls.GyroZNeg] = "Gyro Z-",
            [DS4Controls.SwipeLeft] = "Swipe Left",
            [DS4Controls.SwipeRight] = "Swipe Right",
            [DS4Controls.SwipeUp] = "Swipe Up",
            [DS4Controls.SwipeDown] = "Swipe Down",
            [DS4Controls.L2FullPull] = "L2 Full Pull",
            [DS4Controls.R2FullPull] = "R2 Full Pull",
            [DS4Controls.LSOuter] = "LS Outer",
            [DS4Controls.RSOuter] = "RS Outer",

            [DS4Controls.GyroSwipeLeft] = "Gyro Swipe Left",
            [DS4Controls.GyroSwipeRight] = "Gyro Swipe Right",
            [DS4Controls.GyroSwipeUp] = "Gyro Swipe Up",
            [DS4Controls.GyroSwipeDown] = "Gyro Swipe Down",

            [DS4Controls.TouchStarted] = "Touch Started",
            [DS4Controls.TouchEnded] = "Touch Ended",
        };

        public static Dictionary<DS4Controls, int> macroDS4Values = new Dictionary<DS4Controls, int>()
        {
            [DS4Controls.Cross] = 261,
            [DS4Controls.Circle] = 262,
            [DS4Controls.Square] = 263,
            [DS4Controls.Triangle] = 264,
            [DS4Controls.Options] = 265,
            [DS4Controls.Share] = 266,
            [DS4Controls.DpadUp] = 267,
            [DS4Controls.DpadDown] = 268,
            [DS4Controls.DpadLeft] = 269,
            [DS4Controls.DpadRight] = 270,
            [DS4Controls.PS] = 271,
            [DS4Controls.L1] = 272,
            [DS4Controls.R1] = 273,
            [DS4Controls.L2] = 274,
            [DS4Controls.R2] = 275,
            [DS4Controls.L3] = 276,
            [DS4Controls.R3] = 277,
            [DS4Controls.LXPos] = 278,
            [DS4Controls.LXNeg] = 279,
            [DS4Controls.LYPos] = 280,
            [DS4Controls.LYNeg] = 281,
            [DS4Controls.RXPos] = 282,
            [DS4Controls.RXNeg] = 283,
            [DS4Controls.RYPos] = 284,
            [DS4Controls.RYNeg] = 285,
            [DS4Controls.TouchLeft] = 286,
            [DS4Controls.TouchRight] = 286,
            [DS4Controls.TouchUpper] = 286,
            [DS4Controls.TouchMulti] = 286,
            [DS4Controls.TouchStarted] = 287,
            [DS4Controls.TouchEnded] = 288,
        };

        public static Dictionary<TrayIconChoice, string> iconChoiceResources = new Dictionary<TrayIconChoice, string>
        {
            [TrayIconChoice.Default] = $"{Global.RESOURCES_PREFIX}/DS4W.ico",
            [TrayIconChoice.Colored] = $"{Global.RESOURCES_PREFIX}/DS4W.ico",
            [TrayIconChoice.White] = $"{Global.RESOURCES_PREFIX}/DS4W - White.ico",
            [TrayIconChoice.Black] = $"{Global.RESOURCES_PREFIX}/DS4W - Black.ico",
            [TrayIconChoice.Battery] = $"{RESOURCES_PREFIX}/DS4W.ico"
        };

        public static void SaveWhere(string path)
        {
            appdatapath = path;
            m_Config.m_Profile = appdatapath + "\\Profiles.xml";
            m_Config.m_Actions = appdatapath + "\\Actions.xml";
            m_Config.m_linkedProfiles = Global.appdatapath + "\\LinkedProfiles.xml";
            m_Config.m_controllerConfigs = Global.appdatapath + "\\ControllerConfigs.xml";
        }

        public static bool SaveDefault(string path)
        {
            Boolean Saved = true;
            XmlDocument m_Xdoc = new XmlDocument();
            try
            {
                XmlNode Node;

                m_Xdoc.RemoveAll();

                Node = m_Xdoc.CreateXmlDeclaration("1.0", "utf-8", String.Empty);
                m_Xdoc.AppendChild(Node);

                Node = m_Xdoc.CreateComment(string.Format(" Profile Configuration Data. {0} ", DateTime.Now));
                m_Xdoc.AppendChild(Node);

                Node = m_Xdoc.CreateWhitespace("\r\n");
                m_Xdoc.AppendChild(Node);

                Node = m_Xdoc.CreateNode(XmlNodeType.Element, "Profile", null);

                m_Xdoc.AppendChild(Node);

                m_Xdoc.Save(path);
            }
            catch { Saved = false; }

            return Saved;
        }

        /// <summary>
        /// Check if Admin Rights are needed to write in Appliplation Directory
        /// </summary>
        /// <returns></returns>
        public static bool AdminNeeded()
        {
            try
            {
                File.WriteAllText(exedirpath + "\\test.txt", "test");
                File.Delete(exedirpath + "\\test.txt");
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return true;
            }
        }

        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private class ViGEmBusInfo
        {
            //public string path;
            public string instanceId;
            public string deviceName;
            public string deviceVersionStr;
            public Version deviceVersion;
            public string manufacturer;
            public string driverProviderName;
        }

        private static void FindViGEmDeviceInfo()
        {
            bool result = false;
            Guid deviceGuid = Guid.Parse(VIGEMBUS_GUID);
            NativeMethods.SP_DEVINFO_DATA deviceInfoData =
                new NativeMethods.SP_DEVINFO_DATA();
            deviceInfoData.cbSize =
                System.Runtime.InteropServices.Marshal.SizeOf(deviceInfoData);

            var dataBuffer = new byte[4096];
            ulong propertyType = 0;
            var requiredSize = 0;

            // Properties to retrieve
            NativeMethods.DEVPROPKEY[] lookupProperties = new NativeMethods.DEVPROPKEY[]
            {
                NativeMethods.DEVPKEY_Device_DriverVersion, NativeMethods.DEVPKEY_Device_InstanceId,
                NativeMethods.DEVPKEY_Device_Manufacturer, NativeMethods.DEVPKEY_Device_Provider,
                NativeMethods.DEVPKEY_Device_DeviceDesc,
            };

            List<ViGEmBusInfo> tempViGEmBusInfoList = new List<ViGEmBusInfo>();

            IntPtr deviceInfoSet = NativeMethods.SetupDiGetClassDevs(ref deviceGuid, null, 0,
                NativeMethods.DIGCF_DEVICEINTERFACE);
            for (int i = 0; !result && NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, i, ref deviceInfoData); i++)
            {
                ViGEmBusInfo tempBusInfo = new ViGEmBusInfo();

                foreach (NativeMethods.DEVPROPKEY currentDevKey in lookupProperties)
                {
                    NativeMethods.DEVPROPKEY tempKey = currentDevKey;
                    if (NativeMethods.SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData,
                        ref tempKey, ref propertyType,
                        dataBuffer, dataBuffer.Length, ref requiredSize, 0))
                    {
                        string temp = dataBuffer.ToUTF16String();
                        if (currentDevKey.fmtid == NativeMethods.DEVPKEY_Device_DriverVersion.fmtid &&
                            currentDevKey.pid == NativeMethods.DEVPKEY_Device_DriverVersion.pid)
                        {
                            try
                            {
                                tempBusInfo.deviceVersion = new Version(temp);
                                tempBusInfo.deviceVersionStr = temp;
                            }
                            catch (ArgumentException)
                            {
                                // Default to unknown version
                                tempBusInfo.deviceVersionStr = BLANK_VIGEMBUS_VERSION;
                                tempBusInfo.deviceVersion = new Version(tempBusInfo.deviceVersionStr);
                            }
                        }
                        else if (currentDevKey.fmtid == NativeMethods.DEVPKEY_Device_InstanceId.fmtid &&
                            currentDevKey.pid == NativeMethods.DEVPKEY_Device_InstanceId.pid)
                        {
                            tempBusInfo.instanceId = temp;
                        }
                        else if (currentDevKey.fmtid == NativeMethods.DEVPKEY_Device_Manufacturer.fmtid &&
                            currentDevKey.pid == NativeMethods.DEVPKEY_Device_Manufacturer.pid)
                        {
                            tempBusInfo.manufacturer = temp;
                        }
                        else if (currentDevKey.fmtid == NativeMethods.DEVPKEY_Device_Provider.fmtid &&
                            currentDevKey.pid == NativeMethods.DEVPKEY_Device_Provider.pid)
                        {
                            tempBusInfo.driverProviderName = temp;
                        }
                        else if (currentDevKey.fmtid == NativeMethods.DEVPKEY_Device_DeviceDesc.fmtid &&
                            currentDevKey.pid == NativeMethods.DEVPKEY_Device_DeviceDesc.pid)
                        {
                            tempBusInfo.deviceName = temp;
                        }
                    }
                }

                tempViGEmBusInfoList.Add(tempBusInfo);
            }

            if (deviceInfoSet.ToInt64() != NativeMethods.INVALID_HANDLE_VALUE)
            {
                NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            // Iterate over list and find most recent version number
            //IEnumerable<ViGEmBusInfo> tempResults = tempViGEmBusInfoList.Where(item => minSupportedViGEmBusVersionInfo.CompareTo(item.deviceVersion) <= 0);
            Version latestKnown = new Version(BLANK_VIGEMBUS_VERSION);
            string deviceInstanceId = string.Empty;
            foreach (ViGEmBusInfo item in tempViGEmBusInfoList)
            {
                if (latestKnown.CompareTo(item.deviceVersion) <= 0)
                {
                    latestKnown = item.deviceVersion;
                    deviceInstanceId = item.instanceId;
                }
            }

            // Get bus info for most recent version found and save info
            ViGEmBusInfo latestBusInfo =
                tempViGEmBusInfoList.SingleOrDefault(item => item.instanceId == deviceInstanceId);
            PopulateFromViGEmBusInfo(latestBusInfo);
        }

        private static bool CheckForSysDevice(string searchHardwareId)
        {
            bool result = false;
            Guid sysGuid = Guid.Parse("{4d36e97d-e325-11ce-bfc1-08002be10318}");
            NativeMethods.SP_DEVINFO_DATA deviceInfoData =
                new NativeMethods.SP_DEVINFO_DATA();
            deviceInfoData.cbSize =
                System.Runtime.InteropServices.Marshal.SizeOf(deviceInfoData);
            var dataBuffer = new byte[4096];
            ulong propertyType = 0;
            var requiredSize = 0;
            IntPtr deviceInfoSet = NativeMethods.SetupDiGetClassDevs(ref sysGuid, null, 0, 0);
            for (int i = 0; !result && NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, i, ref deviceInfoData); i++)
            {
                NativeMethods.SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData,
                    ref NativeMethods.DEVPKEY_Device_HardwareIds, ref propertyType,
                    null, 0, ref requiredSize, 0);

                if (requiredSize > 0)
                {
                    NativeMethods.SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData,
                        ref NativeMethods.DEVPKEY_Device_HardwareIds, ref propertyType,
                        dataBuffer, dataBuffer.Length, ref requiredSize, 0);

                    string hardwareId = dataBuffer.ToUTF16String();
                    //if (hardwareIds.Contains("Virtual Gamepad Emulation Bus"))
                    //    result = true;
                    if (hardwareId.Equals(searchHardwareId))
                        result = true;
                }
            }

            if (deviceInfoSet.ToInt64() != NativeMethods.INVALID_HANDLE_VALUE)
            {
                NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            return result;
        }

        internal static string GetStringDeviceProperty(string deviceInstanceId,
            NativeMethods.DEVPROPKEY prop)
        {
            string result = string.Empty;
            NativeMethods.SP_DEVINFO_DATA deviceInfoData = new NativeMethods.SP_DEVINFO_DATA();
            deviceInfoData.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(deviceInfoData);
            ulong propertyType = 0;
            var requiredSize = 0;

            Guid hidGuid = new Guid();
            NativeMethods.HidD_GetHidGuid(ref hidGuid);
            //IntPtr deviceInfoSet = NativeMethods.SetupDiGetClassDevs(IntPtr.Zero, deviceInstanceId, 0, extraFlags | NativeMethods.DIGCF_DEVICEINTERFACE | NativeMethods.DIGCF_ALLCLASSES);
            IntPtr deviceInfoSet = NativeMethods.SetupDiCreateDeviceInfoList(IntPtr.Zero, 0);
            //NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, 0, ref deviceInfoData);
            NativeMethods.SetupDiOpenDeviceInfo(deviceInfoSet, deviceInstanceId, IntPtr.Zero, 0, ref deviceInfoData);
            NativeMethods.SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData, ref prop, ref propertyType,
                    null, 0, ref requiredSize, 0);

            if (requiredSize > 0)
            {
                byte[] dataBuffer = new byte[requiredSize];
                NativeMethods.SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData, ref prop, ref propertyType,
                    dataBuffer, dataBuffer.Length, ref requiredSize, 0);

                result = dataBuffer.ToUTF16String();
            }

            if (deviceInfoSet.ToInt64() != NativeMethods.INVALID_HANDLE_VALUE)
            {
                NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            return result;
        }

        public static bool CheckHidHideAffectedStatus(string deviceInstanceId,
            HashSet<string> affectedDevs, HashSet<string> exemptedDevices, bool force = false)
        {
            bool result = false;
            string tempDeviceInstanceId = deviceInstanceId.ToUpper();
            result = affectedDevs.Contains(tempDeviceInstanceId);
            return result;
        }

        public static bool IsHidHideInstalled()
        {
            return CheckForSysDevice(@"root\HidHide");
        }

        public static bool IsFakerInputInstalled()
        {
            return CheckForSysDevice(@"root\FakerInput");
        }

        const string VIGEMBUS_GUID = "{96E42B22-F5E9-42F8-B043-ED0F932F014F}";
        public static bool IsViGEmBusInstalled()
        {
            return vigemInstalled;
        }

        public static bool IsRunningSupportedViGEmBus()
        {
            //return vigemInstalled;
            return vigemInstalled &&
                minSupportedViGEmBusVersionInfo.CompareTo(vigemBusVersionInfo) <= 0;
        }

        public static bool IsUsingMinViGEm117333()
        {
            bool result = Global.vigemBusVersionInfo.CompareTo(
                new Version(Global.MIN_TOUCHPAD_PASSTHRU_VIGEMBUS_VERSION)) >= 0;
            return result;
        }

        public static void RefreshViGEmBusInfo()
        {
            FindViGEmDeviceInfo();
        }

        public static void RefreshHidHideInfo()
        {
            hidHideInstalled = IsHidHideInstalled();
        }

        private static void PopulateFromViGEmBusInfo(ViGEmBusInfo busInfo)
        {
            if (busInfo != null)
            {
                vigemInstalled = true;
                vigembusVersion = busInfo.deviceVersionStr;
                vigemBusVersionInfo = busInfo.deviceVersion;
            }
            else
            {
                vigemInstalled = false;
                vigembusVersion = BLANK_VIGEMBUS_VERSION;
                vigemBusVersionInfo = new Version(BLANK_VIGEMBUS_VERSION);
            }
        }

        private static string FakerInputVersion()
        {
            // Start with BLANK_FAKERINPUT_VERSION for result
            string result = BLANK_FAKERINPUT_VERSION;
            IntPtr deviceInfoSet = NativeMethods.SetupDiGetClassDevs(ref Util.fakerInputGuid, null, 0, NativeMethods.DIGCF_DEVICEINTERFACE);
            NativeMethods.SP_DEVINFO_DATA deviceInfoData = new NativeMethods.SP_DEVINFO_DATA();
            deviceInfoData.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(deviceInfoData);
            bool foundDev = false;
            //bool success = NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, 0, ref deviceInfoData);
            for (int i = 0; !foundDev && NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, i, ref deviceInfoData); i++)
            {
                ulong devPropertyType = 0;
                int requiredSizeProp = 0;
                NativeMethods.SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData,
                    ref NativeMethods.DEVPKEY_Device_DriverVersion, ref devPropertyType, null, 0, ref requiredSizeProp, 0);

                if (requiredSizeProp > 0)
                {
                    var versionTextBuffer = new byte[requiredSizeProp];
                    NativeMethods.SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData,
                        ref NativeMethods.DEVPKEY_Device_DriverVersion, ref devPropertyType, versionTextBuffer, requiredSizeProp, ref requiredSizeProp, 0);

                    string tmpitnow = System.Text.Encoding.Unicode.GetString(versionTextBuffer);
                    string tempStrip = tmpitnow.TrimEnd('\0');
                    foundDev = true;
                    result = tempStrip;
                }
            }

            if (deviceInfoSet.ToInt64() != NativeMethods.INVALID_HANDLE_VALUE)
            {
                NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            return result;
        }

        public static string GetInstanceIdFromDevicePath(string devicePath)
        {
            string result = string.Empty;
            uint requiredSize = 0;
            NativeMethods.CM_Get_Device_Interface_Property(devicePath, ref NativeMethods.DEVPKEY_Device_InstanceId, out _, null, ref requiredSize, 0);
            if (requiredSize > 0)
            {
                byte[] buffer = new byte[requiredSize];
                NativeMethods.CM_Get_Device_Interface_Property(devicePath, ref NativeMethods.DEVPKEY_Device_InstanceId, out _, buffer, ref requiredSize, 0);
                result = buffer.ToUTF16String();
            }

            return result;
        }

        internal static string[] GetStringArrayDeviceProperty(string deviceInstanceId,
            NativeMethods.DEVPROPKEY prop)
        {
            string[] result = null;
            NativeMethods.SP_DEVINFO_DATA deviceInfoData = new NativeMethods.SP_DEVINFO_DATA();
            deviceInfoData.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(deviceInfoData);
            ulong propertyType = 0;
            var requiredSize = 0;

            IntPtr zero = IntPtr.Zero;
            //IntPtr deviceInfoSet = NativeMethods.SetupDiGetClassDevs(zero, deviceInstanceId, 0, extraFlags | NativeMethods.DIGCF_DEVICEINTERFACE | NativeMethods.DIGCF_ALLCLASSES);
            IntPtr deviceInfoSet = NativeMethods.SetupDiCreateDeviceInfoList(IntPtr.Zero, 0);
            //NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, 0, ref deviceInfoData);
            NativeMethods.SetupDiOpenDeviceInfo(deviceInfoSet, deviceInstanceId, IntPtr.Zero, 0, ref deviceInfoData);
            NativeMethods.SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData, ref prop, ref propertyType,
                    null, 0, ref requiredSize, 0);

            if (requiredSize > 0)
            {
                byte[] dataBuffer = new byte[requiredSize];
                NativeMethods.SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData, ref prop, ref propertyType,
                    dataBuffer, dataBuffer.Length, ref requiredSize, 0);

                string tempStr = Encoding.Unicode.GetString(dataBuffer);
                string[] hardwareIds = tempStr.TrimEnd(new char[] { '\0', '\0' }).Split('\0');
                result = hardwareIds;
            }

            if (deviceInfoSet.ToInt64() != NativeMethods.INVALID_HANDLE_VALUE)
            {
                NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            return result;
        }

        public static bool CheckIfVirtualDevice(string devicePath)
        {
            bool result = false;
            bool excludeMatchFound = false;

            var instanceId = GetInstanceIdFromDevicePath(devicePath);
            var testInstanceId = instanceId;
            while (!string.IsNullOrEmpty(testInstanceId))
            {
                var hardwareIds = GetStringArrayDeviceProperty(testInstanceId, NativeMethods.DEVPKEY_Device_HardwareIds);
                if (hardwareIds != null)
                {
                    // hardware IDs of root hubs/controllers that emit supported virtual devices as sources
                    var excludedIds = new[]
                    {
                        @"ROOT\HIDGAMEMAP", // reWASD
                        @"ROOT\VHUSB3HC", // VirtualHere
                    };

                    excludeMatchFound = hardwareIds.Any(id => excludedIds.Contains(id.ToUpper()));
                    if (excludeMatchFound)
                    {
                        break;
                    }
                }

                // Check for potential non-present device as well
                string parentInstanceId = GetStringDeviceProperty(testInstanceId, NativeMethods.DEVPKEY_Device_Parent);

                // Found root enumerator. Use instanceId of device one layer lower in final check
                if (parentInstanceId.Equals(@"HTREE\ROOT\0", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                testInstanceId = parentInstanceId;
            }

            if (!excludeMatchFound &&
                !string.IsNullOrEmpty(testInstanceId) &&
                (testInstanceId.StartsWith(@"ROOT\SYSTEM", StringComparison.OrdinalIgnoreCase)
                || testInstanceId.StartsWith(@"ROOT\USB", StringComparison.OrdinalIgnoreCase)))
            {
                result = true;
            }

            return result;
        }

        public static void FindConfigLocation()
        {
            bool programFolderAutoProfilesExists = File.Exists(Path.Combine(exedirpath, "Auto Profiles.xml"));
            bool appDataAutoProfilesExists = File.Exists(Path.Combine(appDataPpath, "Auto Profiles.xml"));
            //bool localAppDataAutoProfilesExists = File.Exists(Path.Combine(localAppDataPpath, "Auto Profiles.xml"));
            //bool systemAppConfigExists = appDataAutoProfilesExists || localAppDataAutoProfilesExists;
            bool systemAppConfigExists = appDataAutoProfilesExists;
            bool isSameFolder = appDataAutoProfilesExists && exedirpath == appDataPpath;

            if (programFolderAutoProfilesExists && appDataAutoProfilesExists &&
                !isSameFolder)
            {
                Global.firstRun = true;
                Global.multisavespots = true;
            }
            else if (programFolderAutoProfilesExists)
            {
                SaveWhere(exedirpath);
            }
            //else if (localAppDataAutoProfilesExists)
            //{
            //    SaveWhere(localAppDataPpath);
            //}
            else if (appDataAutoProfilesExists)
            {
                SaveWhere(appDataPpath);
            }
            else if (!programFolderAutoProfilesExists && !appDataAutoProfilesExists)
            {
                Global.firstRun = true;
                Global.multisavespots = false;
            }
        }

        public static void SetCulture(string culture)
        {
            try
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
                CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo(culture);
            }
            catch { /* Skip setting culture that we cannot set */ }
        }

        public static void CreateStdActions()
        {
            XmlDocument xDoc = new XmlDocument();
            try
            {
                string[] profiles = Directory.GetFiles(appdatapath + @"\Profiles\");
                string s = string.Empty;
                //foreach (string s in profiles)
                for (int i = 0, proflen = profiles.Length; i < proflen; i++)
                {
                    s = profiles[i];
                    if (Path.GetExtension(s) == ".xml")
                    {
                        xDoc.Load(s);
                        XmlNode el = xDoc.SelectSingleNode("DS4Windows/ProfileActions");
                        if (el != null)
                        {
                            if (string.IsNullOrEmpty(el.InnerText))
                                el.InnerText = "Disconnect Controller";
                            else
                                el.InnerText += "/Disconnect Controller";
                        }
                        else
                        {
                            XmlNode Node = xDoc.SelectSingleNode("DS4Windows");
                            el = xDoc.CreateElement("ProfileActions");
                            el.InnerText = "Disconnect Controller";
                            Node.AppendChild(el);
                        }

                        xDoc.Save(s);
                        LoadActions();
                    }
                }
            }
            catch { }
        }

        public static bool CreateAutoProfiles(string m_Profile)
        {
            bool Saved = true;

            try
            {
                XmlNode Node;
                XmlDocument doc = new XmlDocument();

                Node = doc.CreateXmlDeclaration("1.0", "utf-8", String.Empty);
                doc.AppendChild(Node);

                Node = doc.CreateComment(string.Format(" Auto-Profile Configuration Data. {0} ", DateTime.Now));
                doc.AppendChild(Node);

                Node = doc.CreateWhitespace("\r\n");
                doc.AppendChild(Node);

                Node = doc.CreateNode(XmlNodeType.Element, "Programs", "");
                doc.AppendChild(Node);
                doc.Save(m_Profile);
            }
            catch { Saved = false; }

            return Saved;
        }

        public static event EventHandler<SerialChangeArgs> DeviceSerialChange;
        public static void OnDeviceSerialChange(object sender, int index, string serial)
        {
            if (DeviceSerialChange != null)
            {
                SerialChangeArgs args = new SerialChangeArgs(index, serial);
                DeviceSerialChange(sender, args);
            }
        }

        public static ulong CompileVersionNumberFromString(string versionStr)
        {
            ulong result = 0;
            try
            {
                Version tmpVersion = new Version(versionStr);
                result = CompileVersionNumber(tmpVersion.Major, tmpVersion.Minor,
                    tmpVersion.Build, tmpVersion.Revision);
            }
            catch (Exception) { }

            return result;
        }

        public static ulong CompileVersionNumber(int majorPart, int minorPart,
            int buildPart, int privatePart)
        {
            ulong result = (ulong)majorPart << 48 | (ulong)minorPart << 32 |
                (ulong)buildPart << 16 | (ushort)privatePart;
            return result;
        }

        // general values
        // -- Re-Enable Exclusive Mode Starts Here --
        // New explicit startup update-check settings
        // 0 = hours, 1 = days
        // Legacy `CheckWhen` removed. Use explicit CheckEveryValue/CheckEveryUnit instead.

        // Phase7-Step3 W4: 読み取り専用にした（代入と番号の算出は AppSettingsService.LastVersionChecked が担う）。
        // Changelog が読むため、Phase7 の後続 Step まで残す。
        public static string LastVersionChecked => m_Config.lastVersionChecked;

        public static int Notifications
        {
            set { m_Config.notifications = value; }
            get { return m_Config.notifications; }
        }

        public static bool ProfileChangedNotification
        {
            get => ProfileSettingsServiceInstance.ProfileChangedNotification;
            set => ProfileSettingsServiceInstance.ProfileChangedNotification = value;
        }

        public static bool UseMoonlight
        {
            get => m_Config.useMoonlight;
            set => m_Config.useMoonlight = value;
        }

        public static bool UseAdvancedMoonlight
        {
            get => m_Config.useAdvancedMoonlight;
            set => m_Config.useAdvancedMoonlight = value;
        }

        public static string UseLang
        {
            set { m_Config.useLang = value; }
            get { return m_Config.useLang; }
        }

        public static int getOSCServerPortNum()
        {
            return m_Config.oscServPort;
        }
        public static event EventHandler<byte> BatteryChanged;

        public static void InvokeBatteryChanged(byte percentage)
        {
            BatteryChanged?.Invoke(typeof(Global), percentage);
        }

        public static AppThemeChoice UseCurrentTheme
        {
            get => m_Config.useCurrentTheme;
            set => m_Config.useCurrentTheme = value;
        }

        public static int[] DebouncingMs => ProfileSettingsServiceInstance.DebouncingMs;

        public static bool UseDs3PitchRollSim
        {
            get => ProfileSettingsServiceInstance.UseDs3PitchRollSim;
            set => ProfileSettingsServiceInstance.UseDs3PitchRollSim = value;
        }

        // controller/profile specfic values
        public static int getIdleDisconnectTimeout(int index)
        {
            return ProfileSettingsServiceInstance.GetIdleDisconnectTimeout(index);
        }

        public static LightbarSettingInfo getLightbarSettingsInfo(int index)
        {
            return ProfileSettingsServiceInstance.GetLightbarSettingsInfo(index);
        }


        public static SASteeringWheelEmulationAxisType GetSASteeringWheelEmulationAxis(int index)
        {
            return ProfileSettingsServiceInstance.GetSASteeringWheelEmulationAxis(index);
        }

        //public static DS4Color[] MainColor => m_Config.m_Leds;
        public static ref DS4Color getMainColor(int index)
        {
            return ref ProfileSettingsServiceInstance.GetMainColor(index);
            //return ref m_Config.m_Leds[index];
        }

        public static ControlServiceDeviceOptions DeviceOptions => m_Config.deviceOptions;

        public static OutContType[] OutContType => m_Config.outputDevType;
        public static string[] ProfilePath => m_Config.profilePath;
        public static string[] OlderProfilePath => m_Config.olderProfilePath;
        public static string[] SelectedProfile => m_Config.selectedProfile;
        public static bool[] DistanceProfiles = m_Config.distanceProfiles;

        // Event for profile change notification
        public static event EventHandler<SelectedProfileChangedEventArgs> SelectedProfileChanged;

        public static void RaiseSelectedProfileChanged(int deviceIndex, string profileName)
        {
            SelectedProfileChanged?.Invoke(null, new SelectedProfileChangedEventArgs(deviceIndex, profileName));
        }


        #region Profile Application Unified Gateway

        /// <summary>
        /// 指定したコントローラースロットに対してプロファイルを安全に適用する共通窓口メソッド。
        /// 手動選択・保存時ホットリロード・スペシャルアクション・自動プロファイル・接続時・Rename時等の全適用経路を一本化します。
        /// 設定（Global.ProfileChangedNotification）に従い、同一の独自ウィンドウデスクトップ通知を一元制御します。
        /// （Phase5-Step14 フェーズDにて、判定基準を Global.Notifications から ProfileChangedNotification に暫定是正済み。
        /// フェーズGにて IProfileApplicationService への完全委譲に置き換え予定）
        /// </summary>
        public static bool ApplyProfileToSlot(int slotIndex, string profileName, ProfileChangeSource source)
        {
            if (slotIndex < 0 || slotIndex >= ControlService.CURRENT_DS4_CONTROLLER_LIMIT)
                return false;

            if (string.IsNullOrWhiteSpace(profileName))
                return false;

            // 接続中のデバイス情報をもとにログメッセージを組み立てる
            string prolog = string.Empty;
            try
            {
                prolog = string.Format(DS4WinWPF.Properties.Resources.UsingProfile, (slotIndex + 1).ToString(), profileName, "");
            }
            catch { }

            // ★通知設定の有効・無効を一元反映（Phase5-Step14 フェーズD暫定是正: 契機5統合に伴い、
            //   従来 Global.Notifications を使用していたが、これは「通知を表示」というシステム通知の
            //   重大度設定であり、プロファイル切替通知専用の設定ではない。正しくは
            //   「Display profile switch notification」チェックボックス（ProfileChangedNotification）
            //   を基準とすべきであり、本フェーズで契機5（ControlService.cs）を本メソッド経由に統合するにあたり
            //   従来の契機5の正しい判定基準を退行させないため暫定是正した。
            //   TODO: フェーズG（Phase5-Step14-ProfileSync-And-ApplyUnified-Plan.md §4.7）にて、
            //   本メソッド自体を IProfileApplicationService.ApplyProfile への委譲に書き換え、
            //   本行は削除される予定（案①）。

            // 引数仕様: (deviceIndex, profileName, isTemp: false, launchProgram: false, service, source, prolog,)
            // 内部の ApplyProfile 呼び出しから display 引数を削除
            return ApplyProfile(slotIndex, profileName, false, false, Program.rootHub, source, null);
        }

        /// <summary>
        /// プロファイル適用の共通メソッド。データ更新、ログ出力、UI通知を一箇所で行う。
        /// </summary>
        /// <param name="device">デバイスインデックス</param>
        /// <param name="profileName">適用するプロファイル名</param>
        /// <param name="isTemp">一時プロファイルかどうか</param>
        /// <param name="launchProgram">プログラム起動を行うか</param>
        /// <param name="control">ControlServiceインスタンス</param>
        /// <param name="source">プロファイル変更元</param>
        /// <param name="prolog">ログメッセージのプロローグ（nullの場合は自動生成）</param>
        /// <param name="displayNotification">通知を表示するか</param>
        /// <returns>プロファイル読み込み成功したか</returns>
        public static bool ApplyProfile(int device, string profileName, bool isTemp, bool launchProgram,
                    ControlService control, ProfileChangeSource source = ProfileChangeSource.Unknown, string prolog = null)
        {
            // Debug: ApplyProfile呼び出しログ
            string stackTrace = new System.Diagnostics.StackTrace(1, true).ToString();
            AppLogger.LogDebug($"ApplyProfile CALLED: device={device}, profile={profileName}, isTemp={isTemp}, source={source}");
            AppLogger.LogTrace($"ApplyProfile CallStack:\n{stackTrace}");

            bool result;

            // プロファイル読み込み
            if (isTemp)
            {
                AppLogger.LogDebug($"ApplyProfile: Loading TEMP profile '{profileName}' for device {device}");
                result = LoadTempProfile(device, profileName, launchProgram, control);
            }
            else
            {
                AppLogger.LogDebug($"ApplyProfile: Loading NORMAL profile '{profileName}' for device {device}");
                // 通常プロファイルの場合、ProfilePathを更新してからLoadProfile
                ProfilePath[device] = profileName;
                result = LoadProfile(device, launchProgram, control);
            }

            if (result)
                CompleteProfileApplication(device, profileName, isTemp, control, source, prolog);
            else
            {
                AppLogger.LogDebug($"ApplyProfile: Profile load FAILED for '{profileName}'");
            }

            AppLogger.LogDebug($"ApplyProfile COMPLETED: device={device}, profile={profileName}, result={result}");
            return result;
        }
        #endregion
        internal static void CompleteProfileApplication(int device, string profileName, bool isTemp,
            ControlService control, ProfileChangeSource source, string prolog)
        {
            AppLogger.LogDebug($"ApplyProfile: Profile loaded successfully. Updating state...");
            SelectedProfile[device] = profileName;

            if (!isTemp)
            {
                OlderProfilePath[device] = profileName;
                AppLogger.LogDebug($"ApplyProfile: OlderProfilePath updated to '{profileName}'");
            }
            else
            {
                AppLogger.LogDebug($"ApplyProfile: OlderProfilePath NOT updated (isTemp=true). Current value: '{OlderProfilePath[device]}'");
            }

            if (prolog == null)
            {
                DS4Device ds4Device = control.DS4Controllers[device];
                string battery = ds4Device != null ? $"{ds4Device.Battery}" : "N/A";
                prolog = $"Controller {device + 1} is now using Profile \"{profileName}\" (Battery: {battery}%)";
            }

            AppLogger.LogToGui(prolog, false);
            AppLogger.LogDebug($"ApplyProfile: Calling LogProfileChanged...");
            try
            {
                AppLogger.LogProfileChanged(device, profileName, isTemp, source, prolog, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[ApplyProfile] Failed to log profile change: {ex.Message}");
            }

            AppLogger.LogDebug($"ApplyProfile: Raising SelectedProfileChanged event");
            RaiseSelectedProfileChanged(device, profileName);

            try
            {
                try { DS4Windows.ActionManager.ClearAllEntries(); } catch { }
                AppLogger.LogDebug($"ApplyProfile: Cleared ActionManager global entries to force re-creation of Action instances for new profile");
            }
            catch (Exception ex)
            {
                AppLogger.LogTrace($"ApplyProfile: Failed to clear ActionManager entries: {ex}");
            }

            try
            {
                DS4Windows.Mapping.ClearKeyButtonControllersForDevice(device);
                try { DS4Windows.ActionManager.ClearDeviceState(device); } catch { }
                AppLogger.LogDebug($"ApplyProfile: Cleared per-device SpecialAction controllers and ActionManager state for device {device}");
            }
            catch (Exception ex)
            {
                AppLogger.LogTrace($"ApplyProfile: Failed to clear per-device controllers: {ex}");
            }

            try { DS4Windows.ActionManager.PreallocateForProfileApply(device); } catch { }

            try
            {
                bool foundToggle = false;
                bool foundPress = false;
                var profileActionNames = getProfileActions(device);
                if (profileActionNames != null)
                {
                    foreach (var actionName in profileActionNames)
                    {
                        try
                        {
                            var sa = GetProfileAction(device, actionName);
                            if (sa == null) continue;
                            if (sa.typeID == SpecialAction.ActionTypeId.Key || sa.typeID == SpecialAction.ActionTypeId.Button)
                            {
                                if (sa.KeyButtonSwitchMode.HasValue)
                                {
                                    if (sa.KeyButtonSwitchMode.Value == SpecialAction.KeyButtonSwitchModeEnum.Toggle) foundToggle = true; else foundPress = true;
                                }
                                else
                                {
                                    bool isToggle = false;
                                    try { isToggle = sa.keyType.HasFlag(DS4KeyType.Toggle); } catch { }
                                    if (isToggle) foundToggle = true; else foundPress = true;
                                }
                            }
                        }
                        catch { }
                        if (foundToggle && foundPress) break;
                    }
                }

                AppLogger.LogDebug("ApplyProfile: Controller activation handled per-SpecialAction SwitchMode");
            }
            catch (Exception ex)
            {
                AppLogger.LogTrace($"ApplyProfile: Controller activation check failed: {ex}");
            }
        }

        public static List<string> getProfileActions(int index)
        {
            return m_Config.profileActions[index];
        }

        public static void SaveAction(string name, string controls, int mode,
            string details, bool edit, double delayTime = 0.0, string extras = "")
        {
            // Mark actionDone as uninitialized before mutating actions to reduce
            // the window where MapCustomAction can observe an inconsistent size.
            try
            {
                try { ActionManager.ClearAllEntries(); } catch { }
            }
            catch { }

            m_Config.SaveActionNew(name, controls, mode, details, edit, delayTime, extras);
            //m_Config.SaveAction(name, controls, mode, details, edit, extras);
            //m_Config.SaveActions();

            // Reload actions from file and verify counts to avoid duplicated in-memory entries.
            ReloadActionsAndVerify("SaveAction");

            // If saving a Key-type SpecialAction (mode==4), clear runtime state related to that key only.
            try
            {
                if (mode == 4)
                {
                    if (ushort.TryParse(details, out ushort savedKey))
                    {
                        Mapping.ResetRuntimeStateForKey(savedKey);
                    }
                }
            }
            catch { }
        }

        public static void SaveActions()
        {
            try
            {
                try { ActionManager.ClearAllEntries(); } catch { }
            }
            catch { }

            m_Config.SaveActions();

            // After saving, reload to ensure in-memory list matches file contents.
            ReloadActionsAndVerify("SaveActions");
        }

        public static void RemoveAction(string name)
        {
            try
            {
                try { ActionManager.ClearAllEntries(); } catch { }
            }
            catch { }

            m_Config.RemoveAction(name);

            // After removal, reload to ensure consistency between file and memory.
            ReloadActionsAndVerify("RemoveAction");
        }

        // Reload Actions.xml into the backing store and verify that the in-memory
        // `actions` count matches the number of <Action> elements in the file.
        private static void ReloadActionsAndVerify(string caller)
        {
            try
            {
                // Attempt to reload from the canonical file
                bool loaded = m_Config.LoadActions();

                int memCount = m_Config.actions?.Count ?? 0;
                int fileCount = -1;
                try
                {
                    XmlDocument xd = new XmlDocument();
                    xd.Load(m_Config.m_Actions);
                    XmlNodeList nodes = xd.SelectNodes("/Actions/Action");
                    fileCount = nodes?.Count ?? 0;
                }
                catch (Exception ex)
                {
                    AppLogger.LogToGui($"Could not count Actions.xml entries: {ex.Message}", false);
                }

                if (fileCount >= 0)
                {
                    if (memCount != fileCount)
                    {
                        AppLogger.LogToGui($"Warning: action count mismatch after {caller}: memory={memCount} file={fileCount}", true);
                    }
                    else
                    {
                        AppLogger.LogToGui($"Action list reloaded ({memCount} entries) after {caller}.", false);
                    }
                }
                else
                {
                    AppLogger.LogToGui($"Action list reloaded ({memCount} entries) after {caller}. (file count unknown)", false);
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogToGui($"Error reloading Actions.xml after {caller}: {ex.Message}", true);
            }
        }

        public static bool LoadActions() => m_Config.LoadActions();

        public static int GetActionIndexOf(string name)
        {
            return m_Config.GetActionIndexOf(name);
        }

        public static SpecialAction GetAction(string name)
        {
            return m_Config.GetAction(name);
        }

        public static SpecialAction GetProfileAction(int device, string name)
        {
            SpecialAction sA = null;
            m_Config.profileActionDict[device].TryGetValue(name, out sA);
            return sA;
        }

        public static string getX360ControlString(X360Controls key)
        {
            return m_Config.getX360ControlString(key);
        }

        //public static bool Load() => m_Config.Load();
        public static bool Load()
        {
            var appSettings = DS4WinWPF.AppHost.GetService<DI.IAppSettingsService>();
            if (appSettings != null)
            {
                return appSettings.Load();
            }

            return m_Config.Load();
        }

        public static bool LoadProfile(int device, bool launchprogram, ControlService control,
            bool xinputChange = true, bool postLoad = true)
        {
            Global.loggedInvalidActions.Clear();
            // Phase5-Step2: XML実体I/OをIProfileXmlStore経由に変更。
            // 内部実装は引き続きBackingStore.LoadProfileへ委譲するため挙動は変更しない。
            bool result = ProfileXmlStoreInstance.LoadProfileXml(device, launchprogram, control, "", xinputChange, postLoad);
            ProfileSettingsServiceInstance.SetTempProfileName(device, string.Empty);
            ProfileSettingsServiceInstance.SetUseTempProfile(device, false);
            tempprofileDistance[device] = false;

            return result;
        }

        public static bool LoadTempProfile(int device, string name, bool launchprogram,
            ControlService control, bool xinputChange = true)
        {
            Global.loggedInvalidActions.Clear();
            bool result = m_Config.LoadProfile(device, launchprogram, control, Path.Combine(appdatapath, "Profiles", $"{name}.xml"));
            //bool result = m_Config.LoadProfile(device, launchprogram, control, Path.Combine(appdatapath, "Profiles", $"{name}.xml"));
            if (result)
            {
                ProfileSettingsServiceInstance.SetTempProfileName(device, name);
                ProfileSettingsServiceInstance.SetUseTempProfile(device, true);
                tempprofileDistance[device] = name.ToLower().Contains("distance");
            }

            return result;
        }

        public static bool SaveControllerConfigs(DS4Device device = null)
        {
            if (device != null)
                return m_Config.SaveControllerConfigsForDevice(device);

            for (int idx = 0; idx < ControlService.MAX_DS4_CONTROLLER_COUNT; idx++)
                if (Program.rootHub.DS4Controllers[idx] != null)
                    m_Config.SaveControllerConfigsForDevice(Program.rootHub.DS4Controllers[idx]);

            return true;
        }

        public static bool LoadControllerConfigs(DS4Device device = null)
        {
            if (device != null)
                return m_Config.LoadControllerConfigsForDevice(device);

            for (int idx = 0; idx < ControlService.MAX_DS4_CONTROLLER_COUNT; idx++)
                if (Program.rootHub.DS4Controllers[idx] != null)
                    m_Config.LoadControllerConfigsForDevice(Program.rootHub.DS4Controllers[idx]);

            return true;
        }

        private static byte applyRatio(byte b1, byte b2, double r)
        {
            if (r > 100.0)
                r = 100.0;
            else if (r < 0.0)
                r = 0.0;

            r *= 0.01;
            return (byte)Math.Round((b1 * (1 - r)) + b2 * r, 0);
        }

        public static DS4Color getTransitionedColor(ref DS4Color c1, ref DS4Color c2, double ratio)
        {
            //Color cs = Color.FromArgb(c1.red, c1.green, c1.blue);
            DS4Color cs = new DS4Color
            {
                red = applyRatio(c1.red, c2.red, ratio),
                green = applyRatio(c1.green, c2.green, ratio),
                blue = applyRatio(c1.blue, c2.blue, ratio)
            };
            return cs;
        }

        private static Color applyRatio(Color c1, Color c2, uint r)
        {
            float ratio = r / 100f;
            float hue1 = c1.GetHue();
            float hue2 = c2.GetHue();
            float bri1 = c1.GetBrightness();
            float bri2 = c2.GetBrightness();
            float sat1 = c1.GetSaturation();
            float sat2 = c2.GetSaturation();
            float hr = hue2 - hue1;
            float br = bri2 - bri1;
            float sr = sat2 - sat1;
            Color csR;
            if (bri1 == 0)
                csR = HuetoRGB(hue2, sat2, bri2 - br * ratio);
            else
                csR = HuetoRGB(hue2 - hr * ratio, sat2 - sr * ratio, bri2 - br * ratio);

            return csR;
        }

        public static Color HuetoRGB(float hue, float sat, float bri)
        {
            float C = (1 - Math.Abs(2 * bri) - 1) * sat;
            float X = C * (1 - Math.Abs((hue / 60) % 2 - 1));
            float m = bri - C / 2;
            float R, G, B;
            if (0 <= hue && hue < 60)
            {
                R = C; G = X; B = 0;
            }
            else if (60 <= hue && hue < 120)
            {
                R = X; G = C; B = 0;
            }
            else if (120 <= hue && hue < 180)
            {
                R = 0; G = C; B = X;
            }
            else if (180 <= hue && hue < 240)
            {
                R = 0; G = X; B = C;
            }
            else if (240 <= hue && hue < 300)
            {
                R = X; G = 0; B = C;
            }
            else if (300 <= hue && hue < 360)
            {
                R = C; G = 0; B = X;
            }
            else
            {
                R = 255; G = 0; B = 0;
            }

            R += m; G += m; B += m;
            R *= 255.0f; G *= 255.0f; B *= 255.0f;
            return Color.FromArgb((int)R, (int)G, (int)B);
        }

        public static double Clamp(double min, double value, double max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        private static int ClampInt(int min, int value, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        public static void InitOutputKBMHandler(string identifier)
        {
            outputKBMHandler = VirtualKBMFactory.DetermineHandler(identifier);
            // For testing: disable fake key repeat to isolate whether repeated synthetic sends
            // are due to fakeKeyRepeat logic. Re-enable or remove this for normal builds.
            if (outputKBMHandler != null)
            {
                try
                {
                    AppLogger.LogDebug($"Output KBM handler fakeKeyRepeat={outputKBMHandler.fakeKeyRepeat} for {outputKBMHandler.GetIdentifier()}");
                }
                catch (Exception)
                {
                    // ignore - defensive in case handler implementation restricts setting
                }
            }
        }

        public static void InitOutputKBMMapping(string identifier)
        {
            outputKBMMapping = VirtualKBMFactory.GetMappingInstance(identifier);
        }

        public static void RefreshFakerInputInfo()
        {
            fakerInputInstalled = IsFakerInputInstalled();
            fakerInputVersion = FakerInputVersion();
        }

        /// <summary>
        /// Take Windows virtual key value and refresh action alias for currently used output KB+M system
        /// </summary>
        /// <param name="setting">Instance of edited DS4ControlSettings object</param>
        /// <param name="shift">Flag to indicate if shift action is being modified</param>
        public static void RefreshActionAlias(DS4ControlSettings setting, bool shift)
        {
            if (!shift)
            {
                setting.action.actionAlias = 0;
                if (setting.actionType == DS4ControlSettings.ActionType.Key)
                {
                    setting.action.actionAlias = outputKBMMapping.GetRealEventKey(Convert.ToUInt32(setting.action.actionKey));
                }
            }
            else
            {
                setting.shiftAction.actionAlias = 0;
                if (setting.shiftActionType == DS4ControlSettings.ActionType.Key)
                {
                    setting.shiftAction.actionAlias = outputKBMMapping.GetRealEventKey(Convert.ToUInt32(setting.shiftAction.actionKey));
                }
            }
        }

    }
}

