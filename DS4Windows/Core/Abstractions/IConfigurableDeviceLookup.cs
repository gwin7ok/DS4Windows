namespace DS4Windows.Core.Abstractions
{
    /// <summary>スロット番号から接続中のデバイスを、狭い契約（<see cref="IConfigurableDevice"/>）で引く。未接続・範囲外は null。</summary>
    public interface IConfigurableDeviceLookup
    {
        IConfigurableDevice GetConfigurableDevice(int deviceIndex);
    }
}
