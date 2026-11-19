#nullable enable

using System;
using System.Diagnostics;
using System.Threading;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Hid;

namespace DS4Windows.Input.Devices
{
    /// <summary>
    /// 読み取りのループの共通の骨組み。1 フレームの流れ（遅延の計測→読み取り→経過時間→解析→アイドル→<c>Report</c>→出力→積まれた処理）を持ち、
    /// 機種ごとに違う部分（読み取りの検証、解析・電池・時間・動き、アイドル切断、出力の書き込み）を差し込み口にする。
    /// 機種の追加は、差し込み口だけを書く。
    /// </summary>
    public abstract class InputLoopDevice : InputDeviceCore
    {
        /// <summary><see cref="InputLoopDevice.ReceiveFrame"/> の結果。</summary>
        protected enum FrameResult
        {
            /// <summary>読み取れた。続きの処理へ進む。</summary>
            Accept,

            /// <summary>このフレームは捨てて、次の読み取りへ。</summary>
            Skip,

            /// <summary>接続が切れた。止めて、切断（<c>Removal</c>）を通知する。</summary>
            Disconnected,

            /// <summary>機種が切断の手順を済ませた。ループを今すぐ終える（<c>exitInputThread</c> は立てずに、スレッドを抜ける）。</summary>
            Terminate,
        }

        // 基底の Report は仮想のイベント。派生で持ち直し、読み取りのスレッドから発火する
        public override event ReportHandler<EventArgs>? Report = null;

        public override void removeReportHandlers()
        {
            this.Report = null;
        }

        /// <summary>1 フレームの処理の途中で起きた、続行できる誤り（タッチパッドの読み取りの失敗など）。フレームの終わりに <c>error</c> へ反映される。</summary>
        protected string frameError = string.Empty;

        /// <summary>このフレームを読み取った時刻（ストップウォッチの刻み）。</summary>
        protected long lastFrameTicks;

        /// <summary>前のフレームの時刻。<see cref="IsExpectedReport"/> でフレームを捨てるとき、時刻を前へ戻すために使う。</summary>
        protected long previousFrameTicks;

        protected InputLoopDevice(IHidTransport hidDevice, string displayName, VidPidFeatureSet featureSet)
            : base(hidDevice, displayName, featureSet)
        {
        }

        /// <summary>ループの開始時（スレッドの中、最初の読み取りの前）に呼ばれる。機種の準備をここに書く。</summary>
        protected virtual void OnLoopStarting()
        {
        }

        /// <summary>
        /// 1 回読み取る。待機中の印（<c>readWaitEv</c>）の解除までを、この中で行う。
        /// 既定は、通信路から読み、切断・読み取りの失敗なら <see cref="FrameResult.Disconnected"/>、それ以外の失敗は <see cref="FrameResult.Skip"/>。
        /// 機種独自の検証（BT の CRC など）や、切断の独自の手順がある機種は、置き換える。
        /// </summary>
        protected virtual FrameResult ReceiveFrame()
        {
            HidReadStatus status = hDevice.ReadFile(inputReport);
            readWaitEv.Wait();
            readWaitEv.Reset();

            if (status == HidReadStatus.NotConnected || status == HidReadStatus.ReadError)
            {
                return FrameResult.Disconnected;
            }

            return status == HidReadStatus.Success ? FrameResult.Accept : FrameResult.Skip;
        }

        /// <summary>このフレームの時刻（ストップウォッチの刻み）。既定は、読み取りが終わったあとの現在時刻。</summary>
        protected virtual long GetFrameTicks() => Stopwatch.GetTimestamp();

        /// <summary>読み取ったレポートが、期待する種類か。違うレポートは捨てる（経過時間の計測のあとに呼ばれる）。</summary>
        protected virtual bool IsExpectedReport() => true;

        /// <summary>
        /// レポートの解析、電池、経過時間（<c>cState.elapsedTime</c>・<c>totalMicroSec</c>）、動き・タッチパッドの取り込み。
        /// 呼ばれる時点で、<c>utcNow</c>、<c>cState.PacketCounter</c>、<c>cState.ReportTimeStamp</c> は設定済み。
        /// </summary>
        protected abstract void ProcessFrame();

        /// <summary>アイドルの判定と最後に操作した時刻の更新。切断してループを終えるなら true。既定は常に操作中とみなす。</summary>
        protected virtual bool ProcessIdle()
        {
            lastActive = utcNow;
            return false;
        }

        /// <summary>出力（振動・ライトバーなど）を、必要なら書き込む。<c>Report</c> のあとに呼ばれる。</summary>
        protected abstract void WriteOutputFrame();

        /// <summary>Bluetooth の入力レポートの CRC の不一致を、連続でいくつまで許すか（次の不一致で切断する）。</summary>
        protected const int CrcMaxConsecutiveFailures = 10;

        /// <summary>Bluetooth の入力レポートの CRC の検証の結果。</summary>
        protected enum BtCrcResult
        {
            /// <summary>一致した。連続の失敗の数は 0 に戻る。</summary>
            Valid,

            /// <summary>不一致。このフレームは捨てて、次の読み取りへ（連続の失敗の数を 1 つ増やした）。</summary>
            Skipped,

            /// <summary>不一致が上限を超えて続いた。切断の手順は機種が行う。</summary>
            LimitReached,
        }

        /// <summary>レポートの <paramref name="pos"/> から、リトルエンディアンの 32 ビットの CRC を読む。</summary>
        protected static uint ReadCrc32(byte[] report, int pos)
        {
            return report[pos] |
                (uint)(report[pos + 1] << 8) |
                (uint)(report[pos + 2] << 16) |
                (uint)(report[pos + 3] << 24);
        }

