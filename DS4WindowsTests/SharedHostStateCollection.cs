using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 共有の状態に触れるテストを、直列に（このコレクションの中で 1 つずつ、ほかのコレクションとも同時にせずに）実行するためのコレクション。
    /// 共有の状態: テスト全体で 1 つの DI のホスト（<see cref="TestHost"/>）、起動前の設定の場所などの静的な状態（<c>PathService</c>・<c>TestGlobals</c>）、
    /// <c>AppLogger</c> の静的なイベント、アプリデータのフォルダーの中のファイル。
    /// Phase14d-Step5: 旧名は "ServiceProviderHolder"（Phase13 で撤廃した型の名前のまま残っていた）。名前は定数 <see cref="Name"/> で参照する。
    /// </summary>
    [CollectionDefinition(Name, DisableParallelization = true)]
    public class SharedHostStateCollection
    {
        public const string Name = "SharedHostState";
    }
}
