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
    /// Phase10-Step4 W4（Phase10-Step4-Plan.md §5）: 第 2 群（Disconnect BT、Calibrate gyro、Calibrate sixaxis wheel。設定は押し続ける秒数だけ）を、
    /// Controls で保存・実行・編集できるようにした縦通しの検査。入力の押し続ける秒数、builder の束縛、編集対象の往復、種別の保存可否、保存と再読み込み、実際の実行。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step4W4ControlSpecialTests
    {
        private const int Device = MappingCharacterizationScope.Device;
        private static readonly DateTime T = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

        private static readonly SpecialAction.ActionTypeId[] Built =
        {
            SpecialAction.ActionTypeId.Program,
            SpecialAction.ActionTypeId.DisconnectBT,
            SpecialAction.ActionTypeId.GyroCalibrate,
            SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate,
        };

        private static ControlSpecialItem Item(SpecialAction.ActionTypeId type, double seconds = 0)
            => new ControlSpecialItem { Type = type, HoldSeconds = seconds };

        // ---- 入力: 押し続ける秒数 ----

        private sealed class Harness : IDisposable
        {
            private readonly int _savedDebounce;
            private readonly DS4StateFieldMapping _fields = new DS4StateFieldMapping();
            private readonly DS4State _state = new DS4State();
            private DateTime _now = T;

            public Harness()
            {
                Settings = TestHost.GetService<IProfileSettingsService>();
                _savedDebounce = Settings.DebouncingMs[Device];
                Settings.DebouncingMs[Device] = 0;
                Input = new TriggerInput(Device, Settings, () => _now);
            }

            public IProfileSettingsService Settings { get; }
            public TriggerInput Input { get; }

            public void Frame(int elapsedMs, bool crossHeld)
            {
                _now = T.AddMilliseconds(elapsedMs);
                Array.Clear(_fields.buttons, 0, _fields.buttons.Length);
                _fields.buttons[(int)DS4Controls.Cross] = crossHeld;
                Input.Update(_state, null, null, null, _fields);
            }

            public void Dispose() => Settings.DebouncingMs[Device] = _savedDebounce;
        }

        [Fact]
        public void TheHoldSeconds_FollowTheSameRuleAsTheSaTriggerDelay()
        {
            using var h = new Harness();
            var controls = new ControlButtonInputAction("c", DS4Controls.Cross, ControlBindingKind.Special, ControlJudgement.Trigger,
                delaySeconds: 0.5, profileSettings: h.Settings);
            var combo = new ComboTriggerInputAction("s", new[] { DS4Controls.Cross }, h.Settings, 0.5);

            // 押す・離す・押し直す、の列で、SA のトリガーと同じ結果になる
            var frames = new (int Ms, bool Held)[] { (0, false), (100, true), (400, true), (500, true), (700, false), (800, true), (1200, true), (1300, true) };
            foreach ((int ms, bool held) in frames)
            {
                h.Frame(ms, held);
                Assert.Equal(combo.Evaluate(h.Input).IsEstablished, controls.Evaluate(h.Input).IsEstablished);
            }

            // 念のため、期待値そのものも固定する（0.5 秒ちょうどで成立）
            using var h2 = new Harness();
            var action = new ControlButtonInputAction("c", DS4Controls.Cross, ControlBindingKind.Special, ControlJudgement.Trigger,
                delaySeconds: 0.5, profileSettings: h2.Settings);
            h2.Frame(0, false);
            Assert.False(action.Evaluate(h2.Input).IsEstablished);
            h2.Frame(100, true);
            Assert.False(action.Evaluate(h2.Input).IsEstablished);
            h2.Frame(400, true);
            Assert.False(action.Evaluate(h2.Input).IsEstablished);
            h2.Frame(500, true);
            Assert.True(action.Evaluate(h2.Input).IsEstablished);    // 基準時刻（最後に未成立だった 0 ms）からちょうど 0.5 秒で成立
        }

        [Fact]
        public void TheHoldSeconds_IgnoreTheInversion_LikeTheSaTrigger()
        {
            using var h = new Harness();
            var action = new ControlButtonInputAction("c", DS4Controls.Cross, ControlBindingKind.Special, ControlJudgement.Trigger,
                invertOnRelease: true, delaySeconds: 0.2, profileSettings: h.Settings);

            h.Frame(0, false);
            action.Evaluate(h.Input);
            h.Frame(300, true);
            Assert.True(action.Evaluate(h.Input).IsEstablished);   // 反転せず、押している間に成立
            h.Frame(400, false);
            Assert.False(action.Evaluate(h.Input).IsEstablished);
        }

        [Fact]
        public void ADelayWithoutTheSettings_IsRejected_AndNoDelayNeedsNone()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ControlButtonInputAction("c", DS4Controls.Cross, ControlBindingKind.Special, ControlJudgement.Trigger, delaySeconds: 1.0));
            Assert.NotNull(new ControlButtonInputAction("c", DS4Controls.Cross, ControlBindingKind.Special, ControlJudgement.Trigger));
        }

        // ---- builder ----

        [Fact]
        public void TheBuilder_MakesASpecialBinding_ForEachBuiltType_WithTheControlsPolicyAndTheHoldSeconds()
        {
            using var t = new ControlsKeyTestScope();
            t.MapSpecial(DS4Controls.Cross, Item(SpecialAction.ActionTypeId.DisconnectBT, 1.5));
            t.MapSpecial(DS4Controls.Circle, Item(SpecialAction.ActionTypeId.GyroCalibrate));
            t.MapSpecial(DS4Controls.Square, Item(SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate, 0.25));

            IReadOnlyList<IActionBinding> bindings = TestHost.GetService<IControlBindingBuilder>().Build(Device);

            Assert.Equal(3, bindings.Count);
            var expected = new[]
            {
                (DS4Controls.Cross, SpecialAction.ActionTypeId.DisconnectBT, typeof(DisconnectBTAction), 1.5),
                (DS4Controls.Circle, SpecialAction.ActionTypeId.GyroCalibrate, typeof(GyroCalibrateAction), 0.0),
                (DS4Controls.Square, SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate, typeof(SteeringWheelCalibrateAction), 0.25),
            };
            foreach ((DS4Controls control, SpecialAction.ActionTypeId type, Type output, double seconds) in expected)
            {
                IActionBinding b = bindings.Single(x => x.Source.name == $"Synthetic_Special_{Device}_{(int)control}");
                var input = Assert.IsType<ControlButtonInputAction>(b.Input);
                Assert.Equal(ControlBindingKind.Special, input.Kind);
                Assert.Equal(ControlJudgement.Trigger, input.Judgement);
                Assert.Equal(new[] { control }, input.Triggers.ToArray());
                Assert.IsType(output, Assert.Single(b.Outputs));
                Assert.Equal(type, b.Source.typeID);
                Assert.Equal(seconds, b.Source.delayTime);
                Assert.Equal(new[] { control }, b.Source.trigger.ToArray());

                Assert.True(b.Policy.RespectsFreshPressGuard);
                Assert.False(b.Policy.SuppressesTriggerButtons);
                Assert.False(b.Policy.ResetsTriggerOutput);
                Assert.True(b.Policy.ErasesOutputEveryFrame);
            }
        }

        [Fact]
        public void TheTypesNotBuiltYet_AndAMissingItem_MakeNoBinding()
        {
            using var t = new ControlsKeyTestScope();
            t.MapSpecial(DS4Controls.Square, Item(SpecialAction.ActionTypeId.BatteryCheck));
            t.MapSpecial(DS4Controls.Triangle, Item(SpecialAction.ActionTypeId.MultiAction));   // Program は W5 で束縛を作るようになった
            t.MapSpecial(DS4Controls.L1, null);

            Assert.Empty(TestHost.GetService<IControlBindingBuilder>().Build(Device));
        }

        [Fact]
        public void WithoutTheFactory_NoSpecialBindingIsMade()
        {
            using var t = new ControlsKeyTestScope();
            t.MapSpecial(DS4Controls.Cross, Item(SpecialAction.ActionTypeId.GyroCalibrate));

            var builder = new ControlBindingBuilder(TestHost.GetService<IProfileSettingsService>());

            Assert.Empty(builder.Build(Device));
        }

        [Fact]
        public void TryInterpretSpecial_AcceptsOnlyTheBuiltTypes()
        {
            foreach (SpecialAction.ActionTypeId type in Built)
            {
                var dcs = new DS4ControlSettings(DS4Controls.Cross) { actionType = DS4ControlSettings.ActionType.Special, special = Item(type) };
                Assert.True(ControlBindingBuilder.TryInterpretSpecial(dcs, out ControlSpecialItem item));
                Assert.Same(dcs.special, item);
            }

            Assert.False(ControlBindingBuilder.TryInterpretSpecial(null, out _));
            Assert.False(ControlBindingBuilder.TryInterpretSpecial(new DS4ControlSettings(DS4Controls.Cross), out _));
            Assert.False(ControlBindingBuilder.TryInterpretSpecial(
                new DS4ControlSettings(DS4Controls.Cross) { actionType = DS4ControlSettings.ActionType.Special }, out _));
            Assert.False(ControlBindingBuilder.TryInterpretSpecial(
                new DS4ControlSettings(DS4Controls.Cross) { actionType = DS4ControlSettings.ActionType.Special, special = Item(SpecialAction.ActionTypeId.BatteryCheck) }, out _));
        }

        [Fact]
        public void ASpecialAssignmentWithExtras_MakesTheExtrasBindingFirst_ThenTheSpecialBinding()
        {
            using var t = new ControlsKeyTestScope();
            DS4ControlSettings dcs = t.MapSpecial(DS4Controls.Cross, Item(SpecialAction.ActionTypeId.GyroCalibrate));
            dcs.extras = "1,2,0,0,0,0,0,0,0";
            var builder = new ControlBindingBuilder(TestHost.GetService<IProfileSettingsService>(), null, t.ExtrasPort, TestHost.GetService<ISpecialActionOutputFactory>());

            IReadOnlyList<IActionBinding> bindings = builder.Build(Device);

            Assert.Equal(2, bindings.Count);
            Assert.Equal(ControlBindingKind.Extras, Assert.IsType<ControlButtonInputAction>(bindings[0].Input).Kind);
            Assert.Equal(ControlBindingKind.Special, Assert.IsType<ControlButtonInputAction>(bindings[1].Input).Kind);
        }

        // ---- 実際の実行 ----

        private static DS4State Cross(bool held)
        {
            DS4State state = ControlsKeyTestScope.Neutral();
            state.Cross = held;
            return state;
        }

        private static DS4State RunFor(ControlsKeyTestScope t, DS4State input, int milliseconds)
        {
            DS4State last = null;
            var sw = Stopwatch.StartNew();
            do
            {
                last = t.Run(input);
                Thread.Sleep(10);
            }
            while (sw.ElapsedMilliseconds < milliseconds);

            return last;
        }

        private static ControlBindingBuilder BuilderWith(RecordingGyroCalibrationPort gyro, RecordingWheelCalibrationPort wheel)
        {
            var factory = new SpecialActionOutputFactory(TestHost.GetService<IProfileSettingsService>(), TestHost.GetService<IManagedActionManager>(),
                TestHost.GetService<IMacroPlayer>(), TestHost.GetService<IProcessLauncher>(), TestHost.GetService<IProfileSwitcher>(),
                TestHost.GetService<ISpecialActionRepository>(), TestHost.GetService<Func<IMapping>>(), gyroCalibration: gyro, wheelCalibrationPort: wheel);
            return new ControlBindingBuilder(TestHost.GetService<IProfileSettingsService>(), null, null, factory);
        }

        [Fact]
        public void CalibrateGyro_RunsOncePerPress_ErasesTheButtonsOutput_AndWaitsForTheFreshPressGuard()
        {
            using var t = new ControlsKeyTestScope();
            var gyro = new RecordingGyroCalibrationPort();
            t.MapSpecial(DS4Controls.Cross, Item(SpecialAction.ActionTypeId.GyroCalibrate));
            t.PublishWith(BuilderWith(gyro, new RecordingWheelCalibrationPort()));

            RunFor(t, Cross(false), 150);   // SA と同じ Fresh-press ガード（離して 80ms 続くまで実行しない）
            Assert.Empty(gyro.Calls);

            DS4State held = RunFor(t, Cross(true), 300);
            Assert.Equal(new[] { Device }, gyro.Calls.ToArray());   // 押して 1 回
            Assert.False(held.Cross);                               // 割り当てたボタンの通常の出力は消える

            RunFor(t, Cross(true), 300);
            Assert.Single(gyro.Calls);                              // 保持中は繰り返さない

            RunFor(t, Cross(false), 150);
            RunFor(t, Cross(true), 300);
            Assert.Equal(2, gyro.Calls.Count);                      // 押し直して、もう 1 回
        }

        [Fact]
        public void CalibrateGyro_WithHoldSeconds_RunsOnlyAfterTheyHavePassed()
        {
            using var t = new ControlsKeyTestScope();
            var gyro = new RecordingGyroCalibrationPort();
            t.MapSpecial(DS4Controls.Cross, Item(SpecialAction.ActionTypeId.GyroCalibrate, 0.4));
            t.PublishWith(BuilderWith(gyro, new RecordingWheelCalibrationPort()));

            RunFor(t, Cross(false), 150);
            RunFor(t, Cross(true), 200);
            Assert.Empty(gyro.Calls);   // まだ 0.4 秒たっていない

            RunFor(t, Cross(true), 500);
            Assert.Single(gyro.Calls);
        }

        [Fact]
        public void SixaxisWheelCalibration_StartsWhileHeld_ThroughTheControlsBinding()
        {
            using var t = new ControlsKeyTestScope();
            var wheel = new RecordingWheelCalibrationPort { State = 0 };
            t.MapSpecial(DS4Controls.Cross, Item(SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate));
            t.PublishWith(BuilderWith(new RecordingGyroCalibrationPort(), wheel));

            RunFor(t, Cross(false), 150);
            RunFor(t, Cross(true), 300);

            Assert.Equal(new[] { 1 }, wheel.Writes.ToArray());   // 状態 0（待機）から 1（較正の開始の要求）へ
        }

        // ---- 編集対象（画面との往復） ----

        private sealed class Rig
        {
            public readonly ProfileSettingsService Profile = TestRepositoryFactory.CreateProfileSettingsService();
            public readonly RecordingSpecialActionRepository Repo = new RecordingSpecialActionRepository();
            public readonly PressKeyViewModel PressKey;
            public readonly MacroViewModel Macro;
            public readonly SpecialActionViewModel Disconnect;
            public readonly SpecialActionViewModel Wheel;
            public readonly SpecialActionViewModel Gyro;
            public readonly IReadOnlyDictionary<SpecialAction.ActionTypeId, SpecialActionViewModel> HoldEditors;
            public ControlSpecialEditors Editors => new ControlSpecialEditors { HoldIntervals = HoldEditors };

            public Rig()
            {
                Profile.OutputKBMMapping = new SendInputMapping();
                PressKey = new PressKeyViewModel(Repo, Profile, TestRepositoryFactory.CreateOutputSlotService());
                Macro = new MacroViewModel(Repo);
                Disconnect = new SpecialActionViewModel(5, Repo);
                Wheel = new SpecialActionViewModel(8, Repo);
                Gyro = new SpecialActionViewModel(9, Repo);
                HoldEditors = new Dictionary<SpecialAction.ActionTypeId, SpecialActionViewModel>
                {
                    [SpecialAction.ActionTypeId.DisconnectBT] = Disconnect,
                    [SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate] = Wheel,
                    [SpecialAction.ActionTypeId.GyroCalibrate] = Gyro,
                };
            }
        }

        private static DS4ControlSettings SpecialSettings(ControlSpecialItem item)
            => new DS4ControlSettings(DS4Controls.Cross) { actionType = DS4ControlSettings.ActionType.Special, special = item };

        [Theory]
        [InlineData(5, SpecialAction.ActionTypeId.DisconnectBT)]
        [InlineData(8, SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate)]
        [InlineData(9, SpecialAction.ActionTypeId.GyroCalibrate)]
        public void WriteFrom_WritesTheTypeAndTheHoldSeconds_AndNothingElse(int typeIndex, SpecialAction.ActionTypeId type)
        {
            var rig = new Rig();
            var original = new DS4ControlSettings(DS4Controls.Cross) { actionType = DS4ControlSettings.ActionType.Key, keyType = DS4KeyType.Toggle };
            original.action.actionKey = 65;
            var target = new ControlSettingsEditTarget(original, rig.Profile);
            rig.HoldEditors[type].HoldInterval = 2.5;

            target.WriteFrom(typeIndex, rig.PressKey, rig.Macro, rig.Editors);

            Assert.Equal(DS4ControlSettings.ActionType.Special, target.Working.actionType);
            Assert.Equal(type, target.Working.special.Type);
            Assert.Equal(2.5, target.Working.special.HoldSeconds);
            Assert.Equal(DS4KeyType.None, target.Working.keyType);   // 前のキーの割り当ては残らない
            Assert.Equal(0, target.Working.action.actionKey);
            Assert.Equal(DS4ControlSettings.ActionType.Key, original.actionType);   // Save（Apply）するまで、元は変わらない

            target.Apply();
            Assert.Equal(type, original.special.Type);
        }

        [Fact]
        public void WriteFrom_WithoutTheEditors_WritesZeroSeconds()
        {
            var rig = new Rig();
            var target = new ControlSettingsEditTarget(new DS4ControlSettings(DS4Controls.Cross), rig.Profile);

            target.WriteFrom(9, rig.PressKey, rig.Macro);

            Assert.Equal(SpecialAction.ActionTypeId.GyroCalibrate, target.Working.special.Type);
            Assert.Equal(0, target.Working.special.HoldSeconds);
        }

        [Theory]
        [InlineData(SpecialAction.ActionTypeId.DisconnectBT, 5)]
        [InlineData(SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate, 8)]
        [InlineData(SpecialAction.ActionTypeId.GyroCalibrate, 9)]
        public void TheInitialType_AndTheLoadedSeconds_FollowTheItem(SpecialAction.ActionTypeId type, int expectedIndex)
        {
            var rig = new Rig();
            var target = new ControlSettingsEditTarget(SpecialSettings(Item(type, 3.25)), rig.Profile);

            target.LoadInto(rig.PressKey, rig.Macro, rig.Editors);

            Assert.Equal(expectedIndex, target.InitialTypeIndex);
            Assert.Equal(3.25, rig.HoldEditors[type].HoldInterval);
        }

        [Fact]
        public void WritingAnotherType_AfterASpecial_DropsTheItem()
        {
            var rig = new Rig();
            var target = new ControlSettingsEditTarget(SpecialSettings(Item(SpecialAction.ActionTypeId.GyroCalibrate, 1)), rig.Profile);

            target.WriteFrom(10, rig.PressKey, rig.Macro, rig.Editors);   // Default

            Assert.Null(target.Working.special);
            Assert.Equal(DS4ControlSettings.ActionType.Default, target.Working.actionType);
        }

        // ---- 保存と再読み込み ----

        [Fact]
        public void ASpecialAssignment_SurvivesSavingAndReloadingTheProfile_ThroughTheEditTarget()
        {
            using var ctx = new PersistenceGoldenContext();
            var rig = new Rig();
            var original = new DS4ControlSettings(DS4Controls.Cross);
            var target = new ControlSettingsEditTarget(original, rig.Profile);
            rig.Disconnect.HoldInterval = 1.75;
            target.WriteFrom(5, rig.PressKey, rig.Macro, rig.Editors);
            target.Apply();

            BackingStore store = PersistenceHarness.NewStore();
            DS4ControlSettings dcs = store.GetDS4CSetting(PersistenceHarness.Slot, DS4Controls.Cross);
            dcs.Reset();
            dcs.actionType = original.actionType;
            dcs.special = original.special;

            string xml = PersistenceHarness.SaveProfile(ctx, store, "S4W4Special");
            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, xml, "S4W4SpecialLoad");

            DS4ControlSettings back = loaded.GetDS4CSetting(PersistenceHarness.Slot, DS4Controls.Cross);
            Assert.Equal(DS4ControlSettings.ActionType.Special, back.actionType);
            Assert.Equal(SpecialAction.ActionTypeId.DisconnectBT, back.special.Type);
            Assert.Equal(1.75, back.special.HoldSeconds);
        }
    }
}
