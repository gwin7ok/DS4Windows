using System;
using System.Collections.Generic;
using System.Text;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 MapCustom の分割 M1（Phase9-MapCustom-Plan.md §3）: <see cref="SyntheticInputCommitter"/>（旧 <c>Mapping.Commit</c>）の差分テスト。
    /// 旧 <c>Commit</c> の逐語コピー（<c>pressagain</c> の判定を除く。常に false だった）と、同じクリックの列で、記録された出力（ボタン、拡張ボタン、ホイール、Sync）が一致することを確かめる。
    /// 時刻は、両方に同じ偽の時計を渡して決定的にする（ホイールの繰り返しは 150ms）。
    /// </summary>
    public class Phase9MapCustomCommitterDifferentialTests
    {
        // ---- 旧 Mapping.Commit の逐語コピー（旧 oldnow／wheel は、この中が持つ。コメントは省略）----
        private sealed class OldCommitter
        {
            private readonly SyntheticInputState _syntheticInput;
            private readonly IVirtualKBM _virtualKbm;
            private readonly Func<DateTime> _utcNow;
            public DateTime oldnow = DateTime.UtcNow;
            private int wheel = 0;

            public OldCommitter(SyntheticInputState syntheticInput, IVirtualKBM virtualKbm, Func<DateTime> utcNow)
            {
                _syntheticInput = syntheticInput;
                _virtualKbm = virtualKbm;
                _utcNow = utcNow;
            }

            private bool pressagain = false;

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
                        oldnow = _utcNow();
                        wheel = settings.OutputKBMMapping.WHEEL_TICK_UP;
                    }
                    else if (_syntheticInput.GlobalState.currentClicks.wUpCount == 0 && _syntheticInput.GlobalState.previousClicks.wUpCount != 0)
                        wheel = 0;

                    if (_syntheticInput.GlobalState.currentClicks.wDownCount != 0 && _syntheticInput.GlobalState.previousClicks.wDownCount == 0)
                    {
                        AppLogger.LogTrace($"SYNTHETIC TRACE device={device} event=MouseWheelDown");
                        AppLogger.LogDebug($"EVENT SENT [SYNTHETIC] device={device} event=MouseWheelDown");
                        _virtualKbm.PerformMouseWheelEvent(settings.OutputKBMMapping.WHEEL_TICK_DOWN, 0);
                        oldnow = _utcNow();
                        wheel = settings.OutputKBMMapping.WHEEL_TICK_DOWN;
                    }
                    if (_syntheticInput.GlobalState.currentClicks.wDownCount == 0 && _syntheticInput.GlobalState.previousClicks.wDownCount != 0)
                        wheel = 0;
                }


                if (wheel != 0) //Continue mouse wheel movement
                {
                    DateTime now = _utcNow();
                    if (now >= oldnow + TimeSpan.FromMilliseconds(150) && !pressagain)
                    {
                        oldnow = now;
                        _virtualKbm.PerformMouseWheelEvent(wheel, 0);
                    }
                }

                _syntheticInput.GlobalState.SaveToPrevious(false);

                _syntheticInput.SyncStateLock.ExitWriteLock();
                state.SaveToPrevious(true);
                _virtualKbm.Sync();
            }
        }

        private sealed class FakeClock
        {
            public DateTime Now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            public DateTime Get() => Now;
        }

        // 出力のすべて（ボタン、拡張ボタン、ホイール、Sync）を、呼ばれた順に記録する
        private sealed class CallLog : IVirtualKBM
        {
            public readonly List<string> Calls = new List<string>();
            public int WheelCount;

            public bool fakeKeyRepeat { get; set; }
            public bool Connect() => true;
            public bool Disconnect() => true;
            public void MoveRelativeMouse(int x, int y) => Calls.Add($"move:{x},{y}");
            public void PerformMouseWheelEvent(int vertical, int horizontal) { WheelCount++; Calls.Add($"wheel:{vertical},{horizontal}"); }
            public void PerformMouseButtonEvent(uint mouseButton) => Calls.Add($"button:{mouseButton}");
            public void PerformMouseButtonEventAlt(uint mouseButton, int type) => Calls.Add($"buttonAlt:{mouseButton},{type}");
            public void PerformMouseButtonPress(uint mouseButton) => Calls.Add($"press:{mouseButton}");
            public void PerformMouseButtonRelease(uint mouseButton) => Calls.Add($"release:{mouseButton}");
            public void PerformKeyPress(uint key) { }
            public void PerformKeyPressAlt(uint key) { }
            public void PerformKeyRelease(uint key) { }
            public void PerformKeyReleaseAlt(uint key) { }
            public void Sync() => Calls.Add("sync");
            public string GetDisplayName() => string.Empty;
            public string GetIdentifier() => string.Empty;
            public string GetFullDisplayName() => string.Empty;
        }

        private sealed class Rig
        {
            public readonly SyntheticInputState State = new SyntheticInputState();
            public readonly CallLog Recorder = new CallLog();
            public readonly FakeClock Clock = new FakeClock();
        }

        private static string Snapshot(Rig rig)
        {
            return string.Join(";", rig.Recorder.Calls);
        }

        private static readonly Click[] Kinds =
        {
            Click.Left, Click.Middle, Click.Right, Click.Fourth, Click.Fifth, Click.WUP, Click.WDOWN,
        };

        // 各ティックの「どのデバイスが何を押しているか」を、数のビット列で表す（デバイス 0・1。種類ごとに 1 ビット）
        private static void Press(Rig rig, int device, int mask)
        {
            for (int i = 0; i < Kinds.Length; i++)
            {
                if ((mask & (1 << i)) != 0) rig.State.CountClick(device, Kinds[i]);
            }
        }

        private static IEnumerable<int[]> Sequences(int length, int alphabet)
        {
            int[] codes = new int[length];
            while (true)
            {
                yield return (int[])codes.Clone();
                int k = 0;
                while (k < length && ++codes[k] == alphabet)
                {
                    codes[k] = 0;
                    k++;
                }

                if (k == length) yield break;
            }
        }

        [Fact]
        public void TheNewCommitter_MatchesTheOldCommit_ForClickSequences_OnTwoDevices()
        {
            using var scope = new MappingCharacterizationScope();
            IProfileSettingsService settings = scope.Settings;

            // 1 ティックの押し方: 何も押さない／左／右／中／4／5／ホイール上／ホイール下／左＋右／ホイール上下 など（ビット列: 1=左, 2=中, 4=右, 8=4, 16=5, 32=ホイール上, 64=ホイール下）
            int[] allMasks = { 0, 1, 4, 2, 8, 16, 32, 64, 5, 96, 127 };
            int[] fewMasks = { 0, 1, 4, 32, 64, 96 };
            int checkedCount = 0;

            // 全種類は 2 ティックまで（11^4 通り）、主要な 6 種類は 3 ティックまで（6^6 通り）。組み合わせの爆発を避ける
            foreach (var (length, masks) in new[] { (1, allMasks), (2, allMasks), (3, fewMasks) })
            {
                foreach (int[] codes in Sequences(length * 2, masks.Length))
                {
                    var oldRig = new Rig();
                    var newRig = new Rig();
                    var oldCommitter = new OldCommitter(oldRig.State, oldRig.Recorder, oldRig.Clock.Get);
                    var newCommitter = new SyntheticInputCommitter(newRig.State, newRig.Recorder, newRig.Clock.Get);
                    oldCommitter.oldnow = oldRig.Clock.Now;
                    newCommitter.SetWheelRepeatBase(newRig.Clock.Now);

                    // 各ティック: デバイス 0 と 1 が（codes の 2 つずつ）押し、それぞれ Commit する。ティックの間に 100ms 進める（ホイールの繰り返しは 150ms）
                    for (int tick = 0; tick < length; tick++)
                    {
                        foreach (var (rig, commit) in new (Rig, Action<int>)[]
                        {
                            (oldRig, d => oldCommitter.Commit(d, settings)),
                            (newRig, d => newCommitter.Commit(d, settings)),
                        })
                        {
                            Press(rig, 0, masks[codes[tick * 2]]);
                            commit(0);
                            Press(rig, 1, masks[codes[tick * 2 + 1]]);
                            commit(1);
                            rig.Clock.Now = rig.Clock.Now.AddMilliseconds(100);
                        }
                    }

                    Assert.Equal(Snapshot(oldRig), Snapshot(newRig));
                    checkedCount++;
                }
            }

            Assert.True(checkedCount > 10000);
        }

        [Fact]
        public void TheWheel_RepeatsEvery150Milliseconds_WhileHeld_AndStopsOnRelease()
        {
            using var scope = new MappingCharacterizationScope();
            var rig = new Rig();
            var committer = new SyntheticInputCommitter(rig.State, rig.Recorder, rig.Clock.Get);
            committer.SetWheelRepeatBase(rig.Clock.Now);

            rig.State.CountClick(0, Click.WUP);
            committer.Commit(0, scope.Settings);
            Assert.Equal(1, rig.Recorder.WheelCount);

            // 押したまま。140ms 後はまだ、160ms 後（最初から 150ms 以上）は 1 回繰り返す
            rig.Clock.Now = rig.Clock.Now.AddMilliseconds(140);
            rig.State.CountClick(0, Click.WUP);
            committer.Commit(0, scope.Settings);
            Assert.Equal(1, rig.Recorder.WheelCount);

            rig.Clock.Now = rig.Clock.Now.AddMilliseconds(20);
            rig.State.CountClick(0, Click.WUP);
            committer.Commit(0, scope.Settings);
            Assert.Equal(2, rig.Recorder.WheelCount);

            // 離すと止まる
            rig.Clock.Now = rig.Clock.Now.AddMilliseconds(500);
            committer.Commit(0, scope.Settings);
            Assert.Equal(2, rig.Recorder.WheelCount);
        }

        [Fact]
        public void TheWheelState_IsSharedByAllDevices_SoAReleaseSeenByAnotherDeviceStopsTheRepeat()
        {
            // 押した側と離した側が別のデバイスでも、繰り返しが止まる（デバイスごとに分けると、止まらなくなる。計画書の調査）
            using var scope = new MappingCharacterizationScope();
            var rig = new Rig();
            var committer = new SyntheticInputCommitter(rig.State, rig.Recorder, rig.Clock.Get);
            committer.SetWheelRepeatBase(rig.Clock.Now);

            rig.State.CountClick(0, Click.WUP);
            committer.Commit(0, scope.Settings);   // デバイス 0 が押す
            int afterPress = rig.Recorder.WheelCount;

            rig.Clock.Now = rig.Clock.Now.AddMilliseconds(1000);
            committer.Commit(1, scope.Settings);   // デバイス 1 の Commit が、離したことを検出する
            int afterRelease = rig.Recorder.WheelCount;

            rig.Clock.Now = rig.Clock.Now.AddMilliseconds(1000);
            committer.Commit(0, scope.Settings);
            committer.Commit(1, scope.Settings);
            Assert.Equal(afterRelease, rig.Recorder.WheelCount);
            Assert.True(afterRelease >= afterPress);
        }
    }
}
