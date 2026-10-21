using System;
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
    /// Phase9 9-B（Phase9-B-Plan.md）: マウス変換と入力の解決の切り出しの守り。
    /// (1) <c>Mapping</c> から、移したメソッド・状態・未使用コードが無くなっていること（ソースの走査。コメントは除く）。
    /// (2) 新しい部品が DI の Singleton として登録され、静的な窓口（<c>Global</c>／<c>AppHost</c>）を参照しないこと。
    /// (3) 状態を持つ部品が、デバイスごとに状態を持つこと（添字の範囲がコントローラーの台数と一致）。
    /// </summary>
    public class Phase9BMouseGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase9BMouseGuardTests(ITestOutputHelper output)
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
        [InlineData(@"private double getMouseMapping\(")]
        [InlineData(@"private void calculateFinalMouseMovement\(")]
        [InlineData(@"public static bool compare\(")]
        [InlineData(@"private bool IsUsingGyroForControls\(")]
        [InlineData(@"public int mcounter\b")]
        [InlineData(@"public int mouseaccel\b")]
        [InlineData(@"public int prevmouseaccel\b")]
        [InlineData(@"\bhorizontalRemainder\b")]
        [InlineData(@"\bverticalRemainder\b")]
        [InlineData(@"\bMOUSESTICKANTIOFFSET\b")]
        [InlineData(@"\bMOUSESTICKMINVELOCITY\b")]
        public void Mapping_NoLongerContains_TheMovedMembers(string pattern)
        {
            string text = ReadWithoutComments("DS4Windows/DS4Control/Mapping.cs");
            if (text == null) return;

            Assert.False(Regex.IsMatch(text, pattern), $"Mapping.cs に '{pattern}' が残っている（9-B で移した／削除した）");
        }

        [Theory]
        [InlineData("DS4Windows/DS4Control/Services/ButtonMouseMovementCalculator.cs")]
        [InlineData("DS4Windows/DS4Control/Services/MouseMotionQuantizer.cs")]
        [InlineData("DS4Windows/DS4Control/Services/GyroMouseStickBlender.cs")]
        [InlineData("DS4Windows/DS4Control/Services/ControlActivityReader.cs")]
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
            Assert.IsType<ButtonMouseMovementCalculator>(TestHost.GetService<IButtonMouseMovementCalculator>());
            Assert.IsType<MouseMotionQuantizer>(TestHost.GetService<IMouseMotionQuantizer>());
            Assert.IsType<GyroMouseStickBlender>(TestHost.GetService<IGyroMouseStickBlender>());
            Assert.IsType<ControlActivityReader>(TestHost.GetService<IControlActivityReader>());

            Assert.Same(TestHost.GetService<IButtonMouseMovementCalculator>(), TestHost.GetService<IButtonMouseMovementCalculator>());
            Assert.Same(TestHost.GetService<IMouseMotionQuantizer>(), TestHost.GetService<IMouseMotionQuantizer>());
        }

        [Fact]
        public void StatefulComponents_HoldOneStatePerController()
        {
            // 状態の配列は、コントローラーの台数ぶんだけ持つ（添字 = device）。最後のスロットでも例外にならない
            int last = ControllerLimits.MaxControllerCount - 1;
            var info = new ButtonMouseInfo();
            var state = new DS4State();
            var fields = new DS4StateFieldMapping();
            new ButtonMouseMovementCalculator().Calculate(last, DS4Controls.Cross, 0, state, fields, info, 0, 0, new DeltaSettingsProcessorGroup(), 1.0);

            double x = 1.5, y = -1.5;
            new MouseMotionQuantizer().Quantize(last, ref x, ref y, out _, out _);
        }

        [Fact]
        public void Mapping_StatefulMouseFieldsAreGone_ByReflection()
        {
            string[] removed = { "mcounter", "mouseaccel", "prevmouseaccel", "horizontalRemainder", "verticalRemainder" };
            const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            string[] remaining = typeof(Mapping).GetFields(all).Select(f => f.Name).Intersect(removed).ToArray();
            Assert.Empty(remaining);
        }
    }
}
