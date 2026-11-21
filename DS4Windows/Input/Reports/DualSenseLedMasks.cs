#nullable enable

using DS4Windows.Core.Settings;

namespace DS4Windows.Input.Reports
{
    /// <summary>DualSense の LED（プレイヤー LED の 5 灯、ミュート LED）の点灯の値。状態を持たない。</summary>
    public static class DualSenseLedMasks
    {
        /// <summary>スロット番号（0〜6）を、左右対称の 5 灯の点灯のビット列にする。それ以外は消灯。</summary>
        public static byte FromSlot(int slot)
        {
            switch (slot)
            {
                case 0: return 0x04;
                case 1: return 0x02 | 0x08;
                case 2: return 0x01 | 0x04 | 0x10;
                case 3: return 0x01 | 0x02 | 0x08 | 0x10;
                case 4: return 0x01 | 0x10;
                case 5: return 0x02 | 0x04 | 0x08;
                case 6: return 0x01 | 0x02 | 0x04 | 0x08 | 0x10;
                default: return 0x00;
            }
        }

        /// <summary>電池の残量（％）を、プレイヤー LED の灯数にする。</summary>
        public static byte BatteryBar(int deviceBattery)
        {
            byte batteryMask;
            if (deviceBattery >= 95)
                batteryMask = 0x01 | 0x02 | 0x08 | 0x10;
            else if (deviceBattery >= 70)
                batteryMask = 0x01 | 0x02 | 0x08;
            else if (deviceBattery >= 50)
                batteryMask = 0x01 | 0x02;
            else if (deviceBattery >= 20)
                batteryMask = 0x01;
            else if (deviceBattery >= 5)
                batteryMask = 0x01 | 0x02 | 0x04;
            else
                batteryMask = 0x00;

            return batteryMask;
        }

        public static byte MuteLed(DualSenseControllerOptions.MuteLEDMode mode)
        {
            switch (mode)
            {
                case DualSenseControllerOptions.MuteLEDMode.On: return 0x01;
                case DualSenseControllerOptions.MuteLEDMode.Pulse: return 0x02;
                default: return 0x00;
            }
        }
    }
}
