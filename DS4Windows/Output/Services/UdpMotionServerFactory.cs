
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Output.Services
{
    /// <summary><see cref="IUdpMotionServerFactory"/> の実装（DI の Singleton。状態は持たない）。</summary>
    public sealed class UdpMotionServerFactory : IUdpMotionServerFactory
    {
        public IUdpMotionServer Create(UdpPadDetailProvider getPadDetail)
        {
            return new UdpMotionServerAdapter(getPadDetail);
        }
    }
}
