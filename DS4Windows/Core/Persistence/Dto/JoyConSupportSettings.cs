using System;
using System.Xml.Serialization;
using System.Globalization;
using DS4Windows.Core.Persistence.Dto;
using DS4Windows.Core.Settings;

namespace DS4Windows.Core.Persistence.Dto;

public class JoyConSupportSettings : BaseInputDeviceSettingsGroup
{
    public JoyConDeviceOptions.LinkMode LinkMode
    {
        get; set;
    } = JoyConDeviceOptions.LINK_MODE_DEFAULT;

    public JoyConDeviceOptions.JoinedGyroProvider JoinedGyroProvider
    {
        get; set;
    }

    public JoyConSupportSettings() : base()
    {
        Enabled = JoyConDeviceOptions.DEFAULT_ENABLE;
    }
}