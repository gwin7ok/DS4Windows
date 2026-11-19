#nullable enable

using System;
using System.Linq;

namespace DS4Windows.Input.Reports
{
    /// <summary>
    /// Switch Pro の振動データの組み立て。左右の強さの比（0〜1）を、振幅の表から引いた 4 バイトずつ（周波数は 320 Hz 固定）にして、
    /// 振動データのバッファの 2〜9 バイトへ書く。状態を持たない（フレーム番号とレポート ID は、デバイスが書く）。
    /// 表は https://github.com/dekuNukem/Nintendo_Switch_Reverse_Engineering/blob/master/rumble_data_table.md の値。
    /// </summary>
    public static class SwitchProRumbleEncoder
    {
        private sealed class RumbleTableData
        {
            public byte high;
            public ushort low;
            public int amp;

            public RumbleTableData(byte high, ushort low, int amp)
            {
                this.high = high;
                this.low = low;
                this.amp = amp;
            }
        }

        private const int AMP_REAL_MIN = 0;
        //private const int AMP_REAL_MAX = 1003;
        //private const int AMP_LIMIT_MAX = 404;
        private const int AMP_LIMIT_MAX = 800;

        private static RumbleTableData[] fixedRumbleTable = new RumbleTableData[]
        {
            new RumbleTableData(high: 0x00, low: 0x0040, amp: 0),
            new RumbleTableData(high: 0x02, low: 0x8040, amp: 10), new RumbleTableData(high: 0x04, low: 0x0041, amp: 12), new RumbleTableData(high: 0x06, low: 0x8041, amp: 14),
            new RumbleTableData(high: 0x08, low: 0x0042, amp: 17), new RumbleTableData(high: 0x0A, low: 0x8042, amp: 20), new RumbleTableData(high: 0x0C, low: 0x0043, amp: 24),
            new RumbleTableData(high: 0x0E, low: 0x8043, amp: 28), new RumbleTableData(high: 0x10, low: 0x0044, amp: 33), new RumbleTableData(high: 0x12, low: 0x8044, amp: 40),
            new RumbleTableData(high: 0x14, low: 0x0045, amp: 47), new RumbleTableData(high: 0x16, low: 0x8045, amp: 56), new RumbleTableData(high: 0x18, low: 0x0046, amp: 67),
            new RumbleTableData(high: 0x1A, low: 0x8046, amp: 80), new RumbleTableData(high: 0x1C, low: 0x0047, amp: 95), new RumbleTableData(high: 0x1E, low: 0x8047, amp: 112),
            new RumbleTableData(high: 0x20, low: 0x0048, amp: 117), new RumbleTableData(high: 0x22, low: 0x8048, amp: 123), new RumbleTableData(high: 0x24, low: 0x0049, amp: 128),
            new RumbleTableData(high: 0x26, low: 0x8049, amp: 134), new RumbleTableData(high: 0x28, low: 0x004A, amp: 140), new RumbleTableData(high: 0x2A, low: 0x804A, amp: 146),
            new RumbleTableData(high: 0x2C, low: 0x004B, amp: 152), new RumbleTableData(high: 0x2E, low: 0x804B, amp: 159), new RumbleTableData(high: 0x30, low: 0x004C, amp: 166),
            new RumbleTableData(high: 0x32, low: 0x804C, amp: 173), new RumbleTableData(high: 0x34, low: 0x004D, amp: 181), new RumbleTableData(high: 0x36, low: 0x804D, amp: 189),
            new RumbleTableData(high: 0x38, low: 0x004E, amp: 198), new RumbleTableData(high: 0x3A, low: 0x804E, amp: 206), new RumbleTableData(high: 0x3C, low: 0x004F, amp: 215),
            new RumbleTableData(high: 0x3E, low: 0x804F, amp: 225), new RumbleTableData(high: 0x40, low: 0x0050, amp: 230), new RumbleTableData(high: 0x42, low: 0x8050, amp: 235),
            new RumbleTableData(high: 0x44, low: 0x0051, amp: 240), new RumbleTableData(high: 0x46, low: 0x8051, amp: 245), new RumbleTableData(high: 0x48, low: 0x0052, amp: 251),
            new RumbleTableData(high: 0x4A, low: 0x8052, amp: 256), new RumbleTableData(high: 0x4C, low: 0x0053, amp: 262), new RumbleTableData(high: 0x4E, low: 0x8053, amp: 268),
            new RumbleTableData(high: 0x50, low: 0x0054, amp: 273), new RumbleTableData(high: 0x52, low: 0x8054, amp: 279), new RumbleTableData(high: 0x54, low: 0x0055, amp: 286),
            new RumbleTableData(high: 0x56, low: 0x8055, amp: 292), new RumbleTableData(high: 0x58, low: 0x0056, amp: 298), new RumbleTableData(high: 0x5A, low: 0x8056, amp: 305),
            new RumbleTableData(high: 0x5C, low: 0x0057, amp: 311), new RumbleTableData(high: 0x5E, low: 0x8057, amp: 318), new RumbleTableData(high: 0x60, low: 0x0058, amp: 325),
            new RumbleTableData(high: 0x62, low: 0x8058, amp: 332), new RumbleTableData(high: 0x64, low: 0x0059, amp: 340), new RumbleTableData(high: 0x66, low: 0x8059, amp: 347),
            new RumbleTableData(high: 0x68, low: 0x005A, amp: 355), new RumbleTableData(high: 0x6A, low: 0x805A, amp: 362), new RumbleTableData(high: 0x6C, low: 0x005B, amp: 370),
            new RumbleTableData(high: 0x6E, low: 0x805B, amp: 378), new RumbleTableData(high: 0x70, low: 0x005C, amp: 387), new RumbleTableData(high: 0x72, low: 0x805C, amp: 395),
            new RumbleTableData(high: 0x74, low: 0x005D, amp: 404), new RumbleTableData(high: 0x76, low: 0x805D, amp: 413), new RumbleTableData(high: 0x78, low: 0x005E, amp: 422),
            new RumbleTableData(high: 0x7A, low: 0x805E, amp: 431), new RumbleTableData(high: 0x7C, low: 0x005F, amp: 440), new RumbleTableData(high: 0x7E, low: 0x805F, amp: 450),
            new RumbleTableData(high: 0x80, low: 0x0060, amp: 460), new RumbleTableData(high: 0x82, low: 0x8060, amp: 470), new RumbleTableData(high: 0x84, low: 0x0061, amp: 480),
            new RumbleTableData(high: 0x86, low: 0x8061, amp: 491), new RumbleTableData(high: 0x88, low: 0x0062, amp: 501), new RumbleTableData(high: 0x8A, low: 0x8062, amp: 512),
            new RumbleTableData(high: 0x8C, low: 0x0063, amp: 524), new RumbleTableData(high: 0x8E, low: 0x8063, amp: 535), new RumbleTableData(high: 0x90, low: 0x0064, amp: 547),
            new RumbleTableData(high: 0x92, low: 0x8064, amp: 559), new RumbleTableData(high: 0x94, low: 0x0065, amp: 571), new RumbleTableData(high: 0x96, low: 0x8065, amp: 584),
            new RumbleTableData(high: 0x98, low: 0x0066, amp: 596), new RumbleTableData(high: 0x9A, low: 0x8066, amp: 609), new RumbleTableData(high: 0x9C, low: 0x0067, amp: 623),
            new RumbleTableData(high: 0x9E, low: 0x8067, amp: 636), new RumbleTableData(high: 0xA0, low: 0x0068, amp: 650), new RumbleTableData(high: 0xA2, low: 0x8068, amp: 665),
            new RumbleTableData(high: 0xA4, low: 0x0069, amp: 679), new RumbleTableData(high: 0xA6, low: 0x8069, amp: 694), new RumbleTableData(high: 0xA8, low: 0x006A, amp: 709),
            new RumbleTableData(high: 0xAA, low: 0x806A, amp: 725), new RumbleTableData(high: 0xAC, low: 0x006B, amp: 741), new RumbleTableData(high: 0xAE, low: 0x806B, amp: 757),
            new RumbleTableData(high: 0xB0, low: 0x006C, amp: 773), new RumbleTableData(high: 0xB2, low: 0x806C, amp: 790), new RumbleTableData(high: 0xB4, low: 0x006D, amp: 808),
            new RumbleTableData(high: 0xB6, low: 0x806D, amp: 825), new RumbleTableData(high: 0xB8, low: 0x006E, amp: 843), new RumbleTableData(high: 0xBA, low: 0x806E, amp: 862),
            new RumbleTableData(high: 0xBC, low: 0x006F, amp: 881), new RumbleTableData(high: 0xBE, low: 0x806F, amp: 900), new RumbleTableData(high: 0xC0, low: 0x0070, amp: 920),
            new RumbleTableData(high: 0xC2, low: 0x8070, amp: 940), new RumbleTableData(high: 0xC4, low: 0x0071, amp: 960), new RumbleTableData(high: 0xC6, low: 0x8071, amp: 981),
            new RumbleTableData(high: 0xC8, low: 0x0072, amp: 1003),
        };

