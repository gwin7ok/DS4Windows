using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using DS4Windows.Actions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step4 W1（Phase8-Step4-Plan.md §2.1）: 契約・登録簿・移行スイッチのテスト。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step4W1ContractTests
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

        private sealed class RecordingFrameOutput : IFrameOutput
        {
            public void ResetTriggerControl(DS4Controls control) { }
            public void PressX360(X360Controls control) { }
        }

        private static TriggerBinding NewBinding(int index = 0)
            => new TriggerBinding(new FakeInputAction(), new IOutputAction[] { new FakeOutputAction() }, BindingPolicy.Default, null, index);

        // ---- ActionBindingRegistry ----

        [Fact]
        public void Registry_ReturnsAnEmptyList_NeverNull()
        {
            var registry = new ActionBindingRegistry();

            for (int device = 0; device < ControllerLimits.MaxControllerCount; device++)
                Assert.Empty(registry.GetBindings(device));

            Assert.Empty(registry.GetBindings(-1));
            Assert.Empty(registry.GetBindings(ControllerLimits.MaxControllerCount));
        }

        [Fact]
        public void Registry_Replace_SwapsTheSnapshot_AndKeepsTheOldOneIntact()
        {
            var registry = new ActionBindingRegistry();
            var first = NewBinding(1);
            var second = NewBinding(2);

            registry.Replace(3, new IActionBinding[] { first });
            IReadOnlyList<IActionBinding> oldSnapshot = registry.GetBindings(3);

            registry.Replace(3, new IActionBinding[] { second, first });

            Assert.Single(oldSnapshot);                     // 読み取り中の古い一覧は壊れない
            Assert.Same(first, oldSnapshot[0]);
            Assert.Equal(2, registry.GetBindings(3).Count);
            Assert.Same(second, registry.GetBindings(3)[0]);
            Assert.Empty(registry.GetBindings(2));          // 他のデバイスには影響しない
        }

        [Fact]
        public void Registry_Replace_CopiesTheGivenList()
        {
            var registry = new ActionBindingRegistry();
            var list = new List<IActionBinding> { NewBinding(1) };

            registry.Replace(0, list);
            list.Clear();

            Assert.Single(registry.GetBindings(0));
        }

        [Fact]
        public void Registry_ReplaceWithNullOrEmpty_AndClear_GiveAnEmptyList()
        {
            var registry = new ActionBindingRegistry();
            registry.Replace(1, new IActionBinding[] { NewBinding() });

            registry.Replace(1, null);
            Assert.Empty(registry.GetBindings(1));

            registry.Replace(1, new IActionBinding[] { NewBinding() });
            registry.Clear(1);
            Assert.Empty(registry.GetBindings(1));

            registry.Replace(1, new IActionBinding[0]);
            Assert.Empty(registry.GetBindings(1));
        }

        [Fact]
        public void Registry_ValidatesItsArguments()
        {
            var registry = new ActionBindingRegistry();

            Assert.Throws<ArgumentOutOfRangeException>(() => registry.Replace(-1, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => registry.Replace(ControllerLimits.MaxControllerCount, null));
            Assert.Throws<ArgumentException>(() => registry.Replace(0, new IActionBinding[] { null }));
            registry.Clear(-1);                              // 範囲外の Clear は何もしない
            registry.Clear(ControllerLimits.MaxControllerCount);
        }

        [Fact]
        public void Registry_ConcurrentReplaceAndRead_NeverSeesABrokenList()
        {
            var registry = new ActionBindingRegistry();
            IActionBinding[] small = { NewBinding(1) };
            IActionBinding[] large = Enumerable.Range(0, 30).Select(i => (IActionBinding)NewBinding(i)).ToArray();
            int stop = 0;
            int broken = 0;

            var writer = System.Threading.Tasks.Task.Run(() =>
            {
                for (int i = 0; i < 20000; i++)
                    registry.Replace(0, i % 2 == 0 ? small : large);
                System.Threading.Volatile.Write(ref stop, 1);
            });

            while (System.Threading.Volatile.Read(ref stop) == 0)
            {
                IReadOnlyList<IActionBinding> snapshot = registry.GetBindings(0);
                int count = snapshot.Count;
                if (count != 0 && count != 1 && count != 30) broken++;
                for (int i = 0; i < count; i++)
                    if (snapshot[i] == null) broken++;
            }

            writer.Wait();
            Assert.Equal(0, broken);
        }

        // ---- BindingMigrationOptions ----

        [Fact]
        public void MigrationOptions_StartEmpty_AndFollowTheSet()
        {
            var options = new BindingMigrationOptions();
            Assert.Empty(options.MigratedTypes);
            foreach (SpecialAction.ActionTypeId type in Enum.GetValues(typeof(SpecialAction.ActionTypeId)))
                Assert.False(options.IsMigrated(type));

            options.MigratedTypes.Add(SpecialAction.ActionTypeId.Button);
            Assert.True(options.IsMigrated(SpecialAction.ActionTypeId.Button));
            Assert.False(options.IsMigrated(SpecialAction.ActionTypeId.Key));

            options.MigratedTypes.Remove(SpecialAction.ActionTypeId.Button);
            Assert.False(options.IsMigrated(SpecialAction.ActionTypeId.Button));
        }

        [Fact]
        public void Host_RegistersTheRegistryAndTheOptions_AsSingletons_WithTheMigratedTypesDefault()
        {
            var registry = TestHost.GetService<IActionRegistry>();
            var options = TestHost.GetService<BindingMigrationOptions>();

            Assert.IsType<ActionBindingRegistry>(registry);
            Assert.Same(registry, TestHost.GetService<IActionRegistry>());
            Assert.Same(options, TestHost.GetService<BindingMigrationOptions>());
            // 本番の既定の一覧（Phase8-Step7: PR ごとに 1 種別ずつ足す。現在は 7.1 の Button、7.2 の Key、7.3 の Macro、7.4 の Program、7.5a の Profile、7.6a の GyroCalibrate、7.6b の DisconnectBT、7.6c の BatteryCheck、7.6d のステアリング較正、7.7b の MultiAction）
            Assert.Equal(new[] { SpecialAction.ActionTypeId.BatteryCheck, SpecialAction.ActionTypeId.Button, SpecialAction.ActionTypeId.DisconnectBT, SpecialAction.ActionTypeId.GyroCalibrate, SpecialAction.ActionTypeId.Key, SpecialAction.ActionTypeId.Macro, SpecialAction.ActionTypeId.MultiAction, SpecialAction.ActionTypeId.Profile, SpecialAction.ActionTypeId.Program, SpecialAction.ActionTypeId.SASteeringWheelEmulationCalibrate }, options.MigratedTypes.OrderBy(t => t.ToString(), StringComparer.Ordinal).ToArray());
        }

        // ---- 出力の文脈の拡張 ----

        [Fact]
        public void OutputContextImpl_CarriesTheFrameAndRecordsTheEndFrameRequest()
        {
            var plain = new OutputContextImpl(2, null);
            Assert.Null(plain.Frame);
            Assert.False(plain.EndFrameRequested);

            var frame = new RecordingFrameOutput();
            var ctx = new OutputContextImpl(2, null, frame);
            Assert.Same(frame, ctx.Frame);
            Assert.Equal(2, ctx.Device);

            ctx.RequestEndFrame();
            Assert.True(ctx.EndFrameRequested);
        }

        [Fact]
        public void TheNewContracts_HaveTheDocumentedShape()
        {
            Assert.Equal(typeof(void), typeof(IFrameOutputAction).GetMethod("ApplyFrame").ReturnType);
            Assert.Equal(3, typeof(IFrameOutputAction).GetMethod("ApplyFrame").GetParameters().Length);
            Assert.True(typeof(IOutputAction).IsAssignableFrom(typeof(IFrameOutputAction)));
            Assert.True(typeof(IOutputAction).IsAssignableFrom(typeof(IHeldOutputAction)));
            Assert.NotNull(typeof(IHeldOutputAction).GetMethod("WhileHeld"));

            Assert.Equal(typeof(FrameResult), typeof(ITriggerDispatchEngine).GetMethod("ProcessOwned").ReturnType);
            foreach (string name in new[] { "Owns", "OwnsControl", "ProcessOwned", "ProcessTriggers", "ApplyFrameOutputs", "IsSuppressed",
                "AddSuppressed", "ReleaseSuppressed", "OnProfileApplied", "OnDeviceDisconnected" })
                Assert.NotNull(typeof(ITriggerDispatchEngine).GetMethod(name));
        }
    }
}
