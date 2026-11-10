using System;
using System.Collections.Generic;
using Xunit;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Input.Devices;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services;

namespace DS4WindowsTests
{
    /// <summary>
    /// デバイスの種類ごとの部品（モジュール）の値の特性化。
    /// 旧 <c>ControlService.CheckForSupportedDevice</c> の判定を、このファイルの中の逐語コピーと突き合わせる（差分テスト）。
    /// <c>InputDeviceCore</c> は作れないため、デバイスを必要としない判定（有効設定、部品の引き当て）を調べる。
    /// </summary>
    public class DeviceModuleTests
    {
        private static readonly InputDeviceType[] AllTypes = (InputDeviceType[])Enum.GetValues(typeof(InputDeviceType));

        // ---- 旧 ControlService.CheckForSupportedDevice の switch（逐語）----
        private static bool OldIsSupported(ControlServiceDeviceOptions deviceOptions, InputDeviceType inputDevType)
        {
            bool result = false;
            switch (inputDevType)
            {
                case InputDeviceType.DS4:
                    result = deviceOptions[InputDeviceFamily.DS4].Enabled;
                    break;
                case InputDeviceType.DualSense:
                    result = deviceOptions[InputDeviceFamily.DualSense].Enabled;
                    break;
                case InputDeviceType.SwitchPro:
                    result = deviceOptions[InputDeviceFamily.SwitchPro].Enabled;
                    break;
                default:
                    break;
            }

            return result;
        }

        private static IInputDeviceModuleRegistry NewRegistry()
        {
            return new InputDeviceModuleRegistry(new IInputDeviceModule[]
            {
                new Ds4DeviceModule(), new DualSenseDeviceModule(), new SwitchProDeviceModule(),
            });
        }

        [Fact]
        public void IsEnabled_MatchesTheOldSwitch_ForEveryTypeAndEveryCombinationOfTheFlags()
        {
            var registry = NewRegistry();
            for (int mask = 0; mask < 8; mask++)
            {
                var options = new ControlServiceDeviceOptions();
                options[InputDeviceFamily.DS4].Enabled = (mask & 1) != 0;
                options[InputDeviceFamily.DualSense].Enabled = (mask & 2) != 0;
                options[InputDeviceFamily.SwitchPro].Enabled = (mask & 4) != 0;

                foreach (InputDeviceType type in AllTypes)
                {
                    Assert.Equal(OldIsSupported(options, type), registry.Resolve(type).IsEnabled(options));
                }
            }
        }

        [Fact]
        public void IsEnabled_IsFalse_ForAnUnknownType()
        {
            var options = new ControlServiceDeviceOptions();
            options[InputDeviceFamily.DS4].Enabled = true;
            options[InputDeviceFamily.DualSense].Enabled = true;
            options[InputDeviceFamily.SwitchPro].Enabled = true;

            var unknown = (InputDeviceType)99;
            Assert.False(NewRegistry().Resolve(unknown).IsEnabled(options));
            Assert.False(OldIsSupported(options, unknown));
        }

        [Fact]
        public void Resolve_FindsExactlyOneModule_PerType()
        {
            var registry = NewRegistry();
            Assert.IsType<Ds4DeviceModule>(registry.Resolve(InputDeviceType.DS4));
            Assert.IsType<DualSenseDeviceModule>(registry.Resolve(InputDeviceType.DualSense));
            Assert.IsType<SwitchProDeviceModule>(registry.Resolve(InputDeviceType.SwitchPro));
            Assert.IsType<NullInputDeviceModule>(registry.Resolve((InputDeviceType)99));

            // どの種類も、扱う部品がちょうど 1 つ
            var modules = new IInputDeviceModule[]
            {
                new Ds4DeviceModule(), new DualSenseDeviceModule(), new SwitchProDeviceModule(),
            };
            foreach (InputDeviceType type in AllTypes)
            {
                int count = 0;
                foreach (var module in modules)
                {
                    if (module.Handles(type)) count++;
                }

                Assert.Equal(1, count);
            }
        }

        [Fact]
        public void ModulesThatHaveNoSpecificProcessing_DoNothing_AndReturnNoExtraButtons()
        {
            // デバイスを使わないメンバーだけ（null のデバイスを渡しても、何もしない）
            var options = new ControlServiceDeviceOptions();
            foreach (IInputDeviceModule module in new IInputDeviceModule[] { new Ds4DeviceModule(), new NullInputDeviceModule() })
            {
                Assert.Empty(module.GetKnownExtraButtons(null));
                Assert.Null(Record.Exception(() => module.OnDeviceInit(null, options)));
                Assert.Null(Record.Exception(() => module.HookSettings(null, () => 0)));
                Assert.Null(Record.Exception(() => module.ApplyProfileOptions(null, 0, TestHost.GetService<IProfileSettingsService>())));
            }

            Assert.Equal(new List<DS4Controls> { DS4Controls.Capture }, new SwitchProDeviceModule().GetKnownExtraButtons(null));
        }
    }
}
