using DS4Windows.Services;
using System;

namespace DS4Windows.Actions
{
    /// <summary>
    /// <see cref="IKeyButtonActionControllerFactory"/> の既定の実装。
    /// Phase13-Step4: 旧 <c>ServiceProviderHolder.Provider</c> による <see cref="IControllerRegistry"/> の解決をやめ、コンストラクターで受ける。
    /// 生成した <see cref="KeyButtonActionController"/> には、トグル状態の管理役（<see cref="IManagedActionManager"/>）を渡す。
    /// </summary>
    public class DefaultKeyButtonActionControllerFactory : IKeyButtonActionControllerFactory
    {
        private readonly IControllerRegistry _registry;
        private readonly IManagedActionManager _manager;

        public DefaultKeyButtonActionControllerFactory(IControllerRegistry registry, IManagedActionManager manager)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        public DS4Windows.KeyButtonActionController Create(int device, DS4Windows.KeyButtonActionController.Mode mode, string actionName = null)
        {
            var inst = new DS4Windows.KeyButtonActionController(device, mode, _manager, actionName);
            try
            {
                var key = device + ":" + (string.IsNullOrEmpty(actionName) ? "<default>" : actionName);
                _registry.Register(key, inst);
            }
            catch { }
            return inst;
        }

        public DS4Windows.KeyButtonActionController Create(int device, DS4Windows.SpecialAction sa, string actionName = null)
        {
            var inst = new DS4Windows.KeyButtonActionController(device, sa, _manager, actionName);
            try
            {
                var name = actionName ?? (sa?.name ?? "<sa>");
                var key = device + ":" + (string.IsNullOrEmpty(name) ? "<default>" : name);
                _registry.Register(key, inst);
            }
            catch { }
            return inst;
        }
    }
}
