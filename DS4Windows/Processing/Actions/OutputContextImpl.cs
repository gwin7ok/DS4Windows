using DS4Windows.Services;
using System;
using DS4Windows.DS4Control;

namespace DS4Windows.Actions
{
    public class OutputContextImpl : IOutputContext
    {
        public OutputContextImpl(int device, IVirtualKBM handler, IFrameOutput frame = null)
        {
            Device = device;
            OutputHandler = handler;
            Frame = frame;
        }

        public int Device { get; }
        public IVirtualKBM OutputHandler { get; }
        public IFrameOutput Frame { get; }

        /// <summary>Phase8-Step4 W1: <see cref="RequestEndFrame"/> が呼ばれたか。</summary>
        public bool EndFrameRequested { get; private set; }

        public void RequestEndFrame()
        {
            EndFrameRequested = true;
        }
    }
}
