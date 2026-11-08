using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Settings;
using DS4Windows.Processing;

namespace DS4Windows.Core.Persistence
{
    public class ProfileRepository : IProfileRepository
    {
        private readonly object _fileLock = new object();
        // Phase7-Step3 W1: Global 経由の参照をやめ、Composition Root から注入された BackingStore（設定の唯一の実体）を使う。
        private readonly BackingStore _config;
        private readonly IProfileSettingsService _profileSettings;
        private readonly IProfileXmlStore _profileXmlStore;
        private readonly IPathService _pathService;
        // Phase7-Step10e W1: スロット数の上限は、静的な ControlService.CURRENT_DS4_CONTROLLER_LIMIT ではなく、注入された環境サービスから読む
        private readonly IEnvironmentService _environment;

        // Phase6-Step10-4-2b: 全ての依存を必須化（Global.*Instance／AppHost のフォールバックを廃止）。
        // Phase13-Step6 6-e: 旧 Func<ControlService>（ControlService → IProfileRepository の依存循環を避ける遅延解決）。プロファイル読込の鎖が使う窓口を直接受ける
        private readonly IProfileLoadTarget _loadTarget;
        // Phase7-Step10c W2: リンクプロファイル・Actions.xml の保存・読込（旧: BackingStore のメソッド）
        private readonly DS4Windows.Core.Persistence.LinkedProfilesXmlSerializer _linkedProfilesXml;
        private readonly DS4Windows.Core.Persistence.SpecialActionsXmlSerializer _actionsXml;
        // Phase7-Step10c W4: プロファイル（旧形式の XML）の読込（旧: BackingStore.LoadProfile）
        private readonly DS4Windows.Core.Persistence.LegacyProfileXmlSerializer _legacyProfileXml;

        public ProfileRepository(BackingStore config, IProfileSettingsService profileSettings,
            IProfileXmlStore profileXmlStore, IPathService pathService,
            IProfileLoadTarget loadTarget,
            DS4Windows.Core.Persistence.LinkedProfilesXmlSerializer linkedProfilesXml, DS4Windows.Core.Persistence.SpecialActionsXmlSerializer actionsXml,
            DS4Windows.Core.Persistence.LegacyProfileXmlSerializer legacyProfileXml, IEnvironmentService environment)
        {
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _legacyProfileXml = legacyProfileXml ?? throw new ArgumentNullException(nameof(legacyProfileXml));
            _linkedProfilesXml = linkedProfilesXml ?? throw new ArgumentNullException(nameof(linkedProfilesXml));
            _actionsXml = actionsXml ?? throw new ArgumentNullException(nameof(actionsXml));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _profileXmlStore = profileXmlStore ?? throw new ArgumentNullException(nameof(profileXmlStore));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _loadTarget = loadTarget ?? throw new ArgumentNullException(nameof(loadTarget));
        }

        private IPathService PathSvc => _pathService;

        public string ProfilesPath => PathSvc.ProfilesPath;

