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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using DS4Windows;
using DS4Windows.InputDevices;
using DS4Windows.StickModifiers;
using DS4WinWPF.DS4Forms.ViewModels;
using static DS4Windows.Mouse;

namespace DS4WinWPF.DS4Control.DTOXml
{
    [XmlRoot("DS4Windows")]
    public class ProfileDTO : IDTO<BackingStore>
    {
        [XmlIgnore]
        public DateTime LastChecked { get; private set; }

        [XmlElement("LastChecked")]
        public string LastCheckString
        {
            get => LastChecked.ToString("MM/dd/yyyy HH:mm:ss");
            set
            {
                if (DateTime.TryParse(value, out DateTime temp))
                {
                    LastChecked = temp;
                }
            }
        }

        // Removed legacy CheckWhen; startup-check interval now persisted as
        // `CheckEveryValue` and `CheckEveryUnit` in AppSettings/profile DTOs.
        public const bool SERIALIZE_HEADER_ATTRS_DEFAULT = true;
        [XmlIgnore]
        public bool SerializeAppAttrs
        {
            get; set;
        } = SERIALIZE_HEADER_ATTRS_DEFAULT;

        [XmlAttribute("app_version")]
        public string AppVersion
        {
            get => Global.exeversion;
            set { }
        }
        public bool ShouldSerializeAppVersion()
        {
            return SerializeAppAttrs;
        }

        [XmlAttribute("config_version")]
        public string ConfigVersion
        {
            get => Global.CONFIG_VERSION.ToString();
            set { }
        }
        public bool ShouldSerializeConfigVersion()
        {
            return SerializeAppAttrs;
        }

        [XmlIgnore]
        public bool TouchToggle
        {
            get; private set;
        } = BackingStore.DEFAULT_TOUCH_TOGGLE;

        [XmlElement("touchToggle")]
        public string TouchToggleString
        {
            get => TouchToggle.ToString();
            set => TouchToggle = XmlDataUtilities.StrToBool(value);
        }

        [XmlElement("idleDisconnectTimeout")]
        public int IdleDisconnect
        {
            get; set;
        }

        [XmlIgnore]
        public bool OutputDataToDS4
        {
            get; private set;
        } = BackingStore.DEFAULT_OUTPUT_TO_DS4;

        [XmlElement("outputDataToDS4")]
        public string OutputDataToDS4String
        {
            get => OutputDataToDS4.ToString();
            set => OutputDataToDS4 = XmlDataUtilities.StrToBool(value);
        }

        private DS4Color _ledColor = new DS4Color();
        [XmlElement("Color")]
        public string ColorString
        {
            get; set;
        }

        /// <summary>
        /// Older capatibility property
        /// </summary>
        [XmlElement("Red")]
        public string RedColorString
        {
            get; set;
        }

        /// <summary>
        /// Older capatibility property
        /// </summary>
        [XmlElement("Green")]
        public string GreenColorString
        {
            get; set;
        }

        /// <summary>
        /// Older capatibility property
        /// </summary>
        [XmlElement("Blue")]
        public string BlueColorString
        {
            get; set;
        }

        [XmlElement("LeftStickDriftXAxis")]
        public sbyte LeftStickDriftXAxis
        {
            get; set;
        }

        [XmlElement("LeftStickDriftYAxis")]
        public sbyte LeftStickDriftYAxis
        {
            get; set;
        }

        [XmlElement("RightStickDriftXAxis")]
        public sbyte RightStickDriftXAxis
        {
            get; set;
        }

        [XmlElement("RightStickDriftYAxis")]
        public sbyte RightStickDriftYAxis
        {
            get; set;
        }

        [XmlElement("DebouncingMs")]
        public int DebouncingMs
        {
            get; set;
        }

        [XmlElement("InverseRumbleMotors")]
        public bool InverseRumbleMotors
        {
            get;
            set;
        }

        [XmlElement("UseDs3PitchRollSim")]
        public bool UseDs3PitchRollSim
        {
            get; set;
        }

        [XmlElement("RumbleBoost")]
        public byte RumbleBoost
        {
            get; set;
        } = BackingStore.DEFAULT_RUMBLE;

        [XmlElement("RumbleAutostopTime")]
        public int RumbleAutostopTime
        {
            get; set;
        }

        [XmlElement("LightbarMode")]
        public LightbarMode LightbarMode
        {
            get; set;
        } = LightbarSettingInfo.DEFAULT_MODE;

        [XmlIgnore]
        public bool LedAsBatteryIndicator
        {
            get; private set;
        }

        [XmlElement("ledAsBatteryIndicator")]
        public string LedAsBatteryIndicatorString
        {
            get => LedAsBatteryIndicator.ToString();
            set => LedAsBatteryIndicator = XmlDataUtilities.StrToBool(value);
        }

        [XmlElement("FlashType")]
        public byte FlashType
        {
            get; set;
        }

        [XmlElement("flashBatteryAt")]
        public int FlashBatteryAt
        {
            get; set;
        }

        [XmlElement("touchSensitivity")]
        public byte TouchSensitivity
        {
            get; set;
        } = BackingStore.DEFAULT_TOUCHPAD_SENS;

        private DS4Color _lowColor = new DS4Color();
        [XmlElement("LowColor")]
        public string LowColorString
        {
            get; set;
        }

        /// <summary>
        /// Older capatibility property
        /// </summary>
        [XmlElement("LowRed")]
        public string LowRedColorString
        {
            get; set;
        }

        /// <summary>
        /// Older capatibility property
        /// </summary>
        [XmlElement("LowGreen")]
        public string LowGreenColorString
        {
            get; set;
        }

        /// <summary>
        /// Older capatibility property
        /// </summary>
        [XmlElement("LowBlue")]
        public string LowBlueColorString
        {
            get; set;
        }

        private DS4Color _chargingColor = new DS4Color();
        [XmlElement("ChargingColor")]
        public string ChargingColorString
        {
            get; set;
        }

        /// <summary>
        /// Older capatibility property
        /// </summary>
        [XmlElement("ChargingRed")]
        public string ChargingRedString
        {
            get; set;
        }

        /// <summary>
        /// Older capatibility property
        /// </summary>
        [XmlElement("ChargingGreen")]
        public string ChargingGreenString
        {
            get; set;
        }

        /// <summary>
        /// Older capatibility property
        /// </summary>
        [XmlElement("ChargingBlue")]
        public string ChargingBlueString
        {
            get; set;
        }

        private DS4Color _flashColor = new DS4Color();
        [XmlElement("FlashColor")]
        public string FlashColorString
        {
            get; set;
        }

        [XmlIgnore]
        public bool TouchpadJitterCompensation
        {
            get; private set;
        } = BackingStore.DEFAULT_TOUCHPAD_JITTER_COMP;

        [XmlElement("touchpadJitterCompensation")]
        public string TouchpadJitterCompensationString
        {
            get => TouchpadJitterCompensation.ToString();
            set => TouchpadJitterCompensation = XmlDataUtilities.StrToBool(value);
        }

        [XmlIgnore]
        public bool LowerRCOn
        {
            get; private set;
        }

        [XmlElement("lowerRCOn")]
        public string LowerRCOnString
        {
            get => LowerRCOn.ToString();
            set => LowerRCOn = XmlDataUtilities.StrToBool(value);
        }

        [XmlElement("tapSensitivity")]
        public byte TapSensitivity
        {
            get; set;
        }

        [XmlIgnore]
        public bool DoubleTap
        {
            get; private set;
        }

        [XmlElement("doubleTap")]
        public string DoubleTapString
        {
            get => DoubleTap.ToString();
            set => DoubleTap = XmlDataUtilities.StrToBool(value);
        }

        [XmlElement("scrollSensitivity")]
        public int ScrollSensitivity
        {
            get; set;
        }

        [XmlElement("LeftTriggerMiddle")]
        public byte LeftTriggerMiddle
        {
            get; set;
        }

        [XmlElement("RightTriggerMiddle")]
        public byte RightTriggerMiddle
        {
            get; set;
        }

        private int _touchpadInvert;
        [XmlElement("TouchpadInvert")]
        public int TouchpadInvert
        {
            get => _touchpadInvert;
            set => _touchpadInvert = Math.Clamp(value, 0, 3);
        }

        [XmlIgnore]
        public bool TouchpadClickPassthru
        {
            get; private set;
        }

        [XmlElement("TouchpadClickPassthru")]
        public string TouchpadClickPassthruString
        {
            get => TouchpadClickPassthru.ToString();
            set => TouchpadClickPassthru = XmlDataUtilities.StrToBool(value);
        }

        [XmlElement("L2AntiDeadZone")]
        public int L2AntiDeadZone
        {
            get; set;
        }

        [XmlElement("R2AntiDeadZone")]
        public int R2AntiDeadZone
        {
            get; set;
        }

        private int _l2MaxZone = TriggerDeadZoneZInfo.DEFAULT_MAX_ZONE;
        [XmlElement("L2MaxZone")]
        public int L2MaxZone
        {
            get => _l2MaxZone;
            set => _l2MaxZone = Math.Clamp(value, 0, 100);
        }

        private int _r2MaxZone = TriggerDeadZoneZInfo.DEFAULT_MAX_ZONE;
        [XmlElement("R2MaxZone")]
        public int R2MaxZone
        {
            get => _r2MaxZone;
            set => _r2MaxZone = Math.Clamp(value, 0, 100);
        }

        private double _l2MaxOutput = TriggerDeadZoneZInfo.DEFAULT_MAX_OUTPUT;
        [XmlElement("L2MaxOutput")]
        public double L2MaxOutput
        {
            get => _l2MaxOutput;
            set => _l2MaxOutput = Math.Clamp(value, 0.0, 100.0);
        }

        private double _r2MaxOutput = TriggerDeadZoneZInfo.DEFAULT_MAX_OUTPUT;
        [XmlElement("R2MaxOutput")]
        public double R2MaxOutput
        {
            get => _r2MaxOutput;
            set => _r2MaxOutput = Math.Clamp(value, 0.0, 100.0);
        }

        [XmlElement("ButtonMouseSensitivity")]
        public int ButtonMouseSensitivity
        {
            get; set;
        } = ButtonMouseInfo.DEFAULT_BUTTON_SENS;

        [XmlElement("ButtonMouseOffset")]
        public double ButtonMouseOffset
        {
            get; set;
        } = ButtonMouseInfo.MOUSESTICKANTIOFFSET;

        [XmlElement("Rainbow")]
        public double Rainbow
        {
            get; set;
        }

        private double _maxSatRainbow = LightbarDS4WinInfo.DEFAULT_MAX_RAINBOW_SAT;
        [XmlElement("MaxSatRainbow")]
        public double MaxSatRainbow
        {
            get => _maxSatRainbow * 100.0;
            set => _maxSatRainbow = Math.Clamp(value, 0.0, 100.0) / 100.0;
        }

        private int _lSDeadZone = StickDeadZoneInfo.DEFAULT_DEADZONE;
        [XmlElement("LSDeadZone")]
        public int LSDeadZone
        {
            get => _lSDeadZone;
            set => _lSDeadZone = Math.Clamp(value, 0, 127);
        }

        private int _rSDeadZone = StickDeadZoneInfo.DEFAULT_DEADZONE;
        [XmlElement("RSDeadZone")]
        public int RSDeadZone
        {
            get => _rSDeadZone;
            set => _rSDeadZone = Math.Clamp(value, 0, 127);
        }

        private int _lSAntiDeadZone = StickDeadZoneInfo.DEFAULT_ANTIDEADZONE;
        [XmlElement("LSAntiDeadZone")]
        public int LSAntiDeadZone
        {
            get => _lSAntiDeadZone;
            set => _lSAntiDeadZone = value;
        }

        private int _rSAntiDeadZone = StickDeadZoneInfo.DEFAULT_ANTIDEADZONE;
        [XmlElement("RSAntiDeadZone")]
        public int RSAntiDeadZone
        {
            get => _rSAntiDeadZone;
            set => _rSAntiDeadZone = value;
        }

        private int _lSMaxZone = StickDeadZoneInfo.DEFAULT_MAXZONE;
        [XmlElement("LSMaxZone")]
        public int LSMaxZone
        {
            get => _lSMaxZone;
            set => _lSMaxZone = Math.Clamp(value, 0, 100);
        }

        private int _rSMaxZone = StickDeadZoneInfo.DEFAULT_MAXZONE;
        [XmlElement("RSMaxZone")]
        public int RSMaxZone
        {
            get => _rSMaxZone;
            set => _rSMaxZone = Math.Clamp(value, 0, 100);
        }

        private double _lSVerticalScale = StickDeadZoneInfo.DEFAULT_VERTICAL_SCALE;
        [XmlElement("LSVerticalScale")]
        public double LSVerticalScale
        {
            get => _lSVerticalScale;
            set => _lSVerticalScale = Math.Clamp(value, 0.0, 400.0);
        }

        private double _rSVerticalScale = StickDeadZoneInfo.DEFAULT_VERTICAL_SCALE;
        [XmlElement("RSVerticalScale")]
        public double RSVerticalScale
        {
            get => _rSVerticalScale;
            set => _rSVerticalScale = Math.Clamp(value, 0.0, 400.0);
        }

        private double _lSMaxOutput = StickDeadZoneInfo.DEFAULT_MAXOUTPUT;
        [XmlElement("LSMaxOutput")]
        public double LSMaxOutput
        {
            get => _lSMaxOutput;
            set => _lSMaxOutput = Math.Clamp(value, 0.0, 100.0);
        }

        private double _rSMaxOutput = StickDeadZoneInfo.DEFAULT_MAXOUTPUT;
        [XmlElement("RSMaxOutput")]
        public double RSMaxOutput
        {
            get => _rSMaxOutput;
            set => _rSMaxOutput = Math.Clamp(value, 0.0, 100.0);
        }

        private bool _lsMaxOutputForce = StickDeadZoneInfo.DEFAULT_MAXOUTPUT_FORCE;
        [XmlElement("LSMaxOutputForce")]
        public string LSMaxOutputForceString
        {
            get => _lsMaxOutputForce.ToString();
            set => _lsMaxOutputForce = XmlDataUtilities.StrToBool(value);
        }

