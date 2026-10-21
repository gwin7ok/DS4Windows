using System;
using System.IO;
using System.Xml;
using DS4Windows;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step6 I5a（Phase10-Step6-Plan.md §4）: SA に付ける Extras の保存・読み込み。<c>Actions.xml</c> の <c>OutputExtras</c> 要素（あるときだけ）に、Controls と同じ書式
    /// （カンマ区切りの 9 個）で保存する。旧ファイル（要素なし）は、Extras なしとして読む。SA の保存は SA を作り直すため、Extras は別の呼び出し（<c>SetActionExtras</c>）で設定する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step6I5aSaExtrasStorageTests
    {
        private const string Extras = "10,20,1,255,0,0,5,1,30";

        private sealed class Rig : IDisposable
        {
            public readonly PersistenceGoldenContext Context = new PersistenceGoldenContext();
            public readonly BackingStore Store = PersistenceHarness.NewStore();
            public readonly SpecialActionsXmlSerializer Xml;
            public readonly string Path;

            public Rig()
            {
                Path = System.IO.Path.Combine(Context.AppData, "Actions.xml");
                Store.m_Actions = Path;
                Xml = new SpecialActionsXmlSerializer(Store);
            }

            public void Dispose() => Context.Dispose();

            public XmlNode Node(string name)
            {
                var doc = new XmlDocument();
                doc.Load(Path);
                return doc.SelectSingleNode($"/Actions/Action[@Name='{name}']");
            }

            public BackingStore Reload()
            {
                BackingStore store = PersistenceHarness.NewStore();
                store.m_Actions = Path;
                Assert.True(new SpecialActionsXmlSerializer(store).LoadActions());
                return store;
            }
        }

        [Theory]
        [InlineData(1, "65/350/65")]    // Macro（Extras 要素は Macro の旗に使っているため、別の名前であること）
        [InlineData(4, "65")]           // Key
        [InlineData(5, "0")]            // DisconnectBT
        public void Extras_AreSavedInTheirOwnElement_AndReadBack_ForEveryTypeTried(int mode, string details)
        {
            using var rig = new Rig();
            rig.Xml.SaveActionNew("A", "Cross", mode, details, false);

            Assert.True(rig.Xml.SetActionExtras("A", Extras));

            Assert.Equal(Extras, rig.Node("A")["OutputExtras"].InnerText);
            Assert.Equal(Extras, rig.Reload().actions.Find(a => a.name == "A").outputExtras);
        }

        [Fact]
        public void WithoutExtras_NoElementIsWritten_AndTheMacroFlagsAreUntouched()
        {
            using var rig = new Rig();
            rig.Xml.SaveActionNew("M", "Cross", 1, "65", false, extras: "RunOnRelease");

            Assert.Null(rig.Node("M")["OutputExtras"]);
            Assert.Equal("RunOnRelease", rig.Node("M")["Extras"].InnerText);
            Assert.Null(rig.Reload().actions.Find(a => a.name == "M").outputExtras);
        }

        [Fact]
        public void MacroFlags_AndTheExtras_LiveSideBySide()
        {
            using var rig = new Rig();
            rig.Xml.SaveActionNew("M", "Cross", 1, "65", false, extras: "RunOnRelease");
            rig.Xml.SetActionExtras("M", Extras);

            SpecialAction loaded = rig.Reload().actions.Find(a => a.name == "M");
            Assert.True(loaded.pressRelease);
            Assert.Equal(Extras, loaded.outputExtras);
        }

        [Fact]
        public void ClearingTheExtras_RemovesTheElement()
        {
            using var rig = new Rig();
            rig.Xml.SaveActionNew("A", "Cross", 4, "65", false);
            rig.Xml.SetActionExtras("A", Extras);

            Assert.True(rig.Xml.SetActionExtras("A", "  "));

            Assert.Null(rig.Node("A")["OutputExtras"]);
            Assert.Null(rig.Reload().actions.Find(a => a.name == "A").outputExtras);
        }

        [Fact]
        public void SavingTheActionAgain_RebuildsIt_WithoutTheExtras_SoTheEditorSetsThemAfterwards()
        {
            using var rig = new Rig();
            rig.Xml.SaveActionNew("A", "Cross", 4, "65", false);
            rig.Xml.SetActionExtras("A", Extras);

            rig.Xml.SaveActionNew("A", "Cross", 4, "66", true);

            Assert.Null(rig.Store.actions.Find(a => a.name == "A").outputExtras);
        }

        [Fact]
        public void UnknownAction_IsNotFound()
        {
            using var rig = new Rig();
            rig.Xml.SaveActionNew("A", "Cross", 4, "65", false);

            Assert.False(rig.Xml.SetActionExtras("Nope", Extras));
        }

        [Fact]
        public void AnOldFile_WithoutTheElement_IsReadAsNoExtras()
        {
            using var rig = new Rig();
            File.WriteAllText(rig.Path, "<?xml version=\"1.0\" encoding=\"utf-8\"?><Actions><Action Name=\"Old\"><Trigger>Cross</Trigger><Type>Key</Type><Details>65</Details></Action></Actions>");

            SpecialAction loaded = rig.Reload().actions.Find(a => a.name == "Old");

            Assert.NotNull(loaded);
            Assert.Null(loaded.outputExtras);
        }
    }
}
