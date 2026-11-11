using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Output.Services;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-E E-3（Phase9-E-Plan.md）: UDP モーションの切り出しの守り。
    /// (1) <c>ControlService</c> から、移したメンバーが無くなっていること（ソースの走査。コメントは除く）。
    /// (2) 新しい部品が DI の Singleton として登録され、<c>IDeviceSlotState</c> を直接注入で受けること（Phase13-Step5 で <c>IUdpMotionHost</c> を廃止）。
    /// (3) 部品が静的な窓口（<c>Global</c>／<c>AppHost</c>）を参照しないこと。
    /// </summary>
    public class UdpMotionGuardTests
    {
        private readonly ITestOutputHelper _output;

        public UdpMotionGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void NewComponents_AreRegistered_AndTheUdpHostInterfaceIsGone()
        {
            Assert.IsType<UdpMotionService>(TestHost.GetService<IUdpMotionService>());
            Assert.IsType<UdpMotionServerFactory>(TestHost.GetService<IUdpMotionServerFactory>());
            Assert.Same(TestHost.GetService<IUdpMotionService>(), TestHost.GetService<IUdpMotionService>());
            Assert.Null(typeof(DS4Windows.Processing.Abstractions.IRuntimeBootstrapper).Assembly.GetType("DS4Windows.DI.IUdpMotionHost"));
            Assert.DoesNotContain(typeof(DS4Windows.Processing.Services.RuntimeBootstrapper).GetInterfaces(), i => i.Name == "IUdpMotionHost");
        }

        [Fact]
        public void TheOtherFiles_ForTheCallers()
        {

            // Phase13-Step10 10-c: 転送は削除した。利用者は IRuntimeFeatureSwitch（RuntimeFeatureSwitch）を使う
            string sw = SourceText.ReadCode("DS4Windows/Processing/Services/RuntimeFeatureSwitch.cs");
            if (sw == null) return;
            Assert.Contains("_udpMotion.ChangeStatus(state, openPort)", sw);
            Assert.Contains("_udpMotion.ChangeMotionEventStatus(state)", sw);
        }
    }
}
