using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step1 W4: Service Locator の 2 系統目 <c>ServiceProviderHolder.Provider</c>／<c>GetRequiredService</c> の退行ガード（ラチェット）。
    /// 1 系統目の <c>AppHost.GetService|Services</c> と <c>Program.rootHub</c> は <see cref="Step10FourGlobalRatchetGuardTests"/> が固定している。
    /// <c>ServiceProviderHolder</c> は台帳・ラチェットの対象外だったため、Phase7-0 の調査（<c>Phase7-0-Survey.md</c> §1.5 F-A）で追加した。
    /// - 件数が減るのは許可する（基準値を更新して、さらに固定すること）。
    /// - 件数が増える、または基準値に無いファイルに現れる場合は失敗する。
    /// Phase13-Step4: <c>ServiceProviderHolder</c>（型ごと）と <c>AppHost</c> の <c>SetProvider</c> 呼び出しを撤廃した。基準値は空（全ファイル 0 件）。
    /// 経緯: 2026-10-17 の走査値は計 23 件、Phase8-Step8 8c で 10 件（<c>Actions/</c>）まで減り、Phase13-Step4 で 0 件になった。
    /// ソースが見つからない環境では、検査を行わず合格とする。
    /// </summary>
    public class Phase7Step1ServiceLocatorRatchetGuardTests
    {
        private static readonly Dictionary<string, int> Baseline = new Dictionary<string, int>
        {
            // Phase13-Step4: ServiceProviderHolder を撤廃した（静的 ActionManager の廃止と、DefaultKeyButtonActionControllerFactory のコンストラクター注入）。
            // 基準値は空＝どのファイルでも 0 件。新しい参照を持ち込むと、この検査と一致の検査（Phase7Step10eW5RatchetEqualityGuardTests）が失敗する。
        };

        private static readonly Regex LocatorPattern =
            new Regex(@"ServiceProviderHolder\s*\.\s*(?:Provider|GetRequiredService)\b", RegexOptions.Compiled);

        private readonly ITestOutputHelper _output;

        public Phase7Step1ServiceLocatorRatchetGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ServiceProviderHolderReferences_DoNotExceedBaseline()
        {
            string anchor = SourceFileLocator.Find("DS4Windows/Core/Settings/BackingStore.cs"); // Phase7-Step10d W5: Global.cs は削除したため、同じフォルダーの BackingStore.cs を起点にする
            if (anchor == null)
            {
                _output.WriteLine("ソースが見つからないため検査を省略した。");
                return;
            }

            // <root>/DS4Windows/DS4Control/Global.cs → <root>
            string root = SourceFileLocator.RootFromAnchor(anchor);
            string sourceDir = Path.Combine(root, "DS4Windows");

            var violations = new List<string>();
            foreach (string file in Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories))
            {
                string normalized = file.Replace('\\', '/');
                if (normalized.Contains("/obj/") || normalized.Contains("/bin/")) continue;

                string relative = "DS4Windows/" + Path.GetRelativePath(sourceDir, file).Replace('\\', '/');
                int count = LocatorPattern.Matches(SourceText.StripComments(File.ReadAllText(file))).Count;
                if (count == 0) continue;

                Baseline.TryGetValue(relative, out int allowed);
                if (count > allowed)
                    violations.Add($"{relative}: ServiceProviderHolder.Provider|GetRequiredService {count} 件（基準 {allowed}）");
            }

            foreach (string v in violations) _output.WriteLine(v);
            Assert.True(violations.Count == 0,
                "基準値を超える Service Locator の参照がある（新しい ServiceProviderHolder の参照を持ち込まず、依存を注入すること）:\n" +
                string.Join("\n", violations));
        }
    }
}
