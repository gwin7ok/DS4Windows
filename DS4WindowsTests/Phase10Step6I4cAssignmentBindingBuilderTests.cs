using System.Threading;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step6 I4c（Phase10-Step6-Plan.md §4）: 共通の組み立て（<see cref="AssignmentBindingBuilder"/>）の検査。入力の型と設定が、由来 × 種別で従来どおりであること。
    /// Controls の束縛としての挙動は Phase8・Phase10 Step4 系の特性化テストが、出力と方針は <c>Phase10Step4W2SpecialActionOutputFactoryTests</c> が守る。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step6I4cAssignmentBindingBuilderTests
    {
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

        private static AssignmentBindingBuilder NewBuilder()
        {
            IProfileSettingsService settings = TestHost.GetService<IProfileSettingsService>();
            var factory = new SpecialActionOutputFactory(settings, TestHost.GetService<IManagedActionManager>(),
                new NoMacroPlayer(), new NoLauncher(), new NoSwitcher(), macroStringPlayer: new NoMacroStringPlayer());
            return new AssignmentBindingBuilder(factory, settings);
        }

        [Fact]
        public void SaOrigin_MakesAComboInput_WithTheDelayAndTheReleaseInversion()
        {
            var sa = new SpecialAction("m", "L1/Cross", "Macro", "65/350/65", 0.5, "RunOnRelease");

            TriggerBinding binding = NewBuilder().Build(0, SpecialActionAssignmentReader.Read(sa), sa, 3);

            var input = Assert.IsType<ComboTriggerInputAction>(binding.Input);
            Assert.Equal(new[] { DS4Controls.L1, DS4Controls.Cross }, input.Triggers);
            Assert.Equal(0.5, input.DelaySeconds);
            Assert.True(input.InvertsOnRelease);
            Assert.Same(sa, binding.Source);
            Assert.Equal(3, binding.Index);
        }

        [Fact]
        public void SaOrigin_MultiAction_MakesTheIndependentMultiTapInput()
        {
            var sa = new SpecialAction("mt", "Cross", "MultiAction", "1,2,3");

            TriggerBinding binding = NewBuilder().Build(0, SpecialActionAssignmentReader.Read(sa), sa, 0);

            Assert.IsType<MultiTapInputAction>(binding.Input);
        }

        [Fact]
        public void ControlsOrigin_MakesAOneButtonInput_WithTheKindTheNatureAndTheDelay()
        {
            var sa = new SpecialAction("Synthetic_Special_0_1", "Synthetic", "Program", "a.exe", 2);
            sa.trigger = new System.Collections.Generic.List<DS4Controls> { DS4Controls.Cross };   // 合成の SA は、トリガーをこの形で持つ（コンストラクターの controls は名前扱い）
            AssignmentDefinition definition = SpecialActionAssignmentReader.Read(sa, AssignmentOrigin.Controls);

            TriggerBinding binding = NewBuilder().Build(0, definition, sa, -1);

            var input = Assert.IsType<ControlButtonInputAction>(binding.Input);
            Assert.Equal(ControlBindingKind.Special, input.Kind);
            Assert.Equal(ControlOutputNature.Digital, input.OutputNature);
            Assert.Equal(new[] { DS4Controls.Cross }, input.Triggers);
            Assert.False(input.InvertsOnRelease);
            Assert.Equal(-1, binding.Index);
        }

        [Fact]
        public void ControlsOrigin_MacroRunOnRelease_InvertsTheInput_AndUsesTheMacroKind()
        {
            var sa = new SpecialAction("Synthetic_Macro_0_1", "Synthetic", "Macro", "65", 0, "RunOnRelease");
            AssignmentDefinition definition = SpecialActionAssignmentReader.Read(sa, AssignmentOrigin.Controls);

            TriggerBinding binding = NewBuilder().Build(0, definition, sa, -1);

            var input = Assert.IsType<ControlButtonInputAction>(binding.Input);
            Assert.Equal(ControlBindingKind.Macro, input.Kind);
            Assert.True(input.InvertsOnRelease);
        }

        [Fact]
        public void WhenTheOutputCannotBeBuilt_ReturnsNull()
        {
            var sa = new SpecialAction("d", "Cross", "DisconnectBT", "");   // 窓口（切断のポート）が未接続

            Assert.Null(NewBuilder().Build(0, SpecialActionAssignmentReader.Read(sa), sa, 0));
        }
    }
}
