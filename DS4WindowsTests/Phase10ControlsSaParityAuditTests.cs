using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10 完了後の点検（2026-10-22）: Controls と SA の両方で選べる割り当て先が、どちらの側でも、実行時の束縛（出力）になること（片側の実装漏れがないこと）。
    /// 出力ピッカーで選べる X360 の出力先（Toggle あり・なし）と、種別（Key、Macro、Program、Profile、Disconnect BT、Check Battery、Multi-action、較正 2 種）を総当たりで調べる。
    /// SA の出力先のうち、パッドのボタンは <c>PressX360</c> が DS4 の同じ添字へ書くため、Controls の写し（<c>reverseX360ButtonMapping</c>）と同じ出力先になることも調べる。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10ControlsSaParityAuditTests
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

        private static IReadOnlyList<IActionBinding> BuildSa(SpecialAction sa, int device)
        {
            var provider = new FakeProvider();
            provider.Add(sa);
            var builder = new ActionBindingBuilder(provider, TestHost.GetService<IProfileSettingsService>(), TestHost.GetService<IManagedActionManager>(),
                TestHost.GetService<IMacroPlayer>(), TestHost.GetService<IProcessLauncher>(), TestHost.GetService<IProfileSwitcher>(),
                TestHost.GetService<ISpecialActionRepository>(), TestHost.GetService<Func<IMapping>>(),
                TestHost.GetService<IGyroCalibrationPort>(), TestHost.GetService<IControllerDisconnectPort>(), TestHost.GetService<IBatteryCheckPort>(),
                TestHost.GetService<IWheelCalibrationPort>(), TestHost.GetService<IMacroStringPlayer>(), outputFactory: TestHost.GetService<ISpecialActionOutputFactory>(),
                extrasPort: TestHost.GetService<IControlExtrasPort>());
            return builder.Build(device);
        }

        public static IEnumerable<object[]> ButtonTargets()
        {
            foreach (X360Controls control in Enum.GetValues(typeof(X360Controls)))
            {
                if (control == X360Controls.None || control == X360Controls.Unbound) continue;
                yield return new object[] { control, false };
                yield return new object[] { control, true };
            }
        }

        [Theory]
        [MemberData(nameof(ButtonTargets))]
        public void EveryOutputTarget_ProducesAnOutputBinding_OnBothSides(X360Controls target, bool toggle)
        {
            using var t = new ControlsKeyTestScope();

            // Controls
            DS4ControlSettings dcs = TestGlobals.Store.GetDS4CSetting(t.Device, DS4Controls.Cross);
            dcs.Reset();
            dcs.actionType = DS4ControlSettings.ActionType.Button;
            dcs.action.actionBtn = target;
            dcs.keyType = toggle ? DS4KeyType.Toggle : DS4KeyType.None;
            IReadOnlyList<IActionBinding> controls = TestHost.GetService<IControlBindingBuilder>().Build(t.Device);
            dcs.Reset();

            // SA
            var sa = new SpecialAction("audit", "Cross", "Button", ((int)target).ToString(), 0, toggle ? "Toggle\n" : "Press\n");
            IReadOnlyList<IActionBinding> sas = BuildSa(sa, t.Device);

            Assert.True(controls.Count > 0, $"Controls に、出力先 {target}（Toggle={toggle}）の束縛がない");
            Assert.True(sas.Count > 0 && sas.Any(b => b.Outputs.Count > 0), $"SA に、出力先 {target}（Toggle={toggle}）の束縛がない");
        }

        [Theory]
        [InlineData(X360Controls.LB)]
        [InlineData(X360Controls.LS)]
        [InlineData(X360Controls.RB)]
        [InlineData(X360Controls.RS)]
        [InlineData(X360Controls.X)]
        [InlineData(X360Controls.Y)]
        [InlineData(X360Controls.B)]
        [InlineData(X360Controls.A)]
        [InlineData(X360Controls.DpadUp)]
        [InlineData(X360Controls.DpadRight)]
        [InlineData(X360Controls.DpadDown)]
        [InlineData(X360Controls.DpadLeft)]
        [InlineData(X360Controls.Guide)]
        [InlineData(X360Controls.Back)]   // Phase10 完了後の点検: SA が別のボタン（TouchLeft・TouchUpper）へ書く不具合だった。PressX360 が変換表を使うよう直した
        [InlineData(X360Controls.Start)]
        public void ThePadButtonTargetOfTheSa_IsTheSameDs4OutputAsTheControlsMapping(X360Controls target)
        {
            // SA の PressX360 は、Controls と同じ変換表（reverseX360ButtonMapping）の DS4 のボタンへ書く（以前は、番号をそのまま使い、Back・Start だけ別のボタンへ書いていた）
            var fields = new DS4StateFieldMapping();
            var frame = new FrameOutput();
            frame.Bind(fields, new SyntheticState());

            frame.PressX360(target);

            DS4Controls expected = X360ControlTables.reverseX360ButtonMapping[(int)target];
            Assert.True(fields.buttons[(int)expected], target + " は " + expected + " へ書く");
            Assert.Equal(1, fields.buttons.Count(b => b));
        }

        private static IEnumerable<SpecialAction> SaTypes()
        {
            yield return new SpecialAction("macro", "Cross", "Macro", "65/350/65");
            yield return new SpecialAction("program", "Cross", "Program", "a.exe", 1.0);
            yield return new SpecialAction("profile", "Cross", "Profile", "Other");
            yield return new SpecialAction("disconnect", "Cross", "DisconnectBT", "1", 1.0);
            yield return new SpecialAction("battery", "Cross", "BatteryCheck", "0|True|True|1|2|3|4|5|6");
            yield return new SpecialAction("multi", "Cross", "MultiAction", "1,2,3");
            yield return new SpecialAction("wheel", "Cross", "SASteeringWheelEmulationCalibrate", "", 1.0);
            yield return new SpecialAction("gyro", "Cross", "GyroCalibration", "1", 1.0);
        }

        [Fact]
        public void EverySaType_ProducesABinding_OnBothSides()
        {
            using var t = new ControlsKeyTestScope();
            var missing = new List<string>();

            foreach (SpecialAction sa in SaTypes())
            {
                IReadOnlyList<IActionBinding> sas = BuildSa(sa, t.Device);
                if (sas.Count == 0) missing.Add("SA: " + sa.type);

                ControlSpecialItem item = SpecialActionItemMapper.ToItem(sa);
                DS4ControlSettings dcs = TestGlobals.Store.GetDS4CSetting(t.Device, DS4Controls.Cross);
                dcs.Reset();
                if (sa.typeID == SpecialAction.ActionTypeId.Macro)
                {
                    dcs.actionType = DS4ControlSettings.ActionType.Macro;
                    dcs.action.actionMacro = new[] { 65, 350, 65 };
                    dcs.keyType = DS4KeyType.Macro;
                }
                else
                {
                    dcs.actionType = DS4ControlSettings.ActionType.Special;
                    item.ProfileName = sa.typeID == SpecialAction.ActionTypeId.Profile ? "Other" : item.ProfileName;
                    item.ProgramPath = sa.typeID == SpecialAction.ActionTypeId.Program ? "a.exe" : item.ProgramPath;
                    dcs.special = item;
                }

                IReadOnlyList<IActionBinding> controls = TestHost.GetService<IControlBindingBuilder>().Build(t.Device);
                dcs.Reset();
                if (controls.Count == 0) missing.Add("Controls: " + sa.type);
            }

            Assert.True(missing.Count == 0, "束縛を作れない種別（片側の実装漏れ）: " + string.Join(", ", missing));
        }
    }
}
