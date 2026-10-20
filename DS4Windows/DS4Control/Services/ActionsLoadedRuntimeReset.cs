using System;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase7-Step7 W2（Q4＝A）: <c>BackingStore.ActionsLoaded</c> の購読者。Actions.xml の読み込み後に、合成入力とマクロの実行時状態を新品へ戻す。
    /// 旧: <c>BackingStore.LoadActions</c> が <c>Mapping</c> の静的な状態を直接書き換えていた（下位層から上位層への逆参照）。
    /// 順序とログは従来と同じ（合成状態のリセット → ActionManager のトグル状態の解除 → ログ。旧: macrodone の解除は、Phase8-Fix-MacroRepeatUnify で macrodone ごと削除した）。
    /// Phase7-Step8 W3: 静的クラスから instance に変え、保持クラスをコンストラクターで受ける（旧: <c>Shared</c> の参照）。
    /// <c>ServiceRegistration</c> が、購読を重複させない（<c>-=</c> してから <c>+=</c>）形で登録する。
    /// </summary>
    public class ActionsLoadedRuntimeReset
    {
        private readonly SyntheticInputState _syntheticInput;

        // Phase8-Fix-MacroRepeatUnify（D5）: macrodone の解除を削除したため、MacroRuntimeState への依存（コンストラクターの引数）も削除した
        public ActionsLoadedRuntimeReset(SyntheticInputState syntheticInput)
        {
            _syntheticInput = syntheticInput ?? throw new ArgumentNullException(nameof(syntheticInput));
        }

        public void Reset()
        {
            try
            {
                _syntheticInput.ResetAll();
                try { ActionManager.ClearAllToggledOn(); } catch { }
                // Phase8-Fix-MacroRepeatUnify（D5）: 旧 macrodone の解除は削除した（macrodone 自体を削除。本番では到達しない経路だった）
                AppLogger.LogToGui("Runtime synthetic state reset after Actions load", false);
            }
            catch { }
        }
    }
}
