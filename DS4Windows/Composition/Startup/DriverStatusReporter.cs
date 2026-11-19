#nullable enable

using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;

namespace DS4Windows.Composition.Startup
{
    /// <summary>
    /// 起動時に、HidHide・ViGEmBus・FakerInput の導入状態を確認して保存し（<see cref="IEnvironmentService"/> の保存値。
    /// 設定タブの「Xbox コントローラーを隠す」の赤文字などが参照する）、結果をログ（ファイルと画面）へ出す。
    /// </summary>
    public sealed class DriverStatusReporter
    {
        private readonly IEnvironmentService _environment;

        public DriverStatusReporter(IEnvironmentService environment)
        {
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public void RefreshAndLog()
        {
            _environment.RefreshHidHideInfo();
            _environment.RefreshViGEmBusInfo();
            _environment.RefreshFakerInputInfo();

            if (_environment.HidHideInstalled)
            {
                AppLogger.LogUser(AppLogLevel.Info, "Log_Driver_HidHideInstalled", _environment.HidHideProductVersion, _environment.HidHideVersion);
            }
            else
            {
                AppLogger.LogUser(AppLogLevel.Info, "Log_Driver_HidHideNotInstalled");
            }

            if (_environment.IsViGEmBusInstalled())
            {
                AppLogger.LogUser(AppLogLevel.Info, "Log_Driver_ViGEmBusInstalled", _environment.ViGEmBusVersion);
            }
            else
            {
                AppLogger.LogUser(AppLogLevel.Info, "Log_Driver_ViGEmBusNotInstalled");
            }

            if (_environment.IsFakerInputInstalled())
            {
                AppLogger.LogUser(AppLogLevel.Info, "Log_Driver_FakerInputInstalled", _environment.FakerInputVersion);
            }
            else
            {
                AppLogger.LogUser(AppLogLevel.Info, "Log_Driver_FakerInputNotInstalled");
            }
        }
    }
}
