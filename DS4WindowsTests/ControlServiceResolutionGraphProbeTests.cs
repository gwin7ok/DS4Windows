using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using DS4Windows.Composition;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Persistence;
using DS4Windows.Core.Settings;
using DS4Windows.Output.Services;
using DS4Windows.Processing;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Actions;
using DS4Windows.Processing.Services;
using DS4Windows.UI.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase13-Step6 6-a: <c>Func&lt;ControlService&gt;</c> を使う 9 クラスのうち、どれが <c>ControlService</c> の依存グラフの内側にあるか
    /// （内側のクラスは <c>ControlService</c> を直接受けると循環する）を、<c>ServiceRegistration</c> の登録から静的に求めて固定する。
    /// 求め方: コンストラクターの引数の型をたどる（<c>Func&lt;T&gt;</c> は遅延のため辺にしない）。ラムダで登録された型は <see cref="FactoryTypes"/> で実体の型を与える。
    /// 結果は計画書（Phase13-Step6-Plan.md §2.1）の「循環の有無」の根拠になる。
    /// </summary>
    public class ControlServiceResolutionGraphProbeTests
    {
        private readonly ITestOutputHelper _output;

        public ControlServiceResolutionGraphProbeTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>ラムダ（ImplementationFactory）で登録されている型の、実体の型（コンストラクターをたどる対象）。</summary>
        private static readonly Dictionary<Type, Type> FactoryTypes = new Dictionary<Type, Type>
        {
            [typeof(BackingStore)] = typeof(BackingStore),
            [typeof(IProfileSettingsService)] = typeof(ProfileSettingsService),
            [typeof(ISpecialActionRepository)] = typeof(SpecialActionRepository),
            [typeof(IProfileActionProvider)] = typeof(ProfileActionProvider),
            [typeof(IOutputSlotService)] = typeof(OutputSlotService),
            [typeof(ISyntheticDispatcher)] = typeof(SyntheticDispatcherService),
            [typeof(IVirtualKBM)] = typeof(OutputKBMHandlerAdapter),
            [typeof(IDeviceStateAccessor)] = typeof(DeviceSlotState),
            [typeof(IInputReportProcessor)] = typeof(InputReportProcessor),
            [typeof(IDs4DeviceRegistry)] = typeof(Ds4DeviceRegistryAdapter),
            [typeof(ControlService)] = typeof(ControlService),
        };

        private static Type ImplementationOf(IServiceCollection services, Type serviceType)
        {
            if (FactoryTypes.TryGetValue(serviceType, out Type factory)) return factory;
            ServiceDescriptor d = services.LastOrDefault(x => x.ServiceType == serviceType);
            if (d == null) return serviceType.IsClass && !serviceType.IsAbstract ? serviceType : null;
            return d.ImplementationType ?? (d.ImplementationFactory == null ? d.ImplementationInstance?.GetType() : null);
        }

        /// <summary><paramref name="root"/> のコンストラクターから、遅延（Func）でない依存を再帰的にたどった型の集合（実装の型）。</summary>
        private static HashSet<Type> Closure(IServiceCollection services, Type root)
        {
            var seen = new HashSet<Type>();
            var stack = new Stack<Type>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                Type t = stack.Pop();
                Type impl = ImplementationOf(services, t);
                if (impl == null || !seen.Add(impl)) continue;

                ConstructorInfo ctor = impl.GetConstructors().OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
                if (ctor == null) continue;
                foreach (ParameterInfo p in ctor.GetParameters())
                {
                    Type pt = p.ParameterType;
                    if (pt.IsGenericType && pt.GetGenericTypeDefinition() == typeof(Func<>)) continue;   // 遅延は辺にしない
                    if (pt.IsPrimitive || pt == typeof(string) || pt.IsEnum) continue;
                    stack.Push(pt);
                }
            }
            return seen;
        }

        private static IServiceCollection Registrations()
        {
            var services = new ServiceCollection();
            services.RegisterServices();
            return services;
        }

        // 9 利用者（実装の型）と、ControlService の依存グラフの内側にあるか（＝ControlService を直接受けると循環するか）
        [Theory]
        [InlineData(typeof(ProfileSettingsService), true)]
        [InlineData(typeof(ProfileRepository), true)]
        [InlineData(typeof(ProfileApplicationService), true)]
        // 2 つのポートは、束縛のビルダー（Func<IActionBindingBuilder> で遅延）の先にあり、ControlService の構築時には作られない（グラフの外）。
        // 直接 ControlService を受けても循環はしないが、狭い IF にする（§1 の依存の向き。計画書 §2.1）
        [InlineData(typeof(DefaultControlExtrasPort), false)]
        [InlineData(typeof(DefaultBatteryCheckPort), false)]
        [InlineData(typeof(AutoProfileService), false)]
        [InlineData(typeof(EditorWindowFactory), false)]
        [InlineData(typeof(MainWindowFactory), false)]
        [InlineData(typeof(ViewModelFactory), false)]
        public void TheNineUsers_AreInsideOrOutsideTheGraphOfControlService(Type user, bool inside)
        {
            IServiceCollection services = Registrations();
            HashSet<Type> closure = Closure(services, typeof(ControlService));

            _output.WriteLine($"{user.Name}: ControlService の依存グラフの内側 = {closure.Contains(user)}（ControlService の推移的な依存は {closure.Count} 型）");
            Assert.Equal(inside, closure.Contains(user));
        }

        /// <summary>
        /// 新設する <c>IProfileLoadTarget</c> の実装（6-d）が依存する型は、<c>ProfileRepository</c>／<c>ProfileApplicationService</c> に依存しない
        /// （依存すると、それらが <c>IProfileLoadTarget</c> を受けたときに循環する）ことの予備検査。依存先の型で先に確かめる。
        /// </summary>
        [Theory]
        [InlineData(typeof(IDeviceSlotState))]
        [InlineData(typeof(IVirtualPadService))]
        [InlineData(typeof(IDeviceConnectionSetup))]
        [InlineData(typeof(ITouchpadToggleService))]
        [InlineData(typeof(StickMotionState))]
        public void TheDependenciesOfTheProfileLoadTarget_DoNotReachTheProfileRepositoryOrTheApplicationService(Type dependency)
        {
            IServiceCollection services = Registrations();
            HashSet<Type> closure = Closure(services, dependency);

            Assert.DoesNotContain(typeof(ProfileRepository), closure);
            Assert.DoesNotContain(typeof(ProfileApplicationService), closure);
            Assert.DoesNotContain(typeof(ControlService), closure);
        }

        /// <summary>6-b: <c>IDeviceBatteryText</c>（依存: <c>IDeviceStateAccessor</c> のみ）と、<c>ProfileSettingsService</c> が受ける <c>IDeviceStateAccessor</c> は依存なし。</summary>
        [Fact]
        public void TheSlotState_HasNoDependencies()
        {
            IServiceCollection services = Registrations();
            HashSet<Type> closure = Closure(services, typeof(IDeviceStateAccessor));
            Assert.Single(closure);
        }

        /// <summary>
        /// Phase13-Step7 7-a: <c>VirtualPadService</c> ⇄ <c>OutputSlotService</c> の相互参照は、<c>Func</c> を辺にしない閉包では、どちらからも他方・
        /// <c>ControlService</c> に届かない（<c>Func</c> が循環の回避のためだけに使われていることの確認。7-d で <c>Func</c> を撤廃しても循環しない根拠）。
        /// </summary>
        [Fact]
        public void TheVirtualPadService_AndTheOutputSlotService_DoNotReachEachOtherOrTheControlService()
        {
            IServiceCollection services = Registrations();
            HashSet<Type> pad = Closure(services, typeof(VirtualPadService));
            Assert.DoesNotContain(typeof(OutputSlotService), pad);
            Assert.DoesNotContain(typeof(ControlService), pad);

            HashSet<Type> slot = Closure(services, typeof(OutputSlotService));
            Assert.DoesNotContain(typeof(ControlService), slot);
        }

        /// <summary>6-c: 3 つの UI ファクトリーを、ホスト構築より前に解決する経路がない（ファクトリーは ControlService の依存グラフの外）。</summary>
        [Fact]
        public void TheUiFactories_AreNotReachableFromTheGraphOfAnyFuncFreeService()
        {
            IServiceCollection services = Registrations();
            foreach (Type root in new[] { typeof(ProfileSettingsService), typeof(ProfileRepository), typeof(ProfileApplicationService), typeof(Mapping) })
            {
                HashSet<Type> closure = Closure(services, root);
                Assert.DoesNotContain(typeof(EditorWindowFactory), closure);
                Assert.DoesNotContain(typeof(MainWindowFactory), closure);
                Assert.DoesNotContain(typeof(ViewModelFactory), closure);
                Assert.DoesNotContain(typeof(AutoProfileService), closure);
            }
        }
    }
}
