using System;
using System.Windows;
using DS4Windows;
using DS4Windows.DS4Control;
using DS4WinWPF.DS4Forms.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step3 W0（Phase10-Step3-Plan.md §3.2・§3.3・§5）: 出力ピッカー（<c>BindingWindowViewModel</c>）の、Default／Unbound／Key／Button の書き込みと、
    /// Key・Button 選択時に Extras が保たれることの特性化。既存の癖（Button の Toggle を書かない。C-5）も、そのまま固定する。製品コードは変えない。
    /// </summary>
    public class Phase10Step3W0BindingWindowWriteTests
    {
        private const string SomeExtras = "10,20,1,255,128,0,100,1,40";

        static Phase10Step3W0BindingWindowWriteTests()
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

        private static BindingWindowViewModel Open(DS4ControlSettings settings)
        {
            var profile = TestRepositoryFactory.CreateProfileSettingsService();
            profile.OutputKBMMapping = new SendInputMapping();
            return new BindingWindowViewModel(ControllerLimits.TestProfileIndex, settings,
                profile, TestRepositoryFactory.CreateOutputSlotService(), new EnvironmentService(), -1);
        }

        private static DS4ControlSettings KeyWithMacroLeftover()
        {
            var s = new DS4ControlSettings(DS4Controls.Cross)
            {
                actionType = DS4ControlSettings.ActionType.Key,
                keyType = DS4KeyType.Toggle | DS4KeyType.ScanCode,
                extras = SomeExtras
            };
            s.action.actionKey = 65;
            return s;
        }

        [Fact]
        public void Default_WritesTheDefaultAction_AndClearsTheKeyType()
        {
            var settings = KeyWithMacroLeftover();
            var vm = Open(settings);

            vm.CurrentOutBind.outputType = OutBinding.OutType.Default;
            vm.WriteBinds();

            Assert.Equal(DS4ControlSettings.ActionType.Default, settings.actionType);
            Assert.Equal(DS4KeyType.None, settings.keyType);
            Assert.Equal(0, settings.action.actionKey);
        }

        [Fact]
        public void Unbound_WritesAButtonAction_WithTheUnboundFlag()
        {
            var settings = KeyWithMacroLeftover();
            var vm = Open(settings);

            vm.CurrentOutBind.outputType = OutBinding.OutType.Button;
            vm.CurrentOutBind.control = X360Controls.Unbound;
            vm.WriteBinds();

            Assert.Equal(DS4ControlSettings.ActionType.Button, settings.actionType);
            Assert.Equal(DS4KeyType.Unbound, settings.keyType);
            Assert.Equal(X360Controls.Unbound, settings.action.actionBtn);
        }

        [Fact]
        public void Button_DoesNotWriteToggle_EvenWhenTheToggleFlagIsSet()
        {
            // C-5（Phase8 の持ち越し）: Button の書き出しは、Toggle を書かない。割り当てエディター（Step2 W4）は、Key と Button の両方に Toggle を書く
            var settings = KeyWithMacroLeftover();
            var vm = Open(settings);

            vm.CurrentOutBind.outputType = OutBinding.OutType.Button;
            vm.CurrentOutBind.control = X360Controls.A;
            vm.CurrentOutBind.toggle = true;
            vm.WriteBinds();

            Assert.Equal(DS4KeyType.None, settings.keyType);
        }

        [Fact]
        public void Key_WritesToggleAndScanCode_FromTheOutBinding()
        {
            var settings = KeyWithMacroLeftover();
            var vm = Open(settings);

            vm.CurrentOutBind.outputType = OutBinding.OutType.Key;
            vm.CurrentOutBind.outkey = 66;
            vm.CurrentOutBind.toggle = true;
            vm.CurrentOutBind.hasScanCode = false;
            vm.WriteBinds();

            Assert.Equal(DS4ControlSettings.ActionType.Key, settings.actionType);
            Assert.Equal(66, settings.action.actionKey);
            Assert.Equal(DS4KeyType.Toggle, settings.keyType);
        }

        [Fact]
        public void Populate_FromAKeyAction_CopiesTheKeyToggleAndScanCode()
        {
            var settings = KeyWithMacroLeftover();
            var vm = Open(settings);

            Assert.Equal(OutBinding.OutType.Key, vm.CurrentOutBind.outputType);
            Assert.Equal(65, vm.CurrentOutBind.outkey);
            Assert.True(vm.CurrentOutBind.toggle);
            Assert.True(vm.CurrentOutBind.hasScanCode);
        }

        [Fact]
        public void Populate_FromADefaultAction_IsDefault()
        {
            var vm = Open(new DS4ControlSettings(DS4Controls.Cross));

            Assert.Equal(OutBinding.OutType.Default, vm.CurrentOutBind.outputType);
        }

        [Theory]
        [InlineData(OutBinding.OutType.Default)]
        [InlineData(OutBinding.OutType.Key)]
        [InlineData(OutBinding.OutType.Button)]
        public void Extras_ArePreserved_WhenTheOutputIsReplaced(OutBinding.OutType replacement)
        {
            var settings = KeyWithMacroLeftover();
            var vm = Open(settings);

            vm.CurrentOutBind.outputType = replacement;
            vm.CurrentOutBind.control = X360Controls.B;
            vm.WriteBinds();

            Assert.Equal(SomeExtras, settings.extras);
        }

        [Fact]
        public void Extras_AreEdited_ThroughTheOutBinding_AndWrittenBack()
        {
            var settings = KeyWithMacroLeftover();
            var vm = Open(settings);

            vm.CurrentOutBind.HeavyRumble = 0;
            vm.CurrentOutBind.LightRumble = 0;
            vm.CurrentOutBind.UseExtrasColor = false;
            vm.CurrentOutBind.FlashRate = 0;
            vm.CurrentOutBind.UseMouseSens = false;
            vm.CurrentOutBind.MouseSens = 25;
            vm.WriteBinds();

            Assert.Equal(string.Empty, settings.extras);
        }

        [Fact]
        public void Extras_TurningTheColorOff_DoesNotResetTheFlashRate()
        {
            // 既存の癖: 色の旗を下ろしても、点滅の間隔は 0 に戻らない（色は 255 に戻る）。点滅の間隔が残ると、Extras は「使用中」のまま書き出される
            var settings = KeyWithMacroLeftover();
            var vm = Open(settings);

            vm.CurrentOutBind.HeavyRumble = 0;
            vm.CurrentOutBind.LightRumble = 0;
            vm.CurrentOutBind.UseExtrasColor = false;
            vm.CurrentOutBind.UseMouseSens = false;
            vm.CurrentOutBind.MouseSens = 25;
            vm.WriteBinds();

            Assert.Equal("0,0,0,0,0,0,0,0,0", settings.extras);
        }
    }
}
