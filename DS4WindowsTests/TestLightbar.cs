using DS4Windows.Core.Model;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase14c-Step4（R-05 の前半）: ライトバーの強制表示の状態（コンテナの Singleton）。旧: <c>DS4LightBar</c> の静的な配列。
    /// テストが部品を直接作るときは、これを渡す（旧の静的な配列と同じく、テスト全体で 1 つを共有する）。
    /// </summary>
    internal static class TestLightbar
    {
        public static LightbarOverrides State => TestHost.GetService<LightbarOverrides>();
    }
}
