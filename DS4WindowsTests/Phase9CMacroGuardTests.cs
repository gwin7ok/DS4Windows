using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-C（Phase9-C-Plan.md）: マクロ再生の切り出しの守り。
    /// (1) <c>Mapping</c> から、移したメソッド・状態が無くなっていること（ソースの走査。コメントは除く）。
    /// (2) 新しい部品が DI の Singleton として登録され、静的な窓口（<c>Global</c>／<c>AppHost</c>）を参照しないこと。
    /// (3) 未使用だった <c>DeviceRuntimeState.MacroControl</c>／<c>MacroCount</c> が削除されていること（Q3）。
    /// </summary>
    public class Phase9CMacroGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase9CMacroGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(@"private void PlayMacro\(")]
        [InlineData(@"private void PlayMacroTask\(")]
        [InlineData(@"private void RunMacroOnce\(")]
        [InlineData(@"private bool PlayMacroCodeValue\(")]
        [InlineData(@"\bmacroControl\b")]
        [InlineData(@"\bmacroTaskQueue\b")]
        [InlineData(@"\b_deviceStateAccessor\b")]
        [InlineData(@"\bEmptyMacroRepeatIntervalMs\b")]
        public void Mapping_NoLongerContains_TheMovedMembers(string pattern)
        {
            string text = SourceText.ReadCode("DS4Windows/Processing/Mapping.cs");
            if (text == null) return;

            Assert.False(Regex.IsMatch(text, pattern), $"Mapping.cs に '{pattern}' が残っている（9-C で移した）");
        }

        [Fact]
        public void NewComponents_AreRegisteredAsSingletons()
        {
            Assert.IsType<MacroStepExecutor>(TestHost.GetService<IMacroStepExecutor>());
            Assert.IsType<MacroPlayback>(TestHost.GetService<IMacroPlayback>());
            Assert.Same(TestHost.GetService<IMacroPlayback>(), TestHost.GetService<IMacroPlayback>());
        }

        [Fact]
        public void DeviceRuntimeState_NoLongerHasTheUnusedMacroFields()
        {
            const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            string[] names = typeof(DeviceRuntimeState).GetFields(all).Select(f => f.Name).ToArray();
            Assert.DoesNotContain("MacroControl", names);
            Assert.DoesNotContain("MacroCount", names);
        }

        [Fact]
        public void MacroStepExecutor_ExecuteIsAtMostTwoHundredLines_AfterTheFolding()
        {
            // Phase9 完了条件: 巨大メソッドは 200 行以下。畳み込み（Q4）の前は約 160 行で、既に満たしている
            string path = SourceFileLocator.Find("DS4Windows/Processing/Services/MacroStepExecutor.cs");
            if (path == null) return;

            string text = File.ReadAllText(path);
            int start = text.IndexOf("public bool Execute(", System.StringComparison.Ordinal);
            Assert.True(start >= 0);
            int lines = text.Substring(start).Split('\n').Length;
            Assert.True(lines <= 200, $"Execute を含むファイルの残りが {lines} 行ある");
        }
    }
}
