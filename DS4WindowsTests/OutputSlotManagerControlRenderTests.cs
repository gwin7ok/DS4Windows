using System;
using System.Threading;
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
    /// </summary>
    public class OutputSlotManagerControlRenderTests
    {
        [Fact]
        public void EveryOutputSlot_IsRenderedAsARow()
        {
            int items = -1, expected = -1, rows = -1;
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var manager = TestRepositoryFactory.CreateOutputSlotManager();
                    var service = TestRepositoryFactory.CreateOutputSlotService(manager);
                    expected = manager.OutputSlots.Length;

                    var control = new OutputSlotManagerControl();
                    control.SetupDataContext(new CurrentOutDeviceViewModel(service));
                    var window = new Window { Content = control, Width = 900, Height = 600, ShowInTaskbar = false };
                    window.Show();
                    control.UpdateLayout();

                    var lv = (ListView)control.FindName("currentOutDevLV");
                    items = lv.Items.Count;
                    int count = 0;
                    for (int i = 0; i < items; i++)
                    {
                        var c = lv.ItemContainerGenerator.ContainerFromIndex(i) as ListViewItem;
                        if (c != null && c.ActualHeight > 0) count++;
                    }
                    rows = count;
                    window.Close();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Assert.Null(failure);
            Assert.Equal(expected, items);
            Assert.Equal(expected, rows);
        }
    }
}
