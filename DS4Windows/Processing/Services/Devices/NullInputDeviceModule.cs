#nullable enable

using DS4Windows.Core.Model;

namespace DS4Windows.Processing.Services.Devices
{
    /// <summary>
    /// 扱う部品がない種類のための、何もしない部品（設定は無効）。旧 <c>CheckForSupportedDevice</c>／<c>GetKnownExtraButtons</c> の <c>default:</c> と同じ結果。
    /// </summary>
    public sealed class NullInputDeviceModule : InputDeviceModuleBase
    {
        protected override InputDeviceFamily? Family => null;
    }
}
