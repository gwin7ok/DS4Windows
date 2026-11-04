using System;
using Xunit;
using DS4Windows.Output.Services;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step2-3 (PR-3): KBM 出力の送出（IVirtualKBM）とハンドラのライフサイクル（IVirtualKBMLifecycle、決定D3）が
    /// Composition Root から ControlService へ正しく配線されていること、および必須引数の null 拒否を検証する。
    /// </summary>
    public class ControlServicePr3DiWiringTests
    {
        [Fact]
        public void AppHost_ResolvesVirtualKBMServices_AsExpectedImplementations()
        {
            DS4Windows.Composition.AppHost.CreateHost();

            Assert.IsType<OutputKBMHandlerAdapter>(TestHost.GetService<IVirtualKBM>());
            Assert.IsType<OutputKBMHandlerLifecycle>(TestHost.GetService<IVirtualKBMLifecycle>());
        }

        [Fact]
        public void AppHost_ResolvesControlService_WithPr3Dependencies()
        {
            DS4Windows.Composition.AppHost.CreateHost();

            ControlService controlService = null;
            var ex = Record.Exception(() => controlService = TestHost.GetService<ControlService>());

            Assert.Null(ex);
            Assert.NotNull(controlService);
        }

        // Phase13-Step5b-5b-e: 該当の引数（ControlService のコンストラクター）は、使われなくなったため削除した。null 検査のテストも不要になった
    }
}