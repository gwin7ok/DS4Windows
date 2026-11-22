#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Input.Devices;
using DS4Windows.Input.Hid;
using DS4Windows.Input.Parsers;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 入力レポートの読み方（パーサー、CRC）が、公開されている別実装（Linux カーネルの <c>hid-playstation.c</c>・<c>hid-nintendo.c</c>）の仕様と一致することを確かめる。
    /// 既存のパーサーのテストは「切り出す前の旧コードとの一致」で、旧コードに誤りがあれば一緒に通ってしまう。
    /// ここでは、台本のバイト列を、パーサーのコードを見ずに、カーネルの構造体のオフセットとビットマスクから組み立てる（定数は下に、出どころの名前つきで書く）。
    /// 食い違いが見つかったものは、テストにせず <c>docs-forDIMG/MadeByAgent/PhaseRV38-Spec-Crosscheck.md</c> に記録する（実機で確かめる）。
    /// </summary>
    public class ReportSpecConformanceTests
    {
        // ---- hid-playstation.c: DS4（common ブロックの先頭からのオフセット）----
        private const int Ds4CommonX = 0, Ds4CommonY = 1, Ds4CommonRx = 2, Ds4CommonRy = 3;
        private const int Ds4CommonButtons0 = 4, Ds4CommonButtons1 = 5, Ds4CommonButtons2 = 6;
        private const int Ds4CommonZ = 7, Ds4CommonRz = 8, Ds4CommonStatus0 = 29;
        private const int Ds4UsbCommonStart = 1;   // dualshock4_input_report_usb: report_id の次
        private const int Ds4BtCommonStart = 3;    // dualshock4_input_report_bt: report_id と reserved[2] の次
        private const int Ds4BtSize = 78;          // DS4_INPUT_REPORT_BT_SIZE（末尾 4 バイトが crc32）

        // buttons[0]、buttons[1]、buttons[2] のマスク（DS_BUTTONS0_*、DS_BUTTONS1_*、DS_BUTTONS2_*）
        private const byte Square = 0x10, Cross = 0x20, Circle = 0x40, Triangle = 0x80;
        private const byte L1 = 0x01, R1 = 0x02, L2Digital = 0x04, R2Digital = 0x08, Create = 0x10, Options = 0x20, L3 = 0x40, R3 = 0x80;
        private const byte PsHome = 0x01, TouchPad = 0x02;

        // ps_gamepad_hat_mapping（添字 0〜7 が上から時計回り、8 が中立）を、(上, 右, 下, 左) の 4 つへ
        private static readonly (bool Up, bool Right, bool Down, bool Left)[] Hat =
        {
            (true, false, false, false),   // 0 {0,-1}
            (true, true, false, false),    // 1 {1,-1}
            (false, true, false, false),   // 2 {1,0}
            (false, true, true, false),    // 3 {1,1}
            (false, false, true, false),   // 4 {0,1}
            (false, false, true, true),    // 5 {-1,1}
            (false, false, false, true),   // 6 {-1,0}
            (true, false, false, true),    // 7 {-1,-1}
            (false, false, false, false),  // 8 {0,0}
        };

        private static bool[] Dpad(ControllerState s) => new[] { s.DpadUp, s.DpadRight, s.DpadDown, s.DpadLeft };

        private static void AssertHat(ControllerState s, int hat)
        {
            (bool up, bool right, bool down, bool left) = Hat[hat];
            Assert.Equal(new[] { up, right, down, left }, Dpad(s));
        }

        // ---- DS4 USB ----
        private static byte[] Ds4Usb(Action<byte[], int> fill)
        {
            var report = new byte[64];   // DS4_INPUT_REPORT_USB_SIZE
            report[0] = 0x01;
            fill(report, Ds4UsbCommonStart);
            return report;
        }

        [Fact]
        public void Ds4Usb_FaceButtons_AndHat_FollowTheKernelMasks()
        {
            var parser = new Ds4ReportParser();
            var faces = new (byte Mask, Func<ControllerState, bool> Read)[]
            {
                (Square, s => s.Square), (Cross, s => s.Cross), (Circle, s => s.Circle), (Triangle, s => s.Triangle),
            };

            foreach (var face in faces)
            {
                var state = new ControllerState();
                parser.Parse(Ds4Usb((r, c) => r[c + Ds4CommonButtons0] = (byte)(face.Mask | 8)), state);
                Assert.True(face.Read(state));
                Assert.Equal(1, faces.Count(f => f.Read(state)));
            }

            for (int hat = 0; hat <= 8; hat++)
            {
                var state = new ControllerState();
                int h = hat;
                parser.Parse(Ds4Usb((r, c) => r[c + Ds4CommonButtons0] = (byte)h), state);
                AssertHat(state, hat);
            }
        }

        [Fact]
        public void Ds4Usb_ShoulderAndSystemButtons_FollowTheKernelMasks()
        {
            var parser = new Ds4ReportParser();
            var cases = new (byte Mask, Func<ControllerState, bool> Read)[]
            {
                (L1, s => s.L1), (R1, s => s.R1), (L2Digital, s => s.L2Btn), (R2Digital, s => s.R2Btn),
                (Create, s => s.Share), (Options, s => s.Options), (L3, s => s.L3), (R3, s => s.R3),
            };

            foreach (var c in cases)
            {
                var state = new ControllerState();
                parser.Parse(Ds4Usb((r, start) => r[start + Ds4CommonButtons1] = c.Mask), state);
                Assert.True(c.Read(state), $"mask {c.Mask:X2}");
                Assert.Equal(1, cases.Count(x => x.Read(state)));
            }

            var ps = new ControllerState();
            parser.Parse(Ds4Usb((r, start) => r[start + Ds4CommonButtons2] = PsHome), ps);
            Assert.True(ps.PS);
            Assert.False(ps.TouchButton);

            var touch = new ControllerState();
            parser.Parse(Ds4Usb((r, start) => r[start + Ds4CommonButtons2] = TouchPad), touch);
            Assert.True(touch.TouchButton);
            Assert.False(touch.PS);
        }

        [Fact]
        public void Ds4Usb_SticksAndTriggers_AreAtTheKernelOffsets()
        {
            var state = new ControllerState();
            new Ds4ReportParser().Parse(Ds4Usb((r, c) =>
            {
                r[c + Ds4CommonX] = 11; r[c + Ds4CommonY] = 22; r[c + Ds4CommonRx] = 33; r[c + Ds4CommonRy] = 44;
                r[c + Ds4CommonZ] = 55; r[c + Ds4CommonRz] = 66;
            }), state);

            Assert.Equal(new byte[] { 11, 22, 33, 44, 55, 66 }, new[] { state.LX, state.LY, state.RX, state.RY, state.L2, state.R2 });
        }

        // ---- DS4 Bluetooth（独立した CRC で封をした 0x11 のレポートを、デバイスの読み取りのループへ流す）----
        [Fact]
        public void Ds4Bluetooth_AFrameSealedWithTheKernelCrc_IsAccepted_AndTheValuesAreAtTheKernelOffsets()
        {
            // 本番の CRC の表は、最初の使用で作られる（InitializeTable が作る）。実機では、接続直後の出力レポートが先に作る。ここでは先に作っておく
            DS4Windows.Input.Crc32Algorithm.InitializeTable(0xEDB88320);
            var transport = new FakeHidTransport(inputReportByteLength: 547, outputReportByteLength: 78, vendorId: 0x054C, productId: 0x09CC);
            var good = new byte[547];
            good[0] = 0x11;
            int c = Ds4BtCommonStart;
            good[c + Ds4CommonX] = 70;
            good[c + Ds4CommonButtons0] = (byte)(Cross | 8);
            good[c + Ds4CommonButtons1] = L1;
            good[c + Ds4CommonZ] = 200;
            good[c + Ds4CommonStatus0] = 0x05;   // 電池の容量（下位 4 ビット）
            PsReportCrc.Seal(good, Ds4BtSize);

            var bad = (byte[])good.Clone();
            bad[c + Ds4CommonX] = 99;   // 封をしたあとに書き換える ＝ CRC 不一致

            var states = RunAndCollect(new DualShock4Device(transport, "DS4", VidPidFeatureSet.NoGyroCalib), transport, good, bad);

            ControllerState only = Assert.Single(states);   // 不正な CRC のフレームは捨てられる
            Assert.Equal(70, only.LX);
            Assert.True(only.Cross);
            Assert.True(only.L1);
            Assert.Equal(200, only.L2);
        }

        // ---- DualSense（struct dualsense_input_report。USB は data[1]、BT 0x31 は data[2] から）----
        private const int DsX = 0, DsY = 1, DsRx = 2, DsRy = 3, DsZ = 4, DsRz = 5, DsButtons0 = 7, DsButtons1 = 8, DsButtons2 = 9;
        private const int DsUsbStart = 1, DsBtStart = 2;
        private const byte MicMute = 0x04;

        [Theory]
        [InlineData(DsUsbStart, 0)]
        [InlineData(DsBtStart, 1)]
        public void DualSense_Buttons_Sticks_AndTriggers_AreAtTheKernelOffsets(int start, int parserOffset)
        {
            var parser = new DualSenseReportParser(parserOffset);
            var report = new byte[78];
            report[0] = start == DsUsbStart ? (byte)0x01 : (byte)0x31;
            report[start + DsX] = 1; report[start + DsY] = 2; report[start + DsRx] = 3; report[start + DsRy] = 4;
            report[start + DsZ] = 5; report[start + DsRz] = 6;
            report[start + DsButtons0] = (byte)(Cross | 8);
            report[start + DsButtons1] = (byte)(L1 | Options);
            report[start + DsButtons2] = (byte)(PsHome | MicMute);

            var state = new ControllerState();
            parser.Parse(report, state);

            Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6 }, new[] { state.LX, state.LY, state.RX, state.RY, state.L2, state.R2 });
            Assert.True(state.Cross);
            Assert.False(state.Square || state.Circle || state.Triangle);
            Assert.True(state.L1);
            Assert.True(state.Options);
            Assert.False(state.R1 || state.Share || state.L3 || state.R3);
            Assert.True(state.PS);
            Assert.True(state.Mute);
            Assert.False(state.TouchButton);
        }

        [Theory]
        [InlineData(DsUsbStart, 0)]
        [InlineData(DsBtStart, 1)]
        public void DualSense_TheHatSwitch_FollowsTheKernelTable(int start, int parserOffset)
        {
            var parser = new DualSenseReportParser(parserOffset);
            for (int hat = 0; hat <= 8; hat++)
            {
                var report = new byte[78];
                report[start + DsButtons0] = (byte)hat;
                var state = new ControllerState();
                parser.Parse(report, state);
                AssertHat(state, hat);
            }
        }

        // ---- Switch Pro 0x30（struct joycon_input_report。button_status は 24 ビットで、ビット n は byte 3 + n / 8）----
        private const int SwButtonStatus = 3;

        // JC_BTN_*（24 ビット中のビット番号）
        private static readonly (string Name, int Bit)[] SwitchButtons =
        {
            ("Y", 0), ("X", 1), ("B", 2), ("A", 3), ("R", 6), ("ZR", 7),
            ("MINUS", 8), ("PLUS", 9), ("RSTICK", 10), ("LSTICK", 11), ("HOME", 12), ("CAP", 13),
            ("DOWN", 16), ("UP", 17), ("RIGHT", 18), ("LEFT", 19), ("L", 22), ("ZL", 23),
        };

        // このリポジトリの割り当て（Pro の A=東、B=南、X=北、Y=西 を、Circle・Cross・Triangle・Square へ）
        private static bool ReadSwitch(string name, ControllerState s) => name switch
        {
            "A" => s.Circle, "B" => s.Cross, "X" => s.Triangle, "Y" => s.Square,
            "R" => s.R1, "ZR" => s.R2Btn, "L" => s.L1, "ZL" => s.L2Btn,
            "MINUS" => s.Share, "PLUS" => s.Options, "RSTICK" => s.R3, "LSTICK" => s.L3, "HOME" => s.PS, "CAP" => s.Capture,
            "DOWN" => s.DpadDown, "UP" => s.DpadUp, "RIGHT" => s.DpadRight, "LEFT" => s.DpadLeft,
            _ => throw new ArgumentOutOfRangeException(nameof(name)),
        };

        [Fact]
        public void SwitchPro_EachButtonBit_OfTheKernelLayout_SetsExactlyThatButton()
        {
            var parser = new SwitchProReportParser();
            foreach ((string name, int bit) in SwitchButtons)
            {
                var report = new byte[362];
                report[0] = 0x30;
                report[SwButtonStatus + bit / 8] = (byte)(1 << (bit % 8));
                var state = new ControllerState();
                parser.Parse(report, state);

                Assert.True(ReadSwitch(name, state), name);
                foreach ((string other, _) in SwitchButtons)
                {
                    if (other != name) Assert.False(ReadSwitch(other, state), $"{name} を押したとき {other} が立った");
                }
            }
        }

        // ---- 共通の補助 ----
        private static List<ControllerState> RunAndCollect(InputDeviceCore device, FakeHidTransport transport, params byte[][] frames)
        {
            foreach (byte[] frame in frames) transport.EnqueueRead(HidReadStatus.Success, frame);
            device.PostInit();
            var states = new List<ControllerState>();
            device.Report += (sender, args) => states.Add(sender.GetCurrentStateRef().Clone());
            using var removed = new ManualResetEventSlim();
            device.Removal += (sender, args) => removed.Set();
            device.StartUpdate();
            Assert.True(removed.Wait(5000), "台本が尽きたら切断になるはず");
            device.StopUpdate();
            return states;
        }
    }
}
