using DS4Windows.InputDevices;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-E E-6c: 扱う部品がない種類のための、何もしない部品（設定は無効）。旧 <c>CheckForSupportedDevice</c>／<c>GetKnownExtraButtons</c> の <c>default:</c> と同じ結果。
    /// </summary>
    public sealed class NullInputDeviceModule : InputDeviceModuleBase
    {
        public override bool Handles(InputDeviceType type) => false;

        public override bool IsEnabled(ControlServiceDeviceOptions options) => false;
    }
}
