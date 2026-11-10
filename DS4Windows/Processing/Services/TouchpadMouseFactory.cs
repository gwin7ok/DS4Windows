using DS4Windows.Processing.Touchpad;
using DS4Windows.Processing.Synthetic;
using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services
{
    /// <summary><see cref="IMouseFactory"/> の実装（DI の Singleton。状態なし）。</summary>
    public sealed class MouseFactory : IMouseFactory
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly IVirtualKBM _virtualKBM;
        private readonly StickMotionState _stickMotion;
        private readonly SyntheticInputState _syntheticInput;
        private readonly TimeProvider _timeProvider;

        public MouseFactory(IProfileSettingsService profileSettings, IVirtualKBM virtualKBM, StickMotionState stickMotion,
            SyntheticInputState syntheticInput, TimeProvider timeProvider)
        {
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _virtualKBM = virtualKBM ?? throw new ArgumentNullException(nameof(virtualKBM));
            _stickMotion = stickMotion ?? throw new ArgumentNullException(nameof(stickMotion));
            _syntheticInput = syntheticInput ?? throw new ArgumentNullException(nameof(syntheticInput));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public TouchpadMouse Create(int deviceIndex, InputDeviceCore device)
        {
            return new TouchpadMouse(deviceIndex, device, _profileSettings, _virtualKBM, _stickMotion, _syntheticInput, _timeProvider);
        }
    }
}
