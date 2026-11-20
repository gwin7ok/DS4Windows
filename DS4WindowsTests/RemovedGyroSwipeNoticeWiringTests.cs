using System;
using System.IO;
using System.Resources;
using Xunit;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;

namespace DS4WindowsTests
{
    /// <summary>
    /// 編集画面の警告の配線のテスト。
    /// 警告の判定（<see cref="RemovedGyroSwipeNotice"/>）は W1 のテスト。ここでは、(1) 編集画面が、保持の入れ物を、サービス経由で得られること、
    /// (2) 警告の文言（英語・日本語）が、書式として使えること、(3) <c>ProfileEditor.Reload</c> が、プロファイルの読込の直後に、警告を呼ぶことを確かめる。
    /// ダイアログそのもの（<c>MessageBoxRemovedGyroSwipePrompt</c>）は画面なので、Step9 の実機の確認（U-48）。
    /// </summary>
    public class RemovedGyroSwipeNoticeWiringTests
    {
        private readonly ProfileSettingsService _profileSettings = TestRepositoryFactory.CreateProfileSettingsService();

        [Fact]
        public void TheService_ReturnsTheStoresHolder_ForEachSlot()
        {
            foreach (int slot in new[] { 0, 3, ControllerLimits.TestProfileIndex })
                Assert.Same(TestGlobals.Store.gyroSlots[slot].RemovedSwipe, _profileSettings.ForSlot(slot).Gyro.RemovedSwipe);
        }

        [Fact]
        public void TheWarningText_ExistsInEnglishAndJapanese_AndTakesTheProfileName()
        {
            var manager = new ResourceManager("DS4Windows.Properties.Resources", typeof(DS4Windows.UI.Services.MessageBoxRemovedGyroSwipePrompt).Assembly);
            string english = manager.GetString("RemovedGyroSwipeWarning", System.Globalization.CultureInfo.InvariantCulture);

            Assert.False(string.IsNullOrEmpty(english));
            Assert.Contains("MyProfile", string.Format(english, "MyProfile"));

            // 日本語は、サテライトのアセンブリの有無に依らないよう、リソースのソースで確かめる
            string japanese = SourceFileLocator.Find("DS4Windows/Properties/Resources.ja.resx");
            if (japanese == null) return;
            string text = File.ReadAllText(japanese);
            int at = text.IndexOf("name=\"RemovedGyroSwipeWarning\"", StringComparison.Ordinal);
            Assert.True(at >= 0, "日本語のリソースに RemovedGyroSwipeWarning がない");
            Assert.Contains("{0}", text.Substring(at, Math.Min(600, text.Length - at)));
        }

        [Fact]
        public void ThePrompt_IsAnIRemovedGyroSwipePrompt_AndTheEditorCallsTheNoticeRightAfterTheLoad()
        {
            Assert.True(typeof(IRemovedGyroSwipePrompt).IsAssignableFrom(typeof(DS4Windows.UI.Services.MessageBoxRemovedGyroSwipePrompt)));

            string editor = SourceFileLocator.Find("DS4Windows/UI/ProfileEditor.xaml.cs");
            if (editor == null) return;

            string text = File.ReadAllText(editor);
            int load = text.IndexOf("backend.LoadProfileForEditing(deviceNum, profile.Name);", StringComparison.Ordinal);
            int notice = text.IndexOf("profileSettingsVM.ApplyRemovedSettingNotices(profile.Name);", StringComparison.Ordinal);
            Assert.True(load >= 0, "プロファイルの読込の呼び出しが見つからない");
            Assert.True(notice > load, "警告の呼び出しが、読込の直後にない");
            Assert.True(notice - load < 800, "警告の呼び出しが、読込から離れている");
            // 警告の出し方は ViewModel の 1 か所に集まり、画面は直接呼ばない
            Assert.Empty(System.Text.RegularExpressions.Regex.Matches(text, "RemovedGyroSwipeNotice\\.Apply\\("));
            string viewModel = SourceFileLocator.Find("DS4Windows/UI/ViewModels/ProfileSettingsViewModel.cs");
            if (viewModel == null) return;
            Assert.Single(System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(viewModel), "RemovedGyroSwipeNotice\\.Apply\\("));
        }
    }
}
