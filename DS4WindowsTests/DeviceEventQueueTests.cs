using System.Collections.Generic;
using DS4Windows.Core.Model;
using DS4Windows.Input;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 入力機器の、読み取りのループへ処理を積む待ち行列（<c>DeviceEventQueue</c>）と、無操作の判定（<c>IdleInputDetector</c>）の振る舞い。
    /// </summary>
    public class DeviceEventQueueTests
    {
        [Fact]
        public void RunPending_RunsEnqueuedActionsInOrderOnce()
        {
            var q = new DeviceEventQueue();
            var log = new List<int>();
            q.Enqueue(() => log.Add(1));
            q.Enqueue(() => log.Add(2));

            q.RunPending();
            q.RunPending();

            Assert.Equal(new[] { 1, 2 }, log);
        }

        [Fact]
        public void RunPending_ActionEnqueuedWhileRunningWaitsForTheNextEnqueue()
        {
            var q = new DeviceEventQueue();
            var log = new List<int>();
            q.Enqueue(() => { log.Add(1); q.Enqueue(() => log.Add(2)); });

            q.RunPending();
            Assert.Equal(new[] { 1 }, log);

            // 実行中に積まれた処理は、実行後にフラグを下ろすため、次に何かが積まれるまで実行されない（従来の振る舞いを固定）
            q.RunPending();
            Assert.Equal(new[] { 1 }, log);

            q.Enqueue(() => log.Add(3));
            q.RunPending();
            Assert.Equal(new[] { 1, 2, 3 }, log);
        }

        [Fact]
        public void IdleInputDetector_NeutralStateIsIdle()
        {
            var s = new ControllerState { LX = 128, LY = 128, RX = 128, RY = 128 };
            Assert.True(IdleInputDetector.IsIdle(s));
        }

        [Fact]
        public void IdleInputDetector_ButtonStickTriggerOrTouchIsNotIdle()
        {
            Assert.False(IdleInputDetector.IsIdle(new ControllerState { LX = 128, LY = 128, RX = 128, RY = 128, Cross = true }));
            Assert.False(IdleInputDetector.IsIdle(new ControllerState { LX = 128, LY = 128, RX = 128, RY = 128, L2 = 1 }));
            Assert.False(IdleInputDetector.IsIdle(new ControllerState { LX = 128 + 64, LY = 128, RX = 128, RY = 128 }));
            Assert.False(IdleInputDetector.IsIdle(new ControllerState { LX = 128, LY = 128, RX = 128, RY = 127 - 64 }));
            Assert.False(IdleInputDetector.IsIdle(new ControllerState { LX = 128, LY = 128, RX = 128, RY = 128, Touch1 = true }));
        }
    }
}
