#nullable enable

namespace DS4Windows.Processing.Abstractions
{
    /// <summary>動作中の OSC リスナー（受信側）。閉じるだけの窓口。</summary>
    public interface IOscListenerHandle
    {
        void Close();
    }
}
