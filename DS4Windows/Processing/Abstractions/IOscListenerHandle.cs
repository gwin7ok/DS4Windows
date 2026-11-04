namespace DS4Windows.DI
{
    /// <summary>Phase9 9-E E-1（Phase9-E-Plan.md）: 動作中の OSC リスナー（受信側）。閉じるだけの窓口。</summary>
    public interface IOscListenerHandle
    {
        void Close();
    }
}
