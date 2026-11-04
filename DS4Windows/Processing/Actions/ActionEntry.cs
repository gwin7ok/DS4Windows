using DS4Windows.Services;
using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using DS4Windows.DS4Control;
using DS4Windows.Actions;

namespace DS4Windows
{

    internal class ActionEntry
    {
        public SpecialAction ActionDef;
        public ActionInstanceState[] States;

        // Phase8-Step8 8c: 系統 B の Action 実体（ActionImpl）とその工場は削除した。このクラスは、アクションごと・デバイスごとの状態の入れ物だけを持つ。
        public ActionEntry(SpecialAction action)
        {
            ActionDef = action;
            States = new ActionInstanceState[ControllerLimits.MaxControllerCount];
            for (int i = 0; i < States.Length; ++i) States[i] = new ActionInstanceState();
            try
            {
                AppLogger.LogTrace($"ActionEntry created: action={(action?.name ?? "(null)")} - initialized ActionInstanceState (IsToggledOn cleared)");
            }
            catch { }
        }
    }
}