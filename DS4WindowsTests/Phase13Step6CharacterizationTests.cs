using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.InputDevices;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase13-Step6 6-a: <c>Func&lt;ControlService&gt;</c> の撤廃（6-b 以降）の前に、置き換える側の挙動を固定する特性テスト。
    /// 入口は、置き換え前の経路（<c>ControlService</c> 経由）。6-b で入口を新しい経路に替えても、同じ結果になることを確かめる。
    /// 書けなかったもの: <c>setRumble</c> と <c>RumbleAutostopTime</c> の実デバイスへの反映は、<c>DS4Device</c> の内部状態（<c>testRumble</c>、タイマー）が
    /// 未初期化のテスト用デバイスでは動かないため、6-b では <c>IDeviceRumbleService</c> の差し替えで検査する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase13Step6CharacterizationTests
    {
        private const BindingFlags All = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const int Slot = 0;

        public Phase13Step6CharacterizationTests()
        {
            DS4WinWPF.AppHost.CreateHost();
            TestHost.GetService<ControlService>();
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo f = null;
            for (Type t = target.GetType(); t != null && f == null; t = t.BaseType)
                f = t.GetField(name, All | BindingFlags.DeclaredOnly);
            Assert.True(f != null, name + " が見つからない");
            f.SetValue(target, value);
        }

        /// <summary>スロットにデバイスを置き、終わったら元に戻す。</summary>
        private sealed class SlotScope : IDisposable
        {
            private readonly DS4Device _old;
            private readonly int _slot;

            public SlotScope(int slot, DS4Device device)
            {
                _slot = slot;
                var slots = TestHost.GetService<IDeviceSlotState>();
                _old = slots.DS4Controllers[slot];
                slots.DS4Controllers[slot] = device;
            }

            public void Dispose() => TestHost.GetService<IDeviceSlotState>().DS4Controllers[_slot] = _old;
        }

        private static TestDs4Device DeviceWithBattery(int percent, bool charging)
        {
            TestDs4Device d = TestDs4Device.Create();
            SetField(d, "battery", percent);
            SetField(d, "charging", charging);
            return d;
        }

        [Fact]
        public void BatteryText_NoDevice_IsNotAvailable()
        {
            var battery = TestHost.GetService<IDeviceBatteryText>();
            using (new SlotScope(Slot, null))
                Assert.Equal(DS4WinWPF.Properties.Resources.NA, battery.GetBatteryText(Slot));
        }

        [Fact]
        public void BatteryText_NotCharging_IsThePercent()
        {
            var battery = TestHost.GetService<IDeviceBatteryText>();
            using (new SlotScope(Slot, DeviceWithBattery(55, false)))
                Assert.Equal("55%", battery.GetBatteryText(Slot));
        }

        [Fact]
        public void BatteryText_Charging_HasAPlus_AndFullAtOneHundred()
        {
            var battery = TestHost.GetService<IDeviceBatteryText>();
            using (new SlotScope(Slot, DeviceWithBattery(55, true)))
                Assert.Equal("55%+", battery.GetBatteryText(Slot));
            using (new SlotScope(Slot, DeviceWithBattery(100, true)))
                Assert.Equal(DS4WinWPF.Properties.Resources.Full, battery.GetBatteryText(Slot));
        }

        [Fact]
        public void RumbleBoost_ADualSense_WithoutTheGenericRescale_IsOneHundred_OtherwiseTheConfiguredValue()
        {
            var settings = TestHost.GetService<IProfileSettingsService>();
            var rumbleSettings = ((ProfileSettingsService)settings).RumbleSettings;
            bool[] rescale = settings.UseGenericRumbleStrRescaleForDualSenses;
            Assert.NotNull(rescale);
            bool oldRescale = rescale[Slot];
            byte oldBoost = rumbleSettings[Slot].RumbleBoost;
            try
            {
                rumbleSettings[Slot].RumbleBoost = 37;   // GetRumbleBoost は RumbleSettings を先に読む
                rescale[Slot] = false;

                var dualSense = (DualSenseDevice)RuntimeHelpers.GetUninitializedObject(typeof(DualSenseDevice));
                using (new SlotScope(Slot, dualSense))
                    Assert.Equal(100, settings.GetRumbleBoost(Slot));   // DualSense で、汎用の再スケールなし → 100 固定

                rescale[Slot] = true;
                using (new SlotScope(Slot, dualSense))
                    Assert.Equal(37, settings.GetRumbleBoost(Slot));    // 再スケールあり → 設定値

                rescale[Slot] = false;
                using (new SlotScope(Slot, TestDs4Device.Create()))
                    Assert.Equal(37, settings.GetRumbleBoost(Slot));    // DualSense でなければ → 設定値
                using (new SlotScope(Slot, null))
                    Assert.Equal(37, settings.GetRumbleBoost(Slot));    // デバイスなし → 設定値
            }
            finally
            {
                rescale[Slot] = oldRescale;
                rumbleSettings[Slot].RumbleBoost = oldBoost;
            }
        }
    }
}