        public string GetProfilePath(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
                return string.Empty;

            if (!profileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                profileName += ".xml";

            return Path.Combine(ProfilesPath, profileName);
        }

        public bool LoadProfile(int deviceIndex, string profileName)
        {
            lock (_fileLock)
            {
                try
                {
                    string path = GetProfilePath(profileName);
                    if (!File.Exists(path))
                        return false;

                    string normalizedName = Path.GetFileNameWithoutExtension(profileName);
                    ProfilePath[deviceIndex] = normalizedName;
                    // Phase5-Step2: Global.LoadProfileへの委譲をやめ、XML I/O(IProfileXmlStore)と
                    // 状態調整ロジック(旧Global.LoadProfile内で行っていたもの)をここに直接内包する。
                    // 挙動はGlobal.LoadProfileと完全に同一に維持する。
                    _config.loggedInvalidActions.Clear();
                    _profileXmlStore.LoadProfileXml(deviceIndex, false, _loadTarget, "", false, true);
                    _profileSettings?.SetTempProfileName(deviceIndex, string.Empty);
                    _profileSettings?.SetUseTempProfile(deviceIndex, false);
                    _profileSettings?.SetTempProfileDistance(deviceIndex, false);

                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace($"[DI] ProfileRepository.LoadProfile: Slot {deviceIndex}, Profile '{profileName}' loaded via DI");
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool SaveProfile(int deviceIndex, string profileName)
        {
            lock (_fileLock)
            {
                try
                {
                    string path = GetProfilePath(profileName);
                    if (string.IsNullOrEmpty(path))
                    {
                        AppLogger.LogUser(AppLogLevel.Error, "Log_Repo_SaveInvalidPath", profileName);
                        return false;
                    }

                    // Phase5-Step2/Step4: IProfileXmlStore.SaveProfileXmlの成否(bool)を伝播し、成否に応じた通知・ログを出力
                    bool saveSuccess = _profileXmlStore.SaveProfileXml(deviceIndex, profileName);
                    if (saveSuccess)
                    {
                        if (AppLogger.IsTraceEnabled)
                            AppLogger.LogTrace($"[DI] SaveProfile succeeded: Slot {deviceIndex}, Profile '{profileName}'");
                    }
                    else
                    {
                        AppLogger.LogUser(AppLogLevel.Error, "Log_Repo_SaveFailed", profileName, deviceIndex + 1);
                        if (AppLogger.IsTraceEnabled)
                            AppLogger.LogDebug($"[DI] SaveProfile failed: Slot {deviceIndex}, Profile '{profileName}'");
                    }

                    return saveSuccess;
                }
                catch (Exception ex)
                {
                    AppLogger.LogUser(AppLogLevel.Error, "Log_Repo_SaveFailedEx", profileName, deviceIndex + 1, ex.Message);
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogDebug($"[DI] SaveProfile failed with exception: Slot {deviceIndex}, Profile '{profileName}', error={ex}");
                    return false;
                }
            }
        }

        public bool ApplyProfileDirect(int deviceIndex, string profileName)
        {
            if (deviceIndex < 0 || deviceIndex >= ProfileSettingsService.MAX_DS4_CONTROLLER_COUNT)
                return false;

            _profileSettings?.SetUseTempProfile(deviceIndex, true);
            _profileSettings?.SetTempProfileName(deviceIndex, profileName);
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ProfileRepository.ApplyProfileDirect: Slot {deviceIndex}, Temp Profile '{profileName}' applied via DI");
            return LoadProfile(deviceIndex, profileName);
        }

        public bool RestoreProfileDirect(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= ProfileSettingsService.MAX_DS4_CONTROLLER_COUNT)
                return false;

            _profileSettings?.SetUseTempProfile(deviceIndex, false);
            _profileSettings?.SetTempProfileName(deviceIndex, string.Empty);
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ProfileRepository.RestoreProfileDirect: Slot {deviceIndex} restored via DI");
            return LoadProfile(deviceIndex, string.Empty);
        }

        // ---- Phase5-Step13-2: デバイススロット別・実行時プロファイル状態 ----
        // Global.ProfilePath 等は m_Config(BackingStore) への薄い公開アクセサであり、
        // 本クラスの LoadProfile 内でも同一配列(Global.ProfilePath[deviceIndex])へ直接書き込んでいる
        // 既存の単一の実体であるため、状態を複製せずそのまま公開する(Phase4-Step10の知見に準拠)。
        public ProfileSlotNames ProfilePath => _config.profilePath;
        public ProfileSlotNames OlderProfilePath => _config.olderProfilePath;
        public ProfileSlotNames SelectedProfile => _config.selectedProfile;
        public ProfileSlotNames LinkedProfileUI => _config.linkedProfileUI;

        // Phase7-Step3c W4: Global の静的イベントから、このリポジトリ（DI のシングルトン）のインスタンスイベントにした。
        // 発火元は ProfileApplicationService（RaiseSelectedProfileChanged 経由）。購読者は ControllerListViewModel のみ。
        public event EventHandler<SelectedProfileChangedEventArgs> SelectedProfileChanged;

        public void RaiseSelectedProfileChanged(int deviceIndex, string profileName)
            => SelectedProfileChanged?.Invoke(this, new SelectedProfileChangedEventArgs(deviceIndex, profileName));

        // ---- Phase5-Step13-2: LinkedProfile（コントローラーMAC単位のプロファイル紐付け）管理 ----
        // Phase7-Step3 W5: 旧 Global.changeLinkedProfile／removeLinkedProfile／containsLinkedProfile／getLinkedProfile の処理
        // （シリアルからコロンを除いて正規化する）をここへ移した。
        public void ChangeLinkedProfile(string serial, string profile)
        {
            string tempSerial = serial.Replace(":", string.Empty);
            _config.linkedProfiles[tempSerial] = profile;
        }

        public void RemoveLinkedProfile(string serial)
        {
            string tempSerial = serial.Replace(":", string.Empty);
            if (_config.linkedProfiles.ContainsKey(tempSerial))
            {
                _config.linkedProfiles.Remove(tempSerial);
            }
        }

        public bool SaveLinkedProfiles()
            => _linkedProfilesXml.SaveLinkedProfiles();

        // Phase10-Step4d W7: 以前ここにあった ReplaceLinkedProfileName（リンクプロファイルの旧名の置き換え）は撤去した。リンクプロファイル（LinkedProfiles.xml の実行時の保持）は、
        // W5d で、プロファイルの固定 ID を保管する窓口（LinkedProfileMap）になり、名前を変えても、読み出す名前が現在の名前になるため。
        // 保存先のファイルの名前と ID は、名前変更の直後に SaveLinkedProfiles で書き直す（MainWindow）

        // Phase10-Step4d W7: 以前の ReplaceProfileSwitchTarget（Phase6-Step10-4-5d）は、名前で置き換える処理だった。切り替え先の ID（SpecialAction.profileId、W3）から現在の名前を引き直し、
        // Actions.xml を保存し直す処理に替えた（メモリの details は、編集画面が読む。実行時の切り替えは、実行の直前に ID から引く［W5c］）。ID のない SA は、旧名との一致で、従来どおり置き換える
        public int RefreshProfileSwitchTargets(string oldName, string newName)
            => _actionsXml.RefreshProfileTargets(oldName, newName);

        // Phase10-Step4 W6: 名前変更後に、Controls のボタンに割り当てた Load Profile の切り替え先が、旧名のまま（切り替わらない）になる不具合の予防。
        // 各プロファイルのファイルの <Control><Special> を読み、Type="Profile" の ProfileName が旧名なら、新名へ書き換えて保存する（ReplaceProfileSwitchTarget の Controls 版）。
        // 実行中のスロットのプロファイルは、名前変更の直後の再適用（MainWindow）でファイルから読み直されるため、ファイルの書き換えだけでよい
        public IReadOnlyList<string> ReplaceControlProfileSwitchTarget(string oldName, string newName)
        {
            var changed = new List<string>();
            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName) ||
                string.Equals(oldName, newName, StringComparison.Ordinal))
                return changed;

            string[] files;
            try
            {
                if (!Directory.Exists(ProfilesPath))
                    return changed;
                files = Directory.GetFiles(ProfilesPath, "*.xml");
            }
            catch (Exception ex)
            {
                AppLogger.Log(AppLogLevel.Error, $"[DI] ProfileRepository: cannot enumerate profiles for Controls profile switch target update: {ex.Message}", LogOptions.UserFacing);
                return changed;
            }

            foreach (string file in files)
            {
                try
                {
                    lock (ProfileXmlStore.XmlIoLock)
                    {
                        var doc = new System.Xml.XmlDocument { PreserveWhitespace = true };
                        doc.Load(file);

                        bool modified = false;
                        System.Xml.XmlNodeList nodes = doc.SelectNodes("DS4Windows/Control/Special/*[@Type='Profile']/ProfileName");
                        if (nodes == null || nodes.Count == 0)
                            continue;

                        foreach (System.Xml.XmlNode node in nodes)
                        {
                            if (string.Equals(node.InnerText, oldName, StringComparison.CurrentCultureIgnoreCase))
                            {
                                node.InnerText = newName;
                                modified = true;
                            }
                        }

                        if (!modified)
                            continue;

                        doc.Save(file);
                        changed.Add(Path.GetFileNameWithoutExtension(file));
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.LogUser(AppLogLevel.Error, "Log_Repo_SwitchTargetUpdateFailed", Path.GetFileName(file), ex.Message);
                }
            }

            if (changed.Count > 0)
            {
                AppLogger.LogUser(AppLogLevel.Info, "Log_Repo_SwitchTargetRenamed", oldName, newName, changed.Count, string.Join(", ", changed));
            }

            return changed;
        }

        // ---- Phase5-Step13-5: デバイス別・割当済みSpecialAction名リスト ----
        public List<string>[] ProfileActions => _config.profileActions;
        public void CacheExtraProfileInfo(int deviceIndex) => _config.CacheExtraProfileInfo(deviceIndex);

        // ---- Phase5-Step13-6: プロファイルのカスタムフラグキャッシュ更新 ----
        public void CacheProfileCustomsFlags(int deviceIndex) => _config.CacheProfileCustomsFlags(deviceIndex);

        // ---- Phase5-Step13-7: 一時プロファイル読込（スワイプ操作等での即時切替用） ----
        // Phase7-Step3c W3: 旧 Global.LoadTempProfile の処理をここへ移した（xinputChange は従来どおり読み込みへ渡さない）。
        public bool LoadTempProfile(int deviceIndex, string profileName, bool launchProgram, IProfileLoadTarget control, bool xinputChange = true)
        {
            _config.loggedInvalidActions.Clear();
            bool result = _legacyProfileXml.LoadProfile(deviceIndex, launchProgram, control, Path.Combine(ProfilesPath, $"{profileName}.xml"));
            if (result)
            {
                _profileSettings.SetTempProfileName(deviceIndex, profileName);
                _profileSettings.SetUseTempProfile(deviceIndex, true);
                _profileSettings.SetTempProfileDistance(deviceIndex, profileName.ToLower().Contains("distance"));
            }

            return result;
        }

        // ---- Phase5-Step13-7 Tier4: プロファイル未定義アクションの警告ログ出力 ----
        public void EmitMissingActionLogsForDevice(int deviceIndex, bool forceEmit = false, string overrideProfileName = null)
            => _config.EmitMissingActionLogsForDevice(deviceIndex, forceEmit, overrideProfileName);

        // ---- Phase6-Step7-1: App.xaml.cs（Post-Host）の Global 直接参照解消 ----
        // Global.SaveAsProfile は戻り値を捨てる void のため、同じ実体（BackingStore.SaveAsProfile）を直接呼んで成否を返す。
        public bool SaveAsProfile(int deviceIndex, string profileName)
            => _legacyProfileXml.SaveAsProfile(deviceIndex, profileName);

        public bool LoadLinkedProfiles()
            => _linkedProfilesXml.LoadLinkedProfiles();

        // ---- Phase6-Step10-3b-1: アクションの名前変更・削除の、全プロファイルへの反映 ----
        public IReadOnlyList<string> ReplaceActionReference(string oldName, string newName)
        {
            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName) ||
                string.Equals(oldName, newName, StringComparison.Ordinal))
                return Array.Empty<string>();

            return UpdateActionReferences(oldName, newName);
        }

