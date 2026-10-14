using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using DS4Windows.DI;
using DS4WinWPF.DS4Control.DTOXml;

namespace DS4Windows
{
    public class ProfileRepository : IProfileRepository
    {
        private readonly object _fileLock = new object();
        private readonly IProfileSettingsService _profileSettings;
        private readonly IProfileXmlStore _profileXmlStore;
        private readonly IPathService _pathService;

        // Phase6-Step10-4-2b: 全ての依存を必須化（Global.*Instance／AppHost のフォールバックを廃止）。
        // ControlService は ControlService → IProfileRepository の依存循環を避けるため、遅延解決（Func）で受け取る。
        private readonly Func<ControlService> _controlProvider;

        public ProfileRepository(IProfileSettingsService profileSettings,
            IProfileXmlStore profileXmlStore, IPathService pathService,
            Func<ControlService> controlProvider)
        {
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _profileXmlStore = profileXmlStore ?? throw new ArgumentNullException(nameof(profileXmlStore));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _controlProvider = controlProvider ?? throw new ArgumentNullException(nameof(controlProvider));
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

        public bool ProfileExists(string profileName)
        {
            string path = GetProfilePath(profileName);
            return !string.IsNullOrEmpty(path) && File.Exists(path);
        }

        public IReadOnlyList<string> GetProfileNames()
        {
            lock (_fileLock)
            {
                try
                {
                    if (!Directory.Exists(ProfilesPath))
                        return Array.Empty<string>();

                    var files = Directory.GetFiles(ProfilesPath, "*.xml");
                    return files.Select(f => Path.GetFileNameWithoutExtension(f)).ToList();
                }
                catch
                {
                    return Array.Empty<string>();
                }
            }
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
                    ControlService control = _controlProvider();
                    if (control == null)
                        return false;

                    // Phase5-Step2: Global.LoadProfileへの委譲をやめ、XML I/O(IProfileXmlStore)と
                    // 状態調整ロジック(旧Global.LoadProfile内で行っていたもの)をここに直接内包する。
                    // 挙動はGlobal.LoadProfileと完全に同一に維持する。
                    Global.loggedInvalidActions.Clear();
                    _profileXmlStore.LoadProfileXml(deviceIndex, false, control, "", false, true);
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
                        AppLogger.LogToGui($"Failed to save profile: invalid path for '{profileName}'", true);
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
                        AppLogger.LogToGui($"Failed to save profile '{profileName}' for device {deviceIndex + 1}", true);
                        if (AppLogger.IsTraceEnabled)
                            AppLogger.LogTrace($"[DI] SaveProfile failed: Slot {deviceIndex}, Profile '{profileName}'");
                    }

                    return saveSuccess;
                }
                catch (Exception ex)
                {
                    AppLogger.LogToGui($"Failed to save profile '{profileName}' for device {deviceIndex + 1}: {ex.Message}", true);
                    if (AppLogger.IsTraceEnabled)
                        AppLogger.LogTrace($"[DI] SaveProfile failed with exception: Slot {deviceIndex}, Profile '{profileName}', error={ex}");
                    return false;
                }
            }
        }

        // ====================================================================================
        // --- 旧 DS4Windows.DS4Control.ProfileRepository から統合した DTO ベースの I/O ロジック ---
        // ====================================================================================
        public bool LoadProfile(string filePath, int deviceIndex, BackingStore destination)
        {
            lock (_fileLock)
            {
                try
                {
                    if (!File.Exists(filePath)) return false;

                    XmlSerializer serializer = new XmlSerializer(typeof(ProfileDTO), ProfileDTO.GetAttributeOverrides());

                    using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        if (serializer.Deserialize(fs) is ProfileDTO dto)
                        {
                            dto.DeviceIndex = deviceIndex;
                            dto.MapTo(destination);

                            // プロファイル読み込み完了後、サブ設定オブジェクトの変更バブリングを再配線
                            _profileSettings?.WireSubSettingsEvents(deviceIndex);

                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.LogError($"Failed to load profile from {filePath}: {ex.Message}");
                }
                return false;
            }
        }

        public bool SaveProfile(string filePath, int deviceIndex, BackingStore source)
        {
            lock (_fileLock)
            {
                try
                {
                    ProfileDTO dto = new ProfileDTO
                    {
                        DeviceIndex = deviceIndex
                    };

                    dto.MapFrom(source);

                    XmlSerializer serializer = new XmlSerializer(typeof(ProfileDTO), ProfileDTO.GetAttributeOverrides());

                    string dir = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        serializer.Serialize(fs, dto);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.LogError($"Failed to save profile to {filePath}: {ex.Message}");
                }
                return false;
            }
        }
        // ====================================================================================

        public bool LoadDefaultProfile(int deviceIndex)
        {
            _profileSettings?.ResetToDefaults(deviceIndex);
            if (AppLogger.IsTraceEnabled)
                AppLogger.LogTrace($"[DI] ProfileRepository.LoadDefaultProfile: Slot {deviceIndex} reset to defaults via DI");
            return true;
        }

        public bool LoadProfileToSlot(int deviceIndex, string profileName)
        {
            if (deviceIndex < 0 || deviceIndex >= ProfileSettingsService.TEST_PROFILE_ITEM_COUNT)
                return false;

            if (string.IsNullOrWhiteSpace(profileName))
            {
                return LoadDefaultProfile(deviceIndex);
            }

            return LoadProfile(deviceIndex, profileName);
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
        public string[] ProfilePath => Global.ProfilePath;
        public string[] OlderProfilePath => Global.OlderProfilePath;
        public string[] SelectedProfile => Global.SelectedProfile;
        public string[] LinkedProfileUI => Global.LinkedProfileUI;

        public event EventHandler<SelectedProfileChangedEventArgs> SelectedProfileChanged
        {
            add => Global.SelectedProfileChanged += value;
            remove => Global.SelectedProfileChanged -= value;
        }

        public void RaiseSelectedProfileChanged(int deviceIndex, string profileName)
            => Global.RaiseSelectedProfileChanged(deviceIndex, profileName);

        // ---- Phase5-Step13-2: LinkedProfile（コントローラーMAC単位のプロファイル紐付け）管理 ----
        public void ChangeLinkedProfile(string serial, string profile)
            => Global.changeLinkedProfile(serial, profile);

        public void RemoveLinkedProfile(string serial)
            => Global.removeLinkedProfile(serial);

        public bool SaveLinkedProfiles()
            => Global.SaveLinkedProfiles();

        // ---- Phase5-Step13-5: デバイス別・割当済みSpecialAction名リスト ----
        public List<string>[] ProfileActions => Global.ProfileActions;
        public void CacheExtraProfileInfo(int deviceIndex) => Global.CacheExtraProfileInfo(deviceIndex);

        // ---- Phase5-Step13-6: プロファイルのカスタムフラグキャッシュ更新 ----
        public void CacheProfileCustomsFlags(int deviceIndex) => Global.CacheProfileCustomsFlags(deviceIndex);

        // ---- Phase5-Step13-7: 一時プロファイル読込（スワイプ操作等での即時切替用） ----
        public bool LoadTempProfile(int deviceIndex, string profileName, bool launchProgram, ControlService control, bool xinputChange = true)
            => Global.LoadTempProfile(deviceIndex, profileName, launchProgram, control, xinputChange);

        // ---- Phase5-Step13-7 Tier4: プロファイル未定義アクションの警告ログ出力 ----
        public void EmitMissingActionLogsForDevice(int deviceIndex, bool forceEmit = false, string overrideProfileName = null)
            => Global.store.EmitMissingActionLogsForDevice(deviceIndex, forceEmit, overrideProfileName);

        // ---- Phase6-Step7-1: App.xaml.cs（Post-Host）の Global 直接参照解消 ----
        // Global.SaveAsProfile は戻り値を捨てる void のため、同じ実体（BackingStore.SaveAsProfile）を直接呼んで成否を返す。
        public bool SaveAsProfile(int deviceIndex, string profileName)
            => Global.store.SaveAsProfile(deviceIndex, profileName);

        public bool LoadLinkedProfiles()
            => Global.LoadLinkedProfiles();

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
            string[] paths = ProfilePath;
            if (actions == null || paths == null)
                return result;

            int slotCount = Math.Min(ControlService.CURRENT_DS4_CONTROLLER_LIMIT, Math.Min(actions.Length, paths.Length));
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
                AppLogger.LogToGui($"[DI] ProfileRepository: cannot enumerate profiles for action reference update: {ex.Message}", true);
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
                    AppLogger.LogToGui($"[DI] ProfileRepository: failed to update action reference in '{Path.GetFileName(file)}': {ex.Message}", true);
                }
            }

            if (changed.Count > 0)
            {
                string action = newName == null ? $"removed '{oldName}'" : $"renamed '{oldName}' to '{newName}'";
                AppLogger.LogToGui($"[DI] Action reference {action} in {changed.Count} profile(s): {string.Join(", ", changed)}", false);
            }

            return changed;
        }

        // ---- Phase6-Step8-1: ProfileEditor.xaml.cs の Global 直接参照解消 ----
        public void LoadBlankProfile(int deviceIndex, bool launchProgram, ControlService control, bool xinputChange = true, bool postLoad = true)
            => Global.LoadBlankDevProfile(deviceIndex, launchProgram, control, xinputChange, postLoad);


        // ---- Phase6-Step2-2 (PR-2): リンクプロファイル解決 ----
        public bool ContainsLinkedProfile(string serial) => Global.containsLinkedProfile(serial);

        public string GetLinkedProfile(string serial) => Global.getLinkedProfile(serial);
    }
}