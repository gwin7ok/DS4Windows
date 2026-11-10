using System;
using System.Collections.Generic;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase14b-Step1（R-01）: 名前空間 → 層の対応と、層の組み合わせ → 規則の対応（`docs-forDIMG/MadeByAgent/Phase14b-Plan.md` §4 Step 14b-1）。
    /// 状態を持たない純粋関数だけにして、<see cref="LayerMapTests"/> で単体テストする。
    /// </summary>
    public static class LayerMap
    {
        // 前方一致は名前空間の区切り単位（"DS4Windows.CoreX" は "DS4Windows.Core" に一致しない）。長いものを先に調べる。
        private static readonly (string Prefix, ArchLayer Layer)[] Prefixes =
        {
            ("DS4Windows.Processing.Abstractions", ArchLayer.ProcessingAbstractions),
            ("DS4Windows.Processing", ArchLayer.Processing),
            ("DS4Windows.Properties", ArchLayer.Shared),
            ("DS4Windows.Translations", ArchLayer.Shared),
            ("DS4Windows.Input", ArchLayer.Input),
            ("DS4Windows.Output", ArchLayer.Output),
            ("DS4Windows.UI", ArchLayer.UI),
            ("DS4Windows.Composition", ArchLayer.Composition),
            ("DS4Windows.Core", ArchLayer.Core),
        };

        /// <summary>記録のみ（失敗させない）の規則。Phase16-Step6a で L2（Core → 第1層・第3層）が 0 件になり、すべて禁止の規則になったため空。</summary>
        public static readonly IReadOnlyCollection<string> RecordOnlyRules = new string[0];

        /// <summary>名前空間から層を決める。ルート名前空間 <c>DS4Windows</c> はアプリの起点（<c>App</c>）なので 4-b とする。</summary>
        public static ArchLayer Classify(string ns)
        {
            if (string.IsNullOrEmpty(ns))
                return ArchLayer.External;
            if (ns == "DS4Windows")
                return ArchLayer.Composition;

            foreach (var (prefix, layer) in Prefixes)
            {
                if (ns == prefix || ns.StartsWith(prefix + ".", StringComparison.Ordinal))
                    return layer;
            }

            return ArchLayer.External;
        }

        /// <summary>
        /// 参照元の層 → 参照先の層が、どの規則に反するか。反しなければ null。
        /// L1〜L7 は計画書 §4 の表と同じ。共有の資源と外部は常に許可。同じ層の中も許可。
        /// </summary>
        public static string RuleFor(ArchLayer source, ArchLayer target)
        {
            if (source == ArchLayer.External || target == ArchLayer.External || target == ArchLayer.Shared || source == ArchLayer.Shared)
                return null;

            switch (source)
            {
                case ArchLayer.Core:
                    // L1: Core は 第2層・4-a・4-b を参照しない（§3.4 R5）
                    if (target == ArchLayer.Processing || target == ArchLayer.ProcessingAbstractions || target == ArchLayer.UI || target == ArchLayer.Composition)
                        return "L1";
                    // L2: Core は 第1層・第3層を参照しない（D3: 記録のみ。レビュー R-17）
                    if (target == ArchLayer.Input || target == ArchLayer.Output)
                        return "L2";
                    return null;

                case ArchLayer.Input:
                    // L3: 第1層は 第2層・第3層・4-a・4-b を参照しない（§1）
                    if (target == ArchLayer.Processing || target == ArchLayer.ProcessingAbstractions || target == ArchLayer.Output
                        || target == ArchLayer.UI || target == ArchLayer.Composition)
                        return "L3";
                    return null;

                case ArchLayer.Output:
                    // L4: 第3層は 4-a・4-b を参照しない。第2層は契約（Processing.Abstractions）だけ（§1、§3.4 R2）
                    if (target == ArchLayer.Processing || target == ArchLayer.UI || target == ArchLayer.Composition)
                        return "L4";
                    return null;

                case ArchLayer.Processing:
                case ArchLayer.ProcessingAbstractions:
                    // L5: 第2層は 4-a・4-b を参照しない（§1）
                    if (target == ArchLayer.UI || target == ArchLayer.Composition)
                        return "L5";
                    return null;

                case ArchLayer.UI:
                    // L6: 4-a は 4-b を参照しない（§3.4 R3）
                    if (target == ArchLayer.Composition)
                        return "L6";
                    // L7: 4-a は 第1層・第3層を参照しない（目標。記録のみ。レビュー R-09）
                    if (target == ArchLayer.Input || target == ArchLayer.Output)
                        return "L7";
                    return null;

                default:
                    // 4-b（Composition Root）は、組み立てのために全層を知ってよい（§1）
                    return null;
            }
        }

        /// <summary>
        /// 型の完全名を、トップレベルの型の完全名にそろえる。ネスト型（"A+B"、"A/B"）とコンパイラーが作る型（"A+&lt;&gt;c"）は宣言側の型へ、
        /// ジェネリックの実引数（"A`1[[...]]"）は外す。
        /// </summary>
        public static string ToTopLevelTypeName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return fullName;

            int cut = fullName.Length;
            foreach (char c in new[] { '+', '/', '[', '<' })
            {
                int i = fullName.IndexOf(c);
                if (i > 0 && i < cut)
                    cut = i;
            }

            return fullName.Substring(0, cut);
        }
    }
}
