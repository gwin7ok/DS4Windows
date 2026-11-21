using System;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Utilities;

namespace DS4Windows.Core.Settings
{
    /// <summary>スペシャルアクションの管理（既定の作成、キャッシュ、検索、トリガー条件）。</summary>
    public partial class BackingStore
    {
        public void CacheProfileCustomsFlags(int device)
        {
            bool customAct = false;
            containsCustomAction[device] = customAct = HasCustomActions(device);
            containsCustomExtras[device] = HasCustomExtras(device);

            {
                customAct = gyroSlots[device].OutputMode == GyroOutMode.MouseJoystick;
                customAct = customAct || stickSlots[device].LSOutput.mode != StickMode.Controls;
                customAct = customAct || stickSlots[device].RSOutput.mode != StickMode.Controls;
                containsCustomAction[device] = customAct;
            }
        }

        public void CacheExtraProfileInfo(int device)
        {
            CalculateProfileActionCount(device);
            CalculateProfileActionDicts(device);
            CacheProfileCustomsFlags(device);
        }

        public void CalculateProfileActionCount(int index)
        {
            profileActionCount[index] = profileActions[index]!.Count;   // 数えるのは、スロットのリストが作られたあと（プロファイルの読み込み）
        }

        public void CalculateProfileActionDicts(int device)
        {
            profileActionDict[device].Clear();
            profileActionIndexDict[device].Clear();

            foreach (string actionname in profileActions[device]!)   // 同上
            {
                var actionObj = GetAction(actionname);
                int index = GetActionIndexOf(actionname);
                profileActionDict[device][actionname] = actionObj;
                profileActionIndexDict[device][actionname] = index;
            }
        }

        /// <summary>
        /// Emit "Invalid action index" logs for missing actions in the profile for the given device.
        /// Respects loggedInvalidActions unless forceEmit is true.
        /// </summary>
        /// <param name="device">Device index</param>
        /// <param name="forceEmit">If true, ignore suppression and always emit logs</param>
        /// <param name="overrideProfileName">If provided, use this profile name instead of looking up profilePath[device]</param>
        public void EmitMissingActionLogsForDevice(int device, bool forceEmit = false, string? overrideProfileName = null)
        {
            if (profileActions == null || device < 0 || device >= profileActions.Length) return;

            foreach (string actionname in profileActions[device]!)   // 同上
            {
                var actionObj = GetAction(actionname);
                int index = GetActionIndexOf(actionname);
                if (actionObj == null || actionObj.name == "null" || index < 0)
                {
                    if (forceEmit || !loggedInvalidActions.Contains(actionname))
                    {
                        string profName;
                        if (overrideProfileName != null)
                        {
                            // Use explicitly provided profile name
                            profName = overrideProfileName;
                        }
                        else
                        {
                            // Determine the profile name actually in use for this device (temporary or regular)
                            profName = (ProfileSettings.GetUseTempProfile(device))
                                ? ProfileSettings.GetTempProfileName(device)
                                : (profilePath != null && device >= 0 && device < profilePath.Length ? profilePath[device] : string.Empty);
                        }

                        string displayProfile = string.IsNullOrEmpty(profName) ? "(unknown)" : profName;
                        AppLogger.LogUser(AppLogLevel.Warn, "Log_Settings_InvalidSpecialAction", displayProfile, actionname);
                        try
                        {
                            loggedInvalidActions.Add(actionname);
                        }
                        catch { /* 欠けた・壊れた値は、既定値のまま続ける */ }
                    }
                }
            }
        }

        public SpecialAction GetAction(string name)
        {
            //foreach (SpecialAction sA in actions)
            for (int i = 0, actionCount = actions.Count; i < actionCount; i++)
            {
                SpecialAction sA = actions[i];
                var normExisting = Util.NormalizeActionName(sA?.name);
                var normSearch = Util.NormalizeActionName(name);
                if (string.Equals(normExisting, normSearch, StringComparison.OrdinalIgnoreCase))
                    return sA!;   // 要素が null のまま空の名前を探したときも、従来どおりその要素を返す
            }

            return new SpecialAction("null", "null", "null", "null");
        }

        public int GetActionIndexOf(string name)
        {
            for (int i = 0, actionCount = actions.Count; i < actionCount; i++)
            {
                var normExisting = Util.NormalizeActionName(actions[i]?.name);
                var normSearch = Util.NormalizeActionName(name);
                if (string.Equals(normExisting, normSearch, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        public static bool SaTriggerCondValue(string text)
        {
            bool result = true;
            switch (text)
            {
                case "and": result = true; break;
                case "or": result = false; break;
                default: result = true; break;
            }

            return result;
        }

        public static string SaTriggerCondString(bool value)
        {
            string result = value ? "and" : "or";
            return result;
        }

        public void SetSaTriggerCond(int index, string text)
        {
            gyroSlots[index].MouseTriggerCondAnd = SaTriggerCondValue(text);
        }

        public void SetSaMouseStickTriggerCond(int index, string text)
        {
            gyroSlots[index].MouseStickTriggerCondAnd = SaTriggerCondValue(text);
        }

        public void ReplaceActionsLoadedHandler(Action handler)
        {
            if (_actionsLoadedHandler != null) ActionsLoaded -= _actionsLoadedHandler;
            _actionsLoadedHandler = handler;
            if (handler != null) ActionsLoaded += handler;
        }

        // Actions.xml の読込を担う SpecialActionsXmlSerializer が呼ぶため、公開にした（ActionsLoaded イベントの発行元は BackingStore のまま）
        public void RaiseActionsLoaded()
        {
            ActionsLoaded?.Invoke();
        }
    }
}
