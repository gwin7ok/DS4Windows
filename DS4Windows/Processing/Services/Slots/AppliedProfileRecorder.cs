using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services
{
    /// <summary><see cref="IAppliedProfileRecorder"/> の実装（DI の Singleton）。<c>ControlService.RememberAppliedProfile</c> をそのまま移したもの。</summary>
    public sealed class AppliedProfileRecorder : IAppliedProfileRecorder
    {
        private readonly IProfileRepository _profileRepository;
        private readonly IProfileSettingsService _profileSettings;
        private readonly IDeviceStateService _deviceStateService;
        private readonly InputDeviceCore[] _ds4Controllers;

        public AppliedProfileRecorder(IProfileRepository profileRepository, IProfileSettingsService profileSettings,
            IDeviceStateService deviceStateService, IDeviceSlotState slots)
        {
            _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _deviceStateService = deviceStateService ?? throw new ArgumentNullException(nameof(deviceStateService));
            _ds4Controllers = (slots ?? throw new ArgumentNullException(nameof(slots))).Controllers;
        }

        public void Attach()
        {
            _profileRepository.SelectedProfileChanged += RememberAppliedProfile;
        }

        // 再接続（同じコントローラー）のとき、切断前のプロファイルを適用するため（Reconnect-Profile-PerController-Fix-Plan.md）。適用のスレッドから呼ばれる。
        private void RememberAppliedProfile(object sender, SelectedProfileChangedEventArgs e)
        {
            try
            {
                int slot = e.DeviceIndex;
                if (slot < 0 || slot >= _ds4Controllers.Length) return;

                InputDeviceCore device = _ds4Controllers[slot];
                if (device == null || !device.isValidSerial()) return;

                // 一時プロファイル（自動プロファイル、一時の Load Profile）は記憶しない。
                // 記憶すると、切断前に自動プロファイル（verifyG）だった場合、再接続でそれが「通常のプロファイル」として最初から適用されてしまう。
                // 一時プロファイルの適用は LoadTempProfile が UseTempProfile を立てたあとで、このイベントが発火する
                if (_profileSettings.GetUseTempProfile(slot)) return;

                _deviceStateService.RememberProfile(device.getMacAddress(), e.ProfileName);
            }
            catch
            {
                // 記憶できなくても、適用を止めない（再接続は、スロットの SelectedProfile に戻る）
            }
        }
    }
}
