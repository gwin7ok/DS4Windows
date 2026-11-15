using DS4Windows.Cli;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>コンソール種別のクライアント（DS4WindowsCli.exe）の引数の解釈。旧 <c>DS4Windows.exe -command</c> と同じ書き方を受ける。</summary>
    public class CliArgumentsTests
    {
        [Theory]
        [InlineData("-command", "Query.1.OutConttype")]
        [InlineData("command", "Query.1.OutConttype")]
        [InlineData("-COMMAND", "Query.1.OutConttype")]
        public void CommandOption_TakesTheNextArgument(string option, string command)
        {
            CliArguments a = CliArguments.Parse(new[] { option, command });

            Assert.Null(a.Error);
            Assert.False(a.ShowHelp);
            Assert.Equal(command, a.Command);
        }

        [Fact]
        public void ABareCommand_IsAccepted()
        {
            CliArguments a = CliArguments.Parse(new[] { "OutputSlot.1.PlugDS4" });

            Assert.Equal("OutputSlot.1.PlugDS4", a.Command);
            Assert.Null(a.Error);
        }

        [Theory]
        [InlineData("Query.1.Battery", true)]
        [InlineData("query.2.profilename", true)]
        [InlineData("OutputSlot.1.Unplug", false)]
        [InlineData("LoadProfile.1.Query.x", false)]
        public void OnlyQueriesWaitForAResult(string command, bool expected)
        {
            Assert.Equal(expected, CliArguments.Parse(new[] { "-command", command }).IsQuery);
        }

        [Fact]
        public void NoArguments_ShowsTheHelp()
        {
            Assert.True(CliArguments.Parse(new string[0]).ShowHelp);
            Assert.True(CliArguments.Parse(new[] { "--help" }).ShowHelp);
        }

        [Fact]
        public void ACommandOptionWithoutACommand_IsAnError()
        {
            CliArguments a = CliArguments.Parse(new[] { "-command" });

            Assert.NotNull(a.Error);
            Assert.Null(a.Command);
        }

        [Fact]
        public void ACommandLongerThan256Characters_IsAnError()
        {
            CliArguments a = CliArguments.Parse(new[] { "-command", new string('a', 257) });

            Assert.NotNull(a.Error);
            Assert.Null(a.Command);
            Assert.Null(CliArguments.Parse(new[] { "-command", new string('a', 256) }).Error);
        }

        [Fact]
        public void AnEmptyCommand_IsAnError()
        {
            Assert.NotNull(CliArguments.Parse(new[] { "-command", "" }).Error);
        }
    }
}
