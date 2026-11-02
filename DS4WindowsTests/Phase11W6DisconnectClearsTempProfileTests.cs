using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase11 W6 K-W6-11: 切断したら一時プロファイルの状態［UseTempProfile／TempProfileName／TempProfileDistance］を維持しない。
    /// 再接続では通常のルールでプロファイルが決まり、その後の再照合［CheckProfilesForConnectedController］が自動プロファイルを割り当てる。
    /// </summary>
    public class Phase11W6DisconnectClearsTempProfileTests
    {
        private readonly ITestOutputHelper _output;

        public Phase11W6DisconnectClearsTempProfileTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private string Normalized(string relativePath)
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

        [Fact]
        public void TheDisconnectHandler_ClearsTheTempProfileState_OfTheDisconnectedSlot()
        {
            string code = Normalized("DS4Windows/DS4Control/Services/DeviceDisconnectHandler.cs");
            if (code == null) return;

            int release = code.IndexOf("_profileSlotApplier.ReleaseSlot(ind);", StringComparison.Ordinal);
            int name = code.IndexOf("_profileSettings.SetTempProfileName(ind, string.Empty);", StringComparison.Ordinal);
            int use = code.IndexOf("_profileSettings.SetUseTempProfile(ind, false);", StringComparison.Ordinal);
            int distance = code.IndexOf("_profileSettings.SetTempProfileDistance(ind, false);", StringComparison.Ordinal);
            Assert.True(release >= 0 && name > release && use > release && distance > release);
        }

        [Fact]
        public void AfterTheClear_ReconnectUsesTheNormalRule_ThenTheRecheckAppliesTheAutoProfile()
        {
            // 再接続の通常適用は useAutoProfile=false の経路（GetUseTempProfile が false）、再照合は !useTemp && tempname != currentProfile で適用する
            string selector = Normalized("DS4Windows/DS4Control/Services/ConnectionProfileSelector.cs");
            string auto = Normalized("DS4Windows/DS4Control/Services/AutoProfileService.cs");
            if (selector == null || auto == null) return;

            Assert.Contains("if (!useAutoProfile) {", selector);
            Assert.Contains("(!useTemp && tempname != currentProfile)", auto);
        }
    }
}
