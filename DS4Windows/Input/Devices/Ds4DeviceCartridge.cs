using System.Collections.Generic;
using static DS4Windows.Input.DS4Devices;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;
using DS4Windows.Input.Hid;

namespace DS4Windows.Input.Devices
{
    /// <summary>
    /// DS4 系（DS4 と互換機。DS4 の USB／Bluetooth／Sony ワイヤレスアダプター）のカートリッジ。対応する VID／PID の表と、デバイスの生成を持つ。
    /// 表は、<c>DS4Devices</c> の対応機器の表から、この種類の行をそのまま（順序・コメントとも）移したもの。
    /// </summary>
    public sealed class Ds4DeviceCartridge : IInputDeviceCartridge
    {
        private static readonly VidPidInfo[] knownDevices =
        {
            new VidPidInfo(SONY_VID, 0xBA0, "Sony WA", InputDeviceType.DS4),
            new VidPidInfo(SONY_VID, 0x5C4, "DS4 v.1"),
            new VidPidInfo(SONY_VID, 0x09CC, "DS4 v.2", InputDeviceType.DS4),
            new VidPidInfo(RAZER_VID, 0x1000, "Razer Raiju PS4"),
            new VidPidInfo(RAZER_VID, 0x1100, "Razer Raion Fightpad PS4", InputDeviceType.DS4, VidPidFeatureSet.NoGyroCalib),
            new VidPidInfo(NACON_VID, 0x0D01, "Nacon Revol Pro v.1", InputDeviceType.DS4, VidPidFeatureSet.NoGyroCalib), // Nacon Revolution Pro v1 and v2 doesn't support DS4 gyro calibration routines
            new VidPidInfo(NACON_VID, 0x0D02, "Nacon Revol Pro v.2", InputDeviceType.DS4, VidPidFeatureSet.NoGyroCalib),
            new VidPidInfo(HORI_VID, 0x00EE, "Hori PS4 Mini", InputDeviceType.DS4, VidPidFeatureSet.NoOutputData | VidPidFeatureSet.NoBatteryReading | VidPidFeatureSet.NoGyroCalib),  // Hori PS4 Mini Wired Gamepad
            new VidPidInfo(0x7545, 0x0104, "Armor 3 LU Cobra"), // Armor 3 Level Up Cobra
            new VidPidInfo(0x2E95, 0x7725, "Scuf Vantage"), // Scuf Vantage gamepad
            new VidPidInfo(0x11C0, 0x4001, "PS4 Fun"), // PS4 Fun Controller
            new VidPidInfo(0x0C12, 0x0E20, "Brook Mars Controller"), // Brook Mars controller (wired) with DS4 mode
            new VidPidInfo(RAZER_VID, 0x1007, "Razer Raiju TE"), // Razer Raiju Tournament Edition (wired)
            new VidPidInfo(RAZER_VID, 0x100A, "Razer Raiju TE BT", InputDeviceType.DS4, VidPidFeatureSet.OnlyInputData0x01 | VidPidFeatureSet.OnlyOutputData0x05 | VidPidFeatureSet.NoBatteryReading | VidPidFeatureSet.NoGyroCalib), // Razer Raiju Tournament Edition (BT). Incoming report data is in "ds4 USB format" (32 bytes) in BT. Also, WriteOutput uses "usb" data packet type in BT.
            new VidPidInfo(RAZER_VID, 0x1004, "Razer Raiju UE USB"), // Razer Raiju Ultimate Edition (wired)
            new VidPidInfo(RAZER_VID, 0x1009, "Razer Raiju UE BT", InputDeviceType.DS4, VidPidFeatureSet.OnlyInputData0x01 | VidPidFeatureSet.OnlyOutputData0x05 | VidPidFeatureSet.NoBatteryReading | VidPidFeatureSet.NoGyroCalib), // Razer Raiju Ultimate Edition (BT)
            new VidPidInfo(SONY_VID, 0x05C5, "CronusMax (PS4 Mode)"), // CronusMax (PS4 Output Mode)
            new VidPidInfo(0x0C12, 0x57AB, "Warrior Joypad JS083", InputDeviceType.DS4, VidPidFeatureSet.NoGyroCalib), // Warrior Joypad JS083 (wired). Custom lightbar color doesn't work, but everything else works OK (except touchpad and gyro because the gamepad doesnt have those).
            new VidPidInfo(0x0C12, 0x0E16, "Steel Play MetalTech"), // Steel Play Metaltech P4 (wired)
            new VidPidInfo(NACON_VID, 0x0D08, "Nacon Revol U Pro"), // Nacon Revolution Unlimited Pro
            new VidPidInfo(NACON_VID, 0x0D10, "Nacon Revol Infinite"), // Nacon Revolution Infinite (sometimes known as Revol Unlimited Pro v2?). Touchpad, gyro, rumble, "led indicator" lightbar.
            new VidPidInfo(HORI_VID, 0x0084, "Hori Fighting Cmd"), // Hori Fighting Commander (special kind of gamepad without touchpad or sticks. There is a hardware switch to alter d-pad type between dpad and LS/RS)
            new VidPidInfo(NACON_VID, 0x0D13, "Nacon Revol Pro v.3"),
            new VidPidInfo(HORI_VID, 0x0055, "Horipad 4 FPS", InputDeviceType.DS4, VidPidFeatureSet.NoGyroCalib), // Horipad 4 FPS (wired only.)
            new VidPidInfo(HORI_VID, 0x0066, "Horipad FPS Plus", InputDeviceType.DS4, VidPidFeatureSet.NoGyroCalib), // Horipad FPS Plus (wired only. No light bar, rumble and Gyro/Accel sensor. Cannot Hide "HID-compliant vendor-defined device" in USB Composite Device. Other feature works fine.)
            new VidPidInfo(0x9886, 0x0025, "Astro C40", InputDeviceType.DS4, VidPidFeatureSet.NoGyroCalib), // Astro C40 (wired and BT. Works if Astro specific xinput drivers haven't been installed. Uninstall those to use the pad as dinput device)
            new VidPidInfo(0x0E8F, 0x1114, "Gamo2 Divaller", InputDeviceType.DS4, VidPidFeatureSet.NoGyroCalib), // Gamo2 Divaller (wired only. Light bar not controllable. No touchpad, gyro or rumble)
            new VidPidInfo(HORI_VID, 0x0101, "Hori Mini Hatsune Miku FT", InputDeviceType.DS4, VidPidFeatureSet.NoGyroCalib), // Hori Mini Hatsune Miku FT (wired only. No light bar, gyro or rumble)
            new VidPidInfo(HORI_VID, 0x00C9, "Hori Taiko Controller",  InputDeviceType.DS4, VidPidFeatureSet.NoGyroCalib), // Hori Taiko Controller (wired only. No light bar, touchpad, gyro, rumble, sticks or triggers)
            new VidPidInfo(0x0C12, 0x1E1C, "SnakeByte Game:Pad 4S", InputDeviceType.DS4, VidPidFeatureSet.NoGyroCalib | VidPidFeatureSet.NoBatteryReading), // SnakeByte Gamepad for PS4 (wired only. No gyro. No light bar). If it doesn't work then try the latest gamepad firmware from https://mysnakebyte.com/
            new VidPidInfo(0x7545, 0x1122, "Gioteck VX4", InputDeviceType.DS4), // Gioteck VX4 (no real lightbar, only some RGB leds)
            new VidPidInfo(0x7331, 0x0001, "DualShock 3 (DS4 Emulation)", InputDeviceType.DS4, VidPidFeatureSet.NoGyroCalib | VidPidFeatureSet.VendorDefinedDevice), // Sony DualShock 3 using DsHidMini driver. DsHidMini uses vendor-defined HID device type when it's emulating DS3 using DS4 button layout
            new VidPidInfo(0x20D6, 0x792A, "PowerA FUSION Wired Fightpad for PS4", InputDeviceType.DS4, VidPidFeatureSet.NoGyroCalib), // No lightbar, gyro, or sticks
            new VidPidInfo(0x044F, 0xD00E, "Thrustmaster eSwap Pro", InputDeviceType.DS4, VidPidFeatureSet.NoGyroCalib | VidPidFeatureSet.NoBatteryReading), // Thrustmaster eSwap Pro (wired only. No lightbar or gyro)
            new VidPidInfo(0x0C12, 0x0E15, "Playmax Wired Controller (PS4)", InputDeviceType.DS4, VidPidFeatureSet.NoBatteryReading | VidPidFeatureSet.NoGyroCalib), // Generic PS4 Controller by Playmax (brand primarily in New Zealand). Standard Wired PS4 controller, no Gyro, no Lightbar, no Battery. There is a newer model but I'm not sure if it uses a different Vid or Pid yet.
        };

        public IReadOnlyList<VidPidInfo> KnownDevices => knownDevices;

        public bool Handles(InputDeviceType type)
        {
            return type == InputDeviceType.DS4;
        }

        /// <summary>Sony ワイヤレスアダプターは機器固有のシリアルを持たないため、擬似のシリアルを作る。それ以外は特徴レポートから読む。</summary>
        public string ReadSerial(VidPidInfo info, IHidTransport hidDevice)
        {
            if (info.checkConnection(hidDevice) == ConnectionType.SONYWA)
            {
                return hidDevice.GenerateFakeHwSerial();
            }

            return hidDevice.ReadSerial(DS4Device.SERIAL_FEATURE_ID);
        }

        public DS4Device Create(InputDeviceType type, IHidTransport hidDevice, string displayName, VidPidFeatureSet featureSet)
        {
            return new DS4Device(hidDevice, displayName, featureSet);
        }
    }
}
