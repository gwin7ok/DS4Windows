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
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;

using DS4Windows.Core.Model;

namespace DS4Windows.UI.ViewModels
{
    public class ControllerRegDeviceOptsViewModel
    {
        private ControlServiceDeviceOptions serviceDeviceOpts;

        /// <summary>「この機種を使う」のチェックボックス（系統の一覧から作る）。</summary>
        public List<FamilyToggleItem> FamilyToggles { get; }

        public bool UseMoonlightChanged
        {
            get;
            private set;
        }

        public bool UseMoonlight
        {
            get => appSettings.UseMoonlight;
            set
            {
                UseMoonlightChanged = value != appSettings.UseMoonlight;
                appSettings.UseMoonlight = value;
            }
        }

        public bool UseAdvancedMoonlight
        {
            get => appSettings.UseAdvancedMoonlight;
            set
            {
                UseMoonlightChanged = value != appSettings.UseAdvancedMoonlight;
                appSettings.UseAdvancedMoonlight = value;
            }
        }

        public bool VerboseLogMessages { get => serviceDeviceOpts.VerboseLogMessages; set => serviceDeviceOpts.VerboseLogMessages = value; }

        private List<DeviceListItem> currentInputDevices = new List<DeviceListItem>();
        public List<DeviceListItem> CurrentInputDevices { get => currentInputDevices; }

        // Serial, ControllerOptionsStore instance
        private Dictionary<string, ControllerOptionsStore> inputDeviceSettings = new Dictionary<string, ControllerOptionsStore>();
        private List<ControllerOptionsStore> controllerOptionsStores = new List<ControllerOptionsStore>();

        private int controllerSelectedIndex = -1;
        public int ControllerSelectedIndex
        {
            get => controllerSelectedIndex;
            set
            {
                if (controllerSelectedIndex == value) return;
                controllerSelectedIndex = value;
                ControllerSelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler ControllerSelectedIndexChanged;

        // Phase6-Step10-2-2: Global.UseMoonlight／UseAdvancedMoonlight／SaveControllerConfigs の置換先（必須引数。null は拒否する）
        private readonly IAppSettingsService appSettings;

        public ControllerRegDeviceOptsViewModel(ControlServiceDeviceOptions serviceDeviceOpts,
            IControllerSlotQuery slots, IAppSettingsService appSettings)
        {
            this.appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            this.serviceDeviceOpts = serviceDeviceOpts;
            FamilyToggles = serviceDeviceOpts?.Families
                .Select(f => new FamilyToggleItem(f, serviceDeviceOpts[f])).ToList() ?? new List<FamilyToggleItem>();
            if (slots == null) throw new ArgumentNullException(nameof(slots));

            for (int slot = 0; slot < slots.SlotCount; slot++)
            {
                IControllerSlotView device = slots.GetSlot(slot);
                if (device != null)
                {
                    currentInputDevices.Add(new DeviceListItem(device));
                    inputDeviceSettings.Add(device.MacAddress, device.OptionsStore);
                    controllerOptionsStores.Add(device.OptionsStore);
                }
            }
        }

        /// <summary>
        /// 選んだコントローラーの設定パネルを作る。系統は、コントローラーの種類から引く。設定項目がない機種（DS3 など）は、項目が空のパネルになる。
        /// </summary>
        public DeviceFamilyPanelModel BuildCurrentPanel()
        {
            ControllerOptionsStore store = controllerOptionsStores[controllerSelectedIndex];
            InputDeviceFamily family = InputDeviceTypeInfo.FamilyOf(store.DeviceType);
            return new DeviceFamilyPanelModel(serviceDeviceOpts[family], store.Settings);
        }

        public void SaveControllerConfigs()
        {
            foreach (DeviceListItem item in currentInputDevices)
            {
                item.Device.SaveControllerConfigs();
            }
        }
    }

}
