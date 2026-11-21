#nullable enable

using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Gyro;

namespace DS4Windows.Processing
{
    /// <summary>
    /// 新経路のマウスの移動（ControlMouseMoveOutputAction）が借りる、ボタンからのマウス移動量の計算。
    /// 今のフレームの入力は、<see cref="Mapping"/> が MapCustom の先頭で <see cref="SetFrame"/> へ渡す。
    /// </summary>
    internal sealed class ButtonMouseSampler
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly IDeviceSlotState _slots;
        private readonly IMouseConversionStage _mouseConversion;
        private readonly DeviceStickMotion[] _motions;
        private readonly ControllerState?[] _state = new ControllerState?[ControllerLimits.MaxControllerCount];
        private readonly ControllerStateFieldMapping?[] _fields = new ControllerStateFieldMapping?[ControllerLimits.MaxControllerCount];

        public ButtonMouseSampler(IProfileSettingsService profileSettings, IDeviceSlotState slots,
            IMouseConversionStage mouseConversion, DeviceStickMotion[] motions)
        {
            ArgumentNullException.ThrowIfNull(profileSettings);
            _profileSettings = profileSettings;
            ArgumentNullException.ThrowIfNull(slots);
            _slots = slots;
            ArgumentNullException.ThrowIfNull(mouseConversion);
            _mouseConversion = mouseConversion;
            ArgumentNullException.ThrowIfNull(motions);
            _motions = motions;
        }

        public void SetFrame(int device, ControllerState state, ControllerStateFieldMapping fields)
        {
            _state[device] = state;
            _fields[device] = fields;
        }

        // 新経路のマウスの移動（ControlMouseMoveOutputAction）が、TriggerInput 経由で呼ぶ。旧 getMouseMapping と同じ計算
        public double SampleMouseMovement(int device, DS4Controls control, int direction)
        {
            ControllerState? state = _state[device];
            ControllerStateFieldMapping? fields = _fields[device];
            if (state == null || fields == null) return 0.0;

            // 旧 getMouseMapping（ctrl から設定と経過時間を読んでいた）は ButtonMouseMovementCalculator へ移した。設定と経過時間は、ここで読んで渡す
            return _mouseConversion.CalculateButtonMouseMovement(device, control, direction, state, fields,
                _profileSettings.ButtonMouseInfos[device],
                _profileSettings.ForSlot(device).Sticks.LSMod.deadZone,
                _profileSettings.ForSlot(device).Sticks.RSMod.deadZone,
                _motions[device].DeltaAccelProcessors,
                _slots.Controllers[device].lastTimeElapsedDouble);
        }

        // SA のマウス移動（強さの固定値）が、TriggerInput 経由で呼ぶ。getMouseMapping の「ボタンが元のとき」（押している）の量
        // （感度 activeButtonSensitivity と、縦方向の倍率 buttonVerticalScale）に、強さ（0〜1）を掛ける。1 は、Controls のボタンからのマウス移動と同じ量
        public double SampleFixedMouseMovement(int device, int direction, double fraction)
        {
            ButtonMouseInfo buttonMouseInfo = _profileSettings.ButtonMouseInfos[device];
            const double root = 1.002;
            const double divide = 10000d;
            double value = Math.Pow(root + buttonMouseInfo.activeButtonSensitivity / divide, 100) - 1;
            if (direction == 0 || direction == 1) value *= buttonMouseInfo.buttonVerticalScale;
            return value * fraction;
        }
    }
}
