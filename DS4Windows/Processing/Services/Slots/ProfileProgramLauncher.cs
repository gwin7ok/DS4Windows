using System;
using System.Threading.Tasks;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Actions;

namespace DS4Windows.Processing.Services.Slots
{
    /// <summary>
    /// <see cref="IProfileProgramLauncher"/> の実装（DI の Singleton。状態なし）。
    /// 稼働の確認は <see cref="IProcessInspector"/>、起動は <see cref="IProcessLauncher"/>、待ちは <see cref="TimeProvider"/> を通す。
    /// 待ちの間にアプリが終わる場合に備え、待ちと起動はスレッドプールで行う（呼び出し元を止めない）。
    /// </summary>
    public sealed class ProfileProgramLauncher : IProfileProgramLauncher
    {
        /// <summary>稼働の確認から起動までの待ち。</summary>
        public static readonly TimeSpan LaunchDelay = TimeSpan.FromSeconds(5);

        private readonly IProfileSettingsService _profileSettings;
        private readonly IProcessInspector _inspector;
        private readonly IProcessLauncher _launcher;
        private readonly TimeProvider _time;

        public ProfileProgramLauncher(IProfileSettingsService profileSettings, IProcessInspector inspector, IProcessLauncher launcher, TimeProvider time)
        {
            ArgumentNullException.ThrowIfNull(profileSettings);
            _profileSettings = profileSettings;
            ArgumentNullException.ThrowIfNull(inspector);
            _inspector = inspector;
            ArgumentNullException.ThrowIfNull(launcher);
            _launcher = launcher;
            ArgumentNullException.ThrowIfNull(time);
            _time = time;
        }

        public void LaunchIfNotRunning(int slot)
        {
            if (slot < 0 || slot >= ControllerLimits.TestProfileItemCount) return;

            string? programPath = _profileSettings.BasicSlot(slot)?.LaunchProgram;
            if (string.IsNullOrEmpty(programPath)) return;

            bool running;
            try { running = _inspector.IsProcessRunning(programPath); }
            catch { running = false; }
            if (running) return;

            Task.Run(async () =>
            {
                await Task.Delay(LaunchDelay, _time).ConfigureAwait(false);
                try { _launcher.LaunchWithWorkingDirectory(programPath); }
                catch (Exception caught) { /* 失敗しても処理を続ける（呼び出し元に影響させない） */ if (AppLogger.IsDebugEnabled) AppLogger.LogDebug($"[ProfileProgramLauncher:51] ignored {caught.GetType().Name}: {caught.Message}"); }
            });
        }
    }
}
