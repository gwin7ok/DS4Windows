using System;
using System.Collections.Generic;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;

namespace DS4Windows.Input.Xbox
{
    /// <summary>
    /// 接続中の Xbox 系コントローラー（XUSB のインターフェース）を、XInput の通信路として見つける供給元。
    /// XInput の番号を読めなかったインターフェースは、まだ使えないので返さない（次の列挙で見つかる）。
    /// </summary>
    public sealed class XInputTransportSource : IInputTransportSource
    {
        private readonly IXInputApi api;
        private readonly IXusbInterfaceSource interfaces;

        public XInputTransportSource(IXInputApi api, IXusbInterfaceSource interfaces)
        {
            this.api = api ?? throw new ArgumentNullException(nameof(api));
            this.interfaces = interfaces ?? throw new ArgumentNullException(nameof(interfaces));
        }

        public IReadOnlyList<InputTransportCandidate> Enumerate()
        {
            var result = new List<InputTransportCandidate>();
            foreach (XusbInterfaceInfo info in interfaces.Enumerate())
            {
                ConnectionType connection = XusbConnectionClassifier.Classify(info.DevicePath);
                AppLogger.LogDebug($"[XInput] XUSB interface {info.DevicePath} vid={info.VendorId:X4} pid={info.ProductId:X4} playerIndex={info.PlayerIndex} connection={connection}");
                if (info.PlayerIndex < 0)
                {
                    // 接続の直後は、番号（LED）がまだ読めないことがある。次の列挙で見つかるまで、返さない
                    AppLogger.LogDebug($"[XInput] skipped (player index not readable yet): {info.DevicePath}");
                    continue;
                }

                var vidPid = new VidPidInfo(info.VendorId, info.ProductId, "Xbox", InputDeviceType.Xbox,
                    VidPidFeatureSet.DefaultDS4, checkConnection: transport => XusbConnectionClassifier.Classify(transport.DevicePath));
                result.Add(new InputTransportCandidate(new XInputTransport(api, interfaces, info), vidPid));
            }

            return result;
        }
    }
}
