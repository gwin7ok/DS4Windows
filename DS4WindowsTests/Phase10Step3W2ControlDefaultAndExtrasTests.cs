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
    /// Phase10-Step3 W2（Phase10-Step3-Plan.md §3.1・§3.3・§5）: Controls のモードの割り当てエディターと、出力ピッカーとの受け渡し。
    /// <see cref="PressKeyViewModel"/> の「Default」の状態、ピッカーへ渡す一時設定（ボタン・Extras・いまの割り当て）、
    /// <see cref="ControlSettingsEditTarget"/> の Default の書き出し・読み込みと、Extras の取り込み（Save で反映、Cancel で破棄）。
    /// SA のモード（<c>allowDefault</c> が既定の <c>false</c>、<c>PrepareSettings()</c> が引数なし）は、従来どおりであること。
    /// </summary>
    public class Phase10Step3W2ControlDefaultAndExtrasTests
    {
        private const string SomeExtras = "10,20,1,255,0,0,1,0,25";
        private const string PickerExtras = "1,2,0,0,0,0,0,1,40";
        private const int KeyIndex = 4;   // ActionTypeCatalog の Press Key（Output Key/Mouse/Button）の添字（W4 のテストと同じ）

        private readonly ITestOutputHelper _output;

        static Phase10Step3W2ControlDefaultAndExtrasTests()
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

        public Phase10Step3W2ControlDefaultAndExtrasTests(ITestOutputHelper output)
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

        private static DS4ControlSettings Settings(DS4ControlSettings.ActionType type, string extras = SomeExtras)
            => new DS4ControlSettings(DS4Controls.Circle) { actionType = type, extras = extras };

        // ---- PressKeyViewModel: Default の状態 ----

        [Fact]
        public void ReadSettings_Default_WithAllowDefault_SelectsDefault()
        {
            var rig = new Rig();

            rig.PressKey.ReadSettings(Settings(DS4ControlSettings.ActionType.Default), allowDefault: true);

            Assert.Equal(DS4ControlSettings.ActionType.Default, rig.PressKey.SelectedActionType);
            Assert.StartsWith(PressKeyViewModel.DefaultText, rig.PressKey.DescribeText);   // Phase9 UI 変更: 「Default (既定の出力名)」
            Assert.False(rig.PressKey.IsScanCodeEnabled);
            Assert.Equal(0, rig.PressKey.Value);
        }

        [Fact]
        public void ReadSettings_Default_DescribesTheButtonsDefaultOutput()
        {
            var rig = new Rig();

            rig.PressKey.ReadSettings(Settings(DS4ControlSettings.ActionType.Default), allowDefault: true);

            // Phase9 UI 変更: 「Default (既定の出力名)」（Circle の既定の出力は B）
            Assert.StartsWith(PressKeyViewModel.DefaultText + " (", rig.PressKey.DescribeText);
            Assert.EndsWith(")", rig.PressKey.DescribeText);
        }

        [Fact]
        public void ReadSettings_Default_WithoutAllowDefault_StaysAKey_AsBefore()
        {
            // SA のモード: 「Default」はない。従来どおり、キーの割り当てとして扱う
            var rig = new Rig();

            rig.PressKey.ReadSettings(Settings(DS4ControlSettings.ActionType.Default));

            Assert.Equal(DS4ControlSettings.ActionType.Key, rig.PressKey.SelectedActionType);
            Assert.True(rig.PressKey.IsScanCodeEnabled);
        }

        [Fact]
        public void ReadSettings_AKey_AfterDefault_LeavesTheDefaultState()
        {
            var rig = new Rig();
            rig.PressKey.ReadSettings(Settings(DS4ControlSettings.ActionType.Default), allowDefault: true);
            DS4ControlSettings key = Settings(DS4ControlSettings.ActionType.Key);
            key.action.actionKey = 65;

            rig.PressKey.ReadSettings(key, allowDefault: true);

            Assert.Equal(DS4ControlSettings.ActionType.Key, rig.PressKey.SelectedActionType);
            Assert.Equal(65, rig.PressKey.Value);
            Assert.True(rig.PressKey.IsScanCodeEnabled);
        }

        // ---- PressKeyViewModel: ピッカーへ渡す一時設定 ----

        [Fact]
        public void PrepareSettings_ForAControl_CarriesTheButtonAndTheExtras()
        {
            var rig = new Rig();
            rig.PressKey.Value = 65;

            DS4ControlSettings s = rig.PressKey.PrepareSettings(DS4Controls.Circle, SomeExtras);

            Assert.Equal(DS4Controls.Circle, s.control);
            Assert.Equal(SomeExtras, s.extras);
            Assert.Equal(DS4ControlSettings.ActionType.Key, s.actionType);
            Assert.Equal(65, s.action.actionKey);
        }

        [Fact]
        public void PrepareSettings_ForAControl_PassesTheCurrentButton_WithoutToggle()
        {
            var rig = new Rig();
            DS4ControlSettings button = Settings(DS4ControlSettings.ActionType.Button);
            button.action.actionBtn = X360Controls.A;
            rig.PressKey.ReadSettings(button);
            rig.PressKey.SwitchMode = "Toggle";

            DS4ControlSettings s = rig.PressKey.PrepareSettings(DS4Controls.Circle, SomeExtras);

            Assert.Equal(DS4ControlSettings.ActionType.Button, s.actionType);
            Assert.Equal(X360Controls.A, s.action.actionBtn);
            Assert.False(s.keyType.HasFlag(DS4KeyType.Toggle));
        }

        [Fact]
        public void PrepareSettings_ForAControl_PassesTheDefaultState()
        {
            var rig = new Rig();
            rig.PressKey.ReadSettings(Settings(DS4ControlSettings.ActionType.Default), allowDefault: true);

            DS4ControlSettings s = rig.PressKey.PrepareSettings(DS4Controls.Circle, SomeExtras);

            Assert.Equal(DS4ControlSettings.ActionType.Default, s.actionType);
            Assert.Equal(DS4KeyType.None, s.keyType);
        }

        [Fact]
        public void PrepareSettings_WithoutAControl_IsAsBefore_ForTheSpecialActionMode()
        {
            var rig = new Rig();
            DS4ControlSettings button = Settings(DS4ControlSettings.ActionType.Button);
            button.action.actionBtn = X360Controls.A;
            rig.PressKey.ReadSettings(button);

            DS4ControlSettings s = rig.PressKey.PrepareSettings();

            Assert.Equal(DS4Controls.None, s.control);
            Assert.Equal(DS4ControlSettings.ActionType.Key, s.actionType);
            Assert.Equal(string.Empty, s.extras ?? string.Empty);
        }

        // ---- ControlSettingsEditTarget: Default ----

        [Fact]
        public void LoadInto_ADefaultAction_SelectsDefaultInThePressKeyInput()
        {
            var rig = new Rig();
            var target = new ControlSettingsEditTarget(Settings(DS4ControlSettings.ActionType.Default), rig.Profile);

            target.LoadInto(rig.PressKey, rig.Macro);

            Assert.Equal(DS4ControlSettings.ActionType.Default, rig.PressKey.SelectedActionType);
        }

        [Fact]
        public void WriteFrom_PressKeyInTheDefaultState_WritesTheDefault_EvenWithToggleAndScanCode()
        {
            var rig = new Rig();
            DS4ControlSettings original = Settings(DS4ControlSettings.ActionType.Key);
            original.action.actionKey = 65;
            original.keyType = DS4KeyType.Toggle | DS4KeyType.ScanCode;
            var target = new ControlSettingsEditTarget(original, rig.Profile);
            rig.PressKey.ReadSettings(Settings(DS4ControlSettings.ActionType.Default), allowDefault: true);
            rig.PressKey.SwitchMode = "Toggle";
            rig.PressKey.UseScanCode = true;

            target.WriteFrom(KeyIndex, rig.PressKey, rig.Macro);

            Assert.Equal(DS4ControlSettings.ActionType.Default, target.Working.actionType);
            Assert.Equal(DS4KeyType.None, target.Working.keyType);
            Assert.Equal(0, target.Working.action.actionKey);
        }

        // ---- ControlSettingsEditTarget: Extras ----

        [Fact]
        public void Extras_StartsAsTheOriginalsExtras_AndIsNotChangedByWriteFrom()
        {
            var rig = new Rig();
            var target = new ControlSettingsEditTarget(Settings(DS4ControlSettings.ActionType.Default), rig.Profile);
            rig.PressKey.Value = 65;

            target.WriteFrom(KeyIndex, rig.PressKey, rig.Macro);

            Assert.Equal(SomeExtras, target.Extras);
        }

        [Fact]
        public void Extras_Set_IsAppliedOnSave_AndDiscardedOnCancel()
        {
            var rig = new Rig();
            DS4ControlSettings original = Settings(DS4ControlSettings.ActionType.Default);
            var target = new ControlSettingsEditTarget(original, rig.Profile);

            target.Extras = PickerExtras;
            Assert.Equal(SomeExtras, original.extras);   // Save までは、元は変わらない

            target.Discard();
            Assert.Equal(SomeExtras, target.Extras);
            Assert.Equal(SomeExtras, original.extras);

            target.Extras = PickerExtras;
            target.Apply();
            Assert.Equal(PickerExtras, original.extras);
        }

        [Fact]
        public void Extras_SetToNull_BecomesEmpty()
        {
            var rig = new Rig();
            var target = new ControlSettingsEditTarget(Settings(DS4ControlSettings.ActionType.Default), rig.Profile);

            target.Extras = null;

            Assert.Equal(string.Empty, target.Extras);
        }

        // ---- ピッカーを経由した流れ（ピッカーが書く内容を、同じ形で再現）----

        [Fact]
        public void APickerResult_Button_WithExtras_IsAppliedOnlyOnSave()
        {
            var rig = new Rig();
            DS4ControlSettings original = Settings(DS4ControlSettings.ActionType.Default);
            var target = new ControlSettingsEditTarget(original, rig.Profile);
            target.LoadInto(rig.PressKey, rig.Macro);

            // 割り当てエディター: ピッカーへ渡す → ピッカーが、一時設定へ書く（Button と Extras）
            DS4ControlSettings fromPicker = rig.PressKey.PrepareSettings(target.Control, target.Extras);
            fromPicker.actionType = DS4ControlSettings.ActionType.Button;
            fromPicker.action.actionBtn = X360Controls.B;
            fromPicker.extras = PickerExtras;

            // 戻ったあとの取り込み
            rig.PressKey.ReadSettings(fromPicker, allowDefault: true);
            target.Extras = fromPicker.extras;
            target.WriteFrom(KeyIndex, rig.PressKey, rig.Macro);

            Assert.Equal(DS4ControlSettings.ActionType.Default, original.actionType);   // Save 前: 元は変わらない
            Assert.Equal(SomeExtras, original.extras);

            target.Apply();
            Assert.Equal(DS4ControlSettings.ActionType.Button, original.actionType);
            Assert.Equal(X360Controls.B, original.action.actionBtn);
            Assert.Equal(PickerExtras, original.extras);
        }

        [Fact]
        public void APickerResult_Unbound_IsWrittenAsAnUnboundButton()
        {
            var rig = new Rig();
            DS4ControlSettings original = Settings(DS4ControlSettings.ActionType.Default);
            var target = new ControlSettingsEditTarget(original, rig.Profile);
            DS4ControlSettings fromPicker = rig.PressKey.PrepareSettings(target.Control, target.Extras);
            fromPicker.actionType = DS4ControlSettings.ActionType.Button;
            fromPicker.action.actionBtn = X360Controls.Unbound;
            fromPicker.keyType = DS4KeyType.Unbound;

            rig.PressKey.ReadSettings(fromPicker, allowDefault: true);
            target.WriteFrom(KeyIndex, rig.PressKey, rig.Macro);
            target.Apply();

            Assert.Equal(DS4ControlSettings.ActionType.Button, original.actionType);
            Assert.Equal(X360Controls.Unbound, original.action.actionBtn);
            Assert.Equal(DS4KeyType.Unbound, original.keyType);
        }

        // ---- 割り当てエディターの呼び出し（ソース走査）----

        [Fact]
        public void TheEditor_OpensThePickerInTheControlMode_WithTheButtonAndTheExtras()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Forms/AssignmentEditor.xaml.cs");
            if (path == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            string source = File.ReadAllText(path);
            Assert.Contains("pressKeyVM.PrepareSettings(controlTarget.Control, controlTarget.Extras)", source);
            Assert.Contains("BindingWindow.ExposeMode.Control", source);
            Assert.Contains("allowDefault: true", source);
            Assert.Contains("controlTarget.Extras = controlSettings.extras", source);
            // SA のモードは、従来どおり
            Assert.Contains("BindingWindow.ExposeMode.SpecialAction", source);
        }
    }
}
