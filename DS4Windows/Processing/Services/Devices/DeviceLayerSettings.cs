using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Input.Abstractions;

namespace DS4Windows.Processing.Services
{
    /// <summary>
    /// Phase7-Step10d W3（Q4＝A）: <see cref="IDeviceLayerSettings"/> の実装。アプリ設定とプロファイル設定のサービスを読むだけの薄い窓口
    /// （旧: 第 1 層が <c>Global</c> 経由で読んでいたのと同じ実体）。
    /// </summary>
    public class DeviceLayerSettings : IDeviceLayerSettings
    {
        private readonly IAppSettingsService _appSettings;
        private readonly IProfileSettingsService _profileSettings;

        public DeviceLayerSettings(IAppSettingsService appSettings, IProfileSettingsService profileSettings)
        {
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
        }

        public bool UseMoonlight => _appSettings.UseMoonlight;

        public bool UseAdvancedMoonlight => _appSettings.UseAdvancedMoonlight;

        public bool VerboseLogMessages => _appSettings.DeviceOptions.VerboseLogMessages;

    }
}
