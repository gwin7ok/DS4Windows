using DS4Windows.InputDevices;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-E E-6c（Phase9-E6-Plan.md §3.1）: デバイスの種類から、その種類の <see cref="IInputDeviceModule"/> を引く。
    /// 扱う部品がない種類は、何もしない部品（設定は無効、追加ボタンなし）を返す（旧コードの <c>default:</c> と同じ）。
    /// </summary>
    public interface IInputDeviceModuleRegistry
    {
        IInputDeviceModule Resolve(InputDeviceType type);
    }
}
