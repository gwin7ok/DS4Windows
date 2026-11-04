using DS4Windows.InputDevices;

namespace DS4Windows.Services
{
    /// <summary>Phase9 9-E E-6c: DS4（DualShock 4）の部品。固有の処理は、設定の有効／無効だけ。</summary>
    public sealed class Ds4DeviceModule : InputDeviceModuleBase
    {
        public override bool Handles(InputDeviceType type) => type == InputDeviceType.DS4;

        public override bool IsEnabled(ControlServiceDeviceOptions options) => options.DS4DeviceOpts.Enabled;
    }
}
