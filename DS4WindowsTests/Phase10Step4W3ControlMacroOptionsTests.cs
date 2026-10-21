using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.DS4Control;
using DS4Windows.Services;
using DS4WinWPF.DS4Forms.ViewModels;
using DS4WinWPF.DS4Forms.ViewModels.SpecialActions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step4 W3（Phase10-Step4-Plan.md §5）: Controls の Macro に、SA 専用の 3 項目（Run on trigger release／Synchronized Run／Keep key state）を
    /// 保存し、実行し、画面で編集できるようにした縦通しの検査。入力（離したとき成立）、builder の写し、編集用の対象の往復、保存と再読み込み、実際の再生。
    /// 項目のない従来の Macro は、変わらない。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step4W3ControlMacroOptionsTests
    {
        private const int Device = MappingCharacterizationScope.Device;
        private static readonly int[] Steps = { 65, 65 };

        private static ControlSpecialItem Options(bool release = false, bool sync = false, bool keep = false)
            => new ControlSpecialItem { Type = SpecialAction.ActionTypeId.Macro, RunOnRelease = release, Synchronized = sync, KeepKeyState = keep };

        // ---- 入力（離したときに成立） ----

        private static TriggerInput InputWith(bool crossHeld)
        {
            var input = new TriggerInput(Device, TestHost.GetService<IProfileSettingsService>());
            var fields = new DS4StateFieldMapping();
            fields.buttons[(int)DS4Controls.Cross] = crossHeld;
            input.Update(new DS4State(), null, null, null, fields);
            return input;
        }

        [Fact]
        public void TheInput_WithoutTheInversion_IsEstablishedWhileHeld_AsBefore()
        {
            var action = new ControlButtonInputAction("n", DS4Controls.Cross, ControlBindingKind.Macro, ControlOutputNature.Digital);

            Assert.False(action.InvertsOnRelease);
            Assert.True(action.Evaluate(InputWith(true)).IsEstablished);
            Assert.True(action.Evaluate(InputWith(true)).IsEstablished);
            Assert.False(action.Evaluate(InputWith(false)).IsEstablished);
        }

        [Fact]
        public void TheInput_WithTheInversion_ArmsOnPress_AndIsEstablishedOnceOnRelease()
        {
            var action = new ControlButtonInputAction("n", DS4Controls.Cross, ControlBindingKind.Macro, ControlOutputNature.Digital, invertOnRelease: true);

            Assert.True(action.InvertsOnRelease);
            Assert.False(action.Evaluate(InputWith(false)).IsEstablished);   // まだ押していない
            Assert.False(action.Evaluate(InputWith(true)).IsEstablished);    // 押した: 準備だけ
            Assert.False(action.Evaluate(InputWith(true)).IsEstablished);    // 押し続けている間は待つ
            Assert.True(action.Evaluate(InputWith(false)).IsEstablished);    // 離した: 1 回だけ成立
            Assert.False(action.Evaluate(InputWith(false)).IsEstablished);   // その後は成立しない
            Assert.False(action.Evaluate(InputWith(true)).IsEstablished);    // 押し直し: また準備
            Assert.True(action.Evaluate(InputWith(false)).IsEstablished);
        }

        [Fact]
        public void TheInput_Reset_DisarmsIt()
        {
            var action = new ControlButtonInputAction("n", DS4Controls.Cross, ControlBindingKind.Macro, ControlOutputNature.Digital, invertOnRelease: true);

            action.Evaluate(InputWith(true));   // 準備
            action.Reset();

            Assert.False(action.Evaluate(InputWith(false)).IsEstablished);   // 準備が解除されているので、離しても成立しない
        }

        // ---- builder の写し ----

        [Fact]
        public void TheBuilder_CopiesTheThreeOptionsToTheSyntheticAction_AndInvertsOnlyForRunOnRelease()
        {
            using var t = new ControlsKeyTestScope();
            t.MapMacro(DS4Controls.Cross, Steps).special = Options(release: true, sync: true, keep: true);
            t.MapMacro(DS4Controls.Circle, Steps).special = Options(sync: true);

            IReadOnlyList<IActionBinding> bindings = TestHost.GetService<IControlBindingBuilder>().Build(Device);

            IActionBinding cross = bindings.Single(b => b.Source.name.EndsWith("_" + (int)DS4Controls.Cross));
            Assert.True(cross.Source.pressRelease);
            Assert.True(cross.Source.synchronized);
            Assert.True(cross.Source.keepKeyState);
            Assert.True(Assert.IsType<ControlButtonInputAction>(cross.Input).InvertsOnRelease);

            IActionBinding circle = bindings.Single(b => b.Source.name.EndsWith("_" + (int)DS4Controls.Circle));
            Assert.False(circle.Source.pressRelease);
            Assert.True(circle.Source.synchronized);
            Assert.False(circle.Source.keepKeyState);
            Assert.False(Assert.IsType<ControlButtonInputAction>(circle.Input).InvertsOnRelease);
        }

        [Fact]
        public void AMacroWithoutOptions_IsBuiltExactlyAsBefore()
        {
            using var t = new ControlsKeyTestScope();
            t.MapMacro(DS4Controls.Cross, Steps);

            IActionBinding macro = Assert.Single(TestHost.GetService<IControlBindingBuilder>().Build(Device));

            Assert.False(macro.Source.pressRelease);
            Assert.False(macro.Source.synchronized);
            Assert.False(macro.Source.keepKeyState);
            Assert.False(Assert.IsType<ControlButtonInputAction>(macro.Input).InvertsOnRelease);
            Assert.True(macro.Policy.RespectsFreshPressGuard);
            Assert.False(macro.Policy.SuppressesTriggerButtons);
            Assert.False(macro.Policy.ResetsTriggerOutput);
            Assert.True(macro.Policy.ErasesOutputEveryFrame);
        }

        [Fact]
        public void AnItemOfAnotherType_IsNotUsedAsMacroOptions()
        {
            using var t = new ControlsKeyTestScope();
            t.MapMacro(DS4Controls.Cross, Steps).special = new ControlSpecialItem { Type = SpecialAction.ActionTypeId.Program, RunOnRelease = true };

            IActionBinding macro = Assert.Single(TestHost.GetService<IControlBindingBuilder>().Build(Device));

            Assert.False(macro.Source.pressRelease);
            Assert.False(Assert.IsType<ControlButtonInputAction>(macro.Input).InvertsOnRelease);
        }

        // ---- 実際の再生 ----

        private static DS4State Cross(bool held)
        {
            DS4State state = ControlsKeyTestScope.Neutral();
            state.Cross = held;
            return state;
        }

        private static void RunFor(ControlsKeyTestScope t, DS4State input, int milliseconds)
        {
            var sw = Stopwatch.StartNew();
            do
            {
                t.Run(input);
                Thread.Sleep(10);
            }
            while (sw.ElapsedMilliseconds < milliseconds);
        }

        private static void WaitFor(Func<bool> condition)
        {
            var sw = Stopwatch.StartNew();
            while (!condition() && sw.ElapsedMilliseconds < 1500)
                Thread.Sleep(10);
        }

        [Fact]
        public void RunOnRelease_PlaysNothingWhileHeld_AndPlaysOnceAfterTheRelease()
        {
            using var t = new ControlsKeyTestScope();
            t.MapMacro(DS4Controls.Cross, Steps).special = Options(release: true);
            t.Publish();

            RunFor(t, Cross(false), 150);   // SA と同じ Fresh-press ガード（離して 80ms 続くまで実行しない）
            RunFor(t, Cross(true), 400);
            Assert.Empty(t.Recorder.KeyPresses);   // 押している間は、再生しない

            RunFor(t, Cross(false), 150);
            WaitFor(() => t.Recorder.KeyPresses.Count >= 1);
            Assert.Equal(t.Native(65), Assert.Single(t.Recorder.KeyPresses));

            RunFor(t, Cross(false), 300);
            Assert.Single(t.Recorder.KeyPresses);   // 1 回だけ
        }

        [Fact]
        public void KeepKeyState_LeavesTheKeyDown_AndWithoutIt_TheKeyIsReleasedAtTheEnd()
        {
            // 手順 { 65 } ＝ キー 65 を押すだけ（離す手順がない）
            foreach (bool keep in new[] { false, true })
            {
                using var t = new ControlsKeyTestScope();
                t.MapMacro(DS4Controls.Cross, new[] { 65 }).special = keep ? Options(keep: true) : null;
                t.Publish();

                RunFor(t, Cross(false), 150);
                RunFor(t, Cross(true), 300);
                WaitFor(() => t.Recorder.KeyPresses.Count >= 1);
                Thread.Sleep(300);   // 再生の後始末（押したままのキーの解放）を待つ

                Assert.Equal(t.Native(65), Assert.Single(t.Recorder.KeyPresses));
                if (keep)
                    Assert.Empty(t.Recorder.KeyReleases);
                else
                    Assert.Equal(t.Native(65), Assert.Single(t.Recorder.KeyReleases));
            }
        }

        // ---- 編集用の対象（画面との往復） ----

        private sealed class Rig
        {
            public readonly ProfileSettingsService Profile = TestRepositoryFactory.CreateProfileSettingsService();
            public readonly RecordingSpecialActionRepository Repo = new RecordingSpecialActionRepository();
            public readonly PressKeyViewModel PressKey;
            public readonly MacroViewModel Macro;

            public Rig()
            {
                Profile.OutputKBMMapping = new SendInputMapping();
                PressKey = new PressKeyViewModel(Repo, Profile, TestRepositoryFactory.CreateOutputSlotService());
                Macro = new MacroViewModel(Repo);
            }
        }

        private static DS4ControlSettings MacroSettings(ControlSpecialItem special)
        {
            var s = new DS4ControlSettings(DS4Controls.Cross) { actionType = DS4ControlSettings.ActionType.Macro, keyType = DS4KeyType.Macro, special = special };
            s.action.actionMacro = Steps;
            return s;
        }

        private const int MacroIndex = 1;

        [Fact]
        public void LoadInto_FillsTheThreeCheckBoxes_FromTheItem()
        {
            var rig = new Rig();
            var target = new ControlSettingsEditTarget(MacroSettings(Options(release: true, keep: true)), rig.Profile);

            target.LoadInto(rig.PressKey, rig.Macro);

            Assert.True(rig.Macro.RunTriggerRelease);
            Assert.False(rig.Macro.SyncRun);
            Assert.True(rig.Macro.KeepKeyState);
        }

        [Fact]
        public void LoadInto_WithoutAnItem_LeavesTheCheckBoxesOff()
        {
            var rig = new Rig();
            rig.Macro.RunTriggerRelease = true;   // 前の編集の残りがあっても、読み込みで上書きされる
            var target = new ControlSettingsEditTarget(MacroSettings(null), rig.Profile);

            target.LoadInto(rig.PressKey, rig.Macro);

            Assert.False(rig.Macro.RunTriggerRelease);
            Assert.False(rig.Macro.SyncRun);
            Assert.False(rig.Macro.KeepKeyState);
        }

        [Fact]
        public void WriteFrom_Macro_WritesTheCheckedOptions_AndNoItemWhenNoneIsChecked()
        {
            var rig = new Rig();
            var original = MacroSettings(null);
            var target = new ControlSettingsEditTarget(original, rig.Profile);
            target.LoadInto(rig.PressKey, rig.Macro);

            rig.Macro.SyncRun = true;
            target.WriteFrom(MacroIndex, rig.PressKey, rig.Macro);
            Assert.NotNull(target.Working.special);
            Assert.Equal(SpecialAction.ActionTypeId.Macro, target.Working.special.Type);
            Assert.True(target.Working.special.Synchronized);
            Assert.False(target.Working.special.RunOnRelease);
            Assert.False(target.Working.special.KeepKeyState);
            Assert.Null(original.special);   // Save（Apply）するまで、元は変わらない

            target.Apply();
            Assert.True(original.special.Synchronized);

            rig.Macro.SyncRun = false;
            target.WriteFrom(MacroIndex, rig.PressKey, rig.Macro);
            Assert.Null(target.Working.special);   // 1 つも立っていなければ、従来の Macro と同じ形（項目なし）
            target.Apply();
            Assert.Null(original.special);
        }

        [Fact]
        public void WriteFrom_AnotherType_DropsTheMacroOptions()
        {
            var rig = new Rig();
            var target = new ControlSettingsEditTarget(MacroSettings(Options(release: true)), rig.Profile);
            target.LoadInto(rig.PressKey, rig.Macro);

            target.WriteFrom(10, rig.PressKey, rig.Macro);   // 10＝Default

            Assert.Null(target.Working.special);
            Assert.Equal(DS4ControlSettings.ActionType.Default, target.Working.actionType);
        }

        // ---- 保存と再読み込み ----

        [Fact]
        public void TheOptions_SurviveSavingAndReloadingTheProfile_ThroughTheEditTarget()
        {
            using var ctx = new PersistenceGoldenContext();
            var rig = new Rig();
            var original = MacroSettings(null);
            var target = new ControlSettingsEditTarget(original, rig.Profile);
            target.LoadInto(rig.PressKey, rig.Macro);
            rig.Macro.RunTriggerRelease = true;
            rig.Macro.KeepKeyState = true;
            target.WriteFrom(MacroIndex, rig.PressKey, rig.Macro);
            target.Apply();

            BackingStore store = PersistenceHarness.NewStore();
            DS4ControlSettings dcs = store.GetDS4CSetting(PersistenceHarness.Slot, DS4Controls.Cross);
            dcs.Reset();
            dcs.actionType = original.actionType;
            dcs.action.actionMacro = original.action.actionMacro;
            dcs.keyType = original.keyType;
            dcs.special = original.special;

            string xml = PersistenceHarness.SaveProfile(ctx, store, "S4W3Macro");
            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, xml, "S4W3MacroLoad");

            DS4ControlSettings back = loaded.GetDS4CSetting(PersistenceHarness.Slot, DS4Controls.Cross);
            Assert.Equal(DS4ControlSettings.ActionType.Macro, back.actionType);
            Assert.Equal(Steps, back.action.actionMacro);
            Assert.True(back.special.RunOnRelease);
            Assert.False(back.special.Synchronized);
            Assert.True(back.special.KeepKeyState);
        }
    }
}
