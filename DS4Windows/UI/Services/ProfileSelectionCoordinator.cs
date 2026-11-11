using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;
using DS4Windows.UI.Abstractions;
using DS4Windows.UI.ViewModels;

namespace DS4Windows.UI.Services
{
    /// <summary>
    /// メイン画面のプロファイルの選択・編集画面の生成・保存後の一覧の同期を受け持つ（<see cref="MainWindow"/> から責務ごと移したもの。
    /// メソッドの本体は移動のみ）。窓の部品（コンボボックス、ドック、トレイ）には触れず、窓が必要な部分だけを自分で行う。
    /// </summary>
    public sealed class ProfileSelectionCoordinator
    {
        private readonly IEditorWindowFactory _editorWindowFactory;
        private readonly IProfileRepository _profileRepo;
        private readonly IProfileSlotApplier _profileSlotApplier;
        private readonly IAppSettingsService _appSettings;
        private readonly IEnvironmentService _environmentService;
        private readonly IControllerSlotQuery _slots;

        public ProfileSelectionCoordinator(IEditorWindowFactory editorWindowFactory, IProfileRepository profileRepo,
            IProfileSlotApplier profileSlotApplier, IAppSettingsService appSettings,
            IEnvironmentService environmentService, IControllerSlotQuery slots)
        {
            _editorWindowFactory = editorWindowFactory ?? throw new ArgumentNullException(nameof(editorWindowFactory));
            _profileRepo = profileRepo ?? throw new ArgumentNullException(nameof(profileRepo));
            _profileSlotApplier = profileSlotApplier ?? throw new ArgumentNullException(nameof(profileSlotApplier));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            _slots = slots ?? throw new ArgumentNullException(nameof(slots));
        }

        /// <summary>
        /// コントローラーの行のコンボボックスで選ばれたプロファイルを適用する。適用した（トレイのメニューを作り直す必要がある）とき true。
        /// </summary>
        public bool ApplySelection(int idx, CompositeDeviceModel item)
        {
            // ★統合抑制フラグ: バックエンドからのプロファイル変更同期中は手動適用をスキップ（無限ループ防止）
            if (item.suppressSelectedIndexChanged)
            {
                AppLogger.LogDebug($"SelectProfCombo_SelectionChanged: Suppressed for device {idx}");
                return false;
            }

            if (item.SelectedIndex > -1 && item.SelectedIndex < item.ProfileListCol.Count)
            {
                string prof = item.ProfileListCol[item.SelectedIndex].Name;

                // 既に同じプロファイルが適用されていればスキップ
                if (_profileRepo.SelectedProfile[idx] == prof)
                {
                    AppLogger.LogDebug($"SelectProfCombo_SelectionChanged: Profile '{prof}' already applied for device {idx}, skipping");
                    return false;
                }

                AppLogger.LogDebug($"SelectProfCombo_SelectionChanged: Applying profile '{prof}' for device {idx}");

                // ★共通適用窓口 ApplyProfileToSlot を呼び出す（すべての切り替え手段を統一）
                _profileSlotApplier.ApplyToSlot(idx, prof, ProfileChangeSource.Manual);
                return true;
            }
            return false;
        }

        /// <summary>トレイのメニューで選ばれたプロファイルを、接続中のコントローラーへ適用する。</summary>
        public void ApplyFromTray(int idx, string profile, CompositeDeviceModel devitem)
        {
            if (devitem != null && devitem.Device != null)
            {
                string prolog = string.Format(Properties.Resources.UsingProfile,
                    (idx + 1).ToString(), profile, $"{devitem.Device.Battery}");
                _profileSlotApplier.ApplyFromUi(idx, profile,
                    ProfileChangeSource.Manual, prolog);
            }
        }

        public ProfileEditor CreateEditor(int targetDevice) => _editorWindowFactory.CreateProfileEditor(targetDevice);

        /// <summary>編集画面を開いたとき、アクションが見つからないログを 1 回出す（開くたびに出す）。</summary>
        public void EmitMissingActionLogsForEditor(ProfileEntity entity)
        {
            try
            {
                // Force emitting missing-action logs for this editor open (ignore suppression).
                // Pass explicit profile name to avoid confusion with device's currently-assigned profile.
                string profileNameForLog = entity != null ? entity.Name : "(new profile)";
                // 読み込んだプロファイルのアクションを調べるため、実機ではなく作業スロットを渡す
                _profileRepo.EmitMissingActionLogsForDevice(ControllerLimits.TestProfileIndex, true, profileNameForLog);
            }
            catch (Exception ex)
            {
                AppLogger.LogError($"[MainWindow] Failed to emit missing action logs: {ex.Message}");
            }
        }

