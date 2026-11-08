using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Core.Settings;
using DS4Windows.Output.Kbm;
using DS4Windows.Output.Services;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step8 W3: 出力 KBM ハンドラの実体を <c>OutputKBMHandlerAdapter</c> へ移したこと（<c>Global.outputKBMHandler</c>／<c>outputKBMMapping</c> の削除）、
    /// 合成キーの送出を <c>SyntheticDispatcherService</c>（<c>ISyntheticDispatcher</c>）にし、静的な <c>SyntheticDispatcher</c> を状態を持たない橋渡しにしたこと、
    /// <c>ActionsLoadedRuntimeReset</c> を instance にしたことの回帰ガード。
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。判断の根拠は Phase7-Step8-Plan.md（Q6）。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class Phase7Step8W3OutputKbmOwnershipGuardTests
    {
        private const int Device = 4;
        private const ushort Key = 77;

        private readonly ITestOutputHelper _output;

        public Phase7Step8W3OutputKbmOwnershipGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static SyntheticDispatcherService CreateService(out SyntheticInputState state, out OutputKBMHandlerAdapter owner,
            out ProfileSettingsService settings)
        {
            state = new SyntheticInputState();
            owner = new OutputKBMHandlerAdapter();
            settings = TestRepositoryFactory.CreateProfileSettingsService(new DS4Windows.Core.Settings.BackingStore(new DS4Windows.Core.Platform.EnvironmentService()));
            return new SyntheticDispatcherService(state, owner, settings);
        }

        [Fact]
        public void SendPress_WithAnExplicitHandler_SendsAndRecordsTheCounters()
        {
            var service = CreateService(out var state, out _, out _);
            var handler = new MockVirtualKBM();

            service.SendPress(Device, Key, 65, false, handler);

            Assert.Equal(new uint[] { 65 }, handler.KeyPressCalls.ToArray());
            Assert.Equal(1, state.DeviceState[Device].keyPresses[Key].current.repeatCount);
            Assert.Equal(1, state.GlobalState.keyPresses[Key].current.repeatCount);
            Assert.Equal(65u, state.DeviceState[Device].nativeKeyAlias[Key]);
        }

        [Fact]
        public void SendPressAndRelease_WithoutAnyHandler_DoNothing_LikeTheOldNullHandlerCheck()
        {
            var service = CreateService(out var state, out var owner, out _);
            Assert.Null(owner.Handler);

            service.SendPress(Device, Key, 65, false);
            service.SendRelease(Device, Key, 65, false);

            Assert.Empty(state.DeviceState[Device].keyPresses);
            Assert.Empty(state.GlobalState.keyPresses);
        }

        [Fact]
        public void SendPressAndRelease_WithoutAnExplicitHandler_UseTheOwnedHandler_AndRelease_ClearsTheCounters()
        {
            var service = CreateService(out var state, out var owner, out _);
            var recorder = new RecordingVirtualKbmBase();
            owner.Handler = recorder;

            service.SendPress(Device, Key, 65, false);
            service.SendPress(Device, Key, 65, false);
            Assert.Equal(2, state.DeviceState[Device].keyPresses[Key].current.repeatCount);

            service.SendRelease(Device, Key, 65, false);

            Assert.Equal(new uint[] { 65 }, recorder.KeyReleases.ToArray());
            Assert.Equal(0, state.DeviceState[Device].keyPresses[Key].current.repeatCount);
            Assert.Equal(0, state.GlobalState.keyPresses[Key].current.repeatCount);
        }

        [Fact]
        public void ResetKeyTiming_ZeroesTheCounters()
        {
            var service = CreateService(out var state, out _, out _);
            service.SendPress(Device, Key, 65, false, new MockVirtualKBM());

            service.ResetKeyTiming(Device, Key);

            Assert.Equal(0, state.DeviceState[Device].keyPresses[Key].current.repeatCount);
            Assert.Equal(0L, state.DeviceState[Device].keyPresses[Key].current.lastSyntheticSendUtcTicks);
        }

        [Fact]
        public void ResolveNativeKey_UsesTheProfileSettingsMapping_AndFallsBackToTheLogicalKey()
        {
            var service = CreateService(out _, out _, out var settings);
            settings.OutputKBMMapping = null;
            Assert.Equal(65u, service.ResolveNativeKey(65));

            var mapping = VirtualKBMFactory.GetMappingInstance();
            mapping.PopulateConstants();
            mapping.PopulateMappings();
            settings.OutputKBMMapping = mapping;
            Assert.Equal(mapping.GetRealEventKey(65), service.ResolveNativeKey(65));
        }

        [Fact]
        public void TheStaticBridge_IsGone()
        {
            // Phase14c-Step3（R-06）: 旧 静的な橋渡し SyntheticDispatcher（Bind で束縛）は撤廃した。キーの操作の部品は、登録簿・ファクトリーから送出役を受け取る
            Assert.Null(typeof(ISyntheticDispatcher).Assembly.GetType("DS4Windows.Processing.SyntheticDispatcher"));
            Assert.Same(TestHost.GetService<ISyntheticDispatcher>(), TestHost.GetService<DS4Windows.Processing.Abstractions.IKeyButtonControllerRegistry>().Dispatcher);
        }

        [Fact]
        public void Host_SharesOneAdapterBetweenTheSenderAndTheLifecycle_AndBindsTheBridge()
        {
            DS4Windows.Composition.AppHost.CreateHost();
            var adapter = TestHost.GetService<OutputKBMHandlerAdapter>();
            Assert.Same(adapter, TestHost.GetService<IVirtualKBM>());
            Assert.NotNull(TestHost.GetService<IVirtualKBMLifecycle>());
            Assert.NotNull(TestHost.GetService<ISyntheticDispatcher>());
            Assert.Same(TestHost.GetService<ISyntheticDispatcher>(), TestHost.GetService<ISyntheticDispatcher>());
        }

        [Fact]
        public void ActionsLoadedRuntimeReset_ResetsTheInjectedHolders()
        {
            var state = new SyntheticInputState();
            state.DeviceState[Device].keyPresses[Key] = new SyntheticState.KeyPresses();
            var oldGlobal = state.GlobalState;

            new ActionsLoadedRuntimeReset(state, TestActions.Manager).Reset();

            Assert.Empty(state.DeviceState[Device].keyPresses);
            Assert.NotSame(oldGlobal, state.GlobalState);
        }
    }
}
