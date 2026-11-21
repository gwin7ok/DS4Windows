using DS4Windows.Processing.Gyro;
using DS4Windows.Processing.Touchpad;
using DS4Windows.Processing.Synthetic;
using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services.Conditioning
{
    /// <summary><see cref="ITouchpadMouseFactory"/> の実装（DI の Singleton。状態なし）。</summary>
    public sealed class TouchpadMouseFactory : ITouchpadMouseFactory
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly IVirtualKBM _virtualKBM;
        private readonly StickMotionState _stickMotion;
        private readonly SyntheticInputState _syntheticInput;
        private readonly TimeProvider _timeProvider;

        public TouchpadMouseFactory(IProfileSettingsService profileSettings, IVirtualKBM virtualKBM, StickMotionState stickMotion,
            SyntheticInputState syntheticInput, TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(profileSettings);
            _profileSettings = profileSettings;
            ArgumentNullException.ThrowIfNull(virtualKBM);
            _virtualKBM = virtualKBM;
            ArgumentNullException.ThrowIfNull(stickMotion);
            _stickMotion = stickMotion;
            ArgumentNullException.ThrowIfNull(syntheticInput);
            _syntheticInput = syntheticInput;
            ArgumentNullException.ThrowIfNull(timeProvider);
            _timeProvider = timeProvider;
        }

        public TouchpadMouse Create(int deviceIndex, InputDeviceCore device)
        {
            var tap = new TapClickDetector(deviceIndex, _timeProvider);
            var cursor = new MouseCursor(deviceIndex, device.GyroMouseSensSettings, _profileSettings, _virtualKBM);
            var wheel = new MouseWheel(deviceIndex, _profileSettings, _virtualKBM);
            var gyro = new GyroOutputProcessor(deviceIndex, device, cursor, _profileSettings, _stickMotion);
            var touchStick = new TouchStickOutput(deviceIndex, _profileSettings, _stickMotion);
            return new TouchpadMouse(deviceIndex, device, _profileSettings, _virtualKBM, _stickMotion, _syntheticInput,
                tap, cursor, wheel, gyro, touchStick);
        }
    }
}
