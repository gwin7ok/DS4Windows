/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI
{
    /// <summary>
    /// Interaction logic for OutputSlotManager.xaml
    /// </summary>
    public partial class OutputSlotManagerControl : UserControl
    {
        private CurrentOutDeviceViewModel currentOutDevVM;
        //private PermanentOutDevViewModel permanentDevVM;

        public OutputSlotManagerControl()
        {
            InitializeComponent();
        }

        // ViewModel は IViewModelFactory が作り、メイン画面が渡す（この画面が出力スロットのサービスを知らずに済む）
        public void SetupDataContext(CurrentOutDeviceViewModel viewModel)
        {
            currentOutDevVM = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            currentOutDevVM.SelectedIndexChanged += CurrentOutDevVM_SelectedIndexChanged;
            currentOutDevLV.DataContext = currentOutDevVM;
            sideStackPanel.DataContext = currentOutDevVM;
            plugDevStackPanel.DataContext = currentOutDevVM;
            outSlotStackPanel.DataContext = null;

            //permanentDevVM = new PermanentOutDevViewModel(controlService, outputMan);
            //permanentOutDevLV.DataContext = permanentDevVM;
        }

        private void CurrentOutDevVM_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = currentOutDevVM.SelectedIndex;
            if (idx >= 0)
            {
                outSlotStackPanel.DataContext = currentOutDevVM.SlotDeviceEntries[idx];
            }
            else
            {
                outSlotStackPanel.DataContext = null;
            }
        }

        /// <summary>列幅をドラッグで変えて離したときに発生する（保存の契機）</summary>
        public event EventHandler ColumnWidthsChanged;

        private void CurrentOutDevLV_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            ColumnWidthsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>列の幅（左から: 出力スロット #、入力スロット #、入力コントローラー ID、現在の種類、予約）</summary>
        public int[] GetColumnWidths()
        {
            var gridView = currentOutDevLV.View as GridView;
            int[] result = new int[gridView.Columns.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (int)gridView.Columns[i].Width;
            }

            return result;
        }

        public void SetColumnWidths(int[] widths)
        {
            var gridView = currentOutDevLV.View as GridView;
            for (int i = 0; i < widths.Length && i < gridView.Columns.Count; i++)
            {
                if (widths[i] > 0)
                {
                    gridView.Columns[i].Width = widths[i];
                }
            }
        }

        public void SetupLateEvents()
        {

        }

        private void PluginBtn_Click(object sender, RoutedEventArgs e)
        {
            int idx = currentOutDevVM.SelectedIndex;
            SlotDeviceEntry tempEntry = null;
            if (idx >= 0)
            {
                tempEntry = currentOutDevVM.SlotDeviceEntries[idx];
            }

            // 恒久（Permanent）の種類が決まっているスロットは、選ばせずにその種類で接続する
            if (tempEntry != null && tempEntry.TryPlugPermanentType())
            {
                return;
            }

            PluginOutDevWindow devWindow = new PluginOutDevWindow();
            devWindow.ShowDialog();
            MessageBoxResult result = devWindow.Result;
            if (result == MessageBoxResult.OK)
            {
                tempEntry.PlugAs(devWindow.ContType, devWindow.ReserveType);
            }
        }

        private void UnplugBtn_Click(object sender, RoutedEventArgs e)
        {
            int idx = currentOutDevVM.SelectedIndex;
            if (idx >= 0)
            {
                currentOutDevVM.SlotDeviceEntries[idx].RequestUnplug();
            }
        }

        private void SlotChangeAcceptBtn_Click(object sender, RoutedEventArgs e)
        {
            int idx = currentOutDevVM.SelectedIndex;
            if (idx >= 0)
            {
                currentOutDevVM.SlotDeviceEntries[idx].ApplyChanges();
            }
        }
    }
}
