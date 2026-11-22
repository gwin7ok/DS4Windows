#nullable enable
using System;
using System.Linq;
using System.Threading;
using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Devices;
using DS4Windows.Input.Hid;
using DS4Windows.Input.Xbox;

namespace DS4WindowsTests
{
    /// <summary>
    /// 入力機種（カートリッジ）ごとの仮想コントローラー。<see cref="PadInput"/> を、その機種のレポートへ変換し、
    /// 偽の通信路（HID は <see cref="FakeHidTransport"/>、Xbox は <see cref="FakeXInputApi"/>）に載せた実際のデバイスを作る。
    /// デバイスは本番の実装（読み取りのループ、パーサー）で、通信路だけが偽物。<see cref="Open"/> が返すデバイスは初期化（PostInit）済みで、まだ開始していない。
    /// 新しい機種は、このクラスを継承して 1 つ足し、<see cref="All"/> へ載せれば、<c>VirtualPadContractTests</c> の全部の検査に載る。
    /// </summary>
    internal abstract class VirtualPad
    {
        public abstract string Name { get; }

        public abstract InputDeviceType DeviceType { get; }

        /// <summary>この機種が、トリガーをアナログで持つか（false なら、押した・押さないの 0／255）。</summary>
        public virtual bool AnalogTriggers => true;

        /// <summary>
        /// <paramref name="held"/> を押し続けた状態のデバイスを作る（初期化済み。開始はしない）。
        /// Xbox を除き、入力は 1 フレームだけで、そのあと切断になる。
        /// </summary>
        public abstract VirtualPadDevice Open(PadInput held);

        public override string ToString() => Name;

        /// <summary>デバイスを開始し、最初のレポートの状態を（複製して）返し、止める。</summary>
        public ControllerState ReadFirstState(PadInput held)
        {
            using VirtualPadDevice pad = Open(held);
            using var reported = new ManualResetEventSlim();
            ControllerState? first = null;
            pad.Device.Report += (sender, args) =>
            {
                if (first != null) return;
                first = sender.GetCurrentStateRef().Clone();
                reported.Set();
            };
            pad.Device.StartUpdate();
            try
            {
                if (!reported.Wait(8000)) throw new TimeoutException(Name + ": レポートが届かなかった");
            }
            finally
            {
                pad.Device.StopUpdate();
            }

            return first!;
        }

        /// <summary>DS4（USB）。</summary>
        public static VirtualPad Ds4 { get; } = new Ds4Pad();

        /// <summary>DualSense（USB）。</summary>
        public static VirtualPad DualSense { get; } = new DualSensePad();

        /// <summary>Switch Pro（USB）。</summary>
        public static VirtualPad SwitchPro { get; } = new SwitchProPad();

        /// <summary>Xbox（XInput）。</summary>
        public static VirtualPad Xbox { get; } = new XboxPad();

        public static VirtualPad[] All { get; } = { Ds4, DualSense, SwitchPro, Xbox };

        // ---- DS4 / DualSense の共通 ----

        private static byte SonyFace(PadInput i)
        {
            byte b = (byte)(i.DpadUp ? 0 : 8);   // 下位 4 ビットは方向キー（8 が中立、0 が上）
            if (i.Square) b |= 0x10;
            if (i.Cross) b |= 0x20;
            if (i.Circle) b |= 0x40;
            if (i.Triangle) b |= 0x80;
            return b;
        }

        private static byte SonyShoulder(PadInput i)
        {
            byte b = 0;
            if (i.L1) b |= 0x01;
            if (i.R1) b |= 0x02;
            if (i.L2Pressed) b |= 0x04;
            if (i.R2Pressed) b |= 0x08;
            return b;
        }

        private sealed class Ds4Pad : VirtualPad
        {
            public override string Name => "DS4 (USB)";
            public override InputDeviceType DeviceType => InputDeviceType.DS4;

