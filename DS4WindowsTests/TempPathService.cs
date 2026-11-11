using DS4Windows.Core.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>テスト用の <see cref="IPathService"/>。保存先を、指定したフォルダ（一時フォルダ）に置き、利用者の環境のファイルに触れない。</summary>
    internal sealed class TempPathService : IPathService
    {
        public TempPathService(string appDataPath)
        {
            AppDataPath = appDataPath;
        }

        public string AppDataPath { get; }
        public string ExecutableDirectory => AppDataPath;
        public string ExecutablePath => System.IO.Path.Combine(AppDataPath, "DS4Windows.exe");
        public string ProfilesPath => AppDataPath;
        public string RoamingAppDataPath => AppDataPath;
        public bool HasMultipleSaveLocations => false;
        public string ProgramDirectory => AppDataPath;

        public string GetProfilePath(string profileName) => System.IO.Path.Combine(AppDataPath, profileName + ".xml");

        public void SetSaveLocation(string path)
        {
        }

        public bool CreateEmptyProfilesFile(string path) => false;
    }
}
