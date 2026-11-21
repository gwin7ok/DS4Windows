#nullable enable

using System;
using System.Linq;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Parsers;

namespace DS4Windows.Input.Devices
{
    /// <summary>
    /// Switch Pro の接続の手順: 通常の動作へ移すサブコマンドの列（<see cref="SetOperational"/>）、較正の読み出し（<see cref="ReadCalibration"/>）、
    /// 切断前の後始末（<see cref="Shutdown"/>）。状態は持たず、通信は <see cref="SwitchProCommandChannel"/>、結果は <see cref="SwitchProCalibration"/> へ置く。
    /// 手順の順序・内容は、実機との互換のため、変えない（<c>SwitchProDeviceBehaviorTests</c> が送るバイト列の列を固定している）。
    /// </summary>
    public sealed class SwitchProSession
    {
        private readonly SwitchProCommandChannel _channel;
        private readonly SwitchProCalibration _calibration;
        private readonly IHidTransport _hid;

        public SwitchProSession(SwitchProCommandChannel channel, SwitchProCalibration calibration, IHidTransport hid)
        {
            ArgumentNullException.ThrowIfNull(channel);
            ArgumentNullException.ThrowIfNull(calibration);
            ArgumentNullException.ThrowIfNull(hid);
            _channel = channel;
            _calibration = calibration;
            _hid = hid;
        }

        public void SetOperational(ConnectionType conType, bool enableHomeLed, byte deviceSlotMask)
        {
            if (conType == ConnectionType.USB)
            {
                _channel.RunUsbSetup();
                //Thread.Sleep(500);
            }

            //Thread.Sleep(1000);

            //EnableFastPollRate();

            //Thread.Sleep(5000);
            //byte[] tmpReport = new byte[INPUT_REPORT_LEN];
            //byte[] command2 = new byte[SUBCOMMAND_BUFFER_LEN];
            //bool result;
            //HidReadStatus res;

            // Set device to normal power state
            byte[] powerChoiceArray = new byte[] { 0x00 };
            _channel.Subcommand(SwitchProDevice.SwitchProSubCmd.SET_LOW_POWER_STATE, powerChoiceArray, 1, checkResponse: true);

            if (enableHomeLed)
            {
                // Turn on Home light (Solid)
                byte[] light = Enumerable.Repeat((byte)0xFF, 25).ToArray();
                light[0] = 0x1F; light[1] = 0xF0;
                //Thread.Sleep(1000);
                _channel.Subcommand(0x38, light, 25, checkResponse: true);
            }

            // Turn on bottom LEDs
            byte[] leds = new byte[] { deviceSlotMask };
            //Thread.Sleep(1000);
            _channel.Subcommand(0x30, leds, 1, checkResponse: true);

            // Enable Gyro
            byte[] imuEnable = new byte[] { 0x01 };
            //Thread.Sleep(1000);
            _channel.Subcommand(0x40, imuEnable, 1, checkResponse: true);

            // Enable High Performance Gyro mode
            byte[] gyroModeBuffer = new byte[] { 0x03, 0x00, 0x00, 0x00 };
            //Thread.Sleep(1000);
            _channel.Subcommand(0x41, gyroModeBuffer, 4, checkResponse: true);

            // Enable Rumble
            byte[] rumbleEnable = new byte[] { 0x01 };
            // Disable Rumble
            //byte[] rumbleEnable = new byte[] { 0x00 };
            //Thread.Sleep(1000);
            _channel.Subcommand(0x48, rumbleEnable, 1, checkResponse: true);

            //Thread.Sleep(1000);
            EnableFastPollRate();

            // USB Connections seem to need a delay after switching input modes
            //if (conType == ConnectionType.USB)
            //{
            //    Thread.Sleep(1000);
            //}

            _channel.SetInitRumble();
            //Thread.Sleep(1000);
            ReadCalibration();


            //if (connectionType == ConnectionType.USB)
            //{
            //    Thread.Sleep(300);
            //    //_channel.SetInitRumble();
            //}

        }

        private void EnableFastPollRate()
        {
            // 最速の送信間隔にする
            byte[] tempArray = new byte[] { 0x30 };
            _channel.Subcommand(0x03, tempArray, 1, checkResponse: true);
        }

        /// <summary>切断の前に、ジャイロと振動を止め、低電力の状態へ戻す（USB は、ハンドシェイクも閉じる）。</summary>
        public void Shutdown(ConnectionType conType)
        {
            // Disable Gyro
            byte[] tmpOffBuffer = new byte[] { 0x0 };
            _channel.Subcommand(0x40, tmpOffBuffer, 1, checkResponse: true);

            tmpOffBuffer = new byte[] { 0x0 };
            _channel.Subcommand(0x48, tmpOffBuffer, 1, checkResponse: true);

            // Revert back to low power state
            byte[] powerChoiceArray = new byte[] { 0x01 };
            _channel.Subcommand(SwitchProDevice.SwitchProSubCmd.SET_LOW_POWER_STATE, powerChoiceArray, 1, checkResponse: true);

            if (conType == ConnectionType.USB)
            {
                byte[] data = new byte[64];
                data[0] = 0x80; data[1] = 0x05;
                _hid.WriteOutputReportViaControl(data);

                data[0] = 0x80; data[1] = 0x06;
                _hid.WriteOutputReportViaControl(data);
            }
        }

