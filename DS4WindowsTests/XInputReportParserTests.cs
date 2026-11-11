using System;
using System.Collections.Generic;
using DS4Windows.Core.Model;
using DS4Windows.Input.Parsers;
using DS4Windows.Input.Xbox;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// XInput の合成レポート（<see cref="XInputReportLayout"/>）の解析（<see cref="XInputReportParser"/>）を確かめる。
    /// ボタンの値は、実機で XInput を読んで得た値（A＝0x1000、Guide＝0x0400、十字キー＝0x0001〜0x0008）。
    /// </summary>
    public class XInputReportParserTests
    {
        private static byte[] Report(ushort buttons = 0, byte lt = 0, byte rt = 0, short lx = 0, short ly = 0, short rx = 0, short ry = 0)
        {
            var r = new byte[XInputReportLayout.ReportLength];
            r[XInputReportLayout.ButtonsOffset] = (byte)(buttons & 0xFF);
            r[XInputReportLayout.ButtonsOffset + 1] = (byte)(buttons >> 8);
            r[XInputReportLayout.LeftTriggerOffset] = lt;
            r[XInputReportLayout.RightTriggerOffset] = rt;
            Put(r, XInputReportLayout.LeftStickXOffset, lx);
            Put(r, XInputReportLayout.LeftStickYOffset, ly);
            Put(r, XInputReportLayout.RightStickXOffset, rx);
            Put(r, XInputReportLayout.RightStickYOffset, ry);
            return r;
        }

        private static void Put(byte[] r, int offset, short v)
        {
            r[offset] = (byte)(v & 0xFF);
            r[offset + 1] = (byte)((v >> 8) & 0xFF);
        }

        private static ControllerState Parse(byte[] report)
        {
            var state = new ControllerState();
            new XInputReportParser().Parse(report, state);
            return state;
        }

        [Fact]
        public void Idle_IsNeutral()
        {
            ControllerState s = Parse(Report());

            Assert.Equal(128, s.LX);
            Assert.Equal(128, s.LY);
            Assert.Equal(128, s.RX);
            Assert.Equal(128, s.RY);
            Assert.Equal(0, s.L2);
            Assert.Equal(0, s.R2);
            Assert.False(s.Cross || s.Circle || s.Square || s.Triangle || s.L1 || s.R1 || s.Share || s.Options || s.L3 || s.R3 || s.PS);
            Assert.False(s.DpadUp || s.DpadDown || s.DpadLeft || s.DpadRight);
        }

        [Theory]
        [InlineData(XInputReportLayout.A, "Cross")]
        [InlineData(XInputReportLayout.B, "Circle")]
        [InlineData(XInputReportLayout.X, "Square")]
        [InlineData(XInputReportLayout.Y, "Triangle")]
        [InlineData(XInputReportLayout.LeftShoulder, "L1")]
        [InlineData(XInputReportLayout.RightShoulder, "R1")]
        [InlineData(XInputReportLayout.Back, "Share")]
        [InlineData(XInputReportLayout.Start, "Options")]
        [InlineData(XInputReportLayout.LeftThumb, "L3")]
        [InlineData(XInputReportLayout.RightThumb, "R3")]
        [InlineData(XInputReportLayout.Guide, "PS")]
        [InlineData(XInputReportLayout.DpadUp, "DpadUp")]
        [InlineData(XInputReportLayout.DpadDown, "DpadDown")]
        [InlineData(XInputReportLayout.DpadLeft, "DpadLeft")]
        [InlineData(XInputReportLayout.DpadRight, "DpadRight")]
        public void SingleButton_SetsOnlyThatButton(ushort mask, string expected)
        {
            ControllerState s = Parse(Report(buttons: mask));

            var pressed = new List<string>();
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
            if (s.PS) pressed.Add("PS");
            if (s.DpadUp) pressed.Add("DpadUp");
            if (s.DpadDown) pressed.Add("DpadDown");
            if (s.DpadLeft) pressed.Add("DpadLeft");
            if (s.DpadRight) pressed.Add("DpadRight");

            Assert.Equal(new[] { expected }, pressed);
        }

        [Fact]
        public void DiagonalDpad_SetsBothDirections()
        {
            ControllerState s = Parse(Report(buttons: (ushort)(XInputReportLayout.DpadDown | XInputReportLayout.DpadRight)));

            Assert.True(s.DpadDown);
            Assert.True(s.DpadRight);
            Assert.False(s.DpadUp);
            Assert.False(s.DpadLeft);
        }

        [Theory]
        [InlineData((short)0, 128)]
        [InlineData(short.MinValue, 0)]
        [InlineData(short.MaxValue, 255)]
        [InlineData((short)-8192, 96)]
        [InlineData((short)8192, 160)]
        public void StickX_ConvertsTo8Bit(short raw, int expected)
        {
            ControllerState s = Parse(Report(lx: raw, rx: raw));

            Assert.Equal(expected, s.LX);
            Assert.Equal(expected, s.RX);
        }

        [Theory]
        [InlineData((short)0, 128)]
        [InlineData(short.MaxValue, 0)]       // XInput は上が正。DS4 は上が 0
        [InlineData(short.MinValue, 255)]
        [InlineData((short)8192, 96)]
        [InlineData((short)-8192, 160)]
        public void StickY_IsInverted(short raw, int expected)
        {
            ControllerState s = Parse(Report(ly: raw, ry: raw));

            Assert.Equal(expected, s.LY);
            Assert.Equal(expected, s.RY);
        }

        [Fact]
        public void Triggers_PassThrough_AndLeaveDigitalButtonsToLaterStage()
        {
            ControllerState s = Parse(Report(lt: 200, rt: 55));

            Assert.Equal(200, s.L2);
            Assert.Equal(55, s.R2);
            Assert.Equal(200, s.L2Raw);
            Assert.Equal(55, s.R2Raw);
            Assert.False(s.L2Btn);
            Assert.False(s.R2Btn);
        }

        [Fact]
        public void ShortReport_IsIgnored()
        {
            var state = new ControllerState { LX = 7 };

            new XInputReportParser().Parse(new byte[XInputReportLayout.ReportLength - 1], state);

            Assert.Equal(7, state.LX);
        }
    }
}
