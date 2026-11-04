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
using System.Windows.Documents;
using System.Xml;
using System.Xml.Serialization;
using DS4Windows;

namespace DS4WinWPF.DS4Control.DTOXml
{
    [XmlRoot("OutputSlots")]
    public class OutputSlotPersistDTO : IDTO<OutputSlotManager>
    {
        [XmlAttribute("app_version")]
        public string AppVersion
        {
            get => DS4Windows.PathService.ExecutableVersionState;
            set { }
        }

        [XmlElement("Slot")] // Use XmlElement here to skip container element
        public List<OutputSlotSerializer> SlotItems
        {
            get; set;
        }

        public OutputSlotPersistDTO()
        {
            SlotItems = new List<OutputSlotSerializer>();
        }

        public void MapFrom(OutputSlotManager source)
        {
            foreach (OutSlotDevice dev in source.OutputSlots)
            {
                if (dev.CurrentReserveStatus == OutSlotDevice.ReserveStatus.Permanent)
                {
                    OutputSlotSerializer tempSlot = new OutputSlotSerializer()
                    {
                        Index = dev.Index,
                        DeviceType = dev.PermanentType,
                    };

                    SlotItems.Add(tempSlot);
                }
            }
        }

        public void MapTo(OutputSlotManager destination)
        {
            foreach(OutputSlotSerializer tempSlot in SlotItems)
            {
                OutSlotDevice tempDev = null;
                // Phase8-Step1 W2b（B-1）: 上流は出力スロットが 4 つだった時代の「<= 3」。出力スロットは slotLimit（最大 8）個あり、
                // 保存側（MapFrom）は全スロットを書くため、読込側もスロット数までを復元する（範囲外は無視する）。
                if (tempSlot.Index >= 0 && tempSlot.Index < destination.OutputSlots.Length)
                {
                    int idx = tempSlot.Index;
                    tempDev = destination.OutputSlots[idx];
                }

                if (tempDev != null)
                {
                    tempDev.CurrentReserveStatus = OutSlotDevice.ReserveStatus.Permanent;
                    tempDev.PermanentType = tempSlot.DeviceType;
                }
            }
        }
    }
}
