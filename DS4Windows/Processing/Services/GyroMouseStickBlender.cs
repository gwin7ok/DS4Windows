using System;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-B（Phase9-B-Plan.md）: <see cref="IGyroMouseStickBlender"/> の実装（DI の Singleton。状態は持たない）。
    /// 旧 <c>Mapping.TempMouseJoystick</c> の内側（出力スティックの X／Y それぞれで、ジャイロ由来の値と通常のスティックの値のうち中心から遠い方を採る）を、そのまま移したもの。
    /// 値の特性化は <c>Phase9BGyroMouseStickBlenderTests</c> が守る。
    /// </summary>
    public sealed class GyroMouseStickBlender : IGyroMouseStickBlender
    {
        public void Blend(GyroMouseStickInfo msinfo, byte gyroX, byte gyroY, DS4State MappedState, PostMapStickData mapStickData)
        {
            byte gyroTempX = gyroX;
            if (msinfo.OutputHorizontal() && gyroTempX != 128)
            {
                byte outputStickXVal = msinfo.outputStick == GyroMouseStickInfo.OutputStick.RightStick ?
                    MappedState.RX : MappedState.LX;

                byte tempAxisVal = 128;

                if (outputStickXVal != 128)
                {
                    tempAxisVal = Math.Abs(gyroTempX - 128) > Math.Abs(outputStickXVal - 128) ?
                        gyroTempX : outputStickXVal;
                }
                else
                {
                    tempAxisVal = gyroTempX;
                }

                if (msinfo.outputStick == GyroMouseStickInfo.OutputStick.RightStick)
                {
                    MappedState.RX = tempAxisVal;
                    mapStickData.RX = tempAxisVal;
                    mapStickData.dirty = true;
                }
                else if (msinfo.outputStick == GyroMouseStickInfo.OutputStick.LeftStick)
                {
                    MappedState.LX = tempAxisVal;
                    mapStickData.LX = tempAxisVal;
                    mapStickData.dirty = true;
                }
            }

            byte gyroTempY = gyroY;
            if (msinfo.OutputVertical() && gyroTempY != 128)
            {
                byte outputStickYVal = msinfo.outputStick == GyroMouseStickInfo.OutputStick.RightStick ?
                MappedState.RY : MappedState.LY;
                byte tempAxisVal = 128;

                if (outputStickYVal != 128)
                    tempAxisVal = Math.Abs(gyroTempY - 128) > Math.Abs(outputStickYVal - 128) ?
                        gyroTempY : outputStickYVal;
                else
                    tempAxisVal = gyroTempY;

                if (msinfo.outputStick == GyroMouseStickInfo.OutputStick.RightStick)
                {
                    MappedState.RY = tempAxisVal;
                    mapStickData.RY = tempAxisVal;
                    mapStickData.dirty = true;
                }
                else if (msinfo.outputStick == GyroMouseStickInfo.OutputStick.LeftStick)
                {
                    MappedState.LY = tempAxisVal;
                    mapStickData.LY = tempAxisVal;
                    mapStickData.dirty = true;
                }
            }

            // Don't reset Mouse Joystick output coords here
            //gyroTempX = gyroTempY = 128;
        }
    }
}
