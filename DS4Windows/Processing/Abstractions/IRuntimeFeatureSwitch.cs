using DS4Windows.Input;

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// Phase13-Step10 10-c（Phase13-Step10-Plan.md）: 画面（メイン画面、自動プロファイル画面、起動処理）が使う、実行中の機能の切り替えの窓口。
    /// OSC の受信・送信、UDP モーション、HidHide のホワイトリスト、タッチパッドのスライド表示を、それぞれの本物のサービスへ委譲するだけで、状態は持たない。
    /// 以前は <c>ControlService</c> の転送メンバー（<c>ChangeOSCListenerStatus</c>／<c>ChangeOSCSenderStatus</c>／<c>ChangeUDPStatus</c>／<c>ChangeMotionEventStatus</c>／
    /// <c>CheckHidHidePresence</c>／<c>UpdateHidHiddenAttributes</c>／<c>TouchpadSlide</c>）と、プロファイル編集画面が使う <c>GetActiveInputControl</c>／<c>setRumble</c> の転送だった。
    /// </summary>
    public interface IRuntimeFeatureSwitch
    {
        /// <summary>OSC の受信の開始・停止（<see cref="IOscService.ChangeListenerStatus"/>）。</summary>
        void ChangeOscListenerStatus(bool state);
        /// <summary>OSC の送信の開始・停止（<see cref="IOscService.ChangeSenderStatus"/>）。</summary>
        void ChangeOscSenderStatus(bool state);
        /// <summary>UDP サーバーの開始・停止（<see cref="IUdpMotionService.ChangeStatus"/>）。</summary>
        void ChangeUdpStatus(bool state, bool openPort = true);
        /// <summary>UDP のモーション送出の開始・停止（<see cref="IUdpMotionService.ChangeMotionEventStatus"/>）。</summary>
        void ChangeMotionEventStatus(bool state);
        /// <summary>HidHide のホワイトリストの確認・登録（<see cref="IHidHideService.CheckPresence"/>）。</summary>
        void CheckHidHidePresence(string exePath = "", string exeName = "Autoprofile Exe", bool addExe = true);
        /// <summary>HidHide が隠すデバイスの属性の更新（<see cref="IHidHideService.UpdateAttributes"/>）。</summary>
        void UpdateHidHiddenAttributes();
        /// <summary>そのスロットのタッチパッドのスライドの表示文字列（<see cref="ITouchpadToggleService.Slide"/>）。</summary>
        string TouchpadSlide(int ind);
        /// <summary>そのスロットで、いま操作されている入力（<see cref="IActiveInputProbe.GetActive"/>。旧 <c>ControlService.GetActiveInputControl</c>）。</summary>
        DS4Controls GetActiveInputControl(int ind);
        /// <summary>そのスロットのコントローラーの振動を、ブースト込みで設定する（<see cref="IDeviceRumbleService.SetDevRumble"/>。旧 <c>ControlService.setRumble</c>。スロットの上限の外、コントローラーがいないときは何もしない）。</summary>
        void SetRumble(byte heavyMotor, byte lightMotor, int deviceNum);
    }
}
