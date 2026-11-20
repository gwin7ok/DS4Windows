namespace DS4Windows.Processing.Abstractions
{
    /// <summary><see cref="IUdpMotionServer"/> を作る（作るだけで、待ち受けは <c>Start</c> まで始めない）。</summary>
    public interface IUdpMotionServerFactory
    {
        IUdpMotionServer Create(UdpPadDetailProvider getPadDetail);
    }
}
