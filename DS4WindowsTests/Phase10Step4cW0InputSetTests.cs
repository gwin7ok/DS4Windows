using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step4c W0／W5（Phase10-Step4c-Plan.md §2 項目 9・§4 W0・W5）: SA のトリガーとして選べる入力と、Controls の一覧に出る入力の差を固定するテスト。
    /// W0 では、SA だけにあるのは Touch Started／Touch Ended、Controls だけにあるのは LS Outer／RS Outer だった。
    /// W5（ユーザー承認、2026-10-21）で、SA に LS Outer／RS Outer、Controls に Touch Started／Touch Ended を足し、<b>入力の集合を同じにした</b>（差は空）。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step4cW0InputSetTests
    {
        private static HashSet<DS4Controls> SaInputs()
        {
            var result = new HashSet<DS4Controls>();
            foreach (TriggerItem item in new TriggerCatalog().Items)
            {
                var action = new SpecialAction("n", item.Name, "Macro", "65");
                Assert.Single(action.trigger);
                result.Add(action.trigger[0]);
            }

            return result;
        }

        private static HashSet<DS4Controls> ControlsInputs()
        {
            var list = new MappingListViewModel(0, OutContType.X360, TestRepositoryFactory.CreateProfileSettingsService());
            return new HashSet<DS4Controls>(list.ControlMap.Keys);
        }

        [Fact]
        public void TheSaOnlyInputs_AreNone()
        {
            Assert.Empty(SaInputs().Except(ControlsInputs()).ToArray());
        }

        [Fact]
        public void TheControlsOnlyInputs_AreNone()
        {
            Assert.Empty(ControlsInputs().Except(SaInputs()).ToArray());
        }

        [Fact]
        public void TheControlsList_KeepsTheFourVirtualButtonsAtTheirIndices_AndEndsWithTheTwoTouchEvents()
        {
            var list = new MappingListViewModel(0, OutContType.X360, TestRepositoryFactory.CreateProfileSettingsService());
            int count = list.Mappings.Count;

            // W5 で Touch Started／Ended を、末尾の後ろに足した。既存の 4 つの仮想ボタンの添字（49 件のときの末尾の 4 つ）は変えていない
            Assert.Equal(51, count);
            Assert.Equal(45, list.ControlIndexMap[DS4Controls.LSOuter]);
            Assert.Equal(46, list.ControlIndexMap[DS4Controls.RSOuter]);
            Assert.Equal(47, list.ControlIndexMap[DS4Controls.L2FullPull]);
            Assert.Equal(48, list.ControlIndexMap[DS4Controls.R2FullPull]);
            Assert.Equal(49, list.ControlIndexMap[DS4Controls.TouchStarted]);
            Assert.Equal(50, list.ControlIndexMap[DS4Controls.TouchEnded]);
        }

        [Fact]
        public void TheSaCatalog_Has51Items_AndTheOutersAreNextToTheSticks()
        {
            var items = new TriggerCatalog().Items;

            Assert.Equal(51, items.Count);
            // Phase9 UI 変更: Outer は、スティックの 4 方向の下（InputControlCatalog の並び）。末尾は Touch Ended
            Assert.Equal("Touch Ended", items[50].Name);
            Assert.Equal("LS Outer", items[items.ToList().FindIndex(i => i.Name == "Left Stick Right") + 1].Name);
            Assert.Equal("RS Outer", items[items.ToList().FindIndex(i => i.Name == "Right Stick Right") + 1].Name);
        }
    }
}
