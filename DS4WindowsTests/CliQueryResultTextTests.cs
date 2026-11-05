using System;
using System.Text;
using DS4Windows.Composition.Startup;
using DS4Windows.Core.Platform;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 不具合の修正（2026-11-05）: CLI の問い合わせ（<c>-command Query…</c>）の結果で、日本語などの 2 バイト文字が「??」に化けた、
    /// <c>|more</c> なしでは表示されなかった。共有メモリの変換（<see cref="IpcResultText"/>）と、出力の文字コードの選び方（<see cref="CliResultConsoleWriter.SelectEncoding"/>）を検査する。
    /// </summary>
    public class CliQueryResultTextTests
    {
        [Theory]
        [InlineData("DS4_for_gwin")]
        [InlineData("日本語DS4_for_gwin")]
        [InlineData("プロファイル①😀")]
        [InlineData("")]
        public void EncodeAndDecode_RoundTripsThroughTheSharedMemory(string text)
        {
            byte[] shared = new byte[IpcResultText.BufferSize];   // 共有メモリ（新規は 0 で埋まっている）
            byte[] encoded = IpcResultText.Encode(text);
            Array.Copy(encoded, shared, encoded.Length);

            Assert.Equal(text, IpcResultText.Decode(shared));
        }

        [Fact]
        public void AsciiResults_AreTheSameBytesAsBefore()
        {
            Assert.Equal(Encoding.ASCII.GetBytes("Attached"), IpcResultText.Encode("Attached"));
        }

        [Fact]
        public void LongResults_AreCutAtACharacterBoundary_AndKeepTheTerminator()
        {
            string text = new string('あ', 100);   // UTF-8 で 300 バイト
            byte[] encoded = IpcResultText.Encode(text);

            Assert.True(encoded.Length <= IpcResultText.MaxTextBytes);
            Assert.Equal(0, encoded.Length % 3);   // 3 バイトの文字の途中で切っていない
            Assert.Equal(new string('あ', encoded.Length / 3), Encoding.UTF8.GetString(encoded));
        }

        [Fact]
        public void Decode_WithoutATerminator_UsesTheWholeBuffer()
        {
            byte[] full = Encoding.ASCII.GetBytes(new string('x', IpcResultText.BufferSize));
            Assert.Equal(new string('x', IpcResultText.BufferSize), IpcResultText.Decode(full));
        }

        [Theory]
        [InlineData(false, 0u, false)]        // GUI アプリを cmd から |more なしで起動: ハンドルが無い → 起動元のコンソールへ（再確認で発覚した不具合の場合）
        [InlineData(true, 0x0002u, false)]    // コンソール（文字デバイス）
        [InlineData(true, 0x0003u, true)]     // パイプ（|more、findstr、PowerShell の取り込み）
        [InlineData(true, 0x0001u, true)]     // ファイル（> out.txt）
        [InlineData(true, 0x8003u, true)]     // リモートのパイプ
        public void IsRedirectedOutput_TreatsAMissingHandleAsTheConsole(bool hasHandle, uint fileType, bool expected)
        {
            Assert.Equal(expected, CliResultConsoleWriter.IsRedirectedOutput(hasHandle, fileType));
        }

        [Fact]
        public void SelectEncoding_UsesTheConsoleCodePage_WithoutABom()
        {
            Encoding sjis = CliResultConsoleWriter.SelectEncoding(932);
            Assert.Equal(932, sjis.CodePage);
            Assert.Empty(sjis.GetPreamble());
            Assert.Equal("日本語", sjis.GetString(sjis.GetBytes("日本語")));
        }

        [Theory]
        [InlineData(0u)]       // コンソールが無い
        [InlineData(65001u)]   // chcp 65001
        [InlineData(999999u)]  // 使えないコードページ
        public void SelectEncoding_FallsBackToUtf8WithoutABom(uint codePage)
        {
            Encoding encoding = CliResultConsoleWriter.SelectEncoding(codePage);
            Assert.Equal(65001, encoding.CodePage);
            Assert.Empty(encoding.GetPreamble());
        }
    }
}
