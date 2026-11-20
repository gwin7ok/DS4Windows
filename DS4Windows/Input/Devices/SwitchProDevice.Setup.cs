using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input.Hid;
using DS4Windows.Core.Platform;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Parsers;
using DS4Windows.Input.Reports;

namespace DS4Windows.Input.Devices
{
    /// <summary>初期化の手順（サブコマンド、較正データ、初期の振動）。</summary>
    public partial class SwitchProDevice
    {
        public void SetOperational()
        {
            if (conType == ConnectionType.USB)
            {
                RunUSBSetup();
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
            Subcommand(SwitchProSubCmd.SET_LOW_POWER_STATE, powerChoiceArray, 1, checkResponse: true);

            if (enableHomeLED)
            {
                // Turn on Home light (Solid)
                byte[] light = Enumerable.Repeat((byte)0xFF, 25).ToArray();
                light[0] = 0x1F; light[1] = 0xF0;
                //Thread.Sleep(1000);
                Subcommand(0x38, light, 25, checkResponse: true);
            }

            // Turn on bottom LEDs
            byte[] leds = new byte[] { deviceSlotMask };
            //Thread.Sleep(1000);
            Subcommand(0x30, leds, 1, checkResponse: true);

            // Enable Gyro
            byte[] imuEnable = new byte[] { 0x01 };
            //Thread.Sleep(1000);
            Subcommand(0x40, imuEnable, 1, checkResponse: true);

            // Enable High Performance Gyro mode
            byte[] gyroModeBuffer = new byte[] { 0x03, 0x00, 0x00, 0x00 };
            //Thread.Sleep(1000);
            Subcommand(0x41, gyroModeBuffer, 4, checkResponse: true);

            // Enable Rumble
            byte[] rumbleEnable = new byte[] { 0x01 };
            // Disable Rumble
            //byte[] rumbleEnable = new byte[] { 0x00 };
            //Thread.Sleep(1000);
            Subcommand(0x48, rumbleEnable, 1, checkResponse: true);

            //Thread.Sleep(1000);
            EnableFastPollRate();

            // USB Connections seem to need a delay after switching input modes
            //if (conType == ConnectionType.USB)
            //{
            //    Thread.Sleep(1000);
            //}

            SetInitRumble();
            //Thread.Sleep(1000);
            CalibrationData();


            //if (connectionType == ConnectionType.USB)
            //{
            //    Thread.Sleep(300);
            //    //SetInitRumble();
            //}

            connectionOpened = true;
        }

        private void RunUSBSetup()
        {
            bool result;
            //byte[] tmpReport = new byte[INPUT_REPORT_LEN];

            byte[] modeSwitchCommand = new byte[] { 0x3F };
            Subcommand(0x03, modeSwitchCommand, 1, checkResponse: true);

            byte[] data = new byte[64];
            data[0] = 0x80; data[1] = 0x01;
            //result = hidDevice.WriteAsyncOutputReportViaInterrupt(data);
            result = hDevice.WriteOutputReportViaInterrupt(data, 0);
            //Array.Clear(tmpReport, 0 , 64);
            //res = hidDevice.ReadWithFileStream(tmpReport);
            //Console.WriteLine("TEST BYTE: {0}", tmpReport[2]);

            data[0] = 0x80; data[1] = 0x02; // USB Pairing
            //result = hidDevice.WriteOutputReportViaControl(data);
            //Thread.Sleep(2000);
            //Thread.Sleep(1000);
            result = hDevice.WriteOutputReportViaInterrupt(data, 0);

            data[0] = 0x80; data[1] = 0x03; // 3Mbit baud rate
            //result = hidDevice.WriteAsyncOutputReportViaInterrupt(data);
            result = hDevice.WriteOutputReportViaInterrupt(data, 0);
            //Thread.Sleep(2000);

            data[0] = 0x80; data[1] = 0x02; // Handshake at new baud rate
            result = hDevice.WriteOutputReportViaInterrupt(data, 0);
            //Thread.Sleep(1000);
            //result = hidDevice.WriteOutputReportViaInterrupt(command, 500);
            //Thread.Sleep(2000);

            data[0] = 0x80; data[1] = 0x4; // Prevent HID timeout
            result = hDevice.WriteOutputReportViaInterrupt(data, 0);
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

            byte[] tmpRumble = new byte[RUMBLE_REPORT_LEN];
            Array.Copy(rumble_data, 0, tmpRumble, 2, rumble_data.Length);
            tmpRumble[0] = 0x10;
            tmpRumble[1] = frameCount;
            frameCount = (byte)(++frameCount & 0x0F);

            result = hDevice.WriteOutputReportViaInterrupt(tmpRumble, 0);
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
                byte[] commandBuffer = new byte[SUBCOMMAND_BUFFER_LEN];
                Array.Copy(commandBuffHeader, 0, commandBuffer, 2, SUBCOMMAND_HEADER_LEN);
                Array.Copy(tmpBuffer, 0, commandBuffer, 11, bufLen);

                commandBuffer[0] = 0x01;
                commandBuffer[1] = frameCount;
                frameCount = (byte)(++frameCount & 0x0F);
                commandBuffer[10] = subcommand;

                result = hDevice.WriteOutputReportViaInterrupt(commandBuffer, 0);

                tmpReport = null;
                if (result && checkResponse)
                {
                    tmpReport = new byte[INPUT_REPORT_LEN];
                    HidReadStatus res;
                    res = hDevice.ReadFile(tmpReport, SUBCOMMAND_RESPONSE_TIMEOUT);
                    int tries = 1;
                    while (res == HidReadStatus.Success &&
                        tmpReport[0] != 0x21 && tmpReport[14] != subcommand && tries < 100)
                    {
                        //Console.WriteLine("TRY AGAIN: {0}", tmpReport[0]);
                        res = hDevice.ReadFile(tmpReport, SUBCOMMAND_RESPONSE_TIMEOUT);
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

        public void PrepareRumbleData(byte[] buffer)
        {
            buffer[0] = 0x10;
            buffer[1] = frameCount;
            frameCount = (byte)(++frameCount & 0x0F);

            SwitchProRumbleEncoder.EncodeAmplitudes(buffer, currentLeftAmpRatio, currentRightAmpRatio);
        }

        public void CalibrationData()
        {
            byte[] command;
            byte[]? tmpBuffer;

            //command = new byte[] { 0x00, 0x50, 0x00, 0x00, 0x01 };
            //tmpBuffer = Subcommand(SwitchProSubCmd.SPI_FLASH_READ, command, 5, checkResponse: true);
            //Console.WriteLine("THE POWER");
            //Console.WriteLine(string.Join(",", tmpBuffer));
            //Console.WriteLine(tmpBuffer[SPI_RESP_OFFSET]);
            //Console.WriteLine();

            bool foundUserCalib = false;
            command = new byte[] { 0x10, 0x80, 0x00, 0x00, 0x02 };
            tmpBuffer = Subcommand(0x10, command, 5, checkResponse: true);
            if (SwitchProCalibrationDecoder.HasUserCalibrationMark(tmpBuffer))
            {
                foundUserCalib = true;
            }

            if (foundUserCalib)
            {
                command = new byte[] { 0x12, 0x80, 0x00, 0x00, 0x09 };
                tmpBuffer = Subcommand(0x10, command, 5, checkResponse: true);
                //Console.WriteLine("FOUND USER CALIB");
            }
            else
            {
                command = new byte[] { 0x3D, 0x60, 0x00, 0x00, 0x09 };
                tmpBuffer = Subcommand(0x10, command, 5, checkResponse: true);
                //Console.WriteLine("CHECK FACTORY CALIB");
            }

            SwitchProCalibrationDecoder.ReadLeftStick(tmpBuffer, leftStickCalib);

            SwitchProCalibrationDecoder.BuildStickAxes(leftStickCalib, foundUserCalib, ref leftStickXData, ref leftStickYData);

            //leftStickOffsetX = leftStickCalib[2];
            //leftStickOffsetY = leftStickCalib[3];

            //Console.WriteLine(string.Join(",", tmpBuffer));
            //Console.WriteLine();
            //Console.WriteLine(string.Join(",", leftStickCalib));

            foundUserCalib = false;
            command = new byte[] { 0x1B, 0x80, 0x00, 0x00, 0x02 };
            tmpBuffer = Subcommand(0x10, command, 5, checkResponse: true);
            if (SwitchProCalibrationDecoder.HasUserCalibrationMark(tmpBuffer))
            {
                foundUserCalib = true;
            }

            if (foundUserCalib)
            {
                command = new byte[] { 0x1D, 0x80, 0x00, 0x00, 0x09 };
                tmpBuffer = Subcommand(0x10, command, 5, checkResponse: true);
                //Console.WriteLine("FOUND RIGHT USER CALIB");
            }
            else
            {
                command = new byte[] { 0x46, 0x60, 0x00, 0x00, 0x09 };
                tmpBuffer = Subcommand(0x10, command, 5, checkResponse: true);
                //Console.WriteLine("CHECK RIGHT FACTORY CALIB");
            }

            SwitchProCalibrationDecoder.ReadRightStick(tmpBuffer, rightStickCalib);

            SwitchProCalibrationDecoder.BuildStickAxes(rightStickCalib, foundUserCalib, ref rightStickXData, ref rightStickYData);

            //rightStickOffsetX = rightStickCalib[2];
            //rightStickOffsetY = rightStickCalib[5];

            //Console.WriteLine(string.Join(",", tmpBuffer));
            //Console.WriteLine();
            //Console.WriteLine(string.Join(",", rightStickCalib));

            /*
            // Grab Factory LS Dead Zone
            command = new byte[] { 0x86, 0x60, 0x00, 0x00, 0x10 };
            byte[] deadZoneBuffer = Subcommand(0x10, command, 5, checkResponse: true);
            deadzoneLS = (ushort)((deadZoneBuffer[4 + SPI_RESP_OFFSET] << 8) & 0xF00 | deadZoneBuffer[3 + SPI_RESP_OFFSET]);
            //Console.WriteLine("DZ Left: {0}", deadzoneLS);
            //Console.WriteLine(string.Join(",", deadZoneBuffer));

            // Grab Factory RS Dead Zone
            command = new byte[] { 0x98, 0x60, 0x00, 0x00, 0x10 };
            deadZoneBuffer = Subcommand(0x10, command, 5, checkResponse: true);
            deadzoneRS = (ushort)((deadZoneBuffer[4 + SPI_RESP_OFFSET] << 8) & 0xF00 | deadZoneBuffer[3 + SPI_RESP_OFFSET]);
            //Console.WriteLine("DZ Right: {0}", deadzoneRS);
            //Console.WriteLine(string.Join(",", deadZoneBuffer));*/

            foundUserCalib = false;
            command = new byte[] { 0x26, 0x80, 0x00, 0x00, 0x02 };
            tmpBuffer = Subcommand(0x10, command, 5, checkResponse: true);
            if (SwitchProCalibrationDecoder.HasUserCalibrationMark(tmpBuffer))
            {
                foundUserCalib = true;
            }

            //Console.WriteLine("{0}", string.Join(",", tmpBuffer.Skip(offset).ToArray()));
            if (foundUserCalib)
            {
                command = new byte[] { 0x28, 0x80, 0x00, 0x00, 0x18 };
                tmpBuffer = Subcommand(0x10, command, 5, checkResponse: true);
                //Console.WriteLine("FOUND USER CALIB");
            }
            else
            {
                command = new byte[] { 0x20, 0x60, 0x00, 0x00, 0x18 };
                tmpBuffer = Subcommand(0x10, command, 5, checkResponse: true);
                //Console.WriteLine("CHECK FACTORY CALIB");
            }

            SwitchProCalibrationDecoder.ReadImu(tmpBuffer, accelNeutral, accelSens, accelCoeff, gyroBias, gyroSens, gyroCoeff);
        }
    }
}
