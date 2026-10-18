using System;
using System.Collections.Generic;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step4 W4（Phase8-Step4-Plan.md §2.4／§3）: <c>Mapping</c> へのエンジンの配線（旧ループのフック）の結合テスト。
    /// ホストの <c>Mapping</c>・エンジン・登録簿・移行スイッチを使い、<c>MapCustom</c> を実走行させる。
    /// Step7.1（Button の切替）の予行も兼ねる: 同じ入力の列を、旧経路（移行なし）と新経路（Button を移行し、実物の束縛を登録）で流し、出力を比較する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step4W4WiringTests
    {
        private sealed class FakeInputAction : IInputAction
        {
            public FakeInputAction(params DS4Controls[] triggers) { Triggers = triggers; }
            public string Name => "fake";
            public IReadOnlyList<DS4Controls> Triggers { get; }
            public bool Established;
            public TriggerState Evaluate(ITriggerInput input) => Established ? TriggerState.Established : TriggerState.NotEstablished;
            public void Reset() { }
        }

        private sealed class RecordingOutput : IHeldOutputAction
        {
            public int Executes, Stops, Held;
            public string Id => "rec";
            public void Execute(IOutputContext ctx) { Executes++; }
            public void Stop(IOutputContext ctx) { Stops++; }
            public void WhileHeld(IOutputContext ctx) { Held++; }
        }

        /// <summary>ホストの登録簿・移行スイッチを一時的に使い、終了時に元（空）へ戻す。</summary>
        private sealed class MigrationScope : IDisposable
        {
            private readonly SpecialActionTestScope _scope;
            private readonly IActionRegistry _registry = TestHost.GetService<IActionRegistry>();
            private readonly BindingMigrationOptions _options = TestHost.GetService<BindingMigrationOptions>();

            public MigrationScope(SpecialActionTestScope scope) { _scope = scope; }

            public void Migrate(SpecialAction.ActionTypeId type, params IActionBinding[] bindings)
            {
                _options.MigratedTypes.Add(type);
                _registry.Replace(_scope.Device, bindings);
            }

            public void Unmigrate()
            {
                _options.MigratedTypes.Clear();
                _registry.Clear(_scope.Device);
            }

            public void Dispose() => Unmigrate();
        }

        private static DS4State Frame(bool l1)
        {
            DS4State state = SpecialActionTestScope.Neutral();
            state.L1 = l1;
            return state;
        }

        // ---- フックが効く ----

        [Fact]
        public void Hook_HandsAMigratedActionToTheEngine_AndSkipsTheLegacyLoop()
        {
            using var t = new SpecialActionTestScope();
            using var m = new MigrationScope(t);
            SpecialAction sa = t.AddButtonPress("L1");
            var input = new FakeInputAction(DS4Controls.L1);
            var output = new RecordingOutput();
            m.Migrate(sa.typeID, new TriggerBinding(input, new IOutputAction[] { output },
                new BindingPolicy(respectsFreshPressGuard: false), sa, 0));

            input.Established = true;
            DS4State first = t.RunFrame(Frame(true));

            // 旧経路なら Button の Press は Cross を出力する。新経路（偽の出力）では、旧ループが通らないので Cross は出ない
            Assert.False(first.Cross);
            Assert.Equal(1, output.Executes);
            Assert.True(t.Engine.IsSuppressed(t.Device, DS4Controls.L1));  // 抑制の集合はエンジンのもの（旧ループと共通）
            Assert.False(first.L1);                                        // トリガーのボタンの出力は消される

            t.RunFrame(Frame(true));
            Assert.Equal(1, output.Executes);                              // 押している間は再実行しない
            Assert.Equal(1, output.Held);

            input.Established = false;
            t.RunFrame(Frame(false));
            Assert.Equal(1, output.Stops);
            Assert.False(t.Engine.IsSuppressed(t.Device, DS4Controls.L1));
        }

        [Fact]
        public void NothingMigrated_TheLegacyLoopRuns_AsBefore()
        {
            using var t = new SpecialActionTestScope();
            t.AddButtonPress("L1");

            DS4State output = t.RunFrame(Frame(true));

            Assert.True(output.Cross);
            Assert.False(output.L1);
            Assert.True(t.Engine.IsSuppressed(t.Device, DS4Controls.L1));
        }

        // ---- 予行: Button（Step7.1）を新経路で流し、旧経路と比較する ----

        private static IActionBinding RealButtonBinding(SpecialAction sa, bool toggle)
        {
            IProfileSettingsService settings = TestHost.GetService<IProfileSettingsService>();
            IManagedActionManager manager = TestHost.GetService<IManagedActionManager>();
            return new TriggerBinding(
                new ComboTriggerInputAction(sa.name, new[] { DS4Controls.L1 }, settings),
                new IOutputAction[] { new ButtonOutputAction(sa, manager) },
                new BindingPolicy(respectsFreshPressGuard: toggle),   // 設計書 §6: Press は false、Toggle は true
                sa, 0);
        }

        private static List<(bool Cross, bool L1)> RunSequence(SpecialActionTestScope t, SpecialAction sa, bool[] l1Sequence)
        {
            ActionInstanceState state = t.StateOf(sa);
            state.IsToggledOn = false;
            state.RequiresFreshPressAfterReset = false;
            state.UntriggeredTimestampTicks = 0;

            var result = new List<(bool, bool)>();
            foreach (bool l1 in l1Sequence)
            {
                DS4State output = t.RunFrame(Frame(l1));
                result.Add((output.Cross, output.L1));
            }

            return result;
        }

        [Fact]
        public void ButtonPress_NewPathProducesTheSameOutputAsTheLegacyPath()
        {
            using var t = new SpecialActionTestScope();
            using var m = new MigrationScope(t);
            SpecialAction sa = t.AddButtonPress("L1");
            bool[] sequence = { false, false, true, true, true, false, true, false, false, true, true, false };

            var legacy = RunSequence(t, sa, sequence);

            t.RunFrame(Frame(false));
            m.Migrate(sa.typeID, RealButtonBinding(sa, toggle: false));
            var migrated = RunSequence(t, sa, sequence);

            Assert.Equal(legacy, migrated);
            Assert.Contains((true, false), legacy);   // 比較が「何も出力しない」同士にならないこと
        }

        [Fact]
        public void ButtonToggle_NewPathProducesTheSameOutputAsTheLegacyPath()
        {
            using var t = new SpecialActionTestScope();
            using var m = new MigrationScope(t);
            SpecialAction sa = t.Add(new SpecialAction("p8s4_w4_toggle_" + Guid.NewGuid().ToString("N"), "L1", "Button",
                ((int)X360Controls.A).ToString(), 0, "Toggle\n"));
            bool[] sequence = { false, false, true, true, false, false, true, false, true, true, false, false };

            var legacy = RunSequence(t, sa, sequence);

            t.RunFrame(Frame(false));
            m.Migrate(sa.typeID, RealButtonBinding(sa, toggle: true));
            var migrated = RunSequence(t, sa, sequence);

            Assert.Equal(legacy, migrated);
            Assert.Contains((true, false), legacy);
            Assert.Contains((false, false), legacy);
        }

        // ---- 割り当て 0 ----

        [Fact]
        public void TheNewPath_DoesNotAddAllocationsToTheFrame()
        {
            using var t = new SpecialActionTestScope();
            using var m = new MigrationScope(t);
            SpecialAction sa = t.AddButtonPress("L1");
            m.Migrate(sa.typeID, RealButtonBinding(sa, toggle: false));
            DS4State held = Frame(true);
            DS4State idle = Frame(false);
            for (int i = 0; i < 300; i++) { t.RunFrame(held); t.RunFrame(idle); }

            long Allocated(DS4State input)
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 1000; i++) t.RunFrame(input);
                return GC.GetAllocatedBytesForCurrentThread() - before;
            }

            long migratedHeld = Allocated(held);
            long migratedIdle = Allocated(idle);

            m.Unmigrate();
            for (int i = 0; i < 300; i++) { t.RunFrame(held); t.RunFrame(idle); }
            long legacyHeld = Allocated(held);
            long legacyIdle = Allocated(idle);

            // 新経路が、旧経路より多く割り当てないこと（フレームあたり）
            Assert.True(migratedHeld <= legacyHeld, $"押下: 新 {migratedHeld} B > 旧 {legacyHeld} B");
            Assert.True(migratedIdle <= legacyIdle, $"未操作: 新 {migratedIdle} B > 旧 {legacyIdle} B");
        }
    }
}
