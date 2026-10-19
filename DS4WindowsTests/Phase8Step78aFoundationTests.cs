using System;
using System.IO;
using System.Linq;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.8a（Phase8-Step7-8a-Plan.md W1〜W4）: Key／Button の出力の値渡し化（解釈は変換層、出力は実行だけ）と、Controls タブの土台（空の配線）のテスト。
    /// 動作・ログは変えない（既存の 7.1・7.2 のテストが期待値を変えずに成功することが、不変の証拠）。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step78aFoundationTests
    {
        private const int Device = 6;

        private sealed class RecordingFrame : IFrameOutput
        {
            public int Presses;
            public X360Controls Last;
            public void ResetTriggerControl(DS4Controls control) { }
            public void PressX360(X360Controls control) { Presses++; Last = control; }
        }

        private sealed class Context : IOutputContext
        {
            public Context(IFrameOutput frame) { Frame = frame; }
            public int Device => Phase8Step78aFoundationTests.Device;
            public IVirtualKBM OutputHandler => null;
            public IFrameOutput Frame { get; }
            public void RequestEndFrame() { }
        }

        private static IManagedActionManager Manager()
        {
            DS4WinWPF.AppHost.CreateHost();
            return TestHost.GetService<IManagedActionManager>();
        }

        // ---- W1: Key の解釈 ----

        [Theory]
        [InlineData("65", DS4KeyType.None, 65, false, false)]
        [InlineData("65", DS4KeyType.ScanCode, 65, true, false)]
        [InlineData("65", DS4KeyType.Toggle, 65, false, true)]
        [InlineData("65", DS4KeyType.ScanCode | DS4KeyType.Toggle, 65, true, true)]
        [InlineData("not a number", DS4KeyType.None, 0, false, false)]
        [InlineData("", DS4KeyType.None, 0, false, false)]
        [InlineData("70000", DS4KeyType.None, 0, false, false)]   // ushort の範囲外は読めない
        public void InterpretKey_ReadsTheLogicalKey_AndTheKeyTypeFlags(string details, DS4KeyType keyType, int logical, bool scan, bool toggle)
        {
            var sa = new SpecialAction("p8s78a_key_" + Guid.NewGuid().ToString("N"), "Cross", "Key", details, 0, string.Empty);
            sa.keyType = keyType;

            KeyBindingValues values = ActionBindingBuilder.InterpretKey(sa);

            Assert.Equal((ushort)logical, values.LogicalKey);
            Assert.Equal(scan, values.ScanCode);
            Assert.Equal(toggle, values.Toggle);
        }

        [Fact]
        public void InterpretKey_WithNull_GivesTheEmptyValues()
        {
            KeyBindingValues values = ActionBindingBuilder.InterpretKey(null);

            Assert.Equal((ushort)0, values.LogicalKey);
            Assert.False(values.ScanCode);
            Assert.False(values.Toggle);
        }

        [Fact]
        public void KeyOutputAction_ValuesConstructor_ValidatesItsArguments_AndKeepsTheIdentity()
        {
            DS4WinWPF.AppHost.CreateHost();
            var settings = TestHost.GetService<IProfileSettingsService>();
            var sa = new SpecialAction("p8s78a_keyid_" + Guid.NewGuid().ToString("N"), "Cross", "Key", "65", 0, string.Empty);

            var output = new KeyOutputAction(sa, new KeyBindingValues(65, false, false), settings);

            Assert.Equal(sa.name, output.Id);
            Assert.Throws<ArgumentNullException>(() => new KeyOutputAction(null, new KeyBindingValues(65, false, false), settings));
            Assert.Throws<ArgumentNullException>(() => new KeyOutputAction(sa, new KeyBindingValues(65, false, false), null));
            // 互換のコンストラクターも、従来どおり引数を検査する
            Assert.Throws<ArgumentNullException>(() => new KeyOutputAction(null, settings));
            Assert.Throws<ArgumentNullException>(() => new KeyOutputAction(sa, null));
        }

        [Fact]
        public void KeyAction_ValuesConstructor_AcceptsTheValues_WithoutReadingTheSettings()
        {
            var sa = new SpecialAction("p8s78a_keyact_" + Guid.NewGuid().ToString("N"), "Cross", "Key", "not a number", 0, string.Empty);

            var action = new KeyAction(sa, -1, 65, true);   // details が読めなくても、渡された値を使う

            Assert.NotNull(action);
        }

        // ---- W2: Button の解釈 ----

        [Theory]
        [InlineData("18", "Press", true, 18, false)]
        [InlineData("18", "Toggle", true, 18, true)]
        [InlineData("0", "Press", true, 0, false)]
        [InlineData("not a number", "Press", false, 0, false)]
        [InlineData("not a number", "Toggle", false, 0, true)]
        [InlineData("", "Press", false, 0, false)]
        public void InterpretButton_ReadsTheControl_AndTheToggleMode(string details, string mode, bool has, int control, bool toggle)
        {
            var sa = new SpecialAction("p8s78a_btn_" + Guid.NewGuid().ToString("N"), "Cross", "Button", details, 0, mode + "\n");

            ButtonBindingValues values = ActionBindingBuilder.InterpretButton(sa);

            Assert.Equal(has, values.HasControl);
            if (has) Assert.Equal((X360Controls)control, values.Control);
            Assert.Equal(toggle, values.Toggle);
        }

        [Fact]
        public void InterpretButton_WithNull_GivesNoControl()
        {
            ButtonBindingValues values = ActionBindingBuilder.InterpretButton(null);

            Assert.False(values.HasControl);
            Assert.False(values.Toggle);
        }

        [Theory]
        [InlineData("Press")]
        [InlineData("Toggle")]
        public void ButtonOutputAction_ValuesConstructor_BehavesLikeTheCompatibleConstructor_OverARandomSequence(string mode)
        {
            IManagedActionManager manager = Manager();
            string suffix = Guid.NewGuid().ToString("N");
            SpecialAction oldSa = new SpecialAction("p8s78a_btn_old_" + mode + suffix, "Cross", "Button", "18", 0, mode + "\n");
            SpecialAction newSa = new SpecialAction("p8s78a_btn_new_" + mode + suffix, "Cross", "Button", "18", 0, mode + "\n");
            var oldAction = new ButtonOutputAction(oldSa, manager);
            var newAction = new ButtonOutputAction(newSa, ActionBindingBuilder.InterpretButton(newSa), manager);
            var oldFrame = new RecordingFrame();
            var newFrame = new RecordingFrame();

            ActionInstanceState oldState = manager.GetStateFor(oldSa, Device);
            ActionInstanceState newState = manager.GetStateFor(newSa, Device);
            oldState.IsToggledOn = false;
            newState.IsToggledOn = false;

            var random = new Random(78101);
            bool previous = false;
            for (int step = 0; step < 600; step++)
            {
                bool guard = random.Next(4) == 0;
                oldState.RequiresFreshPressAfterReset = guard;
                newState.RequiresFreshPressAfterReset = guard;
                bool triggered = random.Next(2) == 0 ? previous : random.Next(2) == 0;

                oldFrame.Presses = 0;
                newFrame.Presses = 0;
                oldAction.ApplyFrame(new Context(oldFrame), triggered, triggered && !previous);
                newAction.ApplyFrame(new Context(newFrame), triggered, triggered && !previous);

                Assert.Equal(oldFrame.Presses, newFrame.Presses);
                Assert.Equal(oldState.IsToggledOn, newState.IsToggledOn);
                if (oldFrame.Presses > 0) Assert.Equal(oldFrame.Last, newFrame.Last);
                previous = triggered;
            }
        }

        [Fact]
        public void ButtonOutputAction_ValuesConstructor_OutputsNothing_WhenTheControlIsNotInterpreted()
        {
            IManagedActionManager manager = Manager();
            var sa = new SpecialAction("p8s78a_btn_none_" + Guid.NewGuid().ToString("N"), "Cross", "Button", "x", 0, "Press\n");
            var recorder = new RecordingFrame();
            var action = new ButtonOutputAction(sa, new ButtonBindingValues(false, X360Controls.None, false), manager);

            action.ApplyFrame(new Context(recorder), true, true);

            Assert.Equal(0, recorder.Presses);
            Assert.Throws<ArgumentNullException>(() => new ButtonOutputAction(null, new ButtonBindingValues(true, X360Controls.A, false), manager));
            Assert.Throws<ArgumentNullException>(() => new ButtonOutputAction(sa, new ButtonBindingValues(true, X360Controls.A, false), null));
        }

        [Fact]
        public void TheOutputs_DoNotInterpretTheSettingsThemselves()
        {
            // 解釈は変換層（ビルダー）の責務。出力のソースに、details の読み取りが残っていないこと（互換のコンストラクターはビルダーの関数を呼ぶ）
            string key = SourceFileLocator.Find("DS4Windows/Actions/KeyOutputAction.cs");
            string button = SourceFileLocator.Find("DS4Windows/Actions/ButtonOutputAction.cs");
            if (key == null || button == null) return;

            string keyText = File.ReadAllText(key);
            string buttonText = File.ReadAllText(button);
            Assert.DoesNotContain("TryParse", keyText);
            Assert.DoesNotContain("TryParse", buttonText);
            Assert.DoesNotContain("KeyButtonSwitchMode", buttonText);
        }

        // ---- W3: 土台（空の配線）----

        [Fact]
        public void TheControlMigration_IsEmptyByDefault_AndFollowsTheSet()
        {
            var options = new BindingMigrationOptions();
            Assert.Empty(options.MigratedControlKinds);
            foreach (ControlBindingKind kind in Enum.GetValues(typeof(ControlBindingKind)))
                Assert.False(options.IsControlMigrated(kind));

            options.MigratedControlKinds.Add(ControlBindingKind.Key);
            Assert.True(options.IsControlMigrated(ControlBindingKind.Key));
            Assert.False(options.IsControlMigrated(ControlBindingKind.Macro));

            options.MigratedControlKinds.Remove(ControlBindingKind.Key);
            Assert.False(options.IsControlMigrated(ControlBindingKind.Key));
        }

        [Fact]
        public void TheHostDefault_MigratesTheKeyMacroAndExtrasKinds_AndRegistersTheBuilder()
        {
            var options = TestHost.GetService<BindingMigrationOptions>();
            var builder = TestHost.GetService<IControlBindingBuilder>();

            // Phase8-Step7 7.8b・7.8c・7.8e・7.8f-1: 既定の一覧に Key、Macro、Extras、ButtonPad（Controls タブ）。7.8f-2・7.8f-3 で増える
            Assert.Equal(new[] { ControlBindingKind.ButtonPad, ControlBindingKind.Extras, ControlBindingKind.Key, ControlBindingKind.Macro }, options.MigratedControlKinds.OrderBy(k => k.ToString(), StringComparer.Ordinal).ToArray());
            Assert.IsType<ControlBindingBuilder>(builder);
            Assert.Same(builder, TestHost.GetService<IControlBindingBuilder>());
        }

        [Fact]
        public void TheControlBindingBuilder_ReturnsAnEmptyList_WhenNoAssignmentQualifies()
        {
            var options = new BindingMigrationOptions();
            var settings = TestHost.GetService<IProfileSettingsService>();
            var builder = new ControlBindingBuilder(options, settings);

            Assert.Empty(builder.Build(MappingCharacterizationScope.Device));
            options.MigratedControlKinds.Add(ControlBindingKind.Key);
            Assert.Empty(builder.Build(MappingCharacterizationScope.Device));   // 割り当てが全部 Default なら、束縛はない
            Assert.Throws<ArgumentNullException>(() => new ControlBindingBuilder(null, settings));
            Assert.Throws<ArgumentNullException>(() => new ControlBindingBuilder(options, null));
        }

        [Fact]
        public void TheEngine_OwnsNoControl_WithoutABinding_AndDoesNotAllocate()
        {
            var engine = TestHost.GetService<ITriggerDispatchEngine>();

            Assert.False(engine.OwnsControl(0, DS4Controls.Cross));
            Assert.False(engine.OwnsControl(-1, DS4Controls.Cross));
            Assert.False(engine.OwnsControl(99, DS4Controls.Cross));

            for (int i = 0; i < 200; i++) { engine.OwnsControl(0, DS4Controls.Cross); engine.OwnsControl(1, DS4Controls.L1); }
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 5000; i++)
            {
                engine.OwnsControl(0, DS4Controls.Cross);
                engine.OwnsControl(1, DS4Controls.L1);
            }

            Assert.Equal(0L, GC.GetAllocatedBytesForCurrentThread() - before);
        }

        [Fact]
        public void TheMapping_ConsultsTheEngine_OnceBeforeTheLegacyBody()
        {
            string mapping = SourceFileLocator.Find("DS4Windows/DS4Control/Mapping.cs");
            if (mapping == null) return;

            string text = File.ReadAllText(mapping);
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf("_engine.OwnsControl(", index, StringComparison.Ordinal)) >= 0) { count++; index++; }
            Assert.Equal(1, count);
        }
    }
}
