#nullable enable
namespace DS4WindowsTests
{
    /// <summary>
    /// DS4・DualSense の Bluetooth の入力レポートの CRC32。Linux カーネル（<c>hid-playstation.c</c> の <c>ps_check_crc32</c>）と同じ手順を、
    /// 本番の <c>Crc32Algorithm</c> を使わずに書いた独立の実装（初期値 0xFFFFFFFF、種のバイト 0xA1 を先に流し、本体を流し、最後に反転、下位バイトから格納）。
    /// </summary>
    internal static class PsReportCrc
    {
        /// <summary>PS_INPUT_CRC32_SEED。</summary>
        public const byte InputSeed = 0xA1;

        /// <summary>先頭の <paramref name="size"/> − 4 バイトの CRC を、末尾の 4 バイトへ書く。</summary>
        public static void Seal(byte[] report, int size)
        {
            uint crc = 0xFFFFFFFF;
            crc = Step(crc, InputSeed);
            for (int i = 0; i < size - 4; i++) crc = Step(crc, report[i]);
            crc = ~crc;
            report[size - 4] = (byte)crc;
            report[size - 3] = (byte)(crc >> 8);
            report[size - 2] = (byte)(crc >> 16);
            report[size - 1] = (byte)(crc >> 24);
        }

        private static uint Step(uint c, byte b)
        {
            c ^= b;
            for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? (c >> 1) ^ 0xEDB88320u : c >> 1;
            return c;
        }
    }
}
