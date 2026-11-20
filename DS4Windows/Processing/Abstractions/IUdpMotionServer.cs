using DS4Windows.Core.Model;

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>
    /// Cemuhook 互換のモーションデータの UDP サーバーの窓口。実装は <c>UdpServer</c>（internal）を包む。
    /// ソケットなしで、開始・停止・送出の判断をテストするための境界。
    /// </summary>
    public interface IUdpMotionServer
    {
        /// <summary>待ち受けを始める。ポートが使えないとき、<see cref="System.Net.Sockets.SocketException"/>。</summary>
        void Start(int port, string listenAddress = "");

        void Stop();

        /// <summary>1 フレーム分の入力を、購読中のクライアントへ送る（入力スレッドから呼ばれる。割り当てなし）。</summary>
        void NewReportIncoming(ref DualShockPadMeta padMeta, ControllerState hidReport, byte[] outputData);
    }
}
