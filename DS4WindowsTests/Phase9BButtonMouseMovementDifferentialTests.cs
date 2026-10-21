using System;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-B（Phase9-B-Plan.md）: <see cref="ButtonMouseMovementCalculator"/> を、旧実装（<c>Mapping.getMouseMapping</c> を逐語的に写したもの。
    /// <c>ctrl</c>／<c>eState</c> の引数を外し、設定と経過時間を引数にした。マウス加速の状態は、旧実装のとおり写した側が 1 組だけ持つ）と<b>突き合わせる差分テスト</b>。
    /// 旧実装は private で、切り出しの後は <c>Mapping</c> から消えるため、写しをこのファイルに残す（Phase9 の後のテスト整理まで。整理ではゴールデンへの置き換えを検討する）。
    /// 設定の組み合わせ（マウス加速、デルタ加速、デッドゾーン、感度、縦倍率）× コントロール × 方向 × 疑似乱数の入力の列で、戻り値が 1 回ごとに完全一致することを調べる。
    /// 加えて、マウス加速の状態がデバイスごとに独立していること（ユーザー判断 Q2）を調べる。
    /// </summary>
    public class Phase9BButtonMouseMovementDifferentialTests
    {
        /// <summary>旧 <c>Mapping.getMouseMapping</c> の逐語的な写し。状態（<c>mcounter</c> ほか）は、旧実装のとおり、この instance が 1 組だけ持つ。</summary>
        private sealed class OldButtonMouseMapping
        {
            private int mcounter = 34;
            private int mouseaccel = 0;
            private int prevmouseaccel = 0;

            public double GetMouseMapping(int device, DS4Controls control, DS4State cState,
                DS4StateFieldMapping fieldMapping, int mnum, ButtonMouseInfo buttonMouseInfo, int lsDeadZone, int rsDeadZone,
                DeltaSettingsProcessorGroup deltaAccelProcessorGroup, double timeElapsed)
            {
                int deadzoneL = 0;
                int deadzoneR = 0;
                if (lsDeadZone == 0)
                    deadzoneL = 3;
                if (rsDeadZone == 0)
                    deadzoneR = 3;

                double value = 0.0;
                int speed = buttonMouseInfo.activeButtonSensitivity;
                const double root = 1.002;
                const double divide = 10000d;

                int controlNum = (int)control;
                DS4StateFieldMapping.ControlType controlType = DS4StateFieldMapping.mappedType[controlNum];
                //long timeElapsed = ctrl.DS4Controllers[device].getLastTimeElapsed();
                //double mouseOffset = 0.025;
                double tempMouseOffsetX = 0.0, tempMouseOffsetY = 0.0;
                double mouseVerticalScale = 1.0;
                bool verticalDir = false;
                // 0 = MouseUp, 1 = MouseDown
                if (mnum == 0 || mnum == 1)
                {
                    verticalDir = true;
                    mouseVerticalScale = buttonMouseInfo.buttonVerticalScale;
                }

                if (controlType == DS4StateFieldMapping.ControlType.Button)
                {
                    bool active = fieldMapping.buttons[controlNum];
                    value = (active ? Math.Pow(root + speed / divide, 100) - 1 : 0);
                    if (verticalDir) value *= mouseVerticalScale;
                }
                else if (controlType == DS4StateFieldMapping.ControlType.AxisDir)
                {
                    double timeDelta = timeElapsed * 0.001;
                    int mouseVelocity = speed * Mapping.MOUSESPEEDFACTOR;
                    double mouseOffset = buttonMouseInfo.mouseVelocityOffset * mouseVelocity;
                    if (verticalDir) mouseVelocity = (int)(mouseVelocity * mouseVerticalScale);

                    //double mouseOffset = MOUSESTICKANTIOFFSET * mouseVelocity;
                    // Cap mouse offset to final mouse velocity
                    //double mouseOffset = mouseVelocity >= MOUSESTICKMINVELOCITY ? MOUSESTICKMINVELOCITY : mouseVelocity;

                    switch (control)
                    {
                        case DS4Controls.LXNeg:
                            {
                                if (cState.LX < 128 - deadzoneL)
                                {
                                    double diff;
                                    if (!deltaAccelProcessorGroup.LSProcessor.useDeltaAccel)
                                    {
                                        diff = -(cState.LX - 128 - deadzoneL) / (double)(0 - 128 - deadzoneL);
                                    }
                                    else
                                    {
                                        diff = -deltaAccelProcessorGroup.LSProcessor.AccelOutXNorm;
                                    }

                                    //tempMouseOffsetX = Math.Abs(Math.Cos(cState.LSAngleRad)) * MOUSESTICKOFFSET;
                                    //tempMouseOffsetX = MOUSESTICKOFFSET;
                                    tempMouseOffsetX = cState.LXUnit * mouseOffset;
                                    value = (mouseVelocity - tempMouseOffsetX) * timeDelta * diff + (tempMouseOffsetX * -1.0 * timeDelta);
                                    //value = diff * MOUSESPEEDFACTOR * (timeElapsed * 0.001) * speed;
                                    //value = -(cState.LX - 127 - deadzoneL) / 2550d * speed;
                                }

                                break;
                            }
                        case DS4Controls.LXPos:
                            {
                                if (cState.LX > 128 + deadzoneL)
                                {
                                    double diff;
                                    if (!deltaAccelProcessorGroup.LSProcessor.useDeltaAccel)
                                    {
                                        diff = (cState.LX - 128 + deadzoneL) / (double)(255 - 128 + deadzoneL);
                                    }
                                    else
                                    {
                                        diff = deltaAccelProcessorGroup.LSProcessor.AccelOutXNorm;
                                    }

                                    tempMouseOffsetX = cState.LXUnit * mouseOffset;
                                    //tempMouseOffsetX = Math.Abs(Math.Cos(cState.LSAngleRad)) * MOUSESTICKOFFSET;
                                    //tempMouseOffsetX = MOUSESTICKOFFSET;
                                    value = (mouseVelocity - tempMouseOffsetX) * timeDelta * diff + (tempMouseOffsetX * timeDelta);
                                    //value = diff * MOUSESPEEDFACTOR * (timeElapsed * 0.001) * speed;
                                    //value = (cState.LX - 127 + deadzoneL) / 2550d * speed;
                                }

                                break;
                            }
                        case DS4Controls.RXNeg:
                            {
                                if (cState.RX < 128 - deadzoneR)
                                {
                                    double diff;
                                    if (!deltaAccelProcessorGroup.RSProcessor.useDeltaAccel)
                                    {
                                        diff = -(cState.RX - 128 - deadzoneR) / (double)(0 - 128 - deadzoneR);
                                    }
                                    else
                                    {
                                        diff = -deltaAccelProcessorGroup.RSProcessor.AccelOutXNorm;
                                    }

                                    tempMouseOffsetX = cState.RXUnit * mouseOffset;
                                    //tempMouseOffsetX = MOUSESTICKOFFSET;
                                    //tempMouseOffsetX = Math.Abs(Math.Cos(cState.RSAngleRad)) * MOUSESTICKOFFSET;
                                    value = (mouseVelocity - tempMouseOffsetX) * timeDelta * diff + (tempMouseOffsetX * -1.0 * timeDelta);
                                    //value = diff * MOUSESPEEDFACTOR * (timeElapsed * 0.001) * speed;
                                    //value = -(cState.RX - 127 - deadzoneR) / 2550d * speed;
                                }

                                break;
                            }
                        case DS4Controls.RXPos:
                            {
                                if (cState.RX > 128 + deadzoneR)
                                {
                                    double diff;
                                    if (!deltaAccelProcessorGroup.RSProcessor.useDeltaAccel)
                                    {
                                        diff = (cState.RX - 128 + deadzoneR) / (double)(255 - 128 + deadzoneR);
                                    }
                                    else
                                    {
                                        diff = deltaAccelProcessorGroup.RSProcessor.AccelOutXNorm;
                                    }

                                    tempMouseOffsetX = cState.RXUnit * mouseOffset;
                                    //tempMouseOffsetX = MOUSESTICKOFFSET;
                                    //tempMouseOffsetX = Math.Abs(Math.Cos(cState.RSAngleRad)) * MOUSESTICKOFFSET;
                                    value = (mouseVelocity - tempMouseOffsetX) * timeDelta * diff + (tempMouseOffsetX * timeDelta);
                                    //value = diff * MOUSESPEEDFACTOR * (timeElapsed * 0.001) * speed;
                                    //value = (cState.RX - 127 + deadzoneR) / 2550d * speed;
                                }

                                break;
                            }
                        case DS4Controls.LYNeg:
                            {
                                if (cState.LY < 128 - deadzoneL)
                                {
                                    double diff;
                                    if (!deltaAccelProcessorGroup.LSProcessor.useDeltaAccel)
                                    {
                                        diff = -(cState.LY - 128 - deadzoneL) / (double)(0 - 128 - deadzoneL);
                                    }
                                    else
                                    {
                                        diff = -deltaAccelProcessorGroup.LSProcessor.AccelOutYNorm;
                                    }

                                    tempMouseOffsetY = cState.LYUnit * mouseOffset;
                                    //tempMouseOffsetY = MOUSESTICKOFFSET;
                                    //tempMouseOffsetY = Math.Abs(Math.Sin(cState.LSAngleRad)) * MOUSESTICKOFFSET;
                                    value = (mouseVelocity - tempMouseOffsetY) * timeDelta * diff + (tempMouseOffsetY * -1.0 * timeDelta);
                                    //value = diff * MOUSESPEEDFACTOR * (timeElapsed * 0.001) * speed;
                                    //value = -(cState.LY - 127 - deadzoneL) / 2550d * speed;
                                }

                                break;
                            }
                        case DS4Controls.LYPos:
                            {
                                if (cState.LY > 128 + deadzoneL)
                                {
                                    double diff;
                                    if (!deltaAccelProcessorGroup.LSProcessor.useDeltaAccel)
                                    {
                                        diff = (cState.LY - 128 + deadzoneL) / (double)(255 - 128 + deadzoneL);
                                    }
                                    else
                                    {
                                        diff = deltaAccelProcessorGroup.LSProcessor.AccelOutYNorm;
                                    }

                                    tempMouseOffsetY = cState.LYUnit * mouseOffset;
                                    //tempMouseOffsetY = MOUSESTICKOFFSET;
                                    //tempMouseOffsetY = Math.Abs(Math.Sin(cState.LSAngleRad)) * MOUSESTICKOFFSET;
                                    value = (mouseVelocity - tempMouseOffsetY) * timeDelta * diff + (tempMouseOffsetY * timeDelta);
                                    //value = diff * MOUSESPEEDFACTOR * (timeElapsed * 0.001) * speed;
                                    //value = (cState.LY - 127 + deadzoneL) / 2550d * speed;
                                }

                                break;
                            }
                        case DS4Controls.RYNeg:
                            {
                                if (cState.RY < 128 - deadzoneR)
                                {
                                    double diff;
                                    if (!deltaAccelProcessorGroup.RSProcessor.useDeltaAccel)
                                    {
                                        diff = -(cState.RY - 128 - deadzoneR) / (double)(0 - 128 - deadzoneR);
                                    }
                                    else
                                    {
                                        diff = -deltaAccelProcessorGroup.RSProcessor.AccelOutYNorm;
                                    }

                                    tempMouseOffsetY = cState.RYUnit * mouseOffset;
                                    //tempMouseOffsetY = MOUSESTICKOFFSET;
                                    //tempMouseOffsetY = Math.Abs(Math.Sin(cState.RSAngleRad)) * MOUSESTICKOFFSET;
                                    value = (mouseVelocity - tempMouseOffsetY) * timeDelta * diff + (tempMouseOffsetY * -1.0 * timeDelta);
                                    //value = diff * MOUSESPEEDFACTOR * (timeElapsed * 0.001) * speed;
                                    //value = -(cState.RY - 127 - deadzoneR) / 2550d * speed;
                                }

                                break;
                            }
                        case DS4Controls.RYPos:
                            {
                                if (cState.RY > 128 + deadzoneR)
                                {
                                    double diff;
                                    if (!deltaAccelProcessorGroup.RSProcessor.useDeltaAccel)
                                    {
                                        diff = (cState.RY - 128 + deadzoneR) / (double)(255 - 128 + deadzoneR);
                                    }
                                    else
                                    {
                                        diff = deltaAccelProcessorGroup.RSProcessor.AccelOutYNorm;
                                    }

                                    tempMouseOffsetY = cState.RYUnit * mouseOffset;
                                    //tempMouseOffsetY = MOUSESTICKOFFSET;
                                    //tempMouseOffsetY = Math.Abs(Math.Sin(cState.RSAngleRad)) * MOUSESTICKOFFSET;
                                    value = (mouseVelocity - tempMouseOffsetY) * timeDelta * diff + (tempMouseOffsetY * timeDelta);
                                    //value = diff * MOUSESPEEDFACTOR * (timeElapsed * 0.001) * speed;
                                    //value = (cState.RY - 127 + deadzoneR) / 2550d * speed;
                                }

                                break;
                            }

                        default: break;
                    }
                }
                else if (controlType == DS4StateFieldMapping.ControlType.Trigger)
                {
                    byte trigger = fieldMapping.triggers[controlNum];
                    value = Math.Pow(root + speed / divide, trigger / 2d) - 1;
                    if (verticalDir) value *= mouseVerticalScale;
                }
                else if (controlType == DS4StateFieldMapping.ControlType.GyroDir)
                {
                    //double SXD = getSXDeadzone(device);
                    //double SZD = getSZDeadzone(device);

                    switch (control)
                    {
                        case DS4Controls.GyroXPos:
                            {
                                int gyroX = fieldMapping.gryodirs[controlNum];
                                value = (byte)(gyroX > 0 ? Math.Pow(root + speed / divide, gyroX) : 0);
                                if (verticalDir) value *= mouseVerticalScale;
                                break;
                            }
                        case DS4Controls.GyroXNeg:
                            {
                                int gyroX = fieldMapping.gryodirs[controlNum];
                                value = (byte)(gyroX < 0 ? Math.Pow(root + speed / divide, -gyroX) : 0);
                                if (verticalDir) value *= mouseVerticalScale;
                                break;
                            }
                        case DS4Controls.GyroZPos:
                            {
                                int gyroZ = fieldMapping.gryodirs[controlNum];
                                value = (byte)(gyroZ > 0 ? Math.Pow(root + speed / divide, gyroZ) : 0);
                                if (verticalDir) value *= mouseVerticalScale;
                                break;
                            }
                        case DS4Controls.GyroZNeg:
                            {
                                int gyroZ = fieldMapping.gryodirs[controlNum];
                                value = (byte)(gyroZ < 0 ? Math.Pow(root + speed / divide, -gyroZ) : 0);
                                if (verticalDir) value *= mouseVerticalScale;
                                break;
                            }
                        default: break;
                    }
                }

                if (buttonMouseInfo.mouseAccel)
                {
                    if (value > 0)
                    {
                        mcounter = 34;
                        mouseaccel++;
                    }

                    if (mouseaccel == prevmouseaccel)
                    {
                        mcounter--;
                    }

                    if (mcounter <= 0)
                    {
                        mouseaccel = 0;
                        mcounter = 34;
                    }

                    value *= 1 + Math.Min(20000, (mouseaccel)) / 10000d;
                    prevmouseaccel = mouseaccel;
                }

                return value;
            }
        }

        private static readonly DS4Controls[] Controls =
        {
            DS4Controls.Cross, DS4Controls.L2, DS4Controls.R2,
            DS4Controls.LXNeg, DS4Controls.LXPos, DS4Controls.LYNeg, DS4Controls.LYPos,
            DS4Controls.RXNeg, DS4Controls.RXPos, DS4Controls.RYNeg, DS4Controls.RYPos,
            DS4Controls.GyroXPos, DS4Controls.GyroXNeg, DS4Controls.GyroZPos, DS4Controls.GyroZNeg,
        };

        /// <summary>1 フレーム分の入力（疑似乱数。128・0・255 の境界の値を混ぜる）。</summary>
        private static void FillFrame(Random rnd, DS4State state, DS4StateFieldMapping fields, DeltaSettingsProcessorGroup accel, bool useDeltaAccel)
        {
            byte Axis()
            {
                int pick = rnd.Next(8);
                if (pick == 0) return 128;
                if (pick == 1) return 0;
                if (pick == 2) return 255;
                if (pick == 3) return (byte)(128 + rnd.Next(-6, 7));   // デッドゾーン付近
                return (byte)rnd.Next(256);
            }

            state.LX = Axis(); state.LY = Axis(); state.RX = Axis(); state.RY = Axis();
            state.calculateStickAngles();
            fields.buttons[(int)DS4Controls.Cross] = rnd.Next(2) == 0;
            fields.triggers[(int)DS4Controls.L2] = (byte)rnd.Next(256);
            fields.triggers[(int)DS4Controls.R2] = (byte)rnd.Next(256);
            fields.gryodirs[(int)DS4Controls.GyroXPos] = rnd.Next(-3, 120);
            fields.gryodirs[(int)DS4Controls.GyroXNeg] = rnd.Next(-120, 3);
            fields.gryodirs[(int)DS4Controls.GyroZPos] = rnd.Next(-3, 120);
            fields.gryodirs[(int)DS4Controls.GyroZNeg] = rnd.Next(-120, 3);
            accel.LSProcessor.useDeltaAccel = useDeltaAccel;
            accel.RSProcessor.useDeltaAccel = useDeltaAccel;
            accel.LSProcessor.AccelOutXNorm = rnd.NextDouble() * 2.0 - 1.0;
            accel.LSProcessor.AccelOutYNorm = rnd.NextDouble() * 2.0 - 1.0;
            accel.RSProcessor.AccelOutXNorm = rnd.NextDouble() * 2.0 - 1.0;
            accel.RSProcessor.AccelOutYNorm = rnd.NextDouble() * 2.0 - 1.0;
        }

        [Fact]
        public void Calculate_MatchesTheOldImplementation_AcrossSettingsControlsAndDirections()
        {
            int seed = 20261022;
            foreach (bool mouseAccel in new[] { false, true })
            foreach (bool useDeltaAccel in new[] { false, true })
            foreach (int deadZone in new[] { 0, 10 })
            foreach (int sens in new[] { 1, 25, 100 })
            foreach (double vertScale in new[] { 1.0, 0.5 })
            {
                var info = new ButtonMouseInfo { mouseAccel = mouseAccel, activeButtonSensitivity = sens, buttonVerticalScale = vertScale };
                var accel = new DeltaSettingsProcessorGroup();
                var state = new DS4State();
                var fields = new DS4StateFieldMapping();
                var rnd = new Random(seed++);
                var oldImpl = new OldButtonMouseMapping();
                var newImpl = new ButtonMouseMovementCalculator();

                for (int frame = 0; frame < 12; frame++)
                {
                    double elapsed = 1.0 + rnd.NextDouble() * 9.0;
                    FillFrame(rnd, state, fields, accel, useDeltaAccel);
                    foreach (DS4Controls control in Controls)
                    {
                        for (int mnum = 0; mnum < 4; mnum++)
                        {
                            double expected = oldImpl.GetMouseMapping(0, control, state, fields, mnum, info, deadZone, deadZone, accel, elapsed);
                            double actual = newImpl.Calculate(0, control, mnum, state, fields, info, deadZone, deadZone, accel, elapsed);
                            Assert.True(expected.Equals(actual),
                                $"mouseAccel={mouseAccel} deltaAccel={useDeltaAccel} dz={deadZone} sens={sens} vs={vertScale} frame={frame} {control} mnum={mnum}: 旧={expected:R} 新={actual:R}");
                        }
                    }
                }
            }
        }

        [Fact]
        public void Calculate_KeepsTheAccelerationStateIndependentPerDevice()
        {
            // デバイス 0 は押し続け（値が出る）、デバイス 1 は押していない（値が 0）。交互に呼んでも、各デバイスの結果は、そのデバイスだけを呼んだ旧実装と一致する
            var info = new ButtonMouseInfo { mouseAccel = true, activeButtonSensitivity = 25 };
            var accel = new DeltaSettingsProcessorGroup();
            var state = new DS4State();
            var active = new DS4StateFieldMapping();
            var idle = new DS4StateFieldMapping();
            active.buttons[(int)DS4Controls.Cross] = true;
            var newImpl = new ButtonMouseMovementCalculator();
            var old0 = new OldButtonMouseMapping();
            var old1 = new OldButtonMouseMapping();

            for (int frame = 0; frame < 80; frame++)
            {
                double a = newImpl.Calculate(0, DS4Controls.Cross, 3, state, active, info, 0, 0, accel, 5.0);
                double b = newImpl.Calculate(1, DS4Controls.Cross, 3, state, idle, info, 0, 0, accel, 5.0);
                Assert.True(old0.GetMouseMapping(0, DS4Controls.Cross, state, active, 3, info, 0, 0, accel, 5.0).Equals(a), $"frame={frame} device 0");
                Assert.True(old1.GetMouseMapping(1, DS4Controls.Cross, state, idle, 3, info, 0, 0, accel, 5.0).Equals(b), $"frame={frame} device 1");
            }
        }

        [Fact]
        public void Calculate_AccelerationGrowsWhileTheButtonStaysDown()
        {
            // 押し続けると、加速（1 + mouseaccel / 10000）で値が増える。加速なしの値より大きくなる
            var accel = new DeltaSettingsProcessorGroup();
            var state = new DS4State();
            var fields = new DS4StateFieldMapping();
            fields.buttons[(int)DS4Controls.Cross] = true;
            var withAccel = new ButtonMouseInfo { mouseAccel = true, activeButtonSensitivity = 25 };
            var withoutAccel = new ButtonMouseInfo { mouseAccel = false, activeButtonSensitivity = 25 };
            var calc = new ButtonMouseMovementCalculator();

            double first = calc.Calculate(0, DS4Controls.Cross, 3, state, fields, withAccel, 0, 0, accel, 5.0);
            double last = first;
            for (int i = 0; i < 200; i++)
            {
                last = calc.Calculate(0, DS4Controls.Cross, 3, state, fields, withAccel, 0, 0, accel, 5.0);
            }
            double plain = calc.Calculate(0, DS4Controls.Cross, 3, state, fields, withoutAccel, 0, 0, accel, 5.0);

            Assert.True(last > first);
            Assert.True(last > plain);
        }
    }
}
