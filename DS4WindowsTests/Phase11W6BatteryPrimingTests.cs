using System;
using System.IO;
using System.Text.RegularExpressions;
using DS4Windows;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase11 W6 K-W6-3: 接続直後のログ・通知の電池残量が 0% だった。プロファイルの適用（ログ・通知を作る）が、最初の入力レポートの読み取りより前に
    /// 行われるため。読み取りのスレッドを始める前に、最初の入力レポートから電池残量だけを取り込む（<see cref="DS4Device.PrimeBatteryReading"/>）。
    /// HID の読み取りは単体で動かせないため、電池の式（読み取りのスレッドと共通）と、呼び出しの順序を検査する。
    /// </summary>
    public class Phase11W6BatteryPrimingTests
    {
        private readonly ITestOutputHelper _output;

        public Phase11W6BatteryPrimingTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        // 電池（BT）: 下位 4 ビットが 0〜8。8 で 100%
        [InlineData(0x08, 100, false)]
        [InlineData(0x04, 50, false)]
        [InlineData(0x00, 0, false)]
        [InlineData(0x0F, 100, false)]   // 8 を超えても 100% で止める
        // 充電中（0x10）: 下位 4 ビットが 0〜11。11 で 100%
        [InlineData(0x1B, 100, true)]
        [InlineData(0x15, 45, true)]
        public void DecodeBatteryPercent_MatchesTheInputLoopFormula(byte power, int expectedPercent, bool expectedCharging)
        {
            int percent = DS4Device.DecodeBatteryPercent(power, out bool charging);

            Assert.Equal(expectedPercent, percent);
            Assert.Equal(expectedCharging, charging);
        }

        [Fact]
        public void TheConnector_PrimesTheBattery_BeforeApplyingTheProfile_AndBeforeStartUpdate()
        {
            string code = ReadNormalizedSource("DS4Windows/DS4Control/Services/DeviceConnector.cs");
            if (code == null) return;

            int prime = code.IndexOf("device.PrimeBatteryReading();", StringComparison.Ordinal);
            int apply = code.IndexOf("_connectionProfileSelector.ApplyOnConnect(", StringComparison.Ordinal);
            int start = code.IndexOf("device.StartUpdate();", StringComparison.Ordinal);
            Assert.True(prime >= 0 && apply > prime && start > apply, "PrimeBatteryReading → ApplyOnConnect → StartUpdate の順になっていない");
        }

        [Fact]
        public void PrimeBatteryReading_DoesNotTouchTheChargingState_OrRaiseEvents()
        {
            string code = ReadNormalizedSource("DS4Windows/DS4Library/DS4Device.cs");
            if (code == null) return;

            int start = code.IndexOf("public void PrimeBatteryReading()", StringComparison.Ordinal);
            int end = code.IndexOf("public virtual void StartUpdate()", StringComparison.Ordinal);
            Assert.True(start >= 0 && end > start);
            string body = code.Substring(start, end - start);

            Assert.DoesNotContain("charging =", body);
            Assert.DoesNotContain("BatteryChanged", body);
            Assert.DoesNotContain("ChargingChanged", body);
        }

        private string ReadNormalizedSource(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null)
            {
                _output.WriteLine(relativePath + " が見つからないため検査を省略した。");
                return null;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return Regex.Replace(text, @"\s+", " ");
        }
    }
}
