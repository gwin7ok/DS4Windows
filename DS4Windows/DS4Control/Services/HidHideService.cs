using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-E E-2（Phase9-E-Plan.md）: <see cref="IHidHideService"/> の実装（DI の Singleton）。
    /// 旧 <c>ControlService</c> の HidHide 系（<c>CheckHidHidePresence</c>、<c>UpdateHidHideAttributes</c>、<c>CheckAffected</c>、<c>ChangeExclusiveStatus</c>、
    /// 状態 <c>hidDeviceHiding*</c>）を、そのまま移したもの（ドライバーの窓口 <c>HidHideAPIDevice</c> は <see cref="IHidHideApiFactory"/> 経由。ログは <see cref="DebugMessage"/> で
    /// <c>ControlService</c> の Debug イベントへ流す）。値の特性化は <c>Phase9EHidHideServiceDifferentialTests</c>（旧実装との差分テスト）が守る。
    /// </summary>
    public sealed class HidHideService : IHidHideService
    {
        private readonly IEnvironmentService _environmentService;
        private readonly IPathService _pathService;
        private readonly IHidHideApiFactory _apiFactory;

        private HashSet<string> hidDeviceHidingAffectedDevs = new HashSet<string>();
        private HashSet<string> hidDeviceHidingExemptedDevs = new HashSet<string>();
        private bool hidDeviceHidingForced = false;
        private bool hidDeviceHidingEnabled = false;

        public event Action<string, bool> DebugMessage;

        public HidHideService(IEnvironmentService environmentService, IPathService pathService, IHidHideApiFactory apiFactory)
        {
            _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _apiFactory = apiFactory ?? throw new ArgumentNullException(nameof(apiFactory));
        }

        // 旧 ControlService.LogDebug(string, bool)。購読者（ControlService）が Debug イベントへ流す
        private void LogDebug(string data, bool warning = false)
        {
            DebugMessage?.Invoke(data, warning);
        }

        public void CheckPresence(string ExePath = "", string ExeName = "Autoprofile Exe", bool AddExe = true) // Default value for D4W Startup
        {
            if (_environmentService.HidHideInstalled)
            {
                LogDebug("HidHide control device found");
                using (IHidHideApi hidHideDevice = _apiFactory.Open())
                {
                    if (!hidHideDevice.IsOpen())
                    {
                        return;
                    }
                    // Catch Blank Values and initialize for Startup. Also catches empty Values.
                    // Also Catches Empty values in auto-profiler, and defaults to trying to re-add D4W. Will fail harmlessly later.
                    if (ExePath == "") { ExePath = _pathService.ExecutablePath; ExeName = "DS4Windows"; AddExe = true; }

                    // Check for inverse application cloak. If setting is being used in HidHide,
                    // skip checking HidHide whitelist for DS4Windows.
                    bool inverseAppCloak = hidHideDevice.GetWhiteListInverseState();
                    if (inverseAppCloak)
                    {
                        return;
                    }


                    List<string> dosPaths = hidHideDevice.GetWhitelist();

                    int maxPathCheckLength = 512;
                    StringBuilder sb = new StringBuilder(maxPathCheckLength);

                    DirectoryInfo dirInfo = new DirectoryInfo(Path.GetDirectoryName(ExePath));
                    // Check if exe is placed in a junction symlink directory (done with Scoop).
                    // Good enough
                    if (dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint) &&
                        dirInfo.LinkTarget != null)
                    {
                        // App directory is a junction. Find real directory and get proper path
                        // for inserting into HidHide
                        ExePath = Path.Combine(dirInfo.LinkTarget, Path.GetFileName(ExePath));
                    }

                    string driveLetter = Path.GetPathRoot(ExePath).Replace("\\", "");
                    uint _ = NativeMethods.QueryDosDevice(driveLetter, sb, maxPathCheckLength);
                    //int error = Marshal.GetLastWin32Error();

                    string dosDrivePath = sb.ToString();
                    // Strip a possible \??\ prefix.
                    if (dosDrivePath.StartsWith(@"\??\"))
                    {
                        dosDrivePath = dosDrivePath.Remove(0, 4);
                    }

                    string partial = ExePath.Replace(driveLetter, "");
                    // Need to trim starting '\\' from path2 or Path.Combine will
                    // treat it as an absolute path and only return path2
                    string realPath = Path.Combine(dosDrivePath, partial.TrimStart('\\'));
                    bool exists = dosPaths.Contains(realPath);
                    if (!exists && AddExe)
                    {
                        LogDebug($"{ExeName} not found in HidHide whitelist. Adding to list");
                        dosPaths.Add(realPath);
                        hidHideDevice.SetWhitelist(dosPaths);
                    }
                    if (exists && !AddExe)
                    {
                        LogDebug($"{ExeName} found in HidHide whitelist. Removing from list");
                        dosPaths.Remove(realPath);
                        hidHideDevice.SetWhitelist(dosPaths);
                    }
                }
            }
        }

        public void UpdateAttributes()
        {
            if (_environmentService.HidHideInstalled)
            {
                hidDeviceHidingAffectedDevs.Clear();
                hidDeviceHidingExemptedDevs.Clear(); // No known equivalent in HidHide
                hidDeviceHidingForced = false; // No known equivalent in HidHide
                hidDeviceHidingEnabled = false;

                using (IHidHideApi hidHideDevice = _apiFactory.Open())
                {
                    if (!hidHideDevice.IsOpen())
                    {
                        return;
                    }

                    bool active = hidHideDevice.GetActiveState();
                    List<string> instances = hidHideDevice.GetBlacklist();

                    hidDeviceHidingEnabled = active;
                    foreach (string instance in instances)
                    {
                        hidDeviceHidingAffectedDevs.Add(instance.ToUpper());
                    }
                }
            }
        }

        public bool IsAffected(DS4Device dev)
        {
            bool result = false;
            if (dev != null && hidDeviceHidingEnabled)
            {
                string deviceInstanceId = _environmentService.GetInstanceIdFromDevicePath(dev.HidDevice.DevicePath);
                if (_environmentService.HidHideInstalled)
                {
                    result = _environmentService.CheckHidHideAffectedStatus(deviceInstanceId,
                        hidDeviceHidingAffectedDevs, hidDeviceHidingExemptedDevs, hidDeviceHidingForced);
                }
            }

            return result;
        }

        public void MarkAffected(DS4Device dev)
        {
            if (_environmentService.HidHideInstalled)
            {
                dev.CurrentExclusiveStatus = DS4Device.ExclusiveStatus.HidHideAffected;
            }
        }
    }
}
