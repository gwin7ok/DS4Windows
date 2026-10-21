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
    /// Phase10-Step4 W0（Phase10-Step4-Plan.md §3.3・§5）: <see cref="ActionBindingBuilder"/> が、SA の種別ごとに作る束縛（入力の型、出力アクションの型、方針）の一覧表の特性化。
    /// Step4 の W2 で、種別ごとの出力アクションの作り方を、共通の部品へ取り出す。そのとき、SA の束縛が変わらないことを、この表で守る。
    /// 窓口（ポート）がないときに、束縛を作らない種別も固定する。製品コードは変えない。
    /// </summary>
    public class Phase10Step4W0SaBindingTableTests
    {
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

        private sealed class NoMacroStringPlayer : IMacroStringPlayer
        {
            public void PlayMacroString(int deviceIndex, string macro) { }
        }

        private static TriggerBinding Build(SpecialAction sa, bool withPorts)
        {
            var provider = new FakeProvider();
            provider.Add(sa);
            ActionBindingBuilder builder = withPorts
                ? new ActionBindingBuilder(provider, TestHost.GetService<IProfileSettingsService>(), TestHost.GetService<IManagedActionManager>(),
                    new NoMacroPlayer(), new NoLauncher(), new NoSwitcher(), gyroCalibration: new RecordingGyroCalibrationPort(),
                    disconnectPort: new RecordingControllerDisconnectPort(), batteryCheckPort: new RecordingBatteryCheckPort(),
                    wheelCalibrationPort: new RecordingWheelCalibrationPort(), macroStringPlayer: new NoMacroStringPlayer())
                : new ActionBindingBuilder(provider, TestHost.GetService<IProfileSettingsService>(), TestHost.GetService<IManagedActionManager>(),
                    new NoMacroPlayer(), new NoLauncher(), new NoSwitcher());

            IReadOnlyList<IActionBinding> result = builder.Build(0);
            if (result.Count == 0) return null;
            Assert.Single(result);
            return Assert.IsType<TriggerBinding>(result[0]);
        }

        private static void AssertDefaultPolicy(BindingPolicy p)
        {
            Assert.True(p.RespectsFreshPressGuard);
            Assert.False(p.BlocksWhileExecuting);
            Assert.Equal(UntriggerPolicy.None, p.Untrigger);
            Assert.True(p.SuppressesTriggerButtons);
            Assert.True(p.ResetsTriggerOutput);
            Assert.True(p.StopOnRelease);
            Assert.False(p.RoutesOutputsByGesture);
            Assert.False(p.ErasesOutputEveryFrame);
        }

        [Fact]
        public void Key_Macro_GyroCalibrate_DisconnectBT_BatteryCheck_Wheel_UseTheDefaultPolicy_WithAComboTriggerInput()
        {
            var cases = new (SpecialAction Sa, Type Output)[]
            {
                (new SpecialAction("key", "Cross", "Key", "65", 0, "Press\n"), typeof(KeyOutputAction)),
                (new SpecialAction("macro", "Cross", "Macro", "65/350/65"), typeof(MacroAction)),
                (new SpecialAction("gyro", "Cross", "GyroCalibration", ""), typeof(GyroCalibrateAction)),
                (new SpecialAction("disc", "Cross", "DisconnectBT", "", 0.5), typeof(DisconnectBTAction)),
                (new SpecialAction("batt", "Cross", "BatteryCheck", "1,2,3|4,5,6"), typeof(BatteryCheckAction)),
                (new SpecialAction("wheel", "Cross", "SASteeringWheelEmulationCalibrate", ""), typeof(SteeringWheelCalibrateAction)),
            };

            foreach ((SpecialAction sa, Type output) in cases)
            {
                TriggerBinding b = Build(sa, withPorts: true);

                Assert.NotNull(b);
                Assert.IsType<ComboTriggerInputAction>(b.Input);
                Assert.IsType(output, Assert.Single(b.Outputs));
                AssertDefaultPolicy(b.Policy);
                Assert.Same(sa, b.Source);
            }
        }

        [Fact]
        public void Program_BlocksWhileExecuting_ButIsOtherwiseTheDefault()
        {
            TriggerBinding b = Build(new SpecialAction("prog", "Cross", "Program", "C:\\x.exe"), withPorts: true);

            Assert.IsType<ComboTriggerInputAction>(b.Input);
            Assert.IsType<LaunchProcessAction>(Assert.Single(b.Outputs));
            Assert.True(b.Policy.BlocksWhileExecuting);
            Assert.True(b.Policy.RespectsFreshPressGuard);
            Assert.Equal(UntriggerPolicy.None, b.Policy.Untrigger);
            Assert.True(b.Policy.StopOnRelease);
            Assert.False(b.Policy.ErasesOutputEveryFrame);
        }

        [Fact]
        public void Profile_BlocksWhileExecuting_AndAutomaticUntriggerTurnsOffStopOnRelease()
        {
            TriggerBinding plain = Build(new SpecialAction("prof", "Cross", "Profile", "other"), withPorts: true);
            Assert.IsType<ProfileSwitchAction>(Assert.Single(plain.Outputs));
            Assert.True(plain.Policy.BlocksWhileExecuting);
            Assert.Equal(UntriggerPolicy.None, plain.Policy.Untrigger);
            Assert.True(plain.Policy.StopOnRelease);

            // 自動復帰（AutomaticUntrigger）のとき: 復帰を持つ Profile は、離したエッジの Stop を呼ばない（復帰は ProcessUntrigger だけが担う）
            TriggerBinding auto = Build(new SpecialAction("prof2", "Cross", "Profile", "other", 0, "AutomaticUntrigger"), withPorts: true);
            Assert.IsType<ProfileSwitchAction>(Assert.Single(auto.Outputs));
            Assert.True(auto.Policy.BlocksWhileExecuting);
            Assert.Equal(UntriggerPolicy.Automatic, auto.Policy.Untrigger);
            Assert.False(auto.Policy.StopOnRelease);
        }

        [Fact]
        public void MultiAction_HasThreeGestureOutputs_ARoutedPolicy_AndAMultiTapInput()
        {
            TriggerBinding b = Build(new SpecialAction("multi", "Cross", "MultiAction", "1,2,3", 0.3), withPorts: true);

            Assert.IsType<MultiTapInputAction>(b.Input);
            Assert.Equal(3, b.Outputs.Count);
            Assert.All(b.Outputs, o => Assert.IsType<MacroStringAction>(o));
            Assert.False(b.Policy.RespectsFreshPressGuard);
            Assert.False(b.Policy.StopOnRelease);
            Assert.True(b.Policy.RoutesOutputsByGesture);
            Assert.False(b.Policy.BlocksWhileExecuting);
        }

        [Fact]
        public void Button_PressHasNoFreshPressGuard_ToggleHasIt()
        {
            string a = ((int)X360Controls.A).ToString();
            TriggerBinding press = Build(new SpecialAction("bp", "Cross", "Button", a, 0, "Press\n"), withPorts: true);
            TriggerBinding toggle = Build(new SpecialAction("bt", "Cross", "Button", a, 0, "Toggle\n"), withPorts: true);

            Assert.IsType<ButtonOutputAction>(Assert.Single(press.Outputs));
            Assert.False(press.Policy.RespectsFreshPressGuard);
            Assert.True(toggle.Policy.RespectsFreshPressGuard);
        }

        [Theory]
        [InlineData("GyroCalibration", "")]
        [InlineData("DisconnectBT", "")]
        [InlineData("BatteryCheck", "1,2,3|4,5,6")]
        [InlineData("SASteeringWheelEmulationCalibrate", "")]
        [InlineData("MultiAction", "1,2,3")]
        public void WithoutThePort_TheTypeBuildsNoBinding(string type, string details)
        {
            Assert.Null(Build(new SpecialAction("nop", "Cross", type, details), withPorts: false));
        }

        [Fact]
        public void ASpecialActionWithoutTriggers_BuildsNoBinding()
        {
            var sa = new SpecialAction("nt", "Cross", "Key", "65", 0, "Press\n");
            sa.trigger.Clear();

            Assert.Null(Build(sa, withPorts: true));
        }
    }
}
