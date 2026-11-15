using DS4Windows.Composition.Startup;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>設定を書けないときの案内。断られたときは、コピーせずに「編集できない」と知らせる。</summary>
    public class SettingsLocationFallbackTests
    {
        [Fact]
        public void WhenDeclined_ShowsTheCannotEditNotice_AndDoesNotCopy()
        {
            var dialogs = new FakeDialogService { ConfirmAnswer = false };
            var fallback = new SettingsLocationFallback(new DS4Windows.Core.Platform.PathService(), dialogs);

            Assert.False(fallback.OfferCopyToAppData());
            Assert.Single(dialogs.Confirmations);
            Assert.Single(dialogs.Messages);
        }

        [Fact]
        public void RequiresItsCollaborators()
        {
            Assert.Throws<System.ArgumentNullException>(() => new SettingsLocationFallback(null, new FakeDialogService()));
            Assert.Throws<System.ArgumentNullException>(() => new SettingsLocationFallback(new DS4Windows.Core.Platform.PathService(), null));
        }
    }
}
