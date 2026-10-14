using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using DS4Windows;
using DS4Windows.DI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step10-4-2: クラスタ 1（プロファイル系サービス）の Global／AppHost／Program.rootHub 直接参照解消の回帰ガード
    /// （<c>Phase6-Step10-4-Plan.md</c> §13）。10-4-2b／2c で、対象のファイルを追加していく。
    /// </summary>
    public class Step10FourTwoGlobalReferenceGuardTests
    {
        // ファイルごとに、使ってよい Global メンバー（薄い委譲・本体が Global にあるもの）
        private static readonly Dictionary<string, string[]> Allowed = new Dictionary<string, string[]>
        {
            // 10-4-2a
            ["DS4Windows/DS4Control/Services/ProfileActionChainService.cs"] = new string[0],
            // ApplyProfile: 実装本体が Global にある（Phase7 で Mapping の instance 化とあわせて移す。TODO 付き）
            ["DS4Windows/DS4Control/Services/ProfileApplicationService.cs"] = new[] { "ApplyProfile" },
            // 実行中アクション状態のクリアを含む Global 側の実装への薄い委譲
            ["DS4Windows/DS4Control/Services/SpecialActionRepository.cs"] =
                new[] { "CreateStdActions", "SaveAction", "RemoveAction", "SaveActions" },
        };

        private static string ReadWithoutComments(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null) return null;
            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);
            return text;
        }

        [Fact]
        public void ReplacedFiles_DoNotReferenceGlobalExceptRetainedMembers()
        {
            foreach (var entry in Allowed)
            {
                string text = ReadWithoutComments(entry.Key);
                if (text == null) continue;

                var unexpected = Regex.Matches(text, @"\bGlobal\.(\w+)")
                    .Cast<Match>().Select(m => m.Groups[1].Value)
                    .Where(n => !entry.Value.Contains(n)).Distinct().ToList();
                Assert.True(unexpected.Count == 0,
                    $"{entry.Key} に温存項目以外の Global 参照がある: {string.Join(", ", unexpected)}");

                Assert.DoesNotContain("AppHost", text);
                Assert.DoesNotContain("Program.rootHub", text);
            }
        }

        [Fact]
        public void ProfileActionChainService_RequiresAllDependencies()
        {
            Assert.Throws<ArgumentNullException>(() => new ProfileActionChainService(null, new MappingActionDispatcher()));
            Assert.Throws<ArgumentNullException>(() => new ProfileActionChainService(new ProfileActionProvider(), null));
        }

        [Fact]
        public void SpecialActionRepository_RequiresStoreAndPathService()
        {
            Assert.Throws<ArgumentNullException>(() => new SpecialActionRepository(null, new PathService()));
            Assert.Throws<ArgumentNullException>(() => new SpecialActionRepository(new BackingStore(), null));
        }

        [Fact]
        public void ProfileApplicationService_RequiresAllDependencies()
        {
            var settings = new ProfileSettingsService();
            var chain = new ProfileActionChainService(new ProfileActionProvider(), new MappingActionDispatcher());
            var state = new DeviceStateService();
            Func<ControlService> control = () => null;
            var repo = new ProfileRepository(settings, null, null);

            Assert.Throws<ArgumentNullException>(() => new ProfileApplicationService(null, chain, state, control, repo));
            Assert.Throws<ArgumentNullException>(() => new ProfileApplicationService(settings, null, state, control, repo));
            Assert.Throws<ArgumentNullException>(() => new ProfileApplicationService(settings, chain, null, control, repo));
            Assert.Throws<ArgumentNullException>(() => new ProfileApplicationService(settings, chain, state, null, repo));
            Assert.Throws<ArgumentNullException>(() => new ProfileApplicationService(settings, chain, state, control, null));
        }
    }
}
