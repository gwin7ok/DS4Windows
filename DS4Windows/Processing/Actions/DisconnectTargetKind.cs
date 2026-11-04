namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.6b: DisconnectBT の実行時に、そのスロットのコントローラーをどう切断できるか（<see cref="IControllerDisconnectPort.GetTargetKind"/> の戻り値）。
    /// </summary>
    public enum DisconnectTargetKind
    {
        /// <summary>切断できない（コントローラーがいない、未同期、充電中、USB 接続など）。旧経路でも何も起きない。</summary>
        None = 0,

        /// <summary>Bluetooth 接続で、同期済み・非充電。すぐに <c>DisconnectBT()</c> を呼べる。</summary>
        Bluetooth = 1,

        /// <summary>ドングル（SONYWA）接続で、同期済み・非充電。離した後、アイドルになったら <c>DisconnectDongle()</c> を呼ぶ。</summary>
        Dongle = 2,
    }
}
