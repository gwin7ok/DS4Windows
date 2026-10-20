using System;
using System.IO;
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
    /// 0 は、Controls では「Default」と読む（既定の割り当て）。末尾（添字 10）に「Unbound」の項目を足す（Button の Unbound）。SA のモードの一覧は、従来のまま。
    /// 左ペインのタブを隠す表示（SA の編集画面も、Controls の割り当てエディターと同じ場所に重ねる）のソース走査も含む。
    /// </summary>
    public class Phase10Step3W4DefaultUnboundTypeTests
    {
        private const string SomeExtras = "10,20,1,255,0,0,1,0,25";
        private const int KeyIndex = 4;
        private const int UnboundIndex = 10;

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
            Assert.Equal(-1, catalog.UnboundIndex);
        }

        [Fact]
        public void TheControlCatalog_AddsAnUnboundItemAtTheEnd()
        {
            var catalog = new ActionTypeCatalog(includeControlOnlyItems: true);

            Assert.Equal(11, catalog.All.Count);
            Assert.Equal(UnboundIndex, catalog.UnboundIndex);
            ActionTypeDescriptor unbound = catalog.At(UnboundIndex);
            Assert.Equal(SpecialAction.ActionTypeId.Button, unbound.TypeId);
            Assert.True(unbound.IsAvailableIn(ActionEditorModes.Control));
            Assert.True(unbound.IsSavableIn(ActionEditorModes.Control));
            Assert.False(unbound.IsAvailableIn(ActionEditorModes.SpecialAction));
            Assert.False(unbound.IsSavableIn(ActionEditorModes.SpecialAction));
        }

        [Fact]
        public void TheControlCatalog_StillMapsButtonToThePressKeyItem()
        {
            var catalog = new ActionTypeCatalog(includeControlOnlyItems: true);

            Assert.Equal(KeyIndex, catalog.IndexOf(SpecialAction.ActionTypeId.Button));
            Assert.Equal(KeyIndex, catalog.IndexOf(SpecialAction.ActionTypeId.Key));
        }

        [Fact]
        public void TheControlEditorViewModel_EnablesDefaultAndUnbound()
        {
            var rig = new Rig();
            var target = new ControlSettingsEditTarget(Settings(DS4ControlSettings.ActionType.Default), rig.Profile);
            var vm = new ControlActionEditorViewModel(DS4Windows.ControllerLimits.TestProfileIndex, target);

            Assert.True(vm.IsTypeEnabled(0));
            Assert.True(vm.IsTypeEnabled(UnboundIndex));
            Assert.Equal(11, vm.TypeAssoc.Length);
        }

        // ---- 割り当ての読み込み・書き出し ----

        [Fact]
        public void InitialTypeIndex_ADefault_IsZero_AnUnboundButton_IsTheUnboundItem()
        {
            var rig = new Rig();

            Assert.Equal(0, new ControlSettingsEditTarget(Settings(DS4ControlSettings.ActionType.Default), rig.Profile).InitialTypeIndex);

            DS4ControlSettings unbound = Settings(DS4ControlSettings.ActionType.Button, DS4KeyType.Unbound);
            unbound.action.actionBtn = X360Controls.Unbound;
            Assert.Equal(UnboundIndex, new ControlSettingsEditTarget(unbound, rig.Profile).InitialTypeIndex);

            DS4ControlSettings button = Settings(DS4ControlSettings.ActionType.Button);
            button.action.actionBtn = X360Controls.A;
            Assert.Equal(KeyIndex, new ControlSettingsEditTarget(button, rig.Profile).InitialTypeIndex);

            Assert.Equal(KeyIndex, new ControlSettingsEditTarget(Settings(DS4ControlSettings.ActionType.Key), rig.Profile).InitialTypeIndex);
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
        public void WriteFrom_ZeroInTheControlMode_WritesTheDefault_NotAnError()
        {
            var rig = new Rig();
            DS4ControlSettings original = Settings(DS4ControlSettings.ActionType.Key);
            original.action.actionKey = 65;
            var target = new ControlSettingsEditTarget(original, rig.Profile);

            target.WriteFrom(0, rig.PressKey, rig.Macro);
            target.Apply();

            Assert.Equal(DS4ControlSettings.ActionType.Default, original.actionType);
            Assert.Equal(DS4KeyType.None, original.keyType);
        }

        [Fact]
        public void TheUnboundItem_RoundTrips_ThroughSaveAndReopen()
        {
            var rig = new Rig();
            DS4ControlSettings original = Settings(DS4ControlSettings.ActionType.Default);
            var first = new ControlSettingsEditTarget(original, rig.Profile);
            first.WriteFrom(UnboundIndex, rig.PressKey, rig.Macro);
            first.Apply();

            var reopened = new ControlSettingsEditTarget(original, rig.Profile);

            Assert.Equal(UnboundIndex, reopened.InitialTypeIndex);
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
        public void TheEditor_ShowsDefaultAndUnbound_OnlyInTheControlMode()
        {
            string xaml = Read("DS4Windows/DS4Forms/SpecialActionEditor.xaml");
            string source = Read("DS4Windows/DS4Forms/SpecialActionEditor.xaml.cs");
            if (xaml == null || source == null)
            {
                return;
            }

            Assert.Contains("x:Name=\"unboundTypeItem\"", xaml);
            Assert.Contains("x:Name=\"unboundActTab\"", xaml);

            // Controls のモード: 0 を「Default」と読む。SA のモード: 「Unbound」を隠す
            Assert.Contains("((ComboBoxItem)actionTypeCombo.Items[0]).Content = DS4WinWPF.Translations.Strings.Default;", source);
            Assert.Contains("unboundTypeItem.Visibility = Visibility.Collapsed;", source);
            Assert.Contains("unboundActTab.Visibility = Visibility.Collapsed;", source);

            // Controls のモードの 0（Default）は、保存できる
            Assert.Contains("specialActVM.ActionTypeIndex <= 0 && !controlMode", source);

            // 出力ピッカーで Default／Unbound を選んだら、一覧の項目も切り替える
            Assert.Contains("actionTypeCombo.SelectedIndex = 0;", source);
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
            Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(source, @"OpenLeftPaneEditor\(actEditor\);").Count);
            // SA の保存・キャンセル（新規と編集で 4）、Controls の保存・キャンセル（2）
            Assert.Equal(6, System.Text.RegularExpressions.Regex.Matches(source, @"CloseLeftPaneEditor\(\);").Count);
        }
    }
}
