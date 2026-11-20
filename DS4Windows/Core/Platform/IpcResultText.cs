using System;
using System.Text;

namespace DS4Windows.Core.Platform
{
    /// <summary>
    /// CLI の問い合わせ（<c>-command Query…</c>）の結果を、共有メモリ（MMF。256 バイト）で受け渡すときの文字列 ⇔ バイト列の変換（状態なし）。
    /// 不具合の修正（2026-11-05）: 書き込み側・読み取り側とも ASCII で変換していたため、日本語などの非 ASCII の文字（プロファイル名など）が「?」に化けていた。UTF-8 にした。
    /// ASCII だけの結果のバイト列は、従来と同じ。
    /// </summary>
    public static class IpcResultText
    {
        /// <summary>共有メモリの大きさ（バイト）。</summary>
        public const int BufferSize = 256;

        /// <summary>終端の 0 を必ず残すため、本文は最大 255 バイト。</summary>
        public const int MaxTextBytes = BufferSize - 1;

        /// <summary>UTF-8 のバイト列にする。255 バイトを超える場合は、文字の途中で切らないよう、文字の境目で切り詰める。</summary>
        public static byte[] Encode(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text ?? string.Empty);
            if (bytes.Length <= MaxTextBytes)
                return bytes;

            int length = MaxTextBytes;
            // bytes[length] は切り捨てる側の先頭。それが継続バイト（10xxxxxx）なら、文字の先頭まで戻る
            while (length > 0 && (bytes[length] & 0xC0) == 0x80)
                length--;

            byte[] cut = new byte[length];
            Array.Copy(bytes, cut, length);
            return cut;
        }

        /// <summary>共有メモリの内容（最初の 0 まで）を UTF-8 として文字列にする。0 が無ければ全体を使う。</summary>
        public static string Decode(byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0)
                return string.Empty;

            int end = Array.IndexOf(buffer, (byte)0);
            if (end < 0)
                end = buffer.Length;

            return Encoding.UTF8.GetString(buffer, 0, end);
        }
    }
}