        /// <summary>
        /// Bluetooth の入力レポート（末尾 4 バイトが CRC32）を検証する。不一致のときは、欠落に気づけるようにパケット番号を進め、
        /// 連続の失敗が <paramref name="maxConsecutiveFailures"/> に達していなければ数を増やして <see cref="BtCrcResult.Skipped"/> を返す
        /// （待機中の印も戻す）。上限に達していれば <see cref="BtCrcResult.LimitReached"/>（ログと切断の手順は機種が行う）。
        /// </summary>
        protected BtCrcResult VerifyBtInputCrc(byte[] report, int maxConsecutiveFailures)
        {
            uint recvCrc32 = ReadCrc32(report, BT_INPUT_REPORT_CRC32_POS);
            int start = 0;
            int size = BT_INPUT_REPORT_CRC32_POS;
            uint calcCrc32 = ~Crc32Algorithm.CalculateFasterBT78Hash(ref HamSeed, ref report, ref start, ref size);
            if (recvCrc32 == calcCrc32)
            {
                inputReportErrorCount = 0;
                return BtCrcResult.Valid;
            }

            cState.PacketCounter = pState.PacketCounter + 1; // 欠落に気づけるよう、不一致でも進める
            if (inputReportErrorCount >= maxConsecutiveFailures)
            {
                return BtCrcResult.LimitReached;
            }

            inputReportErrorCount++;
            readWaitEv.Reset();
            return BtCrcResult.Skipped;
        }

        /// <summary>読み取りのループを終えるときの共通の後始末: 出力を止め、切断中の印を立て、切断（<c>Removal</c>）を通知し、監視スレッドを止める。</summary>
        protected void StopAndRemove()
        {
            StopOutputUpdate();
            isDisconnecting = true;
            RunRemoval();

            timeoutExecuted = true;
        }

        /// <summary>切断に気づけないとき（Windows が機器の消失を検出しないとき）に、機器へ 1 回の操作を起こして気づかせる。既定は何もしない。</summary>
        protected virtual void KickWindowsToNoticeDisconnect()
        {
        }

        /// <summary>
        /// Bluetooth の監視スレッドの本体。<see cref="InputDeviceCore.READ_STREAM_TIMEOUT"/> の間に 1 度も読み取れなかったら、
        /// <see cref="KickWindowsToNoticeDisconnect"/> を呼んで終わる。読み取りのたびに <c>timeoutEvent</c> を下ろす。
        /// </summary>
        protected void TimeoutTestThread()
        {
            while (!timeoutExecuted)
            {
                if (timeoutEvent)
                {
                    timeoutExecuted = true;
                    KickWindowsToNoticeDisconnect();
                }
                else
                {
                    timeoutEvent = true;
                    Thread.Sleep(READ_STREAM_TIMEOUT); // 待機の理由: 読み取りスレッドの中。読み取りの失敗後の再試行の間隔
                }
            }
        }

        /// <summary>背景スレッドを作って始める。優先度と名前は機種が決める。</summary>
        protected static Thread StartBackgroundThread(ThreadStart body, ThreadPriority priority, string name)
        {
            var thread = new Thread(body)
            {
                Priority = priority,
                Name = name,
                IsBackground = true,
            };
            thread.Start();
            return thread;
        }

        protected override void RunInputLoop() => ReadInput();

        /// <summary>読み取りのループの本体（スレッドから呼ぶ。テストでは直接呼べる）。</summary>
        protected internal void ReadInput()
        {
            firstActive = DateTime.UtcNow;
            hDevice.SetNumInputBuffers(3);
            var latencyWindow = new LatencyWindow();
            lastFrameTicks = 0;
            previousFrameTicks = 0;
            ds4InactiveFrame = true;
            idleInput = true;
            OnLoopStarting();

            while (!exitInputThread)
            {
                oldCharging = charging;
                frameError = string.Empty;
                Latency = latencyWindow.Add(lastTimeElapsed);

                // 読み取りで待つ間は「待機中」（HaltReportingRunAction が、この間に報告を止める）
                readWaitEv.Set();
                FrameResult received = ReceiveFrame();
                if (received == FrameResult.Disconnected)
                {
                    exitInputThread = true;
                    StopOutputUpdate();
                    isDisconnecting = true;
                    RunRemoval();
                    break;
                }

                if (received == FrameResult.Terminate)
                {
                    return;
                }

                if (received == FrameResult.Skip)
                {
                    continue;
                }

                long now = GetFrameTicks();
                lastTimeElapsedDouble = (now - lastFrameTicks) * (1.0 / Stopwatch.Frequency) * 1000.0;
                lastTimeElapsed = (long)lastTimeElapsedDouble;
                previousFrameTicks = lastFrameTicks;
                lastFrameTicks = now;
                // 出力遅延（Controller Readings）の計測の起点。機種ごとに持たず、全機種でここで受け取った時刻を残す
                lastInputReportTimestamp = now;

                if (!IsExpectedReport())
                {
                    continue;
                }

                utcNow = DateTime.UtcNow; // 時刻帯が変わっても影響しないよう UTC
                cState.PacketCounter = pState.PacketCounter + 1;
                cState.ReportTimeStamp = utcNow;
                ProcessFrame();

                if (ProcessIdle())
                {
                    return;
                }

                if (fireReport)
                {
                    Report?.Invoke(this, EventArgs.Empty);
                }

                WriteOutputFrame();

                if (!string.IsNullOrEmpty(frameError))
                    error = frameError;
                else if (!string.IsNullOrEmpty(error))
                    error = string.Empty;

                cState.CopyTo(pState);
                RunQueuedInputEvents();
            }

            timeoutExecuted = true;
        }
    }
}
