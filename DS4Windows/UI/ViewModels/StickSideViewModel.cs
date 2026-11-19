using System;
using DS4Windows.Core.Settings;
using DS4Windows.Core.Utilities;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>
    /// プロファイル編集画面の、片方のスティック（左か右）の設定の ViewModel。左右で同じ形の項目を 1 つの型で持ち、
    /// どちら側かは生成のときの <c>isLeft</c> だけで決まる（設定サービスの <see cref="StickSlotSettings"/> の左右の項目へ振り分ける）。
    /// 値は設定サービスにあり、保存・読込の経路は変わらない。画面（<c>StickSideSettingsView</c>）はこれを DataContext にして、左右で同じ部品を使う。
    /// 項目の値の丸め・変更通知は左右とも同じ（左右で違う動きを足さない。<c>StickSideViewModelTests</c> が左右の一致を検査する）。
    /// </summary>
    public class StickSideViewModel : ObservableObject
    {
        private readonly StickSlotSettings sticks;
        private readonly bool isLeft;

        public StickSideViewModel(StickSlotSettings sticks, bool isLeft)
        {
            this.sticks = sticks ?? throw new ArgumentNullException(nameof(sticks));
            this.isLeft = isLeft;
        }

        /// <summary>左側なら true。</summary>
        public bool IsLeft => isLeft;

        /// <summary>デッドゾーンの設定（軸別デッドゾーンの部品が、中身を直接読み書きする）。</summary>
        public StickDeadZoneInfo Mod => isLeft ? sticks.LSMod : sticks.RSMod;

        private StickOutputSetting Output => isLeft ? sticks.LSOutput : sticks.RSOutput;
        private StickAntiSnapbackInfo AntiSnapbackInfo => isLeft ? sticks.LSAntiSnapback : sticks.RSAntiSnapback;
        private BezierCurve Bezier => isLeft ? sticks.LsBezier : sticks.RsBezier;
        private DeltaAccelSettings DeltaAccelInfo => Output.outputSettings.controlSettings.deltaAccelSettings;
        private FlickStickSettings FlickInfo => Output.outputSettings.flickSettings;

        public double DeadZone
        {
            get => Math.Round(Mod.deadZone / 127d, 2);
            set
            {
                double temp = Math.Round(Mod.deadZone / 127d, 2);
                if (temp == value) return;
                Mod.deadZone = (int)Math.Round(value * 127d);
                OnPropertyChanged(nameof(DeadZone));
            }
        }

        public double MaxZone
        {
            get => Mod.maxZone / 100.0;
            set => Mod.maxZone = (int)Math.Round(value * 100.0);
        }

        public double AntiDeadZone
        {
            get => Mod.antiDeadZone / 100.0;
            set => Mod.antiDeadZone = (int)Math.Round(value * 100.0);
        }

        public double VerticalScale
        {
            get => Mod.verticalScale / 100.0;
            set => Mod.verticalScale = value * 100.0;
        }

        public double MaxOutput
        {
            get => Mod.maxOutput / 100.0;
            set => Mod.maxOutput = value * 100.0;
        }

        public bool MaxOutputForce
        {
            get => Mod.maxOutputForce;
            set => Mod.maxOutputForce = value;
        }

        /// <summary>デッドゾーンの種類が Axial（軸ごと）か。</summary>
        public bool IsAxialDeadZone => Mod.deadzoneType == StickDeadZoneInfo.DeadZoneType.Axial;

        /// <summary>0＝Radial、1＝Axial。</summary>
        public int DeadTypeIndex
        {
            get => Mod.deadzoneType == StickDeadZoneInfo.DeadZoneType.Axial ? 1 : 0;
            set
            {
                StickDeadZoneInfo.DeadZoneType temp = value == 1
                    ? StickDeadZoneInfo.DeadZoneType.Axial
                    : StickDeadZoneInfo.DeadZoneType.Radial;
                if (temp == Mod.deadzoneType) return;
                Mod.deadzoneType = temp;
                OnPropertyChanged(nameof(DeadTypeIndex));
            }
        }

        public double Sens
        {
            get => isLeft ? sticks.LSSens : sticks.RSSens;
            set { if (isLeft) sticks.LSSens = value; else sticks.RSSens = value; }
        }

        public bool SquareStick
        {
            get => isLeft ? sticks.SquStick.lsMode : sticks.SquStick.rsMode;
            set
            {
                if (SquareStick == value) return;
                if (isLeft) sticks.SquStick.lsMode = value; else sticks.SquStick.rsMode = value;
                // この値で編集の可否が変わる設定（IsEnabled の束縛）へ知らせる
                OnPropertyChanged(nameof(SquareStick));
            }
        }

        public double SquareRoundness
        {
            get => isLeft ? sticks.SquStick.lsRoundness : sticks.SquStick.rsRoundness;
            set { if (isLeft) sticks.SquStick.lsRoundness = value; else sticks.SquStick.rsRoundness = value; }
        }

        public int OutputCurveIndex
        {
            get => isLeft ? sticks.LSCurveMode : sticks.RSCurveMode;
            set
            {
                if (isLeft) sticks.LSCurveMode = value; else sticks.RSCurveMode = value;
                OnPropertyChanged(nameof(CustomCurveSelected));
            }
        }

        /// <summary>角度（度）。設定サービスにはラジアンで持つ。</summary>
        public double Rotation
        {
            get => (isLeft ? sticks.LSRotation : sticks.RSRotation) * 180.0 / Math.PI;
            set
            {
                double radian = value * Math.PI / 180.0;
                if (isLeft) sticks.LSRotation = radian; else sticks.RSRotation = radian;
            }
        }

        /// <summary>出力カーブが「利用者の定義したカーブ」（6）のとき true。</summary>
        public bool CustomCurveSelected => OutputCurveIndex == 6;

        public string CustomCurve
        {
            get => Bezier.CustomDefinition;
            set => Bezier.InitBezierCurve(value, BezierCurve.AxisType.LSRS, true);
        }

        public int Fuzz
        {
            get => Mod.fuzz;
            set => Mod.fuzz = value;
        }

        public bool AntiSnapback
        {
            get => AntiSnapbackInfo.enabled;
            set
            {
                if (AntiSnapbackInfo.enabled == value) return;
                AntiSnapbackInfo.enabled = value;
                // この値で編集の可否が変わる設定（IsEnabled の束縛）へ知らせる
                OnPropertyChanged(nameof(AntiSnapback));
            }
        }

        public double AntiSnapbackDelta
        {
            get => AntiSnapbackInfo.delta;
            set => AntiSnapbackInfo.delta = value;
        }

        public int AntiSnapbackTimeout
        {
            get => AntiSnapbackInfo.timeout;
            set => AntiSnapbackInfo.timeout = value;
        }

        public bool OuterBindInvert
        {
            get => Mod.outerBindInvert;
            set => Mod.outerBindInvert = value;
        }

        public double OuterBindDead
        {
            get => Mod.outerBindDeadZone / 100.0;
            set => Mod.outerBindDeadZone = value * 100.0;
        }

        public bool DeltaAccelEnabled
        {
            get => DeltaAccelInfo.enabled;
            set
            {
                if (DeltaAccelInfo.enabled == value) return;
                DeltaAccelInfo.enabled = value;
                OnPropertyChanged(nameof(DeltaAccelEnabled));
            }
        }

        public double DeltaMultiplier
        {
            get => DeltaAccelInfo.multiplier;
            set => DeltaAccelInfo.multiplier = value;
        }

        public double DeltaMaxTravel
        {
            get => DeltaAccelInfo.maxTravel;
            set => DeltaAccelInfo.maxTravel = value;
        }

        public double DeltaMinTravel
        {
            get => DeltaAccelInfo.minTravel;
            set => DeltaAccelInfo.minTravel = value;
        }

        public double DeltaEasingDuration
        {
            get => DeltaAccelInfo.easingDuration;
            set => DeltaAccelInfo.easingDuration = value;
        }

        public double DeltaMinFactor
        {
            get => DeltaAccelInfo.minfactor;
            set => DeltaAccelInfo.minfactor = value;
        }

        /// <summary>0＝None、1＝Controls、2＝FlickStick。</summary>
        public int OutputIndex
        {
            get
            {
                switch (Output.mode)
                {
                    case StickMode.Controls: return 1;
                    case StickMode.FlickStick: return 2;
                    default: return 0;
                }
            }
            set
            {
                StickMode temp;
                switch (value)
                {
                    case 1: temp = StickMode.Controls; break;
                    case 2: temp = StickMode.FlickStick; break;
                    default: temp = StickMode.None; break;
                }

                if (temp == Output.mode) return;
                Output.mode = temp;
                OnPropertyChanged(nameof(OutputIndex));
            }
        }

        public double FlickRWC
        {
            get => FlickInfo.realWorldCalibration;
            set => FlickInfo.realWorldCalibration = value;
        }

        public double FlickThreshold
        {
            get => FlickInfo.flickThreshold;
            set => FlickInfo.flickThreshold = value;
        }

        public double FlickTime
        {
            get => FlickInfo.flickTime;
            set => FlickInfo.flickTime = value;
        }

        public double MinAngleThreshold
        {
            get => FlickInfo.minAngleThreshold;
            set => FlickInfo.minAngleThreshold = value;
        }

        public sbyte DriftXAxis
        {
            get => isLeft ? sticks.LeftStickDriftXAxis : sticks.RightStickDriftXAxis;
            set
            {
                if (isLeft) sticks.LeftStickDriftXAxis = value; else sticks.RightStickDriftXAxis = value;
                OnPropertyChanged(nameof(DriftXAxis));
            }
        }

        public sbyte DriftYAxis
        {
            get => isLeft ? sticks.LeftStickDriftYAxis : sticks.RightStickDriftYAxis;
            set
            {
                if (isLeft) sticks.LeftStickDriftYAxis = value; else sticks.RightStickDriftYAxis = value;
                OnPropertyChanged(nameof(DriftYAxis));
            }
        }
    }
}
