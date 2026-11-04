using System;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Actions
{
    /// <summary>
    /// Lightweight DTO describing a detected trigger from input evaluation.
    /// </summary>
    public interface ITriggerContext
    {
        int Device { get; }
        bool IsEdgeEstablished { get; }
        ushort LogicalValue { get; }
        uint NativeValue { get; }
        DateTime Timestamp { get; }
        IVirtualKBM OutputHandler { get; }
    }
}
