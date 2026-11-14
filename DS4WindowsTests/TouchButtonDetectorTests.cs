using System;
using System.Reflection;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Processing.Touchpad;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>タッチパッドのボタンの判定（<see cref="TouchButtonDetector"/>）。TouchpadMouse から切り出したときの特性化。</summary>
    public class TouchButtonDetectorTests
    {
        // Touch のコンストラクターは internal。テストからはリフレクションで作る
        private static Touch MakeTouch(int x) =>
            (Touch)Activator.CreateInstance(typeof(Touch), BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { x, 100, (byte)1, null }, null);

        private static TouchpadEventArgs Args(bool buttonPressed, bool touchActive, params Touch[] touches)
        {
            Touch t0 = touches.Length > 0 ? touches[0] : null;
            Touch t1 = touches.Length > 1 ? touches[1] : null;
            return new TouchpadEventArgs(DateTime.UtcNow, buttonPressed, touchActive, t0, t1);
        }

        [Fact]
        public void Click_LeftHalf_ActivatesLeft_RightHalf_ActivatesRight()
        {
            var d = new TouchButtonDetector();
            d.Check(Args(true, true, MakeTouch(100)), TouchButtonActivationMode.Click);
            Assert.True(d.LeftDown);
            Assert.False(d.RightDown);

            d.ClearDownFlags();
            d.Check(Args(true, true, MakeTouch(1500)), TouchButtonActivationMode.Click);
            Assert.True(d.RightDown);
            Assert.False(d.LeftDown);
        }

        [Fact]
        public void Click_TwoFingers_ActivatesMulti_AndNoFinger_ActivatesUpper()
        {
            var d = new TouchButtonDetector();
            d.Check(Args(true, true, MakeTouch(100), MakeTouch(900)), TouchButtonActivationMode.Click);
            Assert.True(d.MultiDown);

            d.ClearDownFlags();
            d.Check(Args(true, false), TouchButtonActivationMode.Click);
            Assert.True(d.UpperDown);
        }

        [Fact]
        public void Click_WithoutThePress_ActivatesNothing()
        {
            var d = new TouchButtonDetector();
            d.Check(Args(false, true, MakeTouch(100)), TouchButtonActivationMode.Click);
            Assert.False(d.LeftDown || d.RightDown || d.MultiDown || d.UpperDown);
        }

        [Fact]
        public void Touch_ActivatesWhileTouching_AndClearsWhenTheFingerLeaves()
        {
            var d = new TouchButtonDetector();
            d.Check(Args(false, true, MakeTouch(100)), TouchButtonActivationMode.Touch);
            Assert.True(d.LeftDown);

            d.Check(Args(false, false), TouchButtonActivationMode.Touch);
            Assert.False(d.LeftDown);
        }

        [Fact]
        public void Release_ActivatesOnlyAfterTheFingerLeaves()
        {
            var d = new TouchButtonDetector();
            Touch finger = MakeTouch(100);

            d.Check(Args(false, true, finger), TouchButtonActivationMode.Release);
            Assert.False(d.LeftDown);

            // 指が離れ、最後のタッチの位置で左と決まる
            d.WasTouched = true;
            d.Check(Args(false, false, finger), TouchButtonActivationMode.Release);
            Assert.True(d.LeftDown);
            Assert.True(d.ReleaseButtonActive);
        }

        [Fact]
        public void ResetForNewTouch_ClearsTheFlagsAndTheCandidate()
        {
            var d = new TouchButtonDetector();
            d.Check(Args(true, true, MakeTouch(100)), TouchButtonActivationMode.Click);
            d.WasTouched = true;
            d.ReleaseButtonActive = true;

            d.ResetForNewTouch();

            Assert.False(d.LeftDown);
            Assert.False(d.WasTouched);
            Assert.False(d.ReleaseButtonActive);
        }

        [Fact]
        public void ClearDownFlagsKeepCandidate_KeepsTheCandidate_SoTheNextCheckUsesIt()
        {
            var d = new TouchButtonDetector();
            Touch finger = MakeTouch(100);
            d.Check(Args(true, true, finger), TouchButtonActivationMode.Click);
            d.ClearDownFlagsKeepCandidate();
            Assert.False(d.LeftDown);

            // Release 方式: 候補（左）が残っているので、離れたときに左が成立する
            d.WasTouched = true;
            d.Check(Args(false, false), TouchButtonActivationMode.Release);
            Assert.True(d.LeftDown);
        }
    }
}