        private bool _rsMaxOutputForce = StickDeadZoneInfo.DEFAULT_MAXOUTPUT_FORCE;
        [XmlElement("RSMaxOutputForce")]
        public string RSMaxOutputForceString
        {
            get => _rsMaxOutputForce.ToString();
            set => _rsMaxOutputForce = XmlDataUtilities.StrToBool(value);
        }

        [XmlElement("LSDeadZoneType")]
        public StickDeadZoneInfo.DeadZoneType LSDeadZoneType
        {
            get; set;
        }

        [XmlElement("RSDeadZoneType")]
        public StickDeadZoneInfo.DeadZoneType RSDeadZoneType
        {
            get; set;
        }

        [XmlElement("LSAxialDeadOptions")]
        public StickAxialDeadOptionsSerializer LSAxialDeadOptions
        {
            get; set;
        }

        [XmlElement("RSAxialDeadOptions")]
        public StickAxialDeadOptionsSerializer RSAxialDeadOptions
        {
            get; set;
        }


        private double _lSRotation;
        [XmlElement("LSRotation")]
        public double LSRotation
        {
            get => _lSRotation * 180.0 / Math.PI;
            set
            {
                double tempDegrees = Math.Clamp(value, -180.0, 180.0);
                _lSRotation = tempDegrees * Math.PI / 180.0;
            }
        }

        private double _rSRotation;
        [XmlElement("RSRotation")]
        public double RSRotation
        {
            get => _rSRotation * 180.0 / Math.PI;
            set
            {
                double tempDegrees = Math.Clamp(value, -180.0, 180.0);
                _rSRotation = tempDegrees * Math.PI / 180.0;
            }
        }

        private int _lSFuzz;
        [XmlElement("LSFuzz")]
        public int LSFuzz
        {
            get => _lSFuzz;
            set => _lSFuzz = Math.Clamp(value, 0, 100);
        }

        private int _rSFuzz;
        [XmlElement("RSFuzz")]
        public int RSFuzz
        {
            get => _rSFuzz;
            set => _rSFuzz = Math.Clamp(value, 0, 100);
        }

        private double _lSOuterBindDead = StickDeadZoneInfo.DEFAULT_OUTER_BIND_DEAD;
        [XmlElement("LSOuterBindDead")]
        public double LSOuterBindDead
        {
            get => _lSOuterBindDead;
            set => _lSOuterBindDead = Math.Clamp(value, 0.0, 100.0);
        }

        private double _rSOuterBindDead = StickDeadZoneInfo.DEFAULT_OUTER_BIND_DEAD;
        [XmlElement("RSOuterBindDead")]
        public double RSOuterBindDead
        {
            get => _rSOuterBindDead;
            set => _rSOuterBindDead = Math.Clamp(value, 0.0, 100.0);
        }

        private bool _lSOuterBindInvert = StickDeadZoneInfo.DEFAULT_OUTER_BIND_INVERT;
        [XmlElement("LSOuterBindInvert")]
        public string LSOuterBindInvertString
        {
            get => _lSOuterBindInvert.ToString();
            set => _lSOuterBindInvert = XmlDataUtilities.StrToBool(value);
        }

        private bool _rSOuterBindInvert = StickDeadZoneInfo.DEFAULT_OUTER_BIND_INVERT;
        [XmlElement("RSOuterBindInvert")]
        public string RSOuterBindInvertString
        {
            get => _rSOuterBindInvert.ToString();
            set => _rSOuterBindInvert = XmlDataUtilities.StrToBool(value);
        }

        [XmlElement("LSDeltaAccelSettings")]
        public StickDeltaAccelSettings LSDeltaAccelSettings
        {
            get; set;
        }

        [XmlElement("RSDeltaAccelSettings")]
        public StickDeltaAccelSettings RSDeltaAccelSettings
        {
            get; set;
        }

        private double _sXDeadZone = BackingStore.DEFAULT_SX_TILT_DEADZONE;
        [XmlElement("SXDeadZone")]
        public double SXDeadZone
        {
            get => _sXDeadZone;
            set => _sXDeadZone = value;
        }

        private double _sZDeadZone = BackingStore.DEFAULT_SX_TILT_DEADZONE;
        [XmlElement("SZDeadZone")]
        public double SZDeadZone
        {
            get => _sZDeadZone;
            set => _sZDeadZone = value;
        }

        private double _sxMaxZone = BackingStore.DEFAULT_SX_TILT_MAXZONE;
        [XmlElement("SXMaxZone")]
        public double SXMaxZone
        {
            get => _sxMaxZone * 100.0;
            set => _sxMaxZone = Math.Clamp(value * 0.01, 0.0, 1.0);
        }

        private double _szMaxZone = BackingStore.DEFAULT_SX_TILT_MAXZONE;
        [XmlElement("SZMaxZone")]
        public double SZMaxZone
        {
            get => _szMaxZone * 100.0;
            set => _szMaxZone = Math.Clamp(value * 0.01, 0.0, 1.0);
        }

        private double _sxAntiDeadZone;
        [XmlElement("SXAntiDeadZone")]
        public double SXAntiDeadZone
        {
            get => _sxAntiDeadZone * 100.0;
            set => _sxAntiDeadZone = Math.Clamp(value * 0.01, 0.0, 1.0);
        }

        private double _szAntiDeadZone;
        [XmlElement("SZAntiDeadZone")]
        public double SZAntiDeadZone
        {
            get => _szAntiDeadZone * 100.0;
            set => _szAntiDeadZone = Math.Clamp(value * 0.01, 0.0, 1.0);
        }

        /// <summary>
        /// Older capatibility property
        /// </summary>
        [XmlElement("Sensitivity")]
        public string Sensitivity
        {
            get; set;
        } = string.Empty;
        public bool ShouldSerializeSensitivity()
        {
            return !string.IsNullOrEmpty(Sensitivity);
        }

        [XmlElement("ChargingType")]
        public int ChargingType
        {
            get; set;
        }

        private bool _mouseAcceleration;
        [XmlElement("MouseAcceleration")]
        public string MouseAccelerationString
        {
            get => _mouseAcceleration.ToString();
            set => _mouseAcceleration = XmlDataUtilities.StrToBool(value);
        }

        private double _buttonMouseVerticalScale = ButtonMouseInfo.DEFAULT_BUTTON_VERTICAL_SCALE;
        [XmlElement("ButtonMouseVerticalScale")]
        public double ButtonMouseVerticalScale
        {
            get => _buttonMouseVerticalScale * 100.0;
            set => _buttonMouseVerticalScale = Math.Clamp(value, 0.0, 500.0) * 0.01;
        }

        /// <summary>
        /// WTF. Some local var?
        /// </summary>
        [XmlElement("ShiftModifier")]
        public int ShiftModifier
        {
            get; set;
        } = -1;
        public bool ShouldSerializeShiftModifier()
        {
            return ShiftModifier != -1;
        }

        [XmlElement("LaunchProgram")]
        public string LaunchProgram
        {
            get; set;
        } = string.Empty;

        private bool _dinputOnly;
        [XmlElement("DinputOnly")]
        public string DinputOnlyString
        {
            get => _dinputOnly.ToString();
            set => _dinputOnly = XmlDataUtilities.StrToBool(value);
        }

        private bool _startTouchpadOff;
        [XmlElement("StartTouchpadOff")]
        public string StartTouchpadOffString
        {
            get => _startTouchpadOff.ToString();
            set => _startTouchpadOff = XmlDataUtilities.StrToBool(value);
        }

        private bool _hasUseTPforControls;
        private bool _useTPforControls = false;
        [XmlElement("UseTPforControls")]
        public string UseTPforControlsString
        {
            get => _useTPforControls.ToString();
            set
            {
                _useTPforControls = XmlDataUtilities.StrToBool(value);
                _hasUseTPforControls = true;
            }
        }
        public bool ShouldSerializeUseTPforControlsString()
        {
            return false;
        }

        private bool _hasSAforMouse;
        private bool _useSAforMouse;
        [XmlElement("UseSAforMouse")]
        public string UseSAforMouseString
        {
            get => _useSAforMouse.ToString();
            set
            {
                _useSAforMouse = XmlDataUtilities.StrToBool(value);
                _hasSAforMouse = true;
            }
        }
        public bool ShouldSerializeUseSAforMouseString()
        {
            return false;
        }

        [XmlElement("TouchpadOutputMode")]
        public TouchpadOutMode TouchpadOutputMode
        {
            get; set;
        } = BackingStore.DEFAULT_TOUCH_OUT_MODE;

        [XmlElement("SATriggers")]
        public string SATriggers
        {
            get; set;
        } = BackingStore.DEFAULT_SA_TRIGGERS;

        private bool _sATriggerCond = BackingStore.DEFAULT_SA_TRIGGER_COND;
        [XmlElement("SATriggerCond")]
        public string SATriggerCondString
        {
            get => BackingStore.SaTriggerCondString(_sATriggerCond);
            set => _sATriggerCond = BackingStore.SaTriggerCondValue(value);
        }

        private SASteeringWheelEmulationAxisType _sASteeringWheelEmulationAxis = SASteeringWheelEmulationAxisType.None;
        [XmlElement("SASteeringWheelEmulationAxis")]
        public SASteeringWheelEmulationAxisType SASteeringWheelEmulationAxis
        {
            get => _sASteeringWheelEmulationAxis;
            set => _sASteeringWheelEmulationAxis = value;
        }

        private int _sASteeringWheelEmulationRange = BackingStore.DEFAULT_SA_WHEEL_EMULATION_RANGE;
        [XmlElement("SASteeringWheelEmulationRange")]
        public int SASteeringWheelEmulationRange
        {
            get => _sASteeringWheelEmulationRange;
            set => _sASteeringWheelEmulationRange = value;
        }

        private int _sASteeringWheelFuzz = 0;
        [XmlElement("SASteeringWheelFuzz")]
        public int SASteeringWheelFuzz
        {
            get => _sASteeringWheelFuzz;
            set => _sASteeringWheelFuzz = Math.Clamp(value, 0, 100);
        }

        [XmlElement("SASteeringWheelSmoothingOptions")]
        public SASteeringWheelSmoothingOptions SASteeringWheelSmoothingOptions
        {
            get; set;
        }

        private int[] _touchDisInvTriggers = new int[1] { BackingStore.DEFAULT_TOUCH_DIS_INVERT_TRIGGER };
        [XmlElement("TouchDisInvTriggers")]
        public string TouchDisInvTriggersString
        {
            get => string.Join(",", _touchDisInvTriggers);
            set
            {
                string[] triggers = value.Split(',');
                int temp = -1;
                List<int> tempIntList = new List<int>();
                for (int i = 0, arlen = triggers.Length; i < arlen; i++)
                {
                    if (int.TryParse(triggers[i], out temp))
                        tempIntList.Add(temp);
                }

                _touchDisInvTriggers = tempIntList.ToArray();
            }
        }

        private int _gyroSensitivity = BackingStore.DEFAULT_GYRO_SENS;
        [XmlElement("GyroSensitivity")]
        public int GyroSensitivity
        {
            get => _gyroSensitivity;
            set => _gyroSensitivity = value;
        }

        private int _gyroSensVerticalScale = BackingStore.DEFAULT_GYRO_SENS_VERTICAL_SCALE;
        [XmlElement("GyroSensVerticalScale")]
        public int GyroSensVerticalScale
        {
            get => _gyroSensVerticalScale;
            set => _gyroSensVerticalScale = value;
        }

        [XmlElement("GyroInvert")]
        public int GyroInvert
        {
            get; set;
        }

        private bool _gyroTriggerTurns = BackingStore.DEFAULT_GYRO_TRIGGER_TURNS;
        [XmlElement("GyroTriggerTurns")]
        public string GyroTriggerTurnsString
        {
            get => _gyroTriggerTurns.ToString();
            set => _gyroTriggerTurns = XmlDataUtilities.StrToBool(value);
        }

        [XmlElement("GyroControlsSettings")]
        public GyroControlsSettings GyroControlsSettings
        {
            get; set;
        }

        [XmlElement("GyroMouseSmoothingSettings")]
        public GyroMouseSmoothingSettings GyroMouseSmoothingSettings
        {
            get; set;
        }

        private int _gyroMouseHAxis;
        [XmlElement("GyroMouseHAxis")]
        public int GyroMouseHAxis
        {
            get => _gyroMouseHAxis;
            set => _gyroMouseHAxis = Math.Clamp(value, 0, 1);
        }

        private int _gyroMouseDeadZone = MouseCursor.GYRO_MOUSE_DEADZONE;
        [XmlElement("GyroMouseDeadZone")]
        public int GyroMouseDeadZone
        {
            get => _gyroMouseDeadZone;
            set => _gyroMouseDeadZone = value;
        }

        private double _gyroMouseMinThreshold = GyroMouseInfo.DEFAULT_MIN_THRESHOLD;
        [XmlElement("GyroMouseMinThreshold")]
        public double GyroMouseMinThreshold
        {
            get => _gyroMouseMinThreshold;
            set => _gyroMouseMinThreshold = Math.Clamp(value, 1.0, 40.0);
        }

        private bool _gyroMouseToggle;
        [XmlElement("GyroMouseToggle")]
        public string GyroMouseToggleString
        {
            get => _gyroMouseToggle.ToString();
            set => _gyroMouseToggle = XmlDataUtilities.StrToBool(value);
        }

        private bool _gyroMouseJitterCompensation = GyroMouseInfo.JITTER_COMPENSATION_DEFAULT;
        [XmlElement("GyroMouseJitterCompensation")]
        public string GyroMouseJitterCompensation
        {
            get => _gyroMouseJitterCompensation.ToString();
            set => _gyroMouseJitterCompensation = XmlDataUtilities.StrToBool(value);
        }

        [XmlElement("GyroOutputMode")]
        public GyroOutMode GyroOutputMode
        {
            get; set;
        } = BackingStore.DEFAULT_GYRO_OUT_MODE;


        private string _gyroMouseStickTriggers = BackingStore.DEFAULT_GYRO_MSTICK_TRIGGERS;
        [XmlElement("GyroMouseStickTriggers")]
        public string GyroMouseStickTriggers
        {
            get => _gyroMouseStickTriggers;
            set => _gyroMouseStickTriggers = value;
        }

