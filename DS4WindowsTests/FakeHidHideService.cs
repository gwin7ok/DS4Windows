using DS4Windows.Core.Abstractions;
using DS4Windows.Processing.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>テスト用の <see cref="IHidHideService"/>。許可リストへの登録の呼び出しだけを数える。</summary>
    internal sealed class FakeHidHideService : IHidHideService
    {
        public int CheckPresenceCalls { get; private set; }

        public void CheckPresence(string ExePath = "", string ExeName = "Autoprofile Exe", bool AddExe = true) => CheckPresenceCalls++;

        public void UpdateAttributes()
        {
        }

        public bool IsAffected(IOutputBoundDevice dev) => false;

        public void MarkAffected(IOutputBoundDevice dev)
        {
        }
    }
}
