#nullable enable

using System;
using System.Threading.Tasks;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Actions;

namespace DS4Windows.Composition.Startup
{
    /// <summary>
    /// アプリの終了の手順（破棄の順序）。<c>App</c> の終了・サインアウト・未処理例外の経路が、同じこの手順を通る。
    /// 順序は、ドライバーの破棄の順序のガードレール（Phase5-Plan §5 の防壁 5）に従う: サービスの停止 → 設定の保存 →
    /// タイマーの解像度を戻す → 単一起動の停止 → IPC の破棄 → コントローラーのレジストリの破棄 → ホストの破棄 → ログのフラッシュ。
    /// ホストの構築より前の早期終了では、ホスト由来の協力者（<c>null</c>）の手順は何もしない。
    /// 順序は <c>AppShutdownSequenceTests</c> が、呼ばれた順の記録で固定する。
    /// </summary>
    public sealed class AppShutdownSequence
    {
        private readonly IRuntimeBootstrapper _bootstrapper;
        private readonly IServiceLifecycle _lifecycle;
        private readonly Action _saveSettings;
        private readonly IDisposable _ipc;
        private readonly IControllerRegistry _registry;
        private readonly int _controllerSlotCount;
        private readonly Action _resetTimerResolution;
        private readonly Action _stopSingleInstance;
        private readonly Action _disposeHost;
        private readonly Action _flushLogs;

        public AppShutdownSequence(
            IRuntimeBootstrapper bootstrapper, IServiceLifecycle lifecycle,
            Action saveSettings,
            IDisposable ipc, IControllerRegistry registry, int controllerSlotCount,
            Action resetTimerResolution, Action stopSingleInstance, Action disposeHost, Action flushLogs)
        {
            _bootstrapper = bootstrapper;
            _lifecycle = lifecycle;
            _saveSettings = saveSettings;
            _ipc = ipc;
            _registry = registry;
            _controllerSlotCount = controllerSlotCount;
            _resetTimerResolution = resetTimerResolution ?? throw new ArgumentNullException(nameof(resetTimerResolution));
            _stopSingleInstance = stopSingleInstance ?? throw new ArgumentNullException(nameof(stopSingleInstance));
            _disposeHost = disposeHost ?? throw new ArgumentNullException(nameof(disposeHost));
            _flushLogs = flushLogs ?? throw new ArgumentNullException(nameof(flushLogs));
        }

        public void Run()
        {
            if (_bootstrapper != null)
            {
                // 停止は別スレッドで行い、終わるまで待つ（呼び出し元は UI のスレッドのことがある）
                Task.Run(() =>
                {
                    if (_lifecycle != null && _lifecycle.Running)
                    {
                        _lifecycle.Stop(immediateUnplug: true);
                        _bootstrapper.Shutdown();
                    }
                }).Wait();
            }

            // 設定の保存が許されるのは、起動が完了したあと（保存を止める印が解除されたあと）だけ。許されないときは null が渡される
            _saveSettings?.Invoke();

            _resetTimerResolution();
            _stopSingleInstance();
            _ipc?.Dispose();

            // 登録されたコントローラーを破棄する。1 台の失敗で、残りの破棄を止めない
            try
            {
                if (_registry != null)
                {
                    for (int d = 0; d < _controllerSlotCount; d++)
                    {
                        try { _registry.ClearControllersForDevice(d); } catch { }
                    }
                }
            }
            catch { }

            // ホストの破棄は、コントローラーの停止と、レジストリの破棄のあと。ログのフラッシュより前に行い、破棄中のログも出力する。例外は握る
            try { _disposeHost(); } catch { }

            _flushLogs();
        }
    }
}
