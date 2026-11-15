using System;
using System.IO;
using System.Windows;
using Xunit;
using DS4Windows.Core.Model;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Macro;
using DS4Windows.UI.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// 記録画面（<c>RecordBox</c>）のプリセットの読み書きの「ファイルを選ぶ」部分と、移動の記録の上限の通知が、
    /// 窓なしの ViewModel だけで動くこと（偽の <c>IDialogService</c> で確かめる）。
    /// </summary>
    public class RecordBoxViewModelDialogTests
    {
        static RecordBoxViewModelDialogTests()
        {
            if (Application.Current == null)
            {
                try { _ = new Application(); } catch { }
            }

            if (Application.Current != null)
            {
                foreach (string key in new[] { "KeyDownImg", "KeyUpImg", "ClockImg" })
                {
                    if (!Application.Current.Resources.Contains(key)) Application.Current.Resources[key] = "";
                }
            }
        }

        private static RecordBoxViewModel Create(FakeDialogService dialogs)
        {
            var settings = TestRepositoryFactory.CreateProfileSettingsService(new BackingStore(new EnvironmentService()));
            return new RecordBoxViewModel(TestLightbar.State, ControllerLimits.TestProfileIndex, new DS4ControlSettings(DS4Controls.Cross), false,
                settings, new EnvironmentService(), null, -1, dialogs);
        }

        [Fact]
        public void PickAndSavePreset_WhenCancelled_WritesNothing()
        {
            var dialogs = new FakeDialogService { SaveFileAnswer = null };
            var vm = Create(dialogs);

            Assert.False(vm.PickAndSavePreset("InitDir"));
            Assert.Equal("InitDir", dialogs.LastSaveOptions.InitialDirectory);
        }

        [Fact]
        public void PickAndSavePreset_ThenPickAndLoadPreset_RoundTripsTheSteps()
        {
            string file = Path.Combine(Path.GetTempPath(), "RecBoxDlg_" + Guid.NewGuid().ToString("N") + ".txt");
            try
            {
                var dialogs = new FakeDialogService { SaveFileAnswer = file, OpenFileAnswer = file };
                var vm = Create(dialogs);
                vm.InsertMacroStep(0, new MacroStep(65, "A", MacroStep.StepType.ActDown, MacroStep.StepOutput.Key));
                vm.InsertMacroStep(1, new MacroStep(65, "A", MacroStep.StepType.ActUp, MacroStep.StepOutput.Key));

                Assert.True(vm.PickAndSavePreset("InitDir"));
                vm.MacroSteps.Clear();
                Assert.True(vm.PickAndLoadPreset("InitDir"));

                Assert.Equal(2, vm.MacroSteps.Count);
            }
            finally
            {
                try { File.Delete(file); } catch { }
            }
        }

        [Fact]
        public void NotifyMoveRecordLimitReached_ShowsAMessageWithTheLimit()
        {
            var dialogs = new FakeDialogService();
            var vm = Create(dialogs);

            vm.NotifyMoveRecordLimitReached(1234);

            Assert.Single(dialogs.Messages);
            Assert.Contains("1234", dialogs.Messages[0]);
        }
    }
}
