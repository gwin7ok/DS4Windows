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
    /// Phase10-Step6 W8 相当（Multi-action を Controls へ）: 入力（Controls 由来でも、タップ・長押し・ダブルタップを判定する独立した入力。エンジンのボタンごとの表に載る）、
    /// 方針（元のボタンの出力は、待ち時間のあいだだけでなく常に消す。ユーザー決定）、未設定のマクロ（保存を止めず、実行自体を行わない）、保存、画面の入力。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step6W8ControlMultiActionTests
    {
        private sealed class RecordingMacroStringPlayer : IMacroStringPlayer
        {
            public List<string> Played { get; } = new List<string>();

            public void PlayMacroString(int deviceIndex, string macro) => Played.Add(macro);
        }

        private static ControlSpecialItem Multi(string tap = "1/2", string hold = "3/4", string dbl = "5/6")
            => new ControlSpecialItem
            {
                Type = SpecialAction.ActionTypeId.MultiAction,
                TapMacro = Parse(tap),
                HoldMacro = Parse(hold),
                DoubleTapMacro = Parse(dbl),
            };

        private static List<int> Parse(string text)
            => string.IsNullOrEmpty(text) ? new List<int>() : text.Split('/').Select(int.Parse).ToList();

        private static ControlBindingBuilder BuilderWith(RecordingMacroStringPlayer player)
        {
            var factory = new SpecialActionOutputFactory(TestHost.GetService<IProfileSettingsService>(), TestHost.GetService<IManagedActionManager>(),
                TestHost.GetService<IMacroPlayer>(), TestHost.GetService<IProcessLauncher>(), TestHost.GetService<IProfileSwitcher>(),
                TestHost.GetService<ISpecialActionRepository>(), TestHost.GetService<Func<IMapping>>(), macroStringPlayer: player);
            return new ControlBindingBuilder(TestHost.GetService<IProfileSettingsService>(), null, null, factory);
        }


        [Fact]
        public void AMultiActionAssignment_MakesOneBinding_WithTheIndependentInputOnTheControlTable_AndTheControlsPolicy()
        {
            using var t = new ControlsKeyTestScope();
            t.MapSpecial(DS4Controls.Cross, Multi());

            IReadOnlyList<IActionBinding> bindings = BuilderWith(new RecordingMacroStringPlayer()).Build(t.Device);

            var binding = Assert.IsType<TriggerBinding>(Assert.Single(bindings));
            var input = Assert.IsType<ControlMultiTapInputAction>(binding.Input);
            Assert.IsAssignableFrom<IControlBoundInput>(input);   // エンジンのボタンごとの表（ProcessControl）に載る印
            Assert.Equal(new[] { DS4Controls.Cross }, input.Triggers);
            Assert.Equal(3, binding.Outputs.Count);               // タップ・長押し・ダブルタップの順
            Assert.True(binding.Policy.RoutesOutputsByGesture);
            Assert.False(binding.Policy.RespectsFreshPressGuard);
            Assert.False(binding.Policy.SuppressesTriggerButtons);
            Assert.True(binding.Policy.ErasesOutputEveryFrame);   // 割り当てたボタンの元の出力は、常に消す
        }

        // ---- エンジンを通した実行（偽の入力で、時刻と押下を決める。ControlsKeyTestScope の RunMapCustom は、前フレームの状態を渡さないため、ジェスチャーの判定に使えない） ----

        private const int EngineDevice = 5;
        private static readonly DateTime T0 = new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

        private sealed class FakeInput : ITriggerInput
        {
            public bool Held;
            public bool PreviousHeld;
            public DateTime Now = T0;
            public int Device => EngineDevice;
            public bool IsActive(DS4Controls control) => control == DS4Controls.Cross && Held;
            public bool WasActive(DS4Controls control) => control == DS4Controls.Cross && PreviousHeld;
            public DelayClock Delay { get; } = new DelayClock();
            public DateTime UtcNow => Now;
            public long TickCount64 => (long)(Now - T0).TotalMilliseconds + 1000;
        }

        private sealed class RecordingFrame : IFrameOutput
        {
            public int Resets;
            public void ResetTriggerControl(DS4Controls control) { if (control == DS4Controls.Cross) Resets++; }
            public void PressX360(X360Controls control) { }
        }

        private sealed class EngineRig
        {
            public readonly RecordingMacroStringPlayer Player = new RecordingMacroStringPlayer();
            public readonly FakeInput Input = new FakeInput();
            public readonly RecordingFrame Frame = new RecordingFrame();
            private readonly TriggerDispatchEngine _engine;
            private readonly int _savedDebounce;

            public EngineRig(ControlSpecialItem item)
            {
                _savedDebounce = TestHost.GetService<IProfileSettingsService>().DebouncingMs[EngineDevice];
                TestHost.GetService<IProfileSettingsService>().DebouncingMs[EngineDevice] = 0;

                var dcs = new DS4ControlSettings(DS4Controls.Cross) { actionType = DS4ControlSettings.ActionType.Special, special = item };
                ControlsAssignmentReader.TryReadSpecial(EngineDevice, dcs, out AssignmentDefinition definition, out SpecialAction source);
                var factory = new SpecialActionOutputFactory(TestHost.GetService<IProfileSettingsService>(), TestHost.GetService<IManagedActionManager>(),
                    TestHost.GetService<IMacroPlayer>(), TestHost.GetService<IProcessLauncher>(), TestHost.GetService<IProfileSwitcher>(),
                    macroStringPlayer: Player);
                TriggerBinding binding = new AssignmentBindingBuilder(factory, TestHost.GetService<IProfileSettingsService>()).Build(EngineDevice, definition, source, -1);

                var registry = new ActionBindingRegistry();
                registry.Replace(EngineDevice, new IActionBinding[] { binding });
                _engine = new TriggerDispatchEngine(registry, TestHost.GetService<IManagedActionManager>(), null);
            }

            public void Restore() => TestHost.GetService<IProfileSettingsService>().DebouncingMs[EngineDevice] = _savedDebounce;

            /// <summary>t=elapsedMs の 1 フレーム。held はこのフレームの押下。</summary>
            public void Frame1(int elapsedMs, bool held)
            {
                Input.PreviousHeld = Input.Held;
                Input.Held = held;
                Input.Now = T0.AddMilliseconds(elapsedMs);
                _engine.ProcessControl(EngineDevice, DS4Controls.Cross, Input, Frame);
            }

            public bool OwnsCross => _engine.OwnsControl(EngineDevice, DS4Controls.Cross);
        }

        [Fact]
        public void TheEngineTable_OwnsTheButton_OfAControlsMultiAction()
        {
            var rig = new EngineRig(Multi());
            try
            {
                Assert.True(rig.OwnsCross);   // ボタンごとの処理の表に載っている（印 IControlBoundInput）
            }
            finally { rig.Restore(); }
        }

        [Fact]
        public void ATap_PlaysTheTapMacroOnce_AndTheButtonsOwnOutputIsErasedEveryFrame()
        {
            var rig = new EngineRig(Multi());
            try
            {
                rig.Frame1(0, false);
                rig.Frame1(100, true);
                rig.Frame1(160, false);
                Assert.Empty(rig.Player.Played);   // タップは、ダブルタップの余地を残すため、終わりから間隔（150ms）を過ぎるまで待つ
                rig.Frame1(250, false);
                Assert.Empty(rig.Player.Played);
                rig.Frame1(330, false);

                Assert.Equal(new[] { "1/2" }, rig.Player.Played.ToArray());
                Assert.Equal(5, rig.Frame.Resets);   // ここまでの 5 フレーム（待ち時間のあいだも）、元の出力を消した
            }
            finally { rig.Restore(); }
        }

        [Fact]
        public void AHold_PlaysTheHoldMacroOnce_AndTheOriginalOutputStaysErasedAllThrough()
        {
            var rig = new EngineRig(Multi());
            try
            {
                int frames = 0;
                rig.Frame1(0, false); frames++;
                for (int ms = 100; ms <= 800; ms += 100) { rig.Frame1(ms, true); frames++; }

                Assert.Equal(new[] { "3/4" }, rig.Player.Played.ToArray());
                Assert.Equal(frames, rig.Frame.Resets);
            }
            finally { rig.Restore(); }
        }

        [Fact]
        public void ADoubleTap_PlaysTheDoubleTapMacro()
        {
            var rig = new EngineRig(Multi());
            try
            {
                rig.Frame1(0, false);
                rig.Frame1(100, true);
                rig.Frame1(150, false);
                rig.Frame1(200, true);   // 前のタップの終わりから間隔以内の 2 回目の押下
                rig.Frame1(250, false);
                rig.Frame1(500, false);

                Assert.Equal(new[] { "5/6" }, rig.Player.Played.ToArray());
            }
            finally { rig.Restore(); }
        }

        [Fact]
        public void AnUnsetMacro_IsNotExecutedAtAll_ButTheOriginalOutputIsStillErased()
        {
            var rig = new EngineRig(Multi(tap: ""));   // タップのマクロが未設定
            try
            {
                rig.Frame1(0, false);
                rig.Frame1(100, true);
                rig.Frame1(160, false);
                rig.Frame1(400, false);

                Assert.Empty(rig.Player.Played);   // 空のマクロを実行するのではなく、実行自体を行わない
                Assert.Equal(4, rig.Frame.Resets); // 割り当て自体はあるので、元のボタンの出力は消える
            }
            finally { rig.Restore(); }
        }

        [Fact]
        public void TheThreeMacros_SurviveSavingAndReloadingTheProfile()
        {
            using var ctx = new PersistenceGoldenContext();
            BackingStore store = PersistenceHarness.NewStore();
            DS4ControlSettings dcs = store.GetDS4CSetting(PersistenceHarness.Slot, DS4Controls.Cross);
            dcs.Reset();
            dcs.actionType = DS4ControlSettings.ActionType.Special;
            dcs.special = Multi("1/2", "", "7");

            string xml = PersistenceHarness.SaveProfile(ctx, store, "S6W8Multi");
            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, xml, "S6W8MultiLoad");

            ControlSpecialItem back = loaded.GetDS4CSetting(PersistenceHarness.Slot, DS4Controls.Cross).special;
            Assert.Equal(SpecialAction.ActionTypeId.MultiAction, back.Type);
            Assert.Equal(new[] { 1, 2 }, back.TapMacro);
            Assert.Empty(back.HoldMacro);
            Assert.Equal(new[] { 7 }, back.DoubleTapMacro);
        }

        [Fact]
        public void TheInput_RoundTripsTheItem_AndTheValidationNeverBlocksTheSave()
        {
            var vm = new MultiActButtonViewModel(new RecordingSpecialActionRepository());

            vm.LoadControlItem(Multi("1/2", "", "7"));
            Assert.Equal(new[] { 1, 2 }, vm.TapMacro);
            Assert.Empty(vm.HoldMacro);
            Assert.Equal(new[] { 7 }, vm.DoubleTapMacro);

            // 未設定のマクロは、エラー（赤い枠）を出すだけで、保存を止めない（SA と同じ。ユーザー決定）
            Assert.True(vm.IsValid(new SpecialAction("n", "Cross", "MultiAction", "1/2,,7")));
            Assert.True(vm.HasErrors);

            ControlSpecialItem back = vm.ToControlItem(SpecialAction.ActionTypeId.MultiAction);
            Assert.Equal(SpecialAction.ActionTypeId.MultiAction, back.Type);
            Assert.Equal(new[] { 1, 2 }, back.TapMacro);
            Assert.Empty(back.HoldMacro);
            Assert.Equal(new[] { 7 }, back.DoubleTapMacro);

            // 読み込み直しは、前の内容を残さない
            vm.LoadControlItem(Multi("9", "9", "9"));
            Assert.Equal(new[] { 9 }, vm.TapMacro);
            Assert.Equal(new[] { 9 }, vm.HoldMacro);
        }

        [Fact]
        public void TheEditTarget_WritesTheMultiActionItem_ThroughTheEditorSet()
        {
            var profile = TestRepositoryFactory.CreateProfileSettingsService();
            profile.OutputKBMMapping = new SendInputMapping();
            var repo = new RecordingSpecialActionRepository();
            var pressKey = new PressKeyViewModel(repo, profile, TestRepositoryFactory.CreateOutputSlotService());
            var macro = new MacroViewModel(repo);
            var vm = new MultiActButtonViewModel(repo);
            vm.LoadControlItem(Multi("1", "2", "3"));
            var editors = new ActionTypeEditorSet(new Dictionary<SpecialAction.ActionTypeId, IActionTypeEditor>
            {
                [SpecialAction.ActionTypeId.MultiAction] = vm,
            });
            var original = new DS4ControlSettings(DS4Controls.Cross);
            var target = new ControlSettingsEditTarget(original, profile);

            target.WriteFrom(7, pressKey, macro, editors);
            target.Apply();

            Assert.Equal(DS4ControlSettings.ActionType.Special, original.actionType);
            Assert.Equal(SpecialAction.ActionTypeId.MultiAction, original.special.Type);
            Assert.Equal(new[] { 1 }, original.special.TapMacro);
            Assert.Equal(new[] { 2 }, original.special.HoldMacro);
            Assert.Equal(new[] { 3 }, original.special.DoubleTapMacro);
            Assert.Equal(7, new ControlSettingsEditTarget(original, profile).InitialTypeIndex);
        }
    }
}
