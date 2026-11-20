using DS4Windows.Core.Model;
using DS4Windows.UI.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 出力スロットの一覧で、右の設定欄（予約の変更）が出る条件を守る。
    /// 接続中の行に加え、恒久（Permanent）で予約された行は、仮想コントローラーが外れていても出る
    /// （出ないと、Dynamic へ戻せない）。予約のない空の行では出ない。
    /// </summary>
    public class CurrentOutDeviceViewModelSidePanelTests
    {
        [Fact]
        public void SidePanel_ShowsForPermanentlyReservedEmptySlot_ButNotForPlainEmptySlot()
        {
            var manager = TestRepositoryFactory.CreateOutputSlotManager();
            var service = TestRepositoryFactory.CreateOutputSlotService(manager);
            var vm = new CurrentOutDeviceViewModel(service);

            vm.SelectedIndex = 0;
            Assert.False(vm.SidePanelShown);

            service.SlotViews[0].ReserveStatus = OutputSlotReserveStatus.Permanent;
            Assert.True(vm.SidePanelShown);

            service.SlotViews[0].ReserveStatus = OutputSlotReserveStatus.Dynamic;
            Assert.False(vm.SidePanelShown);
        }

        /// <summary>
        /// ViewModel は INotifyPropertyChanged を持たず、WPF は「プロパティ名 + Changed」のイベントで更新を知る。
        /// イベント名がずれると、行を選んでも設定欄が出ない。
        /// </summary>
        [Theory]
        [InlineData("SidePanelShown")]
        [InlineData("PluginEnabled")]
        [InlineData("UnpluginEnabled")]
        public void TheBoundProperties_SupportChangeEvents(string name)
        {
            var manager = TestRepositoryFactory.CreateOutputSlotManager();
            var vm = new CurrentOutDeviceViewModel(TestRepositoryFactory.CreateOutputSlotService(manager));

            var property = System.ComponentModel.TypeDescriptor.GetProperties(vm)[name];

            Assert.True(property.SupportsChangeEvents);
        }
    }
}
