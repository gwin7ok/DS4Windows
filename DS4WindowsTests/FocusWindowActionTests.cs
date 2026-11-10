using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Actions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase12-Step2 2d（R-4）: <see cref="FocusWindowAction"/>（立ち上がりの 1 回。別スレッドで前面化。重ねない）と、
    /// <see cref="SpecialActionOutputFactory"/> への接続（SA／Controls の両方）、Controls で使える種別の表。
    /// </summary>
    public class FocusWindowActionTests
    {
        private sealed class RecordingFocuser : IWindowFocuser
        {
            public readonly List<(string Path, string Title)> Targets = new List<(string, string)>();
            public WindowFocusResult Result = WindowFocusResult.Focused;
            public Exception ToThrow;

            public readonly List<bool> CenterCursorFlags = new List<bool>();

            public WindowFocusResult Focus(WindowTargetMatcher target, bool centerCursor = false)
            {
                CenterCursorFlags.Add(centerCursor);
                Targets.Add((target.Path, target.Title));
                if (ToThrow != null) throw ToThrow;
                return Result;
            }
        }

        private sealed class NoMacroPlayer : IMacroPlayer
        {
            public bool IsPlaying(int deviceIndex) => false;
            public void Play(int deviceIndex, SpecialAction action, CancellationToken cancellationToken = default) { }
            public void Stop(int deviceIndex) { }
        }

        private sealed class NoLauncher : IProcessLauncher
        {
            public void Launch(string filePath) { }
            public void Launch(string fileName, string arguments, bool useShellExecute, bool hidden) { }
        }

        private sealed class NoSwitcher : IProfileSwitcher
        {
            public void SwitchProfile(int deviceIndex, SpecialAction action) { }
            public void RestoreProfile(int deviceIndex) { }
        }

        private static SpecialAction FocusSa(string path = "game.exe$", string title = "*Test", string name = "focus")
            => new SpecialAction(name, "Cross", "FocusWindow", path, 0, title);

        private static SpecialActionOutputFactory NewFactory(IWindowFocuser focuser)
            => TestOutputFactory.Create(TestHost.GetService<IProfileSettingsService>(), TestHost.GetService<IManagedActionManager>(), TestHost.GetService<IKeyButtonControllerRegistry>(),
                new NoMacroPlayer(), new NoLauncher(), new NoSwitcher(), windowFocuser: focuser);

        [Fact]
        public void Execute_FocusesTheTargetBuiltFromPathAndTitle()
        {
            var focuser = new RecordingFocuser();
            var action = new FocusWindowAction(FocusSa("^c:\\games", "Window$"), focuser, work => work());

            action.Execute(null);

            Assert.Equal(new[] { ("^c:\\games", "Window$") }, focuser.Targets);
        }

        [Fact]
        public void Execute_RunsOnAnotherThread_WithTheDefaultRunner()
        {
            var done = new ManualResetEventSlim(false);
            int callerThread = Environment.CurrentManagedThreadId;
            int workerThread = -1;
            var focuser = new ThreadRecordingFocuser(() => { workerThread = Environment.CurrentManagedThreadId; done.Set(); });
            var action = new FocusWindowAction(FocusSa(), focuser);

            action.Execute(null);

            Assert.True(done.Wait(TimeSpan.FromSeconds(5)));
            Assert.NotEqual(callerThread, workerThread);
        }

        private sealed class ThreadRecordingFocuser : IWindowFocuser
        {
            private readonly Action onFocus;
            public ThreadRecordingFocuser(Action onFocus) { this.onFocus = onFocus; }

            public WindowFocusResult Focus(WindowTargetMatcher target, bool centerCursor = false)
            {
                onFocus();
                return WindowFocusResult.Focused;
            }
        }

        [Fact]
        public void Execute_WhilePreviousFocusIsRunning_IsDropped_AndRunsAgainAfterItFinishes()
        {
            var focuser = new RecordingFocuser();
            var pending = new List<Action>();
            var action = new FocusWindowAction(FocusSa(), focuser, work => pending.Add(work));

            action.Execute(null);
            action.Execute(null);   // 前の前面化が終わっていない（作業がまだ走っていない）ので、捨てる
            Assert.Single(pending);

            pending[0]();           // 終わらせる
            Assert.Single(focuser.Targets);

            action.Execute(null);   // 終わったので、次は受け付ける
            Assert.Equal(2, pending.Count);
        }

        [Fact]
        public void Execute_WhenTheFocuserThrows_DoesNotPropagate_AndAllowsTheNextRun()
        {
            var focuser = new RecordingFocuser { ToThrow = new InvalidOperationException("boom") };
            int runs = 0;
            var action = new FocusWindowAction(FocusSa(), focuser, work => { runs++; work(); });

            action.Execute(null);
            action.Execute(null);

            Assert.Equal(2, runs);
            Assert.Equal(2, focuser.Targets.Count);
        }

        [Fact]
        public void Execute_WhenStartingTheBackgroundWorkFails_DoesNotPropagate_AndAllowsTheNextRun()
        {
            var focuser = new RecordingFocuser();
            bool fail = true;
            var action = new FocusWindowAction(FocusSa(), focuser, work =>
            {
                if (fail) throw new InvalidOperationException("no thread");
                work();
            });

            action.Execute(null);
            fail = false;
            action.Execute(null);

            Assert.Single(focuser.Targets);
        }

        [Fact]
        public void Id_IsTheActionName()
        {
            Assert.Equal("my focus", new FocusWindowAction(FocusSa(name: "my focus"), new RecordingFocuser()).Id);
        }

        [Fact]
        public void Factory_CreatesFocusWindowAction_ForSpecialAction_WithDefaultPolicy()
        {
            SpecialActionOutputFactory factory = NewFactory(new RecordingFocuser());

            SpecialActionOutputs outputs = factory.Create(0, FocusSa(), 0);

            Assert.NotNull(outputs);
            Assert.IsType<FocusWindowAction>(Assert.Single(outputs.Outputs));
            Assert.True(outputs.Policy.RespectsFreshPressGuard);
            Assert.False(outputs.Policy.BlocksWhileExecuting);
            Assert.True(outputs.Policy.StopOnRelease);
        }

        [Fact]
        public void Factory_WithoutAFocuser_CreatesNoBinding()
        {
            Assert.Null(NewFactory(null).Create(0, FocusSa(), 0));
        }

        [Fact]
        public void Factory_CreatesFocusWindowAction_ForControls_WithTheControlsPolicy()
        {
            var item = new ControlSpecialItem { Type = SpecialAction.ActionTypeId.FocusWindow, WindowPath = "game.exe$", WindowTitle = "*Test" };
            SpecialAction sa = SpecialActionItemMapper.ToSpecialAction("Synthetic_Special_0_1", new List<DS4Controls> { DS4Controls.Cross }, item);
            var definition = new AssignmentDefinition("Synthetic_Special_0_1", "Synthetic_Special_0_1", AssignmentOrigin.Controls,
                SpecialAction.ActionTypeId.FocusWindow, new List<DS4Controls> { DS4Controls.Cross }, 0, item);

            SpecialActionOutputs outputs = NewFactory(new RecordingFocuser()).Create(0, definition, sa, 0);

            Assert.IsType<FocusWindowAction>(Assert.Single(outputs.Outputs));
            Assert.True(outputs.Policy.ErasesOutputEveryFrame);
            Assert.False(outputs.Policy.SuppressesTriggerButtons);
        }

        [Fact]
        public void ControlsSpecialTypeTable_SupportsFocusWindow()
        {
            Assert.True(ControlsSpecialTypeTable.IsSupported(SpecialAction.ActionTypeId.FocusWindow));
        }

        [Theory]
        [InlineData("game.exe$", "", true)]
        [InlineData("", "*Notes", true)]
        [InlineData("game.exe$", "*Notes", true)]
        [InlineData("", "", false)]
        [InlineData("  ", " ", false)]
        public void Controls_FocusWindowWithoutAnyTarget_IsNotBound(string path, string title, bool expected)
        {
            var dcs = new DS4ControlSettings(DS4Controls.Cross)
            {
                actionType = DS4ControlSettings.ActionType.Special,
                special = new ControlSpecialItem { Type = SpecialAction.ActionTypeId.FocusWindow, WindowPath = path, WindowTitle = title },
            };

            Assert.Equal(expected, DS4Windows.Processing.Services.ControlBindingBuilder.TryInterpretSpecial(dcs, out _));
        }
    }
}
