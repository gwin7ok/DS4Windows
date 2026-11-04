using System;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// <see cref="IServiceDebugRelay"/> の実装。6 つのサービスの <c>DebugMessage</c> を、そのまま 1 つのイベントへ流す
    /// （旧 <c>ControlService</c> のコンストラクターにあった 6 行の購読を、そのまま移したもの）。メッセージの内容には触れない。
    /// </summary>
    public class ServiceDebugRelay : IServiceDebugRelay
    {
        public event Action<string, bool> DebugMessage;

        public ServiceDebugRelay(IHidHideService hidHide, IUdpMotionService udpMotion, ILatencyMonitor latencyMonitor,
            ITouchpadToggleService touchpadToggle, IDeviceConnectionSetup deviceConnectionSetup, IDeviceDisconnectHandler deviceDisconnect)
        {
            if (hidHide == null) throw new ArgumentNullException(nameof(hidHide));
            if (udpMotion == null) throw new ArgumentNullException(nameof(udpMotion));
            if (latencyMonitor == null) throw new ArgumentNullException(nameof(latencyMonitor));
            if (touchpadToggle == null) throw new ArgumentNullException(nameof(touchpadToggle));
            if (deviceConnectionSetup == null) throw new ArgumentNullException(nameof(deviceConnectionSetup));
            if (deviceDisconnect == null) throw new ArgumentNullException(nameof(deviceDisconnect));

            hidHide.DebugMessage += Relay;
            udpMotion.DebugMessage += Relay;
            latencyMonitor.DebugMessage += Relay;
            touchpadToggle.DebugMessage += Relay;
            deviceConnectionSetup.DebugMessage += Relay;
            deviceDisconnect.DebugMessage += Relay;
        }

        private void Relay(string data, bool warning) => DebugMessage?.Invoke(data, warning);
    }
}
