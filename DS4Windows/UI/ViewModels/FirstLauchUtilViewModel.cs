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
using DS4Windows.Core.Settings;

using DS4Windows.Core.Model;

namespace DS4Windows.UI.ViewModels
{
    public class FirstLauchUtilViewModel
    {
        private ControlServiceDeviceOptions serviceDeviceOpts;

        public bool EnableDS4
        {
            get => serviceDeviceOpts[InputDeviceFamily.DS4].Enabled;
            set => serviceDeviceOpts[InputDeviceFamily.DS4].Enabled = value;
        }

        public bool EnableDualSense
        {
            get => serviceDeviceOpts[InputDeviceFamily.DualSense].Enabled;
            set => serviceDeviceOpts[InputDeviceFamily.DualSense].Enabled = value;
        }

        public bool EnableSwitchPro
        {
            get => serviceDeviceOpts[InputDeviceFamily.SwitchPro].Enabled;
            set => serviceDeviceOpts[InputDeviceFamily.SwitchPro].Enabled = value;
        }

        public bool EnableJoyCon
        {
            get => serviceDeviceOpts[InputDeviceFamily.JoyCon].Enabled;
            set => serviceDeviceOpts[InputDeviceFamily.JoyCon].Enabled = value;
        }

        public bool EnableDS3
        {
            get => serviceDeviceOpts[InputDeviceFamily.DS3].Enabled;
            set => serviceDeviceOpts[InputDeviceFamily.DS3].Enabled = value;
        }

        public FirstLauchUtilViewModel(ControlServiceDeviceOptions serviceDeviceOpts)
        {
            this.serviceDeviceOpts = serviceDeviceOpts;
        }
    }
}
