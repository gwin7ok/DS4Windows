using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.DS4Control;
using DS4Windows.Services;
using DS4WinWPF.DS4Forms.ViewModels;
using DS4WinWPF.DS4Forms.ViewModels.SpecialActions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step4 W6（Phase10-Step4-Plan.md §5）: 第 4 群（Load Profile）を、Controls で保存・実行・編集できるようにした縦通しの検査。
    /// builder の合成 <see cref="SpecialAction"/>（切り替え先、離したら戻る）、実際の切り替えと復帰、編集対象の往復（見つからない名前を含む）、Save の可否、保存と再読み込み、
    /// プロファイルの名前変更への追従（全プロファイルの <c>&lt;Control&gt;&lt;Special&gt;</c> の書き換え）。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step4W6ControlProfileTests : IDisposable
    {
        private const int Device = MappingCharacterizationScope.Device;
        private const int ProfileIndex = 3;

        private readonly string _dir;
        private readonly string _profiles;

        public Phase10Step4W6ControlProfileTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "Phase10Step4_W6_" + Guid.NewGuid().ToString("N"));
            _profiles = Path.Combine(_dir, "Profiles");
            Directory.CreateDirectory(_profiles);
        }

        public void Dispose()
        {
            TestHost.GetService<IProfileSettingsService>().SetUseTempProfile(Device, false);
            try { Directory.Delete(_dir, true); } catch { }
        }

        private sealed class RecordingSwitcher : IProfileSwitcher
        {
            public readonly List<(int Device, string Profile, bool Temporary)> Switches = new List<(int, string, bool)>();
            public readonly List<int> Restores = new List<int>();

            // 本物の切り替え（ProfileApplicationService）と同じく、適用のたびに、アクションの状態を初期化し（押している最中の状態は引き継がない）、
            // 離したら戻る切り替えのときは、一時プロファイルを使用中にする（復帰の確認は、SA が有効か、一時プロファイル使用中のときだけ動く）
            public void SwitchProfile(int deviceIndex, SpecialAction action)
            {
                Switches.Add((deviceIndex, action.details, action.IsTemporaryProfileAction));
                ActionManager.ClearDeviceState(deviceIndex);
                TestHost.GetService<IProfileSettingsService>().SetUseTempProfile(deviceIndex, action.IsTemporaryProfileAction);
            }

            public void RestoreProfile(int deviceIndex) => Restores.Add(deviceIndex);
        }

        private sealed class NullContext : IOutputContext
        {
            public int Device => 0;
            public IFrameOutput Frame => null;
            public IVirtualKBM OutputHandler => null;
            public void RequestEndFrame() { }
        }

        private static ControlSpecialItem Profile(string name = "Other", bool auto = false)
            => new ControlSpecialItem { Type = SpecialAction.ActionTypeId.Profile, ProfileName = name, AutomaticUntrigger = auto };

        private static SpecialActionOutputFactory NewFactory(IProfileSwitcher switcher)
            => new SpecialActionOutputFactory(TestHost.GetService<IProfileSettingsService>(), TestHost.GetService<IManagedActionManager>(),
                TestHost.GetService<IMacroPlayer>(), TestHost.GetService<IProcessLauncher>(), switcher,
                TestHost.GetService<ISpecialActionRepository>(), TestHost.GetService<Func<IMapping>>());

        private static ControlBindingBuilder NewBuilder(IProfileSwitcher switcher)
            => new ControlBindingBuilder(TestHost.GetService<IProfileSettingsService>(), null, null, NewFactory(switcher));

        // ---- builder ----

        [Fact]
        public void TheBuilder_MakesTheSyntheticAction_WithTheTargetAndTheUntriggerSetting()
        {
            using var t = new ControlsKeyTestScope();
            t.MapSpecial(DS4Controls.Cross, Profile("Other", auto: false));
            t.MapSpecial(DS4Controls.Circle, Profile("Back", auto: true));

            IReadOnlyList<IActionBinding> bindings = NewBuilder(new RecordingSwitcher()).Build(Device);

            Assert.Equal(2, bindings.Count);

            IActionBinding keep = bindings.Single(b => b.Source.name.EndsWith("_" + (int)DS4Controls.Cross));
            Assert.Equal(SpecialAction.ActionTypeId.Profile, keep.Source.typeID);
            Assert.Equal("Other", keep.Source.details);
            Assert.False(keep.Source.automaticUntrigger);
            Assert.False(keep.Source.IsTemporaryProfileAction);
            Assert.Equal(new[] { DS4Controls.Cross }, keep.Source.trigger.ToArray());
            Assert.IsType<ProfileSwitchAction>(Assert.Single(keep.Outputs));
            Assert.True(keep.Policy.BlocksWhileExecuting);
            Assert.Equal(UntriggerPolicy.None, keep.Policy.Untrigger);
            Assert.True(keep.Policy.StopOnRelease);

            IActionBinding auto = bindings.Single(b => b.Source.name.EndsWith("_" + (int)DS4Controls.Circle));
            Assert.Equal("Back", auto.Source.details);
            Assert.True(auto.Source.automaticUntrigger);
            Assert.True(auto.Source.IsTemporaryProfileAction);
            Assert.Equal(UntriggerPolicy.Automatic, auto.Policy.Untrigger);
            Assert.False(auto.Policy.StopOnRelease);   // 復帰は ProcessUntrigger だけが担う（SA の Profile と同じ）

            foreach (IActionBinding b in bindings)
            {
                Assert.True(b.Policy.RespectsFreshPressGuard);
                Assert.False(b.Policy.SuppressesTriggerButtons);
                Assert.False(b.Policy.ResetsTriggerOutput);
                Assert.True(b.Policy.ErasesOutputEveryFrame);
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void ALoadProfileWithoutATarget_MakesNoBinding(string name)
        {
            using var t = new ControlsKeyTestScope();
            t.MapSpecial(DS4Controls.Cross, Profile(name));

            Assert.Empty(NewBuilder(new RecordingSwitcher()).Build(Device));

            var dcs = new DS4ControlSettings(DS4Controls.Cross) { actionType = DS4ControlSettings.ActionType.Special, special = Profile(name) };
            Assert.False(ControlBindingBuilder.TryInterpretSpecial(dcs, out _));
        }

        // ---- 実行 ----

        [Fact]
        public void TheOutput_SwitchesToTheTarget_ThroughTheSwitcher()
        {
            using var t = new ControlsKeyTestScope();
            t.MapSpecial(DS4Controls.Cross, Profile("Other", auto: true));
            var switcher = new RecordingSwitcher();
            IActionBinding b = Assert.Single(NewBuilder(switcher).Build(Device));

            b.Outputs[0].Execute(new NullContext());

            Assert.Equal((Device, "Other", true), Assert.Single(switcher.Switches));
        }

        private static DS4State Cross(bool held)
        {
            DS4State state = ControlsKeyTestScope.Neutral();
            state.Cross = held;
            return state;
        }

        private static DS4State RunFor(ControlsKeyTestScope t, DS4State input, int milliseconds)
        {
            DS4State last = null;
            var sw = Stopwatch.StartNew();
            do
            {
                last = t.Run(input);
                Thread.Sleep(10);
            }
            while (sw.ElapsedMilliseconds < milliseconds);

            return last;
        }

        [Fact]
        public void APress_SwitchesOnce_AndAnAutomaticOneRestoresOnRelease()
        {
            using var t = new ControlsKeyTestScope();
            t.MapSpecial(DS4Controls.Cross, Profile("Other", auto: true));
            var switcher = new RecordingSwitcher();
            t.PublishWith(NewBuilder(switcher));

            RunFor(t, Cross(false), 150);   // SA と同じ Fresh-press ガード
            Assert.Empty(switcher.Switches);

            DS4State held = RunFor(t, Cross(true), 300);
            Assert.Single(switcher.Switches);
            Assert.False(held.Cross);       // 割り当てたボタンの通常の出力は消える
            Assert.Empty(switcher.Restores);

            RunFor(t, Cross(true), 300);
            Assert.Single(switcher.Switches);   // 押し続けても、繰り返さない

            RunFor(t, Cross(false), 300);
            Assert.Equal(new[] { Device }, switcher.Restores.ToArray());   // 離すと、1 回だけ戻る
        }

        [Fact]
        public void WithoutTheAutomaticUntrigger_NothingIsRestoredOnRelease()
        {
            using var t = new ControlsKeyTestScope();
            t.MapSpecial(DS4Controls.Cross, Profile("Other", auto: false));
            var switcher = new RecordingSwitcher();
            t.PublishWith(NewBuilder(switcher));

            RunFor(t, Cross(false), 150);
            RunFor(t, Cross(true), 300);
            RunFor(t, Cross(false), 300);

            Assert.Single(switcher.Switches);
            Assert.Empty(switcher.Restores);
        }

        // ---- 編集対象（画面との往復） ----

        private sealed class Rig
        {
            public readonly ProfileSettingsService Profile = TestRepositoryFactory.CreateProfileSettingsService();
            public readonly RecordingSpecialActionRepository Repo = new RecordingSpecialActionRepository();
            public readonly PressKeyViewModel PressKey;
            public readonly MacroViewModel Macro;
            public readonly LoadProfileViewModel LoadProfile;
            public ControlSpecialEditors Editors => new ControlSpecialEditors { LoadProfile = LoadProfile };

            public Rig(params string[] profileNames)
            {
                Profile.OutputKBMMapping = new SendInputMapping();
                PressKey = new PressKeyViewModel(Repo, Profile, TestRepositoryFactory.CreateOutputSlotService());
                Macro = new MacroViewModel(Repo);
                var pathService = new PathService();
                var list = new DS4WinWPF.ProfileList(pathService);
                foreach (string name in profileNames)
                    list.ProfileListCol.Add(new DS4WinWPF.ProfileEntity(pathService) { Name = name });
                LoadProfile = new LoadProfileViewModel(list, Repo);
            }
        }

        private static DS4ControlSettings SpecialSettings(ControlSpecialItem item)
            => new DS4ControlSettings(DS4Controls.Cross) { actionType = DS4ControlSettings.ActionType.Special, special = item };

        [Fact]
        public void WriteFrom_WritesTheChosenProfile_AndTheUntriggerSetting()
        {
            var rig = new Rig("First", "Second", "Third");
            var target = new ControlSettingsEditTarget(new DS4ControlSettings(DS4Controls.Cross), rig.Profile);
            rig.LoadProfile.ProfileIndex = 2;   // Second
            rig.LoadProfile.AutoUntrigger = true;

            target.WriteFrom(ProfileIndex, rig.PressKey, rig.Macro, rig.Editors);

            Assert.Equal(DS4ControlSettings.ActionType.Special, target.Working.actionType);
            Assert.Equal(SpecialAction.ActionTypeId.Profile, target.Working.special.Type);
            Assert.Equal("Second", target.Working.special.ProfileName);
            Assert.True(target.Working.special.AutomaticUntrigger);
        }

        [Fact]
        public void LoadInto_SelectsTheProfileByName_IgnoringCase_AndSetsTheUntrigger()
        {
            var rig = new Rig("First", "Second", "Third");
            var target = new ControlSettingsEditTarget(SpecialSettings(Profile("THIRD", auto: true)), rig.Profile);

            target.LoadInto(rig.PressKey, rig.Macro, rig.Editors);

            Assert.Equal(ProfileIndex, target.InitialTypeIndex);
            Assert.Equal(3, rig.LoadProfile.ProfileIndex);
            Assert.True(rig.LoadProfile.AutoUntrigger);
        }

        [Fact]
        public void LoadInto_ANameThatIsNotInTheList_LeavesTheEditorUnselected()
        {
            var rig = new Rig("First", "Second");
            rig.LoadProfile.ProfileIndex = 1;   // 前の編集の残りがあっても、読み込みで上書きされる
            var target = new ControlSettingsEditTarget(SpecialSettings(Profile("Renamed or deleted")), rig.Profile);

            target.LoadInto(rig.PressKey, rig.Macro, rig.Editors);

            Assert.Equal(0, rig.LoadProfile.ProfileIndex);
        }

        // Phase10-Step4c W3: 以前ここにあった CanWrite（Load Profile の未選択で Save を止める暫定）は、種別ごとの入力の IsValid（偽を返す）に置き換えた。
        // 検証は Phase10Step4cW0ValidationTableTests が持つ

        [Fact]
        public void WriteFrom_WithoutAChosenProfile_WritesAnEmptyName()
        {
            var rig = new Rig("First");
            var target = new ControlSettingsEditTarget(new DS4ControlSettings(DS4Controls.Cross), rig.Profile);

            target.WriteFrom(ProfileIndex, rig.PressKey, rig.Macro, rig.Editors);
            Assert.Equal(string.Empty, target.Working.special.ProfileName);

            target.WriteFrom(ProfileIndex, rig.PressKey, rig.Macro);   // 入力がないとき
            Assert.Equal(string.Empty, target.Working.special.ProfileName);
            Assert.False(target.Working.special.AutomaticUntrigger);
        }

        // ---- 保存と再読み込み ----

        [Fact]
        public void ALoadProfileAssignment_SurvivesSavingAndReloadingTheProfile_ThroughTheEditTarget()
        {
            using var ctx = new PersistenceGoldenContext();
            var rig = new Rig("First", "Second");
            var original = new DS4ControlSettings(DS4Controls.Cross);
            var target = new ControlSettingsEditTarget(original, rig.Profile);
            rig.LoadProfile.ProfileIndex = 2;
            rig.LoadProfile.AutoUntrigger = true;
            target.WriteFrom(ProfileIndex, rig.PressKey, rig.Macro, rig.Editors);
            target.Apply();

            BackingStore store = PersistenceHarness.NewStore();
            DS4ControlSettings dcs = store.GetDS4CSetting(PersistenceHarness.Slot, DS4Controls.Cross);
            dcs.Reset();
            dcs.actionType = original.actionType;
            dcs.special = original.special;

            string xml = PersistenceHarness.SaveProfile(ctx, store, "S4W6Profile");
            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, xml, "S4W6ProfileLoad");

            ControlSpecialItem back = loaded.GetDS4CSetting(PersistenceHarness.Slot, DS4Controls.Cross).special;
            Assert.Equal(SpecialAction.ActionTypeId.Profile, back.Type);
            Assert.Equal("Second", back.ProfileName);
            Assert.True(back.AutomaticUntrigger);
        }

        // ---- 名前変更への追従 ----

        private ProfileRepository NewRepository()
            => TestRepositoryFactory.CreateProfileRepository(TestRepositoryFactory.CreateProfileSettingsService(),
                TestRepositoryFactory.CreateProfileXmlStore(), new PathService(_dir));

        private void WriteProfile(string name, string controlSection)
        {
            File.WriteAllText(Path.Combine(_profiles, name + ".xml"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><DS4Windows><Other>1</Other>" + controlSection + "</DS4Windows>");
        }

        private string ReadTargets(string name)
        {
            var doc = new XmlDocument();
            doc.Load(Path.Combine(_profiles, name + ".xml"));
            return string.Join("|", doc.SelectNodes("DS4Windows/Control/Special/*/ProfileName").Cast<XmlNode>().Select(n => n.InnerText));
        }

        private static string Special(string control, string type, string profileName)
            => $"<Control><Special><{control} Type=\"{type}\"><ProfileName>{profileName}</ProfileName></{control}></Special></Control>";

        [Fact]
        public void TheRename_RewritesTheTargetsInEveryProfile_ThatPointAtTheOldName()
        {
            WriteProfile("A", Special("Cross", "Profile", "Old"));
            WriteProfile("B", "<Control><Special>" +
                "<Cross Type=\"Profile\"><ProfileName>old</ProfileName></Cross>" +        // 大文字小文字は区別しない（SA と同じ）
                "<Circle Type=\"Profile\"><ProfileName>Keep</ProfileName></Circle>" +
                "<Square Type=\"Profile\"><ProfileName>Old</ProfileName></Square></Special></Control>");
            WriteProfile("C", Special("Cross", "Profile", "Old10"));                   // 部分一致では書き換えない
            WriteProfile("D", "<Control><Button><Cross>A</Cross></Button></Control>");   // Special なし
            WriteProfile("E", string.Empty);                                             // Control なし

            IReadOnlyList<string> changed = NewRepository().ReplaceControlProfileSwitchTarget("Old", "New");

            Assert.Equal(new[] { "A", "B" }, changed.OrderBy(x => x).ToArray());
            Assert.Equal("New", ReadTargets("A"));
            Assert.Equal("New|Keep|New", ReadTargets("B"));
            Assert.Equal("Old10", ReadTargets("C"));
            Assert.Equal(string.Empty, ReadTargets("D"));
            Assert.Equal(string.Empty, ReadTargets("E"));
        }

        [Fact]
        public void TheRename_OnlyTouchesProfileTypedEntries()
        {
            // Program の ProfileName（通常は書かれないが）や、別の要素は書き換えない
            WriteProfile("A", Special("Cross", "Program", "Old"));

            Assert.Empty(NewRepository().ReplaceControlProfileSwitchTarget("Old", "New"));
            Assert.Equal("Old", ReadTargets("A"));
        }

        [Fact]
        public void TheRename_IgnoresBlankOrUnchangedNames_LeavesFilesUntouched_AndSkipsBrokenFiles()
        {
            WriteProfile("A", Special("Cross", "Profile", "Old"));
            File.WriteAllText(Path.Combine(_profiles, "Broken.xml"), "<DS4Windows><Control>");   // 壊れた XML
            DateTime before = File.GetLastWriteTimeUtc(Path.Combine(_profiles, "A.xml"));
            ProfileRepository repo = NewRepository();

            Assert.Empty(repo.ReplaceControlProfileSwitchTarget("Old", "Old"));
            Assert.Empty(repo.ReplaceControlProfileSwitchTarget("", "New"));
            Assert.Empty(repo.ReplaceControlProfileSwitchTarget("Old", " "));
            Assert.Equal("Old", ReadTargets("A"));
            Assert.Equal(before, File.GetLastWriteTimeUtc(Path.Combine(_profiles, "A.xml")));

            // 壊れたファイルがあっても、ほかのファイルは書き換える
            Assert.Equal(new[] { "A" }, repo.ReplaceControlProfileSwitchTarget("Old", "New").ToArray());
            Assert.Equal("New", ReadTargets("A"));
        }

        [Fact]
        public void TheRename_WithoutAProfilesFolder_ReturnsNothing()
        {
            Directory.Delete(_profiles, true);

            Assert.Empty(NewRepository().ReplaceControlProfileSwitchTarget("Old", "New"));
        }

        [Fact]
        public void TheRename_IsCalledFromTheMainWindowRename_NextToTheSaTargetUpdate()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Forms/MainWindow.xaml.cs");
            if (path == null) return;

            string source = File.ReadAllText(path);
            int sa = source.IndexOf("profileRepo.RefreshProfileSwitchTargets(oldProfile, newProfile);");
            int controls = source.IndexOf("profileRepo.ReplaceControlProfileSwitchTarget(oldProfile, newProfile);");
            Assert.True(sa >= 0 && controls > sa, "名前変更の処理に、Controls の切り替え先の更新が、SA の更新の後にあること");
        }
    }
}
