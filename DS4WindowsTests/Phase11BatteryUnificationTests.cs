using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase11 電池残量の一本化（案C）: 書き込みの入口は DS4Device 基底の UpdateBattery／UpdateCharging、
    /// 通知のイベントは基底の 1 組だけ、読み手は一覧のモデルを経由せずデバイスから直接読む。
    /// </summary>
    public class Phase11BatteryUnificationTests
    {
        private static string Code(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null) return null;

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return Regex.Replace(text, @"\s+", " ");
        }

        private static readonly string[] DeviceFiles =
        {
            "DS4Windows/DS4Library/InputDevices/DS3Device.cs",
            "DS4Windows/DS4Library/InputDevices/DualSenseDevice.cs",
            "DS4Windows/DS4Library/InputDevices/SwitchProDevice.cs",
            "DS4Windows/DS4Library/InputDevices/JoyConDevice.cs",
        };

        [Fact]
        public void TheBase_OwnsTheOnlyPairOfEvents_AndTheWriters()
        {
            string code = Code("DS4Windows/DS4Library/DS4Device.cs");
            if (code == null) return;

            Assert.Contains("public event EventHandler BatteryChanged;", code);
            Assert.Contains("public event EventHandler ChargingChanged;", code);
            Assert.DoesNotContain("virtual event EventHandler BatteryChanged", code);
            Assert.Contains("protected void UpdateBattery(int percent) { if (percent == battery) return; battery = percent; BatteryChanged?.Invoke(this, EventArgs.Empty); }", code);
            Assert.Contains("protected void UpdateCharging(bool isCharging) { if (isCharging == charging) return; charging = isCharging; ChargingChanged?.Invoke(this, EventArgs.Empty); }", code);
        }

        [Fact]
        public void TheDs4ReadLoop_WritesThroughTheUpdateMethods()
        {
            string code = Code("DS4Windows/DS4Library/DS4Device.cs");
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
                string code = Code(file);
                if (code == null) continue;

                Assert.DoesNotContain("override event EventHandler BatteryChanged", code);
                Assert.DoesNotContain("override event EventHandler ChargingChanged", code);
                Assert.DoesNotContain("BatteryChanged?.Invoke", code);
                Assert.DoesNotContain("ChargingChanged?.Invoke", code);
                Assert.Contains("UpdateBattery(tempBattery);", code);
                Assert.Contains("UpdateCharging(tempCharging);", code);
            }
        }

        [Fact]
        public void TheProfileChangedNotification_ReadsTheBatteryFromTheDevice_NotFromTheListRow()
        {
            string code = Code("DS4Windows/DS4Forms/MainWindow.xaml.cs");
            if (code == null) return;

            int start = code.IndexOf("private void OnProfileChanged(", System.StringComparison.Ordinal);
            Assert.True(start >= 0);
            string body = code.Substring(start, 1200);
            Assert.Contains("string battery = \"N/A\";", body);
            Assert.Contains("controlService.DS4Controllers[devIndex]", body);
            Assert.DoesNotContain("battery = $\"{item.Device.Battery}\"", body);
        }

        [Fact]
        public void TheList_ReplacesAStaleRow_OnHotplug_AndKeepsTheNewRowWhenTheOldDeviceIsRemoved()
        {
            string code = Code("DS4Windows/DS4Forms/ViewModels/ControllerListViewModel.cs");
            if (code == null) return;

            Assert.Contains("controllerDict.TryGetValue(index, out CompositeDeviceModel staleRow) && staleRow.Device != device", code);
            Assert.Contains("controllerDict.TryGetValue(found.DevIndex, out CompositeDeviceModel registered) && registered == found", code);
        }
    }
}
