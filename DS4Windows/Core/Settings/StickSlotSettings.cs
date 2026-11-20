#nullable enable

using DS4Windows.Core.Utilities;
using DS4Windows.Core.Logging;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 1 つのスロットのSticksの設定の持ち主（値そのもの）。<see cref="BackingStore"/> がスロットごとに 1 つ持ち、
    /// 保存・読み込み（プロファイルの XML）、実行時の入力処理、設定画面の ViewModel が、同じ 1 つのオブジェクトを読み書きする。
    /// </summary>
    public class StickSlotSettings
    {
        public StickSlotSettings(int slot)
        {
            Slot = slot;
            LSMod = new StickDeadZoneInfo();
            RSMod = new StickDeadZoneInfo();
            LSOutput = new StickOutputSetting();
            RSOutput = new StickOutputSetting();
            LSAntiSnapback = new StickAntiSnapbackInfo();
            RSAntiSnapback = new StickAntiSnapbackInfo();
            SquStick = new SquareStickInfo();
            LsBezier = new BezierCurve();
            RsBezier = new BezierCurve();
            LSSens = BackingStore.DEFAULT_ANALOG_SENS;
            RSSens = BackingStore.DEFAULT_ANALOG_SENS;
            LSRotation = 0.0;
            RSRotation = 0.0;
            LeftStickDriftXAxis = 0;
            LeftStickDriftYAxis = 0;
            RightStickDriftXAxis = 0;
            RightStickDriftYAxis = 0;
        }

        public int Slot { get; }

        public StickDeadZoneInfo LSMod { get; }
        public StickDeadZoneInfo RSMod { get; }
        public StickOutputSetting LSOutput { get; }
        public StickOutputSetting RSOutput { get; }
        public StickAntiSnapbackInfo LSAntiSnapback { get; }
        public StickAntiSnapbackInfo RSAntiSnapback { get; }
        public SquareStickInfo SquStick { get; }
        public BezierCurve LsBezier { get; }
        public BezierCurve RsBezier { get; }
        public double LSSens { get; set; }
        public double RSSens { get; set; }
        public double LSRotation { get; set; }
        public double RSRotation { get; set; }
        public sbyte LeftStickDriftXAxis { get; set; }
        public sbyte LeftStickDriftYAxis { get; set; }
        public sbyte RightStickDriftXAxis { get; set; }
        public sbyte RightStickDriftYAxis { get; set; }

        // 出力カーブのモード。6（利用者の定義したカーブ）を選んだときだけ、ベジェ曲線の参照表を初期化する
        // （1〜5 は、入力処理の側の計算式で扱う）
        private int _lSCurveMode;
        private int _rSCurveMode;

        /// <summary>Ls の出力カーブのモード。画面から変えたときの値の設定（診断ログつき）。</summary>
        public int LSCurveMode
        {
            get => _lSCurveMode;
            set
            {
                ApplyLSCurveMode(value);
                AppLogger.Log(AppLogLevel.Debug, $"[DI] ProfileSettingsService.SetLsOutCurveMode: Slot {Slot} = {value}");
            }
        }

        /// <summary>プロファイルの読み込み・初期化など、画面の操作ではない経路の設定（ログなし）。</summary>
        public void ApplyLSCurveMode(int value)
        {
            if (value == 6) LsBezier.InitBezierCurve(LsBezier.CustomDefinition, BezierCurve.AxisType.LSRS);
            _lSCurveMode = value;
        }

        /// <summary>Rs の出力カーブのモード。画面から変えたときの値の設定（診断ログつき）。</summary>
        public int RSCurveMode
        {
            get => _rSCurveMode;
            set
            {
                ApplyRSCurveMode(value);
                AppLogger.Log(AppLogLevel.Debug, $"[DI] ProfileSettingsService.SetRsOutCurveMode: Slot {Slot} = {value}");
            }
        }

        /// <summary>プロファイルの読み込み・初期化など、画面の操作ではない経路の設定（ログなし）。</summary>
        public void ApplyRSCurveMode(int value)
        {
            if (value == 6) RsBezier.InitBezierCurve(RsBezier.CustomDefinition, BezierCurve.AxisType.LSRS);
            _rSCurveMode = value;
        }

        /// <summary>スティックの設定を既定へ戻す（プロファイルの読み込みの前。<c>BackingStore.ResetProfile</c> から呼ばれる）。</summary>
        public void ResetForProfileLoad()
        {
            // 旧形式の読込に追加した設定の既定値（要素の無いプロファイルで、前の値を引き継がないようにする）
            LeftStickDriftXAxis = 0;
            LeftStickDriftYAxis = 0;
            RightStickDriftXAxis = 0;
            RightStickDriftYAxis = 0;

            LSMod.Reset();
            RSMod.Reset();
            LSMod.deadZone = RSMod.deadZone = StickDeadZoneInfo.DEFAULT_DEADZONE;
            LSMod.antiDeadZone = RSMod.antiDeadZone = StickDeadZoneInfo.DEFAULT_ANTIDEADZONE;
            LSMod.maxZone = RSMod.maxZone = StickDeadZoneInfo.DEFAULT_MAXZONE;
            LSMod.maxOutput = RSMod.maxOutput = StickDeadZoneInfo.DEFAULT_MAXOUTPUT;
            LSMod.fuzz = RSMod.fuzz = StickDeadZoneInfo.DEFAULT_FUZZ;

            // Rotation angles expressed in radians
            LSRotation = 0.0;
            RSRotation = 0.0;

            LSSens = RSSens = BackingStore.DEFAULT_ANALOG_SENS;

            LSOutput.ResetSettings();
            RSOutput.ResetSettings();

            SquStick.Reset();
            LSAntiSnapback.timeout = StickAntiSnapbackInfo.DEFAULT_TIMEOUT;
            LSAntiSnapback.delta = StickAntiSnapbackInfo.DEFAULT_DELTA;
            LSAntiSnapback.enabled = StickAntiSnapbackInfo.DEFAULT_ENABLED;
            ApplyLSCurveMode(0);
            ApplyRSCurveMode(0);
        }
    }
}
