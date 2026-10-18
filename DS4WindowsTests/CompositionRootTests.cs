using System;
using Xunit;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using DS4WinWPF;

namespace DS4WindowsTests
{
    public class CompositionRootTests
    {
        [Fact]
        public void AppHost_CreateHost_ShouldBuildHost()
        {
            var host = DS4WinWPF.AppHost.CreateHost();
            Assert.NotNull(host);
            Assert.NotNull(DS4WinWPF.AppHost.Host);
        }

        [Fact]
        public void AppHost_AllServices_ShouldResolveSuccessfully()
        {
            DS4WinWPF.AppHost.CreateHost();

            // 第4層 4-c 設定・永続化・環境・通知サービス
            Assert.NotNull(TestHost.GetService<IProfileSettingsService>());
            Assert.NotNull(TestHost.GetService<IProfileRepository>());
            Assert.NotNull(TestHost.GetService<ISpecialActionRepository>());
            Assert.NotNull(TestHost.GetService<IPathService>());
            Assert.NotNull(TestHost.GetService<IEnvironmentService>());
            Assert.NotNull(TestHost.GetService<INotificationService>());

            // 第1層 入力監視層・デバイス状態管理サービス
            Assert.NotNull(TestHost.GetService<IDeviceStateService>());
            Assert.NotNull(TestHost.GetService<IDs4DeviceRegistry>());

            // 第3層 信号出力層（仮想コントローラー出力スロット・プロセス起動）
            Assert.NotNull(TestHost.GetService<IOutputSlotService>());
            Assert.NotNull(TestHost.GetService<IElevatedProcessLauncher>());
            Assert.NotNull(TestHost.GetService<IProcessInspector>());
        }

        [Fact]
        public void AppHost_Singletons_ShouldReturnSameInstance()
        {
            DS4WinWPF.AppHost.CreateHost();

            var instance1 = TestHost.GetService<IProfileSettingsService>();
            var instance2 = TestHost.GetService<IProfileSettingsService>();
            Assert.Same(instance1, instance2);

            var path1 = TestHost.GetService<IPathService>();
            var path2 = TestHost.GetService<IPathService>();
            Assert.Same(path1, path2);
        }
    }
}
