using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Utilities;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services
{
    /// <summary>
    /// <see cref="IControllerSlotQuery"/> の実装。スロットの状態の持ち主（<see cref="IDeviceSlotState"/>）から読み、
    /// 機器ごとに 1 つだけ <see cref="ControllerSlotView"/> を作る（一覧での同一判定が、参照の比較でできるようにする）。
    /// </summary>
    public sealed class ControllerSlotQuery : IControllerSlotQuery
    {
        private readonly IDeviceSlotState _slotState;
        private readonly IProfileXmlStore _profileXmlStore;
        private readonly ConditionalWeakTable<InputDeviceCore, ControllerSlotView> _views = new ConditionalWeakTable<InputDeviceCore, ControllerSlotView>();

        public ControllerSlotQuery(IDeviceSlotState slotState, IServiceLifecycle lifecycle, IProfileXmlStore profileXmlStore)
        {
            _slotState = slotState ?? throw new ArgumentNullException(nameof(slotState));
            if (lifecycle == null) throw new ArgumentNullException(nameof(lifecycle));
            _profileXmlStore = profileXmlStore;

            // 接続の通知は、サービスの開始時の一括の接続も含めて、スロットへ入れた後にここへ来る
            lifecycle.HotplugController += (device, index) =>
            {
                IControllerSlotView view = ViewOf(device);
                if (view != null)
                    SlotChanged?.Invoke(this, new ControllerSlotChangedEventArgs(view, index));
            };
        }

        public event EventHandler<ControllerSlotChangedEventArgs> SlotChanged;

        public int SlotCount => _slotState.DS4Controllers?.Length ?? 0;

        public IControllerSlotView GetSlot(int slot)
        {
            InputDeviceCore[] controllers = _slotState.DS4Controllers;
            if (controllers == null || slot < 0 || slot >= controllers.Length)
                return null;

            return ViewOf(controllers[slot]);
        }

        public IReadOnlyList<ControllerSlotEntry> Snapshot()
        {
            ControllerSlotManager manager = _slotState.SlotManager;
            var result = new List<ControllerSlotEntry>();
            using (new ReadLocker(manager.CollectionLocker))
            {
                foreach (InputDeviceCore device in manager.ControllerColl)
                {
                    int slot = manager.ReverseControllerDict.TryGetValue(device, out int index) ? index : -1;
                    result.Add(new ControllerSlotEntry(slot, ViewOf(device)));
                }
            }

            return result;
        }

        private IControllerSlotView ViewOf(InputDeviceCore device)
        {
            if (device == null)
                return null;

            return _views.GetValue(device, d => new ControllerSlotView(d, _profileXmlStore));
        }
    }
}