        private static RumbleTableData[] compiledRumbleTable = new Func<RumbleTableData[]>(() =>
        {
            RumbleTableData[] tmpBuffer = new RumbleTableData[fixedRumbleTable.Last().amp + 1];
            int currentOffset = 0;
            RumbleTableData previousEntry = fixedRumbleTable[0];
            tmpBuffer[currentOffset] = previousEntry;
            int currentAmp = previousEntry.amp + 1;
            currentOffset++;

            for (int i = 1; i < fixedRumbleTable.Length; i++)
            //foreach(RumbleTableData entry in fixedRumbleTable)
            {
                RumbleTableData entry = fixedRumbleTable[i];
                if (currentAmp < entry.amp)
                {
                    while (currentAmp < entry.amp)
                    {
                        tmpBuffer[currentOffset] = previousEntry;
                        currentOffset++;
                        currentAmp++;
                    }
                }

                tmpBuffer[currentOffset] = entry;
                currentAmp = entry.amp + 1;
                currentOffset++;
                previousEntry = entry;
            }

            return tmpBuffer;
        })();

        /// <summary>左右の強さの比から、バッファの 2〜9 バイト（左 4 バイト、右 4 バイト）を書く。比は 0〜1（上限は <c>AMP_LIMIT_MAX</c> で表の添字へ換算する）。</summary>
        public static void EncodeAmplitudes(byte[] buffer, double leftAmpRatio, double rightAmpRatio)
        {
            WriteMotor(buffer, 2, leftAmpRatio);
            WriteMotor(buffer, 6, rightAmpRatio);
        }

        private static void WriteMotor(byte[] buffer, int pos, double ampRatio)
        {
            ushort freq_data_high = 0x0001; // 320 Hz
            byte freq_data_low = 0x60; // 320 Hz
            int idx = (int)(ampRatio * AMP_LIMIT_MAX);
            RumbleTableData entry = compiledRumbleTable[idx];
            byte amp_high = entry.high;
            ushort amp_low = entry.low;

            // Slightly different bit shifts as HF and LF are used as written
            // in the frequency table rather than swapping bytes in advance.
            // Unconventional but it results in the same output
            buffer[pos] = (byte)((freq_data_high >> 8) & 0xFF);
            buffer[pos + 1] = (byte)((freq_data_high & 0xFF) + amp_high);
            buffer[pos + 2] = (byte)(freq_data_low + (amp_low >> 8) & 0xFF);
            buffer[pos + 3] = (byte)(amp_low & 0xFF);
        }
    }
}
