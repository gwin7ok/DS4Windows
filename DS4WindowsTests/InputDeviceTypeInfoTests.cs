using System;
using System.Collections.Generic;
using System.Linq;
using DS4Windows.Core.Model;
using DS4Windows.Input.Abstractions;
using DS4Windows.Processing.Abstractions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 種類と系統の表（<see cref="InputDeviceTypeInfo"/>）の確認。表は <see cref="InputDeviceType"/> の全値を持ち、
    /// カートリッジ（第 1 層）と接続フック（第 2 層）が、どの種類についても同じ系統の判定をする（種類ごとの判定の二重化を防ぐ）。
    /// </summary>
    public class InputDeviceTypeInfoTests
    {
        [Fact]
        public void EveryType_HasAFamily_AndTheExpectedOne()
        {
            var expected = new Dictionary<InputDeviceType, InputDeviceFamily>
            {
                [InputDeviceType.DS4] = InputDeviceFamily.DS4,
                [InputDeviceType.SwitchPro] = InputDeviceFamily.SwitchPro,
                [InputDeviceType.DualSense] = InputDeviceFamily.DualSense,
            };

            foreach (InputDeviceType type in Enum.GetValues(typeof(InputDeviceType)))
            {
                Assert.Equal(expected[type], InputDeviceTypeInfo.FamilyOf(type));
            }

            Assert.Equal(expected.Count, Enum.GetValues(typeof(InputDeviceType)).Length);
            Assert.Throws<ArgumentOutOfRangeException>(() => InputDeviceTypeInfo.FamilyOf((InputDeviceType)99));
        }

        [Fact]
        public void EveryType_IsHandledByExactlyOneCartridge_AndOneModule_OfTheSameFamily()
        {
            var cartridges = TestHost.GetService<IEnumerable<IInputDeviceCartridge>>().ToList();
            var modules = TestHost.GetService<IEnumerable<IInputDeviceModule>>().ToList();
            foreach (InputDeviceType type in Enum.GetValues(typeof(InputDeviceType)))
            {
                Assert.Single(cartridges, c => c.Handles(type));
                Assert.Single(modules, m => m.Handles(type));
                // 同じ系統のカートリッジとモジュールが、同じ種類の集合を扱う
                foreach (InputDeviceType other in Enum.GetValues(typeof(InputDeviceType)))
                {
                    bool sameFamily = InputDeviceTypeInfo.FamilyOf(type) == InputDeviceTypeInfo.FamilyOf(other);
                    Assert.Equal(sameFamily, cartridges.Single(c => c.Handles(type)).Handles(other));
                    Assert.Equal(sameFamily, modules.Single(m => m.Handles(type)).Handles(other));
                }
            }
        }
    }
}