        private bool _gyroMouseStickTriggerCond = BackingStore.DEFAULT_SA_MSTICK_TRIGGER_COND;
        [XmlElement("GyroMouseStickTriggerCond")]
        public string GyroMouseStickTriggerCondString
        {
            get => BackingStore.SaTriggerCondString(_gyroMouseStickTriggerCond);
            set => _gyroMouseStickTriggerCond = BackingStore.SaTriggerCondValue(value);
        }

        private bool _gyroMouseStickTriggerTurns = BackingStore.DEFAULT_GYRO_MSTICK_TRIGGER_TURNS;
        [XmlElement("GyroMouseStickTriggerTurns")]
        public string GyroMouseStickTriggerTurnsString
        {
            get => _gyroMouseStickTriggerTurns.ToString();
            set => _gyroMouseStickTriggerTurns = XmlDataUtilities.StrToBool(value);
        }


        private int _gyroMouseStickHAxis;
        [XmlElement("GyroMouseStickHAxis")]
        public int GyroMouseStickHAxis
        {
            get => _gyroMouseStickHAxis;
            set => _gyroMouseStickHAxis = Math.Clamp(value, 0, 1);
        }

        private int _gyroMouseStickDeadZone = GyroMouseStickInfo.DEFAULT_DEADZONE;
        [XmlElement("GyroMouseStickDeadZone")]
        public int GyroMouseStickDeadZone
        {
            get => _gyroMouseStickDeadZone;
            set => _gyroMouseStickDeadZone = value;
        }

        private int _gyroMouseStickMaxZone = GyroMouseStickInfo.DEFAULT_MAXZONE;
        [XmlElement("GyroMouseStickMaxZone")]
        public int GyroMouseStickMaxZone
        {
            get => _gyroMouseStickMaxZone;
            set => _gyroMouseStickMaxZone = Math.Max(1, value);
        }

        [XmlElement("GyroMouseStickOutputStick")]
        public GyroMouseStickInfo.OutputStick GyroMouseStickOutputStick
        {
            get; set;
        }

        [XmlElement("GyroMouseStickOutputStickAxes")]
        public GyroMouseStickInfo.OutputStickAxes GyroMouseStickOutputStickAxes
        {
            get; set;
        }

        private double _gyroMouseStickAntiDeadX = GyroMouseStickInfo.DEFAULT_ANTI_DEAD;
        [XmlElement("GyroMouseStickAntiDeadX")]
        public double GyroMouseStickAntiDeadX
        {
            get => _gyroMouseStickAntiDeadX;
            set => _gyroMouseStickAntiDeadX = Math.Clamp(value, 0.0, 1.0);
        }

        private double _gyroMouseStickAntiDeadY = GyroMouseStickInfo.DEFAULT_ANTI_DEAD;
        [XmlElement("GyroMouseStickAntiDeadY")]
        public double GyroMouseStickAntiDeadY
        {
            get => _gyroMouseStickAntiDeadY;
            set => _gyroMouseStickAntiDeadY = Math.Clamp(value, 0.0, 1.0);
        }

        private uint _gyroMouseStickInvert = GyroMouseStickInfo.DEFAULT_INVERTED;
        [XmlElement("GyroMouseStickInvert")]
        public uint GyroMouseStickInvert
        {
            get => _gyroMouseStickInvert;
            set => _gyroMouseStickInvert = value;
        }

        private bool _gyroMouseStickToggle;
        [XmlElement("GyroMouseStickToggle")]
        public string GyroMouseStickToggleString
        {
            get => _gyroMouseStickToggle.ToString();
            set => _gyroMouseStickToggle = XmlDataUtilities.StrToBool(value);
        }

        private double _gyroMouseStickMaxOutput = GyroMouseStickInfo.DEFAULT_MAX_OUTPUT;
        [XmlElement("GyroMouseStickMaxOutput")]
        public double GyroMouseStickMaxOutput
        {
            get => _gyroMouseStickMaxOutput;
            set => _gyroMouseStickMaxOutput = Math.Clamp(value, 0.0, 100.0);
        }

        private bool _gyroMouseStickMaxOutputEnabled;
        [XmlElement("GyroMouseStickMaxOutputEnabled")]
        public string GyroMouseStickMaxOutputEnabledString
        {
            get => _gyroMouseStickMaxOutputEnabled.ToString();
            set => _gyroMouseStickMaxOutputEnabled = XmlDataUtilities.StrToBool(value);
        }

        private int _gyroMouseStickVerticalScale = GyroMouseStickInfo.DEFAULT_VERTICAL_SCALE;
        [XmlElement("GyroMouseStickVerticalScale")]
        public int GyroMouseStickVerticalScale
        {
            get => _gyroMouseStickVerticalScale;
            set => _gyroMouseStickVerticalScale = value;
        }

        private bool _gyroMouseStickJitterCompensation = GyroMouseStickInfo.JITTER_COMPENSATION_DEFAULT;
        [XmlElement("GyroMouseStickJitterCompensation")]
        public string GyroMouseStickJitterCompensation
        {
            get => _gyroMouseStickJitterCompensation.ToString();
            set => _gyroMouseStickJitterCompensation = XmlDataUtilities.StrToBool(value);
        }

        [XmlElement("GyroMouseStickSmoothingSettings")]
        public GyroMouseStickSmoothingSettings GyroMouseStickSmoothingSettings
        {
            get; set;
        }

        [XmlElement("GyroSwipeSettings")]
        public GyroSwipeSettings GyroSwipeSettings
        {
            get; set;
        }

        private int _bTPollRate = BackingStore.DEFAULT_DS4_BT_POLL_RATE;
        [XmlElement("BTPollRate")]
        public int BTPollRateString
        {
            get => _bTPollRate;
            set => _bTPollRate = Math.Clamp(value, 1, 16);
        }

        [XmlElement("LSOutputCurveMode")]
        public string LSOutputCurveMode
        {
            get; set;
        } = BackingStore.DEFAULT_STICK_OUTPUT_CURVE;

        [XmlElement("LSOutputCurveCustom")]
        public string LSOutputCurveCustom
        {
            get; set;
        }

        [XmlElement("RSOutputCurveMode")]
        public string RSOutputCurveMode
        {
            get; set;
        } = BackingStore.DEFAULT_STICK_OUTPUT_CURVE;

        [XmlElement("RSOutputCurveCustom")]
        public string RSOutputCurveCustom
        {
            get; set;
        }

        private bool _lsSquareStick;
        [XmlElement("LSSquareStick")]
        public string LSSquareStickString
        {
            get => _lsSquareStick.ToString();
            set => _lsSquareStick = XmlDataUtilities.StrToBool(value);
        }

        private bool _rsSquareStick;
        [XmlElement("RSSquareStick")]
        public string RSSquareStickString
        {
            get => _rsSquareStick.ToString();
            set => _rsSquareStick = XmlDataUtilities.StrToBool(value);
        }

        private double _squareStickRoundness = SquareStickInfo.DEFAULT_ROUNDNESS;
        [XmlElement("SquareStickRoundness")]
        public double SquareStickRoundness
        {
            get => _squareStickRoundness;
            set => _squareStickRoundness = value;
        }

        private double _squareRStickRoundness = SquareStickInfo.DEFAULT_ROUNDNESS;
        [XmlElement("SquareRStickRoundness")]
        public double SquareRStickRoundness
        {
            get => _squareRStickRoundness;
            set => _squareRStickRoundness = value;
        }

        private bool _lsAntiSnapback = StickAntiSnapbackInfo.DEFAULT_ENABLED;
        [XmlElement("LSAntiSnapback")]
        public string LSAntiSnapbackString
        {
            get => _lsAntiSnapback.ToString();
            set => _lsAntiSnapback = XmlDataUtilities.StrToBool(value);
        }

        private bool _rsAntiSnapback = StickAntiSnapbackInfo.DEFAULT_ENABLED;
        [XmlElement("RSAntiSnapback")]
        public string RSAntiSnapbackString
        {
            get => _rsAntiSnapback.ToString();
            set => _rsAntiSnapback = XmlDataUtilities.StrToBool(value);
        }

        private double _lsAntiSnapbackDelta = StickAntiSnapbackInfo.DEFAULT_DELTA;
        [XmlElement("LSAntiSnapbackDelta")]
        public double LSAntiSnapbackDelta
        {
            get => _lsAntiSnapbackDelta;
            set => _lsAntiSnapbackDelta = value;
        }

        private double _rsAntiSnapbackDelta = StickAntiSnapbackInfo.DEFAULT_DELTA;
        [XmlElement("RSAntiSnapbackDelta")]
        public double RSAntiSnapbackDelta
        {
            get => _rsAntiSnapbackDelta;
            set => _rsAntiSnapbackDelta = value;
        }

        private int _lsAntiSnapbackTimeout = StickAntiSnapbackInfo.DEFAULT_TIMEOUT;
        [XmlElement("LSAntiSnapbackTimeout")]
        public int LSAntiSnapbackTimeout
        {
            get => _lsAntiSnapbackTimeout;
            set => _lsAntiSnapbackTimeout = value;
        }

        private int _rsAntiSnapbackTimeout = StickAntiSnapbackInfo.DEFAULT_TIMEOUT;
        [XmlElement("RSAntiSnapbackTimeout")]
        public int RSAntiSnapbackTimeout
        {
            get => _rsAntiSnapbackTimeout;
            set => _rsAntiSnapbackTimeout = value;
        }

        [XmlElement("LSOutputMode")]
        public StickMode LSOutputMode
        {
            get; set;
        } = StickMode.Controls;

        [XmlElement("RSOutputMode")]
        public StickMode RSOutputMode
        {
            get; set;
        } = StickMode.Controls;

        [XmlElement("LSOutputSettings")]
        public StickModeOutputSettings LSOutputSettings
        {
            get; set;
        }

        [XmlElement("RSOutputSettings")]
        public StickModeOutputSettings RSOutputSettings
        {
            get; set;
        }

        [XmlElement("DualSenseControllerSettings")]
        public DualSenseControllerSettings DualSenseControllerSettings
        {
            get; set;
        }

        [XmlElement("L2OutputCurveMode")]
        public string L2OutputCurveMode
        {
            get; set;
        }

        [XmlElement("L2OutputCurveCustom")]
        public string L2OutputCurveCustom
        {
            get; set;
        }

        [XmlElement("L2TwoStageMode")]
        public TwoStageTriggerMode L2TwoStageMode
        {
            get; set;
        }

        [XmlElement("R2TwoStageMode")]
        public TwoStageTriggerMode R2TwoStageMode
        {
            get; set;
        }

        private int _l2HipFireTime = TriggerOutputSettings.DEFAULT_HIP_TIME;
        [XmlElement("L2HipFireTime")]
        public int L2HipFireTime
        {
            get => _l2HipFireTime;
            set => _l2HipFireTime = Math.Clamp(value, 0, 5000);
        }

        private int _r2HipFireTime = TriggerOutputSettings.DEFAULT_HIP_TIME;
        [XmlElement("R2HipFireTime")]
        public int R2HipFireTime
        {
            get => _r2HipFireTime;
            set => _r2HipFireTime = Math.Clamp(value, 0, 5000);
        }

        [XmlElement("L2TriggerEffect")]
        public DS4Windows.InputDevices.TriggerEffects L2TriggerEffect
        {
            get; set;
        }

        [XmlElement("R2TriggerEffect")]
        public DS4Windows.InputDevices.TriggerEffects R2TriggerEffect
        {
            get; set;
        }

        [XmlElement("R2OutputCurveMode")]
        public string R2OutputCurveMode
        {
            get; set;
        }

        [XmlElement("R2OutputCurveCustom")]
        public string R2OutputCurveCustom
        {
            get; set;
        }

        [XmlElement("SXOutputCurveMode")]
        public string SXOutputCurveMode
        {
            get; set;
        } = BackingStore.DEFAULT_SA_OUTPUT_CURVE;


        [XmlElement("SXOutputCurveCustom")]
        public string SXOutputCurveCustom
        {
            get; set;
        }

        [XmlElement("SZOutputCurveMode")]
        public string SZOutputCurveMode
        {
            get; set;
        } = BackingStore.DEFAULT_SA_OUTPUT_CURVE;

        [XmlElement("SZOutputCurveCustom")]
        public string SZOutputCurveCustom
        {
            get; set;
        }

        private bool _trackballMode = BackingStore.DEFAULT_TRACKBALL_MODE;
        [XmlElement("TrackballMode")]
        public string TrackballModeString
        {
            get => _trackballMode.ToString();
            set => _trackballMode = XmlDataUtilities.StrToBool(value);
        }

        private double _trackballFriction = BackingStore.DEFAULT_TRACKBALL_FRICTION;
        [XmlElement("TrackballFriction")]
        public double TrackballFriction
        {
            get => _trackballFriction;
            set => _trackballFriction = value;
        }

        private double _touchRelMouseRotation;
        [XmlElement("TouchRelMouseRotation")]
        public double TouchRelMouseRotation
        {
            get => _touchRelMouseRotation;
            set
            {
                double temp = Math.Clamp(value, -180.0, 180.0);
                _touchRelMouseRotation = temp * Math.PI / 180.0;
            }
        }

        private double _touchRelMouseMinThreshold = TouchpadRelMouseSettings.DEFAULT_MIN_THRESHOLD;
        [XmlElement("TouchRelMouseMinThreshold")]
        public double TouchRelMouseMinThreshold
        {
            get => _touchRelMouseMinThreshold;
            set
            {
                double temp = Math.Clamp(value, 1.0, 40.0);
                _touchRelMouseMinThreshold = temp;
            }
        }

        [XmlElement("TouchpadMouseStick")]
        public TouchpadMouseStickSerializer TouchpadMouseStickSettings
        {
            get; set;
        }

        [XmlElement("TouchpadButtonMode")]
        public TouchButtonActivationMode TouchpadButtonMode
        {
            get; set;
        }

        [XmlElement("OutputContDevice")]
        public OutContType OutputContDevice
        {
            get; set;
        } = BackingStore.DEFAULT_OUT_CONT_TYPE;

        [XmlElement("DS4OutputTriggerMode")]
        public DS4TriggerOutputMode OutputDS4TriggerMode
        {
            get; set;
        }
        public bool ShouldSerializeOutputDS4TriggerMode()
        {
            return OutputDS4TriggerMode != BackingStore.DEFAULT_DS4_TRIGGER_OUTPUT;
        }

