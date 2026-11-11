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
            foreach (XusbInterfaceInfo found in interfaces.Enumerate())
            {
                XusbInterfaceInfo info = found;
                ConnectionType connection = XusbConnectionClassifier.Classify(info.DevicePath);
                AppLogger.LogDebug($"[XInput] XUSB interface {info.DevicePath} vid={info.VendorId:X4} pid={info.ProductId:X4} playerIndex={info.PlayerIndex} connection={connection}");
                if (info.PlayerIndex < 0)
                {
                    // Bluetooth の Xbox は、XInput のクライアントが一度状態を読むまで、番号（LED）が割り当てられない。
                    // HidHide で隠していると、他のアプリが読まないため、いつまでも割り当たらない。自分で読んで、割り当てを促す
                    WakeXInput();
                    int assigned = interfaces.ReadPlayerIndex(info.DevicePath);
                    AppLogger.LogDebug($"[XInput] player index after waking XInput: {assigned} ({info.DevicePath})");
                    if (assigned < 0)
                    {
                        // 割り当てが間に合わないこともある。次の列挙で見つかるまで、返さない
                        AppLogger.LogDebug($"[XInput] skipped (player index not readable yet): {info.DevicePath}");
                        continue;
                    }

                    info = new XusbInterfaceInfo(info.DevicePath, info.InstanceId, info.VendorId, info.ProductId, assigned);
                }

                var vidPid = new VidPidInfo(info.VendorId, info.ProductId, "Xbox", InputDeviceType.Xbox,
                    VidPidFeatureSet.DefaultDS4, checkConnection: transport => XusbConnectionClassifier.Classify(transport.DevicePath));
                result.Add(new InputTransportCandidate(new XInputTransport(api, interfaces, info), vidPid));
            }

            return result;
        }

        // 4 つの番号すべてを一度読む（どの番号に割り当たるかは分からないため）。読んだ結果は使わない
        private void WakeXInput()
        {
            for (int index = 0; index < 4; index++)
            {
                api.TryGetState(index, out _);
            }
        }
    }
}
