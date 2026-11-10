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
    /// Phase13-Step8 8-a: 残る遅延取得（IMapping／IAutoProfileService／IActionBindingBuilder）について、
    /// 利用者が対象の依存グラフの内側にあるか（＝対象を直接受けると循環するか）を、登録から静的に求める。結果は Phase13-Step8-Plan.md §2 の根拠。
    /// 求め方は ControlServiceResolutionGraphProbeTests と同じ（Func は辺にしない）。
    /// </summary>
    public class LazyDependencyResolutionGraphProbeTests
    {
        private readonly ITestOutputHelper _output;

        public LazyDependencyResolutionGraphProbeTests(ITestOutputHelper output)
        {
            _output = output;
        }

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
            [typeof(IInputDeviceRegistry)] = typeof(InputDeviceRegistryAdapter),
            [typeof(IOutputDeviceTypeState)] = typeof(OutputDeviceTypeState),
        };

        private static Type ImplementationOf(IServiceCollection services, Type serviceType)
        {
            if (FactoryTypes.TryGetValue(serviceType, out Type factory)) return factory;
            ServiceDescriptor d = services.LastOrDefault(x => x.ServiceType == serviceType);
            if (d == null) return serviceType.IsClass && !serviceType.IsAbstract ? serviceType : null;
            return d.ImplementationType ?? (d.ImplementationFactory == null ? d.ImplementationInstance?.GetType() : null);
        }

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
                    if (pt.IsGenericType && pt.GetGenericTypeDefinition() == typeof(Func<>)) continue;
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

        // 対象（遅延の中身）, 利用者（実装の型）, 利用者が対象の依存グラフの内側か（true なら直接受けると循環）
        [Theory]
        [InlineData(typeof(IMapping), typeof(DefaultActionManager))]
        [InlineData(typeof(IMapping), typeof(DefaultMacroPlayer))]
        [InlineData(typeof(IMapping), typeof(DefaultMacroStringPlayer))]
        [InlineData(typeof(IMapping), typeof(ProfileSwitchAction))]
        [InlineData(typeof(IMapping), typeof(SpecialActionOutputFactory))]
        [InlineData(typeof(IMapping), typeof(ActionBindingBuilder))]
        [InlineData(typeof(IAutoProfileService), typeof(MainWindowFactory))]
        [InlineData(typeof(IAutoProfileService), typeof(ViewModelFactory))]
        [InlineData(typeof(IActionBindingBuilder), typeof(ProfileApplicationService))]
        public void Probe_PrintsWhetherTheUserIsInsideTheGraphOfTheTarget(Type target, Type user)
        {
            IServiceCollection services = Registrations();
            HashSet<Type> closure = Closure(services, target);
            bool inside = closure.Contains(user);
            // 利用者の側の依存グラフに対象が含まれるか（逆向き）も出す。両方向なら循環
            HashSet<Type> userClosure = Closure(services, user);
            bool targetInUser = userClosure.Contains(ImplementationOf(services, target) ?? target);
            _output.WriteLine($"{target.Name} <- {user.Name}: 利用者が対象のグラフの内側 = {inside}（対象のグラフ {closure.Count} 型）／対象が利用者のグラフの内側 = {targetInUser}");
        }
    }
}
