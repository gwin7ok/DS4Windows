#nullable enable

using System.Diagnostics;

namespace DS4Windows.Input
{
    /// <summary>
    /// 振動の出力を、変化がなくても一定間隔で送り直すための判定（コントローラーが、振動の出力を長く受け取らないと止めるため）。
    /// 時計は機器が持ち、判定だけをここで行う。
    /// </summary>
    public static class HapticStandby
    {
        private const long ResendIntervalMs = 4000L;

        /// <summary>
        /// 出力を送るべきなら true。変化があれば送り、時計を（振動中は再始動、停止中はリセット）。
        /// 変化がなくても、振動中で時計が 4 秒を超えたら送り直し、時計を再始動する。
        /// </summary>
        public static bool ShouldSend(Stopwatch standby, bool change, bool rumbleSet)
        {
            if (change)
            {
                if (rumbleSet)
                    standby.Restart();
                else
                    standby.Reset();
                return true;
            }

            if (rumbleSet && standby.ElapsedMilliseconds >= ResendIntervalMs)
            {
                standby.Restart();
                return true;
            }

            return false;
        }
    }
}
