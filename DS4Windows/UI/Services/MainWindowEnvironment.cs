using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Profiles;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// メイン画面（<see cref="MainWindow"/>）が読む、保存先のパスと実行環境の窓口。画面がパス・環境サービスの型を直接参照しない（V2）ようにする。
    /// </summary>
    public sealed class MainWindowEnvironment
    {
        private readonly IPathService paths;
        private readonly IEnvironmentService environment;

        public MainWindowEnvironment(IPathService paths, IEnvironmentService environment)
        {
            this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public string AppDataPath => paths.AppDataPath;
        public string ExecutablePath => paths.ExecutablePath;
        public string ExecutableDirectory => paths.ExecutableDirectory;

        public bool IsAdministrator() => environment.IsAdministrator();
        public int ControllerSlotLimit => environment.ControllerSlotLimit;
        public string ApplicationVersion => environment.ApplicationVersion;
        public Guid HidClassGuid => environment.HidClassGuid;
        public Guid XusbClassGuid => environment.XusbClassGuid;

        public void RefreshHidHideInfo() => environment.RefreshHidHideInfo();
        public void RefreshFakerInputInfo() => environment.RefreshFakerInputInfo();

        /// <summary>保存先のパスから、プロファイルの一覧の保持の入れ物を作る。</summary>
        public ProfileList CreateProfileList() => new ProfileList(paths);
    }
}