        [XmlElement("ProfileActions")]
        public string ProfileActions
        {
            get; set;
        }

        [XmlElement("Control")]
        public DS4ControlAssignementSerializer Control
        {
            get; set;
        }

        [XmlElement("ShiftControl")]
        public DS4ControlAssignementSerializer ShiftControl
        {
            get; set;
        }

        private int deviceIndex = -1;
        [XmlIgnore]
        public int DeviceIndex
        {
            get => deviceIndex;
            set => deviceIndex = value;
        }

        public ProfileDTO()
        {
            LSAxialDeadOptions = new StickAxialDeadOptionsSerializer();
            LSDeltaAccelSettings = new StickDeltaAccelSettings();
            RSAxialDeadOptions = new StickAxialDeadOptionsSerializer();
            RSDeltaAccelSettings = new StickDeltaAccelSettings();
            LaunchProgram = string.Empty;
            SATriggers = BackingStore.DEFAULT_SA_TRIGGERS;
            SASteeringWheelSmoothingOptions = new SASteeringWheelSmoothingOptions();
            GyroControlsSettings = new GyroControlsSettings();
            GyroMouseStickSmoothingSettings = new GyroMouseStickSmoothingSettings();
            GyroSwipeSettings = new GyroSwipeSettings();
            GyroMouseSmoothingSettings = new GyroMouseSmoothingSettings();
            LSOutputSettings = new StickModeOutputSettings();
            RSOutputSettings = new StickModeOutputSettings();
            DualSenseControllerSettings = new DualSenseControllerSettings();
            Control = new DS4ControlAssignementSerializer();
            ShiftControl = new DS4ControlAssignementSerializer();
        }

