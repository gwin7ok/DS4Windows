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

    public class GyroSwipeSettings
    {
        [XmlElement("DeadZoneX")]
        public int DeadZoneX
        {
            get; set;
        } = GyroDirectionalSwipeInfo.DEFAULT_GYRO_DIR_SPEED;

        [XmlElement("DeadZoneY")]
        public int DeadZoneY
        {
            get; set;
        } = GyroDirectionalSwipeInfo.DEFAULT_GYRO_DIR_SPEED;

        [XmlElement("Triggers")]
        public string Triggers
        {
            get; set;
        } = GyroDirectionalSwipeInfo.DEFAULT_TRIGGERS;

        private bool _triggerCond = GyroDirectionalSwipeInfo.DEFAULT_TRIGGER_COND;
        [XmlIgnore]
        public bool TriggerCond
        {
            get => _triggerCond;
            set => _triggerCond = value;
        }

        [XmlElement("TriggerCond")]
        public string TriggerCondString
        {
            get => BackingStore.SaTriggerCondString(_triggerCond);
            set => _triggerCond = BackingStore.SaTriggerCondValue(value);
        }

        private bool _triggerTurns = GyroDirectionalSwipeInfo.DEFAULT_TRIGGER_TURNS;
        [XmlIgnore]
        public bool TriggerTurns
        {
            get => _triggerTurns;
            set => _triggerTurns = value;
        }
        [XmlElement("TriggerTurns")]
        public string TriggerTurnsString
        {
            get => _triggerTurns.ToString();
            set => _triggerTurns = XmlDataUtilities.StrToBool(value);
        }

        [XmlElement("XAxis")]
        public GyroDirectionalSwipeInfo.XAxisSwipe XAxis
        {
            get; set;
        } = GyroDirectionalSwipeInfo.DEFAULT_X_AXIS;

        [XmlElement("DelayTime")]
        public int DelayTime
        {
            get; set;
        } = GyroDirectionalSwipeInfo.DEFAULT_DELAY_TIME;
    }
}