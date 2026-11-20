using System;
using System.IO;
using Xunit;

namespace DS4WindowsTests
{
    public class ScpUtilIntegrationTests
    {
        [Fact]
        public void LoadSave_Roundtrip_PreservesWidthsAndLastChecked()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".xml");
            try
            {
                string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                    "<Profile app_version=\"3.11.3\" config_version=\"2\">\n" +
                    "  <profileEditorLeftWidth>760</profileEditorLeftWidth>\n" +
                    "  <profileEditorRightWidth>600</profileEditorRightWidth>\n" +
                    "  <specialActionNameColWidth>310</specialActionNameColWidth>\n" +
                    "  <specialActionTriggerColWidth>150</specialActionTriggerColWidth>\n" +
                    "  <specialActionDetailColWidth>220</specialActionDetailColWidth>\n" +
                    "  <LastChecked>11/11/2025 13:14:15</LastChecked>\n" +
                    "</Profile>";

                File.WriteAllText(tempPath, xml);

                var bs = new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService());
                bs.m_Profile = tempPath;
                bool loaded = new DS4Windows.Core.Persistence.AppSettingsXmlSerializer(bs, new DS4Windows.Core.Platform.PathService(), new DS4Windows.Core.Platform.EnvironmentService()).Load();
                Assert.True(loaded, "Initial Load failed");

                Assert.Equal(760, bs.Layout.profileEditorLeftWidth);
                Assert.Equal(600, bs.Layout.profileEditorRightWidth);
                Assert.Equal(310, bs.Layout.specialActionNameColWidth);
                Assert.Equal(150, bs.Layout.specialActionTriggerColWidth);
                Assert.Equal(220, bs.Layout.specialActionDetailColWidth);
                Assert.NotEqual(default(DateTime), bs.UpdateCheck.lastChecked);

                // Modify and save
                bs.Layout.profileEditorLeftWidth = 123;
                bs.Layout.profileEditorRightWidth = 456;
                bs.Layout.specialActionNameColWidth = 11;
                bs.Layout.specialActionTriggerColWidth = 22;
                bs.Layout.specialActionDetailColWidth = 33;

                bool saved = new DS4Windows.Core.Persistence.AppSettingsXmlSerializer(bs, new DS4Windows.Core.Platform.PathService(), new DS4Windows.Core.Platform.EnvironmentService()).Save();
                Assert.True(saved, "Save failed");

                // Load into a new instance
                var bs2 = new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService());
                bs2.m_Profile = tempPath;
                bool loaded2 = new DS4Windows.Core.Persistence.AppSettingsXmlSerializer(bs2, new DS4Windows.Core.Platform.PathService(), new DS4Windows.Core.Platform.EnvironmentService()).Load();
                Assert.True(loaded2, "Reload failed");

                Assert.Equal(123, bs2.Layout.profileEditorLeftWidth);
                Assert.Equal(456, bs2.Layout.profileEditorRightWidth);
                Assert.Equal(11, bs2.Layout.specialActionNameColWidth);
                Assert.Equal(22, bs2.Layout.specialActionTriggerColWidth);
                Assert.Equal(33, bs2.Layout.specialActionDetailColWidth);

                // LastChecked should remain parseable and equal (string->DateTime roundtrip)
                Assert.NotEqual(default(DateTime), bs2.UpdateCheck.lastChecked);
                // Allow exact equality since serializer uses a fixed format
                Assert.Equal(bs.UpdateCheck.lastChecked.ToString("MM/dd/yyyy HH:mm:ss"), bs2.UpdateCheck.lastChecked.ToString("MM/dd/yyyy HH:mm:ss"));
            }
            finally
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
            }
        }
    }
}
