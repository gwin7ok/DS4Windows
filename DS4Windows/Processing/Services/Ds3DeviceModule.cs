using DS4Windows.InputDevices;

namespace DS4Windows.Services
{
    /// <summary>Phase9 9-E E-6c: DS3（DualShock 3）の部品。固有の処理は、設定の有効／無効だけ。</summary>
    public sealed class Ds3DeviceModule : InputDeviceModuleBase
    {
        public override bool Handles(InputDeviceType type) => type == InputDeviceType.DS3;

        public override bool IsEnabled(ControlServiceDeviceOptions options) => options.DS3DeviceOpts.Enabled;
    }
}
