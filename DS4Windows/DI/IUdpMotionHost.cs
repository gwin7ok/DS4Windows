using System;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-E E-3（Phase9-E-Plan.md）: UDP モーションの部品（<see cref="IUdpMotionService"/>）が、<c>ControlService</c> に頼るもの
    /// （パッドの情報、デバイスの現在の入力、作業用の入力、イベントスレッドでの実行）。実装は <c>ControlService</c>。
    /// 部品は <c>Func&lt;IUdpMotionHost&gt;</c> で遅延して取得する（<c>ControlService</c> が部品を受けるため、循環を避ける）。
    /// </summary>
    public interface IUdpMotionHost
    {
        /// <summary>そのスロットのパッドの情報を <paramref name="meta"/> に書く。</summary>
        void GetPadDetailForIdx(int padIdx, ref DualShockPadMeta meta);

        /// <summary>そのスロットのデバイスの現在の入力（旧: <c>ControlService.CurrentState[ind]</c>）。</summary>
        DS4State GetCurrentState(int ind);

        /// <summary>そのスロットの作業用の入力（旧: <c>ControlService.TempState[ind]</c>）。UDP へ送る前の加工に使う。</summary>
        DS4State GetTempState(int ind);

        /// <summary>イベントスレッド（<c>ControlService</c> のディスパッチャー）で、非同期に実行する（旧 <c>TestQueueBus</c>）。</summary>
        void QueueOnEventThread(Action action);
    }
}
