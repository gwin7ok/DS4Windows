using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step7 7.8f-1 W2/W3（Phase8-Step7-8f-Plan.md §3.2）: Controls タブの Button のうち、パッド（X360 の軸・トリガー・ボタン）への割り当てと、Default の軸の素通しの新経路。
    /// アナログ値の読み取り（<c>TriggerInput.GetAxisValue／GetByteValue</c> ＝ 現行の <c>GetXYAxisMapping</c>／<c>GetByteMapping</c>）の一致、解釈、ビルダーの所有の条件、
    /// 評価されたフレームだけの出力、ロールバック、割り当て、ソースのガード。動作の旧・新の比べは <c>Phase8Step78fW1ControlsPadCharacterizationTests</c>（同じテストが新経路を走らせる）。
    /// 他の Host 構築テストと同じコレクションで直列に実行する。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step78fW2ControlsPadTests
    {
        private const int Device = MappingCharacterizationScope.Device;
        private readonly ITestOutputHelper _output;

        public Phase8Step78fW2ControlsPadTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static IProfileSettingsService Settings()
        {
            DS4WinWPF.AppHost.CreateHost();
            return TestHost.GetService<IProfileSettingsService>();
        }

        // ---- アナログ値: 現行の GetXYAxisMapping／GetByteMapping との一致 ----

        [Theory]
        [InlineData(GyroOutMode.Controls)]
        [InlineData(GyroOutMode.Passthru)]
        public void AnalogValues_MatchTheCurrentMappingFunctions_ForEveryControl(GyroOutMode mode)
        {
            IProfileSettingsService settings = Settings();
            Mapping mapping = TestMappingFactory.Create(settings);
            MethodInfo legacyAxis = typeof(Mapping).GetMethod("GetXYAxisMapping", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo legacyByte = typeof(Mapping).GetMethod("GetByteMapping", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(legacyAxis);
            Assert.NotNull(legacyByte);

            var input = new TriggerInput(Device, settings);
            var random = new Random(78601);
            GyroOutMode saved = settings.GyroOutputMode[Device];
            settings.GyroOutputMode[Device] = mode;
            try
            {
                for (int iteration = 0; iteration < 300; iteration++)
                {
                    DS4State state = new DS4State
                    {
                        LX = (byte)random.Next(256), LY = (byte)random.Next(256),
                        RX = (byte)random.Next(256), RY = (byte)random.Next(256),
                    };
                    DS4StateFieldMapping fields = new DS4StateFieldMapping();
                    for (int i = 0; i < fields.buttons.Length; i++)
                    {
                        fields.buttons[i] = random.Next(2) == 0;
                        fields.axisdirs[i] = (byte)random.Next(256);
                        fields.triggers[i] = (byte)random.Next(256);
                        fields.gryodirs[i] = random.Next(-150, 151);
                        fields.swipedirs[i] = (byte)random.Next(256);
                        fields.swipedirbools[i] = random.Next(2) == 0;
                    }

                    input.Update(state, null, null, null, fields);

                    for (int c = 0; c <= DS4StateFieldMapping.LAST_DS4_ACTION; c++)
                    {
                        var control = (DS4Controls)c;
                        byte expectedByte = (byte)legacyByte.Invoke(mapping, new object[] { Device, control, state, null, null, fields });
                        Assert.True(expectedByte == input.GetByteValue(control), $"{control} のバイト値が現行と異なる（iteration={iteration}, mode={mode}）");

                        foreach (bool alt in new[] { false, true })
                        {
                            byte expected = (byte)legacyAxis.Invoke(mapping, new object[] { Device, control, state, null, null, fields, alt });
                            Assert.True(expected == input.GetAxisValue(control, alt), $"{control} の軸の値（alt={alt}）が現行と異なる（iteration={iteration}, mode={mode}）");
                        }
                    }
                }
            }
            finally
            {
                settings.GyroOutputMode[Device] = saved;
            }
        }

        // ---- 解釈 ----

        private static DS4ControlSettings Button(X360Controls target, DS4KeyType keyType = DS4KeyType.None)
        {
            var dcs = new DS4ControlSettings(DS4Controls.Cross);
            dcs.actionType = DS4ControlSettings.ActionType.Button;
            dcs.action.actionBtn = target;
            dcs.keyType = keyType;
            return dcs;
        }

        [Fact]
        public void TryInterpretPad_ClassifiesTheTarget_ByTheReverseMappedControl()
        {
            Assert.True(ControlBindingBuilder.TryInterpretPad(Button(X360Controls.B), out PadBindingValues button, out bool erase));
            Assert.False(erase);
            Assert.Equal(PadTargetKind.Button, button.Kind);
            Assert.Equal(DS4Controls.Circle, button.Output);   // B の標準の割り当ては Circle
            Assert.Equal(DS4Controls.Cross, button.Source);

            Assert.True(ControlBindingBuilder.TryInterpretPad(Button(X360Controls.LXPos), out PadBindingValues axis, out _));
            Assert.Equal(PadTargetKind.Axis, axis.Kind);
            Assert.Equal(DS4Controls.LXPos, axis.Output);

            Assert.True(ControlBindingBuilder.TryInterpretPad(Button(X360Controls.LT), out PadBindingValues trigger, out _));
            Assert.Equal(PadTargetKind.Trigger, trigger.Kind);
            Assert.Equal(DS4Controls.L2, trigger.Output);

            Assert.True(ControlBindingBuilder.TryInterpretPad(Button(X360Controls.Start), out PadBindingValues start, out _));
            Assert.Equal(PadTargetKind.Button, start.Kind);
        }

        [Fact]
        public void TryInterpretPad_UnboundAndNone_OnlyErase()
        {
            Assert.True(ControlBindingBuilder.TryInterpretPad(Button(X360Controls.Unbound), out _, out bool unbound));
            Assert.True(unbound);
            Assert.True(ControlBindingBuilder.TryInterpretPad(Button(X360Controls.None), out _, out bool none));
            Assert.True(none);
        }

        [Fact]
        public void TryInterpretPad_RejectsEverythingElse_SoTheLegacyPathKeepsHandlingIt()
        {
            Assert.False(ControlBindingBuilder.TryInterpretPad(null, out _, out _));
            Assert.False(ControlBindingBuilder.TryInterpretPad(new DS4ControlSettings(DS4Controls.Cross), out _, out _));   // Default

            DS4ControlSettings key = Button(X360Controls.B);
            key.actionType = DS4ControlSettings.ActionType.Key;
            Assert.False(ControlBindingBuilder.TryInterpretPad(key, out _, out _));

            foreach (X360Controls other in new[]
            {
                X360Controls.TouchpadClick, X360Controls.LeftMouse, X360Controls.FifthMouse, X360Controls.WUP, X360Controls.WDOWN,
                X360Controls.MouseUp, X360Controls.MouseRight,
            })
            {
                Assert.False(ControlBindingBuilder.TryInterpretPad(Button(other), out _, out _), $"{other} は 7.8f-2・7.8f-3 の範囲");
            }

            Assert.False(ControlBindingBuilder.TryInterpretPad(Button(X360Controls.B, DS4KeyType.Toggle), out _, out _));   // Toggle は 7.8f-2
        }

        // ---- ビルダー ----

        private static IReadOnlyList<IActionBinding> Build() => TestHost.GetService<IControlBindingBuilder>().Build(Device);

        [Fact]
        public void ButtonToPad_MakesOnePadBinding_ThatErasesTheOriginalEveryFrame()
        {
            using var t = new ControlsKeyTestScope(true);
            t.MapButton(DS4Controls.Cross, X360Controls.B);

            IActionBinding b = Assert.Single(Build());

            Assert.Equal($"Synthetic_Pad_{Device}_{(int)DS4Controls.Cross}", b.Source.name);
            var input = Assert.IsType<ControlButtonInputAction>(b.Input);
            Assert.Equal(ControlBindingKind.ButtonPad, input.Kind);
            Assert.False(b.Policy.RespectsFreshPressGuard);
            Assert.False(b.Policy.SuppressesTriggerButtons);
            Assert.False(b.Policy.ResetsTriggerOutput);
            Assert.True(b.Policy.ErasesOutputEveryFrame);
            Assert.IsType<ControlPadOutputAction>(Assert.Single(b.Outputs));
        }

        [Fact]
        public void UnboundTarget_MakesAnEraseOnlyBinding()
        {
            using var t = new ControlsKeyTestScope(true);
            t.MapButton(DS4Controls.Cross, X360Controls.Unbound);

            IActionBinding b = Assert.Single(Build());

            Assert.Empty(b.Outputs);
            Assert.True(b.Policy.ErasesOutputEveryFrame);
        }

        [Fact]
        public void MouseTargetsAndToggle_AreNotBuilt()
        {
            using var t = new ControlsKeyTestScope(true);
            t.MapButton(DS4Controls.Cross, X360Controls.LeftMouse);
            t.MapButton(DS4Controls.Circle, X360Controls.WUP);
            t.MapButton(DS4Controls.Square, X360Controls.A, DS4KeyType.Toggle);

            Assert.Empty(Build());
        }

        [Fact]
        public void ButtonWithExtras_MakesTheExtrasBindingFirst_ThenThePadBinding()
        {
            using var t = new ControlsKeyTestScope(true);
            t.MapButton(DS4Controls.Cross, X360Controls.B).extras = "100,50,1,10,20,30,5,1,7";

            IReadOnlyList<IActionBinding> bindings = Build();

            Assert.Equal(2, bindings.Count);
            Assert.IsType<ControlExtrasOutputAction>(bindings[0].Outputs[0]);
            Assert.IsType<ControlPadOutputAction>(bindings[1].Outputs[0]);
        }

        [Fact]
        public void TheOppositeDirectionOfAnAssignedAxis_GetsAPassthroughBinding_OnlyWhenNeeded()
        {
            using var t = new ControlsKeyTestScope(true);

            // 反対側（LXPos）が Default で、LXNeg が割り当て済み（Key）→ LXPos に素通し
            t.MapKey(DS4Controls.LXNeg, 66);
            IReadOnlyList<IActionBinding> bindings = Build();
            Assert.Equal(2, bindings.Count);
            IActionBinding pass = Assert.Single(bindings, b => b.Outputs[0] is ControlPassthroughOutputAction);
            Assert.Equal($"Synthetic_PadPassthrough_{Device}_{(int)DS4Controls.LXPos}", pass.Source.name);
            Assert.False(pass.Policy.ErasesOutputEveryFrame);                       // Default は消さない
            Assert.Equal(new[] { DS4Controls.LXPos }, ((ControlButtonInputAction)pass.Input).Triggers.ToArray());

            // LXPos も割り当て済みなら、素通しは要らない（どちらも自分で出力する）
            t.MapKey(DS4Controls.LXPos, 67);
            Assert.DoesNotContain(Build(), b => b.Outputs.Count > 0 && b.Outputs[0] is ControlPassthroughOutputAction);
        }

        [Fact]
        public void ADefaultAxisWithExtras_AndAnAssignedPartner_GetsBothBindings_OrIsLeftToTheLegacyPathWithoutThePadKind()
        {
            using var t = new ControlsKeyTestScope(true);
            t.MapKey(DS4Controls.LXNeg, 66);
            TestGlobals.Store.GetDS4CSetting(Device, DS4Controls.LXPos).extras = "100,50,1,10,20,30,5,1,7";

            IReadOnlyList<IActionBinding> bindings = Build();
            Assert.Equal(3, bindings.Count);   // LXNeg の Key、LXPos の Extras、LXPos の素通し
            int extras = bindings.ToList().FindIndex(b => b.Outputs[0] is ControlExtrasOutputAction);
            Assert.IsType<ControlPassthroughOutputAction>(bindings[extras + 1].Outputs[0]);

            // ロールバック: ButtonPad の 1 行を外すと、素通しを旧経路が行うため、LXPos は Extras も含めて旧経路が処理する
            TestHost.GetService<BindingMigrationOptions>().MigratedControlKinds.Remove(ControlBindingKind.ButtonPad);
            IActionBinding only = Assert.Single(Build());
            Assert.IsType<KeyOutputAction>(only.Outputs[0]);
        }

        // ---- エンジン: 評価されたフレームだけ、出力する ----

        [Fact]
        public void ThePadOutput_IsAppliedOnlyOnFramesWhereTheBindingWasEvaluated()
        {
            using var t = new ControlsKeyTestScope(true);
            t.MapButton(DS4Controls.Cross, X360Controls.B);
            t.Publish();
            var engine = TestHost.GetService<ITriggerDispatchEngine>();
            IProfileSettingsService settings = t.Scope.Settings;

            var input = new TriggerInput(Device, settings);
            var state = new DS4State { LX = 128, LY = 128, RX = 128, RY = 128 };
            var fields = new DS4StateFieldMapping();
            fields.buttons[(int)DS4Controls.Cross] = true;
            input.Update(state, null, null, null, fields);

            var outFields = new DS4StateFieldMapping();
            var frame = new FrameOutput();
            frame.Bind(outFields, new SyntheticState());

            // 評価していないフレーム（旧経路が dcs を処理しなかったフレーム）: 出力しない
            engine.ApplyFrameOutputs(Device, input, frame);
            Assert.False(outFields.buttons[(int)DS4Controls.Circle]);

            // 評価したフレーム: 出力する（B → Circle）
            engine.ProcessControl(Device, DS4Controls.Cross, input, frame);
            engine.ApplyFrameOutputs(Device, input, frame);
            Assert.True(outFields.buttons[(int)DS4Controls.Circle]);
        }

        [Fact]
        public void RemovingTheButtonPadKind_HandsTheAssignmentBackToTheLegacyPath()
        {
            using var t = new ControlsKeyTestScope(true);
            t.MapButton(DS4Controls.Cross, X360Controls.B);
            t.Publish();
            var engine = TestHost.GetService<ITriggerDispatchEngine>();
            Assert.True(engine.OwnsControl(Device, DS4Controls.Cross));

            TestHost.GetService<BindingMigrationOptions>().MigratedControlKinds.Remove(ControlBindingKind.ButtonPad);
            Assert.False(engine.OwnsControl(Device, DS4Controls.Cross));

            t.Publish();
            Assert.Empty(TestHost.GetService<IActionRegistry>().GetBindings(Device));

            DS4State input = ControlsKeyTestScope.Neutral();
            input.Cross = true;
            DS4State output = t.Run(input);   // 旧経路が処理する（旧キューの処理で、B → Circle）
            Assert.True(output.Circle);
            Assert.False(output.Cross);
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
            DS4State held = ControlsKeyTestScope.Neutral(); held.Cross = true; held.L1 = true; held.LX = 40; held.R2 = 200;
            DS4State idle = ControlsKeyTestScope.Neutral();

            foreach (bool migrated in new[] { false, true })
            {
                using var t = new ControlsKeyTestScope(migrated);
                t.MapButton(DS4Controls.Cross, X360Controls.B);
                t.MapButton(DS4Controls.L1, X360Controls.RT);
                t.MapButton(DS4Controls.LXNeg, X360Controls.LYPos);
                t.MapButton(DS4Controls.R2, X360Controls.A);
                t.MapButton(DS4Controls.Square, X360Controls.Unbound);
                t.Publish();
                for (int i = 0; i < 300; i++) { t.Run(held); t.Run(idle); }
                long h = Allocated(t, held);
                long ix = Allocated(t, idle);
                if (migrated) { migratedHeld = h; migratedIdle = ix; _output.WriteLine($"[7.8f-1 Pad] migrated held={h}B idle={ix}B"); }
                else { legacyHeld = h; legacyIdle = ix; _output.WriteLine($"[7.8f-1 Pad] legacy   held={h}B idle={ix}B"); }
            }

            Assert.True(migratedHeld <= legacyHeld, $"押下: 新 {migratedHeld} B > 旧 {legacyHeld} B");
            Assert.True(migratedIdle <= legacyIdle, $"未操作: 新 {migratedIdle} B > 旧 {legacyIdle} B");
        }

        // ---- ソースのガード ----

        [Fact]
        public void TheSources_CarryThePadWiring()
        {
            string engine = SourceFileLocator.Find("DS4Windows/Actions/TriggerDispatchEngine.cs");
            string registration = SourceFileLocator.Find("DS4Windows/DI/ServiceRegistration.cs");
            if (engine == null || registration == null) return;

            Assert.Contains("IEvaluatedFrameOutputAction", File.ReadAllText(engine));
            Assert.Contains("MigratedControlKinds.Add(ControlBindingKind.ButtonPad)", File.ReadAllText(registration));
        }
    }
}
