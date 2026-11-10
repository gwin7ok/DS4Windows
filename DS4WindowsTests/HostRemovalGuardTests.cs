using System;
using System.Linq;
using System.Reflection;
using Xunit;
using DS4Windows.Output.Services;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services;

namespace DS4WindowsTests
{
    /// <summary>Phase13-Step3: IVirtualPadHost／IServiceLifecycleHost と、その Func の撤廃、新サービスの配線を固定する。</summary>
    [Xunit.Collection(SharedHostStateCollection.Name)]
    public class HostRemovalGuardTests
    {
        [Fact]
        public void TheRemovedHostInterfaces_DoNotExist_AndTheBootstrapperDoesNotImplementThem()
        {
            Assembly asm = typeof(DS4Windows.Processing.Abstractions.IRuntimeBootstrapper).Assembly;
            Assert.Null(asm.GetType("DS4Windows.DI.IVirtualPadHost"));
            Assert.Null(asm.GetType("DS4Windows.DI.IServiceLifecycleHost"));
            Assert.DoesNotContain(typeof(DS4Windows.Processing.Services.RuntimeBootstrapper).GetInterfaces(), i => i.Name == "IVirtualPadHost" || i.Name == "IServiceLifecycleHost");
            Assert.Null(asm.GetType("DS4Windows.DI.IUdpMotionHost"));   // Phase13-Step5 で IUdpMotionHost ごと廃止した
        }

        [Fact]
        public void RuntimeBootstrapper_HasNoKbmInitializationParameters_AndHas9Parameters()
        {
            var parameters = typeof(DS4Windows.Processing.Services.RuntimeBootstrapper).GetConstructors().Single().GetParameters();
            Assert.Equal(9, parameters.Length);   // Phase27: ControlService を RuntimeBootstrapper へ置き換えた（取り付けと終了の 9 引数。旧 ControlService は 10）。 使っていなかった IProfileRepository／IMapping を外した（12 → 10）。 Phase16-Step5a: 登録簿のフック・適用プロファイルの記憶・仮想パッドの失敗の処理を部品へ分けた（16 → 12）。 Phase13b-Step3: IServiceDebugRelay を廃止（17 → 16）。 Phase13-Step10 10-e: ログの中継の 6 引数を IServiceDebugRelay 1 つにまとめた（22 → 17）。 Phase13-Step10 10-d: IActiveInputProbe／IDeviceRumbleService／IEnvironmentService を外した（25 → 22）。 Phase13-Step10 10-c: IOscService を外した（26 → 25）。 Phase13-Step5b-5b-e: 使われなくなった 4 引数を外した（30 → 26）。 Phase13-Step5b-5b-c: IInputReportProcessor を足した（29 → 30）。 Phase13-Step5: IDeviceSlotState を足した（28 → 29）
            Assert.DoesNotContain(parameters, p => p.ParameterType == typeof(IVirtualKBMLifecycle) || p.ParameterType.Name == "ArgumentParser");
            Assert.Contains(parameters, p => p.ParameterType == typeof(IEventThreadDispatcher));
            Assert.DoesNotContain(parameters, p => p.ParameterType == typeof(IDeviceRumbleService));   // Phase13-Step10 10-d: setRumble の転送とともに外した（RuntimeFeatureSwitch が受ける）
            Assert.Null(typeof(DS4Windows.Processing.Abstractions.IRuntimeBootstrapper).Assembly.GetType("DS4Windows.Processing.ControlService"));   // Phase27: 型ごと撤去した
        }

        [Fact]
        public void TheNewServices_AreSingletons_AndTheFuncRegistrationsAreGone()
        {
            Assert.IsType<EventThreadDispatcher>(TestHost.GetService<IEventThreadDispatcher>());
            Assert.Same(TestHost.GetService<IEventThreadDispatcher>(), TestHost.GetService<IEventThreadDispatcher>());
            Assert.IsType<DeviceRumbleService>(TestHost.GetService<IDeviceRumbleService>());
            Assert.IsType<OutputKbmInitializer>(TestHost.GetService<IOutputKbmInitializer>());
            // Phase14d-Step2: 旧 Func<IVirtualPadHost>／Func<IServiceLifecycleHost> の登録がないことの検査は外した（型ごと撤去。RemovedApiRuleTests と FuncRatchetGuardTests が守る）
        }
    }
}
