using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.Services;
using NLog;
using NLog.Config;
using NLog.Targets;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.6b（Phase8-Step7-6-Plan.md §3.3。Q3＝B: 切断の成功／失敗と再試行）: DisconnectBT の新経路の確認。
    /// 実機のコントローラーがないため、旧経路は効果の分岐（<c>ctrl.DS4Controllers[device]</c>）で止まる。効果は記録用のポートで確かめる。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step76bDisconnectBTTests
    {
        private static DS4State Frame(bool l1, bool r1 = false)
        {
            DS4State state = SpecialActionTestScope.Neutral();
            state.L1 = l1;
            state.R1 = r1;
            return state;
        }

        private static ActionBindingBuilder NewBuilder(IControllerDisconnectPort port)
            => new ActionBindingBuilder(TestHost.GetService<IProfileActionProvider>(), TestHost.GetService<IProfileSettingsService>(),
                TestHost.GetService<IManagedActionManager>(), TestHost.GetService<IMacroPlayer>(), TestHost.GetService<IProcessLauncher>(),
                TestHost.GetService<IProfileSwitcher>(), TestHost.GetService<ISpecialActionRepository>(),
                TestHost.GetService<Func<IMapping>>(), null, port);

        private static void Publish(SpecialActionTestScope t, IControllerDisconnectPort port)
            => TestHost.GetService<IActionRegistry>().Replace(t.Device, NewBuilder(port).Build(t.Device));

        private static SpecialAction AddDisconnect(SpecialActionTestScope t, string triggers, IControllerDisconnectPort port, bool freshPressGuard = false)
        {
            SpecialAction sa = t.Add(new SpecialAction("p8s76b_dc_" + Guid.NewGuid().ToString("N"), triggers, "DisconnectBT", string.Empty, 0, string.Empty));
            ActionInstanceState state = t.StateOf(sa);
            state.RequiresFreshPressAfterReset = freshPressGuard;
            state.IsExecuting = false;
            Publish(t, port);
            return sa;
        }

        /// <summary>切断の後ろに置く Key（同じ L1 で成立）。フレームが打ち切られたかどうか（後ろの特殊アクションが処理されたか）の観察用。</summary>
        private static SpecialAction AddKeyAfter(SpecialActionTestScope t, IControllerDisconnectPort port)
        {
            SpecialAction key = t.AddKey("L1", toggle: false);
            t.StateOf(key).RequiresFreshPressAfterReset = false;
            Publish(t, port);
            return key;
        }

        [Fact]
        public void ABluetoothDisconnect_IsRequestedOnce_AndTheFrameIsCutEveryFrame()
        {
            using var t = new SpecialActionTestScope();
            var port = new RecordingControllerDisconnectPort();
            SpecialAction dc = AddDisconnect(t, "L1", port);
            SpecialAction key = AddKeyAfter(t, port);
            Assert.True(t.Engine.Owns(t.Device, dc));

            t.RunFrame(Frame(false));
            t.RunFrame(Frame(true));
            Assert.Equal(1, port.BluetoothCalls);
            Assert.Equal(new[] { t.Device }, port.Devices.ToArray());
            Assert.False(ActionManager.IsBeingTriggered(key, t.Device));   // 切断のフレームでは、後ろの Key は処理されない

            t.RunFrame(Frame(true));
            t.RunFrame(Frame(true));
            Assert.Equal(1, port.BluetoothCalls);                           // 成功したので、繰り返さない（旧経路は切断の完了まで毎フレーム繰り返した）
            Assert.False(ActionManager.IsBeingTriggered(key, t.Device));   // 切断の完了までの間、フレームの打ち切りは続く（旧経路の return と同じ）
        }

        [Fact]
        public void AFailedRequest_IsRetriedWhileHeld_UntilItSucceeds()
        {
            using var t = new SpecialActionTestScope();
            var port = new RecordingControllerDisconnectPort();
            port.BluetoothResults.Enqueue(false);
            port.BluetoothResults.Enqueue(false);   // 3 回目から成功（既定）
            SpecialAction dc = AddDisconnect(t, "L1", port);
            SpecialAction key = AddKeyAfter(t, port);

            t.RunFrame(Frame(false));
            t.RunFrame(Frame(true));
            Assert.Equal(1, port.BluetoothCalls);                          // 失敗
            Assert.True(ActionManager.IsBeingTriggered(key, t.Device));    // 失敗したときは、フレームを打ち切らない（後ろの Key が処理される）

            t.RunFrame(Frame(true));
            Assert.Equal(2, port.BluetoothCalls);                          // 再試行（失敗）

            t.RunFrame(Frame(true));
            Assert.Equal(3, port.BluetoothCalls);                          // 再試行（成功）

            t.RunFrame(Frame(true));
            t.RunFrame(Frame(true));
            Assert.Equal(3, port.BluetoothCalls);                          // 成功したあとは、繰り返さない
        }

        [Fact]
        public void AfterTheTriggerIsReleased_ThePressDisconnectsAgain()
        {
            using var t = new SpecialActionTestScope();
            var port = new RecordingControllerDisconnectPort();
            AddDisconnect(t, "L1", port);

            t.RunFrame(Frame(true));
            t.RunFrame(Frame(true));
            Assert.Equal(1, port.BluetoothCalls);

            t.RunFrame(Frame(false));
            t.RunFrame(Frame(true));
            Assert.Equal(2, port.BluetoothCalls);   // 離して押し直すと、もう一度（切断が実際には完了しなかった場合の救済）
        }

        [Theory]
        [InlineData(DisconnectTargetKind.None)]
        [InlineData(DisconnectTargetKind.Dongle)]
        public void WhenTheControllerCannotBeDisconnectedOverBluetooth_NothingIsRequested_AndTheFrameIsNotCut(DisconnectTargetKind kind)
        {
            using var t = new SpecialActionTestScope();
            var port = new RecordingControllerDisconnectPort { Kind = kind };
            AddDisconnect(t, "L1", port);
            SpecialAction key = AddKeyAfter(t, port);

            t.RunFrame(Frame(false));
            t.RunFrame(Frame(true));
            t.RunFrame(Frame(true));

            Assert.Equal(0, port.BluetoothCalls);
            Assert.True(ActionManager.IsBeingTriggered(key, t.Device));   // 後ろの Key が処理される
            Assert.Equal(0, port.DongleCalls);                             // 押している間は、ドングルも切断しない
        }

        [Fact]
        public void WhileTheFreshPressGuardIsOn_NothingIsRequested()
        {
            using var t = new SpecialActionTestScope();
            var port = new RecordingControllerDisconnectPort();
            AddDisconnect(t, "L1", port, freshPressGuard: true);

            t.RunFrame(Frame(true));
            t.RunFrame(Frame(true));

            Assert.Equal(0, port.BluetoothCalls);
        }

        [Fact]
        public void AndCondition_NeedsAllTheTriggers()
        {
            using var t = new SpecialActionTestScope();
            var port = new RecordingControllerDisconnectPort();
            AddDisconnect(t, "L1/R1", port);

            t.RunFrame(Frame(true, false));
            t.RunFrame(Frame(false, true));
            Assert.Equal(0, port.BluetoothCalls);

            t.RunFrame(Frame(true, true));
            Assert.Equal(1, port.BluetoothCalls);
        }

        // ---- ドングル ----

        [Fact]
        public void ADongle_DisconnectsAfterTheTriggerIsReleased_AndTheControllerIsIdle()
        {
            using var t = new SpecialActionTestScope();
            var port = new RecordingControllerDisconnectPort { Kind = DisconnectTargetKind.Dongle, Idle = false };
            AddDisconnect(t, "L1", port);

            t.RunFrame(Frame(true));
            t.RunFrame(Frame(true));
            Assert.Equal(0, port.DongleCalls);        // 押している間は切断しない

            t.RunFrame(Frame(false));
            t.RunFrame(Frame(false));
            Assert.Equal(0, port.DongleCalls);        // 離しても、アイドルになるまで待つ

            port.Idle = true;
            t.RunFrame(Frame(false));
            Assert.Equal(1, port.DongleCalls);        // アイドルになったら切断する

            t.RunFrame(Frame(false));
            t.RunFrame(Frame(false));
            Assert.Equal(1, port.DongleCalls);        // 切断待ちは 1 回で終わる
            Assert.Equal(0, port.BluetoothCalls);
        }

        [Fact]
        public void ADongleThatIsNotSynced_DoesNotDisconnect()
        {
            using var t = new SpecialActionTestScope();
            var port = new RecordingControllerDisconnectPort { Kind = DisconnectTargetKind.Dongle, DongleSynced = false };
            AddDisconnect(t, "L1", port);

            t.RunFrame(Frame(true));
            t.RunFrame(Frame(false));
            t.RunFrame(Frame(false));

            Assert.Equal(0, port.DongleCalls);
        }

        // ---- トリガーのボタンのキーの解放 ----

        private static void WithTheTriggerButtonsKeyAssigned(Action<DS4ControlSettings> body)
        {
            DS4ControlSettings dcs = TestGlobals.Store.GetDS4CSetting(MappingCharacterizationScope.Device, DS4Controls.L1);
            try
            {
                dcs.Reset();
                dcs.actionType = DS4ControlSettings.ActionType.Key;
                dcs.action.actionKey = 65;
                body(dcs);
            }
            finally
            {
                dcs.Reset();
            }
        }

        [Fact]
        public void TheTriggerButtonsKey_IsReleasedWhenTheBluetoothRequestSucceeds_NotBefore()
        {
            WithTheTriggerButtonsKeyAssigned(_ =>
            {
                using var t = new SpecialActionTestScope();
                var port = new RecordingControllerDisconnectPort();
                port.BluetoothResults.Enqueue(false);   // 1 回目は失敗
                AddDisconnect(t, "L1", port);
                int before = t.Scope.Recorder.KeyReleases.Count;

                t.RunFrame(Frame(true));
                Assert.Equal(0, t.Scope.Recorder.KeyReleases.Count - before);   // 失敗したときは離さない

                t.RunFrame(Frame(true));
                Assert.Equal(1, t.Scope.Recorder.KeyReleases.Count - before);   // 成功したとき 1 回

                t.RunFrame(Frame(true));
                Assert.Equal(1, t.Scope.Recorder.KeyReleases.Count - before);   // 繰り返さない
            });
        }

        [Fact]
        public void TheTriggerButtonsKey_IsReleasedWhenTheDongleDisconnects()
        {
            WithTheTriggerButtonsKeyAssigned(_ =>
            {
                using var t = new SpecialActionTestScope();
                var port = new RecordingControllerDisconnectPort { Kind = DisconnectTargetKind.Dongle };
                AddDisconnect(t, "L1", port);
                int before = t.Scope.Recorder.KeyReleases.Count;

                t.RunFrame(Frame(true));
                Assert.Equal(0, t.Scope.Recorder.KeyReleases.Count - before);

                t.RunFrame(Frame(false));
                Assert.Equal(1, t.Scope.Recorder.KeyReleases.Count - before);
                Assert.Equal(1, port.DongleCalls);
            });
        }

        // ---- ビルダー・スイッチ ----

        [Fact]
        public void TheBuilder_BuildsTheBindingOnlyWithAPort()
        {
            using var t = new SpecialActionTestScope();
            var port = new RecordingControllerDisconnectPort();
            SpecialAction sa = AddDisconnect(t, "L1", port);

            IActionBinding binding = Assert.Single(NewBuilder(port).Build(t.Device));
            Assert.Same(sa, binding.Source);
            Assert.IsType<DisconnectBTAction>(Assert.Single(binding.Outputs));
            Assert.True(binding.Policy.RespectsFreshPressGuard);
            Assert.False(binding.Policy.BlocksWhileExecuting);
            Assert.Equal(UntriggerPolicy.None, binding.Policy.Untrigger);

            Assert.Empty(NewBuilder(null).Build(t.Device));   // ポートがなければ、束縛を作らず、旧経路が処理する
        }

        // ---- ログ ----

        private static List<string> RunLogged(string name, LogLevel minLevel, RecordingControllerDisconnectPort port, bool[] l1Frames)
        {
            LoggingConfiguration saved = LogManager.Configuration;
            var target = new MemoryTarget("p8s76b") { Layout = "${level}|${message}" };
            var config = new LoggingConfiguration();
            config.AddTarget(target);
            config.AddRule(minLevel, LogLevel.Fatal, target);
            LogManager.Configuration = config;
            try
            {
                using (var t = new SpecialActionTestScope())
                {
                    SpecialAction sa = t.Add(new SpecialAction(name, "L1", "DisconnectBT", string.Empty, 0, string.Empty));
                    t.StateOf(sa).RequiresFreshPressAfterReset = false;
                    Publish(t, port);
                    foreach (bool l1 in l1Frames) t.RunFrame(Frame(l1));
                }

                return target.Logs.Where(l => l.Contains(name)
                    && (l.Contains("Trigger detected for SA") || l.Contains("maps to Type") || l.Contains("Trigger keys for SA") || l.Contains("output combo"))).ToList();
            }
            finally
            {
                LogManager.Configuration = saved;
            }
        }

        [Fact]
        public void EachAttempt_LogsTheFourTraceLines_AndThereIsNoDebugLine()
        {
            string name = "p8s76b_log_" + Guid.NewGuid().ToString("N");
            var port = new RecordingControllerDisconnectPort();
            port.BluetoothResults.Enqueue(false);   // 1 回目は失敗 → 2 回目で成功

            List<string> lines = RunLogged(name, LogLevel.Trace, port, new[] { false, true, true, true, true });

            Assert.Equal(2, port.BluetoothCalls);
            Assert.Equal(8, lines.Count);   // 試行ごとに Trace 4 行（成功したあとは、繰り返さない）
            Assert.Equal(lines.Take(4).ToList(), lines.Skip(4).Take(4).ToList());
            Assert.Contains(lines, l => l.StartsWith("Trace|Trigger detected for SA") && l.Contains("on device"));
            Assert.Contains(lines, l => l.Contains("maps to Type:DisconnectBT"));
            Assert.Contains(lines, l => l.Contains("Trigger keys for SA") && l.EndsWith("<none>"));
            Assert.Contains(lines, l => l.Contains("output combo: "));
        }

        [Fact]
        public void WhenOnlyWarningsAreEnabled_NothingIsLogged()
        {
            string name = "p8s76b_off_" + Guid.NewGuid().ToString("N");

            Assert.Empty(RunLogged(name, LogLevel.Warn, new RecordingControllerDisconnectPort(), new[] { true, true }));
        }

        // ---- 割り当て ----

        private static long AllocatedWhileHeld(SpecialActionTestScope t)
        {
            DS4State held = Frame(true);
            for (int i = 0; i < 300; i++) t.RunFrame(held);   // 立ち上がりと JIT は、測定の外へ

            // Phase10-Step4 W4: 5 ラウンド測って最小値を使う（一度きりの割り当てが測定の窓に入っても結果が変わらないように。Phase8Step76cBatteryCheckTests と同じ）
            long min = long.MaxValue;
            for (int round = 0; round < 5; round++)
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 1000; i++) t.RunFrame(held);
                min = Math.Min(min, GC.GetAllocatedBytesForCurrentThread() - before);
            }

            return min;
        }

        [Fact]
        public void TheNewPath_DoesNotAddAllocationsToTheFrame_WhileHeldAfterASuccess()
        {
            // 同じ件数の DisconnectBT で、トリガーが押されていない（R1。押すのは L1）場合を基準にする（Phase8Step76aGyroCalibrateTests と同じ方式）
            long baseline;
            using (var t = new SpecialActionTestScope())
            {
                AddDisconnect(t, "R1", new RecordingControllerDisconnectPort());
                baseline = AllocatedWhileHeld(t);
            }

            long withDisconnect;
            var port = new RecordingControllerDisconnectPort();
            using (var t = new SpecialActionTestScope())
            {
                AddDisconnect(t, "L1", port);
                withDisconnect = AllocatedWhileHeld(t);
            }

            Assert.True(withDisconnect <= baseline + 4096, $"押している間: あり {withDisconnect} B > 基準 {baseline} B");
            Assert.Equal(1, port.BluetoothCalls);
        }

        // ---- 本番のポート ----

        [Fact]
        public void TheDefaultPort_DoesNothingWhenThereIsNoController()
        {
            var port = new DefaultControllerDisconnectPort(() => new NoControllerAccessor());

            Assert.Equal(DisconnectTargetKind.None, port.GetTargetKind(0));
            Assert.False(port.DisconnectBluetooth(0));
            Assert.False(port.IsDongleSynced(0));
            Assert.False(port.IsIdle(0));
            Assert.False(port.DisconnectDongle(0));
            Assert.Equal(DisconnectTargetKind.None, port.GetTargetKind(-1));
            Assert.Equal(DisconnectTargetKind.None, port.GetTargetKind(99));

            var nullAccessor = new DefaultControllerDisconnectPort(() => null);
            Assert.Equal(DisconnectTargetKind.None, nullAccessor.GetTargetKind(0));
            Assert.False(nullAccessor.DisconnectBluetooth(0));
        }

        private sealed class NoControllerAccessor : IDeviceStateAccessor
        {
            public DS4Device GetController(int deviceIndex) => null;
        }
    }
}
