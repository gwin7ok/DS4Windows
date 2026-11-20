using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 書き込みの入口は InputDeviceCore 基底の UpdateBattery／UpdateCharging、
    /// 通知のイベントは基底の 1 組だけ、読み手は一覧のモデルを経由せずデバイスから直接読む。
    /// </summary>
    public class BatteryUnificationTests
    {
        private static readonly string[] DeviceFiles =
        {
            "DS4Windows/Input/Devices/DualSenseDevice.cs",
            "DS4Windows/Input/Devices/SwitchProDevice.cs",
        };

        [Fact]
        public void TheBase_OwnsTheOnlyPairOfEvents_AndTheWriters()
        {
            string code = SourceText.ReadNormalized("DS4Windows/Input/InputDeviceCore.cs");
            if (code == null) return;

            // イベントは基底の 1 組だけ。仮想ではなく、派生クラスは再宣言しない
            var core = typeof(DS4Windows.Input.InputDeviceCore);
            foreach (string name in new[] { "BatteryChanged", "ChargingChanged" })
            {
                var ev = core.GetEvent(name);
                Assert.NotNull(ev);
                Assert.False(ev.AddMethod.IsVirtual, name + " は仮想にしない");
                var redeclared = core.Assembly.GetTypes()
                    .Where(t => t != core && core.IsAssignableFrom(t) && t.GetEvent(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly) != null)
                    .Select(t => t.Name).ToArray();
                Assert.True(redeclared.Length == 0, name + " を再宣言している: " + string.Join(", ", redeclared));
            }

            Assert.Contains("protected void UpdateBattery(int percent) { if (percent == battery) return; battery = percent; BatteryChanged?.Invoke(this, EventArgs.Empty); }", code);
            Assert.Contains("protected void UpdateCharging(bool isCharging) { if (isCharging == charging) return; charging = isCharging; ChargingChanged?.Invoke(this, EventArgs.Empty); }", code);
        }

        [Fact]
        public void TheDs4ReadLoop_WritesThroughTheUpdateMethods()
        {
            string code = SourceText.ReadNormalized("DS4Windows/Input/Devices/DualShock4Device.cs");
            if (code == null) return;

            Assert.Contains("UpdateCharging(tempCharging);", code);
            Assert.Contains("UpdateBattery(tempBattery);", code);
            Assert.DoesNotContain("BatteryChanged?.Invoke(this, EventArgs.Empty); } cState", code);
        }

        [Fact]
        public void TheOtherDevices_DoNotRedeclareTheEvents_AndWriteThroughTheUpdateMethods()
        {
            foreach (string file in DeviceFiles)
            {
                string code = SourceText.ReadNormalized(file);
                if (code == null) continue;

                Assert.Contains("UpdateBattery(tempBattery);", code);
                Assert.Contains("UpdateCharging(tempCharging);", code);
            }
        }

        [Fact]
        public void TheProfileChangedNotification_ReadsTheBatteryFromTheDevice_NotFromTheListRow()
        {
            string code = SourceText.ReadNormalized("DS4Windows/UI/MainWindow.xaml.cs");
            if (code == null) return;

            int start = code.IndexOf("private void OnProfileChanged(", System.StringComparison.Ordinal);
            Assert.True(start >= 0);
            string body = code.Substring(start, 1200);
            Assert.Contains("_autoProfileService.BatteryText(devIndex)", body);
            
            Assert.DoesNotContain("battery = $\"{item.Device.Battery}\"", body);
        }

        [Fact]
        public void TheList_ReplacesAStaleRow_OnHotplug_AndKeepsTheNewRowWhenTheOldDeviceIsRemoved()
        {
            string code = SourceText.ReadNormalized("DS4Windows/UI/ViewModels/ControllerListViewModel.cs");
            if (code == null) return;

            Assert.Contains("controllerDict.TryGetValue(index, out CompositeDeviceModel? staleRow) && staleRow.Device != device", code);
            Assert.Contains("controllerDict.TryGetValue(found.DevIndex, out CompositeDeviceModel? registered) && registered == found", code);
        }
    }
}
