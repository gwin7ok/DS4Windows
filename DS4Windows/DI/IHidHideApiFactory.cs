namespace DS4Windows.DI
{
    /// <summary>Phase9 9-E E-2（Phase9-E-Plan.md）: <see cref="IHidHideApi"/> を開く。呼び出し側が <c>using</c> で閉じる（毎回開いて閉じる。旧実装と同じ）。</summary>
    public interface IHidHideApiFactory
    {
        IHidHideApi Open();
    }
}
