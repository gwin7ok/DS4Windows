using System;
using System.IO;
using System.Reflection;
using System.Windows;
using Xunit;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Model;
using DS4Windows.Core.Profiles;
using DS4Windows.Core.Settings;
using DS4Windows.Processing.Abstractions;
using DS4Windows.UI.Abstractions;
using DS4Windows.UI.ViewModels;

namespace DS4WindowsTests
{
    [Collection(SharedHostStateCollection.Name)]
    public class PatternCViewModelTests
    {
        static PatternCViewModelTests()
        {
            if (string.IsNullOrEmpty(TestGlobals.AppDataPath))
            {
                TestGlobals.AppDataPath = AppContext.BaseDirectory;
            }

            if (Application.Current == null)
            {
                try
                {
                    _ = new Application();
                }
                catch { }
            }

            if (Application.ResourceAssembly == null)
            {
                try
                {
                    Application.ResourceAssembly = typeof(DS4Windows.Composition.AppHost).Assembly;
                }
                catch { }
            }

            if (Application.Current != null)
            {
                try
                {
                    var uri = new Uri("pack://application:,,,/DS4Windows;component/Resources/Resources.xaml", UriKind.Absolute);
                    var dict = new ResourceDictionary { Source = uri };
                    Application.Current.Resources.MergedDictionaries.Add(dict);
                }
                catch { }

                string[] resourceKeys = new string[]
                {
                    "KeyDownImg", "KeyUpImg", "KeyWaitImg", "KeyHoldImg",
                    "ClockImg", "MouseImg", "CustomKeyImg", "TouchpadImg",
                    "GyroImg", "WheelImg", "UnboundImg", "WaitImg", "HoldImg",
                    "LeftImg", "RightImg", "MiddleImg", "Clock", "Mouse", "CustomKey"
                };

                foreach (var key in resourceKeys)
                {
                    if (!Application.Current.Resources.Contains(key))
                    {
                        Application.Current.Resources[key] = "";
                    }
                }
            }
        }

        [Fact]
        public void AppHost_ShouldResolve_IViewModelFactory()
        {
            DS4Windows.Composition.AppHost.CreateHost();

            var factory = TestHost.GetService<IViewModelFactory>();
            Assert.NotNull(factory);
        }

        [Fact]
        public void ViewModelFactory_ShouldCreate_ProfileSettingsViewModel()
        {
            DS4Windows.Composition.AppHost.CreateHost();

            var factory = TestHost.GetService<IViewModelFactory>();
            Assert.NotNull(factory);

            var vm = factory.CreateProfileSettingsViewModel(0);
            Assert.NotNull(vm);
            // targetDevice を省略した場合は「実機なし」（Phase6-Step7b-1）
            Assert.Equal(-1, vm.TargetDevice);
        }

        /// <summary>
        /// Phase6-Step7b-1: 実際の DI ホストから解決したファクトリ経由でも、targetDevice が ViewModel に届き、
        /// 編集スロット（device）と別に保持されること。テスト用に new したファクトリではなく、
        /// アプリと同じ AppHost.GetService の経路で確認する（Phase6-Status.md K7-1 の教訓）。
        /// </summary>
        [Fact]
        public void ViewModelFactory_PassesTargetDevice_ToProfileSettingsViewModel()
        {
            DS4Windows.Composition.AppHost.CreateHost();

            var factory = TestHost.GetService<IViewModelFactory>();
            Assert.NotNull(factory);

            var outputSlotService = TestHost.GetService<IOutputSlotService>();
            OutContType savedOutDevTypeTemp = outputSlotService.OutDevTypeTemp[DS4Windows.Core.Settings.ControllerLimits.TestProfileIndex];
            try
            {
                // CURRENT_DS4_CONTROLLER_LIMIT は 4 または 8 のため、どちらでも範囲内の 1 を使う
                var vm = factory.CreateProfileSettingsViewModel(DS4Windows.Core.Settings.ControllerLimits.TestProfileIndex, 1);
                Assert.NotNull(vm);
                Assert.Equal(DS4Windows.Core.Settings.ControllerLimits.TestProfileIndex, vm.Device);
                Assert.Equal(1, vm.TargetDevice);
                Assert.Equal(1, vm.FuncDevNum);
            }
            finally
            {
                outputSlotService.OutDevTypeTemp[DS4Windows.Core.Settings.ControllerLimits.TestProfileIndex] = savedOutDevTypeTemp;
            }
        }

