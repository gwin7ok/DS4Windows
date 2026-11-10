using System;
using System.Linq;
using DS4Windows.Core.Model;
using DS4Windows.Output;
using DS4Windows.Output.Abstractions;
using DS4Windows.Output.Devices;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 出力の種類ごとのカートリッジ（<see cref="IOutputDeviceCartridge"/>）の契約。DI に種類ごとに 1 つ登録され、
    /// 自分の種類の型を作り、型の絞り込みは自分の型だけを通す。
    /// </summary>
    public class OutputDeviceCartridgeTests
    {
        [Fact]
        public void TheHost_RegistersOneCartridgeForEachOutputType()
        {
            var types = ((System.Collections.Generic.IEnumerable<IOutputDeviceCartridge>)TestHost.GetService(typeof(System.Collections.Generic.IEnumerable<IOutputDeviceCartridge>))).Select(c => c.Type).OrderBy(t => t).ToArray();

            Assert.Equal(new[] { OutContType.X360, OutContType.DS4 }, types);
        }

        [Fact]
        public void EachCartridge_CreatesItsOwnDeviceType_AndNarrowsOnlyThatType()
        {
            var backend = new FakeVirtualPadBackend();
            backend.Start();
            var cartridges = TestOutputCartridges.Default().ToDictionary(c => c.Type);

            OutputDevice x360 = cartridges[OutContType.X360].Create(backend, new Version(1, 0));
            OutputDevice ds4 = cartridges[OutContType.DS4].Create(backend, new Version(1, 16, 0, 0));

            Assert.IsType<Xbox360OutDevice>(x360);
            Assert.IsNotType<Xbox360OutDevice>(ds4);   // DS4OutDevice は internal のため、絞り込みの結果で種類を確かめる
            Assert.Same(x360, cartridges[OutContType.X360].Narrow(x360));
            Assert.Null(cartridges[OutContType.X360].Narrow(ds4));
            Assert.Same(ds4, cartridges[OutContType.DS4].Narrow(ds4));
            Assert.Null(cartridges[OutContType.DS4].Narrow(x360));
        }

        [Fact]
        public void TheXbox360Cartridge_EnablesTheXInputSlotNumber_OnlyFromTheMinimumDriverVersion()
        {
            var backend = new FakeVirtualPadBackend();
            backend.Start();
            var x360 = TestOutputCartridges.Default().Single(c => c.Type == OutContType.X360);

            var old = (Xbox360OutDevice)x360.Create(backend, new Version(1, 17, 332, 0));
            var current = (Xbox360OutDevice)x360.Create(backend, new Version(1, 17, 333, 0));

            Assert.Equal((Xbox360OutDevice.X360Features)0, old.Features);
            Assert.Equal(Xbox360OutDevice.X360Features.XInputSlotNum, current.Features);
        }
    }
}
