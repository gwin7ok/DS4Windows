using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step2 W2（Phase10-Step2-Plan.md §3.2・§5）: トリガーの行（<see cref="TriggerOptionViewModel"/>）と、
    /// それを一覧にして持つ編集画面の ViewModel（<see cref="ActionEditorViewModel.TriggerOptions"/>）。
    /// 従来、<c>AssignmentEditor</c> のコードビハインドが、名前つきのチェックボックス 49 個の <c>Click</c> と、読み込み時の照合で行っていた動作と同じ。
    /// </summary>
    public class Phase10Step2W2TriggerOptionsTests
    {
        private sealed class FixedTriggerSource : IActionTriggerSource
        {
            public bool CanChange => false;
            public IReadOnlyList<string> Triggers { get; } = new[] { "Cross" };
            public IReadOnlyList<string> UnloadTriggers { get; } = new string[0];
            public bool SetTrigger(string name, bool selected) => false;
            public bool SetUnloadTrigger(string name, bool selected) => false;
            public string TriggersText => "Cross";
            public string UnloadTriggersText => null;
            public bool Validate(out string error) { error = null; return true; }
        }

        private static SpecialActEditorViewModel NewVm()
            => new SpecialActEditorViewModel(0, null, new RecordingSpecialActionRepository());

        private static TriggerOptionViewModel Option(SpecialActEditorViewModel vm, string name)
            => vm.TriggerOptions.Single(o => o.Name == name);

        [Fact]
        public void TheOptions_FollowTheCatalog_InOrder_AndStartUnselected()
        {
            SpecialActEditorViewModel vm = NewVm();

            Assert.Equal(new TriggerCatalog().Items.Select(i => i.Name), vm.TriggerOptions.Select(o => o.Name));
            Assert.Equal(new TriggerCatalog().Items.Select(i => i.Label), vm.TriggerOptions.Select(o => o.Label));
            Assert.All(vm.TriggerOptions, o => Assert.False(o.IsSelected));
            Assert.All(vm.TriggerOptions, o => Assert.True(o.IsEnabled));
        }

        [Fact]
        public void Selecting_WritesTheTriggerList_InTheOrderOfTheClicks()
        {
            SpecialActEditorViewModel vm = NewVm();

            Option(vm, "Circle").IsSelected = true;
            Option(vm, "Cross").IsSelected = true;
            Option(vm, "L2 Full Pull").IsSelected = true;

            Assert.Equal(new[] { "Circle", "Cross", "L2 Full Pull" }, vm.ControlTriggerList);
            Assert.Equal("Circle/Cross/L2 Full Pull", vm.TriggerSource.TriggersText);
        }

        [Fact]
        public void Unselecting_RemovesItFromTheList()
        {
            SpecialActEditorViewModel vm = NewVm();
            Option(vm, "Cross").IsSelected = true;
            Option(vm, "Circle").IsSelected = true;

            Option(vm, "Cross").IsSelected = false;

            Assert.Equal(new[] { "Circle" }, vm.ControlTriggerList);
            Assert.False(Option(vm, "Cross").IsSelected);
            Assert.True(Option(vm, "Circle").IsSelected);
        }

        [Fact]
        public void ChangingTheSelection_RaisesPropertyChanged_OnlyWhenItReallyChanges()
        {
            SpecialActEditorViewModel vm = NewVm();
            TriggerOptionViewModel option = Option(vm, "Square");
            var raised = new List<string>();
            option.PropertyChanged += (s, e) => raised.Add(e.PropertyName);

            option.IsSelected = true;
            option.IsSelected = true;    // 変化なし
            option.IsSelected = false;

            Assert.Equal(new[] { "IsSelected", "IsSelected" }, raised);
        }

        [Fact]
        public void TheOptions_ReflectTheListWhenItIsChangedFromOutside_AfterARefresh()
        {
            SpecialActEditorViewModel vm = NewVm();
            TriggerOptionViewModel option = Option(vm, "Triangle");

            vm.ControlTriggerList.Add("Triangle");   // 画面の外から（読み込みなど）
            Assert.True(option.IsSelected);          // 値は、供給元が持つ（重複して持たない）
        }

        // ---- 読み込み ----

        [Fact]
        public void LoadAction_SelectsTheLoadedTriggers_InTheFileOrder_AndRoundTripsThem()
        {
            SpecialActEditorViewModel vm = NewVm();
            var action = new SpecialAction("Loaded", "Circle/Cross/Right Stick Left", "Macro", "65");
            var refreshed = new List<string>();
            foreach (TriggerOptionViewModel o in vm.TriggerOptions)
            {
                string name = o.Name;
                o.PropertyChanged += (s, e) => { if (e.PropertyName == "IsSelected") refreshed.Add(name); };
            }

            vm.LoadAction(action);

            Assert.True(Option(vm, "Circle").IsSelected);
            Assert.True(Option(vm, "Cross").IsSelected);
            Assert.True(Option(vm, "Right Stick Left").IsSelected);
            Assert.Equal(3, vm.TriggerOptions.Count(o => o.IsSelected));
            Assert.Equal(new TriggerCatalog().Items.Count, refreshed.Distinct().Count());   // 画面へ、全行の変化を知らせる

            var written = new SpecialAction("n", "n", "n", "n");
            vm.ActionTypeIndex = 1;
            vm.SetAction(written);
            Assert.Equal("Circle/Cross/Right Stick Left", written.controls);   // 読み込みの順を保つ
        }

        [Fact]
        public void LoadAction_DropsNamesThatAreNotInTheCatalog_FromBothLists()
        {
            // 旧: 対応するチェックボックスがない名前は、コードビハインドが、リストから外していた
            SpecialActEditorViewModel vm = NewVm();
            var action = new SpecialAction("Loaded", "Cross/Cross", "Macro", "65");
            action.controls = "Cross/Bogus/Circle";
            action.ucontrols = "R1/BogusUnload/AutomaticUntrigger/L1";

            vm.LoadAction(action);

            Assert.Equal(new[] { "Cross", "Circle" }, vm.ControlTriggerList);
            Assert.Equal(new[] { "R1", "L1" }, vm.ControlUnloadTriggerList);   // 解除トリガーは、画面に出さないが、保って書き戻す
            Assert.Equal(2, vm.TriggerOptions.Count(o => o.IsSelected));
        }

        // ---- 変えられない供給元（Controls のモード。W3 で使う）----

        [Fact]
        public void AFixedSource_ShowsTheTrigger_ButCannotBeChanged()
        {
            var source = new FixedTriggerSource();
            var option = new TriggerOptionViewModel(new TriggerItem("Cross", "Cross"), source);
            var other = new TriggerOptionViewModel(new TriggerItem("Circle", "Circle"), source);

            Assert.True(option.IsSelected);
            Assert.False(option.IsEnabled);

            option.IsSelected = false;    // 無視される
            other.IsSelected = true;      // 無視される

            Assert.True(option.IsSelected);
            Assert.False(other.IsSelected);
        }

        [Fact]
        public void TheOptionRejectsNullArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new TriggerOptionViewModel(null, new SpecialActionTriggerSource()));
            Assert.Throws<ArgumentNullException>(() => new TriggerOptionViewModel(new TriggerItem("Cross", "Cross"), null));
        }
    }
}
