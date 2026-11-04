using System;
using System.Collections.Generic;
using DS4Windows.DI;
using DS4Windows.InputDevices;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-E E-6c: <see cref="IInputDeviceModuleRegistry"/> の実装（DI の Singleton）。DI に登録された <see cref="IInputDeviceModule"/> の全部を受け取り、
    /// 種類から最初に <see cref="IInputDeviceModule.Handles"/> が true の部品を返す。なければ <see cref="NullInputDeviceModule"/>。
    /// </summary>
    public sealed class InputDeviceModuleRegistry : IInputDeviceModuleRegistry
    {
        private readonly List<IInputDeviceModule> _modules;
        private readonly IInputDeviceModule _fallback = new NullInputDeviceModule();

        public InputDeviceModuleRegistry(IEnumerable<IInputDeviceModule> modules)
        {
            if (modules == null) throw new ArgumentNullException(nameof(modules));
            _modules = new List<IInputDeviceModule>(modules);
        }

        public IInputDeviceModule Resolve(InputDeviceType type)
        {
            for (int i = 0; i < _modules.Count; i++)
            {
                if (_modules[i].Handles(type))
                {
                    return _modules[i];
                }
            }

            return _fallback;
        }
    }
}
