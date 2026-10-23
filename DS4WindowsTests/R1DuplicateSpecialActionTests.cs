using System;
using System.IO;
using System.Xml;
using DS4Windows;
using DS4Windows.DS4Control;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// R-1（Release-Backlog.md）: SA の複製。要素の複製（設定をすべて引き継ぐ）、新しい固定 ID、名前の重複の回避。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class R1DuplicateSpecialActionTests
    {
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

            public BackingStore Reload()
            {
                BackingStore store = PersistenceHarness.NewStore();
                store.m_Actions = Path;
                Assert.True(new SpecialActionsXmlSerializer(store).LoadActions());
                return store;
            }
        }

        [Fact]
        public void TheCopy_KeepsEverySetting_AndGetsANewIdAndName()
        {
            using var rig = new Rig();
            rig.Xml.SaveActionNew("A", "Cross/Circle", 10, ((int)X360Controls.LXPos).ToString(), false, extras: "Press\n");
            rig.Xml.SetActionAnalogStrength("A", 40);
            rig.Xml.SetActionExtras("A", "0,0,0,0,0,0,0,0,0");

            Assert.True(rig.Xml.DuplicateAction("A", "A (Copy)"));

            BackingStore reloaded = rig.Reload();
            SpecialAction original = reloaded.actions.Find(a => a.name == "A");
            SpecialAction copy = reloaded.actions.Find(a => a.name == "A (Copy)");
            Assert.NotNull(original);
            Assert.NotNull(copy);
            Assert.NotEqual(original.id, copy.id);
            Assert.True(Guid.TryParseExact(copy.id, "N", out _));
            Assert.Equal(original.typeID, copy.typeID);
            Assert.Equal(original.details, copy.details);
            Assert.Equal(original.controls, copy.controls);
            Assert.Equal(original.trigger, copy.trigger);
            Assert.Equal(original.analogStrength, copy.analogStrength);
            Assert.Equal(40, copy.analogStrength);
            Assert.Equal(original.outputExtras, copy.outputExtras);
            Assert.Equal(original.KeyButtonSwitchMode, copy.KeyButtonSwitchMode);
        }

        [Fact]
        public void TheCopy_IsInsertedRightAfterTheOriginal_AndTheOtherActionsAreUntouched()
        {
            using var rig = new Rig();
            rig.Xml.SaveActionNew("A", "Cross", 4, "65", false, extras: "Press\n");
            rig.Xml.SaveActionNew("B", "Circle", 4, "66", false, extras: "Press\n");

            Assert.True(rig.Xml.DuplicateAction("A", "A (Copy)"));

            BackingStore reloaded = rig.Reload();
            Assert.Equal(new[] { "A", "A (Copy)", "B" }, reloaded.actions.ConvertAll(a => a.name).ToArray());
            Assert.Equal(rig.Store.actions.Find(a => a.name == "B").id, reloaded.actions.Find(a => a.name == "B").id);
        }

        [Fact]
        public void Duplicating_ASourceThatDoesNotExist_OrANameThatIsInUse_DoesNothing()
        {
            using var rig = new Rig();
            rig.Xml.SaveActionNew("A", "Cross", 4, "65", false, extras: "Press\n");
            rig.Xml.SaveActionNew("B", "Circle", 4, "66", false, extras: "Press\n");

            Assert.False(rig.Xml.DuplicateAction("Nothing", "X"));
            Assert.False(rig.Xml.DuplicateAction("A", "b"));   // 大文字小文字を区別せず、使用中
            Assert.False(rig.Xml.DuplicateAction("A", ""));
            Assert.Equal(2, rig.Reload().actions.Count);
        }

        [Fact]
        public void AMacroSa_KeepsItsMacro_InTheCopy()
        {
            using var rig = new Rig();
            rig.Xml.SaveActionNew("M", "Cross", 1, "350/256/310/256", false);

            Assert.True(rig.Xml.DuplicateAction("M", "M (Copy)"));

            SpecialAction copy = rig.Reload().actions.Find(a => a.name == "M (Copy)");
            Assert.Equal(SpecialAction.ActionTypeId.Macro, copy.typeID);
            Assert.Equal(rig.Store.actions.Find(a => a.name == "M").macro, copy.macro);
        }

        [Theory]
        [InlineData("A", "", "A (Copy)")]
        [InlineData("A", "A (Copy)", "A (Copy 2)")]
        [InlineData("A", "A (Copy)|A (Copy 2)", "A (Copy 3)")]
        [InlineData("A (Copy)", "A|A (Copy)", "A (Copy 2)")]            // 「(Copy)」を重ねない
        [InlineData("A (Copy 2)", "A|A (Copy)|A (Copy 2)", "A (Copy 3)")]
        [InlineData("A", "a (copy)", "A (Copy 2)")]                    // 大文字小文字を区別しない
        public void TheCopyName_IsTheFirstUnusedOne(string baseName, string existing, string expected)
        {
            string[] names = existing.Length == 0 ? Array.Empty<string>() : existing.Split('|');
            Assert.Equal(expected, SpecialActionRepository.MakeCopyName(baseName, names));
        }
    }
}
