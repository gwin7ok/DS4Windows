#nullable enable
using System;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Parsers;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 入力レポートの解析（<see cref="IInputReportParser"/>）の頑健性。機器や通信の不具合で、どんなバイト列が来ても、例外を投げず、
    /// 状態の値が範囲に収まることを、乱数（種を固定）と端の値（全 0、全 0xFF）で確かめる。
    /// </summary>
    public class ReportParserRobustnessTests
    {
        public static TheoryData<string> Names() => new TheoryData<string> { "DS4", "DualSense USB", "DualSense BT", "Switch Pro", "Xbox" };

        private static (IInputReportParser Parser, int Length) Make(string name) => name switch
        {
            "DS4" => (new Ds4ReportParser(), 78),
            "DualSense USB" => (new DualSenseReportParser(0), 78),
            "DualSense BT" => (new DualSenseReportParser(1), 78),
            "Switch Pro" => (new SwitchProReportParser(), 362),
            "Xbox" => (new XInputReportParser(), 15),
            _ => throw new ArgumentOutOfRangeException(nameof(name)),
        };

        [Theory]
        [MemberData(nameof(Names))]
        public void RandomAndExtremeReports_NeverThrow(string name)
        {
            (IInputReportParser parser, int length) = Make(name);
            var random = new Random(12345);
            var state = new ControllerState();
            var report = new byte[length];

            for (int n = 0; n < 5000; n++)
            {
                random.NextBytes(report);
                parser.Parse(report, state);
            }

            Array.Clear(report);
            parser.Parse(report, state);
            Array.Fill(report, (byte)0xFF);
            parser.Parse(report, state);
        }

        [Fact]
        public void TheXboxParser_IgnoresAShortReport_AndKeepsTheState()
        {
            var state = new ControllerState { LX = 77 };

            new XInputReportParser().Parse(new byte[5], state);

            Assert.Equal(77, state.LX);
        }

        [Theory]
        [MemberData(nameof(Names))]
        public void TheSameReport_GivesTheSameState_EveryTime(string name)
        {
            (IInputReportParser parser, int length) = Make(name);
            var random = new Random(777);
            var report = new byte[length];
            random.NextBytes(report);

            var a = new ControllerState();
            var b = new ControllerState();
            parser.Parse(report, a);
            parser.Parse(report, b);

            Assert.Equal(a.LX, b.LX);
            Assert.Equal(a.LY, b.LY);
            Assert.Equal(a.RX, b.RX);
            Assert.Equal(a.RY, b.RY);
            Assert.Equal(a.L2, b.L2);
            Assert.Equal(a.R2, b.R2);
            Assert.Equal(a.Cross, b.Cross);
            Assert.Equal(a.DpadUp, b.DpadUp);
        }
    }
}