        public void ReadCalibration()
        {
            byte[] command;
            byte[]? tmpBuffer;

            //command = new byte[] { 0x00, 0x50, 0x00, 0x00, 0x01 };
            //tmpBuffer = _channel.Subcommand(SwitchProSubCmd.SPI_FLASH_READ, command, 5, checkResponse: true);
            //Console.WriteLine("THE POWER");
            //Console.WriteLine(string.Join(",", tmpBuffer));
            //Console.WriteLine(tmpBuffer[SPI_RESP_OFFSET]);
            //Console.WriteLine();

            bool foundUserCalib = false;
            command = new byte[] { 0x10, 0x80, 0x00, 0x00, 0x02 };
            tmpBuffer = _channel.Subcommand(0x10, command, 5, checkResponse: true);
            if (SwitchProCalibrationDecoder.HasUserCalibrationMark(tmpBuffer))
            {
                foundUserCalib = true;
            }

            if (foundUserCalib)
            {
                command = new byte[] { 0x12, 0x80, 0x00, 0x00, 0x09 };
                tmpBuffer = _channel.Subcommand(0x10, command, 5, checkResponse: true);
                //Console.WriteLine("FOUND USER CALIB");
            }
            else
            {
                command = new byte[] { 0x3D, 0x60, 0x00, 0x00, 0x09 };
                tmpBuffer = _channel.Subcommand(0x10, command, 5, checkResponse: true);
                //Console.WriteLine("CHECK FACTORY CALIB");
            }

            SwitchProCalibrationDecoder.ReadLeftStick(tmpBuffer, _calibration.LeftStickCalib);

            SwitchProCalibrationDecoder.BuildStickAxes(_calibration.LeftStickCalib, foundUserCalib, ref _calibration.LeftStickX, ref _calibration.LeftStickY);

            //leftStickOffsetX = _calibration.LeftStickCalib[2];
            //leftStickOffsetY = _calibration.LeftStickCalib[3];

            //Console.WriteLine(string.Join(",", tmpBuffer));
            //Console.WriteLine();
            //Console.WriteLine(string.Join(",", _calibration.LeftStickCalib));

            foundUserCalib = false;
            command = new byte[] { 0x1B, 0x80, 0x00, 0x00, 0x02 };
            tmpBuffer = _channel.Subcommand(0x10, command, 5, checkResponse: true);
            if (SwitchProCalibrationDecoder.HasUserCalibrationMark(tmpBuffer))
            {
                foundUserCalib = true;
            }

            if (foundUserCalib)
            {
                command = new byte[] { 0x1D, 0x80, 0x00, 0x00, 0x09 };
                tmpBuffer = _channel.Subcommand(0x10, command, 5, checkResponse: true);
                //Console.WriteLine("FOUND RIGHT USER CALIB");
            }
            else
            {
                command = new byte[] { 0x46, 0x60, 0x00, 0x00, 0x09 };
                tmpBuffer = _channel.Subcommand(0x10, command, 5, checkResponse: true);
                //Console.WriteLine("CHECK RIGHT FACTORY CALIB");
            }

            SwitchProCalibrationDecoder.ReadRightStick(tmpBuffer, _calibration.RightStickCalib);

            SwitchProCalibrationDecoder.BuildStickAxes(_calibration.RightStickCalib, foundUserCalib, ref _calibration.RightStickX, ref _calibration.RightStickY);

            //rightStickOffsetX = _calibration.RightStickCalib[2];
            //rightStickOffsetY = _calibration.RightStickCalib[5];

            //Console.WriteLine(string.Join(",", tmpBuffer));
            //Console.WriteLine();
            //Console.WriteLine(string.Join(",", _calibration.RightStickCalib));

            /*
            // Grab Factory LS Dead Zone
            command = new byte[] { 0x86, 0x60, 0x00, 0x00, 0x10 };
            byte[] deadZoneBuffer = _channel.Subcommand(0x10, command, 5, checkResponse: true);
            deadzoneLS = (ushort)((deadZoneBuffer[4 + SPI_RESP_OFFSET] << 8) & 0xF00 | deadZoneBuffer[3 + SPI_RESP_OFFSET]);
            //Console.WriteLine("DZ Left: {0}", deadzoneLS);
            //Console.WriteLine(string.Join(",", deadZoneBuffer));

            // Grab Factory RS Dead Zone
            command = new byte[] { 0x98, 0x60, 0x00, 0x00, 0x10 };
            deadZoneBuffer = _channel.Subcommand(0x10, command, 5, checkResponse: true);
            deadzoneRS = (ushort)((deadZoneBuffer[4 + SPI_RESP_OFFSET] << 8) & 0xF00 | deadZoneBuffer[3 + SPI_RESP_OFFSET]);
            //Console.WriteLine("DZ Right: {0}", deadzoneRS);
            //Console.WriteLine(string.Join(",", deadZoneBuffer));*/

            foundUserCalib = false;
            command = new byte[] { 0x26, 0x80, 0x00, 0x00, 0x02 };
            tmpBuffer = _channel.Subcommand(0x10, command, 5, checkResponse: true);
            if (SwitchProCalibrationDecoder.HasUserCalibrationMark(tmpBuffer))
            {
                foundUserCalib = true;
            }

            //Console.WriteLine("{0}", string.Join(",", tmpBuffer.Skip(offset).ToArray()));
            if (foundUserCalib)
            {
                command = new byte[] { 0x28, 0x80, 0x00, 0x00, 0x18 };
                tmpBuffer = _channel.Subcommand(0x10, command, 5, checkResponse: true);
                //Console.WriteLine("FOUND USER CALIB");
            }
            else
            {
                command = new byte[] { 0x20, 0x60, 0x00, 0x00, 0x18 };
                tmpBuffer = _channel.Subcommand(0x10, command, 5, checkResponse: true);
                //Console.WriteLine("CHECK FACTORY CALIB");
            }

            SwitchProCalibrationDecoder.ReadImu(tmpBuffer, _calibration.AccelNeutral, _calibration.AccelSens, _calibration.AccelCoeff, _calibration.GyroBias, _calibration.GyroSens, _calibration.GyroCoeff);
        }    }
}
