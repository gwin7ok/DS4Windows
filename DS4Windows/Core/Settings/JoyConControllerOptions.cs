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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using DS4Windows.Core.Model;
using DS4Windows.Core.Persistence.Dto;

namespace DS4Windows.Core.Settings
{

    public class JoyConControllerOptions : ControllerOptionsStore
    {
        public const string XML_ELEMENT_NAME = "JoyConSupportSettings";

        private bool enableHomeLED = true;
        public bool EnableHomeLED
        {
            get => enableHomeLED;
            set
            {
                if (enableHomeLED == value) return;
                enableHomeLED = value;
                EnableHomeLEDChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler EnableHomeLEDChanged;

        public JoyConControllerOptions(InputDeviceType deviceType) :
            base(deviceType)
        {
        }

        public override void PersistSettings(XmlDocument xmlDoc, XmlNode node)
        {
            JoyConControllerOptsDTO dto = new JoyConControllerOptsDTO();
            dto.MapFrom(this);
            PersistDto(xmlDoc, node, XML_ELEMENT_NAME, dto);
        }

        public override void LoadSettings(XmlDocument xmlDoc, XmlNode node)
        {
            LoadDto<JoyConControllerOptsDTO>(node, XML_ELEMENT_NAME, dto => dto.MapTo(this));
        }
    }
}
