using System;
using System.Linq;
using System.Reflection;
using Xunit;
using DS4Windows.Core.Abstractions;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services;
using DS4Windows.UI;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase13-Step10 10-f: <see cref="ControlService"/> から転送メンバーを削除した結果を固定する。
    /// 削除した転送が戻らないこと、コンストラクター引数が努力目標（20 以下）に収まっていること、
    /// 転送の行き先（<see cref="IRuntimeFeatureSwitch"/>）が本物のサービスへ委譲していること。
    /// </summary>
    public class Phase13Step10ForwardRemovalGuardTests
    {
        private static readonly string[] RemovedForwards =
        {
            "PluginOutDev", "UnplugOutDev", "CheckProfileOptions", "TouchPadOn", "StartTPOff",
            "ChangeUDPStatus", "ChangeMotionEventStatus", "ChangeOSCListenerStatus", "ChangeOSCSenderStatus",
            "CheckHidHidePresence", "UpdateHidHiddenAttributes", "TouchpadSlide", "GetActiveInputControl", "setRumble",
        };

        [Fact]
        public void ControlService_HasNoneOfTheRemovedForwardingMembers()
        {
            BindingFlags all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var remaining = typeof(ControlService).GetMethods(all).Select(m => m.Name).Intersect(RemovedForwards).ToList();
            Assert.True(remaining.Count == 0, "削除した転送が ControlService に戻っている: " + string.Join(", ", remaining));
        }

        [Fact]
        public void ControlService_ConstructorTakesAtMostTwentyParameters()
        {
            // Phase13-Plan Q6 の努力目標（20 以下）。26 → 17（Step10）
            Assert.True(typeof(ControlService).GetConstructors().Single().GetParameters().Length <= 20);
        }

        [Fact]
        public void ControlService_NoLongerHoldsTheServicesOnlyUsedByTheRemovedForwards()
        {
            var types = typeof(ControlService).GetConstructors().Single().GetParameters().Select(p => p.ParameterType).ToList();
            foreach (Type removed in new[]
            {
                typeof(IOscService), typeof(IHidHideService), typeof(IUdpMotionService), typeof(ITouchpadToggleService),
                typeof(IActiveInputProbe), typeof(IDeviceRumbleService), typeof(IEnvironmentService),
                typeof(ILatencyMonitor), typeof(IDeviceConnectionSetup), typeof(IDeviceDisconnectHandler),
            })
            {
                Assert.DoesNotContain(removed, types);
            }
        }

        [Fact]
        public void MainWindow_TakesOneMoreParameterThanBeforeStep10_TheRuntimeFeatureSwitch()
        {
            // 4 つのサービスを直接受けると +4 になるところを、IRuntimeFeatureSwitch にまとめて +1 に抑えた（決定 2）。
            // 引数は ArgumentParser を含めて 22（Step9 の終了時は 21）。Phase13-Step11 11-c で IIpcCommandProcessor が加わり 23
            // Phase14b-Step15（決定 D12）で IStartupEntryService（設定画面のスタートアップ登録。旧 静的 StartupMethods）が加わり 24
            // Phase14b-Step16（決定 D11）で IUiThemeAndLanguage（テーマ・言語の切り替え。旧 Application.Current as App）が加わり 25
            // 同 Step で IIpcResultWriter（CLI の問い合わせの結果の書き込み。旧 (Application.Current as App).WriteIPCResultDataMMF）が加わり 26
            // Phase14b-Step17（決定 D10）で IChangelogService（更新の確認・変更履歴。旧 静的 Changelog）が加わり 27
            var parameters = typeof(MainWindow).GetConstructors().Single().GetParameters();
            Assert.Equal(27, parameters.Length);
            Assert.Contains(parameters, p => p.ParameterType == typeof(IRuntimeFeatureSwitch));
            Assert.DoesNotContain(parameters, p => p.ParameterType == typeof(IOscService) || p.ParameterType == typeof(IUdpMotionService)
                || p.ParameterType == typeof(IHidHideService) || p.ParameterType == typeof(ITouchpadToggleService));
        }

        [Fact]
        public void RuntimeFeatureSwitch_IsASingleton_ResolvedFromTheHost()
        {
            Assert.IsType<RuntimeFeatureSwitch>(TestHost.GetService<IRuntimeFeatureSwitch>());
            Assert.Same(TestHost.GetService<IRuntimeFeatureSwitch>(), TestHost.GetService<IRuntimeFeatureSwitch>());
        }

        [Fact]
        public void RuntimeFeatureSwitch_SetRumble_OutsideTheSlotLimit_OrWithoutAController_DoesNothing()
        {
            var sw = TestHost.GetService<IRuntimeFeatureSwitch>();
            int limit = TestHost.GetService<IEnvironmentService>().ControllerSlotLimit;

            // 上限の外、コントローラーがいないスロットのどちらも、例外にならない（旧 ControlService.setRumble と同じ）
            Assert.Null(Record.Exception(() => sw.SetRumble(0, 0, limit)));
            Assert.Null(Record.Exception(() => sw.SetRumble(0, 0, 0)));
        }

        [Fact]
        public void RuntimeFeatureSwitch_NullDependency_ThrowsWithTheParameterName()
        {
            var osc = TestHost.GetService<IOscService>();
            var udp = TestHost.GetService<IUdpMotionService>();
            var hid = TestHost.GetService<IHidHideService>();
            var tp = TestHost.GetService<ITouchpadToggleService>();
            var slots = TestHost.GetService<IDeviceSlotState>();
            var probe = TestHost.GetService<IActiveInputProbe>();
            var rumble = TestHost.GetService<IDeviceRumbleService>();
            var env = TestHost.GetService<IEnvironmentService>();

            Assert.Equal("osc", Assert.Throws<ArgumentNullException>(() => new RuntimeFeatureSwitch(null, udp, hid, tp, slots, probe, rumble, env)).ParamName);
            Assert.Equal("udpMotion", Assert.Throws<ArgumentNullException>(() => new RuntimeFeatureSwitch(osc, null, hid, tp, slots, probe, rumble, env)).ParamName);
            Assert.Equal("hidHide", Assert.Throws<ArgumentNullException>(() => new RuntimeFeatureSwitch(osc, udp, null, tp, slots, probe, rumble, env)).ParamName);
            Assert.Equal("touchpadToggle", Assert.Throws<ArgumentNullException>(() => new RuntimeFeatureSwitch(osc, udp, hid, null, slots, probe, rumble, env)).ParamName);
            Assert.Equal("slots", Assert.Throws<ArgumentNullException>(() => new RuntimeFeatureSwitch(osc, udp, hid, tp, null, probe, rumble, env)).ParamName);
            Assert.Equal("activeInputProbe", Assert.Throws<ArgumentNullException>(() => new RuntimeFeatureSwitch(osc, udp, hid, tp, slots, null, rumble, env)).ParamName);
            Assert.Equal("rumble", Assert.Throws<ArgumentNullException>(() => new RuntimeFeatureSwitch(osc, udp, hid, tp, slots, probe, null, env)).ParamName);
            Assert.Equal("environmentService", Assert.Throws<ArgumentNullException>(() => new RuntimeFeatureSwitch(osc, udp, hid, tp, slots, probe, rumble, null)).ParamName);
        }
    }
}
