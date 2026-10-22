using System;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-E E-5（Phase9-E5-Plan.md）: <see cref="ILatencyMonitor"/> の実装（DI の Singleton）。
    /// 旧 <c>ControlService.On_Report</c> の先頭（エラーのログと遅延の監視）と <c>LagFlashWarning</c> を、そのまま移したもの。
    /// 値の特性化は <c>Phase9EInputReportPartsTests</c>（旧実装との差分テスト）が守る。
    /// </summary>
    public sealed class LatencyMonitor : ILatencyMonitor
    {
        private readonly IAppSettingsService _appSettings;
        private readonly IProfileSettingsService _profileSettings;

        private readonly bool[] _lag = new bool[ControllerLimits.MaxControllerCount];
        private readonly bool[] _inWarnMonitor = new bool[ControllerLimits.MaxControllerCount];
        private readonly string[] _tempStrings = new string[ControllerLimits.MaxControllerCount];

        public event Action<string, bool> DebugMessage;

        public LatencyMonitor(IAppSettingsService appSettings, IProfileSettingsService profileSettings)
        {
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            for (int i = 0; i < _tempStrings.Length; i++)
            {
                _tempStrings[i] = string.Empty;
            }
        }

        public void OnReport(DS4Device device, int ind)
        {
            string devError = _tempStrings[ind] = device.error;
            if (!string.IsNullOrEmpty(devError))
            {
                LogDebug(devError);
            }

            if (_inWarnMonitor[ind])
            {
                int flashWhenLateAt = _appSettings.FlashWhenLateAt;
                if (!_lag[ind] && device.Latency >= flashWhenLateAt)
                {
                    _lag[ind] = true;
                    LagFlashWarning(device, ind, true);
                }
                else if (_lag[ind] && device.Latency < flashWhenLateAt)
                {
                    _lag[ind] = false;
                    LagFlashWarning(device, ind, false);
                }
            }
            else
            {
                if (DateTime.UtcNow - device.firstActive > TimeSpan.FromSeconds(5))
                {
                    _inWarnMonitor[ind] = true;
                }
            }
        }

        public void Reset(int ind)
        {
            _lag[ind] = false;
            _inWarnMonitor[ind] = false;
        }

        private void LagFlashWarning(DS4Device device, int ind, bool on)
        {
            if (on)
            {
                _lag[ind] = true;
                LogDebug(string.Format(DS4WinWPF.Properties.Resources.LatencyOverTen, (ind + 1), device.Latency), true);
                if (_appSettings.FlashWhenLate)
                {
                    DS4Color color = new DS4Color { red = 50, green = 0, blue = 0 };
                    DS4LightBar.forcedColor[ind] = color;
                    DS4LightBar.forcedFlash[ind] = 2;
                    DS4LightBar.forcelight[ind] = true;
                }
            }
            else
            {
                _lag[ind] = false;
                LogDebug(DS4WinWPF.Properties.Resources.LatencyNotOverTen.Replace("*number*", (ind + 1).ToString()));
                DS4LightBar.forcelight[ind] = false;
                DS4LightBar.forcedFlash[ind] = 0;
                device.LightBarColor = _profileSettings.GetMainColor(ind);
            }
        }

        private void LogDebug(string data, bool warning = false)
        {
            DebugMessage?.Invoke(data, warning);
        }
    }
}
