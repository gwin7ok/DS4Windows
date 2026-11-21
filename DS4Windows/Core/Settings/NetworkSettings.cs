namespace DS4Windows.Core.Settings
{
    /// <summary>
    /// OSC・UDP・Steam フォルダーの設定。<see cref="BackingStore"/> が 1 つ持つ（設定ファイルへ保存される値）。値の読み書きは、<c>AppSettingsService</c> が公開する。
    /// </summary>
    public class NetworkSettings
    {
        public bool useOSCServ { get; set; } = false;
        public int oscServPort { get; set; } = BackingStore.DEFAULT_OSC_SERV_PORT;
        public bool interpretingOscMonitoring { get; set; } = false;
        public bool useOSCSend { get; set; } = false;
        public int oscSendPort { get; set; } = BackingStore.DEFAULT_OSC_SEND_PORT;
        public string oscSendAddress { get; set; } = BackingStore.DEFAULT_OSC_SEND_ADDRESS;
        public bool useUDPServ { get; set; } = false;
        public int udpServPort { get; set; } = BackingStore.DEFAULT_UDP_SERV_PORT;
        public string udpServListenAddress { get; set; } = BackingStore.DEFAULT_UDP_SERV_LISTEN_ADDR;
        public bool useUdpSmoothing { get; set; }
        public double udpSmoothingMincutoff { get; set; } = BackingStore.DEFAULT_UDP_SMOOTH_MINCUTOFF;
        public double udpSmoothingBeta { get; set; } = BackingStore.DEFAULT_UDP_SMOOTH_BETA;
        public bool useCustomSteamFolder { get; set; }
        public string? customSteamFolder { get; set; }
    }
}
