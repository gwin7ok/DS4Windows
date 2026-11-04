using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase13-Step4: 静的 <c>ActionManager</c>、<c>ServiceProviderHolder</c>、<c>ButtonSpecialActionOutput</c> を撤廃したことのガード。
    /// 型が存在しないこと、<c>DS4Windows/</c> に参照が残っていないこと、<c>AppHost</c> が <c>SetProvider</c> を呼ばないこと、
    /// 管理役の契約（<c>IsBeingTriggered</c>）を検査する。ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    public class Phase13Step4ActionManagerRemovalGuardTests
    {
        private static readonly Type[] Asm = { typeof(DS4Windows.Processing.Actions.IManagedActionManager) };

        private static string StripComments(string text)
        {
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            return Regex.Replace(text, @"//[^\r\n]*", string.Empty);
        }

        [Theory]
        [InlineData("DS4Windows.ActionManager")]
        [InlineData("DS4Windows.DI.ServiceProviderHolder")]
        [InlineData("DS4Windows.ButtonSpecialActionOutput")]
        public void RemovedTypes_DoNotExist(string fullName)
        {
            Assert.Null(Asm[0].Assembly.GetType(fullName));
        }

        [Fact]
        public void ProductionSources_DoNotReferenceTheRemovedTypes()
        {
            string anchor = SourceFileLocator.Find("DS4Windows/Core/Settings/BackingStore.cs");
            if (anchor == null) return;

            string root = SourceFileLocator.RootFromAnchor(anchor);
            string sourceDir = Path.Combine(root, "DS4Windows");
            var pattern = new Regex(@"(?<![\w.])(?:DS4Windows\.)?ActionManager\.(?!ToggledOnChanged:)\w+|ServiceProviderHolder|ButtonSpecialActionOutput\.|SetProvider\s*\(");

            var violations = new System.Collections.Generic.List<string>();
            foreach (string file in Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories))
            {
                string normalized = file.Replace('\\', '/');
                if (normalized.Contains("/obj/") || normalized.Contains("/bin/")) continue;

                // ログの文言（"ActionManager.ToggledOnChanged: ..." など）は文字列リテラルなので、除外する
                string code = Regex.Replace(StripComments(File.ReadAllText(file)), @"""(?:[^""\\]|\\.)*""", "\"\"");
                if (pattern.IsMatch(code))
                    violations.Add(Path.GetRelativePath(sourceDir, file));
            }

            Assert.True(violations.Count == 0, "撤廃した型の参照が残っている:\n" + string.Join("\n", violations));
        }

        [Fact]
        public void ManagedActionManager_ExposesIsBeingTriggered()
        {
            Assert.NotNull(typeof(DS4Windows.Processing.Actions.IManagedActionManager).GetMethod("IsBeingTriggered"));
        }
    }
}
