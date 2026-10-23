using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// R-2 W3（R2-MacroToggleRepeat-Plan.md §3.3）: Execute Mode の Repeat (Toggle) が、Controls の出力の選択（<c>OutBinding</c>）と、
    /// 割り当ての説明文（<c>AssignmentDescriber</c>）に反映されること。
    /// </summary>
    public class R2W3ExecuteModeUiTests
    {
        [Theory]
        [InlineData(DS4KeyType.Macro, "Macro: 3 steps (Play once)")]
        [InlineData(DS4KeyType.Macro | DS4KeyType.RepeatMacro, "Macro: 3 steps (Repeat while held)")]
        [InlineData(DS4KeyType.Macro | DS4KeyType.RepeatLatchMacro, "Macro: 3 steps (Repeat (toggle))")]
        [InlineData(DS4KeyType.Macro | DS4KeyType.RepeatLatchMacro | DS4KeyType.ScanCode, "Macro: 3 steps (Repeat (toggle)) (SC)")]
        public void Describer_ShowsTheExecuteMode(DS4KeyType keyType, string expected)
        {
            Assert.Equal(expected, AssignmentDescriber.ForMacro(3, keyType, false, false, false));
        }

        [Theory]
        [InlineData(DS4KeyType.Macro, DS4KeyType.Macro)]
        [InlineData(DS4KeyType.Macro | DS4KeyType.RepeatMacro, DS4KeyType.Macro | DS4KeyType.RepeatMacro)]
        [InlineData(DS4KeyType.Macro | DS4KeyType.RepeatLatchMacro, DS4KeyType.Macro | DS4KeyType.RepeatLatchMacro)]
        public void WriteBind_KeepsTheMacroExecuteMode(DS4KeyType macroType, DS4KeyType expected)
        {
            var profile = TestRepositoryFactory.CreateProfileSettingsService();
            var settings = new DS4ControlSettings(DS4Controls.Cross);
            var bind = new OutBinding { outputType = OutBinding.OutType.Macro, macro = new[] { 350, 256 }, macroType = macroType };

            bind.WriteBind(settings, profile);

            Assert.Equal(expected, settings.keyType);
            Assert.Equal(DS4ControlSettings.ActionType.Macro, settings.actionType);
        }
    }
}
