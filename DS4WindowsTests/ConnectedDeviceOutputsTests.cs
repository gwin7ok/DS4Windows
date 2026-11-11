using DS4Windows.Processing.Synthetic;
using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// デバイスの接続時の出力側の準備（<see cref="IConnectedDeviceOutputs"/>）と、タッチパッドの <c>Mouse</c> の工場（<see cref="ITouchpadMouseFactory"/>）の、
    /// 登録と、何もしない条件（OSC の送信なし、UDP の配信なし）の確認。接続の手順の中での順序は <c>ConnectorGuardTests</c> が固定する。
    /// </summary>
    public class ConnectedDeviceOutputsTests
    {
        [Fact]
        public void TheCollaborators_AreRegisteredAsSingletons()
        {
            Assert.IsType<ConnectedDeviceOutputs>(TestHost.GetService<IConnectedDeviceOutputs>());
            Assert.Same(TestHost.GetService<IConnectedDeviceOutputs>(), TestHost.GetService<IConnectedDeviceOutputs>());
            Assert.IsType<TouchpadMouseFactory>(TestHost.GetService<ITouchpadMouseFactory>());
            Assert.Same(TestHost.GetService<ITouchpadMouseFactory>(), TestHost.GetService<ITouchpadMouseFactory>());
        }

        [Fact]
        public void NotifyPlugged_AndPrepareMotion_DoNothing_WhenOscSenderAndUdpServerAreOff()
        {
            IConnectedDeviceOutputs outputs = TestHost.GetService<IConnectedDeviceOutputs>();
            IUdpMotionService udp = TestHost.GetService<IUdpMotionService>();
            IAppSettingsService app = TestHost.GetService<IAppSettingsService>();
            if (app.UseOscSender || udp.HasServer) return;   // 共有のホストの設定が変わっていたら検査を省く

            outputs.NotifyPlugged(0);
            outputs.PrepareMotion(0, null);   // サーバーがないので、デバイスを触らない
        }

        [Fact]
        public void Constructors_RejectNull()
        {
            var settings = TestHost.GetService<IProfileSettingsService>();
            var app = TestHost.GetService<IAppSettingsService>();
            var osc = TestHost.GetService<IOscService>();
            var pad = TestHost.GetService<IVirtualPadService>();
            var types = TestHost.GetService<IOutputDeviceTypeState>();
            var udp = TestHost.GetService<IUdpMotionService>();
            Assert.Throws<ArgumentNullException>(() => new ConnectedDeviceOutputs(null, app, osc, pad, types, udp));
            Assert.Throws<ArgumentNullException>(() => new ConnectedDeviceOutputs(settings, null, osc, pad, types, udp));
            Assert.Throws<ArgumentNullException>(() => new ConnectedDeviceOutputs(settings, app, null, pad, types, udp));
            Assert.Throws<ArgumentNullException>(() => new ConnectedDeviceOutputs(settings, app, osc, null, types, udp));
            Assert.Throws<ArgumentNullException>(() => new ConnectedDeviceOutputs(settings, app, osc, pad, null, udp));
            Assert.Throws<ArgumentNullException>(() => new ConnectedDeviceOutputs(settings, app, osc, pad, types, null));
            Assert.Throws<ArgumentNullException>(() => new TouchpadMouseFactory(settings, null, new DS4Windows.Processing.StickMotionState(), new DS4Windows.Processing.Synthetic.SyntheticInputState(), TimeProvider.System));
        }
    }
}
