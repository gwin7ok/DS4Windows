using System;
using DS4Windows.DI;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.8e: <see cref="IControlExtrasPort"/> の本番の実装。旧 <c>Mapping.ProcessControlSettingAction</c> の Extras の枝と同じ操作を、この窓口の内側に閉じ込める。
    /// 振動は <c>ControlService.setRumble</c>（ブースト込み。コントローラーがいなければ何もしない）、ライトバーは静的な <c>DS4LightBar</c> の配列、感度は <c>ButtonMouseInfo</c>。
    /// <c>ControlService</c> は後から作られるため、遅延（<c>Func</c>）で受ける。
    /// </summary>
    public sealed class DefaultControlExtrasPort : IControlExtrasPort
    {
        private readonly Func<ControlService> _controlService;
        private readonly IProfileSettingsService _profileSettings;

        public DefaultControlExtrasPort(Func<ControlService> controlService, IProfileSettingsService profileSettings)
        {
            _controlService = controlService ?? throw new ArgumentNullException(nameof(controlService));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
        }

        private static bool InRange(int deviceIndex) => deviceIndex >= 0 && deviceIndex < DS4LightBar.forcelight.Length;

        public void SetRumble(int deviceIndex, byte heavy, byte light)
        {
            if (!InRange(deviceIndex)) return;
            _controlService()?.setRumble(heavy, light, deviceIndex);
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
