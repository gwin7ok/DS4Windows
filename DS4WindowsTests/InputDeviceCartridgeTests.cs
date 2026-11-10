using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Devices;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 入力デバイスの種類ごとのカートリッジ（<see cref="IInputDeviceCartridge"/>）の確認。
    /// 対応機器の表は、カートリッジへ分ける前の <c>DS4Devices</c> の静的な表と同じ内容（42 件。VID／PID・名前・種類・機能フラグ・接続方式の判定）でなければならない
    /// （下の黄金の表は、分ける前の実際の表を書き出したもの）。機種ごとの設定（<see cref="DeviceOptionsBase"/>）の既定値と通知も確かめる。
    /// </summary>
    public class InputDeviceCartridgeTests
    {
        // vid|pid|name|type|featureSet|checkConnection（分ける前の表の書き出し。順序は表の並びのまま）
        private static readonly string[] Golden =
        {
            "0x054C|0x0BA0|Sony WA|DS4|DefaultDS4|InputDeviceCore.HidConnectionType",
            "0x054C|0x05C4|DS4 v.1|DS4|DefaultDS4|InputDeviceCore.HidConnectionType",
            "0x054C|0x09CC|DS4 v.2|DS4|DefaultDS4|InputDeviceCore.HidConnectionType",
            "0x054C|0x0CE6|DualSense|DualSense|DefaultDS4|DualSenseDevice.DetermineConnectionType",
            "0x054C|0x0DF2|DualSense Edge|DualSense|DefaultDS4|DualSenseDevice.DetermineConnectionType",
            "0x1532|0x1000|Razer Raiju PS4|DS4|DefaultDS4|InputDeviceCore.HidConnectionType",
            "0x1532|0x1100|Razer Raion Fightpad PS4|DS4|NoGyroCalib|InputDeviceCore.HidConnectionType",
            "0x146B|0x0D01|Nacon Revol Pro v.1|DS4|NoGyroCalib|InputDeviceCore.HidConnectionType",
            "0x146B|0x0D02|Nacon Revol Pro v.2|DS4|NoGyroCalib|InputDeviceCore.HidConnectionType",
            "0x0F0D|0x00EE|Hori PS4 Mini|DS4|NoOutputData, NoBatteryReading, NoGyroCalib|InputDeviceCore.HidConnectionType",
            "0x7545|0x0104|Armor 3 LU Cobra|DS4|DefaultDS4|InputDeviceCore.HidConnectionType",
            "0x2E95|0x7725|Scuf Vantage|DS4|DefaultDS4|InputDeviceCore.HidConnectionType",
            "0x11C0|0x4001|PS4 Fun|DS4|DefaultDS4|InputDeviceCore.HidConnectionType",
            "0x0C12|0x0E20|Brook Mars Controller|DS4|DefaultDS4|InputDeviceCore.HidConnectionType",
            "0x1532|0x1007|Razer Raiju TE|DS4|DefaultDS4|InputDeviceCore.HidConnectionType",
            "0x1532|0x100A|Razer Raiju TE BT|DS4|OnlyInputData0x01, OnlyOutputData0x05, NoBatteryReading, NoGyroCalib|InputDeviceCore.HidConnectionType",
            "0x1532|0x1004|Razer Raiju UE USB|DS4|DefaultDS4|InputDeviceCore.HidConnectionType",
            "0x1532|0x1009|Razer Raiju UE BT|DS4|OnlyInputData0x01, OnlyOutputData0x05, NoBatteryReading, NoGyroCalib|InputDeviceCore.HidConnectionType",
            "0x054C|0x05C5|CronusMax (PS4 Mode)|DS4|DefaultDS4|InputDeviceCore.HidConnectionType",
            "0x0C12|0x57AB|Warrior Joypad JS083|DS4|NoGyroCalib|InputDeviceCore.HidConnectionType",
            "0x0C12|0x0E16|Steel Play MetalTech|DS4|DefaultDS4|InputDeviceCore.HidConnectionType",
            "0x146B|0x0D08|Nacon Revol U Pro|DS4|DefaultDS4|InputDeviceCore.HidConnectionType",
            "0x146B|0x0D10|Nacon Revol Infinite|DS4|DefaultDS4|InputDeviceCore.HidConnectionType",
            "0x0F0D|0x0084|Hori Fighting Cmd|DS4|DefaultDS4|InputDeviceCore.HidConnectionType",
            "0x146B|0x0D13|Nacon Revol Pro v.3|DS4|DefaultDS4|InputDeviceCore.HidConnectionType",
            "0x0F0D|0x0055|Horipad 4 FPS|DS4|NoGyroCalib|InputDeviceCore.HidConnectionType",
            "0x0F0D|0x0066|Horipad FPS Plus|DS4|NoGyroCalib|InputDeviceCore.HidConnectionType",
            "0x9886|0x0025|Astro C40|DS4|NoGyroCalib|InputDeviceCore.HidConnectionType",
            "0x0E8F|0x1114|Gamo2 Divaller|DS4|NoGyroCalib|InputDeviceCore.HidConnectionType",
            "0x0F0D|0x0101|Hori Mini Hatsune Miku FT|DS4|NoGyroCalib|InputDeviceCore.HidConnectionType",
            "0x0F0D|0x00C9|Hori Taiko Controller|DS4|NoGyroCalib|InputDeviceCore.HidConnectionType",
            "0x0C12|0x1E1C|SnakeByte Game:Pad 4S|DS4|NoBatteryReading, NoGyroCalib|InputDeviceCore.HidConnectionType",
            "0x057E|0x2009|Switch Pro|SwitchPro|DefaultDS4|SwitchProDevice.DetermineConnectionType",
            "0x7545|0x1122|Gioteck VX4|DS4|DefaultDS4|InputDeviceCore.HidConnectionType",
            "0x7331|0x0001|DualShock 3 (DS4 Emulation)|DS4|NoGyroCalib, VendorDefinedDevice|InputDeviceCore.HidConnectionType",
            "0x20D6|0x792A|PowerA FUSION Wired Fightpad for PS4|DS4|NoGyroCalib|InputDeviceCore.HidConnectionType",
            "0x044F|0xD00E|Thrustmaster eSwap Pro|DS4|NoBatteryReading, NoGyroCalib|InputDeviceCore.HidConnectionType",
            "0x0C12|0x0E15|Playmax Wired Controller (PS4)|DS4|NoBatteryReading, NoGyroCalib|InputDeviceCore.HidConnectionType",
        };

        private static IEnumerable<IInputDeviceCartridge> AllCartridges()
        {
            return TestHost.GetService<IEnumerable<IInputDeviceCartridge>>();
        }

        private static string Describe(VidPidInfo x)
        {
            return $"0x{x.vid:X4}|0x{x.pid:X4}|{x.name}|{x.inputDevType}|{x.featureSet}|{x.checkConnection.Method.DeclaringType.Name}.{x.checkConnection.Method.Name}";
        }

        [Fact]
        public void TheCartridgeTables_HoldTheSameDevicesAsTheTableBeforeTheSplit()
        {
            var actual = AllCartridges().SelectMany(c => c.KnownDevices).Select(Describe).ToList();

            Assert.Equal(Golden.Length, actual.Count);
            // 順序は結果に影響しない（VID／PID で引く）ため、集合として一致すればよい
            Assert.Equal(Golden.OrderBy(x => x, StringComparer.Ordinal), actual.OrderBy(x => x, StringComparer.Ordinal));
        }

        [Fact]
        public void EveryVidPid_IsUnique_AndHandledByExactlyOneCartridge()
        {
            var cartridges = AllCartridges().ToList();
            var all = cartridges.SelectMany(c => c.KnownDevices).ToList();

            Assert.Equal(all.Count, all.Select(x => (x.vid, x.pid)).Distinct().Count());   // DS4Devices は Single(...) で引く
            foreach (var cartridge in cartridges)
            {
                foreach (var info in cartridge.KnownDevices)
                {
                    Assert.True(cartridge.Handles(info.inputDevType), $"{info.name} の種類を、載っているカートリッジが扱えない");
                    Assert.Single(cartridges, c => c.Handles(info.inputDevType));
                }
            }
        }

        [Fact]
        public void TheFiveKindsOfDevice_HaveACartridgeEach()
        {
            var cartridges = AllCartridges().ToList();

            Assert.Equal(3, cartridges.Count);
            foreach (InputDeviceType type in Enum.GetValues(typeof(InputDeviceType)))
            {
                Assert.Single(cartridges, c => c.Handles(type));
            }
        }

        [Fact]
        public void TheRegistry_BuildsItsTableFromTheCartridges_AndAnEmptyRegistryHasNone()
        {
            Assert.Throws<ArgumentNullException>(() => new DS4Devices(null, null));
            var devices = new DS4Devices(null, Array.Empty<IInputDeviceCartridge>());
            Assert.Empty(devices.getDS4Controllers());
        }

        [Fact]
        public void DeviceOptions_KeepTheirDefaults_AndNotifyOnlyOnAChange()
        {
            Assert.True(new DS4DeviceOptions().Enabled);
            Assert.False(new DualSenseDeviceOptions().Enabled);
            Assert.False(new SwitchProDeviceOptions().Enabled);

            var options = new DualSenseDeviceOptions();
            int notified = 0;
            object sender = null;
            options.EnabledChanged += (s, e) => { notified++; sender = s; };
            options.Enabled = false;   // 同じ値: 通知しない
            Assert.Equal(0, notified);
            options.Enabled = true;
            Assert.Equal(1, notified);
            Assert.Same(options, sender);
            Assert.True(options.Enabled);
        }
    }
}
