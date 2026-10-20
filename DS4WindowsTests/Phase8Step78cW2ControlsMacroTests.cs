using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.8c-W2/W3（Phase8-Step7-8c-Plan.md §3〜§5）: Controls タブの Macro の新経路（SA の Macro の仕様に合わせた）。
    /// 所有の条件、ビルダー、押下の判定の種類、ロールバック、割り当て。動作の旧・新の比べは <c>Phase8Step78cW1ControlsMacroCharacterizationTests</c>。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step78cW2ControlsMacroTests
    {
        private const int Device = MappingCharacterizationScope.Device;
        private static readonly int[] Steps = { 65, 65 };
        private readonly ITestOutputHelper _output;

        public Phase8Step78cW2ControlsMacroTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static DS4ControlSettings Macro(int[] steps)
        {
            var dcs = new DS4ControlSettings(DS4Controls.Cross);
            dcs.actionType = DS4ControlSettings.ActionType.Macro;
            dcs.action.actionMacro = steps;
            dcs.keyType = DS4KeyType.Macro;
            return dcs;
        }

        [Fact]
        public void TryInterpretMacro_AcceptsAPlainMacro_AndRejectsEverythingElse()
        {
            Assert.True(ControlBindingBuilder.TryInterpretMacro(Macro(Steps), out int[] steps));
            Assert.Same(Steps, steps);

            Assert.False(ControlBindingBuilder.TryInterpretMacro(null, out _));
            Assert.False(ControlBindingBuilder.TryInterpretMacro(new DS4ControlSettings(DS4Controls.Cross), out _));   // Default

            DS4ControlSettings key = Macro(Steps);
            key.actionType = DS4ControlSettings.ActionType.Key;
            Assert.False(ControlBindingBuilder.TryInterpretMacro(key, out _));

            Assert.False(ControlBindingBuilder.TryInterpretMacro(Macro(null), out _));            // 旧経路は、手順がないと例外になる。新経路は所有しない
            Assert.False(ControlBindingBuilder.TryInterpretMacro(Macro(new int[0]), out _));

            // Extras は、通常の割り当てと直交する部品（7.8e）。Macro の解釈は、Extras の有無を見ない（Extras は別の束縛）
            DS4ControlSettings extras = Macro(Steps);
            extras.extras = "1,2,0,0,0,0,0,0,0";
            Assert.True(ControlBindingBuilder.TryInterpretMacro(extras, out _));

        }

        [Fact]
        public void TheBuilder_MakesAMacroBinding_OnTheSpecialActionPath_WithTheSaJudgement()
        {
            using var t = new ControlsKeyTestScope(true);
            t.MapMacro(DS4Controls.Cross, Steps, DS4KeyType.HoldMacro | DS4KeyType.RepeatMacro | DS4KeyType.ScanCode);
            t.MapKey(DS4Controls.Circle, 66);

            IReadOnlyList<IActionBinding> bindings = TestHost.GetService<IControlBindingBuilder>().Build(Device);

            Assert.Equal(2, bindings.Count);
            IActionBinding macro = bindings.Single(b => b.Source.typeID == SpecialAction.ActionTypeId.Macro);
            var input = Assert.IsType<ControlButtonInputAction>(macro.Input);
            Assert.Equal(ControlBindingKind.Macro, input.Kind);
            Assert.Equal(ControlJudgement.Trigger, input.Judgement);
            Assert.Equal(new[] { DS4Controls.Cross }, input.Triggers.ToArray());
            Assert.IsType<MacroAction>(Assert.Single(macro.Outputs));
            Assert.True(macro.Policy.RespectsFreshPressGuard);            // SA の Macro と同じ安全弁
            Assert.False(macro.Policy.SuppressesTriggerButtons);
            Assert.True(macro.Policy.ErasesOutputEveryFrame);             // 割り当てたボタンの通常の出力は、毎フレーム消す（Controls の規則）
            Assert.Equal($"Synthetic_Macro_{Device}_{(int)DS4Controls.Cross}", macro.Source.name);
            Assert.Equal("65/65", macro.Source.details);
            Assert.Equal(new[] { 65, 65 }, macro.Source.macro.ToArray());
            Assert.Equal(DS4KeyType.Macro | DS4KeyType.HoldMacro | DS4KeyType.RepeatMacro | DS4KeyType.ScanCode, macro.Source.keyType);   // dcs の keyType をそのままコピー（Macro の印を含む）
            Assert.False(macro.Source.pressRelease);
            Assert.False(macro.Source.synchronized);
            Assert.Equal(macro.Source.name, macro.Source.controls);       // 繰り返しのマクロの同期キューは、ボタンごとに別
        }

        [Fact]
        public void AMacroBuiltWithoutAPlayer_IsNotBuilt()
        {
            var builder = new ControlBindingBuilder(TestHost.GetService<IProfileSettingsService>());   // プレーヤーなし
            using var t = new ControlsKeyTestScope(false);
            t.MapMacro(DS4Controls.Cross, Steps);

            Assert.Empty(builder.Build(Device));
        }

        [Fact]
        public void TheTriggerJudgement_UsesTheSaThreshold_AndTheAssignmentJudgementTheControlOne()
        {
            var input = new TriggerInput(Device, TestHost.GetService<IProfileSettingsService>());
            DS4StateFieldMapping fields = new DS4StateFieldMapping();
            fields.triggers[(int)DS4Controls.L2] = 1;
            input.Update(new DS4State(), null, null, null, fields);

            var trigger = new ControlButtonInputAction("p8s78c", DS4Controls.L2, ControlBindingKind.Macro, ControlJudgement.Trigger);
            var assignment = new ControlButtonInputAction("p8s78c", DS4Controls.L2);

            Assert.False(trigger.Evaluate(input).IsEstablished);     // 1 は、SA のトリガーの判定（100 より大きい）には届かない
            Assert.True(assignment.Evaluate(input).IsEstablished);   // Controls の Key の判定（0 より大きい）では押下
            Assert.Equal(ControlBindingKind.Key, assignment.Kind);
            Assert.Equal(ControlJudgement.Assignment, assignment.Judgement);

            fields.triggers[(int)DS4Controls.L2] = 101;
            Assert.True(trigger.Evaluate(input).IsEstablished);
        }

        [Fact]
        public void TheEngine_OwnsTheMacroControl()
        {
            using var t = new ControlsKeyTestScope(true);
            t.MapMacro(DS4Controls.Cross, Steps);
            t.Publish();
            var engine = TestHost.GetService<ITriggerDispatchEngine>();

            Assert.True(engine.OwnsControl(Device, DS4Controls.Cross));
        }

        private static long Allocated(ControlsKeyTestScope t, DS4State input)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 200; i++) t.Run(input);
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        [Fact]
        public void TheNewPath_DoesNotAddAllocationsToTheIdleFrame()
        {
            long legacyIdle = 0, migratedIdle = 0;
            DS4State idle = ControlsKeyTestScope.Neutral();

            foreach (bool migrated in new[] { false, true })
            {
                using var t = new ControlsKeyTestScope(migrated);
                foreach (DS4Controls control in new[] { DS4Controls.Cross, DS4Controls.Circle, DS4Controls.Square, DS4Controls.Triangle })
                    t.MapMacro(control, Steps);
                t.Publish();
                for (int i = 0; i < 300; i++) t.Run(idle);
                long ix = Allocated(t, idle);
                if (migrated) migratedIdle = ix; else legacyIdle = ix;
                _output.WriteLine($"[7.8c Macro] {(migrated ? "migrated" : "legacy  ")} idle={ix}B");
            }

            Assert.True(migratedIdle <= legacyIdle + 4096, $"未操作: 新 {migratedIdle} B > 旧 {legacyIdle} B");
        }
    }
}
