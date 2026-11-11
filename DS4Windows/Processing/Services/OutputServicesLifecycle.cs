using System;
using System.Threading;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Output;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services
{
    /// <summary>
    /// <see cref="IOutputServicesLifecycle"/> の実装（DI の Singleton）。<see cref="ServiceLifecycle"/> の <c>Start</c>／<c>Stop</c> にあった、
    /// 出力側のサービスの開始・停止の手順を、そのまま（順序・待ち時間・ログの文言を変えずに）移したもの。
    /// 安全弁（ドライバーの開始・破棄の順序、<see cref="Thread.Sleep(int)"/> の待ち）は、<c>LifecycleGuardTests</c> がソースの順序で固定する。
    /// </summary>
    public sealed class OutputServicesLifecycle : IOutputServicesLifecycle
    {
        private readonly IAppSettingsService _appSettings;
        private readonly IEnvironmentService _environmentService;
        private readonly IVirtualPadService _virtualPad;
        private readonly IOutputKbmInitializer _kbmInitializer;
        private readonly IVirtualKBM _virtualKBM;
        private readonly IOscService _oscService;
        private readonly IUdpMotionService _udpMotion;
        private readonly IHidHideService _hidHideService;
        private readonly IXboxParentHider _xboxParentHider;

        public OutputServicesLifecycle(IAppSettingsService appSettings, IEnvironmentService environmentService, IVirtualPadService virtualPad,
            IOutputKbmInitializer kbmInitializer, IVirtualKBM virtualKBM, IOscService oscService, IUdpMotionService udpMotion, IHidHideService hidHideService,
            IXboxParentHider xboxParentHider)
        {
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            _virtualPad = virtualPad ?? throw new ArgumentNullException(nameof(virtualPad));
            _kbmInitializer = kbmInitializer ?? throw new ArgumentNullException(nameof(kbmInitializer));
            _virtualKBM = virtualKBM ?? throw new ArgumentNullException(nameof(virtualKBM));
            _oscService = oscService ?? throw new ArgumentNullException(nameof(oscService));
            _udpMotion = udpMotion ?? throw new ArgumentNullException(nameof(udpMotion));
            _hidHideService = hidHideService ?? throw new ArgumentNullException(nameof(hidHideService));
            _xboxParentHider = xboxParentHider ?? throw new ArgumentNullException(nameof(xboxParentHider));
        }

        public bool StartVirtualOutputs(bool showlog)
        {
            _virtualPad.StartViGEm();
            if (_virtualPad.HasViGEmClient)
            {
                // Initialize output KBM handler at start of ControlService
                _kbmInitializer.Initialize();

                if (showlog)
                    AppLogger.Log(AppLogLevel.Info, DS4Windows.Properties.Resources.Starting, LogOptions.UserFacing);

                Thread.Sleep(2000);

                bool runningAsAdmin = _environmentService.IsAdministrator();
                if (_virtualKBM.GetIdentifier() != VirtualKbmHandlerIds.FakerInput && !runningAsAdmin)
                {
                    string helpURL = @"https://ryochan7.github.io/ds4windows-site/troubleshooting/kb-mouse-issues/#windows-not-responding-to-ds4ws-kb-m-commands-in-some-situations";
                    AppLogger.LogUser(AppLogLevel.Info, "Log_Svc_UacConflict", helpURL);
                }

                AppLogger.LogUser(AppLogLevel.Info, "Log_Svc_KbmHandler", _virtualKBM.GetFullDisplayName());
                AppLogger.LogUser(AppLogLevel.Info, "Log_Svc_VigemConnected", _environmentService.ViGEmBusVersion);
                return true;
            }

            string logKey = "Log_Svc_VigemConnectFailed";
            object[] logArgs = null;
            if (!_environmentService.IsViGEmBusInstalled())
            {
                logKey = "Log_Svc_VigemNotInstalled";
            }
            else if (!_environmentService.IsRunningSupportedViGEmBus())
            {
                logKey = "Log_Svc_VigemUnsupported";
                logArgs = new object[] { _environmentService.ViGEmBusVersion };
            }
            else
            {
                logKey = "Log_Svc_VigemConnectFailed";
            }

            AppLogger.LogUser(AppLogLevel.Info, logKey, logArgs);
            AppLogger.LogToTray(AppLogger.UserText(logKey, logArgs));
            return false;
        }

        public void UpdateHidHide()
        {
            // 前回の異常終了で、Xbox の USB の親を隠したまま残っていれば戻す（接続のときに、設定に従って、改めて隠す）
            _xboxParentHider.RecoverStale();
            _hidHideService.UpdateAttributes();
        }

        public void StartNetworkOutputs()
        {
            if (_appSettings.UseOscServer && !_oscService.HasListener)
            {
                _oscService.ChangeListenerStatus(true);
            }

            if (_appSettings.UseOscSender && !_oscService.HasSender)
            {
                _oscService.ChangeSenderStatus(true);
            }

            if (_appSettings.UseUdpServer && !_udpMotion.HasServer)
            {
                _udpMotion.ChangeStatus(true, false);
                while (_udpMotion.IsChangingStatus)
                {
                    Thread.SpinWait(500);
                }
            }
        }

        public void StartUdpListening()
        {
            _udpMotion.StartListeningIfCreated();   // 旧: _udpServer != null のときの待ち受けの開始
        }

        public void ResetDeviceState(int index)
        {
            _oscService.ResetDeviceState(index);
        }

        public void StopNetworkOutputs()
        {
            if (_oscService.HasListener)
            {
                _oscService.ChangeListenerStatus(false);
            }

            if (_oscService.HasSender)
            {
                _oscService.ChangeSenderStatus(false);
            }

            if (_udpMotion.HasServer)
            {
                _udpMotion.ChangeStatus(false, true);
            }
        }

        public void StopVirtualOutputs(bool anyUnplugged)
        {
            while (_virtualPad.IsOutputQueueRunning)
            {
                Thread.SpinWait(500);
            }
            _virtualPad.StopOutputSlots(immediate: true);

            if (anyUnplugged)
            {
                Thread.Sleep(OutputSlotManager.DELAY_TIME);
            }

            _virtualPad.StopViGEm();

            // Disconnect from KBM system when stopping ControlService
            AppLogger.LogUser(AppLogLevel.Info, "Log_Svc_KbmClosing", _virtualKBM.GetDisplayName());
            _virtualKBM.Disconnect();

            // 停止のとき、自分が隠した Xbox の USB の親を戻す
            _xboxParentHider.RestoreAll();
        }
    }
}
