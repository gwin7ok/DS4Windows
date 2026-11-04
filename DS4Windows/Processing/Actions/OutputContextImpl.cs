using System;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Actions
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
