using System;
using System.Xml.Serialization;
using System.Globalization;
using DS4Windows.Core.Persistence.Dto;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.Dto;

public class DS4SupportSettingsGroup : BaseInputDeviceSettingsGroup
{
    public DS4SupportSettingsGroup() : base()
    {
        Enabled = DS4DeviceOptions.DEFAULT_ENABLE;
    }
}
