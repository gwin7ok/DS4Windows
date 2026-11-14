using System;
using DS4Windows.Input;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// タッチパッド・6 軸のイベント引数は、毎フレーム生成せず使い回す。値の入れ替えが正しく反映されることと、
    /// タッチパッドの 1 フレームの処理が割り当てを起こさないことを確かめる。
    /// </summary>
    public class InputEventArgsReuseTests
    {
        // Touch のコンストラクターは internal。テストからはリフレクションで作る
        private static Touch MakeTouch(int x) =>
            (Touch)Activator.CreateInstance(typeof(Touch),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, new object[] { x, x + 1, (byte)3, null }, null);

        [Fact]
        public void TouchpadEventArgs_Set_SwitchesBetweenNoneOneAndTwoTouches()
        {
            var args = new TouchpadEventArgs();
            Assert.Null(args.Touches);

            var a = MakeTouch(1);
            var b = MakeTouch(4);
            DateTime ts = DateTime.UtcNow;

            args.Set(ts, true, true, a, b);
            Assert.Equal(2, args.Touches.Length);
            Assert.Same(a, args.Touches[0]);
            Assert.Same(b, args.Touches[1]);
            Assert.True(args.TouchButtonPressed);
            Assert.Equal(ts, args.TimeStamp);

            args.Set(ts, false, true, a);
            Assert.Single(args.Touches);
            Assert.False(args.TouchButtonPressed);

            args.Set(ts, false, false, null, null);
            Assert.Null(args.Touches);
            Assert.False(args.TouchActive);
        }

        [Fact]
        public void TouchpadEventArgs_ConstructorWithTouches_StillBuildsTheSameShape()
        {
            var a = MakeTouch(1);
            var args = new TouchpadEventArgs(DateTime.UtcNow, false, true, a);
            Assert.Single(args.Touches);
            Assert.Same(a, args.Touches[0]);
        }

        [Fact]
        public void SixAxisEventArgs_Set_ReplacesTheValues()
        {
            var args = new SixAxisEventArgs();
            Assert.Null(args.sixAxis);
            var sa = new DS4Windows.Core.Model.SixAxis(1, 2, 3, 4, 5, 6, 0.004);
            DateTime ts = DateTime.UtcNow;
            args.Set(ts, sa);
            Assert.Same(sa, args.sixAxis);
            Assert.Equal(ts, args.timeStamp);
        }

        [Fact]
        public void Touchpad_HandleTouchpad_AllocatesNothingPerFrame()
        {
            var pad = new DS4Touchpad();
            int events = 0;
            pad.TouchesBegan += (s, a) => events++;
            pad.TouchesMoved += (s, a) => events++;
            pad.TouchesEnded += (s, a) => events++;
            pad.TouchButtonDown += (s, a) => events++;
            pad.TouchButtonUp += (s, a) => events++;

            var state = new DS4Windows.Core.Model.ControllerState();
            var data = new byte[8];

            void Frame(int i)
            {
                data[1] = (byte)(i & 0x7F);
                data[5] = (byte)((i * 3) & 0x7F);
                state.Touch1 = (i % 7) != 0;
                state.Touch2 = (i % 5) == 0;
                state.TouchButton = (i % 11) == 0;
                state.ReportTimeStamp = DateTime.UtcNow;
                pad.handleTouchpad(data, state, 0);
            }

            for (int i = 0; i < 200; i++) Frame(i); // ウォームアップ
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 200; i < 2200; i++) Frame(i);
            long perFrame = (GC.GetAllocatedBytesForCurrentThread() - before) / 2000;

            Assert.True(events > 0);
            Assert.True(perFrame < 16, $"1 フレームあたり {perFrame} バイト割り当てられた");
        }
    }
}
