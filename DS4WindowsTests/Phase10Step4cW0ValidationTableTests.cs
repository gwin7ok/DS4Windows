using System;
using System.Collections;
using System.Linq;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.DS4Control;
using DS4Windows.Services;
using DS4WinWPF.DS4Forms.ViewModels.SpecialActions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step4c W0（Phase10-Step4c-Plan.md §2 項目 7・§4 W0）: 種別ごとの入力（<see cref="IActionTypeEditor"/>）の <c>IsValid</c> の現行の挙動を固定する特性化テスト。
    /// 現行は、<b>エラーを記録して表示するが、戻り値は常に <c>true</c></b>（保存を止めない）。また、<c>LaunchProgramViewModel</c> はパスが未選択（null）のとき
    /// <see cref="NullReferenceException"/> になる。製品コードは変えない。W3（項目 7: 必須の値がないエラーは保存を止める。<c>NullReferenceException</c> は直す）で、期待値を書き換える。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step4cW0ValidationTableTests
    {
        private static readonly SpecialAction Action = new SpecialAction("n", "Cross", "Key", "65");

        private static string[] Errors(DS4WinWPF.DS4Forms.ViewModels.Util.NotifyDataErrorBase vm, string property)
            => (vm.GetErrors(property) as IEnumerable)?.Cast<string>().ToArray() ?? new string[0];

        [Fact]
        public void PressKey_WithoutAKey_RecordsTheError_ButIsValid()
        {
            var profile = TestRepositoryFactory.CreateProfileSettingsService();
            profile.OutputKBMMapping = new SendInputMapping();
            var vm = new PressKeyViewModel(new RecordingSpecialActionRepository(), profile, TestRepositoryFactory.CreateOutputSlotService());

            Assert.True(vm.IsValid(Action));
            Assert.True(vm.HasErrors);
            Assert.Equal(new[] { "No key defined" }, Errors(vm, "Value"));
        }

        [Fact]
        public void Macro_IsAlwaysValid_AndRecordsNoError()
        {
            var vm = new MacroViewModel(new RecordingSpecialActionRepository());

            Assert.True(vm.IsValid(Action));
            Assert.False(vm.HasErrors);
        }

        [Fact]
        public void LaunchProgram_WithAnEmptyPath_OrAMissingFile_RecordsTheError_ButIsValid()
        {
            var vm = new LaunchProgramViewModel(new RecordingSpecialActionRepository());

            vm.Filepath = string.Empty;
            Assert.True(vm.IsValid(Action));
            Assert.Equal(new[] { "Filepath empty" }, Errors(vm, "Filepath"));

            vm.Filepath = @"C:\p10step4c-nonexistent\a.exe";
            Assert.True(vm.IsValid(Action));
            Assert.Equal(new[] { "File at path does not exist" }, Errors(vm, "Filepath"));
        }

        [Fact]
        public void LaunchProgram_WithoutAChosenFile_ThrowsNullReferenceException_Today()
        {
            // 現行の不具合: パスが未選択（null）のとき、filepath.Length で例外になる。W3 で直す（エラーを記録して、保存を止める）
            var vm = new LaunchProgramViewModel(new RecordingSpecialActionRepository());

            Assert.Throws<NullReferenceException>(() => vm.IsValid(Action));
        }

        [Fact]
        public void LoadProfile_WithoutAProfile_RecordsTheError_ButIsValid()
        {
            var vm = new LoadProfileViewModel(new DS4WinWPF.ProfileList(new PathService()), new RecordingSpecialActionRepository());

            Assert.True(vm.IsValid(Action));
            Assert.Equal(new[] { "No profile given" }, Errors(vm, "ProfileIndex"));
        }

        [Fact]
        public void CheckBattery_WithoutAnyStatusOption_RecordsTheErrors_ButIsValid()
        {
            var vm = new CheckBatteryViewModel(new RecordingSpecialActionRepository(), TestHost.GetService<IEnvironmentService>())
            {
                Notification = false,
                Lightbar = false,
            };

            Assert.True(vm.IsValid(Action));
            Assert.Equal(new[] { "Need status option" }, Errors(vm, "Notification"));
            Assert.Equal(new[] { "Need status option" }, Errors(vm, "Lightbar"));
        }

        [Fact]
        public void MultiAction_WithoutMacros_RecordsTheThreeErrors_ButIsValid()
        {
            var vm = new MultiActButtonViewModel(new RecordingSpecialActionRepository());

            Assert.True(vm.IsValid(Action));
            Assert.Equal(new[] { "No tap macro defined" }, Errors(vm, "TapMacro"));
            Assert.Equal(new[] { "No hold macro defined" }, Errors(vm, "HoldMacro"));
            Assert.Equal(new[] { "No double tap macro defined" }, Errors(vm, "DoubleTapMacro"));
        }

        [Fact]
        public void HoldInterval_OutOfRange_RecordsTheError_ButIsValid()
        {
            var vm = new SpecialActionViewModel(5, new RecordingSpecialActionRepository()) { HoldInterval = 61 };

            Assert.True(vm.IsValid(Action));
            Assert.Equal(new[] { "Interval not valid" }, Errors(vm, "HoldInterval"));

            vm.HoldInterval = 60;
            Assert.True(vm.IsValid(Action));
            Assert.False(vm.HasErrors);
        }

        [Fact]
        public void TheControlsMode_DoesNotCallTheTypeEditorsValidation_ExceptTheLoadProfileGuard()
        {
            // Controls の Save（SpecialActionEditor.SaveControlMode）は、種別ごとの入力の IsValid を呼ばない。W6 で足した CanWrite（Load Profile の未選択）だけが保存を止める
            string path = SourceFileLocator.Find("DS4Windows/DS4Forms/SpecialActionEditor.xaml.cs");
            if (path == null) return;

            string source = System.IO.File.ReadAllText(path);
            int start = source.IndexOf("private void SaveControlMode()", StringComparison.Ordinal);
            int end = source.IndexOf("private void CancelBtn_Click", start, StringComparison.Ordinal);
            string body = source.Substring(start, end - start);

            Assert.DoesNotContain("typeEditors.IsValid", body);
            Assert.Contains("CanWrite(", body);
        }
    }
}
