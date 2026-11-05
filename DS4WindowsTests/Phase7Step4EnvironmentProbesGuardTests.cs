using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step4 W1: ViGEmBus／HidHide／FakerInput／OS の調べ物とその状態を、<c>Global</c> から <c>EnvironmentService</c> へ移した結果の回帰ガード。
    /// 削除済みのメンバーが <c>Global.cs</c> に再び宣言されないこと、本体のどのファイルも <c>Global.&lt;名前&gt;</c> で参照しないこと、
    /// 移した先（<c>EnvironmentService</c>）が処理と状態を持つことを確かめる。判断の根拠は <c>Phase7-Step4-Plan.md</c> の Q6。
    /// ソースが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class Phase7Step4EnvironmentProbesGuardTests
    {
        // Wave ごとに追記する（W1: 環境のプローブ。W3 以降: 保存場所・実行ファイルの情報）
        private static readonly string[] Removed =
        {
            "IsWin8OrGreater", "IsWin10OrGreater",
            "vigemInstalled", "vigembusVersion", "vigemBusVersionInfo", "minSupportedViGEmBusVersionInfo",
            "hidHideInstalled", "fakerInputInstalled", "fakerInputVersion",
            "BLANK_VIGEMBUS_VERSION", "MIN_SUPPORTED_VIGEMBUS_VERSION", "MIN_TOUCHPAD_PASSTHRU_VIGEMBUS_VERSION",
            "BLANK_FAKERINPUT_VERSION", "VIGEMBUS_GUID",
            "CheckHidHideAffectedStatus", "IsHidHideInstalled", "IsFakerInputInstalled", "IsViGEmBusInstalled",
            "IsRunningSupportedViGEmBus", "IsUsingMinViGEm117333", "RefreshViGEmBusInfo", "RefreshHidHideInfo",
            "RefreshFakerInputInfo", "FindViGEmDeviceInfo", "CheckForSysDevice", "PopulateFromViGEmBusInfo",
            "FakerInputVersion",
        };

        private readonly ITestOutputHelper _output;

        public Phase7Step4EnvironmentProbesGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void NoSourceFile_ReferencesRemovedProbesThroughGlobal()
        {
            string anchor = SourceFileLocator.Find("DS4Windows/Core/Settings/BackingStore.cs"); // Phase7-Step10d W5: Global.cs は削除したため、同じフォルダーの BackingStore.cs を起点にする
            if (anchor == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string root = SourceFileLocator.RootFromAnchor(anchor);
            string sourceDir = Path.Combine(root, "DS4Windows");
            var offenders = new List<string>();
            foreach (string file in Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories))
            {
                string normalized = file.Replace('\\', '/');
                if (normalized.Contains("/obj/") || normalized.Contains("/bin/")) continue;

                string text = SourceText.StripComments(File.ReadAllText(file));
                foreach (string name in Removed)
                {
                    if (Regex.IsMatch(text, @"\bGlobal\." + Regex.Escape(name) + @"\b"))
                        offenders.Add(Path.GetRelativePath(sourceDir, file) + ": Global." + name);
                }
            }

            Assert.True(offenders.Count == 0, "移管済みの調べ物を Global 経由で参照している: " + string.Join(", ", offenders));
        }

        [Fact]
        public void EnvironmentService_OwnsTheProbesAndTheirState()
        {
            string text = SourceText.ReadCode("DS4Windows/Core/Platform/EnvironmentService.cs");
            if (text == null) return;

            foreach (string member in new[]
            {
                "FindViGEmDeviceInfo", "CheckForSysDevice", "QueryFakerInputVersion", "PopulateFromViGEmBusInfo",
                "HidHideInstalledState", "FakerInputInstalledState", "FakerInputVersionState", "ViGEmBusVersionState",
                "IsWin8OrGreaterCore",
            })
            {
                Assert.True(Regex.IsMatch(text, @"\b" + Regex.Escape(member) + @"\b"), "EnvironmentService に " + member + " が見つからない。");
            }

            // 静的初期化の循環を作らない: 静的フィールドの初期化子から Global のメソッド・フィールドを呼ばない
            // （const の MAX_DS4_CONTROLLER_COUNT／OLD_XINPUT_CONTROLLER_COUNT だけを許可。EnvironmentServiceStaticInitGuardTests も参照）
            foreach (Match m in Regex.Matches(text, @"static\s+(?:readonly\s+)?[\w<>?]+\s+\w+\s*=\s*[^;]*Global\.(\w+)"))
            {
                string member = m.Groups[1].Value;
                Assert.True(member == "MAX_DS4_CONTROLLER_COUNT" || member == "OLD_XINPUT_CONTROLLER_COUNT",
                    "静的フィールドの初期化子が Global." + member + " を参照している（静的初期化の循環の原因になる）。");
            }
        }
    }
}
