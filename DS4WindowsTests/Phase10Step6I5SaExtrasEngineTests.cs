using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step6 I5 の修正（実機確認 4 で発見）: SA の Extras の束縛が、実際のエンジンの流れ（プロファイルの SA の一覧の順に <c>ProcessOwned</c>）で処理されること。
    /// 以前は、Extras の束縛（合成の SA）が SA の一覧になく、一度も処理されず、ライトバー・振動・感度が動かなかった（束縛を作るところしか検査していなかった）。
    /// 付随する束縛（<see cref="TriggerBinding.PrimarySource"/>）は、SA 本体の <c>ProcessOwned</c> が、一緒に処理する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step6I5SaExtrasEngineTests
    {
        private const int Device = 5;
        private const string Extras = "10,20,1,255,0,0,5,1,30";   // 強い振動 10、弱い振動 20、ライトバー（255,0,0）点滅 5、感度 30
        private static readonly DateTime T0 = new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

        private sealed class FakeProvider : IProfileActionProvider
        {
            private readonly List<SpecialAction> _actions = new List<SpecialAction>();
            public void Add(SpecialAction sa) => _actions.Add(sa);
            public IReadOnlyList<string> GetProfileActionNames(int deviceIndex) => _actions.Select(a => a.name).ToArray();
            public SpecialAction GetProfileAction(int deviceIndex, string actionName) => _actions.FirstOrDefault(a => a.name == actionName);
            public int GetProfileActionCount(int deviceIndex) => _actions.Count;
            public int GetProfileActionIndexOf(int deviceIndex, string actionName) => _actions.FindIndex(a => a.name == actionName);
            public IReadOnlyList<string> GetProfileActionsRaw(int deviceIndex) => _actions.Select(a => a.name).ToArray();
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

        private sealed class RecordingPort : IControlExtrasPort
        {
            public readonly List<string> Calls = new List<string>();
            public void SetRumble(int deviceIndex, byte heavy, byte light) => Calls.Add($"rumble {heavy},{light}");
            public void ForceLightbar(int deviceIndex, DS4Color color, byte flash) => Calls.Add($"lightbar {color.red},{color.green},{color.blue} flash {flash}");
            public void ClearForcedLightbar(int deviceIndex) => Calls.Add("lightbar clear");
            public void TrySetTemporarySensitivity(int deviceIndex, int sensitivity) => Calls.Add($"sens {sensitivity}");
            public void RestoreSensitivity(int deviceIndex) => Calls.Add("sens restore");
        }

        private sealed class FakeInput : ITriggerInput
        {
            public bool Held;
            public bool PreviousHeld;
            public DateTime Now = T0;
            public int Device => Phase10Step6I5SaExtrasEngineTests.Device;
            public bool IsActive(DS4Controls control) => control == DS4Controls.R1 && Held;
            public bool WasActive(DS4Controls control) => control == DS4Controls.R1 && PreviousHeld;
            public DelayClock Delay { get; } = new DelayClock();
            public DateTime UtcNow => Now;
            public long TickCount64 => (long)(Now - T0).TotalMilliseconds + 1000;
        }

        private sealed class NullFrame : IFrameOutput
        {
            public void ResetTriggerControl(DS4Controls control) { }
            public void PressX360(X360Controls control) { }
        }

        private sealed class Rig
        {
            public readonly RecordingPort Port = new RecordingPort();
            public readonly FakeInput Input = new FakeInput();
            public readonly SpecialAction Sa;
            private readonly TriggerDispatchEngine _engine;

            public Rig(string extras)
            {
                Sa = new SpecialAction("ex", "R1", "Program", "a.exe") { outputExtras = extras };
                var provider = new FakeProvider();
                provider.Add(Sa);
                var builder = new ActionBindingBuilder(provider, TestHost.GetService<IProfileSettingsService>(), TestHost.GetService<IManagedActionManager>(),
                    new NoMacroPlayer(), new NoLauncher(), new NoSwitcher(), extrasPort: Port);
                var registry = new ActionBindingRegistry();
                registry.Replace(Device, builder.Build(Device));
                _engine = new TriggerDispatchEngine(registry, TestHost.GetService<IManagedActionManager>(), null);
                Bindings = registry.GetBindings(Device);
            }

            public IReadOnlyList<IActionBinding> Bindings { get; }

            /// <summary>t=elapsedMs の 1 フレーム。SA の一覧の順に、SA 本体の ProcessOwned を呼ぶ（Mapping の SA ループと同じ）。</summary>
            public void Frame(int elapsedMs, bool held)
            {
                Input.PreviousHeld = Input.Held;
                Input.Held = held;
                Input.Now = T0.AddMilliseconds(elapsedMs);
                if (_engine.Owns(Device, Sa)) _engine.ProcessOwned(Device, Sa, Input, new NullFrame());
            }
        }

        [Fact]
        public void TheExtrasBinding_IsProcessedWithTheSa_ThroughProcessOwned_AndIsNotInTheProfileList()
        {
            var rig = new Rig(Extras);

            // 束縛は 2 つ（Extras、SA 本体）。Extras の束縛は、SA 本体に付随する
            Assert.Equal(2, rig.Bindings.Count);
            var extras = (TriggerBinding)rig.Bindings[0];
            Assert.Same(rig.Sa, ((TriggerBinding)rig.Bindings[1]).Source);
            Assert.Same(rig.Sa, extras.PrimarySource);
            Assert.NotSame(rig.Sa, extras.Source);   // 合成の SA（プロファイルの一覧にはない）
        }

        [Fact]
        public void PressingTheTrigger_AppliesTheExtras_WhileHeld_AndReleasingRestoresThem()
        {
            var rig = new Rig(Extras);

            rig.Frame(0, false);
            rig.Frame(100, false);   // Fresh-press ガード: 離して 80ms 続くまで、新規に成立とみなさない
            rig.Frame(200, true);
            Assert.Contains("rumble 10,20", rig.Port.Calls);
            Assert.Contains("lightbar 255,0,0 flash 5", rig.Port.Calls);
            Assert.Contains("sens 30", rig.Port.Calls);

            int appliedWhilePressed = rig.Port.Calls.Count;
            rig.Frame(260, true);   // 押している間は、毎フレーム適用が続く
            Assert.True(rig.Port.Calls.Count > appliedWhilePressed);

            rig.Frame(320, false);
            Assert.Contains("lightbar clear", rig.Port.Calls);
            Assert.Contains("sens restore", rig.Port.Calls);
            Assert.Equal("rumble 0,0", rig.Port.Calls.Last(c => c.StartsWith("rumble")));
        }

        [Fact]
        public void WithoutTheTrigger_NothingIsApplied()
        {
            var rig = new Rig(Extras);

            rig.Frame(0, false);
            rig.Frame(100, false);

            Assert.Empty(rig.Port.Calls);
        }

        [Fact]
        public void AnSaWithoutExtras_HasNoCompanionBinding_AndStillRuns()
        {
            var rig = new Rig(null);

            Assert.Single(rig.Bindings);
            rig.Frame(0, false);
            rig.Frame(100, false);
            rig.Frame(200, true);
            Assert.Empty(rig.Port.Calls);
        }
    }
}
