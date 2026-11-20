namespace DS4Windows.Input.Abstractions
{
    /// <summary><see cref="IInputTransportSource"/> が見つけた機器 1 台: 通信路と、その機器の VID／PID の情報（種類・名前・機能フラグ・接続方式の判定）。</summary>
    public sealed class InputTransportCandidate
    {
        public InputTransportCandidate(IHidTransport transport, VidPidInfo info)
        {
            Transport = transport;
            Info = info;
        }

        public IHidTransport Transport { get; }

        public VidPidInfo Info { get; }
    }
}
