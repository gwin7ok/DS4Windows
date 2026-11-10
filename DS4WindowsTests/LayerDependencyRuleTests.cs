using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase14b-Step1（R-01）: 層の依存の向き（規約 §1、§3.4 R2・R3・R5）を、本体のアセンブリの実際の型参照で検査する
    /// （計画書 `docs-forDIMG/MadeByAgent/Phase14b-Plan.md` §4 Step 14b-1）。
    /// 禁止の規則（L1〜L7）は、今ある違反を <see cref="LayerDependencyBaseline"/> で凍結し、実数との一致を検査する。
    /// 記録のみの規則は、Phase16-Step6a で L2 が 0 件になり、なくなった（すべて禁止の規則）。
    /// </summary>
    public class LayerDependencyRuleTests
    {
        private const string BaselineOutEnv = "DS4W_LAYER_BASELINE_OUT";

        private readonly ITestOutputHelper _output;

        public LayerDependencyRuleTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ForbiddenLayerDependencies_MatchBaselineExactly()
        {
            IReadOnlyList<string> actual = LayerDependencyScanner.Violations(rule => !LayerMap.RecordOnlyRules.Contains(rule));
            var actualSet = new HashSet<string>(actual, StringComparer.Ordinal);
            var baselineSet = new HashSet<string>(LayerDependencyBaseline.Entries, StringComparer.Ordinal);

            var added = actual.Where(e => !baselineSet.Contains(e)).ToList();
            var removed = LayerDependencyBaseline.Entries.Where(e => !actualSet.Contains(e)).OrderBy(e => e, StringComparer.Ordinal).ToList();

            _output.WriteLine(Subtotals("禁止の規則（実測）", actual));

            if (added.Count == 0 && removed.Count == 0)
                return;

            string outPath = Environment.GetEnvironmentVariable(BaselineOutEnv);
            if (!string.IsNullOrEmpty(outPath))
                File.WriteAllText(outPath, RenderBaseline(actual), new UTF8Encoding(true));

            var message = new StringBuilder();
            message.AppendLine("層の依存の違反が、許可リスト（LayerDependencyBaseline）と一致しません。");
            message.AppendLine($"増えた違反（是正すること。許可リストへは追加しない）: {added.Count} 件");
            foreach (string e in added.Take(50))
                message.AppendLine("  + " + e);
            message.AppendLine($"減った違反（許可リストからも消すこと）: {removed.Count} 件");
            foreach (string e in removed.Take(50))
                message.AppendLine("  - " + e);
            message.AppendLine($"実測から許可リストを作り直すには、環境変数 {BaselineOutEnv} に書き出し先のパスを入れて再実行する。");
            Assert.True(false, message.ToString());
        }

        [Fact]
        public void Scanner_SeesReferencesInsideMethodBodies()
        {
            // 検査そのものが働くことの確認。AppLogger.NotificationService の getter は、本体の中でだけ new NullNotificationService() を行う
            // （シグネチャやフィールドには現れない、メソッド本体だけの参照）。
            // Phase14b-Step5: 旧対象（DS4Devices → UI の NotificationService.ShowToast）は是正したため差し替えた
            Assert.Contains(LayerDependencyScanner.Edges,
                e => e.Source == "DS4Windows.Core.Logging.AppLogger" && e.Target == "DS4Windows.Core.Logging.NullNotificationService");
        }

        [Fact]
        public void Scanner_AttributesCompilerGeneratedTypesToTheDeclaringType()
        {
            // ServiceRegistration.RegisterServices の登録ラムダ（コンパイラーが作るネスト型の中）での new ProfileIdentityService(...) が、
            // 宣言側の ServiceRegistration の辺として数えられること
            Assert.Contains(LayerDependencyScanner.Edges,
                e => e.Source == "DS4Windows.Composition.ServiceRegistration" && e.Target == "DS4Windows.Core.Persistence.ProfileIdentityService");
            Assert.DoesNotContain(LayerDependencyScanner.Edges, e => e.Source.Contains("<") || e.Source.Contains("+"));
        }

        private static string Subtotals(string title, IReadOnlyList<string> entries)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{title}: {entries.Count} 件");
            foreach (var group in entries.GroupBy(e => e.Substring(0, e.IndexOf('|'))).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                int sources = group.Select(e => e.Split('|')[1]).Distinct().Count();
                sb.AppendLine($"  {group.Key}: {group.Count()} 件（参照元の型 {sources}）");
            }

            return sb.ToString();
        }

        // LayerDependencyBaseline.cs の全体（説明のコメントを含む）を、実測の一覧で作り直す
        private static string RenderBaseline(IReadOnlyList<string> entries)
        {
            var sb = new StringBuilder();
            sb.AppendLine("namespace DS4WindowsTests");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Phase14b-Step1（R-01）: 層の依存の規則（L1〜L7）に今ある違反の許可リスト。\"規則|参照元の型|参照先の型\" の形（序数順）。");
            sb.AppendLine("    /// <see cref=\"LayerDependencyRuleTests\"/> が実数と一致することを検査する（多くても少なくても失敗。規約 §3.1 のラチェットの一致検査と同じ運用）。");
            sb.AppendLine("    /// 是正で減ったら、同じ Step でここからも消す。新しい違反は追加しない（是正する）。");
            sb.AppendLine($"    /// 更新の手順: 環境変数 {BaselineOutEnv} にファイルのパスを入れてテストを実行すると、実測から作った本ファイルの内容をそのパスへ書き出す。");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public static class LayerDependencyBaseline");
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
}
