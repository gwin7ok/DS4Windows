using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DS4Windows;
using DS4Windows.DI;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10e W4（承認済み。Phase7-Step10e-Plan.md §4.1）: 呼び出し 0 件だったメンバーの決着の回帰ガード。
    /// - 削除したメンバー（<c>ColorUtil.applyRatio(Color…)</c>／<c>HuetoRGB</c>、<c>IAppSettingsService.CheckWhen</c>、
    ///   <c>IProfileSettingsService.SATriggerCond</c>、<c>IAutoProfileService.ClearState</c>、<c>IProfileRepository.GetProfilePath</c>）が再び宣言されない。
    /// - 維持したメンバーのうち、未接続のもの（<c>SettingChanged</c>／<c>ActionsChanged</c>／<c>UseTempProfileArray</c>）に TODO(Phase8) が付いている。
    /// ソースが見つからない環境では、TODO の検査を省略する。
    /// </summary>
    public class Phase7Step10eW4OrphanMembersGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7Step10eW4OrphanMembersGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void RemovedMembers_AreNotDeclaredAgain()
        {
            Assert.Null(typeof(ColorUtil).GetMethod("HuetoRGB", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance));
            Assert.Empty(typeof(ColorUtil).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(m => m.Name == "applyRatio" && m.GetParameters().Length == 3 && m.GetParameters()[2].ParameterType == typeof(uint)));

            Assert.Null(typeof(IAppSettingsService).GetProperty("CheckWhen"));
            Assert.Null(typeof(IProfileSettingsService).GetProperty("SATriggerCond"));
            Assert.NotNull(typeof(IProfileSettingsService).GetMethod("GetSATriggerCond"));
            Assert.Null(typeof(IAutoProfileService).GetMethod("ClearState"));
            Assert.Null(typeof(IProfileRepository).GetMethod("GetProfilePath"));
            // パスの組み立ての正規の窓口と、実装クラスの内部用の GetProfilePath は残る
            Assert.NotNull(typeof(IPathService).GetMethod("GetProfilePath"));
            Assert.NotNull(typeof(ProfileRepository).GetMethod("GetProfilePath"));
        }

        [Fact]
        public void KeptButUnconnectedMembers_CarryAPhase8Todo()
        {
            string anchor = SourceFileLocator.Find("DS4Windows/DS4Control/BackingStore.cs");
            if (anchor == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(anchor), "..", ".."));
            AssertTodoBefore(root, "DS4Windows/DI/IAppSettingsService.cs", "event EventHandler<string> SettingChanged;");
            AssertTodoBefore(root, "DS4Windows/DI/ISpecialActionRepository.cs", "event EventHandler ActionsChanged;");
            AssertTodoBefore(root, "DS4Windows/DI/IProfileSettingsService.cs", "bool[] UseTempProfileArray { get; set; }");
        }

        private static void AssertTodoBefore(string root, string relative, string declaration)
        {
            string text = File.ReadAllText(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            int index = text.IndexOf(declaration, StringComparison.Ordinal);
            Assert.True(index >= 0, relative + " に宣言が見つからない: " + declaration);
            string before = text.Substring(Math.Max(0, index - 400), Math.Min(400, index));
            Assert.Matches(new Regex(@"TODO\(Phase8\)"), before);
        }
    }
}
