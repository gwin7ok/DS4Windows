using System;
using System.IO;
using System.Xml.Serialization;
using Xunit;
using DS4Windows.Core.Persistence.Dto;

namespace DS4WindowsTests
{
    public class AppSettingsDtoTests
    {
        [Fact]
        public void Deserialize_3_11_3_IncludesWidths()
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

            XmlSerializer serializer = new XmlSerializer(typeof(AppSettingsDTO), new XmlRootAttribute("Profile"));
            using StringReader sr = new StringReader(xml);
            var dto = (AppSettingsDTO)serializer.Deserialize(sr);

            Assert.NotNull(dto);
            Assert.Equal(760, dto.ProfileEditorLeftWidth);
            Assert.Equal(600, dto.ProfileEditorRightWidth);
            Assert.Equal(310, dto.SpecialActionNameColWidth);
            Assert.Equal(150, dto.SpecialActionTriggerColWidth);
            Assert.Equal(220, dto.SpecialActionDetailColWidth);
            // LastChecked should have been parsed
            Assert.NotEqual(default(DateTime), dto.LastChecked);
        }

        [Fact]
        public void LastChecked_MultipleFormats_Parses()
        {
            string[] samples = new[] {
                "11/11/2025 13:14:15",
                "2025-11-11T13:14:15",
                "2025-11-11 13:14:15",
                "2025/11/11 13:14:15",
                DateTime.Now.ToString("o")
            };

            XmlSerializer serializer = new XmlSerializer(typeof(AppSettingsDTO), new XmlRootAttribute("Profile"));

            foreach (var sample in samples)
            {
                string xml = $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<Profile>\n  <LastChecked>{sample}</LastChecked>\n</Profile>";
                using StringReader sr = new StringReader(xml);
                var dto = (AppSettingsDTO)serializer.Deserialize(sr);
                Assert.NotNull(dto);
                Assert.NotEqual(default(DateTime), dto.LastChecked);
            }
        }
    }
}
