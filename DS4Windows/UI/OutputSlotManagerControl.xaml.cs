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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DS4Windows.UI.ViewModels;
using DS4Windows.Core.Model;

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

            if (tempEntry != null &&
                tempEntry.Slot.ReserveStatus ==
                OutputSlotReserveStatus.Permanent &&
                tempEntry.Slot.PermanentType != DS4Windows.Core.Model.OutContType.None)
            {
                tempEntry.Slot.CurrentType = tempEntry.Slot.PermanentType;
                tempEntry.RequestPlugin();
            }
            else
            {
                PluginOutDevWindow devWindow = new PluginOutDevWindow();
                devWindow.ShowDialog();
                MessageBoxResult result = devWindow.Result;
                if (result == MessageBoxResult.OK)
                {
                    tempEntry.Slot.CurrentType = devWindow.ContType;
                    tempEntry.Slot.ReserveStatus = devWindow.ReserveType;
                    if (tempEntry.Slot.ReserveStatus ==
                        OutputSlotReserveStatus.Permanent)
                    {
                        tempEntry.Slot.PermanentType = devWindow.ContType;
                    }

                    tempEntry.RequestPlugin();
                }
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
