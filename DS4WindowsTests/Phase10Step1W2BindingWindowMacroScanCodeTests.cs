using System;
using System.Windows;
using DS4Windows;
using DS4WinWPF.DS4Forms.ViewModels;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step1 W2（実機確認で発見。以前からの不具合）: Controls で、記録画面（<c>RecordBox</c>）の Scan Code にチェックを入れてマクロを保存しても、
    /// 開き直すとチェックが外れていた。原因は、<c>BindingWindowViewModel.PrepareSaveMacro</c> が <c>macroType</c> だけを渡し、
    /// <c>WriteBind</c>（Macro）が見る <c>hasScanCode</c> を渡していなかったこと。
    /// </summary>
    public class Phase10Step1W2BindingWindowMacroScanCodeTests
    {
        static Phase10Step1W2BindingWindowMacroScanCodeTests()
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

        private static DS4KeyType SaveMacroThroughTheWindow(DS4KeyType keyTypeFromRecordBox, bool pickerScanCodeWasChecked)
        {
            var settings = new DS4ControlSettings(DS4Controls.Cross);
            var vm = new BindingWindowViewModel(ControllerLimits.TestProfileIndex, settings,
                TestRepositoryFactory.CreateProfileSettingsService(), TestRepositoryFactory.CreateOutputSlotService(),
                new EnvironmentService(), -1);
            vm.CurrentOutBind.hasScanCode = pickerScanCodeWasChecked;

            // RecordBox の保存（ExportMacro）の結果
            settings.action.actionMacro = new[] { 350, 256, 310, 256 };
            settings.actionType = DS4ControlSettings.ActionType.Macro;
            settings.keyType = keyTypeFromRecordBox;

            // BindingWindow の RecordBox.Save の処理と、ウィンドウを閉じるときの書き込み
            vm.PrepareSaveMacro(vm.CurrentOutBind);
            vm.WriteBinds();

            return settings.keyType;
        }

        [Theory]
        [InlineData(DS4KeyType.Macro)]
        [InlineData(DS4KeyType.Macro | DS4KeyType.RepeatMacro)]
        [InlineData(DS4KeyType.Macro | DS4KeyType.ScanCode)]
        [InlineData(DS4KeyType.Macro | DS4KeyType.RepeatMacro | DS4KeyType.ScanCode)]
        public void SavedMacro_KeepsWhatTheRecordBoxChose(DS4KeyType fromRecordBox)
        {
            Assert.Equal(fromRecordBox, SaveMacroThroughTheWindow(fromRecordBox, pickerScanCodeWasChecked: false));
        }

        [Fact]
        public void SavedMacro_ScanCodeOff_IsNotRevivedByTheDialogCheckbox()
        {
            // 記録画面で Scan Code を外したときは、選択ダイアログ側の古いチェック状態が、勝手に残らない
            Assert.Equal(DS4KeyType.Macro, SaveMacroThroughTheWindow(DS4KeyType.Macro, pickerScanCodeWasChecked: true));
        }
    }
}
