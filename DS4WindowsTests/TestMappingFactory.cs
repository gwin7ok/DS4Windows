using System;
using DS4Windows.Core.Abstractions;
using DS4Windows.Output.Services;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Actions;
using DS4Windows.Processing.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step8 W2: <c>Mapping</c>（旧: static。現在は DI の Singleton の instance）のテスト用の作成の窓口。
    /// 保持クラス（<c>SyntheticInputState</c>／<c>MacroRuntimeState</c>／<c>StickMotionState</c>）は、既定では新品を渡す
    /// （他のテストと状態を共有しない）。サービス（出力先、プロファイル適用、登録簿など）は、実 DI ホストのものを使う。
    /// コンテナの Singleton（<see cref="Host"/>）が必要なテストは、そちらを使う。
    /// </summary>
    public static class TestMappingFactory
    {
        /// <summary>コンテナの Singleton。ホストの保持クラス（<c>Shared</c> と同じ instance）を使う。</summary>
        public static IMapping Host()
        {
            return TestHost.GetService<IMapping>();
        }

        /// <summary>
        /// 独立した <c>Mapping</c> の instance を作る。
        /// <paramref name="settings"/> を省略すると、ホストの <c>IProfileSettingsService</c> を使う。
        /// </summary>
        public static Mapping Create(
            IProfileSettingsService settings = null,
            SyntheticInputState syntheticInput = null,
            MacroRuntimeState macroRuntime = null,
            StickMotionState stickMotion = null,
            IVirtualKBM virtualKbm = null,
            ISyntheticDispatcher syntheticDispatcher = null,
            ITriggerDispatchEngine engine = null)
        {
            DS4Windows.Composition.AppHost.CreateHost();
            var synthetic = syntheticInput ?? new SyntheticInputState();
            var macroState = macroRuntime ?? new MacroRuntimeState();
            var profileSettings = settings ?? TestHost.GetService<IProfileSettingsService>();
            var kbm = virtualKbm ?? TestHost.GetService<IVirtualKBM>();
            // Phase8-Step4 W4: 抑制の集合を持つエンジンも、独立の instance にする（他のテストと状態を共有しない。ホストの登録簿は使わない）
            var triggerEngine = engine ?? new TriggerDispatchEngine(new ActionBindingRegistry(),
                TestHost.GetService<IManagedActionManager>(), kbm);
            // 独立の合成状態・マクロの再生に対して後始末する、独立の instance（他のテストと状態を共有しない）
            var dispatcher = syntheticDispatcher ?? new SyntheticDispatcherService(synthetic, TestHost.GetService<OutputKBMHandlerAdapter>(), profileSettings);
            var playback = new MacroPlayback(TestLightbar.State, new MacroStepExecutor(TestLightbar.State, kbm, profileSettings, macroState, TestHost.GetService<IDeviceStateAccessor>(), new RecordingWindowSystem()), macroState, TestActions.Manager, System.TimeProvider.System);
            var cleanup = new DeviceInputCleanup(synthetic, dispatcher, playback, TestHost.GetService<IKeyButtonControllerRegistry>());
            var stickMotionState = stickMotion ?? new StickMotionState();
            return new Mapping(
                synthetic,
                macroState,
                stickMotionState,
                profileSettings,
                kbm,
                cleanup,
                playback,
                triggerEngine,
                // Phase9 9-B: 加速の状態・端数を持つ部品は、独立の instance にする（他のテストと状態を共有しない）
                new MouseConversionStage(new ButtonMouseMovementCalculator(), TestHost.GetService<IFlickStickProcessor>(),
                    new MouseMotionQuantizer(), TestHost.GetService<IGyroMouseStickBlender>()),
                new CurveAndDeadzoneStage(TestHost.GetService<IStickDeadZoneProcessor>(), TestHost.GetService<IOutputCurveProcessor>(),
                    TestHost.GetService<ITriggerDeadZoneProcessor>(), TestHost.GetService<IGyroControlsProcessor>(),
                    TestHost.GetService<IStickInputConditioner>(), stickMotionState),   // 状態は stickMotion を共有する
                TestHost.GetService<IControlActivityReader>(),   // Phase9 9-B: 状態なし
                new SyntheticInputCommitter(synthetic, kbm),
                new TwoStageTriggerProcessor(),
                TestActions.Manager,
                TestHost.GetService<IDeviceSlotState>(),   // Phase13-Step5b: 旧 MapCustom の ctrl 引数の代わり（ホストの共有のものを使う。ControlService と同じ配列）
                TestHost.GetService<IProfileActionProvider>(),
                TestHost.GetService<ISpecialActionRepository>());   // Phase9 MapCustom の分割 M2: 状態なし   // Phase9 MapCustom の分割 M1: ホイールの繰り返しの状態を持つため、独立の instance にする
        }
    }
}
