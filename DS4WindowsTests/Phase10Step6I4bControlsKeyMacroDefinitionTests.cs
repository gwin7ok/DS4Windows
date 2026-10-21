using System.Threading;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step6 I4b: Controls の Key と Macro が、読み込みの部品（<see cref="ControlsAssignmentReader"/>）と出力の部品
    /// （<see cref="SpecialActionOutputFactory"/>。方針は種別 × 由来の表）を通ること。Controls の束縛としての挙動は、Phase8 の特性化テストが守る。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step6I4bControlsKeyMacroDefinitionTests
    {
        private sealed class NoMacroPlayer : IMacroPlayer
        {
            public bool IsPlaying(int deviceIndex) => false;
            public void Play(int deviceIndex, SpecialAction action, CancellationToken cancellationToken = default) { }
            public void Stop(int deviceIndex) { }
        }

        [Theory]
        [InlineData(AssignmentOrigin.SpecialAction, SpecialAction.ActionTypeId.Key, true)]
        [InlineData(AssignmentOrigin.SpecialAction, SpecialAction.ActionTypeId.Macro, true)]
        [InlineData(AssignmentOrigin.SpecialAction, SpecialAction.ActionTypeId.MultiAction, false)]
        [InlineData(AssignmentOrigin.Controls, SpecialAction.ActionTypeId.Key, false)]
        [InlineData(AssignmentOrigin.Controls, SpecialAction.ActionTypeId.Macro, true)]
        [InlineData(AssignmentOrigin.Controls, SpecialAction.ActionTypeId.Program, true)]
        public void FreshPressGuardTable_For_DecidesByKindAndOrigin(AssignmentOrigin origin, SpecialAction.ActionTypeId kind, bool expected)
        {
            Assert.Equal(expected, FreshPressGuardTable.For(origin, kind));
        }

        [Fact]
        public void ReadKey_CarriesTheAliasAndFlags_WithTheFormerSyntheticName()
        {
            var dcs = new DS4ControlSettings(DS4Controls.L1) { actionType = DS4ControlSettings.ActionType.Key, keyType = DS4KeyType.ScanCode | DS4KeyType.Toggle };
            dcs.action.actionKey = 65;
            dcs.action.actionAlias = 30;

            Assert.True(ControlsAssignmentReader.TryReadKey(2, dcs, out AssignmentDefinition d, out SpecialAction source));

            Assert.Equal("Synthetic_Key_2_" + (int)DS4Controls.L1, source.name);
            Assert.Equal(SpecialAction.ActionTypeId.Key, source.typeID);
            Assert.Equal("65", source.details);
            Assert.Equal(DS4KeyType.ScanCode | DS4KeyType.Toggle, source.keyType);
            Assert.Equal(AssignmentOrigin.Controls, d.Origin);
            Assert.Equal(new[] { DS4Controls.L1 }, d.Triggers);
            Assert.Equal((ushort)65, d.Key.Value.LogicalKey);
            Assert.Equal(30u, d.Key.Value.AliasKey);
        }

        [Fact]
        public void ReadMacro_CarriesTheStepsAndTheThreeOptions()
        {
            var dcs = new DS4ControlSettings(DS4Controls.R1) { actionType = DS4ControlSettings.ActionType.Macro, keyType = DS4KeyType.Macro | DS4KeyType.RepeatMacro };
            dcs.action.actionMacro = new[] { 65, 350, 65 };
            dcs.special = new ControlSpecialItem { Type = SpecialAction.ActionTypeId.Macro, RunOnRelease = true, Synchronized = true, KeepKeyState = true };

            Assert.True(ControlsAssignmentReader.TryReadMacro(1, dcs, out AssignmentDefinition d, out SpecialAction source));

            Assert.Equal("Synthetic_Macro_1_" + (int)DS4Controls.R1, source.name);
            Assert.Equal("65/350/65", source.details);
            Assert.Equal(source.name, source.controls);
            Assert.True(source.pressRelease);
            Assert.True(source.synchronized);
            Assert.True(source.keepKeyState);
            Assert.Equal(new[] { 65, 350, 65 }, d.Macro);
            Assert.True(d.Item.RunOnRelease);
        }

        [Fact]
        public void Read_ReturnsFalse_ForOtherTypesAndMissingSteps()
        {
            Assert.False(ControlsAssignmentReader.TryReadKey(0, new DS4ControlSettings(DS4Controls.Cross), out _, out _));
            Assert.False(ControlsAssignmentReader.TryReadKey(0, null, out _, out _));
            // 手順そのものがない（null）Macro は読まない。空の手順は読む（Phase10-Step3。SA の Macro と同じく、実行して何も再生しない）
            var noSteps = new DS4ControlSettings(DS4Controls.Cross) { actionType = DS4ControlSettings.ActionType.Macro };
            noSteps.action.actionMacro = null;
            Assert.False(ControlsAssignmentReader.TryReadMacro(0, noSteps, out _, out _));
            Assert.False(ControlsAssignmentReader.TryReadMacro(0, new DS4ControlSettings(DS4Controls.Cross), out _, out _));
        }

        [Fact]
        public void MinimalFactory_MakesTheControlsPolicyForKeyAndMacro_AndNothingForTheOthers()
        {
            SpecialActionOutputFactory factory = SpecialActionOutputFactory.ForControlsKeyAndMacro(TestHost.GetService<IProfileSettingsService>(), new NoMacroPlayer());

            var key = new DS4ControlSettings(DS4Controls.L1) { actionType = DS4ControlSettings.ActionType.Key };
            key.action.actionKey = 65;
            ControlsAssignmentReader.TryReadKey(0, key, out AssignmentDefinition kd, out SpecialAction ks);
            SpecialActionOutputs keyOutputs = factory.Create(0, kd, ks, -1);
            Assert.False(keyOutputs.Policy.RespectsFreshPressGuard);
            Assert.False(keyOutputs.Policy.SuppressesTriggerButtons);
            Assert.False(keyOutputs.Policy.ResetsTriggerOutput);
            Assert.True(keyOutputs.Policy.ErasesOutputEveryFrame);

            var macro = new DS4ControlSettings(DS4Controls.R1) { actionType = DS4ControlSettings.ActionType.Macro };
            macro.action.actionMacro = new[] { 65 };
            ControlsAssignmentReader.TryReadMacro(0, macro, out AssignmentDefinition md, out SpecialAction ms);
            SpecialActionOutputs macroOutputs = factory.Create(0, md, ms, -1);
            Assert.True(macroOutputs.Policy.RespectsFreshPressGuard);
            Assert.True(macroOutputs.Policy.ErasesOutputEveryFrame);

            // 窓口が必要な種別は、未接続として null（束縛を作らない）
            var program = new ControlSpecialItem { Type = SpecialAction.ActionTypeId.Program, ProgramPath = "C:\\a.exe" };
            var pd = new AssignmentDefinition("p", "p", AssignmentOrigin.Controls, SpecialAction.ActionTypeId.Program,
                new[] { DS4Controls.Cross }, item: program);
            Assert.Null(factory.Create(0, pd, new SpecialAction("p", "Cross", "Program", "C:\\a.exe", 0, string.Empty), -1));
        }
    }
}
