using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step4c W0（Phase10-Step4c-Plan.md §2 項目 9・§4 W0）: SA のトリガーとして選べる入力と、Controls の一覧に出る入力の<b>現行の差</b>を固定する特性化テスト。
    /// SA だけにあるのは Touch Started／Touch Ended、Controls だけにあるのは LS Outer／RS Outer。製品コードは変えない。
    /// W5（項目 9: 入力の集合を同じにする）で、この差が空になるよう、期待値を書き換える。
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
        public void TheSaOnlyInputs_AreTheTwoTouchEvents()
        {
            var only = SaInputs().Except(ControlsInputs()).OrderBy(c => c).ToArray();

            Assert.Equal(new[] { DS4Controls.TouchStarted, DS4Controls.TouchEnded }.OrderBy(c => c).ToArray(), only);
        }

        [Fact]
        public void TheControlsOnlyInputs_AreTheTwoStickOuters()
        {
            var only = ControlsInputs().Except(SaInputs()).OrderBy(c => c).ToArray();

            Assert.Equal(new[] { DS4Controls.LSOuter, DS4Controls.RSOuter }.OrderBy(c => c).ToArray(), only);
        }

        [Fact]
        public void TheControlsList_EndsWithTheFourVirtualButtons_InThatOrder()
        {
            var list = new MappingListViewModel(0, OutContType.X360, TestRepositoryFactory.CreateProfileSettingsService());
            int count = list.Mappings.Count;

            // W5 で Touch Started／Ended を足すとき、この 4 つの添字は変えない（末尾の後ろに足す）。コントローラーの入力による選択の添字も変えない
            Assert.Equal(count - 4, list.ControlIndexMap[DS4Controls.LSOuter]);
            Assert.Equal(count - 3, list.ControlIndexMap[DS4Controls.RSOuter]);
            Assert.Equal(count - 2, list.ControlIndexMap[DS4Controls.L2FullPull]);
            Assert.Equal(count - 1, list.ControlIndexMap[DS4Controls.R2FullPull]);
            Assert.Equal(49, count);   // W5 で 51（Touch Started／Ended を足す）
        }

        [Fact]
        public void TheSaCatalog_Has49Items()
        {
            Assert.Equal(49, new TriggerCatalog().Items.Count);   // W5 で LS Outer／RS Outer を足すと 51
        }
    }
}
