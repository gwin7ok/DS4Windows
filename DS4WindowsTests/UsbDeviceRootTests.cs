using System.Collections.Generic;
using DS4Windows.Core.Platform;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// XInput の入り口（XUSB インターフェース）の持ち主から、隠す対象の根（USB の複合機器）を求める <see cref="DevicePathProbe.FindUsbDeviceRoot"/> の確認。
    /// 入り口の位置は機種で違う（Xbox 360 は USB の親、Xbox One 世代は HID の子）が、どちらも同じ複合機器へ行き着くこと。
    /// 親の問い合わせ（OS）は、辞書に替える。
    /// </summary>
    public class UsbDeviceRootTests
    {
        private const string Composite = @"USB\VID_045E&PID_0B00\30324D4930303031363232333432";
        private const string UsbInterface = @"USB\VID_045E&PID_02FF&IG_00\00&00&000040046183ED7E";
        private const string Hid = @"HID\VID_045E&PID_02FF&IG_00\9&3001EB5A&0&0000";
        private const string Hub = @"USB\VID_8564&PID_4000\6&3053BAED&0&4";

        private static readonly Dictionary<string, string> Parents = new Dictionary<string, string>
        {
            [Hid] = UsbInterface,
            [UsbInterface] = Composite,
            [Composite] = Hub,
        };

        private static string ParentOf(string id) => Parents.TryGetValue(id, out string parent) ? parent : string.Empty;

        [Fact]
        public void HidChildOwner_ClimbsToTheCompositeDevice_NotTheHub()
        {
            Assert.Equal(Composite, DevicePathProbe.FindUsbDeviceRoot(Hid, ParentOf));
            Assert.Equal(Composite, DevicePathProbe.FindUsbDeviceRoot(UsbInterface, ParentOf));
        }

        [Fact]
        public void UsbParentOwner_StaysAsIs()
        {
            const string xbox360 = @"USB\VID_045E&PID_028E\A02014100201";
            Assert.Equal(xbox360, DevicePathProbe.FindUsbDeviceRoot(xbox360, id => Hub));
            Assert.Equal(Composite, DevicePathProbe.FindUsbDeviceRoot(Composite, ParentOf));
        }

        [Fact]
        public void NonUsbDevice_IsNotClimbed_SoUnrelatedTreesAreNotHidden()
        {
            const string bluetoothHid = @"HID\{00001124-0000-1000-8000-00805F9B34FB}_VID&0002045E_PID&02E0\8&1234&0&0000";
            const string bluetoothParent = @"BTHENUM\{00001124-0000-1000-8000-00805F9B34FB}_VID&0002045E_PID&02E0\8&1234&0&AABBCCDDEEFF_C00000000";
            Assert.Equal(bluetoothHid, DevicePathProbe.FindUsbDeviceRoot(bluetoothHid, id => bluetoothParent));
        }

        [Fact]
        public void WhenTheParentCannotBeFound_TheOwnerIsReturned()
        {
            Assert.Equal(Hid, DevicePathProbe.FindUsbDeviceRoot(Hid, id => string.Empty));
        }
    }
}
