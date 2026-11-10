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

    public class DS4ControllerOptions : ControllerOptionsStore
    {
        public const string XML_ELEMENT_NAME = "DS4SupportSettings";
        private bool copyCatController;
        public bool IsCopyCat
        {
            get => copyCatController;
            set
            {
                if (copyCatController == value) return;
                copyCatController = value;
                IsCopyCatChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler IsCopyCatChanged;

        public DS4ControllerOptions(InputDeviceType deviceType) : base(deviceType)
        {
        }

        public override void PersistSettings(XmlDocument xmlDoc, XmlNode node)
        {
            DS4ControllerOptsDTO dto = new DS4ControllerOptsDTO();
            dto.MapFrom(this);
            PersistDto(xmlDoc, node, XML_ELEMENT_NAME, dto);
        }

        public override void LoadSettings(XmlDocument xmlDoc, XmlNode node)
        {
            LoadDto<DS4ControllerOptsDTO>(node, XML_ELEMENT_NAME, dto => dto.MapTo(this));
        }
    }
}
