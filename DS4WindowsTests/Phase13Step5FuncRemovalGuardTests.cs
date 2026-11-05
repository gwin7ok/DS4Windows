using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase13-Step5: スロット状態の窓口の <c>Func</c>（ロードマップ #6・#7・#8）を撤廃したことのガード。
    /// <c>Func&lt;IDeviceStateAccessor&gt;</c>／<c>Func&lt;IUdpMotionHost&gt;</c> は本番のソースに 0 件、Phase13-Step6 で <c>Func&lt;ControlService&gt;</c> も 0 件にした、
    /// <c>Func&lt;IDeviceSlotHost&gt;</c> も、Phase13-Step5b（5b-d）で 0 件にした（<c>IDeviceSlotHost</c> ごと削除）。
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    public class Phase13Step5FuncRemovalGuardTests
    {
        private static string[] FilesContaining(string pattern)
        {
            string anchor = SourceFileLocator.Find("DS4Windows/Core/Settings/BackingStore.cs");
            if (anchor == null) return null;

            string root = SourceFileLocator.RootFromAnchor(anchor);
            string sourceDir = Path.Combine(root, "DS4Windows");
            var regex = new Regex(pattern);
            return Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Replace('\\', '/').Contains("/obj/") && !f.Replace('\\', '/').Contains("/bin/"))
                .Where(f => regex.IsMatch(SourceText.StripComments(File.ReadAllText(f))))
                .Select(f => Path.GetFileName(f))
                .OrderBy(x => x)
                .ToArray();
        }

        [Theory]
        [InlineData(@"Func<\s*(?:DS4Windows\.Services\.|DS4Windows\.DI\.)?IDeviceStateAccessor\s*>")]
        [InlineData(@"Func<\s*(?:DS4Windows\.DI\.)?IUdpMotionHost\s*>")]
        public void RemovedFuncs_DoNotAppearInProductionSources(string pattern)
        {
            string[] files = FilesContaining(pattern);
            if (files == null) return;
            Assert.True(files.Length == 0, "撤廃した Func が残っている: " + string.Join(", ", files));
        }

        [Fact]
        public void FuncOfTheSlotHost_NoLongerAppears()
        {
            string[] files = FilesContaining(@"Func<\s*(?:DS4Windows\.DI\.)?IDeviceSlotHost\s*>");
            if (files == null) return;
            Assert.True(files.Length == 0, "撤廃した Func<IDeviceSlotHost> が残っている: " + string.Join(", ", files));
        }

        /// <summary>Phase13-Step6 6-e: 9 利用者の <c>Func&lt;ControlService&gt;</c> と、その登録をすべて撤廃した（ロードマップ #1〜#5）。</summary>
        [Fact]
        public void FuncOfControlService_NoLongerAppears()
        {
            string[] files = FilesContaining(@"Func<\s*(?:DS4Windows\.)?ControlService\s*>");
            if (files == null) return;
            Assert.True(files.Length == 0, "撤廃した Func<ControlService> が残っている: " + string.Join(", ", files));
        }

        /// <summary>Phase13-Step7 7-d: <c>VirtualPadService</c> ⇄ <c>OutputSlotService</c> の循環の Func（ロードマップ #6〜#8）を撤廃した。</summary>
        [Theory]
        [InlineData(@"Func<\s*(?:DS4Windows\.DI\.)?IOutputSlotService\s*>")]
        [InlineData(@"Func<\s*(?:DS4Windows\.DI\.)?IVirtualPadService\s*>")]
        [InlineData(@"Func<\s*OutputSlotManager\s*>")]
        public void FuncsOfTheOutputSlotCycle_NoLongerAppear(string pattern)
        {
            string[] files = FilesContaining(pattern);
            if (files == null) return;
            Assert.True(files.Length == 0, "撤廃した Func が残っている: " + string.Join(", ", files));
        }
    }
}
