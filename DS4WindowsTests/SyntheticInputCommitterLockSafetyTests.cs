using System;
using System.Threading.Tasks;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Synthetic;
using DS4Windows.Processing.Touchpad;
using Xunit;
using DS4Windows.Processing.Services.Conditioning;

namespace DS4WindowsTests
{
    /// <summary>
    /// <see cref="SyntheticInputCommitter.Commit"/> の書き込みロックの安全弁のテスト。
    /// ロックは全コントローラーで共有されるため、キーボード・マウスの送出で例外が出ても解放されないと、
    /// 別のコントローラーの入力までが止まる。
    /// </summary>
    public class SyntheticInputCommitterLockSafetyTests
    {
        private sealed class ThrowingKbm : IVirtualKBM
        {
            public bool ThrowOnWheel;
            public int WheelCount;

            public bool fakeKeyRepeat { get; set; }
            public bool Connect() => true;
            public bool Disconnect() => true;
            public void MoveRelativeMouse(int x, int y) { }
            public void PerformMouseWheelEvent(int vertical, int horizontal)
            {
                WheelCount++;
                if (ThrowOnWheel) throw new InvalidOperationException("send failed");
            }
            public void PerformMouseButtonEvent(uint mouseButton) { }
            public void PerformMouseButtonEventAlt(uint mouseButton, int type) { }
            public void PerformMouseButtonPress(uint mouseButton) { }
            public void PerformMouseButtonRelease(uint mouseButton) { }
            public void PerformKeyPress(uint key) { }
            public void PerformKeyPressAlt(uint key) { }
            public void PerformKeyRelease(uint key) { }
            public void PerformKeyReleaseAlt(uint key) { }
            public void Sync() { }
            public string GetDisplayName() => string.Empty;
            public string GetIdentifier() => string.Empty;
            public string GetFullDisplayName() => string.Empty;
        }

        [Fact]
        public void WhenTheSendThrows_TheSharedLockIsReleased_SoAnotherDeviceCanStillCommit()
        {
            using var scope = new MappingCharacterizationScope();
            var state = new SyntheticInputState();
            var kbm = new ThrowingKbm { ThrowOnWheel = true };
            var committer = new SyntheticInputCommitter(state, kbm);

            state.CountClick(0, Click.WUP);
            Assert.Throws<InvalidOperationException>(() => committer.Commit(0, scope.Settings));

            kbm.ThrowOnWheel = false;
            var other = Task.Run(() => committer.Commit(1, scope.Settings));
            Assert.True(other.Wait(TimeSpan.FromSeconds(3)), "別のデバイスの Commit が、解放されなかったロックで止まった");
        }
    }
}
