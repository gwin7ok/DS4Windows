using System;
using System.Windows;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step1 W2（Phase10-Step1-Plan.md §5）: Controls の <c>RecordBox</c> の ViewModel（<see cref="RecordBoxViewModel"/>）が、
    /// 繰り返し（Execute Mode）と Scan Code を共通の <see cref="MacroPlaybackOptions"/> で持つこと。
    /// 読み込みで <c>keyType</c> の <c>RepeatMacro</c>／<c>ScanCode</c> を復元し、<c>ExportMacro</c> が
    /// <c>Macro</c>（＋ <c>RepeatMacro</c>、<c>ScanCode</c>）だけを書く（従来と同じ。<c>HoldMacro</c> は存在しない）。
    /// </summary>
    public class Phase10Step1W2RecordBoxPlaybackOptionsTests
    {
        static Phase10Step1W2RecordBoxPlaybackOptionsTests()
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
                try { Application.ResourceAssembly = typeof(DS4WinWPF.AppHost).Assembly; } catch { }
            }

            // RecordBoxViewModel のコンストラクター（MacroStepItem.CacheImgLocations）が参照するリソースキー（実際の画像は不要）
            if (Application.Current != null)
            {
                string[] resourceKeys =
                {
                    "KeyDownImg", "KeyUpImg", "KeyWaitImg", "KeyHoldImg",
                    "ClockImg", "MouseImg", "CustomKeyImg", "TouchpadImg",
                    "GyroImg", "WheelImg", "UnboundImg", "WaitImg", "HoldImg",
                    "LeftImg", "RightImg", "MiddleImg", "Clock", "Mouse", "CustomKey"
                };

                foreach (string key in resourceKeys)
                {
                    if (!Application.Current.Resources.Contains(key))
                    {
                        Application.Current.Resources[key] = "";
                    }
                }
            }
        }

        private static RecordBoxViewModel CreateViewModel(DS4ControlSettings controlSettings, bool repeatable)
        {
            ProfileSettingsService settings = TestRepositoryFactory.CreateProfileSettingsService(new BackingStore(new EnvironmentService()));
            return new RecordBoxViewModel(ControllerLimits.TestProfileIndex, controlSettings, repeatable,
                settings, new EnvironmentService(), null);
        }

        private static DS4ControlSettings SettingsWith(DS4KeyType keyType)
        {
            // actionType は Default のまま（マクロの手順の読み込みは、この検査の対象外）
            return new DS4ControlSettings(DS4Controls.Cross) { keyType = keyType };
        }

        [Theory]
        [InlineData(DS4KeyType.None, false, false)]
        [InlineData(DS4KeyType.Macro, false, false)]
        [InlineData(DS4KeyType.Macro | DS4KeyType.RepeatMacro, true, false)]
        [InlineData(DS4KeyType.Macro | DS4KeyType.ScanCode, false, true)]
        [InlineData(DS4KeyType.Macro | DS4KeyType.RepeatMacro | DS4KeyType.ScanCode, true, true)]
        public void Constructor_RestoresRepeatAndScanCode_FromKeyType(DS4KeyType keyType, bool repeat, bool scan)
        {
            RecordBoxViewModel vm = CreateViewModel(SettingsWith(keyType), repeatable: true);

            Assert.Equal(repeat, vm.PlaybackOptions.RepeatWhileHeld);
            Assert.Equal(scan, vm.PlaybackOptions.UseScanCode);
            Assert.Equal(repeat ? MacroPlaybackOptions.RepeatWhileHeldIndex : MacroPlaybackOptions.PlayOnceIndex, vm.PlaybackOptions.PlayModeIndex);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Repeatable_IsPassedToThePlaybackOptions(bool repeatable)
        {
            RecordBoxViewModel vm = CreateViewModel(SettingsWith(DS4KeyType.None), repeatable);

            Assert.Equal(repeatable, vm.Repeatable);
            Assert.Equal(repeatable, vm.PlaybackOptions.Repeatable);
        }

        [Fact]
        public void Repeatable_False_StillWritesTheLoadedRepeatMacro_AsBefore()
        {
            // 従来の RecordBox は、ドロップダウンが無効（repeatable=false）でも、読み込んだ RepeatMacro を ExportMacro で書き戻していた
            DS4ControlSettings settings = SettingsWith(DS4KeyType.Macro | DS4KeyType.RepeatMacro);
            RecordBoxViewModel vm = CreateViewModel(settings, repeatable: false);

            vm.ExportMacro();

            Assert.Equal(DS4KeyType.Macro | DS4KeyType.RepeatMacro, settings.keyType);
        }

        [Theory]
        [InlineData(false, false, DS4KeyType.Macro)]
        [InlineData(true, false, DS4KeyType.Macro | DS4KeyType.RepeatMacro)]
        [InlineData(false, true, DS4KeyType.Macro | DS4KeyType.ScanCode)]
        [InlineData(true, true, DS4KeyType.Macro | DS4KeyType.RepeatMacro | DS4KeyType.ScanCode)]
        public void ExportMacro_WritesMacroAndOnlyTheChosenOptions(bool repeat, bool scan, DS4KeyType expected)
        {
            DS4ControlSettings settings = SettingsWith(DS4KeyType.None);
            RecordBoxViewModel vm = CreateViewModel(settings, repeatable: true);
            vm.PlaybackOptions.PlayModeIndex = repeat ? MacroPlaybackOptions.RepeatWhileHeldIndex : MacroPlaybackOptions.PlayOnceIndex;
            vm.PlaybackOptions.UseScanCode = scan;

            vm.ExportMacro();

            Assert.Equal(DS4ControlSettings.ActionType.Macro, settings.actionType);
            Assert.Equal(expected, settings.keyType);
        }

        [Fact]
        public void ExportMacro_DropsOtherBits_AsBefore()
        {
            // 従来どおり、keyType は Macro から作り直す（Toggle など、マクロに無関係のビットは持ち越さない）
            DS4ControlSettings settings = SettingsWith(DS4KeyType.Toggle | DS4KeyType.RepeatMacro);
            RecordBoxViewModel vm = CreateViewModel(settings, repeatable: true);

            vm.ExportMacro();

            Assert.Equal(DS4KeyType.Macro | DS4KeyType.RepeatMacro, settings.keyType);
        }
    }
}
