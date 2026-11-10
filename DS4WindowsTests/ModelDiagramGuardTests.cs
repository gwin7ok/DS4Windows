using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// モデル図（<c>docs-forDIMG/Model-Diagram/01-Structure.md</c>）が、実装と食い違わないための検査。
    /// 「実在する主要な型」の表の 1 列目に書いた型名が、アプリのアセンブリに実在することを確かめる。
    /// 型を改名・削除したときは、図の表も直す。実装されていない構想は <c>Ideas.md</c> に書き、この検査の対象にしない。
    /// ソースが見つからない環境（配布物だけで実行する場合）では検査を省略する。
    /// </summary>
    public class ModelDiagramGuardTests
    {
        private const string StructurePath = "docs-forDIMG/Model-Diagram/01-Structure.md";
        private const string TableHeading = "## 5. 実在する主要な型";

        private static IReadOnlyList<string> ReadTypeNames(string path)
        {
            string[] lines = File.ReadAllLines(path);
            int start = Array.FindIndex(lines, l => l.StartsWith(TableHeading, StringComparison.Ordinal));
            Assert.True(start >= 0, "図に「" + TableHeading + "」の節がない");
            var names = new List<string>();
            for (int i = start + 1; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("## ", StringComparison.Ordinal))
                {
                    break;
                }

                Match m = Regex.Match(lines[i], @"^\|\s*`([A-Za-z0-9_]+)`\s*\|");
                if (m.Success)
                {
                    names.Add(m.Groups[1].Value);
                }
            }

            return names;
        }

        [Fact]
        public void TypesListedInStructureDiagram_ExistInTheApplication()
        {
            string path = SourceFileLocator.Find(StructurePath);
            if (path == null)
            {
                return;
            }

            IReadOnlyList<string> names = ReadTypeNames(path);
            Assert.True(names.Count >= 20, "表から読めた型が少なすぎる: " + names.Count);

            var existing = new HashSet<string>(
                typeof(DS4Windows.Composition.AppHost).Assembly.GetTypes().Select(t => t.Name));
            string[] missing = names.Where(n => !existing.Contains(n)).ToArray();
            Assert.True(missing.Length == 0, "図に載っているが実在しない型: " + string.Join(", ", missing));
        }

        [Fact]
        public void StructureDiagram_HasNoDuplicateTypeRows()
        {
            string path = SourceFileLocator.Find(StructurePath);
            if (path == null)
            {
                return;
            }

            IReadOnlyList<string> names = ReadTypeNames(path);
            string[] dup = names.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
            Assert.True(dup.Length == 0, "図の表に重複した型: " + string.Join(", ", dup));
        }

        [Fact]
        public void ModelDiagramFolder_HasTheThreeDocumentsAndIdeas()
        {
            foreach (string file in new[] { "01-Structure.md", "02-Extension-Guide.md", "03-Lifecycle-and-DI.md", "Ideas.md" })
            {
                // 見つからなければ SourceFileLocator が失敗にする（ソースがない環境では null）
                SourceFileLocator.Find("docs-forDIMG/Model-Diagram/" + file);
            }
        }
    }
}
