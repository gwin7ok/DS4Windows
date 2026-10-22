using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DS4Windows.DI;
using Sensorit.Base;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-E E-3（Phase9-E-Plan.md）: <see cref="IUdpMotionService"/> の実装（DI の Singleton）。
    /// 旧 <c>ControlService</c> の UDP モーション系（Cemuhook 互換サーバーの作成・開始・停止、モーションの送出のハンドラー <c>PrepareDevUDPMotion</c>、
    /// スムージングのフィルター、出力バッファー、状態 <c>_udpServer</c>／<c>udpChangeStatus</c>）を、そのまま移したもの。
    /// <c>ControlService</c> が持つもの（デバイスの状態、イベントスレッド、パッドの情報）は <see cref="IUdpMotionHost"/> 経由で受ける（循環を避けて遅延で取得）。
    /// 値の特性化は <c>Phase9EUdpMotionServiceTests</c> が守る。
    /// </summary>
    public sealed class UdpMotionService : IUdpMotionService
    {
        private readonly IAppSettingsService _appSettings;
        private readonly IDs4DeviceRegistry _deviceRegistry;
        private readonly IUdpMotionServerFactory _serverFactory;
        private readonly Func<IUdpMotionHost> _host;

        private OneEuroFilter3D[] udpEuroPairAccel = new OneEuroFilter3D[UdpServer.NUMBER_SLOTS]
        {
            new OneEuroFilter3D(), new OneEuroFilter3D(),
            new OneEuroFilter3D(), new OneEuroFilter3D(),
        };
        private OneEuroFilter3D[] udpEuroPairGyro = new OneEuroFilter3D[UdpServer.NUMBER_SLOTS]
        {
            new OneEuroFilter3D(), new OneEuroFilter3D(),
            new OneEuroFilter3D(), new OneEuroFilter3D(),
        };
        private byte[][] udpOutBuffers = new byte[UdpServer.NUMBER_SLOTS][]
        {
            new byte[UdpServer.DATA_RSP_PACKET_LEN], new byte[UdpServer.DATA_RSP_PACKET_LEN],
            new byte[UdpServer.DATA_RSP_PACKET_LEN], new byte[UdpServer.DATA_RSP_PACKET_LEN],
        };

        private IUdpMotionServer _udpServer;

        public event Action<string, bool> DebugMessage;

        public UdpMotionService(IAppSettingsService appSettings, IDs4DeviceRegistry deviceRegistry,
            IUdpMotionServerFactory serverFactory, Func<IUdpMotionHost> host)
        {
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _deviceRegistry = deviceRegistry ?? throw new ArgumentNullException(nameof(deviceRegistry));
            _serverFactory = serverFactory ?? throw new ArgumentNullException(nameof(serverFactory));
            _host = host ?? throw new ArgumentNullException(nameof(host));

            // 旧 ControlService のコンストラクターにあった購読
            _appSettings.UDPServerSmoothingMincutoffChanged += ChangeUdpSmoothingAttrs;
            _appSettings.UDPServerSmoothingBetaChanged += ChangeUdpSmoothingAttrs;
        }

        public int SlotCount => UdpServer.NUMBER_SLOTS;

        public bool HasServer => _udpServer != null;

        public bool IsChangingStatus => udpChangeStatus;

        // 旧 ControlService.LogDebug(string, bool)。購読者（ControlService）が Debug イベントへ流す
        private void LogDebug(string data, bool warning = false)
        {
            DebugMessage?.Invoke(data, warning);
        }

        public void ChangeStatus(bool state, bool openPort = true)
        {

            if (state && _udpServer == null)
            {
                udpChangeStatus = true;
                _host().QueueOnEventThread(() =>
                {
                    _udpServer = _serverFactory.Create(_host().GetPadDetailForIdx);
                    if (openPort)
                    {
                        // Change thread affinity of object to have normal priority
                        Task.Run(() =>
                        {
                            var UDP_SERVER_PORT = _appSettings.UdpServerPort;
                            var UDP_SERVER_LISTEN_ADDRESS = _appSettings.UdpServerListenAddress;

                            try
                            {
                                _udpServer.Start(UDP_SERVER_PORT, UDP_SERVER_LISTEN_ADDRESS);
                                LogDebug($"UDP server listening on address {UDP_SERVER_LISTEN_ADDRESS} port {UDP_SERVER_PORT}");
                            }
                            catch (System.Net.Sockets.SocketException ex)
                            {
                                var errMsg = String.Format("Couldn't start UDP server on address {0}:{1}, outside applications won't be able to access pad data ({2})", UDP_SERVER_LISTEN_ADDRESS, UDP_SERVER_PORT, ex.SocketErrorCode);

                                LogDebug(errMsg, true);
                                AppLogger.LogToTray(errMsg, true, true);
                            }
                        }).Wait();
                    }

                    udpChangeStatus = false;
                });
            }
            else if (!state && _udpServer != null)
            {
                _host().QueueOnEventThread(() =>
                {
                    udpChangeStatus = true;
                    _udpServer.Stop();
                    _udpServer = null;
                    AppLogger.LogToGui("Closed UDP server", false);
                    udpChangeStatus = false;

                    for (int i = 0; i < UdpServer.NUMBER_SLOTS; i++)
                    {
                        ResetSmoothingFilters(i);
                    }
                });
            }
        }

        /// <summary>サーバーが作られているとき、設定のポート・アドレスで待ち受けを始める（旧 <c>ControlService.Start</c> の中のブロック）。作られていなければ何もしない。</summary>
        public void StartListeningIfCreated()
        {
            if (_udpServer != null)
            {
                //var UDP_SERVER_PORT = 26760;
                var UDP_SERVER_PORT = _appSettings.UdpServerPort;
                var UDP_SERVER_LISTEN_ADDRESS = _appSettings.UdpServerListenAddress;

                try
                {
                    _udpServer.Start(UDP_SERVER_PORT, UDP_SERVER_LISTEN_ADDRESS);
                    LogDebug($"UDP server listening on address {UDP_SERVER_LISTEN_ADDRESS} port {UDP_SERVER_PORT}");
                }
                catch (System.Net.Sockets.SocketException ex)
                {
                    var errMsg = string.Format("Couldn't start UDP server on address {0}:{1}, outside applications won't be able to access pad data ({2})", UDP_SERVER_LISTEN_ADDRESS, UDP_SERVER_PORT, ex.SocketErrorCode);

                    LogDebug(errMsg, true);
                    AppLogger.LogToTray(errMsg, true, true);
                }
            }
        }

        public void ChangeMotionEventStatus(bool state)
        {
            IEnumerable<DS4Device> devices = _deviceRegistry.GetDS4Controllers();
            if (state)
            {
                int i = 0;
                foreach (DS4Device dev in devices)
                {
                    int tempIdx = i;
                    dev.queueEvent(() =>
                    {
                        if (i < UdpServer.NUMBER_SLOTS)
                        {
                            PrepareDeviceMotion(dev, tempIdx);
                        }
                    });

                    i++;
                }
            }
            else
            {
                foreach (DS4Device dev in devices)
                {
                    dev.queueEvent(() =>
                    {
                        if (dev.MotionEvent != null)
                        {
                            dev.Report -= dev.MotionEvent;
                            dev.MotionEvent = null;
                        }
                    });
                }
            }
        }

        private bool udpChangeStatus = false;
        /// <summary>ポートを開き直している最中か（旧: 公開フィールド <c>changingUDPPort</c>。<see cref="UsePort"/> が立てる）。</summary>
        public bool ChangingPort { get; private set; }

        // TODO(技術的負債・Phase9 9-F 付近): 旧 ControlService.UseUDPPort の移し先。呼び出し元は 0 件（UDP のポートを変えて開き直す処理の名残り。
        // 現在の画面は ChangeStatus(false)→ChangeStatus(true) で開き直している）。使う予定が無ければ、9-F で削除をユーザーに提案する（規約 2.4）。
        public async void UsePort()
        {
            ChangingPort = true;
            IEnumerable<DS4Device> devices = _deviceRegistry.GetDS4Controllers();
            foreach (DS4Device dev in devices)
            {
                dev.queueEvent(() =>
                {
                    if (dev.MotionEvent != null)
                    {
                        dev.Report -= dev.MotionEvent;
                    }
                });
            }

            await Task.Delay(100);

            var UDP_SERVER_PORT = _appSettings.UdpServerPort;
            var UDP_SERVER_LISTEN_ADDRESS = _appSettings.UdpServerListenAddress;

            try
            {
                _udpServer.Start(UDP_SERVER_PORT, UDP_SERVER_LISTEN_ADDRESS);
                foreach (DS4Device dev in devices)
                {
                    dev.queueEvent(() =>
                    {
                        if (dev.MotionEvent != null)
                        {
                            dev.Report += dev.MotionEvent;
                        }
                    });
                }
                LogDebug($"UDP server listening on address {UDP_SERVER_LISTEN_ADDRESS} port {UDP_SERVER_PORT}");
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                var errMsg = String.Format("Couldn't start UDP server on address {0}:{1}, outside applications won't be able to access pad data ({2})", UDP_SERVER_LISTEN_ADDRESS, UDP_SERVER_PORT, ex.SocketErrorCode);

                LogDebug(errMsg, true);
                AppLogger.LogToTray(errMsg, true, true);
            }

            ChangingPort = false;
        }

        public void PrepareDeviceMotion(DS4Device device, int index)
        {
            IUdpMotionHost host = _host();
            int tempIdx = index;
            DS4Device.ReportHandler<EventArgs> tempEvnt = (sender, args) =>
            {
                DualShockPadMeta padDetail = new DualShockPadMeta();
                host.GetPadDetailForIdx(tempIdx, ref padDetail);
                DS4State stateForUdp = host.GetTempState(tempIdx);

                host.GetCurrentState(tempIdx).CopyTo(stateForUdp);
                if (_appSettings.UseUdpServerSmoothing)
                {
                    if (stateForUdp.elapsedTime == 0)
                    {
                        // No timestamp was found. Exit out of routine
                        return;
                    }

                    double rate = 1.0 / stateForUdp.elapsedTime;
                    OneEuroFilter3D accelFilter = udpEuroPairAccel[tempIdx];
                    stateForUdp.Motion.accelXG = accelFilter.axis1Filter.Filter(stateForUdp.Motion.accelXG, rate);
                    stateForUdp.Motion.accelYG = accelFilter.axis2Filter.Filter(stateForUdp.Motion.accelYG, rate);
                    stateForUdp.Motion.accelZG = accelFilter.axis3Filter.Filter(stateForUdp.Motion.accelZG, rate);

                    OneEuroFilter3D gyroFilter = udpEuroPairGyro[tempIdx];
                    stateForUdp.Motion.angVelYaw = gyroFilter.axis1Filter.Filter(stateForUdp.Motion.angVelYaw, rate);
                    stateForUdp.Motion.angVelPitch = gyroFilter.axis2Filter.Filter(stateForUdp.Motion.angVelPitch, rate);
                    stateForUdp.Motion.angVelRoll = gyroFilter.axis3Filter.Filter(stateForUdp.Motion.angVelRoll, rate);
                }

                _udpServer?.NewReportIncoming(ref padDetail, stateForUdp, udpOutBuffers[tempIdx]);
            };

            device.MotionEvent = tempEvnt;
            device.Report += tempEvnt;
        }

        public void ResetSmoothingFilters(int idx)
        {
            if (idx < UdpServer.NUMBER_SLOTS)
            {
                OneEuroFilter3D temp = udpEuroPairAccel[idx] = new OneEuroFilter3D();
                temp.SetFilterAttrs(_appSettings.UDPServerSmoothingMincutoff, _appSettings.UDPServerSmoothingBeta);

                temp = udpEuroPairGyro[idx] = new OneEuroFilter3D();
                temp.SetFilterAttrs(_appSettings.UDPServerSmoothingMincutoff, _appSettings.UDPServerSmoothingBeta);
            }
        }

        private void ChangeUdpSmoothingAttrs(object sender, EventArgs e)
        {
            for (int i = 0; i < udpEuroPairAccel.Length; i++)
            {
                OneEuroFilter3D temp = udpEuroPairAccel[i];
                temp.SetFilterAttrs(_appSettings.UDPServerSmoothingMincutoff, _appSettings.UDPServerSmoothingBeta);
            }

            for (int i = 0; i < udpEuroPairGyro.Length; i++)
            {
                OneEuroFilter3D temp = udpEuroPairGyro[i];
                temp.SetFilterAttrs(_appSettings.UDPServerSmoothingMincutoff, _appSettings.UDPServerSmoothingBeta);
            }
        }
    }
}
