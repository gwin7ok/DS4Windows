using DS4Windows.Processing.Touchpad;
using DS4Windows.Core.Model;

namespace DS4Windows.Processing.Services.Slots
{
    /// <summary>
    /// <see cref="DS4Windows.Processing.Abstractions.IActiveInputProbe"/> の実装（DI の Singleton。状態は持たない）。
    /// 旧 <c>ControlService.GetActiveInputControl</c> を、そのまま移したもの（判定の順序も同じ）。
    /// 値の特性化は 対応するテストが守る。
    /// </summary>
    public sealed class ActiveInputProbe : DS4Windows.Processing.Abstractions.IActiveInputProbe
    {
        public DS4Controls GetActive(ControllerState cState, TouchpadMouse tp, bool connected)
        {
            DS4Controls result = DS4Controls.None;
            if (connected)
            {
                if (Mapping.GetBoolButtonMapping(cState.Cross))
                    result = DS4Controls.Cross;
                else if (Mapping.GetBoolButtonMapping(cState.Circle))
                    result = DS4Controls.Circle;
                else if (Mapping.GetBoolButtonMapping(cState.Triangle))
                    result = DS4Controls.Triangle;
                else if (Mapping.GetBoolButtonMapping(cState.Square))
                    result = DS4Controls.Square;
                else if (Mapping.GetBoolButtonMapping(cState.L1))
                    result = DS4Controls.L1;
                else if (Mapping.GetBoolTriggerMapping(cState.L2))
                    result = DS4Controls.L2;
                else if (Mapping.GetBoolButtonMapping(cState.L3))
                    result = DS4Controls.L3;
                else if (Mapping.GetBoolButtonMapping(cState.R1))
                    result = DS4Controls.R1;
                else if (Mapping.GetBoolTriggerMapping(cState.R2))
                    result = DS4Controls.R2;
                else if (Mapping.GetBoolButtonMapping(cState.R3))
                    result = DS4Controls.R3;
                else if (Mapping.GetBoolButtonMapping(cState.DpadUp))
                    result = DS4Controls.DpadUp;
                else if (Mapping.GetBoolButtonMapping(cState.DpadDown))
                    result = DS4Controls.DpadDown;
                else if (Mapping.GetBoolButtonMapping(cState.DpadLeft))
                    result = DS4Controls.DpadLeft;
                else if (Mapping.GetBoolButtonMapping(cState.DpadRight))
                    result = DS4Controls.DpadRight;
                else if (Mapping.GetBoolButtonMapping(cState.Share))
                    result = DS4Controls.Share;
                else if (Mapping.GetBoolButtonMapping(cState.Options))
                    result = DS4Controls.Options;
                else if (Mapping.GetBoolButtonMapping(cState.PS))
                    result = DS4Controls.PS;
                else if (Mapping.GetBoolAxisDirMapping(cState.LX, true))
                    result = DS4Controls.LXPos;
                else if (Mapping.GetBoolAxisDirMapping(cState.LX, false))
                    result = DS4Controls.LXNeg;
                else if (Mapping.GetBoolAxisDirMapping(cState.LY, true))
                    result = DS4Controls.LYPos;
                else if (Mapping.GetBoolAxisDirMapping(cState.LY, false))
                    result = DS4Controls.LYNeg;
                else if (Mapping.GetBoolAxisDirMapping(cState.RX, true))
                    result = DS4Controls.RXPos;
                else if (Mapping.GetBoolAxisDirMapping(cState.RX, false))
                    result = DS4Controls.RXNeg;
                else if (Mapping.GetBoolAxisDirMapping(cState.RY, true))
                    result = DS4Controls.RYPos;
                else if (Mapping.GetBoolAxisDirMapping(cState.RY, false))
                    result = DS4Controls.RYNeg;
                else if (Mapping.GetBoolTouchMapping(tp.leftDown))
                    result = DS4Controls.TouchLeft;
                else if (Mapping.GetBoolTouchMapping(tp.rightDown))
                    result = DS4Controls.TouchRight;
                else if (Mapping.GetBoolTouchMapping(tp.multiDown))
                    result = DS4Controls.TouchMulti;
                else if (Mapping.GetBoolTouchMapping(tp.upperDown))
                    result = DS4Controls.TouchUpper;
            }

            return result;
        }
    }
}
