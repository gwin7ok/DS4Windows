using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Utilities;

namespace DS4Windows.Core.Settings
{
    /// <summary>OSC・UDP・Steam フォルダーの設定の窓口。</summary>
    public partial class AppSettingsService
    {
        public bool UseUdpServer
        {
            get => _config.Network.useUDPServ;
            set => SetIfChanged(_config.Network.useUDPServ, v => _config.Network.useUDPServ = v, value);
        }

        public int UdpServerPort
        {
            get => _config.Network.udpServPort;
            set => SetIfChanged(_config.Network.udpServPort, v => _config.Network.udpServPort = v, value);
        }

        public string UdpServerListenAddress
        {
            get => _config.Network.udpServListenAddress;
            set
            {
                if (_config.Network.udpServListenAddress != value)
                {
                    _config.Network.udpServListenAddress = value.Trim();
                    NotifyChanged(nameof(UdpServerListenAddress));
                }
            }
        }

        public bool UseOscServer
        {
            get => _config.Network.useOSCServ;
            set => SetIfChanged(_config.Network.useOSCServ, v => _config.Network.useOSCServ = v, value);
        }

        public bool UseOscSender
        {
            get => _config.Network.useOSCSend;
            set => SetIfChanged(_config.Network.useOSCSend, v => _config.Network.useOSCSend = v, value);
        }

        public bool InterpretingOscMonitoring
        {
            get => _config.Network.interpretingOscMonitoring;
            set => SetIfChanged(_config.Network.interpretingOscMonitoring, v => _config.Network.interpretingOscMonitoring = v, value);
        }

        public int OscServerPort
        {
            get => _config.Network.oscServPort;
            set => SetIfChanged(_config.Network.oscServPort, v => _config.Network.oscServPort = v, value);
        }

        public string OscSenderAddress
        {
            get => _config.Network.oscSendAddress;
            set
            {
                if (_config.Network.oscSendAddress != value)
                {
                    _config.Network.oscSendAddress = value.Trim();
                    NotifyChanged(nameof(OscSenderAddress));
                }
            }
        }

        public int OscSenderPort
        {
            get => _config.Network.oscSendPort;
            set => SetIfChanged(_config.Network.oscSendPort, v => _config.Network.oscSendPort = v, value);
        }

        // ---- UDP サーバ平滑化パラメータ ----
        public double UDPServerSmoothingMincutoff
        {
            get => _config.Network.udpSmoothingMincutoff;
            set
            {
                if (_config.Network.udpSmoothingMincutoff != value)
                {
                    _config.Network.udpSmoothingMincutoff = value;
                    UDPServerSmoothingMincutoffChanged?.Invoke(this, EventArgs.Empty);
                    NotifyChanged(nameof(UDPServerSmoothingMincutoff));
                }
            }
        }

        public double UDPServerSmoothingBeta
        {
            get => _config.Network.udpSmoothingBeta;
            set
            {
                if (_config.Network.udpSmoothingBeta != value)
                {
                    _config.Network.udpSmoothingBeta = value;
                    UDPServerSmoothingBetaChanged?.Invoke(this, EventArgs.Empty);
                    NotifyChanged(nameof(UDPServerSmoothingBeta));
                }
            }
        }

        // ---- 入力処理ホットパスで参照する設定 ----
        // 毎レポート評価される経路から呼ばれるため、BackingStore の値を直接返し、キャッシュ・割り当て・ログを行わない。
        public bool UseUdpServerSmoothing
        {
            get => _config.Network.useUdpSmoothing;
            set => SetIfChanged(_config.Network.useUdpSmoothing, v => _config.Network.useUdpSmoothing = v, value);
        }

        // ---- SettingsViewModel の Global 直接参照解消（Global への薄い委譲。独自フィールドは持たない）----
        public bool UseCustomSteamFolder
        {
            get => _config.Network.useCustomSteamFolder;
            set => SetIfChanged(_config.Network.useCustomSteamFolder, v => _config.Network.useCustomSteamFolder = v, value);
        }

        public string? CustomSteamFolder
        {
            get => _config.Network.customSteamFolder;
            set => SetIfChanged(_config.Network.customSteamFolder, v => _config.Network.customSteamFolder = v, value);
        }
    }
}
