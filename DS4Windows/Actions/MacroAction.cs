using System;
using System.Collections.Generic;
using DS4Windows.DI;
using DS4Windows.Services;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.3-W2（Q3＝A）: 統一ディスパッチ（エンジン）から使うときは、旧 <c>MapCustomAction</c> の Macro の分岐が出していたログ
    /// （Trace の立ち上がり 4 行、Debug の「SpecialAction Macro／MacroRelease: …」）を、同じ文言・順序で出す。
    /// 旧経路のアダプター（<c>MacroActionAdapter</c>）は、<c>Mapping</c> が同じログを出すため、このログを出さない（添字つきのコンストラクターを使ったときだけ出す）。
    /// </summary>
    public class MacroAction : IOutputAction
    {
        public SpecialAction sa { get; }
        public int deviceIndex { get; }
        public IMacroPlayer macroPlayer { get; }

        // Phase6-Step10-4-4: マクロ再生役は必須の依存（AppHost／new DefaultMacroPlayer のフォールバックを廃止）
        public MacroAction(SpecialAction sa, IMacroPlayer macroPlayer) : this(sa, -1, macroPlayer) { }

        private readonly bool _logTrigger;
        private readonly int _actionIndex;
        private readonly ISpecialActionRepository _actionRepository;

        /// <summary>エンジン用: 旧経路のログ（立ち上がり）も出す。<paramref name="actionIndex"/> と <paramref name="actionRepository"/> は、ログの「index」「ActionEntries」用。</summary>
        public MacroAction(SpecialAction sa, int deviceIndex, IMacroPlayer macroPlayer, int actionIndex, ISpecialActionRepository actionRepository)
            : this(sa, deviceIndex, macroPlayer)
        {
            _logTrigger = true;
            _actionIndex = actionIndex;
            _actionRepository = actionRepository;
        }

        public MacroAction(SpecialAction sa, int deviceIndex, IMacroPlayer macroPlayer)
        {
            this.sa = sa;
            this.deviceIndex = deviceIndex;
            this.macroPlayer = macroPlayer ?? throw new ArgumentNullException(nameof(macroPlayer));
        }

        public string Id => sa?.name ?? "MacroAction";

        public void Execute(IOutputContext ctx)
        {
            if (sa == null) return;
            int dev = (deviceIndex >= 0) ? deviceIndex : (ctx != null ? ctx.Device : 0);
            if (_logTrigger && ctx != null) LogRising(ctx);
            macroPlayer.Play(dev, sa);
        }

        // 旧 MapCustomAction の Macro の分岐と同じ順序: LogSpecialActionTrace（立ち上がり）→ LogActionDoneCountOnTrigger
        private void LogRising(IOutputContext ctx)
        {
            try
            {
                if (AppLogger.IsTraceEnabled)
                {
                    AppLogger.LogTrace($"Trigger detected for SA '{sa.name}' on device {ctx.Device}");
                    AppLogger.LogTrace($"SA '{sa.name}' maps to Type:{sa.type} Details:{sa.details}");
                    // 旧経路は、状態を渡さずに getBoolSpecialActionMapping を呼ぶため例外（握りつぶし）となり、常に "<none>" を記録している（既存の挙動）
                    AppLogger.LogTrace($"Trigger keys for SA '{sa.name}': <none>");
                    var parts = new List<string>();
                    (ctx.Frame as IFrameOutputTrace)?.AppendOutputCombo(parts);
                    string combo = parts.Count > 0 ? string.Join(", ", parts) : "<none>";
                    AppLogger.LogTrace($"SA '{sa.name}' output combo: {combo}");
                }

                int count = 0;
                try { count = _actionRepository?.ActionList?.Count ?? 0; } catch { }
                string context = sa.pressRelease ? "MacroRelease" : "Macro";
                AppLogger.LogDebug($"SpecialAction {context}: device={ctx.Device}, name={sa.name}, index={_actionIndex}, ActionEntries={count}");
            }
            catch { }
        }

        public void Stop(IOutputContext ctx)
        {
            if (sa == null) return;
            int dev = (deviceIndex >= 0) ? deviceIndex : (ctx != null ? ctx.Device : 0);
            macroPlayer.Stop(dev);
        }
    }
}
