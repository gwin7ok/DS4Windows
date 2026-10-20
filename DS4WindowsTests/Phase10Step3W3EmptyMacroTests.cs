using System.IO;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels.SpecialActions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step3（ユーザー承認、2026-10-21）: 手順が空のマクロも、SA として保存できる（従来は「No macro defined」で保存できなかった）。
    /// 保存できること、保存した値を読み込み直しても空のマクロになること、空のマクロを「押している間繰り返す」で空回りしないこと（ソース走査）を固定する。
    /// </summary>
    public class Phase10Step3W3EmptyMacroTests
    {
        private readonly ITestOutputHelper _output;

        public Phase10Step3W3EmptyMacroTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void AnEmptyMacro_IsValid_AndRecordsNoError()
        {
            var vm = new MacroViewModel(new RecordingSpecialActionRepository());

            Assert.True(vm.IsValid(new SpecialAction("N", "Cross", "Macro", "")));
            Assert.False(vm.HasErrors);
        }

        [Fact]
        public void AnEmptyMacro_IsValid_AfterAnEarlierError_WasCleared()
        {
            var vm = new MacroViewModel(new RecordingSpecialActionRepository());
            vm.Macro.Add(350);
            vm.Macro.Clear();

            Assert.True(vm.IsValid(new SpecialAction("N", "Cross", "Macro", "")));
        }

        [Fact]
        public void AnEmptyMacro_IsSavedWithEmptyDetails()
        {
            var repo = new RecordingSpecialActionRepository();
            var vm = new MacroViewModel(repo);
            var action = new SpecialAction("N", "Cross", "Macro", "");

            vm.SaveAction(action);

            Assert.Equal(1, repo.SaveCount);
            Assert.Equal(string.Empty, repo.SavedDetails);
        }

        [Fact]
        public void AnEmptyMacro_IsLoadedBackAsAnEmptyMacro()
        {
            var action = new SpecialAction("N", "Cross", "Macro", "");

            Assert.Equal(SpecialAction.ActionTypeId.Macro, action.typeID);
            Assert.Empty(action.macro);
        }

        [Fact]
        public void TheRepeatLoop_WaitsBetweenRuns_OfAnEmptyMacro_InsteadOfSpinningOrStopping()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Control/Mapping.cs");
            if (path == null)
            {
                _output.WriteLine("Mapping.cs が見つからないため検査を省略した。");
                return;
            }

            string source = File.ReadAllText(path);
            int guard = source.IndexOf("(macroLst == null || macroLst.Count == 0) && (macroArr == null || macroArr.Length == 0)");
            int wait = source.IndexOf("Task.Delay(EmptyMacroRepeatIntervalMs).Wait()", guard < 0 ? 0 : guard);
            int rerun = source.IndexOf("RunMacroOnce(device, macroLst, macroArr, keyType, action, actionDoneState, markRunning: true)");
            Assert.True(guard >= 0, "空のマクロの繰り返しの間隔の条件がない");
            Assert.True(wait > guard, "空のマクロの繰り返しに、間隔の待ちがない");
            Assert.True(rerun > wait, "間隔の待ちが、繰り返しの再生より前にない");
            // 繰り返しを止める（break）のではなく、待つ。条件の直後に break があってはならない
            Assert.DoesNotContain("break;", source.Substring(guard, wait - guard));
        }
    }
}
