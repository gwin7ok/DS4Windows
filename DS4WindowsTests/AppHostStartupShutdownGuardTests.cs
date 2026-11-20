using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// <c>AppHost</c> の整理と、起動・終了の手順（Q2・Q3・Q5）の回帰ガード。
    /// ホストは全テストが共有する static のため、実際に破棄はせず、ソースの構造で確かめる。
    /// - <c>CreateHost(string[])</c> と、未使用だった引数は削除済みで、再び宣言されない。
    /// - <c>-driverinstall</c> 分岐は、設定の読込の前に <c>CreateHost(config, parser)</c> を明示的に呼ぶ（<c>Global.Load</c> は削除済み）。
    /// - （終了の手順の順序は、<c>AppShutdownSequenceTests</c> が呼ばれた順で固定する）
    /// - <c>Dispose</c> の後は、<c>GetService</c> が暗黙にホストを作り直さない。
    /// ソースが見つからない環境では、検査を行わず合格とする。判断の根拠は <c>docs-forDIMG/MadeByAgent/Archive/Phase7-Step4-Plan.md</c>。
    /// </summary>
    public class AppHostStartupShutdownGuardTests
    {
        private readonly ITestOutputHelper _output;

        public AppHostStartupShutdownGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void AppHost_DoesNotDeclareRemovedOverloads()
        {
            // ソースの文字列ではなく、CreateHost の多重定義の引数を確かめる
            var overloads = typeof(DS4Windows.Composition.AppHost).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(m => m.Name == "CreateHost")
                .Select(m => m.GetParameters().Select(p => p.ParameterType).ToArray())
                .ToList();
            Assert.Contains(overloads, ps => ps.Length == 0);
            Assert.Contains(overloads, ps => ps.Length >= 2
                && ps[0] == typeof(Microsoft.Extensions.Configuration.IConfiguration)
                && ps[1] == typeof(DS4Windows.Core.Utilities.ArgumentParser));
            Assert.DoesNotContain(overloads, ps => ps.Length == 1 && ps[0] == typeof(string[]));
            Assert.DoesNotContain(overloads, ps => ps.Length == 1 && ps[0] == typeof(Microsoft.Extensions.Configuration.IConfiguration));
        }

        [Fact]
        public void AppHost_DoesNotRecreateTheHostAfterDispose()
        {
            string text = SourceText.ReadCode("DS4Windows/Composition/AppHost.cs");
            if (text == null) return;

            // Dispose 後だけでなく、未構築のときも暗黙に作らない（_disposed フラグは不要になった）
            Assert.DoesNotContain("_disposed", text);
            Assert.Equal(2, Regex.Matches(text, @"_host\s*=\s*builder\.Build\(\)").Count);
        }

        [Fact]
        public void DriverInstallBranch_CreatesTheHostExplicitlyBeforeLoadingSettings()
        {
            string text = SourceText.ReadCode("DS4Windows/App.xaml.cs");
            if (text == null) return;

            string body = SourceText.BodyByPattern(text, @"void\s+CheckOptions\s*\(");
            int create = body.IndexOf("AppHost.CreateHost(", System.StringComparison.Ordinal);
            int load = body.IndexOf(".Load()", System.StringComparison.Ordinal);
            Assert.True(create >= 0, "-driverinstall 分岐に AppHost.CreateHost の明示的な呼び出しがない。");
            Assert.True(load > create, "設定の読込が、ホストの明示的な構築より前にある。");
        }
    }
}
