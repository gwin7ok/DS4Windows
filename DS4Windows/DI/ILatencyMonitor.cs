using System;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-E E-5（Phase9-E5-Plan.md）: 入力レポートごとのエラーのログと、遅延の監視（遅れたらライトバーを赤く点滅させる）。
    /// 以前は <c>ControlService.On_Report</c> の先頭と <c>LagFlashWarning</c>、状態 <c>lag</c>／<c>inWarnMonitor</c>／<c>tempStrings</c> だった。
    /// 高頻度（レポートごと）に呼ばれるため、呼び出しで割り当てをしない。
    /// </summary>
    public interface ILatencyMonitor
    {
        /// <summary>ログ（これまでの <c>ControlService.LogDebug</c>）。<c>ControlService</c> が購読して Debug イベントへ流す。</summary>
        event Action<string, bool> DebugMessage;

        /// <summary>レポートが届くたびに呼ぶ。デバイスのエラーをログに出し、接続 5 秒後から遅延を監視する。</summary>
        void OnReport(DS4Device device, int ind);

        /// <summary>切断・停止時に、そのスロットの監視状態を初期状態へ戻す（旧 <c>lag[i] = false; inWarnMonitor[i] = false;</c>）。</summary>
        void Reset(int ind);
    }
}
