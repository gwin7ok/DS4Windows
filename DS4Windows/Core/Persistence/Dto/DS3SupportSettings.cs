using System;
using System.Xml.Serialization;
using System.Globalization;
using DS4Windows.Core.Persistence.Dto;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.Dto;

public class DS3SupportSettings : BaseInputDeviceSettingsGroup
{
    public DS3SupportSettings() : base()
    {
        Enabled = DS3DeviceOptions.DEFAULT_ENABLE;
    }
}
