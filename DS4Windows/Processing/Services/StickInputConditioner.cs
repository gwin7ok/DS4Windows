using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-A(2)-B（Phase9-A2-Plan.md）: <see cref="IStickInputConditioner"/> の実装（DI の Singleton。状態は持たない）。
    /// 旧 <c>Mapping.ApplyStickCalibration</c>／<c>CalcStickAxisFuzz</c>／<c>CalcAntiSnapbackStick</c> を、そのまま移したもの
    /// （<c>out</c> 引数は、入力と出力が同じ変数だったため、<c>ref</c> に直した。アンチスナップバックの時刻は、引数で受ける。計算結果は変わらない）。
    /// 値の特性化は、ドリフト補正とファズが <c>Phase9A1SetCurveAndDeadzoneGoldenTests</c>、アンチスナップバックが <c>Phase9A2AntiSnapbackDifferentialTests</c>（旧実装との差分テスト）が守る。
    /// </summary>
    public sealed class StickInputConditioner : IStickInputConditioner
    {
        public DS4State ApplyDrift(int device, DS4State state, IProfileSettingsService settings)
        {
            if (settings.RightStickDriftXAxis[device] != 0)
            {
                var translated = state.RX - settings.RightStickDriftXAxis[device];
                state.RX = (byte)Math.Clamp(translated, 0, 255);
            }
            if (settings.RightStickDriftYAxis[device] != 0)
            {
                var translated = state.RY - settings.RightStickDriftYAxis[device];
                state.RY = (byte)Math.Clamp(translated, 0, 255);
            }

            if (settings.LeftStickDriftXAxis[device] != 0)
            {
                var translated = state.LX - settings.LeftStickDriftXAxis[device];
                state.LX = (byte)Math.Clamp(translated, 0, 255);
            }

            if (settings.LeftStickDriftYAxis[device] != 0)
            {
                var translated = state.LY - settings.LeftStickDriftYAxis[device];
                state.LY = (byte)Math.Clamp(translated, 0, 255);
            }

            return state;
        }

        public void Fuzz(byte[] last, int stickId, int delta, ref byte x, ref byte y)
        {
            if (stickId < 0 || stickId > 2)
            {
                throw new ArgumentOutOfRangeException("Stick ID has to be either 0 or 1");
            }

            byte axisXValue = x;
            byte axisYValue = y;

            int xIdX = stickId == 0 ? 0 : 2;
            int yIdX = stickId == 1 ? 1 : 3;
            ref byte lastXVal = ref last[xIdX];
            ref byte lastYVal = ref last[yIdX];
            x = lastXVal;
            y = lastYVal;

            int deltaX = axisXValue - lastXVal;
            int deltaY = axisYValue - lastYVal;
            int magSqu = (deltaX * deltaX) + (deltaY * deltaY);
            int deltaSqu = delta * delta;

            if (axisXValue == 0 || axisXValue == 255 || magSqu > deltaSqu)
            {
                x = axisXValue;
                lastXVal = axisXValue;
            }

            if (axisYValue == 0 || axisYValue == 255 || magSqu > deltaSqu)
            {
                y = axisYValue;
                lastYVal = axisYValue;
            }
        }

        public void AntiSnapback(Queue<DS4TimedStickAxisValue> history, double delta, long timeoutMs, long timestamp, ref byte x, ref byte y)
        {
            // 比較には、補正前の入力を使う（ラムダは ref 引数を捕捉できないため、ローカルにコピーする）
            byte axisXValue = x;
            byte axisYValue = y;

            Queue<DS4TimedStickAxisValue> queue = history;
            while (queue.Count > 0 && queue.Peek().timestamp < timestamp - timeoutMs)
            {
                queue.Dequeue();
            }
            if (queue.Any(oldValues =>
            {
                double distanceSquared = Math.Pow(axisXValue - oldValues.x, 2) + Math.Pow(axisYValue - oldValues.y, 2);
                if (distanceSquared >= (delta * delta))
                {
                    //Checks if the line between two points touches a 15 unit circle in the middle
                    double t = ((128 - axisXValue) * (oldValues.x - axisXValue) + (128 - axisYValue) * (oldValues.y - axisYValue)) / distanceSquared;
                    t = Math.Max(0, Math.Min(1, t));
                    double distanceToMiddleSquared = Math.Pow(128 - (axisXValue + t * (oldValues.x - axisXValue)), 2) + Math.Pow(128 - (axisYValue + t * (oldValues.y - axisYValue)), 2);
                    return distanceToMiddleSquared <= 15 * 15;
                }
                else
                {
                    return false;
                }
            }))
            {
                x = y = 128;
            }
            else
            {
                x = axisXValue;
                y = axisYValue;
            }
            queue.Enqueue(new DS4TimedStickAxisValue(axisXValue, axisYValue, timestamp));
        }
    }
}
