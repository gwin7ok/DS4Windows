using System;
using System.Linq;
using Xunit;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Settings;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step2-1 (PR-1): ControlService のコンストラクタ注入拡張（IAppSettingsService /
    /// IEnvironmentService / IPathService）が ServiceRegistration の Composition Root から
    /// 正しく配線されていること、および必須引数（Pure DI、決定D2）が null で受け付けられないことを検証する。
    /// ControlService の生成は <see cref="ControlServiceTestFactory"/> を用いる。
    /// </summary>
    public class ControlServiceDiWiringTests
    {
        [Fact]
        public void AppHost_ResolvesControlService_WithoutCircularDependencyOrMissingRegistration()
        {
            DS4Windows.Composition.AppHost.CreateHost();

            ControlService controlService = null;
            var ex = Record.Exception(() => controlService = TestHost.GetService<ControlService>());

            Assert.Null(ex);
            Assert.NotNull(controlService);
        }

        [Fact]
        public void AppHost_ResolvedControlService_ReceivesDeviceOptionsThroughAppSettingsService()
        {
            DS4Windows.Composition.AppHost.CreateHost();

            var controlService = TestHost.GetService<ControlService>();

            // コンストラクタで _appSettings.DeviceOptions（= Global.DeviceOptions）を受け取っていること。
            Assert.Same(TestGlobals.Store.deviceOptions, controlService.DeviceOptions);
        }

        [Fact]
        public void AppHost_ResolvesPr1Services_AsExpectedImplementations()
        {
            DS4Windows.Composition.AppHost.CreateHost();

            Assert.IsType<AppSettingsService>(TestHost.GetService<IAppSettingsService>());
            Assert.IsType<EnvironmentService>(TestHost.GetService<IEnvironmentService>());
            Assert.IsType<PathService>(TestHost.GetService<IPathService>());
        }

        [Fact]
        public void Constructor_NullAppSettings_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => ControlServiceTestFactory.Create("appSettings"));

            Assert.Equal("appSettings", ex.ParamName);
        }

        [Fact]
        public void Constructor_NullEnvironmentService_ThrowsArgumentNullException()
        {
            // Phase13-Step10 10-d: ControlService は IEnvironmentService を受けなくなった（スロットの上限は RuntimeFeatureSwitch が読む）。
            // 引数が無いことを固定し、必須の検査は RuntimeFeatureSwitch のコンストラクターで行う
            Assert.DoesNotContain(typeof(ControlService).GetConstructors().Single().GetParameters(), p => p.Name == "environmentService");
            var ex = Assert.Throws<ArgumentNullException>(() => new DS4Windows.Processing.Services.RuntimeFeatureSwitch(
                TestHost.GetService<DS4Windows.Processing.Abstractions.IOscService>(), TestHost.GetService<DS4Windows.Processing.Abstractions.IUdpMotionService>(),
                TestHost.GetService<DS4Windows.Processing.Abstractions.IHidHideService>(), TestHost.GetService<DS4Windows.Processing.Abstractions.ITouchpadToggleService>(),
                TestHost.GetService<DS4Windows.Processing.Abstractions.IDeviceSlotState>(), TestHost.GetService<DS4Windows.Processing.Abstractions.IActiveInputProbe>(),
                TestHost.GetService<DS4Windows.Processing.Abstractions.IDeviceRumbleService>(), null));

            Assert.Equal("environmentService", ex.ParamName);
        }

        [Fact]
        public void Constructor_NullPathService_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => ControlServiceTestFactory.Create("pathService"));

            Assert.Equal("pathService", ex.ParamName);
        }
    }
}