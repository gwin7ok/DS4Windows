using System.IO;
using DS4Windows;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step1 W4（実機確認で発見。以前からの不具合）: SA の Press Key（Key）の Scan Code は、Actions.xml に保存され、読み込みで復元される。
    /// 原因: <c>ActionsDTO</c> が、コンストラクターで「Scan Code」を除いた <c>action.details</c> をそのまま Details に書いていた（keyType の ScanCode ビットは書かれない）。
    /// </summary>
    public class Phase10Step1W4KeyActionScanCodePersistenceTests
    {
        private static SpecialAction SaveAndReload(PersistenceGoldenContext ctx, string details, string extras, out string xml)
        {
            string path = Path.Combine(ctx.AppData, "Actions.xml");
            BackingStore saveStore = PersistenceHarness.NewStore();
            saveStore.m_Actions = path;
            // SpecialActionRepository.SaveAction が使う経路（SaveActionNew）
            new SpecialActionsXmlSerializer(saveStore).SaveActionNew("K", "Cross", 4, details, false, 0.0, extras);
            xml = File.ReadAllText(path);

            BackingStore loaded = PersistenceHarness.LoadActions(ctx, xml);
            return loaded.actions.Find(a => a.name == "K");
        }

        [Fact]
        public void KeyWithScanCode_IsWrittenToActionsXml_AndRestoredOnLoad()
        {
            using (var ctx = new PersistenceGoldenContext())
            {
                SpecialAction action = SaveAndReload(ctx, "65 Scan Code", "Press\n", out string xml);

                Assert.Contains("Scan Code", xml);
                Assert.NotNull(action);
                Assert.Equal("65", action.details);
                Assert.True(action.keyType.HasFlag(DS4KeyType.ScanCode));
            }
        }

        [Fact]
        public void KeyWithoutScanCode_StaysWithoutScanCode()
        {
            using (var ctx = new PersistenceGoldenContext())
            {
                SpecialAction action = SaveAndReload(ctx, "65", "Press\n", out string xml);

                Assert.DoesNotContain("Scan Code", xml);
                Assert.Equal("65", action.details);
                Assert.False(action.keyType.HasFlag(DS4KeyType.ScanCode));
            }
        }

        [Fact]
        public void KeyWithScanCodeAndToggle_KeepsBoth()
        {
            using (var ctx = new PersistenceGoldenContext())
            {
                SpecialAction action = SaveAndReload(ctx, "65 Scan Code", "Toggle\nR1", out string xml);

                Assert.True(action.keyType.HasFlag(DS4KeyType.ScanCode));
                Assert.True(action.keyType.HasFlag(DS4KeyType.Toggle));
                Assert.Equal("65", action.details);
            }
        }
    }
}
