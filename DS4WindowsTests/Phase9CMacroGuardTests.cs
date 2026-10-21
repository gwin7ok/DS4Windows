using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;
using Xunit.Abstractions;

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

        private string ReadWithoutComments(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null)
            {
                _output.WriteLine(relativePath + " が見つからないため検査を省略した。");
                return null;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
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
            string text = ReadWithoutComments("DS4Windows/DS4Control/Mapping.cs");
            if (text == null) return;

            Assert.False(Regex.IsMatch(text, pattern), $"Mapping.cs に '{pattern}' が残っている（9-C で移した）");
        }

        [Theory]
        [InlineData("DS4Windows/DS4Control/Services/MacroStepExecutor.cs")]
        [InlineData("DS4Windows/DS4Control/Services/MacroPlayback.cs")]
        public void NewComponents_DoNotReferenceStaticEntryPoints(string relativePath)
        {
            string text = ReadWithoutComments(relativePath);
            if (text == null) return;

            Assert.DoesNotContain("AppHost", text);
            Assert.DoesNotContain("ServiceProviderHolder", text);
            Assert.False(Regex.IsMatch(text, @"\bGlobal\."), relativePath + " が Global を参照している");
            Assert.False(Regex.IsMatch(text, @"\bProgram\.rootHub\b"), relativePath + " が Program.rootHub を参照している");
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
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/Services/MacroStepExecutor.cs");
            if (path == null) return;

            string text = File.ReadAllText(path);
            int start = text.IndexOf("public bool Execute(", System.StringComparison.Ordinal);
            Assert.True(start >= 0);
            int lines = text.Substring(start).Split('\n').Length;
            Assert.True(lines <= 200, $"Execute を含むファイルの残りが {lines} 行ある");
        }
    }
}
