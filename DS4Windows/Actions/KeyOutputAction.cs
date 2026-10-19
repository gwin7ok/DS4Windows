using System;
using System.Collections.Generic;
using DS4Windows.DI;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step3 W4: Key 種別の出力。<see cref="KeyAction"/> へ委譲する（Phase8-Step2-Design.md §7）。押下／解放の意味（Press か Toggle か、リピート）は
    /// 従来どおり <c>KeyButtonActionController</c> が決める（系統 B の <c>KeyActionAdapter</c> 経由と同じ経路）。
    /// 論理キー＝<c>SpecialAction.details</c>、ネイティブキー＝<c>OutputKBMMapping.GetRealEventKey</c>（現行の <c>Mapping</c> の Key 経路と同じ。Q7＝A）。
    /// ScanCode の指定は、現行どおり下流には渡さない（<c>KeyAction</c> 自身が <c>keyType</c> から読む。別件として記録済み）。
    /// 旧実装（<c>TriggerContextImpl</c> を独自に組み立てて再ディスパッチ）は、一度も呼ばれていなかったため置き換えた。
    /// Phase8-Step7 7.2-W2（Q3＝A）: 旧 <c>MapCustomAction</c> の Key の分岐が出していた Debug／Trace のログ（立ち上がり・解放）を、同じ文言・レベル・順序で出す（規約 2.3）。
    /// 旧経路との差: 「untriggerindex」は、新経路では <c>UntriggerIndex</c> を使わないため、常に -1。「beingTriggered」は、立ち上がりのとき常に False（旧経路と同じ）。
    /// 旧経路は Toggle の解放を送らないため（<c>UntriggerIndex = -1</c>）、Toggle のときは解放のログを出さない。
    /// </summary>
    public sealed class KeyOutputAction : IOutputAction
    {
        private readonly SpecialAction _sa;
        private readonly IProfileSettingsService _profileSettings;
        private readonly KeyAction _keyAction;
        private readonly ushort _logicalKey;
        private readonly bool _toggle;
        private readonly int _index;
        private readonly ISpecialActionRepository _actionRepository;

        // TODO(Phase8-Step8。Phase8-Step7-8a-Plan.md Q4＝A): 設定の解釈（details → 論理キー、keyType → ScanCode／Toggle）を、この互換のコンストラクターが
        // ActionBindingBuilder.InterpretKey を呼んで行う。ビルダーは解釈済みの値を渡す下のコンストラクターを使う。既存のテストが使うため残す。Step8 で整理する。
        public KeyOutputAction(SpecialAction sa, IProfileSettingsService profileSettings, int index = -1, ISpecialActionRepository actionRepository = null)
            : this(Require(sa), ActionBindingBuilder.InterpretKey(sa), profileSettings, index, actionRepository)
        {
        }

        /// <summary>
        /// Phase8-Step7 7.8a（Q9＝A）: 解釈済みの値（<paramref name="values"/>）を受け取る。<paramref name="identity"/> は、状態の鍵・ログの名前・系統 B への入力として使う
        /// （Phase8-Step7-8a-Plan.md §2.2。識別用の SpecialAction を中立な型に替えるのは Step8 以降）。
        /// </summary>
        public KeyOutputAction(SpecialAction identity, KeyBindingValues values, IProfileSettingsService profileSettings, int index = -1, ISpecialActionRepository actionRepository = null)
        {
            _index = index;
            _actionRepository = actionRepository;
            _sa = identity ?? throw new ArgumentNullException(nameof(identity));
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _keyAction = new KeyAction(identity, -1, values.LogicalKey, values.ScanCode);
            _logicalKey = values.LogicalKey;
            _toggle = values.Toggle;
        }

        private static SpecialAction Require(SpecialAction sa) => sa ?? throw new ArgumentNullException(nameof(sa));

        public string Id => string.IsNullOrEmpty(_sa.name) ? "KeyOutput" : _sa.name;

        public void Execute(IOutputContext ctx)
        {
            if (ctx == null) return;
            LogRising(ctx);
            _keyAction.OnTrigger(ctx.Device, _logicalKey, ResolveNativeKey(), false, ctx.OutputHandler);
        }

        public void Stop(IOutputContext ctx)
        {
            if (ctx == null) return;
            LogRelease(ctx);
            _keyAction.OnRelease(ctx.Device, _logicalKey, ResolveNativeKey(), false, ctx.OutputHandler);
        }

        private bool IsToggleKey => _toggle;

        private int ActionEntryCount
        {
            get
            {
                try { return _actionRepository?.ActionList?.Count ?? 0; }
                catch { return 0; }
            }
        }

        private string TriggerCombo => _sa.trigger != null && _sa.trigger.Count > 0 ? string.Join("+", _sa.trigger) : "(none)";

        // 旧 MapCustomAction の Key の分岐（立ち上がり）と同じ順序: entry → LogSpecialActionTrace → KeyTriggered ×2 → KEY triggered
        private void LogRising(IOutputContext ctx)
        {
            try
            {
                int device = ctx.Device;
                AppLogger.LogDebug($"SpecialAction KEY entry: name={_sa.name}, device={device}, uTriggerCount=0, untriggerindex=-1, beingTriggered=False");

                if (AppLogger.IsTraceEnabled) LogTraceRising(ctx);

                AppLogger.LogDebug($"SpecialAction KeyTriggered: device={device}, name={_sa.name}, index={_index}, ActionEntries={ActionEntryCount}");
                AppLogger.LogDebug($"SpecialAction KeyTriggered: device={device}, name={_sa.name}, trigger={TriggerCombo}, index={_index}");
                AppLogger.LogDebug($"SpecialAction KEY triggered: name={_sa.name}, device={device}, key={_logicalKey}");
            }
            catch { }
        }

        // 旧 LogSpecialActionTrace（立ち上がり）と同じ文言。「Trigger keys」は、旧経路が常に <none>（状態を渡さず例外を握りつぶす既存の挙動）
        private void LogTraceRising(IOutputContext ctx)
        {
            try
            {
                AppLogger.LogTrace($"Trigger detected for SA '{_sa.name}' on device {ctx.Device}");
                AppLogger.LogTrace($"SA '{_sa.name}' maps to Type:{_sa.type} Details:{_sa.details}");
                AppLogger.LogTrace($"Trigger keys for SA '{_sa.name}': <none>");
                try
                {
                    var parts = new List<string>();
                    (ctx.Frame as IFrameOutputTrace)?.AppendOutputCombo(parts);
                    string combo = parts.Count > 0 ? string.Join(", ", parts) : "<none>";
                    AppLogger.LogTrace($"SA '{_sa.name}' output combo: {combo}");
                }
                catch { }
            }
            catch { }
        }

        // 旧 MapCustomAction の Key の分岐（解放）と同じ: KeyReleased ×2。離されたトリガーは、最初に押されていないものの名前
        private void LogRelease(IOutputContext ctx)
        {
            if (IsToggleKey) return;

            try
            {
                int device = ctx.Device;
                AppLogger.LogDebug($"SpecialAction KeyReleased: device={device}, name={_sa.name}, index={_index}, ActionEntries={ActionEntryCount}");

                string released = "(unknown)";
                ITriggerInput input = (ctx as IOutputContextInput)?.Input;
                if (input != null)
                {
                    if (_sa.trigger != null)
                    {
                        for (int i = 0; i < _sa.trigger.Count; i++)
                        {
                            if (!input.IsActive(_sa.trigger[i])) { released = _sa.trigger[i].ToString(); break; }
                        }
                    }

                    if (released == "(unknown)" && _sa.uTrigger != null)
                    {
                        for (int i = 0; i < _sa.uTrigger.Count; i++)
                        {
                            if (!input.IsActive(_sa.uTrigger[i])) { released = _sa.uTrigger[i].ToString(); break; }
                        }
                    }
                }

                AppLogger.LogDebug($"SpecialAction KeyReleased: device={device}, name={_sa.name}, trigger={TriggerCombo}, released={released}, index={_index}");
            }
            catch { }
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
