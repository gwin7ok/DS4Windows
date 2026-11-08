using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase11 W6（S9-5 の確認で判明）: 自動プロファイルから元のプロファイルへ戻すときの通知は、バッテリー残量を「N/A」固定にせず、
    /// 接続中のコントローラーの値を使う。
    /// </summary>
    public class Phase11W6AutoRevertBatteryTests
    {
        [Fact]
        public void TheRevertNotice_UsesTheDeviceBattery_NotAFixedNA()
        {
            string path = SourceFileLocator.Find("DS4Windows/Processing/Services/AutoProfileService.cs");
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
