using System.Windows;
using System.Windows.Controls;
using DS4Windows.Core.Model;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI
{
    /// <summary>
    /// プロファイル編集画面の「L2 &amp; R2」（トリガー）の部分。DataContext は <see cref="TriggerSettingsViewModel"/>。
    /// 曲線の編集画面を開く操作は、編集画面の全体に関わるため、イベントで編集画面へ知らせる。
    /// </summary>
    public partial class TriggerSettingsPanel : UserControl
    {
        public System.Windows.Input.ICommand CustomEditorCommand { get; }

        public TriggerSettingsPanel()
        {
            CustomEditorCommand = DS4Windows.UI.ViewModels.Util.CommandGuard.CreateSync<object>(parameter => CustomCurveEditorRequested?.Invoke(this, parameter as string), name: nameof(CustomEditorCommand));
            InitializeComponent();
        }

        /// <summary>カスタム曲線の編集が求められた。引数は対象（"L2" か "R2"）。</summary>
        public event System.EventHandler<string> CustomCurveEditorRequested;

        /// <summary>L2・R2 の Full Pull の割り当ての表示を、割り当ての一覧の各コントロールへ結ぶ。</summary>
        public void AssignFullPullAssociation(MappingListViewModel mappingList)
        {
            l2FullPullLb.DataContext = mappingList.ControlMap[DS4Controls.L2FullPull];
            r2FullPullLb.DataContext = mappingList.ControlMap[DS4Controls.R2FullPull];
        }

    }
}
