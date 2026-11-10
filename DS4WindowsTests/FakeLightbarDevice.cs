using System;
using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Input.Abstractions;
using DS4Windows.Processing.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>ライトバーの色の計算のテスト用。入力の値を持ち、最後に設定された状態を記録する。</summary>
    internal sealed class FakeLightbarDevice : ILightbarDevice
    {
        public bool IsCharging { get; set; }
        public int Battery { get; set; } = 100;
        public ConnectionType ConnectionType { get; set; } = ConnectionType.USB;
        public DateTime LastActive { get; set; } = DateTime.UtcNow;
        public byte LeftHeavySlowRumble { get; set; }
        public byte LightBarOnDuration { get; set; }
        public int SetCount { get; private set; }
        public DS4LightbarState Last;

        public void SetLightbarState(ref DS4LightbarState lightState)
        {
            SetCount++;
            Last = lightState;
        }
    }
}
