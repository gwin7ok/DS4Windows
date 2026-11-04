namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-B（Phase9-B-Plan.md）: <see cref="DS4Windows.DI.IControlActivityReader"/> の実装（DI の Singleton。状態は持たない）。
    /// 旧 <c>Mapping.GetBoolMappingExternal</c>（と、その補助 <c>IsUsingGyroForControls</c>）を、そのまま移したもの（設定は引数で受ける）。
    /// 値の特性化は <c>Phase9BControlActivityReaderTests</c> が守る。
    /// </summary>
    public sealed class ControlActivityReader : DS4Windows.DI.IControlActivityReader
    {
        public bool IsActive(DS4Windows.DI.IProfileSettingsService settings, int device, DS4Controls control,
            DS4State cState, DS4StateExposed eState, Mouse tp)
        {
            bool result = false;

            if (control >= DS4Controls.Square && control <= DS4Controls.Cross)
            {
                switch (control)
                {
                    case DS4Controls.Cross: result = cState.Cross; break;
                    case DS4Controls.Square: result = cState.Square; break;
                    case DS4Controls.Triangle: result = cState.Triangle; break;
                    case DS4Controls.Circle: result = cState.Circle; break;
                    default: break;
                }
            }
            else if (control >= DS4Controls.L1 && control <= DS4Controls.R3)
            {
                switch (control)
                {
                    case DS4Controls.L1: result = cState.L1; break;
                    case DS4Controls.R1: result = cState.R1; break;
                    case DS4Controls.L2: result = cState.L2 > 100; break;
                    case DS4Controls.R2: result = cState.R2 > 100; break;
                    case DS4Controls.L3: result = cState.L3; break;
                    case DS4Controls.R3: result = cState.R3; break;
                    default: break;
                }
            }
            else if (control >= DS4Controls.DpadUp && control <= DS4Controls.DpadLeft)
            {
                switch (control)
                {
                    case DS4Controls.DpadUp: result = cState.DpadUp; break;
                    case DS4Controls.DpadDown: result = cState.DpadDown; break;
                    case DS4Controls.DpadLeft: result = cState.DpadLeft; break;
                    case DS4Controls.DpadRight: result = cState.DpadRight; break;
                    default: break;
                }
            }
            else if (control >= DS4Controls.LXNeg && control <= DS4Controls.RYPos)
            {
                switch (control)
                {
                    case DS4Controls.LXNeg: result = cState.LX < 128 - 55; break;
                    case DS4Controls.LYNeg: result = cState.LY < 128 - 55; break;
                    case DS4Controls.RXNeg: result = cState.RX < 128 - 55; break;
                    case DS4Controls.RYNeg: result = cState.RY < 128 - 55; break;
                    case DS4Controls.LXPos: result = cState.LX > 128 + 55; break;
                    case DS4Controls.LYPos: result = cState.LY > 128 + 55; break;
                    case DS4Controls.RXPos: result = cState.RX > 128 + 55; break;
                    case DS4Controls.RYPos: result = cState.RY > 128 + 55; break;
                    default: break;
                }
            }
            else if (control >= DS4Controls.TouchLeft && control <= DS4Controls.TouchRight)
            {
                switch (control)
                {
                    case DS4Controls.TouchLeft: result = (tp != null ? tp.leftDown : false); break;
                    case DS4Controls.TouchRight: result = (tp != null ? tp.rightDown : false); break;
                    case DS4Controls.TouchMulti: result = (tp != null ? tp.multiDown : false); break;
                    case DS4Controls.TouchUpper: result = (tp != null ? tp.upperDown : false); break;
                    default: break;
                }
            }
            else if (control >= DS4Controls.SwipeLeft && control <= DS4Controls.SwipeDown)
            {
                switch (control)
                {
                    case DS4Controls.SwipeUp: result = (tp != null && tp.swipeUp); break;
                    case DS4Controls.SwipeDown: result = (tp != null && tp.swipeDown); break;
                    case DS4Controls.SwipeLeft: result = (tp != null && tp.swipeLeft); break;
                    case DS4Controls.SwipeRight: result = (tp != null && tp.swipeRight); break;
                    default: break;
                }
            }
            else if (control >= DS4Controls.GyroXPos && control <= DS4Controls.GyroZNeg)
            {
                bool saControls = settings.GyroOutputMode[device] == GyroOutMode.Controls;

                switch (control)
                {
                    // Phase6-Step3-6b（決定P2）: 呼び出し元が ctrl を持たないため、引数の settings［Mapping が保持する profileSettings］から読む。Phase7-Step8 W2 で instance 化済み（Phase7c W3 でコメントを訂正）。
                    case DS4Controls.GyroXPos: result = saControls ? settings.SXSens[device] * -eState.AccelX > 67 : false; break;
                    case DS4Controls.GyroXNeg: result = saControls ? settings.SXSens[device] * -eState.AccelX < -67 : false; break;
                    case DS4Controls.GyroZPos: result = saControls ? settings.SZSens[device] * eState.AccelZ > 67 : false; break;
                    case DS4Controls.GyroZNeg: result = saControls ? settings.SZSens[device] * eState.AccelZ < -67 : false; break;
                    default: break;
                }
            }
            else
            {
                switch (control)
                {
                    case DS4Controls.PS: result = cState.PS; break;
                    case DS4Controls.Share: result = cState.Share; break;
                    case DS4Controls.Options: result = cState.Options; break;
                    default: break;
                }
            }

            return result;
        }
    }
}
