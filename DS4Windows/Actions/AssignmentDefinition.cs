using System;
using System.Collections.Generic;

namespace DS4Windows.Actions
{
    /// <summary>
    /// Phase10-Step6 I1（Phase10-Step6-Plan.md §4。目標モデルは Phase10-Integration-Target-Model.md §3）: 共通の割り当て定義。
    /// Controls タブの割り当てと SpecialActions タブの割り当てを、<b>由来（<see cref="Origin"/>）の違いだけで</b>同じ形に表す。不変。
    /// <para>
    /// 種別（<see cref="Kind"/>）は、SA と共通の種別（<see cref="SpecialAction.ActionTypeId"/>）だけを持つ。
    /// Controls 固有の種別（パッドへの出力、マウス、アナログマウス、割り当てなしの素通し）は、定義に入れない
    /// （2026-10-21 ユーザー決定。Controls 固有の部品として builder 側に残す。Step5 で SA のアナログ出力を入れるときに、取り込みを検討する）。
    /// </para>
    /// </summary>
    // TODO(Phase10-Step6 I3): 作られた経緯＝統合 Step の I1。未接続である理由＝読み込みの部品（ControlsAssignmentReader／SpecialActionAssignmentReader）が I3 で作られ、
    // そこで初めてこの型を作る。接続予定＝I3（読み込みの部品）→ I4（builder の 1 本化）。現状の呼び出し側（ControlBindingBuilder／ActionBindingBuilder）は、まだ DS4ControlSettings／SpecialAction を直接読む。
    // 呼出元が 0 件でも、削除しない（CLAUDE.md 2.4）
    public sealed class AssignmentDefinition
    {
        /// <param name="id">固定 ID（SA は <c>SpecialAction.id</c>。Controls は、ボタンから決まる合成の名前でもよい）。</param>
        /// <param name="name">名前（ログや、エンジンが束縛を見分けるための名前）。</param>
        /// <param name="origin">由来。</param>
        /// <param name="kind">種別（SA と共通のもの）。</param>
        /// <param name="triggers">トリガーのボタン。1 つ以上。由来が Controls のときは、ちょうど 1 つ。</param>
        /// <param name="holdSeconds">押し続けてから成立するまでの秒数（0 以上）。</param>
        /// <param name="item">型のある項目（Program、Profile、BatteryCheck、MultiAction ほかの値）。種別に項目が要らなければ null。</param>
        /// <param name="key">Key の解釈済みの値。Key 以外は null。</param>
        /// <param name="button">Button の解釈済みの値。Button 以外は null。</param>
        /// <param name="extras">Extras（ボタンの割り当てに添える追加の出力）。ないときは null。</param>
        /// <param name="macro">Macro の手順（SA の <c>macro</c>）。Macro 以外は null。</param>
        /// <param name="keyType">Macro の再生の指定（ScanCode、Repeat。SA の <c>keyType</c>）。</param>
        public AssignmentDefinition(string id, string name, AssignmentOrigin origin, SpecialAction.ActionTypeId kind,
            IReadOnlyList<DS4Controls> triggers, double holdSeconds = 0, ControlSpecialItem item = null,
            KeyBindingValues? key = null, ButtonBindingValues? button = null, ExtrasBindingValues? extras = null,
            IReadOnlyList<int> macro = null, DS4KeyType keyType = DS4KeyType.None)
        {
            if (triggers == null || triggers.Count == 0)
                throw new ArgumentException("トリガーのボタンは 1 つ以上必要です。", nameof(triggers));
            if (origin == AssignmentOrigin.Controls && triggers.Count != 1)
                throw new ArgumentException("由来が Controls の定義のトリガーは、ちょうど 1 つです。", nameof(triggers));
            if (holdSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(holdSeconds), "押し続ける秒数は 0 以上です。");

            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            Origin = origin;
            Kind = kind;
            Triggers = new List<DS4Controls>(triggers).AsReadOnly();
            HoldSeconds = holdSeconds;
            Item = item?.Clone();
            Key = key;
            Button = button;
            Extras = extras;
            Macro = new List<int>(macro ?? Array.Empty<int>()).AsReadOnly();
            KeyType = keyType;
        }

        public string Id { get; }

        public string Name { get; }

        public AssignmentOrigin Origin { get; }

        public SpecialAction.ActionTypeId Kind { get; }

        public IReadOnlyList<DS4Controls> Triggers { get; }

        public double HoldSeconds { get; }

        /// <summary>型のある項目。取り出すたびに独立した複製を返す（定義を不変に保つため）。</summary>
        public ControlSpecialItem Item { get; }

        public KeyBindingValues? Key { get; }

        public ButtonBindingValues? Button { get; }

        public ExtrasBindingValues? Extras { get; }

        public IReadOnlyList<int> Macro { get; }

        public DS4KeyType KeyType { get; }
    }
}
