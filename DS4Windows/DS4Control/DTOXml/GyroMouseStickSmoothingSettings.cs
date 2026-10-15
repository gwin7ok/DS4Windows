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

    public class GyroMouseStickSmoothingSettings
    {
        protected bool _useSmoothing;
        [XmlIgnore]
        public bool UseSmoothing
        {
            get => _useSmoothing;
            set => _useSmoothing = value;
        }

        [XmlElement("UseSmoothing")]
        public string UseSmoothingString
        {
            get => _useSmoothing.ToString();
            set => _useSmoothing = XmlDataUtilities.StrToBool(value);
        }

        protected string _smoothingMethod = GyroMouseStickInfo.DEFAULT_SMOOTH_TECHNIQUE;
        [XmlElement("SmoothingMethod")]
        public string SmoothingMethod
        {
            get => _smoothingMethod;
            set => _smoothingMethod = value;
        }

        protected double _smoothingWeight = GyroMouseStickInfo.SMOOTHING_WEIGHT_DEFAULT;
        [XmlIgnore]
        public double SmoothingWeightRaw
        {
            get => _smoothingWeight;
            set => _smoothingWeight = value;
        }

        [XmlElement("SmoothingWeight")]
        public double SmoothingWeight
        {
            get => _smoothingWeight * 100.0;
            set => _smoothingWeight = Math.Clamp(value * 0.01, 0.0, 1.0);
        }

        protected double _smoothingMinCutoff = GyroMouseStickInfo.DEFAULT_MINCUTOFF;
        [XmlElement("SmoothingMinCutoff")]
        public double SmoothingMinCutoff
        {
            get => _smoothingMinCutoff;
            set => _smoothingMinCutoff = Math.Clamp(value, 0.0, 100.0);
        }

        protected double _smoothingBeta = GyroMouseStickInfo.DEFAULT_BETA;
        [XmlElement("SmoothingBeta")]
        public double SmoothingBeta
        {
            get => _smoothingBeta;
            set => _smoothingBeta = Math.Clamp(value, 0.0, 1.0);
        }
    }
}