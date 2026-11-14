using System;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Actions;
using NLog;
using NLog.Config;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// <see cref="DefaultActionManager.SetToggledOn"/> は、切り替えのたびに呼ばれる。
    /// スタックトレースの取得（高価）は、Trace のログが有効のときだけにする。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class DefaultActionManagerToggleAllocationTests
    {
        [Fact]
        public void WhenTraceLoggingIsDisabled_TogglingDoesNotCaptureStackTraces()
        {
            LoggingConfiguration saved = LogManager.Configuration;
            // ルールのない設定へ差し替えて、Trace を確実に無効にする（終了時に元へ戻す）
            LogManager.Configuration = new LoggingConfiguration();
            try
            {
                Assert.False(DS4Windows.Core.Logging.AppLogger.IsTraceEnabled);

                var manager = new DefaultActionManager(TestRepositoryFactory.MappingProvider);
                var action = new SpecialAction("toggle-alloc", "0", "Key", "0");
                manager.GetStateFor(action, 0);

                const int toggles = 2000;
                for (int i = 0; i < 10; i++) manager.SetToggledOn(action, 0, (i & 1) == 0); // 暖機

                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < toggles; i++) manager.SetToggledOn(action, 0, (i & 1) == 0);
                long perCall = (GC.GetAllocatedBytesForCurrentThread() - before) / toggles;

                // スタックトレースの取得は、1 回あたり数 KB を割り当てる。取得しなければ、数百バイト以下
                Assert.True(perCall < 512, $"1 回あたり {perCall} バイト");
            }
            finally
            {
                LogManager.Configuration = saved;
            }
        }
    }
}
