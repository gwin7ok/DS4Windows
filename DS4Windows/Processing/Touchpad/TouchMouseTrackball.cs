/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;

namespace DS4Windows.Processing.Touchpad
{
    /// <summary>
    /// タッチパッドのマウスの慣性（トラックボール）。指を離したあとも、直前の速度から摩擦で減速しながらカーソルを動かし続ける。
    /// 触れている間の移動量を短い履歴に貯め、指を離した時点の平均を初速にして、フレームごとに <see cref="Step"/> で進める。
    /// </summary>
    public sealed class TouchMouseTrackball
    {
        internal const int TRACKBALL_INIT_FICTION = 10;
        internal const int TRACKBALL_MASS = 45;
        internal const double TRACKBALL_RADIUS = 0.0245;

        private const double TRACKBALL_INERTIA = 2.0 * (TRACKBALL_MASS * TRACKBALL_RADIUS * TRACKBALL_RADIUS) / 5.0;
        private const double TRACKBALL_SCALE = 0.004;
        private const int TRACKBALL_BUFFER_LEN = 8;

        private readonly double[] xBuffer = new double[TRACKBALL_BUFFER_LEN];
        private readonly double[] yBuffer = new double[TRACKBALL_BUFFER_LEN];
        private int bufferTail = 0;
        private int bufferHead = 0;
        private double accel = TRACKBALL_RADIUS * TRACKBALL_INIT_FICTION / TRACKBALL_INERTIA;
        private double xVel = 0.0;
        private double yVel = 0.0;
        private double dxRemain = 0.0;
        private double dyRemain = 0.0;

        /// <summary>慣性で動いている間は真。</summary>
        public bool Active { get; private set; }

        /// <summary>摩擦の設定（プロファイルの TrackballFriction）から、減速の大きさを決め直す。</summary>
        public void ResetAccel(double friction)
        {
            accel = TRACKBALL_RADIUS * friction / TRACKBALL_INERTIA;
        }

        /// <summary>履歴・速度・端数を初期化する（触れ始め、切断のとき）。</summary>
        public void Reset()
        {
            Array.Clear(xBuffer, 0, TRACKBALL_BUFFER_LEN);
            Array.Clear(yBuffer, 0, TRACKBALL_BUFFER_LEN);
            xVel = 0.0;
            yVel = 0.0;
            Active = false;
            bufferTail = 0;
            bufferHead = 0;
            dxRemain = 0.0;
            dyRemain = 0.0;
        }

        /// <summary>触れている間の 1 フレームの移動量を、履歴へ足す（4 ms を基準にそろえる）。</summary>
        public void AddSample(int deltaX, int deltaY)
        {
            Push((deltaX * TRACKBALL_SCALE) / 0.004, (deltaY * TRACKBALL_SCALE) / 0.004);
        }

        /// <summary>動かなかったフレームを、履歴へ足す。</summary>
        public void AddEmptySample()
        {
            Push(0, 0);
        }

        private void Push(double x, double y)
        {
            int iIndex = bufferTail;
            xBuffer[iIndex] = x;
            yBuffer[iIndex] = y;
            bufferTail = (iIndex + 1) % TRACKBALL_BUFFER_LEN;
            if (bufferHead == bufferTail)
                bufferHead = (bufferHead + 1) % TRACKBALL_BUFFER_LEN;
        }

        /// <summary>まだ慣性が始まっていなければ、履歴の平均を初速にして始める。</summary>
        public void BeginCoast()
        {
            if (Active) return;

            double currentWeight = 1.0;
            double finalWeight = 0.0;
            double x_out = 0.0, y_out = 0.0;
            int idx = -1;
            for (int i = 0; i < TRACKBALL_BUFFER_LEN && idx != bufferHead; i++)
            {
                idx = (bufferTail - i - 1 + TRACKBALL_BUFFER_LEN) % TRACKBALL_BUFFER_LEN;
                x_out += xBuffer[idx] * currentWeight;
                y_out += yBuffer[idx] * currentWeight;
                finalWeight += currentWeight;
                currentWeight *= 1.0;
            }

            x_out /= finalWeight;
            xVel = x_out;
            y_out /= finalWeight;
            yVel = y_out;

            Active = true;
        }

        /// <summary>
        /// 1 フレーム進める。<paramref name="elapsedTime"/> は直前のフレームからの秒数。移動量（整数。端数は次のフレームへ持ち越す）を返し、
        /// 動きがあれば真。動きがなくなったら <see cref="Active"/> を偽にする。
        /// </summary>
        public bool Step(double elapsedTime, out int dx, out int dy)
        {
            double tempAngle = Math.Atan2(-yVel, xVel);
            double normX = Math.Abs(Math.Cos(tempAngle));
            double normY = Math.Abs(Math.Sin(tempAngle));
            int signX = Math.Sign(xVel);
            int signY = Math.Sign(yVel);

            double trackXvDecay = Math.Min(Math.Abs(xVel), accel * elapsedTime * normX);
            double trackYvDecay = Math.Min(Math.Abs(yVel), accel * elapsedTime * normY);
            double xVNew = xVel - (trackXvDecay * signX);
            double yVNew = yVel - (trackYvDecay * signY);
            double xMotion = (xVNew * elapsedTime) / TRACKBALL_SCALE;
            double yMotion = (yVNew * elapsedTime) / TRACKBALL_SCALE;
            if (xMotion != 0.0)
            {
                xMotion += dxRemain;
            }
            else
            {
                dxRemain = 0.0;
            }

            dx = (int)xMotion;
            dxRemain = xMotion - dx;

            if (yMotion != 0.0)
            {
                yMotion += dyRemain;
            }
            else
            {
                dyRemain = 0.0;
            }

            dy = (int)yMotion;
            dyRemain = yMotion - dy;

            xVel = xVNew;
            yVel = yVNew;

            if (dx == 0 && dy == 0)
            {
                Active = false;
                return false;
            }

            return true;
        }
    }
}
