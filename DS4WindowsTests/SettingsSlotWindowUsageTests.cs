using System.Text.RegularExpressions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// プロファイル編集画面が、設定サービスのスロット番号の配列（<c>IProfileSettingsService.XXX[slot]</c>）を直接触らず、
    /// 領域の窓口（<c>ForSlot(slot).Gyro</c> など）だけを通すことの守り。
    /// 領域のサブ ViewModel は設定サービス全体の契約を知らず、窓口だけを受け取る。
    /// 編集画面の外（<c>CompositeDeviceModel</c> など）の配列の直接参照は、この検査の対象外（今後の整理）。
    /// </summary>
    public class SettingsSlotWindowUsageTests
    {
        private static readonly Regex ArrayIndexing =
            new Regex(@"\b(profileSettings|profileSettingsService|settingsService)\.[A-Za-z0-9_]+\[", RegexOptions.Compiled);

        [Theory]
        [InlineData("DS4Windows/UI/ProfileEditor.xaml.cs")]
        [InlineData("DS4Windows/UI/ViewModels/ProfileSettingsViewModel.cs")]
        public void TheEditorAndItsViewModel_DoNotIndexTheSlotArraysDirectly(string path)
        {
            string code = SourceText.ReadCode(path);
            if (code == null) return;

            Assert.Empty(ArrayIndexing.Matches(code));
        }

        [Theory]
        [InlineData("GyroSettingsViewModel")]
        [InlineData("TouchpadSettingsViewModel")]
        [InlineData("TriggerSettingsViewModel")]
        [InlineData("StickSettingsViewModel")]
        [InlineData("LightbarSettingsViewModel")]
        [InlineData("OutputSettingsViewModel")]
        [InlineData("GeneralSettingsViewModel")]
        [InlineData("SixAxisSettingsViewModel")]
        public void TheRegionViewModels_ReceiveOnlyTheirSlotWindow(string viewModel)
        {
            string code = SourceText.ReadCode($"DS4Windows/UI/ViewModels/{viewModel}.cs");
            if (code == null) return;

            Assert.DoesNotContain("IProfileSettingsService", code);
        }
    }
}
