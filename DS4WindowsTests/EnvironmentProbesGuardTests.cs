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
    /// Phase7-Step4 W1: ViGEmBus／HidHide／FakerInput／OS の調べ物とその状態を、<c>Global</c> から <c>EnvironmentService</c> へ移した結果の回帰ガード。
    /// 削除済みのメンバーが <c>Global.cs</c> に再び宣言されないこと、本体のどのファイルも <c>Global.&lt;名前&gt;</c> で参照しないこと、
    /// 移した先（<c>EnvironmentService</c>）が処理と状態を持つことを確かめる。判断の根拠は <c>Phase7-Step4-Plan.md</c> の Q6。
    /// ソースが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class Phase7Step4EnvironmentProbesGuardTests
    {

        private readonly ITestOutputHelper _output;

        public Phase7Step4EnvironmentProbesGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void EnvironmentService_OwnsTheProbesAndTheirState()
        {
            // Phase14d-Step3: ソースの文字列ではなく、宣言されたメンバーを確かめる。旧来の「静的初期化子から Global を呼ばない」検査は、Global 型の撤去で不要になった
            foreach (string member in new[]
            {
                "FindViGEmDeviceInfo", "CheckForSysDevice", "QueryFakerInputVersion", "PopulateFromViGEmBusInfo",
                "HidHideInstalledState", "FakerInputInstalledState", "FakerInputVersionState", "ViGEmBusVersionState",
                "IsWin8OrGreaterCore",
            })
            {
                Assert.True(typeof(DS4Windows.Core.Platform.EnvironmentService).GetMember(member, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly).Length > 0,
                    "EnvironmentService に " + member + " が見つからない。");
            }
        }
    }
}
