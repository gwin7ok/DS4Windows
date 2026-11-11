using System.Collections.Generic;
using DS4Windows.Processing.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>テスト用の <see cref="IXboxParentHider"/>。呼び出しを記録し、決めておいた結果を返す。</summary>
    internal sealed class FakeXboxParentHider : IXboxParentHider
    {
        public List<(string DevicePath, bool Hide)> Applied { get; } = new List<(string, bool)>();
        public int RestoreAllCalls { get; private set; }
        public int RecoverStaleCalls { get; private set; }
        public XboxParentHideResult Result { get; set; } = XboxParentHideResult.Hidden;

        public XboxParentHideResult Apply(string devicePath, bool hide)
        {
            Applied.Add((devicePath, hide));
            return Result;
        }

        public bool RestoreAllResult { get; set; }

        public bool RestoreAll()
        {
            RestoreAllCalls++;
            return RestoreAllResult;
        }

        public void RecoverStale() => RecoverStaleCalls++;
    }
}
