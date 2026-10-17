using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10d W1: テストが <c>Global</c> の静的な窓口を直接参照しないための入口。
    /// 当面は <c>Global</c> を呼ぶだけの薄い委譲で、W5／W6 で実体を差し替える（テスト本体は触らない）。
    /// </summary>
    internal static class TestGlobals
    {
        /// <summary>共有の BackingStore（旧 <c>Global.store</c>）。</summary>
        public static BackingStore Store => Global.store;

        /// <summary>設定の保存場所（旧 <c>Global.appdatapath</c>）。</summary>
        public static string AppDataPath
        {
            get => Global.appdatapath;
            set => Global.appdatapath = value;
        }

        /// <summary>実行ファイルのディレクトリ（旧 <c>Global.exedirpath</c>）。</summary>
        public static string ExeDirPath
        {
            get => Global.exedirpath;
            set => Global.exedirpath = value;
        }

        /// <summary>静的に保持されるプロファイル設定サービス（旧 <c>Global.ProfileSettingsServiceInstance</c>）。</summary>
        public static IProfileSettingsService ProfileSettingsServiceInstance
        {
            get => Global.ProfileSettingsServiceInstance;
            set => Global.ProfileSettingsServiceInstance = value;
        }
    }
}
