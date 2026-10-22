using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>Phase9 9-E E-3: <see cref="IUdpMotionServerFactory"/> の実装（DI の Singleton。状態は持たない）。</summary>
    public sealed class UdpMotionServerFactory : IUdpMotionServerFactory
    {
        public IUdpMotionServer Create(UdpPadDetailProvider getPadDetail)
        {
            return new UdpMotionServerAdapter(getPadDetail);
        }
    }
}
