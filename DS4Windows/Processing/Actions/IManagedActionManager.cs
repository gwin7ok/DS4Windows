using System;
using System.Collections.Generic;

namespace DS4Windows.Processing.Actions
{
    public interface IManagedActionManager
    {
        ActionInstanceState GetStateFor(SpecialAction action, int device);
        // Phase13-Step4: 旧 静的 ActionManager.IsBeingTriggered。トリガーが成立中か（状態を取れないときは false）。
        bool IsBeingTriggered(SpecialAction action, int device);
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
