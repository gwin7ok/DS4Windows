/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using HttpProgress;

namespace DS4Windows.UI
{
    /// <summary>
    /// Interaction logic for WelcomeDialog.xaml
    /// </summary>
    public partial class WelcomeDialog : Window
    {
        private const string InstallerDL1_16 =
            "https://github.com/nefarius/ViGEmBus/releases/download/setup-v1.16.116/ViGEmBus_Setup_1.16.116.exe";
        private const string InstallerDLX64 =
            "https://github.com/nefarius/ViGEmBus/releases/download/v1.22.0/ViGEmBus_1.22.0_x64_x86_arm64.exe";
        private const string InstallerDLX86 =
            "https://github.com/nefarius/ViGEmBus/releases/download/v1.22.0/ViGEmBus_1.22.0_x64_x86_arm64.exe";

        private const string InstallerHidHideX64 = "https://github.com/nefarius/HidHide/releases/download/v1.5.230.0/HidHide_1.5.230_x64.exe";
        private const string InstallerFakerInputX64 = "https://github.com/Ryochan7/FakerInput/releases/download/v0.1.0/FakerInput_0.1.0_x64.msi";
        private const string InstallerFakerInputX86 = "https://github.com/Ryochan7/FakerInput/releases/download/v0.1.0/FakerInput_0.1.0_x86.msi";

        private const string InstFileName1_16 = "ViGEmBus_Setup_1.16.116.exe";
        private string InstFileNameX64 = Path.GetFileName(InstallerDLX64);
        private string InstFileNameX86 = Path.GetFileName(InstallerDLX86);

        private string InstHidHideFileNameX64 = Path.GetFileName(InstallerHidHideX64);

        private string installFakerInputDL = "";
        private string instFakerInputFileName = "";

        // Default to latest known ViGEmBus installer
        private string installDL = InstallerDLX64;
        private string installFileName;

        // 導入の流れ。画面が閉じられたら、終了の監視を取り消す
        private readonly DS4Windows.Core.Platform.ExternalComponentInstaller _installer;
        private readonly System.Threading.CancellationTokenSource _closing = new System.Threading.CancellationTokenSource();

        // Global 直接参照の置換先。呼び出し側（App の -driverinstall 分岐）が渡す（Pure DI）。
        private readonly DS4Windows.UI.Abstractions.IDriverEnvironment _environmentService;

        public WelcomeDialog(DS4Windows.UI.Abstractions.IDriverEnvironment environmentService, System.Net.Http.HttpClient httpClient)
        {
            ArgumentNullException.ThrowIfNull(httpClient);
            _installer = new DS4Windows.Core.Platform.ExternalComponentInstaller(
                httpClient, new DS4Windows.Core.Platform.ShellInstallerProcessLauncher(), TimeProvider.System, Path.GetTempPath());
            ArgumentNullException.ThrowIfNull(environmentService);
            _environmentService = environmentService;

            InitCommands();
            InitializeComponent();

            // Establish default before system checks
            installFileName = InstFileNameX64;

            // Run checks for compatible version of ViGEmBus
            if (!_environmentService.IsWin10OrGreater())
            {
                installDL = InstallerDL1_16;
                installFileName = InstFileName1_16;
            }
            else if (!Environment.Is64BitOperatingSystem)
            {
                installDL = InstallerDLX86;
                installFileName = InstFileNameX86;
            }

            // Run checks for FakerInput driver
            if (_environmentService.IsWin8OrGreater())
            {
                if (Environment.Is64BitOperatingSystem)
                {
                    installFakerInputDL = InstallerFakerInputX64;
                    instFakerInputFileName = new FileInfo(InstallerFakerInputX64).Name;
                }
                else if (!Environment.Is64BitOperatingSystem)
                {
                    installDL = InstallerDLX86;
                    installFileName = InstFileNameX86;

                    installFakerInputDL = InstallerFakerInputX86;
                    instFakerInputFileName = new FileInfo(InstallerFakerInputX86).Name;
                }
            }
            else
            {
                step5FakerInputPanel.IsEnabled = false;
            }

            // HidHide requires Windows 10 (or later) x64
            if (!IsHidHideControlCompatible())
            {
                step4HidHidePanel.IsEnabled = false;
            }
        }

        private bool IsHidHideControlCompatible()
        {
            // HidHide requires Windows 10 (or later) x64
            return _environmentService.IsWin10OrGreater() &&
                Environment.Is64BitOperatingSystem;
        }

        private bool IsFakerInputControlCompatible()
        {
            // FakerInput works on Windows 8.1 and later. Going to attempt
            // to support x64 and x86 arch
            return _environmentService.IsWin8OrGreater();
        }

        private void FinishDialog()
        {
            _closing.Cancel();   // 導入の終了の監視を止める

            this.Close();
        }

        private void OpenXboxDriverPage()
        {
            Process.Start("http://www.microsoft.com/accessories/en-gb/d/xbox-360-controller-for-windows");
        }

        private void OpenBluetoothSettings()
        {
            Process.Start("control", "bthprops.cpl");
        }

        private void EnableControls(bool on)
        {
            vigemInstallBtn.IsEnabled = on;
            step4HidHidePanel.IsEnabled = on;
            step5FakerInputPanel.IsEnabled = on;

            // Perform compatibility checks for controls that might need
            // to be disabled when on is set to true
            if (on)
            {
                LateControlsCheck();
            }
        }

        /// <summary>
        /// Possibly disable some controls for components that are not compatible
        /// with the installed version of Windows or system configuration
        /// </summary>
        private void LateControlsCheck()
        {
            step4HidHidePanel.IsEnabled = IsHidHideControlCompatible();
            step5FakerInputPanel.IsEnabled = IsFakerInputControlCompatible();
        }
    }
}
