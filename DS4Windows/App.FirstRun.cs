using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using WPFLocalizeExtension.Engine;
using DS4Windows.Composition.Startup;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Settings;
using DS4Windows.UI;

namespace DS4Windows
{
    /// <summary>初回の起動の設定（言語・保存場所・マクロのフォルダー・既定の構成）。</summary>
    public partial class App
    {
        /// <summary>
        /// Phase9 9-F F-1b: 初回起動の言語の選択（ダイアログ）。選ばずに閉じられたら、終了を要求して false を返す。
        /// </summary>
        private bool RunFirstRunLanguageSelection(bool firstRun, ref string? selectedLanguage)
        {
            // On first run, show language selection dialog first
            if (firstRun)
            {
                DS4Windows.UI.LanguageSelectDialog langDialog = new DS4Windows.UI.LanguageSelectDialog(culture =>
                {
                    _appSettingsService.UseLang = culture;
                    _appSettingsService.SetCulture(culture);
                    DS4Windows.Core.Logging.AppLogger.LogDebug($"LanguageSelectDialog: Global.UseLang set to '{_appSettingsService.UseLang}'");
                });
                langDialog.ShowDialog();
                if (!langDialog.ChoiceMade)
                {
                    runShutdown = false;
                    Current.Shutdown();
                    return false;
                }

                // Store selected language and apply it immediately for dialogs
                selectedLanguage = langDialog.SelectedCulture;
                if (selectedLanguage != null) _themeCulture.ApplyLanguageSetting(selectedLanguage);
            }
            return true;
        }

        /// <summary>
        /// Phase9 9-F F-1b: 初回起動の保存先の選択と、設定フォルダの骨格の作成。選ばなかった、または作れなかったときは、終了を要求して false を返す。
        /// </summary>
        /// <summary>
        /// 初回の保存場所の選択の適用。SetSaveLocation は <c>IPathService.AppDataPath</c> の setter とは別の動作（設定ファイルのパスも書き換える）。
        /// 複数の保存場所があり、残さないと選んだときは、もう一方の古い設定を削除する。
        /// </summary>
        private void ApplySaveLocationChoice(bool multisaves, bool programFolder, bool keepOldSettings)
        {
            if (programFolder)
            {
                _pathService.SetSaveLocation(_pathService.ProgramDirectory);
                if (multisaves && !keepOldSettings)
                {
                    try
                    {
                        if (Directory.Exists(_pathService.RoamingAppDataPath))
                        {
                            Directory.Delete(_pathService.RoamingAppDataPath, true);
                        }
                    }
                    catch (Exception caught) when (caught is System.IO.IOException or System.UnauthorizedAccessException or System.NotSupportedException or System.ArgumentException) { /* ファイル・プロセス・レジストリの失敗は無視して続ける */ }
                }
                else if (!multisaves)
                {
                    _pathService.CreateEmptyProfilesFile(_pathService.ProgramDirectory + "\\Profiles.xml");
                }
            }
            else
            {
                if (multisaves && !keepOldSettings)
                {
                    // Profiles／Profiles.xml／Auto Profiles.xml だけでなく、設定フォルダのアプリ管理ファイルをすべて削除する（ProgramFolderSettingsCleaner）。
                    if (!ProgramFolderSettingsCleaner.DeleteSettings(_pathService.ProgramDirectory))
                    {
                        MessageBox.Show("Cannot Delete old settings, please manaully delete", "DS4Windows");
                    }
                }
                else if (!multisaves)
                {
                    _pathService.CreateEmptyProfilesFile(Path.Combine(_pathService.RoamingAppDataPath, "Profiles.xml"));
                }

                _pathService.SetSaveLocation(_pathService.RoamingAppDataPath);
            }
        }

