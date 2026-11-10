using System.Collections.Generic;
using System.Linq;
using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Devices;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 接続時のシリアルの読み方（<see cref="IInputDeviceCartridge.ReadSerial"/>）が、カートリッジへ移す前に <c>InputDeviceRegistry</c> が種類ごとの
    /// <c>if</c> で行っていた読み方と同じであることを、対応機器の全行について確かめる（偽の通信路で、呼び出しと戻り値を比べる）。
    /// </summary>
    public class InputDeviceCartridgeSerialTests
    {
        // 移す前の読み方（そのまま）。戻り値と、読んだ特徴レポート番号（擬似のシリアルは -2）を返す
        private static (string serial, int featureId) Legacy(VidPidInfo metainfo, FakeHidTransport hid)
        {
            string serial;
            if (metainfo.inputDevType == InputDeviceType.DualSense)
            {
                serial = hid.ReadSerial(DualSenseDevice.SERIAL_FEATURE_ID);
            }
            else if (metainfo.inputDevType == InputDeviceType.DS4 &&
                metainfo.checkConnection(hid) == ConnectionType.SONYWA)
            {
                serial = hid.GenerateFakeHwSerial();
            }
            else
            {
                serial = hid.ReadSerial(InputDeviceCore.SERIAL_FEATURE_ID);
            }

            return (serial, hid.LastSerialFeatureId);
        }

        [Fact]
        public void EveryKnownDevice_ReadsTheSerialTheWayTheOldBranchesDid()
        {
            var cartridges = TestHost.GetService<IEnumerable<IInputDeviceCartridge>>().ToList();
            int checkedCount = 0;
            foreach (VidPidInfo info in cartridges.SelectMany(c => c.KnownDevices))
            {
                var legacyHid = new FakeHidTransport { Serial = "AA:BB", FakeHwSerial = "FA:KE" };
                var newHid = new FakeHidTransport { Serial = "AA:BB", FakeHwSerial = "FA:KE" };
                (string expected, int expectedId) = Legacy(info, legacyHid);

                IInputDeviceCartridge cartridge = cartridges.Single(c => c.Handles(info.inputDevType));
                string actual = cartridge.ReadSerial(info, newHid);

                Assert.Equal(expected, actual);
                Assert.Equal(expectedId, newHid.LastSerialFeatureId);
                checkedCount++;
            }

            Assert.Equal(38, checkedCount);
        }

        [Fact]
        public void DualSense_ReadsFeatureReport9_AndOthersRead18()
        {
            var cartridges = TestHost.GetService<IEnumerable<IInputDeviceCartridge>>().ToList();
            foreach (VidPidInfo info in cartridges.SelectMany(c => c.KnownDevices).Where(x => x.inputDevType == InputDeviceType.DualSense))
            {
                var hid = new FakeHidTransport();
                cartridges.Single(c => c.Handles(info.inputDevType)).ReadSerial(info, hid);
                Assert.Equal(9, hid.LastSerialFeatureId);
            }

            VidPidInfo switchPro = cartridges.SelectMany(c => c.KnownDevices).First(x => x.inputDevType == InputDeviceType.SwitchPro);
            var hid2 = new FakeHidTransport();
            cartridges.Single(c => c.Handles(InputDeviceType.SwitchPro)).ReadSerial(switchPro, hid2);
            Assert.Equal(18, hid2.LastSerialFeatureId);
        }

    }
}