            public override VirtualPadDevice Open(PadInput i)
            {
                var transport = new FakeHidTransport(inputReportByteLength: 64, outputReportByteLength: 32, vendorId: 0x054C, productId: 0x09CC);
                var f = new byte[64];
                f[0] = 0x01;
                f[1] = i.LX; f[2] = i.LY; f[3] = i.RX; f[4] = i.RY;
                f[5] = SonyFace(i);
                f[6] = SonyShoulder(i);
                f[8] = (byte)(i.L2Pressed ? 255 : 0);
                f[9] = (byte)(i.R2Pressed ? 255 : 0);
                f[10] = 0x10;   // タイムスタンプ
                f[30] = 0x18;   // 電池
                transport.EnqueueRead(HidReadStatus.Success, f);
                var device = new DualShock4Device(transport, "DS4", VidPidFeatureSet.NoGyroCalib);
                device.PostInit();
                return new VirtualPadDevice(device);
            }
        }

        private sealed class DualSensePad : VirtualPad
        {
            public override string Name => "DualSense (USB)";
            public override InputDeviceType DeviceType => InputDeviceType.DualSense;

            public override VirtualPadDevice Open(PadInput i)
            {
                var transport = new FakeHidTransport(inputReportByteLength: 64, outputReportByteLength: 48, vendorId: 0x054C, productId: 0x0CE6);
                var f = new byte[64];
                f[0] = 0x01;
                f[1] = i.LX; f[2] = i.LY; f[3] = i.RX; f[4] = i.RY;
                f[5] = (byte)(i.L2Pressed ? 255 : 0);
                f[6] = (byte)(i.R2Pressed ? 255 : 0);
                f[8] = SonyFace(i);
                f[9] = SonyShoulder(i);
                f[28] = 0xB8; f[29] = 0x0B;   // タイムスタンプ
                f[53] = 0x08;   // 電池
                f[54] = 0x08;   // 充電
                transport.EnqueueRead(HidReadStatus.Success, f);
                var device = new DualSenseDevice(transport, "DualSense", VidPidFeatureSet.NoGyroCalib);
                device.PostInit();
                return new VirtualPadDevice(device);
            }
        }

        private sealed class SwitchProPad : VirtualPad
        {
            public override string Name => "Switch Pro (USB)";
            public override InputDeviceType DeviceType => InputDeviceType.SwitchPro;
            public override bool AnalogTriggers => false;

            private static byte[] Response(byte subcommand, ushort[]? calib = null)
            {
                var f = new byte[362];
                f[0] = 0x21;
                f[14] = subcommand;
                if (calib != null)
                {
                    for (int n = 0; n < 3; n++)
                    {
                        ushort x = calib[n * 2], y = calib[n * 2 + 1];
                        f[20 + n * 3] = (byte)x;
                        f[20 + n * 3 + 1] = (byte)(((x >> 8) & 0x0F) | ((y & 0x0F) << 4));
                        f[20 + n * 3 + 2] = (byte)(y >> 4);
                    }
                }

                return f;
            }

            // 0〜255 を 12bit へ。校正の工場値（中心 2048、片側 1500）に対し、2048 を中心にして端まで振る
            private static ushort Axis(byte v) => (ushort)Math.Clamp(2048 + (v - 128) * 11, 0, 4095);

