using System.IO;
using System.Text.RegularExpressions;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Processing.Services;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// ダンプ 28824（NullReferenceException。ジャイロ較正のリセット）の回帰ガード。
    /// コントローラーが未接続のスロットでは機器が null のため、null の確認なしに <c>SixAxis</c> を使うとアプリが終了する。
    /// 較正のリセットは <see cref="IControllerSlotCommands"/> が行い、未接続のスロットでは false を返すだけで例外にしないこと、
    /// 編集画面がその窓口を呼ぶこと（機器を直接触らないこと）を守る。
    /// </summary>
    public class GyroCalibrationNullDeviceGuardTests
    {
        private readonly ITestOutputHelper _output;

        public GyroCalibrationNullDeviceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(1000)]
        public void ResetGyroCalibration_DoesNothing_ForEmptyOrOutOfRangeSlots(int slot)
        {
            var commands = new ControllerSlotCommands(new DeviceSlotState());

            Assert.False(commands.ResetGyroCalibration(slot));
        }

        [Fact]
        public void GyroCalibration_Click_GoesThroughTheSlotCommands()
        {
            string path = SourceFileLocator.Find("DS4Windows/UI/ProfileEditor.xaml.cs");
            if (path == null)
            {
                _output.WriteLine("ProfileEditor.xaml.cs が見つからないため検査を省略した。");
                return;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", string.Empty);

            int start = text.IndexOf("private void GyroCalibration_Click");
            Assert.True(start >= 0);
            int end = text.IndexOf("private void ", start + 10);
            string method = text.Substring(start, end - start);

            Assert.Contains("slotCommands.ResetGyroCalibration(", method);
            Assert.DoesNotContain("SixAxis", method);
        }
    }
}
