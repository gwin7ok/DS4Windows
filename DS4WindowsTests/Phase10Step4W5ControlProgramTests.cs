using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Phase10-Step4 W5（Phase10-Step4-Plan.md §5）: 第 3 群（Launch Program）を、Controls で保存・実行・編集できるようにした縦通しの検査。
    /// 引数の欄の <c>$hidden</c>（SA の書式）の分離と合成、builder の合成 <see cref="SpecialAction"/>（details／extra）、起動処理に渡る最終的な引数が SA と同じになること、
    /// 編集対象の往復、保存と再読み込み。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step4W5ControlProgramTests
    {
        private const int Device = MappingCharacterizationScope.Device;

        private sealed class RecordingLauncher : IProcessLauncher
        {
            public readonly List<(string File, string Arguments, bool Shell, bool Hidden)> Calls = new List<(string, string, bool, bool)>();

            public void Launch(string filePath) => Calls.Add((filePath, string.Empty, true, false));

            public void Launch(string fileName, string arguments, bool useShellExecute, bool hidden)
                => Calls.Add((fileName, arguments, useShellExecute, hidden));
        }

        private sealed class NullContext : IOutputContext
        {
            public int Device => 0;
            public IFrameOutput Frame => null;
            public IVirtualKBM OutputHandler => null;
            public void RequestEndFrame() { }
        }

        private static ControlSpecialItem Program(string path = "C:\\tools\\app.exe", string args = "", bool hide = false, double seconds = 0)
            => new ControlSpecialItem
            {
                Type = SpecialAction.ActionTypeId.Program,
                ProgramPath = path,
                ProgramArguments = args,
                HideWindow = hide,
                HoldSeconds = seconds,
            };

        // ---- 引数の欄（$hidden）の分離と合成 ----

        [Theory]
        [InlineData("", "", false)]
        [InlineData(null, "", false)]
        [InlineData("-a -b", "-a -b", false)]
        [InlineData("$hidden", "", true)]
        [InlineData("$HIDDEN", "", true)]
        [InlineData("$hidden -a", " -a", true)]
        [InlineData("-a $hidden -b", "-a  -b", true)]
        [InlineData("-a $Hidden", "-a ", true)]
        [InlineData("$hidden $hidden", " $hidden", true)]   // SA の起動処理と同じく、取り除くのは 1 回だけ
        public void SplitArguments_RemovesTheKeywordOnce_LikeTheLaunchAction(string text, string expectedArguments, bool expectedHide)
        {
            ControlSettingsEditTarget.SplitArguments(text, out string arguments, out bool hide);

            Assert.Equal(expectedArguments, arguments);
            Assert.Equal(expectedHide, hide);
        }

        [Fact]
        public void ComposeArguments_PutsTheKeywordFirst_OnlyWhenHidden()
        {
            Assert.Equal("-a -b", ControlSettingsEditTarget.ComposeArguments("-a -b", false));
            Assert.Equal("$hidden-a -b", ControlSettingsEditTarget.ComposeArguments("-a -b", true));
            Assert.Equal("$hidden", ControlSettingsEditTarget.ComposeArguments(null, true));
            Assert.Equal(string.Empty, ControlSettingsEditTarget.ComposeArguments(null, false));
        }

        [Theory]
        [InlineData("")]
        [InlineData("-a -b")]
        [InlineData("$hidden")]
        [InlineData("$hidden -a")]
        [InlineData("-a $hidden -b")]
        public void SplitThenCompose_KeepsTheEffectiveArguments(string text)
        {
            ControlSettingsEditTarget.SplitArguments(text, out string arguments, out bool hide);
            string recomposed = ControlSettingsEditTarget.ComposeArguments(arguments, hide);
            ControlSettingsEditTarget.SplitArguments(recomposed, out string againArguments, out bool againHide);

            // 語の位置は先頭に移るが、起動時の引数（語を除いたもの）と、隠すかどうかは、変わらない
            Assert.Equal(arguments, againArguments);
            Assert.Equal(hide, againHide);
        }

        // ---- builder ----

        [Fact]
        public void TheBuilder_MakesTheSyntheticAction_WithThePathTheArgumentsAndTheHoldSeconds()
        {
            using var t = new ControlsKeyTestScope();
            t.MapSpecial(DS4Controls.Cross, Program("C:\\tools\\app.exe", "-a -b", hide: false, seconds: 1.5));

            IActionBinding b = Assert.Single(TestHost.GetService<IControlBindingBuilder>().Build(Device));

            Assert.Equal(SpecialAction.ActionTypeId.Program, b.Source.typeID);
            Assert.Equal("C:\\tools\\app.exe", b.Source.details);
            Assert.Equal("-a -b", b.Source.extra);
            Assert.Equal(1.5, b.Source.delayTime);
            Assert.IsType<LaunchProcessAction>(Assert.Single(b.Outputs));
            var input = Assert.IsType<ControlButtonInputAction>(b.Input);
            Assert.Equal(ControlBindingKind.Special, input.Kind);
            Assert.True(b.Policy.BlocksWhileExecuting);                 // SA の Program と同じ（実行中は重ねて実行しない）
            Assert.True(b.Policy.RespectsFreshPressGuard);
            Assert.False(b.Policy.SuppressesTriggerButtons);
            Assert.False(b.Policy.ResetsTriggerOutput);
            Assert.True(b.Policy.ErasesOutputEveryFrame);
        }

        [Fact]
        public void WhenHidden_TheExtraStartsWithTheKeyword_AndIsOtherwiseTheArguments()
        {
            using var t = new ControlsKeyTestScope();
            t.MapSpecial(DS4Controls.Cross, Program(args: " -a -b", hide: true));

            IActionBinding b = Assert.Single(TestHost.GetService<IControlBindingBuilder>().Build(Device));

            Assert.Equal("$hidden -a -b", b.Source.extra);
        }

        // 起動処理（SA と同じ LaunchProcessAction）が受け取る最終的な引数は、SA の extra から作る場合と同じになる
        [Theory]
        [InlineData("C:\\tools\\app.exe", "-a -b", false, "C:\\tools\\app.exe", "-a -b", false)]
        [InlineData("C:\\tools\\app.exe", " -a", true, "C:\\tools\\app.exe", " -a", true)]
        [InlineData("C:\\tools\\app.exe", "", true, "C:\\tools\\app.exe", "", true)]
        public void TheLaunchAction_ReceivesTheSameArguments_AsTheSaWithTheSameText(string path, string args, bool hide,
            string expectedFile, string expectedArguments, bool expectedHidden)
        {
            // Controls: 型のある項目から作った合成 SpecialAction
            using var t = new ControlsKeyTestScope();
            t.MapSpecial(DS4Controls.Cross, Program(path, args, hide));
            var launcher = new RecordingLauncher();
            var factory = NewFactory(launcher);
            IActionBinding b = Assert.Single(new ControlBindingBuilder(TestHost.GetService<IProfileSettingsService>(), null, null, factory).Build(Device));
            b.Outputs[0].Execute(new NullContext());

            // SA: 同じ文字列（隠すときは、利用者が引数の欄に書く形）から作る SpecialAction
            var saLauncher = new RecordingLauncher();
            string saExtra = ControlSettingsEditTarget.ComposeArguments(args, hide);
            var sa = new SpecialAction("sa", "Cross", "Program", path, 0, saExtra);
            new LaunchProcessAction(sa, saLauncher).Execute(new NullContext());

            var call = Assert.Single(launcher.Calls);
            var saCall = Assert.Single(saLauncher.Calls);
            Assert.Equal(expectedFile, call.File);
            Assert.Equal(expectedArguments, call.Arguments);
            Assert.Equal(expectedHidden, call.Hidden);
            Assert.Equal(saCall.File, call.File);
            Assert.Equal(saCall.Arguments, call.Arguments);
            Assert.Equal(saCall.Hidden, call.Hidden);
        }

        private static SpecialActionOutputFactory NewFactory(IProcessLauncher launcher)
            => new SpecialActionOutputFactory(TestHost.GetService<IProfileSettingsService>(), TestHost.GetService<IManagedActionManager>(),
                TestHost.GetService<IMacroPlayer>(), launcher, TestHost.GetService<IProfileSwitcher>(),
                TestHost.GetService<ISpecialActionRepository>(), TestHost.GetService<Func<IMapping>>());

        [Fact]
        public void ABatchFile_Hidden_IsRunThroughTheCommandInterpreter_LikeTheSa()
        {
            using var t = new ControlsKeyTestScope();
            t.MapSpecial(DS4Controls.Cross, Program("C:\\tools\\run.bat", "-x", hide: true));
            var launcher = new RecordingLauncher();
            IActionBinding b = Assert.Single(new ControlBindingBuilder(TestHost.GetService<IProfileSettingsService>(), null, null, NewFactory(launcher)).Build(Device));

            b.Outputs[0].Execute(new NullContext());

            var call = Assert.Single(launcher.Calls);
            Assert.True(call.Hidden);
            Assert.EndsWith("/C \"C:\\tools\\run.bat\" -x", call.Arguments);
        }

        [Fact]
        public void AnEmptyPath_StillMakesABinding_ButLaunchesNothing()
        {
            using var t = new ControlsKeyTestScope();
            t.MapSpecial(DS4Controls.Cross, Program(path: string.Empty));
            var launcher = new RecordingLauncher();
            IActionBinding b = Assert.Single(new ControlBindingBuilder(TestHost.GetService<IProfileSettingsService>(), null, null, NewFactory(launcher)).Build(Device));

            b.Outputs[0].Execute(new NullContext());

            Assert.Empty(launcher.Calls);   // SA と同じ: パスが空なら、何もしない
        }

        // ---- 編集対象（画面との往復） ----

        private sealed class Rig
        {
            public readonly ProfileSettingsService Profile = TestRepositoryFactory.CreateProfileSettingsService();
            public readonly RecordingSpecialActionRepository Repo = new RecordingSpecialActionRepository();
            public readonly PressKeyViewModel PressKey;
            public readonly MacroViewModel Macro;
            public readonly LaunchProgramViewModel Launch;

            public Rig()
            {
                Profile.OutputKBMMapping = new SendInputMapping();
                PressKey = new PressKeyViewModel(Repo, Profile, TestRepositoryFactory.CreateOutputSlotService());
                Macro = new MacroViewModel(Repo);
                Launch = new LaunchProgramViewModel(Repo);
            }
        }

        private const int ProgramIndex = 2;

        [Fact]
        public void WriteFrom_WritesThePathTheSecondsAndTheSplitArguments()
        {
            var rig = new Rig();
            var target = new ControlSettingsEditTarget(new DS4ControlSettings(DS4Controls.Cross), rig.Profile);
            rig.Launch.Filepath = "C:\\tools\\app.exe";
            rig.Launch.Delay = 2.5;
            rig.Launch.Arguments = "-a $hidden -b";

            target.WriteFrom(ProgramIndex, rig.PressKey, rig.Macro, null, rig.Launch);

            Assert.Equal(DS4ControlSettings.ActionType.Special, target.Working.actionType);
            ControlSpecialItem item = target.Working.special;
            Assert.Equal(SpecialAction.ActionTypeId.Program, item.Type);
            Assert.Equal("C:\\tools\\app.exe", item.ProgramPath);
            Assert.Equal(2.5, item.HoldSeconds);
            Assert.Equal("-a  -b", item.ProgramArguments);
            Assert.True(item.HideWindow);
        }

        [Fact]
        public void WriteFrom_WithoutAFileChosen_WritesAnEmptyPath_WithoutThrowing()
        {
            var rig = new Rig();   // Filepath は未選択（null）
            var target = new ControlSettingsEditTarget(new DS4ControlSettings(DS4Controls.Cross), rig.Profile);

            target.WriteFrom(ProgramIndex, rig.PressKey, rig.Macro, null, rig.Launch);
            Assert.Equal(string.Empty, target.Working.special.ProgramPath);

            target.WriteFrom(ProgramIndex, rig.PressKey, rig.Macro);   // 入力そのものがないとき
            Assert.Equal(string.Empty, target.Working.special.ProgramPath);
            Assert.Equal(0, target.Working.special.HoldSeconds);
        }

        [Fact]
        public void LoadInto_FillsTheLaunchInput_AndTheInitialTypeIsProgram()
        {
            var rig = new Rig();
            var original = new DS4ControlSettings(DS4Controls.Cross) { actionType = DS4ControlSettings.ActionType.Special, special = Program("C:\\tools\\app.exe", "-a", hide: true, seconds: 3) };
            var target = new ControlSettingsEditTarget(original, rig.Profile);

            target.LoadInto(rig.PressKey, rig.Macro, null, rig.Launch);

            Assert.Equal(ProgramIndex, target.InitialTypeIndex);
            Assert.Equal("C:\\tools\\app.exe", rig.Launch.Filepath);
            Assert.Equal(3, rig.Launch.Delay);
            Assert.Equal("$hidden-a", rig.Launch.Arguments);
        }

        [Fact]
        public void WritingAnotherType_AfterAProgram_DropsTheItem()
        {
            var rig = new Rig();
            var original = new DS4ControlSettings(DS4Controls.Cross) { actionType = DS4ControlSettings.ActionType.Special, special = Program() };
            var target = new ControlSettingsEditTarget(original, rig.Profile);

            target.WriteFrom(10, rig.PressKey, rig.Macro, null, rig.Launch);   // Default

            Assert.Null(target.Working.special);
            Assert.Equal(DS4ControlSettings.ActionType.Default, target.Working.actionType);
        }

        // ---- 保存と再読み込み ----

        [Fact]
        public void AProgramAssignment_SurvivesSavingAndReloadingTheProfile_ThroughTheEditTarget()
        {
            using var ctx = new PersistenceGoldenContext();
            var rig = new Rig();
            var original = new DS4ControlSettings(DS4Controls.Cross);
            var target = new ControlSettingsEditTarget(original, rig.Profile);
            rig.Launch.Filepath = "C:\\Program Files\\Tool\\tool.exe";
            rig.Launch.Delay = 0.75;
            rig.Launch.Arguments = "$hidden -a \"b c\"";
            target.WriteFrom(ProgramIndex, rig.PressKey, rig.Macro, null, rig.Launch);
            target.Apply();

            BackingStore store = PersistenceHarness.NewStore();
            DS4ControlSettings dcs = store.GetDS4CSetting(PersistenceHarness.Slot, DS4Controls.Cross);
            dcs.Reset();
            dcs.actionType = original.actionType;
            dcs.special = original.special;

            string xml = PersistenceHarness.SaveProfile(ctx, store, "S4W5Program");
            BackingStore loaded = PersistenceHarness.LoadProfile(ctx, xml, "S4W5ProgramLoad");

            ControlSpecialItem back = loaded.GetDS4CSetting(PersistenceHarness.Slot, DS4Controls.Cross).special;
            Assert.Equal(SpecialAction.ActionTypeId.Program, back.Type);
            Assert.Equal("C:\\Program Files\\Tool\\tool.exe", back.ProgramPath);
            Assert.Equal(0.75, back.HoldSeconds);
            Assert.Equal(" -a \"b c\"", back.ProgramArguments);
            Assert.True(back.HideWindow);
        }
    }
}
