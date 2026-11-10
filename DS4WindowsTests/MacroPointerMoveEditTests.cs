using DS4Windows.Processing.Macro;
using System;
using System.Windows;
using Xunit;
using DS4Windows.Core.Model;
using DS4Windows.Core.Platform;
using DS4Windows.Core.Settings;
using DS4Windows.Processing;
using DS4Windows.UI.ViewModels;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase12-Step3 3d（R-3）: 記録画面（<c>RecordBox</c>）の、マウス移動ステップの追加（Add Mouse Move）と、座標の編集（モード、X、Y）の ViewModel の部分。
    /// ダブルクリックで出る編集部品の見た目と操作は、実機で確認する。
    /// </summary>
    public class MacroPointerMoveEditTests
    {
        static MacroPointerMoveEditTests()
        {
            if (string.IsNullOrEmpty(TestGlobals.AppDataPath))
            {
                TestGlobals.AppDataPath = AppContext.BaseDirectory;
            }

            if (Application.Current == null)
            {
                try { _ = new Application(); } catch { }
            }

            if (Application.ResourceAssembly == null)
            {
                try { Application.ResourceAssembly = typeof(DS4Windows.Composition.AppHost).Assembly; } catch { }
            }

            // RecordBoxViewModel のコンストラクター（MacroStepItem.CacheImgLocations）が参照するリソースキー（実際の画像は不要）
            if (Application.Current != null)
            {
                foreach (string key in new[] { "KeyDownImg", "KeyUpImg", "ClockImg" })
                {
                    if (!Application.Current.Resources.Contains(key))
                    {
                        Application.Current.Resources[key] = "";
                    }
                }
            }
        }

        private static RecordBoxViewModel CreateViewModel()
        {
            ProfileSettingsService settings = TestRepositoryFactory.CreateProfileSettingsService(new BackingStore(new EnvironmentService()));
            return new RecordBoxViewModel(TestLightbar.State, ControllerLimits.TestProfileIndex, new DS4ControlSettings(DS4Controls.Cross), false,
                settings, new EnvironmentService(), null);
        }

        private static MacroStep Key(int value, MacroStep.StepType type)
            => new MacroStep(value, value.ToString(), type, MacroStep.StepOutput.Key);

        [Fact]
        public void AddPointerMoveStep_WithNoSelection_AppendsAtTheEnd()
        {
            RecordBoxViewModel vm = CreateViewModel();
            vm.InsertMacroStep(0, Key(65, MacroStep.StepType.ActDown));
            vm.InsertMacroStep(1, Key(65, MacroStep.StepType.ActUp));

            int index = vm.AddPointerMoveStep(-1);

            Assert.Equal(2, index);
            Assert.Equal(3, vm.MacroSteps.Count);
            Assert.Equal(MacroStep.StepOutput.PointerMove, vm.MacroSteps[2].Step.OutputType);
            Assert.Equal("Move to (0, 0)", vm.MacroSteps[2].Step.Name);
        }

        [Theory]
        [InlineData(MacroPointerMoveMode.Absolute, "Move to (0, 0)")]
        [InlineData(MacroPointerMoveMode.WindowRelative, "Move in window (0, 0)")]
        public void AddPointerMoveStep_UsesTheModeOfTheList(MacroPointerMoveMode mode, string expectedName)
        {
            RecordBoxViewModel vm = CreateViewModel();

            vm.AddPointerMoveStep(-1, mode);

            Assert.Equal(expectedName, vm.MacroSteps[0].Step.Name);
            Assert.Equal((int)mode, vm.MacroSteps[0].PointerModeIndex);
        }

        [Fact]
        public void AddPointerMoveStep_WithASelection_InsertsBeforeIt()
        {
            RecordBoxViewModel vm = CreateViewModel();
            vm.InsertMacroStep(0, Key(65, MacroStep.StepType.ActDown));
            vm.InsertMacroStep(1, Key(65, MacroStep.StepType.ActUp));

            int index = vm.AddPointerMoveStep(1);

            Assert.Equal(1, index);
            Assert.Equal(MacroStep.StepOutput.Key, vm.MacroSteps[0].Step.OutputType);
            Assert.Equal(MacroStep.StepOutput.PointerMove, vm.MacroSteps[1].Step.OutputType);
            Assert.Equal(MacroStep.StepType.ActUp, vm.MacroSteps[2].Step.ActType);
        }

        [Fact]
        public void EditingTheCoordinates_UpdatesTheStepValueAndName()
        {
            RecordBoxViewModel vm = CreateViewModel();
            vm.AddPointerMoveStep(-1);
            MacroStepItem item = vm.MacroSteps[0];

            item.PointerX = 1920;
            item.PointerY = -40;

            Assert.Equal(1920, item.PointerX);
            Assert.Equal(-40, item.PointerY);
            Assert.Equal("Move to (1920, -40)", item.Step.Name);
            Assert.True(MacroPointerMove.TryFromMacroValue(item.Step.Value, out MacroPointerMove move));
            Assert.Equal(new MacroPointerMove(MacroPointerMoveMode.Absolute, 1920, -40), move);
        }

        [Fact]
        public void ChangingTheMode_KeepsTheCoordinates_AndRenamesTheStep()
        {
            RecordBoxViewModel vm = CreateViewModel();
            vm.AddPointerMoveStep(-1);
            MacroStepItem item = vm.MacroSteps[0];
            item.PointerX = 300;
            item.PointerY = 200;

            item.PointerModeIndex = 1;

            Assert.Equal(1, item.PointerModeIndex);
            Assert.Equal(300, item.PointerX);
            Assert.Equal(200, item.PointerY);
            Assert.Equal("Move in window (300, 200)", item.Step.Name);

            item.PointerModeIndex = 0;
            Assert.Equal("Move to (300, 200)", item.Step.Name);
        }

        [Fact]
        public void CoordinatesOutsideTheModeRange_AreClamped()
        {
            RecordBoxViewModel vm = CreateViewModel();
            vm.AddPointerMoveStep(-1);
            MacroStepItem item = vm.MacroSteps[0];

            item.PointerX = 100000;
            item.PointerY = -100000;
            Assert.Equal(MacroPointerMove.AbsoluteMax, item.PointerX);
            Assert.Equal(MacroPointerMove.AbsoluteMin, item.PointerY);

            // ウィンドウ基準へ切り替えると、その範囲（−8,192〜8,191）へ丸められる
            item.PointerModeIndex = 1;
            item.PointerX = 100000;
            Assert.Equal(MacroPointerMove.WindowRelativeMax, item.PointerX);
        }

        [Fact]
        public void AnOutOfRangeModeIndex_IsClamped()
        {
            RecordBoxViewModel vm = CreateViewModel();
            vm.AddPointerMoveStep(-1);
            MacroStepItem item = vm.MacroSteps[0];

            item.PointerModeIndex = 7;

            Assert.Equal(1, item.PointerModeIndex);
        }

        [Fact]
        public void TheEditedStep_IsSavedAsTheMacroValue()
        {
            RecordBoxViewModel vm = CreateViewModel();
            vm.AddPointerMoveStep(-1);
            vm.MacroSteps[0].PointerX = 10;
            vm.MacroSteps[0].PointerY = 20;

            int expected = new MacroPointerMove(MacroPointerMoveMode.Absolute, 10, 20).ToMacroValue();

            Assert.Equal(expected, vm.MacroSteps[0].Step.Value);
        }
    }
}
