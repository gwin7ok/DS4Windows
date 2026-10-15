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
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using DS4Windows;
using DS4Windows.InputDevices;
using DS4Windows.StickModifiers;
using DS4WinWPF.DS4Forms.ViewModels;
using static DS4Windows.Mouse;

namespace DS4WinWPF.DS4Control.DTOXml
{

    public class StickAxialDeadOptionsSerializer
    {
        private int _deadZoneX;
        [XmlElement("DeadZoneX")]
        public int DeadZoneX
        {
            get => _deadZoneX;
            set => _deadZoneX = Math.Clamp(value, 0, 127);
        }

        private int _deadZoneY;
        [XmlElement("DeadZoneY")]
        public int DeadZoneY
        {
            get => _deadZoneY;
            set => _deadZoneY = Math.Clamp(value, 0, 127);
        }

        private int _maxZoneX;
        [XmlElement("MaxZoneX")]
        public int MaxZoneX
        {
            get => _maxZoneX;
            set => _maxZoneX = Math.Clamp(value, 0, 100);
        }

        private int _maxZoneY;
        [XmlElement("MaxZoneY")]
        public int MaxZoneY
        {
            get => _maxZoneY;
            set => _maxZoneY = Math.Clamp(value, 0, 100);
        }

        private int _antiDeadZoneX;
        [XmlElement("AntiDeadZoneX")]
        public int AntiDeadZoneX
        {
            get => _antiDeadZoneX;
            set => _antiDeadZoneX = Math.Clamp(value, 0, 100);
        }

        private int _antiDeadZoneY;
        [XmlElement("AntiDeadZoneY")]
        public int AntiDeadZoneY
        {
            get => _antiDeadZoneY;
            set => _antiDeadZoneY = Math.Clamp(value, 0, 100);
        }

        private double _maxOutputX;
        [XmlElement("MaxOutputX")]
        public double MaxOutputX
        {
            get => _maxOutputX;
            set => _maxOutputX = Math.Clamp(value, 0.0, 100.0);
        }

        private double _maxOutputY;
        [XmlElement("MaxOutputY")]
        public double MaxOutputY
        {
            get => _maxOutputY;
            set => _maxOutputY = Math.Clamp(value, 0.0, 100.0);
        }
    }
}