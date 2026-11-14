using System;
using System.Threading;
using DS4Windows.Core.Logging;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services
{
    /// <summary>
    /// <see cref="IBackendFailureHandler"/> の実装（DI の Singleton）。<c>ControlService.OutputslotMan_ViGEmFailure</c> をそのまま移したもの。
    /// 失敗の通知は出力スロットのスレッドから来るため、停止の手順はイベントスレッドで行う（ガードレール 5.3）。
    /// </summary>
    public sealed class BackendFailureHandler : IBackendFailureHandler
    {
        private readonly IVirtualPadService _virtualPad;
        private readonly IEventThreadDispatcher _eventThread;
        private readonly IServiceLifecycle _lifecycle;

        public BackendFailureHandler(IVirtualPadService virtualPad, IEventThreadDispatcher eventThread, IServiceLifecycle lifecycle)
        {
            _virtualPad = virtualPad ?? throw new ArgumentNullException(nameof(virtualPad));
            _eventThread = eventThread ?? throw new ArgumentNullException(nameof(eventThread));
            _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
        }

        public void Attach()
        {
            _virtualPad.BackendFailure += OnBackendFailure;
        }

        private void OnBackendFailure(object sender, int errorCode)
        {
            _eventThread.Queue((Action)(() =>
            {
                _lifecycle.LoopControllers = false;
                while (_lifecycle.InServiceTask)
                    Thread.SpinWait(1000);

                AppLogger.Log(AppLogLevel.Warn, string.Format(DS4Windows.Translations.Strings.ViGEmPluginFailure, errorCode), LogOptions.UserFacing);
                _lifecycle.Stop();
            }));
        }
    }
}
