using System;
using System.Reflection;
using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Input.Devices;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// DualSense の出力レポートのバイト列（USB・Bluetooth）。実機がないため、設定の組み合わせごとの期待値を、
    /// 組み立てを部品へ出す前の実装が出した記録そのものとして固定する（振動、ライトバー、プレイヤー LED、ミュート LED、触覚の強さ、トリガーの効果、Bluetooth の CRC）。
    /// </summary>
    public class DualSenseOutputReportTests
    {
        private readonly ITestOutputHelper output;

        public DualSenseOutputReportTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        private sealed class Scenario
        {
            public bool UseRumble;
            public bool UseAccurateRumble;
            public byte Right;
            public byte Left;
            public DS4Color Color;
            public byte HapticPower;
            public byte Mute;
            public byte PlayerMask;
            public DualSenseDevice.TriggerEffectData R2;
            public DualSenseDevice.TriggerEffectData L2;
        }

        private static DualSenseDevice.TriggerEffectData Effect(byte seed)
        {
            return new DualSenseDevice.TriggerEffectData
            {
                triggerMotorMode = seed,
                triggerStartResistance = (byte)(seed + 1),
                triggerEffectForce = (byte)(seed + 2),
                triggerRangeForce = (byte)(seed + 3),
                triggerNearReleaseStrength = (byte)(seed + 4),
                triggerNearMiddleStrength = (byte)(seed + 5),
                triggerPressedStrength = (byte)(seed + 6),
                triggerActuationFrequency = (byte)(seed + 7),
            };
        }

        private static readonly Scenario[] Scenarios =
        {
            new Scenario { UseRumble = true, UseAccurateRumble = true, Right = 200, Left = 100, Color = new DS4Color(10, 20, 30), HapticPower = 3, Mute = 0x04, PlayerMask = 0x15, R2 = Effect(0x11), L2 = Effect(0x21) },
            new Scenario { UseRumble = false, UseAccurateRumble = false, Right = 50, Left = 60, Color = new DS4Color(0, 0, 0), HapticPower = 0, Mute = 0x00, PlayerMask = 0x00 },
            new Scenario { UseRumble = false, UseAccurateRumble = true, Right = 255, Left = 255, Color = new DS4Color(255, 128, 1), HapticPower = 7, Mute = 0x01, PlayerMask = 0x1F, R2 = Effect(0xF0), L2 = Effect(0x05) },
            new Scenario { UseRumble = true, UseAccurateRumble = false, Right = 1, Left = 2, Color = new DS4Color(1, 2, 3), HapticPower = 255, Mute = 0xFF, PlayerMask = 0x0A, R2 = Effect(0x40) },
        };

        private static FieldInfo Field(Type type, string name)
        {
            for (Type t = type; t != null; t = t.BaseType)
            {
                FieldInfo f = t.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (f != null) return f;
            }

            throw new InvalidOperationException(name);
        }

        private static string Capture(bool bluetooth, Scenario s)
        {
            var transport = bluetooth
                ? new FakeHidTransport(inputReportByteLength: 78, outputReportByteLength: 78, vendorId: 0x054C, productId: 0x0CE6)
                : new FakeHidTransport(inputReportByteLength: 64, outputReportByteLength: 48, vendorId: 0x054C, productId: 0x0CE6);
            var device = new DualSenseDevice(transport, "DualSense", VidPidFeatureSet.NoGyroCalib);
            device.PostInit();

            Type type = typeof(DualSenseDevice);
            Field(type, "useRumble").SetValue(device, s.UseRumble);
            Field(type, "useAccurateRumble").SetValue(device, s.UseAccurateRumble);
            Field(type, "hapticPowerLevel").SetValue(device, s.HapticPower);
            Field(type, "muteLEDByte").SetValue(device, s.Mute);
            Field(type, "activePlayerLEDMask").SetValue(device, s.PlayerMask);
            Field(type, "r2EffectData").SetValue(device, s.R2);
            Field(type, "l2EffectData").SetValue(device, s.L2);

            var hap = new DS4HapticState { dirty = true };
            hap.rumbleState.RumbleMotorStrengthRightLightFast = s.Right;
            hap.rumbleState.RumbleMotorStrengthLeftHeavySlow = s.Left;
            hap.lightbarState.LightBarColor = s.Color;
            Field(type, "currentHap").SetValue(device, hap);

            // 出力の組み立てのあとのバイト列を、そのまま取り出す
            type.GetMethod("PrepareOutReport", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(device, null);
            var report = (byte[])Field(type, "outputReport").GetValue(device);
            return Convert.ToHexString(report);
        }

        private static readonly string[] UsbGolden =
        {
            "020F55C864000000000400111213141516170000180021222324252627000028000000000003000600000202150A141E",
            "020C55000000000000000000000000000000000000000000000000000000000000000000000000020000020200000000",
            "020C55FFFF000000000100F0F1F2F3F4F5F60000F70005060708090A0B00000C0000000000070006000002021FFF8001",
            "020F55010200000000FF004041424344454600004700000000000000000000000000000000FF0002000002020A010203",
        };

        private static readonly string[] BtGolden =
        {
            "31020F55C864000000000400111213141516170000180021222324252627000028000000000003000600000202150A141E00000000000000000000000000000000000000000000000000FFFFFFFF",
            "31020C5500000000000000000000000000000000000000000000000000000000000000000000000002000002020000000000000000000000000000000000000000000000000000000000FFFFFFFF",
            "31020C55FFFF000000000100F0F1F2F3F4F5F60000F70005060708090A0B00000C0000000000070006000002021FFF800100000000000000000000000000000000000000000000000000FFFFFFFF",
            "31020F55010200000000FF004041424344454600004700000000000000000000000000000000FF0002000002020A01020300000000000000000000000000000000000000000000000000FFFFFFFF",
        };

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void TheUsbReport_MatchesTheRecordedBytes(int scenario)
        {
            Assert.Equal(UsbGolden[scenario], Capture(false, Scenarios[scenario]));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void TheBluetoothReport_MatchesTheRecordedBytes(int scenario)
        {
            Assert.Equal(BtGolden[scenario], Capture(true, Scenarios[scenario]));
        }
    }
}