        public void MapFrom(BackingStore source)
        {
            if (deviceIndex == -1)
            {
                throw new ArgumentOutOfRangeException("Device Index must be set");
            }

            LightbarSettingInfo lightbarSettings = source.lightbarSettingInfo[deviceIndex];
            LightbarDS4WinInfo lightInfo = lightbarSettings.ds4winSettings;

            UseDs3PitchRollSim = source.useDs3PitchRollSim;
            TouchToggle = source.enableTouchToggle[deviceIndex];
            IdleDisconnect = source.idleDisconnectTimeout[deviceIndex];
            OutputDataToDS4 = source.enableOutputDataToDS4[deviceIndex];
            LightbarMode = source.lightbarSettingInfo[deviceIndex].mode;
            ColorString = $"{lightInfo.m_Led.red},{lightInfo.m_Led.green},{lightInfo.m_Led.blue}";
            _ledColor = new DS4Color(lightInfo.m_Led.red, lightInfo.m_Led.green, lightInfo.m_Led.blue);
            LeftStickDriftXAxis = source.leftStickDriftXAxis[deviceIndex];
            LeftStickDriftYAxis = source.leftStickDriftYAxis[deviceIndex];
            RightStickDriftXAxis = source.rightStickDriftXAxis[deviceIndex];
            RightStickDriftYAxis = source.rightStickDriftYAxis[deviceIndex];
            DebouncingMs = source.debouncingMs[deviceIndex];
            InverseRumbleMotors = source.inverseRumbleMotors[deviceIndex];
            RumbleBoost = source.rumble[deviceIndex];
            RumbleAutostopTime = source.rumbleAutostopTime[deviceIndex];
            LedAsBatteryIndicator = lightInfo.ledAsBattery;
            FlashType = lightInfo.flashType;
            FlashBatteryAt = lightInfo.flashAt;
            TouchSensitivity = source.touchSensitivity[deviceIndex];
            _lowColor = new DS4Color(lightInfo.m_LowLed.red, lightInfo.m_LowLed.green, lightInfo.m_LowLed.blue);
            LowColorString = $"{lightInfo.m_LowLed.red},{lightInfo.m_LowLed.green},{lightInfo.m_LowLed.blue}";

            _chargingColor = new DS4Color(lightInfo.m_ChargingLed.red, lightInfo.m_ChargingLed.green, lightInfo.m_ChargingLed.blue);
            ChargingColorString = $"{lightInfo.m_ChargingLed.red},{lightInfo.m_ChargingLed.green},{lightInfo.m_ChargingLed.blue}";

            _flashColor = new DS4Color(lightInfo.m_FlashLed.red, lightInfo.m_FlashLed.green, lightInfo.m_FlashLed.blue);
            FlashColorString = $"{lightInfo.m_FlashLed.red},{lightInfo.m_FlashLed.green},{lightInfo.m_FlashLed.blue}";

            TouchpadJitterCompensation = source.touchpadJitterCompensation[deviceIndex];
            LowerRCOn = source.lowerRCOn[deviceIndex];
            TapSensitivity = source.tapSensitivity[deviceIndex];
            DoubleTap = source.doubleTap[deviceIndex];
            ScrollSensitivity = source.scrollSensitivity[deviceIndex];
            TouchpadInvert = source.touchpadInvert[deviceIndex];
            TouchpadClickPassthru = source.touchClickPassthru[deviceIndex];
            LeftTriggerMiddle = source.l2ModInfo[deviceIndex].deadZone;
            RightTriggerMiddle = source.r2ModInfo[deviceIndex].deadZone;
            L2AntiDeadZone = source.l2ModInfo[deviceIndex].antiDeadZone;
            R2AntiDeadZone = source.r2ModInfo[deviceIndex].antiDeadZone;
            L2MaxZone = source.l2ModInfo[deviceIndex].maxZone;
            R2MaxZone = source.r2ModInfo[deviceIndex].maxZone;
            L2MaxOutput = source.l2ModInfo[deviceIndex].maxOutput;
            R2MaxOutput = source.r2ModInfo[deviceIndex].maxOutput;
            _lSRotation = source.LSRotation[deviceIndex];
            _rSRotation = source.RSRotation[deviceIndex];
            LSFuzz = source.lsModInfo[deviceIndex].fuzz;
            RSFuzz = source.rsModInfo[deviceIndex].fuzz;
            ButtonMouseSensitivity = source.buttonMouseInfos[deviceIndex].buttonSensitivity;
            ButtonMouseOffset = source.buttonMouseInfos[deviceIndex].mouseVelocityOffset;
            _buttonMouseVerticalScale = source.buttonMouseInfos[deviceIndex].buttonVerticalScale;
            Rainbow = lightInfo.rainbow;
            _maxSatRainbow = lightInfo.maxRainbowSat;
            LSDeadZone = source.lsModInfo[deviceIndex].deadZone;
            RSDeadZone = source.rsModInfo[deviceIndex].deadZone;
            LSAntiDeadZone = source.lsModInfo[deviceIndex].antiDeadZone;
            RSAntiDeadZone = source.rsModInfo[deviceIndex].antiDeadZone;
            LSMaxZone = source.lsModInfo[deviceIndex].maxZone;
            RSMaxZone = source.rsModInfo[deviceIndex].maxZone;
            LSVerticalScale = source.lsModInfo[deviceIndex].verticalScale;
            LSMaxOutput = source.lsModInfo[deviceIndex].maxOutput;
            _lsMaxOutputForce = source.lsModInfo[deviceIndex].maxOutputForce;

            RSVerticalScale = source.rsModInfo[deviceIndex].verticalScale;
            RSMaxOutput = source.rsModInfo[deviceIndex].maxOutput;
            _rsMaxOutputForce = source.rsModInfo[deviceIndex].maxOutputForce;

            _lSOuterBindDead = source.lsModInfo[deviceIndex].outerBindDeadZone;
            _rSOuterBindDead = source.rsModInfo[deviceIndex].outerBindDeadZone;

            _lSOuterBindInvert = source.lsModInfo[deviceIndex].outerBindInvert;
            _rSOuterBindInvert = source.rsModInfo[deviceIndex].outerBindInvert;

            LSDeadZoneType = source.lsModInfo[deviceIndex].deadzoneType;
            LSAxialDeadOptions = new StickAxialDeadOptionsSerializer()
            {
                DeadZoneX = source.lsModInfo[deviceIndex].xAxisDeadInfo.deadZone,
                DeadZoneY = source.lsModInfo[deviceIndex].yAxisDeadInfo.deadZone,
                MaxZoneX = source.lsModInfo[deviceIndex].xAxisDeadInfo.maxZone,
                MaxZoneY = source.lsModInfo[deviceIndex].yAxisDeadInfo.maxZone,
                AntiDeadZoneX = source.lsModInfo[deviceIndex].xAxisDeadInfo.antiDeadZone,
                AntiDeadZoneY = source.lsModInfo[deviceIndex].yAxisDeadInfo.antiDeadZone,
                MaxOutputX = source.lsModInfo[deviceIndex].xAxisDeadInfo.maxOutput,
                MaxOutputY = source.lsModInfo[deviceIndex].yAxisDeadInfo.maxOutput,
            };
            LSDeltaAccelSettings = new StickDeltaAccelSettings()
            {
                Enabled = source.lsOutputSettings[deviceIndex].outputSettings.controlSettings.deltaAccelSettings.enabled,
                Multiplier = source.lsOutputSettings[deviceIndex].outputSettings.controlSettings.deltaAccelSettings.multiplier,
                MaxTravel = source.lsOutputSettings[deviceIndex].outputSettings.controlSettings.deltaAccelSettings.maxTravel,
                MinTravel = source.lsOutputSettings[deviceIndex].outputSettings.controlSettings.deltaAccelSettings.minTravel,
                EasingDuration = source.lsOutputSettings[deviceIndex].outputSettings.controlSettings.deltaAccelSettings.easingDuration,
                MinFactor = source.lsOutputSettings[deviceIndex].outputSettings.controlSettings.deltaAccelSettings.minfactor,
            };

            RSDeadZoneType = source.rsModInfo[deviceIndex].deadzoneType;
            RSAxialDeadOptions = new StickAxialDeadOptionsSerializer()
            {
                DeadZoneX = source.rsModInfo[deviceIndex].xAxisDeadInfo.deadZone,
                DeadZoneY = source.rsModInfo[deviceIndex].yAxisDeadInfo.deadZone,
                MaxZoneX = source.rsModInfo[deviceIndex].xAxisDeadInfo.maxZone,
                MaxZoneY = source.rsModInfo[deviceIndex].yAxisDeadInfo.maxZone,
                AntiDeadZoneX = source.rsModInfo[deviceIndex].xAxisDeadInfo.antiDeadZone,
                AntiDeadZoneY = source.rsModInfo[deviceIndex].yAxisDeadInfo.antiDeadZone,
                MaxOutputX = source.rsModInfo[deviceIndex].xAxisDeadInfo.maxOutput,
                MaxOutputY = source.rsModInfo[deviceIndex].yAxisDeadInfo.maxOutput,
            };
            RSDeltaAccelSettings = new StickDeltaAccelSettings()
            {
                Enabled = source.rsOutputSettings[deviceIndex].outputSettings.controlSettings.deltaAccelSettings.enabled,
                Multiplier = source.rsOutputSettings[deviceIndex].outputSettings.controlSettings.deltaAccelSettings.multiplier,
                MaxTravel = source.rsOutputSettings[deviceIndex].outputSettings.controlSettings.deltaAccelSettings.maxTravel,
                MinTravel = source.rsOutputSettings[deviceIndex].outputSettings.controlSettings.deltaAccelSettings.minTravel,
                EasingDuration = source.rsOutputSettings[deviceIndex].outputSettings.controlSettings.deltaAccelSettings.easingDuration,
                MinFactor = source.rsOutputSettings[deviceIndex].outputSettings.controlSettings.deltaAccelSettings.minfactor,
            };

            SXDeadZone = source.SXDeadzone[deviceIndex];
            SZDeadZone = source.SZDeadzone[deviceIndex];
            _sxMaxZone = source.SXMaxzone[deviceIndex];
            _szMaxZone = source.SZMaxzone[deviceIndex];
            _sxAntiDeadZone = source.SXAntiDeadzone[deviceIndex];
            _szAntiDeadZone = source.SZAntiDeadzone[deviceIndex];

            Sensitivity = $"{source.LSSens[deviceIndex]}|{source.RSSens[deviceIndex]}|{source.l2Sens[deviceIndex]}|{source.r2Sens[deviceIndex]}|{source.SXSens[deviceIndex]}|{source.SZSens[deviceIndex]}";

            ChargingType = lightInfo.chargingType;
            _mouseAcceleration = source.buttonMouseInfos[deviceIndex].mouseAccel;
            //ShiftModifier = source.
            LaunchProgram = source.launchProgram[deviceIndex];
            _dinputOnly = source.dinputOnly[deviceIndex];
            _startTouchpadOff = source.startTouchpadOff[deviceIndex];
            _useTPforControls = source.touchOutMode[deviceIndex] == TouchpadOutMode.Controls;
            _useSAforMouse = source.gyroOutMode[deviceIndex] == GyroOutMode.Mouse;
            SATriggers = source.sATriggers[deviceIndex];
            _sATriggerCond = source.sATriggerCond[deviceIndex];
            SASteeringWheelEmulationAxis = source.sASteeringWheelEmulationAxis[deviceIndex];
            SASteeringWheelEmulationRange = source.sASteeringWheelEmulationRange[deviceIndex];
            SASteeringWheelSmoothingOptions = new SASteeringWheelSmoothingOptions()
            {
                SASteeringWheelUseSmoothing = source.wheelSmoothInfo[deviceIndex].enabled,
                SASteeringWheelSmoothMinCutoff = source.wheelSmoothInfo[deviceIndex].MinCutoff,
                SASteeringWheelSmoothBeta = source.wheelSmoothInfo[deviceIndex].Beta,
            };

            SASteeringWheelFuzz = source.saWheelFuzzValues[deviceIndex];
            GyroOutputMode = source.gyroOutMode[deviceIndex];
            GyroControlsSettings = new GyroControlsSettings()
            {
                Triggers = source.gyroControlsInf[deviceIndex].triggers,
                TriggerCond = source.gyroControlsInf[deviceIndex].triggerCond,
                TriggerTurns = source.gyroControlsInf[deviceIndex].triggerTurns,
                Toggle = source.gyroControlsInf[deviceIndex].triggerToggle,
            };

            _gyroMouseStickTriggers = source.sAMouseStickTriggers[deviceIndex];
            _gyroMouseStickTriggerCond = source.sAMouseStickTriggerCond[deviceIndex];
            _gyroMouseStickTriggerTurns = source.gyroMouseStickTriggerTurns[deviceIndex];
            GyroMouseStickHAxis = source.gyroMouseStickHorizontalAxis[deviceIndex];
            GyroMouseStickDeadZone = source.gyroMStickInfo[deviceIndex].deadZone;
            GyroMouseStickMaxZone = source.gyroMStickInfo[deviceIndex].maxZone;
            GyroMouseStickOutputStick = source.gyroMStickInfo[deviceIndex].outputStick;
            GyroMouseStickOutputStickAxes = source.gyroMStickInfo[deviceIndex].outputStickDir;
            GyroMouseStickAntiDeadX = source.gyroMStickInfo[deviceIndex].antiDeadX;
            GyroMouseStickAntiDeadY = source.gyroMStickInfo[deviceIndex].antiDeadY;
            GyroMouseStickInvert = source.gyroMStickInfo[deviceIndex].inverted;
            _gyroMouseStickToggle = source.gyroMouseStickToggle[deviceIndex];
            GyroMouseStickMaxOutput = source.gyroMStickInfo[deviceIndex].maxOutput;
            _gyroMouseStickMaxOutputEnabled = source.gyroMStickInfo[deviceIndex].maxOutputEnabled;
            GyroMouseStickVerticalScale = source.gyroMStickInfo[deviceIndex].vertScale;
            _gyroMouseStickJitterCompensation = source.gyroMStickInfo[deviceIndex].jitterCompensation;
            GyroMouseStickSmoothingSettings = new GyroMouseStickSmoothingSettings()
            {
                UseSmoothing = source.gyroMStickInfo[deviceIndex].useSmoothing,
                SmoothingMethod = source.gyroMStickInfo[deviceIndex].SmoothMethodIdentifier(),
                SmoothingWeightRaw = source.gyroMStickInfo[deviceIndex].smoothWeight,
                SmoothingMinCutoff = source.gyroMStickInfo[deviceIndex].minCutoff,
                SmoothingBeta = source.gyroMStickInfo[deviceIndex].beta,
            };
            GyroSwipeSettings = new GyroSwipeSettings()
            {
                DeadZoneX = source.gyroSwipeInfo[deviceIndex].deadzoneX,
                DeadZoneY = source.gyroSwipeInfo[deviceIndex].deadzoneY,
                Triggers = source.gyroSwipeInfo[deviceIndex].triggers,
                TriggerCond = source.gyroSwipeInfo[deviceIndex].triggerCond,
                TriggerTurns = source.gyroSwipeInfo[deviceIndex].triggerTurns,
                XAxis = source.gyroSwipeInfo[deviceIndex].xAxis,
                DelayTime = source.gyroSwipeInfo[deviceIndex].delayTime,
            };
            TouchpadOutputMode = source.touchOutMode[deviceIndex];
            _touchDisInvTriggers = source.touchDisInvertTriggers[deviceIndex];
            GyroSensitivity = source.gyroSensitivity[deviceIndex];
            GyroSensVerticalScale = source.gyroSensVerticalScale[deviceIndex];
            GyroInvert = source.gyroInvert[deviceIndex];
            _gyroTriggerTurns = source.gyroTriggerTurns[deviceIndex];
            GyroMouseSmoothingSettings = new GyroMouseSmoothingSettings()
            {
                UseSmoothing = source.gyroMouseInfo[deviceIndex].enableSmoothing,
                SmoothingMethod = source.gyroMouseInfo[deviceIndex].SmoothMethodIdentifier(),
                SmoothingWeightRaw = source.gyroMouseInfo[deviceIndex].smoothingWeight,
                SmoothingMinCutoff = source.gyroMouseInfo[deviceIndex].minCutoff,
                SmoothingBeta = source.gyroMouseInfo[deviceIndex].beta,
            };
            GyroMouseHAxis = source.gyroMouseHorizontalAxis[deviceIndex];
            GyroMouseDeadZone = source.gyroMouseDZ[deviceIndex];
            GyroMouseMinThreshold = source.gyroMouseInfo[deviceIndex].minThreshold;
            _gyroMouseJitterCompensation = source.gyroMouseInfo[deviceIndex].jitterCompensation;
            _gyroMouseToggle = source.gyroMouseToggle[deviceIndex];
            _bTPollRate = source.btPollRate[deviceIndex];

            LSOutputCurveCustom = source.lsOutBezierCurveObj[deviceIndex].CustomDefinition;
            LSOutputCurveMode = source.stickOutputCurveString(source.getLsOutCurveMode(deviceIndex));
            RSOutputCurveCustom = source.rsOutBezierCurveObj[deviceIndex].CustomDefinition;
            RSOutputCurveMode = source.stickOutputCurveString(source.getRsOutCurveMode(deviceIndex));
            _lsSquareStick = source.squStickInfo[deviceIndex].lsMode;
            SquareStickRoundness = source.squStickInfo[deviceIndex].lsRoundness;
            _rsSquareStick = source.squStickInfo[deviceIndex].rsMode;
            SquareRStickRoundness = source.squStickInfo[deviceIndex].rsRoundness;
            _lsAntiSnapback = source.lsAntiSnapbackInfo[deviceIndex].enabled;
            _rsAntiSnapback = source.rsAntiSnapbackInfo[deviceIndex].enabled;
            LSAntiSnapbackDelta = source.lsAntiSnapbackInfo[deviceIndex].delta;
            RSAntiSnapbackDelta = source.rsAntiSnapbackInfo[deviceIndex].delta;
            LSAntiSnapbackTimeout = source.lsAntiSnapbackInfo[deviceIndex].timeout;
            RSAntiSnapbackTimeout = source.rsAntiSnapbackInfo[deviceIndex].timeout;
            LSOutputMode = source.lsOutputSettings[deviceIndex].mode;
            RSOutputMode = source.rsOutputSettings[deviceIndex].mode;
            LSOutputSettings = new StickModeOutputSettings()
            {
                FlickStickSettings = new FlickStickSettings()
                {
                    RealWorldCalibration = source.lsOutputSettings[deviceIndex].outputSettings.flickSettings.realWorldCalibration,
                    FlickThreshold = source.lsOutputSettings[deviceIndex].outputSettings.flickSettings.flickThreshold,
                    FlickTime = source.lsOutputSettings[deviceIndex].outputSettings.flickSettings.flickTime,
                    MinAngleThreshold = source.lsOutputSettings[deviceIndex].outputSettings.flickSettings.minAngleThreshold,
                },
            };
            RSOutputSettings = new StickModeOutputSettings()
            {
                FlickStickSettings = new FlickStickSettings()
                {
                    RealWorldCalibration = source.rsOutputSettings[deviceIndex].outputSettings.flickSettings.realWorldCalibration,
                    FlickThreshold = source.rsOutputSettings[deviceIndex].outputSettings.flickSettings.flickThreshold,
                    FlickTime = source.rsOutputSettings[deviceIndex].outputSettings.flickSettings.flickTime,
                    MinAngleThreshold = source.rsOutputSettings[deviceIndex].outputSettings.flickSettings.minAngleThreshold,
                },
            };

            DualSenseControllerSettings = new DualSenseControllerSettings()
            {
                RumbleSettingsGroup = new DualSenseControllerSettings.RumbleSettings()
                {
                    EmulationMode = source.dualSenseRumbleEmulationMode[deviceIndex],
                    EnableGenericRumbleRescale = source.useGenericRumbleRescaleForDualSenses[deviceIndex],
                    HapticPowerLevel = source.dualSenseHapticPowerLevel[deviceIndex],
                }
            };

            L2OutputCurveCustom = source.l2OutBezierCurveObj[deviceIndex].CustomDefinition;
            L2OutputCurveMode = source.stickOutputCurveString(source.getL2OutCurveMode(deviceIndex));
            L2TwoStageMode = source.l2OutputSettings[deviceIndex].twoStageMode;
            L2HipFireTime = source.l2OutputSettings[deviceIndex].hipFireMS;
            L2TriggerEffect = source.l2OutputSettings[deviceIndex].triggerEffect;

            R2OutputCurveCustom = source.r2OutBezierCurveObj[deviceIndex].CustomDefinition;
            R2OutputCurveMode = source.stickOutputCurveString(source.getR2OutCurveMode(deviceIndex));
            R2TwoStageMode = source.r2OutputSettings[deviceIndex].twoStageMode;
            R2HipFireTime = source.r2OutputSettings[deviceIndex].hipFireMS;
            R2TriggerEffect = source.r2OutputSettings[deviceIndex].triggerEffect;

            SXOutputCurveCustom = source.sxOutBezierCurveObj[deviceIndex].CustomDefinition;
            SXOutputCurveMode = source.stickOutputCurveString(source.getSXOutCurveMode(deviceIndex));
            SZOutputCurveCustom = source.szOutBezierCurveObj[deviceIndex].CustomDefinition;
            SZOutputCurveMode = source.stickOutputCurveString(source.getSZOutCurveMode(deviceIndex));
            _trackballMode = source.trackballMode[deviceIndex];
            TrackballFriction = source.trackballFriction[deviceIndex];
            TouchRelMouseRotation = source.touchpadRelMouse[deviceIndex].rotation;
            TouchRelMouseMinThreshold = source.touchpadRelMouse[deviceIndex].minThreshold;

            TouchpadMouseStickSettings = new TouchpadMouseStickSerializer()
            {
                DeadZone = source.touchMStickInfo[deviceIndex].deadZone,
                MaxZone = source.touchMStickInfo[deviceIndex].maxZone,
                OutputStick = source.touchMStickInfo[deviceIndex].outputStick,
                OutputStickAxes = source.touchMStickInfo[deviceIndex].outputStickDir,
                AntiDeadX = source.touchMStickInfo[deviceIndex].antiDeadX,
                AntiDeadY = source.touchMStickInfo[deviceIndex].antiDeadY,
                Invert = source.touchMStickInfo[deviceIndex].inverted,
                MaxOutput = source.touchMStickInfo[deviceIndex].maxOutput,
                MaxOutputEnabled = source.touchMStickInfo[deviceIndex].maxOutputEnabled,
                VerticalScale = source.touchMStickInfo[deviceIndex].vertScale,
                OutputCurve = source.touchMStickInfo[deviceIndex].outputCurve,
                RotationRad = source.touchMStickInfo[deviceIndex].rotationRad,
                SmoothingSettings = new TouchpadMouseStickSerializer.SmoothingGroupSerializer()
                {
                    SmoothingMethod = source.touchMStickInfo[deviceIndex].smoothingMethod,
                    SmoothingMinCutoff = source.touchMStickInfo[deviceIndex].minCutoff,
                    SmoothingBeta = source.touchMStickInfo[deviceIndex].beta,
                },
            };

            TouchpadButtonMode = source.touchpadButtonMode[deviceIndex];

            OutputContDevice = source.outputDevType[deviceIndex];
            OutputDS4TriggerMode = source.outputDS4TriggerMode[deviceIndex];

            ProfileActions = string.Join("/", source.profileActions[deviceIndex]);
            Control = new DS4ControlAssignementSerializer();
            ShiftControl = new DS4ControlAssignementSerializer();

            DS4ControlButtonAssignmentSerializer buttonSerializer = new DS4ControlButtonAssignmentSerializer();
            DS4ControlKeyAssignmentSerializer keySerializer = new DS4ControlKeyAssignmentSerializer();
            DS4ControlKeyTypeAssignmentSerializer keyTypeSerializer = new DS4ControlKeyTypeAssignmentSerializer();
            DS4ControlMacroAssignmentSerializer macroSerializer = new DS4ControlMacroAssignmentSerializer();
            DS4ControlExtrasAssignmentSerializer extrasSerializer = new DS4ControlExtrasAssignmentSerializer();
            DS4ControlLightbarMacroAssignmentSerializer lightbarMacroSerializer = new();

            DS4ControlButtonAssignmentSerializer shiftButtonSerializer = new DS4ControlButtonAssignmentSerializer();
            DS4ControlKeyAssignmentSerializer shiftKeySerializer = new DS4ControlKeyAssignmentSerializer();
            DS4ControlKeyTypeAssignmentSerializer shiftKeyTypeSerializer = new DS4ControlKeyTypeAssignmentSerializer();
            DS4ControlMacroAssignmentSerializer shiftMacroSerializer = new DS4ControlMacroAssignmentSerializer();
            DS4ControlExtrasAssignmentSerializer shiftExtrasSerializer = new DS4ControlExtrasAssignmentSerializer();

            foreach (DS4ControlSettings dcs in source.ds4settings[deviceIndex])
            {
                if (dcs.actionType != DS4ControlSettings.ActionType.Default)
                {
                    if (dcs.keyType != DS4KeyType.None)
                    {
                        keyTypeSerializer.CustomMapKeyTypes.TryAdd(dcs.control, dcs.keyType);
                    }

                    if (dcs.actionType == DS4ControlSettings.ActionType.Button)
                    {
                        if (dcs.action.actionBtn == X360Controls.Unbound &&
                            !dcs.keyType.HasFlag(DS4KeyType.Unbound))
                        {
                            DS4KeyType tempFlags = DS4KeyType.Unbound;
                            if (keyTypeSerializer.CustomMapKeyTypes.ContainsKey(dcs.control))
                            {
                                tempFlags = keyTypeSerializer.CustomMapKeyTypes[dcs.control];
                                tempFlags |= DS4KeyType.Unbound;
                                keyTypeSerializer.CustomMapKeyTypes[dcs.control] = tempFlags;
                            }
                            else
                            {
                                keyTypeSerializer.CustomMapKeyTypes.TryAdd(dcs.control, tempFlags);
                            }
                        }

                        buttonSerializer.CustomMapButtons.TryAdd(dcs.control, dcs.action.actionBtn);
                    }
                    else if (dcs.actionType == DS4ControlSettings.ActionType.Key)
                    {
                        keySerializer.CustomMapKeys.TryAdd(dcs.control, (ushort)dcs.action.actionKey);
                    }
                    else if (dcs.actionType == DS4ControlSettings.ActionType.Macro)
                    {
                        macroSerializer.CustomMapMacros[dcs.control] =
                            string.Join("/", dcs.action.actionMacro);
                    }
                }

                bool hasExtrasValue = false;
                if (!string.IsNullOrEmpty(dcs.extras))
                {
                    foreach (string s in dcs.extras.Split(','))
                    {
                        if (s != "0")
                        {
                            hasExtrasValue = true;
                            break;
                        }
                    }
                }

                if (hasExtrasValue)
                {
                    extrasSerializer.CustomMapExtras.Add(dcs.control, dcs.extras);
                }

                if (dcs.LightbarMacroString is not null && dcs.LightbarMacroString != string.Empty)
                {
                    lightbarMacroSerializer.CustomMapMacro.Add(dcs.control, dcs.LightbarMacroString);
                }

                if (dcs.shiftActionType != DS4ControlSettings.ActionType.Default && dcs.shiftTrigger > 0)
                {
                    if (dcs.shiftKeyType != DS4KeyType.None)
                    {
                        shiftKeyTypeSerializer.CustomMapKeyTypes.TryAdd(dcs.control, dcs.shiftKeyType);
                    }

                    if (dcs.shiftActionType == DS4ControlSettings.ActionType.Button)
                    {
                        if (dcs.shiftAction.actionBtn == X360Controls.Unbound &&
                            !dcs.shiftKeyType.HasFlag(DS4KeyType.Unbound))
                        {
                            DS4KeyType tempFlags = DS4KeyType.Unbound;
                            if (shiftKeyTypeSerializer.CustomMapKeyTypes.ContainsKey(dcs.control))
                            {
                                tempFlags = shiftKeyTypeSerializer.CustomMapKeyTypes[dcs.control];
                                tempFlags |= DS4KeyType.Unbound;
                                shiftKeyTypeSerializer.CustomMapKeyTypes[dcs.control] = tempFlags;
                            }
                            else
                            {
                                shiftKeyTypeSerializer.CustomMapKeyTypes.TryAdd(dcs.control, tempFlags);
                            }
                        }

                        shiftButtonSerializer.CustomMapButtons.TryAdd(dcs.control, dcs.shiftAction.actionBtn);
                        shiftButtonSerializer.ShiftTriggers.TryAdd(dcs.control, dcs.shiftTrigger);
                    }
                    else if (dcs.shiftActionType == DS4ControlSettings.ActionType.Key)
                    {
                        shiftKeySerializer.CustomMapKeys.TryAdd(dcs.control, (ushort)dcs.shiftAction.actionKey);
                        shiftKeySerializer.ShiftTriggers.TryAdd(dcs.control, dcs.shiftTrigger);
                    }
                    else if (dcs.shiftActionType == DS4ControlSettings.ActionType.Macro)
                    {
                        shiftMacroSerializer.CustomMapMacros[dcs.control] =
                            string.Join("/", dcs.shiftAction.actionMacro);
                        shiftMacroSerializer.ShiftTriggers.TryAdd(dcs.control, dcs.shiftTrigger);
                    }
                }

                hasExtrasValue = false;
                if (!string.IsNullOrEmpty(dcs.shiftExtras))
                {
                    foreach (string s in dcs.shiftExtras.Split(','))
                    {
                        if (s != "0")
                        {
                            hasExtrasValue = true;
                            break;
                        }
                    }
                }
            }

            if (buttonSerializer.CustomMapButtons.Count > 0)
            {
                Control.Button = buttonSerializer;
            }

            if (keySerializer.CustomMapKeys.Count > 0)
            {
                Control.Key = keySerializer;
            }

            if (keyTypeSerializer.CustomMapKeyTypes.Count > 0)
            {
                Control.KeyType = keyTypeSerializer;
            }

            if (macroSerializer.CustomMapMacros.Count > 0)
            {
                Control.Macro = macroSerializer;
            }

            if (extrasSerializer.CustomMapExtras.Count > 0)
            {
                Control.Extras = extrasSerializer;
            }

            if (lightbarMacroSerializer.CustomMapMacro.Count > 0)
            {
                Control.LightbarMacro = lightbarMacroSerializer;
            }


            if (shiftButtonSerializer.CustomMapButtons.Count > 0)
            {
                ShiftControl.Button = shiftButtonSerializer;
            }

            if (shiftKeySerializer.CustomMapKeys.Count > 0)
            {
                ShiftControl.Key = shiftKeySerializer;
            }

            if (shiftKeyTypeSerializer.CustomMapKeyTypes.Count > 0)
            {
                ShiftControl.KeyType = shiftKeyTypeSerializer;
            }

            if (shiftMacroSerializer.CustomMapMacros.Count > 0)
            {
                ShiftControl.Macro = shiftMacroSerializer;
            }

            if (shiftExtrasSerializer.CustomMapExtras.Count > 0)
            {
                ShiftControl.Extras = shiftExtrasSerializer;
            }
        }

