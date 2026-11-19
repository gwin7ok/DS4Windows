#nullable enable
using System;
using System.Threading;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>サービスの開始・停止の呼び出しを数える偽物（画面の ViewModel のコマンドのテスト用）。<see cref="OnCall"/> で、呼び出し中の動き（待機・例外）を差し込める。</summary>
    public sealed class FakeServiceLifecycle : IServiceLifecycle
    {
        public bool Running { get; set; }
        public bool LoopControllers { get; set; }
        public bool InServiceTask { get; set; }
        public int ActiveControllers { get; set; }
        public bool Suspending { get; set; }
        public int StartCalls;
        public int StopCalls;
        public bool LastStopImmediateUnplug;
        public Action? OnCall { get; set; }
#pragma warning disable CS0067
        public event EventHandler? ServiceStarted;
        public event EventHandler? PreServiceStop;
        public event EventHandler? ServiceStopped;
        public event EventHandler? RunningChanged;
        public event Action<InputDeviceCore, int>? HotplugController;
#pragma warning restore CS0067

        public bool Start(bool showlog = true)
        {
            Interlocked.Increment(ref StartCalls);
            OnCall?.Invoke();
            Running = true;
            return true;
        }

        public bool Stop(bool showlog = true, bool immediateUnplug = false)
        {
            Interlocked.Increment(ref StopCalls);
            LastStopImmediateUnplug = immediateUnplug;
            OnCall?.Invoke();
            Running = false;
            return true;
        }

        public bool HotPlug() => true;

        public void PrepareAbort() { }
    }
}
