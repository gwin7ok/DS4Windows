using System.Collections.Generic;
using DS4Windows.Input.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>テスト用の <see cref="IXInputApi"/>。番号ごとの状態を台本で決め、振動の設定を記録する。状態のない番号は「接続されていない」になる。</summary>
    internal sealed class FakeXInputApi : IXInputApi
    {
        private readonly object sync = new object();
        private readonly Dictionary<int, XInputSnapshot> states = new Dictionary<int, XInputSnapshot>();

        public List<(int Index, ushort Left, ushort Right)> Vibrations { get; } = new List<(int, ushort, ushort)>();

        public void Set(int index, XInputSnapshot state)
        {
            lock (sync) states[index] = state;
        }

        public void Disconnect(int index)
        {
            lock (sync) states.Remove(index);
        }

        /// <summary>状態を読んだ回数（番号を問わない）。</summary>
        public int StateReads { get; private set; }

        public bool TryGetState(int index, out XInputSnapshot state)
        {
            lock (sync)
            {
                StateReads++;
                return states.TryGetValue(index, out state);
            }
        }

        /// <summary>電池の台本（番号ごと）。なければ「読めない」になる。</summary>
        public Dictionary<int, XInputBattery> Batteries { get; } = new Dictionary<int, XInputBattery>();

        public bool TryGetBattery(int index, out XInputBattery battery)
        {
            lock (sync) return Batteries.TryGetValue(index, out battery);
        }

        public bool TrySetVibration(int index, ushort leftMotor, ushort rightMotor)
        {
            lock (sync)
            {
                Vibrations.Add((index, leftMotor, rightMotor));
                return states.ContainsKey(index);
            }
        }
    }
}
