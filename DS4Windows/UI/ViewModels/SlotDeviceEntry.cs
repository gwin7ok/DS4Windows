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
using DS4Windows.Core.Model;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.UI.ViewModels
{

    public class SlotDeviceEntry
    {
        private IOutputSlotView slot;
        public IOutputSlotView Slot { get => slot; }

        // Phase6-Step10-5: スロット番号（IOutputSlotService.PluginSlot/UnplugSlot へ渡す）
        public int Index { get => idx; }

        /// <summary>恒久（Permanent）の種類が決まっているなら、その種類で接続を要求して true を返す。決まっていなければ何もせず false。</summary>
        public bool TryPlugPermanentType()
        {
            if (slot.ReserveStatus != OutputSlotReserveStatus.Permanent || slot.PermanentType == OutContType.None)
                return false;

            slot.CurrentType = slot.PermanentType;
            RequestPlugin();
            return true;
        }

        /// <summary>選ばれた種類と確保の方法で接続を要求する。恒久なら、その種類を恒久の種類として覚える。</summary>
        public void PlugAs(OutContType type, OutputSlotReserveStatus reserve)
        {
            slot.CurrentType = type;
            slot.ReserveStatus = reserve;
            if (reserve == OutputSlotReserveStatus.Permanent)
            {
                slot.PermanentType = type;
            }

            RequestPlugin();
        }

        public string CurrentType
        {
            get
            {
                return slot.CurrentTypeName;
            }
        }
        public event EventHandler CurrentTypeChanged;

        public string DesiredType
        {
            get
            {
                string temp = "Dynamic";
                if (slot.ReserveStatus ==
                    OutputSlotReserveStatus.Permanent)
                {
                    temp = slot.PermanentType.ToString();
                }

                return temp;
            }
        }
        public event EventHandler DesiredTypeChanged;

        public bool BoundInput
        {
            get => slot.InputBound == OutputSlotInputBound.Bound;
        }
        public event EventHandler BoundInputChanged;

        private int desiredTypeChoiceIndex = -1;
        public int DesiredTypeChoice
        {
            get => desiredTypeChoiceIndex;
            set
            {
                if (desiredTypeChoiceIndex == value) return;
                desiredTypeChoiceIndex = value;
                DesiredTypeChoiceChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler DesiredTypeChoiceChanged;

        private int reserveChoiceIndex = -1;
        public int ReserveChoice
        {
            get => reserveChoiceIndex;
            set
            {
                if (reserveChoiceIndex == value) return;
                reserveChoiceIndex = value;
                ReserveChoiceChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler ReserveChoiceChanged;

        private bool dirty = false;
        public bool Dirty
        {
            get => dirty;
            set
            {
                if (dirty == value) return;
                dirty = value;
                DirtyChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler DirtyChanged;

        public event EventHandler PluginRequest;
        public event EventHandler UnplugRequest;
        int idx;

        /// <summary>画面に出す、1 から始まるスロット番号（行の位置で決まる固定の値）</summary>
        public int SlotNumber => idx + 1;

        public string InputSlotNum
        {
            get
            {
                string result = "";
                if (slot.InputIndex >= 0)
                {
                    result = $"{slot.InputIndex+1}";
                }

                return result;
            }
        }

        public event EventHandler InputSlotNumChanged;

        public string InputSlotDisplayString
        {
            get
            {
                string result = "";
                if (slot.InputIndex >= 0)
                {
                    result = slot.InputDisplayString;
                }
                return result;
            }
        }
        public event EventHandler InputSlotDisplayStringChanged;

        public SlotDeviceEntry(IOutputSlotView slot, int idx)
        {
            this.slot = slot;
            this.idx = idx;

            //desiredTypeChoiceIndex = DetermineDesiredChoiceIdx();
            reserveChoiceIndex = DetermineReserveChoiceIdx();

            SetupEvents();
        }

        private void SetupEvents()
        {
            //DesiredTypeChoiceChanged += SlotDeviceEntry_FormPropChanged;
            ReserveChoiceChanged += SlotDeviceEntry_FormPropChanged;

            slot.PermanentTypeChanged += OutSlotDevice_PermanentTypeChanged;
            slot.InputBoundChanged += OutSlotDevice_CurrentInputBoundChanged;
        }

        private void OutSlotDevice_CurrentInputBoundChanged(object sender, EventArgs e)
        {
            BoundInputChanged?.Invoke(this, EventArgs.Empty);
            // 結び付け・解除のときは、結び付いた入力の表示も変わる
            InputSlotDisplayStringChanged?.Invoke(this, EventArgs.Empty);
            InputSlotNumChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OutSlotDevice_PermanentTypeChanged(object sender, EventArgs e)
        {
            DesiredTypeChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SlotDeviceEntry_FormPropChanged(object sender, EventArgs e)
        {
            Dirty = true;
        }

        private int DetermineDesiredChoiceIdx()
        {
            return OutContTypeInfo.SlotChoiceIndex(slot.PermanentType);
        }

        private int DetermineReserveChoiceIdx()
        {
            int result = 0;
            switch (slot.ReserveStatus)
            {
                case OutputSlotReserveStatus.Dynamic:
                    result = 0;
                    break;
                case OutputSlotReserveStatus.Permanent:
                    result = 1;
                    break;
                default:
                    break;
            }

            return result;
        }

        private OutContType DetermineDesiredTypeFromIdx()
        {
            OutContType result = OutContTypeInfo.FromSlotChoiceIndex(desiredTypeChoiceIndex);

            return result;
        }

        private OutputSlotReserveStatus DetermineReserveChoiceFromIdx()
        {
            OutputSlotReserveStatus result = OutputSlotReserveStatus.Dynamic;
            switch(reserveChoiceIndex)
            {
                case 0:
                    result = OutputSlotReserveStatus.Dynamic;
                    break;
                case 1:
                    result = OutputSlotReserveStatus.Permanent;
                    break;
                default:
                    break;
            }

            return result;
        }

        public void AssignedDevice()
        {
            Refresh();
        }

        public void RemovedDevice()
        {
            Refresh();
        }

        private void Refresh()
        {
            //DesiredTypeChoice = DetermineDesiredChoiceIdx();
            ReserveChoice = DetermineReserveChoiceIdx();

            CurrentTypeChanged?.Invoke(this, EventArgs.Empty);
            DesiredTypeChanged?.Invoke(this, EventArgs.Empty);
            BoundInputChanged?.Invoke(this, EventArgs.Empty);
            InputSlotDisplayStringChanged?.Invoke(this, EventArgs.Empty);
            InputSlotNumChanged?.Invoke(this, EventArgs.Empty);
            Dirty = false;
        }

        public void RequestPlugin()
        {
            PluginRequest?.Invoke(this, EventArgs.Empty);
        }

        public void RequestUnplug()
        {
            UnplugRequest?.Invoke(this, EventArgs.Empty);
        }

        public void ApplyChanges()
        {
            slot.ReserveStatus = DetermineReserveChoiceFromIdx();
        }
    }
}
