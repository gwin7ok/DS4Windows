using System.Windows;
using System.Windows.Controls;

namespace DS4Windows.UI
{
    /// <summary>
    /// プロファイル編集画面の「Axis Config」タブの 6 軸（加速度）の中身。DataContext は
    /// <see cref="DS4Windows.UI.ViewModels.SixAxisSettingsViewModel"/>。
    /// カスタム曲線のエディターを開くのは画面の外（<c>ProfileEditor</c>）が持つため、求められたことを知らせるだけにする。
    /// </summary>
    public partial class SixAxisSettingsPanel : UserControl
    {
        /// <summary>カスタム曲線の編集が求められた。引数は対象（"SX" か "SZ"）。</summary>
        public event System.EventHandler<string> CustomCurveEditorRequested;

        public SixAxisSettingsPanel()
        {
            InitializeComponent();
        }

        private void CustomEditorBtn_Click(object sender, RoutedEventArgs e)
        {
            CustomCurveEditorRequested?.Invoke(this, (sender as Button)?.Tag as string);
        }
    }
}