        /// <summary>
        /// プロファイル保存・更新・リネーム時の4段階トランザクション同期。
        /// ディスク正本からリストを再構築した上で、各スロットの選択復元および最新設定のホットリロードを一括実行する。
        /// </summary>
        /// <param name="oldProfile">Rename時の旧プロファイル名（保存・更新時はnull）</param>
        /// <param name="newProfile">Rename時の新プロファイル名（保存・更新時はnull）</param>
        /// <param name="targetProfiles">
        /// 複数のプロファイルの XML が書き換えられた場合（アクションの除去・名前変更）の対象一覧。
        /// 指定すると、これらのプロファイルを使っているスロットだけに再適用する（oldProfile／newProfile は null にする）
        /// </param>
        /// <param name="profileListHolder">画面のプロファイル一覧</param>
        /// <param name="saveAutoProfiles">自動プロファイルの設定の保存（名前変更の直後に呼ぶ。自動プロファイルがなければ null）</param>
        public void SyncProfileListAndControllers(ProfileList profileListHolder, Action saveAutoProfiles,
            string oldProfile = null, string newProfile = null, IReadOnlyCollection<string> targetProfiles = null)
        {
            int slotCount = _environmentService.ControllerSlotLimit;

            string[] activeProfiles = new string[slotCount];
            try
            {
                for (int i = 0; i < slotCount; i++)
                {
                    activeProfiles[i] = _profileRepo.ProfilePath[i] ?? string.Empty;
                }

                // ② 【SSOT 再構築 (Rebuild)】
                bool isRename = !string.IsNullOrWhiteSpace(oldProfile) && !string.IsNullOrWhiteSpace(newProfile);
                if (isRename)
                {
                    // 実行時の保持（スロットの現在・直前・選択、一時プロファイル、リンク、自動プロファイルのルール）は、プロファイルの固定 ID を保管する窓口のため、
                    // 名前を変えても、読み出す名前が自動で現在の名前になる。ここで配列や一覧を書き換える処理は、不要になった。
                    // 保存先のファイルの名前と ID を、現在の状態で書き直す（名前変更の直後に、1 回）
                    _profileRepo.SaveLinkedProfiles();
                    saveAutoProfiles?.Invoke();
                    _appSettings.Save();
                    // SA（Actions.xml）の切り替え先は、ID から現在の名前に引き直して保存する
                    _profileRepo.RefreshProfileSwitchTargets(oldProfile, newProfile);
                    // Controls の Load Profile の切り替え先（各プロファイルの <Control><Special>）は、ID のない古いプロファイルのファイルのため、旧名を新名へ書き換える処理を残す
                    // （TODO（Phase10-Step4d W7）: 全プロファイルのファイルに ID を書き足す移行ができたら撤去する。ReplaceActionReference も同じ）
                    _profileRepo.ReplaceControlProfileSwitchTarget(oldProfile, newProfile);
                }

                // ディスク全XMLを正（SSOT）としてプロファイル一覧を再構築
                // （※isProfileSyncing中のため、Clear()が走ってもTargetExceptionクラッシュは完全に防止される）
                profileListHolder.Refresh();

                // ③ 【復元・再選択 (Re-attach)】
                // メモリ上の ProfilePath を確定させ、ComboBox の選択状態を復元
                for (int i = 0; i < slotCount; i++)
                {
                    if (!string.IsNullOrEmpty(activeProfiles[i]))
                    {
                        _profileRepo.ProfilePath[i] = activeProfiles[i];
                    }
                }
            }
            finally
            {
            }

            // ④ 【再適用・ホットリロード (Re-apply)】
            // 保存またはリネームの影響を受けたスロットに対し、共通窓口経由で再適用を実行
            string targetProfile = !string.IsNullOrWhiteSpace(newProfile) ? newProfile : oldProfile;
            for (int i = 0; i < slotCount; i++)
            {
                bool matches = targetProfiles != null
                    ? !string.IsNullOrEmpty(activeProfiles[i]) && targetProfiles.Any(p =>
                        string.Equals(p, activeProfiles[i], StringComparison.CurrentCultureIgnoreCase))
                    : string.IsNullOrEmpty(targetProfile) ||
                      string.Equals(activeProfiles[i], targetProfile, StringComparison.CurrentCultureIgnoreCase);
                // コントローラーが接続されているスロットにだけ適用する。サービスの停止中は、ProfilePath が同じ名前のまま残っているため、
                // 接続していないスロットにも適用され、無意味なログと通知が出ていた。プロファイルの XML は保存済みで、次の接続時に読まれる
                if (matches && IsSlotConnected(i))
                {
                    _profileSlotApplier.ApplyToSlot(i, activeProfiles[i], ProfileChangeSource.Manual);
                }
            }
        }

        // そのスロットに、接続中のコントローラーがあるか（サービス停止中は、どのスロットも false）
        private bool IsSlotConnected(int slot)
        {
            return _slots.GetSlot(slot) != null;
        }
    }
}
