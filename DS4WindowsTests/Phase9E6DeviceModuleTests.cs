using System;
using System.Collections.Generic;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.InputDevices;
using DS4Windows.Services;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-E E-6c（Phase9-E6-Plan.md §3.1・§3.2）: デバイスの種類ごとの部品（カートリッジ）の値の特性化。
    /// 旧 <c>ControlService.CheckForSupportedDevice</c>／<c>PostDS4DeviceInit</c> の判定を、このファイルの中の逐語コピーと突き合わせる（差分テスト）。
    /// <c>DS4Device</c>／<c>JoyConDevice</c> は作れないため、デバイスを必要としない判定（有効設定、結合の初期値、部品の引き当て）を調べる。
    /// </summary>
    public class Phase9E6DeviceModuleTests
    {
        private static readonly InputDeviceType[] AllTypes = (InputDeviceType[])Enum.GetValues(typeof(InputDeviceType));

        // ---- 旧 ControlService.CheckForSupportedDevice の switch（逐語）----
        private static bool OldIsSupported(ControlServiceDeviceOptions deviceOptions, InputDeviceType inputDevType)
        {
            bool result = false;
            switch (inputDevType)
            {
                case InputDeviceType.DS4:
                    result = deviceOptions.DS4DeviceOpts.Enabled;
                    break;
                case InputDeviceType.DualSense:
                    result = deviceOptions.DualSenseOpts.Enabled;
                    break;
                case InputDeviceType.SwitchPro:
                    result = deviceOptions.SwitchProDeviceOpts.Enabled;
                    break;
                case InputDeviceType.JoyConL:
                case InputDeviceType.JoyConR:
                case InputDeviceType.JoyConGrip:
                    result = deviceOptions.JoyConDeviceOpts.Enabled;
                    break;
                case InputDeviceType.DS3:
                    result = deviceOptions.DS3DeviceOpts.Enabled;
                    break;
                default:
                    break;
            }

            return result;
        }

        // ---- 旧 ControlService.PostDS4DeviceInit の判定部分（逐語。デバイスへの代入は戻り値にした）----
        private static (bool applied, bool merge, bool primary, bool gyro) OldJoin(InputDeviceType type, ControlServiceDeviceOptions deviceOptions)
        {
            bool applied = false, merge = false, primary = false, gyro = false;
            if (type == InputDeviceType.JoyConL || type == InputDeviceType.JoyConR)
            {
                if (deviceOptions.JoyConDeviceOpts.LinkedMode == JoyConDeviceOptions.LinkMode.Joined)
                {
                    applied = true;
                    merge = true;

                    if (type == InputDeviceType.JoyConL)
                    {
                        primary = true;
                        if (deviceOptions.JoyConDeviceOpts.JoinGyroProv == JoyConDeviceOptions.JoinedGyroProvider.JoyConL)
                        {
                            gyro = true;
                        }
                        else
                        {
                            gyro = false;
                        }
                    }
                    else
                    {
                        primary = false;
                        if (deviceOptions.JoyConDeviceOpts.JoinGyroProv == JoyConDeviceOptions.JoinedGyroProvider.JoyConR)
                        {
                            gyro = true;
                        }
                        else
                        {
                            gyro = false;
                        }
                    }
                }
            }

            return (applied, merge, primary, gyro);
        }

        private static IInputDeviceModuleRegistry NewRegistry()
        {
            return new InputDeviceModuleRegistry(new IInputDeviceModule[]
            {
                new Ds4DeviceModule(), new DualSenseDeviceModule(), new SwitchProDeviceModule(), new JoyConDeviceModule(), new Ds3DeviceModule(),
            });
        }

        [Fact]
        public void IsEnabled_MatchesTheOldSwitch_ForEveryTypeAndEveryCombinationOfTheFlags()
        {
            var registry = NewRegistry();
            for (int mask = 0; mask < 32; mask++)
            {
                var options = new ControlServiceDeviceOptions();
                options.DS4DeviceOpts.Enabled = (mask & 1) != 0;
                options.DualSenseOpts.Enabled = (mask & 2) != 0;
                options.SwitchProDeviceOpts.Enabled = (mask & 4) != 0;
                options.JoyConDeviceOpts.Enabled = (mask & 8) != 0;
                options.DS3DeviceOpts.Enabled = (mask & 16) != 0;

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
            options.DS4DeviceOpts.Enabled = true;
            options.DualSenseOpts.Enabled = true;
            options.SwitchProDeviceOpts.Enabled = true;
            options.JoyConDeviceOpts.Enabled = true;
            options.DS3DeviceOpts.Enabled = true;

            var unknown = (InputDeviceType)99;
            Assert.False(NewRegistry().Resolve(unknown).IsEnabled(options));
            Assert.False(OldIsSupported(options, unknown));
        }

        [Fact]
        public void Resolve_FindsExactlyOneModule_PerType_AndTheJoyConModuleHandlesAllThreeJoyConTypes()
        {
            var registry = NewRegistry();
            Assert.IsType<Ds4DeviceModule>(registry.Resolve(InputDeviceType.DS4));
            Assert.IsType<DualSenseDeviceModule>(registry.Resolve(InputDeviceType.DualSense));
            Assert.IsType<SwitchProDeviceModule>(registry.Resolve(InputDeviceType.SwitchPro));
            Assert.IsType<Ds3DeviceModule>(registry.Resolve(InputDeviceType.DS3));
            Assert.IsType<JoyConDeviceModule>(registry.Resolve(InputDeviceType.JoyConL));
            Assert.IsType<JoyConDeviceModule>(registry.Resolve(InputDeviceType.JoyConR));
            Assert.IsType<JoyConDeviceModule>(registry.Resolve(InputDeviceType.JoyConGrip));
            Assert.IsType<NullInputDeviceModule>(registry.Resolve((InputDeviceType)99));

            // どの種類も、扱う部品がちょうど 1 つ
            var modules = new IInputDeviceModule[]
            {
                new Ds4DeviceModule(), new DualSenseDeviceModule(), new SwitchProDeviceModule(), new JoyConDeviceModule(), new Ds3DeviceModule(),
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
        public void DecideJoin_MatchesTheOldPostInit_ForEveryTypeModeAndGyroProvider()
        {
            foreach (InputDeviceType type in AllTypes)
            {
                foreach (JoyConDeviceOptions.LinkMode mode in Enum.GetValues(typeof(JoyConDeviceOptions.LinkMode)))
                {
                    foreach (JoyConDeviceOptions.JoinedGyroProvider provider in Enum.GetValues(typeof(JoyConDeviceOptions.JoinedGyroProvider)))
                    {
                        var options = new ControlServiceDeviceOptions();
                        options.JoyConDeviceOpts.LinkedMode = mode;
                        options.JoyConDeviceOpts.JoinGyroProv = provider;

                        var expected = OldJoin(type, options);

                        // OnDeviceInit は L／R のときだけ DecideJoin を使う（Grip などは何もしない）
                        bool isLeftOrRight = type == InputDeviceType.JoyConL || type == InputDeviceType.JoyConR;
                        bool applied = false, primary = false, gyro = false;
                        if (isLeftOrRight)
                        {
                            applied = JoyConDeviceModule.DecideJoin(type, mode, provider, out primary, out gyro);
                        }

                        if (!isLeftOrRight)
                        {
                            Assert.False(expected.applied);
                            continue;
                        }

                        Assert.Equal(expected.applied, applied);
                        if (applied)
                        {
                            Assert.True(expected.merge);
                            Assert.Equal(expected.primary, primary);
                            Assert.Equal(expected.gyro, gyro);
                        }
                    }
                }
            }
        }

        [Fact]
        public void ModulesThatHaveNoSpecificProcessing_DoNothing_AndReturnNoExtraButtons()
        {
            // デバイスを使わないメンバーだけ（null のデバイスを渡しても、何もしない）
            var options = new ControlServiceDeviceOptions();
            foreach (IInputDeviceModule module in new IInputDeviceModule[] { new Ds4DeviceModule(), new Ds3DeviceModule(), new NullInputDeviceModule() })
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
