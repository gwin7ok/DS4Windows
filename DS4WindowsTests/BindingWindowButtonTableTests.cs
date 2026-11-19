#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DS4Windows.UI;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 出力ピッカー（BindingWindow）の 147 のボタンと、割り当て（Key＝仮想キーコード、Button＝X360Controls の名前）の対応の網羅。
    /// 移行前は code-behind の辞書で手結びしていた。移行後は、XAML の <c>Command</c>＋<c>CommandParameter</c>（固定のボタン 121）と、
    /// <see cref="BindingWindowVisualizerButtons"/>（コントローラーの図が生成するボタン 26）が持つ。
    /// 下の表は、移行前の辞書を写した黄金値で、移行後の定義が一致し、ボタンの重複がないことを検査する。
    /// </summary>
    public class BindingWindowButtonTableTests
    {
        private static readonly string[] Golden =
        {
            "aBtn|Button|A",
            "bBtn|Button|B",
            "xBtn|Button|X",
            "yBtn|Button|Y",
            "lbBtn|Button|LB",
            "ltBtn|Button|LT",
            "rbBtn|Button|RB",
            "rtBtn|Button|RT",
            "backBtn|Button|Back",
            "startBtn|Button|Start",
            "guideBtn|Button|Guide",
            "lsbBtn|Button|LS",
            "lsuBtn|Button|LYNeg",
            "lsrBtn|Button|LXPos",
            "lsdBtn|Button|LYPos",
            "lslBtn|Button|LXNeg",
            "dpadUBtn|Button|DpadUp",
            "dpadRBtn|Button|DpadRight",
            "dpadDBtn|Button|DpadDown",
            "dpadLBtn|Button|DpadLeft",
            "rsbBtn|Button|RS",
            "rsuBtn|Button|RYNeg",
            "rsrBtn|Button|RXPos",
            "rsdBtn|Button|RYPos",
            "rslBtn|Button|RXNeg",
            "touchpadClickBtn|Button|TouchpadClick",
            "mouseUpBtn|Button|MouseUp",
            "mouseDownBtn|Button|MouseDown",
            "mouseLeftBtn|Button|MouseLeft",
            "mouseRightBtn|Button|MouseRight",
            "mouseLBBtn|Button|LeftMouse",
            "mouseMBBtn|Button|MiddleMouse",
            "mouseRBBtn|Button|RightMouse",
            "mouse4Btn|Button|FourthMouse",
            "mouse5Btn|Button|FifthMouse",
            "mouseWheelUBtn|Button|WUP",
            "mouseWheelDBtn|Button|WDOWN",
            "escBtn|Key|0x1B",
            "f1Btn|Key|0x70",
            "f2Btn|Key|0x71",
            "f3Btn|Key|0x72",
            "f4Btn|Key|0x73",
            "f5Btn|Key|0x74",
            "f6Btn|Key|0x75",
            "f7Btn|Key|0x76",
            "f8Btn|Key|0x77",
            "f9Btn|Key|0x78",
            "f10Btn|Key|0x79",
            "f11Btn|Key|0x7A",
            "f12Btn|Key|0x7B",
            "oem3Btn|Key|0xC0",
            "oneBtn|Key|0x31",
            "twoBtn|Key|0x32",
            "threeBtn|Key|0x33",
            "fourBtn|Key|0x34",
            "fiveBtn|Key|0x35",
            "sixBtn|Key|0x36",
            "sevenBtn|Key|0x37",
            "eightBtn|Key|0x38",
            "nineBtn|Key|0x39",
            "zeroBtn|Key|0x30",
            "minusBtn|Key|0xBD",
            "equalBtn|Key|0xBB",
            "bsBtn|Key|0x08",
            "tabBtn|Key|0x09",
            "qKey|Key|0x51",
            "wKey|Key|0x57",
            "eKey|Key|0x45",
            "rKey|Key|0x52",
            "tKey|Key|0x54",
            "yKey|Key|0x59",
            "uKey|Key|0x55",
            "iKey|Key|0x49",
            "oKey|Key|0x4F",
            "pKey|Key|0x50",
            "lbracketBtn|Key|0xDB",
            "rbracketBtn|Key|0xDD",
            "bSlashBtn|Key|0xDC",
            "capsLBtn|Key|0x14",
            "aKeyBtn|Key|0x41",
            "sBtn|Key|0x53",
            "dBtn|Key|0x44",
            "fBtn|Key|0x46",
            "gBtn|Key|0x47",
            "hBtn|Key|0x48",
            "jBtn|Key|0x4A",
            "kBtn|Key|0x4B",
            "lBtn|Key|0x4C",
            "semicolonBtn|Key|0xBA",
            "aposBtn|Key|0xDE",
            "enterBtn|Key|0x0D",
            "lshiftBtn|Key|0x10",
            "zBtn|Key|0x5A",
            "xKeyBtn|Key|0x58",
            "cBtn|Key|0x43",
            "vBtn|Key|0x56",
            "bKeyBtn|Key|0x42",
            "nBtn|Key|0x4E",
            "mBtn|Key|0x4D",
            "commaBtn|Key|0xBC",
            "periodBtn|Key|0xBE",
            "bslashBtn|Key|0xBF",
            "rshiftBtn|Key|0xA1",
            "lctrlBtn|Key|0xA2",
            "lWinBtn|Key|0x5B",
            "laltBtn|Key|0x12",
            "spaceBtn|Key|0x20",
            "raltBtn|Key|0xA5",
            "rwinBtn|Key|0x5C",
            "rctrlBtn|Key|0xA3",
            "prtBtn|Key|0x2C",
            "sclBtn|Key|0x91",
            "brkBtn|Key|0x13",
            "insBtn|Key|0x2D",
            "homeBtn|Key|0x24",
            "pgupBtn|Key|0x21",
            "delBtn|Key|0x2E",
            "endBtn|Key|0x23",
            "pgdwBtn|Key|0x22",
            "uarrowBtn|Key|0x26",
            "larrowBtn|Key|0x25",
            "darrowBtn|Key|0x28",
            "rarrowBtn|Key|0x27",
            "prevTrackBtn|Key|0xB1",
            "stopBtn|Key|0xB2",
            "playBtn|Key|0xB3",
            "nextTrackBtn|Key|0xB0",
            "volupBtn|Key|0xAF",
            "numlockBtn|Key|0x90",
            "numdivideBtn|Key|0x6F",
            "nummultiBtn|Key|0x6A",
            "numminusBtn|Key|0x6D",
            "voldownBtn|Key|0xAE",
            "num7Btn|Key|0x67",
            "num8Btn|Key|0x68",
            "num9Btn|Key|0x69",
            "numplusBtn|Key|0x6B",
            "volmuteBtn|Key|0xAD",
            "num4Btn|Key|0x64",
            "num5Btn|Key|0x65",
            "num6Btn|Key|0x66",
            "num1Btn|Key|0x61",
            "num2Btn|Key|0x62",
            "num3Btn|Key|0x63",
            "num0Btn|Key|0x60",
            "numPeriodBtn|Key|0x6E",
            "numEnterBtn|Key|0x0D",
        };

        private static List<string> Actual()
        {
            string? root = SourceFileLocator.RepoRoot();
            Assert.NotNull(root);
            string xaml = File.ReadAllText(Path.Combine(root!, "DS4Windows", "UI", "BindingWindow.xaml"));
            xaml = Regex.Replace(xaml, "<!--.*?-->", string.Empty, RegexOptions.Singleline);

            var result = new List<string>();
            foreach (Match m in Regex.Matches(xaml, "<Button\\b[^>]*>", RegexOptions.Singleline))
            {
                string tag = m.Value;
                Match name = Regex.Match(tag, "x:Name=\"(\\w+)\"");
                Match cmd = Regex.Match(tag, "Command=\"\\{Binding DataContext\\.(SelectKeyCommand|SelectButtonCommand),");
                Match param = Regex.Match(tag, "CommandParameter=\"([^\"]+)\"");
                if (!name.Success || !cmd.Success || !param.Success) continue;
                string kind = cmd.Groups[1].Value == "SelectKeyCommand" ? "Key" : "Button";
                result.Add($"{name.Groups[1].Value}|{kind}|{param.Groups[1].Value}");
            }

            foreach ((string id, string control) in BindingWindowVisualizerButtons.Table)
                result.Add($"{id}|Button|{control}");
            return result;
        }

        [Fact]
        public void EveryOutputButtonIsDefinedExactlyAsBefore()
        {
            var actual = Actual();

            Assert.Equal(147, actual.Count);
            Assert.Equal(Golden.OrderBy(s => s, StringComparer.Ordinal), actual.OrderBy(s => s, StringComparer.Ordinal));
        }

        [Fact]
        public void NoButtonIsDefinedTwice()
        {
            var names = Actual().Select(s => s.Split('|')[0]).ToList();

            Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
        }
    }
}
