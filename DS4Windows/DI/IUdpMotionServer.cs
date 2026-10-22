namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-E E-3（Phase9-E-Plan.md）: Cemuhook 互換のモーションデータの UDP サーバーの窓口。実装は <c>UdpServer</c>（internal）を包む。
    /// ソケットなしで、開始・停止・送出の判断をテストするための境界。
    /// </summary>
    public interface IUdpMotionServer
    {
        /// <summary>待ち受けを始める。ポートが使えないとき、<see cref="System.Net.Sockets.SocketException"/>。</summary>
        void Start(int port, string listenAddress = "");

        void Stop();

        /// <summary>1 フレーム分の入力を、購読中のクライアントへ送る（入力スレッドから呼ばれる。割り当てなし）。</summary>
        void NewReportIncoming(ref DualShockPadMeta padMeta, DS4State hidReport, byte[] outputData);
    }
}
