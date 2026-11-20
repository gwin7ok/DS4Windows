#nullable enable

using System;
using System.Windows;
using System.Windows.Controls;
using DS4Windows.Core.Model;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI
{
    /// <summary>
    /// 片方のスティック（左か右）の設定の部分。DataContext は <see cref="StickSideViewModel"/>で、左右とも同じ部品を使う。
    /// 曲線の編集画面・スティックの校正は、編集画面の全体（実機・ダイアログ）に関わるため、イベントで知らせる（どちら側かは <see cref="StickSideViewModel.IsLeft"/>）。
    /// </summary>
    public partial class StickSideSettingsView : UserControl
    {
        public System.Windows.Input.ICommand CustomEditorCommand { get; }

        public System.Windows.Input.ICommand CalibrationCommand { get; }

        public StickSideSettingsView()
        {
            CustomEditorCommand = DS4Windows.UI.ViewModels.Util.CommandGuard.CreateSync(() => CustomCurveEditorRequested?.Invoke(this, EventArgs.Empty), name: nameof(CustomEditorCommand));
            CalibrationCommand = DS4Windows.UI.ViewModels.Util.CommandGuard.CreateSync(() => CalibrationRequested?.Invoke(this, EventArgs.Empty), name: nameof(CalibrationCommand));
            InitializeComponent();
        }

        /// <summary>カスタム曲線の編集が求められた。</summary>
        public event EventHandler? CustomCurveEditorRequested;

        /// <summary>スティックの校正が求められた。</summary>
        public event EventHandler? CalibrationRequested;

        /// <summary>軸別デッドゾーンの部品。編集画面が、機器との結び付け・試験表示への反映に使う。</summary>
        public AxialStickUserControl AxialStick => axialStickControl;

        /// <summary>Outer Bind の割り当ての表示を、割り当ての一覧の該当のコントロールへ結ぶ。</summary>
        public void AssignOuterBindAssociation(MappingListViewModel mappingList, DS4Controls outerControl)
        {
            outerBindLb.DataContext = mappingList.ControlMap[outerControl];
        }

    }
}
