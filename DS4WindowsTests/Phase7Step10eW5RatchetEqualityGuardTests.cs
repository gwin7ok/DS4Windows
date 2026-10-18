using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step10e W5（Q9＝A）: ラチェットの基準値と実数の一致の検査。
    /// 既存のラチェット（<see cref="Step10FourGlobalRatchetGuardTests"/>、<see cref="Phase7Step1ServiceLocatorRatchetGuardTests"/>）は
    /// 「基準値を超えない」ことしか見ないため、件数を減らしたのに基準値を更新し忘れても見つからない（10c の教訓）。
    /// 本検査は、基準値が実数より大きい場合も失敗させ、更新漏れを防ぐ。基準値に無いファイルは 0 として扱う。
    /// 数え方（コメントの除去と正規表現）は既存のラチェットと同じ。基準値は、既存のラチェットの定義をそのまま読む。
    /// Phase8 で <c>ServiceProviderHolder</c> を減らすときも、同じ検査で基準値を同時に更新させる。
    /// ソースが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class Phase7Step10eW5RatchetEqualityGuardTests
    {
        private static readonly Regex RootHubPattern = new Regex(@"Program\.rootHub", RegexOptions.Compiled);
        private static readonly Regex AppHostPattern = new Regex(@"AppHost\.(?:GetService|Services)", RegexOptions.Compiled);
        private static readonly Regex GlobalPattern = new Regex(@"(?<![\w.])Global\.\w+", RegexOptions.Compiled);
        private static readonly Regex LocatorPattern =
            new Regex(@"ServiceProviderHolder\s*\.\s*(?:Provider|GetRequiredService)\b", RegexOptions.Compiled);

        private readonly ITestOutputHelper _output;

        public Phase7Step10eW5RatchetEqualityGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static string StripComments(string text)
        {
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            return Regex.Replace(text, @"//[^\r\n]*", string.Empty);
        }

        private static IEnumerable<(string Relative, string Code)> EnumerateSources(string root)
        {
            string sourceDir = Path.Combine(root, "DS4Windows");
            foreach (string file in Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories))
            {
                string normalized = file.Replace('\\', '/');
                if (normalized.Contains("/obj/") || normalized.Contains("/bin/")) continue;

                string relative = "DS4Windows/" + Path.GetRelativePath(sourceDir, file).Replace('\\', '/');
                yield return (relative, StripComments(File.ReadAllText(file)));
            }
        }

        private string FindRoot()
        {
            string anchor = SourceFileLocator.Find("DS4Windows/DS4Control/BackingStore.cs");
            if (anchor == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return null;
            }

            return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(anchor), "..", ".."));
        }

        private static object ReadBaseline(Type owner)
        {
            FieldInfo field = owner.GetField("Baseline", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(field);
            return field.GetValue(null);
        }

        [Fact]
        public void GlobalRatchetBaseline_EqualsTheActualCounts()
        {
            string root = FindRoot();
            if (root == null) return;

            var baseline = (Dictionary<string, (int RootHub, int AppHost, int Global)>)ReadBaseline(typeof(Step10FourGlobalRatchetGuardTests));
            var actual = EnumerateSources(root).ToDictionary(
                s => s.Relative,
                s => (RootHubPattern.Matches(s.Code).Count, AppHostPattern.Matches(s.Code).Count, GlobalPattern.Matches(s.Code).Count));

            var mismatches = new List<string>();
            foreach (string relative in actual.Keys.Union(baseline.Keys).OrderBy(k => k, StringComparer.Ordinal))
            {
                baseline.TryGetValue(relative, out var expected);
                actual.TryGetValue(relative, out var found);
                if (expected.RootHub != found.Item1 || expected.AppHost != found.Item2 || expected.Global != found.Item3)
                    mismatches.Add($"{relative}: 実数 (rootHub {found.Item1}, AppHost {found.Item2}, Global {found.Item3})、基準 (rootHub {expected.RootHub}, AppHost {expected.AppHost}, Global {expected.Global})");
            }

            foreach (string m in mismatches) _output.WriteLine(m);
            Assert.True(mismatches.Count == 0,
                "Global／AppHost／Program.rootHub の基準値と実数が一致しない（件数を減らしたら基準値も更新すること）:\n" + string.Join("\n", mismatches));
        }

        [Fact]
        public void ServiceLocatorRatchetBaseline_EqualsTheActualCounts()
        {
            string root = FindRoot();
            if (root == null) return;

            var baseline = (Dictionary<string, int>)ReadBaseline(typeof(Phase7Step1ServiceLocatorRatchetGuardTests));
            var actual = EnumerateSources(root).ToDictionary(s => s.Relative, s => LocatorPattern.Matches(s.Code).Count);

            var mismatches = new List<string>();
            foreach (string relative in actual.Keys.Union(baseline.Keys).OrderBy(k => k, StringComparer.Ordinal))
            {
                baseline.TryGetValue(relative, out int expected);
                actual.TryGetValue(relative, out int found);
                if (expected != found)
                    mismatches.Add($"{relative}: 実数 {found}、基準 {expected}");
            }

            foreach (string m in mismatches) _output.WriteLine(m);
            Assert.True(mismatches.Count == 0,
                "ServiceProviderHolder の基準値と実数が一致しない（件数を減らしたら基準値も更新すること）:\n" + string.Join("\n", mismatches));
        }

        /// <summary>Phase7 の最終の基準値（Phase7-Step10e W5）。合計を固定し、意図しない変動を見つける。</summary>
        [Fact]
        public void FinalTotals_AreFixed()
        {
            string root = FindRoot();
            if (root == null) return;

            int rootHub = 0, appHost = 0, global = 0, locator = 0;
            foreach (var (_, code) in EnumerateSources(root))
            {
                rootHub += RootHubPattern.Matches(code).Count;
                appHost += AppHostPattern.Matches(code).Count;
                global += GlobalPattern.Matches(code).Count;
                locator += LocatorPattern.Matches(code).Count;
            }

            Assert.Equal(0, rootHub);
            Assert.Equal(3, appHost);   // App.xaml.cs の窓口 1 + AppHost.cs のログ文言 2
            Assert.Equal(2, global);    // ログ文言の文字列 2（LanguageSelectDialog.xaml.cs／LanguagePackViewModel.cs）
            Assert.Equal(17, locator);  // Actions/ の 17（Phase8 の持ち越し）
        }
    }
}
