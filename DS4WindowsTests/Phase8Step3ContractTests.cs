using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using DS4Windows.Actions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step3 W1（Phase8-Step3-Plan.md §2.1）: 判定と束縛の契約・値型のテスト。
    /// 対象: <see cref="TriggerState"/>、<see cref="DelayClock"/>、<see cref="BindingPolicy"/>、<see cref="TriggerBinding"/>、
    /// 改訂した <see cref="IActionBinding"/>／<see cref="IInputAction"/>（<c>KeyActionBinding</c> の追従を含む）。
    /// 本番の挙動には配線されていない新しい型の、契約の形（不変性、既定値、削除したメンバー）を固定する。
    /// </summary>
    public class Phase8Step3ContractTests
    {
        private sealed class FakeInputAction : IInputAction
        {
            public string Name => "fake";
            public IReadOnlyList<DS4Controls> Triggers { get; } = new[] { DS4Controls.L1 };
            public TriggerState Evaluate(ITriggerInput input) => TriggerState.NotEstablished;
            public void Reset() { }
        }

        private sealed class FakeOutputAction : IOutputAction
        {
            public string Id => "fake-out";
            public void Execute(IOutputContext ctx) { }
            public void Stop(IOutputContext ctx) { }
        }

        // ---- TriggerState ----

        [Fact]
        public void TriggerState_Constants_AndDefaultValues()
        {
            Assert.False(TriggerState.NotEstablished.IsEstablished);
            Assert.Equal(TriggerState.NoGesture, TriggerState.NotEstablished.Gesture);
            Assert.True(TriggerState.Established.IsEstablished);
            Assert.Equal(TriggerState.NoGesture, TriggerState.Established.Gesture);

            var tap = new TriggerState(true, TriggerState.Tap);
            Assert.True(tap.IsEstablished);
            Assert.Equal(1, tap.Gesture);
            Assert.Equal(2, TriggerState.Hold);
            Assert.Equal(3, TriggerState.DoubleTap);

            TriggerState uninitialized = default;
            Assert.False(uninitialized.IsEstablished);
        }

        [Fact]
        public void TriggerState_IsAReadOnlyValueType()
        {
            Type type = typeof(TriggerState);
            Assert.True(type.IsValueType);
            Assert.True(type.GetProperties().All(p => !p.CanWrite || p.SetMethod == null), "TriggerState のプロパティに setter がある");
            Assert.All(type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public),
                f => Assert.True(f.IsInitOnly, $"TriggerState.{f.Name} が readonly ではない"));
        }

        // ---- DelayClock ----

        [Fact]
        public void DelayClock_StartsAtMinValue_AndResets()
        {
            var clock = new DelayClock();
            Assert.Equal(DateTime.MinValue, clock.Now);
            Assert.Equal(DateTime.MinValue, clock.Old);

            clock.Now = new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc);
            clock.Old = new DateTime(2026, 10, 19, 11, 0, 0, DateTimeKind.Utc);
            clock.Reset();

            Assert.Equal(DateTime.MinValue, clock.Now);
            Assert.Equal(DateTime.MinValue, clock.Old);
        }

        // ---- BindingPolicy ----

        [Fact]
        public void BindingPolicy_Default_HasTheDocumentedValues()
        {
            BindingPolicy p = BindingPolicy.Default;
            Assert.True(p.RespectsFreshPressGuard);
            Assert.False(p.BlocksWhileExecuting);
            Assert.Equal(UntriggerPolicy.None, p.Untrigger);
            Assert.Empty(p.UntriggerButtons);
            Assert.True(p.SuppressesTriggerButtons);
            Assert.True(p.ResetsTriggerOutput);
            Assert.True(p.StopOnRelease);
            Assert.Null(p.ExclusionGroup);
        }

        [Fact]
        public void BindingPolicy_HoldsTheGivenValues_AndCopiesTheUntriggerButtons()
        {
            var buttons = new List<DS4Controls> { DS4Controls.R1, DS4Controls.R2 };
            var p = new BindingPolicy(
                respectsFreshPressGuard: false,
                blocksWhileExecuting: true,
                untrigger: UntriggerPolicy.Buttons,
                untriggerButtons: buttons,
                suppressesTriggerButtons: false,
                resetsTriggerOutput: false,
                stopOnRelease: false,
                exclusionGroup: "shift-cross");

            Assert.False(p.RespectsFreshPressGuard);
            Assert.True(p.BlocksWhileExecuting);
            Assert.Equal(UntriggerPolicy.Buttons, p.Untrigger);
            Assert.Equal(new[] { DS4Controls.R1, DS4Controls.R2 }, p.UntriggerButtons.ToArray());
            Assert.False(p.SuppressesTriggerButtons);
            Assert.False(p.ResetsTriggerOutput);
            Assert.False(p.StopOnRelease);
            Assert.Equal("shift-cross", p.ExclusionGroup);

            // 呼び出し側のリストを書き換えても、方針は変わらない（不変）
            buttons.Clear();
            Assert.Equal(2, p.UntriggerButtons.Count);
        }

        [Fact]
        public void BindingPolicy_IsImmutable()
        {
            Type type = typeof(BindingPolicy);
            Assert.True(type.GetProperties().All(p => p.SetMethod == null), "BindingPolicy に setter がある");
            Assert.True(type.IsSealed);
        }

        // ---- TriggerBinding ----

        [Fact]
        public void TriggerBinding_HoldsItsParts_AndCopiesTheOutputs()
        {
            var input = new FakeInputAction();
            var output = new FakeOutputAction();
            var outputs = new List<IOutputAction> { output };
            var policy = new BindingPolicy(blocksWhileExecuting: true);
            var sa = new SpecialAction("p8s3_binding", "L1", "Program", @"C:\p8s3.exe", 0, string.Empty);

            var binding = new TriggerBinding(input, outputs, policy, sa, index: 7);

            Assert.Same(input, binding.Input);
            Assert.Same(policy, binding.Policy);
            Assert.Same(sa, binding.Source);
            Assert.Equal(7, binding.Index);
            Assert.Single(binding.Outputs);
            Assert.Same(output, binding.Outputs[0]);

            outputs.Clear(); // 渡したリストを書き換えても、束縛は変わらない（不変）
            Assert.Single(binding.Outputs);
        }

        [Fact]
        public void TriggerBinding_Validates_ItsArguments()
        {
            var input = new FakeInputAction();
            var outputs = new IOutputAction[] { new FakeOutputAction() };
            var policy = BindingPolicy.Default;

            Assert.Throws<ArgumentNullException>(() => new TriggerBinding(null, outputs, policy, null));
            Assert.Throws<ArgumentNullException>(() => new TriggerBinding(input, null, policy, null));
            Assert.Throws<ArgumentNullException>(() => new TriggerBinding(input, outputs, null, null));
            Assert.Throws<ArgumentException>(() => new TriggerBinding(input, new IOutputAction[] { null }, policy, null));

            var withoutSource = new TriggerBinding(input, outputs, policy, null);
            Assert.Null(withoutSource.Source);
            Assert.Equal(-1, withoutSource.Index);
        }

        // ---- 改訂した契約 ----

        [Fact]
        public void IActionBinding_NoLongerHasTheLifecycleMembers_AndHasThePolicyMembers()
        {
            string[] names = typeof(IActionBinding).GetMembers().Select(m => m.Name).ToArray();

            Assert.DoesNotContain("OnTriggered", names);
            Assert.DoesNotContain("OnReleased", names);
            foreach (string expected in new[] { "get_Input", "get_Outputs", "get_Policy", "get_Source", "get_Index" })
                Assert.Contains(expected, names);
        }

        [Fact]
        public void IInputAction_EvaluatesTheTriggerInput_AndReturnsAValueType()
        {
            var evaluate = typeof(IInputAction).GetMethod("Evaluate");
            Assert.NotNull(evaluate);
            Assert.Equal(typeof(TriggerState), evaluate.ReturnType);
            Assert.Single(evaluate.GetParameters());
            Assert.Equal(typeof(ITriggerInput), evaluate.GetParameters()[0].ParameterType);
            Assert.NotNull(typeof(IInputAction).GetProperty("Triggers"));
            Assert.NotNull(typeof(IInputAction).GetMethod("Reset"));
        }

        [Fact]
        public void KeyActionBinding_FollowsTheRevisedContract()
        {
            var sa = new SpecialAction("p8s3_key", "Cross", "Key", "65", 0, "Press\n");
            IActionBinding binding = new KeyActionBinding(sa);

            Assert.Null(binding.Input);
            Assert.Same(BindingPolicy.Default, binding.Policy);
            Assert.Same(sa, binding.Source);
            Assert.Equal(-1, binding.Index);
        }
    }
}
