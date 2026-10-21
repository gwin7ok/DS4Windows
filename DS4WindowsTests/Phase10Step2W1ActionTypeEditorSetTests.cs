using System;
using System.Collections.Generic;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;
using DS4WinWPF.DS4Forms.ViewModels.SpecialActions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step2 W1（Phase10-Step2-Plan.md §3.1・§5）: 種別ごとの入力の一式（<see cref="ActionTypeEditorSet"/>）。
    /// 従来は、<c>AssignmentEditor</c> のコードビハインドの 3 つの <c>switch</c> が行っていた「種別 → 入力」の対応を、1 か所に集めた。
    /// (1) 中継の対応（偽の入力で）、(2) 実物の種別ごとの入力が、従来と同じ <c>ISpecialActionRepository.SaveAction</c> の引数で保存すること（10 種）。
    /// </summary>
    public class Phase10Step2W1ActionTypeEditorSetTests
    {
        private sealed class FakeEditor : IActionTypeEditor
        {
            public readonly List<string> Calls = new List<string>();
            public bool ValidResult = true;

            public void LoadAction(SpecialAction action) => Calls.Add("load:" + action.name);
            public void SaveAction(SpecialAction action, bool edit = false) => Calls.Add("save:" + action.name + ":" + edit);
            public bool IsValid(SpecialAction action) { Calls.Add("valid:" + action.name); return ValidResult; }
        }

        private static SpecialAction Named(string name, SpecialAction.ActionTypeId typeId)
            => new SpecialAction(name, "Cross", "Macro", "65") { typeID = typeId };

        private static readonly SpecialAction.ActionTypeId[] EditedTypes =
        {
            SpecialAction.ActionTypeId.Macro, SpecialAction.ActionTypeId.Program, SpecialAction.ActionTypeId.Profile,
            SpecialAction.ActionTypeId.Key, SpecialAction.ActionTypeId.DisconnectBT, SpecialAction.ActionTypeId.BatteryCheck,
            SpecialAction.ActionTypeId.GyroCalibrate,
        };

        private static (ActionTypeEditorSet set, Dictionary<SpecialAction.ActionTypeId, FakeEditor> fakes) FakeSet()
        {
            var fakes = new Dictionary<SpecialAction.ActionTypeId, FakeEditor>();
            var map = new Dictionary<SpecialAction.ActionTypeId, IActionTypeEditor>();
            foreach (SpecialAction.ActionTypeId id in EditedTypes)
            {
                var fake = new FakeEditor();
                fakes[id] = fake;
                map[id] = fake;
            }

            return (new ActionTypeEditorSet(map), fakes);
        }

        // ---- (1) 中継の対応 ----

        [Fact]
        public void Constructor_RejectsNull_AndLoadRejectsANullAction()
        {
            Assert.Throws<ArgumentNullException>(() => new ActionTypeEditorSet(null));
            Assert.Throws<ArgumentNullException>(() => FakeSet().set.Load(null));
        }

        [Fact]
        public void Load_Save_AndIsValid_GoToTheEditorOfTheType()
        {
            foreach (SpecialAction.ActionTypeId id in EditedTypes)
            {
                var (set, fakes) = FakeSet();
                SpecialAction action = Named("n", id);

                set.Load(action);
                Assert.True(set.IsValid(id, action));
                set.Save(id, action, edit: true);

                Assert.Equal(new[] { "load:n", "valid:n", "save:n:True" }, fakes[id].Calls);
                foreach (var other in fakes)
                {
                    if (other.Key != id) Assert.Empty(other.Value.Calls);
                }
            }
        }

        [Fact]
        public void Button_IsEditedByTheKeyEditor()
        {
            var (set, fakes) = FakeSet();
            SpecialAction action = Named("b", SpecialAction.ActionTypeId.Button);

            set.Load(action);
            set.Save(SpecialAction.ActionTypeId.Button, action, edit: false);

            Assert.Equal(new[] { "load:b", "save:b:False" }, fakes[SpecialAction.ActionTypeId.Key].Calls);
        }

        [Fact]
        public void ATypeWithoutAnEditor_LoadsAndSavesNothing_AndIsNotValid()
        {
            var (set, fakes) = FakeSet();
            SpecialAction none = Named("x", SpecialAction.ActionTypeId.None);

            set.Load(none);
            set.Save(SpecialAction.ActionTypeId.None, none, edit: false);

            Assert.False(set.IsValid(SpecialAction.ActionTypeId.None, none));   // 従来の switch の既定値（偽）
            Assert.All(fakes.Values, f => Assert.Empty(f.Calls));
        }

        [Fact]
        public void IsValid_ReturnsTheEditorsResult()
        {
            var (set, fakes) = FakeSet();
            fakes[SpecialAction.ActionTypeId.Program].ValidResult = false;

            Assert.False(set.IsValid(SpecialAction.ActionTypeId.Program, Named("p", SpecialAction.ActionTypeId.Program)));
            Assert.True(set.IsValid(SpecialAction.ActionTypeId.Macro, Named("m", SpecialAction.ActionTypeId.Macro)));
        }

        // ---- (2) 実物の入力の保存の引数（従来と同じ。ISpecialActionRepository.SaveAction へ）----

        private static SpecialAction Target(SpecialAction.ActionTypeId typeId)
            => new SpecialAction("N", "Cross", "Macro", "65") { typeID = typeId };

        [Fact]
        public void Macro_SavesMode1_WithTheStepsAndTheExtras()
        {
            var repo = new RecordingSpecialActionRepository();
            var editor = new MacroViewModel(repo);
            editor.Macro.AddRange(new[] { 350, 256 });
            editor.PlaybackOptions.UseScanCode = true;
            editor.RunTriggerRelease = true;
            var set = new ActionTypeEditorSet(new Dictionary<SpecialAction.ActionTypeId, IActionTypeEditor> { [SpecialAction.ActionTypeId.Macro] = editor });

            set.Save(SpecialAction.ActionTypeId.Macro, Target(SpecialAction.ActionTypeId.Macro), edit: true);

            Assert.Equal(1, repo.SaveCount);
            Assert.Equal(("N", "Cross", 1, "350/256", true), (repo.SavedName, repo.SavedControls, repo.SavedMode, repo.SavedDetails, repo.SavedEdit));
            Assert.Equal("Scan Code/RunOnRelease", repo.SavedExtras);
        }

        [Fact]
        public void Program_SavesMode2_WithThePathTheDelayAndTheArguments()
        {
            var repo = new RecordingSpecialActionRepository();
            var editor = new LaunchProgramViewModel(repo) { Filepath = "C:/tools/a.exe", Delay = 2, Arguments = "-x" };
            var set = new ActionTypeEditorSet(new Dictionary<SpecialAction.ActionTypeId, IActionTypeEditor> { [SpecialAction.ActionTypeId.Program] = editor });

            set.Save(SpecialAction.ActionTypeId.Program, Target(SpecialAction.ActionTypeId.Program), edit: false);

            Assert.Equal((2, "C:/tools/a.exe?2", false, 2.0, "-x"), (repo.SavedMode, repo.SavedDetails, repo.SavedEdit, repo.SavedDelay, repo.SavedExtras));
        }

        [Fact]
        public void Profile_SavesMode3_WithTheProfileNameAndTheAutomaticUntriggerMarker()
        {
            var repo = new RecordingSpecialActionRepository();
            var pathService = new PathService();
            var profiles = new DS4WinWPF.ProfileList(pathService);
            profiles.ProfileListCol.Add(new DS4WinWPF.ProfileEntity(pathService) { Name = "ProfileA" });
            var editor = new LoadProfileViewModel(profiles, repo) { ProfileIndex = 1, AutoUntrigger = true };
            var set = new ActionTypeEditorSet(new Dictionary<SpecialAction.ActionTypeId, IActionTypeEditor> { [SpecialAction.ActionTypeId.Profile] = editor });

            set.Save(SpecialAction.ActionTypeId.Profile, Target(SpecialAction.ActionTypeId.Profile), edit: false);

            Assert.Equal((3, "ProfileA"), (repo.SavedMode, repo.SavedDetails));
            Assert.Equal("AutomaticUntrigger", repo.SavedExtras);
        }

        [Fact]
        public void Profile_WithoutAProfile_SavesNothing()
        {
            var repo = new RecordingSpecialActionRepository();
            var editor = new LoadProfileViewModel(new DS4WinWPF.ProfileList(new PathService()), repo);
            var set = new ActionTypeEditorSet(new Dictionary<SpecialAction.ActionTypeId, IActionTypeEditor> { [SpecialAction.ActionTypeId.Profile] = editor });

            set.Save(SpecialAction.ActionTypeId.Profile, Target(SpecialAction.ActionTypeId.Profile), edit: false);

            Assert.Equal(0, repo.SaveCount);   // 従来どおり（プロファイルが未選択なら、保存しない）
        }

        [Fact]
        public void Key_SavesMode4_WithTheKeyTheScanCodeAndTheSwitchMode()
        {
            var repo = new RecordingSpecialActionRepository();
            var editor = new PressKeyViewModel(repo, TestRepositoryFactory.CreateProfileSettingsService(), TestRepositoryFactory.CreateOutputSlotService())
            {
                Value = 65,
                SwitchMode = "Toggle",
                UseScanCode = true,
            };
            var set = new ActionTypeEditorSet(new Dictionary<SpecialAction.ActionTypeId, IActionTypeEditor> { [SpecialAction.ActionTypeId.Key] = editor });

            set.Save(SpecialAction.ActionTypeId.Key, Target(SpecialAction.ActionTypeId.Key), edit: false);

            Assert.Equal((4, "65 Scan Code"), (repo.SavedMode, repo.SavedDetails));
            Assert.Equal("Toggle\n", repo.SavedExtras);   // SwitchMode と解除トリガー（なし）
        }

        [Fact]
        public void Button_ChosenInThePressKeyTab_SavesMode10_WithTheButtonNumber()
        {
            var repo = new RecordingSpecialActionRepository();
            var editor = new PressKeyViewModel(repo, TestRepositoryFactory.CreateProfileSettingsService(), TestRepositoryFactory.CreateOutputSlotService());
            var chosen = new DS4ControlSettings(DS4Controls.None) { actionType = DS4ControlSettings.ActionType.Button };
            chosen.action.actionBtn = X360Controls.A;
            editor.ReadSettings(chosen);
            var set = new ActionTypeEditorSet(new Dictionary<SpecialAction.ActionTypeId, IActionTypeEditor> { [SpecialAction.ActionTypeId.Key] = editor });

            set.Save(SpecialAction.ActionTypeId.Key, Target(SpecialAction.ActionTypeId.Key), edit: false);

            Assert.Equal((10, ((int)X360Controls.A).ToString()), (repo.SavedMode, repo.SavedDetails));
            Assert.Equal("Press\n", repo.SavedExtras);
        }

        [Theory]
        [InlineData(5, SpecialAction.ActionTypeId.DisconnectBT, 3.0, "3")]
        [InlineData(9, SpecialAction.ActionTypeId.GyroCalibrate, 1.0, "1")]
        public void TheHoldIntervalTypes_SaveTheirOwnMode_WithTheInterval(int mode, SpecialAction.ActionTypeId typeId, double interval, string expectedDetails)
        {
            var repo = new RecordingSpecialActionRepository();
            var editor = new SpecialActionViewModel(mode, repo) { HoldInterval = interval };
            var set = new ActionTypeEditorSet(new Dictionary<SpecialAction.ActionTypeId, IActionTypeEditor> { [typeId] = editor });

            set.Save(typeId, Target(typeId), edit: false);

            Assert.Equal((mode, expectedDetails, interval), (repo.SavedMode, repo.SavedDetails, repo.SavedDelay));
        }

        [Fact]
        public void BatteryCheck_SavesMode6_WithTheOptionsAndTheColors()
        {
            var repo = new RecordingSpecialActionRepository();
            var editor = new CheckBatteryViewModel(repo, new EnvironmentService()) { Delay = 2, Notification = true };
            var set = new ActionTypeEditorSet(new Dictionary<SpecialAction.ActionTypeId, IActionTypeEditor> { [SpecialAction.ActionTypeId.BatteryCheck] = editor });

            set.Save(SpecialAction.ActionTypeId.BatteryCheck, Target(SpecialAction.ActionTypeId.BatteryCheck), edit: false);

            // 既定の色: 空は赤（255,0,0）、満タンは緑（0,255,0）。ライトバーは既定でオン
            Assert.Equal((6, "2|True|True|255|0|0|0|255|0", 2.0), (repo.SavedMode, repo.SavedDetails, repo.SavedDelay));
        }

        [Fact]
        public void MultiAction_SavesMode7_WithTheThreeMacros()
        {
            var repo = new RecordingSpecialActionRepository();
            var editor = new MultiActButtonViewModel(repo);
            editor.TapMacro.Add(1);
            editor.HoldMacro.AddRange(new[] { 2, 3 });
            editor.DoubleTapMacro.Add(4);
            var set = new ActionTypeEditorSet(new Dictionary<SpecialAction.ActionTypeId, IActionTypeEditor> { [SpecialAction.ActionTypeId.MultiAction] = editor });

            set.Save(SpecialAction.ActionTypeId.MultiAction, Target(SpecialAction.ActionTypeId.MultiAction), edit: false);

            Assert.Equal((7, "1,2/3,4"), (repo.SavedMode, repo.SavedDetails));
        }
    }
}
