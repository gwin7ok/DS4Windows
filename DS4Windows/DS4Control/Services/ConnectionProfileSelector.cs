using System;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-E E-6a（Phase9-E6-Plan.md）: <see cref="IConnectionProfileSelector"/> の実装（DI の Singleton。状態なし）。
    /// 旧 <c>ControlService.PrepareConnectedInputControllerSettingEvents</c> の、プロファイルの選択と適用の部分
    /// （初回接続／再接続、Linked、適用、一時プロファイルのときの束縛の差し替え）を、そのまま移したもの。
    /// 値の特性化は <c>Phase9E6ConnectionProfileSelectorTests</c>（偽物の依存を使った差分テスト）が守る。
    /// </summary>
    public sealed class ConnectionProfileSelector : IConnectionProfileSelector
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly IProfileRepository _profileRepository;
        private readonly IDeviceStateService _deviceStateService;
        private readonly IProfileSlotApplier _profileSlotApplier;

        public ConnectionProfileSelector(IProfileSettingsService profileSettings, IProfileRepository profileRepository,
            IDeviceStateService deviceStateService, IProfileSlotApplier profileSlotApplier)
        {
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
            _deviceStateService = deviceStateService ?? throw new ArgumentNullException(nameof(deviceStateService));
            _profileSlotApplier = profileSlotApplier ?? throw new ArgumentNullException(nameof(profileSlotApplier));
        }

        public bool ApplyOnConnect(int index, bool useAutoProfile, bool isValidSerial, string macAddress)
        {
            bool profileLoaded = false;

            // 「初回接続か」「直前のプロファイル」は、スロット番号ではなくコントローラー（MAC）ごとに判定する
            // （別のコントローラーが空いたスロットに入っても、そのコントローラーにとっては初回。Reconnect-Profile-PerController-Fix-Plan.md）。
            // 有効なシリアルがない（MAC が使えない）ときは、従来のスロット単位。
            string connectedMac = isValidSerial ? macAddress : null;

            DS4Windows.AppLogger.LogDebug($"PrepareConnectedInputController: device={index}, useAutoProfile={useAutoProfile}, isFirstConnection={_deviceStateService.IsFirstConnection(index, connectedMac)}");

            if (!useAutoProfile)
            {
                // ===== プロファイル選択ロジック =====
                string profileToApply;

                if (_deviceStateService.IsFirstConnection(index, connectedMac))
                {
                    // 初回接続: LinkedProfiles.xmlまたはOlderProfilePathからプロファイルを選択
                    DS4Windows.AppLogger.LogDebug($"FIRST CONNECTION detected for device {index}");

                    if (isValidSerial && _profileRepository.ContainsLinkedProfile(macAddress))
                    {
                        // Linked登録済み → Linkedを適用
                        profileToApply = _profileRepository.GetLinkedProfile(macAddress);
                        DS4Windows.AppLogger.LogDebug($"Using LINKED profile: '{profileToApply}' for device {index}");
                    }
                    else
                    {
                        // Linked未登録 → OlderProfilePathを使用
                        profileToApply = _profileRepository.OlderProfilePath[index];
                        DS4Windows.AppLogger.LogDebug($"Using OLDER profile: '{profileToApply}' for device {index}");
                    }

                    _deviceStateService.MarkConnected(index, connectedMac); // 初回接続完了マーク
                }
                else
                {
                    // 再接続: そのコントローラーが直前に適用していたプロファイル。記憶がなければ、既存のSelectedProfile（スロット）を使用
                    profileToApply = _deviceStateService.GetRememberedProfile(connectedMac);
                    if (string.IsNullOrWhiteSpace(profileToApply))
                        profileToApply = _profileRepository.SelectedProfile[index];
                    DS4Windows.AppLogger.LogDebug($"RECONNECTION detected for device {index}, using existing profile: '{profileToApply}'");
                }

                // ===== 共通処理: プロファイル設定とUI状態の更新 =====
                _profileRepository.SelectedProfile[index] = profileToApply;
                _deviceStateService.RememberProfile(connectedMac, profileToApply);   // このコントローラーの「直前のプロファイル」（適用のたびにも更新する）

                // LinkedProfiles.xmlの状態を確認してLinkedProfileUIとlinkedProfileCheckを設定
                if (isValidSerial && _profileRepository.ContainsLinkedProfile(macAddress))
                {
                    string linkedProfile = _profileRepository.GetLinkedProfile(macAddress);
                    _profileRepository.LinkedProfileUI[index] = linkedProfile;
                    _profileSettings.SetLinkedProfileCheck(index, true);
                }
                else
                {
                    _profileRepository.LinkedProfileUI[index] = string.Empty;
                    _profileSettings.SetLinkedProfileCheck(index, false);
                }

                // プロファイル適用（Phase5-Step14 フェーズD: 共通窓口 Global.ApplyProfileToSlot 経由に統一。契機5統合）
                DS4Windows.AppLogger.LogDebug($"PrepareConnectedInputController: About to call ApplyProfileToSlot with '{profileToApply}'");
                profileLoaded = _profileSlotApplier.ApplyToSlot(index, profileToApply, ProfileChangeSource.ControlService);
                DS4Windows.AppLogger.LogDebug($"PrepareConnectedInputController: ApplyProfileToSlot returned {profileLoaded}");
            }
            else
            {
                // Phase8-Step6 W2: 一時プロファイル／AutoProfile を使う接続は、プロファイルを適用し直さない。切断で空になった束縛を、ここで差し替える
                _profileSlotApplier.PublishSlot(index);
            }

            return profileLoaded;
        }
    }
}
