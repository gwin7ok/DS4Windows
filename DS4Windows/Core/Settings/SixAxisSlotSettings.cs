using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Utilities;
using DS4Windows.Core.Logging;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 1 つのスロットのSixAxisの設定の持ち主（値そのもの）。<see cref="BackingStore"/> がスロットごとに 1 つ持ち、
    /// 保存・読み込み（プロファイルの XML）、実行時の入力処理、設定画面の ViewModel が、同じ 1 つのオブジェクトを読み書きする。
    /// </summary>
    public class SixAxisSlotSettings
    {
        public SixAxisSlotSettings(int slot)
        {
            Slot = slot;
            SXCurve = new BezierCurve();
            SZCurve = new BezierCurve();
            SXDeadZone = BackingStore.DEFAULT_SX_TILT_DEADZONE;
            SZDeadZone = BackingStore.DEFAULT_SX_TILT_DEADZONE;
            SXMaxZone = BackingStore.DEFAULT_SX_TILT_MAXZONE;
            SZMaxZone = BackingStore.DEFAULT_SX_TILT_MAXZONE;
            SXAntiDeadZone = 0.0;
            SZAntiDeadZone = 0.0;
            SXSens = BackingStore.DEFAULT_ANALOG_SENS;
            SZSens = BackingStore.DEFAULT_ANALOG_SENS;
        }

        public int Slot { get; }

        public double SXDeadZone { get; set; }
        public double SZDeadZone { get; set; }
        public double SXMaxZone { get; set; }
        public double SZMaxZone { get; set; }
        public double SXAntiDeadZone { get; set; }
        public double SZAntiDeadZone { get; set; }
        public double SXSens { get; set; }
        public double SZSens { get; set; }
        public BezierCurve SXCurve { get; }
        public BezierCurve SZCurve { get; }

        // 出力カーブのモード。6（利用者の定義したカーブ）を選んだときだけ、ベジェ曲線の参照表を初期化する
        // （1〜5 は、入力処理の側の計算式で扱う）
        private int _sXCurveMode;
        private int _sZCurveMode;

        /// <summary>Sx の出力カーブのモード。画面から変えたときの値の設定（診断ログつき）。</summary>
        public int SXCurveMode
        {
            get => _sXCurveMode;
            set
            {
                ApplySXCurveMode(value);
                AppLogger.Log(AppLogLevel.Debug, $"[DI] ProfileSettingsService.SetSxOutCurveMode: Slot {Slot} = {value}");
            }
        }

        /// <summary>プロファイルの読み込み・初期化など、画面の操作ではない経路の設定（ログなし）。</summary>
        public void ApplySXCurveMode(int value)
        {
            if (value == 6) SXCurve.InitBezierCurve(SXCurve.CustomDefinition, BezierCurve.AxisType.SA);
            _sXCurveMode = value;
        }

        /// <summary>Sz の出力カーブのモード。画面から変えたときの値の設定（診断ログつき）。</summary>
        public int SZCurveMode
        {
            get => _sZCurveMode;
            set
            {
                ApplySZCurveMode(value);
                AppLogger.Log(AppLogLevel.Debug, $"[DI] ProfileSettingsService.SetSzOutCurveMode: Slot {Slot} = {value}");
            }
        }

        /// <summary>プロファイルの読み込み・初期化など、画面の操作ではない経路の設定（ログなし）。</summary>
        public void ApplySZCurveMode(int value)
        {
            if (value == 6) SZCurve.InitBezierCurve(SZCurve.CustomDefinition, BezierCurve.AxisType.SA);
            _sZCurveMode = value;
        }
    }
}
