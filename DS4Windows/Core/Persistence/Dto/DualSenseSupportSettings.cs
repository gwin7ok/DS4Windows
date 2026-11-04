using System;
using System.Xml.Serialization;
using System.Globalization;
using DS4Windows.Core.Persistence.Dto;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.Dto;

public class DualSenseSupportSettings : BaseInputDeviceSettingsGroup
{
    public DualSenseSupportSettings() : base()
    {
        Enabled = DualSenseDeviceOptions.DEFAULT_ENABLE;
    }
}
