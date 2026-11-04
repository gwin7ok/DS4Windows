using System;
using Xunit;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step2-2 (PR-2): ControlService のコンストラクタ注入拡張（IOutputSlotService の遅延解決〔決定D1〕、
    /// IProfileRepository / IDeviceStateService / IProfileXmlStore / IProfileSlotApplier〔決定O1=B・O4=B-2〕）が
    /// Composition Root から正しく配線されていることと、必須引数が null で受け付けられないことを検証する。
    /// ControlService の生成は <see cref="ControlServiceTestFactory"/> を用いる。
    /// </summary>
    public class ControlServicePr2DiWiringTests
    {
        [Fact]
        public void AppHost_ResolvesProfileSlotApplier_AsDefaultImplementation()
        {
            DS4Windows.Composition.AppHost.CreateHost();

            Assert.IsType<ProfileSlotApplier>(TestHost.GetService<IProfileSlotApplier>());
        }

        [Fact]
        public void AppHost_ResolvesControlService_WithPr2Dependencies()
        {
            DS4Windows.Composition.AppHost.CreateHost();

            ControlService controlService = null;
            var ex = Record.Exception(() => controlService = TestHost.GetService<ControlService>());

            Assert.Null(ex);
            Assert.NotNull(controlService);
        }

        // Phase13-Step7 7-d: 旧 Constructor_DoesNotInvokeOutputSlotServiceFactory（Func<IOutputSlotService> を生成中に呼ばない検査）は、
        // Func をやめて直接注入にしたため不要になった（逆依存は Phase9 E-4 で解消済み。循環しないことは Phase13Step6ResolutionGraphProbeTests が検査する）。

        [Theory]
        [InlineData("outputSlotService")]
        [InlineData("profileRepository")]
        [InlineData("deviceStateService")]
        public void Constructor_NullRequiredParameter_ThrowsArgumentNullException(string parameterName)
        {
            var ex = Assert.Throws<ArgumentNullException>(() => ControlServiceTestFactory.Create(parameterName));

            Assert.Equal(parameterName, ex.ParamName);
        }
    }
}