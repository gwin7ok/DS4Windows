using System;

namespace DS4Windows.Processing.Actions
{
    /// <summary>
    /// <see cref="IKeyButtonActionControllerFactory"/> の既定の実装。
    /// Phase13-Step4: 旧 <c>ServiceProviderHolder.Provider</c> による <see cref="IControllerRegistry"/> の解決をやめ、コンストラクターで受ける。
    /// 生成した <see cref="KeyButtonActionController"/> には、トグル状態の管理役（<see cref="IManagedActionManager"/>）を渡す。
    /// </summary>
    public class DefaultKeyButtonActionControllerFactory : IKeyButtonActionControllerFactory
    {
        private readonly DS4Windows.Processing.Abstractions.ISyntheticDispatcher _dispatcher;
        private readonly IControllerRegistry _registry;
        private readonly IManagedActionManager _manager;
        private readonly TimeProvider _time;

        public DefaultKeyButtonActionControllerFactory(IControllerRegistry registry, IManagedActionManager manager, DS4Windows.Processing.Abstractions.ISyntheticDispatcher dispatcher, TimeProvider timeProvider)
        {
            _time = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));   // Phase14c-Step3（R-06）
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        public DS4Windows.Processing.Actions.KeyButtonActionController Create(int device, DS4Windows.Processing.Actions.KeyButtonActionController.Mode mode, string actionName = null)
        {
            var inst = new DS4Windows.Processing.Actions.KeyButtonActionController(device, mode, _manager, _dispatcher, _time, actionName);
            try
            {
                var key = device + ":" + (string.IsNullOrEmpty(actionName) ? "<default>" : actionName);
                _registry.Register(key, inst);
            }
            catch { }
            return inst;
        }

        public DS4Windows.Processing.Actions.KeyButtonActionController Create(int device, DS4Windows.Core.Settings.SpecialAction sa, string actionName = null)
        {
            var inst = new DS4Windows.Processing.Actions.KeyButtonActionController(device, sa, _manager, _dispatcher, _time, actionName);
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
