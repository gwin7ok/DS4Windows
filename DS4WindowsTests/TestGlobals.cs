using DS4Windows;
using DS4Windows.DI;
using DS4WinWPF;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10d W1／W5: テストが共有の設定（BackingStore）とパスの静的な状態を扱う入口。
    /// W5 で <c>Global</c> を削除したため、共有の BackingStore は、テストのコンテナ（<see cref="AppHost"/>）が持つ実体とする。
    /// コンテナは 1 つがテスト全体で使い回される（Dispose されると取得できなくなるため、最後に取得した実体を保持する）。
    /// </summary>
    internal static class TestGlobals
    {
        private static BackingStore _store;

        /// <summary>共有の BackingStore（旧 <c>Global.store</c>）。コンテナの Singleton と同一。</summary>
        public static BackingStore Store
        {
            get
            {
                var current = AppHost.GetService<BackingStore>();
                if (current != null)
                    _store = current;
                return _store;
            }
        }

        /// <summary>設定の保存場所（旧 <c>Global.appdatapath</c>）。</summary>
        public static string AppDataPath
        {
            get => PathService.AppDataPathState;
            set => PathService.AppDataPathState = value;
        }

        /// <summary>実行ファイルのディレクトリ（旧 <c>Global.exedirpath</c>）。</summary>
        public static string ExeDirPath
        {
            get => PathService.ExecutableDirectoryState;
            set => PathService.ExecutableDirectoryState = value;
        }

        /// <summary>共有の BackingStore が使うプロファイル設定サービス（旧 <c>Global.ProfileSettingsServiceInstance</c>）。</summary>
        public static IProfileSettingsService ProfileSettingsServiceInstance
        {
            get => Store.ProfileSettings;
            set => Store.ProfileSettings = value;
        }
    }
}
