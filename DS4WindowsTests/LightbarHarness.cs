using System;
using System.Diagnostics;
using System.Threading;
using DS4Windows;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.8e-W1: Controls タブの Lightbar マクロの特性化の土台。旧経路は、MapCustom の引数ではなく、デバイスの生の前回・現在の状態
    /// （<c>GetRawPreviousStateRef</c>／<c>GetRawCurrentStateRef</c>）の変化で判定するため、<see cref="TestDs4Device"/> に生の状態を設定して流す。
    /// 終了時に、マクロのタスクが終わる（強制色が戻る）のを待つ。
    /// </summary>
    public sealed class LightbarHarness : IDisposable
    {
        private readonly TestDs4Device _device = TestDs4Device.Create();
        private DS4State _previousRaw = ControlsKeyTestScope.Neutral();

        public LightbarHarness(bool migrated)
        {
            T = new ControlsKeyTestScope(migrated);
            T.Scope.Control.DS4Controllers[T.Device] = _device;
            _device.SetRawStates(_previousRaw, _previousRaw);
        }

        public ControlsKeyTestScope T { get; }

        private static DS4State Frame(bool cross)
        {
            DS4State state = ControlsKeyTestScope.Neutral();
            state.Cross = cross;
            return state;
        }

        /// <summary>1 フレーム流す。<paramref name="rawCross"/> を省略すると、引数の状態と生の状態を同じにする。</summary>
        public DS4State Step(bool cross, bool? rawCross = null)
        {
            DS4State raw = Frame(rawCross ?? cross);
            _device.SetRawStates(_previousRaw, raw);
            DS4State output = T.Run(Frame(cross));
            _previousRaw = raw;
            return output;
        }

        /// <summary>条件が成り立つまで、10ms おきに（フレームは流さずに）待つ。成り立てば真。</summary>
        public static bool WaitUntil(Func<bool> condition, int timeoutMilliseconds)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMilliseconds)
            {
                if (condition()) return true;
                Thread.Sleep(5);
            }

            return condition();
        }

        public void Dispose()
        {
            // 走行中のマクロは、強制色を戻して終わる。終わるのを待ってから、デバイスを外す
            WaitUntil(() => !T.ForceLight, 3000);
            T.Dispose();
            T.Scope.Control.DS4Controllers[T.Device] = null;
        }
    }
}
