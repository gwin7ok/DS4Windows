using System;
using System.Collections.Generic;
using System.Globalization;
using DS4Windows.DS4Control;
using DS4Windows.InputDevices;
using static DS4Windows.Mouse;

namespace DS4Windows.DI
{
    public class ProfileSettingChangedEventArgs : EventArgs
    {
        public int DeviceIndex { get; }
        public string SettingName { get; }
        public object OldValue { get; }
        public object NewValue { get; }

        public ProfileSettingChangedEventArgs(int deviceIndex, string settingName, object oldValue, object newValue)
        {
            DeviceIndex = deviceIndex;
            SettingName = settingName;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}