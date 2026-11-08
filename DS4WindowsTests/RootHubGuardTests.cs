using System.Linq;
using System;
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
    public class RootHubGuardTests
    {
        private readonly ITestOutputHelper _output;

        public RootHubGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void App_HoldsTheControlServiceInAVolatileField()
        {
            // Phase14d-Step3: ソースの文字列ではなく、フィールドの型と volatile の修飾子を確かめる（取得の経路は ServiceLocatorGuardTests が守る）
            System.Reflection.FieldInfo field = typeof(DS4Windows.App).GetField("_controlService", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly);
            Assert.NotNull(field);
            Assert.Equal(typeof(DS4Windows.Processing.ControlService), field.FieldType);
            Assert.Contains(typeof(System.Runtime.CompilerServices.IsVolatile), field.GetRequiredCustomModifiers());
        }

        [Fact]
        public void ServiceRegistration_AlwaysCreatesTheControlService()
        {
            // Phase14d-Step3: 登録の文字列ではなく、コンテナが ControlService を 1 つ作って返すことを確かめる
            var first = TestHost.GetService<DS4Windows.Processing.ControlService>();
            Assert.NotNull(first);
            Assert.Same(first, TestHost.GetService<DS4Windows.Processing.ControlService>());
        }
    }
}
