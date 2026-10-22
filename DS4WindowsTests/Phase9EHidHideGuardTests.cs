using System.IO;
using System.Text.RegularExpressions;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-E E-2（Phase9-E-Plan.md）: HidHide の切り出しの守り。
    /// (1) <c>ControlService</c> から、移したメンバーが無くなっていること（ソースの走査。コメントは除く）。
    /// (2) 新しい部品が DI の Singleton として登録され、静的な窓口（<c>Global</c>／<c>AppHost</c>）を参照しないこと。
    /// (3) ドライバーの窓口（<c>HidHideAPIDevice</c>）は、<c>HidHideApiAdapter</c> の中に閉じていること。
    /// </summary>
    public class Phase9EHidHideGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase9EHidHideGuardTests(ITestOutputHelper output)
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
        [InlineData(@"\bhidDeviceHiding\w+")]
        [InlineData(@"\bCheckAffected\b")]
        [InlineData(@"\bChangeExclusiveStatus\b")]
        [InlineData(@"\bUpdateHidHideAttributes\b")]
        [InlineData(@"\bHidHideAPIDevice\b")]
        public void ControlService_NoLongerContains_TheMovedMembers(string pattern)
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/ControlService.cs");
            if (text == null) return;

            Assert.False(Regex.IsMatch(text, pattern), $"ControlService.cs に '{pattern}' が残っている（9-E E-2 で HidHideService へ移した）");
        }

        [Fact]
        public void ControlService_KeepsTheThinDelegates_ForTheCallers()
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/ControlService.cs");
            if (text == null) return;

            Assert.Contains("public void CheckHidHidePresence(", text);
            Assert.Contains("_hidHideService.CheckPresence(ExePath, ExeName, AddExe);", text);
            Assert.Contains("public void UpdateHidHiddenAttributes()", text);
            Assert.Contains("_hidHideService.UpdateAttributes();", text);
        }

        [Theory]
        [InlineData("DS4Windows/DS4Control/Services/HidHideService.cs")]
        [InlineData("DS4Windows/DS4Control/Services/HidHideApiFactory.cs")]
        [InlineData("DS4Windows/DS4Control/Services/HidHideApiAdapter.cs")]
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
        public void HidHideService_DoesNotTouchTheDriverDirectly()
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/Services/HidHideService.cs");
            if (text == null) return;

            Assert.DoesNotContain("HidHideAPIDevice", text);
            Assert.Contains("_apiFactory.Open()", text);
        }

        [Fact]
        public void NewComponents_AreRegisteredAsSingletons()
        {
            Assert.IsType<HidHideService>(TestHost.GetService<IHidHideService>());
            Assert.IsType<HidHideApiFactory>(TestHost.GetService<IHidHideApiFactory>());
            Assert.Same(TestHost.GetService<IHidHideService>(), TestHost.GetService<IHidHideService>());
        }
    }
}
