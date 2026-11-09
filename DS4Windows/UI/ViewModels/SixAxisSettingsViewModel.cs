using System;
using DS4Windows.Core.Settings;
using DS4Windows.Core.Utilities;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>
    /// プロファイル編集画面の「Axis Config」タブの 6 軸（加速度の X・Z）の ViewModel。1 つのスロットの設定
    /// （<see cref="SixAxisSlotSettings"/>）だけを扱う。プロパティ名は、画面の Binding と同じ名前のまま。
    /// </summary>
    public class SixAxisSettingsViewModel : ObservableObject
    {
        private readonly SixAxisSlotSettings sixAxis;

        public SixAxisSettingsViewModel(SixAxisSlotSettings sixAxis)
        {
            this.sixAxis = sixAxis ?? throw new ArgumentNullException(nameof(sixAxis));
        }

        // デッドゾーンは、試験表示（Controller Readings）が変更を受けるため、変わったときに通知する
        public double SXDeadZone
        {
            get => sixAxis.SXDeadZone;
            set
            {
                if (sixAxis.SXDeadZone == value) return;
                sixAxis.SXDeadZone = value;
                OnPropertyChanged(nameof(SXDeadZone));
            }
        }

        public double SZDeadZone
        {
            get => sixAxis.SZDeadZone;
            set
            {
                if (sixAxis.SZDeadZone == value) return;
                sixAxis.SZDeadZone = value;
                OnPropertyChanged(nameof(SZDeadZone));
            }
        }

        public double SXMaxZone
        {
            get => sixAxis.SXMaxZone;
            set => sixAxis.SXMaxZone = value;
        }

        public double SZMaxZone
        {
            get => sixAxis.SZMaxZone;
            set => sixAxis.SZMaxZone = value;
        }

        public double SXAntiDeadZone
        {
            get => sixAxis.SXAntiDeadZone;
            set => sixAxis.SXAntiDeadZone = value;
        }

        public double SZAntiDeadZone
        {
            get => sixAxis.SZAntiDeadZone;
            set => sixAxis.SZAntiDeadZone = value;
        }

        public double SXSens
        {
            get => sixAxis.SXSens;
            set => sixAxis.SXSens = value;
        }

        public double SZSens
        {
            get => sixAxis.SZSens;
            set => sixAxis.SZSens = value;
        }

        public int SXOutputCurveIndex
        {
            get => sixAxis.SXCurveMode;
            set
            {
                sixAxis.SXCurveMode = value;
                OnPropertyChanged(nameof(SXCustomCurveSelected));
            }
        }

        public int SZOutputCurveIndex
        {
            get => sixAxis.SZCurveMode;
            set
            {
                sixAxis.SZCurveMode = value;
                OnPropertyChanged(nameof(SZCustomCurveSelected));
            }
        }

        public bool SXCustomCurveSelected => sixAxis.SXCurveMode == 6;

        public bool SZCustomCurveSelected => sixAxis.SZCurveMode == 6;

        public string SXCustomCurve
        {
            get => sixAxis.SXCurve.CustomDefinition;
            set => sixAxis.SXCurve.InitBezierCurve(value, BezierCurve.AxisType.SA, true);
        }

        public string SZCustomCurve
        {
            get => sixAxis.SZCurve.CustomDefinition;
            set => sixAxis.SZCurve.InitBezierCurve(value, BezierCurve.AxisType.SA, true);
        }
    }
}
