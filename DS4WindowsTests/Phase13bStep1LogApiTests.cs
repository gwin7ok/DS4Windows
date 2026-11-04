using System.Collections.Generic;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase13b-Step1: 新 API <c>AppLogger.Log</c> と、旧 <c>LogToGui</c> の対応表（Phase13b-Plan.md §4）を固定する。
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
        [InlineData(false, false, AppLogLevel.Info, false)]
        [InlineData(true, false, AppLogLevel.Warn, false)]
        [InlineData(false, true, AppLogLevel.Info, true)]
        [InlineData(true, true, AppLogLevel.Warn, true)]
        public void LogToGui_MapsToUserFacingLog(bool warning, bool temporary, AppLogLevel level, bool expectTemporary)
        {
            var got = Capture(() => AppLogger.LogToGui("P13b-msg", warning, temporary));

            var e = Assert.Single(got);
            Assert.Equal("P13b-msg", e.Data);
            Assert.Equal(warning, e.Warning);
            Assert.Equal(expectTemporary, e.Temporary);
            Assert.Equal(level, e.Level);
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
