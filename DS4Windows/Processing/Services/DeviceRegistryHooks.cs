using System;
using System.Diagnostics;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Settings;
using DS4Windows.Input;
using DS4Windows.Input.Hid;
using DS4Windows.Processing.Abstractions;
using DS4Windows.Input.Abstractions;

namespace DS4Windows.Processing.Services
{
    /// <summary><see cref="IDeviceRegistryHooks"/> の実装（DI の Singleton）。<c>ControlService</c> にあったフックを、そのまま移したもの。</summary>
    public sealed class DeviceRegistryHooks : IDeviceRegistryHooks
    {
        private readonly IDs4DeviceRegistry _deviceRegistry;
        private readonly IInputDeviceModuleRegistry _deviceModules;
        private readonly IAppSettingsService _appSettings;
        private readonly IElevatedProcessLauncher _elevatedProcessLauncher;
        private readonly IPathService _pathService;
        private ControlServiceDeviceOptions _deviceOptions;

        public DeviceRegistryHooks(IDs4DeviceRegistry deviceRegistry, IInputDeviceModuleRegistry deviceModules, IAppSettingsService appSettings,
            IElevatedProcessLauncher elevatedProcessLauncher, IPathService pathService)
        {
            _deviceRegistry = deviceRegistry ?? throw new ArgumentNullException(nameof(deviceRegistry));
            _deviceModules = deviceModules ?? throw new ArgumentNullException(nameof(deviceModules));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _elevatedProcessLauncher = elevatedProcessLauncher ?? throw new ArgumentNullException(nameof(elevatedProcessLauncher));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
        }

        public void Attach()
        {
            _deviceOptions = _appSettings.DeviceOptions;

            _deviceRegistry.RequestElevation += RequestElevation;
            _deviceRegistry.PrepareDS4Init = PrepareDS4DeviceInit;
            _deviceRegistry.PostDS4Init = PostDS4DeviceInit;
            _deviceRegistry.PreparePendingDevice = CheckForSupportedDevice;
        }

        // 種類ごとの処理（Joy-Con の結合の初期値）は IInputDeviceModule が持つ
        public void PostDS4DeviceInit(InputDeviceCore device)
        {
            _deviceModules.Resolve(device.DeviceType).OnDeviceInit(device, _deviceOptions);
        }

        public void PrepareDS4DeviceInit(InputDeviceCore device)
        {
            // 今は何もしない
        }

        // 種類ごとの有効設定の判定は IInputDeviceModule が持つ（扱う部品がない種類は false）
        public bool CheckForSupportedDevice(IHidTransport device, VidPidInfo metaInfo)
        {
            return _deviceModules.Resolve(metaInfo.inputDevType).IsEnabled(_deviceOptions);
        }

        private void RequestElevation(RequestElevationArgs args)
        {
            // 先に DI の IElevatedProcessLauncher を試し、扱えなかったときだけ、直接 Process.Start する
            bool handled = false;
            try
            {
                var launcher = _elevatedProcessLauncher;
                if (launcher != null)
                {
                    int? exitCode = launcher.RelaunchElevated("re-enabledevice " + args.InstanceId, 30000);
                    if (exitCode.HasValue)
                    {
                        args.StatusCode = exitCode.Value;
                    }
                    handled = true;
                }
            }
            catch { }

            if (!handled)
            {
                // Launches an elevated child process to re-enable device
                ProcessStartInfo startInfo =
                    new ProcessStartInfo(_pathService.ExecutablePath);
                startInfo.Verb = "runas";
                startInfo.Arguments = "re-enabledevice " + args.InstanceId;
                startInfo.UseShellExecute = true;

                try
                {
                    Process child = Process.Start(startInfo);
                    if (!child.WaitForExit(30000))
                    {
                        child.Kill();
                    }
                    else
                    {
                        args.StatusCode = child.ExitCode;
                    }
                    child.Dispose();
                }
                catch { }
            }
        }
    }
}
