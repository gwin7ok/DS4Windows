using System;
using System.IO;
using System.Xml;
using DS4Windows;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step4b W2（Phase8-Step4b-Plan.md §3.2）: <c>LoadProfile</c> の分解の基盤（<see cref="ProfileLoadContext"/>、<see cref="IProfileLoadSection"/>）の検査。
    /// 節の中身は、W1 のゴールデン（<c>Phase8Step4bProfileGoldenGapTests</c> ほか）が担保する。
    /// </summary>
    public class Phase8Step4bW2LoadFoundationTests
    {
        private static string Repo()
        {
            string dir = AppContext.BaseDirectory;
            while (dir != null && !File.Exists(Path.Combine(dir, "DS4Windows", "DS4WinWPF.csproj")))
                dir = Path.GetDirectoryName(dir);
            Assert.NotNull(dir);
            return dir;
        }

        // 節の型は internal（Q3＝A）で、テストのアセンブリからは直接見えないため、リフレクションで検査する
        private static readonly Type ContextType = typeof(LegacyProfileXmlSerializer).Assembly.GetType("DS4Windows.Services.ProfileLoadContext", true);

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
            string root = Repo();
            string registration = File.ReadAllText(Path.Combine(root, "DS4Windows", "DI", "ServiceRegistration.cs"));
            Assert.DoesNotContain("LoadSection", registration);

            foreach (string file in Directory.GetFiles(Path.Combine(root, "DS4Windows", "DS4Control", "Services", "ProfileSections"), "Profile*LoadSection.cs"))
            {
                string text = File.ReadAllText(file);
                string name = Path.GetFileNameWithoutExtension(file);
                Assert.Contains("internal sealed class " + name + " : IProfileLoadSection", text);
            }
        }

        [Fact]
        public void TheSerializer_CallsTheSectionsInFileOrder()
        {
            string text = File.ReadAllText(Path.Combine(Repo(), "DS4Windows", "DS4Control", "Services", "LegacyProfileXmlSerializer.cs"));
            int general = text.IndexOf("RunSection(_generalSection", StringComparison.Ordinal);
            int extra = text.IndexOf("LoadLegacyExtraSettings(device, rootname);", StringComparison.Ordinal);
            int touchpad = text.IndexOf("RunSection(_touchpadBasicSection", StringComparison.Ordinal);
            Assert.True(general > 0 && general < extra && extra < touchpad, "節と追加設定の読込は、旧コードと同じ順序で呼ぶ");
        }
    }
}