        public void MapTo(BackingStore destination)
        {
            if (deviceIndex == -1)
            {
                throw new ArgumentOutOfRangeException("Device Index must be set");
            }

            PostProcessXml();

            LightbarSettingInfo lightbarSettings = destination.lightbarSettingInfo[deviceIndex];
            LightbarDS4WinInfo lightInfo = lightbarSettings.ds4winSettings;

            destination.enableTouchToggle[deviceIndex] = TouchToggle;
            destination.idleDisconnectTimeout[deviceIndex] = IdleDisconnect;
            destination.enableOutputDataToDS4[deviceIndex] = OutputDataToDS4;
            destination.lightbarSettingInfo[deviceIndex].mode = LightbarMode;
            lightInfo.m_Led = _ledColor;

            destination.useDs3PitchRollSim = UseDs3PitchRollSim;
            destination.debouncingMs[deviceIndex] = DebouncingMs;
            destination.inverseRumbleMotors[deviceIndex] = InverseRumbleMotors;
            destination.leftStickDriftXAxis[deviceIndex] = LeftStickDriftXAxis;
            destination.leftStickDriftYAxis[deviceIndex] = LeftStickDriftYAxis;
            destination.rightStickDriftXAxis[deviceIndex] = RightStickDriftXAxis;
            destination.rightStickDriftYAxis[deviceIndex] = RightStickDriftYAxis;

            destination.rumble[deviceIndex] = RumbleBoost;
            destination.rumbleAutostopTime[deviceIndex] = RumbleAutostopTime;
            lightInfo.ledAsBattery = LedAsBatteryIndicator;
            lightInfo.flashType = FlashType;
            lightInfo.flashAt = FlashBatteryAt;
            destination.touchSensitivity[deviceIndex] = TouchSensitivity;
            lightInfo.m_LowLed = _lowColor;
            lightInfo.m_ChargingLed = _chargingColor;
            lightInfo.m_FlashLed = _flashColor;

            destination.touchpadJitterCompensation[deviceIndex] = TouchpadJitterCompensation;
            destination.lowerRCOn[deviceIndex] = LowerRCOn;
            destination.tapSensitivity[deviceIndex] = TapSensitivity;
            destination.doubleTap[deviceIndex] = DoubleTap;
            destination.scrollSensitivity[deviceIndex] = ScrollSensitivity;
            destination.touchpadInvert[deviceIndex] = TouchpadInvert;
            destination.touchClickPassthru[deviceIndex] = TouchpadClickPassthru;
            destination.l2ModInfo[deviceIndex].deadZone = LeftTriggerMiddle;
            destination.r2ModInfo[deviceIndex].deadZone = RightTriggerMiddle;
            destination.l2ModInfo[deviceIndex].antiDeadZone = L2AntiDeadZone;
            destination.r2ModInfo[deviceIndex].antiDeadZone = R2AntiDeadZone;
            destination.l2ModInfo[deviceIndex].maxZone = L2MaxZone;
            destination.r2ModInfo[deviceIndex].maxZone = R2MaxZone;
            destination.l2ModInfo[deviceIndex].maxOutput = L2MaxOutput;
            destination.r2ModInfo[deviceIndex].maxOutput = R2MaxOutput;
            destination.LSRotation[deviceIndex] = _lSRotation;
            destination.RSRotation[deviceIndex] = _rSRotation;
            destination.lsModInfo[deviceIndex].fuzz = LSFuzz;
            destination.rsModInfo[deviceIndex].fuzz = RSFuzz;
            destination.buttonMouseInfos[deviceIndex].buttonSensitivity = ButtonMouseSensitivity;
            destination.buttonMouseInfos[deviceIndex].mouseVelocityOffset = ButtonMouseOffset;
            destination.buttonMouseInfos[deviceIndex].buttonVerticalScale = _buttonMouseVerticalScale;
            lightInfo.rainbow = Rainbow;
            lightInfo.maxRainbowSat = _maxSatRainbow;
            destination.lsModInfo[deviceIndex].deadZone = LSDeadZone;
            destination.rsModInfo[deviceIndex].deadZone = RSDeadZone;
            destination.lsModInfo[deviceIndex].antiDeadZone = LSAntiDeadZone;
            destination.rsModInfo[deviceIndex].antiDeadZone = RSAntiDeadZone;
            destination.lsModInfo[deviceIndex].maxZone = LSMaxZone;
            destination.rsModInfo[deviceIndex].maxZone = RSMaxZone;
            destination.lsModInfo[deviceIndex].verticalScale = LSVerticalScale;
            destination.lsModInfo[deviceIndex].maxOutput = LSMaxOutput;
            destination.lsModInfo[deviceIndex].maxOutputForce = _lsMaxOutputForce;

            destination.rsModInfo[deviceIndex].verticalScale = RSVerticalScale;
            destination.rsModInfo[deviceIndex].maxOutput = RSMaxOutput;
            destination.rsModInfo[deviceIndex].maxOutputForce = _rsMaxOutputForce;

            destination.lsModInfo[deviceIndex].outerBindDeadZone = _lSOuterBindDead;
            destination.rsModInfo[deviceIndex].outerBindDeadZone = _rSOuterBindDead;

            destination.lsModInfo[deviceIndex].outerBindInvert = _lSOuterBindInvert;
            destination.rsModInfo[deviceIndex].outerBindInvert = _rSOuterBindInvert;

            destination.lsModInfo[deviceIndex].deadzoneType = LSDeadZoneType;

            if (LSAxialDeadOptions != null)
            {
                destination.lsModInfo[deviceIndex].xAxisDeadInfo.deadZone = LSAxialDeadOptions.DeadZoneX;
                destination.lsModInfo[deviceIndex].yAxisDeadInfo.deadZone = LSAxialDeadOptions.DeadZoneY;
                destination.lsModInfo[deviceIndex].xAxisDeadInfo.maxZone = LSAxialDeadOptions.MaxZoneX;
                destination.lsModInfo[deviceIndex].yAxisDeadInfo.maxZone = LSAxialDeadOptions.MaxZoneY;
                destination.lsModInfo[deviceIndex].xAxisDeadInfo.antiDeadZone = LSAxialDeadOptions.AntiDeadZoneX;
                destination.lsModInfo[deviceIndex].yAxisDeadInfo.antiDeadZone = LSAxialDeadOptions.AntiDeadZoneY;
                destination.lsModInfo[deviceIndex].xAxisDeadInfo.maxOutput = LSAxialDeadOptions.MaxOutputX;
                destination.lsModInfo[deviceIndex].yAxisDeadInfo.maxOutput = LSAxialDeadOptions.MaxOutputY;
            }

            if (LSDeltaAccelSettings != null)
            {
                destination.lsOutputSettings[deviceIndex].outputSettings.controlSettings.deltaAccelSettings.enabled = LSDeltaAccelSettings.Enabled;
                destination.lsOutputSettings[deviceIndex].outputSettings.controlSettings.deltaAccelSettings.multiplier = LSDeltaAccelSettings.Multiplier;
                destination.lsOutputSettings[deviceIndex].outputSettings.controlSettings.deltaAccelSettings.maxTravel = LSDeltaAccelSettings.MaxTravel;
                destination.lsOutputSettings[deviceIndex].outputSettings.controlSettings.deltaAccelSettings.minTravel = LSDeltaAccelSettings.MinTravel;
                destination.lsOutputSettings[deviceIndex].outputSettings.controlSettings.deltaAccelSettings.easingDuration = LSDeltaAccelSettings.EasingDuration;
                destination.lsOutputSettings[deviceIndex].outputSettings.controlSettings.deltaAccelSettings.minfactor = LSDeltaAccelSettings.MinFactor;
            }

            destination.rsModInfo[deviceIndex].deadzoneType = RSDeadZoneType;
            if (RSAxialDeadOptions != null)
            {
                destination.rsModInfo[deviceIndex].xAxisDeadInfo.deadZone = RSAxialDeadOptions.DeadZoneX;
                destination.rsModInfo[deviceIndex].yAxisDeadInfo.deadZone = RSAxialDeadOptions.DeadZoneY;
                destination.rsModInfo[deviceIndex].xAxisDeadInfo.maxZone = RSAxialDeadOptions.MaxZoneX;
                destination.rsModInfo[deviceIndex].yAxisDeadInfo.maxZone = RSAxialDeadOptions.MaxZoneY;
                destination.rsModInfo[deviceIndex].xAxisDeadInfo.antiDeadZone = RSAxialDeadOptions.AntiDeadZoneX;
                destination.rsModInfo[deviceIndex].yAxisDeadInfo.antiDeadZone = RSAxialDeadOptions.AntiDeadZoneY;
                destination.rsModInfo[deviceIndex].xAxisDeadInfo.maxOutput = RSAxialDeadOptions.MaxOutputX;
                destination.rsModInfo[deviceIndex].yAxisDeadInfo.maxOutput = RSAxialDeadOptions.MaxOutputY;
            }

            if (RSDeltaAccelSettings != null)
            {
                destination.rsOutputSettings[deviceIndex].outputSettings.controlSettings.deltaAccelSettings.enabled = RSDeltaAccelSettings.Enabled;
                destination.rsOutputSettings[deviceIndex].outputSettings.controlSettings.deltaAccelSettings.multiplier = RSDeltaAccelSettings.Multiplier;
                destination.rsOutputSettings[deviceIndex].outputSettings.controlSettings.deltaAccelSettings.maxTravel = RSDeltaAccelSettings.MaxTravel;
                destination.rsOutputSettings[deviceIndex].outputSettings.controlSettings.deltaAccelSettings.minTravel = RSDeltaAccelSettings.MinTravel;
                destination.rsOutputSettings[deviceIndex].outputSettings.controlSettings.deltaAccelSettings.easingDuration = RSDeltaAccelSettings.EasingDuration;
                destination.rsOutputSettings[deviceIndex].outputSettings.controlSettings.deltaAccelSettings.minfactor = RSDeltaAccelSettings.MinFactor;
            }

            destination.SXDeadzone[deviceIndex] = SXDeadZone;
            destination.SZDeadzone[deviceIndex] = SZDeadZone;
            destination.SXMaxzone[deviceIndex] = _sxMaxZone;
            destination.SZMaxzone[deviceIndex] = _szMaxZone;
            destination.SXAntiDeadzone[deviceIndex] = _sxAntiDeadZone;
            destination.SZAntiDeadzone[deviceIndex] = _szAntiDeadZone;

            if (!string.IsNullOrEmpty(Sensitivity))
            {
                string[] s = Sensitivity.Split('|');
                if (s.Length == 1)
                    s = Sensitivity.Split(',');

                try
                {
                    if (double.TryParse(s[0], out double temp))
                    {
                        destination.LSSens[deviceIndex] = Math.Clamp(temp, 0.1, 5.0);
                    }
                    else
                    {
                        destination.LSSens[deviceIndex] = 1.0;
                    }

                    if (double.TryParse(s[1], out double tempRSSens))
                    {
                        destination.RSSens[deviceIndex] = Math.Clamp(tempRSSens, 0.1, 5.0);
                    }
                    else
                    {
                        destination.RSSens[deviceIndex] = 1.0;
                    }

                    if (double.TryParse(s[2], out double tempL2Sens))
                    {
                        destination.l2Sens[deviceIndex] = Math.Clamp(tempL2Sens, 0.1, 10.0);
                    }
                    else
                    {
                        destination.l2Sens[deviceIndex] = 1.0;
                    }

                    if (double.TryParse(s[3], out double tempR2Sens))
                    {
                        destination.r2Sens[deviceIndex] = Math.Clamp(tempR2Sens, 0.1, 10.0);
                    }
                    else
                    {
                        destination.r2Sens[deviceIndex] = 1.0;
                    }

                    if (double.TryParse(s[4], out double tempSXSens))
                    {
                        destination.SXSens[deviceIndex] = Math.Clamp(tempSXSens, 0.0, 5.0);
                    }
                    else
                    {
                        destination.SXSens[deviceIndex] = 1.0;
                    }

                    if (double.TryParse(s[5], out double tempSZSens))
                    {
                        destination.SZSens[deviceIndex] = Math.Clamp(tempSZSens, 0.0, 5.0);
                    }
                    else
                    {
                        destination.SZSens[deviceIndex] = 1.0;
                    }
                }
                catch { }
            }

            lightInfo.chargingType = ChargingType;
            destination.buttonMouseInfos[deviceIndex].mouseAccel = _mouseAcceleration;
            //destination. = ShiftModifier 
            destination.launchProgram[deviceIndex] = LaunchProgram;
            destination.dinputOnly[deviceIndex] = _dinputOnly;
            destination.startTouchpadOff[deviceIndex] = _startTouchpadOff;
            destination.sATriggers[deviceIndex] = SATriggers;
            destination.sATriggerCond[deviceIndex] = _sATriggerCond;
            destination.sASteeringWheelEmulationAxis[deviceIndex] = SASteeringWheelEmulationAxis;
            destination.sASteeringWheelEmulationRange[deviceIndex] = SASteeringWheelEmulationRange;

            if (SASteeringWheelSmoothingOptions != null)
            {
                destination.wheelSmoothInfo[deviceIndex].enabled = SASteeringWheelSmoothingOptions.SASteeringWheelUseSmoothing;
                destination.wheelSmoothInfo[deviceIndex].MinCutoff = SASteeringWheelSmoothingOptions.SASteeringWheelSmoothMinCutoff;
                destination.wheelSmoothInfo[deviceIndex].Beta = SASteeringWheelSmoothingOptions.SASteeringWheelSmoothBeta;
            }

            destination.saWheelFuzzValues[deviceIndex] = SASteeringWheelFuzz;
            if (_hasSAforMouse)
            {
                destination.gyroOutMode[deviceIndex] =
                    _useSAforMouse ? GyroOutMode.Mouse : GyroOutMode.Controls;
            }
            else
            {
                destination.gyroOutMode[deviceIndex] = GyroOutputMode;
            }

            if (GyroControlsSettings != null)
            {
                destination.gyroControlsInf[deviceIndex].triggers = GyroControlsSettings.Triggers;
                destination.gyroControlsInf[deviceIndex].triggerCond = GyroControlsSettings.TriggerCond;
                destination.gyroControlsInf[deviceIndex].triggerTurns = GyroControlsSettings.TriggerTurns;
                destination.gyroControlsInf[deviceIndex].triggerToggle = GyroControlsSettings.Toggle;
            }

            destination.sAMouseStickTriggers[deviceIndex] = _gyroMouseStickTriggers;
            destination.sAMouseStickTriggerCond[deviceIndex] = _gyroMouseStickTriggerCond;
            destination.gyroMouseStickTriggerTurns[deviceIndex] = _gyroMouseStickTriggerTurns;
            destination.gyroMouseStickHorizontalAxis[deviceIndex] = GyroMouseStickHAxis;
            destination.gyroMStickInfo[deviceIndex].deadZone = GyroMouseStickDeadZone;
            destination.gyroMStickInfo[deviceIndex].maxZone = GyroMouseStickMaxZone;
            destination.gyroMStickInfo[deviceIndex].outputStick = GyroMouseStickOutputStick;
            destination.gyroMStickInfo[deviceIndex].outputStickDir = GyroMouseStickOutputStickAxes;
            destination.gyroMStickInfo[deviceIndex].antiDeadX = GyroMouseStickAntiDeadX;
            destination.gyroMStickInfo[deviceIndex].antiDeadY = GyroMouseStickAntiDeadY;
            destination.gyroMStickInfo[deviceIndex].inverted = GyroMouseStickInvert;
            destination.gyroMouseStickToggle[deviceIndex] = _gyroMouseStickToggle;
            destination.gyroMStickInfo[deviceIndex].maxOutput = GyroMouseStickMaxOutput;
            destination.gyroMStickInfo[deviceIndex].maxOutputEnabled = _gyroMouseStickMaxOutputEnabled;
            destination.gyroMStickInfo[deviceIndex].vertScale = GyroMouseStickVerticalScale;
            destination.gyroMStickInfo[deviceIndex].jitterCompensation = _gyroMouseStickJitterCompensation;

            if (GyroMouseStickSmoothingSettings != null)
            {
                destination.gyroMStickInfo[deviceIndex].useSmoothing = GyroMouseStickSmoothingSettings.UseSmoothing;
                destination.gyroMStickInfo[deviceIndex].smoothingMethod = GyroMouseStickInfo.SmoothingMethodParse(GyroMouseStickSmoothingSettings.SmoothingMethod);
                destination.gyroMStickInfo[deviceIndex].smoothWeight = GyroMouseStickSmoothingSettings.SmoothingWeightRaw;
                destination.gyroMStickInfo[deviceIndex].minCutoff = GyroMouseStickSmoothingSettings.SmoothingMinCutoff;
                destination.gyroMStickInfo[deviceIndex].beta = GyroMouseStickSmoothingSettings.SmoothingBeta;
            }

            if (GyroSwipeSettings != null)
            {
                destination.gyroSwipeInfo[deviceIndex].deadzoneX = GyroSwipeSettings.DeadZoneX;
                destination.gyroSwipeInfo[deviceIndex].deadzoneY = GyroSwipeSettings.DeadZoneY;
                destination.gyroSwipeInfo[deviceIndex].triggers = GyroSwipeSettings.Triggers;
                destination.gyroSwipeInfo[deviceIndex].triggerCond = GyroSwipeSettings.TriggerCond;
                destination.gyroSwipeInfo[deviceIndex].triggerTurns = GyroSwipeSettings.TriggerTurns;
                destination.gyroSwipeInfo[deviceIndex].xAxis = GyroSwipeSettings.XAxis;
                destination.gyroSwipeInfo[deviceIndex].delayTime = GyroSwipeSettings.DelayTime;
            }

            if (_hasUseTPforControls)
            {
                destination.touchOutMode[deviceIndex] =
                    _useTPforControls ? TouchpadOutMode.Controls : TouchpadOutMode.Mouse;
            }
            else
            {
                destination.touchOutMode[deviceIndex] = TouchpadOutputMode;
            }

            destination.touchDisInvertTriggers[deviceIndex] = _touchDisInvTriggers;
            destination.gyroSensitivity[deviceIndex] = GyroSensitivity;
            destination.gyroSensVerticalScale[deviceIndex] = GyroSensVerticalScale;
            destination.gyroInvert[deviceIndex] = GyroInvert;
            destination.gyroTriggerTurns[deviceIndex] = _gyroTriggerTurns;

            if (GyroMouseSmoothingSettings != null)
            {
                destination.gyroMouseInfo[deviceIndex].enableSmoothing = GyroMouseSmoothingSettings.UseSmoothing;
                destination.gyroMouseInfo[deviceIndex].smoothingMethod = GyroMouseInfo.SmoothingMethodParse(GyroMouseSmoothingSettings.SmoothingMethod);
                destination.gyroMouseInfo[deviceIndex].smoothingWeight = GyroMouseSmoothingSettings.SmoothingWeightRaw;
                destination.gyroMouseInfo[deviceIndex].minCutoff = GyroMouseSmoothingSettings.SmoothingMinCutoff;
                destination.gyroMouseInfo[deviceIndex].beta = GyroMouseSmoothingSettings.SmoothingBeta;
            }

            destination.gyroMouseHorizontalAxis[deviceIndex] = GyroMouseHAxis;
            destination.gyroMouseDZ[deviceIndex] = GyroMouseDeadZone;
            destination.gyroMouseInfo[deviceIndex].minThreshold = GyroMouseMinThreshold;
            destination.gyroMouseInfo[deviceIndex].jitterCompensation = _gyroMouseJitterCompensation;
            destination.gyroMouseToggle[deviceIndex] = _gyroMouseToggle;
            destination.btPollRate[deviceIndex] = _bTPollRate;

            destination.lsOutBezierCurveObj[deviceIndex].CustomDefinition = LSOutputCurveCustom;
            destination.setLsOutCurveMode(deviceIndex, destination.stickOutputCurveId(LSOutputCurveMode));
            destination.rsOutBezierCurveObj[deviceIndex].CustomDefinition = RSOutputCurveCustom;
            destination.setRsOutCurveMode(deviceIndex, destination.stickOutputCurveId(RSOutputCurveMode));
            destination.squStickInfo[deviceIndex].lsMode = _lsSquareStick;
            destination.squStickInfo[deviceIndex].lsRoundness = SquareStickRoundness;
            destination.squStickInfo[deviceIndex].rsMode = _rsSquareStick;
            destination.squStickInfo[deviceIndex].rsRoundness = SquareRStickRoundness;
            destination.lsAntiSnapbackInfo[deviceIndex].enabled = _lsAntiSnapback;
            destination.rsAntiSnapbackInfo[deviceIndex].enabled = _rsAntiSnapback;
            destination.lsAntiSnapbackInfo[deviceIndex].delta = LSAntiSnapbackDelta;
            destination.rsAntiSnapbackInfo[deviceIndex].delta = RSAntiSnapbackDelta;
            destination.lsAntiSnapbackInfo[deviceIndex].timeout = LSAntiSnapbackTimeout;
            destination.rsAntiSnapbackInfo[deviceIndex].timeout = RSAntiSnapbackTimeout;
            destination.lsOutputSettings[deviceIndex].mode = LSOutputMode;
            destination.rsOutputSettings[deviceIndex].mode = RSOutputMode;

            if (LSOutputSettings != null)
            {
                if (LSOutputSettings.FlickStickSettings != null)
                {
                    destination.lsOutputSettings[deviceIndex].outputSettings.flickSettings.realWorldCalibration = LSOutputSettings.FlickStickSettings.RealWorldCalibration;
                    destination.lsOutputSettings[deviceIndex].outputSettings.flickSettings.flickThreshold = LSOutputSettings.FlickStickSettings.FlickThreshold;
                    destination.lsOutputSettings[deviceIndex].outputSettings.flickSettings.flickTime = LSOutputSettings.FlickStickSettings.FlickTime;
                    destination.lsOutputSettings[deviceIndex].outputSettings.flickSettings.minAngleThreshold = LSOutputSettings.FlickStickSettings.MinAngleThreshold;
                }
            }

            if (RSOutputSettings != null)
            {
                if (RSOutputSettings.FlickStickSettings != null)
                {
                    destination.rsOutputSettings[deviceIndex].outputSettings.flickSettings.realWorldCalibration = RSOutputSettings.FlickStickSettings.RealWorldCalibration;
                    destination.rsOutputSettings[deviceIndex].outputSettings.flickSettings.flickThreshold = RSOutputSettings.FlickStickSettings.FlickThreshold;
                    destination.rsOutputSettings[deviceIndex].outputSettings.flickSettings.flickTime = RSOutputSettings.FlickStickSettings.FlickTime;
                    destination.rsOutputSettings[deviceIndex].outputSettings.flickSettings.minAngleThreshold = RSOutputSettings.FlickStickSettings.MinAngleThreshold;
                }
            }

            if (DualSenseControllerSettings != null)
            {
                if (DualSenseControllerSettings.RumbleSettingsGroup != null)
                {
                    destination.dualSenseRumbleEmulationMode[deviceIndex] = DualSenseControllerSettings.RumbleSettingsGroup.EmulationMode;
                    destination.useGenericRumbleRescaleForDualSenses[deviceIndex] = DualSenseControllerSettings.RumbleSettingsGroup.EnableGenericRumbleRescale;
                    destination.dualSenseHapticPowerLevel[deviceIndex] = DualSenseControllerSettings.RumbleSettingsGroup.HapticPowerLevel;
                }
            }

            destination.l2OutBezierCurveObj[deviceIndex].CustomDefinition = L2OutputCurveCustom;
            destination.setL2OutCurveMode(deviceIndex, destination.stickOutputCurveId(L2OutputCurveMode));
            destination.l2OutputSettings[deviceIndex].twoStageMode = L2TwoStageMode;
            destination.l2OutputSettings[deviceIndex].hipFireMS = L2HipFireTime;
            destination.l2OutputSettings[deviceIndex].triggerEffect = L2TriggerEffect;

            destination.r2OutBezierCurveObj[deviceIndex].CustomDefinition = R2OutputCurveCustom;
            destination.setR2OutCurveMode(deviceIndex, destination.stickOutputCurveId(R2OutputCurveMode));
            destination.r2OutputSettings[deviceIndex].twoStageMode = R2TwoStageMode;
            destination.r2OutputSettings[deviceIndex].hipFireMS = R2HipFireTime;
            destination.r2OutputSettings[deviceIndex].triggerEffect = R2TriggerEffect;

            destination.sxOutBezierCurveObj[deviceIndex].CustomDefinition = SXOutputCurveCustom;
            destination.setSXOutCurveMode(deviceIndex, destination.stickOutputCurveId(SXOutputCurveMode));
            destination.szOutBezierCurveObj[deviceIndex].CustomDefinition = SZOutputCurveCustom;
            destination.setSZOutCurveMode(deviceIndex, destination.stickOutputCurveId(SZOutputCurveMode));
            destination.trackballMode[deviceIndex] = _trackballMode;
            destination.trackballFriction[deviceIndex] = TrackballFriction;
            destination.touchpadRelMouse[deviceIndex].rotation = TouchRelMouseRotation;
            destination.touchpadRelMouse[deviceIndex].minThreshold = TouchRelMouseMinThreshold;

            if (TouchpadMouseStickSettings != null)
            {
                destination.touchMStickInfo[deviceIndex].deadZone = TouchpadMouseStickSettings.DeadZone;
                destination.touchMStickInfo[deviceIndex].maxZone = TouchpadMouseStickSettings.MaxZone;
                destination.touchMStickInfo[deviceIndex].outputStick = TouchpadMouseStickSettings.OutputStick;
                destination.touchMStickInfo[deviceIndex].outputStickDir = TouchpadMouseStickSettings.OutputStickAxes;
                destination.touchMStickInfo[deviceIndex].antiDeadX = TouchpadMouseStickSettings.AntiDeadX;
                destination.touchMStickInfo[deviceIndex].antiDeadY = TouchpadMouseStickSettings.AntiDeadY;
                destination.touchMStickInfo[deviceIndex].inverted = TouchpadMouseStickSettings.Invert;
                destination.touchMStickInfo[deviceIndex].maxOutput = TouchpadMouseStickSettings.MaxOutput;
                destination.touchMStickInfo[deviceIndex].maxOutputEnabled = TouchpadMouseStickSettings.MaxOutputEnabled;
                destination.touchMStickInfo[deviceIndex].vertScale = TouchpadMouseStickSettings.VerticalScale;
                destination.touchMStickInfo[deviceIndex].outputCurve = TouchpadMouseStickSettings.OutputCurve;
                destination.touchMStickInfo[deviceIndex].rotationRad = TouchpadMouseStickSettings.RotationRad;
                if (TouchpadMouseStickSettings.SmoothingSettings != null)
                {
                    TouchpadMouseStickSerializer.SmoothingGroupSerializer tempSmoothSettings =
                        TouchpadMouseStickSettings.SmoothingSettings;

                    destination.touchMStickInfo[deviceIndex].smoothingMethod = tempSmoothSettings.SmoothingMethod;
                    destination.touchMStickInfo[deviceIndex].minCutoff = tempSmoothSettings.SmoothingMinCutoff;
                    destination.touchMStickInfo[deviceIndex].beta = tempSmoothSettings.SmoothingBeta;
                }
            }

            destination.touchpadButtonMode[deviceIndex] = TouchpadButtonMode;

            destination.outputDevType[deviceIndex] = OutputContDevice;
            destination.outputDS4TriggerMode[deviceIndex] = OutputDS4TriggerMode;

            if (!string.IsNullOrEmpty(ProfileActions))
            {
                string[] actionNames = ProfileActions.Split('/');
                for (int actIndex = 0, actLen = actionNames.Length; actIndex < actLen; actIndex++)
                {
                    string tempActionName = actionNames[actIndex];
                    if (!destination.profileActions[deviceIndex].Contains(tempActionName))
                    {
                        destination.profileActions[deviceIndex].Add(tempActionName);
                    }
                }
            }

            if (Control != null)
            {
                if (Control.Button != null && Control.Button.CustomMapButtons.Count > 0)
                {
                    foreach (KeyValuePair<DS4Controls, X360Controls> pair in Control.Button.CustomMapButtons)
                    {
                        destination.UpdateDS4CSetting(deviceIndex,
                            pair.Key.ToString(), false, pair.Value, "", DS4KeyType.None, 0);
                    }
                }

                if (Control.Key != null && Control.Key.CustomMapKeys.Count > 0)
                {
                    foreach (KeyValuePair<DS4Controls, ushort> pair in Control.Key.CustomMapKeys)
                    {
                        destination.UpdateDS4CSetting(deviceIndex,
                            pair.Key.ToString(), false, pair.Value, "", DS4KeyType.None, 0);
                    }
                }

                if (Control.Macro != null && Control.Macro.CustomMapMacros.Count > 0)
                {
                    foreach (KeyValuePair<DS4Controls, string> pair in Control.Macro.CustomMapMacros)
                    {
                        string[] skeys;
                        int[] keys;
                        if (!string.IsNullOrEmpty(pair.Value))
                        {
                            skeys = pair.Value.Split('/');
                            keys = new int[skeys.Length];
                        }
                        else
                        {
                            skeys = new string[0];
                            keys = new int[0];
                        }

                        for (int i = 0, keylen = keys.Length; i < keylen; i++)
                            keys[i] = int.Parse(skeys[i]);

                        destination.UpdateDS4CSetting(deviceIndex,
                            pair.Key.ToString(), false, keys, "", DS4KeyType.None, 0);
                    }
                }

                if (Control.Extras != null && Control.Extras.CustomMapExtras.Count > 0)
                {
                    foreach (KeyValuePair<DS4Controls, string> pair in Control.Extras.CustomMapExtras)
                    {
                        destination.UpdateDS4CExtra(deviceIndex,
                            pair.Key.ToString(), false, pair.Value);
                    }
                }

                if (Control.KeyType != null && Control.KeyType.CustomMapKeyTypes.Count > 0)
                {
                    foreach (KeyValuePair<DS4Controls, DS4KeyType> pair in Control.KeyType.CustomMapKeyTypes)
                    {
                        destination.UpdateDS4CKeyType(deviceIndex, pair.Key.ToString(), false, pair.Value);
                    }
                }

                if (Control.LightbarMacro is not null && Control.LightbarMacro.CustomMapMacro.Count > 0)
                {
                    foreach (var pair in Control.LightbarMacro.CustomMapMacro)
                    {
                        destination.UpdateDS4CLightbarMacro(deviceIndex, pair.Key.ToString(), false, pair.Value);
                    }
                }
            }

            if (ShiftControl != null)
            {
                if (ShiftControl.Button != null && ShiftControl.Button.CustomMapButtons.Count > 0)
                {
                    foreach (KeyValuePair<DS4Controls, X360Controls> pair in ShiftControl.Button.CustomMapButtons)
                    {
                        if (ShiftControl.Button.ShiftTriggers.TryGetValue(pair.Key, out int shiftT) &&
                            shiftT > 0)
                        {
                            destination.UpdateDS4CSetting(deviceIndex,
                                pair.Key.ToString(), true, pair.Value, "", DS4KeyType.None, shiftT);
                        }
                    }
                }

                if (ShiftControl.Key != null && ShiftControl.Key.CustomMapKeys.Count > 0)
                {
                    foreach (KeyValuePair<DS4Controls, ushort> pair in ShiftControl.Key.CustomMapKeys)
                    {
                        if (ShiftControl.Key.ShiftTriggers.TryGetValue(pair.Key, out int shiftT) &&
                            shiftT > 0)
                        {
                            destination.UpdateDS4CSetting(deviceIndex,
                                pair.Key.ToString(), true, pair.Value, "", DS4KeyType.None, shiftT);
                        }
                    }
                }

                if (ShiftControl.Macro != null && ShiftControl.Macro.CustomMapMacros.Count > 0)
                {
                    foreach (KeyValuePair<DS4Controls, string> pair in ShiftControl.Macro.CustomMapMacros)
                    {
                        string[] skeys;
                        int[] keys;
                        if (!string.IsNullOrEmpty(pair.Value))
                        {
                            skeys = pair.Value.Split('/');
                            keys = new int[skeys.Length];
                        }
                        else
                        {
                            skeys = new string[0];
                            keys = new int[0];
                        }

                        for (int i = 0, keylen = keys.Length; i < keylen; i++)
                            keys[i] = int.Parse(skeys[i]);

                        if (ShiftControl.Macro.ShiftTriggers.TryGetValue(pair.Key, out int shiftT) &&
                            shiftT > 0)
                        {
                            destination.UpdateDS4CSetting(deviceIndex,
                                pair.Key.ToString(), true, keys, "", DS4KeyType.None, shiftT);
                        }
                    }
                }

                if (ShiftControl.Extras != null && ShiftControl.Extras.CustomMapExtras.Count > 0)
                {
                    foreach (KeyValuePair<DS4Controls, string> pair in ShiftControl.Extras.CustomMapExtras)
                    {
                        destination.UpdateDS4CExtra(deviceIndex,
                            pair.Key.ToString(), false, pair.Value);
                    }
                }

                if (ShiftControl.KeyType != null && ShiftControl.KeyType.CustomMapKeyTypes.Count > 0)
                {
                    foreach (KeyValuePair<DS4Controls, DS4KeyType> pair in ShiftControl.KeyType.CustomMapKeyTypes)
                    {
                        destination.UpdateDS4CKeyType(deviceIndex, pair.Key.ToString(), true, pair.Value);
                    }
                }
            }
        }

