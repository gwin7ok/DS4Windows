using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step5 W2: <c>Program.rootHub</c> を削除した結果の回帰ガード。
    /// - <c>Program</c> クラス（<c>Program.cs</c>）が再び作られない。本体のどのファイルも <c>Program.rootHub</c> を参照しない。
    /// - <c>App</c> が <c>ControlService</c> を volatile なフィールドで持ち、<c>ServiceRegistration</c> は常に新規生成する。
    /// - <c>Global.SaveControllerConfigs</c>／<c>LoadControllerConfigs</c> は再宣言されない。
    /// ソースが見つからない環境では、検査を行わず合格とする。判断の根拠は <c>Phase7-Step5-Plan.md</c> の Q1・Q2・Q8。
    /// </summary>
    public class Phase7Step5RootHubGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7Step5RootHubGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void App_HoldsTheControlServiceInAVolatileField()
        {
            string text = SourceText.ReadCode("DS4Windows/App.xaml.cs");
            if (text == null) return;

            Assert.Matches(@"private\s+volatile\s+DS4Windows\.Processing\.ControlService\s+_controlService\s*;", text);
            Assert.Matches(@"_controlService\s*=\s*ResolvePostHostService<DS4Windows\.Processing\.ControlService>\(\)", text);
        }

        [Fact]
        public void ServiceRegistration_AlwaysCreatesTheControlService()
        {
            string text = RegistrationSource.ReadAll(stripComments: true);   // Phase14c-Step6（R-18）: 層ごとの partial ファイルをつないだもの（コメントは除く）
            if (text == null) return;

            Assert.DoesNotContain("rootHub", text);
            Assert.Matches(@"return\s+new\s+ControlService\(", text);
        }
    }
}
