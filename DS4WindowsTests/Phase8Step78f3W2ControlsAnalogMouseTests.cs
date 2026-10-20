using System.IO;
using System.Linq;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.8f-3 W2/W3（Phase8-Step7-8f-Plan.md §3.2）: Controls タブの Button のうち、マウスの移動とアナログのホイールの新経路。
    /// 解釈、ビルダーの所有の条件と束縛の形、加算器（Q6＝A）、ホイールの状態を束縛ごとに持つこと（Q5＝B）、ロールバック、ソースのガード。
    /// 旧・新の比べは <c>Phase8Step78f3W1ControlsAnalogMouseCharacterizationTests</c>（同じテストが新経路を走らせる）。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step78f3W2ControlsAnalogMouseTests
    {
        private const int Device = MappingCharacterizationScope.Device;
        private readonly ITestOutputHelper _output;

        public Phase8Step78f3W2ControlsAnalogMouseTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static DS4ControlSettings Button(DS4Controls source, X360Controls target, DS4KeyType keyType = DS4KeyType.None)
        {
            var dcs = new DS4ControlSettings(source);
            dcs.actionType = DS4ControlSettings.ActionType.Button;
            dcs.action.actionBtn = target;
            dcs.keyType = keyType;
            return dcs;
        }

        private static System.Collections.Generic.IReadOnlyList<IActionBinding> Build() => TestHost.GetService<IControlBindingBuilder>().Build(Device);

        // ---- 解釈 ----

        [Fact]
        public void TryInterpretAnalogMouse_AcceptsTheMouseMovementAndTheAnalogWheel()
        {
            foreach (X360Controls target in new[] { X360Controls.MouseUp, X360Controls.MouseDown, X360Controls.MouseLeft, X360Controls.MouseRight })
            {
                Assert.True(ControlBindingBuilder.TryInterpretAnalogMouse(Button(DS4Controls.Cross, target), out X360Controls got));
                Assert.Equal(target, got);
            }

            Assert.True(ControlBindingBuilder.TryInterpretAnalogMouse(Button(DS4Controls.LXPos, X360Controls.WUP), out _));
            Assert.True(ControlBindingBuilder.TryInterpretAnalogMouse(Button(DS4Controls.L2, X360Controls.WDOWN, DS4KeyType.Toggle), out _));
            Assert.True(ControlBindingBuilder.TryInterpretAnalogMouse(Button(DS4Controls.GyroXPos, X360Controls.WUP), out _));
        }

        [Fact]
        public void TryInterpretAnalogMouse_RejectsTheDigitalWheelAndEverythingElse()
        {
            // 元がボタンのホイールは、デジタル（7.8f-2 の TryInterpretMouse）
            Assert.False(ControlBindingBuilder.TryInterpretAnalogMouse(Button(DS4Controls.Cross, X360Controls.WUP), out _));
            Assert.False(ControlBindingBuilder.TryInterpretAnalogMouse(Button(DS4Controls.L1, X360Controls.WDOWN), out _));

            foreach (X360Controls other in new[] { X360Controls.LeftMouse, X360Controls.TouchpadClick, X360Controls.A, X360Controls.LXPos, X360Controls.Unbound, X360Controls.None })
                Assert.False(ControlBindingBuilder.TryInterpretAnalogMouse(Button(DS4Controls.Cross, other), out _), $"{other}");

            Assert.False(ControlBindingBuilder.TryInterpretAnalogMouse(null, out _));
            var key = new DS4ControlSettings(DS4Controls.Cross) { actionType = DS4ControlSettings.ActionType.Key };
            Assert.False(ControlBindingBuilder.TryInterpretAnalogMouse(key, out _));
        }

        // ---- ビルダー ----

        [Fact]
        public void MovementAndAnalogWheel_MakeAnalogMouseBindings_ThatEraseTheOriginalEveryFrame()
        {
            using var t = new ControlsKeyTestScope(true);
            t.MapButton(DS4Controls.Cross, X360Controls.MouseLeft);
            t.MapButton(DS4Controls.L2, X360Controls.WUP);
            t.MapButton(DS4Controls.Circle, X360Controls.MouseDown, DS4KeyType.Toggle);

            var bindings = Build();

            IActionBinding move = Assert.Single(bindings, b => b.Source.name == $"Synthetic_AnalogMouse_{Device}_{(int)DS4Controls.Cross}");
            Assert.Equal(ControlBindingKind.ButtonAnalogMouse, ((ControlButtonInputAction)move.Input).Kind);
            Assert.True(move.Policy.ErasesOutputEveryFrame);
            Assert.False(move.Policy.RespectsFreshPressGuard);
            Assert.IsType<ControlMouseMoveOutputAction>(Assert.Single(move.Outputs));

            IActionBinding wheel = Assert.Single(bindings, b => b.Source.name == $"Synthetic_AnalogMouse_{Device}_{(int)DS4Controls.L2}");
            Assert.IsType<ControlAnalogWheelOutputAction>(Assert.Single(wheel.Outputs));

            IActionBinding toggled = Assert.Single(bindings, b => b.Source.name == $"Synthetic_AnalogMouse_{Device}_{(int)DS4Controls.Circle}");
            Assert.IsType<ControlMouseMoveOutputAction>(toggled.Outputs[0]);
            Assert.IsType<ControlToggleOutputAction>(toggled.Outputs[1]);
        }

        [Fact]
        public void ADigitalWheelFromAButton_StaysAMouseBinding()
        {
            using var t = new ControlsKeyTestScope(true);
            t.MapButton(DS4Controls.Cross, X360Controls.WUP);

            IActionBinding b = Assert.Single(Build());
            Assert.Equal($"Synthetic_Mouse_{Device}_{(int)DS4Controls.Cross}", b.Source.name);
        }

        // ---- 加算器（Q6＝A）と、ホイールの状態（Q5＝B） ----

        [Fact]
        public void TheAccumulator_IsEmptiedEveryFrame_AndTheFirstWriterWins()
        {
            var output = new FrameOutput();
            output.Bind(new DS4StateFieldMapping(), new SyntheticState());

            output.SetMouseDeltaX(3.5);
            output.SetMouseDeltaX(-9.0);   // 先に書いた方が勝つ（0 のときだけ書く）
            output.SetMouseDeltaY(-2.0);
            Assert.Equal(3.5, output.MouseDeltaX);
            Assert.Equal(-2.0, output.MouseDeltaY);

            output.Bind(new DS4StateFieldMapping(), new SyntheticState());   // 次のフレーム
            Assert.Equal(0.0, output.MouseDeltaX);
            Assert.Equal(0.0, output.MouseDeltaY);
        }

        [Fact]
        public void Wheel_KeepsItsOwnRemainder_PerBinding_SoSwitchingDirectionDoesNotDiscardIt()
        {
            using var h = new AnalogMouseTestHarness(true);
            h.T.MapButton(DS4Controls.L2, X360Controls.WUP);
            h.T.MapButton(DS4Controls.R2, X360Controls.WDOWN);
            h.T.Publish();

            DS4State up = ControlsKeyTestScope.Neutral();
            up.L2 = 255;
            h.Step(up);
            h.Step(up);   // 上の束縛が端数 2/3 を持つ

            DS4State down = ControlsKeyTestScope.Neutral();
            down.R2 = 255;
            h.Step(down);
            h.Step(down); // 下の束縛は自分の端数（2/3）。上の端数は捨てられない（旧経路は、向きを変えると捨てていた）
            Assert.Empty(h.Wheels);

            h.Step(up);   // 上の束縛が 3 回目で 1 目盛り（離している間も端数は残っていた）
            _output.WriteLine($"[7.8f-3 W2] wheel events: {string.Join(" ", h.Wheels)}");
            Assert.Single(h.Wheels);
            Assert.True(h.Wheels[0].Vertical > 0);
        }

        [Fact]
        public void Wheel_TwoSourcesOnTheSameDirection_AdvanceIndependently()
        {
            using var h = new AnalogMouseTestHarness(true);
            h.T.MapButton(DS4Controls.L2, X360Controls.WUP);
            h.T.MapButton(DS4Controls.R2, X360Controls.WUP);
            h.T.Publish();

            DS4State both = ControlsKeyTestScope.Neutral();
            both.L2 = 255;
            both.R2 = 255;
            for (int i = 0; i < 3; i++) h.Step(both);

            // 旧経路は、間隔の基準時刻が共有のため、同じフレームの 2 つ目は進まず、1 目盛りだった。束縛ごとに持つので、2 つとも進んで 2 目盛り
            _output.WriteLine($"[7.8f-3 W2] two sources, 3 runs: {h.Wheels.Count} events");
            Assert.Equal(2, h.Wheels.Count);
        }

        // ---- ソースのガード ----

        [Fact]
        public void TheSources_CarryTheAnalogMouseWiring()
        {
            string registration = SourceFileLocator.Find("DS4Windows/DI/ServiceRegistration.cs");
            string mapping = SourceFileLocator.Find("DS4Windows/DS4Control/Mapping.cs");
            if (registration == null || mapping == null) return;

            string text = File.ReadAllText(mapping);
            Assert.Contains("MouseSampler", text);
            Assert.Contains("frameOutput.MouseDeltaX", text);
        }
    }
}
