using System;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Actions
{
    public class TriggerContextImpl : ITriggerContext
    {
        public int Device { get; set; }
        public bool IsEdgeEstablished { get; set; }
        public ushort LogicalValue { get; set; }
        public uint NativeValue { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public IVirtualKBM OutputHandler { get; set; }
    }
}
