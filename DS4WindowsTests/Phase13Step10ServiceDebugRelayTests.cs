using System;
using System.Collections.Generic;
using System.Reflection;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase13-Step10 10-e: <see cref="ServiceDebugRelay"/> は、6 つのサービスの <c>DebugMessage</c> を、内容に触れずに 1 つのイベントへ流す。
    /// 各インターフェースは <see cref="DispatchProxy"/> で作り、<c>DebugMessage</c> の購読と発火だけを実装する。
    /// </summary>
    public class Phase13Step10ServiceDebugRelayTests
    {
        public class EventProxy : DispatchProxy
        {
            public Action<string, bool> DebugMessage;

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                if (targetMethod.Name == "add_DebugMessage")
                    DebugMessage += (Action<string, bool>)args[0];
                else if (targetMethod.Name == "remove_DebugMessage")
                    DebugMessage -= (Action<string, bool>)args[0];
                return targetMethod.ReturnType.IsValueType && targetMethod.ReturnType != typeof(void)
                    ? Activator.CreateInstance(targetMethod.ReturnType) : null;
            }
        }

        private static T Make<T>(out EventProxy proxy) where T : class
        {
            T instance = DispatchProxy.Create<T, EventProxy>();
            proxy = (EventProxy)(object)instance;
            return instance;
        }

        [Fact]
        public void EachOfTheSixServices_MessageReachesTheRelay_WithTheSameTextAndWarningFlag()
        {
            var hidHide = Make<IHidHideService>(out EventProxy p1);
            var udp = Make<IUdpMotionService>(out EventProxy p2);
            var latency = Make<ILatencyMonitor>(out EventProxy p3);
            var touchpad = Make<ITouchpadToggleService>(out EventProxy p4);
            var setup = Make<IDeviceConnectionSetup>(out EventProxy p5);
            var disconnect = Make<IDeviceDisconnectHandler>(out EventProxy p6);

            var relay = new ServiceDebugRelay(hidHide, udp, latency, touchpad, setup, disconnect);
            var received = new List<(string, bool)>();
            relay.DebugMessage += (data, warning) => received.Add((data, warning));

            p1.DebugMessage("hidhide", false);
            p2.DebugMessage("udp", true);
            p3.DebugMessage("latency", false);
            p4.DebugMessage("touchpad", true);
            p5.DebugMessage("setup", false);
            p6.DebugMessage("disconnect", true);

            Assert.Equal(new[]
            {
                ("hidhide", false), ("udp", true), ("latency", false),
                ("touchpad", true), ("setup", false), ("disconnect", true),
            }, received);
        }

        [Fact]
        public void NoSubscriber_DoesNotThrow()
        {
            var relay = new ServiceDebugRelay(
                Make<IHidHideService>(out EventProxy p1), Make<IUdpMotionService>(out _), Make<ILatencyMonitor>(out _),
                Make<ITouchpadToggleService>(out _), Make<IDeviceConnectionSetup>(out _), Make<IDeviceDisconnectHandler>(out _));

            Assert.Null(Record.Exception(() => p1.DebugMessage("x", false)));
        }

        [Fact]
        public void NullDependency_ThrowsWithTheParameterName()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new ServiceDebugRelay(
                null, Make<IUdpMotionService>(out _), Make<ILatencyMonitor>(out _),
                Make<ITouchpadToggleService>(out _), Make<IDeviceConnectionSetup>(out _), Make<IDeviceDisconnectHandler>(out _)));
            Assert.Equal("hidHide", ex.ParamName);
        }

        [Fact]
        public void TheRelay_IsASingleton_ResolvedFromTheHost()
        {
            Assert.IsType<ServiceDebugRelay>(TestHost.GetService<IServiceDebugRelay>());
            Assert.Same(TestHost.GetService<IServiceDebugRelay>(), TestHost.GetService<IServiceDebugRelay>());
        }
    }
}
