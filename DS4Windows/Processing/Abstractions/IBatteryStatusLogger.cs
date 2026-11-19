#nullable enable

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// 接続中の全コントローラーの電池の残量を、一定の間隔で、機種や取得方法によらず同じ形式でログに出す。
    /// 取得そのものは機種ごと（DS4 のレポート、USB の Xbox の XInput、Bluetooth の Xbox の GATT）のまま、各機器が保持する値を読む。
    /// </summary>
    public interface IBatteryStatusLogger
    {
        /// <summary>定期の記録を始める。1 回呼ぶ（2 回目以降は何もしない）。</summary>
        void Attach();
    }
}
