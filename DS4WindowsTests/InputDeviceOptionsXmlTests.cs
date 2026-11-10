using System;
using System.IO;
using System.Xml.Serialization;
using DS4Windows.Core.Model;
using DS4Windows.Core.Persistence.Dto;
using DS4Windows.Core.Settings;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// アプリ設定の <c>DeviceOptions</c> 節（機種の系統ごとの設定）の XML。節の名前・要素の順は従来の保存形式のまま、
    /// 系統の定義（<see cref="DeviceOptionsBase.Settings"/>）から読み書きされることを確かめる。
    /// </summary>
    public class InputDeviceOptionsXmlTests
    {
        public sealed class Holder
        {
            public InputDeviceOptions DeviceOptions { get; set; } = new InputDeviceOptions();
        }

        private static string Serialize(Holder holder)
        {
            using var writer = new StringWriter();
            new XmlSerializer(typeof(Holder)).Serialize(writer, holder, new XmlSerializerNamespaces(new[] { System.Xml.XmlQualifiedName.Empty }));
            return writer.ToString();
        }

        private static Holder Deserialize(string xml)
        {
            using var reader = new StringReader(xml);
            return (Holder)new XmlSerializer(typeof(Holder)).Deserialize(reader);
        }

        [Fact]
        public void Writes_EverySectionInFamilyOrder_WithLegacyElementNames()
        {
            string xml = Serialize(new Holder());
            string compact = System.Text.RegularExpressions.Regex.Replace(xml, @">\s+<", "><");
            Assert.Contains("<DeviceOptions><DS4SupportSettings><Enabled>True</Enabled></DS4SupportSettings>"
                + "<DualSenseSupportSettings><Enabled>False</Enabled></DualSenseSupportSettings>"
                + "<SwitchProSupportSettings><Enabled>False</Enabled></SwitchProSupportSettings></DeviceOptions>", compact);
        }

        [Fact]
        public void RoundTrips_EnabledFlags()
        {
            var holder = new Holder();
            holder.DeviceOptions.Options[InputDeviceFamily.DualSense].Enabled = true;
            holder.DeviceOptions.Options[InputDeviceFamily.DS4].Enabled = false;

            Holder loaded = Deserialize(Serialize(holder));

            Assert.True(loaded.DeviceOptions.Options[InputDeviceFamily.DualSense].Enabled);
            Assert.False(loaded.DeviceOptions.Options[InputDeviceFamily.DS4].Enabled);
        }

        [Fact]
        public void MissingSections_KeepDefaults_AndBadBooleanIsIgnored()
        {
            Holder loaded = Deserialize("<Holder><DeviceOptions><DualSenseSupportSettings><Enabled>maybe</Enabled></DualSenseSupportSettings></DeviceOptions></Holder>");
            Assert.True(loaded.DeviceOptions.Options[InputDeviceFamily.DS4].Enabled);
            Assert.False(loaded.DeviceOptions.Options[InputDeviceFamily.DualSense].Enabled);
        }

    }
}
