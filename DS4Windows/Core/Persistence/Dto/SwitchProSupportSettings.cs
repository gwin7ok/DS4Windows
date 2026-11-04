using System;
using System.Xml.Serialization;
using System.Globalization;
using DS4Windows.Core.Persistence.Dto;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.Dto;

public class SwitchProSupportSettings : BaseInputDeviceSettingsGroup
{
    public SwitchProSupportSettings() : base()
    {
        Enabled = SwitchProDeviceOptions.DEFAULT_ENABLE;
    }
}
