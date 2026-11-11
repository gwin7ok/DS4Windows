using System;
using DS4Windows.Core.Model;
using DS4Windows.Input.Parsers;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// USB の Xbox 360 コントローラーの HID 入力レポート（15 バイト）の解析（<see cref="XboxReportParser"/>）を、実機の記録で確かめる。
    /// レポートは、実機（<c>docs-forDIMG/MadeByAgent/tools/xbox-reports.txt</c>）の記録から取った実測値で、
    /// 操作ごとに、どの項目が変わるかを台本にしている。
    /// </summary>
    public class XboxReportParserTests
    {
        private const string Idle = "00-00-80-FF-7F-00-80-FF-7F-00-80-00-00-00-00";

        private static byte[] Bytes(string hex)
        {
            string[] parts = hex.Split('-');
            var bytes = new byte[parts.Length];
            for (int i = 0; i < parts.Length; i++) bytes[i] = Convert.ToByte(parts[i], 16);
            return bytes;
        }

        private static ControllerState Parse(string hex)
        {
            var state = new ControllerState();
            new XboxReportParser().Parse(Bytes(hex), state);
            return state;
        }

        [Fact]
        public void Idle_ReportIsNeutral()
        {
            ControllerState s = Parse(Idle);

            Assert.Equal(128, s.LX);
            Assert.Equal(128, s.LY);
            Assert.Equal(128, s.RX);
            Assert.Equal(128, s.RY);
            Assert.Equal(0, s.L2);
            Assert.Equal(0, s.R2);
            Assert.False(s.Cross || s.Circle || s.Square || s.Triangle || s.L1 || s.R1 || s.Share || s.Options || s.L3 || s.R3);
            Assert.False(s.DpadUp || s.DpadDown || s.DpadLeft || s.DpadRight);
        }

        [Theory]
        [InlineData("00-00-80-FF-7F-00-80-FF-7F-00-80-01-00-00-00", "Cross")]       // A
        [InlineData("00-00-80-FF-7F-00-80-FF-7F-00-80-02-00-00-00", "Circle")]      // B
        [InlineData("00-00-80-FF-7F-00-80-FF-7F-00-80-04-00-00-00", "Square")]      // X
        [InlineData("00-00-80-FF-7F-00-80-FF-7F-00-80-08-00-00-00", "Triangle")]    // Y
        [InlineData("00-00-80-FF-7F-00-80-FF-7F-00-80-10-00-00-00", "L1")]          // LB
        [InlineData("00-00-80-FF-7F-00-80-FF-7F-00-80-20-00-00-00", "R1")]          // RB
        [InlineData("00-00-80-FF-7F-00-80-FF-7F-00-80-40-00-00-00", "Share")]       // Back
        [InlineData("00-00-80-FF-7F-00-80-FF-7F-00-80-80-00-00-00", "Options")]     // Start
        [InlineData("00-00-80-FF-7F-00-80-FF-7F-00-80-00-01-00-00", "L3")]
        [InlineData("00-00-80-FF-7F-00-80-FF-7F-00-80-00-02-00-00", "R3")]
        public void SingleButton_SetsOnlyThatButton(string hex, string expected)
        {
            ControllerState s = Parse(hex);

            var pressed = new System.Collections.Generic.List<string>();
            if (s.Cross) pressed.Add("Cross");
            if (s.Circle) pressed.Add("Circle");
            if (s.Square) pressed.Add("Square");
            if (s.Triangle) pressed.Add("Triangle");
            if (s.L1) pressed.Add("L1");
            if (s.R1) pressed.Add("R1");
            if (s.Share) pressed.Add("Share");
            if (s.Options) pressed.Add("Options");
            if (s.L3) pressed.Add("L3");
            if (s.R3) pressed.Add("R3");

            Assert.Equal(new[] { expected }, pressed);
        }

        [Fact]
        public void FourFaceButtonsTogether_AreAllPressed()
        {
            ControllerState s = Parse("00-00-80-FF-7F-00-80-FF-7F-00-80-0F-00-00-00");

            Assert.True(s.Cross && s.Circle && s.Square && s.Triangle);
            Assert.False(s.L1 || s.R1 || s.Share || s.Options);
        }

        [Theory]
        [InlineData(0x00, false, false, false, false)]   // なし
        [InlineData(0x04, true, false, false, false)]    // 上
        [InlineData(0x08, true, true, false, false)]     // 右上
        [InlineData(0x0C, false, true, false, false)]    // 右
        [InlineData(0x10, false, true, true, false)]     // 右下
        [InlineData(0x14, false, false, true, false)]    // 下
        [InlineData(0x18, false, false, true, true)]     // 左下
        [InlineData(0x1C, false, false, false, true)]    // 左
        [InlineData(0x20, true, false, false, true)]     // 左上
        public void Hat_MapsToDpad(int byte12, bool up, bool right, bool down, bool left)
        {
            string hex = "00-00-80-FF-7F-00-80-FF-7F-00-80-00-" + byte12.ToString("X2") + "-00-00";
            ControllerState s = Parse(hex);

            Assert.Equal(up, s.DpadUp);
            Assert.Equal(right, s.DpadRight);
            Assert.Equal(down, s.DpadDown);
            Assert.Equal(left, s.DpadLeft);
        }

        [Fact]
        public void Hat_DoesNotDisturbStickClicks()
        {
            // 押し込み（bit0・bit1）と方向キー（bit2〜5）は、同じバイトの別のビット
            ControllerState s = Parse("00-00-80-FF-7F-00-80-FF-7F-00-80-00-0F-00-00");

            Assert.True(s.L3);
            Assert.True(s.R3);
            Assert.True(s.DpadRight);
        }

        [Theory]
        [InlineData("00-00-00-FF-7F-00-80-FF-7F-00-80-00-00-00-00", 0, 128, 128, 128)]       // 左スティック 左いっぱい
        [InlineData("00-FF-FF-FF-90-00-80-FF-7F-00-80-00-00-00-00", 255, 145, 128, 128)]     // 左スティック 右いっぱい
        [InlineData("00-00-8A-00-00-00-80-FF-7F-00-80-00-00-00-00", 138, 0, 128, 128)]       // 左スティック 上いっぱい（上が 0）
        [InlineData("00-00-80-FF-FF-00-80-FF-7F-00-80-00-00-00-00", 128, 255, 128, 128)]     // 左スティック 下いっぱい
        public void LeftStick_ConvertsTo8Bit(string hex, int lx, int ly, int rx, int ry)
        {
            ControllerState s = Parse(hex);

            Assert.Equal(lx, s.LX);
            Assert.Equal(ly, s.LY);
            Assert.Equal(rx, s.RX);
            Assert.Equal(ry, s.RY);
        }

        [Theory]
        [InlineData("00-00-80-FF-7F-00-00-FF-89-00-80-00-00-00-00", 0, 138)]       // 右スティック 左いっぱい
        [InlineData("00-00-80-FF-7F-FF-FF-FF-7F-00-80-00-00-00-00", 255, 128)]     // 右スティック 右いっぱい
        [InlineData("00-00-80-FF-7F-00-AC-00-00-00-80-00-00-00-00", 172, 0)]       // 右スティック 上いっぱい
        [InlineData("00-00-80-FF-7F-00-80-FF-FF-00-80-00-00-00-00", 128, 255)]     // 右スティック 下いっぱい
        public void RightStick_ConvertsTo8Bit(string hex, int rx, int ry)
        {
            ControllerState s = Parse(hex);

            Assert.Equal(rx, s.RX);
            Assert.Equal(ry, s.RY);
        }

        [Theory]
        [InlineData("00-00-80-FF-7F-00-80-FF-7F-80-FF-00-00-00-00", 255, 0)]    // LT 最大
        [InlineData("00-00-80-FF-7F-00-80-FF-7F-80-DD-00-00-00-00", 187, 0)]    // LT を半分より少し深く
        [InlineData("00-00-80-FF-7F-00-80-FF-7F-00-80-00-00-00-00", 0, 0)]      // 中立
        [InlineData("00-00-80-FF-7F-00-80-FF-7F-80-00-00-00-00-00", 0, 255)]    // RT 最大
        [InlineData("00-00-80-FF-7F-00-80-FF-7F-00-47-00-00-00-00", 0, 114)]    // 0x4700 は中央から 0x3900 小さい: RT 側
        public void Triggers_AreSplitFromTheCombinedAxis(string hex, int l2, int r2)
        {
            ControllerState s = Parse(hex);

            Assert.Equal(l2, s.L2);
            Assert.Equal(r2, s.R2);
            Assert.Equal(s.L2, s.L2Raw);
            Assert.Equal(s.R2, s.R2Raw);
        }

        [Fact]
        public void ShortReport_IsIgnored()
        {
            var state = new ControllerState { LX = 7 };

            new XboxReportParser().Parse(new byte[14], state);

            Assert.Equal(7, state.LX);
        }
    }
}
