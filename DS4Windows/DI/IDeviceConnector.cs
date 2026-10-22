namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-E E-6f1（Phase9-E6-Plan.md §9.2）: コントローラーをスロットへつなぐときの組み立て（設定の読み込み、イベントの購読、マウスの作成、プロファイルの適用、仮想パッドの接続、レポートの購読、更新の開始）。
    /// 以前は <c>ControlService.PrepareConnectedInputControllerSettingEvents</c>／<c>BeginPrepareConnectedInputController</c>／<c>CheckQuickCharge</c> だった。
    /// <c>ControlService.Start</c>／<c>HotPlug</c> が呼ぶ。入力レポートの処理（<c>On_Report</c>）は <c>ControlService</c> に残り、<see cref="IDeviceSlotHost.OnReport"/> 経由で呼ぶ。
    /// </summary>
    public interface IDeviceConnector
    {
        /// <summary>接続の組み立ての前段（HidHide の影響を受けるデバイスの印、種類ごとの設定の変更への反応）。</summary>
        void BeginPrepare(DS4Device device, bool showlog = false);

        /// <summary>接続の組み立て本体。<c>DS4Controllers[index]</c> へ入れた後に呼ぶ。最後にデバイスの更新（<c>StartUpdate</c>）を始める。</summary>
        void PrepareConnected(int numControllers, DS4Device device, int index);
    }
}
