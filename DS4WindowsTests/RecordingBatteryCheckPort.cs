using System.Collections.Generic;
using DS4Windows;
using DS4Windows.Actions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.6c: <see cref="IBatteryCheckPort"/> の記録用の実装。呼び出しを順に記録し、実機のコントローラー・ライトバー・通知には触れない。
    /// <see cref="Record"/> を偽にすると、記録せず（割り当てなし）、色の書き込みの回数だけを数える。
    /// </summary>
    public sealed class RecordingBatteryCheckPort : IBatteryCheckPort
    {
        public bool Present { get; set; } = true;
        public bool Record { get; set; } = true;
        public DS4Color LightBar { get; set; } = new DS4Color(10, 20, 30);
        public int Battery { get; set; } = 50;
        public readonly List<string> Events = new List<string>();
        public readonly List<DS4Color> Colors = new List<DS4Color>();
        public int ColorWrites { get; private set; }

        public bool HasController(int deviceIndex) => Present;

        public void NotifyBattery(int deviceIndex)
        {
            if (Record) Events.Add("notify:" + deviceIndex);
        }

        public DS4Color GetLightBarColor(int deviceIndex) => LightBar;

        public int GetBattery(int deviceIndex) => Battery;

        public void SetForceLight(int deviceIndex, bool on)
        {
            if (Record) Events.Add(on ? "force:on" : "force:off");
        }

        public void SetForcedColor(int deviceIndex, DS4Color color)
        {
            ColorWrites++;
            if (Record)
            {
                Events.Add("color");
                Colors.Add(color);
            }
        }
    }
}
