#nullable enable
using System.Collections.Generic;
using DS4Windows.UI.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>はじめの案内画面の、導入の操作の使える／使えない。実行中はすべて止め、終わったら OS の条件で使えるものだけを戻す。</summary>
    public class WelcomeDialogViewModelTests
    {
        [Theory]
        [InlineData(true, true)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        public void Idle_EnablesByCompatibility_AndRunningDisablesEverything(bool hidHide, bool faker)
        {
            var vm = new WelcomeDialogViewModel(hidHide, faker);
            Assert.True(vm.ViGEmEnabled);
            Assert.Equal(hidHide, vm.HidHideEnabled);
            Assert.Equal(faker, vm.FakerInputEnabled);

            vm.InstallRunning = true;
            Assert.False(vm.ViGEmEnabled);
            Assert.False(vm.HidHideEnabled);
            Assert.False(vm.FakerInputEnabled);

            vm.InstallRunning = false;
            Assert.True(vm.ViGEmEnabled);
            Assert.Equal(hidHide, vm.HidHideEnabled);
            Assert.Equal(faker, vm.FakerInputEnabled);
        }

        [Fact]
        public void ChangingTheRunningState_NotifiesTheThreeEnabledProperties()
        {
            var vm = new WelcomeDialogViewModel(true, true);
            var names = new List<string?>();
            vm.PropertyChanged += (s, e) => names.Add(e.PropertyName);

            vm.InstallRunning = true;

            Assert.Contains(nameof(WelcomeDialogViewModel.ViGEmEnabled), names);
            Assert.Contains(nameof(WelcomeDialogViewModel.HidHideEnabled), names);
            Assert.Contains(nameof(WelcomeDialogViewModel.FakerInputEnabled), names);
        }
    }
}
