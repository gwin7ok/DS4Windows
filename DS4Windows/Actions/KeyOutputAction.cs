using System;
using DS4Windows.DI;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step3 W4: Key 種別の出力。<see cref="KeyAction"/> へ委譲する（Phase8-Step2-Design.md §7）。押下／解放の意味（Press か Toggle か、リピート）は
    /// 従来どおり <c>KeyButtonActionController</c> が決める（系統 B の <c>KeyActionAdapter</c> 経由と同じ経路）。
    /// 論理キー＝<c>SpecialAction.details</c>、ネイティブキー＝<c>OutputKBMMapping.GetRealEventKey</c>（現行の <c>Mapping</c> の Key 経路と同じ。Q7＝A）。
    /// ScanCode の指定は、現行どおり下流には渡さない（<c>KeyAction</c> 自身が <c>keyType</c> から読む。別件として記録済み）。
    /// 旧実装（<c>TriggerContextImpl</c> を独自に組み立てて再ディスパッチ）は、一度も呼ばれていなかったため置き換えた。まだ <c>Mapping</c>・DI には配線していない（Step4 以降）。
    /// </summary>
    public sealed class KeyOutputAction : IOutputAction
    {
        private readonly SpecialAction _sa;
        private readonly IProfileSettingsService _profileSettings;
        private readonly KeyAction _keyAction;
        private readonly ushort _logicalKey;

        public KeyOutputAction(SpecialAction sa, IProfileSettingsService profileSettings)
        {
            _sa = sa ?? throw new ArgumentNullException(nameof(sa));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _keyAction = new KeyAction(sa, -1);

            ushort logical = 0;
            if (!string.IsNullOrEmpty(sa.details)) ushort.TryParse(sa.details, out logical);
            _logicalKey = logical;
        }

        public string Id => string.IsNullOrEmpty(_sa.name) ? "KeyOutput" : _sa.name;

        public void Execute(IOutputContext ctx)
        {
            if (ctx == null) return;
            _keyAction.OnTrigger(ctx.Device, _logicalKey, ResolveNativeKey(), false, ctx.OutputHandler);
        }

        public void Stop(IOutputContext ctx)
        {
            if (ctx == null) return;
            _keyAction.OnRelease(ctx.Device, _logicalKey, ResolveNativeKey(), false, ctx.OutputHandler);
        }

        // SyntheticDispatcherService.ResolveNativeKey と同じ規則（対応表がない・例外のときは論理キーのまま）
        private uint ResolveNativeKey()
        {
            try
            {
                var mapping = _profileSettings.OutputKBMMapping;
                if (mapping != null) return mapping.GetRealEventKey(_logicalKey);
            }
            catch { }

            return _logicalKey;
        }
    }
}
