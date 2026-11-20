using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;

namespace DS4Windows.UI.ViewModels
{
    /// <summary>OSC・UDP の設定の入れ物。</summary>
    public partial class SettingsViewModel
    {
        public bool UseOSCSender
        {
            get => _appSettings.UseOscSender;
            set
            {
                if (_appSettings.UseOscSender == value) return;
                _appSettings.UseOscSender = value;
                OnPropertyChanged(nameof(UseOSCSender));
            }
        }
        public int OscSendPort { get => _appSettings.OscSenderPort; set => _appSettings.OscSenderPort = value; }

        public bool UseUDPServer
        {
            get => _appSettings.UseUdpServer;
            set
            {
                if (_appSettings.UseUdpServer == value) return;
                _appSettings.UseUdpServer = value;
                OnPropertyChanged(nameof(UseUDPServer));
            }
        }
        public string UdpIpAddress
        {
            get => _appSettings.UdpServerListenAddress;
            set => _appSettings.UdpServerListenAddress = value;
        }
        public int UdpPort { get => _appSettings.UdpServerPort; set => _appSettings.UdpServerPort = value; }

        public bool UseUdpSmoothing
        {
            get => _appSettings.UseUdpServerSmoothing;
            set
            {
                bool temp = _appSettings.UseUdpServerSmoothing;
                if (temp == value) return;
                _appSettings.UseUdpServerSmoothing = value;
                OnPropertyChanged(nameof(UseUdpSmoothing));
            }
        }
        public bool UdpServerOneEuroPanelShown
        {
            get => _appSettings.UseUdpServer && _appSettings.UseUdpServerSmoothing;
        }
        public double UdpSmoothMinCutoff
        {
            get => _appSettings.UDPServerSmoothingMincutoff;
            set
            {
                double temp = _appSettings.UDPServerSmoothingMincutoff;
                if (temp == value) return;
                _appSettings.UDPServerSmoothingMincutoff = value;
                OnPropertyChanged(nameof(UdpSmoothMinCutoff));
            }
        }
        public double UdpSmoothBeta
        {
            get => _appSettings.UDPServerSmoothingBeta;
            set
            {
                double temp = _appSettings.UDPServerSmoothingBeta;
                if (temp == value) return;
                _appSettings.UDPServerSmoothingBeta = value;
                OnPropertyChanged(nameof(UdpSmoothBeta));
            }
        }
        public bool UseCustomSteamFolder
        {
            get => _appSettings.UseCustomSteamFolder;
            set
            {
                _appSettings.UseCustomSteamFolder = value;
                OnPropertyChanged(nameof(UseCustomSteamFolder));
            }
        }
        public string? CustomSteamFolder
        {
            get => _appSettings.CustomSteamFolder;
            set
            {
                // 存在しないフォルダーでも、入力を黙って捨てない。値は保存し、警告を出す。
                // 旧コードは、存在するフォルダーか空文字のときだけ保存し、それ以外は何も言わずに捨てていた
                // （TextBox には入力が残るため、保存されたように見えた）。使う側（AutoProfiles）は Directory.Exists で確かめ、
                // 無ければ既定の Steam フォルダーへ戻るため、存在しないパスを保存しても害はない。
                value ??= string.Empty;
                string? temp = _appSettings.CustomSteamFolder;
                if (temp == value) return;

                _appSettings.CustomSteamFolder = value;
                if (value != string.Empty && !Directory.Exists(value))
                {
                    AppLogger.Log(AppLogLevel.Warn, string.Format(Translations.Strings.CustomSteamFolderNotFound, value), LogOptions.UserFacing);
                }
            }
        }
    }
}
