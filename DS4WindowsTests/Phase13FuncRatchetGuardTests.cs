using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase13-Step8 8-d: サービスを遅延で取る <c>Func&lt;I…&gt;</c> を、許可リスト（型 × 利用者のファイル × 理由）で固定する。増えたら失敗する。
    /// 残るのは、依存グラフに本物の循環があるもの（Phase13Step8ResolutionGraphProbeTests で確認）だけ:
    /// <list type="bullet">
    /// <item><c>Func&lt;IMapping&gt;</c> ← <c>DefaultActionManager</c>: <c>Mapping</c> → … → <c>ActionManager</c>（<c>DefaultActionManager</c>）。直接受けると循環する。
    /// ホスト構築時（<c>AppHost.PreallocateActionEntries</c>）に <c>Mapping</c> を作らない理由も兼ねる。再検討条件: <c>Mapping</c> が <c>IManagedActionManager</c> に依存しなくなったとき。</item>
    /// <item><c>Func&lt;IActionBindingBuilder&gt;</c> ← <c>ProfileApplicationService</c>: <c>ActionBindingBuilder</c> → <c>IProfileSwitcher</c> → <c>IProfileApplicationService</c>。
    /// 再検討条件: 束縛の構築を <c>ProfileApplicationService</c> の外へ出したとき。</item>
    /// </list>
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    public class Phase13FuncRatchetGuardTests
    {
        private static readonly Regex LazyService = new Regex(@"Func<\s*[\w.]*\b(I[A-Z]\w*)\s*>");

        // ファイル名 → そのファイルに現れてよい遅延取得の型
        private static readonly (string File, string Type)[] Allowed =
        {
            ("DefaultActionManager.cs", "IMapping"),            // 本物の循環（Mapping → … → ActionManager）
            ("ProfileApplicationService.cs", "IActionBindingBuilder"),   // 本物の循環（ActionBindingBuilder → … → ProfileApplicationService）
            ("ServiceRegistration.Processing.cs", "IMapping"),             // 上の 2 つの登録（Phase14c-Step6: 第2層の登録のファイル）
            ("ServiceRegistration.Processing.cs", "IActionBindingBuilder"),
        };

        [Fact]
        public void LazyServiceFuncs_AreOnlyTheAllowedOnes()
        {
            string anchor = SourceFileLocator.Find("DS4Windows/Core/Settings/BackingStore.cs");
            if (anchor == null) return;

            string root = SourceFileLocator.RootFromAnchor(anchor);
            var found = Directory.GetFiles(Path.Combine(root, "DS4Windows"), "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Replace('\\', '/').Contains("/obj/") && !f.Replace('\\', '/').Contains("/bin/"))
                .SelectMany(f => LazyService.Matches(SourceText.StripComments(File.ReadAllText(f))).Cast<Match>()
                    .Select(m => (File: Path.GetFileName(f), Type: m.Groups[1].Value)))
                .Distinct()
                .ToList();

            var unexpected = found.Where(x => !Allowed.Contains(x)).ToList();
            Assert.True(unexpected.Count == 0,
                "許可リストにない遅延取得（Func<I…>）がある: " + string.Join(", ", unexpected.Select(x => x.File + " " + x.Type)));

            var stale = Allowed.Where(a => !found.Contains(a)).ToList();
            Assert.True(stale.Count == 0,
                "許可リストにあるが、もう現れない（リストから外してください）: " + string.Join(", ", stale.Select(x => x.File + " " + x.Type)));
        }
    }
}
