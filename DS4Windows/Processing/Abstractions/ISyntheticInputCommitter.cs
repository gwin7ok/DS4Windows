namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 MapCustom の分割 M1（Phase9-MapCustom-Plan.md §3）: 合成入力（マウスのクリック・ホイール）の確定と出力。
    /// 以前は <c>Mapping.Commit</c>。デバイスごとの合成状態を全デバイスの合計（<c>GlobalState</c>）へ集計し、前回との差から、押下・離上・ホイールの出力を
    /// <c>IVirtualKBM</c> へ送る。ホイールは押している間、一定間隔（150ms）で繰り返す。<c>Mapping.Commit</c> は薄い委譲として残す。
    /// </summary>
    public interface ISyntheticInputCommitter
    {
        void Commit(int device, IProfileSettingsService settings);
    }
}
