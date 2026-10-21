using System.Linq;
using DS4Windows;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-C（Phase9-C-Plan.md）: <see cref="MacroRuntimeState"/> のデバイスごとの形と、<see cref="MacroRuntimeState.ApplyHeldControls"/>
    /// （旧 <c>Mapping.MapCustom</c> の末尾の「マクロが押しているボタンを出力へ重ねる」ブロック）の値の特性化。
    /// </summary>
    public class Phase9CMacroRuntimeStateTests
    {
        private static DS4State Apply(int flag, int device = 0, int applyDevice = 0)
        {
            var state = new MacroRuntimeState();
            state.MacroControl[device][flag] = true;
            state.MacroCount[device] = 1;
            var mapped = new DS4State();
            state.ApplyHeldControls(applyDevice, mapped);
            return mapped;
        }

        [Fact]
        public void NewInstance_HasPerDeviceFlagsAndCounts()
        {
            var state = new MacroRuntimeState();
            Assert.Equal(ControllerLimits.MaxControllerCount, state.MacroControl.Length);
            Assert.Equal(ControllerLimits.MaxControllerCount, state.MacroCount.Length);
            Assert.All(state.MacroControl, flags => Assert.Equal(MacroRuntimeState.MacroControlLength, flags.Length));
            Assert.True(state.MacroControl.Select(f => f).Distinct().Count() == state.MacroControl.Length, "デバイスごとの配列は別の instance");
        }

        [Fact]
        public void ApplyHeldControls_DoesNothing_WhenTheCountIsZero()
        {
            var state = new MacroRuntimeState();
            state.MacroControl[0][0] = true;   // フラグがあっても、個数が 0 なら重ねない（旧: MacroCount > 0 の条件）
            var mapped = new DS4State();
            state.ApplyHeldControls(0, mapped);
            Assert.False(mapped.Cross);
        }

        [Fact]
        public void ApplyHeldControls_MapsEveryFlagToItsOutput()
        {
            Assert.True(Apply(0).Cross);
            Assert.True(Apply(1).Circle);
            Assert.True(Apply(2).Square);
            Assert.True(Apply(3).Triangle);
            Assert.True(Apply(4).Options);
            Assert.True(Apply(5).Share);
            Assert.True(Apply(6).DpadUp);
            Assert.True(Apply(7).DpadDown);
            Assert.True(Apply(8).DpadLeft);
            Assert.True(Apply(9).DpadRight);
            Assert.True(Apply(10).PS);
            Assert.True(Apply(11).L1);
            Assert.True(Apply(12).R1);
            Assert.Equal(255, Apply(13).L2);
            Assert.Equal(255, Apply(14).R2);
            Assert.True(Apply(15).L3);
            Assert.True(Apply(16).R3);
            Assert.Equal(255, Apply(17).LX);
            Assert.Equal(0, Apply(18).LX);
            Assert.Equal(255, Apply(19).LY);
            Assert.Equal(0, Apply(20).LY);
            Assert.Equal(255, Apply(21).RX);
            Assert.Equal(0, Apply(22).RX);
            Assert.Equal(255, Apply(23).RY);
            Assert.Equal(0, Apply(24).RY);
            Assert.True(Apply(25).OutputTouchButton);
        }

        [Fact]
        public void ApplyHeldControls_OnlyAffectsTheDeviceThatHoldsTheFlags()
        {
            // ユーザー判断 Q2: デバイス 1 のマクロが押したボタンは、デバイス 2 の出力に重ならない（旧: 全コントローラーに重なっていた）
            DS4State other = Apply(0, device: 1, applyDevice: 2);
            Assert.False(other.Cross);

            DS4State same = Apply(0, device: 1, applyDevice: 1);
            Assert.True(same.Cross);
        }

        [Fact]
        public void ApplyHeldControls_DoesNotClearAnExistingOutput()
        {
            var state = new MacroRuntimeState();
            state.MacroControl[0][1] = true;
            state.MacroCount[0] = 1;
            var mapped = new DS4State { Cross = true, LX = 77 };
            state.ApplyHeldControls(0, mapped);
            Assert.True(mapped.Cross);   // 既にある出力は、そのまま（重ねるだけ）
            Assert.True(mapped.Circle);
            Assert.Equal(77, mapped.LX);
        }
    }
}
