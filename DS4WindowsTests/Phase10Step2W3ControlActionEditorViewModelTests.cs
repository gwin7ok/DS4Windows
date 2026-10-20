using System.Collections;
using System.Linq;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step2 W3（Phase10-Step2-Plan.md §3.3・§5）: 共通の編集画面の、Controls のモードの ViewModel
    /// （<see cref="ControlActionEditorViewModel"/>）。名前は、ボタンの表示名で編集不可、トリガーは、そのボタンだけで変更不可、
    /// 種別の一覧は、Controls で現在保存できる種別（Key・Button・Macro）だけ選べる。SA のモードの ViewModel は、従来どおり。
    /// </summary>
    public class Phase10Step2W3ControlActionEditorViewModelTests
    {
        private static ControlActionEditorViewModel NewControlVm(DS4Controls control = DS4Controls.Circle)
            => new ControlActionEditorViewModel(0, control);

        private static string[] ErrorsOf(ActionEditorViewModel vm, string key)
            => ((IEnumerable)vm.GetErrors(key))?.Cast<string>().ToArray();

        [Fact]
        public void TheNameField_ShowsTheButtonName_AndIsReadOnly()
        {
            ControlActionEditorViewModel vm = NewControlVm(DS4Controls.Circle);

            Assert.Equal(DS4ControlNames.ds4inputNames[DS4Controls.Circle], vm.ActionName);
            Assert.False(vm.ActionNameEditable);
            Assert.True(vm.ActionNameReadOnly);
            Assert.Equal(DS4Controls.Circle, vm.Control);
        }

        [Fact]
        public void AWriteToTheName_IsIgnored()
        {
            ControlActionEditorViewModel vm = NewControlVm();
            string original = vm.ActionName;

            vm.ActionName = "Something else";

            Assert.Equal(original, vm.ActionName);
        }

        [Fact]
        public void TheTriggerList_HasOnlyTheTargetButton_SelectedAndLocked()
        {
            ControlActionEditorViewModel vm = NewControlVm(DS4Controls.Circle);

            TriggerOptionViewModel only = Assert.Single(vm.TriggerOptions);
            Assert.Equal(DS4ControlNames.ds4inputNames[DS4Controls.Circle], only.Label);
            Assert.True(only.IsSelected);
            Assert.False(only.IsEnabled);

            only.IsSelected = false;   // 変えられない
            Assert.True(only.IsSelected);
            Assert.False(vm.TriggerSource.CanChange);
        }

        [Fact]
        public void TheModeIsControl()
        {
            Assert.Equal(ActionEditorModes.Control, NewControlVm().Mode);
        }

        [Theory]
        [InlineData(0, true)]    // 種別を選んでください（常に選べる）
        [InlineData(1, true)]    // Macro
        [InlineData(2, false)]   // Launch Program（Step3 まで、Controls に保存する場所がない）
        [InlineData(3, false)]   // Load Profile
        [InlineData(4, true)]    // Output Key/Mouse/Button（Key・Button）
        [InlineData(5, false)]   // Disconnect BT
        [InlineData(6, false)]   // Check Battery
        [InlineData(7, false)]   // Multi Action
        [InlineData(8, false)]   // 6 軸ホイール較正
        [InlineData(9, false)]   // ジャイロ較正
        public void OnlyTheTypesControlsCanSaveNow_AreSelectable(int index, bool expected)
        {
            Assert.Equal(expected, NewControlVm().IsTypeEnabled(index));
        }

        [Fact]
        public void IsValid_NeedsOnlyAType_NoNameOrTriggerErrors()
        {
            ControlActionEditorViewModel vm = NewControlVm();

            vm.ActionTypeIndex = 0;
            Assert.False(vm.IsValid(new SpecialAction("n", "n", "n", "n")));
            Assert.Equal(new[] { "Specify an action type" }, ErrorsOf(vm, "ActionTypeIndex"));
            Assert.Null(ErrorsOf(vm, "ActionName"));
            Assert.False(vm.TriggerError);

            vm.ActionTypeIndex = 4;
            Assert.True(vm.IsValid(new SpecialAction("n", "n", "n", "n")));
            Assert.False(vm.HasErrors);
            Assert.False(vm.ExistingName);
        }

        [Fact]
        public void SetAction_WritesTheFixedNameTheTriggerAndTheType()
        {
            ControlActionEditorViewModel vm = NewControlVm(DS4Controls.Circle);
            vm.ActionTypeIndex = 1;
            var action = new SpecialAction("n", "n", "n", "n");

            vm.SetAction(action);

            Assert.Equal(DS4ControlNames.ds4inputNames[DS4Controls.Circle], action.name);
            Assert.Equal("Circle", action.controls);
            Assert.Equal(SpecialAction.ActionTypeId.Macro, action.typeID);
            Assert.Null(action.ucontrols);
        }

        // ---- SA のモードは、従来どおり ----

        [Fact]
        public void TheSpecialActionMode_IsUnchanged_NameEditable_AndEveryRealTypeSelectable()
        {
            var vm = new SpecialActEditorViewModel(0, null, new RecordingSpecialActionRepository());

            Assert.Equal(ActionEditorModes.SpecialAction, vm.Mode);
            Assert.True(vm.ActionNameEditable);
            Assert.False(vm.ActionNameReadOnly);
            for (int i = 0; i < 10; i++)
                Assert.True(vm.IsTypeEnabled(i));
        }
    }
}
