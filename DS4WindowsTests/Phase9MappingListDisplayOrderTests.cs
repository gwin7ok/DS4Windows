using System.Linq;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>Phase9 UI 変更: Controls タブの一覧の表示順（LS／RS Outer は LS／RS の下、L2／R2 Full Pull は L2／R2 の下）。Mappings の順序と添字は変えない。</summary>
    public class Phase9MappingListDisplayOrderTests
    {
        private static MappingListViewModel Create() => new MappingListViewModel(0, OutContType.DS4, TestRepositoryFactory.CreateProfileSettingsService());

        [Fact]
        public void TheDisplayList_PlacesTheVirtualControlsNextToTheirGroups()
        {
            var vm = Create();
            var order = vm.DisplayMappings.Select(m => m.Control).ToList();

            Assert.Equal(DS4Controls.L2FullPull, order[order.IndexOf(DS4Controls.L2) + 1]);
            Assert.Equal(DS4Controls.R2FullPull, order[order.IndexOf(DS4Controls.R2) + 1]);
            Assert.Equal(DS4Controls.LSOuter, order[order.IndexOf(DS4Controls.LXPos) + 1]);
            Assert.Equal(DS4Controls.RSOuter, order[order.IndexOf(DS4Controls.RXPos) + 1]);
        }

        [Fact]
        public void TheDisplayList_HasTheSameItemsAsTheMappings_AndTheMappingsKeepTheirIndexes()
        {
            var vm = Create();

            Assert.Equal(vm.Mappings.Count, vm.DisplayMappings.Count);
            Assert.Equal(vm.Mappings.OrderBy(m => (int)m.Control), vm.DisplayMappings.OrderBy(m => (int)m.Control));
            Assert.Equal(47, vm.ControlIndexMap[DS4Controls.L2FullPull]);   // 添字は変えない（入力による選択、ホバーの表が使う）
        }

        [Fact]
        public void TheSelection_FollowsTheMappingsIndex_NotTheDisplayPosition()
        {
            var vm = Create();
            MappedControl l2Full = vm.ControlMap[DS4Controls.L2FullPull];

            vm.SelectedMapping = l2Full;

            Assert.Equal(vm.ControlIndexMap[DS4Controls.L2FullPull], vm.SelectedIndex);
            Assert.Same(l2Full, vm.SelectedMapping);
        }
    }
}
