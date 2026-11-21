#nullable enable

using System;
using System.Linq;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Hid;
using DS4Windows.Input.Parsers;
using DS4Windows.Input.Reports;

namespace DS4Windows.Input.Devices
{
    /// <summary>
    /// Switch Pro への書き込みの経路。サブコマンドの送信と応答の待ち、USB の起動手順、振動のレポートの組み立てを持ち、
    /// 送信ごとに 0〜15 で一周するフレーム番号（<see cref="FrameCount"/>）の持ち主になる。
    /// 読み取りのループとは別のスレッド（接続の開始・切断）から使われるが、同時には使われない。
    /// </summary>
    public sealed class SwitchProCommandChannel
    {
        internal const int InputReportLength = 362;
        internal const int RumbleReportLength = 64;
        private const int SubcommandHeaderLength = 8;
        private const int SubcommandBufferLength = 64;
        private const int SubcommandResponseTimeout = 500;

        private static readonly byte[] CommandHeader =
            { 0x0, 0x1, 0x40, 0x40, 0x0, 0x1, 0x40, 0x40 };

        private readonly IHidTransport _hid;

        public SwitchProCommandChannel(IHidTransport hid)
        {
            ArgumentNullException.ThrowIfNull(hid);
            _hid = hid;
        }

        /// <summary>次に送るレポートへ入れるフレーム番号（0〜15）。</summary>
        public byte FrameCount { get; set; }

        public void RunUsbSetup()
        {
            bool result;
            //byte[] tmpReport = new byte[InputReportLength];

            byte[] modeSwitchCommand = new byte[] { 0x3F };
            Subcommand(0x03, modeSwitchCommand, 1, checkResponse: true);

            byte[] data = new byte[64];
            data[0] = 0x80; data[1] = 0x01;
            //result = hidDevice.WriteAsyncOutputReportViaInterrupt(data);
            result = _hid.WriteOutputReportViaInterrupt(data, 0);
            //Array.Clear(tmpReport, 0 , 64);
            //res = hidDevice.ReadWithFileStream(tmpReport);
            //Console.WriteLine("TEST BYTE: {0}", tmpReport[2]);

            data[0] = 0x80; data[1] = 0x02; // USB Pairing
            //result = hidDevice.WriteOutputReportViaControl(data);
            //Thread.Sleep(2000);
            //Thread.Sleep(1000);
            result = _hid.WriteOutputReportViaInterrupt(data, 0);

            data[0] = 0x80; data[1] = 0x03; // 3Mbit baud rate
            //result = hidDevice.WriteAsyncOutputReportViaInterrupt(data);
            result = _hid.WriteOutputReportViaInterrupt(data, 0);
            //Thread.Sleep(2000);

            data[0] = 0x80; data[1] = 0x02; // Handshake at new baud rate
            result = _hid.WriteOutputReportViaInterrupt(data, 0);
            //Thread.Sleep(1000);
            //result = hidDevice.WriteOutputReportViaInterrupt(command, 500);
            //Thread.Sleep(2000);

            data[0] = 0x80; data[1] = 0x4; // Prevent HID timeout
            result = _hid.WriteOutputReportViaInterrupt(data, 0);
            //result = hidDevice.WriteOutputReportViaInterrupt(command, 500);
        }

        public void SetInitRumble()
        {
            bool result;
            //HidReadStatus res;
            //byte[] tmpReport = new byte[64];
            byte[] rumble_data = new byte[8];
            rumble_data[0] = 0x0;
            rumble_data[1] = 0x1;
            rumble_data[2] = 0x40;
            rumble_data[3] = 0x40;

            for (int i = 0; i < 4; i++)
            {
                rumble_data[4 + i] = rumble_data[i];
            }

            byte[] tmpRumble = new byte[RumbleReportLength];
            Array.Copy(rumble_data, 0, tmpRumble, 2, rumble_data.Length);
            tmpRumble[0] = 0x10;
            tmpRumble[1] = FrameCount;
            FrameCount = (byte)(++FrameCount & 0x0F);

            result = _hid.WriteOutputReportViaInterrupt(tmpRumble, 0);
            //res = hidDevice.ReadWithFileStream(tmpReport, 500);
            //res = hidDevice.ReadFile(tmpReport);
        }

        public byte[]? Subcommand(byte subcommand, byte[] tmpBuffer, uint bufLen,
            bool checkResponse = false)
        {
            int retryLimit = 100;
            byte[]? tmpReport;

            do
            {
                bool result;
                byte[] commandBuffer = new byte[SubcommandBufferLength];
                Array.Copy(CommandHeader, 0, commandBuffer, 2, SubcommandHeaderLength);
                Array.Copy(tmpBuffer, 0, commandBuffer, 11, bufLen);

                commandBuffer[0] = 0x01;
                commandBuffer[1] = FrameCount;
                FrameCount = (byte)(++FrameCount & 0x0F);
                commandBuffer[10] = subcommand;

                result = _hid.WriteOutputReportViaInterrupt(commandBuffer, 0);

                tmpReport = null;
                if (result && checkResponse)
                {
                    tmpReport = new byte[InputReportLength];
                    HidReadStatus res;
                    res = _hid.ReadFile(tmpReport, SubcommandResponseTimeout);
                    int tries = 1;
                    while (res == HidReadStatus.Success &&
                        tmpReport[0] != 0x21 && tmpReport[14] != subcommand && tries < 100)
                    {
                        //Console.WriteLine("TRY AGAIN: {0}", tmpReport[0]);
                        res = _hid.ReadFile(tmpReport, SubcommandResponseTimeout);
                        tries++;
                    }

                    //Console.WriteLine("END GAME: {0} {1} {2}", subcommand, tmpReport[0], tries);
                }
            } while (ReloadStickCalib(subcommand, tmpBuffer, tmpReport, ref retryLimit));

            return tmpReport;
        }

        private bool ReloadStickCalib(byte subcommand, ReadOnlySpan<byte> tmpBuffer, ReadOnlySpan<byte> tmpReport, ref int retryLimit)
        {
            if (subcommand != 0x10) { return false; }
            if (retryLimit-- <= 0) { return false; }

            if (tmpBuffer.SequenceEqual<byte>([0x3D, 0x60, 0x00, 0x00, 0x09]) || // LEFT  STICK FACTORY CALIB
                tmpBuffer.SequenceEqual<byte>([0x46, 0x60, 0x00, 0x00, 0x09]) || // RIGHT STICK FACTORY CALIB
                tmpBuffer.SequenceEqual<byte>([0x12, 0x80, 0x00, 0x00, 0x09]) || // LEFT  STICK USER    CALIB
                tmpBuffer.SequenceEqual<byte>([0x1D, 0x80, 0x00, 0x00, 0x09]))   // RIGHT STICK USER    CALIB
            {
                var stickCalib = new ushort[6];
                SwitchProCalibrationDecoder.ReadLeftStick(tmpReport, stickCalib);

                return stickCalib.Any(item => item == 0);
            }
            else
            {
                return false;
            }
        }

        public void PrepareRumbleData(byte[] buffer, double leftAmpRatio, double rightAmpRatio)
        {
            buffer[0] = 0x10;
            buffer[1] = FrameCount;
            FrameCount = (byte)(++FrameCount & 0x0F);

            SwitchProRumbleEncoder.EncodeAmplitudes(buffer, leftAmpRatio, rightAmpRatio);
        }
    }
}
