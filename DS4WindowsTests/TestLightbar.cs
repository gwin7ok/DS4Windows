using DS4Windows.Core.Model;

namespace DS4WindowsTests
{
    /// <summary>
    /// ライトバーの強制表示の状態（コンテナの Singleton）。旧: <c>DS4LightBar</c> の静的な配列。
    /// テストが部品を直接作るときは、これを渡す。
    /// </summary>
    internal static class TestLightbar
    {
        public static LightbarOverrides State => TestHost.GetService<LightbarOverrides>();
    }
}