        private bool RunFirstRunSaveLocationAndSkeleton(bool firstRun)
        {
            // Could not find unique profile location; does not exist or multiple places.
            // Advise user to specify where DS4Windows should save its configuation files
            // and profiles
            if (firstRun)
            {
                bool multisaves = _pathService.HasMultipleSaveLocations;
                DS4Windows.UI.SaveWhere savewh = new DS4Windows.UI.SaveWhere(multisaves, _environmentService.AdminNeeded(),
                    (programFolder, keepOldSettings) => ApplySaveLocationChoice(multisaves, programFolder, keepOldSettings));
                savewh.ShowDialog();
                if (!savewh.ChoiceMade)
                {
                    runShutdown = false;
                    Current.Shutdown();
                    return false;
                }
            }

            // Exit if base configuration could not be generated
            if (firstRun && !CreateConfDirSkeleton())
            {
                MessageBox.Show($"Cannot create config folder structure in {_pathService.AppDataPath}. Exiting",
                    "DS4Windows", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown(1);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Phase9 9-F F-1b: マクロの保存・読込ダイアログの初期フォルダ（Macros）を、毎回の起動で確認する。
        /// </summary>
        private void EnsureMacrosFolder()
        {
            // マクロの保存・読込ダイアログの初期フォルダ（RecordBox が <保存場所>\Macros を指定する）。
            // CreateConfDirSkeleton は初回起動時しか呼ばれないため、既存の環境にも作られるよう毎回確認する。
            // 作成できなくても（書き込み権限がない場合など）起動は続ける。
            try
            {
                Directory.CreateDirectory(Path.Combine(_pathService.AppDataPath, "Macros"));
            }
            catch (Exception caught) when (caught is System.IO.IOException or System.UnauthorizedAccessException or System.NotSupportedException or System.ArgumentException)
            {
                // ファイル・プロセス・レジストリの失敗は無視して続ける
            }
        }

        /// <summary>
        /// Phase9 9-F F-1b: 初回起動のデバイスの選択と既定の設定の作成、接続フラグのリセット。最後に skipSave を解除する（保存を許可する）。
        /// </summary>
        private void RunFirstLaunchSetup(bool firstRun)
        {
            // Ask user which devices the mapper should attempt to open when detected.
            // Currently only support DS4 by default to avoid extra complications from
            // Steam Input
            if (firstRun)
            {
                _themeCulture.ApplyLanguageSetting(_appSettingsService.UseLang);
                DS4Windows.UI.FirstLaunchUtilWindow firstLaunchUtilWin =
                    new DS4Windows.UI.FirstLaunchUtilWindow(ResolvePostHostService<DS4Windows.UI.Abstractions.IShellViewModelFactory>()
                        .CreateFirstLaunchUtilViewModel(_appSettingsService.DeviceOptions));
                firstLaunchUtilWin.ShowDialog();
                _appSettingsService.Save();
            }

            if (firstRun)
            {
                AppLogger.LogInfo("No config found. Creating default config");
                AttemptSave();

                _profileRepository.SaveAsProfile(0, "Default");
                ProfileSlotNames profilePath = _profileRepository.ProfilePath;
                ProfileSlotNames olderProfilePath = _profileRepository.OlderProfilePath;
                for (int i = 0; i < DS4Windows.Core.Settings.ControllerLimits.MaxControllerCount; i++)
                {
                    profilePath[i] = olderProfilePath[i] = "Default";
                }

                AppLogger.LogInfo("Default config created");
            }

            // Reset first connection flags at startup
            _deviceStateService.ResetConnectionFlags();

            skipSave = false;
        }

        /// <summary>
        /// Phase9 9-F F-1b: プロファイルの固定 ID、SA の読み込み、言語とテーマの適用、リンクされたプロファイルの読み込み（メイン画面の生成の前）。
        /// </summary>
        private void PrepareProfilesCultureAndTheme()
        {
            // プロファイルの固定 ID の確定 → SA の読み込み（順序の理由は ProfileStartupLoader）
            new DS4Windows.Composition.Startup.ProfileStartupLoader(_profileIdentityService, _specialActionRepository).Run();

            // Have app use selected culture
            _themeCulture.SetUICulture(_appSettingsService.UseLang);
            _themeCulture.ApplyLanguageSetting(_appSettingsService.UseLang);
            AppLogger.LogInfo($"Effective UI culture after initialization: CurrentUICulture={Thread.CurrentThread.CurrentUICulture}, DefaultThreadCurrentUICulture={CultureInfo.DefaultThreadCurrentUICulture}, UseLang={_appSettingsService.UseLang}");

            DS4Windows.Core.Settings.AppThemeChoice themeChoice = _appearanceSettingsService.UseCurrentTheme;
            _themeCulture.ChangeTheme(_appearanceSettingsService.UseCurrentTheme, false);
            // Diagnostic: log culture state after applying theme to detect theme-induced localization regressions
            AppLogger.LogDebug($"Post-ChangeTheme: LocalizeDictionary={LocalizeDictionary.Instance.Culture}, SetCurrentThreadCulture={LocalizeDictionary.Instance.SetCurrentThreadCulture}, DefaultThreadCurrentUICulture={CultureInfo.DefaultThreadCurrentUICulture}, CurrentUICulture={Thread.CurrentThread.CurrentUICulture}");

            _profileRepository.LoadLinkedProfiles();
        }

        private bool CreateConfDirSkeleton()
        {
            bool result = true;
            try
            {
                string appDataPath = _pathService.AppDataPath;
                Directory.CreateDirectory(appDataPath);
                Directory.CreateDirectory(appDataPath + @"\Profiles\");
                Directory.CreateDirectory(appDataPath + @"\Logs\");
                // Macros フォルダは、毎回の起動で Application_Startup が作成する（初回起動時に限らないため）
            }
            catch (UnauthorizedAccessException)
            {
                result = false;
            }


            return result;
        }

        private void AttemptSave()
        {
            if (!_appSettingsService.Save()) //if can't write to file
            {
                new SettingsLocationFallback(_pathService, ResolvePostHostService<DS4Windows.UI.Abstractions.IDialogService>()).OfferCopyToAppData();

                // 旧 TODO(Phase6-Step7 決定3＝P1)（解消済み。当時の経緯）: IPathService.AppDataPath のセッターは、保存先の状態を変えない孤立実装だったため、
                // 置き換え先にならず、保存先の状態を直接書き換えていた。
                // Phase7-Step4 W4: 保存先の所有を PathService へ移した。状態そのものを null に戻す（旧 Global.appdatapath = null と同じ）。
                // （Phase7c W3: 本行の TODO は Phase7-Step4 W4 で解消済みのため、TODO 表記を外した。）
                DS4Windows.Core.Platform.PathService.AppDataPathState = null;
                skipSave = true;
                Current.Shutdown();
                return;
            }
        }
    }
}
