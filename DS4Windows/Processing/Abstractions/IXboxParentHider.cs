namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// Xbox 系コントローラーの USB の親（XUSB のデバイス インターフェースを持つ機器）と、その下の HID の子を、HidHide で隠す・戻す。
    /// XInput を隠すには、親と HID の子の両方が要る（実機の調査: Xbox-Input-Research.md §6）。足した分だけを記録し、戻すのも足した分だけ。
    /// 実装は <c>XboxParentHider</c>（DI の Singleton）。
    /// </summary>
    public interface IXboxParentHider
    {
        /// <summary>
        /// 機器（XUSB のデバイス インターフェースのパス。持ち主の機器のインスタンス ID は、例: <c>USB\VID_045E&amp;PID_028E\A02014100201</c>）を隠す／戻す。隠すときは、先に DS4Windows 自身を許可リストへ入れて、
        /// 隠したあとも、DS4Windows が XInput で読めるようにする。HidHide を使えない場合は、何も変えずに理由を返す（切り替えはしない）。
        /// </summary>
        XboxParentHideResult Apply(string devicePath, bool hide);

        /// <summary>自分が足した分を、すべて戻す（サービスの停止のとき）。</summary>
        /// <returns>戻した分があったか（なければ false）</returns>
        bool RestoreAll();

        /// <summary>前回の異常終了などで、足したまま残った分があれば戻す（サービスの開始のとき）。</summary>
        void RecoverStale();
    }
}
