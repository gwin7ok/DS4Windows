using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step4c W5（Phase10-Step4c-Plan.md §2 項目 9・§4 W5、ユーザー承認 2026-10-21）: 追加した入力が使えることを確かめる。
    /// Controls に足した Touch Started／Touch Ended は、束縛が作られ、プロファイル XML に保存・読み込みされる。
    /// SA に足した LS Outer／RS Outer は、トリガーとして保存した名前から読み戻せる。一覧の項目数と差は <c>Phase10Step4cW0InputSetTests</c> が固定する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step4cW5InputSetTests
    {
        private const int Device = MappingCharacterizationScope.Device;
        private const int Slot = PersistenceHarness.Slot;

        [Theory]
        [InlineData(DS4Controls.TouchStarted)]
        [InlineData(DS4Controls.TouchEnded)]
        public void AKeyOnATouchEvent_MakesABinding(DS4Controls control)
        {
            using var t = new ControlsKeyTestScope();
            t.MapKey(control, 65);

            IReadOnlyList<IActionBinding> bindings = TestHost.GetService<IControlBindingBuilder>().Build(Device);

            IActionBinding b = bindings.SingleOrDefault(x => x.Source.name == $"Synthetic_Key_{Device}_{(int)control}");
            Assert.NotNull(b);
            Assert.Equal(new[] { control }, b.Input.Triggers.ToArray());
        }

        [Theory]
        [InlineData(DS4Controls.TouchStarted)]
        [InlineData(DS4Controls.TouchEnded)]
        public void ATouchEventAssignment_IsSavedAndLoadedInTheProfile(DS4Controls control)
        {
            using var ctx = new PersistenceGoldenContext();
            string baseXml = PersistenceHarness.SaveProfile(ctx, PersistenceHarness.NewStore(), "Step4cW5In" + control);
            string xml = baseXml.Replace("<Control />", $"<Control><Key><{control}>65</{control}></Key></Control>");

            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, xml, "Step4cW5Load" + control);

            DS4ControlSettings dcs = loaded.GetDS4CSetting(Slot, control);
            Assert.Equal(DS4ControlSettings.ActionType.Key, dcs.actionType);
            Assert.Equal(65, dcs.action.actionKey);

            string resaved = PersistenceHarness.SaveProfile(ctx, loaded, "Step4cW5Resave" + control);
            Assert.Contains($"<{control}>65</{control}>", resaved);
        }

        [Theory]
        [InlineData("LS Outer", DS4Controls.LSOuter)]
        [InlineData("RS Outer", DS4Controls.RSOuter)]
        public void TheOuterTriggerNames_AreReadBackAsTheStickOuters(string name, DS4Controls expected)
        {
            var action = new SpecialAction("n", name, "Macro", "65");

            Assert.Equal(new[] { expected }, action.trigger.ToArray());
        }
    }
}
