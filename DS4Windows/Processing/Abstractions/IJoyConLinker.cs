using System.Collections.Generic;

namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-E E-6d（Phase9-E6-Plan.md §3.2）: Joy-Con の L と R を 1 つに結合する手順。
    /// 以前は <c>ControlService.Start</c>（先に来たものを主にして順に組む）と <c>HotPlug</c>（主と副を先に探してから組む）の中にあった。
    /// 2 通りの手順は、挙動を変えないため、統合せずにそのまま 2 組のメソッドにした。状態は呼び出し側（ループの局所変数）が持つ。
    /// </summary>
    public interface IJoyConLinker
    {
        /// <summary>
        /// <c>Start</c> 用。デバイスが結合の対象なら、保留中の主デバイスと結合する（結合したら保留を空にし、最初の 1 つなら保留にする）。
        /// 戻り値は、更新後の保留中の主デバイス。
        /// </summary>
        IJoinableDevice LinkOnStart(IJoinableDevice device, IJoinableDevice pendingPrimary, JoyConDeviceOptions.LinkMode mode);

        /// <summary><c>HotPlug</c> 用。接続済みで結合相手のいない Joy-Con から、保留中の主デバイスと副デバイスを探す（結合しない設定のときは両方 null）。</summary>
        void FindPendingForHotPlug(IEnumerable<IJoinableDevice> devices, JoyConDeviceOptions.LinkMode mode,
            out IJoinableDevice pendingPrimary, out IJoinableDevice pendingSecondary);

        /// <summary><c>HotPlug</c> 用。デバイスが結合の対象なら、保留中の相手と結合する（結合したら、使った保留を空にする）。</summary>
        void LinkOnHotPlug(IJoinableDevice device, ref IJoinableDevice pendingPrimary, ref IJoinableDevice pendingSecondary,
            JoyConDeviceOptions.LinkMode mode);
    }
}
