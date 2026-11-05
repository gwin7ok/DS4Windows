using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Actions
{
    /// <summary>
    /// Phase8-Step7 7.6c: <see cref="IBatteryCheckPort"/> の本番の実装。<c>IDeviceStateAccessor</c>（＝IDeviceSlotState。Phase13-Step5 で直接注入に変更）越しに <c>DS4Device</c> を操作し、
    /// ライトバーの強制色は静的な <c>DS4LightBar</c> を、通知は <c>AppLogger.LogToTray</c> を、この窓口の内側に閉じ込めて使う（Q6＝A）。
    /// 電池残量の文言は <see cref="IDeviceBatteryText"/>（旧 <c>ControlService.GetDS4Battery</c>。Phase13-Step6 6-b で移し、<c>Func&lt;ControlService&gt;</c> をやめた）。
    /// 旧経路と違い、コントローラーがいなくても例外にしない。
    /// </summary>
    public sealed class DefaultBatteryCheckPort : IBatteryCheckPort
    {
        private readonly IDeviceStateAccessor _deviceStateAccessor;
        private readonly IDeviceBatteryText _batteryText;

        // Phase14c-Step4（R-05 の前半）: ライトバーの強制表示の状態（旧: DS4LightBar の静的な配列）
        private readonly DS4Windows.Core.Model.LightbarOverrides _lightbar;

        public DefaultBatteryCheckPort(DS4Windows.Core.Model.LightbarOverrides lightbar, IDeviceStateAccessor deviceStateAccessor, IDeviceBatteryText batteryText)
        {
            _lightbar = lightbar ?? throw new System.ArgumentNullException(nameof(lightbar));
            _deviceStateAccessor = deviceStateAccessor ?? throw new ArgumentNullException(nameof(deviceStateAccessor));
            _batteryText = batteryText ?? throw new ArgumentNullException(nameof(batteryText));
        }

        private DS4Device Controller(int deviceIndex) => _deviceStateAccessor.GetController(deviceIndex);

        private static bool InRange(int deviceIndex) => deviceIndex >= 0 && deviceIndex < DS4Windows.Core.Settings.ControllerLimits.MaxControllerCount;   // 旧 DS4LightBar.forcelight.Length（同じ値）

        public bool HasController(int deviceIndex) => Controller(deviceIndex) != null;

        public void NotifyBattery(int deviceIndex)
        {
            string text = _batteryText.GetBatteryText(deviceIndex);
            if (text == null) return;

            AppLogger.LogToTray("Controller " + (deviceIndex + 1) + ": " + text, true);
        }

        public DS4Color GetLightBarColor(int deviceIndex) => Controller(deviceIndex)?.LightBarColor ?? default(DS4Color);

        public int GetBattery(int deviceIndex) => Controller(deviceIndex)?.Battery ?? 0;

        public void SetForceLight(int deviceIndex, bool on)
        {
            if (InRange(deviceIndex)) _lightbar.forcelight[deviceIndex] = on;
        }

        public void SetForcedColor(int deviceIndex, DS4Color color)
        {
            if (InRange(deviceIndex)) _lightbar.forcedColor[deviceIndex] = color;
        }
    }
}
