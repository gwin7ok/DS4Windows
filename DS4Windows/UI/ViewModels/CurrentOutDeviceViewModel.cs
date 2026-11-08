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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DS4Windows.Core.Model;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.UI.ViewModels
{
    public class CurrentOutDeviceViewModel
    {
        private int selectedIndex = -1;
        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                if (selectedIndex == value) return;
                selectedIndex = value;
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler SelectedIndexChanged;

        public Visibility SidePanelVisibility
        {
            get
            {
                Visibility result = Visibility.Collapsed;
                if (selectedIndex >= 0)
                {
                    SlotDeviceEntry temp = slotDeviceEntries[selectedIndex];
                    if (temp.Slot.CurrentType != OutContType.None)
                    {
                        result = Visibility.Visible;
                    }
                }

                return result;
            }
        }
        public event EventHandler SidePanelVisibilityChanged;

        public bool PluginEnabled
        {
            get
            {
                bool result = false;
                if (selectedIndex >= 0)
                {
                    SlotDeviceEntry temp = slotDeviceEntries[selectedIndex];
                    if (temp.Slot.AttachedStatus ==
                        OutputSlotAttachedStatus.UnAttached)
                    {
                        result = true;
                    }
                }

                return result;
            }
        }
        public event EventHandler PluginEnabledChanged;

        public bool UnpluginEnabled
        {
            get
            {
                bool result = false;
                if (selectedIndex >= 0)
                {
                    SlotDeviceEntry temp = slotDeviceEntries[selectedIndex];
                    if (temp.Slot.AttachedStatus ==
                        OutputSlotAttachedStatus.Attached)
                    {
                        result = true;
                    }
                }

                return result;
            }
        }
        public event EventHandler UnpluginEnabledChanged;

        private List<SlotDeviceEntry> slotDeviceEntries;

        public List<SlotDeviceEntry> SlotDeviceEntries { get => slotDeviceEntries; }

        // Phase6-Step10-5: ControlService の直接呼び出しを廃止し、抜き差しの正式な入口（IOutputSlotService）を使う
        private readonly DS4Windows.Processing.Abstractions.IOutputSlotService outputSlotService;

        public CurrentOutDeviceViewModel(DS4Windows.Processing.Abstractions.IOutputSlotService outputSlotService)
        {
            this.outputSlotService = outputSlotService ?? throw new ArgumentNullException(nameof(outputSlotService));
            // スロット数は、スロット管理が上限どおりの長さで作る
            var views = outputSlotService.SlotViews;
            slotDeviceEntries = new List<SlotDeviceEntry>(views.Count);
            int idx = 0;
            foreach (IOutputSlotView tempDev in views)
            {
                SlotDeviceEntry tempEntry = new SlotDeviceEntry(tempDev, idx);
                tempEntry.PluginRequest += OutSlot_PluginRequest;
                tempEntry.UnplugRequest += OutSlot_UnplugRequest;
                slotDeviceEntries.Add(tempEntry);
                idx++;
            }

            outputSlotService.SlotAssigned += OutSlotManager_SlotAssigned;
            outputSlotService.SlotUnassigned += OutSlotManager_SlotUnassigned;
            SelectedIndexChanged += CurrentOutDeviceViewModel_SelectedIndexChanged;
        }

        private void CurrentOutDeviceViewModel_SelectedIndexChanged(object sender,
            EventArgs e)
        {
            SidePanelVisibilityChanged?.Invoke(this, EventArgs.Empty);
            PluginEnabledChanged?.Invoke(this, EventArgs.Empty);
            UnpluginEnabledChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OutSlot_PluginRequest(object sender, EventArgs e)
        {
            SlotDeviceEntry entry = sender as SlotDeviceEntry;
            // 事前条件の確認と UI スレッドへの切り替えは、サービスが行う
            outputSlotService.PluginSlot(entry.Index, entry.Slot.CurrentType);
        }

        private void OutSlot_UnplugRequest(object sender, EventArgs e)
        {
            SlotDeviceEntry entry = sender as SlotDeviceEntry;
            outputSlotService.UnplugSlot(entry.Index);
        }

        private void OutSlotManager_SlotUnassigned(object sender, OutputSlotAssignmentEventArgs e)
        {
            slotDeviceEntries[e.SlotIndex].RemovedDevice();
            RefreshPanels();
        }

        private void OutSlotManager_SlotAssigned(object sender, OutputSlotAssignmentEventArgs e)
        {
            slotDeviceEntries[e.SlotIndex].AssignedDevice();
            RefreshPanels();
        }

        private void RefreshPanels()
        {
            SidePanelVisibilityChanged?.Invoke(this, EventArgs.Empty);
            PluginEnabledChanged?.Invoke(this, EventArgs.Empty);
            UnpluginEnabledChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
