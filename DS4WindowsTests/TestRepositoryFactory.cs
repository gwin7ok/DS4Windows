using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step10-4-2: 必須引数化したサービスのテスト用の生成ヘルパー。
    /// 既定では本番と同じく Global.store と PathService（Global.appdatapath をオンデマンドで参照）を渡す。
    /// </summary>
    internal static class TestRepositoryFactory
    {
        public static SpecialActionRepository CreateSpecialActionRepository(BackingStore store = null, IPathService pathService = null)
        {
            return new SpecialActionRepository(store ?? Global.store, pathService ?? new PathService());
        }
    }
}
