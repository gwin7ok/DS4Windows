using System;
using System.Collections.Generic;
using DS4Windows.DI;
using DS4Windows.InputDevices;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-E E-6c（Phase9-E6-Plan.md §3.1）: <see cref="IInputDeviceModule"/> の共通の土台。種類によって何もしないメンバーは、ここで何もしない（旧コードで該当する分岐がなかったもの）。
    /// </summary>
    public abstract class InputDeviceModuleBase : IInputDeviceModule
    {
        public abstract bool Handles(InputDeviceType type);

        public abstract bool IsEnabled(ControlServiceDeviceOptions options);

        public virtual List<DS4Controls> GetKnownExtraButtons(DS4Device device)
        {
            return new List<DS4Controls>();
        }

        public virtual void OnDeviceInit(DS4Device device, ControlServiceDeviceOptions options)
        {
        }

        public virtual void HookSettings(DS4Device device, Func<int> activeControllers)
        {
        }

        public virtual void ApplyProfileOptions(DS4Device device, int ind, IProfileSettingsService settings)
        {
        }
    }
}
