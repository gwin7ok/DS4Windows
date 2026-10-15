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

    public class TouchpadMouseStickSerializer
    {
        public class SmoothingGroupSerializer
        {
            private TouchMouseStickInfo.SmoothingMethod _smoothMethod =
                TouchMouseStickInfo.SmoothingMethod.None;
            [XmlElement("SmoothingMethod")]
            public TouchMouseStickInfo.SmoothingMethod SmoothingMethod
            {
                get => _smoothMethod;
                set => _smoothMethod = value;
            }

            private double _smoothingMinCutoff =
                TouchMouseStickInfo.DEFAULT_MINCUTOFF;
            [XmlElement("SmoothingMinCutoff")]
            public double SmoothingMinCutoff
            {
                get => _smoothingMinCutoff;
                set => _smoothingMinCutoff = value;
            }

            private double _smoothingBeta =
                TouchMouseStickInfo.DEFAULT_BETA;
            [XmlElement("SmoothingBeta")]
            public double SmoothingBeta
            {
                get => _smoothingBeta;
                set => _smoothingBeta = value;
            }
        }

        private int _deadZone = TouchMouseStickInfo.DEFAULT_DEADZONE;
        [XmlElement("DeadZone")]
        public int DeadZone
        {
            get => _deadZone;
            set => _deadZone = value;
        }

        private int _maxZone = TouchMouseStickInfo.MAX_ZONE_DEFAULT;
        [XmlElement("MaxZone")]
        public int MaxZone
        {
            get => _maxZone;
            set => _maxZone = value;
        }

        private TouchMouseStickInfo.OutputStick _outputStick =
            TouchMouseStickInfo.DEFAULT_OUTPUT_STICK;
        [XmlElement("OutputStick")]
        public TouchMouseStickInfo.OutputStick OutputStick
        {
            get => _outputStick;
            set => _outputStick = value;
        }

        private TouchMouseStickInfo.OutputStickAxes _outputStickAxes =
            TouchMouseStickInfo.DEFAULT_OUTPUT_STICK_AXES;
        [XmlElement("OutputStickAxes")]
        public TouchMouseStickInfo.OutputStickAxes OutputStickAxes
        {
            get => _outputStickAxes;
            set => _outputStickAxes = value;
        }

        private double _antiDeadZoneX = TouchMouseStickInfo.ANTI_DEADZONE_DEFAULT;
        [XmlElement("AntiDeadX")]
        public double AntiDeadX
        {
            get => _antiDeadZoneX;
            set => _antiDeadZoneX = value;
        }

        private double _antiDeadZoneY = TouchMouseStickInfo.ANTI_DEADZONE_DEFAULT;
        [XmlElement("AntiDeadY")]
        public double AntiDeadY
        {
            get => _antiDeadZoneY;
            set => _antiDeadZoneY = value;
        }

        private uint _invert = TouchMouseStickInfo.DEFAULT_INVERTED;
        [XmlElement("Invert")]
        public uint Invert
        {
            get => _invert;
            set => _invert = value;
        }

        private double _maxOutput = TouchMouseStickInfo.DEFAULT_MAX_OUTPUT;
        [XmlElement("MaxOutput")]
        public double MaxOutput
        {
            get => _maxOutput;
            set => _maxOutput = value;
        }

        private bool _maxOutputEnabled = TouchMouseStickInfo.DEFAULT_MAX_OUTPUT_ENABLED;
        [XmlIgnore]
        public bool MaxOutputEnabled
        {
            get => _maxOutputEnabled;
            set => _maxOutputEnabled = value;
        }

        [XmlElement("MaxOutputEnabled")]
        public string MaxOutputEnabledString
        {
            get => _maxOutputEnabled.ToString();
            set => _maxOutputEnabled = XmlDataUtilities.StrToBool(value);
        }

        private int _verticalScale = TouchMouseStickInfo.DEFAULT_VERT_SCALE;
        [XmlElement("VerticalScale")]
        public int VerticalScale
        {
            get => _verticalScale;
            set => _verticalScale = value;
        }

        private StickOutCurve.Curve _outputCurve = TouchMouseStickInfo.OUTPUT_CURVE_DEFAULT;
        [XmlElement("OutputCurve")]
        public StickOutCurve.Curve OutputCurve
        {
            get => _outputCurve;
            set => _outputCurve = value;
        }

        private double _rotationRad = TouchMouseStickInfo.ANG_RAD_DEFAULT;
        [XmlElement("Rotation")]
        public int Rotation
        {
            get => (int)(_rotationRad / 180 * Math.PI);
            set => _rotationRad = value * Math.PI / 180.0;
        }

        [XmlIgnore]
        public double RotationRad
        {
            get => _rotationRad;
            set => _rotationRad = value;
        }

        [XmlElement("SmoothingSettings")]
        public SmoothingGroupSerializer SmoothingSettings
        {
            get; set;
        }
        public bool ShouldSerializeSmoothingSettings()
        {
            return SmoothingSettings != null;
        }
    }
}