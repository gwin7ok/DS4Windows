using System;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>Phase13-Step3: <see cref="IDeviceRumbleService"/> の実装。旧 <c>ControlService.SetDevRumble</c> の本体をそのまま移した。</summary>
    public sealed class DeviceRumbleService : IDeviceRumbleService
    {
        private readonly IProfileSettingsService _profileSettings;

        public DeviceRumbleService(IProfileSettingsService profileSettings)
        {
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
        }

        // sets the rumble adjusted with rumble boost. Method more used for
        // report handling. Avoid constant checking for a device.
        public void SetDevRumble(DS4Device device, byte heavyMotor, byte lightMotor, int deviceNum)
        {
            byte boost = _profileSettings.GetRumbleBoost(deviceNum);
            Adjust(heavyMotor, lightMotor, boost, _profileSettings.InverseRumbleMotors[deviceNum], out byte right, out byte left);
            device.setRumble(right, left);
        }

        /// <summary>ブースト（百分率）と反転を適用した <c>setRumble</c> の引数（右の軽く速い、左の重く遅い）を求める。旧 <c>SetDevRumble</c> の式そのまま。</summary>
        public static void Adjust(byte heavyMotor, byte lightMotor, byte boost, bool inverse, out byte rightLightFast, out byte leftHeavySlow)
        {
            uint lightBoosted = ((uint)lightMotor * (uint)boost) / 100;
            if (lightBoosted > 255)
                lightBoosted = 255;
            uint heavyBoosted = ((uint)heavyMotor * (uint)boost) / 100;
            if (heavyBoosted > 255)
                heavyBoosted = 255;

            if (inverse)
            {
                rightLightFast = (byte)heavyBoosted;
                leftHeavySlow = (byte)lightBoosted;
            }
            else
            {
                rightLightFast = (byte)lightBoosted;
                leftHeavySlow = (byte)heavyBoosted;
            }
        }
    }
}
