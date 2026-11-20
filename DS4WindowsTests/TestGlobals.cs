
using DS4Windows.Composition;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Settings;

namespace DS4WindowsTests
{
    /// <summary>
    /// テストが共有の設定（BackingStore）とパスの静的な状態を扱う入口。
    /// W5 で <c>Global</c> を削除したため、共有の BackingStore は、テストのコンテナ（<see cref="AppHost"/>）が持つ実体とする。
    /// コンテナは 1 つがテスト全体で使い回される（Dispose されると取得できなくなるため、最後に取得した実体を保持する）。
    /// </summary>
    internal static class TestGlobals
    {
        private static BackingStore _store;

        /// <summary>共有の BackingStore。コンテナの Singleton と同一。</summary>
        public static BackingStore Store
        {
            get
            {
                var current = TestHost.GetService<BackingStore>();
                if (current != null)
                    _store = current;
                return _store;
            }
        }

        /// <summary>設定の保存場所。</summary>
        public static string AppDataPath
        {
            get => PathService.AppDataPathState;
            set => PathService.AppDataPathState = value;
        }

        /// <summary>実行ファイルのディレクトリ。</summary>
        public static string ExeDirPath
        {
            get => PathService.ExecutableDirectoryState;
            set => PathService.ExecutableDirectoryState = value;
        }

        /// <summary>共有の BackingStore が使うプロファイル設定サービス。</summary>
        public static IProfileSettingsService ProfileSettingsServiceInstance
        {
            // BackingStore のフォールバックを撤去したため、コンテナの ProfileSettingsService を解決して結んでから読む
            // （生成時に共有の BackingStore へ自分を結ぶ。setter で差し替えた後は、その値が優先される）
            get { TestHost.GetService<IProfileSettingsService>(); return Store.ProfileSettings; }
            set => Store.ProfileSettings = value;
        }
    }
}
