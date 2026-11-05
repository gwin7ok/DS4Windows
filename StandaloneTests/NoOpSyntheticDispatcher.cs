using DS4Windows.Processing.Abstractions;

namespace StandaloneTests
{
    /// <summary>
    /// Phase14c-Step3（R-06）: キーの操作の部品（KeyButtonControllerRegistry など）がコンストラクターで受ける合成キーの送出の、何もしない偽物。
    /// StandaloneTests はホストを作らないため、これを渡す（旧: 静的な橋渡し SyntheticDispatcher が未束縛のときと同じく、キーの解決はそのまま返す）。
    /// </summary>
    public sealed class NoOpSyntheticDispatcher : ISyntheticDispatcher
    {
        public uint ResolveNativeKey(ushort kvpKey) => kvpKey;

        public void SendPress(int device, ushort kvpKey, uint nativeKey, bool useScanCode, IVirtualKBM handler = null) { }

        public void SendRelease(int device, ushort kvpKey, uint nativeKey, bool useScanCode, IVirtualKBM handler = null) { }

        public void ResetKeyTiming(int device, ushort kvpKey) { }
    }
}
