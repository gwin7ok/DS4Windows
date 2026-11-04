using System;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.8e: <see cref="IControlExtrasPort"/> の本番の実装。旧 <c>Mapping.ProcessControlSettingAction</c> の Extras の枝と同じ操作を、この窓口の内側に閉じ込める。
    /// 振動は <see cref="IDeviceRumbleService"/>（ブースト込み。旧 <c>ControlService.setRumble</c> と同じ、スロット上限・コントローラーの有無の検査つき。コントローラーがいなければ何もしない）、ライトバーは静的な <c>DS4LightBar</c> の配列、感度は <c>ButtonMouseInfo</c>。
    /// Phase13-Step6 6-b: <c>Func&lt;ControlService&gt;</c> をやめ、狭い窓口を直接注入で受ける。
    /// </summary>
    public sealed class DefaultControlExtrasPort : IControlExtrasPort
    {
        private readonly IDeviceStateAccessor _devices;
        private readonly IDeviceRumbleService _rumble;
        private readonly IEnvironmentService _environment;
        private readonly IProfileSettingsService _profileSettings;

        public DefaultControlExtrasPort(IDeviceStateAccessor devices, IDeviceRumbleService rumble, IEnvironmentService environment, IProfileSettingsService profileSettings)
        {
            _devices = devices ?? throw new ArgumentNullException(nameof(devices));
            _rumble = rumble ?? throw new ArgumentNullException(nameof(rumble));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
        }

        private static bool InRange(int deviceIndex) => deviceIndex >= 0 && deviceIndex < DS4LightBar.forcelight.Length;

        public void SetRumble(int deviceIndex, byte heavy, byte light)
        {
            if (!InRange(deviceIndex)) return;
            // 旧 ControlService.setRumble と同じ（スロット上限の検査 → コントローラーがいれば、ブースト・反転つきの振動）
            if (deviceIndex < _environment.ControllerSlotLimit)
            {
                DS4Device device = _devices.GetController(deviceIndex);
                if (device != null)
                    _rumble.SetDevRumble(device, heavy, light, deviceIndex);
            }
        }

        public void ForceLightbar(int deviceIndex, DS4Color color, byte flash)
        {
            if (!InRange(deviceIndex)) return;
            DS4LightBar.forcedColor[deviceIndex] = color;
            DS4LightBar.forcedFlash[deviceIndex] = flash;
            DS4LightBar.forcelight[deviceIndex] = true;
        }

        public void ClearForcedLightbar(int deviceIndex)
        {
            if (!InRange(deviceIndex)) return;
            DS4LightBar.forcelight[deviceIndex] = false;
            DS4LightBar.forcedFlash[deviceIndex] = 0;
        }

        public void TrySetTemporarySensitivity(int deviceIndex, int sensitivity)
        {
            ButtonMouseInfo info = InfoOf(deviceIndex);
            if (info == null) return;

            if (info.tempButtonSensitivity == -1)
            {
                info.tempButtonSensitivity = sensitivity;
                info.SetActiveButtonSensitivity(sensitivity);
            }
        }

        public void RestoreSensitivity(int deviceIndex)
        {
            ButtonMouseInfo info = InfoOf(deviceIndex);
            if (info == null) return;

            if (info.tempButtonSensitivity != -1)
            {
                info.SetActiveButtonSensitivity(info.buttonSensitivity);
                info.tempButtonSensitivity = -1;
            }
        }

        private ButtonMouseInfo InfoOf(int deviceIndex)
        {
            if (!InRange(deviceIndex)) return null;
            ButtonMouseInfo[] infos = _profileSettings.ButtonMouseInfos;
            return infos != null && deviceIndex < infos.Length ? infos[deviceIndex] : null;
        }
    }
}
