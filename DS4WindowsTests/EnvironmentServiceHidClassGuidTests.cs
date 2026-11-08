using System;
using DS4Windows.Core.Platform;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 画面が HID の抜き差しの通知をウィンドウへ登録するときに使う GUID を、環境サービスが返すこと
    /// （画面が第 1 層の <c>NativeMethods</c> を直接呼ばないための窓口）。HID クラスの GUID は OS が決める固定の値。
    /// </summary>
    public class EnvironmentServiceHidClassGuidTests
    {
        [Fact]
        public void HidClassGuid_IsTheWindowsHidInterfaceClass()
        {
            Assert.Equal(new Guid("4D1E55B2-F16F-11CF-88CB-001111000030"), new EnvironmentService().HidClassGuid);
        }
    }
}
