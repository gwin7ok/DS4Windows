using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// ダンプ 28824（NullReferenceException。<c>ProfileEditor.GyroCalibration_Click</c>）の回帰ガード。
    /// コントローラーが未接続のスロットでは <c>DS4Controllers[slot]</c> が null のため、null の確認なしに <c>SixAxis</c> を使うとアプリが終了する。
    /// </summary>
    public class GyroCalibrationNullDeviceGuardTests
    {
        private readonly ITestOutputHelper _output;

        public GyroCalibrationNullDeviceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void GyroCalibration_Click_ChecksTheDeviceForNull_BeforeUsingTheSixAxis()
        {
            string path = SourceFileLocator.Find("DS4Windows/DS4Forms/ProfileEditor.xaml.cs");
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
            int end = text.IndexOf("private void GyroControlsTrigBtn_Click", start);
            string method = text.Substring(start, end - start);

            int nullCheck = method.IndexOf("if (d == null) return;");
            int use = method.IndexOf("d.SixAxis.ResetContinuousCalibration();");
            Assert.True(nullCheck >= 0 && use > nullCheck, "d の null の確認が、SixAxis の使用より前にない");
        }
    }
}
