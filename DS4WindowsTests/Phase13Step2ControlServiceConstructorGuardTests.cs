using System;
using System.Linq;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase13-Step2: <see cref="ControlService"/> のコンストラクター引数から、保持するだけで使っていなかった 6 つを削除した（34 → 28）。
    /// 引数の数と、削除した型が戻っていないこと、削除した型が従来どおり ControlService 以外の経路でも生成されることを固定する。
    /// </summary>
    [Xunit.Collection("ServiceProviderHolder")]
    public class Phase13Step2ControlServiceConstructorGuardTests
    {
        private static readonly Type[] Removed =
        {
            typeof(IProfileXmlStore), typeof(IProfileSlotApplier), typeof(SyntheticInputState),
            typeof(IConnectionProfileSelector), typeof(IJoyConLinker), typeof(IDeviceConnector),
        };

        [Fact]
        public void ControlService_Constructor_HasNoUnusedParameters()
        {
            var parameters = typeof(ControlService).GetConstructors().Single().GetParameters();
            Assert.Equal(25, parameters.Length);   // Phase13-Step10 10-c: IOscService を外した（26 → 25）。 Phase13-Step5b-5b-e: 使われなくなった IVirtualKBM／IAppearanceSettingsService／IProfileActionProvider／ISpecialActionRepository を外した（30 → 26）。 Phase13-Step5b-5b-c: IInputReportProcessor を足した（29 → 30）。 Phase13-Step5: スロット状態の持ち主（IDeviceSlotState）を足した（28 → 29。Step5b 以降で減らす）
            foreach (Type t in Removed)
                Assert.DoesNotContain(parameters, p => p.ParameterType == t);
        }

        [Fact]
        public void TheRemovedDependencies_StillResolve_AsSingletons()
        {
            Assert.NotNull(TestHost.GetService<ControlService>());
            foreach (Type t in Removed)
            {
                object a = TestHost.GetService(t);
                Assert.NotNull(a);
                Assert.Same(a, TestHost.GetService(t));
            }
        }
    }
}
