using DS4Windows.Processing.Synthetic;
using System;
using DS4Windows.Core.Logging;

namespace DS4Windows.Processing.Services.Slots
{
    /// <summary>
    /// <c>BackingStore.ActionsLoaded</c> の購読者。Actions.xml の読み込み後に、合成入力とマクロの実行時状態を新品へ戻す。
    /// 旧: <c>BackingStore.LoadActions</c> が <c>Mapping</c> の静的な状態を直接書き換えていた（下位層から上位層への逆参照）。
    /// 順序とログは従来と同じ（合成状態のリセット → ActionManager のトグル状態の解除 → ログ。旧: macrodone の解除は、Phase8-Fix-MacroRepeatUnify で macrodone ごと削除した）。
    /// 静的クラスから instance に変え、保持クラスをコンストラクターで受ける。
    /// <c>ServiceRegistration</c> が、購読を重複させない（<c>-=</c> してから <c>+=</c>）形で登録する。
    /// </summary>
    public class ActionsLoadedRuntimeReset
    {
        private readonly SyntheticInputState _syntheticInput;
        private readonly DS4Windows.Processing.Actions.IManagedActionManager _actionManager;   // 旧 静的 ActionManager.ClearAllToggledOn の代わり

        // macrodone の解除を削除したため、MacroRuntimeState への依存（コンストラクターの引数）も削除した
        public ActionsLoadedRuntimeReset(SyntheticInputState syntheticInput, DS4Windows.Processing.Actions.IManagedActionManager actionManager)
        {
            ArgumentNullException.ThrowIfNull(actionManager);
            _actionManager = actionManager;
            ArgumentNullException.ThrowIfNull(syntheticInput);
            _syntheticInput = syntheticInput;
        }

        public void Reset()
        {
            try
            {
                _syntheticInput.ResetAll();
                try { _actionManager.ClearAllToggledOn(); } catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
                AppLogger.LogUser(AppLogLevel.Info, "Log_Actions_RuntimeReset");
            }
            catch { /* 後始末・解放の失敗は無視してよい（解放済み・切断済みでも続ける） */ }
        }
    }
}
