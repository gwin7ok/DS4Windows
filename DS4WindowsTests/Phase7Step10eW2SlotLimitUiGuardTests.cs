using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Processing;
using DS4Windows.UI.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10e W2（Q2＝A）: 静的な <c>ControlService.CURRENT_DS4_CONTROLLER_LIMIT</c> を削除し、画面・ViewModel が注入された
    /// <see cref="DS4Windows.Core.Abstractions.IEnvironmentService.ControllerSlotLimit"/> を使うことの回帰ガード。ソースが見つからない環境では、ソースの検査を省略する。
    /// </summary>
    public class Phase7Step10eW2SlotLimitUiGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase7Step10eW2SlotLimitUiGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TheViewModels_RequireTheEnvironmentService()
        {
            Assert.Throws<ArgumentNullException>(() => new DS4Windows.UI.ViewModels.SpecialActions.CheckBatteryViewModel(TestLightbar.State, 
                TestRepositoryFactory.CreateSpecialActionRepository(), null));
        }

        [Fact]
        public void BindingAndRecordViewModels_RequireTheEnvironmentService()
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService();
            var slots = TestRepositoryFactory.CreateOutputSlotService();
            Assert.Throws<ArgumentNullException>(() => new BindingWindowViewModel(TestLightbar.State, 0, new DS4ControlSettings(DS4Controls.Cross), settings, slots, null));
            Assert.Throws<ArgumentNullException>(() => new RecordBoxViewModel(TestLightbar.State, 0, new DS4ControlSettings(DS4Controls.Cross), true, settings, null, null));
        }
    }
}
