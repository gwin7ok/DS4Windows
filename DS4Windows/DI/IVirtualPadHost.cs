using System;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-E E-4（Phase9-E-Plan.md）: 仮想パッド出力の部品（<see cref="IVirtualPadService"/>）が、<c>ControlService</c> に頼るもの
    /// （振動のブースト込みの設定、イベントスレッドでの実行）。実装は <c>ControlService</c>。部品は <c>Func&lt;IVirtualPadHost&gt;</c> で遅延して取得する（循環を避ける）。
    /// </summary>
    public interface IVirtualPadHost
    {
        /// <summary>設定の「振動のブースト」と「モーターの入れ替え」を反映して、デバイスの振動を設定する（仮想パッドからの振動の要求を、実機へ戻すのに使う）。</summary>
        void SetDevRumble(DS4Device device, byte heavyMotor, byte lightMotor, int deviceNum);

        /// <summary>イベントスレッド（<c>ControlService</c> のディスパッチャー）で、非同期に実行する。</summary>
        void QueueOnEventThread(Action action);
    }
}
