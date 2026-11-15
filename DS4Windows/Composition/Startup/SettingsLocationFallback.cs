using System;
using System.IO;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.UI.Abstractions;

namespace DS4Windows.Composition.Startup
{
    /// <summary>
    /// 設定の保存先（実行ファイルのフォルダー）へ書けないときの案内（旧 <c>App.AttemptSave</c> の判断の部分）。
    /// 設定を appdata へコピーするかを尋ね、コピーしてよければコピーして再起動を促し、断られたら編集できないことを知らせる。
    /// どちらの場合も、呼び出し側（<c>App</c>）は、そのあとアプリを終了する。
    /// </summary>
    public sealed class SettingsLocationFallback
    {
        private readonly IPathService _pathService;
        private readonly IDialogService _dialogs;

        public SettingsLocationFallback(IPathService pathService, IDialogService dialogs)
        {
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        }

        /// <returns>コピーを行ったとき true。断られたとき false</returns>
        public bool OfferCopyToAppData()
        {
            if (_dialogs.Confirm(AppLogger.UserText("Startup_CannotWriteCopyPrompt"), "DS4Windows", DialogIcon.Warning))
            {
                try
                {
                    string roamingAppDataPath = _pathService.RoamingAppDataPath;
                    // Global.exedirpath と同じ値（ジャンクション解決済みの実行ファイルの親フォルダ）。
                    // IPathService.ExecutableDirectory（AppContext.BaseDirectory）とは値が異なりうるため使わない。
                    string exeDirPath = Path.GetDirectoryName(_pathService.ExecutablePath);
                    Directory.CreateDirectory(roamingAppDataPath);
                    File.Copy(Path.Combine(exeDirPath, "Profiles.xml"),
                        Path.Combine(roamingAppDataPath, "Profiles.xml"));
                    File.Copy(Path.Combine(exeDirPath, "Auto Profiles.xml"),
                        Path.Combine(roamingAppDataPath, "Auto Profiles.xml"));
                    Directory.CreateDirectory(Path.Combine(roamingAppDataPath, "Profiles"));
                    foreach (string s in Directory.GetFiles(Path.Combine(exeDirPath, "Profiles")))
                    {
                        File.Copy(s, Path.Combine(roamingAppDataPath, "Profiles", Path.GetFileName(s)));
                    }
                }
                catch { }

                _dialogs.ShowMessage(AppLogger.UserText("Startup_CopyComplete"), "DS4Windows", DialogIcon.None);
                return true;
            }

            _dialogs.ShowMessage(AppLogger.UserText("Startup_CannotEditClose"), "DS4Windows", DialogIcon.None);
            return false;
        }
    }
}
