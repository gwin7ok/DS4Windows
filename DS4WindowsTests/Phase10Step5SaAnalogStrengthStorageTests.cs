using System;
using System.IO;
using System.Xml;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.DS4Control;
using DS4Windows.Services;
using DS4WinWPF.DS4Forms.ViewModels.SpecialActions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step5 S5a（Phase10-Step6 後の Step5。SA 側のアナログ出力）: アナログな出力の種類の規則（<see cref="AnalogOutputTargets"/>）、強さ（0〜100%）の保存・読み込み
    /// （<c>Actions.xml</c> の Button の <c>Strength</c> 要素。100 のときは書かない。古いファイルは 100）、割り当て編集画面の入力（<see cref="PressKeyViewModel"/>）。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step5SaAnalogStrengthStorageTests
    {
        [Theory]
        [InlineData(X360Controls.LXNeg, AnalogOutputKind.PadAxis)]
        [InlineData(X360Controls.LXPos, AnalogOutputKind.PadAxis)]
        [InlineData(X360Controls.RYPos, AnalogOutputKind.PadAxis)]
        [InlineData(X360Controls.LT, AnalogOutputKind.PadTrigger)]
        [InlineData(X360Controls.RT, AnalogOutputKind.PadTrigger)]
        [InlineData(X360Controls.MouseUp, AnalogOutputKind.MouseMove)]
        [InlineData(X360Controls.MouseRight, AnalogOutputKind.MouseMove)]
        [InlineData(X360Controls.A, AnalogOutputKind.None)]
        [InlineData(X360Controls.LB, AnalogOutputKind.None)]
        [InlineData(X360Controls.DpadUp, AnalogOutputKind.None)]
        [InlineData(X360Controls.LeftMouse, AnalogOutputKind.None)]
        [InlineData(X360Controls.WUP, AnalogOutputKind.None)]   // ホイールは、今のまま（強さなし。ユーザー決定）
        [InlineData(X360Controls.TouchpadClick, AnalogOutputKind.None)]
        [InlineData(X360Controls.Unbound, AnalogOutputKind.None)]
        [InlineData(X360Controls.None, AnalogOutputKind.None)]
        public void TheKind_IsDecidedByTheOutputTarget(X360Controls control, AnalogOutputKind expected)
        {
            Assert.Equal(expected, AnalogOutputTargets.KindOf(control));
        }

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

        [Fact]
        public void TheStrength_IsSavedOnlyWhenNot100_AndReadBack()
        {
            using var rig = new Rig();
            rig.Xml.SaveActionNew("A", "Cross", 10, ((int)X360Controls.LXPos).ToString(), false, extras: "Press\n");

            Assert.Null(rig.Node("A")["Strength"]);   // 既定の 100 は書かない
            Assert.Equal(100, rig.Reload().actions.Find(a => a.name == "A").analogStrength);

            Assert.True(rig.Xml.SetActionAnalogStrength("A", 40));

            Assert.Equal("40", rig.Node("A")["Strength"].InnerText);
            Assert.Equal(40, rig.Reload().actions.Find(a => a.name == "A").analogStrength);

            Assert.True(rig.Xml.SetActionAnalogStrength("A", 100));
            Assert.Null(rig.Node("A")["Strength"]);
        }

        [Fact]
        public void TheStrength_IsClampedTo0To100_OnSave_AndOnLoad()
        {
            using var rig = new Rig();
            rig.Xml.SaveActionNew("A", "Cross", 10, ((int)X360Controls.LT).ToString(), false, extras: "Press\n");
            rig.Xml.SetActionAnalogStrength("A", 250);
            Assert.Equal(100, rig.Store.actions.Find(a => a.name == "A").analogStrength);

            rig.Xml.SetActionAnalogStrength("A", -5);
            Assert.Equal(0, rig.Store.actions.Find(a => a.name == "A").analogStrength);

            File.WriteAllText(rig.Path, "<?xml version=\"1.0\" encoding=\"utf-8\"?><Actions><Action Name=\"Wild\"><Trigger>Cross</Trigger><Type>Button</Type><Details>2</Details><Strength>999</Strength></Action></Actions>");
            Assert.Equal(100, rig.Reload().actions.Find(a => a.name == "Wild").analogStrength);
        }

        [Fact]
        public void OnlyAButtonAction_HasAStrength()
        {
            using var rig = new Rig();
            rig.Xml.SaveActionNew("K", "Cross", 4, "65", false, extras: "Press\n");

            Assert.False(rig.Xml.SetActionAnalogStrength("K", 50));
            Assert.False(rig.Xml.SetActionAnalogStrength("Nope", 50));
            Assert.Null(rig.Node("K")["Strength"]);
        }

        [Fact]
        public void AnOldFile_WithoutTheElement_IsReadAs100()
        {
            using var rig = new Rig();
            File.WriteAllText(rig.Path, "<?xml version=\"1.0\" encoding=\"utf-8\"?><Actions><Action Name=\"Old\"><Trigger>Cross</Trigger><Type>Button</Type><Details>2</Details></Action></Actions>");

            Assert.Equal(100, rig.Reload().actions.Find(a => a.name == "Old").analogStrength);
        }

        [Fact]
        public void SavingTheActionAgain_RebuildsIt_At100_SoTheEditorSetsTheStrengthAfterwards()
        {
            using var rig = new Rig();
            rig.Xml.SaveActionNew("A", "Cross", 10, ((int)X360Controls.LXPos).ToString(), false, extras: "Press\n");
            rig.Xml.SetActionAnalogStrength("A", 30);

            rig.Xml.SaveActionNew("A", "Cross", 10, ((int)X360Controls.LXPos).ToString(), true, extras: "Press\n");

            Assert.Equal(100, rig.Store.actions.Find(a => a.name == "A").analogStrength);
        }

        private static PressKeyViewModel NewVm(RecordingSpecialActionRepository repo)
        {
            var profile = TestRepositoryFactory.CreateProfileSettingsService();
            profile.OutputKBMMapping = new SendInputMapping();
            return new PressKeyViewModel(repo, profile, TestRepositoryFactory.CreateOutputSlotService());
        }

        [Fact]
        public void TheInput_IsShownOnlyForAnAnalogOutput_AndOnlyInTheSaMode()
        {
            var vm = NewVm(new RecordingSpecialActionRepository());

            vm.LoadAction(new SpecialAction("a", "Cross", "Button", ((int)X360Controls.LXPos).ToString(), 0, "Press\n"));
            Assert.True(vm.IsAnalogOutput);
            Assert.Equal(System.Windows.Visibility.Visible, vm.ShowAnalogStrength);

            vm.LoadAction(new SpecialAction("a", "Cross", "Button", ((int)X360Controls.A).ToString(), 0, "Press\n"));
            Assert.False(vm.IsAnalogOutput);
            Assert.Equal(System.Windows.Visibility.Collapsed, vm.ShowAnalogStrength);

            vm.LoadAction(new SpecialAction("a", "Cross", "Key", "65", 0, "Press\n"));
            Assert.Equal(System.Windows.Visibility.Collapsed, vm.ShowAnalogStrength);

            vm.LoadAction(new SpecialAction("a", "Cross", "Button", ((int)X360Controls.MouseUp).ToString(), 0, "Press\n"));
            vm.StrengthApplies = false;   // Controls のモード
            Assert.Equal(System.Windows.Visibility.Collapsed, vm.ShowAnalogStrength);
        }

        [Fact]
        public void TheStrength_IsLoadedFromTheAction_AndSavedAfterTheActionOnlyWhenAnalogAndNot100()
        {
            var repo = new RecordingSpecialActionRepository();
            var vm = NewVm(repo);
            var sa = new SpecialAction("a", "Cross", "Button", ((int)X360Controls.RT).ToString(), 0, "Press\n") { analogStrength = 60 };

            vm.LoadAction(sa);
            Assert.Equal(60, vm.AnalogStrength);

            vm.SaveAction(sa, edit: true);
            Assert.Equal(new[] { ("a", 60) }, repo.StrengthSaves.ToArray());

            // 100 は書かない
            repo.StrengthSaves.Clear();
            vm.AnalogStrength = 100;
            vm.SaveAction(sa, edit: true);
            Assert.Empty(repo.StrengthSaves);

            // デジタルな出力先では、強さを書かない
            var digital = new SpecialAction("d", "Cross", "Button", ((int)X360Controls.A).ToString(), 0, "Press\n") { analogStrength = 60 };
            vm.LoadAction(digital);
            vm.SaveAction(digital, edit: true);
            Assert.Empty(repo.StrengthSaves);
        }

        [Fact]
        public void TheStrength_IsClampedTo0To100_InTheInput()
        {
            var vm = NewVm(new RecordingSpecialActionRepository());

            vm.AnalogStrength = 250;
            Assert.Equal(100, vm.AnalogStrength);
            vm.AnalogStrength = -3;
            Assert.Equal(0, vm.AnalogStrength);
        }
    }
}
