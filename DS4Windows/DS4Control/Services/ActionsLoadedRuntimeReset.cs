using System;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase7-Step7 W2（Q4＝A）: <c>BackingStore.ActionsLoaded</c> の購読者。Actions.xml の読み込み後に、合成入力とマクロの実行時状態を新品へ戻す。
    /// 旧: <c>BackingStore.LoadActions</c> が <c>Mapping</c> の静的な状態を直接書き換えていた（下位層から上位層への逆参照）。
    /// 順序とログは従来と同じ（合成状態のリセット → ActionManager のトグル状態の解除 → macrodone の解除 → ログ）。
    /// 静的メソッドなのは、<c>ServiceRegistration</c> が購読を重複させない（<c>-=</c> してから <c>+=</c>）ため。
    /// </summary>
    public static class ActionsLoadedRuntimeReset
    {
        public static void Reset()
        {
            try
            {
                SyntheticInputState.Shared.ResetAll();
                try { ActionManager.ClearAllToggledOn(); } catch { }
                // TODO(Phase7-Step7 W3): macrodone は MacroRuntimeState へ移す。
                if (Mapping.macrodone != null)
                {
                    for (int i = 0; i < Mapping.macrodone.Length; i++) Mapping.macrodone[i] = false;
                }
                AppLogger.LogToGui("Runtime synthetic state reset after Actions load", false);
            }
            catch { }
        }
    }
}
