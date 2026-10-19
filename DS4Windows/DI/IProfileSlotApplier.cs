namespace DS4Windows.Services
{
    /// <summary>
    /// 接続時などに、指定スロットへプロファイルを適用するための <see cref="ControlService"/> 側の出力ポート。
    /// ControlService は <c>IProfileApplicationService</c> に直接依存せず、本インターフェース（依存の逆転）のみを知る。
    /// 戻り値は適用の成否で、呼び出し側が後続処理（ログ・プロファイル既定値の反映）に使う。
    /// </summary>
    public interface IProfileSlotApplier
    {
        bool ApplyToSlot(int slotIndex, string profileName, ProfileChangeSource source);

        /// <summary>
        /// Phase8-Step6 W2（Phase8-Step6-Plan.md §3.2。Q1＝A）: コントローラーが物理的に切断されたときの後始末。
        /// エンジンの状態（抑制の集合、束縛ごとの実行時の状態、復帰の合図）と、登録簿の束縛を消す（<c>ITriggerDispatchEngine.OnDeviceDisconnected</c>）。
        /// <c>Mapping.HandleDeviceDisconnect</c> はプロファイルの適用のたびにも呼ばれるため、そこには足せない。切断の経路（<c>On_DS4Removal</c>）だけが呼ぶ。
        /// スロットのプロファイル選択（<c>OlderProfilePath</c> ほか）には触れない。範囲外のスロットでは何もしない。例外は投げない（切断処理を止めない）。
        /// </summary>
        void ReleaseSlot(int slotIndex);

        /// <summary>
        /// Phase8-Step6 W2（Q2＝A）: 接続のときに、プロファイルを適用し直さない経路（一時プロファイル／AutoProfile の使用中）で、
        /// そのスロットの束縛を登録簿へ差し替える（<c>IProfileApplicationService.PublishBindings</c>）。切断で登録簿を空にしても、再接続後に束縛が戻る。
        /// 範囲外のスロットでは何もしない。
        /// </summary>
        void PublishSlot(int slotIndex);
    }
}