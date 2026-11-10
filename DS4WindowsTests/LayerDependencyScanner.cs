using System;
using System.Collections.Generic;
using System.Linq;
using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Dependencies;
using ArchUnitNET.Loader;
using DS4Windows.Composition;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase14b-Step1（R-01）: 本体のアセンブリ（DS4WinWPF.csproj の出力）を ArchUnitNET で読み、型どうしの参照の辺を返す。
    /// 参照はメソッド本体（呼び出し、<c>new</c>、フィールドの読み書き）、シグネチャ、基底型、属性、ジェネリックの実引数を含む。
    /// <c>const</c> の値・列挙型の定数・<c>nameof</c>・<c>using</c>・コメントは IL に残らないため、辺にならない（計画書 §3.3 の限界）。
    /// 読み込みは重いので、1 回だけ行って使い回す。
    /// </summary>
    public static class LayerDependencyScanner
    {
        /// <summary>トップレベルの型どうしの、層をまたぐ参照の辺。</summary>
        public readonly struct Edge
        {
            public Edge(string source, ArchLayer sourceLayer, string target, ArchLayer targetLayer)
            {
                Source = source;
                SourceLayer = sourceLayer;
                Target = target;
                TargetLayer = targetLayer;
            }

            public string Source { get; }
            public ArchLayer SourceLayer { get; }
            public string Target { get; }
            public ArchLayer TargetLayer { get; }
        }

        private static readonly Lazy<Architecture> s_architecture =
            new Lazy<Architecture>(() => new ArchLoader().LoadAssemblies(typeof(AppHost).Assembly).Build());

        private static readonly Lazy<IReadOnlyList<Edge>> s_edges = new Lazy<IReadOnlyList<Edge>>(Scan);

        /// <summary>
        /// Phase14d-Step3: 本体の型（トップレベル。入れ子の型の参照も含めてまとめる）が参照する型の完全名（本体の外の型も含む。例: SharpOSC の型）。
        /// 外部のライブラリへの依存を特定の型に閉じ込めていることの検査に使う。
        /// </summary>
        public static IReadOnlyCollection<string> AllTargetsOf(string topLevelTypeFullName)
        {
            var targets = new HashSet<string>(StringComparer.Ordinal);
            bool found = false;
            foreach (IType type in s_architecture.Value.Types)
            {
                if (type.IsStub || type.IsGenericParameter) continue;
                if (LayerMap.ToTopLevelTypeName(type.FullName) != topLevelTypeFullName) continue;
                found = true;
                foreach (ITypeDependency dependency in type.Dependencies)
                    foreach (IType target in TargetsOf(dependency))
                        if (target != null && !target.IsGenericParameter)
                            targets.Add(target.FullName);
            }
            if (!found)
                throw new InvalidOperationException($"{topLevelTypeFullName} が本体に見つからない（型の名前を変えたら、検査も直す）");
            return targets;
        }

        /// <summary>
        /// Phase16-Step4a: 本体のトップレベルの型のうち、名前空間が <paramref name="externalNamespacePrefix"/> で始まる型（本体の外の型。例: ViGEm）を参照するものの完全名。
        /// 外部のドライバーへの依存を特定の名前空間に閉じ込めていることの検査に使う。
        /// </summary>
        public static IReadOnlyCollection<string> TopLevelTypesReferencingNamespace(string externalNamespacePrefix)
        {
            var sources = new HashSet<string>(StringComparer.Ordinal);
            foreach (IType type in s_architecture.Value.Types)
            {
                if (type.IsStub || type.IsGenericParameter) continue;
                foreach (ITypeDependency dependency in type.Dependencies)
                    foreach (IType target in TargetsOf(dependency))
                        if (target != null && !target.IsGenericParameter
                            && (target.Namespace?.FullName ?? string.Empty).StartsWith(externalNamespacePrefix, StringComparison.Ordinal))
                            sources.Add(LayerMap.ToTopLevelTypeName(type.FullName));
            }
            return sources;
        }

        /// <summary>本体の型（スタブを除く）から、本体の別の型への参照の辺（重複なし、同じトップレベルの型への参照は除く）。</summary>
        public static IReadOnlyList<Edge> Edges => s_edges.Value;

        /// <summary>辺に規則を当て、規則に反するものを "規則|参照元|参照先" の形で返す（重複なし、序数順）。</summary>
        public static IReadOnlyList<string> Violations(Func<string, bool> ruleFilter)
        {
            return Edges
                .Select(e => (Rule: LayerMap.RuleFor(e.SourceLayer, e.TargetLayer), e.Source, e.Target))
                .Where(x => x.Rule != null && ruleFilter(x.Rule))
                .Select(x => $"{x.Rule}|{x.Source}|{x.Target}")
                .Distinct(StringComparer.Ordinal)
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();
        }

        private static IReadOnlyList<Edge> Scan()
        {
            Architecture architecture = s_architecture.Value;

            var seen = new HashSet<(string, string)>();
            var edges = new List<Edge>();

            foreach (IType type in architecture.Types)
            {
                if (type.IsStub || type.IsGenericParameter)
                    continue;

                ArchLayer sourceLayer = LayerMap.Classify(type.Namespace?.FullName);
                if (sourceLayer == ArchLayer.External)
                    continue;

                string source = LayerMap.ToTopLevelTypeName(type.FullName);

                foreach (ITypeDependency dependency in type.Dependencies)
                {
                    foreach (IType target in TargetsOf(dependency))
                    {
                        if (target == null || target.IsGenericParameter)
                            continue;

                        ArchLayer targetLayer = LayerMap.Classify(target.Namespace?.FullName);
                        if (targetLayer == ArchLayer.External)
                            continue;

                        string targetName = LayerMap.ToTopLevelTypeName(target.FullName);
                        if (targetName == source)
                            continue;

                        if (seen.Add((source, targetName)))
                            edges.Add(new Edge(source, sourceLayer, targetName, targetLayer));
                    }
                }
            }

            return edges;
        }

        // 参照先の型と、そのジェネリックの実引数（List<SpecialAction> の SpecialAction など。入れ子も含む）
        private static IEnumerable<IType> TargetsOf(ITypeDependency dependency)
        {
            yield return dependency.Target;

            foreach (IType t in Flatten(dependency.TargetGenericArguments))
                yield return t;
        }

        private static IEnumerable<IType> Flatten(IEnumerable<GenericArgument> arguments)
        {
            if (arguments == null)
                yield break;

            foreach (GenericArgument argument in arguments)
            {
                yield return argument.Type;

                foreach (IType inner in Flatten(argument.GenericArguments))
                    yield return inner;
            }
        }
    }
}
