using DS4Windows.Actions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>Phase10-Step6 I4a: Controls の束縛の出力の種類（デジタル／アナログ）を決める表。判定の中身は Phase10-Step4c W4 のまま。</summary>
    public class Phase10Step6I4aOutputNatureTableTests
    {
        [Theory]
        [InlineData(ControlBindingKind.Key, ControlOutputNature.Digital)]
        [InlineData(ControlBindingKind.Macro, ControlOutputNature.Digital)]
        [InlineData(ControlBindingKind.Special, ControlOutputNature.Digital)]
        [InlineData(ControlBindingKind.Extras, ControlOutputNature.Digital)]
        [InlineData(ControlBindingKind.ButtonMouse, ControlOutputNature.Digital)]
        [InlineData(ControlBindingKind.ButtonPad, ControlOutputNature.Analog)]
        [InlineData(ControlBindingKind.ButtonAnalogMouse, ControlOutputNature.Analog)]
        public void ForKind_DecidesTheNatureFromTheKind(ControlBindingKind kind, ControlOutputNature expected)
        {
            Assert.Equal(expected, ControlOutputNatureTable.ForKind(kind));
        }

        [Theory]
        [InlineData(false, PadTargetKind.Button, ControlOutputNature.Digital)]
        [InlineData(false, PadTargetKind.Axis, ControlOutputNature.Analog)]
        [InlineData(false, PadTargetKind.Trigger, ControlOutputNature.Analog)]
        [InlineData(true, PadTargetKind.Button, ControlOutputNature.Analog)]
        public void ForPad_DependsOnThePadTarget_AndErasingOnlyIsAnalog(bool eraseOnly, PadTargetKind target, ControlOutputNature expected)
        {
            Assert.Equal(expected, ControlOutputNatureTable.ForPad(eraseOnly, target));
        }
    }
}
