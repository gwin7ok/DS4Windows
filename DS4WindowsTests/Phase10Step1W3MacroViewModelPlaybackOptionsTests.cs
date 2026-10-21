using System;
using System.Collections.Generic;
using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF.DS4Forms.ViewModels.SpecialActions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step1 W3（Phase10-Step1-Plan.md §5）: SA の Macro タブの ViewModel（<see cref="MacroViewModel"/>）が、
    /// 繰り返し（Execute Mode）と Scan Code を共通の <c>MacroPlaybackOptions</c> で持つこと。
    /// 読み込み（<c>Actions.xml</c> の extras）、記録画面へ渡す設定（<c>PrepareSettings</c>）、保存（extras の文字列）が、従来と同じ。
    /// </summary>
    public class Phase10Step1W3MacroViewModelPlaybackOptionsTests
    {
        private sealed class RecordingRepository : ISpecialActionRepository
        {
            public string SavedDetails;
            public string SavedExtras;

            public string ActionsPath => string.Empty;
            public IReadOnlyList<SpecialAction> Actions => new List<SpecialAction>();
            public List<SpecialAction> ActionList => new List<SpecialAction>();
            public bool LoadActions() => true;
            public void CreateStandardActions() { }
            public SpecialAction GetActionOrPlaceholder(string actionName) => null;

            public void SaveAction(string name, string controls, int mode, string details, bool edit, double delayTime = 0.0, string extras = "", string actionId = null)
            {
                SavedDetails = details;
                SavedExtras = extras;
            }

            public bool RemoveActionAndSave(string actionName) => true;
            public bool SaveActionExtras(string actionName, string outputExtras) => true;
            public event EventHandler ActionsChanged { add { } remove { } }
        }

        private static SpecialAction MacroAction(string extras) => new SpecialAction("m", "Cross", "Macro", "350/256/310/256", 0, extras);

        [Theory]
        [InlineData("", false, false, false, false, false)]
        [InlineData("Scan Code", true, false, false, false, false)]
        [InlineData("Repeat", false, true, false, false, false)]
        [InlineData("Scan Code/RunOnRelease/Sync/KeepKeyState/Repeat", true, true, true, true, true)]
        public void LoadAction_RestoresEveryOption(string extras, bool scan, bool repeat, bool runRelease, bool sync, bool keep)
        {
            var vm = new MacroViewModel(new RecordingRepository());

            vm.LoadAction(MacroAction(extras));

            Assert.Equal(scan, vm.PlaybackOptions.UseScanCode);
            Assert.Equal(repeat, vm.PlaybackOptions.RepeatWhileHeld);
            Assert.Equal(runRelease, vm.RunTriggerRelease);
            Assert.Equal(sync, vm.SyncRun);
            Assert.Equal(keep, vm.KeepKeyState);
        }

        [Theory]
        [InlineData(false, false, DS4KeyType.Macro)]
        [InlineData(true, false, DS4KeyType.Macro | DS4KeyType.RepeatMacro)]
        [InlineData(false, true, DS4KeyType.Macro | DS4KeyType.ScanCode)]
        [InlineData(true, true, DS4KeyType.Macro | DS4KeyType.RepeatMacro | DS4KeyType.ScanCode)]
        public void PrepareSettings_PassesTheOptionsToTheRecordBox(bool repeat, bool scan, DS4KeyType expected)
        {
            var vm = new MacroViewModel(new RecordingRepository());
            vm.PlaybackOptions.RepeatWhileHeld = repeat;
            vm.PlaybackOptions.UseScanCode = scan;
            vm.Macro.AddRange(new[] { 350, 256 });

            DS4ControlSettings settings = vm.PrepareSettings();

            Assert.Equal(DS4ControlSettings.ActionType.Macro, settings.actionType);
            Assert.Equal(expected, settings.keyType);
        }

        [Fact]
        public void RecordBoxRoundTrip_KeepsTheOptions_WithoutAnyWriteBack()
        {
            // 記録画面は、PrepareSettings の keyType から読み、ExportMacro で同じ値を書く（オプションを出さないため、変えない）。
            // そのため、SA の Macro タブ側は、保存後に書き戻さなくても、値が保たれる
            var vm = new MacroViewModel(new RecordingRepository());
            vm.PlaybackOptions.RepeatWhileHeld = true;
            vm.PlaybackOptions.UseScanCode = true;
            DS4ControlSettings settings = vm.PrepareSettings();

            DS4WinWPF.DS4Forms.ViewModels.MacroPlaybackOptions seenByRecordBox =
                DS4WinWPF.DS4Forms.ViewModels.MacroPlaybackOptions.FromKeyType(settings.keyType);
            DS4KeyType exported = seenByRecordBox.ApplyTo(DS4KeyType.Macro);

            Assert.Equal(settings.keyType, exported);
            Assert.True(vm.PlaybackOptions.RepeatWhileHeld);
            Assert.True(vm.PlaybackOptions.UseScanCode);
        }

        [Theory]
        [InlineData(false, false, false, false, false, "")]
        [InlineData(true, false, false, false, false, "Scan Code")]
        [InlineData(false, true, false, false, false, "Repeat")]
        [InlineData(true, true, true, true, true, "Scan Code/RunOnRelease/Sync/KeepKeyState/Repeat")]
        public void SaveAction_WritesTheSameExtrasAsBefore(bool scan, bool repeat, bool runRelease, bool sync, bool keep, string expectedExtras)
        {
            var repo = new RecordingRepository();
            var vm = new MacroViewModel(repo);
            vm.PlaybackOptions.UseScanCode = scan;
            vm.PlaybackOptions.RepeatWhileHeld = repeat;
            vm.RunTriggerRelease = runRelease;
            vm.SyncRun = sync;
            vm.KeepKeyState = keep;
            vm.Macro.AddRange(new[] { 350, 256 });

            vm.SaveAction(new SpecialAction("m", "Cross", "Macro", "350/256"));

            Assert.Equal(expectedExtras, repo.SavedExtras);
            Assert.Equal("350/256", repo.SavedDetails);
        }
    }
}
