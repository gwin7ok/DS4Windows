using System;
using System.Collections.Generic;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step7 7.7b（W2b。<c>MultiActionOutput</c> の置き換え）: 「このマクロの文字列を再生する」だけの、汎用の出力（信号・アクション出力層）。
    /// どのジェスチャーでどのマクロを実行するかは、この出力は知らない。**変換層（ビルダーが作る束縛）が決めて**、ジェスチャーごとにこの出力を1つずつ持たせる
    /// （<c>BindingPolicy.RoutesOutputsByGesture</c> が真の束縛では、エンジンが <c>Outputs[Gesture - 1]</c> を実行する）。
    /// 実行されたとき、マクロが空なら何もしない。空でなければ、旧 <c>MapCustomAction</c> の MultiAction の枝と同じく、実行の直前に Trace 4 行（<c>LogSpecialActionTrace</c>）を出し、
    /// <see cref="IMacroStringPlayer"/> で再生する。Debug のログはない（旧経路と同じ）。
    /// </summary>
    public sealed class MacroStringAction : IOutputAction
    {
        private readonly SpecialAction _sa;
        private readonly int _deviceIndex;
        private readonly string _macro;
        private readonly IMacroStringPlayer _player;

        public MacroStringAction(SpecialAction sa, int deviceIndex, string macro, IMacroStringPlayer player)
        {
            _sa = sa ?? throw new ArgumentNullException(nameof(sa));
            _deviceIndex = deviceIndex;
            _macro = macro ?? string.Empty;
            _player = player ?? throw new ArgumentNullException(nameof(player));
        }

        public string Id => _sa.name ?? "MacroString";

        public void Execute(IOutputContext ctx)
        {
            if (_macro.Length == 0) return;

            int dev = _deviceIndex >= 0 ? _deviceIndex : (ctx != null ? ctx.Device : 0);
            LogExecution(ctx, dev);
            _player.PlayMacroString(dev, _macro);
        }

        public void Stop(IOutputContext ctx)
        {
        }

        // 旧 MapCustomAction の MultiAction の枝と同じ: LogSpecialActionTrace（Trace の 4 行）
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
