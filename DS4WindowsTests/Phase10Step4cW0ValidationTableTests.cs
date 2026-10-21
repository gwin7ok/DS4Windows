using System;
using System.Collections;
using System.Linq;
using System.Windows.Media;
using DS4Windows;
using DS4Windows.DI;
using DS4Windows.DS4Control;
using DS4Windows.Services;
using DS4WinWPF.DS4Forms.ViewModels.SpecialActions;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase10-Step4c W0／W3（Phase10-Step4c-Plan.md §2 項目 7・§4 W0・W3）: 種別ごとの入力（<see cref="IActionTypeEditor"/>）の <c>IsValid</c> の挙動を固定するテスト。
    /// W0 では、エラーを記録して表示するだけで、戻り値は常に <c>true</c>（保存を止めない）だった。W3（ユーザー承認、2026-10-21）で、
    /// <b>必須の値がないエラーは <c>false</c> を返して保存を止める</b>ようにした（キー未選択、プログラムのパスが空、プロファイル未選択、Check Battery の方法が未選択・2 色が同じ、Hold の範囲外）。
    /// 止めないもの: 「ファイルが存在しない」（警告のみ）、Toggle の「解除トリガーなし」（機能廃止済みで入力する画面がない）、Multi-action の未設定のマクロ（空のジェスチャーは何もしない）。
    /// <c>LaunchProgramViewModel</c> のパス未選択（null）の <see cref="NullReferenceException"/> も直した（空と同じエラー）。
    /// </summary>
    [Collection("ServiceProviderHolder")]
    public class Phase10Step4cW0ValidationTableTests
    {
        private static readonly SpecialAction Action = new SpecialAction("n", "Cross", "Key", "65");

        private static string[] Errors(DS4WinWPF.DS4Forms.ViewModels.Util.NotifyDataErrorBase vm, string property)
            => (vm.GetErrors(property) as IEnumerable)?.Cast<string>().ToArray() ?? new string[0];

        private static PressKeyViewModel PressKey()
        {
            var profile = TestRepositoryFactory.CreateProfileSettingsService();
            profile.OutputKBMMapping = new SendInputMapping();
            return new PressKeyViewModel(new RecordingSpecialActionRepository(), profile, TestRepositoryFactory.CreateOutputSlotService());
        }

        [Fact]
        public void PressKey_WithoutAKey_RecordsTheError_AndIsNotValid()
        {
            var vm = PressKey();

            Assert.False(vm.IsValid(Action));
            Assert.True(vm.HasErrors);
            Assert.Equal(new[] { "No key defined" }, Errors(vm, "Value"));
        }

        [Fact]
        public void PressKey_WithAKey_IsValid_AndTheErrorIsCleared()
        {
            var vm = PressKey();
            Assert.False(vm.IsValid(Action));

            vm.Value = 65;

            Assert.True(vm.IsValid(Action));
            Assert.False(vm.HasErrors);
        }

        [Fact]
        public void PressKey_ToggleWithoutUnloadTriggers_ShowsTheError_ButStaysValid()
        {
            // 解除トリガーの機能は廃止済みで、入力する画面もない。止めると Toggle の Key が保存できなくなるため、表示のみ
            var vm = PressKey();
            vm.Value = 65;
            vm.SwitchMode = "Toggle";

            Assert.True(vm.IsValid(Action));
            Assert.Equal(new[] { "No unload triggers specified" }, Errors(vm, "UnloadError"));
        }

        [Fact]
        public void Macro_IsAlwaysValid_AndRecordsNoError()
        {
            var vm = new MacroViewModel(new RecordingSpecialActionRepository());

            Assert.True(vm.IsValid(Action));
            Assert.False(vm.HasErrors);
        }

        [Fact]
        public void LaunchProgram_WithAnEmptyPath_IsNotValid()
        {
            var vm = new LaunchProgramViewModel(new RecordingSpecialActionRepository());

            vm.Filepath = string.Empty;

            Assert.False(vm.IsValid(Action));
            Assert.Equal(new[] { "Filepath empty" }, Errors(vm, "Filepath"));
        }

        [Fact]
        public void LaunchProgram_WithAMissingFile_ShowsTheWarning_ButStaysValid()
        {
            var vm = new LaunchProgramViewModel(new RecordingSpecialActionRepository());

            vm.Filepath = @"C:\p10step4c-nonexistent\a.exe";

            Assert.True(vm.IsValid(Action));
            Assert.Equal(new[] { "File at path does not exist" }, Errors(vm, "Filepath"));
        }

        [Fact]
        public void LaunchProgram_WithoutAChosenFile_IsNotValid_InsteadOfThrowing()
        {
            // 以前の不具合: パスが未選択（null）のとき、filepath.Length で NullReferenceException になった。空と同じエラーにして、保存を止める
            var vm = new LaunchProgramViewModel(new RecordingSpecialActionRepository());

            Assert.False(vm.IsValid(Action));
            Assert.Equal(new[] { "Filepath empty" }, Errors(vm, "Filepath"));
        }

        [Fact]
        public void LaunchProgram_WithAnExistingFile_IsValid_AndHasNoError()
        {
            var vm = new LaunchProgramViewModel(new RecordingSpecialActionRepository());

            vm.Filepath = typeof(Phase10Step4cW0ValidationTableTests).Assembly.Location;

            Assert.True(vm.IsValid(Action));
            Assert.False(vm.HasErrors);
        }

        [Fact]
        public void LoadProfile_WithoutAProfile_IsNotValid()
        {
            var vm = new LoadProfileViewModel(new DS4WinWPF.ProfileList(new PathService()), new RecordingSpecialActionRepository());

            Assert.False(vm.IsValid(Action));
            Assert.Equal(new[] { "No profile given" }, Errors(vm, "ProfileIndex"));
        }

        [Fact]
        public void CheckBattery_WithoutAnyStatusOption_IsNotValid()
        {
            var vm = new CheckBatteryViewModel(new RecordingSpecialActionRepository(), TestHost.GetService<IEnvironmentService>())
            {
                Notification = false,
                Lightbar = false,
            };

            Assert.False(vm.IsValid(Action));
            Assert.Equal(new[] { "Need status option" }, Errors(vm, "Notification"));
            Assert.Equal(new[] { "Need status option" }, Errors(vm, "Lightbar"));
        }

        [Fact]
        public void CheckBattery_WithTheSameTwoColors_IsNotValid_AndWithDifferentOnes_IsValid()
        {
            var vm = new CheckBatteryViewModel(new RecordingSpecialActionRepository(), TestHost.GetService<IEnvironmentService>())
            {
                Notification = false,
                Lightbar = true,
                EmptyColor = Colors.Red,
                FullColor = Colors.Red,
            };

            Assert.False(vm.IsValid(Action));
            Assert.Equal(new[] { "Need to set two different colors" }, Errors(vm, "Lightbar"));

            vm.FullColor = Colors.Lime;

            Assert.True(vm.IsValid(Action));
            Assert.False(vm.HasErrors);
        }

        [Fact]
        public void MultiAction_WithoutMacros_RecordsTheThreeErrors_ButStaysValid()
        {
            // 空のジェスチャーは何もしない。Controls に Multi-action が来る段階（W8 相当）で、改めて決める
            var vm = new MultiActButtonViewModel(new RecordingSpecialActionRepository());

            Assert.True(vm.IsValid(Action));
            Assert.Equal(new[] { "No tap macro defined" }, Errors(vm, "TapMacro"));
            Assert.Equal(new[] { "No hold macro defined" }, Errors(vm, "HoldMacro"));
            Assert.Equal(new[] { "No double tap macro defined" }, Errors(vm, "DoubleTapMacro"));
        }

        [Fact]
        public void HoldInterval_OutOfRange_IsNotValid()
        {
            var vm = new SpecialActionViewModel(5, new RecordingSpecialActionRepository()) { HoldInterval = 61 };

            Assert.False(vm.IsValid(Action));
            Assert.Equal(new[] { "Interval not valid" }, Errors(vm, "HoldInterval"));

            vm.HoldInterval = 60;
            Assert.True(vm.IsValid(Action));
            Assert.False(vm.HasErrors);
        }

        [Fact]
        public void TheControlsMode_CallsTheTypeEditorsValidation_AndNoLongerHasTheLoadProfileGuard()
        {
            // Controls の Save（AssignmentEditor.SaveControlMode）は、SA のモードと同じく、種別ごとの入力の IsValid を呼ぶ（Default と Unbound は入力がないため除く）。
            // W6 の暫定の CanWrite（Load Profile の未選択だけ止める）は、IsValid が偽を返すことで不要になり、削除した
            string path = SourceFileLocator.Find("DS4Windows/DS4Forms/AssignmentEditor.xaml.cs");
            if (path == null) return;

            string source = System.IO.File.ReadAllText(path);
            int start = source.IndexOf("private void SaveControlMode()", StringComparison.Ordinal);
            int end = source.IndexOf("private void CancelBtn_Click", start, StringComparison.Ordinal);
            string body = source.Substring(start, end - start);

            Assert.Contains("typeEditors.IsValid", body);
            Assert.Contains("DefaultIndex", body);
            Assert.Contains("UnboundIndex", body);
            Assert.DoesNotContain("CanWrite(", body);
        }

        [Fact]
        public void TheErrorsAreShownWithARedBorder_OnTheTargetOfEachRequiredValue()
        {
            // 名前欄・トリガー欄と同じ仕組み（ValidatesOnNotifyDataErrors）で、必須の値がない入力を赤い枠で示す（Phase10-Step4c W3）
            string path = SourceFileLocator.Find("DS4Windows/DS4Forms/AssignmentEditor.xaml");
            if (path == null) return;

            string xaml = System.IO.File.ReadAllText(path);

            Assert.Contains("Binding Value,Mode=OneWay,ValidatesOnNotifyDataErrors=True", xaml);        // Press Key: キーの選択
            Assert.Contains("Binding Filepath,Mode=OneWay,ValidatesOnNotifyDataErrors=True", xaml);     // Launch Program: パス
            Assert.Contains("Binding ProfileIndex,FallbackValue=0,ValidatesOnNotifyDataErrors=True", xaml);   // Load Profile
            Assert.Contains("Binding Notification,ValidatesOnNotifyDataErrors=True", xaml);             // Check Battery
            Assert.Contains("Binding Lightbar,ValidatesOnNotifyDataErrors=True", xaml);
            Assert.Equal(4, xaml.Split(new[] { "Binding HoldInterval,ValidatesOnNotifyDataErrors=True" }, StringSplitOptions.None).Length - 1);   // Hold の秒数（4 か所）
        }
    }
}
