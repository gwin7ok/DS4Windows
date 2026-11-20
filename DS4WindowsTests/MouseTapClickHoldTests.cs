using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// タップによる左クリックが、瞬間的ではなく一定時間（50ms）保持されること（2026-10-15 承認）。
    /// 判定は <c>TapClickDetector</c>、呼び出しは <c>TouchpadMouse</c> にある。
    /// </summary>
    public class MouseTapClickHoldTests
    {
        [Fact]
        public void TapClick_UsesHoldInsteadOfSingleFramePulse()
        {
            Assert.Equal(50.0, ConstantValueReader.Get<double>(typeof(DS4Windows.Processing.Touchpad.TapClickDetector), "TapClickHoldMs"));

            string text = SourceText.ReadCode("DS4Windows/Processing/Touchpad/TapClickDetector.cs");
            if (text == null) return;

            // タップ成立（即時／Double Tap の保留）の 2 経路とも、StartTapClickHold を呼ぶ
            Assert.True(Regex.Matches(text, @"\bStartTapClickHold\(\)\s*;").Count >= 2);

            // 保持中は毎フレーム Click(Left) を出す
            Assert.Matches(@"_time\.GetUtcNow\(\)\.UtcDateTime\s*<\s*tapClickHoldUntil\s*\)\s*\{\s*syntheticInput\.CountClick\(deviceNum,\s*Click\.Left\)", text);
        }

        [Fact]
        public void TouchUnchanged_ContinuesSynthesis_WhileTapPendingOrHeld()
        {
            string text = SourceText.ReadCode("DS4Windows/Processing/Touchpad/TouchpadMouse.cs");
            if (text == null) return;

            Assert.Matches(@"_buttons\.ReleaseButtonActive\s*\|\|\s*releaseButtonChanged\s*\|\|\s*_tap\.IsPendingOrHolding", text);

            string tap = SourceText.ReadCode("DS4Windows/Processing/Touchpad/TapClickDetector.cs");
            if (tap == null) return;
            Assert.Matches(@"tappedOnce\s*\|\|\s*_time\.GetUtcNow\(\)\.UtcDateTime\s*<\s*tapClickHoldUntil", tap);
        }
    }
}
