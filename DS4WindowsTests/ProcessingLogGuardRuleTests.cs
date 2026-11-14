using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 第 2 層（<c>Processing</c>）の入力ループから呼ばれる経路で、Trace／Debug のログの文字列を、無効のときに組み立てない。
    /// 定数でない引数の <c>AppLogger.LogTrace／LogDebug</c> は、同じ行か直前の数行（6 行まで）で <c>IsTraceEnabled／IsDebugEnabled</c>（または Trace の真偽値）を確かめる。
    /// </summary>
    public class ProcessingLogGuardRuleTests
    {
        private static readonly Regex Call = new Regex(@"AppLogger\.Log(Trace|Debug)\(", RegexOptions.Compiled);
        private static readonly Regex ConstantCall = new Regex(@"AppLogger\.Log(Trace|Debug)\(""([^""\\]|\\.)*""\);", RegexOptions.Compiled);

        [Fact]
        public void EveryNonConstantTraceOrDebugCall_InProcessing_IsGuardedByTheLevel()
        {
            string root = SourceFileLocator.RepoRoot();
            if (root == null) return;

            var unguarded = new System.Collections.Generic.List<string>();
            foreach (string file in Directory.EnumerateFiles(Path.Combine(root, "DS4Windows", "Processing"), "*.cs", SearchOption.AllDirectories))
            {
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    string t = lines[i].TrimStart();
                    if (t.StartsWith("//") || !Call.IsMatch(lines[i])) continue;
                    if (ConstantCall.IsMatch(lines[i])) continue;
                    // 引数が次の行に続く形（`AppLogger.LogTrace(` で行が終わる）は、定数ではないものとして扱う
                    string context = string.Join(" ", lines.Skip(System.Math.Max(0, i - 6)).Take(7));
                    if (context.Contains("IsTraceEnabled") || context.Contains("IsDebugEnabled") || Regex.IsMatch(context, @"\b(trace|debug)\b")) continue;
                    unguarded.Add($"{Path.GetRelativePath(root, file)}:{i + 1}");
                }
            }

            Assert.True(unguarded.Count == 0, "レベルを確かめていない Trace／Debug のログがある:\n" + string.Join("\n", unguarded.Take(40)));
        }
    }
}
