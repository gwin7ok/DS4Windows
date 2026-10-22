using System.Threading;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9（Phase9-ControlsPadToggle-Plan.md）: Controls のパッドボタンの Toggle。SA の Button と同じ部品（<c>ButtonOutputAction</c>）で処理する。
    /// 押すたびに ON／OFF が切り替わり、ON の間は、仮想パッドのそのボタンが押され続ける（元のボタンの通常の出力は消える）。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase9ControlsPadToggleTests
    {
        [Fact]
        public void APadButtonWithToggle_IsBuiltWithTheSpecialActionButtonOutput()
        {
            using var t = new ControlsKeyTestScope();
            t.MapButton(DS4Controls.Cross, X360Controls.B, DS4KeyType.Toggle);
            t.MapButton(DS4Controls.Circle, X360Controls.LXNeg, DS4KeyType.Toggle);   // 軸 + Toggle も同じ
            t.MapButton(DS4Controls.Square, X360Controls.B);                          // Toggle なしは、従来のパッドの出力

            IReadOnlyList<IActionBinding> bindings = TestHost.GetService<IControlBindingBuilder>().Build(t.Device);

            IActionBinding cross = Assert.Single(bindings, b => b.Source.name == $"Synthetic_Pad_{t.Device}_{(int)DS4Controls.Cross}");
            Assert.IsType<ButtonOutputAction>(Assert.Single(cross.Outputs));
            Assert.True(cross.Policy.ErasesOutputEveryFrame);

            IActionBinding circle = Assert.Single(bindings, b => b.Source.name == $"Synthetic_Pad_{t.Device}_{(int)DS4Controls.Circle}");
            Assert.IsType<ButtonOutputAction>(Assert.Single(circle.Outputs));

            IActionBinding square = Assert.Single(bindings, b => b.Source.name == $"Synthetic_Pad_{t.Device}_{(int)DS4Controls.Square}");
            Assert.IsType<ControlPadOutputAction>(Assert.Single(square.Outputs));
        }

        [Fact]
        public void Toggle_FlipsAtEveryPress_AndHoldsThePadButtonWhileOn()
        {
            using var t = new ControlsKeyTestScope();
            t.MapButton(DS4Controls.Cross, X360Controls.B, DS4KeyType.Toggle);   // B の標準の割り当ては Circle
            t.Publish();

            DS4State Pressed() { DS4State s = ControlsKeyTestScope.Neutral(); s.Cross = true; return s; }

            // Toggle は Fresh-press ガードを見る（SA・Key の Toggle と同じ）。ガードは、トリガーが 80ms 連続して未成立になるまで解除されない
            t.Run(ControlsKeyTestScope.Neutral());
            Thread.Sleep(120);
            DS4State idle = t.Run(ControlsKeyTestScope.Neutral());
            Assert.False(idle.Circle);

            DS4State firstPress = t.Run(Pressed());
            Assert.True(firstPress.Circle);    // ON
            Assert.False(firstPress.Cross);    // 元のボタンの通常の出力は消える

            DS4State released = t.Run(ControlsKeyTestScope.Neutral());
            Assert.True(released.Circle);      // 離しても、ON のまま

            DS4State secondPress = t.Run(Pressed());
            Assert.False(secondPress.Circle);  // OFF

            DS4State releasedAgain = t.Run(ControlsKeyTestScope.Neutral());
            Assert.False(releasedAgain.Circle);
        }
    }
}
