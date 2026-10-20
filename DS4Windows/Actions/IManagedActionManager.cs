using DS4Windows.Services;
using System;
using System.Collections.Generic;
using DS4Windows.Actions;
using DS4Windows.DS4Control;

namespace DS4Windows.Actions
{
    public interface IManagedActionManager
    {
        ActionInstanceState GetStateFor(SpecialAction action, int device);
        void ClearToggledOnForKey(ushort key);
        void ClearAllToggledOn();
        void ClearAllEntries();
        void ClearDeviceState(int device);
        // Set the toggled-on flag for given action/device and notify listeners.
        void SetToggledOn(SpecialAction action, int device, bool value);

        // Phase 5 Step 9: トグル状態変更を DI サービス経由で直接購読可能にするイベント
        event Action<SpecialAction, int, bool, bool> ToggledOnChanged;
    }
}
