#nullable enable

using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Actions
{
    // 出力アクションに渡す文脈（デバイスごとに再利用。割り当て 0）
    internal sealed class EngineContext : IOutputContext, IOutputContextInput, IPendingUntriggerQuery
    {
        private readonly DeviceRuntime _owner;

        public EngineContext(int device, IVirtualKBM handler, DeviceRuntime owner)
        {
            Device = device;
            OutputHandler = handler;
            _owner = owner;
        }

        public SpecialAction? PendingUntriggerAction => _owner.Pending?.Source;

        public int Device { get; }
        public IVirtualKBM OutputHandler { get; }
        public IFrameOutput? Frame { get; private set; }
        public bool EndFrameRequested { get; private set; }

        public ITriggerInput? Input { get; private set; }

        public void Begin(IFrameOutput? frame, ITriggerInput? input)
        {
            Frame = frame;
            Input = input;
            EndFrameRequested = false;
        }

        public void RequestEndFrame()
        {
            EndFrameRequested = true;
        }
    }
}
