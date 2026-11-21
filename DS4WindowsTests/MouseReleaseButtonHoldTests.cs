#nullable enable
using System;
using DS4Windows.Processing.Touchpad;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Touchpad Button Activation Mode が Release のとき、Left/Right Touch 等のフラグを
    /// 離した後 100ms 保持してから下ろすこと（上流の比較反転の是正）。手動の時計で、保持の前後を挙動で確かめる。
    /// </summary>
    public class MouseReleaseButtonHoldTests
    {
        private static TouchButtonDetector ActiveDetector(ManualTimeProvider time)
        {
            var d = new TouchButtonDetector(time) { ReleaseButtonActive = true, OnReleaseTime = time.GetUtcNow().UtcDateTime };
            return d;
        }

        [Fact]
        public void ReleaseButtonFlags_AreKeptUntilTheHoldTimePasses_ThenCleared()
        {
            var time = new ManualTimeProvider();
            TouchButtonDetector d = ActiveDetector(time);

            Assert.False(d.TryExpireRelease());   // 離した直後のフレームでは下ろさない（割り当てへ届くように）
            Assert.True(d.ReleaseButtonActive);

            time.Advance(TimeSpan.FromMilliseconds(TouchButtonDetector.ReleaseHoldMs - 1));
            Assert.False(d.TryExpireRelease());
            Assert.True(d.ReleaseButtonActive);

            time.Advance(TimeSpan.FromMilliseconds(1));   // ちょうど 100ms で下ろす
            Assert.True(d.TryExpireRelease());
            Assert.False(d.ReleaseButtonActive);
            Assert.False(d.TryExpireRelease());   // 下ろした後は、変化なし
        }

        [Fact]
        public void WhenNotActive_NothingExpires()
        {
            var time = new ManualTimeProvider();
            var d = new TouchButtonDetector(time);
            time.Advance(TimeSpan.FromSeconds(10));

            Assert.False(d.TryExpireRelease());
        }

        [Fact]
        public void TheHoldTime_Is100Ms()
        {
            Assert.Equal(100.0, TouchButtonDetector.ReleaseHoldMs);
        }
    }
}
