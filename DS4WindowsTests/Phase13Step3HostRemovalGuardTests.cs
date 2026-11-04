using System;
using System.Linq;
using System.Reflection;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>Phase13-Step3: IVirtualPadHost／IServiceLifecycleHost と、その Func の撤廃、新サービスの配線を固定する。</summary>
    [Xunit.Collection("ServiceProviderHolder")]
    public class Phase13Step3HostRemovalGuardTests
    {
        [Fact]
        public void TheRemovedHostInterfaces_DoNotExist_AndControlServiceDoesNotImplementThem()
        {
            Assembly asm = typeof(ControlService).Assembly;
            Assert.Null(asm.GetType("DS4Windows.DI.IVirtualPadHost"));
            Assert.Null(asm.GetType("DS4Windows.DI.IServiceLifecycleHost"));
            Assert.DoesNotContain(typeof(ControlService).GetInterfaces(), i => i.Name == "IVirtualPadHost" || i.Name == "IServiceLifecycleHost");
            Assert.Null(asm.GetType("DS4Windows.DI.IUdpMotionHost"));   // Phase13-Step5 で IUdpMotionHost ごと廃止した
        }

        [Fact]
        public void ControlService_HasNoKbmInitializationParameters_AndStill29Parameters()
        {
            var parameters = typeof(ControlService).GetConstructors().Single().GetParameters();
            Assert.Equal(29, parameters.Length);   // Phase13-Step5: IDeviceSlotState を足した（28 → 29）
            Assert.DoesNotContain(parameters, p => p.ParameterType == typeof(IVirtualKBMLifecycle) || p.ParameterType.Name == "ArgumentParser");
            Assert.Contains(parameters, p => p.ParameterType == typeof(IEventThreadDispatcher));
            Assert.Contains(parameters, p => p.ParameterType == typeof(IDeviceRumbleService));
        }

        [Fact]
        public void TheNewServices_AreSingletons_AndTheFuncRegistrationsAreGone()
        {
            Assert.IsType<EventThreadDispatcher>(TestHost.GetService<IEventThreadDispatcher>());
            Assert.Same(TestHost.GetService<IEventThreadDispatcher>(), TestHost.GetService<IEventThreadDispatcher>());
            Assert.IsType<DeviceRumbleService>(TestHost.GetService<IDeviceRumbleService>());
            Assert.IsType<OutputKbmInitializer>(TestHost.GetService<IOutputKbmInitializer>());

            string anchor = SourceFileLocator.Find("DS4Windows/DI/ServiceRegistration.cs");
            if (anchor == null) return;
            string text = System.IO.File.ReadAllText(anchor);
            Assert.DoesNotContain("Func<IVirtualPadHost>", text);
            Assert.DoesNotContain("Func<IServiceLifecycleHost>", text);
        }
    }
}
