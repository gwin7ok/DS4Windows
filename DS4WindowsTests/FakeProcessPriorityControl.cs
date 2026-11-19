#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using DS4Windows.UI.Abstractions;

namespace DS4WindowsTests
{
    /// <summary><see cref="IProcessPriorityControl"/> の偽物。実際の優先度は変えず、渡された値を記録する。</summary>
    public sealed class FakeProcessPriorityControl : IProcessPriorityControl
    {
        public readonly List<ProcessPriorityClass> Applied = new List<ProcessPriorityClass>();

        public void Apply(ProcessPriorityClass priority) => Applied.Add(priority);
    }
}
