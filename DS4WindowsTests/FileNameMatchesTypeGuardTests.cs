using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// ファイル名＝型名の検査（規約 3.3 原則2）。本体の .cs が、名前空間の直下に型を 1 つだけ宣言するとき、その型の名前がファイル名と一致すること。
    /// 例外: <c>partial</c> の分割ファイル、<c>*.xaml.cs</c>、自動生成（<c>*.Designer.cs</c>）、<c>GlobalSuppressions.cs</c>、<c>AssemblyInfo.cs</c>。
    /// 型を持たないファイルは対象外。
    /// </summary>
    public class FileNameMatchesTypeGuardTests
    {
        private static readonly Regex TypeDeclaration = new Regex(
            @"^ {4}(?:\[[^\]]*\]\s*)*(?:(?:public|internal|sealed|abstract|static|unsafe|readonly|ref|partial|new)\s+)*(?<kind>class|interface|struct|enum|record)\s+(?<name>\w+)",
            RegexOptions.Multiline | RegexOptions.Compiled);

        [Fact]
        public void EveryFileWithASingleTopLevelType_IsNamedAfterThatType()
        {
            IReadOnlyList<string> files = SourceText.ProductionFiles();
            var offenders = new List<string>();
            int checkedCount = 0;
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                if (!name.EndsWith(".cs") || name.EndsWith(".xaml.cs") || name.EndsWith(".Designer.cs")
                    || name == "GlobalSuppressions.cs" || name == "AssemblyInfo.cs") continue;

                string text = SourceText.StripComments(File.ReadAllText(file));
                MatchCollection matches = TypeDeclaration.Matches(text);
                if (matches.Count != 1) continue;
                if (matches[0].Value.Contains("partial ")) continue;

                checkedCount++;
                string typeName = matches[0].Groups["name"].Value;
                if (Path.GetFileNameWithoutExtension(file) != typeName) offenders.Add(name + " → " + typeName);
            }

            if (files.Count > 0) Assert.True(checkedCount > 500, "検査の対象が少なすぎる（型の検出が壊れている）: " + checkedCount);
            Assert.True(offenders.Count == 0, "ファイル名と型名が違う: " + string.Join(", ", offenders));
        }
    }
}
