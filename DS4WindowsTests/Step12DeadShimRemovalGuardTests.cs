using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step12: 呼び出し元 0 件の旧静的シムを削除した結果の回帰ガード。
    /// (1) 削除済みのメンバーが <c>Global.cs</c> に再び現れないこと、
    /// (2) 削除禁止の防壁（定数、Pre-Host、ViGEm クライアント依存ほか）にあたるメンバーが残っていること、を固定する。
    /// 削除した名前は Wave ごとに <see cref="RemovedFromGlobal"/> へ追記する（名簿は Phase6-Step12-Plan.md §0A.8）。
    /// ソースが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class Step12DeadShimRemovalGuardTests
    {
        private static readonly string[] RemovedFromGlobal =
        {
            // Wave 1（孤立コード）
            "m_IdleTimeout", "exeFileName", "exeversionLong", "fullExeVersionLong", "localAppDataPpath", "linkedProfileCheck",
            "DS4Mapping", "DownloadLang", "CheckForDevice",
            "ControllerStatusChange", "ControllerStatusChanged", "BatteryStatusChange", "OnBatteryStatusChange",
            "ControllerRemoved", "OnControllerRemoved", "DeviceStatusChange", "OnDeviceStatusChanged",
            "getUseExclusiveMode", "getQuickCharge", "GetMinToTaskbar", "getFlashWhenLate", "getFlashWhenLateAt",
            "IsUsingUDPServerSmoothing", "NormalizeActionName",
        };

        private static readonly string[] RemovedFiles =
        {
            "DS4Windows/DS4Control/BatteryReportArgs.cs",
            "DS4Windows/DS4Control/ControllerRemovedArgs.cs",
            "DS4Windows/DS4Control/DeviceStatusChangeEventArgs.cs",
        };

        // 防壁 3（Pre-Host）、防壁 4（定数・純粋計算）、防壁 5（ViGEm クライアント依存）にあたる Global のメンバー
        private static readonly string[] ProtectedInGlobal =
        {
            "MAX_DS4_CONTROLLER_COUNT", "TEST_PROFILE_ITEM_COUNT", "TEST_PROFILE_INDEX", "ASSEMBLY_RESOURCE_PREFIX",
            "RESOURCES_PREFIX", "DS4_STICK_AXIS_MIDPOINT", "Clamp", "getTransitionedColor",
            "appdatapath", "multisavespots", "LogMaxArchiveFiles", "LogMinLevel", "FindConfigLocation", "SetCulture",
            "RefreshViGEmBusInfo", "IsRunningSupportedViGEmBus", "vigembusVersion", "vigemInstalled",
        };

        private readonly ITestOutputHelper _output;

        public Step12DeadShimRemovalGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static string StripComments(string text)
        {
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        private static bool Declares(string strippedSource, string name)
        {
            // 「static … 名前」の宣言（メソッド、プロパティ、フィールド、定数、イベント）を検出する
            return Regex.IsMatch(strippedSource,
                @"\bstatic\b[^\r\n(=;{]*?\b" + Regex.Escape(name) + @"\b\s*(\(|=|;|\{|=>|\r|\n)") ||
                Regex.IsMatch(strippedSource, @"\bconst\b[^\r\n(=;{]*?\b" + Regex.Escape(name) + @"\b\s*=");
        }

        [Fact]
        public void RemovedMembers_DoNotReappearInGlobal()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/Global.cs");
            if (path == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string text = StripComments(File.ReadAllText(path));
            List<string> reappeared = RemovedFromGlobal.Where(n => Declares(text, n)).ToList();
            Assert.True(reappeared.Count == 0,
                "削除済みの旧シムが Global.cs に再び宣言されている: " + string.Join(", ", reappeared));
        }

        [Fact]
        public void RemovedFiles_DoNotExist()
        {
            string anchor = SourceFileLocator.Find("DS4Windows/DS4Control/Global.cs");
            if (anchor == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(anchor), "..", ".."));
            List<string> existing = RemovedFiles.Where(f => File.Exists(Path.Combine(root, f))).ToList();
            Assert.True(existing.Count == 0, "削除済みのファイルが再び存在する: " + string.Join(", ", existing));
        }

        [Fact]
        public void ProtectedMembers_RemainInGlobal()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/Global.cs");
            if (path == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string text = StripComments(File.ReadAllText(path));
            List<string> missing = ProtectedInGlobal.Where(n => !Declares(text, n)).ToList();
            Assert.True(missing.Count == 0,
                "削除禁止の防壁にあたる Global のメンバーが見つからない: " + string.Join(", ", missing));
        }
    }
}
