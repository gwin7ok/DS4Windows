using DS4Windows.Processing.Touchpad;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>タッチパッドのマウスの慣性（<see cref="TouchMouseTrackball"/>）。TouchpadMouse から切り出したときの特性化。</summary>
    public class TouchMouseTrackballTests
    {
        private const double Dt = 0.004;

        [Fact]
        public void WithoutSamples_TheCoastDoesNotMove()
        {
            var t = new TouchMouseTrackball();
            t.BeginCoast();
            Assert.True(t.Active);
            Assert.False(t.Step(Dt, out int dx, out int dy));
            Assert.Equal(0, dx);
            Assert.Equal(0, dy);
            Assert.False(t.Active);
        }

        [Fact]
        public void AfterFastSamples_TheCoastMovesInThatDirection_AndSlowsToAStop()
        {
            var t = new TouchMouseTrackball();
            for (int i = 0; i < 8; i++) t.AddSample(40, -20);

            t.BeginCoast();
            Assert.True(t.Step(Dt, out int firstDx, out int firstDy));
            Assert.True(firstDx > 0);
            Assert.True(firstDy < 0);

            int frames = 0;
            int peak = System.Math.Abs(firstDx);
            while (t.Active && frames < 100000)
            {
                t.Step(Dt, out int dx, out _);
                Assert.True(System.Math.Abs(dx) <= peak + 1, "初速より大きく動いた");
                frames++;
            }

            Assert.False(t.Active);
            Assert.True(frames > 1);
        }

        [Fact]
        public void Reset_ClearsTheHistoryAndStopsTheCoast()
        {
            var t = new TouchMouseTrackball();
            for (int i = 0; i < 8; i++) t.AddSample(40, 40);
            t.BeginCoast();
            Assert.True(t.Active);

            t.Reset();

            Assert.False(t.Active);
            t.BeginCoast();
            Assert.False(t.Step(Dt, out _, out _));
        }

        [Fact]
        public void ALargerFriction_StopsTheCoastSooner()
        {
            int Frames(double friction)
            {
                var t = new TouchMouseTrackball();
                t.ResetAccel(friction);
                for (int i = 0; i < 8; i++) t.AddSample(40, 0);
                t.BeginCoast();
                int n = 0;
                while (t.Active && n < 1000000) { t.Step(Dt, out _, out _); n++; }
                return n;
            }

            Assert.True(Frames(40.0) < Frames(10.0));
        }
    }
}
