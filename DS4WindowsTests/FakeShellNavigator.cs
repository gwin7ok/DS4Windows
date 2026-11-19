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

        public void NewProfile() => Shown.Add("NewProfile");

        public void EditProfile() => Shown.Add("EditProfile");

        public void DeleteProfile() => Shown.Add("DeleteProfile");

        public void RenameProfile() => Shown.Add("RenameProfile");

        public void DuplicateProfile() => Shown.Add("DuplicateProfile");

        public void ImportProfile() => Shown.Add("ImportProfile");

        public void ExportProfile() => Shown.Add("ExportProfile");

        public void EditDeviceProfile(int deviceIndex) => Shown.Add("EditDeviceProfile:" + deviceIndex);

        public void NewDeviceProfile(int deviceIndex) => Shown.Add("NewDeviceProfile:" + deviceIndex);
    }
}
