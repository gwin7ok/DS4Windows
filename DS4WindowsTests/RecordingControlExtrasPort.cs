using System.Collections.Generic;
using DS4Windows;
using DS4Windows.Actions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.8e: テスト用の <see cref="IControlExtrasPort"/>。振動の呼び出しだけを記録して（実機がないため、転送しない）、
    /// ライトバーの強制色と感度は、本番の窓口（<see cref="DefaultControlExtrasPort"/>）へ転送する（旧経路と同じ静的な状態に書く）。
    /// </summary>
    public sealed class RecordingControlExtrasPort : IControlExtrasPort
    {
        private readonly IControlExtrasPort _inner;

        public RecordingControlExtrasPort(IControlExtrasPort inner)
        {
            _inner = inner;
        }

        public List<(int Device, byte Heavy, byte Light)> RumbleCalls { get; } = new List<(int, byte, byte)>();

        /// <summary>最後の振動の呼び出しが、0 でないか（旧経路の <c>extrasRumbleActive</c> に相当）。</summary>
        public bool RumbleOn { get; private set; }

        public void SetRumble(int deviceIndex, byte heavy, byte light)
        {
            RumbleCalls.Add((deviceIndex, heavy, light));
            RumbleOn = heavy != 0 || light != 0;
        }

        public void ForceLightbar(int deviceIndex, DS4Color color, byte flash) => _inner.ForceLightbar(deviceIndex, color, flash);

        public void ClearForcedLightbar(int deviceIndex) => _inner.ClearForcedLightbar(deviceIndex);

        public void TrySetTemporarySensitivity(int deviceIndex, int sensitivity) => _inner.TrySetTemporarySensitivity(deviceIndex, sensitivity);

        public void RestoreSensitivity(int deviceIndex) => _inner.RestoreSensitivity(deviceIndex);
    }
}
