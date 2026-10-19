using System;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.6b（Phase8-Step7-6-Plan.md Q5＝A）: 特殊アクションのトリガーのボタンの通常の割り当て（キー、マクロ）のキーを離す。
    /// 旧 <c>Mapping.ReleaseActionKeys</c> と同じ処理。DisconnectBT（切断の直後）と、Profile の切替（<see cref="ProfileSwitchAction"/>。7.5a）が共有する。
    /// 切断・切替のあと、キーが押されたまま残らないようにする。
    /// </summary>
    public sealed class TriggerButtonKeyReleaser
    {
        private readonly IProfileSettingsService _settings;

        public TriggerButtonKeyReleaser(IProfileSettingsService settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void Release(SpecialAction sa, int device, IVirtualKBM kbm)
        {
            if (kbm == null || sa == null || sa.trigger == null) return;

            for (int i = 0, count = sa.trigger.Count; i < count; i++)
            {
                DS4ControlSettings dcs = _settings.GetDS4CSetting(device, sa.trigger[i]);
                if (dcs.actionType == DS4ControlSettings.ActionType.Default) continue;

                if (dcs.actionType == DS4ControlSettings.ActionType.Key)
                {
                    uint tempKey = _settings.OutputKBMMapping.GetRealEventKey((uint)dcs.action.actionKey);
                    kbm.PerformKeyRelease(tempKey);
                }
                else if (dcs.actionType == DS4ControlSettings.ActionType.Macro)
                {
                    int[] keys = (int[])dcs.action.actionMacro;
                    for (int j = 0, keysLen = keys.Length; j < keysLen; j++)
                    {
                        uint tempKey = _settings.OutputKBMMapping.GetRealEventKey((uint)keys[j]);
                        kbm.PerformKeyRelease(tempKey);
                    }
                }
            }
        }
    }
}
