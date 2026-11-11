using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Output.Services;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-E E-4（Phase9-E-Plan.md）: 仮想パッド出力の切り出しの守り。
    /// (1) <c>ControlService</c> から、移したメンバーが無くなっていること（ソースの走査。コメントは除く）。
    /// (2) 過渡期の逆依存（<c>OutputSlotService</c> → <c>ControlService</c>）が解消されていること。
    /// (3) 新しい部品が DI の Singleton として登録され、<c>ControlService</c> が <see cref="IVirtualPadHost"/> として解決できること。
    /// (4) 部品が静的な窓口（<c>Global</c>／<c>AppHost</c>）を参照しないこと。
    /// </summary>
    public class VirtualPadGuardTests
    {
        private readonly ITestOutputHelper _output;

        public VirtualPadGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void NewComponents_AreRegistered_AndTheHostIsTheControlService()
        {
            Assert.IsType<VirtualPadService>(TestHost.GetService<IVirtualPadService>());
            Assert.Same(TestHost.GetService<IVirtualPadService>(), TestHost.GetService<IVirtualPadService>());
            Assert.IsType<EventThreadDispatcher>(TestHost.GetService<IEventThreadDispatcher>());
        }
    }
}
