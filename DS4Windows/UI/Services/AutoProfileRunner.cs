#nullable enable

using System;
using System.Threading.Tasks;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Profiles;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// 自動プロファイルの監視の運転（1 秒ごとの照合のタイマー、接続直後の照合し直し、通知の判定）。
    /// メイン画面は業務のサービス（<see cref="IAutoProfileService"/>・<see cref="IControllerSlotQuery"/>）を直接触らず、この部品を通す。
    /// タイマーは例外が出ても、監視が有効なら始め直す（止まったままだと、自動プロファイルが以後動かなくなるため）。
    /// </summary>
    public sealed class AutoProfileRunner
    {
        private readonly IAutoProfileService autoProfileService;
        private readonly IControllerSlotQuery slots;
        private readonly System.Timers.Timer timer;

        public AutoProfileRunner(IAutoProfileService autoProfileService, IControllerSlotQuery slots)
        {
            ArgumentNullException.ThrowIfNull(autoProfileService);
            this.autoProfileService = autoProfileService;
            ArgumentNullException.ThrowIfNull(slots);
            this.slots = slots;
            timer = new System.Timers.Timer { Interval = 1000, AutoReset = false };
        }

        /// <summary>監視に使う、唯一の保持の入れ物（画面と共有する）。</summary>
        public AutoProfileHolder Holder => autoProfileService.Holder;

        public bool Running => autoProfileService.Running;

        public int DebugLogLevel
        {
            set => autoProfileService.AutoProfileDebugLogLevel = value;
        }

        /// <summary>サービスが、サービスの切り替えを求めたとき。</summary>
        public event Action<bool> RequestServiceChange
        {
            add => autoProfileService.RequestServiceChange += value;
            remove => autoProfileService.RequestServiceChange -= value;
        }

        public void EnsureFile(string path)
        {
            if (!System.IO.File.Exists(path))
                autoProfileService.CreateAutoProfilesFile(path);
        }

        /// <summary>接続中のスロットにだけ適用するため、接続した直後に、最後の前景ウィンドウで照合し直す配線をする。</summary>
        public void AttachHotplugRecheck() => slots.SlotChanged += OnSlotChanged;

        /// <summary>規則の件数に合わせて、監視を始める／止める。</summary>
        public void UpdateStatus(int ruleCount)
        {
            bool enabled = autoProfileService.Running;
            if (ruleCount > 0 && !enabled) SetEnabled(true);
            else if (ruleCount == 0 && enabled) SetEnabled(false);
        }

        public void SetEnabled(bool state)
        {
            if (state)
            {
                timer.Elapsed += OnTimerElapsed;
                timer.Start();
                autoProfileService.Running = true;
            }
            else
            {
                timer.Stop();
                timer.Elapsed -= OnTimerElapsed;
                autoProfileService.Running = false;
            }
        }

        public void Stop() => timer.Stop();

        /// <summary>自動プロファイル由来の切り替えの通知を出すか（通知の選択と、接続の有無で決まる）。</summary>
        public bool ShouldNotify(ProfileChangeSource source, bool connected)
            => AutoProfileNotificationPolicy.ShouldNotify(source, autoProfileService.AutoProfileSwitchNotifyChoice, connected);

        /// <summary>スロットの電池の残量（スロットが空なら "N/A"）。</summary>
        public string BatteryText(int deviceIndex)
        {
            IControllerSlotView device = slots.GetSlot(deviceIndex);
            return device != null ? $"{device.Battery}" : "N/A";
        }

        private void OnSlotChanged(object sender, ControllerSlotChangedEventArgs args)
        {
            if (!autoProfileService.Running)
                return;

            // 接続処理（サービスのスレッド）を待たせず、別スレッドで照合する
            Task.Run(() =>
            {
                try { autoProfileService.CheckProfilesForConnectedController(); }
                catch (Exception ex) { AppLogger.LogError($"[DI] CheckProfilesForConnectedController failed: {ex.Message}"); }
            });
        }

        private void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer.Stop();
            GuardedTimerTick.Run(
                () => autoProfileService.CheckProfiles(),
                () => autoProfileService.Running,
                () => timer.Start(),
                ex => AppLogger.LogError($"AutoProfilesTimer tick failed: {ex}"));
        }
    }
}
