using System;
using System.Collections.Generic;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// R-5（Release-Backlog.md。ユーザー決定 2026-10-23）: SA の割り当ての押下の判定。
    /// ① 複数トリガーの組み合わせ、② 割り当て先がデジタル: 閾値なしのデジタルな判定（L2／R2 は 0 を超えたら、スティックの軸は中心から少しでも傾いたら。値は、デッドゾーンの補正の後）。
    /// ③ トリガーが単体で、割り当て先がアナログ（パッドの軸・トリガー、マウス移動）: トリガーの値に比例したアナログな出力（強さは上限）。
    /// Controls のデジタルな出力も、同じ判定（<see cref="ITriggerInput.IsDigitalActive"/>。ユーザー決定 2026-10-23。Phase10-Step4c W4 の「SA と同じ判定」は維持し、閾値だけをなくした）。
    /// アナログな出力（Controls のパッドの軸・トリガー、マウスの移動）は、従来の Controls の判定（<c>IsControlActive</c>）のまま。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class R5SaDigitalJudgementTests
    {
        private const int Device = 5;

        private static IProfileSettingsService Settings()
        {
            DS4WinWPF.AppHost.CreateHost();
            return TestHost.GetService<IProfileSettingsService>();
        }

        // ---- 判定（TriggerInput）----

        [Theory]
        [InlineData(0, false, false)]
        [InlineData(1, true, false)]      // SA（デジタル）は 0 を超えれば成立。従来の IsActive は 100 を超えるまで不成立
        [InlineData(100, true, false)]
        [InlineData(101, true, true)]
        [InlineData(255, true, true)]
        public void Triggers_TheDigitalJudgementHasNoThreshold_TheControlsJudgementKeepsOver100(byte value, bool digital, bool threshold)
        {
            var input = new TriggerInput(Device, Settings());
            var fields = new DS4StateFieldMapping();
            fields.triggers[(int)DS4Controls.L2] = value;
            input.Update(new DS4State(), null, null, null, fields);

            Assert.Equal(digital, input.IsDigitalActive(DS4Controls.L2));
            Assert.Equal(threshold, input.IsActive(DS4Controls.L2));
        }

        [Theory]
        [InlineData(127, true, false)]    // 中心からわずかに傾いた負の向き
        [InlineData(128, false, false)]
        [InlineData(73, true, false)]     // 幅 55 ちょうど
        [InlineData(72, true, true)]
        public void StickNegative_TheDigitalJudgementIsActiveOnAnyDeflection(byte value, bool digital, bool threshold)
        {
            var input = new TriggerInput(Device, Settings());
            input.Update(new DS4State { LX = value, LY = value, RX = value, RY = value }, null, null, null, new DS4StateFieldMapping());

            foreach (DS4Controls control in new[] { DS4Controls.LXNeg, DS4Controls.LYNeg, DS4Controls.RXNeg, DS4Controls.RYNeg })
            {
                Assert.Equal(digital, input.IsDigitalActive(control));
                Assert.Equal(threshold, input.IsActive(control));
            }
        }

        [Theory]
        [InlineData(129, true, false)]
        [InlineData(128, false, false)]
        [InlineData(183, true, false)]
        [InlineData(184, true, true)]
        public void StickPositive_TheDigitalJudgementIsActiveOnAnyDeflection(byte value, bool digital, bool threshold)
        {
            var input = new TriggerInput(Device, Settings());
            var fields = new DS4StateFieldMapping();
            foreach (DS4Controls control in new[] { DS4Controls.LXPos, DS4Controls.LYPos, DS4Controls.RXPos, DS4Controls.RYPos })
                fields.axisdirs[(int)control] = value;
            input.Update(new DS4State(), null, null, null, fields);

            foreach (DS4Controls control in new[] { DS4Controls.LXPos, DS4Controls.LYPos, DS4Controls.RXPos, DS4Controls.RYPos })
            {
                Assert.Equal(digital, input.IsDigitalActive(control));
                Assert.Equal(threshold, input.IsActive(control));
            }
        }

        [Fact]
        public void ButtonsAndGyro_AreTheSameInBothJudgements()
        {
            IProfileSettingsService settings = Settings();
            var input = new TriggerInput(Device, settings);
            var fields = new DS4StateFieldMapping();
            fields.buttons[(int)DS4Controls.Cross] = true;
            fields.gryodirs[(int)DS4Controls.GyroXPos] = 68;
            fields.gryodirs[(int)DS4Controls.GyroZPos] = 67;
            input.Update(new DS4State(), null, null, null, fields);

            GyroOutMode saved = settings.GyroOutputMode[Device];
            settings.GyroOutputMode[Device] = GyroOutMode.Controls;
            try
            {
                Assert.True(input.IsDigitalActive(DS4Controls.Cross));
                Assert.True(input.IsDigitalActive(DS4Controls.GyroXPos));
                Assert.False(input.IsDigitalActive(DS4Controls.GyroZPos));   // ジャイロの閾値（67）は、変えない
            }
            finally
            {
                settings.GyroOutputMode[Device] = saved;
            }
        }

        [Fact]
        public void WasDigitalActive_ReadsThePreviousFrame_WithoutAThreshold()
        {
            var input = new TriggerInput(Device, Settings());
            var previous = new DS4State { L2 = 1 };
            var current = new DS4State();
            var fields = new DS4StateFieldMapping();
            fields.triggers[(int)DS4Controls.L2] = 0;

            input.Update(current, previous, new DS4StateExposed(current), null, fields);

            Assert.True(input.WasDigitalActive(DS4Controls.L2));
            Assert.False(input.WasActive(DS4Controls.L2));   // 従来の判定は、1 では不成立
            Assert.False(input.IsDigitalActive(DS4Controls.L2));
        }

        // ---- 入力アクション ----

        private sealed class LightInput : ITriggerInput
        {
            public bool Digital;
            public bool Threshold;
            public int Device => 0;
            public bool IsActive(DS4Controls control) => Threshold;
            public bool WasActive(DS4Controls control) => Threshold;
            public bool IsDigitalActive(DS4Controls control) => Digital;
            public bool WasDigitalActive(DS4Controls control) => Digital;
            public DelayClock Delay { get; } = new DelayClock();
            public DateTime UtcNow => DateTime.UtcNow;
            public long TickCount64 => 0;
        }

        [Fact]
        public void TheComboTrigger_UsesTheDigitalJudgement_ByDefault_AndTheThresholdOneWhenAsked()
        {
            IProfileSettingsService settings = Settings();
            var triggers = new[] { DS4Controls.L2, DS4Controls.Cross };
            var input = new LightInput { Digital = true, Threshold = false };

            Assert.True(new ComboTriggerInputAction("sa", triggers, settings).Evaluate(input).IsEstablished);
            Assert.False(new ComboTriggerInputAction("sa", triggers, settings, digitalJudgement: false).Evaluate(input).IsEstablished);
        }

        // ---- ③ アナログな出力 ----

        private sealed class RecordingFrame : IPadFrameOutput, IMouseMoveFrameOutput
        {
            public readonly List<byte> AxisValues = new List<byte>();
            public readonly List<byte> TriggerValues = new List<byte>();
            public readonly List<double> DeltaX = new List<double>();

            public void ResetTriggerControl(DS4Controls control) { }
            public void PressX360(X360Controls control) { }
            public void SetAxisPair(DS4Controls outControl, byte value) => AxisValues.Add(value);
            public void SetTrigger(DS4Controls outControl, byte value) => TriggerValues.Add(value);
            public void SetButton(DS4Controls outControl) { }
            public void SetMouseDeltaX(double value) => DeltaX.Add(value);
            public void SetMouseDeltaY(double value) { }
        }

        private sealed class AnalogInput : IAnalogTriggerInput, IFixedMouseInput
        {
            public byte Value;
            public int Device => 0;
            public bool IsActive(DS4Controls control) => Value > 0;
            public bool WasActive(DS4Controls control) => false;
            public bool IsControlActive(DS4Controls control) => Value > 0;
            public DelayClock Delay { get; } = new DelayClock();
            public DateTime UtcNow => DateTime.UtcNow;
            public long TickCount64 => 0;
            public byte GetAxisValue(DS4Controls control, bool positive) => 128;
            public byte GetByteValue(DS4Controls control) => Value;
            public double GetMouseMovement(DS4Controls control, int direction) => 0;
            public double GetFixedMouseMovement(int direction, double fraction) => 10.0 * fraction;
        }

        private sealed class AnalogContext : IOutputContext, IOutputContextInput
        {
            public AnalogContext(IFrameOutput frame, ITriggerInput input) { Frame = frame; Input = input; }
            public int Device => 0;
            public IVirtualKBM OutputHandler => null;
            public IFrameOutput Frame { get; }
            public ITriggerInput Input { get; }
            public void RequestEndFrame() { }
        }

        private static ButtonOutputAction Action(string controls, X360Controls target, int strength, bool toggle = false)
        {
            var sa = new SpecialAction("analog", controls, "Button", ((int)target).ToString());
            return new ButtonOutputAction(sa, new ButtonBindingValues(true, target, toggle, strength), TestHost.GetService<IManagedActionManager>());
        }

        [Theory]
        [InlineData(255, 100, 255)]
        [InlineData(128, 100, 128)]    // L2 を半分押す → 出力のトリガーも約半分
        [InlineData(255, 50, 128)]     // 強さは上限
        [InlineData(128, 50, 64)]
        [InlineData(0, 100, 0)]
        public void ASingleTrigger_DrivesAnAnalogTriggerOutput_InProportionToTheInputValue(byte input, int strength, byte expected)
        {
            var frame = new RecordingFrame();
            var context = new AnalogContext(frame, new AnalogInput { Value = input });

            Action("L2", X360Controls.RT, strength).ApplyFrame(context, active: true, risingEdge: true);

            Assert.Equal(expected, Assert.Single(frame.TriggerValues));
        }

        [Fact]
        public void ASingleTrigger_DrivesAnAxisOutput_FromNeutralInProportionToTheInputValue()
        {
            var frame = new RecordingFrame();
            var context = new AnalogContext(frame, new AnalogInput { Value = 255 });
            Action("L2", X360Controls.LXPos, 100).ApplyFrame(context, active: true, risingEdge: true);
            Assert.Equal(255, Assert.Single(frame.AxisValues));

            frame = new RecordingFrame();
            context = new AnalogContext(frame, new AnalogInput { Value = 0 });
            Action("L2", X360Controls.LXPos, 100).ApplyFrame(context, active: true, risingEdge: true);
            Assert.Equal(128, Assert.Single(frame.AxisValues));   // 値 0 は、中立
        }

        [Fact]
        public void ASingleTrigger_DrivesTheMouseMove_InProportionToTheInputValue()
        {
            var frame = new RecordingFrame();
            var context = new AnalogContext(frame, new AnalogInput { Value = 255 });
            Action("L2", X360Controls.MouseRight, 100).ApplyFrame(context, active: true, risingEdge: true);
            Assert.Equal(10.0, Assert.Single(frame.DeltaX), 6);

            frame = new RecordingFrame();
            context = new AnalogContext(frame, new AnalogInput { Value = 51 });
            Action("L2", X360Controls.MouseRight, 100).ApplyFrame(context, active: true, risingEdge: true);
            Assert.Equal(2.0, Assert.Single(frame.DeltaX), 6);   // 51 / 255 = 0.2
        }

        [Fact]
        public void SeveralTriggers_OutputTheFixedStrength_NotTheInputValue()
        {
            var frame = new RecordingFrame();
            var context = new AnalogContext(frame, new AnalogInput { Value = 51 });

            Action("L2/Cross", X360Controls.RT, 100).ApplyFrame(context, active: true, risingEdge: true);

            Assert.Equal(255, Assert.Single(frame.TriggerValues));
        }

        [Fact]
        public void AToggle_OutputsTheFixedStrength_BecauseTheTriggerIsReleasedWhileItIsOn()
        {
            var frame = new RecordingFrame();
            var context = new AnalogContext(frame, new AnalogInput { Value = 51 });

            // Toggle が ON の状態（立ち上がりの Fresh-press ガードを経た後）を、管理役に直接作る。ON の間は、トリガーを離していても出力し続ける
            var sa = new SpecialAction("analog", "L2", "Button", ((int)X360Controls.RT).ToString());
            IManagedActionManager manager = TestHost.GetService<IManagedActionManager>();
            var action = new ButtonOutputAction(sa, new ButtonBindingValues(true, X360Controls.RT, true, 100), manager);
            manager.SetToggledOn(sa, 0, true);

            action.ApplyFrame(context, active: false, risingEdge: false);

            Assert.Equal(255, Assert.Single(frame.TriggerValues));   // 入力の値（51）ではなく、強さの固定値
        }
    }
}
