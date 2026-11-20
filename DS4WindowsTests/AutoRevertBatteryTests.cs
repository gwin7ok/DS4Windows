using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using DS4Windows.Processing.Services.Slots;

namespace DS4WindowsTests
{
    /// <summary>
    /// 自動プロファイルから元のプロファイルへ戻すときの通知は、バッテリー残量を「N/A」固定にせず、
    /// 接続中のコントローラーの値を使う。
    /// </summary>
    public class AutoRevertBatteryTests
    {
        [Fact]
        public void TheRevertNotice_UsesTheDeviceBattery_NotAFixedNA()
        {
            string path = SourceFileLocator.Find("DS4Windows/Processing/Services/Slots/AutoProfileService.cs");
            if (path == null) return;

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            text = Regex.Replace(text, @"\s+", " ");

            Assert.DoesNotContain("defaultProfile, \"N/A\")", text);
            Assert.Contains("defaultProfile, BatteryText(j))", text);
            Assert.Contains("device != null ? $\"{device.Battery}\" : \"N/A\"", text);
        }
    }
}