        [Fact]
        public void ViewModelFactory_ShouldCreate_RecordBoxViewModel()
        {
            DS4Windows.Composition.AppHost.CreateHost();

            var factory = TestHost.GetService<IViewModelFactory>();
            Assert.NotNull(factory);

            var settings = new DS4ControlSettings(DS4Controls.Cross);
            // Phase6-Step7b-2: targetDevice を省略（-1＝実機なし）するため、以前のように
            // TouchOutMode[0] を Passthru にしたまま戻さない副作用は起きない
            var vm = factory.CreateRecordBoxViewModel(0, settings, false);
            Assert.NotNull(vm);
            Assert.Equal(-1, vm.TargetDevice);
        }

        /// <summary>
        /// Phase6-Step7b-2: 実際の DI ホストから解決したファクトリ経由でも、targetDevice が RecordBoxViewModel に届き、
        /// 記録中のタッチパッド Passthru が実機のスロットに設定され、RevertControlsSettings で元に戻ること
        /// （Phase6-Status.md K7-1 の教訓により、アプリと同じ AppHost.GetService の経路で確認する）。
        /// </summary>
        [Fact]
        public void ViewModelFactory_PassesTargetDevice_ToRecordBoxViewModel()
        {
            DS4Windows.Composition.AppHost.CreateHost();

            var factory = TestHost.GetService<IViewModelFactory>();
            Assert.NotNull(factory);
            var profileSettings = TestHost.GetService<IProfileSettingsService>();
            Assert.NotNull(profileSettings);

            // CURRENT_DS4_CONTROLLER_LIMIT は 4 または 8 のため、どちらでも範囲内の 1 を使う
            const int targetSlot = 1;
            TouchpadOutMode savedTargetMode = profileSettings.ForSlot(targetSlot).Touchpad.OutputMode;
            TouchpadOutMode savedEditSlotMode = profileSettings.ForSlot(DS4Windows.Core.Settings.ControllerLimits.TestProfileIndex).Touchpad.OutputMode;
            RecordBoxViewModel vm = null;
            try
            {
                profileSettings.ForSlot(targetSlot).Touchpad.OutputMode = TouchpadOutMode.Mouse;
                profileSettings.ForSlot(DS4Windows.Core.Settings.ControllerLimits.TestProfileIndex).Touchpad.OutputMode = TouchpadOutMode.Mouse;

                vm = factory.CreateRecordBoxViewModel(DS4Windows.Core.Settings.ControllerLimits.TestProfileIndex,
                    new DS4ControlSettings(DS4Controls.Cross), false, targetSlot);
                Assert.NotNull(vm);
                Assert.Equal(DS4Windows.Core.Settings.ControllerLimits.TestProfileIndex, vm.DeviceNum);
                Assert.Equal(targetSlot, vm.TargetDevice);
                Assert.Equal(TouchpadOutMode.Passthru, profileSettings.ForSlot(targetSlot).Touchpad.OutputMode);
                Assert.Equal(TouchpadOutMode.Mouse, profileSettings.ForSlot(DS4Windows.Core.Settings.ControllerLimits.TestProfileIndex).Touchpad.OutputMode);

                vm.RevertControlsSettings();
                Assert.Equal(TouchpadOutMode.Mouse, profileSettings.ForSlot(targetSlot).Touchpad.OutputMode);
            }
            finally
            {
                vm?.RevertControlsSettings();
                profileSettings.ForSlot(targetSlot).Touchpad.OutputMode = savedTargetMode;
                profileSettings.ForSlot(DS4Windows.Core.Settings.ControllerLimits.TestProfileIndex).Touchpad.OutputMode = savedEditSlotMode;
            }
        }

        [Fact]
        public void ViewModelFactory_ShouldCreate_AutoProfilesViewModel()
        {
            DS4Windows.Composition.AppHost.CreateHost();

            var factory = TestHost.GetService<IViewModelFactory>();
            Assert.NotNull(factory);

            var profileList = new ProfileList(TestHost.GetService<DS4Windows.Core.Abstractions.IPathService>());
            var holder = TestRepositoryFactory.CreateAutoProfileHolder();
            var vm = factory.CreateAutoProfilesViewModel(holder, profileList);
            Assert.NotNull(vm);
        }
    }
}
