using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step4 W2（Phase10-Step4-Plan.md §3.3）: 種別ごとの出力アクション・方針の作り方を、共通の部品（<see cref="SpecialActionOutputFactory"/>）へ取り出した。
    /// SA の束縛が変わらないこと（W0 の一覧表のテストが守る）に加えて、部品そのものの契約を固定する。
    /// </summary>
    public class Phase10Step4W2SpecialActionOutputFactoryTests
    {
        private sealed class FakeProvider : IProfileActionProvider
        {
            private readonly List<SpecialAction> _actions = new List<SpecialAction>();

            public void Add(SpecialAction sa) => _actions.Add(sa);

            public IReadOnlyList<string> GetProfileActionNames(int deviceIndex) => _actions.Select(a => a.name).ToArray();
            public SpecialAction GetProfileAction(int deviceIndex, string actionName) => _actions.FirstOrDefault(a => a.name == actionName);
            public int GetProfileActionCount(int deviceIndex) => _actions.Count;
            public int GetProfileActionIndexOf(int deviceIndex, string actionName) => _actions.FindIndex(a => a.name == actionName);
            public IReadOnlyList<string> GetProfileActionsRaw(int deviceIndex) => _actions.Select(a => a.name).ToArray();
        }

        private sealed class NoMacroPlayer : IMacroPlayer
        {
            public bool IsPlaying(int deviceIndex) => false;
            public void Play(int deviceIndex, SpecialAction action, CancellationToken cancellationToken = default) { }
            public void Stop(int deviceIndex) { }
        }

        private sealed class NoLauncher : IProcessLauncher
        {
            public void Launch(string filePath) { }
            public void Launch(string fileName, string arguments, bool useShellExecute, bool hidden) { }
        }

        private sealed class NoSwitcher : IProfileSwitcher
        {
            public void SwitchProfile(int deviceIndex, SpecialAction action) { }
            public void RestoreProfile(int deviceIndex) { }
        }

        private sealed class NoMacroStringPlayer : IMacroStringPlayer
        {
            public void PlayMacroString(int deviceIndex, string macro) { }
        }

        private static SpecialActionOutputFactory NewFactory(bool withPorts)
        {
            return withPorts
                ? new SpecialActionOutputFactory(TestHost.GetService<IProfileSettingsService>(), TestHost.GetService<IManagedActionManager>(),
                    new NoMacroPlayer(), new NoLauncher(), new NoSwitcher(), gyroCalibration: new RecordingGyroCalibrationPort(),
                    disconnectPort: new RecordingControllerDisconnectPort(), batteryCheckPort: new RecordingBatteryCheckPort(),
                    wheelCalibrationPort: new RecordingWheelCalibrationPort(), macroStringPlayer: new NoMacroStringPlayer())
                : new SpecialActionOutputFactory(TestHost.GetService<IProfileSettingsService>(), TestHost.GetService<IManagedActionManager>(),
                    new NoMacroPlayer(), new NoLauncher(), new NoSwitcher());
        }

        private static IEnumerable<SpecialAction> EveryType()
        {
            yield return new SpecialAction("button", "Cross", "Button", "18");
            yield return new SpecialAction("key", "Cross", "Key", "65", 0, "Press\n");
            yield return new SpecialAction("macro", "Cross", "Macro", "65/350/65");
            yield return new SpecialAction("macro-release", "Cross", "Macro", "65/350/65", 0, "RunOnRelease");
            yield return new SpecialAction("prog", "Cross", "Program", "C:\\x.exe");
            yield return new SpecialAction("prof", "Cross", "Profile", "other");
            yield return new SpecialAction("prof-auto", "Cross", "Profile", "other", 0, "AutomaticUntrigger");
            yield return new SpecialAction("gyro", "Cross", "GyroCalibration", "");
            yield return new SpecialAction("disc", "Cross", "DisconnectBT", "", 0.5);
            yield return new SpecialAction("batt", "Cross", "BatteryCheck", "1,2,3|4,5,6");
            yield return new SpecialAction("wheel", "Cross", "SASteeringWheelEmulationCalibrate", "");
            yield return new SpecialAction("multi", "Cross", "MultiAction", "1,2,3");
        }

        private static void AssertSamePolicy(BindingPolicy expected, BindingPolicy actual)
        {
            Assert.Equal(expected.RespectsFreshPressGuard, actual.RespectsFreshPressGuard);
            Assert.Equal(expected.BlocksWhileExecuting, actual.BlocksWhileExecuting);
            Assert.Equal(expected.Untrigger, actual.Untrigger);
            Assert.Equal(expected.SuppressesTriggerButtons, actual.SuppressesTriggerButtons);
            Assert.Equal(expected.ResetsTriggerOutput, actual.ResetsTriggerOutput);
            Assert.Equal(expected.StopOnRelease, actual.StopOnRelease);
            Assert.Equal(expected.ExclusionGroup, actual.ExclusionGroup);
            Assert.Equal(expected.RoutesOutputsByGesture, actual.RoutesOutputsByGesture);
            Assert.Equal(expected.ErasesOutputEveryFrame, actual.ErasesOutputEveryFrame);
        }

        [Fact]
        public void TheSaBuilder_UsesTheSameOutputsAndPolicyAsTheFactory_ForEveryType()
        {
            SpecialActionOutputFactory factory = NewFactory(withPorts: true);

            foreach (SpecialAction sa in EveryType())
            {
                var provider = new FakeProvider();
                provider.Add(sa);
                var builder = new ActionBindingBuilder(provider, TestHost.GetService<IProfileSettingsService>(), TestHost.GetService<IManagedActionManager>(),
                    new NoMacroPlayer(), new NoLauncher(), new NoSwitcher(), outputFactory: factory);

                TriggerBinding binding = Assert.IsType<TriggerBinding>(Assert.Single(builder.Build(0)));
                SpecialActionOutputs outputs = factory.Create(0, sa, 0);

                Assert.NotNull(outputs);
                Assert.Equal(outputs.Outputs.Select(o => o.GetType()), binding.Outputs.Select(o => o.GetType()));
                AssertSamePolicy(outputs.Policy, binding.Policy);
                Assert.Equal(outputs.IsMultiTap ? typeof(MultiTapInputAction) : typeof(ComboTriggerInputAction), binding.Input.GetType());
            }
        }

        [Fact]
        public void ABuilderWithoutAFactory_BuildsItsOwn_AndGivesTheSameBindings()
        {
            SpecialActionOutputFactory factory = NewFactory(withPorts: true);

            foreach (SpecialAction sa in EveryType())
            {
                TriggerBinding own = BuildWith(sa, factory: null);
                TriggerBinding shared = BuildWith(sa, factory);

                Assert.Equal(own == null, shared == null);
                if (own == null) continue;

                Assert.Equal(own.Outputs.Select(o => o.GetType()), shared.Outputs.Select(o => o.GetType()));
                AssertSamePolicy(own.Policy, shared.Policy);
                Assert.Equal(own.Input.GetType(), shared.Input.GetType());
            }
        }

        private static TriggerBinding BuildWith(SpecialAction sa, ISpecialActionOutputFactory factory)
        {
            var provider = new FakeProvider();
            provider.Add(sa);
            ActionBindingBuilder builder = factory != null
                ? new ActionBindingBuilder(provider, TestHost.GetService<IProfileSettingsService>(), TestHost.GetService<IManagedActionManager>(),
                    new NoMacroPlayer(), new NoLauncher(), new NoSwitcher(), outputFactory: factory)
                : new ActionBindingBuilder(provider, TestHost.GetService<IProfileSettingsService>(), TestHost.GetService<IManagedActionManager>(),
                    new NoMacroPlayer(), new NoLauncher(), new NoSwitcher(), gyroCalibration: new RecordingGyroCalibrationPort(),
                    disconnectPort: new RecordingControllerDisconnectPort(), batteryCheckPort: new RecordingBatteryCheckPort(),
                    wheelCalibrationPort: new RecordingWheelCalibrationPort(), macroStringPlayer: new NoMacroStringPlayer());

            IReadOnlyList<IActionBinding> result = builder.Build(0);
            return result.Count == 0 ? null : Assert.IsType<TriggerBinding>(result[0]);
        }

        [Fact]
        public void ForControls_ChangesOnlyTheThreeControlsFlagsOfThePolicy_ForEveryType()
        {
            SpecialActionOutputFactory factory = NewFactory(withPorts: true);

            foreach (SpecialAction sa in EveryType())
            {
                SpecialActionOutputs plain = factory.Create(0, sa, 0);
                SpecialActionOutputs erasing = factory.Create(0, sa, 0, forControls: true);

                // Phase10-Step4 W4: Controls 用は、Controls の既存の束縛と同じ 3 つのフラグ（抑制しない・出力を消さない・割り当てたボタンの出力を毎フレーム消す）
                Assert.True(plain.Policy.SuppressesTriggerButtons);
                Assert.True(plain.Policy.ResetsTriggerOutput);
                Assert.False(plain.Policy.ErasesOutputEveryFrame);
                Assert.False(erasing.Policy.SuppressesTriggerButtons);
                Assert.False(erasing.Policy.ResetsTriggerOutput);
                Assert.True(erasing.Policy.ErasesOutputEveryFrame);
                Assert.Equal(plain.Policy.RespectsFreshPressGuard, erasing.Policy.RespectsFreshPressGuard);
                Assert.Equal(plain.Policy.BlocksWhileExecuting, erasing.Policy.BlocksWhileExecuting);
                Assert.Equal(plain.Policy.Untrigger, erasing.Policy.Untrigger);
                Assert.Equal(plain.Policy.StopOnRelease, erasing.Policy.StopOnRelease);
                Assert.Equal(plain.Policy.RoutesOutputsByGesture, erasing.Policy.RoutesOutputsByGesture);
                Assert.Equal(plain.InvertOnRelease, erasing.InvertOnRelease);
                Assert.Equal(plain.IsMultiTap, erasing.IsMultiTap);
                Assert.Equal(plain.Outputs.Select(o => o.GetType()), erasing.Outputs.Select(o => o.GetType()));
            }
        }

        [Fact]
        public void TheDefaultPolicy_IsSharedForTheSa_AndANewOneIsMadeOnlyForControls()
        {
            SpecialActionOutputFactory factory = NewFactory(withPorts: true);
            var key = new SpecialAction("key", "Cross", "Key", "65", 0, "Press\n");

            Assert.Same(BindingPolicy.Default, factory.Create(0, key, 0).Policy);
            Assert.NotSame(BindingPolicy.Default, factory.Create(0, key, 0, forControls: true).Policy);
        }

        [Fact]
        public void TheInputKind_FollowsTheType()
        {
            SpecialActionOutputFactory factory = NewFactory(withPorts: true);

            SpecialActionOutputs onPress = factory.Create(0, new SpecialAction("m", "Cross", "Macro", "65/350/65"), 0);
            SpecialActionOutputs onRelease = factory.Create(0, new SpecialAction("m", "Cross", "Macro", "65/350/65", 0, "RunOnRelease"), 0);
            SpecialActionOutputs multi = factory.Create(0, new SpecialAction("x", "Cross", "MultiAction", "1,2,3"), 0);
            SpecialActionOutputs key = factory.Create(0, new SpecialAction("k", "Cross", "Key", "65"), 0);

            Assert.False(onPress.InvertOnRelease);
            Assert.True(onRelease.InvertOnRelease);
            Assert.False(onRelease.IsMultiTap);
            Assert.True(multi.IsMultiTap);
            Assert.False(multi.InvertOnRelease);
            Assert.False(key.InvertOnRelease);
            Assert.False(key.IsMultiTap);
        }

        [Fact]
        public void MultiAction_HasThreeGestureOutputs_EvenWhenTheMacrosAreMissing()
        {
            SpecialActionOutputFactory factory = NewFactory(withPorts: true);

            SpecialActionOutputs full = factory.Create(0, new SpecialAction("x", "Cross", "MultiAction", "1,2,3"), 0);
            SpecialActionOutputs shortOne = factory.Create(0, new SpecialAction("y", "Cross", "MultiAction", "1"), 0);

            Assert.Equal(3, full.Outputs.Length);
            Assert.Equal(3, shortOne.Outputs.Length);
            Assert.All(shortOne.Outputs, o => Assert.IsType<MacroStringAction>(o));
            Assert.True(full.Policy.RoutesOutputsByGesture);
            Assert.False(full.Policy.RespectsFreshPressGuard);
            Assert.False(full.Policy.StopOnRelease);
        }

        [Fact]
        public void WithoutThePorts_TheirTypesGiveNull_AndTheOthersStillWork()
        {
            SpecialActionOutputFactory factory = NewFactory(withPorts: false);

            Assert.Null(factory.Create(0, new SpecialAction("g", "Cross", "GyroCalibration", ""), 0));
            Assert.Null(factory.Create(0, new SpecialAction("d", "Cross", "DisconnectBT", ""), 0));
            Assert.Null(factory.Create(0, new SpecialAction("b", "Cross", "BatteryCheck", "1,2,3|4,5,6"), 0));
            Assert.Null(factory.Create(0, new SpecialAction("w", "Cross", "SASteeringWheelEmulationCalibrate", ""), 0));
            Assert.Null(factory.Create(0, new SpecialAction("x", "Cross", "MultiAction", "1,2,3"), 0));

            Assert.NotNull(factory.Create(0, new SpecialAction("k", "Cross", "Key", "65"), 0));
            Assert.NotNull(factory.Create(0, new SpecialAction("m", "Cross", "Macro", "65"), 0));
            Assert.NotNull(factory.Create(0, new SpecialAction("p", "Cross", "Program", "C:\\x.exe"), 0));
            Assert.NotNull(factory.Create(0, new SpecialAction("f", "Cross", "Profile", "other"), 0));
            Assert.NotNull(factory.Create(0, new SpecialAction("b", "Cross", "Button", "18"), 0));
        }

        [Fact]
        public void ANoneType_GivesNull()
        {
            Assert.Null(NewFactory(withPorts: true).Create(0, new SpecialAction("n", "Cross", "None", ""), 0));
        }

        [Fact]
        public void ANullAction_Throws_AndTheConstructorRejectsNullRequiredArguments()
        {
            SpecialActionOutputFactory factory = NewFactory(withPorts: true);
            Assert.Throws<ArgumentNullException>(() => factory.Create(0, null, 0));

            IProfileSettingsService settings = TestHost.GetService<IProfileSettingsService>();
            IManagedActionManager manager = TestHost.GetService<IManagedActionManager>();
            Assert.Throws<ArgumentNullException>(() => new SpecialActionOutputFactory(null, manager, new NoMacroPlayer(), new NoLauncher(), new NoSwitcher()));
            Assert.Throws<ArgumentNullException>(() => new SpecialActionOutputFactory(settings, null, new NoMacroPlayer(), new NoLauncher(), new NoSwitcher()));
            Assert.Throws<ArgumentNullException>(() => new SpecialActionOutputFactory(settings, manager, null, new NoLauncher(), new NoSwitcher()));
            Assert.Throws<ArgumentNullException>(() => new SpecialActionOutputFactory(settings, manager, new NoMacroPlayer(), null, new NoSwitcher()));
            Assert.Throws<ArgumentNullException>(() => new SpecialActionOutputFactory(settings, manager, new NoMacroPlayer(), new NoLauncher(), null));
        }

        [Fact]
        public void TheOutputs_AreImmutableCopies_AndRejectNullArguments()
        {
            var outputs = new IOutputAction[] { new NoOutput() };
            var result = new SpecialActionOutputs(outputs, BindingPolicy.Default);

            outputs[0] = null;
            Assert.NotNull(result.Outputs[0]);
            Assert.Throws<ArgumentNullException>(() => new SpecialActionOutputs(null, BindingPolicy.Default));
            Assert.Throws<ArgumentNullException>(() => new SpecialActionOutputs(new IOutputAction[0], null));
        }

        private sealed class NoOutput : IOutputAction
        {
            public string Id => "none";
            public void Execute(IOutputContext context) { }
            public void Stop(IOutputContext context) { }
        }

        [Fact]
        public void TheHost_ResolvesTheFactoryAsASingleton()
        {
            ISpecialActionOutputFactory first = TestHost.GetService<ISpecialActionOutputFactory>();

            Assert.NotNull(first);
            Assert.IsType<SpecialActionOutputFactory>(first);
            Assert.Same(first, TestHost.GetService<ISpecialActionOutputFactory>());
        }
    }
}
