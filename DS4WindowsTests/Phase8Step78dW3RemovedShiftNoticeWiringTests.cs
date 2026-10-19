using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.8d-W3（Phase8-Step7-8d-Plan.md §4.2）: 編集画面の警告の配線のテスト。
    /// 警告の判定（<see cref="RemovedShiftNotice"/>）は W1 のテスト。ここでは、(1) 編集画面が、設定の保持の入れ物を、サービス経由で得られること、
    /// (2) 警告の文言（英語・日本語）が、書式として使えること、(3) <c>ProfileEditor.Reload</c> が、プロファイルの読込の直後に、警告を呼ぶことを確かめる。
    /// ダイアログそのもの（<c>MessageBoxRemovedShiftPrompt</c>）は画面なので、Step9 の実機の確認（U-43）。
    /// </summary>
    public class Phase8Step78dW3RemovedShiftNoticeWiringTests
    {
        private readonly ProfileSettingsService _profileSettings = TestRepositoryFactory.CreateProfileSettingsService();

        [Fact]
        public void TheService_ReturnsTheStoresHolder_ForEachSlot()
        {
            foreach (int slot in new[] { 0, 3, ControllerLimits.TestProfileIndex })
                Assert.Same(TestGlobals.Store.removedShiftSettings[slot], _profileSettings.GetRemovedShiftSettings(slot));
        }

        [Fact]
        public void TheWarningText_ExistsInEnglishAndJapanese_AndTakesTheProfileName()
        {
            var manager = new ResourceManager("DS4WinWPF.Properties.Resources", typeof(DS4WinWPF.DS4Forms.MessageBoxRemovedShiftPrompt).Assembly);
            string english = manager.GetString("RemovedShiftWarning", System.Globalization.CultureInfo.InvariantCulture);

            Assert.False(string.IsNullOrEmpty(english));
            Assert.Contains("MyProfile", string.Format(english, "MyProfile"));

            // 日本語は、サテライトのアセンブリの有無に依らないよう、リソースのソースで確かめる
            string japanese = SourceFileLocator.Find("DS4Windows/Properties/Resources.ja.resx");
            if (japanese == null) return;
            string text = File.ReadAllText(japanese);
            int at = text.IndexOf("name=\"RemovedShiftWarning\"", StringComparison.Ordinal);
            Assert.True(at >= 0, "日本語のリソースに RemovedShiftWarning がない");
            Assert.Contains("{0}", text.Substring(at, Math.Min(600, text.Length - at)));
        }

        [Fact]
        public void ThePrompt_IsAnIRemovedShiftPrompt_AndTheEditorCallsTheNoticeRightAfterTheLoad()
        {
            Assert.True(typeof(IRemovedShiftPrompt).IsAssignableFrom(typeof(DS4WinWPF.DS4Forms.MessageBoxRemovedShiftPrompt)));

            string editor = SourceFileLocator.Find("DS4Windows/DS4Forms/ProfileEditor.xaml.cs");
            if (editor == null) return;

            string text = File.ReadAllText(editor);
            int load = text.IndexOf("profileRepository.LoadProfile(deviceNum, profile.Name);", StringComparison.Ordinal);
            int notice = text.IndexOf("RemovedShiftNotice.Apply(profileSettingsService.GetRemovedShiftSettings(deviceNum), profile.Name, removedShiftPrompt);", StringComparison.Ordinal);
            Assert.True(load >= 0, "プロファイルの読込の呼び出しが見つからない");
            Assert.True(notice > load, "警告の呼び出しが、読込の直後にない");
            Assert.True(notice - load < 400, "警告の呼び出しが、読込から離れている");
            Assert.Single(System.Text.RegularExpressions.Regex.Matches(text, "RemovedShiftNotice\\.Apply\\("));   // 警告の出し方が 1 か所に集まっている
        }
    }
}
