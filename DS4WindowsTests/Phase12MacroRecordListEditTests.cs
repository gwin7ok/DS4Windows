using System;
using System.Linq;
using System.Windows;
using Xunit;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Processing;
using DS4Windows.UI.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase12（ユーザー要望 2026-11-03）: 記録画面（<c>RecordBox</c>）のコマンド一覧の編集。
    /// 複数選択の挿入位置（いちばん上の選択行）、選択行の一括削除、ドラッグ＆ドロップによる並べ替えの ViewModel の部分。
    /// Ctrl＋クリック・Shift＋クリックの選択、空白クリックでの選択解除、ドラッグ操作そのものは、実機で確認する。
    /// </summary>
    public class Phase12MacroRecordListEditTests
    {
        static Phase12MacroRecordListEditTests()
        {
            if (string.IsNullOrEmpty(TestGlobals.AppDataPath))
            {
                TestGlobals.AppDataPath = AppContext.BaseDirectory;
            }

            if (Application.Current == null)
            {
                try { _ = new Application(); } catch { }
            }

            if (Application.ResourceAssembly == null)
            {
                try { Application.ResourceAssembly = typeof(DS4Windows.Composition.AppHost).Assembly; } catch { }
            }

            if (Application.Current != null)
            {
                foreach (string key in new[] { "KeyDownImg", "KeyUpImg", "ClockImg" })
                {
                    if (!Application.Current.Resources.Contains(key))
                    {
                        Application.Current.Resources[key] = "";
                    }
                }
            }
        }

        private static RecordBoxViewModel CreateViewModel(int stepCount)
        {
            ProfileSettingsService settings = TestRepositoryFactory.CreateProfileSettingsService(new BackingStore(new EnvironmentService()));
            var vm = new RecordBoxViewModel(ControllerLimits.TestProfileIndex, new DS4ControlSettings(DS4Controls.Cross), false,
                settings, new EnvironmentService(), null);
            for (int i = 0; i < stepCount; i++)
            {
                // 値で区別する（キー 65 + i の押下）
                vm.InsertMacroStep(i, new MacroStep(65 + i, "K" + i, MacroStep.StepType.ActDown, MacroStep.StepOutput.Key));
            }

            return vm;
        }

        private static string Order(RecordBoxViewModel vm) => string.Join(",", vm.MacroSteps.Select(item => item.Step.Name));

        [Theory]
        [InlineData(new int[0], -1)]
        [InlineData(new[] { 3 }, 3)]
        [InlineData(new[] { 5, 2, 7 }, 2)]
        [InlineData(new[] { -1, 4 }, 4)]
        public void FirstIndexOf_IsTheTopmostSelectedRow_OrMinusOneForNoSelection(int[] selected, int expected)
        {
            Assert.Equal(expected, RecordBoxViewModel.FirstIndexOf(selected));
        }

        [Fact]
        public void RemoveSteps_RemovesEverySelectedRow_AndKeepsTheOthersInOrder()
        {
            RecordBoxViewModel vm = CreateViewModel(6);

            int removed = vm.RemoveSteps(new[] { 4, 1, 2 });

            Assert.Equal(3, removed);
            Assert.Equal("K0,K3,K5", Order(vm));
        }

        [Fact]
        public void RemoveSteps_IgnoresDuplicatesAndOutOfRangeIndexes()
        {
            RecordBoxViewModel vm = CreateViewModel(3);

            int removed = vm.RemoveSteps(new[] { 1, 1, -1, 9 });

            Assert.Equal(1, removed);
            Assert.Equal("K0,K2", Order(vm));
        }

        [Fact]
        public void RemoveSteps_WithNothingSelected_RemovesNothing()
        {
            RecordBoxViewModel vm = CreateViewModel(3);

            Assert.Equal(0, vm.RemoveSteps(new int[0]));
            Assert.Equal("K0,K1,K2", Order(vm));
        }

        [Fact]
        public void MoveSteps_MovesOneRowDown_BeforeTheTargetRow()
        {
            RecordBoxViewModel vm = CreateViewModel(5);

            int[] moved = vm.MoveSteps(new[] { 0 }, 3);   // K0 を、K3 の前へ

            Assert.Equal("K1,K2,K0,K3,K4", Order(vm));
            Assert.Equal(new[] { 2 }, moved);
        }

        [Fact]
        public void MoveSteps_MovesOneRowUp()
        {
            RecordBoxViewModel vm = CreateViewModel(5);

            int[] moved = vm.MoveSteps(new[] { 4 }, 1);

            Assert.Equal("K0,K4,K1,K2,K3", Order(vm));
            Assert.Equal(new[] { 1 }, moved);
        }

        [Fact]
        public void MoveSteps_MovesToTheEnd()
        {
            RecordBoxViewModel vm = CreateViewModel(4);

            int[] moved = vm.MoveSteps(new[] { 1 }, 4);

            Assert.Equal("K0,K2,K3,K1", Order(vm));
            Assert.Equal(new[] { 3 }, moved);
        }

        [Fact]
        public void MoveSteps_MovesSeveralRowsAsOneBlock_KeepingTheirOrder()
        {
            RecordBoxViewModel vm = CreateViewModel(7);

            int[] moved = vm.MoveSteps(new[] { 4, 1 }, 6);   // K1 と K4 を、K6 の前へ

            Assert.Equal("K0,K2,K3,K5,K1,K4,K6", Order(vm));
            Assert.Equal(new[] { 4, 5 }, moved);
        }

        [Fact]
        public void MoveSteps_MovesSeveralRowsUp()
        {
            RecordBoxViewModel vm = CreateViewModel(6);

            int[] moved = vm.MoveSteps(new[] { 3, 5 }, 1);

            Assert.Equal("K0,K3,K5,K1,K2,K4", Order(vm));
            Assert.Equal(new[] { 1, 2 }, moved);
        }

        [Theory]
        [InlineData(new[] { 2 }, 2)]            // 自分の前
        [InlineData(new[] { 2 }, 3)]            // 自分の次の前（=同じ位置）
        [InlineData(new[] { 1, 2 }, 1)]         // かたまりの先頭
        [InlineData(new[] { 1, 2 }, 2)]         // かたまりの中
        [InlineData(new[] { 1, 2 }, 3)]         // かたまりのすぐ後
        public void MoveSteps_DroppedWhereItAlreadyIs_ChangesNothing(int[] sources, int target)
        {
            RecordBoxViewModel vm = CreateViewModel(5);

            Assert.Null(vm.MoveSteps(sources, target));
            Assert.Equal("K0,K1,K2,K3,K4", Order(vm));
        }

        [Fact]
        public void MoveSteps_ClampsTheTargetAndIgnoresBadIndexes()
        {
            RecordBoxViewModel vm = CreateViewModel(3);

            int[] moved = vm.MoveSteps(new[] { 0, 99 }, 99);

            Assert.Equal("K1,K2,K0", Order(vm));
            Assert.Equal(new[] { 2 }, moved);
            Assert.Null(vm.MoveSteps(new int[0], 1));
            Assert.Null(vm.MoveSteps(new[] { 99 }, 1));
        }
    }
}
