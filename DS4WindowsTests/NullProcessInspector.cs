using DS4Windows.Core.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// クラスタ 1（プロファイル系サービス）の Global／AppHost／Program.rootHub 直接参照解消の回帰ガード
    /// （<c>docs-forDIMG/MadeByAgent/Archive/Phase6-Step10-4-Plan.md</c> §13）。10-4-2b／2c で、対象のファイルを追加していく。
    /// </summary>
    internal class NullProcessInspector : IProcessInspector
    {
        public bool IsProcessRunning(string exePath) => false;
        public bool GetForegroundProcessInfo(out string processPath, out string windowTitle)
        {
            processPath = string.Empty;
            windowTitle = string.Empty;
            return false;
        }
    }
}
