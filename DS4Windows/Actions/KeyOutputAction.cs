using System;
using DS4Windows.DS4Control;

namespace DS4Windows.Actions
{
    // TODO(Phase8-Step2/3): 使うべき型だが、書き直しが必要（D1 の注記）。現状は KeyAction に委譲せず、独自に TriggerContextImpl を組み立てて IActionController.Handle で再ディスパッチする
    // 未検証の実装で、Execute／Stop は一度も呼ばれていない（2025-12-19、efcfa29e）。Step3 で KeyAction へ委譲する形に改め、系統 B（KeyActionAdapter 経由）との挙動一致を単体テストで確認してから採用する。
    public class KeyOutputAction : IOutputAction
    {
        private readonly SpecialAction sa;

        public KeyOutputAction(SpecialAction sa)
        {
            this.sa = sa;
        }

        public string Id => sa?.name ?? "KeyOutput";

        public void Execute(IOutputContext ctx)
        {
            try
            {
                if (sa == null) return;
                ushort logical = 0;
                if (!string.IsNullOrEmpty(sa.details)) ushort.TryParse(sa.details, out logical);
                uint native = 0;
                try { native = SyntheticDispatcher.ResolveNativeKey(logical); } catch { native = 0; }

                var trigger = new TriggerContextImpl
                {
                    Device = ctx.Device,
                    IsEdgeEstablished = true,
                    LogicalValue = logical,
                    NativeValue = native,
                    OutputHandler = ctx.OutputHandler,
                    Timestamp = DateTime.UtcNow
                };

                var binding = new KeyActionBinding(sa);
                var ctrl = ActionManager.GetOrCreateControllerForAction(ctx.Device, sa);
                try { ctrl?.Handle(binding, trigger); } catch { }
            }
            catch { }
        }

        public void Stop(IOutputContext ctx)
        {
            try
            {
                if (sa == null) return;
                ushort logical = 0;
                if (!string.IsNullOrEmpty(sa.details)) ushort.TryParse(sa.details, out logical);
                uint native = 0;
                try { native = SyntheticDispatcher.ResolveNativeKey(logical); } catch { native = 0; }

                var trigger = new TriggerContextImpl
                {
                    Device = ctx.Device,
                    IsEdgeEstablished = false,
                    LogicalValue = logical,
                    NativeValue = native,
                    OutputHandler = ctx.OutputHandler,
                    Timestamp = DateTime.UtcNow
                };

                var binding = new KeyActionBinding(sa);
                var ctrl = ActionManager.GetOrCreateControllerForAction(ctx.Device, sa);
                try { ctrl?.Handle(binding, trigger); } catch { }
            }
            catch { }
        }
    }
}
