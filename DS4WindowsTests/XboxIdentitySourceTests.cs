using DS4Windows.Input.Xbox;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// USB の Xbox の擬似のシリアルの元の選び方。Razer など変換型の機器は、つなぎ直すたびにインスタンス ID の末尾の連番（01、02、03…）が進むため、
    /// 連番のときは親（USB ポートで決まる）を使い、個体のシリアルを持つ機器はこれまでどおりインスタンス ID を使う。
    /// </summary>
    public class XboxIdentitySourceTests
    {
        private const string Parent = @"USB\VID_1532&PID_0244&MI_01\9&933b870&0&0001";

        [Theory]
        [InlineData(@"USB\VID_045E&PID_028E&REV_0110\01")]
        [InlineData(@"USB\VID_045E&PID_028E&REV_0110\02")]
        [InlineData(@"USB\VID_045E&PID_028E&REV_0110\03")]
        [InlineData(@"USB\VID_045E&PID_028E&REV_0110\123")]
        public void ACounterTail_UsesTheParent(string instanceId)
        {
            Assert.Equal(Parent, XInputTransport.ChooseIdentitySource(instanceId, Parent, "path"));
        }

        [Theory]
        [InlineData(@"USB\VID_045E&PID_028E\A02014100201")]
        [InlineData(@"USB\VID_045E&PID_02FF\5&2A1B3C4D&0&1")]
        [InlineData(@"USB\VID_045E&PID_028E&REV_0110\1234")]
        public void ASerialTail_KeepsTheInstanceId(string instanceId)
        {
            Assert.Equal(instanceId, XInputTransport.ChooseIdentitySource(instanceId, Parent, "path"));
        }

        [Fact]
        public void WithoutAParent_ACounterTailKeepsTheInstanceId()
        {
            string id = @"USB\VID_045E&PID_028E&REV_0110\03";
            Assert.Equal(id, XInputTransport.ChooseIdentitySource(id, null, "path"));
            Assert.Equal(id, XInputTransport.ChooseIdentitySource(id, "", "path"));
        }

        [Fact]
        public void WithoutAnInstanceId_UsesThePath()
        {
            Assert.Equal("path", XInputTransport.ChooseIdentitySource("", Parent, "path"));
            Assert.Equal("path", XInputTransport.ChooseIdentitySource(null, Parent, "path"));
        }

        [Fact]
        public void TheParentGivesTheSameIdWhateverTheCounter()
        {
            string a = XInputTransport.ChooseIdentitySource(@"USB\VID_045E&PID_028E&REV_0110\03", Parent, "p");
            string b = XInputTransport.ChooseIdentitySource(@"USB\VID_045E&PID_028E&REV_0110\04", Parent, "p");
            Assert.Equal(a, b);
        }
    }
}
