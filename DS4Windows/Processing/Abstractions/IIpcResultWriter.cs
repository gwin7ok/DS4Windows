namespace DS4Windows.UI.Abstractions
{
    /// <summary>
    /// Phase14b-Step16（D11 と同じ形）: CLI の問い合わせ（<c>-command Query…</c>）の結果を、クライアントのプロセスへ書き込む窓口。
    /// 実装は Composition の <c>AppIpcResultWriterAdapter</c>（結果のメモリマップドファイルを持つ <c>App</c> へ委譲する）。
    /// 旧: <c>MainWindow</c>（<c>IIpcCommandHost.WriteQueryResult</c>）が <c>(Application.Current as App).WriteIPCResultDataMMF</c> を直接呼んでいた。
    /// </summary>
    public interface IIpcResultWriter
    {
        /// <summary>問い合わせの結果を書き込み、クライアントへ通知する（旧 <c>App.WriteIPCResultDataMMF</c>）。</summary>
        void WriteQueryResult(string value);
    }
}
