#nullable enable
using System;
using System.IO;
using System.Runtime.InteropServices;
using DS4Windows.Core.Platform;
using Xunit;

namespace DS4WindowsTests
{
    /// <summary>
    /// ショートカットの解決（自動プロファイルの対象探索とスタートアップの確認が共用）。
    /// 一時フォルダーに .lnk を作って読む。利用者のスタートアップ・レジストリには触れない。
    /// </summary>
    public class ShortcutResolverTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "ShortcutResolverTests_" + Guid.NewGuid().ToString("N"));

        public ShortcutResolverTests() => Directory.CreateDirectory(_dir);

        public void Dispose()
        {
            try { Directory.Delete(_dir, true); }
            catch (IOException) { /* 一時フォルダーの後始末。残っても害はない */ }
        }

        private string CreateShortcut(string target, string arguments)
        {
            string lnk = Path.Combine(_dir, "test.lnk");
            Type t = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8"))!;
            dynamic shell = Activator.CreateInstance(t)!;
            try
            {
                var shortcut = shell.CreateShortcut(lnk);
                shortcut.TargetPath = target;
                shortcut.Arguments = arguments;
                shortcut.Save();
                Marshal.FinalReleaseComObject(shortcut);
            }
            finally
            {
                Marshal.FinalReleaseComObject(shell);
            }
            return lnk;
        }

        [Fact]
        public void ResolveLnkTarget_ReturnsTheTargetOfAShortcut()
        {
            string target = Path.Combine(_dir, "app.exe");
            File.WriteAllText(target, "x");
            string lnk = CreateShortcut(target, "--flag");

            Assert.Equal(target, ShortcutResolver.ResolveLnkTarget(lnk));
            Assert.Equal(target + " --flag", ShortcutResolver.ResolveLnkTargetAndArguments(lnk));
        }

        [Fact]
        public void ResolveLnkTarget_IsNullOrEmptyForAFileThatIsNotAShortcut()
        {
            string notLnk = Path.Combine(_dir, "plain.txt");
            File.WriteAllText(notLnk, "x");

            Assert.True(string.IsNullOrEmpty(ShortcutResolver.ResolveLnkTarget(notLnk)));
        }

        [Fact]
        public void ResolveTarget_FallsBackToTheLnkTarget_WhenItIsNotAnInstallerShortcut()
        {
            string target = Path.Combine(_dir, "app.exe");
            File.WriteAllText(target, "x");
            string lnk = CreateShortcut(target, "");

            Assert.Equal(target, ShortcutResolver.ResolveTarget(lnk));
        }
    }
}
