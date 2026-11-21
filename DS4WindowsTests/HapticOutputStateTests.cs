using DS4Windows.Input;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 触覚の出力の状態（<c>HapticOutputState</c>）。要求された振動を、いまの出力へ合成する流れと、
    /// 自動停止の時計の始動・停止（仮想パッドからの停止の通知が失われても振動が止まるための保険）を固定する。
    /// </summary>
    public class HapticOutputStateTests
    {
        [Fact]
        public void SetRumble_IsAppliedToCurrentOnlyAfterMerge_AndRequestsOutput()
        {
            var h = new HapticOutputState();

            h.SetRumble(10, 20);
            Assert.Equal(0, h.Current.rumbleState.RumbleMotorStrengthRightLightFast);
            Assert.False(h.Current.dirty);

            h.Merge();
            Assert.Equal(10, h.Current.rumbleState.RumbleMotorStrengthRightLightFast);
            Assert.Equal(20, h.Current.rumbleState.RumbleMotorStrengthLeftHeavySlow);
            Assert.True(h.Current.dirty);

            h.Merge();   // 要求が消費されたので、2 回目は出力を要求しない
            Assert.False(h.Current.dirty);
        }

        [Fact]
        public void SetLightbar_MarksDirtyOnlyWhenChanged()
        {
            var h = new HapticOutputState();
            var light = new DS4Windows.Core.Model.DS4LightbarState();
            light.LightBarColor = new DS4Windows.Core.Model.DS4Color(1, 2, 3);

            h.SetLightbar(ref light);
            Assert.True(h.Current.dirty);

            h.Current.dirty = false;
            h.SetLightbar(ref light);
            Assert.False(h.Current.dirty);
        }

        [Fact]
        public void AutostopTimer_RunsWhileRumbleIsOn_AndStopsWhenSetToZero()
        {
            var h = new HapticOutputState();
            h.SetAutostopTime(0);
            h.SetRumble(5, 5);
            Assert.False(h.AutostopTimer.IsRunning);   // 無効なら、振動を設定しても時計は動かない

            h.SetAutostopTime(3000);
            Assert.True(h.AutostopTimer.IsRunning);

            h.SetRumble(0, 0);
            Assert.False(h.AutostopTimer.IsRunning);   // 0 の通知で止まる

            h.SetRumble(5, 5);
            Assert.True(h.AutostopTimer.IsRunning);    // 変化があれば、再始動する
        }
    }
}
