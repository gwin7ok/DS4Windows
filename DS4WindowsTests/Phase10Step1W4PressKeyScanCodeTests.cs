using System;
using System.Collections.Generic;
using System.Windows;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.DS4Control;
using DS4WinWPF.DS4Forms.ViewModels;
using DS4WinWPF.DS4Forms.ViewModels.SpecialActions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step1 W4（Phase10-Step1-Plan.md §3.3・§5）: SA の Press Key タブが、Scan Code を自分で持つこと
    /// （選択ダイアログのチェックボックスは廃止）。値は、タブ → 設定の箱（<c>PrepareSettings</c>）→ 選択ダイアログ
    /// （<c>BindingWindowViewModel.WriteBinds</c>）→ タブ（<c>ReadSettings</c>）と通っても、保たれる。
    /// </summary>
    public class Phase10Step1W4PressKeyScanCodeTests
    {
        static Phase10Step1W4PressKeyScanCodeTests()
        {
            if (string.IsNullOrEmpty(TestGlobals.AppDataPath))
            {
                TestGlobals.AppDataPath = AppContext.BaseDirectory;
            }

            if (Application.Current == null)
            {
                try { _ = new Application(); } catch { }
            }
        }

        private sealed class RecordingRepository : ISpecialActionRepository
        {
            public int SavedMode;
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
                SavedMode = mode;
                SavedDetails = details;
                SavedExtras = extras;
            }

            public bool RemoveActionAndSave(string actionName) => true;
            public bool SaveActionExtras(string actionName, string outputExtras) => true;
            public bool SaveActionAnalogStrength(string actionName, int strength) => true;
            public event EventHandler ActionsChanged { add { } remove { } }
        }

        private static PressKeyViewModel CreateViewModel(RecordingRepository repo = null)
        {
            return new PressKeyViewModel(repo ?? new RecordingRepository(),
                TestRepositoryFactory.CreateProfileSettingsService(), TestRepositoryFactory.CreateOutputSlotService());
        }

        /// <summary>選択ダイアログを、チェックボックスを触らずに（キー 65 を選んで）通す。</summary>
        private static void ThroughTheDialog(DS4ControlSettings settings, int pickedKey)
        {
            // WriteBind（Key）は、actionAlias の更新（RefreshActionAlias）で OutputKBMMapping を使う。この service だけに設定する
            ProfileSettingsService profileSettings = TestRepositoryFactory.CreateProfileSettingsService();
            profileSettings.OutputKBMMapping = new SendInputMapping();
            var dialogVm = new BindingWindowViewModel(ControllerLimits.TestProfileIndex, settings,
                profileSettings, TestRepositoryFactory.CreateOutputSlotService(),
                new EnvironmentService(), -1);
            dialogVm.CurrentOutBind.outputType = OutBinding.OutType.Key;
            dialogVm.CurrentOutBind.outkey = pickedKey;
            dialogVm.WriteBinds();
        }

        private static SpecialAction KeyAction(string details, string extras = "Press\n")
            => new SpecialAction("k", "Cross", "Key", details, 0, extras);

        [Theory]
        [InlineData("65", false)]
        [InlineData("65 Scan Code", true)]
        public void LoadAction_RestoresScanCode(string details, bool expected)
        {
            PressKeyViewModel vm = CreateViewModel();

            vm.LoadAction(KeyAction(details));

            Assert.Equal(expected, vm.UseScanCode);
        }

        [Theory]
        [InlineData(false, "Press", false, false)]
        [InlineData(true, "Press", true, false)]
        [InlineData(false, "Toggle", false, true)]
        [InlineData(true, "Toggle", true, true)]
        public void PrepareSettings_PassesScanCodeAndToggleFromTheTab(bool scan, string mode, bool expectScan, bool expectToggle)
        {
            PressKeyViewModel vm = CreateViewModel();
            vm.Value = 65;
            vm.SwitchMode = mode;
            vm.UseScanCode = scan;

            DS4ControlSettings settings = vm.PrepareSettings();

            Assert.Equal(expectScan, settings.keyType.HasFlag(DS4KeyType.ScanCode));
            Assert.Equal(expectToggle, settings.keyType.HasFlag(DS4KeyType.Toggle));
            Assert.Equal(DS4ControlSettings.ActionType.Key, settings.actionType);
        }

        [Theory]
        [InlineData(false, "Press")]
        [InlineData(true, "Press")]
        [InlineData(false, "Toggle")]
        [InlineData(true, "Toggle")]
        public void TabToDialogToTab_KeepsScanCodeAndSwitchMode(bool scan, string mode)
        {
            PressKeyViewModel vm = CreateViewModel();
            vm.Value = 65;
            vm.SwitchMode = mode;
            vm.UseScanCode = scan;

            DS4ControlSettings settings = vm.PrepareSettings();
            ThroughTheDialog(settings, pickedKey: 66);   // ダイアログでは、キーだけを選ぶ
            vm.ReadSettings(settings);

            Assert.Equal(scan, vm.UseScanCode);
            Assert.Equal(mode, vm.SwitchMode);
            Assert.Equal(66, vm.Value);
        }

        [Theory]
        [InlineData(false, 4, "65", "Press")]
        [InlineData(true, 4, "65 Scan Code", "Press")]
        [InlineData(true, 4, "65 Scan Code", "Toggle")]
        public void SaveAction_WritesScanCodeInTheDetails(bool scan, int expectedMode, string expectedDetails, string mode)
        {
            var repo = new RecordingRepository();
            PressKeyViewModel vm = CreateViewModel(repo);
            vm.Value = 65;
            vm.SwitchMode = mode;
            vm.UseScanCode = scan;

            vm.SaveAction(new SpecialAction("k", "Cross", "Key", "65"));

            Assert.Equal(expectedMode, repo.SavedMode);
            Assert.Equal(expectedDetails, repo.SavedDetails);
            Assert.StartsWith(mode, repo.SavedExtras);
        }

        [Fact]
        public void SaveAndLoad_RoundTripsScanCode()
        {
            var repo = new RecordingRepository();
            PressKeyViewModel saver = CreateViewModel(repo);
            saver.Value = 65;
            saver.SwitchMode = "Press";
            saver.UseScanCode = true;
            saver.SaveAction(new SpecialAction("k", "Cross", "Key", "65"));

            PressKeyViewModel loader = CreateViewModel();
            loader.LoadAction(new SpecialAction("k", "Cross", "Key", repo.SavedDetails, 0, repo.SavedExtras));

            Assert.True(loader.UseScanCode);
            Assert.Equal(65, loader.Value);
        }

        [Fact]
        public void ChoosingAButton_KeepsTheScanCodeChoice_ButDisablesTheCheckbox()
        {
            PressKeyViewModel vm = CreateViewModel();
            vm.Value = 65;
            vm.UseScanCode = true;
            Assert.True(vm.IsScanCodeEnabled);

            var buttonSettings = new DS4ControlSettings(DS4Controls.None);
            buttonSettings.actionType = DS4ControlSettings.ActionType.Button;
            buttonSettings.action.actionBtn = X360Controls.A;
            vm.ReadSettings(buttonSettings);

            Assert.False(vm.IsScanCodeEnabled);
            Assert.True(vm.UseScanCode);   // キーに戻したとき、選択が残っている
        }

        [Fact]
        public void SettingScanCode_BeforeAnyKeyIsChosen_KeepsTheSelectAKeyPrompt()
        {
            PressKeyViewModel vm = CreateViewModel();
            string before = vm.DescribeText;

            vm.UseScanCode = true;

            Assert.Equal(before, vm.DescribeText);
        }
    }
}
