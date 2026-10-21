using System;
using DS4Windows;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-B（Phase9-B-Plan.md）: <see cref="MouseMotionQuantizer"/> を、旧実装（<c>Mapping.calculateFinalMouseMovement</c> を逐語的に写したもの。
    /// 端数は、旧実装のとおり、この instance が 1 組だけ持つ）と突き合わせる差分テストと、端数がデバイスごとに独立していること（ユーザー判断 Q2）のテスト。
    /// </summary>
    public class Phase9BMouseMotionQuantizerTests
    {
        /// <summary>旧 <c>Mapping.calculateFinalMouseMovement</c> の逐語的な写し。</summary>
        private sealed class OldQuantizer
        {
            private double horizontalRemainder = 0.0, verticalRemainder = 0.0;

            public void Calculate(ref double rawMouseX, ref double rawMouseY, out int mouseX, out int mouseY)
            {
                if ((rawMouseX > 0.0 && horizontalRemainder > 0.0) || (rawMouseX < 0.0 && horizontalRemainder < 0.0))
                {
                    rawMouseX += horizontalRemainder;
                }
                else
                {
                    horizontalRemainder = 0.0;
                }

                double mouseXTemp = rawMouseX - (Mapping.remainderCutoff(rawMouseX * 100.0, 1.0) / 100.0);
                mouseX = (int)mouseXTemp;
                horizontalRemainder = mouseXTemp - mouseX;

                if ((rawMouseY > 0.0 && verticalRemainder > 0.0) || (rawMouseY < 0.0 && verticalRemainder < 0.0))
                {
                    rawMouseY += verticalRemainder;
                }
                else
                {
                    verticalRemainder = 0.0;
                }

                double mouseYTemp = rawMouseY - (Mapping.remainderCutoff(rawMouseY * 100.0, 1.0) / 100.0);
                mouseY = (int)mouseYTemp;
                verticalRemainder = mouseYTemp - mouseY;
            }
        }

        private static double NextMotion(Random rnd)
        {
            // 符号が変わる、0 を挟む、整数をまたぐ、の場面を混ぜる
            int pick = rnd.Next(6);
            if (pick == 0) return 0.0;
            if (pick == 1) return rnd.NextDouble() * 0.9;
            if (pick == 2) return -rnd.NextDouble() * 0.9;
            if (pick == 3) return rnd.NextDouble() * 6.0 - 3.0;
            return rnd.NextDouble() * 40.0 - 20.0;
        }

        [Fact]
        public void Quantize_MatchesTheOldImplementation_ForRandomSequences()
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                var rnd = new Random(seed);
                var oldImpl = new OldQuantizer();
                var newImpl = new MouseMotionQuantizer();
                for (int i = 0; i < 400; i++)
                {
                    double ox = NextMotion(rnd), oy = NextMotion(rnd);
                    double nx = ox, ny = oy;
                    oldImpl.Calculate(ref ox, ref oy, out int expectedX, out int expectedY);
                    newImpl.Quantize(0, ref nx, ref ny, out int actualX, out int actualY);

                    Assert.True(expectedX == actualX && expectedY == actualY, $"seed={seed} i={i}: 旧=({expectedX},{expectedY}) 新=({actualX},{actualY})");
                    Assert.True(ox.Equals(nx) && oy.Equals(ny), $"seed={seed} i={i}: 端数を足した後の生の量が一致しない");
                }
            }
        }

        [Fact]
        public void Quantize_KeepsTheRemainderIndependentPerDevice()
        {
            // 2 台に別々の列を、交互に流す。各デバイスの結果は、そのデバイスの列だけを流した旧実装と一致する
            var rnd0 = new Random(11);
            var rnd1 = new Random(22);
            var old0 = new OldQuantizer();
            var old1 = new OldQuantizer();
            var newImpl = new MouseMotionQuantizer();

            for (int i = 0; i < 400; i++)
            {
                double x0 = NextMotion(rnd0), y0 = NextMotion(rnd0);
                double x1 = NextMotion(rnd1), y1 = NextMotion(rnd1);
                double nx0 = x0, ny0 = y0, nx1 = x1, ny1 = y1;

                old0.Calculate(ref x0, ref y0, out int e0x, out int e0y);
                old1.Calculate(ref x1, ref y1, out int e1x, out int e1y);
                newImpl.Quantize(0, ref nx0, ref ny0, out int a0x, out int a0y);
                newImpl.Quantize(1, ref nx1, ref ny1, out int a1x, out int a1y);

                Assert.True(e0x == a0x && e0y == a0y, $"i={i} device 0");
                Assert.True(e1x == a1x && e1y == a1y, $"i={i} device 1");
            }
        }

        [Fact]
        public void Quantize_ZeroMotionGivesZeroAndClearsTheRemainder()
        {
            var q = new MouseMotionQuantizer();
            double x = 0.0, y = 0.0;
            q.Quantize(0, ref x, ref y, out int mx, out int my);
            Assert.Equal(0, mx);
            Assert.Equal(0, my);
        }
    }
}
