using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using DS4Windows;
using DS4Windows.DS4Control;
using DS4WinWPF.DS4Forms.ViewModels;
using DS4WinWPF.DS4Forms.ViewModels.SpecialActions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step3 W4（実機確認での追加要望）: 割り当てエディターの種別の一覧（Controls のモード）で、出力ピッカーの Default／Unbound ボタンと同じ設定を選べる。
    /// Controls のモードだけの 2 項目（添字 10＝Default、11＝Unbound）を末尾に足す。モードごとの入力の制限は、一覧の項目の表示・非表示だけで行う
    /// （Controls のモードでは、添字 0 の「種別を選んでください」を隠し、SA のモードでは、足した 2 項目を隠す。検証・保存ボタンは、モードを区別しない）。
    /// SA のモードの一覧は、従来のまま。左ペインのタブを隠す表示（SA の編集画面も、Controls の割り当てエディターと同じ場所に重ねる）のソース走査も含む。
    /// </summary>
    public class Phase10Step3W4DefaultUnboundTypeTests
    {
        private const string SomeExtras = "10,20,1,255,0,0,1,0,25";
        private const int KeyIndex = 4;
        private const int DefaultIndex = 10;
        private const int UnboundIndex = 11;

        private readonly ITestOutputHelper _output;

        static Phase10Step3W4DefaultUnboundTypeTests()
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

        public Phase10Step3W4DefaultUnboundTypeTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private sealed class Rig
        {
            public readonly ProfileSettingsService Profile;
            public readonly PressKeyViewModel PressKey;
            public readonly MacroViewModel Macro;

            public Rig()
            {
                Profile = TestRepositoryFactory.CreateProfileSettingsService();
                Profile.OutputKBMMapping = new SendInputMapping();
                var repo = new RecordingSpecialActionRepository();
                PressKey = new PressKeyViewModel(repo, Profile, TestRepositoryFactory.CreateOutputSlotService());
                Macro = new MacroViewModel(repo);
            }
        }

        private static DS4ControlSettings Settings(DS4ControlSettings.ActionType type, DS4KeyType keyType = DS4KeyType.None)
            => new DS4ControlSettings(DS4Controls.Circle) { actionType = type, keyType = keyType, extras = SomeExtras };

        // ---- 種別の一覧 ----

        [Fact]
        public void TheDefaultCatalog_IsUnchanged_ForTheSpecialActionMode()
        {
            var catalog = new ActionTypeCatalog();

            Assert.Equal(10, catalog.All.Count);
            Assert.Equal(-1, catalog.DefaultIndex);
            Assert.Equal(-1, catalog.UnboundIndex);
        }

        [Fact]
        public void TheControlCatalog_AddsDefaultAndUnboundAtTheEnd()
        {
            var catalog = new ActionTypeCatalog(includeControlOnlyItems: true);

            Assert.Equal(12, catalog.All.Count);
            Assert.Equal(DefaultIndex, catalog.DefaultIndex);
            Assert.Equal(UnboundIndex, catalog.UnboundIndex);

            foreach (int index in new[] { DefaultIndex, UnboundIndex })
            {
                ActionTypeDescriptor item = catalog.At(index);
                Assert.True(item.IsAvailableIn(ActionEditorModes.Control));
                Assert.True(item.IsSavableIn(ActionEditorModes.Control));
                Assert.False(item.IsAvailableIn(ActionEditorModes.SpecialAction));
                Assert.False(item.IsSavableIn(ActionEditorModes.SpecialAction));
            }

            Assert.Equal(SpecialAction.ActionTypeId.None, catalog.At(DefaultIndex).TypeId);
            Assert.Equal(SpecialAction.ActionTypeId.Button, catalog.At(UnboundIndex).TypeId);
        }

        [Fact]
        public void TheControlCatalog_StillMapsTheOriginalTypesToTheOriginalItems()
        {
            var catalog = new ActionTypeCatalog(includeControlOnlyItems: true);

            Assert.Equal(KeyIndex, catalog.IndexOf(SpecialAction.ActionTypeId.Button));
            Assert.Equal(KeyIndex, catalog.IndexOf(SpecialAction.ActionTypeId.Key));
            Assert.Equal(0, catalog.IndexOf(SpecialAction.ActionTypeId.None));
        }

        [Fact]
        public void TheControlEditorViewModel_EnablesDefaultAndUnbound()
        {
            var rig = new Rig();
            var target = new ControlSettingsEditTarget(Settings(DS4ControlSettings.ActionType.Default), rig.Profile);
            var vm = new ControlActionEditorViewModel(DS4Windows.ControllerLimits.TestProfileIndex, target);

            Assert.True(vm.IsTypeEnabled(DefaultIndex));
            Assert.True(vm.IsTypeEnabled(UnboundIndex));
            Assert.Equal(12, vm.TypeAssoc.Length);
        }

        // ---- 割り当ての読み込み・書き出し ----

        [Fact]
        public void InitialTypeIndex_NeverIsZero_InTheControlMode()
        {
            var rig = new Rig();

            Assert.Equal(DefaultIndex, new ControlSettingsEditTarget(Settings(DS4ControlSettings.ActionType.Default), rig.Profile).InitialTypeIndex);

            DS4ControlSettings unbound = Settings(DS4ControlSettings.ActionType.Button, DS4KeyType.Unbound);
            unbound.action.actionBtn = X360Controls.Unbound;
            Assert.Equal(UnboundIndex, new ControlSettingsEditTarget(unbound, rig.Profile).InitialTypeIndex);

            DS4ControlSettings button = Settings(DS4ControlSettings.ActionType.Button);
            button.action.actionBtn = X360Controls.A;
            Assert.Equal(KeyIndex, new ControlSettingsEditTarget(button, rig.Profile).InitialTypeIndex);

            Assert.Equal(KeyIndex, new ControlSettingsEditTarget(Settings(DS4ControlSettings.ActionType.Key), rig.Profile).InitialTypeIndex);
        }

        [Fact]
        public void WriteFrom_TheDefaultItem_WritesTheDefault_EvenWithToggleAndScanCodeSet()
        {
            var rig = new Rig();
            DS4ControlSettings original = Settings(DS4ControlSettings.ActionType.Key, DS4KeyType.Toggle | DS4KeyType.ScanCode);
            original.action.actionKey = 65;
            var target = new ControlSettingsEditTarget(original, rig.Profile);
            rig.PressKey.Value = 65;
            rig.PressKey.SwitchMode = "Toggle";
            rig.PressKey.UseScanCode = true;

            target.WriteFrom(DefaultIndex, rig.PressKey, rig.Macro);
            target.Apply();

            Assert.Equal(DS4ControlSettings.ActionType.Default, original.actionType);
            Assert.Equal(DS4KeyType.None, original.keyType);
            Assert.Equal(0, original.action.actionKey);
            Assert.Equal(SomeExtras, original.extras);
        }

        [Fact]
        public void WriteFrom_TheUnboundItem_WritesAnUnboundButton_AndClearsToggleAndScanCode()
        {
            var rig = new Rig();
            DS4ControlSettings original = Settings(DS4ControlSettings.ActionType.Key, DS4KeyType.Toggle | DS4KeyType.ScanCode);
            original.action.actionKey = 65;
            var target = new ControlSettingsEditTarget(original, rig.Profile);
            rig.PressKey.Value = 65;
            rig.PressKey.SwitchMode = "Toggle";
            rig.PressKey.UseScanCode = true;

            target.WriteFrom(UnboundIndex, rig.PressKey, rig.Macro);
            target.Apply();

            Assert.Equal(DS4ControlSettings.ActionType.Button, original.actionType);
            Assert.Equal(X360Controls.Unbound, original.action.actionBtn);
            Assert.Equal(DS4KeyType.Unbound, original.keyType);
            Assert.Equal(SomeExtras, original.extras);
        }

        [Fact]
        public void TheDefaultAndUnboundItems_RoundTrip_ThroughSaveAndReopen()
        {
            var rig = new Rig();
            DS4ControlSettings original = Settings(DS4ControlSettings.ActionType.Key);
            original.action.actionKey = 65;

            var toUnbound = new ControlSettingsEditTarget(original, rig.Profile);
            toUnbound.WriteFrom(UnboundIndex, rig.PressKey, rig.Macro);
            toUnbound.Apply();
            Assert.Equal(UnboundIndex, new ControlSettingsEditTarget(original, rig.Profile).InitialTypeIndex);

            var toDefault = new ControlSettingsEditTarget(original, rig.Profile);
            toDefault.WriteFrom(DefaultIndex, rig.PressKey, rig.Macro);
            toDefault.Apply();
            Assert.Equal(DefaultIndex, new ControlSettingsEditTarget(original, rig.Profile).InitialTypeIndex);
        }

        // ---- 画面（ソース走査）----

        private string Read(string relative)
        {
            string path = SourceFileLocator.Find(relative);
            if (path == null)
            {
                _output.WriteLine(relative + " が見つからないため検査を省略した。");
                return null;
            }

            return File.ReadAllText(path);
        }

        [Fact]
        public void TheModesLimitTheInput_OnlyByShowingOrHidingTheDropdownItems()
        {
            string xaml = Read("DS4Windows/DS4Forms/SpecialActionEditor.xaml");
            string source = Read("DS4Windows/DS4Forms/SpecialActionEditor.xaml.cs");
            string editorVm = Read("DS4Windows/DS4Forms/ViewModels/ActionEditorViewModel.cs");
            if (xaml == null || source == null || editorVm == null)
            {
                return;
            }

            Assert.Contains("x:Name=\"defaultTypeItem\"", xaml);
            Assert.Contains("x:Name=\"unboundTypeItem\"", xaml);
            Assert.Contains("x:Name=\"defaultActTab\"", xaml);
            Assert.Contains("x:Name=\"unboundActTab\"", xaml);

            // Controls のモード: 「種別を選んでください」（0）を隠す。SA のモード: 足した 2 項目を隠す
            Assert.Contains("((ComboBoxItem)actionTypeCombo.Items[0]).Visibility = Visibility.Collapsed;", source);
            Assert.Contains("defaultTypeItem.Visibility = Visibility.Collapsed;", source);
            Assert.Contains("unboundTypeItem.Visibility = Visibility.Collapsed;", source);

            // 検証と保存ボタンは、モードを区別しない（制御が 2 か所に分散しない）
            Assert.Contains("if (ActionTypeIndex == 0)", editorVm);
            Assert.DoesNotContain("IndexZeroMeansDefault", editorVm);
            Assert.DoesNotContain("IndexZeroMeansDefault", source);
            Assert.Contains("if (specialActVM.ActionTypeIndex <= 0)", source);

            // 出力ピッカーで Default／Unbound を選んだら、一覧の項目も切り替える
            Assert.Contains("actionTypeCombo.SelectedIndex = specialActVM.TypeCatalog.DefaultIndex;", source);
            Assert.Contains("actionTypeCombo.SelectedIndex = specialActVM.TypeCatalog.UnboundIndex;", source);
        }

        [Fact]
        public void TheSpecialActionEditorFromTheSaTab_AlsoCoversTheLeftPane()
        {
            string source = Read("DS4Windows/DS4Forms/ProfileEditor.xaml.cs");
            if (source == null)
            {
                return;
            }

            // SA の新規・編集は、Controls の割り当てエディターと同じ場所（左ペイン全体）へ重ねる。タブを隠すため、編集中は、左ペインのタブを切り替えられない
            Assert.DoesNotContain("specialActionDockPanel.Children.Add(", source);
            Assert.DoesNotContain("specialActionDockPanel.Children.Remove(", source);
            Assert.Equal(2, Regex.Matches(source, @"OpenLeftPaneEditor\(actEditor\);").Count);
            // SA の保存・キャンセル（新規と編集で 4）、Controls の保存・キャンセル（2）
            Assert.Equal(6, Regex.Matches(source, @"CloseLeftPaneEditor\(\);").Count);
        }
    }
}
