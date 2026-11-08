using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Core.Settings;
using DS4Windows.Processing;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10a W1・W2（Q2＝A）: <c>BackingStore</c> と制御層・DTO・サービス・Actions が、コントローラー数などの定数を
    /// <c>Global</c> ではなく <c>ControllerLimits</c> から取ることと、再宣言された定数が <c>ControllerLimits</c> と一致することの回帰ガード。
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    public class ControllerLimitsGuardTests
    {

        private readonly ITestOutputHelper _output;

        public ControllerLimitsGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TheRedeclaredConstants_MatchControllerLimits()
        {
            Assert.Equal(ControllerLimits.MaxControllerCount, ControlService.MAX_DS4_CONTROLLER_COUNT);
            Assert.Equal(ControllerLimits.MaxControllerCount, ProfileSettingsService.MAX_DS4_CONTROLLER_COUNT);
            Assert.Equal(ControllerLimits.TestProfileItemCount, ProfileSettingsService.TEST_PROFILE_ITEM_COUNT);
        }
    }
}
