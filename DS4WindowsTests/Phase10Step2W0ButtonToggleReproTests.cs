using System;
using System.Windows;
using DS4Windows;
using DS4Windows.DS4Control;
using DS4WinWPF.DS4Forms.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step2 W0（Phase10-Step2-Plan.md §5）: 持ち越し C-5 と、Step9 の B-11 の食い違いの再現。選択ダイアログ（<c>BindingWindow</c>）の書き込み
    /// （<c>OutBinding.WriteBind</c>）は、Toggle を、Key の分岐でしか書かない。Button（マウスボタン、パッドのボタン）の分岐は、
    /// 最初に <c>keyType</c> を <c>None</c> にして、Toggle を書かない。そのため、Toggle つきの Button の設定は、ダイアログを開いて閉じるだけでも、Toggle が消える。
    /// ここでは、現行の挙動を固定する。Step4（共通画面の Switch Mode が Key と Button の両方に Toggle を書く）で、是正する。
    /// </summary>
    public class Phase10Step2W0ButtonToggleReproTests
    {
        static Phase10Step2W0ButtonToggleReproTests()
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

        private static ProfileSettingsService SettingsWithKeyboardMapping()
        {
            // Key の分岐は、actionAlias の更新（RefreshActionAlias）で OutputKBMMapping を使う。このサービスだけに設定する
            ProfileSettingsService profileSettings = TestRepositoryFactory.CreateProfileSettingsService();
            profileSettings.OutputKBMMapping = new SendInputMapping();
            return profileSettings;
        }

        private static void OpenAndCloseTheDialog(DS4ControlSettings settings)
        {
            var vm = new BindingWindowViewModel(ControllerLimits.TestProfileIndex, settings,
                SettingsWithKeyboardMapping(), TestRepositoryFactory.CreateOutputSlotService(),
                new EnvironmentService(), -1);
            vm.WriteBinds();   // BindingWindow.Window_Closing と同じ（閉じるたびに書き込む）
        }

        [Fact]
        public void WriteBind_Button_DoesNotWriteToggle_EvenWhenToggleIsSet()
        {
            var settings = new DS4ControlSettings(DS4Controls.Cross);
            var bind = new OutBinding { outputType = OutBinding.OutType.Button, control = X360Controls.A, toggle = true };

            bind.WriteBind(settings, SettingsWithKeyboardMapping());

            Assert.Equal(DS4ControlSettings.ActionType.Button, settings.actionType);
            Assert.False(settings.keyType.HasFlag(DS4KeyType.Toggle));   // 現行の欠落（C-5）
        }

        [Fact]
        public void WriteBind_Key_WritesToggle()
        {
            var settings = new DS4ControlSettings(DS4Controls.Cross);
            var bind = new OutBinding { outputType = OutBinding.OutType.Key, outkey = 65, toggle = true };

            bind.WriteBind(settings, SettingsWithKeyboardMapping());

            Assert.True(settings.keyType.HasFlag(DS4KeyType.Toggle));
        }

        [Fact]
        public void OpeningAndClosingTheDialog_DropsTheToggleOfAnExistingButtonAssignment()
        {
            // プロファイルの XML に Toggle つきの Button がある（実行側は対応している）。ダイアログを開いて閉じるだけで、Toggle が消える
            var settings = new DS4ControlSettings(DS4Controls.Cross)
            {
                actionType = DS4ControlSettings.ActionType.Button,
                keyType = DS4KeyType.Toggle,
            };
            settings.action.actionBtn = X360Controls.A;

            OpenAndCloseTheDialog(settings);

            Assert.Equal(DS4ControlSettings.ActionType.Button, settings.actionType);
            Assert.False(settings.keyType.HasFlag(DS4KeyType.Toggle));
        }

        [Fact]
        public void OpeningAndClosingTheDialog_KeepsTheToggleOfAnExistingKeyAssignment()
        {
            // 対照: Key の Toggle は、開いて閉じても保たれる（Key の分岐が書き戻す）
            var settings = new DS4ControlSettings(DS4Controls.Cross)
            {
                actionType = DS4ControlSettings.ActionType.Key,
                keyType = DS4KeyType.Toggle,
            };
            settings.action.actionKey = 65;

            OpenAndCloseTheDialog(settings);

            Assert.True(settings.keyType.HasFlag(DS4KeyType.Toggle));
        }
    }
}
