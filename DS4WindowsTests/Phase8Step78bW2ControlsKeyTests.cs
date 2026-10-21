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

        [Fact]
        public void ControlButtonInputAction_IsEstablishedByTheControlOutputNature_NotByTheSpecialActionOne()
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
            Assert.False(v.SuppressSpecialActionLogs);   // Phase10-Step4c W2（ユーザー承認、2026-10-21）: Controls の Key も、SA と同じ形式のログを出す（以前は Q4＝A で抑止していた）
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

            // Extras は、通常の割り当てと直交する部品（7.8e）。Key の解釈は、Extras の有無を見ない（Extras は別の束縛。ビルダーが dcs 全体の所有を決める）
            DS4ControlSettings extras = Key();
            extras.extras = "1,2,0,0,0,0,0,0,0";
            Assert.True(ControlBindingBuilder.TryInterpretKey(extras, out _));

            Assert.False(ControlBindingBuilder.TryInterpretKey(Key(-1), out _));      // 旧経路は範囲外で例外になる。新経路は所有しない
            Assert.False(ControlBindingBuilder.TryInterpretKey(Key(70000), out _));
            Assert.True(ControlBindingBuilder.TryInterpretKey(Key(0), out _));
            Assert.True(ControlBindingBuilder.TryInterpretKey(Key(65535), out _));
        }

        // ---- ビルダー ----

        [Fact]
        public void TheBuilder_MakesABindingForEachQualifyingAssignment_WithTheLegacySyntheticName()
        {
            using var t = new ControlsKeyTestScope();
            t.MapKey(DS4Controls.Cross, 65);
            t.MapKey(DS4Controls.LXNeg, 66, DS4KeyType.Toggle);
            DS4ControlSettings withExtras = t.MapKey(DS4Controls.Circle, 67);
            withExtras.extras = "1,2,0,0,0,0,0,0,0";   // 7.8e: Extras つきも新経路が処理する（Extras の束縛 ＋ Key の束縛）

            IReadOnlyList<IActionBinding> all = TestHost.GetService<IControlBindingBuilder>().Build(Device);

            Assert.Equal(5, all.Count);
            Assert.Single(all, b => b.Outputs[0] is ControlExtrasOutputAction);
            Assert.Single(all, b => b.Outputs[0] is ControlPassthroughOutputAction);   // 7.8f-1: LXNeg の反対側（LXPos）は Default なので、素通し
            List<IActionBinding> bindings = all.Where(b => b.Outputs[0] is KeyOutputAction).ToList();
            Assert.Equal(3, bindings.Count);
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
        public void TheActionBindingBuilder_AppendsTheControlBindings_AfterTheSpecialActionOnes()
        {
            using var t = new SpecialActionTestScope();
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
            }
        }

        // ---- 所有とロールバック ----

        [Fact]
        public void TheEngine_OwnsOnlyTheMappedControl()
        {
            using var t = new ControlsKeyTestScope();
            t.MapKey(DS4Controls.Cross, 65);
            t.Publish();
            var engine = TestHost.GetService<ITriggerDispatchEngine>();

            Assert.True(engine.OwnsControl(Device, DS4Controls.Cross));
            Assert.False(engine.OwnsControl(Device, DS4Controls.Circle));
            Assert.False(engine.OwnsControl(Device + 1, DS4Controls.Cross));
        }

        // ---- 割り当て ----

        private static long Allocated(ControlsKeyTestScope t, DS4State input)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) t.Run(input);
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        // ---- ログ（Q4＝A）----

        private static List<string> RunLogged(bool[] crossFrames)
        {
            LoggingConfiguration saved = LogManager.Configuration;
            var target = new MemoryTarget("p8s78b") { Layout = "${level}|${message}" };
            var config = new LoggingConfiguration();
            config.AddTarget(target);
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, target);
            LogManager.Configuration = config;
            try
            {
                using (var t = new ControlsKeyTestScope())
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
        public void TheNewPath_LogsTheSpecialActionStyleLines_AndKeepsTheKeyActionLines()
        {
            bool[] frames = { false, true, true, false };
            string name = $"Synthetic_Key_{Device}_{(int)DS4Controls.Cross}";

            var logged = RunLogged(frames);

            // Phase10-Step4c W2（ユーザー承認、2026-10-21）: Controls タブの Key も、特殊アクションの書式のログを出す（立ち上がりは Trace と Debug、離したときは KeyReleased。以前は抑止していた［Q4＝A］）
            Assert.Contains(logged, l => l.Contains("Trigger detected for SA '" + name + "'"));
            Assert.Contains(logged, l => l.Contains("SpecialAction KEY triggered: name=" + name));
            Assert.Contains(logged, l => l.Contains("SpecialAction KeyReleased: device=" + Device + ", name=" + name));
            Assert.Contains(logged, l => l.Contains("Trigger released for SA '" + name + "'"));

            // KeyAction 以降のログ（同じ名前・同じキー）は出る
            List<string> KeyActionLines(List<string> logs) => logs.Where(l => (l.Contains("KeyAction.On") || l.Contains("KeyAction: trigger")) && l.Contains(name)).Select(l => System.Text.RegularExpressions.Regex.Replace(l, @"controller=\d+", "controller=#")).ToList();   // コントローラーの番号は、生成のたびに増えるため、そろえる
            Assert.NotEmpty(KeyActionLines(logged));
        }
    }
}
