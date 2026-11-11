using DS4Windows.Processing.Macro;
using DS4Windows.Processing.Synthetic;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Core.Abstractions;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Actions;
using DS4Windows.Processing.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase7-Step8 W2: <c>Mapping</c> を DI の Singleton（<c>IMapping</c>）の instance にしたこと、サービスへの静的な束縛
    /// （<c>AppHost</c>／<c>ServiceProviderHolder</c>／<c>Global</c> のフォールバック）をコンストラクター注入に替えたこと、
    /// キー／ボタンのコントローラー登録簿を <c>KeyButtonControllerRegistry</c> に切り出したことの回帰ガード。
    /// ソースが見つからない環境では、ソースの検査を行わず合格とする。判断の根拠は Phase7-Step8-Plan.md（Q1〜Q4・Q8）。
    /// </summary>
    [Collection(SharedHostStateCollection.Name)]
    public class MappingInstanceGuardTests
    {
        // 状態を持たない表（読み取り専用の運用）。Q2＝A により static のまま残すもの
        private static readonly string[] AllowedStaticFields =
        {
            // Phase8-Fix-MacroRepeatUnify（D5）: ds4ControlMapping／macroEndIndex は、macrodone の添字の表だったため削除した
            // Phase9 Steering 削除 W2: calibrationColor_0〜3（ステアリング較正のライトバーの色）は、機能ごと削除した
        };

        private readonly ITestOutputHelper _output;

        public MappingInstanceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Mapping_ImplementsIMapping_AndHasNoStaticMutableStateOtherThanTheAllowedTables()
        {
            Assert.True(typeof(IMapping).IsAssignableFrom(typeof(Mapping)));

            var statics = typeof(Mapping)
                .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(f => !f.IsLiteral) // const を除く
                .Select(f => f.Name)
                .OrderBy(n => n)
                .ToArray();

            Assert.Equal(AllowedStaticFields.OrderBy(n => n).ToArray(), statics);
        }

        [Fact]
        public void IMapping_HasExactlyTheTenWindowMembers()
        {
            string[] names = typeof(IMapping).GetMethods().Select(m => m.Name).OrderBy(n => n).ToArray();
            // Phase7b W1: プロファイル切替の復帰の合図の退避・復元（CaptureProfileUntrigger／RestoreProfileUntrigger）を追加した（8 → 10）
            string[] expected =
            {
                "CaptureProfileUntrigger", "Commit", "EndMacroDirect", "GetBoolMappingExternal", "HandleDeviceDisconnect",
                "MapCustom", "PlayMacroDirect", "RestoreProfileUntrigger", "SetCurveAndDeadzone", "TempMouseJoystick",
            };
            Assert.Equal(expected, names);
        }

        [Fact]
        public void Constructor_RequiresEveryDependency()
        {
            ConstructorInfo ctor = typeof(Mapping).GetConstructors().Single();
            ParameterInfo[] parameters = ctor.GetParameters();
            Assert.Equal(15, parameters.Length); // 設計の経緯は git の履歴と Phase16-Plan.md（16-1: 28 → 25。16-10: 25 → 20。16-13a: 20 → 17）。Phase18 後の整理: ログだけに使っていた IManagedActionManager と、個数の取得だけに使っていた ISpecialActionRepository を外し 17 → 15

            // 実 DI ホストの部品で 1 つずつ null にして、ArgumentNullException になることを確認する
            var valid = new object[]
            {
                new SyntheticInputState(), new MacroRuntimeState(), new StickMotionState(),
                TestHost.GetService<IProfileSettingsService>(),
                TestHost.GetService<IVirtualKBM>(),
                TestHost.GetService<IDeviceInputCleanup>(),
                TestHost.GetService<IMacroPlayback>(),   // Phase9 9-C: 旧 Func<IDeviceStateAccessor> の位置
                TestHost.GetService<ITriggerDispatchEngine>(),
                TestHost.GetService<IMouseConversionStage>(),
                TestHost.GetService<ICurveAndDeadzoneStage>(),
                TestHost.GetService<IControlActivityReader>(),
                TestHost.GetService<ISyntheticInputCommitter>(),
                TestHost.GetService<ITwoStageTriggerProcessor>(),
                TestHost.GetService<IDeviceSlotState>(),   // Phase13-Step5b
                TestHost.GetService<IProfileActionProvider>(),   // Phase13-Step5b
            };

            for (int i = 0; i < valid.Length; i++)
            {
                object[] args = (object[])valid.Clone();
                args[i] = null;
                var ex = Assert.Throws<TargetInvocationException>(() => ctor.Invoke(args));
                Assert.IsType<ArgumentNullException>(ex.InnerException);
            }
        }

        [Fact]
        public void Host_ResolvesTheSameMappingInstance_ForControlServiceAndTheContainer()
        {
            DS4Windows.Composition.AppHost.CreateHost();
            var mapping = TestHost.GetService<IMapping>();
            Assert.NotNull(mapping);
            Assert.Same(mapping, TestHost.GetService<IMapping>());
            Assert.Same(mapping, TestHost.GetService<Func<IMapping>>()());
        }

        [Fact]
        public void Registry_ReusesTheControllerPerDeviceAndName_AndClearsOnlyTheGivenDevice()
        {
            var registry = new KeyButtonControllerRegistry(new DefaultKeyButtonActionControllerFactory(new DefaultControllerRegistry(), TestActions.Manager, TestActions.Dispatcher, System.TimeProvider.System), new DefaultControllerRegistry(), TestActions.Manager, TestActions.Dispatcher, System.TimeProvider.System);
            const int deviceA = 6;
            const int deviceB = 7;

            var a1 = registry.GetOrCreate(deviceA, KeyButtonActionController.Mode.Press, "step8-a1");
            var a1again = registry.GetOrCreate(deviceA, KeyButtonActionController.Mode.Press, "step8-a1");
            var a2 = registry.GetOrCreate(deviceA, KeyButtonActionController.Mode.Toggle, "step8-a2");
            var b1 = registry.GetOrCreate(deviceB, KeyButtonActionController.Mode.Press, "step8-b1");

            Assert.NotNull(a1);
            Assert.Same(a1, a1again);
            Assert.NotSame(a1, a2);

            registry.ClearForDevice(deviceA);

            // 破棄されたデバイスのコントローラーは、再度要求すると新しく作られる。他のデバイスは残る
            Assert.NotSame(a1, registry.GetOrCreate(deviceA, KeyButtonActionController.Mode.Press, "step8-a1"));
            Assert.Same(b1, registry.GetOrCreate(deviceB, KeyButtonActionController.Mode.Press, "step8-b1"));

            registry.ClearForDevice(deviceA);
            registry.ClearForDevice(deviceB);
        }

        [Fact]
        public void Registry_RequiresItsDependencies()
        {
            Assert.Throws<ArgumentNullException>(() => new KeyButtonControllerRegistry(null, new DefaultControllerRegistry(), TestActions.Manager, TestActions.Dispatcher, System.TimeProvider.System));
            Assert.Throws<ArgumentNullException>(() => new KeyButtonControllerRegistry(new DefaultKeyButtonActionControllerFactory(new DefaultControllerRegistry(), TestActions.Manager, TestActions.Dispatcher, System.TimeProvider.System), null, TestActions.Manager, TestActions.Dispatcher, System.TimeProvider.System));
            Assert.Contains(typeof(ProfileApplicationService).GetConstructors().Single().GetParameters(), p => p.ParameterType == typeof(IKeyButtonControllerRegistry));
        }
    }
}
