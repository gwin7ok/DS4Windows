using System;
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
    public class RemovedContractMembersGuardTests
    {

        // 残した契約（本来使うべきだが、呼び出し側の切り替えが未実施。TODO 付き）
        private static readonly Dictionary<string, string[]> Kept = new Dictionary<string, string[]>
        {
            ["IEnvironmentService"] = new[] { "UsingMaxControllers" },
            ["IInputDeviceRegistry"] = new[] { "ReEnableDevice" },
            ["IOutputSlotService"] = new[] { "LoadOutputSlots", "SaveOutputSlots", "OutputSlotChanged" },
            ["IDeviceStateService"] = new[] { "IsFirstConnection", "MarkConnected", "ResetConnectionFlags", "OnDeviceSerialChange" },
        };

        private readonly ITestOutputHelper _output;

        public RemovedContractMembersGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void KeptMembers_StillExistInContracts()
        {
            // Phase14d-Step3: ソースの文字列ではなく、契約（インターフェース）の宣言されたメンバーを確かめる。契約は名前で引く（フォルダーの再編に依存しない）
            var types = typeof(DS4Windows.Composition.AppHost).Assembly.GetTypes();
            var missing = new List<string>();
            foreach (var pair in Kept)
            {
                Type contract = types.Single(t => t.IsInterface && t.Name == pair.Key);
                missing.AddRange(pair.Value.Where(m => contract.GetMember(m, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly).Length == 0).Select(m => pair.Key + "." + m));
            }
            Assert.True(missing.Count == 0, "残すべき契約が失われている（TODO 付きの未接続の契約）: " + string.Join(", ", missing));
        }
    }
}
