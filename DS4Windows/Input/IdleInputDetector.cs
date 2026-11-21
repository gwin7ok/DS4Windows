#nullable enable

using DS4Windows.Core.Model;

namespace DS4Windows.Input
{
    /// <summary>入力の 1 フレームが「無操作」（ボタン・十字・トリガー・タッチが無く、スティックが中央付近）かを判定する。状態を持たない。</summary>
    public static class IdleInputDetector
    {
        // スティックの中央付近とみなす幅。個体差の較正は未対応（TODO: 実機の遊びとジッターから決める）
        private const int Slop = 64;

        public static bool IsIdle(ControllerState cState)
        {
            if (cState.Square || cState.Cross || cState.Circle || cState.Triangle)
                return false;
            if (cState.DpadUp || cState.DpadLeft || cState.DpadDown || cState.DpadRight)
                return false;
            if (cState.L3 || cState.R3 || cState.L1 || cState.R1 || cState.Share || cState.Options || cState.PS)
                return false;
            if (cState.L2 != 0 || cState.R2 != 0)
                return false;
            if (cState.LX <= 127 - Slop || cState.LX >= 128 + Slop || cState.LY <= 127 - Slop || cState.LY >= 128 + Slop)
                return false;
            if (cState.RX <= 127 - Slop || cState.RX >= 128 + Slop || cState.RY <= 127 - Slop || cState.RY >= 128 + Slop)
                return false;
            if (cState.Touch1 || cState.Touch2 || cState.TouchButton)
                return false;
            return true;
        }
    }
}
