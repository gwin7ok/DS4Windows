using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>Phase9 9-E E-3: <see cref="IUdpMotionServer"/> の実装。<c>UdpServer</c>（internal）を、そのまま包む。</summary>
    public sealed class UdpMotionServerAdapter : IUdpMotionServer
    {
        private readonly UdpServer _server;

        public UdpMotionServerAdapter(UdpPadDetailProvider getPadDetail)
        {
            _server = new UdpServer((int padIdx, ref DualShockPadMeta meta) => getPadDetail(padIdx, ref meta));
        }

        public void Start(int port, string listenAddress = "") => _server.Start(port, listenAddress);

        public void Stop() => _server.Stop();

        public void NewReportIncoming(ref DualShockPadMeta padMeta, DS4State hidReport, byte[] outputData)
        {
            _server.NewReportIncoming(ref padMeta, hidReport, outputData);
        }
    }
}
