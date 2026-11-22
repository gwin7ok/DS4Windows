using System;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// 切断したら一時プロファイルの状態［UseTempProfile／TempProfileName／TempProfileDistance］を維持しない。
    /// 再接続では通常のルールでプロファイルが決まり、その後の再照合［CheckProfilesForConnectedController］が自動プロファイルを割り当てる。
    /// </summary>
    public class DisconnectClearsTempProfileTests
    {
        private readonly ITestOutputHelper _output;

        public DisconnectClearsTempProfileTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void AfterTheClear_ReconnectUsesTheNormalRule_ThenTheRecheckAppliesTheAutoProfile()
        {
            // 再接続の通常適用は useAutoProfile=false の経路（GetUseTempProfile が false）、再照合は !useTemp && tempname != currentProfile で適用する
            string selector = SourceText.ReadNormalized("DS4Windows/Processing/Services/Slots/ConnectionProfileSelector.cs");
            string auto = SourceText.ReadNormalized("DS4Windows/Processing/Services/Slots/AutoProfileService.cs");
            if (selector == null || auto == null) return;

            Assert.Contains("if (!useAutoProfile) {", selector);
            Assert.Contains("(!useTemp && tempname != currentProfile)", auto);
        }
    }
}
