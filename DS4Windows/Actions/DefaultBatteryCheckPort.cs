using System;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.6c: <see cref="IBatteryCheckPort"/> の本番の実装。<c>IDeviceStateAccessor</c>（＝ControlService）越しに <c>DS4Device</c> を操作し、
    /// ライトバーの強制色は静的な <c>DS4LightBar</c> を、通知は <c>AppLogger.LogToTray</c> を、この窓口の内側に閉じ込めて使う（Q6＝A）。
    /// 電池残量の文言は <c>ControlService.GetDS4Battery</c>（旧 <c>Mapping</c> と同じ）。ControlService は後から作られるため、遅延（<c>Func</c>）で受ける。
    /// 旧経路と違い、コントローラーがいなくても例外にしない。
    /// </summary>
    public sealed class DefaultBatteryCheckPort : IBatteryCheckPort
    {
        private readonly Func<IDeviceStateAccessor> _deviceStateAccessor;
        private readonly Func<ControlService> _controlService;

        public DefaultBatteryCheckPort(Func<IDeviceStateAccessor> deviceStateAccessor, Func<ControlService> controlService)
        {
            _deviceStateAccessor = deviceStateAccessor ?? throw new ArgumentNullException(nameof(deviceStateAccessor));
            _controlService = controlService ?? throw new ArgumentNullException(nameof(controlService));
        }

        private DS4Device Controller(int deviceIndex) => _deviceStateAccessor()?.GetController(deviceIndex);

        private static bool InRange(int deviceIndex) => deviceIndex >= 0 && deviceIndex < DS4LightBar.forcelight.Length;

        public bool HasController(int deviceIndex) => Controller(deviceIndex) != null;

        public void NotifyBattery(int deviceIndex)
        {
            string text = _controlService()?.GetDS4Battery(deviceIndex);
            if (text == null) return;

            AppLogger.LogToTray("Controller " + (deviceIndex + 1) + ": " + text, true);
        }

        public DS4Color GetLightBarColor(int deviceIndex) => Controller(deviceIndex)?.LightBarColor ?? default(DS4Color);

        public int GetBattery(int deviceIndex) => Controller(deviceIndex)?.Battery ?? 0;

        public void SetForceLight(int deviceIndex, bool on)
        {
            if (InRange(deviceIndex)) DS4LightBar.forcelight[deviceIndex] = on;
        }

        public void SetForcedColor(int deviceIndex, DS4Color color)
        {
            if (InRange(deviceIndex)) DS4LightBar.forcedColor[deviceIndex] = color;
        }
    }
}
