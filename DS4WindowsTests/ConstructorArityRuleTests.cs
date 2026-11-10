using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using DS4Windows.Composition;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// コンストラクターの引数の数の努力目標（15 以下。規約 §3.1）を、本体のアセンブリのリフレクションで検査する。
    /// 15 を超える型は <see cref="ConstructorArityBaseline"/> に凍結し、実数との一致を検査する（増えても減っても失敗。
    /// 規約 §3.1 のラチェットの一致検査と同じ運用）。値の入れ物・工場・View も免除せず、許可リストで増加だけを止める。
    /// 引数の数は型ごとに、インスタンス コンストラクター（非公開を含む。主コンストラクターも同じ）の最大値。
    /// 作り直し: 環境変数 DS4W_ARITY_BASELINE_OUT に書き出し先のパスを入れて実行する。
    /// </summary>
    public class ConstructorArityRuleTests
    {
        private const string BaselineOutEnv = "DS4W_ARITY_BASELINE_OUT";
        public const int Threshold = 15;

        private readonly ITestOutputHelper _output;

        public ConstructorArityRuleTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TypesOverThreshold_MatchBaselineExactly()
        {
            IReadOnlyDictionary<string, int> all = Measure();
            var actual = all.Where(kv => kv.Value > Threshold)
                .Select(kv => kv.Key + "|" + kv.Value)
                .OrderBy(e => e, StringComparer.Ordinal).ToList();
            var baseline = new HashSet<string>(ConstructorArityBaseline.Entries, StringComparer.Ordinal);
            var actualSet = new HashSet<string>(actual, StringComparer.Ordinal);

            WriteDistribution(all);

            var added = actual.Where(e => !baseline.Contains(e)).ToList();
            var removed = ConstructorArityBaseline.Entries.Where(e => !actualSet.Contains(e)).OrderBy(e => e, StringComparer.Ordinal).ToList();
            if (added.Count == 0 && removed.Count == 0)
                return;

            string outPath = Environment.GetEnvironmentVariable(BaselineOutEnv);
            if (!string.IsNullOrEmpty(outPath))
                File.WriteAllText(outPath, Render(actual), new UTF8Encoding(true));

            var message = new StringBuilder();
            message.AppendLine($"引数が {Threshold} を超える型が、許可リスト（ConstructorArityBaseline）と一致しません。");
            message.AppendLine("増えた、または引数が増えた型（責務の分割や束ねで是正すること。許可リストへは追加しない）:");
            foreach (string e in added) message.AppendLine("  + " + e);
            message.AppendLine("減った型（許可リストからも消す、または数を更新すること）:");
            foreach (string e in removed) message.AppendLine("  - " + e);
            message.AppendLine($"実測から作り直すには、環境変数 {BaselineOutEnv} に書き出し先のパスを入れて再実行する。");
            Assert.True(false, message.ToString());
        }

        [Fact]
        public void Measure_CountsKnownTypesAndSkipsCompilerGeneratedTypes()
        {
            IReadOnlyDictionary<string, int> all = Measure();
            Assert.True(all["DS4Windows.Processing.Mapping"] > Threshold);
            Assert.DoesNotContain(all.Keys, k => k.Contains('<') || k.Contains("DisplayClass"));
        }

        private void WriteDistribution(IReadOnlyDictionary<string, int> all)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"コンストラクターを持つ型: {all.Count}");
            foreach (int t in new[] { 5, 8, 10, 12, 15, 20 })
                sb.AppendLine($"  引数 {t} 以上の型: {all.Count(kv => kv.Value >= t)}");
            foreach (var kv in all.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).Take(20))
                sb.AppendLine($"  {kv.Value,3}  {kv.Key}");
            _output.WriteLine(sb.ToString());
        }

        private static IReadOnlyDictionary<string, int> Measure()
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (Type type in typeof(AppHost).Assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface || type.IsEnum || type.IsDelegate())
                    continue;
                if (type.FullName == null || type.FullName.Contains('<') || type.IsDefined(typeof(CompilerGeneratedAttribute), false))
                    continue;

                ConstructorInfo[] ctors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (ctors.Length == 0)
                    continue;
                result[type.FullName] = ctors.Max(c => c.GetParameters().Length);
            }

            return result;
        }

        private static string Render(IReadOnlyList<string> entries)
        {
            var sb = new StringBuilder();
            sb.AppendLine("namespace DS4WindowsTests");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// コンストラクターの引数が 15 を超える型の許可リスト（\"型の完全名|引数の数\"。序数順）。");
            sb.AppendLine("    /// <see cref=\"ConstructorArityRuleTests\"/> が実数と一致することを検査する（増えても減っても失敗）。");
            sb.AppendLine("    /// 是正で減ったら、同じコミットでここも更新する。新しい型は追加しない（責務の分割や束ねで是正する）。");
            sb.AppendLine("    /// 更新の手順: 環境変数 DS4W_ARITY_BASELINE_OUT にファイルのパスを入れてテストを実行すると、実測から作ったこのファイルの内容をそのパスへ書き出す。");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public static class ConstructorArityBaseline");
            sb.AppendLine("    {");
            sb.AppendLine("        public static readonly string[] Entries =");
            sb.AppendLine("        {");
            foreach (string e in entries)
                sb.AppendLine($"            \"{e}\",");
            sb.AppendLine("        };");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }
    }

    internal static class TypeDelegateExtensions
    {
        public static bool IsDelegate(this Type type) => typeof(Delegate).IsAssignableFrom(type);
    }
}
