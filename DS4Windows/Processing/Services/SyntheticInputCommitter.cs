using System;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 MapCustom の分割 M1（Phase9-MapCustom-Plan.md §3）: <see cref="ISyntheticInputCommitter"/> の実装（DI の Singleton）。
    /// 旧 <c>Mapping.Commit</c>（合成入力のクリック・ホイールの確定と出力）を、そのまま移したもの
    /// （旧 <c>oldnow</c>／<c>wheel</c> も、ここが持つ。<b>全デバイスで共有</b>のまま。<c>GlobalState</c> が全デバイスの合計で遷移を検出するため、
    /// 繰り返しの状態だけをデバイスごとにすると、押した側と離した側が別のデバイスのとき、繰り返しが止まらなくなる）。
    /// 常に <c>false</c> だった <c>pressagain</c>（上流 DS4Windows 由来の残骸）は削除した。
    /// 値の特性化は <c>Phase7Step7CommitCharacterizationTests</c> と <c>Phase9MapCustomCommitterDifferentialTests</c>（旧実装との差分テスト）が守る。
    /// </summary>
    public sealed class SyntheticInputCommitter : ISyntheticInputCommitter
    {
        private readonly SyntheticInputState _syntheticInput;
        private readonly IVirtualKBM _virtualKbm;
        private readonly Func<DateTime> _utcNow;

        // ホイールの繰り返しの基準時刻と、押されているホイールの向き（全デバイス共有。旧 Mapping.oldnow／wheel）
        private DateTime _oldnow = DateTime.UtcNow;
        private int _wheel = 0;

        public SyntheticInputCommitter(SyntheticInputState syntheticInput, IVirtualKBM virtualKbm, Func<DateTime> utcNow = null)
        {
            _syntheticInput = syntheticInput ?? throw new ArgumentNullException(nameof(syntheticInput));
            _virtualKbm = virtualKbm ?? throw new ArgumentNullException(nameof(virtualKbm));
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        /// <summary>ホイールの繰り返しの基準時刻を設定する（テスト用。旧 <c>Mapping.oldnow</c> の代入）。</summary>
        public void SetWheelRepeatBase(DateTime value)
        {
            _oldnow = value;
        }

        public void Commit(int device, IProfileSettingsService settings)
        {
            SyntheticState state = _syntheticInput.DeviceState[device];
            _syntheticInput.SyncStateLock.EnterWriteLock();

            _syntheticInput.GlobalState.currentClicks.leftCount += state.currentClicks.leftCount - state.previousClicks.leftCount;
            _syntheticInput.GlobalState.currentClicks.middleCount += state.currentClicks.middleCount - state.previousClicks.middleCount;
            _syntheticInput.GlobalState.currentClicks.rightCount += state.currentClicks.rightCount - state.previousClicks.rightCount;
            _syntheticInput.GlobalState.currentClicks.fourthCount += state.currentClicks.fourthCount - state.previousClicks.fourthCount;
            _syntheticInput.GlobalState.currentClicks.fifthCount += state.currentClicks.fifthCount - state.previousClicks.fifthCount;
            _syntheticInput.GlobalState.currentClicks.wUpCount += state.currentClicks.wUpCount - state.previousClicks.wUpCount;
            _syntheticInput.GlobalState.currentClicks.wDownCount += state.currentClicks.wDownCount - state.previousClicks.wDownCount;
            _syntheticInput.GlobalState.currentClicks.toggleCount += state.currentClicks.toggleCount - state.previousClicks.toggleCount;
            _syntheticInput.GlobalState.currentClicks.toggle = state.currentClicks.toggle;

            if (_syntheticInput.GlobalState.currentClicks.toggleCount != 0 && _syntheticInput.GlobalState.previousClicks.toggleCount == 0 && _syntheticInput.GlobalState.currentClicks.toggle)
            {
                AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseLeftDown");
                AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseLeftDown");
                // Phase7-Step8 W2（Q8＝A）: 旧 MouseOutputAction の検索（未登録のため常に null で、下の出力が実際の経路だった）を削除した。
                _virtualKbm.PerformMouseButtonEvent(settings.OutputKBMMapping.MOUSEEVENTF_LEFTDOWN);

                if (_syntheticInput.GlobalState.currentClicks.rightCount != 0 && _syntheticInput.GlobalState.previousClicks.rightCount == 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseRightDown");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseRightDown");
                    _virtualKbm.PerformMouseButtonEvent(settings.OutputKBMMapping.MOUSEEVENTF_RIGHTDOWN);
                }
                if (_syntheticInput.GlobalState.currentClicks.middleCount != 0 && _syntheticInput.GlobalState.previousClicks.middleCount == 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseMiddleDown");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseMiddleDown");
                    _virtualKbm.PerformMouseButtonEvent(settings.OutputKBMMapping.MOUSEEVENTF_MIDDLEDOWN);
                }
                if (_syntheticInput.GlobalState.currentClicks.fourthCount != 0 && _syntheticInput.GlobalState.previousClicks.fourthCount == 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseXButtonDown btn=1");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseXButtonDown btn=1");
                    _virtualKbm.PerformMouseButtonEventAlt(settings.OutputKBMMapping.MOUSEEVENTF_XBUTTONDOWN, 1);
                }
                if (_syntheticInput.GlobalState.currentClicks.fifthCount != 0 && _syntheticInput.GlobalState.previousClicks.fifthCount == 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseXButtonDown btn=2");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseXButtonDown btn=2");
                    _virtualKbm.PerformMouseButtonEventAlt(settings.OutputKBMMapping.MOUSEEVENTF_XBUTTONDOWN, 2);
                }
            }
            else if (_syntheticInput.GlobalState.currentClicks.toggleCount != 0 && _syntheticInput.GlobalState.previousClicks.toggleCount == 0 && !_syntheticInput.GlobalState.currentClicks.toggle)
            {
                if (_syntheticInput.GlobalState.currentClicks.leftCount != 0 && _syntheticInput.GlobalState.previousClicks.leftCount == 0)
                    _virtualKbm.PerformMouseButtonEvent(settings.OutputKBMMapping.MOUSEEVENTF_LEFTUP);
                if (_syntheticInput.GlobalState.currentClicks.rightCount != 0 && _syntheticInput.GlobalState.previousClicks.rightCount == 0)
                    _virtualKbm.PerformMouseButtonEvent(settings.OutputKBMMapping.MOUSEEVENTF_RIGHTUP);
                if (_syntheticInput.GlobalState.currentClicks.middleCount != 0 && _syntheticInput.GlobalState.previousClicks.middleCount == 0)
                    _virtualKbm.PerformMouseButtonEvent(settings.OutputKBMMapping.MOUSEEVENTF_MIDDLEUP);
                if (_syntheticInput.GlobalState.currentClicks.fourthCount != 0 && _syntheticInput.GlobalState.previousClicks.fourthCount == 0)
                    _virtualKbm.PerformMouseButtonEventAlt(settings.OutputKBMMapping.MOUSEEVENTF_XBUTTONUP, 1);
                if (_syntheticInput.GlobalState.currentClicks.fifthCount != 0 && _syntheticInput.GlobalState.previousClicks.fifthCount == 0)
                    _virtualKbm.PerformMouseButtonEventAlt(settings.OutputKBMMapping.MOUSEEVENTF_XBUTTONUP, 2);
            }

            if (_syntheticInput.GlobalState.currentClicks.toggleCount == 0 && _syntheticInput.GlobalState.previousClicks.toggleCount == 0)
            {
                if (_syntheticInput.GlobalState.currentClicks.leftCount != 0 && _syntheticInput.GlobalState.previousClicks.leftCount == 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseLeftDown");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseLeftDown");
                    // Phase7-Step8 W2（Q8＝A）: 旧 MouseOutputAction の検索（未登録のため常に null で、下の出力が実際の経路だった）を削除した。
                    _virtualKbm.PerformMouseButtonEvent(settings.OutputKBMMapping.MOUSEEVENTF_LEFTDOWN);
                }
                else if (_syntheticInput.GlobalState.currentClicks.leftCount == 0 && _syntheticInput.GlobalState.previousClicks.leftCount != 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseLeftUp");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseLeftUp");
                    _virtualKbm.PerformMouseButtonEvent(settings.OutputKBMMapping.MOUSEEVENTF_LEFTUP);
                }

                if (_syntheticInput.GlobalState.currentClicks.middleCount != 0 && _syntheticInput.GlobalState.previousClicks.middleCount == 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseMiddleDown");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseMiddleDown");
                    _virtualKbm.PerformMouseButtonEvent(settings.OutputKBMMapping.MOUSEEVENTF_MIDDLEDOWN);
                }
                else if (_syntheticInput.GlobalState.currentClicks.middleCount == 0 && _syntheticInput.GlobalState.previousClicks.middleCount != 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseMiddleUp");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseMiddleUp");
                    _virtualKbm.PerformMouseButtonEvent(settings.OutputKBMMapping.MOUSEEVENTF_MIDDLEUP);
                }

                if (_syntheticInput.GlobalState.currentClicks.rightCount != 0 && _syntheticInput.GlobalState.previousClicks.rightCount == 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseRightDown");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseRightDown");
                    _virtualKbm.PerformMouseButtonEvent(settings.OutputKBMMapping.MOUSEEVENTF_RIGHTDOWN);
                }
                else if (_syntheticInput.GlobalState.currentClicks.rightCount == 0 && _syntheticInput.GlobalState.previousClicks.rightCount != 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseRightUp");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseRightUp");
                    _virtualKbm.PerformMouseButtonEvent(settings.OutputKBMMapping.MOUSEEVENTF_RIGHTUP);
                }

                if (_syntheticInput.GlobalState.currentClicks.fourthCount != 0 && _syntheticInput.GlobalState.previousClicks.fourthCount == 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseXButtonDown btn=1");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseXButtonDown btn=1");
                    _virtualKbm.PerformMouseButtonEventAlt(settings.OutputKBMMapping.MOUSEEVENTF_XBUTTONDOWN, 1);
                }
                else if (_syntheticInput.GlobalState.currentClicks.fourthCount == 0 && _syntheticInput.GlobalState.previousClicks.fourthCount != 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseXButtonUp btn=1");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseXButtonUp btn=1");
                    _virtualKbm.PerformMouseButtonEventAlt(settings.OutputKBMMapping.MOUSEEVENTF_XBUTTONUP, 1);
                }

                if (_syntheticInput.GlobalState.currentClicks.fifthCount != 0 && _syntheticInput.GlobalState.previousClicks.fifthCount == 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseXButtonDown btn=2");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseXButtonDown btn=2");
                    _virtualKbm.PerformMouseButtonEventAlt(settings.OutputKBMMapping.MOUSEEVENTF_XBUTTONDOWN, 2);
                }
                else if (_syntheticInput.GlobalState.currentClicks.fifthCount == 0 && _syntheticInput.GlobalState.previousClicks.fifthCount != 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseXButtonUp btn=2");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseXButtonUp btn=2");
                    _virtualKbm.PerformMouseButtonEventAlt(settings.OutputKBMMapping.MOUSEEVENTF_XBUTTONUP, 2);
                }

                if (_syntheticInput.GlobalState.currentClicks.wUpCount != 0 && _syntheticInput.GlobalState.previousClicks.wUpCount == 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseWheelUp");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseWheelUp");
                    _virtualKbm.PerformMouseWheelEvent(settings.OutputKBMMapping.WHEEL_TICK_UP, 0);
                    _oldnow = _utcNow();
                    _wheel = settings.OutputKBMMapping.WHEEL_TICK_UP;
                }
                else if (_syntheticInput.GlobalState.currentClicks.wUpCount == 0 && _syntheticInput.GlobalState.previousClicks.wUpCount != 0)
                    _wheel = 0;

                if (_syntheticInput.GlobalState.currentClicks.wDownCount != 0 && _syntheticInput.GlobalState.previousClicks.wDownCount == 0)
                {
                    AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseWheelDown");
                    AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseWheelDown");
                    _virtualKbm.PerformMouseWheelEvent(settings.OutputKBMMapping.WHEEL_TICK_DOWN, 0);
                    _oldnow = _utcNow();
                    _wheel = settings.OutputKBMMapping.WHEEL_TICK_DOWN;
                }
                if (_syntheticInput.GlobalState.currentClicks.wDownCount == 0 && _syntheticInput.GlobalState.previousClicks.wDownCount != 0)
                    _wheel = 0;
            }


            if (_wheel != 0) //Continue mouse wheel movement
            {
                DateTime now = _utcNow();
                if (now >= _oldnow + TimeSpan.FromMilliseconds(150))
                {
                    _oldnow = now;
                    _virtualKbm.PerformMouseWheelEvent(_wheel, 0);
                }
            }

            _syntheticInput.GlobalState.SaveToPrevious(false);

            _syntheticInput.SyncStateLock.ExitWriteLock();
            state.SaveToPrevious(true);

            // Send possible virtual events to system. Only used for FakerInput atm.
            // SendInput version does nothing
            // Update toggle-driven repeat controller so it can emit repeats using stored handlers
            // ToggleActionController removed from per-path usage — per-device controllers handle updates.
            _virtualKbm.Sync();
        }
    }
}
