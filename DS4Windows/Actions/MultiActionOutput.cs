using System;
using System.Collections.Generic;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.7b: MultiAction の出力。ジェスチャー（<see cref="TriggerState.Tap"/>／<see cref="TriggerState.Hold"/>／<see cref="TriggerState.DoubleTap"/>）に対応するマクロ
    /// （<c>details</c> を「,」で分けた 1〜3 つ目。足りなければ空）が空でなければ、旧 <c>MapCustomAction</c> の MultiAction の枝と同じ順序で、
    /// Trace 4 行（旧 <c>LogSpecialActionTrace</c>。Debug はなし）→ マクロの実行（<see cref="IMultiActionMacroPlayer"/>）を行う。
    /// 通常の出力（<see cref="Execute"/>／<see cref="Stop"/>）は何もしない（旧経路の MultiAction は、<c>BeingTriggered</c> を使わず、ジェスチャーのときだけ動く）。
    /// </summary>
    public sealed class MultiActionOutput : IGestureOutputAction
    {
        private readonly SpecialAction _sa;
        private readonly int _deviceIndex;
        private readonly IMultiActionMacroPlayer _player;
        private readonly string[] _macros;

        public MultiActionOutput(SpecialAction sa, int deviceIndex, IMultiActionMacroPlayer player)
        {
            _sa = sa ?? throw new ArgumentNullException(nameof(sa));
            _deviceIndex = deviceIndex;
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _macros = (sa.details ?? string.Empty).Split(',');
        }

        public string Id => _sa.name ?? "MultiAction";

        public void Execute(IOutputContext ctx)
        {
        }

        public void Stop(IOutputContext ctx)
        {
        }

        public void OnGesture(IOutputContext ctx, int gesture)
        {
            int index = gesture - 1;   // Tap＝1 → 1 つ目、Hold＝2 → 2 つ目、DoubleTap＝3 → 3 つ目
            string macro = index >= 0 && index < _macros.Length ? _macros[index] : string.Empty;
            if (string.IsNullOrEmpty(macro)) return;

            int dev = _deviceIndex >= 0 ? _deviceIndex : (ctx != null ? ctx.Device : 0);
            LogExecution(ctx, dev);
            _player.PlayMacroString(dev, macro);
        }

        private void LogExecution(IOutputContext ctx, int dev)
        {
            try
            {
                if (!AppLogger.IsTraceEnabled) return;

                AppLogger.LogTrace($"Trigger detected for SA '{_sa.name}' on device {dev}");
                AppLogger.LogTrace($"SA '{_sa.name}' maps to Type:{_sa.type} Details:{_sa.details}");
                // 旧経路は、状態を渡さずに getBoolSpecialActionMapping を呼ぶため例外（握りつぶし）となり、常に "<none>" を記録している（既存の挙動）
                AppLogger.LogTrace($"Trigger keys for SA '{_sa.name}': <none>");
                var parts = new List<string>();
                (ctx?.Frame as IFrameOutputTrace)?.AppendOutputCombo(parts);
                string combo = parts.Count > 0 ? string.Join(", ", parts) : "<none>";
                AppLogger.LogTrace($"SA '{_sa.name}' output combo: {combo}");
            }
            catch { }
        }
    }
}
