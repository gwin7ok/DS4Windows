using System;

namespace DS4Windows.Input
{
    /// <summary>
    /// 機種ごとの電池の段階の値（「最大が max のうち value」）を、0〜100 の % にする共通の計算。
    /// 段階の数と、値の取り出し方（レポートのビット、別の API）は機種で違うが、% への変換は同じ（<c>value * 100 / max</c>、100 を超えない）。
    /// </summary>
    public static class BatteryScale
    {
        public static int ToPercent(int value, int max)
        {
            if (max <= 0)
            {
                return 0;
            }

            return Math.Min(Math.Max(value, 0) * 100 / max, 100);
        }
    }
}
