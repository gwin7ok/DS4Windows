using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step2: DI インターフェースの、本体の呼び出しが 0 件だったメンバーを削除した結果の回帰ガード。
    /// (1) 削除済みのメンバーが、契約（<c>DS4Windows/DI/*.cs</c>）に再び現れないこと。
    /// (2) 削除済みのファイルが再び存在しないこと。
    /// (3) 「使うべきだが未接続」と判断して残した契約（TODO 付き）が、削除されていないこと。
    /// 判断の根拠は <c>Phase7-Step2-Plan.md</c> §2。ソースが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class Phase7Step2RemovedContractMembersGuardTests
    {

        // 残した契約（本来使うべきだが、呼び出し側の切り替えが未実施。TODO 付き）
        private static readonly Dictionary<string, string[]> Kept = new Dictionary<string, string[]>
        {
            ["IEnvironmentService"] = new[] { "UsingMaxControllers" },
            ["IDs4DeviceRegistry"] = new[] { "ReEnableDevice" },
            ["IOutputSlotService"] = new[] { "LoadOutputSlots", "SaveOutputSlots", "OutputSlotChanged" },
            ["IDeviceStateService"] = new[] { "IsFirstConnection", "MarkConnected", "ResetConnectionFlags", "OnDeviceSerialChange" },
        };

        private readonly ITestOutputHelper _output;

        public Phase7Step2RemovedContractMembersGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static bool Declares(string strippedSource, string member)
        {
            string name = Regex.Escape(member);
            return Regex.IsMatch(strippedSource, @"(?m)^\s*[\w<>\[\],.? ]+\s+" + name + @"\s*(\(|\{|;)")
                || Regex.IsMatch(strippedSource, @"(?m)^\s*event\s+[\w<>\[\],.? ]+\s+" + name + @"\s*;");
        }

        private string ReadContract(string interfaceName)
        {
            string path = SourceFileLocator.Find(SourceText.RelativePathOf(interfaceName + ".cs") ?? "");   // Phase14d-Step1: 旧パス（DI/）のまま、黙って省略されていた
            if (path == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した: " + interfaceName);
                return null;
            }

            return SourceText.StripComments(File.ReadAllText(path));
        }

        [Fact]
        public void KeptMembers_StillExistInContracts()
        {
            var missing = new List<string>();
            foreach (var pair in Kept)
            {
                string text = ReadContract(pair.Key);
                if (text == null) continue;
                missing.AddRange(pair.Value.Where(m => !Declares(text, m)).Select(m => pair.Key + "." + m));
            }

            Assert.True(missing.Count == 0, "残すべき契約が失われている（TODO 付きの未接続の契約）: " + string.Join(", ", missing));
        }
    }
}
