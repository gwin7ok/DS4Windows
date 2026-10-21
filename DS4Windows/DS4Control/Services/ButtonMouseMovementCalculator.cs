using System;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-B（Phase9-B-Plan.md）: <see cref="IButtonMouseMovementCalculator"/> の実装（DI の Singleton）。
    /// 旧 <c>Mapping.getMouseMapping</c> の計算を、そのまま移したもの（<c>ctrl</c>／<c>eState</c> の引数を外し、設定と経過時間は引数で受ける。計算結果は変わらない）。
    /// マウス加速の状態（旧 <c>Mapping.mcounter</c>／<c>mouseaccel</c>／<c>prevmouseaccel</c>。全コントローラーで 1 つを共有していた）は、
    /// ユーザー判断（Q2）によりデバイスごとに持つ（2 台以上のとき、互いの加速を邪魔しない。1 台のときの挙動は変わらない）。
    /// 値の特性化は <c>Phase9BButtonMouseMovementDifferentialTests</c>（旧実装との差分テスト）が守る。
    /// </summary>
    public sealed class ButtonMouseMovementCalculator : IButtonMouseMovementCalculator
    {
        private const int AccelCounterStart = 34;

        private readonly int[] _mcounter = new int[ControllerLimits.MaxControllerCount];
        private readonly int[] _mouseaccel = new int[ControllerLimits.MaxControllerCount];
        private readonly int[] _prevmouseaccel = new int[ControllerLimits.MaxControllerCount];

        public ButtonMouseMovementCalculator()
        {
            for (int i = 0; i < _mcounter.Length; i++)
            {
                _mcounter[i] = AccelCounterStart;
            }
        }

        public double Calculate(int device, DS4Controls control, int mnum, DS4State cState,
            DS4StateFieldMapping fieldMapping, ButtonMouseInfo buttonMouseInfo, int lsDeadZone, int rsDeadZone,
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
                    _mcounter[device] = AccelCounterStart;
                    _mouseaccel[device]++;
                }

                if (_mouseaccel[device] == _prevmouseaccel[device])
                {
                    _mcounter[device]--;
                }

                if (_mcounter[device] <= 0)
                {
                    _mouseaccel[device] = 0;
                    _mcounter[device] = AccelCounterStart;
                }

                value *= 1 + Math.Min(20000, (_mouseaccel[device])) / 10000d;
                _prevmouseaccel[device] = _mouseaccel[device];
            }

            return value;
        }
    }
}
