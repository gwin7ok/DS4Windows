using System;
using DS4Windows;

namespace DS4Windows.DI
{

    public interface IDeviceStateService
    {
        // Phase7-Step2 W3: デバイスの一覧・接続状態・電池残量などの取得（Devices／GetDevice／IsDeviceConnected／ConnectedControllersCount／
        // GetDeviceMacAddress／GetConnectionType／GetBatteryLevel／SetDevice／DeviceStateChanged）は削除した。
        // 実装の内部配列は本体で一度も設定されず、常に「未接続」を返す孤立実装だった。接続中のデバイスの実体は
        // ControlService.DS4Controllers（IDeviceStateAccessor.GetController で参照する）。
        // 参照: docs-forDIMG/MadeByAgent/Phase7-Step2-Plan.md §2.2

        // ---- Phase6-Step2-2 (PR-2): 起動後の初回接続判定（Global への薄い委譲）----
        bool IsFirstConnection(int slotIndex);
        void MarkConnected(int slotIndex);

        // ---- 再接続のプロファイルをコントローラー単位にする修正（Reconnect-Profile-PerController-Fix-Plan.md）----
        // 「起動後の初回接続」と「直前に適用したプロファイル」を、スロット番号ではなく、コントローラー（MAC アドレス）ごとに持つ。
        // macAddress が null／空（有効なシリアルがない）のときは、従来のスロット単位の動作に落とす。

        /// <summary>起動後に、そのコントローラー（MAC）を初めて接続するか。MAC がなければ、スロット単位（<see cref="IsFirstConnection(int)"/>）。</summary>
        bool IsFirstConnection(int slotIndex, string macAddress);

        /// <summary>初回接続の完了を記録する（スロットのフラグと、MAC のフラグの両方）。</summary>
        void MarkConnected(int slotIndex, string macAddress);

        /// <summary>そのコントローラー（MAC）に、最後に適用したプロファイルを記憶する（起動後だけ有効。永続化しない）。</summary>
        void RememberProfile(string macAddress, string profileName);

        /// <summary>そのコントローラー（MAC）に、最後に適用したプロファイル。記憶がなければ null。</summary>
        string GetRememberedProfile(string macAddress);

        // ---- Phase6-Step7-1: 起動時に全スロットの「起動後の初回接続」フラグを立て直す（Global への薄い委譲）----
        void ResetConnectionFlags();

        // ---- Phase6-Step2-4 (PR-4): デバイスのシリアル（MAC アドレス）変更通知（Global への薄い委譲）----
        void OnDeviceSerialChange(object sender, int slotIndex, string serial);

        // Phase7-Step10d W4: 旧 Global.DeviceSerialChange（static イベント）の移行先。
        event System.EventHandler<SerialChangeArgs> DeviceSerialChange;
    }
}