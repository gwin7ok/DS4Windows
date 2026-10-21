using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 Steering 削除 W3（Phase9-SteeringWheel-Removal-Plan.md）: 出力デバイス 3 つ（Xbox 360、DS4 の 2 種）の <c>ConvertandSendReport</c> から、ステアリングの軸の分岐（<c>switch</c>）が除かれ、
    /// 通常の軸（左右スティックの X・Y）の出力だけが残っていること（ソースの走査。出力デバイスは ViGEmBus の実体が要るため、挙動のテストは作れない）。
    /// vJoy（<c>VJoyFeeder</c>）と <c>SASteeringWheelEmulationUnit</c> の参照が、コードから無くなっていることも調べる。
    /// </summary>
    public class Phase9SteeringW3OutputDeviceGuardTests
    {
        private readonly ITestOutputHelper _output;

        public Phase9SteeringW3OutputDeviceGuardTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private string ReadCode(string relativePath)
        {
            string path = SourceFileLocator.Find(relativePath);
            if (path == null)
            {
                _output.WriteLine(relativePath + " が見つからないため検査を省略した。");
                return null;
            }

            string text = File.ReadAllText(path);
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            return Regex.Replace(text, @"//[^\r\n]*", string.Empty);
        }

        [Theory]
        [InlineData("DS4Windows/DS4Control/Xbox360OutDevice.cs", "cont.LeftThumbX = AxisScale(state.LX, false);", "cont.RightThumbY = AxisScale(state.RY, true);")]
        [InlineData("DS4Windows/DS4Control/DS4OutDevices/DS4OutDeviceBasic.cs", "cont.LeftThumbX = state.LX;", "cont.RightThumbY = state.RY;")]
        [InlineData("DS4Windows/DS4Control/DS4OutDevices/DS4OutDeviceExt.cs", "outDS4Report.bThumbLX = state.LX;", "outDS4Report.bThumbRY = state.RY;")]
        public void TheOutputDevice_WritesTheNormalAxes_AndHasNoSteeringBranch(string relativePath, string firstAxis, string lastAxis)
        {
            string code = ReadCode(relativePath);
            if (code == null) return;

            Assert.Contains(firstAxis, code);
            Assert.Contains(lastAxis, code);
            Assert.DoesNotContain("SASteeringWheelEmulation", code);
            Assert.DoesNotContain("GetSteeringWheelEmulationAxis", code);
            Assert.DoesNotContain("vJoyFeeder", code);
        }

        [Theory]
        [InlineData("DS4Windows/DS4Control/ControlService.cs")]
        [InlineData("DS4Windows/DS4Control/OutputDevice.cs")]
        [InlineData("DS4Windows/DS4Control/OutputSlotManager.cs")]
        [InlineData("DS4Windows/DS4Library/DS4State.cs")]
        public void TheVJoyAndTheSteeringUnit_AreGone(string relativePath)
        {
            string code = ReadCode(relativePath);
            if (code == null) return;

            Assert.DoesNotContain("VJoyFeeder", code);
            Assert.DoesNotContain("SASteeringWheelEmulationUnit", code);
            Assert.DoesNotContain("SteeringWheelAxisProvider", code);
        }
    }
}
