using System.Threading;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step6 I3（Phase10-Step6-Plan.md §4）: 読み込みの部品 2 つ（<see cref="SpecialActionAssignmentReader"/>、<see cref="ControlsAssignmentReader"/>）と、
    /// 定義を受ける <see cref="ISpecialActionOutputFactory.Create(int, AssignmentDefinition, SpecialAction, int)"/> の検査。
    /// 全種別 × 由来の出力・方針が、旧 <c>Create(device, sa, index, forControls)</c> と同じであることは、<c>Phase10Step4W2SpecialActionOutputFactoryTests</c> ほか既存のテストが守る。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step6I3AssignmentReaderTests
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

        private static SpecialActionOutputFactory NewFactory()
            => new SpecialActionOutputFactory(TestHost.GetService<IProfileSettingsService>(), TestHost.GetService<IManagedActionManager>(),
                new NoMacroPlayer(), new NoLauncher(), new NoSwitcher(), macroStringPlayer: new NoMacroStringPlayer());

        [Fact]
        public void SaReader_KeyAndButton_CarryTheInterpretedValues_AndNoItem()
        {
            var key = new SpecialAction("k", "L1/Cross", "Key", "65 Scan Code", 0.25, "Toggle\n");
            AssignmentDefinition k = SpecialActionAssignmentReader.Read(key);

            Assert.Equal(AssignmentOrigin.SpecialAction, k.Origin);
            Assert.Equal(SpecialAction.ActionTypeId.Key, k.Kind);
            Assert.Equal(key.id, k.Id);
            Assert.Equal("k", k.Name);
            Assert.Equal(new[] { DS4Controls.L1, DS4Controls.Cross }, k.Triggers);
            Assert.Equal(0.25, k.HoldSeconds);
            Assert.Null(k.Item);
            Assert.Equal((ushort)65, k.Key.Value.LogicalKey);
            Assert.True(k.Key.Value.ScanCode);
            Assert.True(k.Key.Value.Toggle);
            Assert.Null(k.Button);

            AssignmentDefinition b = SpecialActionAssignmentReader.Read(new SpecialAction("b", "Cross", "Button", "18", 0, "Toggle\n"));
            Assert.Equal((X360Controls)18, b.Button.Value.Control);
            Assert.True(b.Button.Value.Toggle);
            Assert.Null(b.Key);
        }

        [Fact]
        public void SaReader_Macro_CarriesTheStepsKeyTypeAndOptions()
        {
            var sa = new SpecialAction("m", "Cross", "Macro", "65/350/65", 0, "RunOnRelease Repeat Scan Code");

            AssignmentDefinition d = SpecialActionAssignmentReader.Read(sa);

            Assert.Equal(new[] { 65, 350, 65 }, d.Macro);
            Assert.True(d.KeyType.HasFlag(DS4KeyType.RepeatMacro));
            Assert.True(d.KeyType.HasFlag(DS4KeyType.ScanCode));
            Assert.True(d.Item.RunOnRelease);
        }

        [Fact]
        public void SaReader_ReturnsNull_ForNullNoTypeOrNoTrigger()
        {
            Assert.Null(SpecialActionAssignmentReader.Read(null));
            Assert.Null(SpecialActionAssignmentReader.Read(new SpecialAction("n", "Cross", "Nothing", "")));
            var noTrigger = new SpecialAction("p", "Cross", "Program", "a.exe");
            noTrigger.trigger.Clear();
            Assert.Null(SpecialActionAssignmentReader.Read(noTrigger));
        }

        [Fact]
        public void SaReader_ControlsOrigin_UsesTheFirstTriggerOnly()
        {
            var sa = new SpecialAction("p", "Cross/Circle", "Program", "a.exe");

            AssignmentDefinition d = SpecialActionAssignmentReader.Read(sa, AssignmentOrigin.Controls);

            Assert.Equal(AssignmentOrigin.Controls, d.Origin);
            Assert.Equal(new[] { DS4Controls.Cross }, d.Triggers);
        }

        [Fact]
        public void ControlsReader_BuildsTheSameSyntheticSaAsTheFormerIdentity()
        {
            var item = new ControlSpecialItem
            {
                Type = SpecialAction.ActionTypeId.Program, HoldSeconds = 2, ProgramPath = "C:\\a.exe", ProgramArguments = "-x", HideWindow = true,
            };
            var dcs = new DS4ControlSettings(DS4Controls.Square) { actionType = DS4ControlSettings.ActionType.Special, special = item };

            Assert.True(ControlsAssignmentReader.TryReadSpecial(3, dcs, out AssignmentDefinition d, out SpecialAction source));

            string name = $"Synthetic_Special_3_{(int)DS4Controls.Square}";
            Assert.Equal(name, source.name);
            Assert.Equal(name, source.controls);
            Assert.Equal(SpecialAction.ActionTypeId.Program, source.typeID);
            Assert.Equal("Program", source.type);
            Assert.Equal(2, source.delayTime);
            Assert.Equal(new[] { DS4Controls.Square }, source.trigger);
            Assert.Equal("C:\\a.exe", source.details);
            Assert.Equal("$hidden-x", source.extra);

            Assert.Equal(AssignmentOrigin.Controls, d.Origin);
            Assert.Equal(name, d.Name);
            Assert.Equal(SpecialAction.ActionTypeId.Program, d.Kind);
            Assert.Equal(new[] { DS4Controls.Square }, d.Triggers);
            Assert.Equal(2, d.HoldSeconds);
            Assert.Equal("C:\\a.exe", d.Item.ProgramPath);
        }

        [Fact]
        public void ControlsReader_ProfileWithoutTarget_AndNonSpecialTypes_AreNotRead()
        {
            var profile = new DS4ControlSettings(DS4Controls.Cross)
            {
                actionType = DS4ControlSettings.ActionType.Special,
                special = new ControlSpecialItem { Type = SpecialAction.ActionTypeId.Profile, ProfileName = " " },
            };
            Assert.False(ControlsAssignmentReader.TryReadSpecial(0, profile, out AssignmentDefinition d1, out SpecialAction s1));
            Assert.Null(d1);
            Assert.Null(s1);

            Assert.False(ControlsAssignmentReader.TryReadSpecial(0, new DS4ControlSettings(DS4Controls.Cross), out _, out _));
            Assert.False(ControlsAssignmentReader.TryReadSpecial(0, null, out _, out _));
        }

        [Fact]
        public void Factory_DefinitionOverload_FollowsTheOrigin_ForThePolicy()
        {
            SpecialActionOutputFactory factory = NewFactory();
            var sa = new SpecialAction("p", "Cross", "Program", "a.exe");

            SpecialActionOutputs fromSa = factory.Create(0, SpecialActionAssignmentReader.Read(sa), sa, 0);
            SpecialActionOutputs fromControls = factory.Create(0, SpecialActionAssignmentReader.Read(sa, AssignmentOrigin.Controls), sa, -1);

            Assert.True(fromSa.Policy.SuppressesTriggerButtons);
            Assert.False(fromSa.Policy.ErasesOutputEveryFrame);
            Assert.False(fromControls.Policy.SuppressesTriggerButtons);
            Assert.True(fromControls.Policy.ErasesOutputEveryFrame);
            Assert.True(fromSa.Policy.BlocksWhileExecuting);
            Assert.True(fromControls.Policy.BlocksWhileExecuting);
        }

        [Fact]
        public void Factory_MultiAction_SplitsTheThreeMacrosFromTheItem()
        {
            SpecialActionOutputFactory factory = NewFactory();
            var sa = new SpecialAction("m", "Cross", "MultiAction", "1/2,3");

            SpecialActionOutputs outputs = factory.Create(0, SpecialActionAssignmentReader.Read(sa), sa, 0);

            Assert.True(outputs.IsMultiTap);
            Assert.Equal(3, outputs.Outputs.Length);
            Assert.True(outputs.Policy.RoutesOutputsByGesture);
        }

        [Fact]
        public void Factory_KeyAndButton_WithoutTheirValues_AreNotBuilt()
        {
            SpecialActionOutputFactory factory = NewFactory();
            var sa = new SpecialAction("k", "Cross", "Key", "65", 0, "Press\n");
            var noValues = new AssignmentDefinition("k", "k", AssignmentOrigin.SpecialAction, SpecialAction.ActionTypeId.Key, new[] { DS4Controls.Cross });

            Assert.Null(factory.Create(0, noValues, sa, 0));
        }
    }
}
