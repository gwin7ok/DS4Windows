using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Utilities;

namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// 1 つのスロットのTriggersの設定の持ち主（値そのもの）。<see cref="BackingStore"/> がスロットごとに 1 つ持ち、
    /// 保存・読み込み（プロファイルの XML）、実行時の入力処理、設定画面の ViewModel が、同じ 1 つのオブジェクトを読み書きする。
    /// </summary>
    public class TriggerSlotSettings
    {
        public TriggerSlotSettings(int slot)
        {
            Slot = slot;
            L2Mod = new TriggerDeadZoneZInfo();
            R2Mod = new TriggerDeadZoneZInfo();
            L2Output = new TriggerOutputSettings();
            R2Output = new TriggerOutputSettings();
            L2Bezier = new BezierCurve();
            R2Bezier = new BezierCurve();
            L2Sens = BackingStore.DEFAULT_ANALOG_SENS;
            R2Sens = BackingStore.DEFAULT_ANALOG_SENS;
        }

        public int Slot { get; }

        public TriggerDeadZoneZInfo L2Mod { get; }
        public TriggerDeadZoneZInfo R2Mod { get; }
        public double L2Sens { get; set; }
        public double R2Sens { get; set; }
        public TriggerOutputSettings L2Output { get; }
        public TriggerOutputSettings R2Output { get; }
        public BezierCurve L2Bezier { get; }
        public BezierCurve R2Bezier { get; }

        // 出力カーブのモード。6（利用者の定義したカーブ）を選んだときだけ、ベジェ曲線の参照表を初期化する
        // （1〜5 は、入力処理の側の計算式で扱う）
        private int _l2CurveMode;
        private int _r2CurveMode;

        /// <summary>L2 の出力カーブのモード。画面から変えたときの値の設定（診断ログつき）。</summary>
        public int L2CurveMode
        {
            get => _l2CurveMode;
            set
            {
                ApplyL2CurveMode(value);
                AppLogger.Log(AppLogLevel.Debug, $"[DI] ProfileSettingsService.SetL2OutCurveMode: Slot {Slot} = {value}");
            }
        }

        /// <summary>R2 の出力カーブのモード。画面から変えたときの値の設定（診断ログつき）。</summary>
        public int R2CurveMode
        {
            get => _r2CurveMode;
            set
            {
                ApplyR2CurveMode(value);
                AppLogger.Log(AppLogLevel.Debug, $"[DI] ProfileSettingsService.SetR2OutCurveMode: Slot {Slot} = {value}");
            }
        }

        /// <summary>プロファイルの読み込み・初期化など、画面の操作ではない経路の設定（ログなし）。</summary>
        public void ApplyL2CurveMode(int value)
        {
            if (value == 6) L2Bezier.InitBezierCurve(L2Bezier.CustomDefinition, BezierCurve.AxisType.L2R2);
            _l2CurveMode = value;
        }

        public void ApplyR2CurveMode(int value)
        {
            if (value == 6) R2Bezier.InitBezierCurve(R2Bezier.CustomDefinition, BezierCurve.AxisType.L2R2);
            _r2CurveMode = value;
        }
    }
}
