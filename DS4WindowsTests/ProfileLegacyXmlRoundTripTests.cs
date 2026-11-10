using System.IO;
using System.Reflection;
using System.Xml;
using Xunit;
using DS4Windows.Core.Settings;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase6-Step12（U-8 の是正）: 旧形式（3.9.9 互換）のプロファイル XML の保存・読込で欠けていた 10 要素
    /// （DebouncingMs、InverseRumbleMotors、UseDs3PitchRollSim、スティックのドリフト補正 4 件、DS4OutputTriggerMode、
    /// ジャイロマウス／ジャイロマウススティックのジッター補正）が、保存して読み込むと同じ値に戻ることを固定する。
    /// 実ファイルと Global の状態には触れず、独立した BackingStore と XmlDocument だけを使う。
    /// </summary>
    public class ProfileLegacyXmlRoundTripTests
    {
        private const int Device = 2;
        private const string Root = "DS4Windows";

        // Phase7-Step10c W4: 読込（LoadLegacyExtraSettings）は LegacyProfileXmlSerializer へ移した。作業用の XmlDocument はその m_Xdoc
        private static readonly FieldInfo SerializerXdocField =
            typeof(DS4Windows.Core.Persistence.LegacyProfileXmlSerializer).GetField("m_Xdoc", BindingFlags.Instance | BindingFlags.NonPublic);

        private static DS4Windows.Core.Persistence.LegacyProfileXmlSerializer NewSerializer(BackingStore store, XmlDocument doc)
        {
            var serializer = TestRepositoryFactory.CreateLegacyProfileXml(store);
            SerializerXdocField.SetValue(serializer, doc);
            return serializer;
        }

        // ResetProfile は、プロファイルの読込を担う型が呼ぶため公開にした（Phase7-Step10c W4）
        private static readonly MethodInfo ResetProfileMethod =
            typeof(BackingStore).GetMethod("ResetProfile", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        // Phase7-Step10c W5: 作業用の XmlDocument は、読み書きのどちらも LegacyProfileXmlSerializer の m_Xdoc（NewSerializer で設定）
        private static XmlDocument NewDocument(BackingStore store)
        {
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateElement(Root));
            return doc;
        }

        private static void ResetProfile(BackingStore store)
        {
            ResetProfileMethod.Invoke(store, new object[] { Device });
        }

        [Fact]
        public void SaveThenLoad_RestoresAllTenSettings()
        {
            var source = new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService());
            source.inverseRumbleMotors[Device] = true;
            source.leftStickDriftXAxis[Device] = -5;
            source.leftStickDriftYAxis[Device] = 7;
            source.rightStickDriftXAxis[Device] = 3;
            source.rightStickDriftYAxis[Device] = -9;
            source.outputDS4TriggerMode[Device] = DS4TriggerOutputMode.Buttons;
            source.gyroMouseInfo[Device].jitterCompensation = !GyroMouseInfo.JITTER_COMPENSATION_DEFAULT;
            source.gyroMStickInfo[Device].jitterCompensation = !GyroMouseStickInfo.JITTER_COMPENSATION_DEFAULT;

            XmlDocument doc = NewDocument(source);
            NewSerializer(source, doc).SaveLegacyExtraSettings(doc.DocumentElement, Device);

            var target = new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService());
            var targetXml = NewSerializer(target, doc);
            ResetProfile(target);
            targetXml.LoadLegacyExtraSettings(Device, Root);

            Assert.True(target.inverseRumbleMotors[Device]);
            Assert.Equal(-5, target.leftStickDriftXAxis[Device]);
            Assert.Equal(7, target.leftStickDriftYAxis[Device]);
            Assert.Equal(3, target.rightStickDriftXAxis[Device]);
            Assert.Equal(-9, target.rightStickDriftYAxis[Device]);
            Assert.Equal(DS4TriggerOutputMode.Buttons, target.outputDS4TriggerMode[Device]);
            Assert.Equal(!GyroMouseInfo.JITTER_COMPENSATION_DEFAULT, target.gyroMouseInfo[Device].jitterCompensation);
            Assert.Equal(!GyroMouseStickInfo.JITTER_COMPENSATION_DEFAULT, target.gyroMStickInfo[Device].jitterCompensation);

            // 要素名は ProfileDTO と同じ（旧版・DTO 経路との互換）
            foreach (string name in new[]
            {
                "InverseRumbleMotors", "LeftStickDriftXAxis", "LeftStickDriftYAxis",
                "RightStickDriftXAxis", "RightStickDriftYAxis", "DS4OutputTriggerMode", "GyroMouseJitterCompensation",
                "GyroMouseStickJitterCompensation",
            })
            {
                Assert.NotNull(doc.SelectSingleNode("/" + Root + "/" + name));
            }
        }

        [Fact]
        public void Load_WithoutElements_KeepsDefaultsAfterReset()
        {
            // 古いプロファイル（10 要素が無い）を読み込んでも、既定値になること（前のプロファイルの値を引き継がない）
            var store = new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService());
            store.inverseRumbleMotors[Device] = true;
            store.leftStickDriftXAxis[Device] = -5;
            store.rightStickDriftYAxis[Device] = 9;
            store.outputDS4TriggerMode[Device] = DS4TriggerOutputMode.Buttons;
            store.gyroMouseInfo[Device].jitterCompensation = !GyroMouseInfo.JITTER_COMPENSATION_DEFAULT;
            store.gyroMStickInfo[Device].jitterCompensation = !GyroMouseStickInfo.JITTER_COMPENSATION_DEFAULT;

            var xml = NewSerializer(store, NewDocument(store));
            ResetProfile(store);
            xml.LoadLegacyExtraSettings(Device, Root);

            Assert.False(store.inverseRumbleMotors[Device]);
            Assert.Equal(0, store.leftStickDriftXAxis[Device]);
            Assert.Equal(0, store.rightStickDriftYAxis[Device]);
            Assert.Equal(BackingStore.DEFAULT_DS4_TRIGGER_OUTPUT, store.outputDS4TriggerMode[Device]);
            Assert.Equal(GyroMouseInfo.JITTER_COMPENSATION_DEFAULT, store.gyroMouseInfo[Device].jitterCompensation);
            Assert.Equal(GyroMouseStickInfo.JITTER_COMPENSATION_DEFAULT, store.gyroMStickInfo[Device].jitterCompensation);
        }

        [Fact]
        public void Load_WithInvalidValues_IgnoresThem()
        {
            var store = new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService());
            XmlDocument doc = NewDocument(store);
            var xml = NewSerializer(store, doc);
            ResetProfile(store);

            void Add(string name, string value)
            {
                XmlElement element = doc.CreateElement(name);
                element.InnerText = value;
                doc.DocumentElement.AppendChild(element);
            }

            Add("LeftStickDriftXAxis", "9999");
            Add("DS4OutputTriggerMode", "NoSuchMode");
            Add("InverseRumbleMotors", "maybe");

            xml.LoadLegacyExtraSettings(Device, Root);

            Assert.Equal(0, store.leftStickDriftXAxis[Device]);
            Assert.Equal(BackingStore.DEFAULT_DS4_TRIGGER_OUTPUT, store.outputDS4TriggerMode[Device]);
            Assert.False(store.inverseRumbleMotors[Device]);
        }

        [Fact]
        public void Save_DoesNotWriteDebouncingMs_AndLoad_IgnoresAnOldDebouncingMsElement()
        {
            // Phase11 K-W6-10: デバウンス機能の廃止。保存はしない。古いプロファイルの <DebouncingMs> は、読み捨てる（エラーにしない）
            var store = new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService());
            XmlDocument doc = NewDocument(store);
            var xml = NewSerializer(store, doc);
            xml.SaveLegacyExtraSettings(doc.DocumentElement, Device);
            Assert.Null(doc.SelectSingleNode("/" + Root + "/DebouncingMs"));

            ResetProfile(store);
            XmlElement element = doc.CreateElement("DebouncingMs");
            element.InnerText = "5000";
            doc.DocumentElement.AppendChild(element);

            xml.LoadLegacyExtraSettings(Device, Root);
        }

        [Fact]
        public void SaveProfileAndLoadProfile_CallTheExtraSettingsHelpers()
        {
            // 往復テストは補助メソッド単体の確認。SaveProfile／LoadProfile から呼ばれていることは、ソース走査で固定する。
            // Phase7-Step10c W4: 読込側は LegacyProfileXmlSerializer へ移した（保存側は W5 まで BackingStore）。両方のソースを合わせて確認する
            string path = SourceFileLocator.Find("DS4Windows/Core/Settings/BackingStore.cs");
            string serializerPath = SourceFileLocator.Find("DS4Windows/Core/Persistence/LegacyProfileXmlSerializer.cs");
            if (path == null || serializerPath == null) return;

            string text = File.ReadAllText(path) + File.ReadAllText(serializerPath);
            Assert.Contains("SaveLegacyExtraSettings(rootElement, device);", text);
            Assert.Contains("LoadLegacyExtraSettings(device, rootname);", text);
        }
    }
}
