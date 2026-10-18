using System;
using DS4Windows;
using DS4Windows.Actions;
using DS4Windows.DI;
using DS4Windows.Services;

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
            ISyntheticDispatcher syntheticDispatcher = null)
        {
            DS4WinWPF.AppHost.CreateHost();
            var synthetic = syntheticInput ?? new SyntheticInputState();
            var profileSettings = settings ?? TestHost.GetService<IProfileSettingsService>();
            return new Mapping(
                synthetic,
                macroRuntime ?? new MacroRuntimeState(),
                stickMotion ?? new StickMotionState(),
                profileSettings,
                TestHost.GetService<IProfileApplicationService>(),
                virtualKbm ?? TestHost.GetService<IVirtualKBM>(),
                TestHost.GetService<IKeyButtonControllerRegistry>(),
                TestHost.GetService<IProcessLauncher>(),
                TestHost.GetService<IProfileSwitcher>(),
                // 独立の合成状態（synthetic）に対して送出する、独立の送出役（出力先の既定はホストのアダプター）
                syntheticDispatcher ?? new SyntheticDispatcherService(synthetic, TestHost.GetService<OutputKBMHandlerAdapter>(), profileSettings),
                () => TestHost.GetService<IDeviceStateAccessor>());
        }
    }
}
