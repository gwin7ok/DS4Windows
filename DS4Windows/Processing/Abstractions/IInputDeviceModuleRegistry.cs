
using DS4Windows.Core.Model;

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// デバイスの種類から、その種類の <see cref="IInputDeviceModule"/> を引く。
    /// 扱う部品がない種類は、何もしない部品（設定は無効、追加ボタンなし）を返す。
    /// </summary>
    public interface IInputDeviceModuleRegistry
    {
        IInputDeviceModule Resolve(InputDeviceType type);
    }
}
