using System;
using System.Collections.Generic;
using System.Threading;
using DS4Windows;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.8f-3: マウスの移動とアナログのホイールのテスト用ハーネス。実機がないため、スロットにテスト用のデバイス（経過時間つき）を置く。
    /// ホイールの 10ms の間隔は、旧経路は <c>Mapping.oldnow</c> をフレームごとに戻し、新経路は束縛ごとの時刻のため、フレームの間に 12ms 待って、どちらも決定的にする。
    /// ホイールと移動の端数（Mapping が共有）は、作成時に初期化する。
    /// </summary>
    public sealed class AnalogMouseTestHarness : IDisposable
    {
        private readonly TestDs4Device _device = TestDs4Device.Create();
        private readonly DS4Device _savedDevice;

        public AnalogMouseTestHarness(double elapsedMs = 5.0)
        {
            T = new ControlsKeyTestScope();
            _savedDevice = T.Scope.Control.DS4Controllers[T.Device];
            _device.lastTimeElapsedDouble = elapsedMs;
            T.Scope.Control.DS4Controllers[T.Device] = _device;
            ResetSharedState();
        }

        public ControlsKeyTestScope T { get; }

        public List<(int X, int Y)> Moves => T.Scope.Recorder.MoveEvents;

        public List<(int Vertical, int Horizontal)> Wheels => T.Scope.Recorder.WheelEvents;

        public DS4State Step(DS4State input)
        {
            Thread.Sleep(12);
            T.Mapping.oldnow = DateTime.MinValue;   // 旧経路の 10ms の間隔を満たした状態にする
            return T.Run(input);
        }

        private void ResetSharedState()
        {
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            void Set(string name, object value) => typeof(Mapping).GetField(name, flags)?.SetValue(T.Mapping, value);
            Set("stickWheelRemainder", 0.0);
            Set("stickWheel", 0.0);
            Set("stickWheelDownDir", false);
            Set("pressagain", false);
            // Phase9 9-B: 旧 horizontalRemainder／verticalRemainder は MouseMotionQuantizer（TestMappingFactory が独立の instance を渡す）へ移ったため、作成時は 0 から始まる。
        }

        public void Dispose()
        {
            T.Scope.Control.DS4Controllers[T.Device] = _savedDevice;
            T.Dispose();
        }
    }
}
