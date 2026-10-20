using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.DS4Control;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step4 W3（Phase8-Step4-Plan.md §2.3／§3）: <see cref="TriggerDispatchEngine"/> の単体テスト。
    /// 偽の入力・出力・束縛と、実物の <c>IManagedActionManager</c>（状態）を使う。時刻（<c>TickCount64</c>）は入力から注入し、実時間のスリープは使わない。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step4W3EngineTests
    {
        private const int Device = 5;

        // ---- 偽物 ----

        private sealed class FakeTriggerInput : ITriggerInput
        {
            public HashSet<DS4Controls> Active { get; } = new HashSet<DS4Controls>();
            public int Device => Phase8Step4W3EngineTests.Device;
            public bool IsActive(DS4Controls control) => Active.Contains(control);
            public bool WasActive(DS4Controls control) => false;
            public DelayClock Delay { get; } = new DelayClock();
            public DateTime UtcNow { get; set; } = new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc);
            public long TickCount64 { get; set; } = 1000;
        }

        private sealed class FakeInputAction : IInputAction
        {
            public FakeInputAction(params DS4Controls[] triggers) { Triggers = triggers; }
            public string Name => "fake";
            public IReadOnlyList<DS4Controls> Triggers { get; }
            public bool Established;
            public int Evaluations;
            public int Resets;
            public TriggerState Evaluate(ITriggerInput input) { Evaluations++; return Established ? TriggerState.Established : TriggerState.NotEstablished; }
            public void Reset() { Resets++; }
        }

        private class RecordingOutput : IHeldOutputAction
        {
            public readonly List<string> Events = new List<string>();
            public bool RequestEndFrameOnExecute;
            public bool ThrowOnExecute;
            public bool? ExecutingDuringExecute;
            public SpecialAction Source;
            public int Held;
            public string Id => "rec";

            public void Execute(IOutputContext ctx)
            {
                Events.Add("execute");
                if (Source != null) ExecutingDuringExecute = ActionManager.GetStateFor(Source, Phase8Step4W3EngineTests.Device).IsExecuting;
                if (ThrowOnExecute) throw new InvalidOperationException("boom");
                if (RequestEndFrameOnExecute) ctx.RequestEndFrame();
            }

            public void Stop(IOutputContext ctx) { Events.Add("stop"); }
            public void WhileHeld(IOutputContext ctx) { Held++; }
        }

        private sealed class RecordingFrameOutputAction : IFrameOutputAction
        {
            public readonly List<(bool Active, bool Rising)> Calls = new List<(bool, bool)>();
            public bool Record = true;
            public string Id => "frame";
            public void Execute(IOutputContext ctx) { }
            public void Stop(IOutputContext ctx) { }
            public void ApplyFrame(IOutputContext ctx, bool active, bool risingEdge) { if (Record) Calls.Add((active, risingEdge)); }
        }

        private sealed class RecordingFrame : IFrameOutput
        {
            public readonly List<DS4Controls> Resets = new List<DS4Controls>();
            public bool Record = true;
            public void ResetTriggerControl(DS4Controls control) { if (Record) Resets.Add(control); }
            public void PressX360(X360Controls control) { }
        }

        private sealed class Harness
        {
            public readonly ActionBindingRegistry Registry = new ActionBindingRegistry();
            public readonly IManagedActionManager Manager;
            public readonly TriggerDispatchEngine Engine;
            public readonly FakeTriggerInput Input = new FakeTriggerInput();
            public readonly RecordingFrame Frame = new RecordingFrame();

            public Harness()
            {
                DS4WinWPF.AppHost.CreateHost();
                Manager = TestHost.GetService<IManagedActionManager>();
                Engine = new TriggerDispatchEngine(Registry, Manager, null);
            }

            public SpecialAction NewSource() =>
                new SpecialAction("p8s4_eng_" + Guid.NewGuid().ToString("N"), "Cross", "Button", "1", 0, "Press\n");

            public ActionInstanceState State(SpecialAction sa) => Manager.GetStateFor(sa, Device);

            /// <summary>Fresh-press ガードを解除した状態にする（テストの本題がガードでないとき）。</summary>
            public void Unguard(SpecialAction sa) => State(sa).RequiresFreshPressAfterReset = false;

            public TriggerBinding Bind(FakeInputAction input, SpecialAction source, BindingPolicy policy, params IOutputAction[] outputs)
                => new TriggerBinding(input, outputs, policy, source, 0);

            public void Register(params IActionBinding[] bindings) => Registry.Replace(Device, bindings);
        }

        private static BindingPolicy NoGuard() => new BindingPolicy(respectsFreshPressGuard: false);

        // ---- 束縛がないとき（Phase8-Step8 8d: 移行のスイッチは削除した。束縛がなければ何もしない） ----

        [Fact]
        public void EmptyRegistry_DoesNothing_AndOutOfRangeDevicesAreIgnored()
        {
            var h = new Harness();
            SpecialAction sa = h.NewSource();

            Assert.False(h.Engine.Owns(Device, sa));
            Assert.False(h.Engine.Owns(-1, sa));
            Assert.False(h.Engine.Owns(ControllerLimits.MaxControllerCount, sa));
            Assert.False(h.Engine.Owns(Device, null));
            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);
            h.Engine.ProcessTriggers(-1, h.Input, h.Frame);
            h.Engine.ApplyFrameOutputs(-1, h.Input, h.Frame);
            h.Engine.AddSuppressed(-1, new[] { DS4Controls.L1 });
            h.Engine.ReleaseSuppressed(-1, h.Input);
            h.Engine.OnProfileApplied(-1);
            h.Engine.OnDeviceDisconnected(-1);
            Assert.False(h.Engine.IsSuppressed(-1, DS4Controls.L1));
        }

        [Fact]
        public void Owns_RequiresARegisteredBinding()
        {
            var h = new Harness();
            SpecialAction registered = h.NewSource();
            SpecialAction other = h.NewSource();
            h.Register(h.Bind(new FakeInputAction(DS4Controls.L1), registered, NoGuard(), new RecordingOutput()));

            Assert.True(h.Engine.Owns(Device, registered));
            Assert.False(h.Engine.Owns(Device, other));
        }

        // ---- エッジ ----

        [Fact]
        public void Edge_ExecutesOnce_HoldsWhileHeld_AndStopsOnRelease()
        {
            var h = new Harness();
            SpecialAction sa = h.NewSource();
            var input = new FakeInputAction(DS4Controls.L1);
            var output = new RecordingOutput();
            h.Register(h.Bind(input, sa, NoGuard(), output));

            input.Established = true;
            Assert.Equal(FrameResult.Continue, h.Engine.ProcessOwned(Device, sa, h.Input, h.Frame));
            Assert.Equal(new[] { "execute" }, output.Events);
            Assert.True(h.State(sa).BeingTriggered);

            h.Engine.ProcessOwned(Device, sa, h.Input, h.Frame);
            h.Engine.ProcessOwned(Device, sa, h.Input, h.Frame);
            Assert.Equal(new[] { "execute" }, output.Events);   // 押している間は再実行しない
            Assert.Equal(2, output.Held);

            input.Established = false;
            h.Engine.ProcessOwned(Device, sa, h.Input, h.Frame);
            Assert.Equal(new[] { "execute", "stop" }, output.Events);
            Assert.False(h.State(sa).BeingTriggered);

            h.Engine.ProcessOwned(Device, sa, h.Input, h.Frame);
            Assert.Equal(2, output.Events.Count);                // 離した後は何も起きない
        }

        [Fact]
        public void StopOnRelease_False_DoesNotCallStop()
        {
            var h = new Harness();
            SpecialAction sa = h.NewSource();
            var input = new FakeInputAction(DS4Controls.L1) { Established = true };
            var output = new RecordingOutput();
            h.Register(h.Bind(input, sa, new BindingPolicy(respectsFreshPressGuard: false, stopOnRelease: false), output));

            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);
            input.Established = false;
            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);

            Assert.Equal(new[] { "execute" }, output.Events);
            Assert.False(h.State(sa).BeingTriggered);
        }

        // ---- Fresh-press ガード ----

        [Fact]
        public void FreshPressGuard_BlocksUntilTheTriggerIsUntriggeredFor80Ms()
        {
            var h = new Harness();
            SpecialAction sa = h.NewSource();
            var input = new FakeInputAction(DS4Controls.L1);
            var output = new RecordingOutput();
            h.Register(h.Bind(input, sa, BindingPolicy.Default, output));
            Assert.True(h.State(sa).RequiresFreshPressAfterReset);

            // 押しっぱなしで始まる: 実行しない
            input.Established = true;
            h.Input.TickCount64 = 1000;
            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);
            Assert.Empty(output.Events);

            // 未成立になる（タイマー開始）→ 70ms ではまだ解除されない → 途中で成立するとタイマーは戻る
            input.Established = false;
            h.Input.TickCount64 = 1100;
            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);
            h.Input.TickCount64 = 1170;
            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);
            Assert.True(h.State(sa).RequiresFreshPressAfterReset);

            input.Established = true;
            h.Input.TickCount64 = 1175;
            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);
            Assert.Empty(output.Events);
            Assert.Equal(0, h.State(sa).UntriggeredTimestampTicks);

            // 改めて未成立を 80ms 続けると解除される
            input.Established = false;
            h.Input.TickCount64 = 1200;
            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);
            h.Input.TickCount64 = 1279;
            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);
            Assert.True(h.State(sa).RequiresFreshPressAfterReset);
            h.Input.TickCount64 = 1280;
            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);
            Assert.False(h.State(sa).RequiresFreshPressAfterReset);

            // 解除後の新規成立は実行される
            input.Established = true;
            h.Input.TickCount64 = 1300;
            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);
            Assert.Equal(new[] { "execute" }, output.Events);
        }

        [Fact]
        public void GuardIgnored_WhenThePolicyDoesNotRespectIt()
        {
            var h = new Harness();
            SpecialAction sa = h.NewSource();
            var input = new FakeInputAction(DS4Controls.L1) { Established = true };
            var output = new RecordingOutput();
            h.Register(h.Bind(input, sa, NoGuard(), output));
            Assert.True(h.State(sa).RequiresFreshPressAfterReset);

            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);

            Assert.Equal(new[] { "execute" }, output.Events);
        }

        // ---- 実行中のガード ----

        [Fact]
        public void BlocksWhileExecuting_SkipsAnExecutingAction_AndMarksItDuringExecute()
        {
            var h = new Harness();
            SpecialAction sa = h.NewSource();
            var input = new FakeInputAction(DS4Controls.L1) { Established = true };
            var output = new RecordingOutput { Source = sa };
            h.Register(h.Bind(input, sa, new BindingPolicy(respectsFreshPressGuard: false, blocksWhileExecuting: true), output));

            h.State(sa).IsExecuting = true;
            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);
            Assert.Empty(output.Events);

            h.State(sa).IsExecuting = false;
            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);
            Assert.Equal(new[] { "execute" }, output.Events);
            Assert.True(output.ExecutingDuringExecute);   // 実行中は IsExecuting
            Assert.False(h.State(sa).IsExecuting);        // 終われば戻る
        }

        [Fact]
        public void BlocksWhileExecuting_IsClearedEvenWhenTheOutputThrows()
        {
            var h = new Harness();
            SpecialAction sa = h.NewSource();
            var input = new FakeInputAction(DS4Controls.L1) { Established = true };
            var output = new RecordingOutput { ThrowOnExecute = true };
            h.Register(h.Bind(input, sa, new BindingPolicy(respectsFreshPressGuard: false, blocksWhileExecuting: true), output));

            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);   // 例外は外へ出ない

            Assert.False(h.State(sa).IsExecuting);
        }

        // ---- トリガーのボタンの出力の消去と抑制 ----

        [Fact]
        public void ResetsTriggerOutput_ClearsEveryTriggerWhileEstablished()
        {
            var h = new Harness();
            SpecialAction sa = h.NewSource();
            var input = new FakeInputAction(DS4Controls.L1, DS4Controls.R1);
            h.Register(h.Bind(input, sa, NoGuard(), new RecordingOutput()));

            input.Established = false;
            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);
            Assert.Empty(h.Frame.Resets);

            input.Established = true;
            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);
            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);
            Assert.Equal(new[] { DS4Controls.L1, DS4Controls.R1, DS4Controls.L1, DS4Controls.R1 }, h.Frame.Resets);
        }

        [Fact]
        public void ResetsTriggerOutput_False_LeavesTheOutputAlone()
        {
            var h = new Harness();
            SpecialAction sa = h.NewSource();
            var input = new FakeInputAction(DS4Controls.L1) { Established = true };
            h.Register(h.Bind(input, sa, new BindingPolicy(respectsFreshPressGuard: false, resetsTriggerOutput: false), new RecordingOutput()));

            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);

            Assert.Empty(h.Frame.Resets);
        }

        [Fact]
        public void Suppression_AddsEveryTrigger_AndReleasesEachButtonWhenItIsReleased()
        {
            var h = new Harness();
            SpecialAction sa = h.NewSource();
            var input = new FakeInputAction(DS4Controls.L1, DS4Controls.R1) { Established = true };
            h.Register(h.Bind(input, sa, NoGuard(), new RecordingOutput()));
            h.Input.Active.Add(DS4Controls.L1);
            h.Input.Active.Add(DS4Controls.R1);

            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);
            Assert.True(h.Engine.IsSuppressed(Device, DS4Controls.L1));
            Assert.True(h.Engine.IsSuppressed(Device, DS4Controls.R1));
            Assert.False(h.Engine.IsSuppressed(Device, DS4Controls.Cross));

            // 一部のボタンだけ離す: そのボタンだけ抑制が外れ、まだ押しているボタンは抑制が続く
            input.Established = false;
            h.Input.Active.Remove(DS4Controls.L1);
            h.Engine.ReleaseSuppressed(Device, h.Input);
            Assert.False(h.Engine.IsSuppressed(Device, DS4Controls.L1));
            Assert.True(h.Engine.IsSuppressed(Device, DS4Controls.R1));

            h.Input.Active.Remove(DS4Controls.R1);
            h.Engine.ReleaseSuppressed(Device, h.Input);
            Assert.False(h.Engine.IsSuppressed(Device, DS4Controls.R1));
        }

        [Fact]
        public void Suppression_False_DoesNotSuppress_AndAddSuppressedWorksDirectly()
        {
            var h = new Harness();
            SpecialAction sa = h.NewSource();
            var input = new FakeInputAction(DS4Controls.L1) { Established = true };
            h.Register(h.Bind(input, sa, new BindingPolicy(respectsFreshPressGuard: false, suppressesTriggerButtons: false), new RecordingOutput()));

            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);
            Assert.False(h.Engine.IsSuppressed(Device, DS4Controls.L1));

            h.Engine.AddSuppressed(Device, new[] { DS4Controls.Cross, DS4Controls.Circle });
            Assert.True(h.Engine.IsSuppressed(Device, DS4Controls.Cross));
            Assert.True(h.Engine.IsSuppressed(Device, DS4Controls.Circle));
        }

        // ---- 排他グループ（シフト）----

        [Fact]
        public void ExclusionGroup_OnlyTheFirstEstablishedBindingRuns()
        {
            var h = new Harness();
            SpecialAction first = h.NewSource();
            SpecialAction second = h.NewSource();
            var firstInput = new FakeInputAction(DS4Controls.L1, DS4Controls.Cross);
            var secondInput = new FakeInputAction(DS4Controls.Cross) { Established = true };
            var firstOutput = new RecordingOutput();
            var secondOutput = new RecordingOutput();
            var policy = new BindingPolicy(respectsFreshPressGuard: false, exclusionGroup: "shift-cross");
            h.Register(h.Bind(firstInput, first, policy, firstOutput), h.Bind(secondInput, second, policy, secondOutput));

            // シフトが成立していない: 後ろの束縛が実行される
            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);
            h.Engine.ApplyFrameOutputs(Device, h.Input, h.Frame);
            Assert.Empty(firstOutput.Events);
            Assert.Equal(new[] { "execute" }, secondOutput.Events);

            // 次のフレームで、先頭の束縛も成立したら: 先頭だけが実行され、後ろは新たに実行されない
            firstInput.Established = true;
            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);
            h.Engine.ApplyFrameOutputs(Device, h.Input, h.Frame);
            Assert.Equal(new[] { "execute" }, firstOutput.Events);
            Assert.Equal(new[] { "execute" }, secondOutput.Events);   // 後ろは、止められる（Stop）ことも、再実行されることもない
        }

        [Fact]
        public void ExclusionGroup_DoesNotLeakToTheNextFrame()
        {
            var h = new Harness();
            SpecialAction first = h.NewSource();
            SpecialAction second = h.NewSource();
            var firstInput = new FakeInputAction(DS4Controls.L1) { Established = true };
            var secondInput = new FakeInputAction(DS4Controls.R1);
            var secondOutput = new RecordingOutput();
            var policy = new BindingPolicy(respectsFreshPressGuard: false, exclusionGroup: "g");
            h.Register(h.Bind(firstInput, first, policy, new RecordingOutput()), h.Bind(secondInput, second, policy, secondOutput));

            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);
            h.Engine.ApplyFrameOutputs(Device, h.Input, h.Frame);

            firstInput.Established = false;
            secondInput.Established = true;
            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);

            Assert.Equal(new[] { "execute" }, secondOutput.Events);
        }

        // ---- フレームの打ち切り・例外 ----

        [Fact]
        public void RequestEndFrame_StopsTheScan_ForProcessTriggersAndProcessOwned()
        {
            var h = new Harness();
            SpecialAction first = h.NewSource();
            SpecialAction second = h.NewSource();
            var firstOutput = new RecordingOutput { RequestEndFrameOnExecute = true };
            var secondOutput = new RecordingOutput();
            h.Register(
                h.Bind(new FakeInputAction(DS4Controls.L1) { Established = true }, first, NoGuard(), firstOutput),
                h.Bind(new FakeInputAction(DS4Controls.R1) { Established = true }, second, NoGuard(), secondOutput));

            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);
            Assert.Equal(new[] { "execute" }, firstOutput.Events);
            Assert.Empty(secondOutput.Events);                 // 打ち切られた

            // フック経路: 打ち切りの結果を返す（旧ループが return する）
            var h2 = new Harness();
            SpecialAction only = h2.NewSource();
            h2.Register(h2.Bind(new FakeInputAction(DS4Controls.L1) { Established = true }, only, NoGuard(), new RecordingOutput { RequestEndFrameOnExecute = true }));
            Assert.Equal(FrameResult.EndFrame, h2.Engine.ProcessOwned(Device, only, h2.Input, h2.Frame));
        }

        [Fact]
        public void AnExceptionInOneBinding_DoesNotStopTheOthers()
        {
            var h = new Harness();
            SpecialAction first = h.NewSource();
            SpecialAction second = h.NewSource();
            var secondOutput = new RecordingOutput();
            h.Register(
                h.Bind(new FakeInputAction(DS4Controls.L1) { Established = true }, first, NoGuard(), new RecordingOutput { ThrowOnExecute = true }),
                h.Bind(new FakeInputAction(DS4Controls.R1) { Established = true }, second, NoGuard(), secondOutput));

            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);

            Assert.Equal(new[] { "execute" }, secondOutput.Events);
        }

        // ---- 毎フレームの出力 ----

        [Fact]
        public void ApplyFrameOutputs_ReusesTheEvaluation_AndReportsTheRisingEdge()
        {
            var h = new Harness();
            SpecialAction sa = h.NewSource();
            var input = new FakeInputAction(DS4Controls.L1);
            var frameOutput = new RecordingFrameOutputAction();
            h.Register(h.Bind(input, sa, NoGuard(), frameOutput));

            void Frame(bool established)
            {
                input.Established = established;
                h.Engine.ProcessOwned(Device, sa, h.Input, h.Frame);
                h.Engine.ApplyFrameOutputs(Device, h.Input, h.Frame);
            }

            Frame(true);
            Frame(true);
            Frame(false);
            Frame(true);

            Assert.Equal(new[] { (true, true), (true, false), (false, false), (true, true) }, frameOutput.Calls.ToArray());
            Assert.Equal(4, input.Evaluations);   // 1 フレームにつき評価は 1 回（ApplyFrameOutputs は再評価しない）
        }

        [Fact]
        public void ApplyFrameOutputs_TreatsABindingThatWasNotEvaluatedThisFrameAsNotEstablished()
        {
            var h = new Harness();
            SpecialAction sa = h.NewSource();
            var input = new FakeInputAction(DS4Controls.L1) { Established = true };
            var frameOutput = new RecordingFrameOutputAction();
            h.Register(h.Bind(input, sa, NoGuard(), frameOutput));

            h.Engine.ProcessOwned(Device, sa, h.Input, h.Frame);
            h.Engine.ApplyFrameOutputs(Device, h.Input, h.Frame);   // 評価済み: 成立
            h.Engine.ApplyFrameOutputs(Device, h.Input, h.Frame);   // 評価されていないフレーム: 未成立

            Assert.Equal(new[] { (true, true), (false, false) }, frameOutput.Calls.ToArray());
        }

        // ---- 後始末 ----

        [Fact]
        public void OnProfileApplied_ResetsOnTheNextProcessing_NotImmediately()
        {
            var h = new Harness();
            SpecialAction sa = h.NewSource();
            var input = new FakeInputAction(DS4Controls.L1);
            h.Register(h.Bind(input, sa, NoGuard(), new RecordingOutput()));
            h.Input.Active.Add(DS4Controls.L1);        // ボタンは押されたまま（解除パスでは外れない）
            h.Engine.AddSuppressed(Device, new[] { DS4Controls.L1 });
            h.Input.Delay.Now = DateTime.UtcNow;

            h.Engine.OnProfileApplied(Device);
            Assert.True(h.Engine.IsSuppressed(Device, DS4Controls.L1));   // 適用スレッドは直接書かない

            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);           // 次の処理の先頭でリセット
            Assert.False(h.Engine.IsSuppressed(Device, DS4Controls.L1));
            Assert.Equal(1, input.Resets);
            Assert.Equal(DateTime.MinValue, h.Input.Delay.Now);
        }

        [Fact]
        public void OnDeviceDisconnected_ClearsTheRegistryAndTheStateImmediately()
        {
            var h = new Harness();
            SpecialAction sa = h.NewSource();
            h.Register(h.Bind(new FakeInputAction(DS4Controls.L1) { Established = true }, sa, NoGuard(), new RecordingOutput()));
            h.Engine.ProcessTriggers(Device, h.Input, h.Frame);
            Assert.True(h.Engine.IsSuppressed(Device, DS4Controls.L1));

            h.Engine.OnDeviceDisconnected(Device);

            Assert.Empty(h.Registry.GetBindings(Device));
            Assert.False(h.Engine.IsSuppressed(Device, DS4Controls.L1));
            Assert.False(h.Engine.Owns(Device, sa));
        }

        [Fact]
        public void ReplacingTheBindings_TakesEffectFromTheNextFrame_AndResetsTheRuntimeArrays()
        {
            var h = new Harness();
            SpecialAction sa = h.NewSource();
            var oldOutput = new RecordingOutput();
            h.Register(h.Bind(new FakeInputAction(DS4Controls.L1) { Established = true }, sa, NoGuard(), oldOutput));
            h.Engine.ProcessOwned(Device, sa, h.Input, h.Frame);
            Assert.Single(oldOutput.Events);

            SpecialAction sb = h.NewSource();
            var newOutput = new RecordingOutput();
            h.Register(h.Bind(new FakeInputAction(DS4Controls.R1) { Established = true }, sb, NoGuard(), newOutput));

            Assert.False(h.Engine.Owns(Device, sa));
            Assert.True(h.Engine.Owns(Device, sb));
            h.Engine.ProcessOwned(Device, sb, h.Input, h.Frame);
            Assert.Equal(new[] { "execute" }, newOutput.Events);
            Assert.Single(oldOutput.Events);
        }

        // ---- 割り当て 0・同時実行 ----

        [Fact]
        public void SteadyState_ProcessTriggersAndApplyFrameOutputs_DoNotAllocate()
        {
            var h = new Harness();
            var bindings = new List<IActionBinding>();
            var inputs = new List<FakeInputAction>();
            var buttons = new[] { DS4Controls.L1, DS4Controls.R1, DS4Controls.Cross, DS4Controls.Circle };
            for (int i = 0; i < 30; i++)
            {
                SpecialAction sa = h.NewSource();
                var input = new FakeInputAction(buttons[i % 4], buttons[(i + 1) % 4]) { Established = i % 3 != 0 };
                inputs.Add(input);
                IOutputAction output = i % 2 == 0 ? (IOutputAction)new RecordingOutput() : new RecordingFrameOutputAction();
                bindings.Add(new TriggerBinding(input, new[] { output }, NoGuard(), sa, i));
            }

            h.Registry.Replace(Device, bindings);
            h.Input.Active.Add(DS4Controls.L1);
            h.Input.Active.Add(DS4Controls.R1);

            for (int i = 0; i < 300; i++)
            {
                h.Engine.ProcessTriggers(Device, h.Input, h.Frame);
                h.Engine.ApplyFrameOutputs(Device, h.Input, h.Frame);
            }

            // 偽物の記録（リストへの追加）が割り当てを起こさないよう、計測中は記録を止める
            foreach (var b in bindings)
            {
                if (b.Outputs[0] is RecordingFrameOutputAction r) r.Record = false;
            }

            h.Frame.Record = false;

            // JIT の段階的な最適化（Tiered Compilation）が、計測の途中で 1 回だけ割り当てることがある（Phase8-Step7-3a の W1 の実行で 1 回だけ発生）。
            // 本物の割り当てなら、毎回起きる。3 回まで測り、最小値が 0 であることを確かめる
            long allocated = long.MaxValue;
            for (int attempt = 0; attempt < 3 && allocated != 0; attempt++)
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 1000; i++)
                {
                    h.Engine.ProcessTriggers(Device, h.Input, h.Frame);
                    h.Engine.ApplyFrameOutputs(Device, h.Input, h.Frame);
                }

                allocated = Math.Min(allocated, GC.GetAllocatedBytesForCurrentThread() - before);
            }

            Assert.True(allocated == 0, $"エンジンが {allocated} バイトを割り当てた");
        }

        [Fact]
        public void SteadyState_ProcessOwned_DoesNotAllocate()
        {
            var h = new Harness();
            var bindings = new List<IActionBinding>();
            var sources = new List<SpecialAction>();
            var buttons = new[] { DS4Controls.L1, DS4Controls.R1, DS4Controls.Cross, DS4Controls.Circle };
            for (int i = 0; i < 30; i++)
            {
                SpecialAction sa = h.NewSource();
                sources.Add(sa);
                var input = new FakeInputAction(buttons[i % 4], buttons[(i + 1) % 4]) { Established = i % 3 != 0 };
                bindings.Add(new TriggerBinding(input, new IOutputAction[] { new RecordingOutput() }, NoGuard(), sa, i));
            }

            h.Registry.Replace(Device, bindings);
            h.Input.Active.Add(DS4Controls.L1);
            h.Input.Active.Add(DS4Controls.R1);
            h.Frame.Record = false;

            for (int i = 0; i < 300; i++)
            {
                h.Engine.ReleaseSuppressed(Device, h.Input);
                foreach (SpecialAction sa in sources) h.Engine.ProcessOwned(Device, sa, h.Input, h.Frame);
            }

            // JIT の段階的な最適化（Tiered Compilation）が、計測の途中で割り当てることがある。本物の割り当てなら、毎回起きる。3 回まで測り、最小値が 0 であることを確かめる
            long allocated = long.MaxValue;
            for (int attempt = 0; attempt < 3 && allocated != 0; attempt++)
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 1000; i++)
                {
                    h.Engine.ReleaseSuppressed(Device, h.Input);
                    for (int k = 0; k < sources.Count; k++)
                    {
                        h.Engine.Owns(Device, sources[k]);
                        h.Engine.ProcessOwned(Device, sources[k], h.Input, h.Frame);
                    }
                }

                allocated = Math.Min(allocated, GC.GetAllocatedBytesForCurrentThread() - before);
            }

            Assert.True(allocated == 0, $"ProcessOwned／Owns が {allocated} バイトを割り当てた");
        }

        [Fact]
        public void ConcurrentProcessingAndReplacement_DoNotThrow()
        {
            var h = new Harness();
            var sources = Enumerable.Range(0, 8).Select(_ => h.NewSource()).ToArray();
            IActionBinding[] MakeList() => sources
                .Select((sa, i) => (IActionBinding)new TriggerBinding(new FakeInputAction(DS4Controls.L1) { Established = i % 2 == 0 },
                    new IOutputAction[] { new RecordingOutput() }, NoGuard(), sa, i)).ToArray();
            h.Register(MakeList());
            int stop = 0;
            Exception failure = null;

            var writer = System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < 2000; i++)
                    {
                        h.Register(MakeList());
                        h.Engine.OnProfileApplied(Device);
                        if (i % 100 == 0) h.Engine.OnDeviceDisconnected(Device);
                    }
                }
                catch (Exception ex) { failure = ex; }
                finally { System.Threading.Volatile.Write(ref stop, 1); }
            });

            var input = new FakeTriggerInput();
            input.Active.Add(DS4Controls.L1);
            try
            {
                while (System.Threading.Volatile.Read(ref stop) == 0)
                {
                    foreach (SpecialAction sa in sources)
                        h.Engine.ProcessOwned(Device, sa, input, h.Frame);
                    h.Engine.ApplyFrameOutputs(Device, input, h.Frame);
                    h.Engine.IsSuppressed(Device, DS4Controls.L1);
                }
            }
            catch (Exception ex) { failure = ex; }

            writer.Wait();
            Assert.Null(failure);
        }
    }
}
