using System;
using System.Collections.Generic;
using DS4Windows.DI;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase8-Step3 W3: 1 つ以上のボタンの AND 条件で成立するトリガー（Phase8-Step2-Design.md §4。D2＝B1: Controls タブとスペシャルアクションの両方を扱う汎用クラス）。
    /// 判定は、現行の <c>Mapping.MapCustomAction</c> の分岐 A（<c>delayTime</c> つき）と分岐 C（標準。Macro の <c>RunOnRelease</c> を含む）と同じ意味
    /// （Phase8-Step2-Intent-Decisions.md 項目 2。タップ判定の分岐 B は移植しない［項目 3］）。
    /// <list type="bullet">
    /// <item><description>遅延（<c>delaySeconds</c> &gt; 0）: <see cref="ITriggerInput.Delay"/> の共有の基準時刻を、現行と同じ規則で更新する。遅延つきのときは <c>invertOnRelease</c> を無視する（現行も分岐 A では反転しない）。</description></item>
    /// <item><description><c>invertOnRelease</c>（Macro の <c>RunOnRelease</c>）: 押された時点で「準備」状態になり（成立にはしない）、離された時点で 1 回だけ成立する。準備状態は、この型の内部状態（Q2＝A。現行の <c>SpecialAction.firstTouch</c> は使わない）。</description></item>
    /// </list>
    /// 割り当て 0（<see cref="Evaluate"/> は、構築時に確保した配列の走査と、構造体の返却のみ）。
    /// </summary>
    public sealed class ComboTriggerInputAction : IInputAction
    {
        private readonly DS4Controls[] _triggers;
        private readonly IProfileSettingsService _profileSettings;
        private readonly double _delaySeconds;
        private readonly bool _invertOnRelease;
        private bool _armed;

        /// <param name="triggers">構成するボタン（1 つ以上）。</param>
        /// <param name="profileSettings">遅延の基準時刻の更新に使うデバウンス（<c>DebouncingMs</c>）の取得元。</param>
        /// <param name="delaySeconds">0 より大きいと、押し続けてこの秒数が経ったときに成立する（現行の <c>delayTime</c>）。</param>
        /// <param name="invertOnRelease">true なら、離したときに成立する（現行の Macro の <c>RunOnRelease</c>）。</param>
        public ComboTriggerInputAction(string name, IReadOnlyList<DS4Controls> triggers, IProfileSettingsService profileSettings,
            double delaySeconds = 0.0, bool invertOnRelease = false)
        {
            if (triggers == null) throw new ArgumentNullException(nameof(triggers));
            if (triggers.Count == 0) throw new ArgumentException("トリガーのボタンが 1 つ以上必要です。", nameof(triggers));

            Name = name ?? string.Empty;
            _triggers = new DS4Controls[triggers.Count];
            for (int i = 0; i < _triggers.Length; i++) _triggers[i] = triggers[i];
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _delaySeconds = delaySeconds;
            _invertOnRelease = invertOnRelease;
        }

        public string Name { get; }

        public IReadOnlyList<DS4Controls> Triggers => _triggers;

        /// <summary>押し続けて成立するまでの秒数（現行の <c>delayTime</c>。0 なら遅延なし）。</summary>
        public double DelaySeconds => _delaySeconds;

        /// <summary>true なら、離したときに成立する（現行の Macro の <c>RunOnRelease</c>）。</summary>
        public bool InvertsOnRelease => _invertOnRelease;

        public TriggerState Evaluate(ITriggerInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            bool allActive = true;
            for (int i = 0; i < _triggers.Length; i++)
            {
                if (!input.IsActive(_triggers[i]))
                {
                    allActive = false;
                    break;
                }
            }

            if (_delaySeconds > 0.0)
                return EvaluateDelayed(input, allActive);

            if (!_invertOnRelease)
                return allActive ? TriggerState.Established : TriggerState.NotEstablished;

            if (_armed)
            {
                if (allActive) return TriggerState.NotEstablished; // 押し続けている間は待つ
                _armed = false;
                return TriggerState.Established;                   // 離された: 1 回だけ成立
            }

            if (allActive) _armed = true;                          // 押された: 準備（成立にはしない）
            return TriggerState.NotEstablished;
        }

        // 現行の MapCustomAction 分岐 A と同じ更新の規則（nowAction／oldnowAction は input.Delay に置き換わる）
        private TriggerState EvaluateDelayed(ITriggerInput input, bool allActive)
        {
            DelayClock clock = input.Delay;
            DateTime utcNow = input.UtcNow;

            if (allActive)
            {
                clock.Now = utcNow;
                return clock.Now >= clock.Old + TimeSpan.FromSeconds(_delaySeconds)
                    ? TriggerState.Established
                    : TriggerState.NotEstablished;
            }

            if (clock.Now < utcNow - TimeSpan.FromMilliseconds(100) + TimeSpan.FromMilliseconds(_profileSettings.DebouncingMs[input.Device]))
                clock.Old = utcNow;

            return TriggerState.NotEstablished;
        }

        public void Reset()
        {
            _armed = false;
        }
    }
}
