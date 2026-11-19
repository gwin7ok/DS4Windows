#nullable enable
using System.Collections.Generic;
using DS4Windows.UI.Abstractions;

namespace DS4WindowsTests
{
    /// <summary><see cref="IShellLauncher"/> の偽物。OS には何も開かせず、呼び出しを記録する。</summary>
    public sealed class FakeShellLauncher : IShellLauncher
    {
        public readonly List<string> Calls = new List<string>();

        public void OpenPath(string path) => Calls.Add("OpenPath:" + path);

        public void RunProgram(string fileName, string? arguments = null) => Calls.Add("RunProgram:" + fileName + (arguments == null ? "" : " " + arguments));

        public void RunProgramIfExists(string path) => Calls.Add("RunProgramIfExists:" + path);

        public void RunElevatedAndWait(string fileName, string arguments) => Calls.Add("RunElevatedAndWait:" + fileName + " " + arguments);

        public void OpenUrl(string url) => Calls.Add("OpenUrl:" + url);
    }
}
