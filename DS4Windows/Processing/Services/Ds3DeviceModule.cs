
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;

namespace DS4Windows.Processing.Services
{
    /// <summary>Phase9 9-E E-6c: DS3（DualShock 3）の部品。固有の処理は、設定の有効／無効だけ。</summary>
    public sealed class Ds3DeviceModule : InputDeviceModuleBase
    {
        protected override InputDeviceFamily? Family => InputDeviceFamily.DS3;
    }
}
