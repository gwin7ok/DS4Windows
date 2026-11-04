namespace DS4Windows.DI
{
    /// <summary>
    /// Phase13-Step6 6-d（Phase13-Step6-Plan.md §4.1）: プロファイル読込の鎖（<c>ProfileRepository.LoadProfile</c> → <c>IProfileXmlStore</c> →
    /// <c>LegacyProfileXmlSerializer</c> → <c>ProfileLoadContext</c> → <c>Profile*LoadSection</c> → <c>BackingStore.SetGyro*</c> ほか）が、
    /// 引数 <c>control</c>（以前は <c>ControlService</c>）から実際に使う 8 つのメンバーだけを切り出した窓口。
    /// 6-e で、鎖の <c>ControlService control</c> 引数の型をこの型に置き換える（<c>BackingStore</c> に新しい依存を足さず、引数のまま型だけ替える）。
    /// </summary>
    public interface IProfileLoadTarget
    {
        /// <summary>スロットのコントローラー（旧: <c>control.DS4Controllers[ind]</c>。範囲外は例外にせず null）。</summary>
        DS4Device GetController(int ind);

        /// <summary>スロットのタッチパッドのマウス（旧: <c>control.touchPad[ind]</c>。範囲外は元と同じく例外）。</summary>
        Mouse GetTouchPad(int ind);

        /// <summary>スロットの仮想パッド（旧: <c>control.outputDevices[ind]</c>。範囲外は元と同じく例外）。</summary>
        OutputDevice GetOutputDevice(int ind);

        /// <summary>仮想パッドを接続する（<c>IVirtualPadService.PluginOutDev</c> への転送）。</summary>
        void PluginOutDev(int index, DS4Device device);

        /// <summary>仮想パッドを取り外す（<c>IVirtualPadService.UnplugOutDev</c> への転送）。</summary>
        void UnplugOutDev(int index, DS4Device device, bool immediate = false, bool force = false);

        /// <summary>プロファイルの設定をデバイスへ反映する（<c>IDeviceConnectionSetup.ApplyProfileOptions</c>。タッチパッドはこの窓口が渡す）。</summary>
        void CheckProfileOptions(int ind, DS4Device device, bool startUp = false);

        /// <summary>タッチパッドの OFF 状態を開始する（<c>ITouchpadToggleService.StartTouchpadOff</c> への転送）。</summary>
        void StartTPOff(int deviceID);

        /// <summary>
        /// プロファイルを読み込む前に、前のプロファイルのフリックスティックの進行・デルタ加速・マウスの状態をリセットする
        /// （旧 <c>ControlService.PreLoadReset</c>。テストプロファイルの添字では何もしない）。
        /// </summary>
        void PreLoadReset(int ind);
    }
}
