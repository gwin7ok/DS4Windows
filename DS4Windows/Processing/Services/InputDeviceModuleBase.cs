using System;
using System.Collections.Generic;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services
{
    /// <summary>
    /// Phase9 9-E E-6c（Phase9-E6-Plan.md §3.1）: <see cref="IInputDeviceModule"/> の共通の土台。種類によって何もしないメンバーは、ここで何もしない（旧コードで該当する分岐がなかったもの）。
    /// </summary>
    public abstract class InputDeviceModuleBase : IInputDeviceModule
    {
        /// <summary>この部品が扱う系統。種類の判定（<see cref="Handles"/>）と、設定の有効・無効（<see cref="IsEnabled"/>）の両方がこの系統で決まる。null は「どの系統も扱わない」（何もしない部品）。</summary>
        protected abstract InputDeviceFamily? Family { get; }

        public bool Handles(InputDeviceType type) => Family.HasValue && InputDeviceTypeInfo.IsFamily(type, Family.Value);

        public bool IsEnabled(ControlServiceDeviceOptions options) => Family.HasValue && options[Family.Value].Enabled;

        public virtual List<DS4Controls> GetKnownExtraButtons(InputDeviceCore device)
        {
            return new List<DS4Controls>();
        }

        public virtual void OnDeviceInit(InputDeviceCore device, ControlServiceDeviceOptions options)
        {
        }

        public virtual void HookSettings(InputDeviceCore device, Func<int> activeControllers)
        {
        }

        public virtual void ApplyProfileOptions(InputDeviceCore device, int ind, IProfileSettingsService settings)
        {
        }
    }
}
