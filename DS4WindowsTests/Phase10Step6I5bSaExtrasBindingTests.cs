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
    /// Phase10-Step6 I5b（Phase10-Step6-Plan.md §4）: SA に付けた Extras の実行。読み込みの部品が定義の <c>Extras</c> に解釈済みの値を入れ、
    /// <see cref="AssignmentBindingBuilder.BuildExtras"/> が Extras 用の別の束縛を作り、<see cref="ActionBindingBuilder"/> が SA の束縛の<b>前</b>に並べる。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step6I5bSaExtrasBindingTests
    {
        private const string Extras = "10,20,1,255,0,0,5,1,30";

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

        private sealed class SilentPort : IControlExtrasPort
        {
            public void SetRumble(int deviceIndex, byte heavy, byte light) { }
            public void ForceLightbar(int deviceIndex, DS4Color color, byte flash) { }
            public void ClearForcedLightbar(int deviceIndex) { }
            public void TrySetTemporarySensitivity(int deviceIndex, int sensitivity) { }
            public void RestoreSensitivity(int deviceIndex) { }
        }

        private static AssignmentBindingBuilder NewBuilder(bool withPort = true)
        {
            IProfileSettingsService settings = TestHost.GetService<IProfileSettingsService>();
            var factory = new SpecialActionOutputFactory(settings, TestHost.GetService<IManagedActionManager>(),
                new NoMacroPlayer(), new NoLauncher(), new NoSwitcher(), macroStringPlayer: new NoMacroStringPlayer());
            return new AssignmentBindingBuilder(factory, settings, withPort ? new SilentPort() : null);
        }

        private static SpecialAction WithExtras(SpecialAction sa)
        {
            sa.outputExtras = Extras;
            return sa;
        }

        [Fact]
        public void Reader_PutsTheInterpretedExtrasIntoTheDefinition_OrNull()
        {
            AssignmentDefinition with = SpecialActionAssignmentReader.Read(WithExtras(new SpecialAction("a", "Cross", "Key", "65", 0, "Press\n")));
            Assert.Equal(10, with.Extras.Value.HeavyRumble);
            Assert.Equal(20, with.Extras.Value.LightRumble);
            Assert.True(with.Extras.Value.LightbarEnabled);
            Assert.Equal(255, with.Extras.Value.Red);
            Assert.Equal(5, with.Extras.Value.Flash);
            Assert.True(with.Extras.Value.SensitivityEnabled);
            Assert.Equal(30, with.Extras.Value.Sensitivity);

            Assert.Null(SpecialActionAssignmentReader.Read(new SpecialAction("a", "Cross", "Key", "65", 0, "Press\n")).Extras);
            var broken = new SpecialAction("a", "Cross", "Key", "65", 0, "Press\n") { outputExtras = "1,2" };
            Assert.Null(SpecialActionAssignmentReader.Read(broken).Extras);
        }

        [Fact]
        public void BuildExtras_MakesAHeldComboBinding_WithTheSaTriggersAndDelay()
        {
            SpecialAction sa = WithExtras(new SpecialAction("p", "L1/Cross", "Program", "a.exe", 0.5));
            AssignmentDefinition definition = SpecialActionAssignmentReader.Read(sa);
            AssignmentBindingBuilder builder = NewBuilder();
            TriggerBinding main = builder.Build(0, definition, sa, 2);

            TriggerBinding extras = builder.BuildExtras(0, definition, sa, main.Policy);

            var input = Assert.IsType<ComboTriggerInputAction>(extras.Input);
            Assert.Equal(new[] { DS4Controls.L1, DS4Controls.Cross }, input.Triggers);
            Assert.Equal(0.5, input.DelaySeconds);
            Assert.False(input.InvertsOnRelease);
            Assert.IsType<ControlExtrasOutputAction>(Assert.Single(extras.Outputs));
            Assert.Equal($"Synthetic_SaExtras_0_{sa.id}", extras.Source.name);
            Assert.Equal(-1, extras.Index);
            Assert.False(extras.Policy.SuppressesTriggerButtons);
            Assert.False(extras.Policy.ResetsTriggerOutput);
            Assert.False(extras.Policy.ErasesOutputEveryFrame);
            Assert.True(extras.Policy.LogsRisingEdge);
            Assert.Equal(main.Policy.RespectsFreshPressGuard, extras.Policy.RespectsFreshPressGuard);
        }

        [Fact]
        public void BuildExtras_FollowsTheFreshPressRuleOfTheMainBinding()
        {
            // Button の Press は Fresh-press ガードを見ない。Macro は見る。Extras も同じにする
            SpecialAction button = WithExtras(new SpecialAction("b", "Cross", "Button", "18"));
            SpecialAction macro = WithExtras(new SpecialAction("m", "Cross", "Macro", "65"));
            AssignmentBindingBuilder builder = NewBuilder();

            foreach (SpecialAction sa in new[] { button, macro })
            {
                AssignmentDefinition definition = SpecialActionAssignmentReader.Read(sa);
                TriggerBinding main = builder.Build(0, definition, sa, 0);
                Assert.Equal(main.Policy.RespectsFreshPressGuard, builder.BuildExtras(0, definition, sa, main.Policy).Policy.RespectsFreshPressGuard);
            }

            Assert.False(builder.Build(0, SpecialActionAssignmentReader.Read(button), button, 0).Policy.RespectsFreshPressGuard);
            Assert.True(builder.Build(0, SpecialActionAssignmentReader.Read(macro), macro, 0).Policy.RespectsFreshPressGuard);
        }

        [Fact]
        public void BuildExtras_ForRunOnReleaseMacro_IsNotInverted_AndForMultiAction_HasNoDelay()
        {
            SpecialAction macro = WithExtras(new SpecialAction("m", "Cross", "Macro", "65", 0, "RunOnRelease"));
            SpecialAction multi = WithExtras(new SpecialAction("t", "Cross", "MultiAction", "1,2,3", 0.7));
            AssignmentBindingBuilder builder = NewBuilder();

            AssignmentDefinition macroDefinition = SpecialActionAssignmentReader.Read(macro);
            var macroInput = (ComboTriggerInputAction)builder.BuildExtras(0, macroDefinition, macro, builder.Build(0, macroDefinition, macro, 0).Policy).Input;
            Assert.False(macroInput.InvertsOnRelease);

            AssignmentDefinition multiDefinition = SpecialActionAssignmentReader.Read(multi);
            var multiInput = (ComboTriggerInputAction)builder.BuildExtras(0, multiDefinition, multi, builder.Build(0, multiDefinition, multi, 0).Policy).Input;
            Assert.Equal(0.0, multiInput.DelaySeconds);
        }

        [Fact]
        public void BuildExtras_IsNull_WithoutExtras_OrWithoutThePort()
        {
            SpecialAction plain = new SpecialAction("p", "Cross", "Program", "a.exe");
            SpecialAction withExtras = WithExtras(new SpecialAction("q", "Cross", "Program", "a.exe"));
            AssignmentBindingBuilder builder = NewBuilder();
            AssignmentDefinition plainDefinition = SpecialActionAssignmentReader.Read(plain);
            AssignmentDefinition extrasDefinition = SpecialActionAssignmentReader.Read(withExtras);
            BindingPolicy policy = builder.Build(0, plainDefinition, plain, 0).Policy;

            Assert.Null(builder.BuildExtras(0, plainDefinition, plain, policy));
            Assert.Null(NewBuilder(withPort: false).BuildExtras(0, extrasDefinition, withExtras, policy));
        }

        [Fact]
        public void ActionBindingBuilder_PutsTheExtrasBindingBeforeTheSaBinding_OnlyForSasWithExtras()
        {
            var provider = new FakeProvider();
            SpecialAction withExtras = WithExtras(new SpecialAction("p", "Cross", "Program", "a.exe"));
            SpecialAction plain = new SpecialAction("q", "Circle", "Program", "b.exe");
            provider.Add(withExtras);
            provider.Add(plain);
            var builder = new ActionBindingBuilder(provider, TestHost.GetService<IProfileSettingsService>(), TestHost.GetService<IManagedActionManager>(),
                new NoMacroPlayer(), new NoLauncher(), new NoSwitcher(), extrasPort: new SilentPort());

            IReadOnlyList<IActionBinding> result = builder.Build(0);

            Assert.Equal(3, result.Count);
            Assert.Equal($"Synthetic_SaExtras_0_{withExtras.id}", ((TriggerBinding)result[0]).Source.name);
            Assert.Same(withExtras, ((TriggerBinding)result[1]).Source);
            Assert.Same(plain, ((TriggerBinding)result[2]).Source);
        }
    }
}
