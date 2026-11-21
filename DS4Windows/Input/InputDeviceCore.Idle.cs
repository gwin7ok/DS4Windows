using System;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;

namespace DS4Windows.Input
{
    /// <summary>無操作の判定と、入力イベントのキュー。</summary>
    public partial class InputDeviceCore
    {
        public bool IsDS4Idle() => IdleInputDetector.IsIdle(cState);

        /// <summary>
        /// 読み取りのループの 1 フレームごとに、アイドルの判定と最後に操作した時刻（<c>lastActive</c>）を更新する。
        /// USB は時刻の更新だけ。BT などは、アイドルが <c>idleTimeout</c> 秒続き、充電中でなければ true（切断すべき）を返す。
        /// 切断の手順（<c>DisconnectBT</c> など）は機種ごとに違うため、呼ぶ側が行う。
        /// </summary>
        protected bool EvaluateIdleDisconnect(DateTime now)
        {
            if (conType == ConnectionType.USB)
            {
                if (idleTimeout == 0)
                {
                    lastActive = now;
                }
                else
                {
                    idleInput = IsDS4Idle();
                    if (!idleInput)
                    {
                        lastActive = now;
                    }
                }

                return false;
            }

            bool shouldDisconnect = false;
            if (!isRemoved && idleTimeout > 0)
            {
                idleInput = IsDS4Idle();
                if (idleInput)
                {
                    DateTime timeout = lastActive + TimeSpan.FromSeconds(idleTimeout);
                    if (!charging)
                        shouldDisconnect = now >= timeout;
                }
                else
                {
                    lastActive = now;
                }
            }
            else
            {
                lastActive = now;
            }

            if (shouldDisconnect)
            {
                AppLogger.LogUser(AppLogLevel.Info, "Log_Device_IdleDisconnect", Mac.ToString());
            }

            return shouldDisconnect;
        }

        /// <summary>読み取りのループの末尾で、他のスレッドから積まれた処理（<c>eventQueue</c>）を、その時点の件数だけ実行する。</summary>
        protected void RunQueuedInputEvents() => _events.RunPending();

        public void QueueEvent(Action act) => _events.Enqueue(act);

        /// <summary>
        /// Must not be run from input thread. Waits for input thread to be in a wait state
        /// and then tell thread to no longer invoke the Report event. Input thread will then
        /// resume followed by invoking the action passed. Flag will be set to have
        /// Report event to resume being invoked after
        /// </summary>
        /// <param name="act">Action to execute in current thread</param>
        public void HaltReportingRunAction(Action act)
        {
            // Wait for controller to be in a wait period
            bool result = readWaitEv.Wait(millisecondsTimeout: 500);
            if (result)
            {
                readWaitEv.Reset();

                // Tell device to no longer fire reports
                fireReport = false;

                // Flag is set. Allow input thread to resume
                readWaitEv.Set();

                // Invoke main desired action
                act?.Invoke();

                // Start firing reports again
                fireReport = true;
            }
        }
    }
}
