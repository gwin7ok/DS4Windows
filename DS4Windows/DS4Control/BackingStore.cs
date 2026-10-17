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

    public class BackingStore
    {
        // ProfileEditor layout fields (DTOと統合)
        public const int DEFAULT_PROFILE_EDITOR_LEFT_WIDTH = WindowLayoutDefaults.PROFILE_EDITOR_LEFT_WIDTH;
        public const int DEFAULT_PROFILE_EDITOR_RIGHT_WIDTH = WindowLayoutDefaults.PROFILE_EDITOR_RIGHT_WIDTH;
        public const int DEFAULT_SPECIAL_ACTION_NAME_COL_WIDTH = WindowLayoutDefaults.SPECIAL_ACTION_NAME_COL_WIDTH;
        public const int DEFAULT_SPECIAL_ACTION_TRIGGER_COL_WIDTH = WindowLayoutDefaults.SPECIAL_ACTION_TRIGGER_COL_WIDTH;
        public const int DEFAULT_SPECIAL_ACTION_DETAIL_COL_WIDTH = WindowLayoutDefaults.SPECIAL_ACTION_DETAIL_COL_WIDTH;
        // Active 列はプロファイルに永続化しない列幅の初期値
        public const int DEFAULT_SPECIAL_ACTION_ACTIVE_COL_WIDTH = WindowLayoutDefaults.SPECIAL_ACTION_ACTIVE_COL_WIDTH;
        // SpecialActionDeleteColWidth 定数削除

        // Controller tab column widths
        public const int DEFAULT_CONTROLLER_INDEX_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_INDEX_COL_WIDTH;
        public const int DEFAULT_CONTROLLER_ID_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_ID_COL_WIDTH;
        public const int DEFAULT_CONTROLLER_STATUS_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_STATUS_COL_WIDTH;
        public const int DEFAULT_CONTROLLER_EXCLUSIVE_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_EXCLUSIVE_COL_WIDTH;
        public const int DEFAULT_CONTROLLER_BATTERY_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_BATTERY_COL_WIDTH;
        public const int DEFAULT_CONTROLLER_SELECTPROFILE_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_SELECTPROFILE_COL_WIDTH;
        public const int DEFAULT_CONTROLLER_EDIT_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_EDIT_COL_WIDTH;
        public const int DEFAULT_CONTROLLER_LINKED_PROFILE_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_LINKED_PROFILE_COL_WIDTH;
        public const int DEFAULT_CONTROLLER_LINK_PROF_ID_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_LINK_PROF_ID_COL_WIDTH;
        public const int DEFAULT_CONTROLLER_CUSTOMCOLOR_COL_WIDTH = WindowLayoutDefaults.CONTROLLER_CUSTOMCOLOR_COL_WIDTH;

        // Log settings
        public const int DEFAULT_LOG_MAX_ARCHIVE_FILES = 50;
        public const string DEFAULT_LOG_MIN_LEVEL = "Info";

        public int profileEditorLeftWidth = DEFAULT_PROFILE_EDITOR_LEFT_WIDTH;
        public int profileEditorRightWidth = DEFAULT_PROFILE_EDITOR_RIGHT_WIDTH;
        public int specialActionNameColWidth = DEFAULT_SPECIAL_ACTION_NAME_COL_WIDTH;
        public int specialActionTriggerColWidth = DEFAULT_SPECIAL_ACTION_TRIGGER_COL_WIDTH;
        public int specialActionDetailColWidth = DEFAULT_SPECIAL_ACTION_DETAIL_COL_WIDTH;
        // SpecialActionDeleteColWidth フィールド削除

        public int controllerIndexColWidth = DEFAULT_CONTROLLER_INDEX_COL_WIDTH;
        public int controllerIdColWidth = DEFAULT_CONTROLLER_ID_COL_WIDTH;
        public int controllerStatusColWidth = DEFAULT_CONTROLLER_STATUS_COL_WIDTH;
        public int controllerExclusiveColWidth = DEFAULT_CONTROLLER_EXCLUSIVE_COL_WIDTH;
        public int controllerBatteryColWidth = DEFAULT_CONTROLLER_BATTERY_COL_WIDTH;
        public int controllerSelectProfileColWidth = DEFAULT_CONTROLLER_SELECTPROFILE_COL_WIDTH;
        public int controllerEditColWidth = DEFAULT_CONTROLLER_EDIT_COL_WIDTH;
        public int controllerLinkedProfileColWidth = DEFAULT_CONTROLLER_LINKED_PROFILE_COL_WIDTH;
        public int controllerLinkProfIdColWidth = DEFAULT_CONTROLLER_LINK_PROF_ID_COL_WIDTH;
        public int controllerCustomColorColWidth = DEFAULT_CONTROLLER_CUSTOMCOLOR_COL_WIDTH;

        public int logMaxArchiveFiles = DEFAULT_LOG_MAX_ARCHIVE_FILES;
        public string logMinLevel = DEFAULT_LOG_MIN_LEVEL;

        public const double DEFAULT_UDP_SMOOTH_MINCUTOFF = 0.4;
        public const double DEFAULT_UDP_SMOOTH_BETA = 0.2;
        // Use 15 minutes for default Idle Disconnect when initially enabling the option
        public const int DEFAULT_ENABLE_IDLE_DISCONN_MINS = 15;
        public const double DEFAULT_SX_TILT_DEADZONE = 0.25;
        public const double DEFAULT_SX_TILT_MAXZONE = 1.0;
        public const string DEFAULT_SA_TRIGGERS = "-1";
        public const string DEFAULT_GYRO_MSTICK_TRIGGERS = "-1";
        public const OutContType DEFAULT_OUT_CONT_TYPE = OutContType.X360;
        public const bool DEFAULT_OUTPUT_TO_DS4 = true;
        public const bool DEFAULT_TOUCH_TOGGLE = true;
        public const bool DEFAULT_TOUCHPAD_JITTER_COMP = true;
        public const int DEFAULT_TOUCHPAD_SENS = 100;
        public const int DEFAULT_DS4_BT_POLL_RATE = 4;
        public const int DEFAULT_RUMBLE = 100;
        public const double DEFAULT_ANALOG_SENS = 1.0;
        public const bool DEFAULT_DINPUT_ONLY = false;
        public const TouchpadOutMode DEFAULT_TOUCH_OUT_MODE = TouchpadOutMode.Mouse;
        public const GyroOutMode DEFAULT_GYRO_OUT_MODE = GyroOutMode.Controls;
        public const bool DEFAULT_SA_TRIGGER_COND = true;
        public const bool DEFAULT_SA_MSTICK_TRIGGER_COND = true;
        public const bool DEFAULT_GYRO_TRIGGER_TURNS = true;
        public const bool DEFAULT_GYRO_MSTICK_TRIGGER_TURNS = true;
        public const int DEFAULT_SA_WHEEL_EMULATION_RANGE = 360;
        public const int DEFAULT_GYRO_SENS = 100;
        public const int DEFAULT_GYRO_SENS_VERTICAL_SCALE = 100;
        public const int DEFAULT_TOUCH_DIS_INVERT_TRIGGER = -1;
        public const bool DEFAULT_TRACKBALL_MODE = false;
        public const double DEFAULT_TRACKBALL_FRICTION = 10.0;

        // Stick output curve consts in place more as a precaution
        public const string DEFAULT_STICK_OUTPUT_CURVE = "linear";
        public const int DEFAULT_STICK_OUTPUT_CURVE_ID = 0;
        public const string DEFAULT_SA_OUTPUT_CURVE = "linear";
        public const int DEFAULT_SA_OUTPUT_CURVE_ID = 0;

        public String m_Profile = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName + "\\Profiles.xml";
        public String m_Actions = Global.appdatapath + "\\Actions.xml";
        public string m_linkedProfiles = Global.appdatapath + "\\LinkedProfiles.xml";
        public string m_controllerConfigs = Global.appdatapath + "\\ControllerConfigs.xml";

        protected XmlDocument m_Xdoc = new XmlDocument();
        // ninth (fifth in old builds) value used for options, not last controller
        public ButtonMouseInfo[] buttonMouseInfos = new ButtonMouseInfo[Global.TEST_PROFILE_ITEM_COUNT]
        {
            new ButtonMouseInfo(), new ButtonMouseInfo(), new ButtonMouseInfo(),
            new ButtonMouseInfo(), new ButtonMouseInfo(), new ButtonMouseInfo(),
            new ButtonMouseInfo(), new ButtonMouseInfo(), new ButtonMouseInfo(),
        };

        public bool[] enableTouchToggle = new bool[Global.TEST_PROFILE_ITEM_COUNT]
        { DEFAULT_TOUCH_TOGGLE, DEFAULT_TOUCH_TOGGLE, DEFAULT_TOUCH_TOGGLE,
          DEFAULT_TOUCH_TOGGLE, DEFAULT_TOUCH_TOGGLE, DEFAULT_TOUCH_TOGGLE,
          DEFAULT_TOUCH_TOGGLE, DEFAULT_TOUCH_TOGGLE, DEFAULT_TOUCH_TOGGLE };
        public int[] idleDisconnectTimeout = new int[Global.TEST_PROFILE_ITEM_COUNT] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public bool[] enableOutputDataToDS4 = new bool[Global.TEST_PROFILE_ITEM_COUNT]
        { DEFAULT_OUTPUT_TO_DS4, DEFAULT_OUTPUT_TO_DS4, DEFAULT_OUTPUT_TO_DS4,
          DEFAULT_OUTPUT_TO_DS4, DEFAULT_OUTPUT_TO_DS4, DEFAULT_OUTPUT_TO_DS4,
          DEFAULT_OUTPUT_TO_DS4, DEFAULT_OUTPUT_TO_DS4, DEFAULT_OUTPUT_TO_DS4 };
        public bool[] touchpadJitterCompensation = new bool[Global.TEST_PROFILE_ITEM_COUNT]
        { DEFAULT_TOUCHPAD_JITTER_COMP, DEFAULT_TOUCHPAD_JITTER_COMP, DEFAULT_TOUCHPAD_JITTER_COMP,
          DEFAULT_TOUCHPAD_JITTER_COMP, DEFAULT_TOUCHPAD_JITTER_COMP, DEFAULT_TOUCHPAD_JITTER_COMP,
          DEFAULT_TOUCHPAD_JITTER_COMP, DEFAULT_TOUCHPAD_JITTER_COMP, DEFAULT_TOUCHPAD_JITTER_COMP };
        public bool[] lowerRCOn = new bool[Global.TEST_PROFILE_ITEM_COUNT] { false, false, false, false, false, false, false, false, false };
        public bool[] touchClickPassthru = new bool[Global.TEST_PROFILE_ITEM_COUNT] { false, false, false, false, false, false, false, false, false };
        public string[] profilePath = new string[Global.TEST_PROFILE_ITEM_COUNT] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
        public string[] olderProfilePath = new string[Global.TEST_PROFILE_ITEM_COUNT] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
        public string[] selectedProfile = new string[Global.TEST_PROFILE_ITEM_COUNT] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
        public string[] linkedProfileUI = new string[Global.TEST_PROFILE_ITEM_COUNT] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
        // プロファイル切り替え時の無効アクション名エラーログ抑制用（Phase7-Step3c W3: Global の静的フィールドから移した）
        public HashSet<string> loggedInvalidActions = new HashSet<string>();
        public bool[] firstConnectionAfterStartup = new bool[Global.TEST_PROFILE_ITEM_COUNT] { true, true, true, true, true, true, true, true, true };
        public Dictionary<string, string> linkedProfiles = new Dictionary<string, string>();
        // Cache properties instead of performing a string comparison every frame
        public bool[] distanceProfiles = new bool[Global.TEST_PROFILE_ITEM_COUNT] { false, false, false, false, false, false, false, false, false };
        public Byte[] rumble = new Byte[Global.TEST_PROFILE_ITEM_COUNT]
        { DEFAULT_RUMBLE, DEFAULT_RUMBLE, DEFAULT_RUMBLE,
          DEFAULT_RUMBLE, DEFAULT_RUMBLE, DEFAULT_RUMBLE,
          DEFAULT_RUMBLE, DEFAULT_RUMBLE, DEFAULT_RUMBLE };
        public int[] rumbleAutostopTime = new int[Global.TEST_PROFILE_ITEM_COUNT] { 0, 0, 0, 0, 0, 0, 0, 0, 0 }; // Value in milliseconds (0=autustop timer disabled)
        public Byte[] touchSensitivity = new Byte[Global.TEST_PROFILE_ITEM_COUNT]
        { DEFAULT_TOUCHPAD_SENS, DEFAULT_TOUCHPAD_SENS, DEFAULT_TOUCHPAD_SENS,
          DEFAULT_TOUCHPAD_SENS, DEFAULT_TOUCHPAD_SENS, DEFAULT_TOUCHPAD_SENS,
          DEFAULT_TOUCHPAD_SENS, DEFAULT_TOUCHPAD_SENS, DEFAULT_TOUCHPAD_SENS };
        public StickDeadZoneInfo[] lsModInfo = new StickDeadZoneInfo[Global.TEST_PROFILE_ITEM_COUNT]
        {
            new StickDeadZoneInfo(), new StickDeadZoneInfo(),
            new StickDeadZoneInfo(), new StickDeadZoneInfo(),
            new StickDeadZoneInfo(), new StickDeadZoneInfo(),
            new StickDeadZoneInfo(), new StickDeadZoneInfo(),
            new StickDeadZoneInfo(),
        };
        public StickDeadZoneInfo[] rsModInfo = new StickDeadZoneInfo[Global.TEST_PROFILE_ITEM_COUNT]
        {
            new StickDeadZoneInfo(), new StickDeadZoneInfo(),
            new StickDeadZoneInfo(), new StickDeadZoneInfo(),
            new StickDeadZoneInfo(), new StickDeadZoneInfo(),
            new StickDeadZoneInfo(), new StickDeadZoneInfo(),
            new StickDeadZoneInfo(),
        };
        public TriggerDeadZoneZInfo[] l2ModInfo = new TriggerDeadZoneZInfo[Global.TEST_PROFILE_ITEM_COUNT]
        {
            new TriggerDeadZoneZInfo(), new TriggerDeadZoneZInfo(),
            new TriggerDeadZoneZInfo(), new TriggerDeadZoneZInfo(),
            new TriggerDeadZoneZInfo(), new TriggerDeadZoneZInfo(),
            new TriggerDeadZoneZInfo(), new TriggerDeadZoneZInfo(),
            new TriggerDeadZoneZInfo(),
        };
        public TriggerDeadZoneZInfo[] r2ModInfo = new TriggerDeadZoneZInfo[Global.TEST_PROFILE_ITEM_COUNT]
        {
            new TriggerDeadZoneZInfo(), new TriggerDeadZoneZInfo(),
            new TriggerDeadZoneZInfo(), new TriggerDeadZoneZInfo(),
            new TriggerDeadZoneZInfo(), new TriggerDeadZoneZInfo(),
            new TriggerDeadZoneZInfo(), new TriggerDeadZoneZInfo(),
            new TriggerDeadZoneZInfo(),
        };

        // Rotation angle expressed in radians
        public double[] LSRotation = new double[Global.TEST_PROFILE_ITEM_COUNT] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };

        // Rotation angle expressed in radians
        public double[] RSRotation = new double[Global.TEST_PROFILE_ITEM_COUNT] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };

        public double[] SXDeadzone = new double[Global.TEST_PROFILE_ITEM_COUNT] { DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE },
            SZDeadzone = new double[Global.TEST_PROFILE_ITEM_COUNT] { DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE, DEFAULT_SX_TILT_DEADZONE };

        public double[] SXMaxzone = new double[Global.TEST_PROFILE_ITEM_COUNT]
        { DEFAULT_SX_TILT_MAXZONE, DEFAULT_SX_TILT_MAXZONE, DEFAULT_SX_TILT_MAXZONE,
            DEFAULT_SX_TILT_MAXZONE, DEFAULT_SX_TILT_MAXZONE, DEFAULT_SX_TILT_MAXZONE,
            DEFAULT_SX_TILT_MAXZONE, DEFAULT_SX_TILT_MAXZONE, DEFAULT_SX_TILT_MAXZONE };
        public double[] SZMaxzone = new double[Global.TEST_PROFILE_ITEM_COUNT]
        { DEFAULT_SX_TILT_MAXZONE, DEFAULT_SX_TILT_MAXZONE, DEFAULT_SX_TILT_MAXZONE,
          DEFAULT_SX_TILT_MAXZONE, DEFAULT_SX_TILT_MAXZONE, DEFAULT_SX_TILT_MAXZONE,
          DEFAULT_SX_TILT_MAXZONE, DEFAULT_SX_TILT_MAXZONE, DEFAULT_SX_TILT_MAXZONE };
        public double[] SXAntiDeadzone = new double[Global.TEST_PROFILE_ITEM_COUNT] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 },
            SZAntiDeadzone = new double[Global.TEST_PROFILE_ITEM_COUNT] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };
        public double[] l2Sens = new double[Global.TEST_PROFILE_ITEM_COUNT]
        { DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS,
          DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS,
          DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS };
        public double[] r2Sens = new double[Global.TEST_PROFILE_ITEM_COUNT]
        { DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS,
          DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS,
          DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS };
        public double[] LSSens = new double[Global.TEST_PROFILE_ITEM_COUNT]
        { DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS,
          DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS,
          DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS};
        public double[] RSSens = new double[Global.TEST_PROFILE_ITEM_COUNT]
        { DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS,
          DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS,
          DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS };
        public double[] SXSens = new double[Global.TEST_PROFILE_ITEM_COUNT]
        { DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS,
          DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS,
          DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS };
        public double[] SZSens = new double[Global.TEST_PROFILE_ITEM_COUNT]
        { DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS,
          DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS,
          DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS, DEFAULT_ANALOG_SENS };
        public Byte[] tapSensitivity = new Byte[Global.TEST_PROFILE_ITEM_COUNT] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public bool[] doubleTap = new bool[Global.TEST_PROFILE_ITEM_COUNT] { false, false, false, false, false, false, false, false, false };
        public int[] scrollSensitivity = new int[Global.TEST_PROFILE_ITEM_COUNT] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public int[] touchpadInvert = new int[Global.TEST_PROFILE_ITEM_COUNT] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public int[] btPollRate = new int[Global.TEST_PROFILE_ITEM_COUNT]
        { DEFAULT_DS4_BT_POLL_RATE, DEFAULT_DS4_BT_POLL_RATE, DEFAULT_DS4_BT_POLL_RATE,
          DEFAULT_DS4_BT_POLL_RATE, DEFAULT_DS4_BT_POLL_RATE, DEFAULT_DS4_BT_POLL_RATE,
          DEFAULT_DS4_BT_POLL_RATE, DEFAULT_DS4_BT_POLL_RATE, DEFAULT_DS4_BT_POLL_RATE };
        public int[] gyroMouseDZ = new int[Global.TEST_PROFILE_ITEM_COUNT] { MouseCursor.GYRO_MOUSE_DEADZONE, MouseCursor.GYRO_MOUSE_DEADZONE,
            MouseCursor.GYRO_MOUSE_DEADZONE, MouseCursor.GYRO_MOUSE_DEADZONE,
            MouseCursor.GYRO_MOUSE_DEADZONE, MouseCursor.GYRO_MOUSE_DEADZONE,
            MouseCursor.GYRO_MOUSE_DEADZONE,MouseCursor.GYRO_MOUSE_DEADZONE,
            MouseCursor.GYRO_MOUSE_DEADZONE,};
        public bool[] gyroMouseToggle = new bool[Global.TEST_PROFILE_ITEM_COUNT] { false, false, false,
            false, false, false, false, false, false };

        public SquareStickInfo[] squStickInfo = new SquareStickInfo[Global.TEST_PROFILE_ITEM_COUNT]
        {
            new SquareStickInfo(), new SquareStickInfo(),
            new SquareStickInfo(), new SquareStickInfo(),
            new SquareStickInfo(), new SquareStickInfo(),
            new SquareStickInfo(), new SquareStickInfo(),
            new SquareStickInfo(),
        };

        public StickAntiSnapbackInfo[] lsAntiSnapbackInfo = new StickAntiSnapbackInfo[Global.TEST_PROFILE_ITEM_COUNT]
        {
            new StickAntiSnapbackInfo(), new StickAntiSnapbackInfo(),
            new StickAntiSnapbackInfo(), new StickAntiSnapbackInfo(),
            new StickAntiSnapbackInfo(), new StickAntiSnapbackInfo(),
            new StickAntiSnapbackInfo(), new StickAntiSnapbackInfo(),
            new StickAntiSnapbackInfo(),
        };

        public StickAntiSnapbackInfo[] rsAntiSnapbackInfo = new StickAntiSnapbackInfo[Global.TEST_PROFILE_ITEM_COUNT]
        {
            new StickAntiSnapbackInfo(), new StickAntiSnapbackInfo(),
            new StickAntiSnapbackInfo(), new StickAntiSnapbackInfo(),
            new StickAntiSnapbackInfo(), new StickAntiSnapbackInfo(),
            new StickAntiSnapbackInfo(), new StickAntiSnapbackInfo(),
            new StickAntiSnapbackInfo(),
        };

        public StickOutputSetting[] lsOutputSettings = new StickOutputSetting[Global.TEST_PROFILE_ITEM_COUNT]
        {
            new StickOutputSetting(), new StickOutputSetting(), new StickOutputSetting(),
            new StickOutputSetting(), new StickOutputSetting(), new StickOutputSetting(),
            new StickOutputSetting(), new StickOutputSetting(), new StickOutputSetting(),
        };

        public StickOutputSetting[] rsOutputSettings = new StickOutputSetting[Global.TEST_PROFILE_ITEM_COUNT]
        {
            new StickOutputSetting(), new StickOutputSetting(), new StickOutputSetting(),
            new StickOutputSetting(), new StickOutputSetting(), new StickOutputSetting(),
            new StickOutputSetting(), new StickOutputSetting(), new StickOutputSetting(),
        };

        public TriggerOutputSettings[] l2OutputSettings = new TriggerOutputSettings[Global.TEST_PROFILE_ITEM_COUNT]
        {
            new TriggerOutputSettings(), new TriggerOutputSettings(), new TriggerOutputSettings(),
            new TriggerOutputSettings(), new TriggerOutputSettings(), new TriggerOutputSettings(),
            new TriggerOutputSettings(), new TriggerOutputSettings(), new TriggerOutputSettings(),
        };

        public TriggerOutputSettings[] r2OutputSettings = new TriggerOutputSettings[Global.TEST_PROFILE_ITEM_COUNT]
        {
            new TriggerOutputSettings(), new TriggerOutputSettings(), new TriggerOutputSettings(),
            new TriggerOutputSettings(), new TriggerOutputSettings(), new TriggerOutputSettings(),
            new TriggerOutputSettings(), new TriggerOutputSettings(), new TriggerOutputSettings(),
        };

        public SteeringWheelSmoothingInfo[] wheelSmoothInfo = new SteeringWheelSmoothingInfo[Global.TEST_PROFILE_ITEM_COUNT]
        {
            new SteeringWheelSmoothingInfo(), new SteeringWheelSmoothingInfo(),
            new SteeringWheelSmoothingInfo(), new SteeringWheelSmoothingInfo(),
            new SteeringWheelSmoothingInfo(), new SteeringWheelSmoothingInfo(),
            new SteeringWheelSmoothingInfo(), new SteeringWheelSmoothingInfo(),
            new SteeringWheelSmoothingInfo(),
        };

        public int[] saWheelFuzzValues = new int[Global.TEST_PROFILE_ITEM_COUNT];


        // Start of DualSense specific profile options
        //
        public DualSenseDevice.RumbleEmulationMode[] dualSenseRumbleEmulationMode = new DualSenseDevice.RumbleEmulationMode[Global.TEST_PROFILE_ITEM_COUNT]
        {
            0,0,0,0,0,0,0,0,0
        };
        public bool[] useGenericRumbleRescaleForDualSenses = new bool[Global.TEST_PROFILE_ITEM_COUNT] { false, false, false, false, false, false, false, false, false };
        public byte[] dualSenseHapticPowerLevel = new byte[Global.TEST_PROFILE_ITEM_COUNT]
        {
            0,0,0,0,0,0,0,0,0
        };
        //
        // End of DualSense specific profile options

        private void setOutBezierCurveObjArrayItem(BezierCurve[] bezierCurveArray, int device, int curveOptionValue, BezierCurve.AxisType axisType)
        {
            // Set bezier curve obj of axis. 0=Linear (no curve mapping), 1-5=Pre-defined curves, 6=User supplied custom curve string value of a profile (comma separated list of 4 decimal numbers)
            switch (curveOptionValue)
            {
                // Commented out case 1..5 because Mapping.cs:SetCurveAndDeadzone function has the original IF-THEN-ELSE code logic for those original 1..5 output curve mappings (ie. no need to initialize the lookup result table).
                // Only the new bezier custom curve option 6 uses the lookup result table (initialized in BezierCurve class based on an input curve definition).
                //case 1: bezierCurveArray[device].InitBezierCurve(99.0, 91.0, 0.00, 0.00, axisType); break;  // Enhanced Precision (hard-coded curve) (almost the same curve as bezier 0.70, 0.28, 1.00, 1.00)
                //case 2: bezierCurveArray[device].InitBezierCurve(99.0, 92.0, 0.00, 0.00, axisType); break;  // Quadric
                //case 3: bezierCurveArray[device].InitBezierCurve(99.0, 93.0, 0.00, 0.00, axisType); break;  // Cubic
                //case 4: bezierCurveArray[device].InitBezierCurve(99.0, 94.0, 0.00, 0.00, axisType); break;  // Easeout Quad
                //case 5: bezierCurveArray[device].InitBezierCurve(99.0, 95.0, 0.00, 0.00, axisType); break;  // Easeout Cubic
                case 6: bezierCurveArray[device].InitBezierCurve(bezierCurveArray[device].CustomDefinition, axisType); break;  // Custom output curve
            }
        }

        public BezierCurve[] lsOutBezierCurveObj = new BezierCurve[Global.TEST_PROFILE_ITEM_COUNT] { new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve() };
        public BezierCurve[] rsOutBezierCurveObj = new BezierCurve[Global.TEST_PROFILE_ITEM_COUNT] { new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve() };
        public BezierCurve[] l2OutBezierCurveObj = new BezierCurve[Global.TEST_PROFILE_ITEM_COUNT] { new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve() };
        public BezierCurve[] r2OutBezierCurveObj = new BezierCurve[Global.TEST_PROFILE_ITEM_COUNT] { new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve() };
        public BezierCurve[] sxOutBezierCurveObj = new BezierCurve[Global.TEST_PROFILE_ITEM_COUNT] { new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve() };
        public BezierCurve[] szOutBezierCurveObj = new BezierCurve[Global.TEST_PROFILE_ITEM_COUNT] { new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve(), new BezierCurve() };

        private int[] _lsOutCurveMode = new int[Global.TEST_PROFILE_ITEM_COUNT] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public int getLsOutCurveMode(int index) { return _lsOutCurveMode[index]; }
        public void setLsOutCurveMode(int index, int value)
        {
            if (value >= 1) setOutBezierCurveObjArrayItem(lsOutBezierCurveObj, index, value, BezierCurve.AxisType.LSRS);
            _lsOutCurveMode[index] = value;
        }

        private int[] _rsOutCurveMode = new int[Global.TEST_PROFILE_ITEM_COUNT] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public int getRsOutCurveMode(int index) { return _rsOutCurveMode[index]; }
        public void setRsOutCurveMode(int index, int value)
        {
            if (value >= 1) setOutBezierCurveObjArrayItem(rsOutBezierCurveObj, index, value, BezierCurve.AxisType.LSRS);
            _rsOutCurveMode[index] = value;
        }

        private int[] _l2OutCurveMode = new int[Global.TEST_PROFILE_ITEM_COUNT] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public int getL2OutCurveMode(int index) { return _l2OutCurveMode[index]; }
        public void setL2OutCurveMode(int index, int value)
        {
            if (value >= 1) setOutBezierCurveObjArrayItem(l2OutBezierCurveObj, index, value, BezierCurve.AxisType.L2R2);
            _l2OutCurveMode[index] = value;
        }

        private int[] _r2OutCurveMode = new int[Global.TEST_PROFILE_ITEM_COUNT] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public int getR2OutCurveMode(int index) { return _r2OutCurveMode[index]; }
        public void setR2OutCurveMode(int index, int value)
        {
            if (value >= 1) setOutBezierCurveObjArrayItem(r2OutBezierCurveObj, index, value, BezierCurve.AxisType.L2R2);
            _r2OutCurveMode[index] = value;
        }

        private int[] _sxOutCurveMode = new int[Global.TEST_PROFILE_ITEM_COUNT] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public int getSXOutCurveMode(int index) { return _sxOutCurveMode[index]; }
        public void setSXOutCurveMode(int index, int value)
        {
            if (value >= 1) setOutBezierCurveObjArrayItem(sxOutBezierCurveObj, index, value, BezierCurve.AxisType.SA);
            _sxOutCurveMode[index] = value;
        }

        private int[] _szOutCurveMode = new int[Global.TEST_PROFILE_ITEM_COUNT] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public int getSZOutCurveMode(int index) { return _szOutCurveMode[index]; }
        public void setSZOutCurveMode(int index, int value)
        {
            if (value >= 1) setOutBezierCurveObjArrayItem(szOutBezierCurveObj, index, value, BezierCurve.AxisType.SA);
            _szOutCurveMode[index] = value;
        }

        public LightbarSettingInfo[] lightbarSettingInfo = new LightbarSettingInfo[Global.TEST_PROFILE_ITEM_COUNT]
        {
            new LightbarSettingInfo(), new LightbarSettingInfo(),
            new LightbarSettingInfo(), new LightbarSettingInfo(),
            new LightbarSettingInfo(), new LightbarSettingInfo(),
            new LightbarSettingInfo(), new LightbarSettingInfo(),
            new LightbarSettingInfo(),
        };

        public int processPriority;
        public string[] launchProgram = new string[Global.TEST_PROFILE_ITEM_COUNT] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
        public bool[] dinputOnly = new bool[Global.TEST_PROFILE_ITEM_COUNT]
        { DEFAULT_DINPUT_ONLY, DEFAULT_DINPUT_ONLY, DEFAULT_DINPUT_ONLY,
          DEFAULT_DINPUT_ONLY, DEFAULT_DINPUT_ONLY, DEFAULT_DINPUT_ONLY,
          DEFAULT_DINPUT_ONLY, DEFAULT_DINPUT_ONLY, DEFAULT_DINPUT_ONLY };
        public bool[] startTouchpadOff = new bool[Global.TEST_PROFILE_ITEM_COUNT] { false, false, false, false, false, false, false, false, false };
        public TouchpadOutMode[] touchOutMode = new TouchpadOutMode[Global.TEST_PROFILE_ITEM_COUNT]
        { DEFAULT_TOUCH_OUT_MODE, DEFAULT_TOUCH_OUT_MODE, DEFAULT_TOUCH_OUT_MODE,
          DEFAULT_TOUCH_OUT_MODE, DEFAULT_TOUCH_OUT_MODE, DEFAULT_TOUCH_OUT_MODE,
          DEFAULT_TOUCH_OUT_MODE, DEFAULT_TOUCH_OUT_MODE, DEFAULT_TOUCH_OUT_MODE };
        public GyroOutMode[] gyroOutMode = new GyroOutMode[Global.TEST_PROFILE_ITEM_COUNT]
        { DEFAULT_GYRO_OUT_MODE, DEFAULT_GYRO_OUT_MODE, DEFAULT_GYRO_OUT_MODE,
          DEFAULT_GYRO_OUT_MODE, DEFAULT_GYRO_OUT_MODE, DEFAULT_GYRO_OUT_MODE,
          DEFAULT_GYRO_OUT_MODE, DEFAULT_GYRO_OUT_MODE, DEFAULT_GYRO_OUT_MODE };
        public GyroControlsInfo[] gyroControlsInf = new GyroControlsInfo[Global.TEST_PROFILE_ITEM_COUNT]
        {
            new GyroControlsInfo(), new GyroControlsInfo(), new GyroControlsInfo(),
            new GyroControlsInfo(), new GyroControlsInfo(), new GyroControlsInfo(),
            new GyroControlsInfo(), new GyroControlsInfo(), new GyroControlsInfo(),
        };

        public RumbleSettings[] rumbleSettings = new RumbleSettings[Global.TEST_PROFILE_ITEM_COUNT]
                {
            new RumbleSettings(), new RumbleSettings(),
            new RumbleSettings(), new RumbleSettings(),
            new RumbleSettings(), new RumbleSettings(),
            new RumbleSettings(), new RumbleSettings(),
            new RumbleSettings(),
                };

        public string[] sATriggers = new string[Global.TEST_PROFILE_ITEM_COUNT]
        { BackingStore.DEFAULT_SA_TRIGGERS, BackingStore.DEFAULT_SA_TRIGGERS, BackingStore.DEFAULT_SA_TRIGGERS,
          BackingStore.DEFAULT_SA_TRIGGERS, BackingStore.DEFAULT_SA_TRIGGERS, BackingStore.DEFAULT_SA_TRIGGERS,
          BackingStore.DEFAULT_SA_TRIGGERS, BackingStore.DEFAULT_SA_TRIGGERS, BackingStore.DEFAULT_SA_TRIGGERS };
        public string[] sAMouseStickTriggers = new string[Global.TEST_PROFILE_ITEM_COUNT]
        { BackingStore.DEFAULT_GYRO_MSTICK_TRIGGERS, BackingStore.DEFAULT_GYRO_MSTICK_TRIGGERS, BackingStore.DEFAULT_GYRO_MSTICK_TRIGGERS,
          BackingStore.DEFAULT_GYRO_MSTICK_TRIGGERS, BackingStore.DEFAULT_GYRO_MSTICK_TRIGGERS, BackingStore.DEFAULT_GYRO_MSTICK_TRIGGERS,
          BackingStore.DEFAULT_GYRO_MSTICK_TRIGGERS, BackingStore.DEFAULT_GYRO_MSTICK_TRIGGERS, BackingStore.DEFAULT_GYRO_MSTICK_TRIGGERS };
        public bool[] sATriggerCond = new bool[Global.TEST_PROFILE_ITEM_COUNT]
        { DEFAULT_SA_TRIGGER_COND, DEFAULT_SA_TRIGGER_COND, DEFAULT_SA_TRIGGER_COND,
          DEFAULT_SA_TRIGGER_COND, DEFAULT_SA_TRIGGER_COND, DEFAULT_SA_TRIGGER_COND,
          DEFAULT_SA_TRIGGER_COND, DEFAULT_SA_TRIGGER_COND, DEFAULT_SA_TRIGGER_COND };
        public bool[] sAMouseStickTriggerCond = new bool[Global.TEST_PROFILE_ITEM_COUNT]
        { DEFAULT_SA_MSTICK_TRIGGER_COND, DEFAULT_SA_MSTICK_TRIGGER_COND, DEFAULT_SA_MSTICK_TRIGGER_COND,
          DEFAULT_SA_MSTICK_TRIGGER_COND, DEFAULT_SA_MSTICK_TRIGGER_COND, DEFAULT_SA_MSTICK_TRIGGER_COND,
          DEFAULT_SA_MSTICK_TRIGGER_COND, DEFAULT_SA_MSTICK_TRIGGER_COND, DEFAULT_SA_MSTICK_TRIGGER_COND };
        public bool[] gyroMouseStickTriggerTurns = new bool[Global.TEST_PROFILE_ITEM_COUNT]
        { DEFAULT_GYRO_MSTICK_TRIGGER_TURNS, DEFAULT_GYRO_MSTICK_TRIGGER_TURNS, DEFAULT_GYRO_MSTICK_TRIGGER_TURNS,
          DEFAULT_GYRO_MSTICK_TRIGGER_TURNS, DEFAULT_GYRO_MSTICK_TRIGGER_TURNS, DEFAULT_GYRO_MSTICK_TRIGGER_TURNS,
          DEFAULT_GYRO_MSTICK_TRIGGER_TURNS, DEFAULT_GYRO_MSTICK_TRIGGER_TURNS, DEFAULT_GYRO_MSTICK_TRIGGER_TURNS };
        public GyroMouseStickInfo[] gyroMStickInfo = new GyroMouseStickInfo[Global.TEST_PROFILE_ITEM_COUNT]
        {
            new GyroMouseStickInfo(),
            new GyroMouseStickInfo(),
            new GyroMouseStickInfo(), new GyroMouseStickInfo(),
            new GyroMouseStickInfo(), new GyroMouseStickInfo(),
            new GyroMouseStickInfo(), new GyroMouseStickInfo(),
            new GyroMouseStickInfo(),
        };
        public GyroDirectionalSwipeInfo[] gyroSwipeInfo = new GyroDirectionalSwipeInfo[Global.TEST_PROFILE_ITEM_COUNT]
        {
            new GyroDirectionalSwipeInfo(), new GyroDirectionalSwipeInfo(),
            new GyroDirectionalSwipeInfo(), new GyroDirectionalSwipeInfo(),
            new GyroDirectionalSwipeInfo(), new GyroDirectionalSwipeInfo(),
            new GyroDirectionalSwipeInfo(), new GyroDirectionalSwipeInfo(),
            new GyroDirectionalSwipeInfo(),
        };

        public bool[] gyroMouseStickToggle = new bool[Global.TEST_PROFILE_ITEM_COUNT] { false, false, false,
            false, false, false, false, false, false };

        public SASteeringWheelEmulationAxisType[] sASteeringWheelEmulationAxis = new SASteeringWheelEmulationAxisType[Global.TEST_PROFILE_ITEM_COUNT] { SASteeringWheelEmulationAxisType.None, SASteeringWheelEmulationAxisType.None, SASteeringWheelEmulationAxisType.None, SASteeringWheelEmulationAxisType.None, SASteeringWheelEmulationAxisType.None, SASteeringWheelEmulationAxisType.None, SASteeringWheelEmulationAxisType.None, SASteeringWheelEmulationAxisType.None, SASteeringWheelEmulationAxisType.None };
        public int[] sASteeringWheelEmulationRange = new int[Global.TEST_PROFILE_ITEM_COUNT]
        { DEFAULT_SA_WHEEL_EMULATION_RANGE, DEFAULT_SA_WHEEL_EMULATION_RANGE, DEFAULT_SA_WHEEL_EMULATION_RANGE,
          DEFAULT_SA_WHEEL_EMULATION_RANGE, DEFAULT_SA_WHEEL_EMULATION_RANGE, DEFAULT_SA_WHEEL_EMULATION_RANGE,
          DEFAULT_SA_WHEEL_EMULATION_RANGE, DEFAULT_SA_WHEEL_EMULATION_RANGE, DEFAULT_SA_WHEEL_EMULATION_RANGE };
        public int[][] touchDisInvertTriggers = new int[Global.TEST_PROFILE_ITEM_COUNT][]
        { new int[1] { DEFAULT_TOUCH_DIS_INVERT_TRIGGER}, new int[1] { DEFAULT_TOUCH_DIS_INVERT_TRIGGER}, new int[1] { DEFAULT_TOUCH_DIS_INVERT_TRIGGER },
          new int[1] { DEFAULT_TOUCH_DIS_INVERT_TRIGGER}, new int[1] { DEFAULT_TOUCH_DIS_INVERT_TRIGGER }, new int[1] { DEFAULT_TOUCH_DIS_INVERT_TRIGGER },
          new int[1] { DEFAULT_TOUCH_DIS_INVERT_TRIGGER }, new int[1] { DEFAULT_TOUCH_DIS_INVERT_TRIGGER}, new int[1] { DEFAULT_TOUCH_DIS_INVERT_TRIGGER } };
        public Boolean useExclusiveMode = false; // Re-enable Ex Mode

        public const int DEFAULT_FORM_WIDTH = WindowLayoutDefaults.MAIN_WINDOW_WIDTH;
        public int formWidth = DEFAULT_FORM_WIDTH;

        public const int DEFAULT_FORM_HEIGHT = WindowLayoutDefaults.MAIN_WINDOW_HEIGHT;
        public int formHeight = DEFAULT_FORM_HEIGHT;

        public const int DEFAULT_FORM_LOCATION_X = WindowLayoutDefaults.MAIN_WINDOW_LOCATION_X;
        public int formLocationX = DEFAULT_FORM_LOCATION_X;

        public const int DEFAULT_FORM_LOCATION_Y = WindowLayoutDefaults.MAIN_WINDOW_LOCATION_Y;
        public int formLocationY = DEFAULT_FORM_LOCATION_Y;

        public Boolean startMinimized = false;
        public Boolean minToTaskbar = false;
        public DateTime lastChecked;
        public string lastVersionChecked = string.Empty;
        public ulong lastVersionCheckedNum;

        // Legacy CheckWhen removed. Store explicit defaults for the new fields.
        public const int DEFAULT_CHECK_EVERY_VALUE = 1;
        public const int DEFAULT_CHECK_EVERY_UNIT = 1;
        // New explicit startup update-check persistence fields
        public bool checkUpdateStartup = true;
        public int checkEveryValue = 1; // default 1 day
        public int checkEveryUnit = 1; // 0=hours,1=days

        public const int DEFAULT_NOTIFICATIONS = 2;
        public int notifications = DEFAULT_NOTIFICATIONS;
        public bool profileChangedNotification;
        public bool disconnectBTAtStop = false;

        public const bool DEFAULT_SWIPE_PROFILES = false;
        public bool swipeProfiles = DEFAULT_SWIPE_PROFILES;
        public bool ds4Mapping = false;
        public bool quickCharge = false;
        public bool useMoonlight = false;
        public bool useAdvancedMoonlight = false;
        public bool closeMini = false;
        public List<SpecialAction> actions = new List<SpecialAction>();
        public List<DS4ControlSettings>[] ds4settings = new List<DS4ControlSettings>[Global.TEST_PROFILE_ITEM_COUNT]
            { new List<DS4ControlSettings>(), new List<DS4ControlSettings>(), new List<DS4ControlSettings>(),
              new List<DS4ControlSettings>(), new List<DS4ControlSettings>(), new List<DS4ControlSettings>(), new List<DS4ControlSettings>(), new List<DS4ControlSettings>(), new List<DS4ControlSettings>() };
        public ControlSettingsGroup[] ds4controlSettings;

        public List<string>[] profileActions = new List<string>[Global.TEST_PROFILE_ITEM_COUNT] { null, null, null, null, null, null, null, null, null };
        public int[] profileActionCount = new int[Global.TEST_PROFILE_ITEM_COUNT] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public Dictionary<string, SpecialAction>[] profileActionDict = new Dictionary<string, SpecialAction>[Global.TEST_PROFILE_ITEM_COUNT]
            { new Dictionary<string, SpecialAction>(), new Dictionary<string, SpecialAction>(), new Dictionary<string, SpecialAction>(),
              new Dictionary<string, SpecialAction>(), new Dictionary<string, SpecialAction>(), new Dictionary<string, SpecialAction>(), new Dictionary<string, SpecialAction>(), new Dictionary<string, SpecialAction>(), new Dictionary<string, SpecialAction>() };

        public Dictionary<string, int>[] profileActionIndexDict = new Dictionary<string, int>[Global.TEST_PROFILE_ITEM_COUNT]
            { new Dictionary<string, int>(), new Dictionary<string, int>(), new Dictionary<string, int>(),
              new Dictionary<string, int>(), new Dictionary<string, int>(), new Dictionary<string, int>(), new Dictionary<string, int>(), new Dictionary<string, int>(), new Dictionary<string, int>() };

        public string useLang = "";
        public bool downloadLang = true;
        public TrayIconChoice useIconChoice;
        public const bool DEFAULT_FLASH_WHEN_LATE = true;
        public bool flashWhenLate = DEFAULT_FLASH_WHEN_LATE;

        public const int DEFAULT_FLASH_WHEN_LATE_AT = 500;
        public int flashWhenLateAt = DEFAULT_FLASH_WHEN_LATE_AT;
        public bool useOSCServ = false;

        public const int DEFAULT_OSC_SERV_PORT = 9000;
        public int oscServPort = DEFAULT_OSC_SERV_PORT;

        public bool interpretingOscMonitoring = false;

        public bool useOSCSend = false;

        public const int DEFAULT_OSC_SEND_PORT = 9001;
        public int oscSendPort = DEFAULT_OSC_SEND_PORT;

        public const string DEFAULT_OSC_SEND_ADDRESS = "127.0.0.1";
        public string oscSendAddress = DEFAULT_OSC_SEND_ADDRESS;
        public bool useUDPServ = false;

        public const int DEFAULT_UDP_SERV_PORT = 26760;
        public int udpServPort = DEFAULT_UDP_SERV_PORT;

        // 127.0.0.1=IPAddress.Loopback (default), 0.0.0.0=IPAddress.Any as all interfaces, x.x.x.x = Specific ipv4 interface address or hostname
        public const string DEFAULT_UDP_SERV_LISTEN_ADDR = "127.0.0.1";
        public string udpServListenAddress = DEFAULT_UDP_SERV_LISTEN_ADDR;
        public bool useUdpSmoothing;

        public double udpSmoothingMincutoff = DEFAULT_UDP_SMOOTH_MINCUTOFF;
        public double udpSmoothingBeta = DEFAULT_UDP_SMOOTH_BETA;
        public bool useCustomSteamFolder;
        public string customSteamFolder;
        public AppThemeChoice useCurrentTheme;
        public string fakeExeFileName = string.Empty;

        public sbyte[] leftStickDriftXAxis = new sbyte[Global.TEST_PROFILE_ITEM_COUNT]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0
        };
        public sbyte[] leftStickDriftYAxis = new sbyte[Global.TEST_PROFILE_ITEM_COUNT]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0
        };
        public sbyte[] rightStickDriftXAxis = new sbyte[Global.TEST_PROFILE_ITEM_COUNT]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0
        };
        public sbyte[] rightStickDriftYAxis = new sbyte[Global.TEST_PROFILE_ITEM_COUNT]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0
        };

        public int[] debouncingMs = new int[Global.TEST_PROFILE_ITEM_COUNT]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0
        };

        public bool[] inverseRumbleMotors = {
            false, false, false, false, false, false, false, false, false
        };

        public bool useDs3PitchRollSim = false;

        public ControlServiceDeviceOptions deviceOptions =
            new ControlServiceDeviceOptions();

        // Cache whether profile has custom action
        public bool[] containsCustomAction = new bool[Global.TEST_PROFILE_ITEM_COUNT] { false, false, false, false, false, false, false, false, false };

        // Cache whether profile has custom extras
        public bool[] containsCustomExtras = new bool[Global.TEST_PROFILE_ITEM_COUNT] { false, false, false, false, false, false, false, false, false };

        public int[] gyroSensitivity = new int[Global.TEST_PROFILE_ITEM_COUNT]
        { DEFAULT_GYRO_SENS, DEFAULT_GYRO_SENS, DEFAULT_GYRO_SENS,
          DEFAULT_GYRO_SENS, DEFAULT_GYRO_SENS, DEFAULT_GYRO_SENS,
          DEFAULT_GYRO_SENS, DEFAULT_GYRO_SENS, DEFAULT_GYRO_SENS };
        public int[] gyroSensVerticalScale = new int[Global.TEST_PROFILE_ITEM_COUNT]
        { DEFAULT_GYRO_SENS_VERTICAL_SCALE, DEFAULT_GYRO_SENS_VERTICAL_SCALE, DEFAULT_GYRO_SENS_VERTICAL_SCALE,
          DEFAULT_GYRO_SENS_VERTICAL_SCALE, DEFAULT_GYRO_SENS_VERTICAL_SCALE, DEFAULT_GYRO_SENS_VERTICAL_SCALE,
          DEFAULT_GYRO_SENS_VERTICAL_SCALE, DEFAULT_GYRO_SENS_VERTICAL_SCALE, DEFAULT_GYRO_SENS_VERTICAL_SCALE };
        public int[] gyroInvert = new int[Global.TEST_PROFILE_ITEM_COUNT] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public bool[] gyroTriggerTurns = new bool[Global.TEST_PROFILE_ITEM_COUNT]
        { DEFAULT_GYRO_TRIGGER_TURNS, DEFAULT_GYRO_TRIGGER_TURNS, DEFAULT_GYRO_TRIGGER_TURNS,
          DEFAULT_GYRO_TRIGGER_TURNS, DEFAULT_GYRO_TRIGGER_TURNS, DEFAULT_GYRO_TRIGGER_TURNS,
          DEFAULT_GYRO_TRIGGER_TURNS, DEFAULT_GYRO_TRIGGER_TURNS, DEFAULT_GYRO_TRIGGER_TURNS };

        public GyroMouseInfo[] gyroMouseInfo = new GyroMouseInfo[Global.TEST_PROFILE_ITEM_COUNT]
        {
            new GyroMouseInfo(), new GyroMouseInfo(),
            new GyroMouseInfo(), new GyroMouseInfo(),
            new GyroMouseInfo(), new GyroMouseInfo(),
            new GyroMouseInfo(), new GyroMouseInfo(),
            new GyroMouseInfo(),
        };

        public int[] gyroMouseHorizontalAxis = new int[Global.TEST_PROFILE_ITEM_COUNT] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        public int[] gyroMouseStickHorizontalAxis = new int[Global.TEST_PROFILE_ITEM_COUNT] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        public bool[] trackballMode = new bool[Global.TEST_PROFILE_ITEM_COUNT]
        { DEFAULT_TRACKBALL_MODE, DEFAULT_TRACKBALL_MODE, DEFAULT_TRACKBALL_MODE,
          DEFAULT_TRACKBALL_MODE, DEFAULT_TRACKBALL_MODE, DEFAULT_TRACKBALL_MODE,
          DEFAULT_TRACKBALL_MODE, DEFAULT_TRACKBALL_MODE, DEFAULT_TRACKBALL_MODE };
        public double[] trackballFriction = new double[Global.TEST_PROFILE_ITEM_COUNT]
        { DEFAULT_TRACKBALL_FRICTION, DEFAULT_TRACKBALL_FRICTION, DEFAULT_TRACKBALL_FRICTION,
          DEFAULT_TRACKBALL_FRICTION, DEFAULT_TRACKBALL_FRICTION, DEFAULT_TRACKBALL_FRICTION,
          DEFAULT_TRACKBALL_FRICTION, DEFAULT_TRACKBALL_FRICTION, DEFAULT_TRACKBALL_FRICTION };
        //public bool[] touchStickTrackballMode = new bool[Global.TEST_PROFILE_ITEM_COUNT] { false, false, false, false, false, false, false, false, false };
        //public double[] touchStickTrackballFriction = new double[Global.TEST_PROFILE_ITEM_COUNT] { 10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0 };

        public TouchMouseStickInfo[] touchMStickInfo = new TouchMouseStickInfo[Global.TEST_PROFILE_ITEM_COUNT]
        {
            new TouchMouseStickInfo(),
            new TouchMouseStickInfo(),
            new TouchMouseStickInfo(), new TouchMouseStickInfo(),
            new TouchMouseStickInfo(), new TouchMouseStickInfo(),
            new TouchMouseStickInfo(), new TouchMouseStickInfo(),
            new TouchMouseStickInfo(),
        };

        public TouchpadRelMouseSettings[] touchpadRelMouse = new TouchpadRelMouseSettings[Global.TEST_PROFILE_ITEM_COUNT] { new TouchpadRelMouseSettings(), new TouchpadRelMouseSettings(), new TouchpadRelMouseSettings(), new TouchpadRelMouseSettings(),
            new TouchpadRelMouseSettings(), new TouchpadRelMouseSettings(), new TouchpadRelMouseSettings(), new TouchpadRelMouseSettings(), new TouchpadRelMouseSettings() };

        public TouchButtonActivationMode[] touchpadButtonMode = new TouchButtonActivationMode[Global.TEST_PROFILE_ITEM_COUNT]
        {
            new TouchButtonActivationMode(), new TouchButtonActivationMode(), new TouchButtonActivationMode(), new TouchButtonActivationMode(),
            new TouchButtonActivationMode(), new TouchButtonActivationMode(), new TouchButtonActivationMode(), new TouchButtonActivationMode(), new TouchButtonActivationMode(),
        };

        // Used to hold the controller type desired in a profile
        public OutContType[] outputDevType = new OutContType[Global.TEST_PROFILE_ITEM_COUNT]
        { DEFAULT_OUT_CONT_TYPE, DEFAULT_OUT_CONT_TYPE, DEFAULT_OUT_CONT_TYPE,
          DEFAULT_OUT_CONT_TYPE, DEFAULT_OUT_CONT_TYPE, DEFAULT_OUT_CONT_TYPE,
          DEFAULT_OUT_CONT_TYPE, DEFAULT_OUT_CONT_TYPE, DEFAULT_OUT_CONT_TYPE};

        public const bool DEFAULT_OUTPUT_VIRTUAL_TRIG_BUTTONS = true;
        public bool[] outputVirtualTriggerButtons = new bool[Global.TEST_PROFILE_ITEM_COUNT]
        {
            DEFAULT_OUTPUT_VIRTUAL_TRIG_BUTTONS, DEFAULT_OUTPUT_VIRTUAL_TRIG_BUTTONS, DEFAULT_OUTPUT_VIRTUAL_TRIG_BUTTONS, DEFAULT_OUTPUT_VIRTUAL_TRIG_BUTTONS,
            DEFAULT_OUTPUT_VIRTUAL_TRIG_BUTTONS, DEFAULT_OUTPUT_VIRTUAL_TRIG_BUTTONS, DEFAULT_OUTPUT_VIRTUAL_TRIG_BUTTONS, DEFAULT_OUTPUT_VIRTUAL_TRIG_BUTTONS,
            DEFAULT_OUTPUT_VIRTUAL_TRIG_BUTTONS,
        };

        public const DS4TriggerOutputMode DEFAULT_DS4_TRIGGER_OUTPUT = DS4TriggerOutputMode.Default;
        public DS4TriggerOutputMode[] outputDS4TriggerMode = new DS4TriggerOutputMode[Global.TEST_PROFILE_ITEM_COUNT]
        {
            DEFAULT_DS4_TRIGGER_OUTPUT,DEFAULT_DS4_TRIGGER_OUTPUT,DEFAULT_DS4_TRIGGER_OUTPUT,
            DEFAULT_DS4_TRIGGER_OUTPUT,DEFAULT_DS4_TRIGGER_OUTPUT,DEFAULT_DS4_TRIGGER_OUTPUT,
            DEFAULT_DS4_TRIGGER_OUTPUT,DEFAULT_DS4_TRIGGER_OUTPUT,DEFAULT_DS4_TRIGGER_OUTPUT,
        };


        // TRUE=AutoProfile reverts to default profile if current foreground process is unknown, FALSE=Leave existing profile active when a foreground proces is unknown (ie. no matching auto-profile rule)
        public const bool DEFAULT_AUTO_PROFILE_REVERT_DEFAULT_PROFILE = true;
        public bool autoProfileRevertDefaultProfile = DEFAULT_AUTO_PROFILE_REVERT_DEFAULT_PROFILE;
        public AutoProfileDisplayProfileSwitchChoices autoProfileSwitchNotifyChoice =
            AutoProfileDisplayProfileSwitchChoices.None;

        bool tempBool = false;

        // Phase7-Step5 W4（Q6＝A）: 旧 AppHost.GetService<IProcessInspector>()。ServiceRegistration の BackingStore の登録が設定する。
        // 未設定のとき（コンテナを通らないテストなど）は、LoadProfile が従来のフォールバック（プロセス一覧の走査）を使う。
        // TODO(Phase7-Step10): BackingStore の分割（永続化の切り出し）で、コンストラクター注入にする。
        public DS4Windows.Services.IProcessInspector ProcessInspector { get; set; }

        public BackingStore()
        {
            ds4controlSettings = new ControlSettingsGroup[Global.TEST_PROFILE_ITEM_COUNT];

            for (int i = 0; i < Global.TEST_PROFILE_ITEM_COUNT; i++)
            {
                foreach (DS4Controls dc in Enum.GetValues(typeof(DS4Controls)))
                {
                    if (dc != DS4Controls.None)
                        ds4settings[i].Add(new DS4ControlSettings(dc));
                }

                ds4controlSettings[i] = new ControlSettingsGroup(ds4settings[i]);

                // RumbleSettings の変更を生配列 rumble / rumbleAutostopTime に連動させる
                int slot = i;
                rumbleSettings[slot].RumbleSettingsChanged += (sender, e) =>
                {
                    if (rumble != null && slot < rumble.Length)
                        rumble[slot] = rumbleSettings[slot].RumbleBoost;
                    if (rumbleAutostopTime != null && slot < rumbleAutostopTime.Length)
                        rumbleAutostopTime[slot] = rumbleSettings[slot].RumbleAutostopTime;
                };

                EstablishDefaultSpecialActions(i);
                CacheExtraProfileInfo(i);
            }

            SetupDefaultColors();
        }
        public void EstablishDefaultSpecialActions(int idx)
        {
            // Do not overwrite an existing profileActions list. Only seed a default
            // when the list is missing (null). This ensures we only add the default
            // Disconnect action for truly new/unspecified profiles and won't force-enable
            // it for existing profiles on startup.
            if (profileActions[idx] != null)
            {
                profileActionCount[idx] = profileActions[idx].Count;
                return;
            }

            // Initialize to an empty list for unspecified profiles but do NOT
            // automatically add any Disconnect action. That behavior caused
            // unwanted enabling of Disconnect for profiles that simply had no
            // explicit action configuration.
            profileActions[idx] = new List<string>();
        }

        public void CacheProfileCustomsFlags(int device)
        {
            bool customAct = false;
            containsCustomAction[device] = customAct = HasCustomActions(device);
            containsCustomExtras[device] = HasCustomExtras(device);

            {
                customAct = gyroOutMode[device] == GyroOutMode.MouseJoystick;
                customAct = customAct || lsOutputSettings[device].mode != StickMode.Controls;
                customAct = customAct || rsOutputSettings[device].mode != StickMode.Controls;
                containsCustomAction[device] = customAct;
            }
        }

        private void SetupDefaultColors()
        {
            lightbarSettingInfo[0].ds4winSettings.m_Led = new DS4Color(Color.Blue);
            lightbarSettingInfo[1].ds4winSettings.m_Led = new DS4Color(Color.Red);
            lightbarSettingInfo[2].ds4winSettings.m_Led = new DS4Color(Color.Green);
            lightbarSettingInfo[3].ds4winSettings.m_Led = new DS4Color(Color.Pink);
            lightbarSettingInfo[4].ds4winSettings.m_Led = new DS4Color(Color.Blue);
            lightbarSettingInfo[5].ds4winSettings.m_Led = new DS4Color(Color.Red);
            lightbarSettingInfo[6].ds4winSettings.m_Led = new DS4Color(Color.Green);
            lightbarSettingInfo[7].ds4winSettings.m_Led = new DS4Color(Color.Pink);
            lightbarSettingInfo[8].ds4winSettings.m_Led = new DS4Color(Color.White);
        }

        public void CacheExtraProfileInfo(int device)
        {
            CalculateProfileActionCount(device);
            CalculateProfileActionDicts(device);
            CacheProfileCustomsFlags(device);
        }

        public void CalculateProfileActionCount(int index)
        {
            profileActionCount[index] = profileActions[index].Count;
        }

        public void CalculateProfileActionDicts(int device)
        {
            profileActionDict[device].Clear();
            profileActionIndexDict[device].Clear();

            foreach (string actionname in profileActions[device])
            {
                var actionObj = GetAction(actionname);
                int index = GetActionIndexOf(actionname);
                profileActionDict[device][actionname] = actionObj;
                profileActionIndexDict[device][actionname] = index;
            }
        }

        /// <summary>
        /// Emit "Invalid action index" logs for missing actions in the profile for the given device.
        /// Respects loggedInvalidActions unless forceEmit is true.
        /// </summary>
        /// <param name="device">Device index</param>
        /// <param name="forceEmit">If true, ignore suppression and always emit logs</param>
        /// <param name="overrideProfileName">If provided, use this profile name instead of looking up profilePath[device]</param>
        public void EmitMissingActionLogsForDevice(int device, bool forceEmit = false, string overrideProfileName = null)
        {
            if (profileActions == null || device < 0 || device >= profileActions.Length) return;

            foreach (string actionname in profileActions[device])
            {
                var actionObj = GetAction(actionname);
                int index = GetActionIndexOf(actionname);
                if (actionObj == null || actionObj.name == "null" || index < 0)
                {
                    if (forceEmit || !loggedInvalidActions.Contains(actionname))
                    {
                        string profName;
                        if (overrideProfileName != null)
                        {
                            // Use explicitly provided profile name
                            profName = overrideProfileName;
                        }
                        else
                        {
                            // Determine the profile name actually in use for this device (temporary or regular)
                            profName = (Global.ProfileSettingsServiceInstance.GetUseTempProfile(device))
                                ? Global.ProfileSettingsServiceInstance.GetTempProfileName(device)
                                : (profilePath != null && device >= 0 && device < profilePath.Length ? profilePath[device] : string.Empty);
                        }

                        string displayProfile = string.IsNullOrEmpty(profName) ? "(unknown)" : profName;
                        AppLogger.LogToGui($"Profile '{displayProfile}' contains an invalid special action '{actionname}'.", false);
                        try
                        {
                            loggedInvalidActions.Add(actionname);
                        }
                        catch { }
                    }
                }
            }
        }

        public SpecialAction GetAction(string name)
        {
            //foreach (SpecialAction sA in actions)
            for (int i = 0, actionCount = actions.Count; i < actionCount; i++)
            {
                SpecialAction sA = actions[i];
                var normExisting = Util.NormalizeActionName(sA?.name);
                var normSearch = Util.NormalizeActionName(name);
                if (string.Equals(normExisting, normSearch, StringComparison.OrdinalIgnoreCase))
                    return sA;
            }

            return new SpecialAction("null", "null", "null", "null");
        }

        public int GetActionIndexOf(string name)
        {
            for (int i = 0, actionCount = actions.Count; i < actionCount; i++)
            {
                var normExisting = Util.NormalizeActionName(actions[i]?.name);
                var normSearch = Util.NormalizeActionName(name);
                if (string.Equals(normExisting, normSearch, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        public string stickOutputCurveString(int id)
        {
            string result = "linear";
            switch (id)
            {
                case 0: break;
                case 1: result = "enhanced-precision"; break;
                case 2: result = "quadratic"; break;
                case 3: result = "cubic"; break;
                case 4: result = "easeout-quad"; break;
                case 5: result = "easeout-cubic"; break;
                case 6: result = "custom"; break;
                default: break;
            }

            return result;
        }

        public int stickOutputCurveId(string name)
        {
            int id = 0;
            switch (name)
            {
                case "linear": id = 0; break;
                case "enhanced-precision": id = 1; break;
                case "quadratic": id = 2; break;
                case "cubic": id = 3; break;
                case "easeout-quad": id = 4; break;
                case "easeout-cubic": id = 5; break;
                case "custom": id = 6; break;
                default: break;
            }

            return id;
        }

        private string axisOutputCurveString(int id)
        {
            return stickOutputCurveString(id);
        }

        private int axisOutputCurveId(string name)
        {
            return stickOutputCurveId(name);
        }

        public static bool SaTriggerCondValue(string text)
        {
            bool result = true;
            switch (text)
            {
                case "and": result = true; break;
                case "or": result = false; break;
                default: result = true; break;
            }

            return result;
        }

        public static string SaTriggerCondString(bool value)
        {
            string result = value ? "and" : "or";
            return result;
        }

        public void SetSaTriggerCond(int index, string text)
        {
            sATriggerCond[index] = SaTriggerCondValue(text);
        }

        public void SetSaMouseStickTriggerCond(int index, string text)
        {
            sAMouseStickTriggerCond[index] = SaTriggerCondValue(text);
        }

        public void SetGyroMouseDZ(int index, int value, ControlService control)
        {
            gyroMouseDZ[index] = value;
            if (index < ControlService.CURRENT_DS4_CONTROLLER_LIMIT && control.touchPad[index] != null)
                control.touchPad[index].CursorGyroDead = value;
        }

        public void SetGyroControlsToggle(int index, bool value, ControlService control)
        {
            gyroControlsInf[index].triggerToggle = value;
            if (index < ControlService.CURRENT_DS4_CONTROLLER_LIMIT && control.touchPad[index] != null)
                control.touchPad[index].ToggleGyroControls = value;
        }

        public void SetGyroMouseToggle(int index, bool value, ControlService control)
        {
            gyroMouseToggle[index] = value;
            if (index < ControlService.CURRENT_DS4_CONTROLLER_LIMIT && control.touchPad[index] != null)
                control.touchPad[index].ToggleGyroMouse = value;
        }

        public void SetGyroMouseStickToggle(int index, bool value, ControlService control)
        {
            gyroMouseStickToggle[index] = value;
            if (index < ControlService.CURRENT_DS4_CONTROLLER_LIMIT && control.touchPad[index] != null)
                control.touchPad[index].ToggleGyroStick = value;
        }

        private string OutContDeviceString(OutContType id)
        {
            string result = "X360";
            switch (id)
            {
                case OutContType.None:
                case OutContType.X360: result = "X360"; break;
                case OutContType.DS4: result = "DS4"; break;
                default: break;
            }

            return result;
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
            if (gyroOutMode[device] == GyroOutMode.None)
            {
                gyroOutMode[device] = GyroOutMode.Controls;
            }
        }

        private string GetLightbarModeString(LightbarMode mode)
        {
            string result = "DS4Win";
            switch (mode)
            {
                case LightbarMode.DS4Win:
                    result = "DS4Win";
                    break;
                case LightbarMode.Passthru:
                    result = "Passthru";
                    break;
                default:
                    break;
            }
            return result;
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

        public bool SaveAsProfile(int device, string proName)
        {
            bool Saved = true;
            ResetProfile(device);
            Saved = SaveProfile(device, proName);
            return Saved;
        }

        public bool SaveProfile(int device, string proName)
        {
            bool Saved = true;
            //string path = Global.appdatapath + @"\Profiles\" + Path.GetFileNameWithoutExtension(proName) + ".xml";
            if (proName.EndsWith(Global.XML_EXTENSION))
            {
                proName = proName.Remove(proName.LastIndexOf(Global.XML_EXTENSION));
            }

            string path = $@"{Global.appdatapath}\Profiles\{proName}{Global.XML_EXTENSION}";
            try
            {
                XmlNode tmpNode;
                XmlNode xmlControls = m_Xdoc.SelectSingleNode("/DS4Windows/Control");
                XmlNode xmlShiftControls = m_Xdoc.SelectSingleNode("/DS4Windows/ShiftControl");
                m_Xdoc.RemoveAll();

                tmpNode = m_Xdoc.CreateXmlDeclaration("1.0", "utf-8", string.Empty);
                m_Xdoc.AppendChild(tmpNode);

                tmpNode = m_Xdoc.CreateComment(string.Format(" DS4Windows Configuration Data. {0} ", DateTime.Now));
                m_Xdoc.AppendChild(tmpNode);

                tmpNode = m_Xdoc.CreateComment(string.Format(" Made with DS4Windows version {0} ", Global.exeversion));
                m_Xdoc.AppendChild(tmpNode);

                tmpNode = m_Xdoc.CreateWhitespace("\r\n");
                m_Xdoc.AppendChild(tmpNode);

                XmlElement rootElement = m_Xdoc.CreateElement("DS4Windows", null);
                rootElement.SetAttribute("app_version", Global.exeversion);
                rootElement.SetAttribute("config_version", Global.CONFIG_VERSION.ToString());

                LightbarSettingInfo lightbarSettings = lightbarSettingInfo[device];
                LightbarDS4WinInfo lightInfo = lightbarSettings.ds4winSettings;

                XmlNode xmlTouchToggle = m_Xdoc.CreateNode(XmlNodeType.Element, "touchToggle", null); xmlTouchToggle.InnerText = enableTouchToggle[device].ToString(); rootElement.AppendChild(xmlTouchToggle);
                XmlNode xmlIdleDisconnectTimeout = m_Xdoc.CreateNode(XmlNodeType.Element, "idleDisconnectTimeout", null); xmlIdleDisconnectTimeout.InnerText = idleDisconnectTimeout[device].ToString(); rootElement.AppendChild(xmlIdleDisconnectTimeout);
                XmlNode xmlOutputDataToDS4 = m_Xdoc.CreateNode(XmlNodeType.Element, "outputDataToDS4", null); xmlOutputDataToDS4.InnerText = enableOutputDataToDS4[device].ToString(); rootElement.AppendChild(xmlOutputDataToDS4);
                XmlNode xmlColor = m_Xdoc.CreateNode(XmlNodeType.Element, "Color", null);
                xmlColor.InnerText = lightInfo.m_Led.red.ToString() + "," + lightInfo.m_Led.green.ToString() + "," + lightInfo.m_Led.blue.ToString();
                rootElement.AppendChild(xmlColor);
                XmlNode xmlRumbleBoost = m_Xdoc.CreateNode(XmlNodeType.Element, "RumbleBoost", null); xmlRumbleBoost.InnerText = rumble[device].ToString(); rootElement.AppendChild(xmlRumbleBoost);
                XmlNode xmlRumbleAutostopTime = m_Xdoc.CreateNode(XmlNodeType.Element, "RumbleAutostopTime", null); xmlRumbleAutostopTime.InnerText = rumbleAutostopTime[device].ToString(); rootElement.AppendChild(xmlRumbleAutostopTime);
                XmlNode xmlLightbarMode = m_Xdoc.CreateNode(XmlNodeType.Element, "LightbarMode", null); xmlLightbarMode.InnerText = GetLightbarModeString(lightbarSettings.mode); rootElement.AppendChild(xmlLightbarMode);
                XmlNode xmlLedAsBatteryIndicator = m_Xdoc.CreateNode(XmlNodeType.Element, "ledAsBatteryIndicator", null); xmlLedAsBatteryIndicator.InnerText = lightInfo.ledAsBattery.ToString(); rootElement.AppendChild(xmlLedAsBatteryIndicator);
                XmlNode xmlLowBatteryFlash = m_Xdoc.CreateNode(XmlNodeType.Element, "FlashType", null); xmlLowBatteryFlash.InnerText = lightInfo.flashType.ToString(); rootElement.AppendChild(xmlLowBatteryFlash);
                XmlNode xmlFlashBatterAt = m_Xdoc.CreateNode(XmlNodeType.Element, "flashBatteryAt", null); xmlFlashBatterAt.InnerText = lightInfo.flashAt.ToString(); rootElement.AppendChild(xmlFlashBatterAt);
                XmlNode xmlTouchSensitivity = m_Xdoc.CreateNode(XmlNodeType.Element, "touchSensitivity", null); xmlTouchSensitivity.InnerText = touchSensitivity[device].ToString(); rootElement.AppendChild(xmlTouchSensitivity);
                XmlNode xmlLowColor = m_Xdoc.CreateNode(XmlNodeType.Element, "LowColor", null);
                xmlLowColor.InnerText = lightInfo.m_LowLed.red.ToString() + "," + lightInfo.m_LowLed.green.ToString() + "," + lightInfo.m_LowLed.blue.ToString();
                rootElement.AppendChild(xmlLowColor);
                XmlNode xmlChargingColor = m_Xdoc.CreateNode(XmlNodeType.Element, "ChargingColor", null);
                xmlChargingColor.InnerText = lightInfo.m_ChargingLed.red.ToString() + "," + lightInfo.m_ChargingLed.green.ToString() + "," + lightInfo.m_ChargingLed.blue.ToString();
                rootElement.AppendChild(xmlChargingColor);
                XmlNode xmlFlashColor = m_Xdoc.CreateNode(XmlNodeType.Element, "FlashColor", null);
                xmlFlashColor.InnerText = lightInfo.m_FlashLed.red.ToString() + "," + lightInfo.m_FlashLed.green.ToString() + "," + lightInfo.m_FlashLed.blue.ToString();
                rootElement.AppendChild(xmlFlashColor);
                XmlNode xmlTouchpadJitterCompensation = m_Xdoc.CreateNode(XmlNodeType.Element, "touchpadJitterCompensation", null); xmlTouchpadJitterCompensation.InnerText = touchpadJitterCompensation[device].ToString(); rootElement.AppendChild(xmlTouchpadJitterCompensation);
                // Phase6-Step12: 旧形式（3.9.9 互換）の保存で欠けていた 10 要素（要素名は ProfileDTO と同じ。旧版は未知の要素を無視する）
                SaveLegacyExtraSettings(rootElement, device);
                XmlNode xmlLowerRCOn = m_Xdoc.CreateNode(XmlNodeType.Element, "lowerRCOn", null); xmlLowerRCOn.InnerText = lowerRCOn[device].ToString(); rootElement.AppendChild(xmlLowerRCOn);
                XmlNode xmlTapSensitivity = m_Xdoc.CreateNode(XmlNodeType.Element, "tapSensitivity", null); xmlTapSensitivity.InnerText = tapSensitivity[device].ToString(); rootElement.AppendChild(xmlTapSensitivity);
                XmlNode xmlDouble = m_Xdoc.CreateNode(XmlNodeType.Element, "doubleTap", null); xmlDouble.InnerText = doubleTap[device].ToString(); rootElement.AppendChild(xmlDouble);
                XmlNode xmlScrollSensitivity = m_Xdoc.CreateNode(XmlNodeType.Element, "scrollSensitivity", null); xmlScrollSensitivity.InnerText = scrollSensitivity[device].ToString(); rootElement.AppendChild(xmlScrollSensitivity);
                XmlNode xmlLeftTriggerMiddle = m_Xdoc.CreateNode(XmlNodeType.Element, "LeftTriggerMiddle", null); xmlLeftTriggerMiddle.InnerText = l2ModInfo[device].deadZone.ToString(); rootElement.AppendChild(xmlLeftTriggerMiddle);
                XmlNode xmlRightTriggerMiddle = m_Xdoc.CreateNode(XmlNodeType.Element, "RightTriggerMiddle", null); xmlRightTriggerMiddle.InnerText = r2ModInfo[device].deadZone.ToString(); rootElement.AppendChild(xmlRightTriggerMiddle);
                XmlNode xmlTouchpadInvert = m_Xdoc.CreateNode(XmlNodeType.Element, "TouchpadInvert", null); xmlTouchpadInvert.InnerText = touchpadInvert[device].ToString(); rootElement.AppendChild(xmlTouchpadInvert);
                XmlNode xmlTouchClickPasthru = m_Xdoc.CreateNode(XmlNodeType.Element, "TouchpadClickPassthru", null); xmlTouchClickPasthru.InnerText = touchClickPassthru[device].ToString(); rootElement.AppendChild(xmlTouchClickPasthru);

                XmlNode xmlL2AD = m_Xdoc.CreateNode(XmlNodeType.Element, "L2AntiDeadZone", null); xmlL2AD.InnerText = l2ModInfo[device].antiDeadZone.ToString(); rootElement.AppendChild(xmlL2AD);
                XmlNode xmlR2AD = m_Xdoc.CreateNode(XmlNodeType.Element, "R2AntiDeadZone", null); xmlR2AD.InnerText = r2ModInfo[device].antiDeadZone.ToString(); rootElement.AppendChild(xmlR2AD);
                XmlNode xmlL2Maxzone = m_Xdoc.CreateNode(XmlNodeType.Element, "L2MaxZone", null); xmlL2Maxzone.InnerText = l2ModInfo[device].maxZone.ToString(); rootElement.AppendChild(xmlL2Maxzone);
                XmlNode xmlR2Maxzone = m_Xdoc.CreateNode(XmlNodeType.Element, "R2MaxZone", null); xmlR2Maxzone.InnerText = r2ModInfo[device].maxZone.ToString(); rootElement.AppendChild(xmlR2Maxzone);
                XmlNode xmlL2MaxOutput = m_Xdoc.CreateNode(XmlNodeType.Element, "L2MaxOutput", null); xmlL2MaxOutput.InnerText = l2ModInfo[device].maxOutput.ToString(); rootElement.AppendChild(xmlL2MaxOutput);
                XmlNode xmlR2MaxOutput = m_Xdoc.CreateNode(XmlNodeType.Element, "R2MaxOutput", null); xmlR2MaxOutput.InnerText = r2ModInfo[device].maxOutput.ToString(); rootElement.AppendChild(xmlR2MaxOutput);
                XmlNode xmlButtonMouseSensitivity = m_Xdoc.CreateNode(XmlNodeType.Element, "ButtonMouseSensitivity", null); xmlButtonMouseSensitivity.InnerText = buttonMouseInfos[device].buttonSensitivity.ToString(); rootElement.AppendChild(xmlButtonMouseSensitivity);
                XmlNode xmlButtonMouseOffset = m_Xdoc.CreateNode(XmlNodeType.Element, "ButtonMouseOffset", null); xmlButtonMouseOffset.InnerText = buttonMouseInfos[device].mouseVelocityOffset.ToString(); rootElement.AppendChild(xmlButtonMouseOffset);
                XmlNode xmlRainbow = m_Xdoc.CreateNode(XmlNodeType.Element, "Rainbow", null); xmlRainbow.InnerText = lightInfo.rainbow.ToString(); rootElement.AppendChild(xmlRainbow);
                XmlNode xmlMaxSatRainbow = m_Xdoc.CreateNode(XmlNodeType.Element, "MaxSatRainbow", null); xmlMaxSatRainbow.InnerText = Convert.ToInt32(lightInfo.maxRainbowSat * 100.0).ToString(); rootElement.AppendChild(xmlMaxSatRainbow);
                XmlNode xmlLSD = m_Xdoc.CreateNode(XmlNodeType.Element, "LSDeadZone", null); xmlLSD.InnerText = lsModInfo[device].deadZone.ToString(); rootElement.AppendChild(xmlLSD);
                XmlNode xmlRSD = m_Xdoc.CreateNode(XmlNodeType.Element, "RSDeadZone", null); xmlRSD.InnerText = rsModInfo[device].deadZone.ToString(); rootElement.AppendChild(xmlRSD);
                XmlNode xmlLSAD = m_Xdoc.CreateNode(XmlNodeType.Element, "LSAntiDeadZone", null); xmlLSAD.InnerText = lsModInfo[device].antiDeadZone.ToString(); rootElement.AppendChild(xmlLSAD);
                XmlNode xmlRSAD = m_Xdoc.CreateNode(XmlNodeType.Element, "RSAntiDeadZone", null); xmlRSAD.InnerText = rsModInfo[device].antiDeadZone.ToString(); rootElement.AppendChild(xmlRSAD);
                XmlNode xmlLSMaxZone = m_Xdoc.CreateNode(XmlNodeType.Element, "LSMaxZone", null); xmlLSMaxZone.InnerText = lsModInfo[device].maxZone.ToString(); rootElement.AppendChild(xmlLSMaxZone);
                XmlNode xmlRSMaxZone = m_Xdoc.CreateNode(XmlNodeType.Element, "RSMaxZone", null); xmlRSMaxZone.InnerText = rsModInfo[device].maxZone.ToString(); rootElement.AppendChild(xmlRSMaxZone);
                XmlNode xmlLSVerticalScale = m_Xdoc.CreateNode(XmlNodeType.Element, "LSVerticalScale", null); xmlLSVerticalScale.InnerText = lsModInfo[device].verticalScale.ToString(); rootElement.AppendChild(xmlLSVerticalScale);
                XmlNode xmlRSVerticalScale = m_Xdoc.CreateNode(XmlNodeType.Element, "RSVerticalScale", null); xmlRSVerticalScale.InnerText = rsModInfo[device].verticalScale.ToString(); rootElement.AppendChild(xmlRSVerticalScale);
                XmlNode xmlLSMaxOutput = m_Xdoc.CreateNode(XmlNodeType.Element, "LSMaxOutput", null); xmlLSMaxOutput.InnerText = lsModInfo[device].maxOutput.ToString(); rootElement.AppendChild(xmlLSMaxOutput);
                XmlNode xmlRSMaxOutput = m_Xdoc.CreateNode(XmlNodeType.Element, "RSMaxOutput", null); xmlRSMaxOutput.InnerText = rsModInfo[device].maxOutput.ToString(); rootElement.AppendChild(xmlRSMaxOutput);
                XmlNode xmlLSMaxOutputForce = m_Xdoc.CreateNode(XmlNodeType.Element, "LSMaxOutputForce", null); xmlLSMaxOutputForce.InnerText = lsModInfo[device].maxOutputForce.ToString(); rootElement.AppendChild(xmlLSMaxOutputForce);
                XmlNode xmlRSMaxOutputForce = m_Xdoc.CreateNode(XmlNodeType.Element, "RSMaxOutputForce", null); xmlRSMaxOutputForce.InnerText = rsModInfo[device].maxOutputForce.ToString(); rootElement.AppendChild(xmlRSMaxOutputForce);
                XmlNode xmlLSDeadZoneType = m_Xdoc.CreateNode(XmlNodeType.Element, "LSDeadZoneType", null); xmlLSDeadZoneType.InnerText = lsModInfo[device].deadzoneType.ToString(); rootElement.AppendChild(xmlLSDeadZoneType);
                XmlNode xmlRSDeadZoneType = m_Xdoc.CreateNode(XmlNodeType.Element, "RSDeadZoneType", null); xmlRSDeadZoneType.InnerText = rsModInfo[device].deadzoneType.ToString(); rootElement.AppendChild(xmlRSDeadZoneType);

                XmlElement xmlLSAxialDeadGroupEl = m_Xdoc.CreateElement("LSAxialDeadOptions");
                XmlElement xmlLSAxialDeadX = m_Xdoc.CreateElement("DeadZoneX"); xmlLSAxialDeadX.InnerText = lsModInfo[device].xAxisDeadInfo.deadZone.ToString(); xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialDeadX);
                XmlElement xmlLSAxialDeadY = m_Xdoc.CreateElement("DeadZoneY"); xmlLSAxialDeadY.InnerText = lsModInfo[device].yAxisDeadInfo.deadZone.ToString(); xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialDeadY);
                XmlElement xmlLSAxialMaxX = m_Xdoc.CreateElement("MaxZoneX"); xmlLSAxialMaxX.InnerText = lsModInfo[device].xAxisDeadInfo.maxZone.ToString(); xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialMaxX);
                XmlElement xmlLSAxialMaxY = m_Xdoc.CreateElement("MaxZoneY"); xmlLSAxialMaxY.InnerText = lsModInfo[device].yAxisDeadInfo.maxZone.ToString(); xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialMaxY);
                XmlElement xmlLSAxialAntiDeadX = m_Xdoc.CreateElement("AntiDeadZoneX"); xmlLSAxialAntiDeadX.InnerText = lsModInfo[device].xAxisDeadInfo.antiDeadZone.ToString(); xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialAntiDeadX);
                XmlElement xmlLSAxialAntiDeadY = m_Xdoc.CreateElement("AntiDeadZoneY"); xmlLSAxialAntiDeadY.InnerText = lsModInfo[device].yAxisDeadInfo.antiDeadZone.ToString(); xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialAntiDeadY);
                XmlElement xmlLSAxialMaxOutputX = m_Xdoc.CreateElement("MaxOutputX"); xmlLSAxialMaxOutputX.InnerText = lsModInfo[device].xAxisDeadInfo.maxOutput.ToString(); xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialMaxOutputX);
                XmlElement xmlLSAxialMaxOutputY = m_Xdoc.CreateElement("MaxOutputY"); xmlLSAxialMaxOutputY.InnerText = lsModInfo[device].yAxisDeadInfo.maxOutput.ToString(); xmlLSAxialDeadGroupEl.AppendChild(xmlLSAxialMaxOutputY);
                rootElement.AppendChild(xmlLSAxialDeadGroupEl);

                XmlElement xmlRSAxialDeadGroupEl = m_Xdoc.CreateElement("RSAxialDeadOptions");
                XmlElement xmlRSAxialDeadX = m_Xdoc.CreateElement("DeadZoneX"); xmlRSAxialDeadX.InnerText = rsModInfo[device].xAxisDeadInfo.deadZone.ToString(); xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialDeadX);
                XmlElement xmlRSAxialDeadY = m_Xdoc.CreateElement("DeadZoneY"); xmlRSAxialDeadY.InnerText = rsModInfo[device].yAxisDeadInfo.deadZone.ToString(); xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialDeadY);
                XmlElement xmlRSAxialMaxX = m_Xdoc.CreateElement("MaxZoneX"); xmlRSAxialMaxX.InnerText = rsModInfo[device].xAxisDeadInfo.maxZone.ToString(); xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialMaxX);
                XmlElement xmlRSAxialMaxY = m_Xdoc.CreateElement("MaxZoneY"); xmlRSAxialMaxY.InnerText = rsModInfo[device].yAxisDeadInfo.maxZone.ToString(); xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialMaxY);
                XmlElement xmlRSAxialAntiDeadX = m_Xdoc.CreateElement("AntiDeadZoneX"); xmlRSAxialAntiDeadX.InnerText = rsModInfo[device].xAxisDeadInfo.antiDeadZone.ToString(); xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialAntiDeadX);
                XmlElement xmlRSAxialAntiDeadY = m_Xdoc.CreateElement("AntiDeadZoneY"); xmlRSAxialAntiDeadY.InnerText = rsModInfo[device].yAxisDeadInfo.antiDeadZone.ToString(); xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialAntiDeadY);
                XmlElement xmlRSAxialMaxOutputX = m_Xdoc.CreateElement("MaxOutputX"); xmlRSAxialMaxOutputX.InnerText = rsModInfo[device].xAxisDeadInfo.maxOutput.ToString(); xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialMaxOutputX);
                XmlElement xmlRSAxialMaxOutputY = m_Xdoc.CreateElement("MaxOutputY"); xmlRSAxialMaxOutputY.InnerText = rsModInfo[device].yAxisDeadInfo.maxOutput.ToString(); xmlRSAxialDeadGroupEl.AppendChild(xmlRSAxialMaxOutputY);
                rootElement.AppendChild(xmlRSAxialDeadGroupEl);

                // Output rotation values to profile in degrees
                XmlNode xmlLSRotation = m_Xdoc.CreateNode(XmlNodeType.Element, "LSRotation", null); xmlLSRotation.InnerText = Convert.ToInt32(LSRotation[device] * 180.0 / Math.PI).ToString(); rootElement.AppendChild(xmlLSRotation);
                XmlNode xmlRSRotation = m_Xdoc.CreateNode(XmlNodeType.Element, "RSRotation", null); xmlRSRotation.InnerText = Convert.ToInt32(RSRotation[device] * 180.0 / Math.PI).ToString(); rootElement.AppendChild(xmlRSRotation);

                XmlNode xmlLSFuzz = m_Xdoc.CreateNode(XmlNodeType.Element, "LSFuzz", null); xmlLSFuzz.InnerText = lsModInfo[device].fuzz.ToString(); rootElement.AppendChild(xmlLSFuzz);
                XmlNode xmlRSFuzz = m_Xdoc.CreateNode(XmlNodeType.Element, "RSFuzz", null); xmlRSFuzz.InnerText = rsModInfo[device].fuzz.ToString(); rootElement.AppendChild(xmlRSFuzz);
                XmlNode xmlLSOuterBindDead = m_Xdoc.CreateNode(XmlNodeType.Element, "LSOuterBindDead", null); xmlLSOuterBindDead.InnerText = Convert.ToInt32(lsModInfo[device].outerBindDeadZone).ToString(); rootElement.AppendChild(xmlLSOuterBindDead);
                XmlNode xmlRSOuterBindDead = m_Xdoc.CreateNode(XmlNodeType.Element, "RSOuterBindDead", null); xmlRSOuterBindDead.InnerText = Convert.ToInt32(rsModInfo[device].outerBindDeadZone).ToString(); rootElement.AppendChild(xmlRSOuterBindDead);
                XmlNode xmlLSOuterBindInvert = m_Xdoc.CreateNode(XmlNodeType.Element, "LSOuterBindInvert", null); xmlLSOuterBindInvert.InnerText = lsModInfo[device].outerBindInvert.ToString(); rootElement.AppendChild(xmlLSOuterBindInvert);
                XmlNode xmlRSOuterBindInvert = m_Xdoc.CreateNode(XmlNodeType.Element, "RSOuterBindInvert", null); xmlRSOuterBindInvert.InnerText = rsModInfo[device].outerBindInvert.ToString(); rootElement.AppendChild(xmlRSOuterBindInvert);

                XmlElement xmlLSDeltaAccelGroupEl = m_Xdoc.CreateElement("LSDeltaAccelSettings");
                XmlElement xmlLSDeltaEnabled = m_Xdoc.CreateElement("Enabled"); xmlLSDeltaEnabled.InnerText = lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.enabled.ToString(); xmlLSDeltaAccelGroupEl.AppendChild(xmlLSDeltaEnabled);
                XmlElement xmlLSDeltaMultiplier = m_Xdoc.CreateElement("Multiplier"); xmlLSDeltaMultiplier.InnerText = lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.multiplier.ToString(); xmlLSDeltaAccelGroupEl.AppendChild(xmlLSDeltaMultiplier);
                XmlElement xmlLSDeltaMaxTravel = m_Xdoc.CreateElement("MaxTravel"); xmlLSDeltaMaxTravel.InnerText = lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.maxTravel.ToString(); xmlLSDeltaAccelGroupEl.AppendChild(xmlLSDeltaMaxTravel);
                XmlElement xmlLSDeltaMinTravel = m_Xdoc.CreateElement("MinTravel"); xmlLSDeltaMinTravel.InnerText = lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.minTravel.ToString(); xmlLSDeltaAccelGroupEl.AppendChild(xmlLSDeltaMinTravel);
                XmlElement xmlLSDeltaEasingDuration = m_Xdoc.CreateElement("EasingDuration"); xmlLSDeltaEasingDuration.InnerText = lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.easingDuration.ToString(); xmlLSDeltaAccelGroupEl.AppendChild(xmlLSDeltaEasingDuration);
                XmlElement xmlLSDeltaMinFactor = m_Xdoc.CreateElement("MinFactor"); xmlLSDeltaMinFactor.InnerText = lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.minfactor.ToString(); xmlLSDeltaAccelGroupEl.AppendChild(xmlLSDeltaMinFactor);
                rootElement.AppendChild(xmlLSDeltaAccelGroupEl);

                XmlElement xmlRSDeltaAccelGroupEl = m_Xdoc.CreateElement("RSDeltaAccelSettings");
                XmlElement xmlRSDeltaEnabled = m_Xdoc.CreateElement("Enabled"); xmlRSDeltaEnabled.InnerText = rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.enabled.ToString(); xmlRSDeltaAccelGroupEl.AppendChild(xmlRSDeltaEnabled);
                XmlElement xmlRSDeltaMultiplier = m_Xdoc.CreateElement("Multiplier"); xmlRSDeltaMultiplier.InnerText = rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.multiplier.ToString(); xmlRSDeltaAccelGroupEl.AppendChild(xmlRSDeltaMultiplier);
                XmlElement xmlRSDeltaMaxTravel = m_Xdoc.CreateElement("MaxTravel"); xmlRSDeltaMaxTravel.InnerText = rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.maxTravel.ToString(); xmlRSDeltaAccelGroupEl.AppendChild(xmlRSDeltaMaxTravel);
                XmlElement xmlRSDeltaMinTravel = m_Xdoc.CreateElement("MinTravel"); xmlRSDeltaMinTravel.InnerText = rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.minTravel.ToString(); xmlRSDeltaAccelGroupEl.AppendChild(xmlRSDeltaMinTravel);
                XmlElement xmlRSDeltaEasingDuration = m_Xdoc.CreateElement("EasingDuration"); xmlRSDeltaEasingDuration.InnerText = rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.easingDuration.ToString(); xmlRSDeltaAccelGroupEl.AppendChild(xmlRSDeltaEasingDuration);
                XmlElement xmlRSDeltaMinFactor = m_Xdoc.CreateElement("MinFactor"); xmlRSDeltaMinFactor.InnerText = rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.minfactor.ToString(); xmlRSDeltaAccelGroupEl.AppendChild(xmlRSDeltaMinFactor);
                rootElement.AppendChild(xmlRSDeltaAccelGroupEl);

                XmlNode xmlSXD = m_Xdoc.CreateNode(XmlNodeType.Element, "SXDeadZone", null); xmlSXD.InnerText = SXDeadzone[device].ToString(); rootElement.AppendChild(xmlSXD);
                XmlNode xmlSZD = m_Xdoc.CreateNode(XmlNodeType.Element, "SZDeadZone", null); xmlSZD.InnerText = SZDeadzone[device].ToString(); rootElement.AppendChild(xmlSZD);

                XmlNode xmlSXMaxzone = m_Xdoc.CreateNode(XmlNodeType.Element, "SXMaxZone", null); xmlSXMaxzone.InnerText = Convert.ToInt32(SXMaxzone[device] * 100.0).ToString(); rootElement.AppendChild(xmlSXMaxzone);
                XmlNode xmlSZMaxzone = m_Xdoc.CreateNode(XmlNodeType.Element, "SZMaxZone", null); xmlSZMaxzone.InnerText = Convert.ToInt32(SZMaxzone[device] * 100.0).ToString(); rootElement.AppendChild(xmlSZMaxzone);

                XmlNode xmlSXAntiDeadzone = m_Xdoc.CreateNode(XmlNodeType.Element, "SXAntiDeadZone", null); xmlSXAntiDeadzone.InnerText = Convert.ToInt32(SXAntiDeadzone[device] * 100.0).ToString(); rootElement.AppendChild(xmlSXAntiDeadzone);
                XmlNode xmlSZAntiDeadzone = m_Xdoc.CreateNode(XmlNodeType.Element, "SZAntiDeadZone", null); xmlSZAntiDeadzone.InnerText = Convert.ToInt32(SZAntiDeadzone[device] * 100.0).ToString(); rootElement.AppendChild(xmlSZAntiDeadzone);

                XmlNode xmlSens = m_Xdoc.CreateNode(XmlNodeType.Element, "Sensitivity", null);
                xmlSens.InnerText = $"{LSSens[device]}|{RSSens[device]}|{l2Sens[device]}|{r2Sens[device]}|{SXSens[device]}|{SZSens[device]}";
                rootElement.AppendChild(xmlSens);

                XmlNode xmlChargingType = m_Xdoc.CreateNode(XmlNodeType.Element, "ChargingType", null); xmlChargingType.InnerText = lightInfo.chargingType.ToString(); rootElement.AppendChild(xmlChargingType);
                XmlNode xmlMouseAccel = m_Xdoc.CreateNode(XmlNodeType.Element, "MouseAcceleration", null); xmlMouseAccel.InnerText = buttonMouseInfos[device].mouseAccel.ToString(); rootElement.AppendChild(xmlMouseAccel);
                XmlNode xmlMouseVerticalScale = m_Xdoc.CreateNode(XmlNodeType.Element, "ButtonMouseVerticalScale", null); xmlMouseVerticalScale.InnerText = Convert.ToInt32(buttonMouseInfos[device].buttonVerticalScale * 100).ToString(); rootElement.AppendChild(xmlMouseVerticalScale);
                //XmlNode xmlShiftMod = m_Xdoc.CreateNode(XmlNodeType.Element, "ShiftModifier", null); xmlShiftMod.InnerText = shiftModifier[device].ToString(); rootElement.AppendChild(xmlShiftMod);
                XmlNode xmlLaunchProgram = m_Xdoc.CreateNode(XmlNodeType.Element, "LaunchProgram", null); xmlLaunchProgram.InnerText = launchProgram[device].ToString(); rootElement.AppendChild(xmlLaunchProgram);
                XmlNode xmlDinput = m_Xdoc.CreateNode(XmlNodeType.Element, "DinputOnly", null); xmlDinput.InnerText = dinputOnly[device].ToString(); rootElement.AppendChild(xmlDinput);
                XmlNode xmlStartTouchpadOff = m_Xdoc.CreateNode(XmlNodeType.Element, "StartTouchpadOff", null); xmlStartTouchpadOff.InnerText = startTouchpadOff[device].ToString(); rootElement.AppendChild(xmlStartTouchpadOff);
                XmlNode xmlTouchOutMode = m_Xdoc.CreateNode(XmlNodeType.Element, "TouchpadOutputMode", null); xmlTouchOutMode.InnerText = touchOutMode[device].ToString(); rootElement.AppendChild(xmlTouchOutMode);
                XmlNode xmlSATriggers = m_Xdoc.CreateNode(XmlNodeType.Element, "SATriggers", null); xmlSATriggers.InnerText = sATriggers[device].ToString(); rootElement.AppendChild(xmlSATriggers);
                XmlNode xmlSATriggerCond = m_Xdoc.CreateNode(XmlNodeType.Element, "SATriggerCond", null); xmlSATriggerCond.InnerText = SaTriggerCondString(sATriggerCond[device]); rootElement.AppendChild(xmlSATriggerCond);
                XmlNode xmlSASteeringWheelEmulationAxis = m_Xdoc.CreateNode(XmlNodeType.Element, "SASteeringWheelEmulationAxis", null); xmlSASteeringWheelEmulationAxis.InnerText = sASteeringWheelEmulationAxis[device].ToString("G"); rootElement.AppendChild(xmlSASteeringWheelEmulationAxis);
                XmlNode xmlSASteeringWheelEmulationRange = m_Xdoc.CreateNode(XmlNodeType.Element, "SASteeringWheelEmulationRange", null); xmlSASteeringWheelEmulationRange.InnerText = sASteeringWheelEmulationRange[device].ToString(); rootElement.AppendChild(xmlSASteeringWheelEmulationRange);
                XmlNode xmlSASteeringWheelFuzz = m_Xdoc.CreateNode(XmlNodeType.Element, "SASteeringWheelFuzz", null); xmlSASteeringWheelFuzz.InnerText = saWheelFuzzValues[device].ToString(); rootElement.AppendChild(xmlSASteeringWheelFuzz);

                XmlElement xmlSASteeringWheelSmoothingGroupEl = m_Xdoc.CreateElement("SASteeringWheelSmoothingOptions");
                XmlElement xmlSASteeringWheelUseSmoothing = m_Xdoc.CreateElement("SASteeringWheelUseSmoothing"); xmlSASteeringWheelUseSmoothing.InnerText = wheelSmoothInfo[device].Enabled.ToString(); xmlSASteeringWheelSmoothingGroupEl.AppendChild(xmlSASteeringWheelUseSmoothing);
                XmlElement xmlSASteeringWheelSmoothMinCutoff = m_Xdoc.CreateElement("SASteeringWheelSmoothMinCutoff"); xmlSASteeringWheelSmoothMinCutoff.InnerText = wheelSmoothInfo[device].MinCutoff.ToString(); xmlSASteeringWheelSmoothingGroupEl.AppendChild(xmlSASteeringWheelSmoothMinCutoff);
                XmlElement xmlSASteeringWheelSmoothBeta = m_Xdoc.CreateElement("SASteeringWheelSmoothBeta"); xmlSASteeringWheelSmoothBeta.InnerText = wheelSmoothInfo[device].Beta.ToString(); xmlSASteeringWheelSmoothingGroupEl.AppendChild(xmlSASteeringWheelSmoothBeta);
                rootElement.AppendChild(xmlSASteeringWheelSmoothingGroupEl);

                //XmlNode xmlSASteeringWheelUseSmoothing = m_Xdoc.CreateNode(XmlNodeType.Element, "SASteeringWheelUseSmoothing", null); xmlSASteeringWheelUseSmoothing.InnerText = wheelSmoothInfo[device].Enabled.ToString(); rootElement.AppendChild(xmlSASteeringWheelUseSmoothing);
                //XmlNode xmlSASteeringWheelSmoothMinCutoff = m_Xdoc.CreateNode(XmlNodeType.Element, "SASteeringWheelSmoothMinCutoff", null); xmlSASteeringWheelSmoothMinCutoff.InnerText = wheelSmoothInfo[device].MinCutoff.ToString(); rootElement.AppendChild(xmlSASteeringWheelSmoothMinCutoff);
                //XmlNode xmlSASteeringWheelSmoothBeta = m_Xdoc.CreateNode(XmlNodeType.Element, "SASteeringWheelSmoothBeta", null); xmlSASteeringWheelSmoothBeta.InnerText = wheelSmoothInfo[device].Beta.ToString(); rootElement.AppendChild(xmlSASteeringWheelSmoothBeta);

                XmlNode xmlTouchDisInvTriggers = m_Xdoc.CreateNode(XmlNodeType.Element, "TouchDisInvTriggers", null);
                string tempTouchDisInv = string.Join(",", touchDisInvertTriggers[device]);
                xmlTouchDisInvTriggers.InnerText = tempTouchDisInv;
                rootElement.AppendChild(xmlTouchDisInvTriggers);

                XmlNode xmlGyroSensitivity = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroSensitivity", null); xmlGyroSensitivity.InnerText = gyroSensitivity[device].ToString(); rootElement.AppendChild(xmlGyroSensitivity);
                XmlNode xmlGyroSensVerticalScale = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroSensVerticalScale", null); xmlGyroSensVerticalScale.InnerText = gyroSensVerticalScale[device].ToString(); rootElement.AppendChild(xmlGyroSensVerticalScale);
                XmlNode xmlGyroInvert = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroInvert", null); xmlGyroInvert.InnerText = gyroInvert[device].ToString(); rootElement.AppendChild(xmlGyroInvert);
                XmlNode xmlGyroTriggerTurns = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroTriggerTurns", null); xmlGyroTriggerTurns.InnerText = gyroTriggerTurns[device].ToString(); rootElement.AppendChild(xmlGyroTriggerTurns);
                /*XmlNode xmlGyroSmoothWeight = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroSmoothingWeight", null); xmlGyroSmoothWeight.InnerText = Convert.ToInt32(gyroSmoothWeight[device] * 100).ToString(); rootElement.AppendChild(xmlGyroSmoothWeight);
                XmlNode xmlGyroSmoothing = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroSmoothing", null); xmlGyroSmoothing.InnerText = gyroSmoothing[device].ToString(); rootElement.AppendChild(xmlGyroSmoothing);
                */

                XmlElement xmlGyroControlsSettingsElement = m_Xdoc.CreateElement("GyroControlsSettings");
                XmlNode xmlGyroControlsTriggers = m_Xdoc.CreateNode(XmlNodeType.Element, "Triggers", null); xmlGyroControlsTriggers.InnerText = gyroControlsInf[device].triggers; xmlGyroControlsSettingsElement.AppendChild(xmlGyroControlsTriggers);
                XmlNode xmlGyroControlsTriggerCond = m_Xdoc.CreateNode(XmlNodeType.Element, "TriggerCond", null); xmlGyroControlsTriggerCond.InnerText = SaTriggerCondString(gyroControlsInf[device].triggerCond); xmlGyroControlsSettingsElement.AppendChild(xmlGyroControlsTriggerCond);
                XmlNode xmlGyroControlsTriggerTurns = m_Xdoc.CreateNode(XmlNodeType.Element, "TriggerTurns", null); xmlGyroControlsTriggerTurns.InnerText = gyroControlsInf[device].triggerTurns.ToString(); xmlGyroControlsSettingsElement.AppendChild(xmlGyroControlsTriggerTurns);
                XmlNode xmlGyroControlsToggle = m_Xdoc.CreateNode(XmlNodeType.Element, "Toggle", null); xmlGyroControlsToggle.InnerText = gyroControlsInf[device].triggerToggle.ToString(); xmlGyroControlsSettingsElement.AppendChild(xmlGyroControlsToggle);
                rootElement.AppendChild(xmlGyroControlsSettingsElement);

                XmlElement xmlGyroSmoothingElement = m_Xdoc.CreateElement("GyroMouseSmoothingSettings");
                XmlNode xmlGyroSmoothing = m_Xdoc.CreateNode(XmlNodeType.Element, "UseSmoothing", null); xmlGyroSmoothing.InnerText = gyroMouseInfo[device].enableSmoothing.ToString(); xmlGyroSmoothingElement.AppendChild(xmlGyroSmoothing);
                XmlNode xmlGyroSmoothingMethod = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingMethod", null); xmlGyroSmoothingMethod.InnerText = gyroMouseInfo[device].SmoothMethodIdentifier(); xmlGyroSmoothingElement.AppendChild(xmlGyroSmoothingMethod);
                XmlNode xmlGyroSmoothWeight = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingWeight", null); xmlGyroSmoothWeight.InnerText = Convert.ToInt32(gyroMouseInfo[device].smoothingWeight * 100).ToString(); xmlGyroSmoothingElement.AppendChild(xmlGyroSmoothWeight);
                XmlNode xmlGyroSmoothMincutoff = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingMinCutoff", null); xmlGyroSmoothMincutoff.InnerText = gyroMouseInfo[device].minCutoff.ToString(); xmlGyroSmoothingElement.AppendChild(xmlGyroSmoothMincutoff);
                XmlNode xmlGyroSmoothBeta = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingBeta", null); xmlGyroSmoothBeta.InnerText = gyroMouseInfo[device].beta.ToString(); xmlGyroSmoothingElement.AppendChild(xmlGyroSmoothBeta);
                rootElement.AppendChild(xmlGyroSmoothingElement);

                XmlNode xmlGyroMouseHAxis = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseHAxis", null); xmlGyroMouseHAxis.InnerText = gyroMouseHorizontalAxis[device].ToString(); rootElement.AppendChild(xmlGyroMouseHAxis);
                XmlNode xmlGyroMouseDZ = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseDeadZone", null); xmlGyroMouseDZ.InnerText = gyroMouseDZ[device].ToString(); rootElement.AppendChild(xmlGyroMouseDZ);
                XmlNode xmlGyroMinThreshold = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseMinThreshold", null); xmlGyroMinThreshold.InnerText = gyroMouseInfo[device].minThreshold.ToString(); rootElement.AppendChild(xmlGyroMinThreshold);
                XmlNode xmlGyroMouseToggle = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseToggle", null); xmlGyroMouseToggle.InnerText = gyroMouseToggle[device].ToString(); rootElement.AppendChild(xmlGyroMouseToggle);

                XmlNode xmlGyroOutMode = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroOutputMode", null); xmlGyroOutMode.InnerText = gyroOutMode[device].ToString(); rootElement.AppendChild(xmlGyroOutMode);
                XmlNode xmlGyroMStickTriggers = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickTriggers", null); xmlGyroMStickTriggers.InnerText = sAMouseStickTriggers[device].ToString(); rootElement.AppendChild(xmlGyroMStickTriggers);
                XmlNode xmlGyroMStickTriggerCond = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickTriggerCond", null); xmlGyroMStickTriggerCond.InnerText = SaTriggerCondString(sAMouseStickTriggerCond[device]); rootElement.AppendChild(xmlGyroMStickTriggerCond);
                XmlNode xmlGyroMStickTriggerTurns = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickTriggerTurns", null); xmlGyroMStickTriggerTurns.InnerText = gyroMouseStickTriggerTurns[device].ToString(); rootElement.AppendChild(xmlGyroMStickTriggerTurns);
                XmlNode xmlGyroMStickHAxis = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickHAxis", null); xmlGyroMStickHAxis.InnerText = gyroMouseStickHorizontalAxis[device].ToString(); rootElement.AppendChild(xmlGyroMStickHAxis);
                XmlNode xmlGyroMStickDZ = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickDeadZone", null); xmlGyroMStickDZ.InnerText = gyroMStickInfo[device].deadZone.ToString(); rootElement.AppendChild(xmlGyroMStickDZ);
                XmlNode xmlGyroMStickMaxZ = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickMaxZone", null); xmlGyroMStickMaxZ.InnerText = gyroMStickInfo[device].maxZone.ToString(); rootElement.AppendChild(xmlGyroMStickMaxZ);
                XmlNode xmlGyroMStickOutputStick = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickOutputStick", null); xmlGyroMStickOutputStick.InnerText = gyroMStickInfo[device].outputStick.ToString(); rootElement.AppendChild(xmlGyroMStickOutputStick);
                XmlNode xmlGyroMStickOutputStickAxes = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickOutputStickAxes", null); xmlGyroMStickOutputStickAxes.InnerText = gyroMStickInfo[device].outputStickDir.ToString(); rootElement.AppendChild(xmlGyroMStickOutputStickAxes);
                XmlNode xmlGyroMStickAntiDX = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickAntiDeadX", null); xmlGyroMStickAntiDX.InnerText = gyroMStickInfo[device].antiDeadX.ToString(); rootElement.AppendChild(xmlGyroMStickAntiDX);
                XmlNode xmlGyroMStickAntiDY = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickAntiDeadY", null); xmlGyroMStickAntiDY.InnerText = gyroMStickInfo[device].antiDeadY.ToString(); rootElement.AppendChild(xmlGyroMStickAntiDY);
                XmlNode xmlGyroMStickInvert = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickInvert", null); xmlGyroMStickInvert.InnerText = gyroMStickInfo[device].inverted.ToString(); rootElement.AppendChild(xmlGyroMStickInvert);
                XmlNode xmlGyroMStickToggle = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickToggle", null); xmlGyroMStickToggle.InnerText = gyroMouseStickToggle[device].ToString(); rootElement.AppendChild(xmlGyroMStickToggle);
                XmlNode xmlGyroMStickMaxOutput = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickMaxOutput", null); xmlGyroMStickMaxOutput.InnerText = gyroMStickInfo[device].maxOutput.ToString(); rootElement.AppendChild(xmlGyroMStickMaxOutput);
                XmlNode xmlGyroMStickMaxOutputEnabled = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickMaxOutputEnabled", null); xmlGyroMStickMaxOutputEnabled.InnerText = gyroMStickInfo[device].maxOutputEnabled.ToString(); rootElement.AppendChild(xmlGyroMStickMaxOutputEnabled);
                XmlNode xmlGyroMStickVerticalScale = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickVerticalScale", null); xmlGyroMStickVerticalScale.InnerText = gyroMStickInfo[device].vertScale.ToString(); rootElement.AppendChild(xmlGyroMStickVerticalScale);


                /*XmlNode xmlGyroMStickSmoothing = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickSmoothing", null); xmlGyroMStickSmoothing.InnerText = gyroMStickInfo[device].useSmoothing.ToString(); rootElement.AppendChild(xmlGyroMStickSmoothing);
                XmlNode xmlGyroMStickSmoothWeight = m_Xdoc.CreateNode(XmlNodeType.Element, "GyroMouseStickSmoothingWeight", null); xmlGyroMStickSmoothWeight.InnerText = Convert.ToInt32(gyroMStickInfo[device].smoothWeight * 100).ToString(); rootElement.AppendChild(xmlGyroMStickSmoothWeight);
                */
                XmlElement xmlGyroMStickSmoothingElement = m_Xdoc.CreateElement("GyroMouseStickSmoothingSettings");
                XmlNode xmlGyroMStickSmoothing = m_Xdoc.CreateNode(XmlNodeType.Element, "UseSmoothing", null); xmlGyroMStickSmoothing.InnerText = gyroMStickInfo[device].useSmoothing.ToString(); xmlGyroMStickSmoothingElement.AppendChild(xmlGyroMStickSmoothing);
                XmlNode xmlGyroMStickSmoothingMethod = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingMethod", null); xmlGyroMStickSmoothingMethod.InnerText = gyroMStickInfo[device].SmoothMethodIdentifier(); xmlGyroMStickSmoothingElement.AppendChild(xmlGyroMStickSmoothingMethod);
                XmlNode xmlGyroMStickSmoothWeight = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingWeight", null); xmlGyroMStickSmoothWeight.InnerText = Convert.ToInt32(gyroMStickInfo[device].smoothWeight * 100).ToString(); xmlGyroMStickSmoothingElement.AppendChild(xmlGyroMStickSmoothWeight);
                XmlNode xmlGyroMStickSmoothMincutoff = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingMinCutoff", null); xmlGyroMStickSmoothMincutoff.InnerText = gyroMStickInfo[device].minCutoff.ToString(); xmlGyroMStickSmoothingElement.AppendChild(xmlGyroMStickSmoothMincutoff);
                XmlNode xmlGyroMStickSmoothBeta = m_Xdoc.CreateNode(XmlNodeType.Element, "SmoothingBeta", null); xmlGyroMStickSmoothBeta.InnerText = gyroMStickInfo[device].beta.ToString(); xmlGyroMStickSmoothingElement.AppendChild(xmlGyroMStickSmoothBeta);
                rootElement.AppendChild(xmlGyroMStickSmoothingElement);

                XmlElement xmlGyroSwipeSettingsElement = m_Xdoc.CreateElement("GyroSwipeSettings");
                XmlNode xmlGyroSwipeDeadzoneX = m_Xdoc.CreateNode(XmlNodeType.Element, "DeadZoneX", null); xmlGyroSwipeDeadzoneX.InnerText = gyroSwipeInfo[device].deadzoneX.ToString(); xmlGyroSwipeSettingsElement.AppendChild(xmlGyroSwipeDeadzoneX);
                XmlNode xmlGyroSwipeDeadzoneY = m_Xdoc.CreateNode(XmlNodeType.Element, "DeadZoneY", null); xmlGyroSwipeDeadzoneY.InnerText = gyroSwipeInfo[device].deadzoneY.ToString(); xmlGyroSwipeSettingsElement.AppendChild(xmlGyroSwipeDeadzoneY);
                XmlNode xmlGyroSwipeTriggers = m_Xdoc.CreateNode(XmlNodeType.Element, "Triggers", null); xmlGyroSwipeTriggers.InnerText = gyroSwipeInfo[device].triggers; xmlGyroSwipeSettingsElement.AppendChild(xmlGyroSwipeTriggers);
                XmlNode xmlGyroSwipeTriggerCond = m_Xdoc.CreateNode(XmlNodeType.Element, "TriggerCond", null); xmlGyroSwipeTriggerCond.InnerText = SaTriggerCondString(gyroSwipeInfo[device].triggerCond); xmlGyroSwipeSettingsElement.AppendChild(xmlGyroSwipeTriggerCond);
                XmlNode xmlGyroSwipeTriggerTurns = m_Xdoc.CreateNode(XmlNodeType.Element, "TriggerTurns", null); xmlGyroSwipeTriggerTurns.InnerText = gyroSwipeInfo[device].triggerTurns.ToString(); xmlGyroSwipeSettingsElement.AppendChild(xmlGyroSwipeTriggerTurns);
                XmlNode xmlGyroSwipeXAxis = m_Xdoc.CreateNode(XmlNodeType.Element, "XAxis", null); xmlGyroSwipeXAxis.InnerText = gyroSwipeInfo[device].xAxis.ToString(); xmlGyroSwipeSettingsElement.AppendChild(xmlGyroSwipeXAxis);
                XmlNode xmlGyroSwipeDelayTime = m_Xdoc.CreateNode(XmlNodeType.Element, "DelayTime", null); xmlGyroSwipeDelayTime.InnerText = gyroSwipeInfo[device].delayTime.ToString(); xmlGyroSwipeSettingsElement.AppendChild(xmlGyroSwipeDelayTime);
                rootElement.AppendChild(xmlGyroSwipeSettingsElement);

                // Ensure profileActions list exists. Do NOT auto-add "Disconnect Controller"
                // here — respect the stored list so users can intentionally disable
                // the Bluetooth-disconnect special action for an existing profile.
                if (profileActions[device] == null)
                {
                    profileActions[device] = new List<string>();
                }
                XmlNode xmlProfileActions = m_Xdoc.CreateNode(XmlNodeType.Element, "ProfileActions", null);
                xmlProfileActions.InnerText = string.Join("/", profileActions[device]); rootElement.AppendChild(xmlProfileActions);
                XmlNode xmlBTPollRate = m_Xdoc.CreateNode(XmlNodeType.Element, "BTPollRate", null); xmlBTPollRate.InnerText = btPollRate[device].ToString(); rootElement.AppendChild(xmlBTPollRate);

                XmlNode xmlLsOutputCurveMode = m_Xdoc.CreateNode(XmlNodeType.Element, "LSOutputCurveMode", null); xmlLsOutputCurveMode.InnerText = stickOutputCurveString(getLsOutCurveMode(device)); rootElement.AppendChild(xmlLsOutputCurveMode);
                XmlNode xmlLsOutputCurveCustom = m_Xdoc.CreateNode(XmlNodeType.Element, "LSOutputCurveCustom", null); xmlLsOutputCurveCustom.InnerText = lsOutBezierCurveObj[device].ToString(); rootElement.AppendChild(xmlLsOutputCurveCustom);

                XmlNode xmlRsOutputCurveMode = m_Xdoc.CreateNode(XmlNodeType.Element, "RSOutputCurveMode", null); xmlRsOutputCurveMode.InnerText = stickOutputCurveString(getRsOutCurveMode(device)); rootElement.AppendChild(xmlRsOutputCurveMode);
                XmlNode xmlRsOutputCurveCustom = m_Xdoc.CreateNode(XmlNodeType.Element, "RSOutputCurveCustom", null); xmlRsOutputCurveCustom.InnerText = rsOutBezierCurveObj[device].ToString(); rootElement.AppendChild(xmlRsOutputCurveCustom);

                XmlNode xmlLsSquareStickMode = m_Xdoc.CreateNode(XmlNodeType.Element, "LSSquareStick", null); xmlLsSquareStickMode.InnerText = squStickInfo[device].lsMode.ToString(); rootElement.AppendChild(xmlLsSquareStickMode);
                XmlNode xmlRsSquareStickMode = m_Xdoc.CreateNode(XmlNodeType.Element, "RSSquareStick", null); xmlRsSquareStickMode.InnerText = squStickInfo[device].rsMode.ToString(); rootElement.AppendChild(xmlRsSquareStickMode);

                XmlNode xmlSquareStickRoundness = m_Xdoc.CreateNode(XmlNodeType.Element, "SquareStickRoundness", null); xmlSquareStickRoundness.InnerText = squStickInfo[device].lsRoundness.ToString(); rootElement.AppendChild(xmlSquareStickRoundness);
                XmlNode xmlSquareRStickRoundness = m_Xdoc.CreateNode(XmlNodeType.Element, "SquareRStickRoundness", null); xmlSquareRStickRoundness.InnerText = squStickInfo[device].rsRoundness.ToString(); rootElement.AppendChild(xmlSquareRStickRoundness);

                XmlNode xmlLsAntiSnapbackEnabled = m_Xdoc.CreateNode(XmlNodeType.Element, "LSAntiSnapback", null); xmlLsAntiSnapbackEnabled.InnerText = lsAntiSnapbackInfo[device].enabled.ToString(); rootElement.AppendChild(xmlLsAntiSnapbackEnabled);
                XmlNode xmlRsAntiSnapbackEnabled = m_Xdoc.CreateNode(XmlNodeType.Element, "RSAntiSnapback", null); xmlRsAntiSnapbackEnabled.InnerText = rsAntiSnapbackInfo[device].enabled.ToString(); rootElement.AppendChild(xmlRsAntiSnapbackEnabled);

                XmlNode xmlLsAntiSnapbackDelta = m_Xdoc.CreateNode(XmlNodeType.Element, "LSAntiSnapbackDelta", null); xmlLsAntiSnapbackDelta.InnerText = lsAntiSnapbackInfo[device].delta.ToString(); rootElement.AppendChild(xmlLsAntiSnapbackDelta);
                XmlNode xmlRsAntiSnapbackDelta = m_Xdoc.CreateNode(XmlNodeType.Element, "RSAntiSnapbackDelta", null); xmlRsAntiSnapbackDelta.InnerText = rsAntiSnapbackInfo[device].delta.ToString(); rootElement.AppendChild(xmlRsAntiSnapbackDelta);

                XmlNode xmlLsAntiSnapbackTimeout = m_Xdoc.CreateNode(XmlNodeType.Element, "LSAntiSnapbackTimeout", null); xmlLsAntiSnapbackTimeout.InnerText = lsAntiSnapbackInfo[device].timeout.ToString(); rootElement.AppendChild(xmlLsAntiSnapbackTimeout);
                XmlNode xmlRsAntiSnapbackTimeout = m_Xdoc.CreateNode(XmlNodeType.Element, "RSAntiSnapbackTimeout", null); xmlRsAntiSnapbackTimeout.InnerText = rsAntiSnapbackInfo[device].timeout.ToString(); rootElement.AppendChild(xmlRsAntiSnapbackTimeout);

                XmlNode xmlLsOutputMode = m_Xdoc.CreateNode(XmlNodeType.Element, "LSOutputMode", null); xmlLsOutputMode.InnerText = lsOutputSettings[device].mode.ToString(); rootElement.AppendChild(xmlLsOutputMode);
                XmlNode xmlRsOutputMode = m_Xdoc.CreateNode(XmlNodeType.Element, "RSOutputMode", null); xmlRsOutputMode.InnerText = rsOutputSettings[device].mode.ToString(); rootElement.AppendChild(xmlRsOutputMode);

                XmlElement xmlLsOutputSettingsElement = m_Xdoc.CreateElement("LSOutputSettings");
                XmlElement xmlLsFlickStickGroupElement = m_Xdoc.CreateElement("FlickStickSettings"); xmlLsOutputSettingsElement.AppendChild(xmlLsFlickStickGroupElement);
                XmlNode xmlLsFlickStickRWC = m_Xdoc.CreateNode(XmlNodeType.Element, "RealWorldCalibration", null); xmlLsFlickStickRWC.InnerText = lsOutputSettings[device].outputSettings.flickSettings.realWorldCalibration.ToString(); xmlLsFlickStickGroupElement.AppendChild(xmlLsFlickStickRWC);
                XmlNode xmlLsFlickStickThreshold = m_Xdoc.CreateNode(XmlNodeType.Element, "FlickThreshold", null); xmlLsFlickStickThreshold.InnerText = lsOutputSettings[device].outputSettings.flickSettings.flickThreshold.ToString(); xmlLsFlickStickGroupElement.AppendChild(xmlLsFlickStickThreshold);
                XmlNode xmlLsFlickStickTime = m_Xdoc.CreateNode(XmlNodeType.Element, "FlickTime", null); xmlLsFlickStickTime.InnerText = lsOutputSettings[device].outputSettings.flickSettings.flickTime.ToString(); xmlLsFlickStickGroupElement.AppendChild(xmlLsFlickStickTime);
                XmlNode xmlLsMinAngleThreshold = m_Xdoc.CreateNode(XmlNodeType.Element, "MinAngleThreshold", null); xmlLsMinAngleThreshold.InnerText = lsOutputSettings[device].outputSettings.flickSettings.minAngleThreshold.ToString(); xmlLsFlickStickGroupElement.AppendChild(xmlLsMinAngleThreshold);
                rootElement.AppendChild(xmlLsOutputSettingsElement);

                XmlElement xmlRsOutputSettingsElement = m_Xdoc.CreateElement("RSOutputSettings");
                XmlElement xmlRsFlickStickGroupElement = m_Xdoc.CreateElement("FlickStickSettings"); xmlRsOutputSettingsElement.AppendChild(xmlRsFlickStickGroupElement);
                XmlNode xmlRsFlickStickRWC = m_Xdoc.CreateNode(XmlNodeType.Element, "RealWorldCalibration", null); xmlRsFlickStickRWC.InnerText = rsOutputSettings[device].outputSettings.flickSettings.realWorldCalibration.ToString(); xmlRsFlickStickGroupElement.AppendChild(xmlRsFlickStickRWC);
                XmlNode xmlRsFlickStickThreshold = m_Xdoc.CreateNode(XmlNodeType.Element, "FlickThreshold", null); xmlRsFlickStickThreshold.InnerText = rsOutputSettings[device].outputSettings.flickSettings.flickThreshold.ToString(); xmlRsFlickStickGroupElement.AppendChild(xmlRsFlickStickThreshold);
                XmlNode xmlRsFlickStickTime = m_Xdoc.CreateNode(XmlNodeType.Element, "FlickTime", null); xmlRsFlickStickTime.InnerText = rsOutputSettings[device].outputSettings.flickSettings.flickTime.ToString(); xmlRsFlickStickGroupElement.AppendChild(xmlRsFlickStickTime);
                XmlNode xmlRsMinAngleThreshold = m_Xdoc.CreateNode(XmlNodeType.Element, "MinAngleThreshold", null); xmlRsMinAngleThreshold.InnerText = rsOutputSettings[device].outputSettings.flickSettings.minAngleThreshold.ToString(); xmlRsFlickStickGroupElement.AppendChild(xmlRsMinAngleThreshold);
                rootElement.AppendChild(xmlRsOutputSettingsElement);

                XmlNode xmlL2OutputCurveMode = m_Xdoc.CreateNode(XmlNodeType.Element, "L2OutputCurveMode", null); xmlL2OutputCurveMode.InnerText = axisOutputCurveString(getL2OutCurveMode(device)); rootElement.AppendChild(xmlL2OutputCurveMode);
                XmlNode xmlL2OutputCurveCustom = m_Xdoc.CreateNode(XmlNodeType.Element, "L2OutputCurveCustom", null); xmlL2OutputCurveCustom.InnerText = l2OutBezierCurveObj[device].ToString(); rootElement.AppendChild(xmlL2OutputCurveCustom);

                XmlNode xmlL2TwoStageMode = m_Xdoc.CreateNode(XmlNodeType.Element, "L2TwoStageMode", null); xmlL2TwoStageMode.InnerText = l2OutputSettings[device].twoStageMode.ToString(); rootElement.AppendChild(xmlL2TwoStageMode);
                XmlNode xmlR2TwoStageMode = m_Xdoc.CreateNode(XmlNodeType.Element, "R2TwoStageMode", null); xmlR2TwoStageMode.InnerText = r2OutputSettings[device].twoStageMode.ToString(); rootElement.AppendChild(xmlR2TwoStageMode);

                XmlNode xmlL2HipFireTime = m_Xdoc.CreateNode(XmlNodeType.Element, "L2HipFireTime", null); xmlL2HipFireTime.InnerText = l2OutputSettings[device].hipFireMS.ToString(); rootElement.AppendChild(xmlL2HipFireTime);
                XmlNode xmlR2HipFireTime = m_Xdoc.CreateNode(XmlNodeType.Element, "R2HipFireTime", null); xmlR2HipFireTime.InnerText = r2OutputSettings[device].hipFireMS.ToString(); rootElement.AppendChild(xmlR2HipFireTime);

                XmlNode xmlL2TriggerEffect = m_Xdoc.CreateNode(XmlNodeType.Element, "L2TriggerEffect", null); xmlL2TriggerEffect.InnerText = l2OutputSettings[device].triggerEffect.ToString(); rootElement.AppendChild(xmlL2TriggerEffect);
                XmlNode xmlR2TriggerEffect = m_Xdoc.CreateNode(XmlNodeType.Element, "R2TriggerEffect", null); xmlR2TriggerEffect.InnerText = r2OutputSettings[device].triggerEffect.ToString(); rootElement.AppendChild(xmlR2TriggerEffect);

                XmlNode xmlR2OutputCurveMode = m_Xdoc.CreateNode(XmlNodeType.Element, "R2OutputCurveMode", null); xmlR2OutputCurveMode.InnerText = axisOutputCurveString(getR2OutCurveMode(device)); rootElement.AppendChild(xmlR2OutputCurveMode);
                XmlNode xmlR2OutputCurveCustom = m_Xdoc.CreateNode(XmlNodeType.Element, "R2OutputCurveCustom", null); xmlR2OutputCurveCustom.InnerText = r2OutBezierCurveObj[device].ToString(); rootElement.AppendChild(xmlR2OutputCurveCustom);

                XmlNode xmlSXOutputCurveMode = m_Xdoc.CreateNode(XmlNodeType.Element, "SXOutputCurveMode", null); xmlSXOutputCurveMode.InnerText = axisOutputCurveString(getSXOutCurveMode(device)); rootElement.AppendChild(xmlSXOutputCurveMode);
                XmlNode xmlSXOutputCurveCustom = m_Xdoc.CreateNode(XmlNodeType.Element, "SXOutputCurveCustom", null); xmlSXOutputCurveCustom.InnerText = sxOutBezierCurveObj[device].ToString(); rootElement.AppendChild(xmlSXOutputCurveCustom);

                XmlNode xmlSZOutputCurveMode = m_Xdoc.CreateNode(XmlNodeType.Element, "SZOutputCurveMode", null); xmlSZOutputCurveMode.InnerText = axisOutputCurveString(getSZOutCurveMode(device)); rootElement.AppendChild(xmlSZOutputCurveMode);
                XmlNode xmlSZOutputCurveCustom = m_Xdoc.CreateNode(XmlNodeType.Element, "SZOutputCurveCustom", null); xmlSZOutputCurveCustom.InnerText = szOutBezierCurveObj[device].ToString(); rootElement.AppendChild(xmlSZOutputCurveCustom);

                XmlNode xmlTrackBallMode = m_Xdoc.CreateNode(XmlNodeType.Element, "TrackballMode", null); xmlTrackBallMode.InnerText = trackballMode[device].ToString(); rootElement.AppendChild(xmlTrackBallMode);
                XmlNode xmlTrackBallFriction = m_Xdoc.CreateNode(XmlNodeType.Element, "TrackballFriction", null); xmlTrackBallFriction.InnerText = trackballFriction[device].ToString(); rootElement.AppendChild(xmlTrackBallFriction);

                XmlNode xmlTouchRelMouseRotation = m_Xdoc.CreateNode(XmlNodeType.Element, "TouchRelMouseRotation", null); xmlTouchRelMouseRotation.InnerText = Convert.ToInt32(touchpadRelMouse[device].rotation * 180.0 / Math.PI).ToString(); rootElement.AppendChild(xmlTouchRelMouseRotation);
                XmlNode xmlTouchRelMouseMinThreshold = m_Xdoc.CreateNode(XmlNodeType.Element, "TouchRelMouseMinThreshold", null); xmlTouchRelMouseMinThreshold.InnerText = touchpadRelMouse[device].minThreshold.ToString(); rootElement.AppendChild(xmlTouchRelMouseMinThreshold);

                // Isolate as a group. More readable for this???
                //if (false)
                {
                    XmlElement xmlTouchMouseStickGroupEl = m_Xdoc.CreateElement("TouchpadMouseStick");
                    XmlElement xmlTouchMouseStickDeadZone = m_Xdoc.CreateElement("DeadZone"); xmlTouchMouseStickDeadZone.InnerText = touchMStickInfo[device].deadZone.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickDeadZone);
                    XmlElement xmlTouchMouseStickMaxZone = m_Xdoc.CreateElement("MaxZone"); xmlTouchMouseStickMaxZone.InnerText = touchMStickInfo[device].maxZone.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickMaxZone);
                    XmlElement xmlTouchMouseStickOutStick = m_Xdoc.CreateElement("OutputStick"); xmlTouchMouseStickOutStick.InnerText = touchMStickInfo[device].outputStick.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickOutStick);
                    XmlElement xmlTouchMouseStickOutStickAxes = m_Xdoc.CreateElement("OutputStickAxes"); xmlTouchMouseStickOutStickAxes.InnerText = touchMStickInfo[device].outputStickDir.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickOutStickAxes);
                    XmlElement xmlTouchMouseStickAntiDeadX = m_Xdoc.CreateElement("AntiDeadX"); xmlTouchMouseStickAntiDeadX.InnerText = touchMStickInfo[device].antiDeadX.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickAntiDeadX);
                    XmlElement xmlTouchMouseStickAntiDeadY = m_Xdoc.CreateElement("AntiDeadY"); xmlTouchMouseStickAntiDeadY.InnerText = touchMStickInfo[device].antiDeadY.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickAntiDeadY);
                    XmlElement xmlTouchMouseStickInvert = m_Xdoc.CreateElement("Invert"); xmlTouchMouseStickInvert.InnerText = touchMStickInfo[device].inverted.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickInvert);
                    XmlElement xmlTouchMouseStickMaxOutput = m_Xdoc.CreateElement("MaxOutput"); xmlTouchMouseStickMaxOutput.InnerText = touchMStickInfo[device].maxOutput.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickMaxOutput);
                    XmlElement xmlTouchMouseStickMaxOutputEnabled = m_Xdoc.CreateElement("MaxOutputEnabled"); xmlTouchMouseStickMaxOutputEnabled.InnerText = touchMStickInfo[device].maxOutputEnabled.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickMaxOutputEnabled);
                    XmlElement xmlTouchMouseStickVerticalScale = m_Xdoc.CreateElement("VerticalScale"); xmlTouchMouseStickVerticalScale.InnerText = touchMStickInfo[device].vertScale.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickVerticalScale);
                    XmlElement xmlTouchMouseStickOutputCurve = m_Xdoc.CreateElement("OutputCurve"); xmlTouchMouseStickOutputCurve.InnerText = touchMStickInfo[device].outputCurve.ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickOutputCurve);
                    XmlElement xmlTouchMouseStickRotation = m_Xdoc.CreateElement("Rotation"); xmlTouchMouseStickRotation.InnerText = Convert.ToInt32(touchMStickInfo[device].rotationRad * 180.0 / Math.PI).ToString(); xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickRotation);

                    XmlElement xmlTouchMouseStickSmoothSettingsEl = m_Xdoc.CreateElement("SmoothingSettings");
                    XmlElement xmlTouchMouseStickSmoothMethod = m_Xdoc.CreateElement("SmoothingMethod"); xmlTouchMouseStickSmoothMethod.InnerText = touchMStickInfo[device].smoothingMethod.ToString(); xmlTouchMouseStickSmoothSettingsEl.AppendChild(xmlTouchMouseStickSmoothMethod);
                    XmlElement xmlTouchMouseStickSmoothMinCutoff = m_Xdoc.CreateElement("SmoothingMinCutoff"); xmlTouchMouseStickSmoothMinCutoff.InnerText = touchMStickInfo[device].minCutoff.ToString(); xmlTouchMouseStickSmoothSettingsEl.AppendChild(xmlTouchMouseStickSmoothMinCutoff);
                    XmlElement xmlTouchMouseStickSmoothBeta = m_Xdoc.CreateElement("SmoothingBeta"); xmlTouchMouseStickSmoothBeta.InnerText = touchMStickInfo[device].beta.ToString(); xmlTouchMouseStickSmoothSettingsEl.AppendChild(xmlTouchMouseStickSmoothBeta);
                    xmlTouchMouseStickGroupEl.AppendChild(xmlTouchMouseStickSmoothSettingsEl);
                    rootElement.AppendChild(xmlTouchMouseStickGroupEl);
                }

                XmlNode xmlTouchButtonMode = m_Xdoc.CreateNode(XmlNodeType.Element, "TouchpadButtonMode", null); xmlTouchButtonMode.InnerText = touchpadButtonMode[device].ToString(); rootElement.AppendChild(xmlTouchButtonMode);
                // Start of DualSense specific settings
                // xmlDSRumbleGroupElement.AppendChild();
                XmlElement xmlDualSenseControllerSettingsElement = m_Xdoc.CreateElement("DualSenseControllerSettings");
                XmlElement xmlDSRumbleGroupElement = m_Xdoc.CreateElement("RumbleSettings"); xmlDualSenseControllerSettingsElement.AppendChild(xmlDSRumbleGroupElement);
                XmlNode xmlDSREmulationModeElement = m_Xdoc.CreateNode(XmlNodeType.Element, "EmulationMode", null); xmlDSREmulationModeElement.InnerText = dualSenseRumbleEmulationMode[device].ToString(); xmlDSRumbleGroupElement.AppendChild(xmlDSREmulationModeElement);
                XmlNode xmlDSREnableGenericRumbleRescaleElement = m_Xdoc.CreateNode(XmlNodeType.Element, "EnableGenericRumbleRescale", null); xmlDSREnableGenericRumbleRescaleElement.InnerText = useGenericRumbleRescaleForDualSenses[device].ToString(); xmlDSRumbleGroupElement.AppendChild(xmlDSREnableGenericRumbleRescaleElement);
                XmlNode xmlDSRHapticPowerLevelElement = m_Xdoc.CreateNode(XmlNodeType.Element, "HapticPowerLevel", null); xmlDSRHapticPowerLevelElement.InnerText = dualSenseHapticPowerLevel[device].ToString(); xmlDSRumbleGroupElement.AppendChild(xmlDSRHapticPowerLevelElement);
                rootElement.AppendChild(xmlDualSenseControllerSettingsElement);
                //
                // End of DualSense specific settings

                XmlNode xmlOutContDevice = m_Xdoc.CreateNode(XmlNodeType.Element, "OutputContDevice", null); xmlOutContDevice.InnerText = OutContDeviceString(outputDevType[device]); rootElement.AppendChild(xmlOutContDevice);

                XmlNode NodeControl = m_Xdoc.CreateNode(XmlNodeType.Element, "Control", null);
                XmlNode Key = m_Xdoc.CreateNode(XmlNodeType.Element, "Key", null);
                XmlNode Macro = m_Xdoc.CreateNode(XmlNodeType.Element, "Macro", null);
                XmlNode KeyType = m_Xdoc.CreateNode(XmlNodeType.Element, "KeyType", null);
                XmlNode Button = m_Xdoc.CreateNode(XmlNodeType.Element, "Button", null);
                XmlNode Extras = m_Xdoc.CreateNode(XmlNodeType.Element, "Extras", null);

                XmlNode NodeShiftControl = m_Xdoc.CreateNode(XmlNodeType.Element, "ShiftControl", null);

                XmlNode ShiftKey = m_Xdoc.CreateNode(XmlNodeType.Element, "Key", null);
                XmlNode ShiftMacro = m_Xdoc.CreateNode(XmlNodeType.Element, "Macro", null);
                XmlNode ShiftKeyType = m_Xdoc.CreateNode(XmlNodeType.Element, "KeyType", null);
                XmlNode ShiftButton = m_Xdoc.CreateNode(XmlNodeType.Element, "Button", null);
                XmlNode ShiftExtras = m_Xdoc.CreateNode(XmlNodeType.Element, "Extras", null);

                foreach (DS4ControlSettings dcs in ds4settings[device])
                {
                    if (dcs.actionType != DS4ControlSettings.ActionType.Default)
                    {
                        XmlNode buttonNode;
                        string keyType = string.Empty;

                        if (dcs.actionType == DS4ControlSettings.ActionType.Button &&
                            dcs.action.actionBtn == X360Controls.Unbound)
                        {
                            keyType += DS4KeyType.Unbound;
                        }

                        if (dcs.keyType.HasFlag(DS4KeyType.HoldMacro))
                            keyType += DS4KeyType.HoldMacro;
                        else if (dcs.keyType.HasFlag(DS4KeyType.Macro))
                            keyType += DS4KeyType.Macro;

                        if (dcs.keyType.HasFlag(DS4KeyType.Toggle))
                            keyType += DS4KeyType.Toggle;
                        if (dcs.keyType.HasFlag(DS4KeyType.ScanCode))
                            keyType += DS4KeyType.ScanCode;

                        if (keyType != string.Empty)
                        {
                            buttonNode = m_Xdoc.CreateNode(XmlNodeType.Element, dcs.control.ToString(), null);
                            buttonNode.InnerText = keyType;
                            KeyType.AppendChild(buttonNode);
                        }

                        buttonNode = m_Xdoc.CreateNode(XmlNodeType.Element, dcs.control.ToString(), null);
                        if (dcs.actionType == DS4ControlSettings.ActionType.Macro)
                        {
                            int[] ii = dcs.action.actionMacro;
                            buttonNode.InnerText = string.Join("/", ii);
                            Macro.AppendChild(buttonNode);
                        }
                        else if (dcs.actionType == DS4ControlSettings.ActionType.Key)
                        {
                            buttonNode.InnerText = dcs.action.actionKey.ToString();
                            Key.AppendChild(buttonNode);
                        }
                        else if (dcs.actionType == DS4ControlSettings.ActionType.Button)
                        {
                            buttonNode.InnerText = getX360ControlString((X360Controls)dcs.action.actionBtn);
                            Button.AppendChild(buttonNode);
                        }
                    }

                    bool hasvalue = false;
                    if (!string.IsNullOrEmpty(dcs.extras))
                    {
                        foreach (string s in dcs.extras.Split(','))
                        {
                            if (s != "0")
                            {
                                hasvalue = true;
                                break;
                            }
                        }
                    }

                    if (hasvalue)
                    {
                        XmlNode extraNode = m_Xdoc.CreateNode(XmlNodeType.Element, dcs.control.ToString(), null);
                        extraNode.InnerText = dcs.extras;
                        Extras.AppendChild(extraNode);
                    }

                    if (dcs.shiftActionType != DS4ControlSettings.ActionType.Default && dcs.shiftTrigger > 0)
                    {
                        XmlElement buttonNode;
                        string keyType = string.Empty;

                        if (dcs.shiftActionType == DS4ControlSettings.ActionType.Button &&
                            dcs.shiftAction.actionBtn == X360Controls.Unbound)
                        {
                            keyType += DS4KeyType.Unbound;
                        }

                        if (dcs.shiftKeyType.HasFlag(DS4KeyType.HoldMacro))
                            keyType += DS4KeyType.HoldMacro;
                        if (dcs.shiftKeyType.HasFlag(DS4KeyType.Macro))
                            keyType += DS4KeyType.Macro;
                        if (dcs.shiftKeyType.HasFlag(DS4KeyType.Toggle))
                            keyType += DS4KeyType.Toggle;
                        if (dcs.shiftKeyType.HasFlag(DS4KeyType.ScanCode))
                            keyType += DS4KeyType.ScanCode;

                        if (keyType != string.Empty)
                        {
                            buttonNode = m_Xdoc.CreateElement(dcs.control.ToString());
                            buttonNode.InnerText = keyType;
                            ShiftKeyType.AppendChild(buttonNode);
                        }

                        buttonNode = m_Xdoc.CreateElement(dcs.control.ToString());
                        buttonNode.SetAttribute("Trigger", dcs.shiftTrigger.ToString());
                        if (dcs.shiftActionType == DS4ControlSettings.ActionType.Macro)
                        {
                            int[] ii = dcs.shiftAction.actionMacro;
                            buttonNode.InnerText = string.Join("/", ii);
                            ShiftMacro.AppendChild(buttonNode);
                        }
                        else if (dcs.shiftActionType == DS4ControlSettings.ActionType.Key)
                        {
                            buttonNode.InnerText = dcs.shiftAction.actionKey.ToString();
                            ShiftKey.AppendChild(buttonNode);
                        }
                        else if (dcs.shiftActionType == DS4ControlSettings.ActionType.Button)
                        {
                            buttonNode.InnerText = dcs.shiftAction.actionBtn.ToString();
                            ShiftButton.AppendChild(buttonNode);
                        }
                    }

                    hasvalue = false;
                    if (!string.IsNullOrEmpty(dcs.shiftExtras))
                    {
                        foreach (string s in dcs.shiftExtras.Split(','))
                        {
                            if (s != "0")
                            {
                                hasvalue = true;
                                break;
                            }
                        }
                    }

                    if (hasvalue)
                    {
                        XmlNode extraNode = m_Xdoc.CreateNode(XmlNodeType.Element, dcs.control.ToString(), null);
                        extraNode.InnerText = dcs.shiftExtras;
                        ShiftExtras.AppendChild(extraNode);
                    }
                }

                rootElement.AppendChild(NodeControl);
                if (Button.HasChildNodes)
                    NodeControl.AppendChild(Button);
                if (Macro.HasChildNodes)
                    NodeControl.AppendChild(Macro);
                if (Key.HasChildNodes)
                    NodeControl.AppendChild(Key);
                if (Extras.HasChildNodes)
                    NodeControl.AppendChild(Extras);
                if (KeyType.HasChildNodes)
                    NodeControl.AppendChild(KeyType);

                if (NodeControl.HasChildNodes)
                    rootElement.AppendChild(NodeControl);

                rootElement.AppendChild(NodeShiftControl);
                if (ShiftButton.HasChildNodes)
                    NodeShiftControl.AppendChild(ShiftButton);
                if (ShiftMacro.HasChildNodes)
                    NodeShiftControl.AppendChild(ShiftMacro);
                if (ShiftKey.HasChildNodes)
                    NodeShiftControl.AppendChild(ShiftKey);
                if (ShiftKeyType.HasChildNodes)
                    NodeShiftControl.AppendChild(ShiftKeyType);
                if (ShiftExtras.HasChildNodes)
                    NodeShiftControl.AppendChild(ShiftExtras);

                m_Xdoc.AppendChild(rootElement);
                m_Xdoc.Save(path);
            }
            catch { Saved = false; }
            return Saved;
        }

        public DS4Controls getDS4ControlsByName(string key)
        {
            if (!key.StartsWith("bn"))
                return (DS4Controls)Enum.Parse(typeof(DS4Controls), key, true);

            switch (key)
            {
                case "bnShare": return DS4Controls.Share;
                case "bnL3": return DS4Controls.L3;
                case "bnR3": return DS4Controls.R3;
                case "bnOptions": return DS4Controls.Options;
                case "bnUp": return DS4Controls.DpadUp;
                case "bnRight": return DS4Controls.DpadRight;
                case "bnDown": return DS4Controls.DpadDown;
                case "bnLeft": return DS4Controls.DpadLeft;

                case "bnL1": return DS4Controls.L1;
                case "bnR1": return DS4Controls.R1;
                case "bnTriangle": return DS4Controls.Triangle;
                case "bnCircle": return DS4Controls.Circle;
                case "bnCross": return DS4Controls.Cross;
                case "bnSquare": return DS4Controls.Square;

                case "bnPS": return DS4Controls.PS;
                case "bnLSLeft": return DS4Controls.LXNeg;
                case "bnLSUp": return DS4Controls.LYNeg;
                case "bnRSLeft": return DS4Controls.RXNeg;
                case "bnRSUp": return DS4Controls.RYNeg;

                case "bnLSRight": return DS4Controls.LXPos;
                case "bnLSDown": return DS4Controls.LYPos;
                case "bnRSRight": return DS4Controls.RXPos;
                case "bnRSDown": return DS4Controls.RYPos;
                case "bnL2": return DS4Controls.L2;
                case "bnR2": return DS4Controls.R2;

                case "bnTouchLeft": return DS4Controls.TouchLeft;
                case "bnTouchMulti": return DS4Controls.TouchMulti;
                case "bnTouchUpper": return DS4Controls.TouchUpper;
                case "bnTouchRight": return DS4Controls.TouchRight;
                case "bnGyroXP": return DS4Controls.GyroXPos;
                case "bnGyroXN": return DS4Controls.GyroXNeg;
                case "bnGyroZP": return DS4Controls.GyroZPos;
                case "bnGyroZN": return DS4Controls.GyroZNeg;

                case "bnSwipeUp": return DS4Controls.SwipeUp;
                case "bnSwipeDown": return DS4Controls.SwipeDown;
                case "bnSwipeLeft": return DS4Controls.SwipeLeft;
                case "bnSwipeRight": return DS4Controls.SwipeRight;
                case "bnTouchStarted": return DS4Controls.TouchStarted;
                case "bnTouchEnded": return DS4Controls.TouchEnded;

                #region OldShiftname
                case "sbnShare": return DS4Controls.Share;
                case "sbnL3": return DS4Controls.L3;
                case "sbnR3": return DS4Controls.R3;
                case "sbnOptions": return DS4Controls.Options;
                case "sbnUp": return DS4Controls.DpadUp;
                case "sbnRight": return DS4Controls.DpadRight;
                case "sbnDown": return DS4Controls.DpadDown;
                case "sbnLeft": return DS4Controls.DpadLeft;

                case "sbnL1": return DS4Controls.L1;
                case "sbnR1": return DS4Controls.R1;
                case "sbnTriangle": return DS4Controls.Triangle;
                case "sbnCircle": return DS4Controls.Circle;
                case "sbnCross": return DS4Controls.Cross;
                case "sbnSquare": return DS4Controls.Square;

                case "sbnPS": return DS4Controls.PS;
                case "sbnLSLeft": return DS4Controls.LXNeg;
                case "sbnLSUp": return DS4Controls.LYNeg;
                case "sbnRSLeft": return DS4Controls.RXNeg;
                case "sbnRSUp": return DS4Controls.RYNeg;

                case "sbnLSRight": return DS4Controls.LXPos;
                case "sbnLSDown": return DS4Controls.LYPos;
                case "sbnRSRight": return DS4Controls.RXPos;
                case "sbnRSDown": return DS4Controls.RYPos;
                case "sbnL2": return DS4Controls.L2;
                case "sbnR2": return DS4Controls.R2;

                case "sbnTouchLeft": return DS4Controls.TouchLeft;
                case "sbnTouchMulti": return DS4Controls.TouchMulti;
                case "sbnTouchUpper": return DS4Controls.TouchUpper;
                case "sbnTouchRight": return DS4Controls.TouchRight;
                case "sbnGsyroXP": return DS4Controls.GyroXPos;
                case "sbnGyroXN": return DS4Controls.GyroXNeg;
                case "sbnGyroZP": return DS4Controls.GyroZPos;
                case "sbnGyroZN": return DS4Controls.GyroZNeg;
                #endregion

                case "bnShiftShare": return DS4Controls.Share;
                case "bnShiftL3": return DS4Controls.L3;
                case "bnShiftR3": return DS4Controls.R3;
                case "bnShiftOptions": return DS4Controls.Options;
                case "bnShiftUp": return DS4Controls.DpadUp;
                case "bnShiftRight": return DS4Controls.DpadRight;
                case "bnShiftDown": return DS4Controls.DpadDown;
                case "bnShiftLeft": return DS4Controls.DpadLeft;

                case "bnShiftL1": return DS4Controls.L1;
                case "bnShiftR1": return DS4Controls.R1;
                case "bnShiftTriangle": return DS4Controls.Triangle;
                case "bnShiftCircle": return DS4Controls.Circle;
                case "bnShiftCross": return DS4Controls.Cross;
                case "bnShiftSquare": return DS4Controls.Square;

                case "bnShiftPS": return DS4Controls.PS;
                case "bnShiftLSLeft": return DS4Controls.LXNeg;
                case "bnShiftLSUp": return DS4Controls.LYNeg;
                case "bnShiftRSLeft": return DS4Controls.RXNeg;
                case "bnShiftRSUp": return DS4Controls.RYNeg;

                case "bnShiftLSRight": return DS4Controls.LXPos;
                case "bnShiftLSDown": return DS4Controls.LYPos;
                case "bnShiftRSRight": return DS4Controls.RXPos;
                case "bnShiftRSDown": return DS4Controls.RYPos;
                case "bnShiftL2": return DS4Controls.L2;
                case "bnShiftR2": return DS4Controls.R2;

                case "bnShiftTouchLeft": return DS4Controls.TouchLeft;
                case "bnShiftTouchMulti": return DS4Controls.TouchMulti;
                case "bnShiftTouchUpper": return DS4Controls.TouchUpper;
                case "bnShiftTouchRight": return DS4Controls.TouchRight;
                case "bnShiftGyroXP": return DS4Controls.GyroXPos;
                case "bnShiftGyroXN": return DS4Controls.GyroXNeg;
                case "bnShiftGyroZP": return DS4Controls.GyroZPos;
                case "bnShiftGyroZN": return DS4Controls.GyroZNeg;

                case "bnShiftSwipeUp": return DS4Controls.SwipeUp;
                case "bnShiftSwipeDown": return DS4Controls.SwipeDown;
                case "bnShiftSwipeLeft": return DS4Controls.SwipeLeft;
                case "bnShiftSwipeRight": return DS4Controls.SwipeRight;

                case "bnShiftTouchStarted": return DS4Controls.TouchStarted;
                case "bnShiftTouchEnded": return DS4Controls.TouchEnded;
            }

            return 0;
        }

        public X360Controls getX360ControlsByName(string key)
        {
            X360Controls x3c;
            if (Enum.TryParse(key, true, out x3c))
                return x3c;

            switch (key)
            {
                case "Back": return X360Controls.Back;
                case "Left Stick": return X360Controls.LS;
                case "Right Stick": return X360Controls.RS;
                case "Start": return X360Controls.Start;
                case "Up Button": return X360Controls.DpadUp;
                case "Right Button": return X360Controls.DpadRight;
                case "Down Button": return X360Controls.DpadDown;
                case "Left Button": return X360Controls.DpadLeft;

                case "Left Bumper": return X360Controls.LB;
                case "Right Bumper": return X360Controls.RB;
                case "Y Button": return X360Controls.Y;
                case "B Button": return X360Controls.B;
                case "A Button": return X360Controls.A;
                case "X Button": return X360Controls.X;

                case "Guide": return X360Controls.Guide;
                case "Left X-Axis-": return X360Controls.LXNeg;
                case "Left Y-Axis-": return X360Controls.LYNeg;
                case "Right X-Axis-": return X360Controls.RXNeg;
                case "Right Y-Axis-": return X360Controls.RYNeg;

                case "Left X-Axis+": return X360Controls.LXPos;
                case "Left Y-Axis+": return X360Controls.LYPos;
                case "Right X-Axis+": return X360Controls.RXPos;
                case "Right Y-Axis+": return X360Controls.RYPos;
                case "Left Trigger": return X360Controls.LT;
                case "Right Trigger": return X360Controls.RT;
                case "Touchpad Click": return X360Controls.TouchpadClick;

                case "Left Mouse Button": return X360Controls.LeftMouse;
                case "Right Mouse Button": return X360Controls.RightMouse;
                case "Middle Mouse Button": return X360Controls.MiddleMouse;
                case "4th Mouse Button": return X360Controls.FourthMouse;
                case "5th Mouse Button": return X360Controls.FifthMouse;
                case "Mouse Wheel Up": return X360Controls.WUP;
                case "Mouse Wheel Down": return X360Controls.WDOWN;
                case "Mouse Up": return X360Controls.MouseUp;
                case "Mouse Down": return X360Controls.MouseDown;
                case "Mouse Left": return X360Controls.MouseLeft;
                case "Mouse Right": return X360Controls.MouseRight;
                case "Unbound": return X360Controls.Unbound;
            }

            return X360Controls.Unbound;
        }

        public string getX360ControlString(X360Controls key)
        {
            switch (key)
            {
                case X360Controls.Back: return "Back";
                case X360Controls.LS: return "Left Stick";
                case X360Controls.RS: return "Right Stick";
                case X360Controls.Start: return "Start";
                case X360Controls.DpadUp: return "Up Button";
                case X360Controls.DpadRight: return "Right Button";
                case X360Controls.DpadDown: return "Down Button";
                case X360Controls.DpadLeft: return "Left Button";

                case X360Controls.LB: return "Left Bumper";
                case X360Controls.RB: return "Right Bumper";
                case X360Controls.Y: return "Y Button";
                case X360Controls.B: return "B Button";
                case X360Controls.A: return "A Button";
                case X360Controls.X: return "X Button";

                case X360Controls.Guide: return "Guide";
                case X360Controls.LXNeg: return "Left X-Axis-";
                case X360Controls.LYNeg: return "Left Y-Axis-";
                case X360Controls.RXNeg: return "Right X-Axis-";
                case X360Controls.RYNeg: return "Right Y-Axis-";

                case X360Controls.LXPos: return "Left X-Axis+";
                case X360Controls.LYPos: return "Left Y-Axis+";
                case X360Controls.RXPos: return "Right X-Axis+";
                case X360Controls.RYPos: return "Right Y-Axis+";
                case X360Controls.LT: return "Left Trigger";
                case X360Controls.RT: return "Right Trigger";
                case X360Controls.TouchpadClick: return "Touchpad Click";

                case X360Controls.LeftMouse: return "Left Mouse Button";
                case X360Controls.RightMouse: return "Right Mouse Button";
                case X360Controls.MiddleMouse: return "Middle Mouse Button";
                case X360Controls.FourthMouse: return "4th Mouse Button";
                case X360Controls.FifthMouse: return "5th Mouse Button";
                case X360Controls.WUP: return "Mouse Wheel Up";
                case X360Controls.WDOWN: return "Mouse Wheel Down";
                case X360Controls.MouseUp: return "Mouse Up";
                case X360Controls.MouseDown: return "Mouse Down";
                case X360Controls.MouseLeft: return "Mouse Left";
                case X360Controls.MouseRight: return "Mouse Right";
                case X360Controls.Unbound: return "Unbound";
            }

            return "Unbound";
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
                profilepath = Global.appdatapath + @"\Profiles\" + profilePath[device] + ".xml";
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

                if (device < Global.MAX_DS4_CONTROLLER_COUNT)
                {
                    DS4LightBar.forcelight[device] = false;
                    DS4LightBar.forcedFlash[device] = 0;
                }

                OutContType oldContType = Global.activeOutDevType[device];
                LightbarSettingInfo lightbarSettings = lightbarSettingInfo[device];
                LightbarDS4WinInfo lightInfo = lightbarSettings.ds4winSettings;

                // Make sure to reset currently set profile values before parsing
                ResetProfile(device);
                ResetMouseProperties(device, control);
                // Reset some Mapping properties before attempting to load different
                // profile
                control.PreLoadReset(device);

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/touchToggle"); Boolean.TryParse(Item.InnerText, out enableTouchToggle[device]); }
                catch { missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/idleDisconnectTimeout");
                    if (int.TryParse(Item?.InnerText ?? "", out int temp))
                    {
                        idleDisconnectTimeout[device] = temp;
                    }
                }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/outputDataToDS4"); Boolean.TryParse(Item.InnerText, out enableOutputDataToDS4[device]); }
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

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RumbleBoost"); Byte.TryParse(Item.InnerText, out rumble[device]); }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RumbleAutostopTime"); Int32.TryParse(Item.InnerText, out rumbleAutostopTime[device]); }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ledAsBatteryIndicator"); Boolean.TryParse(Item.InnerText, out lightInfo.ledAsBattery); }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/FlashType"); Byte.TryParse(Item.InnerText, out lightInfo.flashType); }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/flashBatteryAt"); Int32.TryParse(Item.InnerText, out lightInfo.flashAt); }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/touchSensitivity"); Byte.TryParse(Item.InnerText, out touchSensitivity[device]); }
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

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/touchpadJitterCompensation"); bool.TryParse(Item.InnerText, out touchpadJitterCompensation[device]); }
                catch { missingSetting = true; }
                // Phase6-Step12: 旧形式の読込で欠けていた 10 要素。要素が無い（古いプロファイル）、または値が不正なときは、
                // ResetProfile で設定済みの既定値のままにする（missingSetting は立てない）。
                LoadLegacyExtraSettings(device, rootname);

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/lowerRCOn"); bool.TryParse(Item.InnerText, out lowerRCOn[device]); }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/tapSensitivity"); byte.TryParse(Item.InnerText, out tapSensitivity[device]); }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/doubleTap"); bool.TryParse(Item.InnerText, out doubleTap[device]); }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/scrollSensitivity"); int.TryParse(Item.InnerText, out scrollSensitivity[device]); }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchpadInvert"); int temp = 0; int.TryParse(Item.InnerText, out temp); touchpadInvert[device] = Math.Min(Math.Max(temp, 0), 3); }
                catch { touchpadInvert[device] = 0; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchpadClickPassthru"); bool.TryParse(Item.InnerText, out touchClickPassthru[device]); }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LeftTriggerMiddle"); byte.TryParse(Item.InnerText, out l2ModInfo[device].deadZone); }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RightTriggerMiddle"); byte.TryParse(Item.InnerText, out r2ModInfo[device].deadZone); }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2AntiDeadZone"); int.TryParse(Item.InnerText, out l2ModInfo[device].antiDeadZone); }
                catch { l2ModInfo[device].antiDeadZone = 0; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2AntiDeadZone"); int.TryParse(Item.InnerText, out r2ModInfo[device].antiDeadZone); }
                catch { r2ModInfo[device].antiDeadZone = 0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2MaxZone"); int temp = 100;
                    int.TryParse(Item.InnerText, out temp);
                    l2ModInfo[device].maxZone = Math.Min(Math.Max(temp, 0), 100);
                }
                catch { l2ModInfo[device].maxZone = 100; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2MaxZone"); int temp = 100;
                    int.TryParse(Item.InnerText, out temp);
                    r2ModInfo[device].maxZone = Math.Min(Math.Max(temp, 0), 100);
                }
                catch { r2ModInfo[device].maxZone = 100; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2MaxOutput"); double temp = 100.0;
                    temp = double.Parse(Item.InnerText);
                    l2ModInfo[device].maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                }
                catch { l2ModInfo[device].maxOutput = 100.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2MaxOutput"); double temp = 100.0;
                    temp = double.Parse(Item.InnerText);
                    r2ModInfo[device].maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                }
                catch { r2ModInfo[device].maxOutput = 100.0; missingSetting = true; }

                try
                {
                    // Take a rotation angle (degrees) and convert to radians
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSRotation"); int tempDegrees = 0;
                    int.TryParse(Item.InnerText, out tempDegrees);
                    tempDegrees = Math.Min(Math.Max(tempDegrees, -180), 180);
                    LSRotation[device] = tempDegrees * Math.PI / 180.0;
                }
                catch { LSRotation[device] = 0.0; missingSetting = true; }

                try
                {
                    // Take a rotation angle (degrees) and convert to radians
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSRotation"); int tempDegrees = 0;
                    int.TryParse(Item.InnerText, out tempDegrees);
                    tempDegrees = Math.Min(Math.Max(tempDegrees, -180), 180);
                    RSRotation[device] = tempDegrees * Math.PI / 180.0;
                }
                catch { RSRotation[device] = 0.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSFuzz"); int temp = 0;
                    int.TryParse(Item.InnerText, out temp);
                    temp = Math.Min(Math.Max(temp, 0), 100);
                    lsModInfo[device].fuzz = temp;
                }
                catch { lsModInfo[device].fuzz = StickDeadZoneInfo.DEFAULT_FUZZ; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSFuzz"); int temp = 0;
                    int.TryParse(Item.InnerText, out temp);
                    temp = Math.Min(Math.Max(temp, 0), 100);
                    rsModInfo[device].fuzz = temp;
                }
                catch { rsModInfo[device].fuzz = StickDeadZoneInfo.DEFAULT_FUZZ; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ButtonMouseSensitivity"); int.TryParse(Item.InnerText, out buttonMouseInfos[device].buttonSensitivity); }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ButtonMouseOffset"); double.TryParse(Item.InnerText, out buttonMouseInfos[device].mouseVelocityOffset); }
                catch { missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ButtonMouseVerticalScale");
                    int.TryParse(Item.InnerText, out int temp);
                    buttonMouseInfos[device].buttonVerticalScale = Math.Min(Math.Max(temp, 0), 500) * 0.01;
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
                    lsModInfo[device].deadZone = temp;
                }
                catch
                {
                    lsModInfo[device].deadZone = 10; missingSetting = true;
                }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSDeadZone");
                    int.TryParse(Item.InnerText, out int temp);
                    temp = Math.Min(Math.Max(temp, 0), 127);
                    rsModInfo[device].deadZone = temp;
                }
                catch
                {
                    rsModInfo[device].deadZone = 10; missingSetting = true;
                }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSAntiDeadZone"); int.TryParse(Item.InnerText, out lsModInfo[device].antiDeadZone); }
                catch { lsModInfo[device].antiDeadZone = 20; missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSAntiDeadZone"); int.TryParse(Item.InnerText, out rsModInfo[device].antiDeadZone); }
                catch { rsModInfo[device].antiDeadZone = 20; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSMaxZone"); int temp = 100;
                    int.TryParse(Item.InnerText, out temp);
                    lsModInfo[device].maxZone = Math.Min(Math.Max(temp, 0), 100);
                }
                catch { lsModInfo[device].maxZone = 100; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSMaxZone"); int temp = 100;
                    int.TryParse(Item.InnerText, out temp);
                    rsModInfo[device].maxZone = Math.Min(Math.Max(temp, 0), 100);
                }
                catch { rsModInfo[device].maxZone = 100; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSVerticalScale");
                    if (double.TryParse(Item?.InnerText ?? "", out double temp))
                    {
                        lsModInfo[device].verticalScale = Math.Min(Math.Max(temp, 0.0), 200.0);
                    }
                }
                catch { lsModInfo[device].verticalScale = StickDeadZoneInfo.DEFAULT_VERTICAL_SCALE; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSMaxOutput"); double temp = 100.0;
                    temp = double.Parse(Item.InnerText);
                    lsModInfo[device].maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                }
                catch { lsModInfo[device].maxOutput = 100.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSMaxOutputForce");
                    if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                    {
                        lsModInfo[device].maxOutputForce = temp;
                    }
                }
                catch { lsModInfo[device].maxOutputForce = StickDeadZoneInfo.DEFAULT_MAXOUTPUT_FORCE; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSMaxOutput"); double temp = 100.0;
                    temp = double.Parse(Item.InnerText);
                    rsModInfo[device].maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                }
                catch { rsModInfo[device].maxOutput = 100; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSMaxOutputForce");
                    if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                    {
                        rsModInfo[device].maxOutputForce = temp;
                    }
                }
                catch { rsModInfo[device].maxOutputForce = StickDeadZoneInfo.DEFAULT_MAXOUTPUT_FORCE; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOuterBindDead");
                    if (int.TryParse(Item?.InnerText ?? "", out int temp))
                    {
                        temp = Math.Min(Math.Max(temp, 0), 100);
                        lsModInfo[device].outerBindDeadZone = temp;
                    }
                }
                catch { lsModInfo[device].outerBindDeadZone = StickDeadZoneInfo.DEFAULT_OUTER_BIND_DEAD; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOuterBindDead");
                    if (int.TryParse(Item?.InnerText ?? "", out int temp))
                    {
                        temp = Math.Min(Math.Max(temp, 0), 100);
                        rsModInfo[device].outerBindDeadZone = temp;
                    }
                }
                catch { rsModInfo[device].outerBindDeadZone = StickDeadZoneInfo.DEFAULT_OUTER_BIND_DEAD; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOuterBindInvert");
                    if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                    {
                        lsModInfo[device].outerBindInvert = temp;
                    }
                }
                catch { lsModInfo[device].outerBindInvert = StickDeadZoneInfo.DEFAULT_OUTER_BIND_INVERT; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOuterBindInvert");
                    if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                    {
                        rsModInfo[device].outerBindInvert = temp;
                    }
                }
                catch { rsModInfo[device].outerBindInvert = StickDeadZoneInfo.DEFAULT_OUTER_BIND_INVERT; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSVerticalScale");
                    if (double.TryParse(Item?.InnerText ?? "", out double temp))
                    {
                        rsModInfo[device].verticalScale = Math.Min(Math.Max(temp, 0.0), 200.0);
                    }
                }
                catch { rsModInfo[device].verticalScale = StickDeadZoneInfo.DEFAULT_VERTICAL_SCALE; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSDeadZoneType");
                    if (Enum.TryParse(Item?.InnerText ?? "", out StickDeadZoneInfo.DeadZoneType temp))
                    {
                        lsModInfo[device].deadzoneType = temp;
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
                        lsModInfo[device].xAxisDeadInfo.deadZone = temp;
                    }
                    catch { }

                    try
                    {
                        Item = lsAxialDeadElement.SelectSingleNode("DeadZoneY");
                        int.TryParse(Item.InnerText, out int temp);
                        temp = Math.Min(Math.Max(temp, 0), 127);
                        lsModInfo[device].yAxisDeadInfo.deadZone = temp;
                    }
                    catch { }

                    try
                    {
                        Item = lsAxialDeadElement.SelectSingleNode("MaxZoneX");
                        int.TryParse(Item.InnerText, out int temp);
                        lsModInfo[device].xAxisDeadInfo.maxZone = Math.Min(Math.Max(temp, 0), 100);
                    }
                    catch { }

                    try
                    {
                        Item = lsAxialDeadElement.SelectSingleNode("MaxZoneY");
                        int.TryParse(Item.InnerText, out int temp);
                        lsModInfo[device].yAxisDeadInfo.maxZone = Math.Min(Math.Max(temp, 0), 100);
                    }
                    catch { }

                    try
                    {
                        Item = lsAxialDeadElement.SelectSingleNode("AntiDeadZoneX");
                        int.TryParse(Item.InnerText, out int temp);
                        lsModInfo[device].xAxisDeadInfo.antiDeadZone = Math.Min(Math.Max(temp, 0), 100);
                    }
                    catch { }

                    try
                    {
                        Item = lsAxialDeadElement.SelectSingleNode("AntiDeadZoneY");
                        int.TryParse(Item.InnerText, out int temp);
                        lsModInfo[device].yAxisDeadInfo.antiDeadZone = Math.Min(Math.Max(temp, 0), 100);
                    }
                    catch { }

                    try
                    {
                        Item = lsAxialDeadElement.SelectSingleNode("MaxOutputX");
                        double.TryParse(Item.InnerText, out double temp);
                        lsModInfo[device].xAxisDeadInfo.maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                    }
                    catch { }

                    try
                    {
                        Item = lsAxialDeadElement.SelectSingleNode("MaxOutputY");
                        double.TryParse(Item.InnerText, out double temp);
                        lsModInfo[device].yAxisDeadInfo.maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
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
                            lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.enabled
                                = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = lsDeltaAccelElement.SelectSingleNode("Multiplier");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.multiplier =
                                Math.Clamp(temp, 0.0, 10.0);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = lsDeltaAccelElement.SelectSingleNode("MaxTravel");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.maxTravel =
                                Math.Clamp(temp, 0.0, 1.0);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = lsDeltaAccelElement.SelectSingleNode("MinTravel");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.minTravel =
                                Math.Clamp(temp, 0.0, 1.0);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = lsDeltaAccelElement.SelectSingleNode("EasingDuration");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.easingDuration =
                                Math.Clamp(temp, 0.0, 600.0);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = lsDeltaAccelElement.SelectSingleNode("MinFactor");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            lsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.minfactor =
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
                        rsModInfo[device].deadzoneType = temp;
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
                        rsModInfo[device].xAxisDeadInfo.deadZone = temp;
                    }
                    catch { }

                    try
                    {
                        Item = rsAxialDeadElement.SelectSingleNode("DeadZoneY");
                        int.TryParse(Item.InnerText, out int temp);
                        temp = Math.Min(Math.Max(temp, 0), 127);
                        rsModInfo[device].yAxisDeadInfo.deadZone = temp;
                    }
                    catch { }

                    try
                    {
                        Item = rsAxialDeadElement.SelectSingleNode("MaxZoneX");
                        int.TryParse(Item.InnerText, out int temp);
                        rsModInfo[device].xAxisDeadInfo.maxZone = Math.Min(Math.Max(temp, 0), 100);
                    }
                    catch { }

                    try
                    {
                        Item = rsAxialDeadElement.SelectSingleNode("MaxZoneY");
                        int.TryParse(Item.InnerText, out int temp);
                        rsModInfo[device].yAxisDeadInfo.maxZone = Math.Min(Math.Max(temp, 0), 100);
                    }
                    catch { }

                    try
                    {
                        Item = rsAxialDeadElement.SelectSingleNode("AntiDeadZoneX");
                        int.TryParse(Item.InnerText, out int temp);
                        rsModInfo[device].xAxisDeadInfo.antiDeadZone = Math.Min(Math.Max(temp, 0), 100);
                    }
                    catch { }

                    try
                    {
                        Item = rsAxialDeadElement.SelectSingleNode("AntiDeadZoneY");
                        int.TryParse(Item.InnerText, out int temp);
                        rsModInfo[device].yAxisDeadInfo.antiDeadZone = Math.Min(Math.Max(temp, 0), 100);
                    }
                    catch { }

                    try
                    {
                        Item = rsAxialDeadElement.SelectSingleNode("MaxOutputX");
                        double.TryParse(Item.InnerText, out double temp);
                        rsModInfo[device].xAxisDeadInfo.maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                    }
                    catch { }

                    try
                    {
                        Item = rsAxialDeadElement.SelectSingleNode("MaxOutputY");
                        double.TryParse(Item.InnerText, out double temp);
                        rsModInfo[device].yAxisDeadInfo.maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
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
                            rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.enabled
                                = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = rsDeltaAccelElement.SelectSingleNode("Multiplier");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.multiplier =
                                Math.Clamp(temp, 0.0, 10.0);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = rsDeltaAccelElement.SelectSingleNode("MaxTravel");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.maxTravel =
                                Math.Clamp(temp, 0.0, 1.0);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = rsDeltaAccelElement.SelectSingleNode("MinTravel");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.minTravel =
                                Math.Clamp(temp, 0.0, 1.0);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = rsDeltaAccelElement.SelectSingleNode("EasingDuration");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.easingDuration =
                                Math.Clamp(temp, 0.0, 600.0);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = rsDeltaAccelElement.SelectSingleNode("MinFactor");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            rsOutputSettings[device].outputSettings.controlSettings.deltaAccelSettings.minfactor =
                                Math.Clamp(temp, 1.0, 10.0);
                        }
                    }
                    catch { }
                }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXDeadZone");
                    double.TryParse(Item.InnerText, out SXDeadzone[device]);
                }
                catch { SXDeadzone[device] = DEFAULT_SX_TILT_DEADZONE; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZDeadZone");
                    double.TryParse(Item.InnerText, out SZDeadzone[device]);
                }
                catch { SZDeadzone[device] = DEFAULT_SX_TILT_DEADZONE; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXMaxZone");
                    int temp = 0;
                    int.TryParse(Item.InnerText, out temp);
                    SXMaxzone[device] = Math.Min(Math.Max(temp * 0.01, 0.0), 1.0);
                }
                catch { SXMaxzone[device] = 1.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZMaxZone");
                    int temp = 0;
                    int.TryParse(Item.InnerText, out temp);
                    SZMaxzone[device] = Math.Min(Math.Max(temp * 0.01, 0.0), 1.0);
                }
                catch { SZMaxzone[device] = 1.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXAntiDeadZone");
                    int temp = 0;
                    int.TryParse(Item.InnerText, out temp);
                    SXAntiDeadzone[device] = Math.Min(Math.Max(temp * 0.01, 0.0), 1.0);
                }
                catch { SXAntiDeadzone[device] = 0.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZAntiDeadZone");
                    int temp = 0;
                    int.TryParse(Item.InnerText, out temp);
                    SZAntiDeadzone[device] = Math.Min(Math.Max(temp * 0.01, 0.0), 1.0);
                }
                catch { SZAntiDeadzone[device] = 0.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/Sensitivity");
                    string[] s = Item.InnerText.Split('|');
                    if (s.Length == 1)
                        s = Item.InnerText.Split(',');

                    if (double.TryParse(s[0], out LSSens[device]))
                    {
                        LSSens[device] = Math.Clamp(LSSens[device], 0.1, 5.0);
                    }
                    else
                    {
                        LSSens[device] = 1;
                    }

                    if (double.TryParse(s[1], out RSSens[device]))
                    {
                        RSSens[device] = Math.Clamp(RSSens[device], 0.1, 5.0);
                    }
                    else
                    {
                        RSSens[device] = 1;
                    }

                    if (double.TryParse(s[2], out l2Sens[device]))
                    {
                        l2Sens[device] = Math.Clamp(l2Sens[device], 0.1, 10.0);
                    }
                    else
                    {
                        l2Sens[device] = 1;
                    }

                    if (double.TryParse(s[3], out r2Sens[device]))
                    {
                        r2Sens[device] = Math.Clamp(r2Sens[device], 0.1, 10.0);
                    }
                    else
                    {
                        r2Sens[device] = 1;
                    }

                    if (double.TryParse(s[4], out SXSens[device]))
                    {
                        SXSens[device] = Math.Clamp(SXSens[device], 0.0, 5.0);
                    }
                    else
                    {
                        SXSens[device] = 1;
                    }

                    if (double.TryParse(s[5], out SZSens[device]))
                    {
                        SZSens[device] = Math.Clamp(SZSens[device], 0.0, 5.0);
                    }
                    else
                    {
                        SZSens[device] = 1;
                    }
                }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ChargingType"); int.TryParse(Item.InnerText, out lightInfo.chargingType); }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/MouseAcceleration"); bool.TryParse(Item.InnerText, out buttonMouseInfos[device].mouseAccel); }
                catch { missingSetting = true; }

                int shiftM = 0;
                if (m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftModifier") != null)
                    int.TryParse(m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftModifier").InnerText, out shiftM);

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LaunchProgram");
                    launchProgram[device] = Item.InnerText;
                }
                catch { launchProgram[device] = string.Empty; missingSetting = true; }

                if (launchprogram == true && launchProgram[device] != string.Empty)
                {
                    string programPath = launchProgram[device];
                    bool procFound = false;
                    bool handled = false;
                    try
                    {
                        var inspector = ProcessInspector;
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
                    bool.TryParse(Item.InnerText, out dinputOnly[device]);
                }
                catch { dinputOnly[device] = false; missingSetting = true; }

                bool oldUseDInputOnly = Global.useDInputOnly[device];

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/StartTouchpadOff");
                    bool.TryParse(Item.InnerText, out startTouchpadOff[device]);
                    if (startTouchpadOff[device] == true) control.StartTPOff(device);
                }
                catch { startTouchpadOff[device] = false; missingSetting = true; }

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
                            if (temp) touchOutMode[device] = TouchpadOutMode.Controls;
                        }
                    }
                    catch { touchOutMode[device] = TouchpadOutMode.Mouse; }
                }

                // Fallback lookup if GyroOutMode is not set
                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/UseSAforMouse");
                    if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                    {
                        if (temp) gyroOutMode[device] = GyroOutMode.Mouse;
                    }
                }
                catch { gyroOutMode[device] = GyroOutMode.Controls; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SATriggers"); sATriggers[device] = Item.InnerText; }
                catch { sATriggers[device] = "-1"; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SATriggerCond"); sATriggerCond[device] = SaTriggerCondValue(Item.InnerText); }
                catch { sATriggerCond[device] = true; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelEmulationAxis"); SASteeringWheelEmulationAxisType.TryParse(Item.InnerText, out sASteeringWheelEmulationAxis[device]); }
                catch { sASteeringWheelEmulationAxis[device] = SASteeringWheelEmulationAxisType.None; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelEmulationRange"); int.TryParse(Item.InnerText, out sASteeringWheelEmulationRange[device]); }
                catch { sASteeringWheelEmulationRange[device] = 360; missingSetting = true; }

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
                        wheelSmoothInfo[device].Enabled = temp;
                    }
                    catch { wheelSmoothInfo[device].Reset(); missingSetting = true; }

                    try
                    {
                        Item = xmlSASteeringWheelSmoothElement.SelectSingleNode("SASteeringWheelSmoothMinCutoff");
                        //Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelSmoothMinCutoff");
                        double.TryParse(Item.InnerText, out double temp);
                        wheelSmoothInfo[device].MinCutoff = temp;
                    }
                    catch { wheelSmoothInfo[device].MinCutoff = OneEuroFilterPair.DEFAULT_WHEEL_CUTOFF; missingSetting = true; }

                    try
                    {
                        Item = xmlSASteeringWheelSmoothElement.SelectSingleNode("SASteeringWheelSmoothBeta");
                        //Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelSmoothBeta");
                        double.TryParse(Item.InnerText, out double temp);
                        wheelSmoothInfo[device].Beta = temp;
                    }
                    catch { wheelSmoothInfo[device].Beta = OneEuroFilterPair.DEFAULT_WHEEL_BETA; missingSetting = true; }
                }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SASteeringWheelFuzz");
                    int.TryParse(Item.InnerText, out int temp);
                    saWheelFuzzValues[device] = temp >= 0 && temp <= 100 ? temp : 0;
                }
                catch { saWheelFuzzValues[device] = 0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroOutputMode");
                    string tempMode = Item.InnerText;
                    //gyroOutMode[device] = GetGyroOutModeType(tempMode);
                    Enum.TryParse(tempMode, out gyroOutMode[device]);
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
                            gyroControlsInf[device].triggers = Item.InnerText;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlGyroControlsElement.SelectSingleNode("TriggerCond");
                        if (Item != null)
                        {
                            gyroControlsInf[device].triggerCond = SaTriggerCondValue(Item.InnerText);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlGyroControlsElement.SelectSingleNode("TriggerTurns");
                        if (bool.TryParse(Item?.InnerText ?? "", out bool tempTurns))
                        {
                            gyroControlsInf[device].triggerTurns = tempTurns;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlGyroControlsElement.SelectSingleNode("Toggle");
                        if (bool.TryParse(Item?.InnerText ?? "", out bool tempToggle))
                        {
                            SetGyroControlsToggle(device, tempToggle, control);
                        }
                    }
                    catch { }
                }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickTriggers"); sAMouseStickTriggers[device] = Item.InnerText; }
                catch { sAMouseStickTriggers[device] = "-1"; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickTriggerCond"); sAMouseStickTriggerCond[device] = SaTriggerCondValue(Item.InnerText); }
                catch { sAMouseStickTriggerCond[device] = true; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickTriggerTurns"); bool.TryParse(Item.InnerText, out gyroMouseStickTriggerTurns[device]); }
                catch { gyroMouseStickTriggerTurns[device] = true; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickHAxis"); int temp = 0; int.TryParse(Item.InnerText, out temp); gyroMouseStickHorizontalAxis[device] = Math.Min(Math.Max(0, temp), 1); }
                catch { gyroMouseStickHorizontalAxis[device] = 0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickDeadZone"); int.TryParse(Item.InnerText, out int temp);
                    gyroMStickInfo[device].deadZone = temp;
                }
                catch { gyroMStickInfo[device].deadZone = 30; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickMaxZone"); int.TryParse(Item.InnerText, out int temp);
                    gyroMStickInfo[device].maxZone = Math.Max(temp, 1);
                }
                catch { gyroMStickInfo[device].maxZone = 830; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickOutputStick");
                    if (Enum.TryParse(Item?.InnerText ?? "", out GyroMouseStickInfo.OutputStick temp))
                    {
                        gyroMStickInfo[device].outputStick = temp;
                    }
                }
                catch { }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickOutputStickAxes");
                    if (Enum.TryParse(Item?.InnerText ?? "", out GyroMouseStickInfo.OutputStickAxes temp))
                    {
                        gyroMStickInfo[device].outputStickDir = temp;
                    }
                }
                catch { }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickAntiDeadX"); double.TryParse(Item.InnerText, out double temp);
                    gyroMStickInfo[device].antiDeadX = temp;
                }
                catch { gyroMStickInfo[device].antiDeadX = 0.4; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickAntiDeadY"); double.TryParse(Item.InnerText, out double temp);
                    gyroMStickInfo[device].antiDeadY = temp;
                }
                catch { gyroMStickInfo[device].antiDeadY = 0.4; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickInvert"); uint.TryParse(Item.InnerText, out gyroMStickInfo[device].inverted); }
                catch { gyroMStickInfo[device].inverted = 0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickToggle");
                    bool.TryParse(Item.InnerText, out bool temp);
                    SetGyroMouseStickToggle(device, temp, control);
                }
                catch
                {
                    SetGyroMouseStickToggle(device, false, control);
                    missingSetting = true;
                }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickMaxOutput"); double temp = 100.0;
                    temp = double.Parse(Item.InnerText);
                    gyroMStickInfo[device].maxOutput = Math.Min(Math.Max(temp, 0.0), 100.0);
                }
                catch { gyroMStickInfo[device].maxOutput = 100.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickMaxOutputEnabled");
                    bool.TryParse(Item.InnerText, out bool temp);
                    gyroMStickInfo[device].maxOutputEnabled = temp;
                }
                catch { gyroMStickInfo[device].maxOutputEnabled = false; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickVerticalScale"); int.TryParse(Item.InnerText, out gyroMStickInfo[device].vertScale); }
                catch { gyroMStickInfo[device].vertScale = 100; missingSetting = true; }

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
                        gyroMStickInfo[device].useSmoothing = tempSmoothing;
                    }
                    catch { gyroMStickInfo[device].useSmoothing = false; missingSetting = true; }

                    try
                    {
                        Item = xmlGyroMStickSmoothingElement.SelectSingleNode("SmoothingMethod");
                        string temp = Item?.InnerText ?? GyroMouseStickInfo.DEFAULT_SMOOTH_TECHNIQUE;
                        gyroMStickInfo[device].DetermineSmoothMethod(temp);
                    }
                    catch { gyroMStickInfo[device].ResetSmoothing(); missingSetting = true; }

                    try
                    {
                        Item = xmlGyroMStickSmoothingElement.SelectSingleNode("SmoothingWeight");
                        int temp = 0; int.TryParse(Item.InnerText, out temp);
                        gyroMStickInfo[device].smoothWeight = Math.Min(Math.Max(0.0, Convert.ToDouble(temp * 0.01)), 1.0);
                    }
                    catch { gyroMStickInfo[device].smoothWeight = 0.5; missingSetting = true; }

                    try
                    {
                        Item = xmlGyroMStickSmoothingElement.SelectSingleNode("SmoothingMinCutoff");
                        double.TryParse(Item.InnerText, out double temp);
                        gyroMStickInfo[device].minCutoff = Math.Min(Math.Max(0.0, temp), 100.0);
                    }
                    catch { gyroMStickInfo[device].minCutoff = GyroMouseStickInfo.DEFAULT_MINCUTOFF; missingSetting = true; }

                    try
                    {
                        Item = xmlGyroMStickSmoothingElement.SelectSingleNode("SmoothingBeta");
                        double.TryParse(Item.InnerText, out double temp);
                        gyroMStickInfo[device].beta = Math.Min(Math.Max(0.0, temp), 1.0);
                    }
                    catch { gyroMStickInfo[device].beta = GyroMouseStickInfo.DEFAULT_BETA; missingSetting = true; }
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
                            gyroSwipeInfo[device].deadzoneX = tempDead;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlGyroSwipeElement.SelectSingleNode("DeadZoneY");
                        if (int.TryParse(Item?.InnerText ?? "", out int tempDead))
                        {
                            gyroSwipeInfo[device].deadzoneY = tempDead;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlGyroSwipeElement.SelectSingleNode("Triggers");
                        if (Item != null)
                        {
                            gyroSwipeInfo[device].triggers = Item.InnerText;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlGyroSwipeElement.SelectSingleNode("TriggerCond");
                        if (Item != null)
                        {
                            gyroSwipeInfo[device].triggerCond = SaTriggerCondValue(Item.InnerText);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlGyroSwipeElement.SelectSingleNode("TriggerTurns");
                        if (bool.TryParse(Item?.InnerText ?? "", out bool tempTurns))
                        {
                            gyroSwipeInfo[device].triggerTurns = tempTurns;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlGyroSwipeElement.SelectSingleNode("XAxis");
                        if (Enum.TryParse(Item?.InnerText ?? "", out GyroDirectionalSwipeInfo.XAxisSwipe tempX))
                        {
                            gyroSwipeInfo[device].xAxis = tempX;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlGyroSwipeElement.SelectSingleNode("DelayTime");
                        if (int.TryParse(Item?.InnerText ?? "", out int tempDelay))
                        {
                            gyroSwipeInfo[device].delayTime = tempDelay;
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
                        Enum.TryParse(tempMode, out touchOutMode[device]);
                    }
                    catch { touchOutMode[device] = TouchpadOutMode.Mouse; missingSetting = true; }
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

                    touchDisInvertTriggers[device] = tempIntList.ToArray();
                }
                catch { touchDisInvertTriggers[device] = new int[1] { -1 }; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroSensitivity"); int.TryParse(Item.InnerText, out gyroSensitivity[device]); }
                catch { gyroSensitivity[device] = 100; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroSensVerticalScale"); int.TryParse(Item.InnerText, out gyroSensVerticalScale[device]); }
                catch { gyroSensVerticalScale[device] = 100; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroInvert"); int.TryParse(Item.InnerText, out gyroInvert[device]); }
                catch { gyroInvert[device] = 0; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroTriggerTurns"); bool.TryParse(Item.InnerText, out gyroTriggerTurns[device]); }
                catch { gyroTriggerTurns[device] = true; missingSetting = true; }

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
                        gyroMouseInfo[device].enableSmoothing = temp;
                    }
                    catch { gyroMouseInfo[device].enableSmoothing = false; missingSetting = true; }

                    try
                    {
                        Item = xmlGyroSmoothingElement.SelectSingleNode("SmoothingMethod");
                        string temp = Item?.InnerText ?? GyroMouseInfo.DEFAULT_SMOOTH_TECHNIQUE;
                        gyroMouseInfo[device].DetermineSmoothMethod(temp);
                    }
                    catch { gyroMouseInfo[device].ResetSmoothing(); missingSetting = true; }

                    try
                    {
                        Item = xmlGyroSmoothingElement.SelectSingleNode("SmoothingWeight");
                        int.TryParse(Item.InnerText, out int temp);
                        gyroMouseInfo[device].smoothingWeight = Math.Min(Math.Max(0.0, Convert.ToDouble(temp * 0.01)), 1.0);
                    }
                    catch { gyroMouseInfo[device].smoothingWeight = 0.5; missingSetting = true; }

                    try
                    {
                        Item = xmlGyroSmoothingElement.SelectSingleNode("SmoothingMinCutoff");
                        double.TryParse(Item.InnerText, out double temp);
                        gyroMouseInfo[device].minCutoff = Math.Min(Math.Max(0.0, temp), 100.0);
                    }
                    catch { gyroMouseInfo[device].minCutoff = GyroMouseInfo.DEFAULT_MINCUTOFF; missingSetting = true; }

                    try
                    {
                        Item = xmlGyroSmoothingElement.SelectSingleNode("SmoothingBeta");
                        double.TryParse(Item.InnerText, out double temp);
                        gyroMouseInfo[device].beta = Math.Min(Math.Max(0.0, temp), 1.0);
                    }
                    catch { gyroMouseInfo[device].beta = GyroMouseInfo.DEFAULT_BETA; missingSetting = true; }
                }
                else
                {
                    missingSetting = true;
                }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseHAxis"); int temp = 0; int.TryParse(Item.InnerText, out temp); gyroMouseHorizontalAxis[device] = Math.Min(Math.Max(0, temp), 1); }
                catch { gyroMouseHorizontalAxis[device] = 0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseDeadZone"); int.TryParse(Item.InnerText, out int temp);
                    SetGyroMouseDZ(device, temp, control);
                }
                catch { SetGyroMouseDZ(device, MouseCursor.GYRO_MOUSE_DEADZONE, control); missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseMinThreshold");
                    double.TryParse(Item.InnerText, out double temp);
                    temp = Math.Min(Math.Max(temp, 1.0), 40.0);
                    gyroMouseInfo[device].minThreshold = temp;
                }
                catch { gyroMouseInfo[device].minThreshold = GyroMouseInfo.DEFAULT_MIN_THRESHOLD; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseToggle");
                    bool.TryParse(Item.InnerText, out bool temp);
                    SetGyroMouseToggle(device, temp, control);
                }
                catch
                {
                    SetGyroMouseToggle(device, false, control);
                    missingSetting = true;
                }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/BTPollRate");
                    int temp = 0;
                    int.TryParse(Item.InnerText, out temp);
                    btPollRate[device] = (temp >= 0 && temp <= 16) ? temp : 4;
                }
                catch { btPollRate[device] = 4; missingSetting = true; }

                // Note! xxOutputCurveCustom property needs to be read before xxOutputCurveMode property in case the curveMode is value 6
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOutputCurveCustom"); lsOutBezierCurveObj[device].CustomDefinition = Item.InnerText; }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOutputCurveMode"); setLsOutCurveMode(device, stickOutputCurveId(Item.InnerText)); }
                catch { setLsOutCurveMode(device, 0); missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOutputCurveCustom"); rsOutBezierCurveObj[device].CustomDefinition = Item.InnerText; }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOutputCurveMode"); setRsOutCurveMode(device, stickOutputCurveId(Item.InnerText)); }
                catch { setRsOutCurveMode(device, 0); missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSSquareStick"); bool.TryParse(Item.InnerText, out squStickInfo[device].lsMode); }
                catch { squStickInfo[device].lsMode = false; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SquareStickRoundness"); double.TryParse(Item.InnerText, out squStickInfo[device].lsRoundness); }
                catch { squStickInfo[device].lsRoundness = 5.0; missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SquareRStickRoundness"); double.TryParse(Item.InnerText, out squStickInfo[device].rsRoundness); }
                catch { squStickInfo[device].rsRoundness = 5.0; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSSquareStick"); bool.TryParse(Item.InnerText, out squStickInfo[device].rsMode); }
                catch { squStickInfo[device].rsMode = false; missingSetting = true; }


                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSAntiSnapback"); bool.TryParse(Item.InnerText, out lsAntiSnapbackInfo[device].enabled); }
                catch { lsAntiSnapbackInfo[device].enabled = StickAntiSnapbackInfo.DEFAULT_ENABLED; missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSAntiSnapback"); bool.TryParse(Item.InnerText, out rsAntiSnapbackInfo[device].enabled); }
                catch { rsAntiSnapbackInfo[device].enabled = StickAntiSnapbackInfo.DEFAULT_ENABLED; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSAntiSnapbackDelta"); double.TryParse(Item.InnerText, out lsAntiSnapbackInfo[device].delta); }
                catch { lsAntiSnapbackInfo[device].delta = StickAntiSnapbackInfo.DEFAULT_DELTA; missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSAntiSnapbackDelta"); double.TryParse(Item.InnerText, out rsAntiSnapbackInfo[device].delta); }
                catch { rsAntiSnapbackInfo[device].delta = StickAntiSnapbackInfo.DEFAULT_DELTA; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSAntiSnapbackTimeout"); int.TryParse(Item.InnerText, out lsAntiSnapbackInfo[device].timeout); }
                catch { lsAntiSnapbackInfo[device].timeout = StickAntiSnapbackInfo.DEFAULT_TIMEOUT; missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSAntiSnapbackTimeout"); int.TryParse(Item.InnerText, out rsAntiSnapbackInfo[device].timeout); }
                catch { rsAntiSnapbackInfo[device].timeout = StickAntiSnapbackInfo.DEFAULT_TIMEOUT; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSOutputMode"); Enum.TryParse(Item.InnerText, out lsOutputSettings[device].mode); }
                catch { missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSOutputMode"); Enum.TryParse(Item.InnerText, out rsOutputSettings[device].mode); }
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
                            lsOutputSettings[device].outputSettings.flickSettings.realWorldCalibration = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlFlickStickLSElement.SelectSingleNode("FlickThreshold");
                            double.TryParse(Item.InnerText, out double temp);
                            lsOutputSettings[device].outputSettings.flickSettings.flickThreshold = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlFlickStickLSElement.SelectSingleNode("FlickTime");
                            double.TryParse(Item.InnerText, out double temp);
                            lsOutputSettings[device].outputSettings.flickSettings.flickTime = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlFlickStickLSElement.SelectSingleNode("MinAngleThreshold");
                            double.TryParse(Item.InnerText, out double temp);
                            lsOutputSettings[device].outputSettings.flickSettings.minAngleThreshold = temp;
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
                            rsOutputSettings[device].outputSettings.flickSettings.realWorldCalibration = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlFlickStickRSElement.SelectSingleNode("FlickThreshold");
                            double.TryParse(Item.InnerText, out double temp);
                            rsOutputSettings[device].outputSettings.flickSettings.flickThreshold = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlFlickStickRSElement.SelectSingleNode("FlickTime");
                            double.TryParse(Item.InnerText, out double temp);
                            rsOutputSettings[device].outputSettings.flickSettings.flickTime = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlFlickStickRSElement.SelectSingleNode("MinAngleThreshold");
                            double.TryParse(Item.InnerText, out double temp);
                            rsOutputSettings[device].outputSettings.flickSettings.minAngleThreshold = temp;
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
                            dualSenseRumbleEmulationMode[device] = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlDSRumbleGroupElement.SelectSingleNode("EnableGenericRumbleRescale");
                            bool.TryParse(Item.InnerText, out bool temp);
                            useGenericRumbleRescaleForDualSenses[device] = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlDSRumbleGroupElement.SelectSingleNode("HapticPowerLevel");
                            byte.TryParse(Item.InnerText, out byte temp);
                            dualSenseHapticPowerLevel[device] = temp;
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

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2OutputCurveCustom"); l2OutBezierCurveObj[device].CustomDefinition = Item.InnerText; }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2OutputCurveMode"); setL2OutCurveMode(device, axisOutputCurveId(Item.InnerText)); }
                catch { setL2OutCurveMode(device, 0); missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2TwoStageMode");
                    if (Enum.TryParse(Item?.InnerText, out TwoStageTriggerMode tempMode))
                    {
                        l2OutputSettings[device].TwoStageMode = tempMode;
                    }
                }
                catch { }


                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2HipFireTime");
                    if (int.TryParse(Item?.InnerText, out int temp))
                    {
                        l2OutputSettings[device].hipFireMS = Math.Clamp(temp, 0, 5000);
                    }
                }
                catch { }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2TriggerEffect");
                    if (Enum.TryParse(Item?.InnerText, out InputDevices.TriggerEffects tempMode))
                    {
                        l2OutputSettings[device].TriggerEffect = tempMode;
                    }
                }
                catch { }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2OutputCurveCustom"); r2OutBezierCurveObj[device].CustomDefinition = Item.InnerText; }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2OutputCurveMode"); setR2OutCurveMode(device, axisOutputCurveId(Item.InnerText)); }
                catch { setR2OutCurveMode(device, 0); missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2TwoStageMode");
                    if (Enum.TryParse(Item?.InnerText, out TwoStageTriggerMode tempMode))
                    {
                        r2OutputSettings[device].TwoStageMode = tempMode;
                    }
                }
                catch { }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2HipFireTime");
                    if (int.TryParse(Item?.InnerText, out int temp))
                    {
                        r2OutputSettings[device].hipFireMS = Math.Clamp(temp, 0, 5000);
                    }
                }
                catch { }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2TriggerEffect");
                    if (Enum.TryParse(Item?.InnerText, out InputDevices.TriggerEffects tempMode))
                    {
                        r2OutputSettings[device].TriggerEffect = tempMode;
                    }
                }
                catch { }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXOutputCurveCustom"); sxOutBezierCurveObj[device].CustomDefinition = Item.InnerText; }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SXOutputCurveMode"); setSXOutCurveMode(device, axisOutputCurveId(Item.InnerText)); }
                catch { setSXOutCurveMode(device, 0); missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZOutputCurveCustom"); szOutBezierCurveObj[device].CustomDefinition = Item.InnerText; }
                catch { missingSetting = true; }
                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/SZOutputCurveMode"); setSZOutCurveMode(device, axisOutputCurveId(Item.InnerText)); }
                catch { setSZOutCurveMode(device, 0); missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TrackballMode"); bool.TryParse(Item.InnerText, out trackballMode[device]); }
                catch { trackballMode[device] = false; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TrackballFriction"); double.TryParse(Item.InnerText, out trackballFriction[device]); }
                catch { trackballFriction[device] = 10.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchRelMouseRotation");
                    int.TryParse(Item.InnerText, out int temp);
                    temp = Math.Min(Math.Max(temp, -180), 180);
                    touchpadRelMouse[device].rotation = temp * Math.PI / 180.0;
                }
                catch { touchpadRelMouse[device].rotation = 0.0; missingSetting = true; }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/TouchRelMouseMinThreshold");
                    double.TryParse(Item.InnerText, out double temp);
                    temp = Math.Min(Math.Max(temp, 1.0), 40.0);
                    touchpadRelMouse[device].minThreshold = temp;
                }
                catch { touchpadRelMouse[device].minThreshold = TouchpadRelMouseSettings.DEFAULT_MIN_THRESHOLD; missingSetting = true; }


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
                            touchMStickInfo[device].deadZone = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("MaxZone");
                        if (int.TryParse(Item?.InnerText ?? "", out int temp))
                        {
                            touchMStickInfo[device].maxZone = Math.Max(temp, 1);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("OutputStick");
                        if (Enum.TryParse(Item?.InnerText ?? "",
                            out TouchMouseStickInfo.OutputStick temp))
                        {
                            touchMStickInfo[device].outputStick = temp;
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
                            touchMStickInfo[device].outputStickDir = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("AntiDeadX");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            touchMStickInfo[device].antiDeadX = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("AntiDeadY");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            touchMStickInfo[device].antiDeadY = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("Invert");
                        if (uint.TryParse(Item?.InnerText ?? "",
                            out uint temp))
                        {
                            touchMStickInfo[device].inverted = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("MaxOutput");
                        if (double.TryParse(Item?.InnerText ?? "", out double temp))
                        {
                            touchMStickInfo[device].maxOutput = Math.Clamp(temp, 0.0, 100.0);
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("MaxOutputEnabled");
                        if (bool.TryParse(Item?.InnerText ?? "", out bool temp))
                        {
                            touchMStickInfo[device].maxOutputEnabled = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("VerticalScale");
                        if (int.TryParse(Item?.InnerText ?? "", out int temp))
                        {
                            touchMStickInfo[device].vertScale = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("OutputCurve");
                        if (Enum.TryParse(Item?.InnerText ?? "", out StickOutCurve.Curve temp))
                        {
                            touchMStickInfo[device].outputCurve = temp;
                        }
                    }
                    catch { }

                    try
                    {
                        Item = xmlTouchMStickSmoothingElement.SelectSingleNode("Rotation");
                        if (int.TryParse(Item?.InnerText ?? "", out int temp))
                        {
                            temp = Math.Clamp(temp, -180, 180);
                            touchMStickInfo[device].rotationRad = temp * Math.PI / 180.0;
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
                                touchMStickInfo[device].smoothingMethod = temp;
                            }
                        }
                        catch { }

                        try
                        {
                            Item = xmlTouchMStickSmoothingElement.SelectSingleNode("SmoothingMinCutoff");
                            if (double.TryParse(Item?.InnerText ?? "", out double temp))
                            {
                                touchMStickInfo[device].minCutoff = Math.Clamp(temp, 0.0, 100.0);
                            }
                        }
                        catch { }

                        try
                        {
                            Item = xmlTouchMStickSmoothingElement.SelectSingleNode("SmoothingBeta");
                            if (double.TryParse(Item?.InnerText ?? "", out double temp))
                            {
                                touchMStickInfo[device].beta = Math.Clamp(temp, 0.0, 1.0);
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
                        touchpadButtonMode[device] = tempMode;
                    }
                }
                catch { touchpadButtonMode[device] = TouchButtonActivationMode.Click; missingSetting = true; }

                try { Item = m_Xdoc.SelectSingleNode("/" + rootname + "/OutputContDevice"); outputDevType[device] = OutContDeviceId(Item.InnerText); }
                catch { outputDevType[device] = OutContType.X360; missingSetting = true; }

                // Only change xinput devices under certain conditions. Avoid
                // performing this upon program startup before loading devices.
                if (xinputChange && device < ControlService.CURRENT_DS4_CONTROLLER_LIMIT)
                {
                    CheckOldDevicestatus(device, control, oldContType,
                        out xinputPlug, out xinputStatus);
                }

                try
                {
                    Item = m_Xdoc.SelectSingleNode("/" + rootname + "/ProfileActions");
                    profileActions[device].Clear();
                    if (!string.IsNullOrEmpty(Item.InnerText))
                    {
                        string[] actionNames = Item.InnerText.Split('/');
                        for (int actIndex = 0, actLen = actionNames.Length; actIndex < actLen; actIndex++)
                        {
                            string tempActionName = actionNames[actIndex];
                            if (!profileActions[device].Contains(tempActionName))
                            {
                                profileActions[device].Add(tempActionName);
                            }
                        }
                    }
                }
                catch { profileActions[device].Clear(); missingSetting = true; }

                foreach (DS4ControlSettings dcs in ds4settings[device])
                    dcs.Reset();

                containsCustomAction[device] = false;
                containsCustomExtras[device] = false;
                profileActionCount[device] = profileActions[device].Count;
                profileActionDict[device].Clear();
                profileActionIndexDict[device].Clear();
                foreach (string actionname in profileActions[device])
                {
                    profileActionDict[device][actionname] = Global.GetAction(actionname);
                    profileActionIndexDict[device][actionname] = Global.GetActionIndexOf(actionname);
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
                                UpdateDS4CSetting(device, item.Name, false, getX360ControlsByName(item.InnerText), "", DS4KeyType.None, 0);
                                customMapButtons.Add(getDS4ControlsByName(item.Name), getX360ControlsByName(item.InnerText));
                            }
                        }
                    }

                    ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/Control/Macro");
                    if (ParentItem != null)
                    {
                        foreach (XmlNode item in ParentItem.ChildNodes)
                        {
                            customMapMacros.Add(getDS4ControlsByName(item.Name), item.InnerText);
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
                                UpdateDS4CSetting(device, item.Name, false, keys, "", DS4KeyType.None, 0);
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
                                UpdateDS4CSetting(device, item.Name, false, wvk, "", DS4KeyType.None, 0);
                                customMapKeys.Add(getDS4ControlsByName(item.Name), wvk);
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
                                UpdateDS4CExtra(device, item.Name, false, item.InnerText);
                                customMapExtras.Add(getDS4ControlsByName(item.Name), item.InnerText);
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
                                    UpdateDS4CKeyType(device, item.Name, false, keyType);
                                    customMapKeyTypes.Add(getDS4ControlsByName(item.Name), keyType);
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
                                UpdateDS4CSetting(device, item.Name, true, getX360ControlsByName(item.InnerText), "", DS4KeyType.None, shiftT);
                                shiftCustomMapButtons.Add(getDS4ControlsByName(item.Name), getX360ControlsByName(item.InnerText));
                            }
                        }
                    }

                    ParentItem = m_Xdoc.SelectSingleNode("/" + rootname + "/ShiftControl/Macro");
                    if (ParentItem != null)
                    {
                        foreach (XmlElement item in ParentItem.ChildNodes)
                        {
                            shiftCustomMapMacros.Add(getDS4ControlsByName(item.Name), item.InnerText);
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
                                UpdateDS4CSetting(device, item.Name, true, keys, "", DS4KeyType.None, shiftT);
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
                                    UpdateDS4CSetting(device, item.Name, true, wvk, "", DS4KeyType.None, shiftT);
                                    shiftCustomMapKeys.Add(getDS4ControlsByName(item.Name), wvk);
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
                                    UpdateDS4CExtra(device, item.Name, true, item.InnerText);
                                    shiftCustomMapExtras.Add(getDS4ControlsByName(item.Name), item.InnerText);
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
                                    UpdateDS4CKeyType(device, item.Name, true, keyType);
                                    shiftCustomMapKeyTypes.Add(getDS4ControlsByName(item.Name), keyType);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Loaded = false;
                ResetProfile(device);
                ResetMouseProperties(device, control);

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
                SaveProfile(device, proName);
            }

            if (Loaded)
            {
                CacheProfileCustomsFlags(device);
                buttonMouseInfos[device].activeButtonSensitivity =
                    buttonMouseInfos[device].buttonSensitivity;

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
                if (postLoad && device < Global.MAX_DS4_CONTROLLER_COUNT)
                {
                    PostLoadSnippet(device, control, xinputStatus, xinputPlug);
                }
            }

            return Loaded;
        }

        // Legacy Load implementation (formerly LoadOld) will be the canonical Load method below.

        public bool Load()
        {
            bool Loaded = true;
            bool missingSetting = false;

            try
            {
                if (File.Exists(m_Profile))
                {
                    // First try DTO-based deserialization. Support both <AppSettingsDTO> and
                    // <Profile> roots by attempting XmlSerializer with a Profile root
                    // then falling back to the default DTO root.
                    string fileXml = File.ReadAllText(m_Profile);
                    bool dtoLoaded = false;
                    try
                    {
                        AppSettingsDTO dto = null;
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
                            dto.MapTo(this);
                            dtoLoaded = true;
                        }
                    }
                    catch { /* fall through to legacy loader below */ }

                    if (dtoLoaded)
                    {
                        // Phase7-Step1 W3（不具合 B の是正）: 旧形式の経路と同じ後処理を行う。
                        // これまでは「Post processing same as legacy path」とコメントされていたが、実際には何も実行されず、
                        // 偽の exe 名・スキップしたバージョンの数値・ポートの範囲・Distance プロファイルの判定が欠落していた
                        // （105575a7 〜。移行前から続く不具合で、回帰ではない）。
                        PostProcessLoad();
                        LoadFakeExeName();
                        Loaded = true;
                        return Loaded;
                    }

                    // Fall back to legacy DOM-based loader
                    XmlNode Item;

                    m_Xdoc.Load(m_Profile);

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/useExclusiveMode"); Boolean.TryParse(Item.InnerText, out useExclusiveMode); } // Ex Mode
                    catch { missingSetting = true; } // Ex Mode
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/startMinimized"); Boolean.TryParse(Item.InnerText, out startMinimized); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/minimizeToTaskbar"); Boolean.TryParse(Item.InnerText, out minToTaskbar); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/formWidth"); Int32.TryParse(Item.InnerText, out formWidth); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/formHeight"); Int32.TryParse(Item.InnerText, out formHeight); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/formLocationX"); Int32.TryParse(Item.InnerText, out formLocationX); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/formLocationY"); Int32.TryParse(Item.InnerText, out formLocationY); }
                    catch { missingSetting = true; }

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/profileEditorLeftWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out profileEditorLeftWidth); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/profileEditorRightWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out profileEditorRightWidth); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/specialActionNameColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out specialActionNameColWidth); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/specialActionTriggerColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out specialActionTriggerColWidth); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/specialActionDetailColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out specialActionDetailColWidth); }
                    catch { missingSetting = true; }

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerIndexColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out controllerIndexColWidth); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerIdColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out controllerIdColWidth); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerStatusColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out controllerStatusColWidth); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerExclusiveColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out controllerExclusiveColWidth); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerBatteryColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out controllerBatteryColWidth); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerSelectProfileColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out controllerSelectProfileColWidth); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerEditColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out controllerEditColWidth); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerLinkedProfileColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out controllerLinkedProfileColWidth); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerLinkProfIdColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out controllerLinkProfIdColWidth); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/controllerCustomColorColWidth"); Int32.TryParse(Item?.InnerText ?? string.Empty, out controllerCustomColorColWidth); }
                    catch { missingSetting = true; }

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/logMaxArchiveFiles"); Int32.TryParse(Item?.InnerText ?? string.Empty, out logMaxArchiveFiles); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/logMinLevel"); logMinLevel = Item?.InnerText ?? DEFAULT_LOG_MIN_LEVEL; }
                    catch { missingSetting = true; }

                    for (int i = 0; i < Global.MAX_DS4_CONTROLLER_COUNT; i++)
                    {
                        string contTag = $"/Profile/Controller{i + 1}";
                        try
                        {
                            Item = m_Xdoc.SelectSingleNode(contTag); profilePath[i] = Item?.InnerText ?? string.Empty;
                            if (profilePath[i].ToLower().Contains("distance"))
                            {
                                distanceProfiles[i] = true;
                            }

                            olderProfilePath[i] = profilePath[i];
                        }
                        catch { profilePath[i] = olderProfilePath[i] = string.Empty; distanceProfiles[i] = false; }
                    }

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/LastChecked"); DateTime.TryParse(Item.InnerText, out lastChecked); }
                    catch { missingSetting = true; }
                    // Read new explicit startup-check fields when available. If not present,
                    // fall back to legacy <CheckWhen> value and map it into daily/hourly values.
                    try
                    {
                        XmlNode everyValNode = m_Xdoc.SelectSingleNode("/Profile/CheckEveryValue");
                        XmlNode everyUnitNode = m_Xdoc.SelectSingleNode("/Profile/CheckEveryUnit");
                        if (everyValNode != null)
                        {
                            Int32.TryParse(everyValNode.InnerText, out checkEveryValue);
                            Int32.TryParse(everyUnitNode?.InnerText ?? "1", out checkEveryUnit);
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
                                    checkEveryValue = legacyCheckWhen / 24;
                                    checkEveryUnit = 1; // days
                                }
                                else
                                {
                                    checkEveryValue = legacyCheckWhen;
                                    checkEveryUnit = 0; // hours
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
                            lastVersionCheckedNum = Global.CompileVersionNumberFromString(tempVer);
                            if (lastVersionCheckedNum > 0) lastVersionChecked = tempVer;
                        }
                    }
                    catch { missingSetting = true; }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/Profile/Notifications");
                        if (!int.TryParse(Item.InnerText, out notifications))
                            notifications = (Boolean.Parse(Item.InnerText) ? 2 : 0);
                    }
                    catch { missingSetting = true; }

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/DisconnectBTAtStop"); Boolean.TryParse(Item.InnerText, out disconnectBTAtStop); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/SwipeProfiles"); Boolean.TryParse(Item.InnerText, out swipeProfiles); }
                    catch { missingSetting = true; }
                    //try { Item = m_Xdoc.SelectSingleNode("/Profile/UseDS4ForMapping"); Boolean.TryParse(Item.InnerText, out ds4Mapping); }
                    //catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/QuickCharge"); Boolean.TryParse(Item.InnerText, out quickCharge); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/CloseMinimizes"); Boolean.TryParse(Item.InnerText, out closeMini); }
                    catch { missingSetting = true; }
                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/Profile/UseLang"); useLang = Item.InnerText;
                        AppLogger.LogDebug($"ScpUtil: loaded UseLang='{useLang}' from profile (appdatapath={Global.appdatapath})");
                    }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/DownloadLang"); Boolean.TryParse(Item.InnerText, out downloadLang); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/FlashWhenLate"); Boolean.TryParse(Item.InnerText, out flashWhenLate); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/FlashWhenLateAt"); int.TryParse(Item.InnerText, out flashWhenLateAt); }
                    catch { missingSetting = true; }

                    Item = m_Xdoc.SelectSingleNode("/Profile/AppIcon");
                    bool hasIconChoice = Item != null;
                    if (hasIconChoice)
                    {
                        hasIconChoice = Enum.TryParse(Item.InnerText ?? "", out useIconChoice);
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
                                useIconChoice = temp ? TrayIconChoice.White : TrayIconChoice.Default;
                            }
                        }
                        catch { missingSetting = true; }
                    }

                    try
                    {
                        Item = m_Xdoc.SelectSingleNode("/Profile/AppTheme");
                        string temp = Item.InnerText;
                        Enum.TryParse(temp, out AppThemeChoice choice);
                        useCurrentTheme = choice;
                    }
                    catch { missingSetting = true; useCurrentTheme = AppThemeChoice.Default; }

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/UseOSCServer"); Boolean.TryParse(Item.InnerText, out useOSCServ); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/OSCServerPort"); int temp; int.TryParse(Item.InnerText, out temp); oscServPort = Math.Min(Math.Max(temp, 1024), 65535); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/InterpretingOscMonitoring"); Boolean.TryParse(Item.InnerText, out interpretingOscMonitoring); }
                    catch { missingSetting = true; }

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/UseOSCSender"); Boolean.TryParse(Item.InnerText, out useOSCSend); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/OSCSenderPort"); int temp; int.TryParse(Item.InnerText, out temp); oscSendPort = Math.Min(Math.Max(temp, 1024), 65535); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/OSCSenderAddress"); oscSendAddress = Item.InnerText; }
                    catch { missingSetting = true; }

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/UseUDPServer"); Boolean.TryParse(Item.InnerText, out useUDPServ); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/UDPServerPort"); int temp; int.TryParse(Item.InnerText, out temp); udpServPort = Math.Min(Math.Max(temp, 1024), 65535); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/UDPServerListenAddress"); udpServListenAddress = Item.InnerText; }
                    catch { missingSetting = true; }

                    bool udpServerSmoothingGroup = false;
                    XmlNode xmlUdpServerSmoothElement =
                        m_Xdoc.SelectSingleNode("/Profile/UDPServerSmoothingOptions");
                    udpServerSmoothingGroup = xmlUdpServerSmoothElement != null;
                    if (udpServerSmoothingGroup)
                    {
                        try
                        {
                            Item = xmlUdpServerSmoothElement.SelectSingleNode("UseSmoothing");
                            bool.TryParse(Item.InnerText, out bool temp);
                            useUdpSmoothing = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlUdpServerSmoothElement.SelectSingleNode("UdpSmoothMinCutoff");
                            double.TryParse(Item.InnerText, out double temp);
                            temp = Math.Min(Math.Max(temp, 0.00001), 100.0);
                            udpSmoothingMincutoff = temp;
                        }
                        catch { missingSetting = true; }

                        try
                        {
                            Item = xmlUdpServerSmoothElement.SelectSingleNode("UdpSmoothBeta");
                            double.TryParse(Item.InnerText, out double temp);
                            temp = Math.Min(Math.Max(temp, 0.0), 1.0);
                            udpSmoothingBeta = temp;
                        }
                        catch { missingSetting = true; }
                    }

                    try { Item = m_Xdoc.SelectSingleNode("/Profile/UseCustomSteamFolder"); Boolean.TryParse(Item.InnerText, out useCustomSteamFolder); }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/CustomSteamFolder"); customSteamFolder = Item.InnerText; }
                    catch { missingSetting = true; }
                    try { Item = m_Xdoc.SelectSingleNode("/Profile/AutoProfileRevertDefaultProfile"); Boolean.TryParse(Item.InnerText, out autoProfileRevertDefaultProfile); }
                    catch { missingSetting = true; }

                    XmlNode xmlDeviceOptions = m_Xdoc.SelectSingleNode("/Profile/DeviceOptions");
                    if (xmlDeviceOptions != null)
                    {
                        XmlNode xmlDS4Support = xmlDeviceOptions.SelectSingleNode("DS4SupportSettings");
                        if (xmlDS4Support != null)
                        {
                            try
                            {
                                XmlNode item = xmlDS4Support.SelectSingleNode("Enabled");
                                if (bool.TryParse(item?.InnerText ?? "", out bool temp))
                                {
                                    deviceOptions.DS4DeviceOpts.Enabled = temp;
                                }
                            }
                            catch { }
                        }

                        XmlNode xmlDualSenseSupport = xmlDeviceOptions.SelectSingleNode("DualSenseSupportSettings");
                        if (xmlDualSenseSupport != null)
                        {
                            try
                            {
                                XmlNode item = xmlDualSenseSupport.SelectSingleNode("Enabled");
                                if (bool.TryParse(item?.InnerText ?? "", out bool temp))
                                {
                                    deviceOptions.DualSenseOpts.Enabled = temp;
                                }
                            }
                            catch { }
                        }

                        XmlNode xmlSwitchProSupport = xmlDeviceOptions.SelectSingleNode("SwitchProSupportSettings");
                        if (xmlSwitchProSupport != null)
                        {
                            try
                            {
                                XmlNode item = xmlSwitchProSupport.SelectSingleNode("Enabled");
                                if (bool.TryParse(item?.InnerText ?? "", out bool temp))
                                {
                                    deviceOptions.SwitchProDeviceOpts.Enabled = temp;
                                }
                            }
                            catch { }
                        }

                        XmlNode xmlJoyConSupport = xmlDeviceOptions.SelectSingleNode("JoyConSupportSettings");
                        if (xmlJoyConSupport != null)
                        {
                            try
                            {
                                XmlNode item = xmlJoyConSupport.SelectSingleNode("Enabled");
                                if (bool.TryParse(item?.InnerText ?? "", out bool temp))
                                {
                                    deviceOptions.JoyConDeviceOpts.Enabled = temp;
                                }
                            }
                            catch { }

                            try
                            {
                                XmlNode item = xmlJoyConSupport.SelectSingleNode("LinkMode");
                                if (Enum.TryParse(item?.InnerText ?? "", out JoyConDeviceOptions.LinkMode temp))
                                {
                                    deviceOptions.JoyConDeviceOpts.LinkedMode = temp;
                                }
                            }
                            catch { }

                            try
                            {
                                XmlNode item = xmlJoyConSupport.SelectSingleNode("JoinedGyroProvider");
                                if (Enum.TryParse(item?.InnerText ?? "", out JoyConDeviceOptions.JoinedGyroProvider temp))
                                {
                                    deviceOptions.JoyConDeviceOpts.JoinGyroProv = temp;
                                }
                            }
                            catch { }
                        }
                    }

                    for (int i = 0; i < Global.MAX_DS4_CONTROLLER_COUNT; i++)
                    {
                        try
                        {
                            Item = m_Xdoc.SelectSingleNode("/Profile/CustomLed" + (i + 1));
                            string[] ss = Item.InnerText.Split(':');
                            bool.TryParse(ss[0], out lightbarSettingInfo[i].ds4winSettings.useCustomLed);
                            DS4Color.TryParse(ss[1], ref lightbarSettingInfo[i].ds4winSettings.m_CustomLed);
                        }
                        catch { lightbarSettingInfo[i].ds4winSettings.useCustomLed = false; lightbarSettingInfo[i].ds4winSettings.m_CustomLed = new DS4Color(Color.Blue); missingSetting = true; }
                    }
                }
            }
            catch { }

            if (missingSetting)
            {
                // Do not auto-save changes discovered during legacy load. Auto-saving
                // at startup may overwrite files and cause conflicts (file locks).
                AppLogger.LogToGui("Profiles.xml is missing some settings; not auto-saving to avoid overwriting existing file.", false);
            }

            if (Loaded)
            {
                LoadFakeExeName();
            }

            return Loaded;
        }

        // Phase7-Step1 W3: Load() の末尾にあった偽の exe 名の読み込みを切り出した（旧形式の経路と DTO の経路の両方から呼ぶ。処理は同一）
        private void LoadFakeExeName()
        {
            string custom_exe_name_path = Path.Combine(Global.exedirpath, Global.CUSTOM_EXE_CONFIG_FILENAME);
            bool fakeExeFileExists = File.Exists(custom_exe_name_path);
            if (fakeExeFileExists)
            {
                string fake_exe_name = File.ReadAllText(custom_exe_name_path).Trim();
                bool valid = !(fake_exe_name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0);
                if (valid)
                {
                    fakeExeFileName = fake_exe_name;
                }
            }
        }

        public bool Save()
        {
            // Use DTO-based serialization as canonical save format but keep compatibility
            // with older readers by writing a <Profile> root (same as legacy files).
            bool saved = true;
            string output_path = m_Profile;
            string testStr = string.Empty;

            XmlSerializer serializer = new XmlSerializer(typeof(AppSettingsDTO), new XmlRootAttribute("Profile"));
            using (Utf8StringWriter strWriter = new Utf8StringWriter())
            {
                using XmlWriter xmlWriter = XmlWriter.Create(strWriter,
                    new XmlWriterSettings()
                    {
                        Encoding = Encoding.UTF8,
                        Indent = true,
                    });

                // Write header comments
                xmlWriter.WriteComment(String.Format(" Profile Configuration Data. {0} ", DateTime.Now));
                xmlWriter.WriteWhitespace("\r\n");
                xmlWriter.WriteWhitespace("\r\n");

                // Serialize DTO with root <Profile>
                AppSettingsDTO dto = new AppSettingsDTO();
                dto.MapFrom(this);
                serializer.Serialize(xmlWriter, dto,
                    new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty }));
                xmlWriter.Flush();
                xmlWriter.Close();

                testStr = strWriter.ToString();
            }

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
                AppLogger.LogToGui($"Could not save Profiles.xml due to IO error: {ex.Message}", false);
                saved = false;
            }

            // Write fake exe config file if needed (same behavior as legacy save)
            bool adminNeeded = Global.AdminNeeded();
            if (saved &&
                (!adminNeeded || (adminNeeded && Global.IsAdministrator())))
            {
                string custom_exe_name_path = Path.Combine(Global.exedirpath, Global.CUSTOM_EXE_CONFIG_FILENAME);
                bool fakeExeFileExists = File.Exists(custom_exe_name_path);
                if (!string.IsNullOrEmpty(fakeExeFileName) || fakeExeFileExists)
                {
                    File.WriteAllText(custom_exe_name_path, fakeExeFileName);
                }
            }

            return saved;
        }

        public void PostProcessLoad()
        {
            // Check if any set profile names should be considered Distance profiles
            for (int i = 0; i < Global.MAX_DS4_CONTROLLER_COUNT; i++)
            {
                if (profilePath[i] != null && profilePath[i].ToLower().Contains("distance"))
                {
                    distanceProfiles[i] = true;
                }
            }

            // Compile shortcut version number if lastVersionChecked is populated
            if (!string.IsNullOrEmpty(lastVersionChecked))
            {
                lastVersionCheckedNum = Global.CompileVersionNumberFromString(lastVersionChecked);
                if (lastVersionCheckedNum == 0) lastVersionChecked = string.Empty;
            }

            oscServPort = Math.Clamp(oscServPort, 1024, 65535);
            oscSendPort = Math.Clamp(oscSendPort, 1024, 65535);
            udpServPort = Math.Clamp(udpServPort, 1024, 65535);

            udpSmoothingMincutoff = Math.Clamp(udpSmoothingMincutoff, 0.00001, 100.0);
            udpSmoothingBeta = Math.Clamp(udpSmoothingBeta, 0.0, 1.0);
        }

        public static void ParseCustomLedString(string source, LightbarDS4WinInfo destination)
        {
            try
            {
                string[] ss = source.Split(':');
                bool.TryParse(ss[0], out destination.useCustomLed);
                DS4Color.TryParse(ss[1], ref destination.m_CustomLed);
            }
            catch
            {
                destination.useCustomLed = false;
                destination.m_CustomLed = new DS4Color(Color.Blue);
            }
        }

        public static string CompileCustomLedString(LightbarDS4WinInfo ledInfo)
        {
            string result = $"{ledInfo.useCustomLed}:{ledInfo.m_CustomLed.red},{ledInfo.m_CustomLed.green},{ledInfo.m_CustomLed.blue}";
            return result;

        }

        public void PopulateLightbarDS4WinInfo(int index, LightbarDS4WinInfo source)
        {
            if (index < 0 && index > Global.MAX_DS4_CONTROLLER_COUNT)
            {
                throw new ArgumentOutOfRangeException();
            }

            lightbarSettingInfo[index].ds4winSettings.useCustomLed = source.useCustomLed;
            lightbarSettingInfo[index].ds4winSettings.m_CustomLed = source.m_CustomLed;
        }

        public LightbarDS4WinInfo ObtainLightbarDS4WinInfo(int index)
        {
            if (index < 0 && index > Global.MAX_DS4_CONTROLLER_COUNT)
            {
                throw new ArgumentOutOfRangeException();
            }

            return lightbarSettingInfo[index].ds4winSettings;
        }

        private void CreateAction()
        {
            XmlDocument m_Xdoc = new XmlDocument();
            PrepareActionsXml(m_Xdoc);
            m_Xdoc.Save(m_Actions);
        }

        private void PrepareActionsXml(XmlDocument xmlDoc)
        {
            XmlNode Node;

            Node = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", String.Empty);
            xmlDoc.AppendChild(Node);

            Node = xmlDoc.CreateComment(String.Format(" Special Actions Configuration Data. {0} ", DateTime.Now));
            xmlDoc.AppendChild(Node);

            Node = xmlDoc.CreateWhitespace("\r\n");
            xmlDoc.AppendChild(Node);

            Node = xmlDoc.CreateNode(XmlNodeType.Element, "Actions", "");
            xmlDoc.AppendChild(Node);
        }

        public bool SaveActions()
        {
            bool saved = true;

            string output_path = m_Actions;
            string testStr = string.Empty;
            XmlSerializer serializer = new XmlSerializer(typeof(ActionsDTO));
            using (Utf8StringWriter strWriter = new Utf8StringWriter())
            {
                using XmlWriter xmlWriter = XmlWriter.Create(strWriter,
                    new XmlWriterSettings()
                    {
                        Encoding = Encoding.UTF8,
                        Indent = true,
                    });

                // Write header explicitly
                //xmlWriter.WriteStartDocument();
                xmlWriter.WriteComment(String.Format(" Special Actions Configuration Data. {0} ", DateTime.Now));
                xmlWriter.WriteWhitespace("\r\n");
                xmlWriter.WriteWhitespace("\r\n");

                // Write root element and children
                ActionsDTO dto = new ActionsDTO();
                dto.MapFrom(this);
                // Omit xmlns:xsi and xmlns:xsd from output
                serializer.Serialize(xmlWriter, dto,
                    new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty }));
                xmlWriter.Flush();
                xmlWriter.Close();

                testStr = strWriter.ToString();
                //Trace.WriteLine("TEST OUTPUT");
                //Trace.WriteLine(testStr);
            }

            try
            {
                using (StreamWriter sw = new StreamWriter(output_path, false))
                {
                    sw.Write(testStr);
                }
            }
            catch (UnauthorizedAccessException)
            {
                saved = false;
            }

            return saved;
        }

        public void SaveActionNew(string name, string controls, int mode, string details, bool edit, double delayTime = 0.0, string extras = "")
        {
            SpecialAction tempAction = null;

            switch (mode)
            {
                case 1:
                    tempAction = new SpecialAction(name, controls, "Macro", details, extras: extras);
                    break;
                case 2:
                    string[] tempDetails = details.Split("?");
                    //double doub = 0.0;
                    //double.TryParse(tempDetails[1], out doub);
                    tempAction = new SpecialAction(name, controls, "Program", tempDetails[0],
                        delay: delayTime, extras: extras);
                    break;
                case 3:
                    tempAction = new SpecialAction(name, controls, "Profile", details, extras: extras);
                    break;
                case 4:
                    tempAction = new SpecialAction(name, controls, "Key", details, extras: extras);
                    break;
                case 10:
                    // Button mapping for SpecialAction
                    tempAction = new SpecialAction(name, controls, "Button", details, extras: extras);
                    break;
                case 5:
                    tempAction = new SpecialAction(name, controls, "DisconnectBT", details, delayTime);
                    break;
                case 6:
                    tempAction = new SpecialAction(name, controls, "BatteryCheck", details, delayTime);
                    break;
                case 7:
                    tempAction = new SpecialAction(name, controls, "MultiAction", details);
                    break;
                case 8:
                    tempAction = new SpecialAction(name, controls, "SASteeringWheelEmulationCalibrate",
                        details, delayTime);
                    break;
                case 9:
                    tempAction = new SpecialAction(name, controls, "GyroCalibration", details, delayTime);
                    break;
                default:
                    break;
            }

            if (edit)
            {
                int tempIndex = actions.FindIndex(item => item.name == name);
                if (tempIndex != -1 && tempAction != null)
                {
                    actions[tempIndex] = tempAction;
                }
            }
            else if (tempAction != null)
            {
                actions.Add(tempAction);
            }

            SaveActions();
        }

        public bool SaveAction(string name, string controls, int mode, string details, bool edit, string extras = "")
        {
            bool saved = true;
            if (!File.Exists(m_Actions))
                CreateAction();

            try
            {
                m_Xdoc.Load(m_Actions);
            }
            catch (XmlException)
            {
                // XML file has become corrupt. Start from scratch
                AppLogger.LogToGui(DS4WinWPF.Properties.Resources.XMLActionsCorrupt, true);
                m_Xdoc.RemoveAll();
                PrepareActionsXml(m_Xdoc);
            }

            XmlNode Node;

            Node = m_Xdoc.CreateComment(String.Format(" Special Actions Configuration Data. {0} ", DateTime.Now));
            foreach (XmlNode node in m_Xdoc.SelectNodes("//comment()"))
                node.ParentNode.ReplaceChild(Node, node);

            Node = m_Xdoc.SelectSingleNode("Actions");
            XmlElement el = m_Xdoc.CreateElement("Action");
            el.SetAttribute("Name", name);
            el.AppendChild(m_Xdoc.CreateElement("Trigger")).InnerText = controls;
            switch (mode)
            {
                case 1:
                    el.AppendChild(m_Xdoc.CreateElement("Type")).InnerText = "Macro";
                    el.AppendChild(m_Xdoc.CreateElement("Details")).InnerText = details;
                    if (extras != string.Empty)
                        el.AppendChild(m_Xdoc.CreateElement("Extras")).InnerText = extras;
                    break;
                case 2:
                    el.AppendChild(m_Xdoc.CreateElement("Type")).InnerText = "Program";
                    el.AppendChild(m_Xdoc.CreateElement("Details")).InnerText = details.Split('?')[0];
                    el.AppendChild(m_Xdoc.CreateElement("Arguements")).InnerText = extras;
                    el.AppendChild(m_Xdoc.CreateElement("Delay")).InnerText = details.Split('?')[1];
                    break;
                case 3:
                    el.AppendChild(m_Xdoc.CreateElement("Type")).InnerText = "Profile";
                    el.AppendChild(m_Xdoc.CreateElement("Details")).InnerText = details;
                    el.AppendChild(m_Xdoc.CreateElement("UnloadTrigger")).InnerText = extras;
                    break;
                case 4:
                    el.AppendChild(m_Xdoc.CreateElement("Type")).InnerText = "Key";
                    el.AppendChild(m_Xdoc.CreateElement("Details")).InnerText = details;
                    if (!string.IsNullOrEmpty(extras))
                    {
                        string[] exts = extras.Split('\n');
                        el.AppendChild(m_Xdoc.CreateElement("UnloadTrigger")).InnerText = exts.Length > 1 ? exts[1] : string.Empty;
                        // New element name: SwitchMode. Values: "Press" or "Toggle". Keep compatibility with older UnloadStyle.
                        el.AppendChild(m_Xdoc.CreateElement("SwitchMode")).InnerText = exts.Length > 0 ? exts[0] : string.Empty;
                    }
                    break;
                case 10:
                    // Button mapping
                    el.AppendChild(m_Xdoc.CreateElement("Type")).InnerText = "Button";
                    el.AppendChild(m_Xdoc.CreateElement("Details")).InnerText = details;
                    break;
                case 5:
                    el.AppendChild(m_Xdoc.CreateElement("Type")).InnerText = "DisconnectBT";
                    el.AppendChild(m_Xdoc.CreateElement("Details")).InnerText = details;
                    break;
                case 6:
                    el.AppendChild(m_Xdoc.CreateElement("Type")).InnerText = "BatteryCheck";
                    el.AppendChild(m_Xdoc.CreateElement("Details")).InnerText = details;
                    break;
                case 7:
                    el.AppendChild(m_Xdoc.CreateElement("Type")).InnerText = "MultiAction";
                    el.AppendChild(m_Xdoc.CreateElement("Details")).InnerText = details;
                    break;
                case 8:
                    el.AppendChild(m_Xdoc.CreateElement("Type")).InnerText = "SASteeringWheelEmulationCalibrate";
                    el.AppendChild(m_Xdoc.CreateElement("Details")).InnerText = details;
                    break;
            }

            if (edit)
            {
                XmlNode oldxmlprocess = m_Xdoc.SelectSingleNode("/Actions/Action[@Name=\"" + name + "\"]");
                Node.ReplaceChild(el, oldxmlprocess);
            }
            else { Node.AppendChild(el); }

            m_Xdoc.AppendChild(Node);
            try { m_Xdoc.Save(m_Actions); }
            catch { saved = false; }
            LoadActions();
            return saved;
        }

        public void RemoveAction(string name)
        {
            int tempIndex = actions.FindIndex(item => item.name == name);
            if (tempIndex != -1)
            {
                actions.RemoveAt(tempIndex);
            }

            SaveActions();

            //m_Xdoc.Load(m_Actions);
            //XmlNode Node = m_Xdoc.SelectSingleNode("Actions");
            //XmlNode Item = m_Xdoc.SelectSingleNode("/Actions/Action[@Name=\"" + name + "\"]");
            //if (Item != null)
            //    Node.RemoveChild(Item);

            //m_Xdoc.AppendChild(Node);
            //m_Xdoc.Save(m_Actions);
            //LoadActions();
        }

        public bool LoadActions()
        {
            bool loaded = true;

            actions.Clear();
            try { ActionManager.ClearAllEntries(); } catch { }

            //string configFile = Path.Combine(Global.appdatapath, "Actions.xml");
            if (!File.Exists(m_Actions))
            {
                actions.Add(new SpecialAction("Disconnect Controller", "PS/Options", "DisconnectBT", "0"));
                loaded = SaveActions();

                // Reset runtime synthetic state and ActionManager entries after creating defaults
                if (loaded)
                {
                    try
                    {
                        try { ActionManager.ClearAllEntries(); } catch { }
                        Mapping.globalState = new DS4Windows.Mapping.SyntheticState();
                        for (int d = 0; d < Mapping.deviceState.Length; d++)
                        {
                            Mapping.deviceState[d] = new DS4Windows.Mapping.SyntheticState();
                        }
                        try { ActionManager.ClearAllToggledOn(); } catch { }
                        if (Mapping.macrodone != null)
                        {
                            for (int i = 0; i < Mapping.macrodone.Length; i++) Mapping.macrodone[i] = false;
                        }
                        AppLogger.LogToGui("Runtime synthetic state reset after Actions load", false);
                    }
                    catch { }
                }

                return loaded;
            }

            XmlSerializer serializer = new XmlSerializer(typeof(ActionsDTO));
            using StreamReader sr = new StreamReader(m_Actions);
            try
            {
                ActionsDTO dto = serializer.Deserialize(sr) as ActionsDTO;
                dto.MapTo(this);
            }
            catch (InvalidOperationException e)
            {
                AppLogger.LogToGui($"Actions.xml contains invalid data. Could not be read. {e.InnerException.Message}", false);
                loaded = false;
            }
            catch (XmlException e)
            {
                AppLogger.LogToGui($"Actions.xml could not be read. Invalid XML syntax. {e.InnerException.Message}", false);
                loaded = false;
            }

            // After loading actions, clear ActionManager entries and reset runtime state
            if (loaded)
            {
                try
                {
                    try { ActionManager.ClearAllEntries(); } catch { }
                    Mapping.globalState = new DS4Windows.Mapping.SyntheticState();
                    for (int d = 0; d < Mapping.deviceState.Length; d++)
                    {
                        Mapping.deviceState[d] = new DS4Windows.Mapping.SyntheticState();
                    }
                    try { ActionManager.ClearAllToggledOn(); } catch { }
                    if (Mapping.macrodone != null)
                    {
                        for (int i = 0; i < Mapping.macrodone.Length; i++) Mapping.macrodone[i] = false;
                    }
                    AppLogger.LogToGui("Runtime synthetic state reset after Actions load", false);
                }
                catch { }
            }

            return loaded;

            //bool saved = true;
            //if (!File.Exists(Global.appdatapath + "\\Actions.xml"))
            //{
            //    SaveAction("Disconnect Controller", "PS/Options", 5, "0", false);
            //    saved = false;
            //}

            //try
            //{
            //    actions.Clear();
            //    XmlDocument doc = new XmlDocument();
            //    doc.Load(Global.appdatapath + "\\Actions.xml");
            //    XmlNodeList actionslist = doc.SelectNodes("Actions/Action");
            //    string name, controls, type, details, extras, extras2;
            //    Mapping.actionDone.Clear();
            //    foreach (XmlNode x in actionslist)
            //    {
            //        name = x.Attributes["Name"].Value;
            //        controls = x.ChildNodes[0].InnerText;
            //        type = x.ChildNodes[1].InnerText;
            //        details = x.ChildNodes[2].InnerText;
            //        Mapping.actionDone.Add(new Mapping.ActionState());
            //        if (type == "Profile")
            //        {
            //            extras = x.ChildNodes[3].InnerText;
            //            actions.Add(new SpecialAction(name, controls, type, details, 0, extras));
            //        }
            //        else if (type == "Macro")
            //        {
            //            if (x.ChildNodes[3] != null) extras = x.ChildNodes[3].InnerText;
            //            else extras = string.Empty;
            //            actions.Add(new SpecialAction(name, controls, type, details, 0, extras));
            //        }
            //        else if (type == "Key")
            //        {
            //            if (x.ChildNodes[3] != null)
            //            {
            //                extras = x.ChildNodes[3].InnerText;
            //                extras2 = x.ChildNodes[4].InnerText;
            //            }
            //            else
            //            {
            //                extras = string.Empty;
            //                extras2 = string.Empty;
            //            }
            //            if (!string.IsNullOrEmpty(extras))
            //                actions.Add(new SpecialAction(name, controls, type, details, 0, extras2 + '\n' + extras));
            //            else
            //                actions.Add(new SpecialAction(name, controls, type, details));
            //        }
            //        else if (type == "DisconnectBT")
            //        {
            //            double doub;
            //            if (double.TryParse(details, System.Globalization.NumberStyles.Float, Global.configFileDecimalCulture, out doub))
            //                actions.Add(new SpecialAction(name, controls, type, "", doub));
            //            else
            //                actions.Add(new SpecialAction(name, controls, type, ""));
            //        }
            //        else if (type == "BatteryCheck")
            //        {
            //            double doub;
            //            if (double.TryParse(details.Split('|')[0], System.Globalization.NumberStyles.Float, Global.configFileDecimalCulture, out doub))
            //                actions.Add(new SpecialAction(name, controls, type, details, doub));
            //            else if (double.TryParse(details.Split(',')[0], System.Globalization.NumberStyles.Float, Global.configFileDecimalCulture, out doub))
            //                actions.Add(new SpecialAction(name, controls, type, details, doub));
            //            else
            //                actions.Add(new SpecialAction(name, controls, type, details));
            //        }
            //        else if (type == "Program")
            //        {
            //            double doub;
            //            if (x.ChildNodes[3] != null)
            //            {
            //                extras = x.ChildNodes[3].InnerText;
            //                if (double.TryParse(x.ChildNodes[4].InnerText, System.Globalization.NumberStyles.Float, Global.configFileDecimalCulture, out doub))
            //                    actions.Add(new SpecialAction(name, controls, type, details, doub, extras));
            //                else
            //                    actions.Add(new SpecialAction(name, controls, type, details, 0, extras));
            //            }
            //            else
            //            {
            //                actions.Add(new SpecialAction(name, controls, type, details));
            //            }
            //        }
            //        else if (type == "XboxGameDVR" || type == "MultiAction")
            //        {
            //            actions.Add(new SpecialAction(name, controls, type, details));
            //        }
            //        else if (type == "SASteeringWheelEmulationCalibrate")
            //        {
            //            double doub;
            //            if (double.TryParse(details, System.Globalization.NumberStyles.Float, Global.configFileDecimalCulture, out doub))
            //                actions.Add(new SpecialAction(name, controls, type, "", doub));
            //            else
            //                actions.Add(new SpecialAction(name, controls, type, ""));
            //        }
            //    }
            //}
            //catch { saved = false; }
            //return saved;
        }

        public bool createLinkedProfiles()
        {
            bool saved = true;

            string output_path = m_linkedProfiles;
            string testStr = string.Empty;
            XmlSerializer serializer = new XmlSerializer(typeof(LinkedProfilesDTO));
            using (Utf8StringWriter strWriter = new Utf8StringWriter())
            {
                using XmlWriter xmlWriter = XmlWriter.Create(strWriter,
                    new XmlWriterSettings()
                    {
                        Encoding = Encoding.UTF8,
                        Indent = true,
                    });

                // Write header explicitly
                //xmlWriter.WriteStartDocument();
                xmlWriter.WriteComment(string.Format(" Mac Address and Profile Linking Data. {0} ", DateTime.Now));
                xmlWriter.WriteWhitespace("\r\n");
                xmlWriter.WriteWhitespace("\r\n");

                // Write root element and children
                LinkedProfilesDTO dto = new LinkedProfilesDTO();
                dto.MapFrom(this);
                // Omit xmlns:xsi and xmlns:xsd from output
                serializer.Serialize(xmlWriter, dto,
                    new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty }));
                xmlWriter.Flush();
                xmlWriter.Close();

                testStr = strWriter.ToString();
                //Trace.WriteLine("TEST OUTPUT");
                //Trace.WriteLine(testStr);
            }

            try
            {
                using (StreamWriter sw = new StreamWriter(output_path, false))
                {
                    sw.Write(testStr);
                }
            }
            catch (UnauthorizedAccessException)
            {
                AppLogger.LogToGui("Unauthorized Access - Save failed to path: " + m_linkedProfiles, false);
                saved = false;
            }

            return saved;

            //bool saved = true;
            //XmlDocument m_Xdoc = new XmlDocument();
            //XmlNode Node;

            //Node = m_Xdoc.CreateXmlDeclaration("1.0", "utf-8", string.Empty);
            //m_Xdoc.AppendChild(Node);

            //Node = m_Xdoc.CreateComment(string.Format(" Mac Address and Profile Linking Data. {0} ", DateTime.Now));
            //m_Xdoc.AppendChild(Node);

            //Node = m_Xdoc.CreateWhitespace("\r\n");
            //m_Xdoc.AppendChild(Node);

            //Node = m_Xdoc.CreateNode(XmlNodeType.Element, "LinkedControllers", "");
            //m_Xdoc.AppendChild(Node);

            //try { m_Xdoc.Save(m_linkedProfiles); }
            //catch (UnauthorizedAccessException) { AppLogger.LogToGui("Unauthorized Access - Save failed to path: " + m_linkedProfiles, false); saved = false; }

            //return saved;
        }

        public bool LoadLinkedProfiles()
        {
            bool loaded = true;
            if (File.Exists(m_linkedProfiles))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(LinkedProfilesDTO));
                using StreamReader sr = new StreamReader(m_linkedProfiles);
                try
                {
                    LinkedProfilesDTO dto = serializer.Deserialize(sr) as LinkedProfilesDTO;
                    dto.MapTo(this);
                }
                catch (InvalidOperationException e)
                {
                    AppLogger.LogToGui($"LinkedProfiles.xml contains invalid data. Could not be read. {e.InnerException.Message}", false);
                }
                catch (XmlException e)
                {
                    AppLogger.LogToGui($"LinkedProfiles.xml could not be read. Invalid XML syntax. {e.InnerException.Message}", false);
                }
            }
            else
            {
                AppLogger.LogToGui("LinkedProfiles.xml can't be found.", false);
                loaded = false;
            }

            return loaded;

            //bool loaded = true;
            //if (File.Exists(m_linkedProfiles))
            //{
            //    XmlDocument linkedXdoc = new XmlDocument();
            //    XmlNode Node;
            //    linkedXdoc.Load(m_linkedProfiles);
            //    linkedProfiles.Clear();

            //    try
            //    {
            //        Node = linkedXdoc.SelectSingleNode("/LinkedControllers");
            //        XmlNodeList links = Node.ChildNodes;
            //        for (int i = 0, listLen = links.Count; i < listLen; i++)
            //        {
            //            XmlNode current = links[i];
            //            string serial = current.Name.Replace("MAC", string.Empty);
            //            string profile = current.InnerText;
            //            linkedProfiles[serial] = profile;
            //        }
            //    }
            //    catch { loaded = false; }
            //}
            //else
            //{
            //    AppLogger.LogToGui("LinkedProfiles.xml can't be found.", false);
            //    loaded = false;
            //}

            //return loaded;
        }

        public bool SaveLinkedProfiles()
        {
            bool saved = true;

            string output_path = m_linkedProfiles;
            if (File.Exists(m_linkedProfiles))
            {
                string testStr = string.Empty;
                XmlSerializer serializer = new XmlSerializer(typeof(LinkedProfilesDTO));
                using (Utf8StringWriter strWriter = new Utf8StringWriter())
                {
                    using XmlWriter xmlWriter = XmlWriter.Create(strWriter,
                        new XmlWriterSettings()
                        {
                            Encoding = Encoding.UTF8,
                            Indent = true,
                        });

                    // Write header explicitly
                    //xmlWriter.WriteStartDocument();
                    //xmlWriter.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"utf-8\"");
                    xmlWriter.WriteComment(string.Format(" Mac Address and Profile Linking Data. {0} ", DateTime.Now));
                    xmlWriter.WriteWhitespace("\r\n");
                    xmlWriter.WriteWhitespace("\r\n");

                    // Write root element and children
                    LinkedProfilesDTO dto = new LinkedProfilesDTO();
                    dto.MapFrom(this);
                    // Omit xmlns:xsi and xmlns:xsd from output
                    serializer.Serialize(xmlWriter, dto,
                        new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty }));
                    xmlWriter.Flush();
                    xmlWriter.Close();

                    testStr = strWriter.ToString();
                    //Trace.WriteLine("TEST OUTPUT");
                    //Trace.WriteLine(testStr);
                }

                try
                {
                    using (StreamWriter sw = new StreamWriter(output_path, false))
                    {
                        sw.Write(testStr);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    AppLogger.LogToGui("Unauthorized Access - Save failed to path: " + m_linkedProfiles, false);
                    saved = false;
                }
            }
            else
            {
                saved = createLinkedProfiles();
            }

            return saved;

            //bool saved = true;
            //if (File.Exists(m_linkedProfiles))
            //{
            //    XmlDocument linkedXdoc = new XmlDocument();
            //    XmlNode Node;

            //    Node = linkedXdoc.CreateXmlDeclaration("1.0", "utf-8", string.Empty);
            //    linkedXdoc.AppendChild(Node);

            //    Node = linkedXdoc.CreateComment(string.Format(" Mac Address and Profile Linking Data. {0} ", DateTime.Now));
            //    linkedXdoc.AppendChild(Node);

            //    Node = linkedXdoc.CreateWhitespace("\r\n");
            //    linkedXdoc.AppendChild(Node);

            //    Node = linkedXdoc.CreateNode(XmlNodeType.Element, "LinkedControllers", "");
            //    linkedXdoc.AppendChild(Node);

            //    Dictionary<string, string>.KeyCollection serials = linkedProfiles.Keys;
            //    //for (int i = 0, itemCount = linkedProfiles.Count; i < itemCount; i++)
            //    for (var serialEnum = serials.GetEnumerator(); serialEnum.MoveNext();)
            //    {
            //        //string serial = serials.ElementAt(i);
            //        string serial = serialEnum.Current;
            //        string profile = linkedProfiles[serial];
            //        XmlElement link = linkedXdoc.CreateElement("MAC" + serial);
            //        link.InnerText = profile;
            //        Node.AppendChild(link);
            //    }

            //    try { linkedXdoc.Save(m_linkedProfiles); }
            //    catch (UnauthorizedAccessException) { AppLogger.LogToGui("Unauthorized Access - Save failed to path: " + m_linkedProfiles, false); saved = false; }
            //}
            //else
            //{
            //    saved = createLinkedProfiles();
            //    saved = saved && SaveLinkedProfiles();
            //}

            //return saved;
        }

        public bool createControllerConfigs()
        {
            bool saved = true;
            XmlDocument configXdoc = new XmlDocument();
            XmlNode Node;

            Node = configXdoc.CreateXmlDeclaration("1.0", "utf-8", string.Empty);
            configXdoc.AppendChild(Node);

            Node = configXdoc.CreateComment(string.Format(" Controller config data. {0} ", DateTime.Now));
            configXdoc.AppendChild(Node);

            Node = configXdoc.CreateWhitespace("\r\n");
            configXdoc.AppendChild(Node);

            Node = configXdoc.CreateNode(XmlNodeType.Element, "Controllers", "");
            configXdoc.AppendChild(Node);

            try { configXdoc.Save(m_controllerConfigs); }
            catch (UnauthorizedAccessException) { AppLogger.LogToGui("Unauthorized Access - Save failed to path: " + m_controllerConfigs, false); saved = false; }

            return saved;
        }

        public bool LoadControllerConfigsForDevice(DS4Device device)
        {
            bool loaded = false;

            if (device == null) return false;
            if (!File.Exists(m_controllerConfigs)) createControllerConfigs();

            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(m_controllerConfigs);

                XmlNode node = xmlDoc.SelectSingleNode("/Controllers/Controller[@Mac=\"" + device.getMacAddress() + "\"]");
                if (node != null)
                {
                    Int32 intValue;
                    if (Int32.TryParse(node["wheelCenterPoint"]?.InnerText.Split(',')[0] ?? "", out intValue)) device.wheelCenterPoint.X = intValue;
                    if (Int32.TryParse(node["wheelCenterPoint"]?.InnerText.Split(',')[1] ?? "", out intValue)) device.wheelCenterPoint.Y = intValue;
                    if (Int32.TryParse(node["wheel90DegPointLeft"]?.InnerText.Split(',')[0] ?? "", out intValue)) device.wheel90DegPointLeft.X = intValue;
                    if (Int32.TryParse(node["wheel90DegPointLeft"]?.InnerText.Split(',')[1] ?? "", out intValue)) device.wheel90DegPointLeft.Y = intValue;
                    if (Int32.TryParse(node["wheel90DegPointRight"]?.InnerText.Split(',')[0] ?? "", out intValue)) device.wheel90DegPointRight.X = intValue;
                    if (Int32.TryParse(node["wheel90DegPointRight"]?.InnerText.Split(',')[1] ?? "", out intValue)) device.wheel90DegPointRight.Y = intValue;

                    device.optionsStore?.LoadSettings(xmlDoc, node);

                    loaded = true;
                }
            }
            catch
            {
                AppLogger.LogToGui("ControllerConfigs.xml can't be found.", false);
                loaded = false;
            }

            return loaded;
        }

        public bool SaveControllerConfigsForDevice(DS4Device device)
        {
            bool saved = true;

            if (device == null) return false;
            if (!File.Exists(m_controllerConfigs)) createControllerConfigs();

            try
            {
                //XmlNode node = null;
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(m_controllerConfigs);

                XmlNode node = xmlDoc.SelectSingleNode("/Controllers/Controller[@Mac=\"" + device.getMacAddress() + "\"]");
                XmlNode xmlControllersNode = xmlDoc.SelectSingleNode("/Controllers");
                if (node == null)
                {
                    XmlElement el = xmlDoc.CreateElement("Controller");
                    node = xmlControllersNode.AppendChild(el);
                }
                else
                {
                    node.RemoveAll();
                }

                XmlAttribute macAttr = xmlDoc.CreateAttribute("Mac");
                macAttr.Value = device.getMacAddress();
                node.Attributes.Append(macAttr);

                XmlAttribute contTypeAttr = xmlDoc.CreateAttribute("ControllerType");
                contTypeAttr.Value = device.DeviceType.ToString();
                node.Attributes.Append(contTypeAttr);

                if (!device.wheelCenterPoint.IsEmpty)
                {
                    XmlElement wheelCenterEl = xmlDoc.CreateElement("wheelCenterPoint");
                    wheelCenterEl.InnerText = $"{device.wheelCenterPoint.X},{device.wheelCenterPoint.Y}";
                    node.AppendChild(wheelCenterEl);

                    XmlElement wheel90DegPointLeftEl = xmlDoc.CreateElement("wheel90DegPointLeft");
                    wheel90DegPointLeftEl.InnerText = $"{device.wheel90DegPointLeft.X},{device.wheel90DegPointLeft.Y}";
                    node.AppendChild(wheel90DegPointLeftEl);

                    XmlElement wheel90DegPointRightEl = xmlDoc.CreateElement("wheel90DegPointRight");
                    wheel90DegPointRightEl.InnerText = $"{device.wheel90DegPointRight.X},{device.wheel90DegPointRight.Y}";
                    node.AppendChild(wheel90DegPointRightEl);
                }

                device.optionsStore?.PersistSettings(xmlDoc, node);

                // Remove old elements
                xmlDoc.RemoveAll();

                XmlNode Node;
                Node = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", string.Empty);
                xmlDoc.AppendChild(Node);

                Node = xmlDoc.CreateComment(string.Format(" Controller config data. {0} ", DateTime.Now));
                xmlDoc.AppendChild(Node);

                Node = xmlDoc.CreateWhitespace("\r\n");
                xmlDoc.AppendChild(Node);

                // Write old Controllers node back in
                xmlDoc.AppendChild(xmlControllersNode);

                // Save XML to file
                xmlDoc.Save(m_controllerConfigs);
            }
            catch (UnauthorizedAccessException)
            {
                AppLogger.LogToGui("Unauthorized Access - Save failed to path: " + m_controllerConfigs, false);
                saved = false;
            }

            return saved;
        }

        public void UpdateDS4CSetting(int deviceNum, string buttonName, bool shift, object action, string exts, DS4KeyType kt, int trigger = 0)
        {
            DS4Controls dc;
            if (buttonName.StartsWith("bn"))
                dc = getDS4ControlsByName(buttonName);
            else
                dc = (DS4Controls)Enum.Parse(typeof(DS4Controls), buttonName, true);

            int temp = (int)dc;
            if (temp > 0)
            {
                int index = temp - 1;
                DS4ControlSettings dcs = ds4settings[deviceNum][index];
                dcs.UpdateSettings(shift, action, exts, kt, trigger);
                Global.RefreshActionAlias(dcs, shift);
            }
        }

        public void UpdateDS4CExtra(int deviceNum, string buttonName, bool shift, string exts)
        {
            DS4Controls dc;
            if (buttonName.StartsWith("bn"))
                dc = getDS4ControlsByName(buttonName);
            else
                dc = (DS4Controls)Enum.Parse(typeof(DS4Controls), buttonName, true);

            int temp = (int)dc;
            if (temp > 0)
            {
                int index = temp - 1;
                DS4ControlSettings dcs = ds4settings[deviceNum][index];
                if (shift)
                    dcs.shiftExtras = exts;
                else
                    dcs.extras = exts;
            }
        }

        public void UpdateDS4CKeyType(int deviceNum, string buttonName, bool shift, DS4KeyType keyType)
        {
            DS4Controls dc;
            if (buttonName.StartsWith("bn"))
                dc = getDS4ControlsByName(buttonName);
            else
                dc = (DS4Controls)Enum.Parse(typeof(DS4Controls), buttonName, true);

            int temp = (int)dc;
            if (temp > 0)
            {
                int index = temp - 1;
                DS4ControlSettings dcs = ds4settings[deviceNum][index];
                if (shift)
                    dcs.shiftKeyType = keyType;
                else
                    dcs.keyType = keyType;
            }
        }

        public DS4ControlSettings GetDS4CSetting(int deviceNum, string buttonName)
        {
            DS4Controls dc;
            if (buttonName.StartsWith("bn"))
                dc = getDS4ControlsByName(buttonName);
            else
                dc = (DS4Controls)Enum.Parse(typeof(DS4Controls), buttonName, true);

            int temp = (int)dc;
            if (temp > 0)
            {
                int index = temp - 1;
                DS4ControlSettings dcs = ds4settings[deviceNum][index];
                return dcs;
            }

            return null;
        }

        public DS4ControlSettings GetDS4CSetting(int deviceNum, DS4Controls dc)
        {
            int temp = (int)dc;
            if (temp > 0)
            {
                int index = temp - 1;
                DS4ControlSettings dcs = ds4settings[deviceNum][index];
                return dcs;
            }

            return null;
        }

        public bool HasCustomActions(int deviceNum)
        {
            List<DS4ControlSettings> ds4settingsList = ds4settings[deviceNum];
            for (int i = 0, settingsLen = ds4settingsList.Count; i < settingsLen; i++)
            {
                DS4ControlSettings dcs = ds4settingsList[i];
                if (dcs.actionType != DS4ControlSettings.ActionType.Default || dcs.shiftActionType != DS4ControlSettings.ActionType.Default)
                    return true;
            }

            return false;
        }

        public bool HasCustomExtras(int deviceNum)
        {
            List<DS4ControlSettings> ds4settingsList = ds4settings[deviceNum];
            for (int i = 0, settingsLen = ds4settingsList.Count; i < settingsLen; i++)
            {
                DS4ControlSettings dcs = ds4settingsList[i];
                if (dcs.extras != null || dcs.shiftExtras != null)
                    return true;
            }

            return false;
        }

        // Phase6-Step12: 旧形式の保存・読込に欠けていた設定（ProfileDTO にあり、旧形式に無かった 10 要素）の読み書き。
        public void SaveLegacyExtraSettings(XmlNode rootElement, int device)
        {
            AppendLegacyElement(rootElement, "DebouncingMs", debouncingMs[device].ToString());
            AppendLegacyElement(rootElement, "InverseRumbleMotors", inverseRumbleMotors[device].ToString());
            AppendLegacyElement(rootElement, "UseDs3PitchRollSim", useDs3PitchRollSim.ToString());
            AppendLegacyElement(rootElement, "LeftStickDriftXAxis", leftStickDriftXAxis[device].ToString());
            AppendLegacyElement(rootElement, "LeftStickDriftYAxis", leftStickDriftYAxis[device].ToString());
            AppendLegacyElement(rootElement, "RightStickDriftXAxis", rightStickDriftXAxis[device].ToString());
            AppendLegacyElement(rootElement, "RightStickDriftYAxis", rightStickDriftYAxis[device].ToString());
            AppendLegacyElement(rootElement, "DS4OutputTriggerMode", outputDS4TriggerMode[device].ToString());
            AppendLegacyElement(rootElement, "GyroMouseJitterCompensation", gyroMouseInfo[device].jitterCompensation.ToString());
            AppendLegacyElement(rootElement, "GyroMouseStickJitterCompensation", gyroMStickInfo[device].jitterCompensation.ToString());
        }

        private void AppendLegacyElement(XmlNode parent, string name, string value)
        {
            XmlNode node = m_Xdoc.CreateNode(XmlNodeType.Element, name, null);
            node.InnerText = value;
            parent.AppendChild(node);
        }

        private string ReadLegacyElement(string rootname, string name)
        {
            return m_Xdoc.SelectSingleNode("/" + rootname + "/" + name)?.InnerText;
        }

        public void LoadLegacyExtraSettings(int device, string rootname)
        {
            if (int.TryParse(ReadLegacyElement(rootname, "DebouncingMs"), out int debounceMs))
                debouncingMs[device] = Math.Min(Math.Max(debounceMs, 0), 1000);

            if (bool.TryParse(ReadLegacyElement(rootname, "InverseRumbleMotors"), out bool inverseRumble))
                inverseRumbleMotors[device] = inverseRumble;

            // useDs3PitchRollSim は全体で 1 つの値（ProfileDTO.MapTo と同じ扱い）。要素があるときだけ代入する
            if (bool.TryParse(ReadLegacyElement(rootname, "UseDs3PitchRollSim"), out bool ds3Sim))
                useDs3PitchRollSim = ds3Sim;

            if (sbyte.TryParse(ReadLegacyElement(rootname, "LeftStickDriftXAxis"), out sbyte value))
                leftStickDriftXAxis[device] = value;
            if (sbyte.TryParse(ReadLegacyElement(rootname, "LeftStickDriftYAxis"), out value))
                leftStickDriftYAxis[device] = value;
            if (sbyte.TryParse(ReadLegacyElement(rootname, "RightStickDriftXAxis"), out value))
                rightStickDriftXAxis[device] = value;
            if (sbyte.TryParse(ReadLegacyElement(rootname, "RightStickDriftYAxis"), out value))
                rightStickDriftYAxis[device] = value;

            string triggerMode = ReadLegacyElement(rootname, "DS4OutputTriggerMode");
            if (triggerMode != null && Enum.TryParse(triggerMode, out DS4TriggerOutputMode mode) &&
                Enum.IsDefined(typeof(DS4TriggerOutputMode), mode))
            {
                outputDS4TriggerMode[device] = mode;
            }

            if (bool.TryParse(ReadLegacyElement(rootname, "GyroMouseJitterCompensation"), out bool gyroMouseJitter))
                gyroMouseInfo[device].jitterCompensation = gyroMouseJitter;
            if (bool.TryParse(ReadLegacyElement(rootname, "GyroMouseStickJitterCompensation"), out bool gyroStickJitter))
                gyroMStickInfo[device].jitterCompensation = gyroStickJitter;
        }

        private void ResetMouseProperties(int device, ControlService control)
        {
            if (device < Global.MAX_DS4_CONTROLLER_COUNT &&
                    control.touchPad[device] != null)
            {
                control.touchPad[device]?.ResetToggleGyroModes();
            }
        }

        private void ResetProfile(int device)
        {
            buttonMouseInfos[device].Reset();
            gyroControlsInf[device].Reset();

            enableTouchToggle[device] = DEFAULT_TOUCH_TOGGLE;
            idleDisconnectTimeout[device] = 0;
            enableOutputDataToDS4[device] = DEFAULT_OUTPUT_TO_DS4;
            touchpadJitterCompensation[device] = DEFAULT_TOUCHPAD_JITTER_COMP;
            // Phase6-Step12: 旧形式の読込に追加した設定の既定値（要素の無いプロファイルで、前の値を引き継がないようにする）。
            // useDs3PitchRollSim は全体で 1 つの値のため、ここではリセットしない。
            debouncingMs[device] = 0;
            inverseRumbleMotors[device] = false;
            leftStickDriftXAxis[device] = 0;
            leftStickDriftYAxis[device] = 0;
            rightStickDriftXAxis[device] = 0;
            rightStickDriftYAxis[device] = 0;
            outputDS4TriggerMode[device] = DEFAULT_DS4_TRIGGER_OUTPUT;
            lowerRCOn[device] = false;
            touchClickPassthru[device] = false;

            rumble[device] = DEFAULT_RUMBLE;
            rumbleAutostopTime[device] = 0;
            touchSensitivity[device] = DEFAULT_TOUCHPAD_SENS;

            lsModInfo[device].Reset();
            rsModInfo[device].Reset();
            lsModInfo[device].deadZone = rsModInfo[device].deadZone = StickDeadZoneInfo.DEFAULT_DEADZONE;
            lsModInfo[device].antiDeadZone = rsModInfo[device].antiDeadZone = StickDeadZoneInfo.DEFAULT_ANTIDEADZONE;
            lsModInfo[device].maxZone = rsModInfo[device].maxZone = StickDeadZoneInfo.DEFAULT_MAXZONE;
            lsModInfo[device].maxOutput = rsModInfo[device].maxOutput = StickDeadZoneInfo.DEFAULT_MAXOUTPUT;
            lsModInfo[device].fuzz = rsModInfo[device].fuzz = StickDeadZoneInfo.DEFAULT_FUZZ;

            //l2ModInfo[device].deadZone = r2ModInfo[device].deadZone = 0;
            //l2ModInfo[device].antiDeadZone = r2ModInfo[device].antiDeadZone = 0;
            //l2ModInfo[device].maxZone = r2ModInfo[device].maxZone = 100;
            //l2ModInfo[device].maxOutput = r2ModInfo[device].maxOutput = 100.0;
            l2ModInfo[device].Reset();
            r2ModInfo[device].Reset();

            // Rotation angles expressed in radians
            LSRotation[device] = 0.0;
            RSRotation[device] = 0.0;

            SXDeadzone[device] = SZDeadzone[device] = DEFAULT_SX_TILT_DEADZONE;
            SXMaxzone[device] = SZMaxzone[device] = DEFAULT_RUMBLE;
            SXAntiDeadzone[device] = SZAntiDeadzone[device] = 0.0;
            l2Sens[device] = r2Sens[device] = DEFAULT_ANALOG_SENS;
            LSSens[device] = RSSens[device] = DEFAULT_ANALOG_SENS;
            SXSens[device] = SZSens[device] = DEFAULT_ANALOG_SENS;
            tapSensitivity[device] = 0;
            doubleTap[device] = false;
            scrollSensitivity[device] = 0;
            touchpadInvert[device] = 0;
            btPollRate[device] = DEFAULT_DS4_BT_POLL_RATE;

            lsOutputSettings[device].ResetSettings();
            rsOutputSettings[device].ResetSettings();
            l2OutputSettings[device].ResetSettings();
            r2OutputSettings[device].ResetSettings();

            LightbarSettingInfo lightbarSettings = lightbarSettingInfo[device];
            LightbarDS4WinInfo lightInfo = lightbarSettings.ds4winSettings;
            lightbarSettings.Mode = LightbarMode.DS4Win;
            lightInfo.m_LowLed = new DS4Color(Color.Black);
            //m_LowLeds[device] = new DS4Color(Color.Black);

            Color tempColor = Color.Blue;
            switch (device)
            {
                case 0: tempColor = Color.Blue; break;
                case 1: tempColor = Color.Red; break;
                case 2: tempColor = Color.Green; break;
                case 3: tempColor = Color.Pink; break;
                case 4: tempColor = Color.Blue; break;
                case 5: tempColor = Color.Red; break;
                case 6: tempColor = Color.Green; break;
                case 7: tempColor = Color.Pink; break;
                case 8: tempColor = Color.White; break;
                default: tempColor = Color.Blue; break;
            }

            lightInfo.m_Led = new DS4Color(tempColor);
            lightInfo.m_ChargingLed = new DS4Color(Color.Black);
            lightInfo.m_FlashLed = new DS4Color(Color.Black);
            lightInfo.flashAt = 0;
            lightInfo.flashType = 0;
            lightInfo.chargingType = 0;
            lightInfo.rainbow = 0;
            lightInfo.maxRainbowSat = LightbarDS4WinInfo.DEFAULT_MAX_RAINBOW_SAT;
            lightInfo.ledAsBattery = false;

            launchProgram[device] = string.Empty;
            dinputOnly[device] = DEFAULT_DINPUT_ONLY;
            startTouchpadOff[device] = false;
            touchOutMode[device] = DEFAULT_TOUCH_OUT_MODE;
            sATriggers[device] = BackingStore.DEFAULT_SA_TRIGGERS;
            sATriggerCond[device] = DEFAULT_SA_TRIGGER_COND;
            gyroOutMode[device] = DEFAULT_GYRO_OUT_MODE;
            sAMouseStickTriggers[device] = BackingStore.DEFAULT_GYRO_MSTICK_TRIGGERS;
            sAMouseStickTriggerCond[device] = true;

            gyroMStickInfo[device].Reset();
            gyroSwipeInfo[device].Reset();

            gyroMouseStickToggle[device] = false;
            gyroMouseStickTriggerTurns[device] = DEFAULT_GYRO_MSTICK_TRIGGER_TURNS;
            sASteeringWheelEmulationAxis[device] = SASteeringWheelEmulationAxisType.None;
            sASteeringWheelEmulationRange[device] = DEFAULT_SA_WHEEL_EMULATION_RANGE;
            saWheelFuzzValues[device] = 0;
            wheelSmoothInfo[device].Reset();
            touchDisInvertTriggers[device] = new int[1] { DEFAULT_TOUCH_DIS_INVERT_TRIGGER };
            gyroSensitivity[device] = DEFAULT_GYRO_SENS;
            gyroSensVerticalScale[device] = DEFAULT_GYRO_SENS_VERTICAL_SCALE;
            gyroInvert[device] = 0;
            gyroTriggerTurns[device] = DEFAULT_GYRO_TRIGGER_TURNS;
            gyroMouseInfo[device].Reset();

            gyroMouseHorizontalAxis[device] = 0;
            gyroMouseToggle[device] = false;
            //squStickInfo[device].lsMode = false;
            //squStickInfo[device].rsMode = false;
            //squStickInfo[device].lsRoundness = SquareStickInfo.DEFAULT_ROUNDNESS;
            //squStickInfo[device].rsRoundness = SquareStickInfo.DEFAULT_ROUNDNESS;
            squStickInfo[device].Reset();
            lsAntiSnapbackInfo[device].timeout = StickAntiSnapbackInfo.DEFAULT_TIMEOUT;
            lsAntiSnapbackInfo[device].delta = StickAntiSnapbackInfo.DEFAULT_DELTA;
            lsAntiSnapbackInfo[device].enabled = StickAntiSnapbackInfo.DEFAULT_ENABLED;
            setLsOutCurveMode(device, 0);
            setRsOutCurveMode(device, 0);
            setL2OutCurveMode(device, 0);
            setR2OutCurveMode(device, 0);
            setSXOutCurveMode(device, 0);
            setSZOutCurveMode(device, 0);
            trackballMode[device] = DEFAULT_TRACKBALL_MODE;
            trackballFriction[device] = DEFAULT_TRACKBALL_FRICTION;
            touchpadRelMouse[device].Reset();
            touchMStickInfo[device].Reset();
            touchpadButtonMode[device] = TouchButtonActivationMode.Click;
            outputDevType[device] = DEFAULT_OUT_CONT_TYPE;
            ds4Mapping = false;
        }

        private void PrepareBlankingProfile(int device, ControlService control, out bool xinputPlug, out bool xinputStatus, bool xinputChange = true)
        {
            xinputPlug = false;
            xinputStatus = false;

            OutContType oldContType = Global.activeOutDevType[device];

            // Make sure to reset currently set profile values before parsing
            ResetProfile(device);
            ResetMouseProperties(device, control);

            // Only change xinput devices under certain conditions. Avoid
            // performing this upon program startup before loading devices.
            if (xinputChange)
            {
                CheckOldDevicestatus(device, control, oldContType,
                    out xinputPlug, out xinputStatus);
            }

            foreach (DS4ControlSettings dcs in ds4settings[device])
                dcs.Reset();

            profileActions[device].Clear();
            containsCustomAction[device] = false;
            containsCustomExtras[device] = false;
        }

        public void LoadBlankDS4Profile(int device, bool launchprogram, ControlService control,
            string propath = "", bool xinputChange = true, bool postLoad = true)
        {
            PrepareBlankingProfile(device, control, out bool xinputPlug, out bool xinputStatus, xinputChange);

            StickDeadZoneInfo lsInfo = lsModInfo[device];
            lsInfo.deadZone = (int)(0.00 * 127);
            lsInfo.antiDeadZone = 0;
            lsInfo.maxZone = 100;

            StickDeadZoneInfo rsInfo = rsModInfo[device];
            rsInfo.deadZone = (int)(0.00 * 127);
            rsInfo.antiDeadZone = 0;
            rsInfo.maxZone = 100;

            TriggerDeadZoneZInfo l2Info = l2ModInfo[device];
            l2Info.deadZone = (byte)(0.00 * 255);

            TriggerDeadZoneZInfo r2Info = r2ModInfo[device];
            r2Info.deadZone = (byte)(0.00 * 255);

            outputDevType[device] = OutContType.DS4;

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < Global.MAX_DS4_CONTROLLER_COUNT)
            {
                PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }

        public void LoadBlankProfile(int device, bool launchprogram, ControlService control,
            string propath = "", bool xinputChange = true, bool postLoad = true)
        {
            bool xinputPlug = false;
            bool xinputStatus = false;

            OutContType oldContType = Global.activeOutDevType[device];

            // Make sure to reset currently set profile values before parsing
            ResetProfile(device);
            ResetMouseProperties(device, control);

            // Only change xinput devices under certain conditions. Avoid
            // performing this upon program startup before loading devices.
            if (xinputChange)
            {
                CheckOldDevicestatus(device, control, oldContType,
                    out xinputPlug, out xinputStatus);
            }

            foreach (DS4ControlSettings dcs in ds4settings[device])
                dcs.Reset();

            profileActions[device].Clear();
            containsCustomAction[device] = false;
            containsCustomExtras[device] = false;

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < Global.MAX_DS4_CONTROLLER_COUNT)
            {
                PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }

        public void LoadDefaultGamepadGyroProfile(int device, bool launchprogram, ControlService control,
            string propath = "", bool xinputChange = true, bool postLoad = true)
        {
            bool xinputPlug = false;
            bool xinputStatus = false;

            OutContType oldContType = Global.activeOutDevType[device];

            // Make sure to reset currently set profile values before parsing
            ResetProfile(device);
            ResetMouseProperties(device, control);

            // Only change xinput devices under certain conditions. Avoid
            // performing this upon program startup before loading devices.
            if (xinputChange)
            {
                CheckOldDevicestatus(device, control, oldContType,
                    out xinputPlug, out xinputStatus);
            }

            foreach (DS4ControlSettings dcs in ds4settings[device])
                dcs.Reset();

            profileActions[device].Clear();
            containsCustomAction[device] = false;
            containsCustomExtras[device] = false;

            gyroOutMode[device] = GyroOutMode.MouseJoystick;
            sAMouseStickTriggers[device] = "4";
            sAMouseStickTriggerCond[device] = true;
            gyroMouseStickTriggerTurns[device] = false;
            gyroMStickInfo[device].useSmoothing = true;
            gyroMStickInfo[device].smoothingMethod = GyroMouseStickInfo.SmoothingMethod.OneEuro;

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < Global.MAX_DS4_CONTROLLER_COUNT)
            {
                PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }


        public void LoadDefaultDS4GamepadGyroProfile(int device, bool launchprogram, ControlService control,
            string propath = "", bool xinputChange = true, bool postLoad = true)
        {
            PrepareBlankingProfile(device, control, out bool xinputPlug, out bool xinputStatus, xinputChange);

            StickDeadZoneInfo lsInfo = lsModInfo[device];
            lsInfo.deadZone = (int)(0.00 * 127);
            lsInfo.antiDeadZone = 0;
            lsInfo.maxZone = 100;

            StickDeadZoneInfo rsInfo = rsModInfo[device];
            rsInfo.deadZone = (int)(0.00 * 127);
            rsInfo.antiDeadZone = 0;
            rsInfo.maxZone = 100;

            TriggerDeadZoneZInfo l2Info = l2ModInfo[device];
            l2Info.deadZone = (byte)(0.00 * 255);

            TriggerDeadZoneZInfo r2Info = r2ModInfo[device];
            r2Info.deadZone = (byte)(0.00 * 255);

            gyroOutMode[device] = GyroOutMode.MouseJoystick;
            sAMouseStickTriggers[device] = "4";
            sAMouseStickTriggerCond[device] = true;
            gyroMouseStickTriggerTurns[device] = false;
            gyroMStickInfo[device].useSmoothing = true;
            gyroMStickInfo[device].smoothingMethod = GyroMouseStickInfo.SmoothingMethod.OneEuro;

            outputDevType[device] = OutContType.DS4;

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < Global.MAX_DS4_CONTROLLER_COUNT)
            {
                PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }

        public void LoadDefaultMixedGyroMouseProfile(int device, bool launchprogram, ControlService control,
            string propath = "", bool xinputChange = true, bool postLoad = true)
        {
            bool xinputPlug = false;
            bool xinputStatus = false;

            OutContType oldContType = Global.activeOutDevType[device];

            // Make sure to reset currently set profile values before parsing
            ResetProfile(device);
            ResetMouseProperties(device, control);

            // Only change xinput devices under certain conditions. Avoid
            // performing this upon program startup before loading devices.
            if (xinputChange)
            {
                CheckOldDevicestatus(device, control, oldContType,
                    out xinputPlug, out xinputStatus);
            }

            foreach (DS4ControlSettings dcs in ds4settings[device])
                dcs.Reset();

            profileActions[device].Clear();
            containsCustomAction[device] = false;
            containsCustomExtras[device] = false;

            gyroOutMode[device] = GyroOutMode.Mouse;
            sATriggers[device] = "4";
            sATriggerCond[device] = true;
            gyroTriggerTurns[device] = false;
            gyroMouseInfo[device].enableSmoothing = true;
            gyroMouseInfo[device].smoothingMethod = GyroMouseInfo.SmoothingMethod.OneEuro;

            StickDeadZoneInfo rsInfo = rsModInfo[device];
            rsInfo.deadZone = (int)(0.10 * 127);
            rsInfo.antiDeadZone = 0;
            rsInfo.maxZone = 90;

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < Global.MAX_DS4_CONTROLLER_COUNT)
            {
                PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }


        public void LoadDefaultDS4MixedGyroMouseProfile(int device, bool launchprogram, ControlService control,
            string propath = "", bool xinputChange = true, bool postLoad = true)
        {
            PrepareBlankingProfile(device, control, out bool xinputPlug, out bool xinputStatus, xinputChange);

            StickDeadZoneInfo lsInfo = lsModInfo[device];
            lsInfo.deadZone = (int)(0.00 * 127);
            lsInfo.antiDeadZone = 0;
            lsInfo.maxZone = 100;

            StickDeadZoneInfo rsInfo = rsModInfo[device];
            rsInfo.deadZone = (int)(0.10 * 127);
            rsInfo.antiDeadZone = 0;
            rsInfo.maxZone = 100;

            TriggerDeadZoneZInfo l2Info = l2ModInfo[device];
            l2Info.deadZone = (byte)(0.00 * 255);

            TriggerDeadZoneZInfo r2Info = r2ModInfo[device];
            r2Info.deadZone = (byte)(0.00 * 255);

            gyroOutMode[device] = GyroOutMode.Mouse;
            sATriggers[device] = "4";
            sATriggerCond[device] = true;
            gyroTriggerTurns[device] = false;
            gyroMouseInfo[device].enableSmoothing = true;
            gyroMouseInfo[device].smoothingMethod = GyroMouseInfo.SmoothingMethod.OneEuro;

            outputDevType[device] = OutContType.DS4;

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < Global.MAX_DS4_CONTROLLER_COUNT)
            {
                PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }

        public void LoadDefaultDS4MixedControlsProfile(int device, bool launchprogram, ControlService control,
            string propath = "", bool xinputChange = true, bool postLoad = true)
        {
            PrepareBlankingProfile(device, control, out bool xinputPlug, out bool xinputStatus, xinputChange);

            DS4ControlSettings setting = GetDS4CSetting(device, DS4Controls.RYNeg);
            setting.UpdateSettings(false, X360Controls.MouseUp, "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.RYPos);
            setting.UpdateSettings(false, X360Controls.MouseDown, "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.RXNeg);
            setting.UpdateSettings(false, X360Controls.MouseLeft, "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.RXPos);
            setting.UpdateSettings(false, X360Controls.MouseRight, "", DS4KeyType.None);

            StickDeadZoneInfo rsInfo = rsModInfo[device];
            rsInfo.deadZone = (int)(0.035 * 127);
            rsInfo.antiDeadZone = 0;
            rsInfo.maxZone = 90;

            outputDevType[device] = OutContType.DS4;

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < Global.MAX_DS4_CONTROLLER_COUNT)
            {
                PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }

        public void LoadDefaultMixedControlsProfile(int device, bool launchprogram, ControlService control,
            string propath = "", bool xinputChange = true, bool postLoad = true)
        {
            bool xinputPlug = false;
            bool xinputStatus = false;

            OutContType oldContType = Global.activeOutDevType[device];

            // Make sure to reset currently set profile values before parsing
            ResetProfile(device);
            ResetMouseProperties(device, control);

            // Only change xinput devices under certain conditions. Avoid
            // performing this upon program startup before loading devices.
            if (xinputChange)
            {
                CheckOldDevicestatus(device, control, oldContType,
                    out xinputPlug, out xinputStatus);
            }

            foreach (DS4ControlSettings dcs in ds4settings[device])
                dcs.Reset();

            profileActions[device].Clear();
            containsCustomAction[device] = false;
            containsCustomExtras[device] = false;

            DS4ControlSettings setting = GetDS4CSetting(device, DS4Controls.RYNeg);
            setting.UpdateSettings(false, X360Controls.MouseUp, "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.RYPos);
            setting.UpdateSettings(false, X360Controls.MouseDown, "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.RXNeg);
            setting.UpdateSettings(false, X360Controls.MouseLeft, "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.RXPos);
            setting.UpdateSettings(false, X360Controls.MouseRight, "", DS4KeyType.None);

            StickDeadZoneInfo rsInfo = rsModInfo[device];
            rsInfo.deadZone = (int)(0.035 * 127);
            rsInfo.antiDeadZone = 0;
            rsInfo.maxZone = 90;

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < Global.MAX_DS4_CONTROLLER_COUNT)
            {
                PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }

        public void LoadDefaultKBMProfile(int device, bool launchprogram, ControlService control,
            string propath = "", bool xinputChange = true, bool postLoad = true)
        {
            bool xinputPlug = false;
            bool xinputStatus = false;

            OutContType oldContType = Global.activeOutDevType[device];

            // Make sure to reset currently set profile values before parsing
            ResetProfile(device);
            ResetMouseProperties(device, control);

            // Only change xinput devices under certain conditions. Avoid
            // performing this upon program startup before loading devices.
            if (xinputChange)
            {
                CheckOldDevicestatus(device, control, oldContType,
                    out xinputPlug, out xinputStatus);
            }

            foreach (DS4ControlSettings dcs in ds4settings[device])
                dcs.Reset();

            profileActions[device].Clear();
            containsCustomAction[device] = false;
            containsCustomExtras[device] = false;

            StickDeadZoneInfo lsInfo = lsModInfo[device];
            lsInfo.antiDeadZone = 0;

            StickDeadZoneInfo rsInfo = rsModInfo[device];
            rsInfo.deadZone = (int)(0.035 * 127);
            rsInfo.antiDeadZone = 0;
            rsInfo.maxZone = 90;

            TriggerDeadZoneZInfo l2Info = l2ModInfo[device];
            l2Info.deadZone = (byte)(0.20 * 255);

            TriggerDeadZoneZInfo r2Info = r2ModInfo[device];
            r2Info.deadZone = (byte)(0.20 * 255);

            // Flag to unplug virtual controller
            dinputOnly[device] = true;

            DS4ControlSettings setting = GetDS4CSetting(device, DS4Controls.LYNeg);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.W), "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.LXNeg);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.A), "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.LYPos);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.S), "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.LXPos);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.D), "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.L3);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.LeftShift), "", DS4KeyType.None);

            setting = GetDS4CSetting(device, DS4Controls.RYNeg);
            setting.UpdateSettings(false, X360Controls.MouseUp, "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.RYPos);
            setting.UpdateSettings(false, X360Controls.MouseDown, "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.RXNeg);
            setting.UpdateSettings(false, X360Controls.MouseLeft, "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.RXPos);
            setting.UpdateSettings(false, X360Controls.MouseRight, "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.R3);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.LeftCtrl), "", DS4KeyType.None);

            setting = GetDS4CSetting(device, DS4Controls.DpadUp);
            setting.UpdateSettings(false, X360Controls.Unbound, "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.DpadRight);
            setting.UpdateSettings(false, X360Controls.WDOWN, "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.DpadDown);
            setting.UpdateSettings(false, X360Controls.Unbound, "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.DpadLeft);
            setting.UpdateSettings(false, X360Controls.WUP, "", DS4KeyType.None);

            setting = GetDS4CSetting(device, DS4Controls.Cross);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.Space), "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.Square);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.F), "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.Triangle);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.E), "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.Circle);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.C), "", DS4KeyType.None);

            setting = GetDS4CSetting(device, DS4Controls.L1);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.Q), "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.L2);
            setting.UpdateSettings(false, X360Controls.RightMouse, "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.R1);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.R), "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.R2);
            setting.UpdateSettings(false, X360Controls.LeftMouse, "", DS4KeyType.None);

            setting = GetDS4CSetting(device, DS4Controls.Share);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.Tab), "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.Options);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.Escape), "", DS4KeyType.None);

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < Global.MAX_DS4_CONTROLLER_COUNT)
            {
                PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }

        public void LoadDefaultKBMGyroMouseProfile(int device, bool launchprogram, ControlService control,
            string propath = "", bool xinputChange = true, bool postLoad = true)
        {
            bool xinputPlug = false;
            bool xinputStatus = false;

            OutContType oldContType = Global.activeOutDevType[device];

            // Make sure to reset currently set profile values before parsing
            ResetProfile(device);
            ResetMouseProperties(device, control);

            // Only change xinput devices under certain conditions. Avoid
            // performing this upon program startup before loading devices.
            if (xinputChange)
            {
                CheckOldDevicestatus(device, control, oldContType,
                    out xinputPlug, out xinputStatus);
            }

            foreach (DS4ControlSettings dcs in ds4settings[device])
                dcs.Reset();

            profileActions[device].Clear();
            containsCustomAction[device] = false;
            containsCustomExtras[device] = false;

            StickDeadZoneInfo lsInfo = lsModInfo[device];
            lsInfo.antiDeadZone = 0;

            StickDeadZoneInfo rsInfo = rsModInfo[device];
            rsInfo.deadZone = (int)(0.105 * 127);
            rsInfo.antiDeadZone = 0;
            rsInfo.maxZone = 90;

            TriggerDeadZoneZInfo l2Info = l2ModInfo[device];
            l2Info.deadZone = (byte)(0.20 * 255);

            TriggerDeadZoneZInfo r2Info = r2ModInfo[device];
            r2Info.deadZone = (byte)(0.20 * 255);

            gyroOutMode[device] = GyroOutMode.Mouse;
            sATriggers[device] = "4";
            sATriggerCond[device] = true;
            gyroTriggerTurns[device] = false;
            gyroMouseInfo[device].enableSmoothing = true;
            gyroMouseInfo[device].smoothingMethod = GyroMouseInfo.SmoothingMethod.OneEuro;

            // Flag to unplug virtual controller
            dinputOnly[device] = true;

            DS4ControlSettings setting = GetDS4CSetting(device, DS4Controls.LYNeg);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.W), "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.LXNeg);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.A), "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.LYPos);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.S), "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.LXPos);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.D), "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.L3);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.LeftShift), "", DS4KeyType.None);

            setting = GetDS4CSetting(device, DS4Controls.RYNeg);
            setting.UpdateSettings(false, X360Controls.MouseUp, "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.RYPos);
            setting.UpdateSettings(false, X360Controls.MouseDown, "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.RXNeg);
            setting.UpdateSettings(false, X360Controls.MouseLeft, "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.RXPos);
            setting.UpdateSettings(false, X360Controls.MouseRight, "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.R3);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.LeftCtrl), "", DS4KeyType.None);

            setting = GetDS4CSetting(device, DS4Controls.DpadUp);
            setting.UpdateSettings(false, X360Controls.Unbound, "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.DpadRight);
            setting.UpdateSettings(false, X360Controls.WDOWN, "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.DpadDown);
            setting.UpdateSettings(false, X360Controls.Unbound, "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.DpadLeft);
            setting.UpdateSettings(false, X360Controls.WUP, "", DS4KeyType.None);

            setting = GetDS4CSetting(device, DS4Controls.Cross);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.Space), "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.Square);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.F), "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.Triangle);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.E), "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.Circle);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.C), "", DS4KeyType.None);

            setting = GetDS4CSetting(device, DS4Controls.L1);
            //setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.Q), "", DS4KeyType.None);
            setting.UpdateSettings(false, X360Controls.Unbound, "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.L2);
            setting.UpdateSettings(false, X360Controls.RightMouse, "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.R1);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.R), "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.R2);
            setting.UpdateSettings(false, X360Controls.LeftMouse, "", DS4KeyType.None);

            setting = GetDS4CSetting(device, DS4Controls.Share);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.Tab), "", DS4KeyType.None);
            setting = GetDS4CSetting(device, DS4Controls.Options);
            setting.UpdateSettings(false, KeyInterop.VirtualKeyFromKey(Key.Escape), "", DS4KeyType.None);

            // If a device exists, make sure to transfer relevant profile device
            // options to device instance
            if (postLoad && device < Global.MAX_DS4_CONTROLLER_COUNT)
            {
                PostLoadSnippet(device, control, xinputStatus, xinputPlug);
            }
        }

        private void CheckOldDevicestatus(int device, ControlService control,
            OutContType oldContType, out bool xinputPlug, out bool xinputStatus)
        {
            xinputPlug = false;
            xinputStatus = false;

            if (device < ControlService.CURRENT_DS4_CONTROLLER_LIMIT)
            {
                bool oldUseDInputOnly = Global.useDInputOnly[device];
                DS4Device tempDevice = control.DS4Controllers[device];
                bool exists = tempBool = (tempDevice != null);
                bool synced = tempBool = exists ? tempDevice.isSynced() : false;
                bool isAlive = tempBool = exists ? tempDevice.IsAlive() : false;
                if (dinputOnly[device] != oldUseDInputOnly)
                {
                    if (dinputOnly[device] == true)
                    {
                        xinputPlug = false;
                        xinputStatus = true;
                    }
                    else if (synced && isAlive)
                    {
                        xinputPlug = true;
                        xinputStatus = true;
                    }
                }
                else if (!dinputOnly[device] &&
                    oldContType != outputDevType[device])
                {
                    xinputPlug = true;
                    xinputStatus = true;
                }
            }
        }

        private void PostLoadSnippet(int device, ControlService control, bool xinputStatus, bool xinputPlug)
        {
            DS4Device tempDev = control.DS4Controllers[device];
            if (tempDev != null && tempDev.isSynced())
            {
                tempDev.queueEvent(() =>
                {
                    //tempDev.setIdleTimeout(idleDisconnectTimeout[device]);
                    //tempDev.setBTPollRate(btPollRate[device]);
                    if (xinputStatus && tempDev.PrimaryDevice)
                    {
                        if (xinputPlug)
                        {
                            OutputDevice tempOutDev = control.outputDevices[device];
                            if (tempOutDev != null)
                            {
                                tempOutDev = null;
                                //Global.activeOutDevType[device] = OutContType.None;
                                control.UnplugOutDev(device, tempDev);
                            }

                            OutContType tempContType = outputDevType[device];
                            control.PluginOutDev(device, tempDev);
                            //Global.useDInputOnly[device] = false;
                        }
                        else
                        {
                            //Global.activeOutDevType[device] = OutContType.None;
                            control.UnplugOutDev(device, tempDev);
                        }
                    }

                    //tempDev.RumbleAutostopTime = rumbleAutostopTime[device];
                    //tempDev.setRumble(0, 0);
                    //tempDev.LightBarColor = Global.getMainColor(device);
                    control.CheckProfileOptions(device, tempDev, true);
                });

                //Program.rootHub.touchPad[device]?.ResetTrackAccel(trackballFriction[device]);
            }
        }
    }
}

