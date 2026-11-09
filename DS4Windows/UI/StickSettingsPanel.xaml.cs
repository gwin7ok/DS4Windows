using System;
using System.Windows;
using System.Windows.Controls;
using DS4Windows.Core.Model;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI
{
    /// <summary>
    /// プロファイル編集画面の「LS」「RS」（スティック）の部分。DataContext は <see cref="StickSettingsViewModel"/>。
    /// 曲線の編集画面・スティックの校正は、編集画面の全体（実機・ダイアログ）に関わるため、イベントで編集画面へ知らせる。
    /// </summary>
    public partial class StickSettingsPanel : UserControl
    {
        public StickSettingsPanel()
        {
            InitializeComponent();
        }

        /// <summary>カスタム曲線の編集が求められた。引数は対象（"LS" か "RS"）。</summary>
        public event EventHandler<string> CustomCurveEditorRequested;

        /// <summary>スティックの校正が求められた。引数は 0＝左、1＝右。</summary>
        public event EventHandler<int> CalibrationRequested;

        // 軸別デッドゾーンの部品。編集画面が、機器との結び付け・試験表示への反映に使う
        public AxialStickUserControl AxialLeft => axialLSStickControl;
        public AxialStickUserControl AxialRight => axialRSStickControl;

        /// <summary>LS・RS の Outer Bind の割り当ての表示を、割り当ての一覧の各コントロールへ結ぶ。</summary>
        public void AssignOuterBindAssociation(MappingListViewModel mappingList)
        {
            lsOuterBindLb.DataContext = mappingList.ControlMap[DS4Controls.LSOuter];
            rsOuterBindLb.DataContext = mappingList.ControlMap[DS4Controls.RSOuter];
        }

        private void CustomEditorBtn_Click(object sender, RoutedEventArgs e)
        {
            CustomCurveEditorRequested?.Invoke(this, (sender as Button)?.Tag as string);
        }

        private void CalibrateStick_OnClick(object sender, RoutedEventArgs e)
        {
            CalibrationRequested?.Invoke(this, Convert.ToInt32((sender as Button)?.Tag));
        }
    }
}
