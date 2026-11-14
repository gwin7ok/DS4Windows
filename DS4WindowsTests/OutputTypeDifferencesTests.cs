using System;
using System.Collections.Generic;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Output;
using DS4Windows.Output.Backend;
using DS4Windows.Output.Devices;
using DS4Windows.Processing.Macro;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 出力の種類（Xbox 360／DS4）による違いが、種類の名指しの分岐ではなく、値の表（<see cref="OutContTypeInfo"/>）と
    /// 出力デバイスの操作から引かれること。表示名・IPC の名前・DS4 のトリガーの後処理・XInput のスロット番号が、分岐を移す前と同じ結果を返す。
    /// </summary>
    public class OutputTypeDifferencesTests
    {
        [Theory]
        [InlineData("plugx360", true, OutContType.X360)]
        [InlineData("plugds4", true, OutContType.DS4)]
        [InlineData("unplug", false, OutContType.None)]
        [InlineData("PlugDS4", false, OutContType.None)]   // 呼び出し側が小文字にそろえる約束
        [InlineData(null, false, OutContType.None)]
        public void IpcPlugNames_MapToTheTypes(string name, bool ok, OutContType expected)
        {
            Assert.Equal(ok, OutContTypeInfo.TryFromIpcPlugName(name, out OutContType type));
            Assert.Equal(expected, type);
        }

        [Fact]
        public void ButtonNames_AreTheTablesOfTheTypes()
        {
            Assert.Same(X360ControlTables.xboxDefaultNames, OutContTypeInfo.ButtonNames(OutContType.X360));
            Assert.Same(X360ControlTables.ds4DefaultNames, OutContTypeInfo.ButtonNames(OutContType.DS4));
            Assert.Null(OutContTypeInfo.ButtonNames(OutContType.None));

            Assert.Equal("A Button", X360ControlTables.getX360ControlString(X360Controls.A, OutContType.X360));
            Assert.Equal("Cross", X360ControlTables.getX360ControlString(X360Controls.A, OutContType.DS4));
            Assert.Equal(string.Empty, X360ControlTables.getX360ControlString(X360Controls.A, OutContType.None));
        }

        [Fact]
        public void EveryChoiceOfTheProfile_HasAButtonNameTable_AndAnIpcName()
        {
            for (int i = 0; OutContTypeInfo.TryFromProfileChoiceIndex(i, out OutContType type); i++)
            {
                Assert.NotNull(OutContTypeInfo.ButtonNames(type));
                Assert.Equal(type, OutContTypeInfo.FromSaveKey(OutContTypeInfo.SaveKey(type)));
            }

            Assert.Equal(OutContTypeInfo.DefaultType, OutContTypeInfo.FromProfileChoiceIndex(OutContTypeInfo.ProfileChoiceIndex(OutContTypeInfo.DefaultType)));
        }

        [Fact]
        public void MacroInputNames_Xbox360_AreTheLegacyTable_ForEveryMacroValue()
        {
            foreach (KeyValuePair<int, string> pair in MacroParser.macroInputNames)
            {
                Assert.Equal(pair.Value, MacroParser.InputName(pair.Key, OutContType.X360));
                Assert.Equal(pair.Value, MacroParser.InputName(pair.Key));
                Assert.Equal(pair.Value, MacroParser.InputName(pair.Key, OutContType.None));
            }
        }

        [Fact]
        public void MacroInputNames_Ds4_UseTheDs4Notation_OnlyForTheControllerButtons()
        {
            var ds4 = new Dictionary<int, string>
            {
                [261] = "Cross", [262] = "Circle", [263] = "Square", [264] = "Triangle", [265] = "Options", [266] = "Share",
                [267] = "Dpad Up", [268] = "Dpad Down", [269] = "Dpad Left", [270] = "Dpad Right", [271] = "PS",
                [272] = "L1", [273] = "R1", [274] = "L2", [275] = "R2", [276] = "L3", [277] = "R3", [286] = "Touchpad Click",
            };

            foreach (KeyValuePair<int, string> pair in MacroParser.macroInputNames)
            {
                string expected = ds4.TryGetValue(pair.Key, out string name) ? name : pair.Value;
                Assert.Equal(expected, MacroParser.InputName(pair.Key, OutContType.DS4));
            }
        }

        private static (OutputDevice Device, IProfileSettingsService Settings, FakeDs4Target Target) CreateDs4(DS4TriggerOutputMode mode)
        {
            var backend = new FakeVirtualPadBackend();
            backend.Start();
            OutputDevice device = TestOutputCartridges.Allocate(OutContType.DS4, backend, new Version(1, 16, 0, 0));
            device.Connect();
            var settings = TestHost.GetService<IProfileSettingsService>();
            settings.OutputDS4TriggerMode[0] = mode;
            return (device, settings, backend.Ds4Targets[0]);
        }

        [Theory]
        [InlineData(DS4TriggerOutputMode.Default, true, 9, 8)]
        [InlineData(DS4TriggerOutputMode.Analog, false, 9, 8)]
        [InlineData(DS4TriggerOutputMode.Buttons, true, 0, 0)]
        public void Ds4TriggerMode_IsAppliedByTheDevice_BeforeTheReport(DS4TriggerOutputMode mode, bool buttons, byte l2, byte r2)
        {
            DS4TriggerOutputMode original = TestHost.GetService<IProfileSettingsService>().OutputDS4TriggerMode[0];
            try
            {
                var t = CreateDs4(mode);
                var state = new ControllerState { L2 = 9, R2 = 8 };
                t.Device.ConvertandSendReport(state, 0);

                Assert.Equal(buttons, state.L2Btn);
                Assert.Equal(buttons, state.R2Btn);
                Assert.Equal(l2, state.L2);   // 状態は呼び出し元と共有で、書き換えが見える（旧・入力レポートの処理と同じ）
                Assert.Equal(r2, state.R2);
                Assert.Equal(l2, t.Target.LeftTriggerValue);
                Assert.Equal(r2, t.Target.RightTriggerValue);
                Assert.Equal(buttons, (t.Target.Buttons & Ds4Buttons.TriggerLeft) != 0);
            }
            finally
            {
                TestHost.GetService<IProfileSettingsService>().OutputDS4TriggerMode[0] = original;
            }
        }

        [Fact]
        public void Xbox360_IgnoresTheDs4TriggerMode()
        {
            DS4TriggerOutputMode original = TestHost.GetService<IProfileSettingsService>().OutputDS4TriggerMode[0];
            try
            {
                TestHost.GetService<IProfileSettingsService>().OutputDS4TriggerMode[0] = DS4TriggerOutputMode.Buttons;
                var backend = new FakeVirtualPadBackend();
                backend.Start();
                OutputDevice device = TestOutputCartridges.Allocate(OutContType.X360, backend, new Version(1, 0));
                device.Connect();
                var state = new ControllerState { L2 = 200, R2 = 7 };
                device.ConvertandSendReport(state, 0);

                Assert.Equal(200, state.L2);
                Assert.False(state.L2Btn);
                Assert.Equal(200, backend.Xbox360Targets[0].LeftTriggerValue);
            }
            finally
            {
                TestHost.GetService<IProfileSettingsService>().OutputDS4TriggerMode[0] = original;
            }
        }

        [Fact]
        public void XInputSlotNumber_IsAnOperationOfTheDevice()
        {
            var backend = new FakeVirtualPadBackend();
            backend.Start();

            var withSlots = (Xbox360OutDevice)TestOutputCartridges.Allocate(OutContType.X360, backend, new Version(1, 17, 333, 0));
            Assert.True(withSlots.SupportsXInputSlotNumber);
            Assert.Equal(-1, withSlots.XInputSlotNumber);
            withSlots.XinputSlotNum = 2;
            Assert.Equal(2, withSlots.XInputSlotNumber);

            var withoutSlots = TestOutputCartridges.Allocate(OutContType.X360, backend, new Version(1, 0));
            Assert.False(withoutSlots.SupportsXInputSlotNumber);

            OutputDevice ds4 = TestOutputCartridges.Allocate(OutContType.DS4, backend, new Version(1, 16, 0, 0));
            Assert.False(ds4.SupportsXInputSlotNumber);
            Assert.Equal(-1, ds4.XInputSlotNumber);
        }
    }
}
