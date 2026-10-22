using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>Phase9 9-E E-2: <see cref="IHidHideApiFactory"/> の実装（DI の Singleton。状態は持たない）。呼ぶたびに制御デバイスを開く。</summary>
    public sealed class HidHideApiFactory : IHidHideApiFactory
    {
        public IHidHideApi Open()
        {
            return new HidHideApiAdapter();
        }
    }
}
