
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Output.Services
{
    /// <summary><see cref="IHidHideApiFactory"/> の実装（DI の Singleton。状態は持たない）。呼ぶたびに制御デバイスを開く。</summary>
    public sealed class HidHideApiFactory : IHidHideApiFactory
    {
        public IHidHideApi Open()
        {
            return new HidHideApiAdapter();
        }
    }
}
