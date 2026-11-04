namespace DS4Windows.DI
{
    /// <summary>
    /// Phase9 9-E E-1（Phase9-E-Plan.md）: OSC メッセージ（アドレス 1 つと値 1 つ）の送信。実装は SharpOSC の <c>UDPSender</c> を包む
    /// （テストプロジェクトが SharpOSC を参照せずに、送信内容を記録して比べられるようにするための境界）。
    /// </summary>
    public interface IOscMessageSender
    {
        /// <summary>メッセージを送る。<paramref name="value"/> の型（int、byte など）は、そのまま OSC の引数の型になる。</summary>
        void Send(string address, object value);

        /// <summary>送信側のソケットを閉じる。</summary>
        void Close();
    }
}
