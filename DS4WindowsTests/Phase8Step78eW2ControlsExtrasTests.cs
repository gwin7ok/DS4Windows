using System;
using System.Collections.Generic;
using System.IO;
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
    /// Phase8-Step7 7.8e-W3/W4（Phase8-Step7-8e-Plan.md §3.2・§4）: Controls タブの Extras の新経路。
    /// 値の解釈、出力（振動、ライトバー、感度、元に戻す、後始末）、ビルダーの所有の条件（Extras は通常の割り当てと直交する別の束縛）、エンジン（1 ボタン複数束縛、
    /// 後始末）、ロールバック、割り当て、ソースのガード。動作の旧・新の比べは <c>Phase8Step78eW1ControlsExtrasCharacterizationTests</c>（同じテストが新経路を走らせる）。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step78eW2ControlsExtrasTests
    {
        private const int Device = MappingCharacterizationScope.Device;
        private const string Full = "100,50,1,10,20,30,5,1,7";
        private readonly ITestOutputHelper _output;

        public Phase8Step78eW2ControlsExtrasTests(ITestOutputHelper output)
        {
            _output = output;
        }

        // ---- 値の解釈 ----

        [Fact]
        public void TryInterpretExtras_ReadsTheNineValues()
        {
            Assert.True(ControlBindingBuilder.TryInterpretExtras(Full, out ExtrasBindingValues v));

            Assert.Equal(100, v.HeavyRumble);
            Assert.Equal(50, v.LightRumble);
            Assert.True(v.LightbarEnabled);
            Assert.Equal(10, v.Red);
            Assert.Equal(20, v.Green);
            Assert.Equal(30, v.Blue);
            Assert.Equal(5, v.Flash);
            Assert.True(v.SensitivityEnabled);
            Assert.Equal(7, v.Sensitivity);
        }

        [Fact]
        public void TryInterpretExtras_TreatsUnreadableValuesAsZero_AndAcceptsMoreThanNine()
        {
            Assert.True(ControlBindingBuilder.TryInterpretExtras("x,y,1,a,b,c,d,0,0", out ExtrasBindingValues v));
            Assert.Equal(0, v.HeavyRumble);
            Assert.True(v.LightbarEnabled);
            Assert.Equal(0, v.Red);

            Assert.True(ControlBindingBuilder.TryInterpretExtras("1,2,0,0,0,0,0,0,0,99", out _));
        }

        [Fact]
        public void TryInterpretExtras_RejectsEmptyAndShortValues_SoTheLegacyPathKeepsHandlingThem()
        {
            Assert.False(ControlBindingBuilder.TryInterpretExtras(null, out _));
            Assert.False(ControlBindingBuilder.TryInterpretExtras(string.Empty, out _));
            Assert.False(ControlBindingBuilder.TryInterpretExtras("0,0,0,0,0,0,0,0,0", out _));   // 空の Extras
            Assert.False(ControlBindingBuilder.TryInterpretExtras("5,5,1", out _));                 // 9 個未満: 旧経路は途中で例外になる
        }

        // ---- 出力 ----

        private sealed class FakePort : IControlExtrasPort
        {
            public readonly List<string> Calls = new List<string>();
            public bool Throw;

            public void SetRumble(int deviceIndex, byte heavy, byte light) { Calls.Add($"Rumble({deviceIndex},{heavy},{light})"); if (Throw) throw new InvalidOperationException(); }
            public void ForceLightbar(int deviceIndex, DS4Color color, byte flash) => Calls.Add($"Force({deviceIndex},{color.red},{color.green},{color.blue},{flash})");
            public void ClearForcedLightbar(int deviceIndex) => Calls.Add($"Clear({deviceIndex})");
            public void TrySetTemporarySensitivity(int deviceIndex, int sensitivity) => Calls.Add($"Sens({deviceIndex},{sensitivity})");
            public void RestoreSensitivity(int deviceIndex) => Calls.Add($"Restore({deviceIndex})");
        }

        private static ControlExtrasOutputAction Output(FakePort port, string extras = Full)
        {
            Assert.True(ControlBindingBuilder.TryInterpretExtras(extras, out ExtrasBindingValues v));
            return new ControlExtrasOutputAction(new SpecialAction("Synthetic_Extras_t", "Synthetic", "Extras", "Extras", 0, string.Empty), v, port);
        }

        private static readonly OutputContextImpl Ctx = new OutputContextImpl(3, null);

        [Fact]
        public void Execute_AppliesRumbleLightbarAndSensitivity_InThatOrder()
        {
            var port = new FakePort();
            Output(port).Execute(Ctx);

            Assert.Equal(new[] { "Rumble(3,100,50)", "Force(3,10,20,30,5)", "Sens(3,7)" }, port.Calls);
        }

        [Fact]
        public void WhileHeld_AppliesAgainEveryFrame_LikeTheLegacyPath()
        {
            var port = new FakePort();
            ControlExtrasOutputAction output = Output(port);
            output.Execute(Ctx);
            port.Calls.Clear();

            output.WhileHeld(Ctx);

            Assert.Equal(new[] { "Rumble(3,100,50)", "Force(3,10,20,30,5)", "Sens(3,7)" }, port.Calls);
        }

        [Fact]
        public void ZeroRumbleAndDisabledParts_AreNotApplied_AndTheStopStillClearsTheLightbarAndSensitivity()
        {
            var port = new FakePort();
            ControlExtrasOutputAction output = Output(port, "0,0,0,1,2,3,0,0,0");
            output.Execute(Ctx);
            Assert.Empty(port.Calls);

            output.Stop(Ctx);   // 旧経路の癖: 無効の設定でも、離したときに強制を解除し、感度を戻す。振動は出していないので戻さない

            Assert.Equal(new[] { "Clear(3)", "Restore(3)" }, port.Calls);
        }

        [Fact]
        public void Stop_RestoresInTheLegacyOrder_AndResetsTheRumbleOnlyOnce()
        {
            var port = new FakePort();
            ControlExtrasOutputAction output = Output(port);
            output.Execute(Ctx);
            port.Calls.Clear();

            output.Stop(Ctx);
            Assert.Equal(new[] { "Clear(3)", "Restore(3)", "Rumble(3,0,0)" }, port.Calls);

            port.Calls.Clear();
            output.Stop(Ctx);
            Assert.Equal(new[] { "Clear(3)", "Restore(3)" }, port.Calls);   // 振動の記録は、すでに戻っている
        }

        [Fact]
        public void TheValuesAreCastToByteLikeTheLegacyPath()
        {
            var port = new FakePort();
            Output(port, "300,-1,1,256,257,-2,260,0,0").Execute(Ctx);

            Assert.Equal(new[] { "Rumble(3,44,255)", "Force(3,0,1,254,4)" }, port.Calls);
        }

        [Fact]
        public void OnReset_RestoresOnlyWhatWasApplied_AndOnlyOnce()
        {
            var port = new FakePort();
            ControlExtrasOutputAction output = Output(port);

            output.OnReset(Ctx);   // 何も適用していない
            Assert.Empty(port.Calls);

            output.Execute(Ctx);
            port.Calls.Clear();
            output.OnReset(Ctx);
            Assert.Equal(new[] { "Clear(3)", "Restore(3)", "Rumble(3,0,0)" }, port.Calls);

            port.Calls.Clear();
            output.OnReset(Ctx);   // もう戻っている
            Assert.Empty(port.Calls);

            output.Execute(Ctx);
            output.Stop(Ctx);
            port.Calls.Clear();
            output.OnReset(Ctx);   // 離したあと
            Assert.Empty(port.Calls);
        }

        [Fact]
        public void APortThatThrows_DoesNotBreakTheApply_AndNullArgumentsAreHandled()
        {
            var port = new FakePort { Throw = true };
            ControlExtrasOutputAction output = Output(port);

            output.Execute(Ctx);   // 例外は握りつぶす（旧経路と同じ）
            output.Execute(null);
            output.WhileHeld(null);
            output.Stop(null);
            output.OnReset(null);

            Assert.Throws<ArgumentNullException>(() => new ControlExtrasOutputAction(null, default, port));
            Assert.Throws<ArgumentNullException>(() => new ControlExtrasOutputAction(new SpecialAction("n", "c", "t", "d", 0, string.Empty), default, null));
            Assert.Equal("Synthetic_Extras_t", output.Id);
        }

        // ---- ビルダー: 所有の条件（Extras は通常の割り当てと直交する別の束縛）----

        private static IReadOnlyList<IActionBinding> Build() => TestHost.GetService<IControlBindingBuilder>().Build(Device);

        [Fact]
        public void DefaultWithExtras_MakesOneExtrasBinding_ThatDoesNotEraseTheOutput()
        {
            using var t = new ControlsKeyTestScope(true);
            t.MapExtras(DS4Controls.Cross, Full);

            IReadOnlyList<IActionBinding> bindings = Build();

            IActionBinding b = Assert.Single(bindings);
            Assert.Equal($"Synthetic_Extras_{Device}_{(int)DS4Controls.Cross}", b.Source.name);
            var input = Assert.IsType<ControlButtonInputAction>(b.Input);
            Assert.Equal(ControlBindingKind.Extras, input.Kind);
            Assert.Equal(ControlJudgement.Assignment, input.Judgement);
            Assert.False(b.Policy.RespectsFreshPressGuard);
            Assert.False(b.Policy.SuppressesTriggerButtons);
            Assert.False(b.Policy.ResetsTriggerOutput);
            Assert.False(b.Policy.ErasesOutputEveryFrame);
            Assert.True(b.Policy.StopOnRelease);
            Assert.IsType<ControlExtrasOutputAction>(Assert.Single(b.Outputs));
        }

        [Fact]
        public void KeyAndMacroWithExtras_MakeTheExtrasBindingFirst_ThenTheAssignmentBinding()
        {
            using var t = new ControlsKeyTestScope(true);
            t.MapKey(DS4Controls.Cross, 65).extras = Full;
            t.MapMacro(DS4Controls.Circle, new[] { 65, 65 }).extras = Full;

            IReadOnlyList<IActionBinding> bindings = Build();

            Assert.Equal(4, bindings.Count);
            AssertExtrasThen<KeyOutputAction>(bindings, DS4Controls.Cross);
            AssertExtrasThen<MacroAction>(bindings, DS4Controls.Circle);
        }

        private static void AssertExtrasThen<TAssignment>(IReadOnlyList<IActionBinding> bindings, DS4Controls control)
        {
            int extras = -1;
            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i].Outputs[0] is ControlExtrasOutputAction && ((ControlButtonInputAction)bindings[i].Input).Triggers[0] == control)
                    extras = i;
            }

            Assert.True(extras >= 0 && extras + 1 < bindings.Count);
            Assert.IsType<TAssignment>(bindings[extras + 1].Outputs[0]);
            Assert.Equal(control, ((ControlButtonInputAction)bindings[extras + 1].Input).Triggers[0]);
        }

        [Fact]
        public void ButtonWithExtras_AndShortExtras_AreNotOwned_SoTheLegacyPathHandlesTheWholeAssignment()
        {
            using var t = new ControlsKeyTestScope(true);
            t.Scope.RemapToButton(DS4Controls.Cross, X360Controls.A);
            TestGlobals.Store.GetDS4CSetting(Device, DS4Controls.Cross).extras = "5,5,1";   // 値が読めない（Button も 7.8f で新経路になったため、Extras が読めないことで旧経路に残す）
            t.MapKey(DS4Controls.Circle, 65).extras = "5,5,1";                              // 値が読めない

            Assert.Empty(Build());
        }

        [Fact]
        public void WithoutTheExtrasKind_OrThePort_AnAssignmentWithExtrasIsNotOwnedAtAll()
        {
            using var t = new ControlsKeyTestScope(true);
            t.MapKey(DS4Controls.Cross, 65).extras = Full;
            t.MapKey(DS4Controls.Circle, 66);
            var options = TestHost.GetService<BindingMigrationOptions>();

            // ロールバック: Extras の 1 行を外すと、Extras つきの割り当ては、Key も含めて旧経路が処理する。Extras なしの Key は新経路のまま
            options.MigratedControlKinds.Remove(ControlBindingKind.Extras);
            IActionBinding only = Assert.Single(Build());
            Assert.Equal(DS4Controls.Circle, ((ControlButtonInputAction)only.Input).Triggers[0]);

            // 窓口がない（テスト用の構築）: 同じ
            options.MigratedControlKinds.Add(ControlBindingKind.Extras);
            var withoutPort = new ControlBindingBuilder(options, t.Scope.Settings, TestHost.GetService<IMacroPlayer>());
            IActionBinding alsoOnly = Assert.Single(withoutPort.Build(Device));
            Assert.Equal(DS4Controls.Circle, ((ControlButtonInputAction)alsoOnly.Input).Triggers[0]);
        }

        [Fact]
        public void TheHostDefault_MigratesExtras_AndRegistersThePort()
        {
            var options = TestHost.GetService<BindingMigrationOptions>();
            Assert.True(options.IsControlMigrated(ControlBindingKind.Extras));
            Assert.IsType<DefaultControlExtrasPort>(TestHost.GetService<IControlExtrasPort>());
            Assert.Same(TestHost.GetService<IControlExtrasPort>(), TestHost.GetService<IControlExtrasPort>());
        }

        // ---- エンジン: 1 ボタン複数束縛、後始末、ロールバック ----

        private static DS4State State(bool cross = false)
        {
            DS4State state = ControlsKeyTestScope.Neutral();
            state.Cross = cross;
            return state;
        }

        [Fact]
        public void TheEngine_OwnsAControlWithSeveralBindings_AndProcessesThemInOrder()
        {
            using var t = new ControlsKeyTestScope(true);
            t.MapKey(DS4Controls.Cross, 65).extras = Full;
            t.PublishControlsOnly();
            var engine = TestHost.GetService<ITriggerDispatchEngine>();
            SpecialAction key = t.SyntheticFor(DS4Controls.Cross, 65);

            Assert.True(engine.OwnsControl(Device, DS4Controls.Cross));
            Assert.False(engine.OwnsControl(Device, DS4Controls.Circle));

            t.Run(State());
            t.Run(State(cross: true));

            Assert.True(t.IsBeingTriggered(key));                 // 鎖の 2 つ目（Key）も処理される
            Assert.True(t.ForceLight);                            // 1 つ目（Extras）も処理される
            Assert.Equal(7, t.MouseInfo.tempButtonSensitivity);
        }

        [Fact]
        public void RemovingTheExtrasKind_StopsOwningTheAssignment()
        {
            using var t = new ControlsKeyTestScope(true);
            t.MapExtras(DS4Controls.Cross, Full);
            t.PublishControlsOnly();
            var engine = TestHost.GetService<ITriggerDispatchEngine>();
            Assert.True(engine.OwnsControl(Device, DS4Controls.Cross));

            TestHost.GetService<BindingMigrationOptions>().MigratedControlKinds.Remove(ControlBindingKind.Extras);
            Assert.False(engine.OwnsControl(Device, DS4Controls.Cross));
        }

        [Fact]
        public void ADisconnectWhileHeld_RestoresTheEffects()
        {
            using var t = new ControlsKeyTestScope(true);
            t.MapExtras(DS4Controls.Cross, Full);
            t.PublishControlsOnly();

            t.Run(State());
            t.Run(State(cross: true));
            Assert.True(t.ForceLight);
            Assert.True(t.RumbleActive);

            TestHost.GetService<ITriggerDispatchEngine>().OnDeviceDisconnected(Device);   // 切断（登録簿も空にする）

            Assert.False(t.ForceLight);
            Assert.False(t.RumbleActive);
            Assert.Equal(-1, t.MouseInfo.tempButtonSensitivity);
        }

        [Fact]
        public void AnOtherOutputsHeldAcrossAnApply_IsNotTouchedByTheReset()
        {
            // 後始末の契約は、Extras だけが実装する。押したままの適用で、Key の押下は従来どおり（エンジンは Stop を呼ばない）
            using var t = new ControlsKeyTestScope(true);
            t.MapKey(DS4Controls.Cross, 65);
            t.PublishControlsOnly();
            SpecialAction key = t.SyntheticFor(DS4Controls.Cross, 65);

            t.Run(State());
            t.Run(State(cross: true));
            Assert.True(t.IsBeingTriggered(key));

            t.PublishControlsOnly();   // 束縛の差し替え
            t.Run(State(cross: true));

            Assert.True(t.IsBeingTriggered(key));   // Stop は呼ばれず、押下のまま
        }

        // ---- 割り当て ----

        private sealed class SilentPort : IControlExtrasPort
        {
            public void SetRumble(int deviceIndex, byte heavy, byte light) { }
            public void ForceLightbar(int deviceIndex, DS4Color color, byte flash) { }
            public void ClearForcedLightbar(int deviceIndex) { }
            public void TrySetTemporarySensitivity(int deviceIndex, int sensitivity) { }
            public void RestoreSensitivity(int deviceIndex) { }
        }

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
                    t.MapExtras(control, Full);

                var builder = new ControlBindingBuilder(TestHost.GetService<BindingMigrationOptions>(), t.Scope.Settings, TestHost.GetService<IMacroPlayer>(), new SilentPort());
                TestHost.GetService<IActionRegistry>().Replace(Device, builder.Build(Device));
                for (int i = 0; i < 300; i++) { t.Run(held); t.Run(idle); }
                long h = Allocated(t, held);
                long ix = Allocated(t, idle);
                if (migrated) { migratedHeld = h; migratedIdle = ix; _output.WriteLine($"[7.8e Extras] migrated held={h}B idle={ix}B"); }
                else { legacyHeld = h; legacyIdle = ix; _output.WriteLine($"[7.8e Extras] legacy   held={h}B idle={ix}B"); }
            }

            Assert.True(migratedHeld <= legacyHeld, $"押下: 新 {migratedHeld} B > 旧 {legacyHeld} B");
            Assert.True(migratedIdle <= legacyIdle, $"未操作: 新 {migratedIdle} B > 旧 {legacyIdle} B");
        }

        // ---- ソースのガード ----

        [Fact]
        public void TheSources_CarryTheExtrasWiring_AndTheLightbarMacroIsGone()
        {
            string engine = SourceFileLocator.Find("DS4Windows/Actions/TriggerDispatchEngine.cs");
            string registration = SourceFileLocator.Find("DS4Windows/DI/ServiceRegistration.cs");
            string mapping = SourceFileLocator.Find("DS4Windows/DS4Control/Mapping.cs");
            if (engine == null || registration == null || mapping == null) return;

            string engineText = File.ReadAllText(engine);
            Assert.Contains("ControlNext", engineText);
            Assert.Contains("IResettableOutputAction", engineText);
            Assert.Contains("MigratedControlKinds.Add(ControlBindingKind.Extras)", File.ReadAllText(registration));

            string mappingText = File.ReadAllText(mapping);
            Assert.DoesNotContain("LightbarMacroTrigger", mappingText);
            Assert.DoesNotContain("lightbarMacroTask", mappingText);
            Assert.DoesNotContain("threadCts", mappingText);
            Assert.DoesNotContain("RunLightbarMacro", mappingText);
            Assert.DoesNotContain("BoolDS4Controls", mappingText);

            Assert.Null(SourceFileLocator.Find("DS4Windows/DS4Forms/ViewModels/LightbarMacro.cs"));
            Assert.Null(SourceFileLocator.Find("DS4Windows/DS4Forms/LightbarMacroCreator.xaml"));
        }
    }
}
