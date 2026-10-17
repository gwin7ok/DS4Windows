using System;
using Xunit;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    public class NotificationServiceTests
    {
        [Fact]
        public void SendNotification_ShouldFireNotificationTriggered_WhenEnabled()
        {
            var origNotifications = TestGlobals.Store.notifications;
            try
            {
                var service = TestRepositoryFactory.CreateAppNotificationService();
                TestGlobals.Store.notifications = 1;

                NotificationEventArgs eventArgs = null;
                service.NotificationTriggered += (s, e) => eventArgs = e;

                service.SendNotification("TitleTest", "MessageTest", isToast: true);

                Assert.NotNull(eventArgs);
                Assert.Equal("TitleTest", eventArgs.Title);
                Assert.Equal("MessageTest", eventArgs.Message);
                Assert.False(eventArgs.Warning);
                Assert.False(eventArgs.Temporary);
                Assert.True(eventArgs.IsToast);
            }
            finally
            {
                TestGlobals.Store.notifications = origNotifications;
            }
        }

        [Fact]
        public void SendNotification_ShouldPropagateWarningFlag()
        {
            var origNotifications = TestGlobals.Store.notifications;
            try
            {
                var service = TestRepositoryFactory.CreateAppNotificationService();
                TestGlobals.Store.notifications = 1;

                NotificationEventArgs eventArgs = null;
                service.NotificationTriggered += (s, e) => eventArgs = e;

                service.SendNotification("TitleTest", "MessageTest", warning: true);

                Assert.NotNull(eventArgs);
                Assert.True(eventArgs.Warning);
                Assert.False(eventArgs.Temporary);
            }
            finally
            {
                TestGlobals.Store.notifications = origNotifications;
            }
        }

        [Fact]
        public void SendNotification_ShouldPropagateTemporaryFlag()
        {
            var origNotifications = TestGlobals.Store.notifications;
            try
            {
                var service = TestRepositoryFactory.CreateAppNotificationService();
                TestGlobals.Store.notifications = 1;

                NotificationEventArgs eventArgs = null;
                service.NotificationTriggered += (s, e) => eventArgs = e;

                service.SendNotification("TitleTest", "MessageTest", temporary: true);

                Assert.NotNull(eventArgs);
                Assert.False(eventArgs.Warning);
                Assert.True(eventArgs.Temporary);
            }
            finally
            {
                TestGlobals.Store.notifications = origNotifications;
            }
        }

        [Fact]
        public void SendNotification_ShouldNotFire_WhenDisabled()
        {
            var origNotifications = TestGlobals.Store.notifications;
            try
            {
                var service = TestRepositoryFactory.CreateAppNotificationService();
                bool eventFired = false;
                service.NotificationTriggered += (s, e) => eventFired = true;

                TestGlobals.Store.notifications = 0;
                // warning/temporary はデフォルト値(false)のまま。シグネチャ拡張後も本テストの意図（無効時は発火しない）に影響しない。
                service.SendNotification("TitleTest", "MessageTest");

                Assert.False(eventFired);
                Assert.Equal(0, TestGlobals.Store.notifications);
            }
            finally
            {
                TestGlobals.Store.notifications = origNotifications;
            }
        }
    }
}