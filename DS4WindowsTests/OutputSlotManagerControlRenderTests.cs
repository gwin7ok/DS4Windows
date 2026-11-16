using System;
using System.Windows;
using System.Windows.Controls;
using DS4Windows.UI;
using DS4Windows.UI.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 出力スロットの一覧（<see cref="OutputSlotManagerControl"/>）が、スロット管理の全スロットを行として描画することを守る。
    /// 実機で、コントローラーが 3 台でも 1 行しか出なかった報告の調査で、描画まで通して数える形にした。
    /// 画面部品のテストは、共有の STA スレッド（<see cref="WpfPanelTestRunner"/>）で行う。
    /// </summary>
    public class OutputSlotManagerControlRenderTests
    {
        [Fact]
        public void EveryOutputSlot_IsRenderedAsARow()
        {
            WpfPanelTestRunner.Run(() =>
            {
                var manager = TestRepositoryFactory.CreateOutputSlotManager();
                var service = TestRepositoryFactory.CreateOutputSlotService(manager);
                var theme = (ResourceDictionary)Application.LoadComponent(
                    new Uri("/DS4Windows;component/UI/Themes/DefaultTheme.xaml", UriKind.Relative));
                Application.Current.Resources.MergedDictionaries.Add(theme);
                try
                {
                    var control = new OutputSlotManagerControl();
                    control.SetupDataContext(new CurrentOutDeviceViewModel(service));
                    control.Measure(new Size(900, 600));
                    control.Arrange(new Rect(0, 0, 900, 600));
                    control.UpdateLayout();

                    var lv = (ListView)control.FindName("currentOutDevLV");
                    Assert.Equal(manager.OutputSlots.Length, lv.Items.Count);
                }
                finally
                {
                    Application.Current.Resources.MergedDictionaries.Remove(theme);
                }
            });
        }
    }
}
