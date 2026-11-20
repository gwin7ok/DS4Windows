#nullable enable

using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Input.Abstractions;

namespace DS4Windows.Processing.Services.Devices
{
    /// <summary>
    /// <see cref="IDeviceLayerSettings"/> の実装。アプリ設定とプロファイル設定のサービスを読むだけの薄い窓口。
    /// </summary>
    public class DeviceLayerSettings : IDeviceLayerSettings
    {
        private readonly IAppSettingsService _appSettings;
        private readonly IProfileSettingsService _profileSettings;

        public DeviceLayerSettings(IAppSettingsService appSettings, IProfileSettingsService profileSettings)
        {
            ArgumentNullException.ThrowIfNull(appSettings);
            _appSettings = appSettings;
            ArgumentNullException.ThrowIfNull(profileSettings);
            _profileSettings = profileSettings;
        }

        public bool UseMoonlight => _appSettings.UseMoonlight;

        public bool UseAdvancedMoonlight => _appSettings.UseAdvancedMoonlight;

        public bool VerboseLogMessages => _appSettings.DeviceOptions.VerboseLogMessages;

    }
}
