#nullable enable

using System;
using DS4Windows.Core.Logging;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// <see cref="AppLogger"/> は呼び出し側へ例外を出さない（ログを包む <c>try</c>／<c>catch</c> を呼び出し側から外せる根拠。PhaseRV2-2）。
    /// 画面の購読者が例外を投げても、<c>Log</c>・<c>LogUser</c>・短縮形は例外を出さない。
    /// このテストは、静的な <c>GuiLog</c> へ一時的に購読するため、終わりに必ず外す。
    /// </summary>
    public class AppLoggerNeverThrowsTests
    {
        [Fact]
        public void ASubscriberThatThrows_DoesNotReachTheCaller()
        {
            EventHandler<DebugEventArgs> bad = (s, e) => throw new InvalidOperationException("subscriber failure");
            AppLogger.GuiLog += bad;
            try
            {
                var ex = Record.Exception(() =>
                {
                    AppLogger.Log(AppLogLevel.Info, "x", LogOptions.UserFacing);
                    AppLogger.LogUser(AppLogLevel.Info, "Log_DoesNotExist_{0}", 1);
                    AppLogger.LogInfo("y");
                    AppLogger.LogWarn("z");
                });
                Assert.Null(ex);
            }
            finally
            {
                AppLogger.GuiLog -= bad;
            }
        }

        [Fact]
        public void ABadFormatString_DoesNotReachTheCaller()
        {
            Assert.Null(Record.Exception(() => AppLogger.LogUser(AppLogLevel.Info, "{", 1, 2)));
        }
    }
}
