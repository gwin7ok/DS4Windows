using DS4Windows.DI;
using SharpOSC;

namespace DS4Windows.Services
{
    /// <summary>Phase9 9-E E-1: <see cref="IOscMessageSender"/> の実装。SharpOSC の <c>UDPSender</c> を包む（旧 <c>ControlService.oscSender</c>）。</summary>
    public sealed class SharpOscMessageSender : IOscMessageSender
    {
        private readonly UDPSender _sender;

        public SharpOscMessageSender(string address, int port)
        {
            _sender = new UDPSender(address, port);
        }

        public void Send(string address, object value)
        {
            _sender.Send(new OscMessage(address, value));
        }

        public void Close()
        {
            _sender.Close();
        }
    }
}
