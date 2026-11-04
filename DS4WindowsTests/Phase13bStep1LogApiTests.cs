using System.Collections.Generic;
using Xunit;
using DS4Windows.Core.Logging;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase13b-Step1: 新 API <c>AppLogger.Log</c> との出力（レベル、利用者向け、一時表示）を固定する。旧 <c>LogToGui</c> は Phase13b-Step9 で削除した。
    /// GuiLog は静的イベントのため、他のテストと並行しないよう同じコレクションに入れる。
    /// </summary>
    [Collection("GuiLogSubscribers")]
    public class Phase13bStep1LogApiTests
    {
        private static List<DebugEventArgs> Capture(System.Action act)
        {
            var got = new List<DebugEventArgs>();
            System.EventHandler<DebugEventArgs> sink = (s, e) => { if (e.Data != null && e.Data.StartsWith("P13b-")) got.Add(e); };
            AppLogger.GuiLog += sink;
            try { act(); }
            finally { AppLogger.GuiLog -= sink; }
            return got;
        }

        [Theory]
        [InlineData(AppLogLevel.Info, false)]
        [InlineData(AppLogLevel.Warn, true)]
        [InlineData(AppLogLevel.Error, true)]
        public void Log_UserFacing_FiresGuiLogWithLevel(AppLogLevel level, bool expectWarning)
        {
            var got = Capture(() => AppLogger.Log(level, "P13b-m", LogOptions.UserFacing));

            var e = Assert.Single(got);
            Assert.Equal(level, e.Level);
            Assert.Equal(expectWarning, e.Warning);
            Assert.False(e.Temporary);
        }

        [Fact]
        public void Log_Temporary_FiresGuiLogAsTemporary()
        {
            var e = Assert.Single(Capture(() => AppLogger.Log(AppLogLevel.Info, "P13b-m", LogOptions.Temporary)));
            Assert.True(e.Temporary);
        }

        [Fact]
        public void Log_WithoutUserFacing_DoesNotFireGuiLog()
        {
            var got = Capture(() =>
            {
                AppLogger.Log(AppLogLevel.Warn, "P13b-a");
                AppLogger.Log(AppLogLevel.Error, "P13b-b", LogOptions.FileOnly);
                AppLogger.LogTrace("P13b-c");
                AppLogger.LogDebug("P13b-d");
                AppLogger.LogInfo("P13b-e");
                AppLogger.LogWarn("P13b-f");
                AppLogger.LogError("P13b-g");
            });

            Assert.Empty(got);
        }

        [Fact]
        public void ToNLogLevel_MapsEveryLevel()
        {
            Assert.Equal(NLog.LogLevel.Trace, AppLogger.ToNLogLevel(AppLogLevel.Trace));
            Assert.Equal(NLog.LogLevel.Debug, AppLogger.ToNLogLevel(AppLogLevel.Debug));
            Assert.Equal(NLog.LogLevel.Info, AppLogger.ToNLogLevel(AppLogLevel.Info));
            Assert.Equal(NLog.LogLevel.Warn, AppLogger.ToNLogLevel(AppLogLevel.Warn));
            Assert.Equal(NLog.LogLevel.Error, AppLogger.ToNLogLevel(AppLogLevel.Error));
        }

        [Fact]
        public void LegacyDebugEventArgs_DeriveLevelFromWarning()
        {
            Assert.Equal(AppLogLevel.Info, new DebugEventArgs("x", false).Level);
            Assert.Equal(AppLogLevel.Warn, new DebugEventArgs("x", true).Level);
        }
    }
}
