#nullable enable
using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Input.Devices;
using DS4Windows.Input.Reports;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// DS4・DualSense の出力レポートの各フィールドの位置と、LED の値を、公開された別実装の仕様と突き合わせる。
    /// 出どころは Linux カーネルの <c>hid-playstation.c</c>（構造体 <c>dualshock4_output_report_common</c>、<c>dualsense_output_report_common</c>）と
    /// SDL の <c>SDL_hidapi_ps4.c</c>・<c>SDL_hidapi_ps5.c</c>（2026-11-22 に取得。経緯は <c>PhaseRV38-Spec-Crosscheck.md</c>）。
    /// 構造体の先頭からの位置は、USB のレポートでは +1（レポート ID の分）、DualSense の Bluetooth では +3。
    /// </summary>
    public class OutputReportLayoutSpecConformanceTests
    {
        private static DS4HapticState Haptics()
        {
            var h = new DS4HapticState();
            h.rumbleState.RumbleMotorStrengthRightLightFast = 0x11;   // motor_right
            h.rumbleState.RumbleMotorStrengthLeftHeavySlow = 0x22;    // motor_left
            h.lightbarState.LightBarColor = new DS4Color(0x33, 0x44, 0x55);
            h.lightbarState.LightBarFlashDurationOn = 0x66;
            h.lightbarState.LightBarFlashDurationOff = 0x77;
            return h;
        }

        [Fact]
        public void Ds4Usb_TheFieldsAreAtTheKernelOffsets()
        {
            // dualshock4_output_report_common: valid_flag0、valid_flag1、reserved、motor_right、motor_left、赤・緑・青、blink_on、blink_off（USB の ID 0x05 の次から）
            var report = new byte[32];
            DualShock4OutputReportBuilder.FillUsb(report, 0x07, Haptics());

            Assert.Equal(0x05, report[0]);
            Assert.Equal(0x07, report[1]);   // valid_flag0: 振動 0x01、ライトバー 0x02、点滅 0x04
            Assert.Equal(0x11, report[4]);
            Assert.Equal(0x22, report[5]);
            Assert.Equal(new byte[] { 0x33, 0x44, 0x55, 0x66, 0x77 }, report[6..11]);
        }

        [Fact]
        public void Ds4Bluetooth_TheFieldsAreAtTheKernelOffsets()
        {
            // BT 0x11: [1]＝hw_control（0x80 HID、0x40 CRC、下位 6 ビット＝入力の間隔）、[2]＝audio_control、[3]＝valid_flag0、[4]＝valid_flag1、[6]・[7]＝モーター、[8..10]＝色、[11]・[12]＝点滅
            var report = new byte[78];
            DualShock4OutputReportBuilder.FillBluetooth(report, 0x11, 4, 0x07, Haptics());

            Assert.Equal(0x11, report[0]);
            Assert.Equal(0xC0 | 4, report[1]);
            Assert.Equal(0x07, report[3]);
            Assert.Equal(new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77 }, report[6..13]);
        }

        private static byte[] DualSenseUsb(DualSenseOutputState state)
        {
            var report = new byte[48];
            DualSenseOutputReportBuilder.Fill(report, false, in state);
            return report;
        }

        private static DualSenseDevice.TriggerEffectData Effect(byte seed) => new DualSenseDevice.TriggerEffectData
        {
            triggerMotorMode = seed,
            triggerStartResistance = (byte)(seed + 1),
            triggerEffectForce = (byte)(seed + 2),
            triggerRangeForce = (byte)(seed + 3),
            triggerNearReleaseStrength = (byte)(seed + 4),
            triggerNearMiddleStrength = (byte)(seed + 5),
            triggerPressedStrength = (byte)(seed + 6),
            triggerActuationFrequency = (byte)(seed + 9),
        };

        [Fact]
        public void DualSenseUsb_TheFieldsAreAtTheKernelAndSdlOffsets()
        {
            // 構造体の先頭から: motor_right 2、motor_left 3、mute_button_led 8、右トリガー 10〜、左トリガー 21〜、valid_flag2 38、player_leds 43、赤・緑・青 44〜46
            // （USB のレポートでは、構造体が [1] から始まる）
            byte[] r = DualSenseUsb(new DualSenseOutputState
            {
                UseRumble = true,
                UseAccurateRumble = true,
                RumbleRightLightFast = 0x11,
                RumbleLeftHeavySlow = 0x22,
                MuteLedByte = 0x01,
                PlayerLedMask = 0x15,
                LightbarColor = new DS4Color(0x33, 0x44, 0x55),
                R2Effect = Effect(0x60),
                L2Effect = Effect(0x70),
            });

            Assert.Equal(0x02, r[0]);
            Assert.Equal(0x11, r[1 + 2]);
            Assert.Equal(0x22, r[1 + 3]);
            Assert.Equal(0x01, r[1 + 8]);
            Assert.Equal(0x15, r[1 + 43]);
            Assert.Equal(new byte[] { 0x33, 0x44, 0x55 }, r[(1 + 44)..(1 + 47)]);

            // トリガーの効果は、モード、開始位置、力、…の順。右は構造体の 10、左は 21（SDL の rgucRightTriggerEffect・rgucLeftTriggerEffect）
            Assert.Equal(0x60, r[1 + 10]);
            Assert.Equal(0x61, r[1 + 11]);
            Assert.Equal(0x69, r[1 + 19]);
            Assert.Equal(0x70, r[1 + 21]);
            Assert.Equal(0x71, r[1 + 22]);
            Assert.Equal(0x79, r[1 + 30]);
        }

        [Fact]
        public void DualSenseUsb_TheValidFlags_EnableOnlyWhatTheReportCarries()
        {
            byte[] r = DualSenseUsb(new DualSenseOutputState { UseRumble = true, UseAccurateRumble = true });

            // valid_flag0: 0x01 振動（互換）、0x02 触覚の選択、0x04 右トリガーのモーター、0x08 左トリガーのモーター
            Assert.Equal(0x0F, r[1 + 0]);

            // valid_flag1: 0x01 ミュート LED、0x04 ライトバー、0x10 プレイヤー LED（0x40 は仕様にない印だが、実機で動いている値）
            Assert.Equal(0x15, r[1 + 1] & 0x3F);

            // valid_flag2: 0x02 ライトバーの設定、0x04 改良された振動（ファームウェア 2.21 以降）
            Assert.Equal(0x06, r[1 + 38]);
        }

        [Fact]
        public void DualSenseUsb_WithoutTheAccurateRumble_TheImprovedVibrationFlagIsNotSet()
        {
            byte[] r = DualSenseUsb(new DualSenseOutputState { UseRumble = true, UseAccurateRumble = false });

            Assert.Equal(0x00, r[1 + 38] & 0x04);
        }

        [Theory]
        [InlineData(0, 0x04)]   // 真ん中
        [InlineData(1, 0x0A)]
        [InlineData(2, 0x15)]
        [InlineData(3, 0x1B)]
        public void DualSense_ThePlayerLedMasks_AreTheKernelPatterns(int slot, int mask)
        {
            // カーネルの player_ids と、SDL の lights（先頭の 4 つ）。5 つ目以降（4＝0x11、5＝0x0E、6＝0x1F）は、このアプリ独自の並び（SDL は 0x1F、0x11、0x0E）
            Assert.Equal(mask, DualSenseLedMasks.FromSlot(slot));
        }

        [Fact]
        public void DualSense_TheMuteLedValues_AreTheSdlBitmask()
        {
            // SDL: 0x00＝消灯、0x01＝点灯、0x02＝脈動
            Assert.Equal(0x00, DualSenseLedMasks.MuteLed(DS4Windows.Core.Settings.DualSenseControllerOptions.MuteLEDMode.Off));
            Assert.Equal(0x01, DualSenseLedMasks.MuteLed(DS4Windows.Core.Settings.DualSenseControllerOptions.MuteLEDMode.On));
            Assert.Equal(0x02, DualSenseLedMasks.MuteLed(DS4Windows.Core.Settings.DualSenseControllerOptions.MuteLEDMode.Pulse));
        }

        [Fact]
        public void DualSenseBluetooth_TheStructureStartsThreeBytesIn_AndHasTheSameFieldOrderAsUsb()
        {
            var state = new DualSenseOutputState
            {
                UseRumble = true,
                UseAccurateRumble = true,
                RumbleRightLightFast = 0x11,
                RumbleLeftHeavySlow = 0x22,
                PlayerLedMask = 0x15,
                LightbarColor = new DS4Color(0x33, 0x44, 0x55),
                R2Effect = Effect(0x60),
            };
            byte[] usb = DualSenseUsb(state);
            var bt = new byte[78];
            DualSenseOutputReportBuilder.Fill(bt, true, in state);

            Assert.Equal(0x31, bt[0]);
            Assert.Equal(0x10, bt[2]);
            for (int i = 0; i < 47; i++)
            {
                Assert.True(usb[1 + i] == bt[3 + i], $"構造体の {i} バイト目が、USB（[{1 + i}]）と Bluetooth（[{3 + i}]）で違う");
            }
        }
    }
}
