using System;
using System.IO;
using DS4Windows.DI;

namespace DS4Windows
{
    public class PathService : IPathService
    {
        private string _customAppDataPath;

        public PathService(string appDataPath = null)
        {
            _customAppDataPath = appDataPath;
        }

        /// <summary>
        /// アプリケーションデータパスを取得します（コンストラクタ引数で指定した場合は、その値を返します）。
        /// 固定キャッシュを行わず、常に Global.appdatapath の最新値を On-Demand 評価します（§5.4 ガードレール）。
        /// これにより、起動順序逆転によるパス固定化ハザードを完全に防止します。
        /// </summary>
        public string AppDataPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_customAppDataPath))
                    return _customAppDataPath;

                return !string.IsNullOrEmpty(Global.appdatapath)
                    ? Global.appdatapath
                    : AppContext.BaseDirectory;
            }
        }

        public string ExecutableDirectory => AppContext.BaseDirectory;

        // Phase6-Step9-1: Global.exedirpath への薄い委譲（ExecutableDirectory とは値が異なりうる。IPathService の注記を参照）
        public string ProgramDirectory => Global.exedirpath;

        /// <summary>
        /// 実行ファイルのフルパスを取得します。Scoop等のジャンクションシンボリックリンク解決ロジックを
        /// 含む Global.exelocation への薄い委譲です（Phase5-Step14前クリーンアップ）。
        /// </summary>
        public string ExecutablePath => Global.exelocation;

        public string ProfilesPath
        {
            get
            {
                string path = Path.Combine(AppDataPath, "Profiles");
                if (!Directory.Exists(path))
                {
                    try { Directory.CreateDirectory(path); } catch { }
                }
                return path;
            }
        }

        public string AutoProfilesPath => Path.Combine(AppDataPath, "Auto Profiles.xml");
        public string LinkedProfilesPath => Path.Combine(AppDataPath, "LinkedProfiles.xml");
        public string ControllerConfigsPath => Path.Combine(AppDataPath, "ControllerConfigs.xml");

        public string GetProfilePath(string profileName)
        {
            if (string.IsNullOrEmpty(profileName))
                return string.Empty;

            if (!profileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                profileName += ".xml";

            return Path.Combine(ProfilesPath, profileName);
        }

        // ---- Phase6-Step7-1: App.xaml.cs（Post-Host）の Global 直接参照解消 ----
        // いずれも Global の現在値をその都度返す薄い委譲（独自の状態を持たない）。
        public string RoamingAppDataPath => Global.appDataPpath;

        public bool HasMultipleSaveLocations => Global.multisavespots;

        // ---- Phase6-Step10-1-1: Global への薄い委譲（AppDataPath の setter とは別物。IPathService の注記を参照）----
        public void SetSaveLocation(string path) => Global.SaveWhere(path);

        public bool CreateEmptyProfilesFile(string path) => Global.SaveDefault(path);
    }
}