namespace DS4Windows.DI
{
    /// <summary>Phase9 9-E E-3（Phase9-E-Plan.md）: <see cref="IUdpMotionServer"/> を作る（作るだけで、待ち受けは <c>Start</c> まで始めない）。</summary>
    public interface IUdpMotionServerFactory
    {
        IUdpMotionServer Create(UdpPadDetailProvider getPadDetail);
    }
}
