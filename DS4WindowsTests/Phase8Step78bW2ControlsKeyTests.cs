using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.Services;
using NLog;
using NLog.Config;
using NLog.Targets;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.8b-W2/W3（Phase8-Step7-8b-Plan.md §4）: Controls タブの Key の新経路。
    /// 押下の判定（<c>TriggerInput.IsControlActive</c> ＝ 現行の <c>GetBoolActionMapping</c>）の一致、所有の条件、ビルダー、連結、ロールバック、割り当て、ログ。
    /// 動作の旧・新の比べは <c>Phase8Step78bW1ControlsKeyCharacterizationTests</c>（同じテストが新経路を走らせる）。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step78bW2ControlsKeyTests
    {
        private const int Device = MappingCharacterizationScope.Device;
        private readonly ITestOutputHelper _output;

        public Phase8Step78bW2ControlsKeyTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static IProfileSettingsService Settings()
        {
            DS4WinWPF.AppHost.CreateHost();
            return TestHost.GetService<IProfileSettingsService>();
        }

        // ---- 押下の判定: 現行の GetBoolActionMapping との一致 ----

        [Theory]
        [InlineData(GyroOutMode.Controls)]
        [InlineData(GyroOutMode.Passthru)]
        public void IsControlActive_MatchesTheCurrentMappingEvaluation_ForEveryControl(GyroOutMode mode)
        {
            IProfileSettingsService settings = Settings();
            Mapping mapping = TestMappingFactory.Create(settings);
            MethodInfo legacy = typeof(Mapping).GetMethod("GetBoolActionMapping", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(legacy);

            var input = new TriggerInput(Device, settings);
            var random = new Random(78201);
            GyroOutMode saved = settings.GyroOutputMode[Device];
            settings.GyroOutputMode[Device] = mode;
            try
            {
                for (int iteration = 0; iteration < 400; iteration++)
                {
                    DS4State state = new DS4State
                    {
                        LX = (byte)random.Next(256), LY = (byte)random.Next(256),
                        RX = (byte)random.Next(256), RY = (byte)random.Next(256),
                    };
                    state.calculateStickAngles();
                    DS4StateFieldMapping fields = new DS4StateFieldMapping();
                    for (int i = 0; i < fields.buttons.Length; i++)
                    {
                        fields.buttons[i] = random.Next(2) == 0;
                        fields.axisdirs[i] = (byte)random.Next(256);
                        fields.triggers[i] = (byte)random.Next(256);
                        fields.gryodirs[i] = random.Next(-150, 151);
                        fields.swipedirbools[i] = random.Next(2) == 0;
                    }

                    input.Update(state, null, null, null, fields);

                    for (int c = 0; c <= DS4StateFieldMapping.LAST_DS4_ACTION; c++)
                    {
                        var control = (DS4Controls)c;
                        bool expected = (bool)legacy.Invoke(mapping, new object[] { Device, control, state, null, null, fields, false });
                        Assert.True(expected == input.IsControlActive(control), $"{control}（iteration={iteration}, mode={mode}）で現行の判定と異なる");
                    }
                }
            }
            finally
            {
                settings.GyroOutputMode[Device] = saved;
            }
        }

        [Fact]
        public void ControlButtonInputAction_IsEstablishedByTheControlJudgement_NotByTheSpecialActionOne()
        {
            IProfileSettingsService settings = Settings();
            var input = new TriggerInput(Device, settings);
            var action = new ControlButtonInputAction("p8s78b", DS4Controls.L2);
            DS4StateFieldMapping fields = new DS4StateFieldMapping();
            var state = new DS4State();

            fields.triggers[(int)DS4Controls.L2] = 1;   // 0 より大きい（Controls の判定）。100 より大きくはない（特殊アクションの判定）
            input.Update(state, null, null, null, fields);

            Assert.True(action.Evaluate(input).IsEstablished);
            Assert.False(input.IsActive(DS4Controls.L2));
            Assert.Equal(new[] { DS4Controls.L2 }, action.Triggers.ToArray());
            Assert.Equal("p8s78b", action.Name);
            Assert.Throws<ArgumentNullException>(() => action.Evaluate(null));
        }

        // ---- 所有の条件（Key、シフトなし、Extras なし、Lightbar マクロなし、範囲内）----

        private static DS4ControlSettings Key(int key = 65, DS4KeyType keyType = DS4KeyType.None, uint alias = 0)
        {
            var dcs = new DS4ControlSettings(DS4Controls.Cross);
            dcs.actionType = DS4ControlSettings.ActionType.Key;
            dcs.action.actionKey = key;
            dcs.action.actionAlias = alias;
            dcs.keyType = keyType;
            return dcs;
        }

        [Fact]
        public void TryInterpretKey_AcceptsAPlainKey_AndInterpretsTheValues()
        {
            Assert.True(ControlBindingBuilder.TryInterpretKey(Key(65, DS4KeyType.ScanCode | DS4KeyType.Toggle, 7), out KeyBindingValues v));

            Assert.Equal((ushort)65, v.LogicalKey);
            Assert.True(v.ScanCode);
            Assert.True(v.Toggle);
            Assert.Equal(7u, v.AliasKey);
            Assert.True(v.SuppressSpecialActionLogs);   // 旧経路の Controls タブの Key は、SA タブ形式のログを出していない（Q4＝A）
        }

        [Fact]
        public void TryInterpretKey_RejectsEverythingElse_SoTheLegacyPathKeepsHandlingIt()
        {
            Assert.False(ControlBindingBuilder.TryInterpretKey(null, out _));
            Assert.False(ControlBindingBuilder.TryInterpretKey(new DS4ControlSettings(DS4Controls.Cross), out _));   // Default

            DS4ControlSettings macro = Key();
            macro.actionType = DS4ControlSettings.ActionType.Macro;
            Assert.False(ControlBindingBuilder.TryInterpretKey(macro, out _));

            DS4ControlSettings button = Key();
            button.actionType = DS4ControlSettings.ActionType.Button;
            Assert.False(ControlBindingBuilder.TryInterpretKey(button, out _));

            DS4ControlSettings shifted = Key();
            shifted.shiftActionType = DS4ControlSettings.ActionType.Key;
            shifted.shiftAction.actionKey = 66;
            Assert.False(ControlBindingBuilder.TryInterpretKey(shifted, out _));

            DS4ControlSettings extras = Key();
            extras.extras = "1,2,0,0,0,0,0,0,0";
            Assert.False(ControlBindingBuilder.TryInterpretKey(extras, out _));

            DS4ControlSettings shiftExtras = Key();
            shiftExtras.shiftExtras = "1,2,0,0,0,0,0,0,0";
            Assert.False(ControlBindingBuilder.TryInterpretKey(shiftExtras, out _));

            Assert.False(ControlBindingBuilder.TryInterpretKey(Key(-1), out _));      // 旧経路は範囲外で例外になる。新経路は所有しない
            Assert.False(ControlBindingBuilder.TryInterpretKey(Key(70000), out _));
            Assert.True(ControlBindingBuilder.TryInterpretKey(Key(0), out _));
            Assert.True(ControlBindingBuilder.TryInterpretKey(Key(65535), out _));
        }

        // ---- ビルダー ----

        [Fact]
        public void TheBuilder_MakesABindingForEachQualifyingAssignment_WithTheLegacySyntheticName()
        {
            using var t = new ControlsKeyTestScope(true);
            t.MapKey(DS4Controls.Cross, 65);
            t.MapKey(DS4Controls.LXNeg, 66, DS4KeyType.Toggle);
            DS4ControlSettings shifted = t.MapKey(DS4Controls.Circle, 67);
            shifted.shiftActionType = DS4ControlSettings.ActionType.Key;   // 条件を満たさない
            shifted.shiftAction.actionKey = 68;
            t.MapKey(DS4Controls.GyroSwipeLeft, 69);                        // 対象外

            IReadOnlyList<IActionBinding> bindings = TestHost.GetService<IControlBindingBuilder>().Build(Device);

            Assert.Equal(2, bindings.Count);
            Assert.All(bindings, b =>
            {
                Assert.IsType<ControlButtonInputAction>(b.Input);
                Assert.False(b.Policy.RespectsFreshPressGuard);
                Assert.False(b.Policy.SuppressesTriggerButtons);
                Assert.True(b.Policy.ErasesOutputEveryFrame);
                Assert.Single(b.Outputs);
                Assert.IsType<KeyOutputAction>(b.Outputs[0]);
            });
            Assert.Contains(bindings, b => b.Source.name == $"Synthetic_Key_{Device}_{(int)DS4Controls.Cross}" && b.Source.details == "65");
            Assert.Contains(bindings, b => b.Source.name == $"Synthetic_Key_{Device}_{(int)DS4Controls.LXNeg}" && b.Source.keyType.HasFlag(DS4KeyType.Toggle));
        }

        [Fact]
        public void TheBuilder_ReturnsNothing_WhenTheKeyKindIsNotMigrated()
        {
            using var t = new ControlsKeyTestScope(false);
            t.MapKey(DS4Controls.Cross, 65);

            Assert.Empty(TestHost.GetService<IControlBindingBuilder>().Build(Device));
        }

        [Fact]
        public void TheActionBindingBuilder_AppendsTheControlBindings_AfterTheSpecialActionOnes()
        {
            using var t = new SpecialActionTestScope(true);
            var options = TestHost.GetService<BindingMigrationOptions>();
            var saved = options.MigratedControlKinds.ToArray();
            options.MigratedControlKinds.Clear();
            options.MigratedControlKinds.Add(ControlBindingKind.Key);
            DS4ControlSettings dcs = TestGlobals.Store.GetDS4CSetting(Device, DS4Controls.Cross);
            dcs.Reset();
            dcs.actionType = DS4ControlSettings.ActionType.Key;
            dcs.action.actionKey = 65;
            try
            {
                SpecialAction sa = t.AddKey("L1", toggle: false);

                IReadOnlyList<IActionBinding> all = TestHost.GetService<IActionBindingBuilder>().Build(Device);

                Assert.Equal(2, all.Count);
                Assert.Same(sa, all[0].Source);                       // 特殊アクションが先
                Assert.IsType<ControlButtonInputAction>(all[1].Input); // Controls が後
            }
            finally
            {
                dcs.Reset();
                options.MigratedControlKinds.Clear();
                foreach (ControlBindingKind kind in saved) options.MigratedControlKinds.Add(kind);
            }
        }

        // ---- 所有とロールバック ----

        [Fact]
        public void TheEngine_OwnsOnlyTheMappedControl_AndRemovingTheKindHandsItBackToTheLegacyPath()
        {
            using var t = new ControlsKeyTestScope(true);
            t.MapKey(DS4Controls.Cross, 65);
            t.Publish();
            var engine = TestHost.GetService<ITriggerDispatchEngine>();

            Assert.True(engine.OwnsControl(Device, DS4Controls.Cross));
            Assert.False(engine.OwnsControl(Device, DS4Controls.Circle));
            Assert.False(engine.OwnsControl(Device + 1, DS4Controls.Cross));

            // 既定の一覧から Key だけを外した状態（ロールバック）: 所有しない。束縛も作られない
            TestHost.GetService<BindingMigrationOptions>().MigratedControlKinds.Remove(ControlBindingKind.Key);
            Assert.False(engine.OwnsControl(Device, DS4Controls.Cross));
            t.Publish();
            Assert.Empty(TestHost.GetService<IActionRegistry>().GetBindings(Device));

            // 旧経路が処理する（合成 SA の状態が変わる）
            SpecialAction synthetic = t.SyntheticFor(DS4Controls.Cross, 65);
            DS4State held = ControlsKeyTestScope.Neutral();
            held.Cross = true;
            t.Run(held);
            Assert.True(t.IsBeingTriggered(synthetic));
        }

        // ---- 割り当て ----

        private static long Allocated(ControlsKeyTestScope t, DS4State input)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) t.Run(input);
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        [Fact]
        public void TheNewPath_DoesNotAddAllocationsToTheFrame()
        {
            long legacyHeld = 0, legacyIdle = 0, migratedHeld = 0, migratedIdle = 0;
            DS4State held = ControlsKeyTestScope.Neutral(); held.Cross = true; held.L1 = true;
            DS4State idle = ControlsKeyTestScope.Neutral();

            foreach (bool migrated in new[] { false, true })
            {
                using var t = new ControlsKeyTestScope(migrated);
                foreach (DS4Controls control in new[] { DS4Controls.Cross, DS4Controls.Circle, DS4Controls.Square, DS4Controls.Triangle, DS4Controls.L1, DS4Controls.R1 })
                    t.MapKey(control, 65);
                t.Publish();
                for (int i = 0; i < 300; i++) { t.Run(held); t.Run(idle); }
                long h = Allocated(t, held);
                long ix = Allocated(t, idle);
                if (migrated) { migratedHeld = h; migratedIdle = ix; _output.WriteLine($"[7.8b Key] migrated held={h}B idle={ix}B"); }
                else { legacyHeld = h; legacyIdle = ix; _output.WriteLine($"[7.8b Key] legacy   held={h}B idle={ix}B"); }
            }

            Assert.True(migratedHeld <= legacyHeld, $"押下: 新 {migratedHeld} B > 旧 {legacyHeld} B");
            Assert.True(migratedIdle <= legacyIdle, $"未操作: 新 {migratedIdle} B > 旧 {legacyIdle} B");
        }

        // ---- ログ（Q4＝A）----

        private static List<string> RunLogged(bool migrated, bool[] crossFrames)
        {
            LoggingConfiguration saved = LogManager.Configuration;
            var target = new MemoryTarget("p8s78b") { Layout = "${level}|${message}" };
            var config = new LoggingConfiguration();
            config.AddTarget(target);
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, target);
            LogManager.Configuration = config;
            try
            {
                using (var t = new ControlsKeyTestScope(migrated))
                {
                    t.MapKey(DS4Controls.Cross, 65);
                    t.Publish();
                    foreach (bool cross in crossFrames)
                    {
                        DS4State input = ControlsKeyTestScope.Neutral();
                        input.Cross = cross;
                        t.Run(input);
                    }
                }

                return target.Logs.ToList();
            }
            finally
            {
                LogManager.Configuration = saved;
            }
        }

        [Fact]
        public void TheNewPath_LogsNoSpecialActionStyleLines_AndKeepsTheKeyActionLines()
        {
            bool[] frames = { false, true, true, false };
            string name = $"Synthetic_Key_{Device}_{(int)DS4Controls.Cross}";

            var legacy = RunLogged(false, frames);
            var migrated = RunLogged(true, frames);

            bool IsSpecialActionStyle(string l) => l.Contains(name) && (l.Contains("SpecialAction KEY") || l.Contains("SpecialAction Key") || l.Contains("Trigger detected for SA")
                || l.Contains("maps to Type:Key") || l.Contains("Trigger keys for SA") || l.Contains("output combo"));
            Assert.Empty(legacy.Where(IsSpecialActionStyle));     // 旧経路の Controls タブの Key は、これらを出していない
            Assert.Empty(migrated.Where(IsSpecialActionStyle));

            // KeyAction 以降のログ（同じ名前・同じキー）は、旧・新で同じ
            List<string> KeyActionLines(List<string> logs) => logs.Where(l => (l.Contains("KeyAction.On") || l.Contains("KeyAction: trigger")) && l.Contains(name)).Select(l => System.Text.RegularExpressions.Regex.Replace(l, @"controller=\d+", "controller=#")).ToList();   // コントローラーの番号は、生成のたびに増えるため、そろえる
            Assert.NotEmpty(KeyActionLines(legacy));
            Assert.Equal(KeyActionLines(legacy), KeyActionLines(migrated));
        }
    }
}
