using System;
using System.Collections;
using System.Linq;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step2 W1（Phase10-Step2-Plan.md §3.1・§5）: SA の編集画面の ViewModel（<see cref="SpecialActEditorViewModel"/>）。
    /// 中身を <see cref="ActionEditorViewModel"/> へ移しても、読み込み・書き出し・検証（名前の必須と重複、トリガーが 1 つ以上、種別の指定）と、
    /// エラーの文言・キーが、従来と同じであること。
    /// </summary>
    public class Phase10Step2W1SpecialActEditorViewModelTests
    {
        private static SpecialAction Existing(string name)
            => new SpecialAction(name, "Cross", "Macro", "65");

        private static SpecialActEditorViewModel New(RecordingSpecialActionRepository repo = null, SpecialAction saved = null)
            => new SpecialActEditorViewModel(0, saved, repo ?? new RecordingSpecialActionRepository());

        private static string[] ErrorsOf(SpecialActEditorViewModel vm, string key)
            => ((IEnumerable)vm.GetErrors(key))?.Cast<string>().ToArray();

        [Fact]
        public void Constructor_RequiresTheRepository()
        {
            Assert.Throws<ArgumentNullException>(() => new SpecialActEditorViewModel(0, null, null));
        }

        [Fact]
        public void ANewAction_IsNotInEditMode_AndAnExistingOneIs()
        {
            Assert.False(New().EditMode);
            Assert.Null(New().SavedAction);

            SpecialAction saved = Existing("A");
            SpecialActEditorViewModel editing = New(saved: saved);
            Assert.True(editing.EditMode);
            Assert.Same(saved, editing.SavedAction);
        }

        // ---- 検証 ----

        [Fact]
        public void IsValid_AcceptsANameATriggerAndAType()
        {
            SpecialActEditorViewModel vm = New();
            vm.ActionName = "New action";
            vm.ControlTriggerList.Add("Cross");
            vm.ActionTypeIndex = 1;

            Assert.True(vm.IsValid(new SpecialAction("n", "n", "n", "n")));
            Assert.False(vm.HasErrors);
            Assert.False(vm.ExistingName);
            Assert.False(vm.TriggerError);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IsValid_RejectsAnEmptyName(string name)
        {
            SpecialActEditorViewModel vm = New();
            vm.ActionName = name;
            vm.ControlTriggerList.Add("Cross");
            vm.ActionTypeIndex = 1;

            Assert.False(vm.IsValid(new SpecialAction("n", "n", "n", "n")));
            Assert.Equal(new[] { "No name provided" }, ErrorsOf(vm, "ActionName"));
            Assert.False(vm.ExistingName);   // 名前の重複ではない
        }

        [Fact]
        public void IsValid_RejectsADuplicateName_IgnoringCase_AndReportsIt()
        {
            var repo = new RecordingSpecialActionRepository();
            repo.ActionList.Add(Existing("Existing"));
            SpecialActEditorViewModel vm = New(repo);
            vm.ActionName = "EXISTING";
            vm.ControlTriggerList.Add("Cross");
            vm.ActionTypeIndex = 1;

            Assert.False(vm.IsValid(new SpecialAction("n", "n", "n", "n")));
            Assert.True(vm.ExistingName);
            Assert.Equal(new[] { "Existing action with name already exists" }, ErrorsOf(vm, "ActionName"));
        }

        [Fact]
        public void IsValid_InEditMode_AllowsTheSameName_ButNotAnotherExistingOne()
        {
            var repo = new RecordingSpecialActionRepository();
            SpecialAction mine = Existing("Mine");
            repo.ActionList.Add(mine);
            repo.ActionList.Add(Existing("Other"));
            SpecialActEditorViewModel vm = New(repo, mine);
            vm.ControlTriggerList.Add("Cross");
            vm.ActionTypeIndex = 1;

            vm.ActionName = "Mine";   // 名前を変えない: 自分自身との重複ではない
            Assert.True(vm.IsValid(new SpecialAction("n", "n", "n", "n")));

            vm.ActionName = "Other";  // 別の既存の名前に変える
            Assert.False(vm.IsValid(new SpecialAction("n", "n", "n", "n")));
            Assert.True(vm.ExistingName);
        }

        [Fact]
        public void IsValid_RequiresATrigger()
        {
            SpecialActEditorViewModel vm = New();
            vm.ActionName = "New action";
            vm.ActionTypeIndex = 1;

            Assert.False(vm.IsValid(new SpecialAction("n", "n", "n", "n")));
            Assert.True(vm.TriggerError);
            Assert.Equal(new[] { "No triggers provided" }, ErrorsOf(vm, "TriggerError"));
        }

        [Fact]
        public void IsValid_RequiresAType()
        {
            SpecialActEditorViewModel vm = New();
            vm.ActionName = "New action";
            vm.ControlTriggerList.Add("Cross");
            vm.ActionTypeIndex = 0;

            Assert.False(vm.IsValid(new SpecialAction("n", "n", "n", "n")));
            Assert.Equal(new[] { "Specify an action type" }, ErrorsOf(vm, "ActionTypeIndex"));
        }

        [Fact]
        public void IsValid_AgainAfterTheFix_ClearsTheOldErrors()
        {
            SpecialActEditorViewModel vm = New();
            vm.ActionName = "";
            vm.ActionTypeIndex = 1;
            Assert.False(vm.IsValid(new SpecialAction("n", "n", "n", "n")));
            Assert.True(vm.HasErrors);

            vm.ActionName = "Fixed";
            vm.ControlTriggerList.Add("Cross");
            Assert.True(vm.IsValid(new SpecialAction("n", "n", "n", "n")));
            Assert.False(vm.HasErrors);
        }

        // ---- 書き出し ----

        [Fact]
        public void SetAction_WritesTheNameTheTriggersAndTheType()
        {
            SpecialActEditorViewModel vm = New();
            vm.ActionName = "Out";
            vm.ControlTriggerList.Add("Cross");
            vm.ControlTriggerList.Add("Circle");
            vm.ActionTypeIndex = 4;
            var action = new SpecialAction("n", "n", "n", "n");

            vm.SetAction(action);

            Assert.Equal("Out", action.name);
            Assert.Equal("Cross/Circle", action.controls);
            Assert.Equal(SpecialAction.ActionTypeId.Key, action.typeID);
            Assert.Null(action.ucontrols);   // 解除トリガーがなければ、書き換えない
        }

        [Fact]
        public void SetAction_WritesTheUnloadTriggers_OnlyWhenThereAreSome()
        {
            SpecialActEditorViewModel vm = New();
            vm.ActionName = "Out";
            vm.ControlTriggerList.Add("Cross");
            vm.ActionTypeIndex = 3;
            var action = new SpecialAction("n", "n", "n", "n") { ucontrols = "keep" };

            vm.SetAction(action);
            Assert.Equal("keep", action.ucontrols);

            vm.ControlUnloadTriggerList.Add("R1");
            vm.SetAction(action);
            Assert.Equal("R1", action.ucontrols);
            Assert.Equal(SpecialAction.ActionTypeId.Profile, action.typeID);
        }

        // ---- 読み込み ----

        [Fact]
        public void LoadAction_ReadsTheNameTheTriggersAndTheUnloadTriggers_WithoutTheAutomaticUntriggerMarker()
        {
            SpecialActEditorViewModel vm = New();
            var action = new SpecialAction("Loaded", "Cross/Circle", "Macro", "65") { ucontrols = "R1/AutomaticUntrigger/L1" };

            vm.LoadAction(action);

            Assert.Equal("Loaded", vm.ActionName);
            Assert.Equal(new[] { "Cross", "Circle" }, vm.ControlTriggerList);
            Assert.Equal(new[] { "R1", "L1" }, vm.ControlUnloadTriggerList);
        }

        [Theory]
        [InlineData(SpecialAction.ActionTypeId.None, 0)]
        [InlineData(SpecialAction.ActionTypeId.Macro, 1)]
        [InlineData(SpecialAction.ActionTypeId.Program, 2)]
        [InlineData(SpecialAction.ActionTypeId.Profile, 3)]
        [InlineData(SpecialAction.ActionTypeId.Key, 4)]
        [InlineData(SpecialAction.ActionTypeId.Button, 4)]   // 一覧に独立した項目がなく、Key の項目（Press Key のタブ）で編集する
        [InlineData(SpecialAction.ActionTypeId.DisconnectBT, 5)]
        [InlineData(SpecialAction.ActionTypeId.BatteryCheck, 6)]
        [InlineData(SpecialAction.ActionTypeId.MultiAction, 7)]
        [InlineData(SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate, 8)]
        [InlineData(SpecialAction.ActionTypeId.GyroCalibrate, 9)]
        public void LoadAction_SelectsTheTypeItem(SpecialAction.ActionTypeId typeId, int expectedIndex)
        {
            SpecialActEditorViewModel vm = New();
            var action = new SpecialAction("n", "Cross", "Macro", "65") { typeID = typeId };

            vm.LoadAction(action);

            Assert.Equal(expectedIndex, vm.ActionTypeIndex);
        }

        [Fact]
        public void TypeAssoc_IsTheLegacyArray_ThroughTheCatalog()
        {
            SpecialActEditorViewModel vm = New();

            Assert.Equal(new ActionTypeCatalog().TypeIds, vm.TypeAssoc);
            Assert.Equal(SpecialAction.ActionTypeId.GyroCalibrate, vm.TypeAssoc[9]);
        }

        [Fact]
        public void TheSharedBase_ExposesTheNameAsEditableForTheSpecialActionMode()
        {
            SpecialActEditorViewModel vm = New();

            Assert.True(vm.ActionNameEditable);
            Assert.True(vm.TriggerSource.CanChange);
            Assert.True(vm.EditTarget.NameEditable);
        }
    }
}
