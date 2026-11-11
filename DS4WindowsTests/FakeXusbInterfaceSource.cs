using System.Collections.Generic;
using System.Linq;
using DS4Windows.Input.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>テスト用の <see cref="IXusbInterfaceSource"/>。インターフェースの一覧と、パスごとの XInput の番号を台本で決める。</summary>
    internal sealed class FakeXusbInterfaceSource : IXusbInterfaceSource
    {
        private readonly object sync = new object();
        private readonly List<XusbInterfaceInfo> interfaces = new List<XusbInterfaceInfo>();
        private readonly Dictionary<string, int> indexes = new Dictionary<string, int>();

        public void Add(string path, int vid, int pid, int playerIndex, string instanceId = null)
        {
            lock (sync)
            {
                interfaces.Add(new XusbInterfaceInfo(path, instanceId ?? @"USB\VID_" + vid.ToString("X4") + "&PID_" + pid.ToString("X4") + @"\SERIAL" + path.Length,
                    vid, pid, playerIndex));
                indexes[path] = playerIndex;
            }
        }

        /// <summary>機器を繋ぎ直したときのように、番号だけを変える（-1 は、読めない）。</summary>
        public void MoveTo(string path, int playerIndex)
        {
            lock (sync) indexes[path] = playerIndex;
        }

        public IReadOnlyList<XusbInterfaceInfo> Enumerate()
        {
            lock (sync) return interfaces.ToList();
        }

        public int ReadPlayerIndex(string devicePath)
        {
            lock (sync) return indexes.TryGetValue(devicePath, out int index) ? index : -1;
        }
    }
}
