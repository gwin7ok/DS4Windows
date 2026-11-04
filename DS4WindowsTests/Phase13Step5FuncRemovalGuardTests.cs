using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase13-Step5: スロット状態の窓口の <c>Func</c>（ロードマップ #6・#7・#8）を撤廃したことのガード。
    /// <c>Func&lt;IDeviceStateAccessor&gt;</c>／<c>Func&lt;IUdpMotionHost&gt;</c> は本番のソースに 0 件、
    /// <c>Func&lt;IDeviceSlotHost&gt;</c> は <c>DeviceConnector</c> と登録（<c>ServiceRegistration</c>）だけ（Step5b で 0 にする）。
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。
    /// </summary>
    public class Phase13Step5FuncRemovalGuardTests
    {
        private static string StripComments(string text)
        {
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            return Regex.Replace(text, @"//[^\r\n]*", string.Empty);
        }

        private static string[] FilesContaining(string pattern)
        {
            string anchor = SourceFileLocator.Find("DS4Windows/DS4Control/BackingStore.cs");
            if (anchor == null) return null;

            string root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(anchor), "..", ".."));
            string sourceDir = Path.Combine(root, "DS4Windows");
            var regex = new Regex(pattern);
            return Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Replace('\\', '/').Contains("/obj/") && !f.Replace('\\', '/').Contains("/bin/"))
                .Where(f => regex.IsMatch(StripComments(File.ReadAllText(f))))
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
        public void FuncOfTheSlotHost_RemainsOnlyForTheConnectorAndTheRegistration()
        {
            string[] files = FilesContaining(@"Func<\s*(?:DS4Windows\.DI\.)?IDeviceSlotHost\s*>");
            if (files == null) return;
            Assert.Equal(new[] { "DeviceConnector.cs", "ServiceRegistration.cs" }, files);
        }
    }
}
