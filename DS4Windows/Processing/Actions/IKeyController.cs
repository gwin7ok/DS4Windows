#nullable enable

using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Actions
{
    // Internal interface for per-mode controllers
    internal interface IKeyController
    {
        void OnDown(ushort kvpKey, uint nativeKey, bool useScanCode, IVirtualKBM? handler, bool isSpecialAction);
        void OnUp(ushort kvpKey, uint nativeKey, bool useScanCode, IVirtualKBM? handler);
        // Invoked when a Toggle-mode action is explicitly toggled OFF (controller-level stop + release).
        void OnToggleOff(ushort kvpKey, uint nativeKey, bool useScanCode, IVirtualKBM? handler);
        void Clear(ushort kvpKey);
        void ClearAll();
    }
}