        public void PostProcessXml()
        {
            if (!string.IsNullOrEmpty(ColorString))
            {
                string[] tempColors = ColorString.Split(',');
                if (tempColors.Length == 3)
                {
                    _ledColor = new DS4Color(byte.Parse(tempColors[0]),
                        byte.Parse(tempColors[1]), byte.Parse(tempColors[2]));
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(RedColorString))
                {
                    byte.TryParse(RedColorString, out _ledColor.red);
                }

                if (!string.IsNullOrEmpty(GreenColorString))
                {
                    byte.TryParse(RedColorString, out _ledColor.green);
                }

                if (!string.IsNullOrEmpty(BlueColorString))
                {
                    byte.TryParse(RedColorString, out _ledColor.blue);
                }
            }

            if (!string.IsNullOrEmpty(LowColorString))
            {
                string[] tempColors = LowColorString.Split(',');
                if (tempColors.Length == 3)
                {
                    _lowColor = new DS4Color(byte.Parse(tempColors[0]),
                        byte.Parse(tempColors[1]), byte.Parse(tempColors[2]));
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(LowRedColorString))
                {
                    byte.TryParse(LowRedColorString, out _lowColor.red);
                }

                if (!string.IsNullOrEmpty(LowGreenColorString))
                {
                    byte.TryParse(LowGreenColorString, out _lowColor.green);
                }

                if (!string.IsNullOrEmpty(LowBlueColorString))
                {
                    byte.TryParse(LowBlueColorString, out _lowColor.blue);
                }
            }

            if (!string.IsNullOrEmpty(ChargingColorString))
            {
                string[] tempColors = ChargingColorString.Split(',');
                if (tempColors.Length == 3)
                {
                    _chargingColor = new DS4Color(byte.Parse(tempColors[0]),
                        byte.Parse(tempColors[1]), byte.Parse(tempColors[2]));
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(ChargingRedString))
                {
                    byte.TryParse(ChargingRedString, out _chargingColor.red);
                }

                if (!string.IsNullOrEmpty(ChargingGreenString))
                {
                    byte.TryParse(ChargingGreenString, out _chargingColor.green);
                }

                if (!string.IsNullOrEmpty(ChargingBlueString))
                {
                    byte.TryParse(ChargingBlueString, out _chargingColor.blue);
                }
            }

            if (!string.IsNullOrEmpty(FlashColorString))
            {
                string[] tempColors = FlashColorString.Split(',');
                if (tempColors.Length == 3)
                {
                    _flashColor = new DS4Color(byte.Parse(tempColors[0]),
                        byte.Parse(tempColors[1]), byte.Parse(tempColors[2]));
                }
            }

            //LowColor +Done
            //ChargingColor +Done
            //Sensitivity +Done
            //LaunchProgram +Done
            //StartTouchpadOff +Done
            //UseTPforControls +Done
            //GyroOutputMode +Done
            //GyroControlsSettings/Toggle +Done
            //GyroMouseStickToggle + Done
            //GyroMouseStickSmoothingSettings/SmoothingMethod +Done
            //TouchpadOutputMode +Done
            //TouchDisInvTriggers +Done
            //GyroMouseDeadZone +Done
            //LSOutputCurveMode +Done
            //RSOutputCurveMode +Done
            //L2OutputCurveMode +Done
            //R2OutputCurveMode +Done
            //SXOutputCurveMode +Done
            //SZOutputCurveMode +Done
            //OutputContDevice +Done
        }

        public static XmlAttributeOverrides GetAttributeOverrides()
        {
            XmlAttributeOverrides xmlOverrides = new XmlAttributeOverrides();

            XmlAttributes xmlAttribs = new XmlAttributes();
            xmlAttribs.XmlType = new XmlTypeAttribute("TouchMouseStickInfo.OutputStick");
            xmlOverrides.Add(typeof(TouchMouseStickInfo.OutputStick), xmlAttribs);

            xmlAttribs = new XmlAttributes();
            xmlAttribs.XmlType = new XmlTypeAttribute("TouchMouseStickInfo.OutputStickAxes");
            xmlOverrides.Add(typeof(TouchMouseStickInfo.OutputStickAxes), xmlAttribs);
            return xmlOverrides;
        }
    }
}