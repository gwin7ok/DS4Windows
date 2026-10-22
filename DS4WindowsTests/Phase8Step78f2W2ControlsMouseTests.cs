using System.Threading;
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
    /// Phase8-Step7 7.8f-2 W2/W3（Phase8-Step7-8f-Plan.md §3.2）: Controls タブの Button のうち、マウスのボタン・タッチパッドクリック・デジタルのホイール・Toggle の新経路。
    /// 解釈、ビルダーの所有の条件と束縛の形、Toggle の是正（Q8＝A: 立ち上がりで 1 回だけ反転）、ロールバック、ソースのガード。
    /// 旧・新の比べは <c>Phase8Step78f2W1ControlsMouseCharacterizationTests</c>（同じテストが新経路を走らせる）。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step78f2W2ControlsMouseTests
    {
        private const int Device = MappingCharacterizationScope.Device;
        private readonly ITestOutputHelper _output;

        public Phase8Step78f2W2ControlsMouseTests(ITestOutputHelper output)
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
        public void TryInterpretMouse_AcceptsTheMouseButtonsTheTouchpadClickAndTheDigitalWheel()
        {
            foreach (X360Controls target in new[]
            {
                X360Controls.LeftMouse, X360Controls.RightMouse, X360Controls.MiddleMouse, X360Controls.FourthMouse, X360Controls.FifthMouse,
                X360Controls.TouchpadClick,
            })
            {
                Assert.True(ControlBindingBuilder.TryInterpretMouse(Button(DS4Controls.Cross, target), out X360Controls got));
                Assert.Equal(target, got);
            }

            Assert.True(ControlBindingBuilder.TryInterpretMouse(Button(DS4Controls.Cross, X360Controls.WUP), out _));
            Assert.True(ControlBindingBuilder.TryInterpretMouse(Button(DS4Controls.L1, X360Controls.WDOWN, DS4KeyType.Toggle), out _));   // Toggle の有無は見ない
        }

        [Fact]
        public void TryInterpretMouse_RejectsTheAnalogWheelTheMouseMovementAndEverythingElse()
        {
            // 元がスティック・トリガー・ジャイロのホイールは、アナログ（7.8f-3）
            Assert.False(ControlBindingBuilder.TryInterpretMouse(Button(DS4Controls.LXPos, X360Controls.WUP), out _));
            Assert.False(ControlBindingBuilder.TryInterpretMouse(Button(DS4Controls.RYNeg, X360Controls.WDOWN), out _));
            Assert.False(ControlBindingBuilder.TryInterpretMouse(Button(DS4Controls.L2, X360Controls.WUP), out _));
            Assert.False(ControlBindingBuilder.TryInterpretMouse(Button(DS4Controls.R2, X360Controls.WDOWN), out _));
            Assert.False(ControlBindingBuilder.TryInterpretMouse(Button(DS4Controls.GyroXPos, X360Controls.WUP), out _));

            foreach (X360Controls other in new[] { X360Controls.MouseUp, X360Controls.MouseDown, X360Controls.MouseLeft, X360Controls.MouseRight, X360Controls.A, X360Controls.LXPos, X360Controls.Unbound, X360Controls.None })
                Assert.False(ControlBindingBuilder.TryInterpretMouse(Button(DS4Controls.Cross, other), out _), $"{other}");

            Assert.False(ControlBindingBuilder.TryInterpretMouse(null, out _));
            var key = new DS4ControlSettings(DS4Controls.Cross) { actionType = DS4ControlSettings.ActionType.Key };
            Assert.False(ControlBindingBuilder.TryInterpretMouse(key, out _));
        }

        [Fact]
        public void TryInterpretPadTarget_IgnoresToggle_ButTryInterpretPadStillRejectsIt()
        {
            DS4ControlSettings toggled = Button(DS4Controls.Cross, X360Controls.B, DS4KeyType.Toggle);
            Assert.False(ControlBindingBuilder.TryInterpretPad(toggled, out _, out _));
            Assert.True(ControlBindingBuilder.TryInterpretPadTarget(toggled, out PadBindingValues values, out bool eraseOnly));
            Assert.False(eraseOnly);
            Assert.Equal(DS4Controls.Circle, values.Output);

            Assert.True(ControlBindingBuilder.TryInterpretPadTarget(Button(DS4Controls.Cross, X360Controls.Unbound, DS4KeyType.Toggle), out _, out eraseOnly));
            Assert.True(eraseOnly);
        }

        // ---- ビルダー ----

        [Fact]
        public void AMouseButton_MakesOneMouseBinding_ThatErasesTheOriginalEveryFrame()
        {
            using var t = new ControlsKeyTestScope();
            t.MapButton(DS4Controls.Cross, X360Controls.RightMouse);

            IActionBinding b = Assert.Single(Build());

            Assert.Equal($"Synthetic_Mouse_{Device}_{(int)DS4Controls.Cross}", b.Source.name);
            var input = Assert.IsType<ControlButtonInputAction>(b.Input);
            Assert.Equal(ControlBindingKind.ButtonMouse, input.Kind);
            Assert.False(b.Policy.RespectsFreshPressGuard);
            Assert.False(b.Policy.SuppressesTriggerButtons);
            Assert.True(b.Policy.ErasesOutputEveryFrame);
            Assert.IsType<ControlMouseOutputAction>(Assert.Single(b.Outputs));
        }

        [Fact]
        public void TheTouchpadClick_UsesItsOwnOutput_AndToggleAddsTheToggleOutput()
        {
            using var t = new ControlsKeyTestScope();
            t.MapButton(DS4Controls.Cross, X360Controls.TouchpadClick);
            t.MapButton(DS4Controls.Circle, X360Controls.LeftMouse, DS4KeyType.Toggle);
            t.MapButton(DS4Controls.Square, X360Controls.B, DS4KeyType.Toggle);
            t.MapButton(DS4Controls.Triangle, X360Controls.Unbound, DS4KeyType.Toggle);

            var bindings = Build();

            IActionBinding touch = Assert.Single(bindings, b => b.Source.name == $"Synthetic_Mouse_{Device}_{(int)DS4Controls.Cross}");
            Assert.IsType<ControlTouchpadClickOutputAction>(Assert.Single(touch.Outputs));

            IActionBinding mouseToggle = Assert.Single(bindings, b => b.Source.name == $"Synthetic_Mouse_{Device}_{(int)DS4Controls.Circle}");
            // Phase9: マウスのボタン＋Toggle は、SA の Button と同じ出力（ボタンごとの Toggle の状態）
            Assert.IsType<ButtonOutputAction>(Assert.Single(mouseToggle.Outputs));
            Assert.True(mouseToggle.Policy.ErasesOutputEveryFrame);

            // パッド＋Toggle（Phase9: Controls のパッドボタンの Toggle）: SA の Button と同じ出力（ButtonOutputAction）が、Toggle の反転と出力を担う。
            // 元のボタンの通常の出力は、方針（毎フレーム消す）で消える
            IActionBinding padToggle = Assert.Single(bindings, b => b.Source.name == $"Synthetic_Pad_{Device}_{(int)DS4Controls.Square}");
            Assert.IsType<ButtonOutputAction>(Assert.Single(padToggle.Outputs));
            Assert.True(padToggle.Policy.ErasesOutputEveryFrame);

            // Unbound＋Toggle: 消して、Toggle の出力だけ
            IActionBinding unboundToggle = Assert.Single(bindings, b => b.Source.name == $"Synthetic_Pad_{Device}_{(int)DS4Controls.Triangle}");
            Assert.IsType<ControlToggleOutputAction>(Assert.Single(unboundToggle.Outputs));
            Assert.True(unboundToggle.Policy.ErasesOutputEveryFrame);
        }

        [Fact]
        public void ButtonWithExtras_MakesTheExtrasBindingFirst_ThenTheMouseBinding()
        {
            using var t = new ControlsKeyTestScope();
            t.MapButton(DS4Controls.Cross, X360Controls.LeftMouse).extras = "100,50,1,10,20,30,5,1,7";

            var bindings = Build();

            Assert.Equal(2, bindings.Count);
            Assert.IsType<ControlExtrasOutputAction>(bindings[0].Outputs[0]);
            Assert.IsType<ControlMouseOutputAction>(bindings[1].Outputs[0]);
        }

        // ---- Toggle の是正（Q8＝A） ----

        private static (bool Toggle, int ToggleCount, int Left) Frame(ControlsKeyTestScope t, DS4State input)
        {
            t.Run(input);
            SyntheticState state = t.Scope.Synthetic.DeviceState[t.Device];
            var result = (state.currentClicks.toggle, state.currentClicks.toggleCount, state.currentClicks.leftCount);
            state.SaveToPrevious(true);
            return result;
        }

        [Fact]
        public void Toggle_FlipsOnceAtTheRisingEdge_AndHoldsTheClickWhileOn()
        {
            // Phase9: 旧テストはデバイスの toggle フラグを見ていた。SA の Button と同じ部品になり、ON の間は、クリックが押され続ける（ボタンごとの状態）
            using var t = new ControlsKeyTestScope();
            t.MapButton(DS4Controls.Cross, X360Controls.LeftMouse, DS4KeyType.Toggle);
            t.Publish();

            DS4State held = ControlsKeyTestScope.Neutral();
            held.Cross = true;

            // Toggle は Fresh-press ガードを見る（SA・Key と同じ）。ガードは、トリガーが 80ms 連続して未成立になるまで解除されない
            Frame(t, ControlsKeyTestScope.Neutral());
            Thread.Sleep(120);
            Assert.Equal(0, Frame(t, ControlsKeyTestScope.Neutral()).Left);

            var frames = new (bool Toggle, int ToggleCount, int Left)[4];
            for (int i = 0; i < frames.Length; i++) frames[i] = Frame(t, held);
            Assert.All(frames, f => Assert.Equal(1, f.Left));                 // 立ち上がりで ON。押している間は、そのまま

            Assert.Equal(1, Frame(t, ControlsKeyTestScope.Neutral()).Left);   // 離しても、ON のまま

            Assert.Equal(0, Frame(t, held).Left);                             // もう一度押すと OFF
            Assert.Equal(0, Frame(t, ControlsKeyTestScope.Neutral()).Left);
        }

        [Fact]
        public void TwoToggledMouseButtons_KeepTheirOwnState()
        {
            // Phase9: 以前は、デバイスに 1 つの toggle フラグを共有し、2 つの Toggle が連動した
            using var t = new ControlsKeyTestScope();
            t.MapButton(DS4Controls.Cross, X360Controls.LeftMouse, DS4KeyType.Toggle);
            t.MapButton(DS4Controls.Circle, X360Controls.RightMouse, DS4KeyType.Toggle);
            t.Publish();

            DS4State crossHeld = ControlsKeyTestScope.Neutral();
            crossHeld.Cross = true;

            SyntheticState state = t.Scope.Synthetic.DeviceState[t.Device];

            t.Run(ControlsKeyTestScope.Neutral()); state.SaveToPrevious(true);
            Thread.Sleep(120);                                   // Fresh-press ガードの解除（80ms 連続して未成立）
            t.Run(ControlsKeyTestScope.Neutral()); state.SaveToPrevious(true);
            t.Run(crossHeld); state.SaveToPrevious(true);        // Cross だけ ON にする
            t.Run(ControlsKeyTestScope.Neutral());               // 離しても ON のまま。このフレームのカウンターを見る
            Assert.Equal(1, state.currentClicks.leftCount);
            Assert.Equal(0, state.currentClicks.rightCount);     // Circle は、連動して ON にならない
        }

        // ---- ソースのガード ----

        [Fact]
        public void TheSources_CarryTheMouseWiring()
        {
            string registration = SourceFileLocator.Find("DS4Windows/DI/ServiceRegistration.cs");
            string frameOutput = SourceFileLocator.Find("DS4Windows/Actions/FrameOutput.cs");
            if (registration == null || frameOutput == null) return;

            Assert.Contains("IToggleFrameOutput", File.ReadAllText(frameOutput));
        }
    }
}
