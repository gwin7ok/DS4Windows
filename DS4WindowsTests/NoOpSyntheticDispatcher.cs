using DS4Windows.Processing.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// キーの操作の部品（KeyButtonControllerRegistry など）がコンストラクターで受ける合成キーの送出の、何もしない偽物。
    /// ホストを作らないテストでは、これを渡す。
    /// </summary>
    public sealed class NoOpSyntheticDispatcher : ISyntheticDispatcher
    {
        public uint ResolveNativeKey(ushort kvpKey) => kvpKey;

        public void SendPress(int device, ushort kvpKey, uint nativeKey, bool useScanCode, IVirtualKBM handler = null) { }

        public void SendRelease(int device, ushort kvpKey, uint nativeKey, bool useScanCode, IVirtualKBM handler = null) { }

        public void ResetKeyTiming(int device, ushort kvpKey) { }
    }
}
