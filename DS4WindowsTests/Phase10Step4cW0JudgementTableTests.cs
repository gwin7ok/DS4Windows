using System;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step4c W0（Phase10-Step4c-Plan.md §2 項目 1・§4 W0）: 押下判定の閾値の、SA（<see cref="TriggerInput.IsActive"/>）と Controls（<see cref="TriggerInput.IsControlActive"/>）の
    /// <b>現行の差</b>を、同じ入力で並べて固定する特性化テスト。製品コードは変えない。
    /// W4（項目 1: デジタルな出力だけ SA の閾値に揃える）で、Controls 側の期待値のうち、デジタルな出力に使う判定の分を書き換える。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step4cW0JudgementTableTests
    {
        private const int Device = 5;

        private sealed class Rig
        {
            public readonly IProfileSettingsService Settings = TestHost.GetService<IProfileSettingsService>();
            public readonly DS4State State = new DS4State();
            public readonly DS4StateFieldMapping Fields = new DS4StateFieldMapping();
            public readonly TriggerInput Input;

            public Rig()
            {
                Input = new TriggerInput(Device, Settings);
            }

            public void Apply() => Input.Update(State, null, null, null, Fields);
        }

        private static void SetStick(DS4State state, DS4Controls control, byte value, double angle)
        {
            switch (control)
            {
                case DS4Controls.LXNeg: case DS4Controls.LXPos: state.LX = value; state.LSAngle = angle; break;
                case DS4Controls.LYNeg: case DS4Controls.LYPos: state.LY = value; state.LSAngle = angle; break;
                case DS4Controls.RXNeg: case DS4Controls.RXPos: state.RX = value; state.RSAngle = angle; break;
                case DS4Controls.RYNeg: case DS4Controls.RYPos: state.RY = value; state.RSAngle = angle; break;
            }
        }

        // ---- スティックの向き ----

        // 負の向き: SA は軸の値が 73 未満（中心 128 から幅 55 を超えて倒したとき）。Controls は軸の値が 128 未満で、かつ角度が扇形（22.5° 刻みの 8 方向の 1 つ）に入るとき
        [Theory]
        [InlineData(DS4Controls.LXNeg, 112.5, 247.5)]
        [InlineData(DS4Controls.LYNeg, 22.5, 157.5)]
        [InlineData(DS4Controls.RXNeg, 112.5, 247.5)]
        [InlineData(DS4Controls.RYNeg, 22.5, 157.5)]
        public void StickNegativeDirections_SaNeedsADeflectionOver55_ControlsOnlyTheSector(DS4Controls control, double sectorLow, double sectorHigh)
        {
            double inside = (sectorLow + sectorHigh) / 2;
            double outside = sectorLow - 20;

            // (軸の値, 角度, Controls の期待, SA の期待)
            var rows = new (byte Value, double Angle, bool Controls, bool Sa)[]
            {
                (72, inside, true, true),            // 幅 56 の傾き: どちらも成立
                (73, inside, true, false),           // 幅 55 ちょうど: SA は不成立（中心 128 - 55 = 73 は含まない）
                (127, inside, true, false),          // わずかな傾き: Controls だけ成立
                (128, inside, false, false),         // 中心
                (50, outside, false, true),          // 扇形の外: Controls は不成立、SA は軸の値だけで成立
                (50, sectorLow, true, true),         // 扇形の端（含む）
                (50, sectorHigh, true, true),
                (50, sectorLow - 0.1, false, true),  // 扇形の端の外
                (50, sectorHigh + 0.1, false, true),
            };

            foreach ((byte value, double angle, bool controlsExpected, bool saExpected) in rows)
            {
                var rig = new Rig();
                SetStick(rig.State, control, value, angle);
                rig.Apply();

                Assert.True(controlsExpected == rig.Input.IsControlActive(control), $"{control} value={value} angle={angle}: Controls");
                Assert.True(saExpected == rig.Input.IsActive(control), $"{control} value={value} angle={angle}: SA");
            }
        }

        // 正の向き: SA は表の値（axisdirs）が 183 を超えるとき。Controls は軸の値が 128 を超え、かつ角度が扇形に入るとき
        [Theory]
        [InlineData(DS4Controls.LXPos, true)]
        [InlineData(DS4Controls.LYPos, false)]
        [InlineData(DS4Controls.RXPos, true)]
        [InlineData(DS4Controls.RYPos, false)]
        public void StickPositiveDirections_SaNeedsADeflectionOver55_ControlsOnlyTheSector(DS4Controls control, bool horizontal)
        {
            // 水平（X）の正の向きの扇形は 0° 付近（<= 67.5 または >= 292.5）、垂直（Y）の正の向きは 202.5°〜337.5°
            double inside = horizontal ? 0.0 : 270.0;
            double outside = horizontal ? 180.0 : 90.0;

            var rows = new (byte Value, double Angle, bool Controls, bool Sa)[]
            {
                (184, inside, true, true),
                (183, inside, true, false),          // SA は幅 55 ちょうど（128 + 55 = 183）を含まない
                (129, inside, true, false),          // わずかな傾き: Controls だけ成立
                (128, inside, false, false),
                (200, outside, false, true),         // 扇形の外: Controls は不成立、SA は軸の値だけで成立
            };

            foreach ((byte value, double angle, bool controlsExpected, bool saExpected) in rows)
            {
                var rig = new Rig();
                SetStick(rig.State, control, value, angle);
                rig.Fields.axisdirs[(int)control] = value;
                rig.Apply();

                Assert.True(controlsExpected == rig.Input.IsControlActive(control), $"{control} value={value} angle={angle}: Controls");
                Assert.True(saExpected == rig.Input.IsActive(control), $"{control} value={value} angle={angle}: SA");
            }
        }

        [Fact]
        public void StickPositiveDirection_ControlsSectorBoundaries_AreInclusive()
        {
            foreach ((double angle, bool expected) in new[] { (67.5, true), (67.6, false), (292.5, true), (292.4, false), (0.0, true), (359.9, true) })
            {
                var rig = new Rig();
                SetStick(rig.State, DS4Controls.LXPos, 200, angle);
                rig.Apply();

                Assert.True(expected == rig.Input.IsControlActive(DS4Controls.LXPos), $"LXPos angle={angle}");
            }
        }

        // ---- アナログのトリガー（L2／R2）と、スティックの外周（Outer。種別は Trigger）----

        [Theory]
        [InlineData(DS4Controls.L2)]
        [InlineData(DS4Controls.R2)]
        [InlineData(DS4Controls.LSOuter)]
        [InlineData(DS4Controls.RSOuter)]
        public void Triggers_SaNeedsMoreThan100_ControlsMoreThanZero(DS4Controls control)
        {
            foreach ((byte value, bool controlsExpected, bool saExpected) in new[]
            {
                ((byte)0, false, false), ((byte)1, true, false), ((byte)100, true, false), ((byte)101, true, true), ((byte)255, true, true),
            })
            {
                var rig = new Rig();
                rig.Fields.triggers[(int)control] = value;
                rig.Apply();

                Assert.True(controlsExpected == rig.Input.IsControlActive(control), $"{control} value={value}: Controls");
                Assert.True(saExpected == rig.Input.IsActive(control), $"{control} value={value}: SA");
            }
        }

        // ---- ボタン、タッチ、スワイプ（SA と Controls で同じ）----

        [Theory]
        [InlineData(DS4Controls.Cross)]
        [InlineData(DS4Controls.L1)]
        [InlineData(DS4Controls.L2FullPull)]
        [InlineData(DS4Controls.R2FullPull)]
        [InlineData(DS4Controls.TouchLeft)]
        [InlineData(DS4Controls.TouchMulti)]
        [InlineData(DS4Controls.TouchStarted)]
        [InlineData(DS4Controls.TouchEnded)]
        public void DigitalInputs_AreTheSameForSaAndControls(DS4Controls control)
        {
            foreach (bool pressed in new[] { false, true })
            {
                var rig = new Rig();
                rig.Fields.buttons[(int)control] = pressed;
                rig.Apply();

                Assert.Equal(pressed, rig.Input.IsControlActive(control));
                Assert.Equal(pressed, rig.Input.IsActive(control));
            }
        }

        [Theory]
        [InlineData(DS4Controls.SwipeUp)]
        [InlineData(DS4Controls.SwipeDown)]
        [InlineData(DS4Controls.SwipeLeft)]
        [InlineData(DS4Controls.SwipeRight)]
        public void Swipes_AreTheSameForSaAndControls(DS4Controls control)
        {
            foreach (bool swiped in new[] { false, true })
            {
                var rig = new Rig();
                rig.Fields.swipedirbools[(int)control] = swiped;
                rig.Apply();

                Assert.Equal(swiped, rig.Input.IsControlActive(control));
                Assert.Equal(swiped, rig.Input.IsActive(control));
            }
        }

        // ---- ジャイロ（Controls のモードのときだけ）----

        [Fact]
        public void Gyro_SaNeedsMoreThan67_ControlsAnyTilt_AndBothNeedTheControlsMode()
        {
            IProfileSettingsService settings = TestHost.GetService<IProfileSettingsService>();
            GyroOutMode saved = settings.GyroOutputMode[Device];
            try
            {
                settings.GyroOutputMode[Device] = GyroOutMode.Controls;

                // 正の向き（GyroXPos）: (値, Controls, SA)
                foreach ((int value, bool controlsExpected, bool saExpected) in new[]
                {
                    (0, false, false), (1, true, false), (67, true, false), (68, true, true),
                })
                {
                    var rig = new Rig();
                    rig.Fields.gryodirs[(int)DS4Controls.GyroXPos] = value;
                    rig.Apply();

                    Assert.True(controlsExpected == rig.Input.IsControlActive(DS4Controls.GyroXPos), $"GyroXPos {value}: Controls");
                    Assert.True(saExpected == rig.Input.IsActive(DS4Controls.GyroXPos), $"GyroXPos {value}: SA");
                }

                // 負の向き（GyroXNeg）
                foreach ((int value, bool controlsExpected, bool saExpected) in new[]
                {
                    (0, false, false), (-1, true, false), (-67, true, false), (-68, true, true),
                })
                {
                    var rig = new Rig();
                    rig.Fields.gryodirs[(int)DS4Controls.GyroXNeg] = value;
                    rig.Apply();

                    Assert.True(controlsExpected == rig.Input.IsControlActive(DS4Controls.GyroXNeg), $"GyroXNeg {value}: Controls");
                    Assert.True(saExpected == rig.Input.IsActive(DS4Controls.GyroXNeg), $"GyroXNeg {value}: SA");
                }

                // Controls のモードでないとき: どちらも常に不成立
                settings.GyroOutputMode[Device] = GyroOutMode.Passthru;
                var off = new Rig();
                off.Fields.gryodirs[(int)DS4Controls.GyroXPos] = 200;
                off.Fields.gryodirs[(int)DS4Controls.GyroXNeg] = -200;
                off.Apply();

                Assert.False(off.Input.IsControlActive(DS4Controls.GyroXPos));
                Assert.False(off.Input.IsActive(DS4Controls.GyroXPos));
                Assert.False(off.Input.IsControlActive(DS4Controls.GyroXNeg));
                Assert.False(off.Input.IsActive(DS4Controls.GyroXNeg));
            }
            finally
            {
                settings.GyroOutputMode[Device] = saved;
            }
        }

        [Fact]
        public void BeforeAnyUpdate_BothAreInactive()
        {
            var input = new TriggerInput(Device, TestHost.GetService<IProfileSettingsService>());

            Assert.False(input.IsControlActive(DS4Controls.Cross));
            Assert.False(input.IsActive(DS4Controls.Cross));
        }
    }
}
