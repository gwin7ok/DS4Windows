using System.Linq;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;
using DS4Windows.Processing.Abstractions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 入力機種（系統）の登録の整合を守る。機種は <c>AddInputDevice&lt;カートリッジ, モジュール&gt;()</c> の 1 行で、
    /// 第 1 層のカートリッジと第 2 層の接続フックが同じ並びで揃う。種類と系統の対応は enum の属性が持つ。
    /// </summary>
    public class InputDeviceRegistrationTests
    {
        [Fact]
        public void EveryFamily_HasExactlyOneCartridge_AndOneModule_InTheSameOrder()
        {
            DS4Windows.Composition.AppHost.CreateHost();
            InputDeviceFamily[] cartridges = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetServices<IInputDeviceCartridge>(TestHost.Ensure().Services).Select(c => c.Family).ToArray();
            InputDeviceFamily[] modules = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetServices<IInputDeviceModule>(TestHost.Ensure().Services)
                .Select(m => System.Enum.GetValues(typeof(InputDeviceFamily)).Cast<InputDeviceFamily>()
                    .First(f => m.Handles(FirstTypeOf(f))))
                .ToArray();

            Assert.Equal(System.Enum.GetValues(typeof(InputDeviceFamily)).Length, cartridges.Distinct().Count());
            Assert.Equal(cartridges, modules);
        }

        [Fact]
        public void CartridgeHandles_FollowsTheFamilyOfEachType()
        {
            DS4Windows.Composition.AppHost.CreateHost();
            foreach (IInputDeviceCartridge cartridge in Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetServices<IInputDeviceCartridge>(TestHost.Ensure().Services))
            {
                foreach (InputDeviceType type in System.Enum.GetValues(typeof(InputDeviceType)))
                {
                    Assert.Equal(InputDeviceTypeInfo.FamilyOf(type) == cartridge.Family, cartridge.Handles(type));
                }
            }
        }

        [Fact]
        public void EveryInputDeviceType_DeclaresItsFamily()
        {
            foreach (InputDeviceType type in System.Enum.GetValues(typeof(InputDeviceType)))
            {
                var attr = (InputDeviceFamilyAttribute)System.Attribute.GetCustomAttribute(
                    typeof(InputDeviceType).GetField(type.ToString()), typeof(InputDeviceFamilyAttribute));
                Assert.NotNull(attr);
                Assert.Equal(attr.Family, InputDeviceTypeInfo.FamilyOf(type));
            }
        }

        private static InputDeviceType FirstTypeOf(InputDeviceFamily family)
        {
            return System.Enum.GetValues(typeof(InputDeviceType)).Cast<InputDeviceType>()
                .First(t => InputDeviceTypeInfo.FamilyOf(t) == family);
        }
    }
}
