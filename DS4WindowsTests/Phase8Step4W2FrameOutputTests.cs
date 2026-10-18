using System;
using System.Linq;
using System.Reflection;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.DS4Control;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase8-Step4 W2（Phase8-Step4-Plan.md §2.2）: <see cref="FrameOutput"/> と <see cref="ButtonOutputAction"/> のテスト。
    /// 現行の <c>Mapping.ResetToDefaultValue</c>（private。リフレクション）・<c>ButtonSpecialActionOutput.ResolveOutputActive</c> と同じ結果になることを確認する。
    /// Stage3 全体との比較（<c>MapCustom</c> の出力）は、配線後の W4 の結合テストで行う。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase8Step4W2FrameOutputTests
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
            public int Device => Phase8Step4W2FrameOutputTests.Device;
            public IVirtualKBM OutputHandler => null;
            public IFrameOutput Frame { get; }
            public void RequestEndFrame() { }
        }

        private static IManagedActionManager Manager()
        {
            DS4WinWPF.AppHost.CreateHost();
            return TestHost.GetService<IManagedActionManager>();
        }

        // ---- ResetTriggerControl: 現行の ResetToDefaultValue との一致 ----

        [Fact]
        public void ResetTriggerControl_MatchesTheCurrentMappingMethod_ForEveryControl()
        {
            Mapping mapping = TestMappingFactory.Create();
            MethodInfo legacy = typeof(Mapping).GetMethod("ResetToDefaultValue", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(legacy);

            var random = new Random(8401);
            for (int c = 0; c <= DS4StateFieldMapping.LAST_DS4_ACTION; c++)
            {
                var control = (DS4Controls)c;
                for (int iteration = 0; iteration < 5; iteration++)
                {
                    DS4StateFieldMapping expected = RandomFields(random);
                    DS4StateFieldMapping actual = Clone(expected);

                    legacy.Invoke(mapping, new object[] { control, new DS4State(), expected });
                    var frame = new FrameOutput();
                    frame.Bind(actual, new SyntheticState());
                    frame.ResetTriggerControl(control);

                    AssertEqualFields(expected, actual, control);
                }
            }
        }

        private static DS4StateFieldMapping RandomFields(Random random)
        {
            var fields = new DS4StateFieldMapping();
            for (int i = 0; i < fields.buttons.Length; i++)
            {
                fields.buttons[i] = random.Next(2) == 0;
                fields.axisdirs[i] = (byte)random.Next(256);
                fields.triggers[i] = (byte)random.Next(256);
            }

            return fields;
        }

        private static DS4StateFieldMapping Clone(DS4StateFieldMapping source)
        {
            var copy = new DS4StateFieldMapping();
            Array.Copy(source.buttons, copy.buttons, source.buttons.Length);
            Array.Copy(source.axisdirs, copy.axisdirs, source.axisdirs.Length);
            Array.Copy(source.triggers, copy.triggers, source.triggers.Length);
            copy.outputTouchButton = source.outputTouchButton;
            return copy;
        }

        private static void AssertEqualFields(DS4StateFieldMapping expected, DS4StateFieldMapping actual, DS4Controls control)
        {
            Assert.True(expected.buttons.SequenceEqual(actual.buttons), $"{control}: buttons");
            Assert.True(expected.axisdirs.SequenceEqual(actual.axisdirs), $"{control}: axisdirs");
            Assert.True(expected.triggers.SequenceEqual(actual.triggers), $"{control}: triggers");
            Assert.Equal(expected.outputTouchButton, actual.outputTouchButton);
        }

        // ---- PressX360: Stage3 の分岐 ----

        [Fact]
        public void PressX360_FollowsTheStage3Branches_ForEveryX360Control()
        {
            foreach (X360Controls control in Enum.GetValues(typeof(X360Controls)))
            {
                var fields = new DS4StateFieldMapping();
                var state = new SyntheticState();
                var frame = new FrameOutput();
                frame.Bind(fields, state);

                frame.PressX360(control);

                bool touch = control == X360Controls.TouchpadClick;
                bool mouse = control >= X360Controls.LeftMouse && control <= X360Controls.WDOWN;
                bool button = !touch && !mouse && (int)control < fields.buttons.Length && control != X360Controls.None;

                Assert.Equal(touch, fields.outputTouchButton);

                int mouseTotal = state.currentClicks.leftCount + state.currentClicks.rightCount + state.currentClicks.middleCount
                    + state.currentClicks.fourthCount + state.currentClicks.fifthCount + state.currentClicks.wUpCount + state.currentClicks.wDownCount;
                Assert.Equal(mouse ? 1 : 0, mouseTotal);

                int pressed = 0;
                for (int i = 0; i < fields.buttons.Length; i++) if (fields.buttons[i]) pressed++;
                Assert.Equal(button ? 1 : 0, pressed);
                if (button) Assert.True(fields.buttons[(int)control], control.ToString());
            }
        }

        [Fact]
        public void PressX360_MouseControls_IncrementTheirOwnCounters()
        {
            var state = new SyntheticState();
            var frame = new FrameOutput();
            frame.Bind(new DS4StateFieldMapping(), state);

            frame.PressX360(X360Controls.LeftMouse);
            frame.PressX360(X360Controls.LeftMouse);
            frame.PressX360(X360Controls.RightMouse);
            frame.PressX360(X360Controls.MiddleMouse);
            frame.PressX360(X360Controls.FourthMouse);
            frame.PressX360(X360Controls.FifthMouse);
            frame.PressX360(X360Controls.WUP);
            frame.PressX360(X360Controls.WDOWN);

            Assert.Equal(2, state.currentClicks.leftCount);
            Assert.Equal(1, state.currentClicks.rightCount);
            Assert.Equal(1, state.currentClicks.middleCount);
            Assert.Equal(1, state.currentClicks.fourthCount);
            Assert.Equal(1, state.currentClicks.fifthCount);
            Assert.Equal(1, state.currentClicks.wUpCount);
            Assert.Equal(1, state.currentClicks.wDownCount);
        }

        [Fact]
        public void FrameOutput_WithoutBinding_DoesNothing_AndDoesNotAllocate()
        {
            var frame = new FrameOutput();
            frame.ResetTriggerControl(DS4Controls.Cross);   // 例外にならない
            frame.PressX360(X360Controls.A);

            var fields = new DS4StateFieldMapping();
            var state = new SyntheticState();
            frame.Bind(fields, state);
            for (int i = 0; i < 100; i++) { frame.PressX360(X360Controls.A); frame.PressX360(X360Controls.LeftMouse); frame.ResetTriggerControl(DS4Controls.Cross); }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 5000; i++)
            {
                frame.PressX360(X360Controls.A);
                frame.PressX360(X360Controls.LeftMouse);
                frame.PressX360(X360Controls.TouchpadClick);
                frame.ResetTriggerControl(DS4Controls.Cross);
                frame.ResetTriggerControl(DS4Controls.LXNeg);
            }

            Assert.Equal(0L, GC.GetAllocatedBytesForCurrentThread() - before);
        }

        // ---- ButtonOutputAction: ButtonSpecialActionOutput との一致 ----

        private static SpecialAction NewButton(string name, string mode, string details = "18")
            => new SpecialAction(name, "Cross", "Button", details, 0, mode + "\n");

        [Theory]
        [InlineData("Press")]
        [InlineData("Toggle")]
        public void ApplyFrame_MatchesTheCurrentResolveOutputActive_OverARandomSequence(string mode)
        {
            IManagedActionManager manager = Manager();
            string suffix = Guid.NewGuid().ToString("N");
            SpecialAction reference = NewButton("p8s4_btn_ref_" + mode + suffix, mode);
            SpecialAction subject = NewButton("p8s4_btn_new_" + mode + suffix, mode);
            var action = new ButtonOutputAction(subject, manager);
            var recorder = new RecordingFrame();
            var ctx = new Context(recorder);

            ActionInstanceState refState = ActionManager.GetStateFor(reference, Device);
            ActionInstanceState newState = manager.GetStateFor(subject, Device);
            Assert.NotNull(refState);
            Assert.NotNull(newState);
            refState.IsToggledOn = false;
            newState.IsToggledOn = false;

            var random = new Random(8402);
            bool previous = false;
            for (int step = 0; step < 600; step++)
            {
                bool guard = random.Next(4) == 0;
                refState.RequiresFreshPressAfterReset = guard;
                newState.RequiresFreshPressAfterReset = guard;
                bool triggered = random.Next(2) == 0 ? previous : random.Next(2) == 0; // 前の状態が続きやすい列

                bool expected = ButtonSpecialActionOutput.ResolveOutputActive(reference, Device, triggered, previous);

                recorder.Presses = 0;
                action.ApplyFrame(ctx, triggered, triggered && !previous);

                Assert.Equal(expected ? 1 : 0, recorder.Presses);
                Assert.Equal(refState.IsToggledOn, newState.IsToggledOn);
                if (expected) Assert.Equal((X360Controls)18, recorder.Last);
                previous = triggered;
            }
        }

        [Fact]
        public void ApplyFrame_WithAnInvalidDetails_OutputsNothing()
        {
            IManagedActionManager manager = Manager();
            var recorder = new RecordingFrame();
            var action = new ButtonOutputAction(NewButton("p8s4_btn_bad_" + Guid.NewGuid().ToString("N"), "Press", "not a number"), manager);

            action.ApplyFrame(new Context(recorder), true, true);

            Assert.Equal(0, recorder.Presses);
        }

        [Fact]
        public void ButtonOutputAction_EdgeCallsDoNothing_AndValidatesItsArguments()
        {
            IManagedActionManager manager = Manager();
            SpecialAction sa = NewButton("p8s4_btn_edge_" + Guid.NewGuid().ToString("N"), "Press");
            var recorder = new RecordingFrame();
            var ctx = new Context(recorder);
            var action = new ButtonOutputAction(sa, manager);

            action.Execute(ctx);
            action.Stop(ctx);
            action.ApplyFrame(null, true, true);          // 文脈が null でも例外にならない
            action.ApplyFrame(new Context(null), true, true); // 出力先が無くても例外にならない

            Assert.Equal(0, recorder.Presses);
            Assert.Equal(sa.name, action.Id);
            Assert.Throws<ArgumentNullException>(() => new ButtonOutputAction(null, manager));
            Assert.Throws<ArgumentNullException>(() => new ButtonOutputAction(sa, null));
            Assert.IsAssignableFrom<IFrameOutputAction>(action);
        }
    }
}