            public override VirtualPadDevice Open(PadInput i)
            {
                var transport = new FakeHidTransport(inputReportByteLength: 362, outputReportByteLength: 49, vendorId: 0x057E, productId: 0x2009) { ReadDelayMs = 12 };
                for (int n = 0; n < 8; n++) transport.EnqueueRead(HidReadStatus.Success, Response(0x03));   // 初期化の応答
                var calib = new ushort[] { 1500, 1500, 2048, 2048, 1500, 1500 };
                transport.EnqueueRead(HidReadStatus.Success, Response(0x10));
                transport.EnqueueRead(HidReadStatus.Success, Response(0x10, calib));
                transport.EnqueueRead(HidReadStatus.Success, Response(0x10));
                // 右スティックは、並びが左と違う（中心、下側、上側）。左と同じ値を渡すと中心がずれる
                transport.EnqueueRead(HidReadStatus.Success, Response(0x10, new ushort[] { 2048, 2048, 1500, 1500, 1500, 1500 }));
                transport.EnqueueRead(HidReadStatus.Success, Response(0x10));
                transport.EnqueueRead(HidReadStatus.Success, Response(0x10));

                var f = new byte[362];
                f[0] = 0x30;
                f[2] = 0x80;   // 電池
                byte b3 = 0, b5 = 0;
                if (i.Circle) b3 |= 0x08;
                if (i.Cross) b3 |= 0x04;
                if (i.Triangle) b3 |= 0x02;
                if (i.Square) b3 |= 0x01;
                if (i.R1) b3 |= 0x40;
                if (i.R2Pressed) b3 |= 0x80;
                if (i.DpadUp) b5 |= 0x02;
                if (i.L1) b5 |= 0x40;
                if (i.L2Pressed) b5 |= 0x80;
                f[3] = b3;
                f[5] = b5;
                // Switch Pro は上を大きくするため、Y は反転する
                SetStick(f, 6, Axis(i.LX), Axis((byte)(255 - i.LY)));
                SetStick(f, 9, Axis(i.RX), Axis((byte)(255 - i.RY)));
                transport.EnqueueRead(HidReadStatus.Success, f);

                var device = new SwitchProDevice(transport, "Switch Pro", VidPidFeatureSet.DefaultDS4);
                device.PostInit();
                return new VirtualPadDevice(device);
            }

            private static void SetStick(byte[] f, int o, ushort x, ushort y)
            {
                f[o] = (byte)x;
                f[o + 1] = (byte)(((x >> 8) & 0x0F) | ((y & 0x0F) << 4));
                f[o + 2] = (byte)(y >> 4);
            }
        }

        private sealed class XboxPad : VirtualPad
        {
            private const string Path = @"\\?\usb#vid_045e&pid_028e#virtualpad1#{ec87f1e3-c13b-4100-b5f7-8b84d54260cb}";

            public override string Name => "Xbox (XInput)";
            public override InputDeviceType DeviceType => InputDeviceType.Xbox;

            private static short Axis(byte v) => (short)Math.Clamp((v - 128) * 256, -32767, 32767);

            public override VirtualPadDevice Open(PadInput i)
            {
                var api = new FakeXInputApi();
                var interfaces = new FakeXusbInterfaceSource();
                interfaces.Add(Path, 0x045E, 0x028E, 0);
                ushort buttons = 0;
                if (i.Cross) buttons |= XInputReportLayout.A;
                if (i.Circle) buttons |= XInputReportLayout.B;
                if (i.Square) buttons |= XInputReportLayout.X;
                if (i.Triangle) buttons |= XInputReportLayout.Y;
                if (i.L1) buttons |= XInputReportLayout.LeftShoulder;
                if (i.R1) buttons |= XInputReportLayout.RightShoulder;
                if (i.DpadUp) buttons |= XInputReportLayout.DpadUp;
                // XInput は Y が上を正にするため、DS4 の並び（上が 0）から反転する
                api.Set(0, new XInputSnapshot(1, buttons, (byte)(i.L2Pressed ? 255 : 0), (byte)(i.R2Pressed ? 255 : 0),
                    Axis(i.LX), (short)-Axis(i.LY), Axis(i.RX), (short)-Axis(i.RY)));
                var candidate = new XInputTransportSource(api, interfaces).Enumerate().Single();
                InputDeviceCore device = new XboxDeviceCartridge().Create(InputDeviceType.Xbox, candidate.Transport, candidate.Info.name, candidate.Info.featureSet);
                device.PostInit();
                return new VirtualPadDevice(device);
            }
        }
    }
}
