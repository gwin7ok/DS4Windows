using System;
using Xunit;
using DS4Windows.Core.Abstractions;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step2-5 (PR-5): ControlService のコンストラクタ注入拡張（IAppearanceSettingsService、IProfileActionProvider）が
    /// Composition Root から正しく配線されていることと、必須引数が null で受け付けられないことを検証する。
    /// </summary>
    public class ControlServicePr5DiWiringTests
    {
        [Fact]
        public void AppHost_ResolvesControlService_WithPr5Dependencies()
        {
            DS4Windows.Composition.AppHost.CreateHost();

            ControlService controlService = null;
            var ex = Record.Exception(() => controlService = TestHost.GetService<ControlService>());

            Assert.Null(ex);
            Assert.NotNull(controlService);
        }

        [Fact]
        public void AppHost_ResolvesPr5Services()
        {
            DS4Windows.Composition.AppHost.CreateHost();

            Assert.NotNull(TestHost.GetService<IAppearanceSettingsService>());
            Assert.NotNull(TestHost.GetService<IProfileActionProvider>());
        }

        // Phase13-Step5b-5b-e: 該当の引数（ControlService のコンストラクター）は、使われなくなったため削除した。null 検査のテストも不要になった
    }
}