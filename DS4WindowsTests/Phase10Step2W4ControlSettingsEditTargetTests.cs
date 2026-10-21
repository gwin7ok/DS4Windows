using System;
using System.Windows;
using DS4Windows;
using DS4Windows.DS4Control;
using DS4WinWPF.DS4Forms.ViewModels;
using DS4WinWPF.DS4Forms.ViewModels.SpecialActions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step2 W4（Phase10-Step2-Plan.md §3.3・§5）: Controls のモードの、設定（<see cref="DS4ControlSettings"/>）との変換と、編集用のコピー
    /// （<see cref="ControlSettingsEditTarget"/>）。Default／Key／Button／Macro の、読み込み・書き出し・往復、Toggle・Scan Code・Repeat、
    /// <c>extras</c> の保持、Save（Apply）／Cancel（Discard）で、元が変わる・変わらないこと。持ち越し C-5（Button の Toggle）の是正の土台。
    /// </summary>
    public class Phase10Step2W4ControlSettingsEditTargetTests
    {
        private const string SomeExtras = "10,20,1,255,0,0,1,0,25";

        static Phase10Step2W4ControlSettingsEditTargetTests()
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

        private sealed class Rig
        {
            public readonly ProfileSettingsService Profile;
            public readonly RecordingSpecialActionRepository Repo = new RecordingSpecialActionRepository();
            public readonly PressKeyViewModel PressKey;
            public readonly MacroViewModel Macro;

            public Rig()
            {
                Profile = TestRepositoryFactory.CreateProfileSettingsService();
                Profile.OutputKBMMapping = new SendInputMapping();   // Key の書き出しは、actionAlias の更新で使う（この service だけ）
                PressKey = new PressKeyViewModel(Repo, Profile, TestRepositoryFactory.CreateOutputSlotService());
                Macro = new MacroViewModel(Repo);
            }
        }

        private static DS4ControlSettings Default() => new DS4ControlSettings(DS4Controls.Circle) { extras = SomeExtras };

        private static DS4ControlSettings Key(int key, DS4KeyType keyType = DS4KeyType.None)
        {
            var s = new DS4ControlSettings(DS4Controls.Circle) { actionType = DS4ControlSettings.ActionType.Key, keyType = keyType, extras = SomeExtras };
            s.action.actionKey = key;
            return s;
        }

        private static DS4ControlSettings Button(X360Controls button, DS4KeyType keyType = DS4KeyType.None)
        {
            var s = new DS4ControlSettings(DS4Controls.Circle) { actionType = DS4ControlSettings.ActionType.Button, keyType = keyType, extras = SomeExtras };
            s.action.actionBtn = button;
            return s;
        }

        private static DS4ControlSettings Macro(int[] steps, DS4KeyType keyType = DS4KeyType.Macro)
        {
            var s = new DS4ControlSettings(DS4Controls.Circle) { actionType = DS4ControlSettings.ActionType.Macro, keyType = keyType, extras = SomeExtras };
            s.action.actionMacro = steps;
            return s;
        }

        private static void AssertSameAssignment(DS4ControlSettings expected, DS4ControlSettings actual)
        {
            Assert.Equal(expected.actionType, actual.actionType);
            Assert.Equal(expected.keyType, actual.keyType);
            Assert.Equal(expected.action.actionKey, actual.action.actionKey);
            Assert.Equal(expected.action.actionBtn, actual.action.actionBtn);
            Assert.Equal(expected.action.actionMacro, actual.action.actionMacro);
            Assert.Equal(expected.extras, actual.extras);
        }

        // ---- 初期の種別 ----

        [Fact]
        public void InitialTypeIndex_FollowsTheAssignment()
        {
            var rig = new Rig();

            Assert.Equal(9, new ControlSettingsEditTarget(Default(), rig.Profile).InitialTypeIndex);   // Phase10-Step3: 「Default」の項目（添字 0 は、Controls のモードでは隠す）
            Assert.Equal(4, new ControlSettingsEditTarget(Key(65), rig.Profile).InitialTypeIndex);
            Assert.Equal(4, new ControlSettingsEditTarget(Button(X360Controls.A), rig.Profile).InitialTypeIndex);   // Button も Press Key の項目
            Assert.Equal(1, new ControlSettingsEditTarget(Macro(new[] { 350, 256 }), rig.Profile).InitialTypeIndex);
        }

        // ---- 読み込み ----

        [Fact]
        public void LoadInto_Key_FillsThePressKeyEditor_WithTheToggleAndTheScanCode()
        {
            var rig = new Rig();
            var target = new ControlSettingsEditTarget(Key(65, DS4KeyType.Toggle | DS4KeyType.ScanCode), rig.Profile);

            target.LoadInto(rig.PressKey, rig.Macro);

            Assert.Equal(DS4ControlSettings.ActionType.Key, rig.PressKey.SelectedActionType);
            Assert.Equal(65, rig.PressKey.Value);
            Assert.Equal("Toggle", rig.PressKey.SwitchMode);
            Assert.True(rig.PressKey.UseScanCode);
        }

        [Theory]
        [InlineData(DS4KeyType.None, "Press")]
        [InlineData(DS4KeyType.Toggle, "Toggle")]
        public void LoadInto_Button_UsesTheSavedToggle_UnlikeTheSpecialActionDialog(DS4KeyType keyType, string expectedMode)
        {
            var rig = new Rig();
            var target = new ControlSettingsEditTarget(Button(X360Controls.A, keyType), rig.Profile);

            target.LoadInto(rig.PressKey, rig.Macro);

            Assert.Equal(DS4ControlSettings.ActionType.Button, rig.PressKey.SelectedActionType);
            Assert.Equal((int)X360Controls.A, rig.PressKey.SelectedButton);
            Assert.Equal(expectedMode, rig.PressKey.SwitchMode);
        }

        [Fact]
        public void LoadInto_Macro_FillsTheMacroEditor_WithTheStepsAndTheOptions()
        {
            var rig = new Rig();
            var target = new ControlSettingsEditTarget(
                Macro(new[] { 350, 256, 310, 256 }, DS4KeyType.Macro | DS4KeyType.RepeatMacro | DS4KeyType.ScanCode), rig.Profile);

            target.LoadInto(rig.PressKey, rig.Macro);

            Assert.Equal(new[] { 350, 256, 310, 256 }, rig.Macro.Macro);
            Assert.True(rig.Macro.PlaybackOptions.RepeatWhileHeld);
            Assert.True(rig.Macro.PlaybackOptions.UseScanCode);
        }

        // ---- 書き出し ----

        [Fact]
        public void WriteFrom_Key_WritesTheKeyTheScanCodeAndTheToggle_AndRefreshesTheAlias()
        {
            var rig = new Rig();
            var target = new ControlSettingsEditTarget(Default(), rig.Profile);
            rig.PressKey.Value = 66;
            rig.PressKey.SwitchMode = "Toggle";
            rig.PressKey.UseScanCode = true;

            target.WriteFrom(4, rig.PressKey, rig.Macro);

            Assert.Equal(DS4ControlSettings.ActionType.Key, target.Working.actionType);
            Assert.Equal(66, target.Working.action.actionKey);
            Assert.Equal(DS4KeyType.ScanCode | DS4KeyType.Toggle, target.Working.keyType);
            Assert.Equal(rig.Profile.OutputKBMMapping.GetRealEventKey(66), target.Working.action.actionAlias);
        }

        [Fact]
        public void WriteFrom_Button_WritesTheToggle_TheFixForC5()
        {
            // 従来の選択ダイアログ（OutBinding.WriteBind）は、Button に Toggle を書かなかった（Phase10Step2W0ButtonToggleReproTests）。共通の編集画面は、書く
            var rig = new Rig();
            var target = new ControlSettingsEditTarget(Button(X360Controls.A), rig.Profile);
            target.LoadInto(rig.PressKey, rig.Macro);
            rig.PressKey.SwitchMode = "Toggle";

            target.WriteFrom(4, rig.PressKey, rig.Macro);

            Assert.Equal(DS4ControlSettings.ActionType.Button, target.Working.actionType);
            Assert.Equal(X360Controls.A, target.Working.action.actionBtn);
            Assert.Equal(DS4KeyType.Toggle, target.Working.keyType);
        }

        [Fact]
        public void WriteFrom_ButtonPress_WritesNoToggle()
        {
            var rig = new Rig();
            var target = new ControlSettingsEditTarget(Button(X360Controls.A, DS4KeyType.Toggle), rig.Profile);
            target.LoadInto(rig.PressKey, rig.Macro);
            rig.PressKey.SwitchMode = "Press";

            target.WriteFrom(4, rig.PressKey, rig.Macro);

            Assert.Equal(DS4KeyType.None, target.Working.keyType);
        }

        [Fact]
        public void WriteFrom_UnboundButton_WritesTheUnboundFlag_AndNoToggle()
        {
            var rig = new Rig();
            var target = new ControlSettingsEditTarget(Button(X360Controls.Unbound, DS4KeyType.Unbound), rig.Profile);
            target.LoadInto(rig.PressKey, rig.Macro);
            rig.PressKey.SwitchMode = "Toggle";   // 出力先がないので、Toggle の対象も、ない

            target.WriteFrom(4, rig.PressKey, rig.Macro);

            Assert.Equal(DS4KeyType.Unbound, target.Working.keyType);
        }

        [Fact]
        public void WriteFrom_Macro_WritesTheStepsAndTheOptions_WithTheMacroMarkAlways()
        {
            var rig = new Rig();
            var target = new ControlSettingsEditTarget(Default(), rig.Profile);
            rig.Macro.Macro.AddRange(new[] { 350, 256 });
            rig.Macro.PlaybackOptions.RepeatWhileHeld = true;
            rig.Macro.PlaybackOptions.UseScanCode = true;

            target.WriteFrom(1, rig.PressKey, rig.Macro);

            Assert.Equal(DS4ControlSettings.ActionType.Macro, target.Working.actionType);
            Assert.Equal(new[] { 350, 256 }, target.Working.action.actionMacro);
            Assert.Equal(DS4KeyType.Macro | DS4KeyType.RepeatMacro | DS4KeyType.ScanCode, target.Working.keyType);
        }

        [Fact]
        public void WriteFrom_Zero_ResetsToTheDefault()
        {
            var rig = new Rig();
            var target = new ControlSettingsEditTarget(Key(65, DS4KeyType.Toggle), rig.Profile);

            target.WriteFrom(0, rig.PressKey, rig.Macro);

            Assert.Equal(DS4ControlSettings.ActionType.Default, target.Working.actionType);
            Assert.Equal(DS4KeyType.None, target.Working.keyType);
            Assert.Equal(0, target.Working.action.actionKey);
        }

        // Phase10-Step6 W7・W8 相当: Check Battery と Multi-action も Controls に保存できるようになり、「Controls にまだ保存できない種別」は一覧になくなった。
        // 以前ここにあった、保存できない種別（Check Battery、Multi-action）の拒否の検査（WriteFrom_ATypeControlsCannotSaveYet_IsRefused_AndChangesNothing）は、対象がなくなったため削除した。
        // 拒否の規則そのもの（WriteFrom の「保存できない種別は例外」）は、種別の表（ControlsSpecialTypeTable）に種別がないとき働く。

        [Fact]
        public void WriteFrom_AnIndexOutOfRange_Throws()
        {
            var rig = new Rig();
            var target = new ControlSettingsEditTarget(Default(), rig.Profile);

            Assert.Throws<ArgumentOutOfRangeException>(() => target.WriteFrom(11, rig.PressKey, rig.Macro));   // Phase10-Step3: 添字 10 は「Default」、11 は「Unbound」（Controls のモードの項目）。範囲外は 12 から
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(4)]
        public void WriteFrom_NeverChangesTheExtras(int typeIndex)
        {
            var rig = new Rig();
            var target = new ControlSettingsEditTarget(Default(), rig.Profile);
            rig.PressKey.Value = 65;
            rig.Macro.Macro.Add(350);

            target.WriteFrom(typeIndex, rig.PressKey, rig.Macro);

            Assert.Equal(SomeExtras, target.Working.extras);
        }

        // ---- 往復（読み込み → 同じ種別で書き出し）----

        [Fact]
        public void RoundTrip_KeepsEveryKind_IncludingTheExtras()
        {
            var cases = new[]
            {
                Key(65),
                Key(65, DS4KeyType.Toggle),
                Key(65, DS4KeyType.ScanCode),
                Key(65, DS4KeyType.Toggle | DS4KeyType.ScanCode),
                Button(X360Controls.A),
                Button(X360Controls.A, DS4KeyType.Toggle),
                Button(X360Controls.LeftMouse, DS4KeyType.Toggle),
                Button(X360Controls.Unbound, DS4KeyType.Unbound),
                Macro(new[] { 350, 256, 310, 256 }),
                Macro(new[] { 350, 256 }, DS4KeyType.Macro | DS4KeyType.RepeatMacro),
                Macro(new[] { 350, 256 }, DS4KeyType.Macro | DS4KeyType.RepeatMacro | DS4KeyType.ScanCode),
            };

            foreach (DS4ControlSettings source in cases)
            {
                var rig = new Rig();
                var target = new ControlSettingsEditTarget(source, rig.Profile);

                target.LoadInto(rig.PressKey, rig.Macro);
                target.WriteFrom(target.InitialTypeIndex, rig.PressKey, rig.Macro);

                AssertSameAssignment(source, target.Working);
            }
        }

        // ---- 編集用のコピー: Save（Apply）／Cancel（Discard）----

        [Fact]
        public void TheWorkingCopy_IsSeparateFromTheOriginal_UntilApply()
        {
            var rig = new Rig();
            DS4ControlSettings original = Key(65);
            var target = new ControlSettingsEditTarget(original, rig.Profile);
            rig.PressKey.Value = 70;
            rig.PressKey.SwitchMode = "Toggle";

            target.WriteFrom(4, rig.PressKey, rig.Macro);

            Assert.Equal(70, target.Working.action.actionKey);
            AssertSameAssignment(Key(65), original);   // 元は、変わっていない（Cancel の状態）
            Assert.NotSame(original, target.Working);
            Assert.NotSame(original.action, target.Working.action);
        }

        [Fact]
        public void Apply_CopiesTheWorkingValuesToTheOriginal_ForEveryField()
        {
            var rig = new Rig();
            DS4ControlSettings original = Default();
            var target = new ControlSettingsEditTarget(original, rig.Profile);
            rig.Macro.Macro.AddRange(new[] { 350, 256 });
            rig.Macro.PlaybackOptions.RepeatWhileHeld = true;
            target.WriteFrom(1, rig.PressKey, rig.Macro);

            target.Apply();

            AssertSameAssignment(target.Working, original);
            Assert.NotSame(original.action.actionMacro, target.Working.action.actionMacro);   // 配列を共有しない
        }

        [Fact]
        public void Discard_ReturnsTheWorkingCopyToTheOriginal()
        {
            var rig = new Rig();
            DS4ControlSettings original = Key(65);
            var target = new ControlSettingsEditTarget(original, rig.Profile);
            rig.PressKey.Value = 70;
            target.WriteFrom(4, rig.PressKey, rig.Macro);

            target.Discard();

            AssertSameAssignment(original, target.Working);
            AssertSameAssignment(Key(65), original);
        }

        // ---- 名前・検査（W3 と同じ）----

        [Fact]
        public void TheName_IsTheButtonsDisplayName_NotEditable_AndNeverInvalid()
        {
            var rig = new Rig();
            var target = new ControlSettingsEditTarget(Key(65), rig.Profile);

            Assert.Equal(DS4ControlNames.ds4inputNames[DS4Controls.Circle], target.Name);
            target.Name = "ignored";
            Assert.Equal(DS4ControlNames.ds4inputNames[DS4Controls.Circle], target.Name);
            Assert.False(target.NameEditable);
            Assert.True(target.EditMode);
            Assert.Null(target.SavedAction);
            Assert.Empty(target.ValidateName(out bool alreadyExists));
            Assert.False(alreadyExists);
            Assert.Equal(DS4Controls.Circle, target.Control);
        }

        [Fact]
        public void TheEditorViewModel_TakesTheTargetsButtonAndName()
        {
            var rig = new Rig();
            var target = new ControlSettingsEditTarget(Key(65), rig.Profile);

            var vm = new ControlActionEditorViewModel(0, target);

            Assert.Equal(DS4Controls.Circle, vm.Control);
            Assert.Equal(target.Name, vm.ActionName);
            Assert.False(vm.ActionNameEditable);
            Assert.Same(target, vm.EditTarget);
        }

        [Fact]
        public void Constructors_RejectNull()
        {
            var rig = new Rig();

            Assert.Throws<ArgumentNullException>(() => new ControlSettingsEditTarget(null, rig.Profile));
            Assert.Throws<ArgumentNullException>(() => new ControlSettingsEditTarget(Default(), null));
            Assert.Throws<ArgumentNullException>(() => new ControlActionEditorViewModel(0, (ControlSettingsEditTarget)null));
            var target = new ControlSettingsEditTarget(Default(), rig.Profile);
            Assert.Throws<ArgumentNullException>(() => target.LoadInto(null, rig.Macro));
            Assert.Throws<ArgumentNullException>(() => target.WriteFrom(4, rig.PressKey, null));
        }
    }
}
