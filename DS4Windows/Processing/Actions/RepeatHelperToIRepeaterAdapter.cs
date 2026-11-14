using System;

namespace DS4Windows.Processing.Actions
{
    /// <summary>
    /// Adapter that wraps existing <see cref="RepeatHelper"/> instances
    /// and exposes the <see cref="IRepeater"/> interface.
    /// Note: <see cref="RepeatHelper"/> encapsulates SyntheticDispatcher-based
    /// key sends and does not accept an external tick Action. If an external
    /// tick action is supplied to Start(...) it will be ignored and a trace
    /// will be emitted.
    /// </summary>
    public class RepeatHelperToIRepeaterAdapter : IRepeater
    {
        private RepeatHelper helper;
        private IRepeater helperIRepeater;
        private readonly Func<RepeatHelper> helperFactory;

        // 差し替え用の工場（主にテスト）。null でなければ、元の RepeatHelper の代わりにこれが IRepeater を作る。
        // 引数は、元の RepeatHelper を作る工場。
        private readonly Func<Func<RepeatHelper>, IRepeater> repeaterFactoryOverride;

        public RepeatHelperToIRepeaterAdapter(RepeatHelper existing)
        {
            helper = existing ?? throw new ArgumentNullException(nameof(existing));
        }

        public RepeatHelperToIRepeaterAdapter(Func<RepeatHelper> factory, Func<Func<RepeatHelper>, IRepeater> repeaterFactoryOverride = null)
        {
            helperFactory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.repeaterFactoryOverride = repeaterFactoryOverride;
        }

        public void Start(TimeSpan initialDelay, TimeSpan interval, System.Action tickAction)
        {
            try
            {
                if (tickAction != null)
                {
                    try { DS4Windows.Core.Logging.AppLogger.LogTrace("RepeatHelperToIRepeaterAdapter: supplied tickAction will be ignored; using RepeatHelper behavior"); } catch { }
                }

                // 差し替えの工場が渡されていれば、IRepeater の包みを優先して作る
                if (repeaterFactoryOverride != null)
                {
                    try
                    {
                        helperIRepeater ??= repeaterFactoryOverride(helperFactory);
                        helperIRepeater?.Start(initialDelay, interval, tickAction);
                        return;
                    }
                    catch { }
                }

                if (helper == null && helperFactory != null)
                {
                    helper = helperFactory();
                }

                helper?.Start();
            }
            catch { }
        }

        public void Stop()
        {
            try
            {
                if (helperIRepeater != null) try { helperIRepeater.Stop(); } catch { }
                else try { helper?.Stop(); } catch { }
            }
            catch { }
        }

        public void Dispose()
        {
            try
            {
                try { helperIRepeater?.Dispose(); } catch { }
                try { helper?.Dispose(); } catch { }
                helperIRepeater = null;
                helper = null;
            }
            catch { }
        }
    }
}
