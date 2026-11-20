using System;
using System.IO;
using System.Linq;
using System.Xml;
using Xunit;
using DS4Windows.Core.Persistence;
using DS4Windows.Core.Persistence.ProfileSections;
using DS4Windows.Core.Settings;

namespace DS4WindowsTests
{
    /// <summary>
    /// <c>LoadProfile</c> の分解の基盤（<see cref="ProfileLoadContext"/>、<see cref="IProfileLoadSection"/>）の検査。
    /// 節の中身は、W1 のゴールデン（<c>ProfileGoldenGapTests</c> ほか）が担保する。
    /// </summary>
    public class ProfileLoadFoundationTests
    {
        // 節の型は internal（Q3＝A）で、テストのアセンブリからは直接見えないため、リフレクションで検査する
        private static readonly Type ContextType = typeof(LegacyProfileXmlSerializer).Assembly.GetType("DS4Windows.Core.Persistence.ProfileSections.ProfileLoadContext", true);

        [Fact]
        public void Context_StartsWithoutAMissingSetting_AndRejectsNullCollaborators()
        {
            BackingStore store = PersistenceHarness.NewStore();
            var doc = new XmlDocument();
            object context = Activator.CreateInstance(ContextType, store, doc, "DS4Windows", 3, null, null, null);

            Assert.False((bool)ContextType.GetProperty("MissingSetting").GetValue(context));
            Assert.Same(store, ContextType.GetProperty("Store").GetValue(context));
            Assert.Same(doc, ContextType.GetProperty("Xdoc").GetValue(context));
            Assert.Equal("DS4Windows", ContextType.GetProperty("RootName").GetValue(context));
            Assert.Equal(3, ContextType.GetProperty("Device").GetValue(context));

            foreach (object[] args in new[]
            {
                new object[] { null, doc, "DS4Windows", 0, null, null, null },
                new object[] { store, null, "DS4Windows", 0, null, null, null },
                new object[] { store, doc, null, 0, null, null, null },
            })
            {
                var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() => Activator.CreateInstance(ContextType, args));
                Assert.IsType<ArgumentNullException>(ex.InnerException);
            }
        }

        [Fact]
        public void Sections_AreInternalSealedAndNotRegisteredInTheContainer()
        {
            string root = SourceFileLocator.RepoRoot();
            // 登録は層ごとの partial ファイル（ServiceRegistration*.cs）に分かれている
            string registration = string.Join(System.Environment.NewLine, Directory.GetFiles(Path.Combine(root, "DS4Windows", "Composition"), "ServiceRegistration*.cs").Select(File.ReadAllText));
            Assert.DoesNotContain("LoadSection", registration);

            foreach (string file in Directory.GetFiles(Path.Combine(root, "DS4Windows", "Core", "Persistence", "ProfileSections"), "Profile*LoadSection.cs"))
            {
                string text = File.ReadAllText(file);
                string name = Path.GetFileNameWithoutExtension(file);
                Assert.Contains("internal sealed class " + name + " : IProfileLoadSection", text);
            }
        }

        [Fact]
        public void TheSerializer_CallsTheSectionsInFileOrder()
        {
            string text = File.ReadAllText(Path.Combine(SourceFileLocator.RepoRoot(), "DS4Windows", "Core", "Persistence", "LegacyProfileXmlSerializer.cs"));
            string[] order =
            {
                "_general", "LoadLegacyExtraSettings(device, rootname);", "_triggerStick", "_motion", "_outputCurve", "_outputController",
                "_actions", "_button",
            };

            // 節は、旧コードの要素の順序（ファイル順）で呼ぶ。追加設定の読込は、全般の節の直後
            int previous = -1;
            foreach (string item in order)
            {
                string call = item.EndsWith(";") ? item : item + ".Load(context);";
                int at = text.IndexOf(call, StringComparison.Ordinal);
                Assert.True(at > previous, item + " は、前の呼び出しの後に呼ぶ");
                previous = at;
            }

            // 読込の最後に、節が書き戻した状態を受け取る
            Assert.True(text.IndexOf("missingSetting = context.MissingSetting;", previous, StringComparison.Ordinal) > previous);
        }

        [Fact]
        public void TheSerializer_CallsTheSaveSectionsInTheXmlOrder()
        {
            // W6: 保存の節は、保存の順序（XML の要素の並び。ゴールデンが固定している）で呼ぶ。追加設定の保存は、全般の節の直後
            string text = File.ReadAllText(Path.Combine(SourceFileLocator.RepoRoot(), "DS4Windows", "Core", "Persistence", "LegacyProfileXmlSerializer.cs"));
            string[] order =
            {
                "_general.Save(context);", "SaveLegacyExtraSettings(rootElement, device);", "_triggerStick.Save(context);",
                "_motion.Save(context);", "_actions.Save(context);", "_outputCurve.Save(context);",
                "_outputController.Save(context);", "_button.Save(context);", "m_Xdoc.AppendChild(rootElement);",
            };

            int previous = -1;
            foreach (string call in order)
            {
                int at = text.IndexOf(call, StringComparison.Ordinal);
                Assert.True(at > previous, call + " は、前の呼び出しの後に呼ぶ");
                previous = at;
            }
        }
    }
}
