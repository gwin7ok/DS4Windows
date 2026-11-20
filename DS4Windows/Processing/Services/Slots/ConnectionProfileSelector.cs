using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services.Slots
{
    /// <summary>
    /// <see cref="IConnectionProfileSelector"/> の実装（DI の Singleton。状態なし）。
    /// 旧 <c>ControlService.PrepareConnectedInputControllerSettingEvents</c> の、プロファイルの選択と適用の部分
    /// （初回接続／再接続、Linked、適用、一時プロファイルのときの束縛の差し替え）を、そのまま移したもの。
    /// 値の特性化は 対応するテスト（偽物の依存を使った差分テスト）が守る。
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
            ArgumentNullException.ThrowIfNull(profileSettings);
            _profileSettings = profileSettings;
            ArgumentNullException.ThrowIfNull(profileRepository);
            _profileRepository = profileRepository;
            ArgumentNullException.ThrowIfNull(deviceStateService);
            _deviceStateService = deviceStateService;
            ArgumentNullException.ThrowIfNull(profileSlotApplier);
            _profileSlotApplier = profileSlotApplier;
        }

        public bool ApplyOnConnect(int index, bool useAutoProfile, bool IsValidSerial, string? macAddress)
        {
            bool profileLoaded = false;

            // 「初回接続か」「直前のプロファイル」は、スロット番号ではなくコントローラー（MAC）ごとに判定する
            // （別のコントローラーが空いたスロットに入っても、そのコントローラーにとっては初回。Reconnect-Profile-PerController-Fix-Plan.md）。
            // 有効なシリアルがない（MAC が使えない）ときは、従来のスロット単位。
            string? connectedMac = IsValidSerial ? macAddress : null;

            if (DS4Windows.Core.Logging.AppLogger.IsDebugEnabled) DS4Windows.Core.Logging.AppLogger.LogDebug($"PrepareConnectedInputController: device={index}, useAutoProfile={useAutoProfile}, isFirstConnection={_deviceStateService.IsFirstConnection(index, connectedMac)}");

            if (!useAutoProfile)
            {
                // ===== プロファイル選択ロジック =====
                string? profileToApply;

                if (_deviceStateService.IsFirstConnection(index, connectedMac))
                {
                    // 初回接続: LinkedProfiles.xmlまたはOlderProfilePathからプロファイルを選択
                    if (DS4Windows.Core.Logging.AppLogger.IsDebugEnabled) DS4Windows.Core.Logging.AppLogger.LogDebug($"FIRST CONNECTION detected for device {index}");

                    if (IsValidSerial && _profileRepository.ContainsLinkedProfile(macAddress!)) // 有効なシリアルのときだけ MAC を使う
                    {
                        // Linked登録済み → Linkedを適用
                        profileToApply = _profileRepository.GetLinkedProfile(macAddress!); // 有効なシリアルのときだけ MAC を使う
                        if (DS4Windows.Core.Logging.AppLogger.IsDebugEnabled) DS4Windows.Core.Logging.AppLogger.LogDebug($"Using LINKED profile: '{profileToApply}' for device {index}");
                    }
                    else
                    {
                        // Linked未登録 → OlderProfilePathを使用
                        profileToApply = _profileRepository.OlderProfilePath[index];
                        if (DS4Windows.Core.Logging.AppLogger.IsDebugEnabled) DS4Windows.Core.Logging.AppLogger.LogDebug($"Using OLDER profile: '{profileToApply}' for device {index}");
                    }

                    _deviceStateService.MarkConnected(index, connectedMac); // 初回接続完了マーク
                }
                else
                {
                    // 再接続: そのコントローラーが直前に適用していたプロファイル。記憶がなければ、既存のSelectedProfile（スロット）を使用
                    profileToApply = _deviceStateService.GetRememberedProfile(connectedMac);
                    if (string.IsNullOrWhiteSpace(profileToApply))
                        profileToApply = _profileRepository.SelectedProfile[index];
                    if (DS4Windows.Core.Logging.AppLogger.IsDebugEnabled) DS4Windows.Core.Logging.AppLogger.LogDebug($"RECONNECTION detected for device {index}, using existing profile: '{profileToApply}'");
                }

                // ===== 共通処理: プロファイル設定とUI状態の更新 =====
                _profileRepository.SelectedProfile[index] = profileToApply;
                _deviceStateService.RememberProfile(connectedMac, profileToApply);   // このコントローラーの「直前のプロファイル」（適用のたびにも更新する）

                // LinkedProfiles.xmlの状態を確認してLinkedProfileUIとlinkedProfileCheckを設定
                if (IsValidSerial && _profileRepository.ContainsLinkedProfile(macAddress!)) // 有効なシリアルのときだけ MAC を使う
                {
                    string linkedProfile = _profileRepository.GetLinkedProfile(macAddress!); // 有効なシリアルのときだけ MAC を使う
                    _profileRepository.LinkedProfileUI[index] = linkedProfile;
                    _profileSettings.SetLinkedProfileCheck(index, true);
                }
                else
                {
                    _profileRepository.LinkedProfileUI[index] = string.Empty;
                    _profileSettings.SetLinkedProfileCheck(index, false);
                }

                // プロファイル適用（Phase5-Step14 フェーズD: 共通窓口 Global.ApplyProfileToSlot 経由に統一。契機5統合）
                if (DS4Windows.Core.Logging.AppLogger.IsDebugEnabled) DS4Windows.Core.Logging.AppLogger.LogDebug($"PrepareConnectedInputController: About to call ApplyProfileToSlot with '{profileToApply}'");
                profileLoaded = _profileSlotApplier.ApplyToSlot(index, profileToApply, ProfileChangeSource.ControlService);
                if (DS4Windows.Core.Logging.AppLogger.IsDebugEnabled) DS4Windows.Core.Logging.AppLogger.LogDebug($"PrepareConnectedInputController: ApplyProfileToSlot returned {profileLoaded}");
            }
            else
            {
                // 一時プロファイル／AutoProfile を使う接続は、プロファイルを適用し直さない。切断で空になった束縛を、ここで差し替える
                _profileSlotApplier.PublishSlot(index);
            }

            return profileLoaded;
        }
    }
}
