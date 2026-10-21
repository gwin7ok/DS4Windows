using System.Collections.Generic;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step5 S5b（Phase10-Step6 後の Step5。SA 側のアナログ出力）: SA の Button（Output Key/Mouse/Button）の出力先が、パッドの軸・トリガー、マウス移動のとき、
    /// 強さ（0〜100%）の固定値で出す。Controls のボタンが元のときの出力（軸は端［255／0］、トリガーは 255、マウス移動はボタンが元のときの量）を 100% とし、比例させる。
    /// デジタルな出力先（ボタン、マウスのボタン、ホイール）は、従来どおり <c>PressX360</c>。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step5SaAnalogOutputTests
    {
        private sealed class RecordingFrame : IPadFrameOutput, IMouseMoveFrameOutput
        {
            public readonly List<(DS4Controls Out, byte Value)> Axes = new List<(DS4Controls, byte)>();
            public readonly List<(DS4Controls Out, byte Value)> Triggers = new List<(DS4Controls, byte)>();
            public readonly List<X360Controls> Pressed = new List<X360Controls>();
            public readonly List<double> DeltaX = new List<double>();
            public readonly List<double> DeltaY = new List<double>();

            public void ResetTriggerControl(DS4Controls control) { }
            public void PressX360(X360Controls control) => Pressed.Add(control);
            public void SetAxisPair(DS4Controls outControl, byte value) => Axes.Add((outControl, value));
            public void SetTrigger(DS4Controls outControl, byte value) => Triggers.Add((outControl, value));
            public void SetButton(DS4Controls outControl) { }
            public void SetMouseDeltaX(double value) => DeltaX.Add(value);
            public void SetMouseDeltaY(double value) => DeltaY.Add(value);
        }

        private sealed class FakeInput : ITriggerInput, IFixedMouseInput
        {
            public int Device => 0;
            public bool IsActive(DS4Controls control) => false;
            public bool WasActive(DS4Controls control) => false;
            public DelayClock Delay { get; } = new DelayClock();
            public System.DateTime UtcNow => System.DateTime.UtcNow;
            public long TickCount64 => 0;
            public double GetFixedMouseMovement(int direction, double fraction) => 10.0 * fraction;
        }

        private sealed class FakeContext : IOutputContext, IOutputContextInput
        {
            public FakeContext(IFrameOutput frame) { Frame = frame; }
            public int Device => 0;
            public IVirtualKBM OutputHandler => null;
            public IFrameOutput Frame { get; }
            public ITriggerInput Input { get; } = new FakeInput();
            public void RequestEndFrame() { }
        }

        private static ButtonOutputAction Action(X360Controls control, int strength, bool toggle = false)
        {
            var sa = new SpecialAction("analog", "Cross", "Button", ((int)control).ToString());
            return new ButtonOutputAction(sa, new ButtonBindingValues(true, control, toggle, strength), TestHost.GetService<IManagedActionManager>());
        }

        [Theory]
        [InlineData(X360Controls.LXPos, 100, 255)]
        [InlineData(X360Controls.LXNeg, 100, 0)]
        [InlineData(X360Controls.RYPos, 100, 255)]
        [InlineData(X360Controls.LYNeg, 100, 0)]
        [InlineData(X360Controls.LXPos, 50, 192)]   // 128 + 127 × 0.5 ≒ 191.5 → 192
        [InlineData(X360Controls.LXNeg, 50, 64)]    // 128 − 128 × 0.5
        [InlineData(X360Controls.LXPos, 0, 128)]
        [InlineData(X360Controls.LXNeg, 0, 128)]
        public void AnAxisTarget_IsPushedFromNeutralTowardsTheEnd_InProportionToTheStrength(X360Controls control, int strength, byte expected)
        {
            var frame = new RecordingFrame();

            Action(control, strength).ApplyFrame(new FakeContext(frame), active: true, risingEdge: true);

            (DS4Controls Out, byte Value) written = Assert.Single(frame.Axes);
            Assert.Equal(expected, written.Value);
            Assert.Empty(frame.Pressed);   // デジタルのボタンとしては出さない
        }

        [Theory]
        [InlineData(X360Controls.LT, 100, 255)]
        [InlineData(X360Controls.RT, 100, 255)]
        [InlineData(X360Controls.LT, 40, 102)]
        [InlineData(X360Controls.RT, 0, 0)]
        public void ATriggerTarget_IsPressed255TimesTheStrength(X360Controls control, int strength, byte expected)
        {
            var frame = new RecordingFrame();

            Action(control, strength).ApplyFrame(new FakeContext(frame), active: true, risingEdge: true);

            Assert.Equal(expected, Assert.Single(frame.Triggers).Value);
            Assert.Empty(frame.Pressed);
        }

        [Theory]
        [InlineData(X360Controls.MouseUp, 100, 0, -10.0)]
        [InlineData(X360Controls.MouseDown, 100, 0, 10.0)]
        [InlineData(X360Controls.MouseLeft, 100, 1, -10.0)]
        [InlineData(X360Controls.MouseRight, 100, 1, 10.0)]
        [InlineData(X360Controls.MouseRight, 30, 1, 3.0)]
        public void AMouseMoveTarget_MovesByTheFixedAmountTimesTheStrength(X360Controls control, int strength, int axis, double expected)
        {
            var frame = new RecordingFrame();

            Action(control, strength).ApplyFrame(new FakeContext(frame), active: true, risingEdge: true);

            List<double> written = axis == 0 ? frame.DeltaY : frame.DeltaX;
            Assert.Equal(expected, Assert.Single(written), 6);
            Assert.Empty(axis == 0 ? frame.DeltaX : frame.DeltaY);
            Assert.Empty(frame.Pressed);
        }

        [Theory]
        [InlineData(X360Controls.A)]
        [InlineData(X360Controls.LB)]
        [InlineData(X360Controls.LeftMouse)]
        [InlineData(X360Controls.WUP)]
        [InlineData(X360Controls.TouchpadClick)]
        public void ADigitalTarget_IsStillPressedAsAButton_AndTheStrengthIsIgnored(X360Controls control)
        {
            var frame = new RecordingFrame();

            Action(control, 30).ApplyFrame(new FakeContext(frame), active: true, risingEdge: true);

            Assert.Equal(new[] { control }, frame.Pressed.ToArray());
            Assert.Empty(frame.Axes);
            Assert.Empty(frame.Triggers);
            Assert.Empty(frame.DeltaX);
            Assert.Empty(frame.DeltaY);
        }

        [Fact]
        public void WhileNotActive_NothingIsOutput()
        {
            var frame = new RecordingFrame();

            Action(X360Controls.LXPos, 100).ApplyFrame(new FakeContext(frame), active: false, risingEdge: false);
            Action(X360Controls.MouseUp, 100).ApplyFrame(new FakeContext(frame), active: false, risingEdge: false);

            Assert.Empty(frame.Axes);
            Assert.Empty(frame.DeltaY);
        }

        [Fact]
        public void TheStrength_FromTheSa_ReachesTheOutput_ThroughTheSharedReaderAndFactory()
        {
            var sa = new SpecialAction("analog", "Cross", "Button", ((int)X360Controls.LT).ToString(), 0, "Press\n") { analogStrength = 40 };

            AssignmentDefinition definition = SpecialActionAssignmentReader.Read(sa);

            Assert.Equal(40, definition.Button.Value.Strength);
            Assert.Equal(40, ActionBindingBuilder.InterpretButton(sa).Strength);
        }

        [Fact]
        public void TheValues_ClampTheStrengthTo0To100()
        {
            Assert.Equal(100, new ButtonBindingValues(true, X360Controls.A, false, 250).Strength);
            Assert.Equal(0, new ButtonBindingValues(true, X360Controls.A, false, -5).Strength);
            Assert.Equal(100, new ButtonBindingValues(true, X360Controls.A, false).Strength);
        }
    }
}