        public IReadOnlyList<string> RemoveActionReference(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Array.Empty<string>();

            return UpdateActionReferences(name, null);
        }

        public IReadOnlyList<string> GetAppliedProfilesUsingAction(string actionName)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(actionName))
                return result;

            List<string>[] actions = ProfileActions;
            ProfileSlotNames paths = ProfilePath;
            if (actions == null || paths == null)
                return result;

            int slotCount = Math.Min(_environment.ControllerSlotLimit, Math.Min(actions.Length, paths.Length));
            for (int i = 0; i < slotCount; i++)
            {
                if (actions[i] != null && !string.IsNullOrEmpty(paths[i]) &&
                    actions[i].Contains(actionName, StringComparer.Ordinal) &&
                    !result.Contains(paths[i], StringComparer.OrdinalIgnoreCase))
                {
                    result.Add(paths[i]);
                }
            }

            return result;
        }

        // newName が null のときは、oldName を取り除く。
        // XmlIoLock（同一 XML 排他ロック）の下で、プロファイルごとに読み込み → 置換 → 保存を行う。
        private IReadOnlyList<string> UpdateActionReferences(string oldName, string newName)
        {
            var changed = new List<string>();
            string[] files;
            try
            {
                if (!Directory.Exists(ProfilesPath))
                    return changed;
                files = Directory.GetFiles(ProfilesPath, "*.xml");
            }
            catch (Exception ex)
            {
                AppLogger.Log(AppLogLevel.Error, $"[DI] ProfileRepository: cannot enumerate profiles for action reference update: {ex.Message}", LogOptions.UserFacing);
                return changed;
            }

            foreach (string file in files)
            {
                try
                {
                    lock (ProfileXmlStore.XmlIoLock)
                    {
                        var doc = new System.Xml.XmlDocument { PreserveWhitespace = true };
                        doc.Load(file);
                        System.Xml.XmlNode node = doc.SelectSingleNode("DS4Windows/ProfileActions");
                        if (node == null || string.IsNullOrEmpty(node.InnerText))
                            continue;

                        string[] names = node.InnerText.Split('/');
                        if (!names.Contains(oldName, StringComparer.Ordinal))
                            continue;

                        var result = new List<string>();
                        foreach (string n in names)
                        {
                            string value = string.Equals(n, oldName, StringComparison.Ordinal) ? newName : n;
                            if (value == null || result.Contains(value, StringComparer.Ordinal))
                                continue;
                            result.Add(value);
                        }

                        node.InnerText = string.Join("/", result);
                        doc.Save(file);
                        changed.Add(Path.GetFileNameWithoutExtension(file));
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.LogUser(AppLogLevel.Error, "Log_Repo_ActionRefUpdateFailed", Path.GetFileName(file), ex.Message);
                }
            }

            if (changed.Count > 0)
            {
                if (newName == null)
                    AppLogger.LogUser(AppLogLevel.Info, "Log_Repo_ActionRefRemoved", oldName, changed.Count, string.Join(", ", changed));
                else
                    AppLogger.LogUser(AppLogLevel.Info, "Log_Repo_ActionRefRenamed", oldName, newName, changed.Count, string.Join(", ", changed));
            }

            return changed;
        }

        // Phase7-Step3c W2: LoadBlankProfile は削除した（IProfilePresetLoader.LoadBlank に統合）。


        // ---- Phase6-Step2-2 (PR-2): リンクプロファイル解決 ----
        public bool ContainsLinkedProfile(string serial)
        {
            string tempSerial = serial.Replace(":", string.Empty);
            return _config.linkedProfiles.ContainsKey(tempSerial);
        }

        public string GetLinkedProfile(string serial)
        {
            string temp = string.Empty;
            string tempSerial = serial.Replace(":", string.Empty);
            if (_config.linkedProfiles.ContainsKey(tempSerial))
            {
                temp = _config.linkedProfiles[tempSerial];
            }

            return temp;
        }
    }
}