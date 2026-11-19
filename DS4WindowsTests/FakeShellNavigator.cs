#nullable enable
using System.Collections.Generic;
using DS4Windows.UI.Abstractions;

namespace DS4WindowsTests
{
    /// <summary><see cref="IShellNavigator"/> の偽物。子画面を開かず、頼まれた画面の名前を記録する。</summary>
    public sealed class FakeShellNavigator : IShellNavigator
    {
        public readonly List<string> Shown = new List<string>();

        public void ShowAbout() => Shown.Add("About");

        public void ShowChangelog() => Shown.Add("Changelog");

        public void ShowDeviceOptions() => Shown.Add("DeviceOptions");

        public void ShowUpdater(string version) => Shown.Add("Updater:" + version);
    }
}
