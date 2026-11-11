using System;
using System.Threading.Tasks;
using DS4Windows.Input.Abstractions;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace DS4Windows.Input.Xbox
{
    /// <summary>
    /// <see cref="IGattBatteryFetcher"/> の実装（WinRT）。Bluetooth LE の機器の、標準のバッテリーサービス（0x180F）のバッテリーレベル（0x2A19。0〜100% の 1 バイト）を読む。
    /// 機器の状態は変えない（読み取りだけ）。サービスが見つからない、アクセスを拒まれたときは null。
    /// </summary>
    public sealed class GattBatteryFetcher : IGattBatteryFetcher
    {
        public async Task<int?> FetchAsync(ulong bluetoothAddress)
        {
            using (BluetoothLEDevice device = await BluetoothLEDevice.FromBluetoothAddressAsync(bluetoothAddress))
            {
                if (device == null)
                {
                    return null;
                }

                GattDeviceServicesResult services = await device.GetGattServicesForUuidAsync(GattServiceUuids.Battery, BluetoothCacheMode.Uncached);
                if (services.Status != GattCommunicationStatus.Success || services.Services.Count == 0)
                {
                    return null;
                }

                using (GattDeviceService service = services.Services[0])
                {
                    GattCharacteristicsResult characteristics = await service.GetCharacteristicsForUuidAsync(GattCharacteristicUuids.BatteryLevel, BluetoothCacheMode.Uncached);
                    if (characteristics.Status != GattCommunicationStatus.Success || characteristics.Characteristics.Count == 0)
                    {
                        return null;
                    }

                    GattReadResult read = await characteristics.Characteristics[0].ReadValueAsync(BluetoothCacheMode.Uncached);
                    if (read.Status != GattCommunicationStatus.Success || read.Value == null || read.Value.Length < 1)
                    {
                        return null;
                    }

                    DataReader reader = DataReader.FromBuffer(read.Value);
                    return Math.Min((int)reader.ReadByte(), 100);
                }
            }
        }
    }
}
