#nullable enable
using System;
using System.Windows;
using DS4Windows.Core.Model;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Settings;
using DS4Windows.Output.Kbm;
using DS4Windows.UI.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// 出力ピッカーのボタン（Command）の動作。キー・仮想パッドのボタン・Default・Unbound を選ぶと、
    /// 割り当てが書き換わり、ピッカーを閉じる通知が出る。仮想パッドのボタンが既定の割り当てと同じなら、Default の選択として扱う。
    /// </summary>
    [Xunit.Collection(SharedHostStateCollection.Name)]
    public class BindingWindowSelectCommandTests
    {
        static BindingWindowSelectCommandTests()
        {
            if (string.IsNullOrEmpty(TestGlobals.AppDataPath))
            {
                TestGlobals.AppDataPath = AppContext.BaseDirectory;
            }

            if (Application.Current == null)
            {
                try { _ = new Application(); } catch { }
            }
        }

        private sealed class Opened
        {
            public BindingWindowViewModel Vm = null!;
            public int Closed;
        }

        private static BindingWindowViewModel Open(out Opened state)
        {
            var profile = TestRepositoryFactory.CreateProfileSettingsService();
            profile.OutputKBMMapping = new SendInputMapping();
            var vm = new BindingWindowViewModel(TestLightbar.State, ControllerLimits.TestProfileIndex, new DS4ControlSettings(DS4Controls.Cross),
                profile, TestRepositoryFactory.CreateOutputSlotService(), new EnvironmentService(), -1);
            vm.ActionBinding = vm.CurrentOutBind;
            var opened = new Opened { Vm = vm };
            vm.CloseRequested += (s, e) => opened.Closed++;
            state = opened;
            return vm;
        }

        [Fact]
        public void SelectKeyCommand_SetsKeyOutputAndRequestsClose()
        {
            var vm = Open(out var st);

            vm.SelectKeyCommand.Execute("0x44");

            Assert.Equal(OutBinding.OutType.Key, vm.ActionBinding!.outputType);
            Assert.Equal(0x44, vm.ActionBinding!.outkey);
            Assert.Equal(1, st.Closed);
        }

        [Fact]
        public void SelectButtonCommand_SetsButtonOutput_WhenItDiffersFromTheDefault()
        {
            var vm = Open(out var st);
            X360Controls other = vm.DefaultMappingFor(vm.ActionBinding!.input) == X360Controls.LT ? X360Controls.RT : X360Controls.LT;

            vm.SelectButtonCommand.Execute(other.ToString());

            Assert.Equal(OutBinding.OutType.Button, vm.ActionBinding!.outputType);
            Assert.Equal(other, vm.ActionBinding!.control);
            Assert.False(vm.DefaultPicked);
            Assert.Equal(1, st.Closed);
        }

        [Fact]
        public void SelectButtonCommand_TreatsTheDefaultMappingAsDefault()
        {
            var vm = Open(out var st);
            X360Controls def = vm.DefaultMappingFor(vm.ActionBinding!.input);

            vm.SelectButtonCommand.Execute(def.ToString());

            Assert.Equal(OutBinding.OutType.Default, vm.ActionBinding!.outputType);
            Assert.True(vm.DefaultPicked);
        }

        [Fact]
        public void PickUnboundCommand_SetsUnbound()
        {
            var vm = Open(out var st);

            vm.PickUnboundCommand.Execute(null);

            Assert.Equal(OutBinding.OutType.Button, vm.ActionBinding!.outputType);
            Assert.Equal(X360Controls.Unbound, vm.ActionBinding!.control);
            Assert.False(vm.DefaultPicked);
        }

        [Fact]
        public void PickDefaultCommand_SetsDefaultAndFlagsIt()
        {
            var vm = Open(out var st);

            vm.PickDefaultCommand.Execute(null);

            Assert.Equal(OutBinding.OutType.Default, vm.ActionBinding!.outputType);
            Assert.True(vm.DefaultPicked);
        }

        [Theory]
        [InlineData("0x44", 0x44)]
        [InlineData("0xBA", 0xBA)]
        [InlineData("1B", 0x1B)]
        public void ParseKeyCode_ReadsHex(string text, int expected)
        {
            Assert.Equal(expected, BindingWindowViewModel.ParseKeyCode(text));
        }
    }
}
