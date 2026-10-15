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

    public class StickDeltaAccelSettings
    {
        private bool _enabled;
        [XmlIgnore]
        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }
        [XmlElement("Enabled")]
        public string EnabledString
        {
            get => _enabled.ToString();
            set => _enabled = XmlDataUtilities.StrToBool(value);
        }

        private double _multiplier = DeltaAccelSettings.MULTIPLIER_DEFAULT;
        [XmlElement("Multiplier")]
        public double Multiplier
        {
            get => _multiplier;
            set => _multiplier = Math.Clamp(value, 0.0, 10.0);
        }

        private double _maxTravel = DeltaAccelSettings.MAX_TRAVEL_DEFAULT;
        [XmlElement("MaxTravel")]
        public double MaxTravel
        {
            get => _maxTravel;
            set => _maxTravel = Math.Clamp(value, 0.0, 1.0);
        }

        private double _minTravel = DeltaAccelSettings.MIN_TRAVEL_DEFAULT;
        [XmlElement("MinTravel")]
        public double MinTravel
        {
            get => _minTravel;
            set => _minTravel = Math.Clamp(value, 0.0, 1.0);
        }

        private double _easingDuration = DeltaAccelSettings.EASING_DURATION_DEFAULT;
        [XmlElement("EasingDuration")]
        public double EasingDuration
        {
            get => _easingDuration;
            set => _easingDuration = Math.Clamp(value, 0.0, 600.0);
        }

        private double _minFactor = DeltaAccelSettings.MINFACTOR_DEFAULT;
        [XmlElement("MinFactor")]
        public double MinFactor
        {
            get => _minFactor;
            set => _minFactor = Math.Clamp(value, 1.0, 10.0);
        }
    }
}