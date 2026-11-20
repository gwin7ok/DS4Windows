using System;

namespace DS4Windows.Processing.Actions
{
    /// <summary>
    /// Adapter that wraps existing <see cref="RepeatHelper"/> instances
    /// and exposes the <see cref="IRepeater"/> interface.
    /// Note: <see cref="RepeatHelper"/> encapsulates SyntheticDispatcher-based
    /// key sends（間隔・最初の待ちも自前で持つ）ため、<see cref="IRepeater.Start"/> は引数を取らない。
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

        public void Start()
        {
            try
            {
                // 差し替えの工場が渡されていれば、IRepeater の包みを優先して作る
                if (repeaterFactoryOverride != null)
                {
                    try
                    {
                        helperIRepeater ??= repeaterFactoryOverride(helperFactory);
                        helperIRepeater?.Start();
                        return;
                    }
                    catch (Exception)
                    {
                        DS4Windows.Core.Logging.AppLogger.LogTrace("RepeatHelperToIRepeaterAdapter: override repeater failed; falling back to RepeatHelper");
                    }
                }

                if (helper == null && helperFactory != null)
                {
                    helper = helperFactory();
                }

                helper?.Start();
            }
            catch (Exception)
            {
                // 連打の開始の失敗は、呼び出し元（入力の処理）へ伝えない
                DS4Windows.Core.Logging.AppLogger.LogTrace("RepeatHelperToIRepeaterAdapter: start failed");
            }
        }

        public void Stop()
        {
            try
            {
                if (helperIRepeater != null) try { helperIRepeater.Stop(); } catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
                else try { helper?.Stop(); } catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
            }
            catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
        }

        public void Dispose()
        {
            try
            {
                try { helperIRepeater?.Dispose(); } catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
                try { helper?.Dispose(); } catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
                helperIRepeater = null;
                helper = null;
            }
            catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
        }
    }
}
