using System;
using System.Collections.Generic;
using DS4Windows.Core.Settings;
using DS4Windows.Core.Utilities;
using DS4Windows.UI.ViewModels.Util;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>
    /// プロファイル編集画面の「LS」「RS」（スティック）の ViewModel。1 つのスロットのスティックの設定（<see cref="StickSlotSettings"/>）だけを扱う。
    /// プロパティ名は、画面の Binding と同じ名前のまま（画面の部品は、この ViewModel を DataContext にして、そのまま使える）。
    /// 値は設定サービスの配列にあり、保存・読込の経路は変わらない。
    /// </summary>
    public class StickSettingsViewModel : ObservableObject
    {
        private readonly StickSlotSettings sticks;

        public StickSettingsViewModel(StickSlotSettings sticks)
        {
            this.sticks = sticks ?? throw new ArgumentNullException(nameof(sticks));
        }

        public double LSDeadZone
        {
            get => Math.Round(sticks.LSMod.deadZone / 127d, 2);
            set
            {
                double temp = Math.Round(sticks.LSMod.deadZone / 127d, 2);
                if (temp == value) return;
                sticks.LSMod.deadZone = (int)Math.Round(value * 127d);
                OnPropertyChanged(nameof(LSDeadZone));
            }
        }
        public double RSDeadZone
        {
            get => Math.Round(sticks.RSMod.deadZone / 127d, 2);
            set
            {
                double temp = Math.Round(sticks.RSMod.deadZone / 127d, 2);
                if (temp == value) return;
                sticks.RSMod.deadZone = (int)Math.Round(value * 127d);
                OnPropertyChanged(nameof(RSDeadZone));
            }
        }
        public double LSMaxZone
        {
            get => sticks.LSMod.maxZone / 100.0;
            set => sticks.LSMod.maxZone = (int)Math.Round(value * 100.0);
        }

        public double RSMaxZone
        {
            get => sticks.RSMod.maxZone / 100.0;
            set => sticks.RSMod.maxZone = (int)Math.Round(value * 100.0);
        }

        public double LSAntiDeadZone
        {
            get => sticks.LSMod.antiDeadZone / 100.0;
            set => sticks.LSMod.antiDeadZone = (int)Math.Round(value * 100.0);
        }

        public double RSAntiDeadZone
        {
            get => sticks.RSMod.antiDeadZone / 100.0;
            set => sticks.RSMod.antiDeadZone = (int)Math.Round(value * 100.0);
        }

        public double LSVerticalScale
        {
            get => sticks.LSMod.verticalScale / 100.0;
            set => sticks.LSMod.verticalScale = value * 100.0;
        }

        public double LSMaxOutput
        {
            get => sticks.LSMod.maxOutput / 100.0;
            set => sticks.LSMod.maxOutput = value * 100.0;
        }

        public bool LSMaxOutputForce
        {
            get => sticks.LSMod.maxOutputForce;
            set => sticks.LSMod.maxOutputForce = value;
        }

        public double RSVerticalScale
        {
            get => sticks.RSMod.verticalScale / 100.0;
            set => sticks.RSMod.verticalScale = value * 100.0;
        }

        public double RSMaxOutput
        {
            get => sticks.RSMod.maxOutput / 100.0;
            set => sticks.RSMod.maxOutput = value * 100.0;
        }

        public bool RSMaxOutputForce
        {
            get => sticks.RSMod.maxOutputForce;
            set => sticks.RSMod.maxOutputForce = value;
        }

        public int LSDeadTypeIndex
        {
            get
            {
                int index = 0;
                switch (sticks.LSMod.deadzoneType)
                {
                    case StickDeadZoneInfo.DeadZoneType.Radial:
                        break;
                    case StickDeadZoneInfo.DeadZoneType.Axial:
                        index = 1; break;
                    default: break;
                }

                return index;
            }
            set
            {
                StickDeadZoneInfo.DeadZoneType temp = StickDeadZoneInfo.DeadZoneType.Radial;
                switch (value)
                {
                    case 0: break;
                    case 1:
                        temp = StickDeadZoneInfo.DeadZoneType.Axial;
                        break;
                    default: break;
                }

                StickDeadZoneInfo.DeadZoneType current = sticks.LSMod.deadzoneType;
                if (temp == current) return;
                sticks.LSMod.deadzoneType = temp;
            }
        }

        public int RSDeadTypeIndex
        {
            get
            {
                int index = 0;
                switch (sticks.RSMod.deadzoneType)
                {
                    case StickDeadZoneInfo.DeadZoneType.Radial:
                        break;
                    case StickDeadZoneInfo.DeadZoneType.Axial:
                        index = 1; break;
                    default: break;
                }

                return index;
            }
            set
            {
                StickDeadZoneInfo.DeadZoneType temp = StickDeadZoneInfo.DeadZoneType.Radial;
                switch (value)
                {
                    case 0: break;
                    case 1:
                        temp = StickDeadZoneInfo.DeadZoneType.Axial;
                        break;
                    default: break;
                }

                StickDeadZoneInfo.DeadZoneType current = sticks.RSMod.deadzoneType;
                if (temp == current) return;
                sticks.RSMod.deadzoneType = temp;
            }
        }

        public double LSSens
        {
            get => sticks.LSSens;
            set => sticks.LSSens = value;
        }

        public double RSSens
        {
            get => sticks.RSSens;
            set => sticks.RSSens = value;
        }

        public bool LSSquareStick
        {
            get => sticks.SquStick.lsMode;
            set => sticks.SquStick.lsMode = value;
        }

        public bool RSSquareStick
        {
            get => sticks.SquStick.rsMode;
            set => sticks.SquStick.rsMode = value;
        }

        public double LSSquareRoundness
        {
            get => sticks.SquStick.lsRoundness;
            set => sticks.SquStick.lsRoundness = value;
        }

        public double RSSquareRoundness
        {
            get => sticks.SquStick.rsRoundness;
            set => sticks.SquStick.rsRoundness = value;
        }

        public int LSOutputCurveIndex
        {
            get => sticks.LSCurveMode;
            set
            {
                sticks.LSCurveMode = value;
                OnPropertyChanged(nameof(LSCustomCurveSelected));
            }
        }

        public int RSOutputCurveIndex
        {
            get => sticks.RSCurveMode;
            set
            {
                sticks.RSCurveMode = value;
                OnPropertyChanged(nameof(RSCustomCurveSelected));
            }
        }

        public double LSRotation
        {
            get => sticks.LSRotation * 180.0 / Math.PI;
            set => sticks.LSRotation = value * Math.PI / 180.0;
        }

        public double RSRotation
        {
            get => sticks.RSRotation * 180.0 / Math.PI;
            set => sticks.RSRotation = value * Math.PI / 180.0;
        }

        public bool LSCustomCurveSelected
        {
            get => sticks.LSCurveMode == 6;
        }
        public bool RSCustomCurveSelected
        {
            get => sticks.RSCurveMode == 6;
        }
        public string LSCustomCurve
        {
            get => sticks.LsBezier.CustomDefinition;
            set => sticks.LsBezier.InitBezierCurve(value, BezierCurve.AxisType.LSRS, true);
        }

        public string RSCustomCurve
        {
            get => sticks.RsBezier.CustomDefinition;
            set => sticks.RsBezier.InitBezierCurve(value, BezierCurve.AxisType.LSRS, true);
        }

        public int LSFuzz
        {
            get => sticks.LSMod.fuzz;
            set => sticks.LSMod.fuzz = value;
        }

        public int RSFuzz
        {
            get => sticks.RSMod.fuzz;
            set => sticks.RSMod.fuzz = value;
        }

        public bool LSAntiSnapback
        {
            get => sticks.LSAntiSnapback.enabled;
            set => sticks.LSAntiSnapback.enabled = value;
        }

        public bool RSAntiSnapback
        {
            get => sticks.RSAntiSnapback.enabled;
            set => sticks.RSAntiSnapback.enabled = value;
        }

        public double LSAntiSnapbackDelta
        {
            get => sticks.LSAntiSnapback.delta;
            set => sticks.LSAntiSnapback.delta = value;
        }

        public double RSAntiSnapbackDelta
        {
            get => sticks.RSAntiSnapback.delta;
            set => sticks.RSAntiSnapback.delta = value;
        }
        public int LSAntiSnapbackTimeout
        {
            get => sticks.LSAntiSnapback.timeout;
            set => sticks.LSAntiSnapback.timeout = value;
        }

        public int RSAntiSnapbackTimeout
        {
            get => sticks.RSAntiSnapback.timeout;
            set => sticks.RSAntiSnapback.timeout = value;
        }

        public bool LSOuterBindInvert
        {
            get => sticks.LSMod.outerBindInvert;
            set => sticks.LSMod.outerBindInvert = value;
        }

        public bool RSOuterBindInvert
        {
            get => sticks.RSMod.outerBindInvert;
            set => sticks.RSMod.outerBindInvert = value;
        }

        public double LSOuterBindDead
        {
            get => sticks.LSMod.outerBindDeadZone / 100.0;
            set => sticks.LSMod.outerBindDeadZone = value * 100.0;
        }

        public double RSOuterBindDead
        {
            get => sticks.RSMod.outerBindDeadZone / 100.0;
            set => sticks.RSMod.outerBindDeadZone = value * 100.0;
        }

        public bool LSDeltaAccelEnabled
        {
            get => sticks.LSOutput.outputSettings.controlSettings.deltaAccelSettings.enabled;
            set
            {
                bool temp = sticks.LSOutput.outputSettings.controlSettings.deltaAccelSettings.enabled;
                if (temp == value) return;

                sticks.LSOutput.outputSettings.controlSettings.deltaAccelSettings.enabled = value;
                OnPropertyChanged(nameof(LSDeltaAccelEnabled));
            }
        }
        public double LSDeltaMultiplier
        {
            get => sticks.LSOutput.outputSettings.controlSettings.deltaAccelSettings.multiplier;
            set
            {
                sticks.LSOutput.outputSettings.controlSettings.deltaAccelSettings.multiplier = value;
            }
        }

        public double LSDeltaMaxTravel
        {
            get => sticks.LSOutput.outputSettings.controlSettings.deltaAccelSettings.maxTravel;
            set
            {
                sticks.LSOutput.outputSettings.controlSettings.deltaAccelSettings.maxTravel = value;
            }
        }

        public double LSDeltaMinTravel
        {
            get => sticks.LSOutput.outputSettings.controlSettings.deltaAccelSettings.minTravel;
            set
            {
                sticks.LSOutput.outputSettings.controlSettings.deltaAccelSettings.minTravel = value;
            }
        }

        public double LSDeltaEasingDuration
        {
            get => sticks.LSOutput.outputSettings.controlSettings.deltaAccelSettings.easingDuration;
            set
            {
                sticks.LSOutput.outputSettings.controlSettings.deltaAccelSettings.easingDuration = value;
            }
        }

        public double LSDeltaMinFactor
        {
            get => sticks.LSOutput.outputSettings.controlSettings.deltaAccelSettings.minfactor;
            set
            {
                sticks.LSOutput.outputSettings.controlSettings.deltaAccelSettings.minfactor = value;
            }
        }

        public bool RSDeltaAccelEnabled
        {
            get => sticks.RSOutput.outputSettings.controlSettings.deltaAccelSettings.enabled;
            set
            {
                bool temp = sticks.RSOutput.outputSettings.controlSettings.deltaAccelSettings.enabled;
                if (temp == value) return;

                sticks.RSOutput.outputSettings.controlSettings.deltaAccelSettings.enabled = value;
                OnPropertyChanged(nameof(RSDeltaAccelEnabled));
            }
        }
        public double RSDeltaMultiplier
        {
            get => sticks.RSOutput.outputSettings.controlSettings.deltaAccelSettings.multiplier;
            set
            {
                sticks.RSOutput.outputSettings.controlSettings.deltaAccelSettings.multiplier = value;
            }
        }

        public double RSDeltaMaxTravel
        {
            get => sticks.RSOutput.outputSettings.controlSettings.deltaAccelSettings.maxTravel;
            set
            {
                sticks.RSOutput.outputSettings.controlSettings.deltaAccelSettings.maxTravel = value;
            }
        }

        public double RSDeltaMinTravel
        {
            get => sticks.RSOutput.outputSettings.controlSettings.deltaAccelSettings.minTravel;
            set
            {
                sticks.RSOutput.outputSettings.controlSettings.deltaAccelSettings.minTravel = value;
            }
        }

        public double RSDeltaEasingDuration
        {
            get => sticks.RSOutput.outputSettings.controlSettings.deltaAccelSettings.easingDuration;
            set
            {
                sticks.RSOutput.outputSettings.controlSettings.deltaAccelSettings.easingDuration = value;
            }
        }

        public double RSDeltaMinFactor
        {
            get => sticks.RSOutput.outputSettings.controlSettings.deltaAccelSettings.minfactor;
            set
            {
                sticks.RSOutput.outputSettings.controlSettings.deltaAccelSettings.minfactor = value;
            }
        }

        public int LSOutputIndex
        {
            get
            {
                int index = 0;
                switch (sticks.LSOutput.mode)
                {
                    case StickMode.None:
                        index = 0; break;
                    case StickMode.Controls:
                        index = 1; break;
                    case StickMode.FlickStick:
                        index = 2; break;
                    default: break;
                }
                return index;
            }
            set
            {
                StickMode temp = StickMode.None;
                switch (value)
                {
                    case 0:
                        temp = StickMode.None;
                        break;
                    case 1:
                        temp = StickMode.Controls;
                        break;
                    case 2:
                        temp = StickMode.FlickStick;
                        break;
                    default:
                        break;
                }

                StickMode current = sticks.LSOutput.mode;
                if (temp == current) return;
                sticks.LSOutput.mode = temp;
                OnPropertyChanged(nameof(LSOutputIndex));
            }
        }
        public double LSFlickRWC
        {
            get => sticks.LSOutput.outputSettings.flickSettings.realWorldCalibration;
            set
            {
                sticks.LSOutput.outputSettings.flickSettings.realWorldCalibration = value;
            }
        }

        public double LSFlickThreshold
        {
            get => sticks.LSOutput.outputSettings.flickSettings.flickThreshold;
            set
            {
                sticks.LSOutput.outputSettings.flickSettings.flickThreshold = value;
            }
        }

        public double LSFlickTime
        {
            get => sticks.LSOutput.outputSettings.flickSettings.flickTime;
            set
            {
                sticks.LSOutput.outputSettings.flickSettings.flickTime = value;
            }
        }

        public double LSMinAngleThreshold
        {
            get => sticks.LSOutput.outputSettings.flickSettings.minAngleThreshold;
            set
            {
                sticks.LSOutput.outputSettings.flickSettings.minAngleThreshold = value;
            }
        }

        public int RSOutputIndex
        {
            get
            {
                int index = 0;
                switch (sticks.RSOutput.mode)
                {
                    case StickMode.None:
                        break;
                    case StickMode.Controls:
                        index = 1; break;
                    case StickMode.FlickStick:
                        index = 2; break;
                    default: break;
                }
                return index;
            }
            set
            {
                StickMode temp = StickMode.None;
                switch (value)
                {
                    case 0:
                        temp = StickMode.None;
                        break;
                    case 1:
                        temp = StickMode.Controls;
                        break;
                    case 2:
                        temp = StickMode.FlickStick;
                        break;
                    default:
                        break;
                }

                StickMode current = sticks.RSOutput.mode;
                if (temp == current) return;
                sticks.RSOutput.mode = temp;
                OnPropertyChanged(nameof(RSOutputIndex));
            }
        }
        public double RSFlickRWC
        {
            get => sticks.RSOutput.outputSettings.flickSettings.realWorldCalibration;
            set
            {
                sticks.RSOutput.outputSettings.flickSettings.realWorldCalibration = value;
            }
        }

        public double RSFlickThreshold
        {
            get => sticks.RSOutput.outputSettings.flickSettings.flickThreshold;
            set
            {
                sticks.RSOutput.outputSettings.flickSettings.flickThreshold = value;
            }
        }

        public double RSFlickTime
        {
            get => sticks.RSOutput.outputSettings.flickSettings.flickTime;
            set
            {
                sticks.RSOutput.outputSettings.flickSettings.flickTime = value;
            }
        }

        public double RSMinAngleThreshold
        {
            get => sticks.RSOutput.outputSettings.flickSettings.minAngleThreshold;
            set
            {
                sticks.RSOutput.outputSettings.flickSettings.minAngleThreshold = value;
            }
        }

        public sbyte LeftStickDriftXAxis
        {
            get => sticks.LeftStickDriftXAxis;
            set
            {
                sticks.LeftStickDriftXAxis = value;
                OnPropertyChanged(nameof(LeftStickDriftXAxis));
            }
        }

        public sbyte LeftStickDriftYAxis
        {
            get => sticks.LeftStickDriftYAxis;
            set
            {
                sticks.LeftStickDriftYAxis = value;
                OnPropertyChanged(nameof(LeftStickDriftYAxis));
            }
        }

        public sbyte RightStickDriftXAxis
        {
            get => sticks.RightStickDriftXAxis;
            set
            {
                sticks.RightStickDriftXAxis = value;
                OnPropertyChanged(nameof(RightStickDriftXAxis));
            }
        }

        public sbyte RightStickDriftYAxis
        {
            get => sticks.RightStickDriftYAxis;
            set
            {
                sticks.RightStickDriftYAxis = value;
                OnPropertyChanged(nameof(RightStickDriftYAxis));
            }
        }
    }
}
