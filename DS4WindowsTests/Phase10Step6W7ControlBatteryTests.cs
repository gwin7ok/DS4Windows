using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.DS4Control;
using DS4Windows.Services;
using DS4WinWPF.DS4Forms.ViewModels;
using DS4WinWPF.DS4Forms.ViewModels.SpecialActions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step6 W7 相当（Check Battery を Controls へ）: 保存（通知とライトバーの on／off、2 色、押し続ける秒数）、実行（共通の出力の部品と Controls 由来の方針）、
    /// 画面の入力（<see cref="CheckBatteryViewModel"/> の型のある項目との往復）。SA と同じ処理で、Controls 固有の分岐はない。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step6W7ControlBatteryTests
    {

        private static ControlSpecialItem Battery(bool notification = true, bool lightbar = true, double seconds = 0)
            => new ControlSpecialItem
            {
                Type = SpecialAction.ActionTypeId.BatteryCheck,
                HoldSeconds = seconds,
                BatteryNotification = notification,
                BatteryLightbar = lightbar,
                EmptyColor = new DS4Color(10, 20, 30),
                FullColor = new DS4Color(40, 50, 60),
            };

        [Fact]
        public void ACheckBatteryAssignment_MakesOneSpecialBinding_WithTheSharedOutputAndTheControlsPolicy()
        {
            using var t = new ControlsKeyTestScope();
            t.MapSpecial(DS4Controls.Cross, Battery(seconds: 1.5));

            IReadOnlyList<IActionBinding> bindings = TestHost.GetService<IControlBindingBuilder>().Build(t.Device);

            var binding = Assert.IsType<TriggerBinding>(Assert.Single(bindings));
            var input = Assert.IsType<ControlButtonInputAction>(binding.Input);
            Assert.Equal(ControlBindingKind.Special, input.Kind);
            Assert.Equal(new[] { DS4Controls.Cross }, input.Triggers);
            Assert.IsType<BatteryCheckAction>(Assert.Single(binding.Outputs));
            Assert.False(binding.Policy.SuppressesTriggerButtons);
            Assert.True(binding.Policy.ErasesOutputEveryFrame);   // 割り当てたボタンの元の出力は、毎フレーム消す（Controls の規則）
            Assert.True(binding.Policy.RespectsFreshPressGuard);

            // 実行の部品が読む形（SA と同じ details）: 「秒数,通知,ライトバー,空の R,G,B,満の R,G,B」
            Assert.Equal("1.5,True,True,10,20,30,40,50,60", binding.Source.details);
            Assert.Equal(1.5, binding.Source.delayTime);
        }

        [Fact]
        public void TheSyntheticSa_HasTheSameDetailsAsASaSavedByTheEditor()
        {
            ControlSpecialItem item = Battery(notification: false, lightbar: true, seconds: 2);

            SpecialAction sa = SpecialActionItemMapper.ToSpecialAction("n", new[] { DS4Controls.Cross }, item);

            Assert.Equal("2,False,True,10,20,30,40,50,60", sa.details);
            ControlSpecialItem back = SpecialActionItemMapper.ToItem(sa);
            Assert.False(back.BatteryNotification);
            Assert.True(back.BatteryLightbar);
            Assert.Equal(new DS4Color(10, 20, 30), back.EmptyColor);
            Assert.Equal(new DS4Color(40, 50, 60), back.FullColor);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void TheBatterySettings_SurviveSavingAndReloadingTheProfile(bool notification, bool lightbar)
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore store = PersistenceHarness.NewStore();
            DS4ControlSettings dcs = store.GetDS4CSetting(PersistenceHarness.Slot, DS4Controls.Cross);
            dcs.Reset();
            dcs.actionType = DS4ControlSettings.ActionType.Special;
            dcs.special = Battery(notification, lightbar, 2.5);

            string xml = PersistenceHarness.SaveProfile(ctx, store, "S6W7Battery");
            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, xml, "S6W7BatteryLoad");

            ControlSpecialItem back = loaded.GetDS4CSetting(PersistenceHarness.Slot, DS4Controls.Cross).special;
            Assert.Equal(SpecialAction.ActionTypeId.BatteryCheck, back.Type);
            Assert.Equal(notification, back.BatteryNotification);
            Assert.Equal(lightbar, back.BatteryLightbar);
            Assert.Equal(2.5, back.HoldSeconds);
            Assert.Equal(new DS4Color(10, 20, 30), back.EmptyColor);
            Assert.Equal(new DS4Color(40, 50, 60), back.FullColor);
        }

        private static CheckBatteryViewModel NewVm()
            => new CheckBatteryViewModel(new RecordingSpecialActionRepository(), TestHost.GetService<IEnvironmentService>());

        [Fact]
        public void TheInput_RoundTripsTheItem_IncludingTheColors()
        {
            CheckBatteryViewModel vm = NewVm();

            vm.LoadControlItem(Battery(notification: false, lightbar: true, seconds: 3));

            Assert.False(vm.Notification);
            Assert.True(vm.Lightbar);
            Assert.Equal(3, vm.Delay);
            Assert.Equal(Color.FromArgb(255, 10, 20, 30), vm.EmptyColor);
            Assert.Equal(Color.FromArgb(255, 40, 50, 60), vm.FullColor);

            ControlSpecialItem back = vm.ToControlItem(SpecialAction.ActionTypeId.BatteryCheck);
            Assert.Equal(SpecialAction.ActionTypeId.BatteryCheck, back.Type);
            Assert.False(back.BatteryNotification);
            Assert.True(back.BatteryLightbar);
            Assert.Equal(3, back.HoldSeconds);
            Assert.Equal(new DS4Color(10, 20, 30), back.EmptyColor);
            Assert.Equal(new DS4Color(40, 50, 60), back.FullColor);
        }

        [Fact]
        public void AnItemWithoutColors_KeepsTheInputDefaultColors()
        {
            CheckBatteryViewModel vm = NewVm();
            Color empty = vm.EmptyColor, full = vm.FullColor;

            vm.LoadControlItem(new ControlSpecialItem { Type = SpecialAction.ActionTypeId.BatteryCheck });

            Assert.Equal(empty, vm.EmptyColor);
            Assert.Equal(full, vm.FullColor);
        }

        [Fact]
        public void TheEditTarget_WritesAndReadsTheBatteryItem_ThroughTheEditorSet()
        {
            var profile = TestRepositoryFactory.CreateProfileSettingsService();
            profile.OutputKBMMapping = new SendInputMapping();
            var repo = new RecordingSpecialActionRepository();
            var pressKey = new PressKeyViewModel(repo, profile, TestRepositoryFactory.CreateOutputSlotService());
            var macro = new MacroViewModel(repo);
            CheckBatteryViewModel vm = NewVm();
            vm.Notification = true;
            vm.Lightbar = false;
            vm.Delay = 1.25;
            var editors = new ActionTypeEditorSet(new Dictionary<SpecialAction.ActionTypeId, IActionTypeEditor>
            {
                [SpecialAction.ActionTypeId.BatteryCheck] = vm,
            });
            var original = new DS4ControlSettings(DS4Controls.Cross);
            var target = new ControlSettingsEditTarget(original, profile);

            target.WriteFrom(6, pressKey, macro, editors);
            target.Apply();

            Assert.Equal(DS4ControlSettings.ActionType.Special, original.actionType);
            Assert.Equal(SpecialAction.ActionTypeId.BatteryCheck, original.special.Type);
            Assert.True(original.special.BatteryNotification);
            Assert.False(original.special.BatteryLightbar);
            Assert.Equal(1.25, original.special.HoldSeconds);

            CheckBatteryViewModel reopened = NewVm();
            var reopenedTarget = new ControlSettingsEditTarget(original, profile);
            reopenedTarget.LoadInto(pressKey, macro, new ActionTypeEditorSet(new Dictionary<SpecialAction.ActionTypeId, IActionTypeEditor>
            {
                [SpecialAction.ActionTypeId.BatteryCheck] = reopened,
            }));

            Assert.True(reopened.Notification);
            Assert.False(reopened.Lightbar);
            Assert.Equal(1.25, reopened.Delay);
            Assert.Equal(6, reopenedTarget.InitialTypeIndex);
        }

        [Fact]
        public void TheValidation_IsTheSameAsTheSaOne()
        {
            CheckBatteryViewModel vm = NewVm();
            vm.Notification = false;
            vm.Lightbar = false;

            Assert.False(vm.IsValid(new SpecialAction("n", "Cross", "BatteryCheck", "0|False|False|1|2|3|4|5|6")));   // どちらの方法も選んでいない

            vm.Notification = true;
            Assert.True(vm.IsValid(new SpecialAction("n", "Cross", "BatteryCheck", "0|True|False|1|2|3|4|5|6")));
        }
    }
}
