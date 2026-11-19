#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using DS4Windows.Core.Settings;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// プロファイルの読み込みで、要素が「ない」「解析できない」「空」「範囲の外」のときの結果を、全要素について固定する。
    /// 非既定値で保存したプロファイルの葉の要素を 1 つずつ変えて読み込み、変えていない読み込みとの状態の差を、1 つのゴールデンに並べる。
    /// 節の読み込みの書き方を替えても（補助への置き換えなど）、このゴールデンが変わらないことで、読み込みの動きの不変を確かめる。
    /// 動きを意図して変えるとき（解析に失敗した要素を代入しない）は、ゴールデンの差分が、変わる要素の一覧になる。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class ProfileLoadMutationGoldenTests
    {
        private readonly ITestOutputHelper _output;

        public ProfileLoadMutationGoldenTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static readonly (string Name, string? Value)[] Variants =
        {
            ("missing", null),
            ("unparsable", "abc"),
            ("empty", ""),
            ("huge", "99999"),
        };

        [Fact]
        [Trait("Category", "Slow")]
        public void LeafElementVariants_MatchGolden()
        {
            using (var ctx = new PersistenceGoldenContext())
            {
                string? baseXml = GoldenFile.ReadOrNull("profile-gap.xml");
                if (baseXml == null) return;

                BackingStore baseline = PersistenceHarness.LoadProfile(ctx, baseXml, "MutationBase");
                string baseState = ctx.Normalize(BackingStoreStateDumper.Dump(baseline));

                var doc = new XmlDocument();
                doc.LoadXml(baseXml);
                var leaves = new List<string>();
                CollectLeafPaths(doc.DocumentElement!, "/" + doc.DocumentElement!.Name, leaves);

                var output = new StringBuilder();
                int index = 0;
                foreach (string path in leaves)
                {
                    foreach ((string name, string? value) in Variants)
                    {
                        var variant = new XmlDocument();
                        variant.LoadXml(baseXml);
                        XmlNode node = variant.SelectSingleNode(path)!;
                        if (value == null) node.ParentNode!.RemoveChild(node);
                        else node.InnerText = value;

                        string result;
                        try
                        {
                            BackingStore store = Load(ctx, variant.OuterXml, "Mutation" + index++);
                            string diff = BackingStoreStateDumper.Diff(baseState, ctx.Normalize(BackingStoreStateDumper.Dump(store)));
                            result = string.IsNullOrWhiteSpace(diff) ? "(same)" : diff.Trim();
                        }
                        catch (Exception ex)
                        {
                            result = "EXCEPTION " + ex.GetType().Name;
                        }

                        output.Append("## ").Append(path).Append(" [").Append(name).AppendLine("]");
                        output.AppendLine(result);
                    }
                }

                GoldenFile.AssertMatches("profile-load-mutations.txt", output.ToString(), _output);
            }
        }

        private static BackingStore Load(PersistenceGoldenContext ctx, string xml, string name)
        {
            string path = Path.Combine(ctx.AppData, "Profiles", name + ".xml");
            File.WriteAllText(path, xml);
            BackingStore store = PersistenceHarness.NewStore();
            TestRepositoryFactory.CreateLegacyProfileXml(store).LoadProfile(PersistenceHarness.Slot, ctx.LoadTarget, path, false, false);
            return store;
        }

        private static void CollectLeafPaths(XmlElement element, string path, List<string> leaves)
        {
            var children = element.ChildNodes.OfType<XmlElement>().ToList();
            if (children.Count == 0)
            {
                if (element.ParentNode is XmlElement) leaves.Add(path);
                return;
            }

            var seen = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (XmlElement child in children)
            {
                seen.TryGetValue(child.Name, out int n);
                seen[child.Name] = ++n;
                int total = children.Count(c => c.Name == child.Name);
                CollectLeafPaths(child, total > 1 ? $"{path}/{child.Name}[{n}]" : $"{path}/{child.Name}", leaves);
            }
        }
    }
}
