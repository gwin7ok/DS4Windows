#nullable enable

namespace DS4Windows.Input.Devices
{
    /// <summary>機器のスロット番号（0〜14）を、Switch Pro の下側 4 つの LED の点灯のビット列へ変換する。状態を持たない。</summary>
    public static class SwitchProSlotMask
    {
        public static byte FromSlot(int slot)
        {
            // 1〜15 を、4 つの LED の点灯の組へ割り当てる
            switch (slot)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                    return (byte)(1 << slot);
                case 4:
                    return (byte)(0x01 | 0x02);
                case 5:
                    return (byte)(0x01 | 0x04);
                case 6:
                    return (byte)(0x01 | 0x08);

                case 7:
                    return (byte)(0x02 | 0x04);
                case 8:
                    return (byte)(0x02 | 0x08);

                case 9:
                    return (byte)(0x04 | 0x08);

                case 10:
                    return (byte)(0x01 | 0x02 | 0x04);
                case 11:
                    return (byte)(0x01 | 0x02 | 0x08);
                case 12:
                    return (byte)(0x01 | 0x04 | 0x08);

                case 13:
                    return (byte)(0x02 | 0x04 | 0x08);

                case 14:
                    return (byte)(0x01 | 0x02 | 0x04 | 0x08);
                default:
                    return (byte)(0x00);
            }
        }
    }
}
