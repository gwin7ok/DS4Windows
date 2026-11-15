using DS4Windows.Composition.Startup;
using DS4Windows.Core.Utilities;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>起動引数から起動の種類を決める判断（優先順位を含む）。</summary>
    public class StartupModeSelectorTests
    {
        private static StartupMode Select(params string[] args)
        {
            var parser = new ArgumentParser();
            parser.Parse(args);
            return StartupModeSelector.Select(parser);
        }

        [Fact]
        public void NoArguments_IsNormal() => Assert.Equal(StartupMode.Normal, Select());

        [Theory]
        [InlineData("-m")]
        [InlineData("-stop")]
        public void MinimizedAndStop_AreStillNormalStartups(string arg) => Assert.Equal(StartupMode.Normal, Select(arg));

        [Theory]
        [InlineData("driverinstall")]
        [InlineData("-driverinstall")]
        public void DriverInstall_IsRecognized(string arg) => Assert.Equal(StartupMode.DriverInstall, Select(arg));

        [Fact]
        public void ReenableDevice_IsRecognized() => Assert.Equal(StartupMode.ReenableDevice, Select("-re-enabledevice", "USB-VID_1234"));

        [Fact]
        public void RunTask_IsRecognized() => Assert.Equal(StartupMode.RunTask, Select("-runtask"));

        [Fact]
        public void Command_WithAnArgument_IsTheLegacyCommand() => Assert.Equal(StartupMode.LegacyCommand, Select("-command", "Query"));

        [Fact]
        public void Command_WithoutAnArgument_IsAnArgumentError() => Assert.Equal(StartupMode.ArgumentError, Select("-command"));

        [Fact]
        public void Errors_TakePriorityOverOtherModes() => Assert.Equal(StartupMode.ArgumentError, Select("-driverinstall", "-command"));
    }
}
