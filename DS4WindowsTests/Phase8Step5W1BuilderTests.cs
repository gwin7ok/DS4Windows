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
    /// Phase8-Step5 W1（Phase8-Step5-Plan.md §3.1・§4）: <see cref="ActionBindingBuilder"/> の単体テスト。
    /// 種別ごとの構成（入力・出力・方針）、順序、フィルタ（移行対象が空／一部）、未対応の種別を作らないこと。
    /// </summary>
    public class Phase8Step5W1BuilderTests
    {
        private sealed class FakeProvider : IProfileActionProvider
        {
            public readonly List<SpecialAction> Actions = new List<SpecialAction>();
            public readonly List<string> Names = new List<string>();

            public void Add(SpecialAction sa)
            {
                Actions.Add(sa);
                Names.Add(sa.name);
            }

            public IReadOnlyList<string> GetProfileActionNames(int deviceIndex) => Names.ToArray();
            public SpecialAction GetProfileAction(int deviceIndex, string actionName) => Actions.FirstOrDefault(a => a.name == actionName);
            public int GetProfileActionCount(int deviceIndex) => Names.Count;
            public int GetProfileActionIndexOf(int deviceIndex, string actionName) => 100 + Names.IndexOf(actionName);
            public IReadOnlyList<string> GetProfileActionsRaw(int deviceIndex) => Names;
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

        private static ActionBindingBuilder NewBuilder(FakeProvider provider, BindingMigrationOptions options)
            => new ActionBindingBuilder(provider, TestHost.GetService<IProfileSettingsService>(), TestHost.GetService<IManagedActionManager>(),
                new NoMacroPlayer(), new NoLauncher(), new NoSwitcher(), options);

        private static BindingMigrationOptions All()
        {
            var o = new BindingMigrationOptions();
            foreach (SpecialAction.ActionTypeId t in Enum.GetValues(typeof(SpecialAction.ActionTypeId))) o.MigratedTypes.Add(t);
            return o;
        }

        private static SpecialAction Button(string name, string triggers, string mode, double delay = 0)
            => new SpecialAction(name, triggers, "Button", ((int)X360Controls.A).ToString(), delay, mode + "\n");

        private static TriggerBinding Only(IReadOnlyList<IActionBinding> list)
        {
            Assert.Single(list);
            return Assert.IsType<TriggerBinding>(list[0]);
        }

        [Fact]
        public void NothingMigrated_ReturnsAnEmptyList_AndDoesNotReadTheProfile()
        {
            var provider = new FakeProvider();
            provider.Add(Button("b", "Cross", "Press"));
            var builder = NewBuilder(provider, new BindingMigrationOptions());

            IReadOnlyList<IActionBinding> result = builder.Build(0);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void ButtonPress_HasNoFreshPressGuard_AndToggleHasIt()
        {
            var provider = new FakeProvider();
            provider.Add(Button("press", "Cross/L1", "Press", delay: 1.5));
            provider.Add(Button("toggle", "Circle", "Toggle"));
            IReadOnlyList<IActionBinding> result = NewBuilder(provider, All()).Build(2);

            Assert.Equal(2, result.Count);
            var press = Assert.IsType<TriggerBinding>(result[0]);
            var toggle = Assert.IsType<TriggerBinding>(result[1]);

            Assert.False(press.Policy.RespectsFreshPressGuard);
            Assert.True(toggle.Policy.RespectsFreshPressGuard);

            var input = Assert.IsType<ComboTriggerInputAction>(press.Input);
            Assert.Equal(new[] { DS4Controls.Cross, DS4Controls.L1 }, input.Triggers.ToArray());
            Assert.Equal(1.5, input.DelaySeconds);
            Assert.False(input.InvertsOnRelease);
            Assert.IsType<ButtonOutputAction>(Assert.Single(press.Outputs));
            Assert.Same(provider.Actions[0], press.Source);
            Assert.Equal(100, press.Index);
            Assert.Equal(101, toggle.Index);
        }

        [Fact]
        public void Key_UsesTheKeyOutputAndTheDefaultPolicy()
        {
            var provider = new FakeProvider();
            provider.Add(new SpecialAction("key", "Square", "Key", "65", 0, "Press\n"));

            TriggerBinding b = Only(NewBuilder(provider, All()).Build(0));

            Assert.IsType<KeyOutputAction>(Assert.Single(b.Outputs));
            Assert.True(b.Policy.RespectsFreshPressGuard);
            Assert.False(b.Policy.BlocksWhileExecuting);
            Assert.Equal(UntriggerPolicy.None, b.Policy.Untrigger);
        }

        [Theory]
        [InlineData("Sync", false)]
        [InlineData("RunOnRelease", true)]
        public void Macro_RunOnRelease_BecomesInvertOnRelease(string extras, bool expectedInvert)
        {
            var provider = new FakeProvider();
            provider.Add(new SpecialAction("macro", "Triangle", "Macro", "65/66", 0, extras));

            TriggerBinding b = Only(NewBuilder(provider, All()).Build(1));

            Assert.IsType<MacroAction>(Assert.Single(b.Outputs));
            Assert.True(b.Policy.RespectsFreshPressGuard);
            Assert.Equal(expectedInvert, ((ComboTriggerInputAction)b.Input).InvertsOnRelease);
        }

        [Fact]
        public void Program_BlocksWhileExecuting()
        {
            var provider = new FakeProvider();
            provider.Add(new SpecialAction("prog", "Share", "Program", "notepad.exe", 0, ""));

            TriggerBinding b = Only(NewBuilder(provider, All()).Build(0));

            Assert.IsType<LaunchProcessAction>(Assert.Single(b.Outputs));
            Assert.True(b.Policy.BlocksWhileExecuting);
            Assert.True(b.Policy.RespectsFreshPressGuard);
        }

        [Fact]
        public void Profile_UntriggerPolicy_FollowsAutomaticOrTheUntriggerButtons()
        {
            var provider = new FakeProvider();
            provider.Add(new SpecialAction("none", "L1", "Profile", "ProfileA", 0, ""));
            provider.Add(new SpecialAction("auto", "L2", "Profile", "ProfileB", 0, "AutomaticUntrigger"));
            provider.Add(new SpecialAction("buttons", "R1", "Profile", "ProfileC", 0, "Cross/Circle"));

            IReadOnlyList<IActionBinding> result = NewBuilder(provider, All()).Build(0);

            // Phase8-Step7 7.5a: 復帰（AutomaticUntrigger／解除トリガー）を持つ Profile は、7.5b まで束縛を作らず、旧経路が処理する。
            // 切替のみの Profile だけが束縛になる（7.5b で、auto と buttons の束縛も作る形に戻し、方針［Automatic／Buttons］を検査する）
            var none = Assert.IsType<TriggerBinding>(Assert.Single(result));
            Assert.Equal("none", none.Source.name);
            Assert.Equal(UntriggerPolicy.None, none.Policy.Untrigger);
            Assert.IsType<ProfileSwitchAction>(Assert.Single(none.Outputs));
            Assert.True(none.Policy.BlocksWhileExecuting);
        }

        [Fact]
        public void OrderFollowsTheProfile_MissingNamesAreSkipped_AndUnsupportedTypesAreNotBuilt()
        {
            var provider = new FakeProvider();
            provider.Add(new SpecialAction("a", "Cross", "Key", "65", 0, "Press\n"));
            provider.Add(new SpecialAction("disconnect", "PS", "DisconnectBT", "1.0", 0, ""));
            provider.Names.Add("ghost");   // 名前はあるが、定義が見つからない
            provider.Add(Button("b", "Circle", "Press"));
            provider.Add(new SpecialAction("battery", "Options", "BatteryCheck", "1|2|3", 0, ""));

            IReadOnlyList<IActionBinding> result = NewBuilder(provider, All()).Build(0);

            Assert.Equal(new[] { "a", "b" }, result.Cast<TriggerBinding>().Select(x => x.Source.name).ToArray());
        }

        [Fact]
        public void OnlyTheMigratedTypesAreBuilt()
        {
            var provider = new FakeProvider();
            provider.Add(new SpecialAction("k", "Cross", "Key", "65", 0, "Press\n"));
            provider.Add(Button("b", "Circle", "Press"));
            provider.Add(new SpecialAction("p", "L1", "Program", "notepad.exe", 0, ""));
            var options = new BindingMigrationOptions();
            options.MigratedTypes.Add(SpecialAction.ActionTypeId.Button);

            IReadOnlyList<IActionBinding> result = NewBuilder(provider, options).Build(0);

            Assert.Equal("b", Only(result).Source.name);
        }

        [Fact]
        public void AnActionWithoutTriggerButtons_IsNotBuilt_AndTheResultIsImmutableAcrossBuilds()
        {
            var provider = new FakeProvider();
            var empty = new SpecialAction("empty", "Cross", "Key", "65", 0, "Press\n");
            empty.trigger.Clear();
            provider.Add(empty);
            provider.Add(Button("b", "Circle", "Press"));
            var builder = NewBuilder(provider, All());

            IReadOnlyList<IActionBinding> first = builder.Build(0);
            provider.Names.Clear();
            IReadOnlyList<IActionBinding> second = builder.Build(0);

            Assert.Equal("b", Only(first).Source.name);   // 以後の変更が、作成済みのリストに影響しない
            Assert.Empty(second);
        }

        [Fact]
        public void Constructor_RejectsNull()
        {
            var p = new FakeProvider();
            var s = TestHost.GetService<IProfileSettingsService>();
            var m = TestHost.GetService<IManagedActionManager>();
            var o = new BindingMigrationOptions();
            Assert.Throws<ArgumentNullException>(() => new ActionBindingBuilder(null, s, m, new NoMacroPlayer(), new NoLauncher(), new NoSwitcher(), o));
            Assert.Throws<ArgumentNullException>(() => new ActionBindingBuilder(p, null, m, new NoMacroPlayer(), new NoLauncher(), new NoSwitcher(), o));
            Assert.Throws<ArgumentNullException>(() => new ActionBindingBuilder(p, s, null, new NoMacroPlayer(), new NoLauncher(), new NoSwitcher(), o));
            Assert.Throws<ArgumentNullException>(() => new ActionBindingBuilder(p, s, m, null, new NoLauncher(), new NoSwitcher(), o));
            Assert.Throws<ArgumentNullException>(() => new ActionBindingBuilder(p, s, m, new NoMacroPlayer(), null, new NoSwitcher(), o));
            Assert.Throws<ArgumentNullException>(() => new ActionBindingBuilder(p, s, m, new NoMacroPlayer(), new NoLauncher(), null, o));
            Assert.Throws<ArgumentNullException>(() => new ActionBindingBuilder(p, s, m, new NoMacroPlayer(), new NoLauncher(), new NoSwitcher(), null));
        }
    }
}
