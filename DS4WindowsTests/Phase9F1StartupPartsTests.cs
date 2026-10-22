using System;
using System.Globalization;
using System.IO;
using DS4WinWPF;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// Phase9 9-F F-1a（Phase9-F-Plan.md §5）: 起動処理から切り出した、状態を持たない部品の値の特性化。
    /// 旧 <c>App.Application_Startup</c> の中の処理と同じ結果を返すこと（文言とレベルは変えない）。
    /// </summary>
    public class Phase9F1StartupPartsTests
    {
        // ---- CompleteInstallArguments ----

        [Fact]
        public void CompleteInstall_BothArguments_AreRead()
        {
            Assert.True(CompleteInstallArguments.TryParse(
                new[] { "--complete-install", @"C:\tmp\new", "--complete-install-target", @"C:\Program Files\DS4Windows" }, out string src, out string tgt));
            Assert.Equal(@"C:\tmp\new", src);
            Assert.Equal(@"C:\Program Files\DS4Windows", tgt);
        }

        [Theory]
        [InlineData("--complete-install", "src")]                          // target がない
        [InlineData("--complete-install-target", "tgt")]                   // source がない
        [InlineData("--complete-install")]                                 // 値がない
        [InlineData("--other", "x")]
        public void CompleteInstall_MissingEither_IsNotTheCompletionMode(params string[] args)
        {
            Assert.False(CompleteInstallArguments.TryParse(args, out _, out _));
        }

        [Fact]
        public void CompleteInstall_NullOrEmptyArguments_AreNotTheCompletionMode()
        {
            Assert.False(CompleteInstallArguments.TryParse(null, out _, out _));
            Assert.False(CompleteInstallArguments.TryParse(Array.Empty<string>(), out _, out _));
        }

        [Fact]
        public void CompleteInstall_TheLastValueWins_AndTheOrderDoesNotMatter()
        {
            Assert.True(CompleteInstallArguments.TryParse(
                new[] { "--complete-install-target", "T1", "--complete-install", "S1", "--complete-install", "S2" }, out string src, out string tgt));
            Assert.Equal("S2", src);
            Assert.Equal("T1", tgt);
        }

        // ---- BuildStampReader ----

        [Fact]
        public void BuildStamp_WithADateAndACommit_IsLoggedWithTheCommit()
        {
            Assert.Equal("This binary was built at 2026-10-22 21:50:14 (commit 0bdfe58)",
                BuildStampReader.BuildLogMessage("built 2026-10-22 21:50:14 commit=0bdfe58\n"));
            // 日時は、読み取った文字列のまま出す（旧コードと同じ。「T」区切りもそのまま）
            Assert.Equal("This binary was built at 2026-10-22T21:50:14 (commit 0bdfe58-dirty)",
                BuildStampReader.BuildLogMessage("2026-10-22T21:50:14 commit=0bdfe58-dirty"));
        }

        [Fact]
        public void BuildStamp_WithoutACommit_IsLoggedWithTheDateOnly()
        {
            Assert.Equal("This binary was built at 2026-10-22 21:50:14", BuildStampReader.BuildLogMessage("2026-10-22 21:50:14"));
        }

        [Fact]
        public void BuildStamp_WithoutADate_IsLoggedAsOneLine()
        {
            Assert.Equal("This binary was built: first second", BuildStampReader.BuildLogMessage("  first" + Environment.NewLine + "second  "));
            Assert.Equal("This binary was built: a b", BuildStampReader.BuildLogMessage("a\nb"));
        }

        // ---- StartupDiagnostics ----

        [Fact]
        public void TheCultureMessages_UseTheCurrentCulture()
        {
            Assert.Equal($"Startup culture: CurrentCulture={CultureInfo.CurrentCulture}, CurrentUICulture={CultureInfo.CurrentUICulture}", StartupDiagnostics.CultureMessage());
            Assert.Equal($"DefaultThreadCurrentCulture={CultureInfo.DefaultThreadCurrentCulture}, DefaultThreadCurrentUICulture={CultureInfo.DefaultThreadCurrentUICulture}",
                StartupDiagnostics.DefaultCultureMessage());
        }

        [Fact]
        public void TheArgsMessage_JoinsTheArguments_AndToleratesNull()
        {
            Assert.Equal("Startup args: -a -b", StartupDiagnostics.ArgsMessage(new[] { "-a", "-b" }));
            Assert.Equal("Startup args: ", StartupDiagnostics.ArgsMessage(null));
        }

        [Fact]
        public void TheLangFolderMessages_ListTheSubfolders()
        {
            string dir = Path.Combine(Path.GetTempPath(), "ds4w_f1_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(dir, "ja"));
            Directory.CreateDirectory(Path.Combine(dir, "de"));
            try
            {
                Assert.Equal($"Lang folder exists: True at {dir}", StartupDiagnostics.LangFolderMessage(dir));
                string subdirs = StartupDiagnostics.LangSubdirsMessage(dir);
                Assert.StartsWith("Lang subdirs: ", subdirs);
                Assert.Contains("ja", subdirs);
                Assert.Contains("de", subdirs);
            }
            finally
            {
                Directory.Delete(dir, true);
            }

            Assert.Equal($"Lang folder exists: False at {dir}", StartupDiagnostics.LangFolderMessage(dir));
        }

        [Fact]
        public void TheEnvironmentMessages_KeepTheOriginalWordingAndOrder()
        {
            string[] messages = StartupDiagnostics.EnvironmentMessages("3.11.7", @"C:\Program Files\DS4Windows\DS4Windows.exe", "Windows 10 Pro", "2009");

            Assert.Equal(7, messages.Length);
            Assert.Equal("DS4Windows version 3.11.7", messages[0]);
            Assert.Equal("DS4Windows exe file: DS4Windows.exe", messages[1]);
            Assert.StartsWith("DS4Windows Assembly Architecture: ", messages[2]);
            Assert.StartsWith("OS Version: ", messages[3]);
            Assert.Equal("OS Product Name: Windows 10 Pro", messages[4]);
            Assert.Equal("OS Release ID: 2009", messages[5]);
            Assert.StartsWith("System Architecture: ", messages[6]);
        }
    }
}
