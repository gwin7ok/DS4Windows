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

                // Phase9 9-B（Q5）: 旧 8 方向の case（ほぼ同じコードの繰り返し）を AxisDirMovement に畳んだ。式と演算の順序は変えていない
                // （旧実装との一致は Phase9BButtonMouseMovementDifferentialTests が守る）。コメントアウトされていた旧式は、AxisDirMovement の上にまとめて残した。
                DeltaSettingsProcessor lsAccel = deltaAccelProcessorGroup.LSProcessor;
                DeltaSettingsProcessor rsAccel = deltaAccelProcessorGroup.RSProcessor;
                switch (control)
                {
                    case DS4Controls.LXNeg:
                        value = AxisDirMovement(false, cState.LX, deadzoneL, lsAccel.useDeltaAccel, lsAccel.AccelOutXNorm, cState.LXUnit, mouseOffset, mouseVelocity, timeDelta);
                        break;
                    case DS4Controls.LXPos:
                        value = AxisDirMovement(true, cState.LX, deadzoneL, lsAccel.useDeltaAccel, lsAccel.AccelOutXNorm, cState.LXUnit, mouseOffset, mouseVelocity, timeDelta);
                        break;
                    case DS4Controls.RXNeg:
                        value = AxisDirMovement(false, cState.RX, deadzoneR, rsAccel.useDeltaAccel, rsAccel.AccelOutXNorm, cState.RXUnit, mouseOffset, mouseVelocity, timeDelta);
                        break;
                    case DS4Controls.RXPos:
                        value = AxisDirMovement(true, cState.RX, deadzoneR, rsAccel.useDeltaAccel, rsAccel.AccelOutXNorm, cState.RXUnit, mouseOffset, mouseVelocity, timeDelta);
                        break;
                    case DS4Controls.LYNeg:
                        value = AxisDirMovement(false, cState.LY, deadzoneL, lsAccel.useDeltaAccel, lsAccel.AccelOutYNorm, cState.LYUnit, mouseOffset, mouseVelocity, timeDelta);
                        break;
                    case DS4Controls.LYPos:
                        value = AxisDirMovement(true, cState.LY, deadzoneL, lsAccel.useDeltaAccel, lsAccel.AccelOutYNorm, cState.LYUnit, mouseOffset, mouseVelocity, timeDelta);
                        break;
                    case DS4Controls.RYNeg:
                        value = AxisDirMovement(false, cState.RY, deadzoneR, rsAccel.useDeltaAccel, rsAccel.AccelOutYNorm, cState.RYUnit, mouseOffset, mouseVelocity, timeDelta);
                        break;
                    case DS4Controls.RYPos:
                        value = AxisDirMovement(true, cState.RY, deadzoneR, rsAccel.useDeltaAccel, rsAccel.AccelOutYNorm, cState.RYUnit, mouseOffset, mouseVelocity, timeDelta);
                        break;

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

                // Phase9 9-B（Q5）: 旧 4 方向の case は、正方向（X／Z）と負方向（X／Z）の 2 つにまとめた。式は変えていない
                // （旧: 値は byte に切り詰められる。上限の 255 を超えると桁あふれする。旧実装から同じ癖。計画書 K2）。
                switch (control)
                {
                    case DS4Controls.GyroXPos:
                    case DS4Controls.GyroZPos:
                        {
                            int gyro = fieldMapping.gryodirs[controlNum];
                            value = (byte)(gyro > 0 ? Math.Pow(root + speed / divide, gyro) : 0);
                            if (verticalDir) value *= mouseVerticalScale;
                            break;
                        }
                    case DS4Controls.GyroXNeg:
                    case DS4Controls.GyroZNeg:
                        {
                            int gyro = fieldMapping.gryodirs[controlNum];
                            value = (byte)(gyro < 0 ? Math.Pow(root + speed / divide, -gyro) : 0);
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

        // 旧 8 つの case にコメントアウトで残っていた旧式（Q5 の畳み込みで case をまとめたため、ここに原文のまま集めた。参考。削除しない）:
        //tempMouseOffsetX = Math.Abs(Math.Cos(cState.LSAngleRad)) * MOUSESTICKOFFSET;
        //tempMouseOffsetX = MOUSESTICKOFFSET;
        //value = -(cState.LX - 127 - deadzoneL) / 2550d * speed;
        //tempMouseOffsetX = Math.Abs(Math.Cos(cState.LSAngleRad)) * MOUSESTICKOFFSET;
        //tempMouseOffsetX = MOUSESTICKOFFSET;
        //value = (cState.LX - 127 + deadzoneL) / 2550d * speed;
        //tempMouseOffsetX = MOUSESTICKOFFSET;
        //tempMouseOffsetX = Math.Abs(Math.Cos(cState.RSAngleRad)) * MOUSESTICKOFFSET;
        //value = -(cState.RX - 127 - deadzoneR) / 2550d * speed;
        //tempMouseOffsetX = MOUSESTICKOFFSET;
        //tempMouseOffsetX = Math.Abs(Math.Cos(cState.RSAngleRad)) * MOUSESTICKOFFSET;
        //value = (cState.RX - 127 + deadzoneR) / 2550d * speed;
        //tempMouseOffsetY = MOUSESTICKOFFSET;
        //tempMouseOffsetY = Math.Abs(Math.Sin(cState.LSAngleRad)) * MOUSESTICKOFFSET;
        //value = -(cState.LY - 127 - deadzoneL) / 2550d * speed;
        //tempMouseOffsetY = MOUSESTICKOFFSET;
        //tempMouseOffsetY = Math.Abs(Math.Sin(cState.LSAngleRad)) * MOUSESTICKOFFSET;
        //value = (cState.LY - 127 + deadzoneL) / 2550d * speed;
        //tempMouseOffsetY = MOUSESTICKOFFSET;
        //tempMouseOffsetY = Math.Abs(Math.Sin(cState.RSAngleRad)) * MOUSESTICKOFFSET;
        //value = -(cState.RY - 127 - deadzoneR) / 2550d * speed;
        //tempMouseOffsetY = MOUSESTICKOFFSET;
        //tempMouseOffsetY = Math.Abs(Math.Sin(cState.RSAngleRad)) * MOUSESTICKOFFSET;
        //value = (cState.RY - 127 + deadzoneR) / 2550d * speed;

        /// <summary>
        /// スティックの 1 方向（左右・上下 × 正負の 8 通りの 1 つ）の移動量。倒していない（デッドゾーン内の）ときは 0。
        /// 旧 <c>getMouseMapping</c> の 8 つの case の共通部分。<paramref name="positive"/> が false のとき、負方向（中心より小さい側）。
        /// </summary>
        private static double AxisDirMovement(bool positive, int axisValue, int deadzone, bool useDeltaAccel,
            double accelNorm, double unit, double mouseOffset, int mouseVelocity, double timeDelta)
        {
            if (positive)
            {
                if (axisValue > 128 + deadzone)
                {
                    double diff;
                    if (!useDeltaAccel)
                    {
                        diff = (axisValue - 128 + deadzone) / (double)(255 - 128 + deadzone);
                    }
                    else
                    {
                        diff = accelNorm;
                    }

                    double tempMouseOffset = unit * mouseOffset;
                    //value = diff * MOUSESPEEDFACTOR * (timeElapsed * 0.001) * speed;
                    return (mouseVelocity - tempMouseOffset) * timeDelta * diff + (tempMouseOffset * timeDelta);
                }
            }
            else
            {
                if (axisValue < 128 - deadzone)
                {
                    double diff;
                    if (!useDeltaAccel)
                    {
                        diff = -(axisValue - 128 - deadzone) / (double)(0 - 128 - deadzone);
                    }
                    else
                    {
                        diff = -accelNorm;
                    }

                    double tempMouseOffset = unit * mouseOffset;
                    //value = diff * MOUSESPEEDFACTOR * (timeElapsed * 0.001) * speed;
                    return (mouseVelocity - tempMouseOffset) * timeDelta * diff + (tempMouseOffset * -1.0 * timeDelta);
                }
            }

            return 0.0;
        }
    }
}
