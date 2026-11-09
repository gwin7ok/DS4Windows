using System;
using System.Windows;
using Xunit;
using DS4Windows.Core.Model;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Settings;
using DS4Windows.UI.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase11 W6 K-W6-4: 編集画面で Emulated Controller（<see cref="ProfileSettingsViewModel.TempControllerIndex"/>）を DS4 ↔ Xbox 360 に変えたとき、
    /// 出力ピッカー（BindingWindow）が Pad タブの種類を決めるのに使う <c>OutDevTypeTemp</c> も、同じ種類へ更新される。
    /// 以前は <c>OutContType</c> だけが変わり、<c>OutDevTypeTemp</c> は編集画面を開いたときにしか書かれなかったため、
    /// 保存せずに出力ピッカーを開くと、前の種類の表示のままだった。
    /// </summary>
    public class OutDevTypeTempFollowsEditorTests
    {
        private const int Device = 0;

        static OutDevTypeTempFollowsEditorTests()
        {
            if (string.IsNullOrEmpty(TestGlobals.AppDataPath))
            {
                TestGlobals.AppDataPath = AppContext.BaseDirectory;
            }

            if (Application.Current == null)
            {
                try { _ = new Application(); }
                catch { }
            }
        }

        private static (ProfileSettingsViewModel Vm, ProfileSettingsService Service, DS4Windows.Processing.Abstractions.IOutputSlotService Slots) Create(OutContType type)
        {
            ProfileSettingsService service = TestRepositoryFactory.CreateProfileSettingsService(new BackingStore(new EnvironmentService()));
            service.OutContType[Device] = type;
            var slots = TestHost.GetService<DS4Windows.Processing.Abstractions.IOutputSlotService>();
            return (ProfileSettingsViewModelTestFactory.Create(Device, service, slots), service, slots);
        }

        [Theory]
        [InlineData(OutContType.DS4, 0, OutContType.X360)]
        [InlineData(OutContType.X360, 1, OutContType.DS4)]
        public void ChangingTheEmulatedController_UpdatesOutDevTypeTemp(OutContType start, int newIndex, OutContType expected)
        {
            var (vm, service, slots) = Create(start);
            // 画面を開いた直後の状態（編集画面を開くと、OutDevTypeTemp は保存されている種類で始まる）
            slots.OutDevTypeTemp[Device] = start;
            vm.Output.TempControllerIndex = start == OutContType.DS4 ? 1 : 0;

            vm.Output.TempControllerIndex = newIndex;

            Assert.Equal(expected, slots.OutDevTypeTemp[Device]);
            Assert.Equal(expected, service.OutContType[Device]);
        }

        [Fact]
        public void TheCorrectionToDs4_AlsoUpdatesOutDevTypeTemp()
        {
            var (vm, _, slots) = Create(OutContType.X360);
            slots.OutDevTypeTemp[Device] = OutContType.X360;
            vm.UseDs3PitchRollSim = true;

            Assert.Equal(Ds4OutputCorrectionReason.Ds3PitchRollSim, vm.CorrectOutputTypeToDs4());

            Assert.Equal(OutContType.DS4, slots.OutDevTypeTemp[Device]);
        }
    }
}
