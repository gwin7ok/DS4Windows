using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase14b-Step3（R-03）: <see cref="AppLogger.NotificationService"/> が未設定のときは、何もしない <see cref="NullNotificationService"/> を使い、
    /// 2 つ目の <c>BackingStore</c> を作らないこと（旧: <c>new AppNotificationService(new BackingStore(new EnvironmentService()))</c>）。
    /// 設定後の委譲は <see cref="LogToTrayNotificationBridgeTests"/> が検査する。
    /// </summary>
    public class AppLoggerNotificationFallbackTests
    {
        [Fact]
        public void WhenNotSet_TheFallbackIsTheNullService_AndLogToTrayDoesNotThrow()
        {
            INotificationService original = AppLogger.NotificationService;
            try
            {
                AppLogger.NotificationService = null;

                Assert.IsType<NullNotificationService>(AppLogger.NotificationService);
                Assert.Same(AppLogger.NotificationService, AppLogger.NotificationService);   // 一度作ったら使い回す

                AppLogger.LogToTray("not shown", warning: true);
                AppLogger.LogToTray("not shown either");
            }
            finally
            {
                AppLogger.NotificationService = original;
            }
        }

        [Fact]
        public void TheNullService_NeverRaisesTheEvent()
        {
            var service = new NullNotificationService();
            int raised = 0;
            EventHandler<NotificationEventArgs> handler = (s, e) => raised++;

            service.NotificationTriggered += handler;
            service.SendNotification("title", "message", warning: true, temporary: true, isToast: true);
            service.SendNotification("title", "message");
            service.NotificationTriggered -= handler;

            Assert.Equal(0, raised);
        }

        [Theory]
        [InlineData("DS4Windows.Core.Settings.BackingStore")]
        [InlineData("DS4Windows.Core.Platform.EnvironmentService")]
        [InlineData("DS4Windows.UI.Services.AppNotificationService")]
        public void AppLogger_NoLongerCreatesTheSettingsOrTheUiNotificationService(string target)
        {
            Assert.DoesNotContain(LayerDependencyScanner.Edges, e => e.Source == "DS4Windows.Core.Logging.AppLogger" && e.Target == target);
        }
    }
}
