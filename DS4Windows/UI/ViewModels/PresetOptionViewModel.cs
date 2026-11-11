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

using DS4Windows.Processing.Gyro;
using System;
using System.Collections.Generic;
using DS4Windows.Core.Settings;
using DS4Windows.Processing;
using DS4Windows.UI.ViewModels.Util;

namespace DS4Windows.UI.ViewModels
{
    public class PresetOptionViewModel
    {
        private int presetIndex;
        private PresetOption.OutputContChoice controllerChoice =
            PresetOption.OutputContChoice.Xbox360;

        public int PresetIndex
        {
            get => presetIndex;
            set
            {
                if (presetIndex == value) return;
                presetIndex = value;
                PresetIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler PresetIndexChanged;

        private List<PresetOption> presetList;
        public List<PresetOption> PresetsList { get => presetList; }

        public string PresetDescription
        {
            get => presetList[presetIndex].Description;
        }
        public event EventHandler PresetDescriptionChanged;

        public bool PresetDisplayOutputCont
        {
            get => presetList[presetIndex].OutputControllerChoice;
        }
        public event EventHandler PresetDisplayOutputContChanged;

        public PresetOption.OutputContChoice ControllerChoice
        {
            get => controllerChoice;
            set => controllerChoice = value;
        }

        private List<EnumChoiceSelection<PresetOption.OutputContChoice>> outputChoices =
            new List<EnumChoiceSelection<PresetOption.OutputContChoice>>()
            {
                new EnumChoiceSelection<PresetOption.OutputContChoice>("Xbox 360", PresetOption.OutputContChoice.Xbox360),
                new EnumChoiceSelection<PresetOption.OutputContChoice>("DualShock 4", PresetOption.OutputContChoice.DualShock4),
            };

        public List<EnumChoiceSelection<PresetOption.OutputContChoice>> OutputChoices { get => outputChoices; }

        public PresetOptionViewModel(DS4Windows.Core.Abstractions.IProfilePresetLoader presetLoader, DS4Windows.Core.Abstractions.IProfileLoadTarget loadTarget)
        {
            if (presetLoader == null) throw new ArgumentNullException(nameof(presetLoader));
            if (loadTarget == null) throw new ArgumentNullException(nameof(loadTarget));
            presetList = new List<PresetOption>();
            presetList.Add(new GamepadPreset(presetLoader, loadTarget));
            presetList.Add(new GamepadGyroCamera(presetLoader, loadTarget));
            presetList.Add(new MixedPreset(presetLoader, loadTarget));
            presetList.Add(new MixedGyroMousePreset(presetLoader, loadTarget));
            presetList.Add(new KBMPreset(presetLoader, loadTarget));
            presetList.Add(new KBMGyroMouse(presetLoader, loadTarget));

            PresetIndexChanged += PresetOptionViewModel_PresetIndexChanged;
        }

        private void PresetOptionViewModel_PresetIndexChanged(object sender, EventArgs e)
        {
            PresetDescriptionChanged?.Invoke(this, EventArgs.Empty);
            PresetDisplayOutputContChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ApplyPreset(int index)
        {
            if (presetIndex >= 0)
            {
                PresetOption current = presetList[presetIndex];
                if (current.OutputControllerChoice &&
                    controllerChoice != PresetOption.OutputContChoice.None)
                {
                    current.OutputCont = controllerChoice;
                }

                current.ApplyPreset(index);
            }
        }
    }
}
