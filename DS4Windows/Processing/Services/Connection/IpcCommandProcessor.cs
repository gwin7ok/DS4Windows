using System;
using System.IO;
using DS4Windows.Core.Abstractions;
using DS4Windows.Core.Logging;
using DS4Windows.Core.Model;
using DS4Windows.Input;
using DS4Windows.Processing.Abstractions;

namespace DS4Windows.Processing.Services.Connection
{
    /// <summary>
    /// <see cref="IIpcCommandProcessor"/> の実装。旧 <c>MainWindow.Ipc.cs</c> の 9 個の <c>Ipc*</c> を、そのまま移したもの
    /// （受け付けの順序、引数の個数の条件、範囲の検査、ログの文言、query の結果の書き込みは従来のまま）。UI に頼む操作は <see cref="IIpcCommandHost"/> 経由。
    /// </summary>
    public class IpcCommandProcessor : IIpcCommandProcessor
    {
        private readonly IServiceLifecycle _lifecycle;
        private readonly IOutputSlotService _outputSlotService;
        private readonly IProfileRepository _profileRepo;
        private readonly IProfileSettingsService _profileSettings;
        private readonly IProfileApplicationService _profileApplication;
        private readonly IProfileLoadTarget _loadTarget;
        private readonly IPathService _pathService;
        private readonly IDeviceSlotState _slots;
        private readonly IVirtualPadService _virtualPad;
        private readonly IIpcResultWriter _resultWriter;

        public IpcCommandProcessor(IServiceLifecycle lifecycle, IOutputSlotService outputSlotService, IProfileRepository profileRepo,
            IProfileSettingsService profileSettings, IProfileApplicationService profileApplication, IProfileLoadTarget loadTarget,
            IPathService pathService, IDeviceSlotState slots, IVirtualPadService virtualPad, IIpcResultWriter resultWriter)
        {
            ArgumentNullException.ThrowIfNull(lifecycle);
            _lifecycle = lifecycle;
            ArgumentNullException.ThrowIfNull(outputSlotService);
            _outputSlotService = outputSlotService;
            ArgumentNullException.ThrowIfNull(profileRepo);
            _profileRepo = profileRepo;
            ArgumentNullException.ThrowIfNull(profileSettings);
            _profileSettings = profileSettings;
            ArgumentNullException.ThrowIfNull(profileApplication);
            _profileApplication = profileApplication;
            ArgumentNullException.ThrowIfNull(loadTarget);
            _loadTarget = loadTarget;
            ArgumentNullException.ThrowIfNull(pathService);
            _pathService = pathService;
            ArgumentNullException.ThrowIfNull(slots);
            _slots = slots;
            ArgumentNullException.ThrowIfNull(resultWriter);
            _resultWriter = resultWriter;
            ArgumentNullException.ThrowIfNull(virtualPad);
            _virtualPad = virtualPad;
        }

        public void Execute(IIpcCommandHost host, string[] strData)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (strData == null || strData.Length < 1) return;

            if (strData[0] == "start")
            {
                if (!_lifecycle.Running)
                    host.ToggleService();
            }
            else if (strData[0] == "stop")
            {
                if (_lifecycle.Running)
                    host.ToggleService();
            }
            else if (strData[0] == "cycle")
            {
                host.ToggleService();
            }
            else if (strData[0] == "shutdown")
            {
                // コントローラーを切断してから終了する（「閉じてよいか」の確認を出さないため）
                if (_lifecycle.Running)
                    host.ToggleService();
                host.CloseApplication();
            }
            else if (strData[0] == "disconnect")
            {
                Disconnect(host, strData);
            }
            else if ((strData[0] == "changeledcolor") && strData.Length >= 5)
            {
                ChangeLedColor(host, strData);
            }
            else if ((strData[0] == "loadprofile" || strData[0] == "loadtempprofile") && strData.Length >= 3)
            {
                LoadProfile(host, strData);
            }
            else if (strData[0] == "outputslot" && strData.Length >= 3)
            {
                OutputSlot(strData);
            }
            else if (strData[0] == "query" && strData.Length >= 3)
            {
                Query(host, strData);
            }
        }

        // Command syntax: Disconnect[.device#] (fex Disconnect.1)。引数なしは、すべてのワイヤレスのコントローラーを切断する
        private static void Disconnect(IIpcCommandHost host, string[] strData)
        {
            if (strData.Length == 1)
            {
                host.DisconnectAllControllers();
            }
            else
            {
                int tdevice = -1;
                if (int.TryParse(strData[1], out tdevice)) tdevice--;
                host.DisconnectController(tdevice);
            }
        }

        // Command syntax: changeledcolor.device#.red.gree.blue (ex changeledcolor.1.255.0.0)
        private static void ChangeLedColor(IIpcCommandHost host, string[] strData)
        {
            int tdevice = -1;
            if (int.TryParse(strData[1], out tdevice))
                tdevice--;
            if (tdevice >= 0 && tdevice < DS4Windows.Core.Settings.ControllerLimits.MaxControllerCount)
            {
                byte.TryParse(strData[2], out byte red);
                byte.TryParse(strData[3], out byte green);
                byte.TryParse(strData[4], out byte blue);

                host.SetLightColor(tdevice, red, green, blue);
            }
        }

        // Command syntax: LoadProfile.device#.profileName (fex LoadProfile.1.GameSnake or LoadTempProfile.1.WebBrowserSet)
        private void LoadProfile(IIpcCommandHost host, string[] strData)
        {
            int tdevice = -1;
            if (int.TryParse(strData[1], out tdevice)) tdevice--;

            if (tdevice >= 0 && tdevice < DS4Windows.Core.Settings.ControllerLimits.MaxControllerCount &&
                    File.Exists(_pathService.AppDataPath + "\\Profiles\\" + strData[2] + ".xml"))
            {
                if (strData[0] == "loadprofile")
                {
                    int idx = host.FindProfileIndex(strData[2]);
                    // 一覧にある正式な名前（大文字小文字）へ揃える（Windows のファイルは区別しないが、一覧・選択状態は区別するため）
                    if (idx >= 0)
                    {
                        string? canonical = host.GetProfileName(idx);
                        if (!string.IsNullOrEmpty(canonical)) strData[2] = canonical;
                    }

                    // 番号は入力スロット（ほかのコマンドと同じ）。その番号のコントローラーが接続中なら、いま適用する
                    IControllerSlotView? devitem = host.GetControllerDevice(tdevice);
                    if (idx >= 0 && devitem != null)
                    {
                        string prolog = string.Format(DS4Windows.Properties.Resources.UsingProfile,
                            (tdevice + 1).ToString(), strData[2], $"{devitem.Battery}");
                        if (!_profileApplication.ApplyProfile(tdevice, strData[2], false, true,
                            ProfileChangeSource.Manual, prolog))
                            AppLogger.LogWarn($"[IPC] LoadProfile: ApplyProfile failed (device={tdevice + 1}, profile='{strData[2]}')");
                    }
                    else
                    {
                        AppLogger.LogWarn($"[IPC] LoadProfile: not applied now (profileIndex={idx}, connected={devitem != null}, device={tdevice + 1}); only the profile path for later was set");
                        // Preset profile name for later loading
                        _profileRepo.ProfilePath[tdevice] = strData[2];
                    }
                }
                else
                {
                    // 一時プロファイルも、通常の適用と同じ入口（ProfileApplicationService）で適用する
                    // （報告の停止・読み込み・束縛の差し替え・通知・プログラムの起動を、1 か所で行う。以前の独自の経路は、停止の待ちに失敗すると何もせず戻っていた）
                    int idx = host.FindProfileIndex(strData[2]);
                    if (idx >= 0)
                    {
                        string? canonical = host.GetProfileName(idx);
                        if (!string.IsNullOrEmpty(canonical)) strData[2] = canonical;
                    }

                    IControllerSlotView? devitem = host.GetControllerDevice(tdevice);
                    if (devitem != null)
                    {
                        string prolog = string.Format(DS4Windows.Properties.Resources.UsingProfile,
                            (tdevice + 1).ToString(), strData[2], $"{devitem.Battery}");
                        if (!_profileApplication.ApplyProfile(tdevice, strData[2], true, true,
                            ProfileChangeSource.Manual, prolog))
                            AppLogger.LogWarn($"[IPC] LoadTempProfile: ApplyProfile failed (device={tdevice + 1}, profile='{strData[2]}')");
                    }
                    else
                    {
                        AppLogger.LogWarn($"[IPC] LoadTempProfile: no controller device in slot {tdevice + 1} (profile='{strData[2]}'); the profile was not applied");
                    }
                }

                IControllerSlotView? device = host.GetControllerDevice(tdevice);
                if (device != null)
                {
                    string prolog = string.Format(DS4Windows.Properties.Resources.UsingProfile, (tdevice + 1).ToString(), strData[2], $"{device.Battery}");
                    host.LogDebug(prolog);
                }
            }
        }

        // Command syntax: OutputSlot.slot#.Unplug／PlugDS4／PlugX360
        private void OutputSlot(string[] strData)
        {
            int tdevice = -1;
            if (int.TryParse(strData[1], out tdevice))
                tdevice--;

            if (tdevice >= 0 && tdevice < DS4Windows.Core.Settings.ControllerLimits.MaxControllerCount)
            {
                strData[2] = strData[2].ToLower();
                // 抜き差しの正式な入口（IOutputSlotService）経由。事前条件の確認と UI スレッドへの切り替えは、サービスが行う
                if (strData[2] == "unplug")
                    _outputSlotService.UnplugSlot(tdevice);
                else if (OutContTypeInfo.TryFromIpcPlugName(strData[2], out OutContType plugType))
                    _outputSlotService.PluginSlot(tdevice, plugType);
            }
        }

        // Command syntax: QueryProfile.device#.Name (fex "Query.1.ProfileName" would print out the name of the active profile in controller 1)
        private void Query(IIpcCommandHost host, string[] strData)
        {
            int tdevice = -1;
            string propValue = String.Empty;

            if (int.TryParse(strData[1], out tdevice))
                tdevice--;

            if (tdevice >= 0 && tdevice < DS4Windows.Core.Settings.ControllerLimits.MaxControllerCount)
            {
                string propName = strData[2].ToLower();
                InputDeviceCore[] controllers = _slots.Controllers;

                if (propName == "profilename")
                {
                    if (_profileSettings.GetUseTempProfile(tdevice))
                        propValue = _profileSettings.GetTempProfileName(tdevice);
                    else
                        propValue = _profileRepo.ProfilePath[tdevice];
                }
                else if (propName == "outconttype")
                    // 正本であるプロファイル設定（永続設定の OutContType）を返す
                    propValue = _profileSettings.OutputType(tdevice).ContType.ToString();
                else if (propName == "activeoutdevtype")
                    propValue = _outputSlotService.ActiveOutDevType[tdevice].ToString();
                else if (propName == "usedinputonly")
                    propValue = _profileSettings.UseDInputOnlyArray[tdevice].ToString();

                else if (propName == "devicevidpid" && controllers[tdevice] != null)
                    propValue = $"VID={controllers[tdevice].HidDevice.Attributes.VendorHexId}, PID={controllers[tdevice].HidDevice.Attributes.ProductHexId}";
                else if (propName == "devicepath" && controllers[tdevice] != null)
                    propValue = controllers[tdevice].HidDevice.DevicePath;
                else if (propName == "macaddress" && controllers[tdevice] != null)
                    propValue = controllers[tdevice].MacAddress;
                else if (propName == "displayname" && controllers[tdevice] != null)
                    propValue = controllers[tdevice].DisplayName;
                else if (propName == "conntype" && controllers[tdevice] != null)
                    propValue = controllers[tdevice].ConnectionType.ToString();
                else if (propName == "exclusivestatus" && controllers[tdevice] != null)
                    propValue = controllers[tdevice].CurrentExclusiveStatus.ToString();
                else if (propName == "battery" && controllers[tdevice] != null)
                    propValue = controllers[tdevice].Battery.ToString();
                else if (propName == "charging" && controllers[tdevice] != null)
                    propValue = controllers[tdevice].Charging.ToString();
                else if (propName == "outputslottype")
                    propValue = _virtualPad.DescribeSlot(tdevice, OutputSlotInfoKind.CurrentType);
                else if (propName == "outputslotpermanenttype")
                    propValue = _virtualPad.DescribeSlot(tdevice, OutputSlotInfoKind.PermanentType);
                else if (propName == "outputslotattachedstatus")
                    propValue = _virtualPad.DescribeSlot(tdevice, OutputSlotInfoKind.AttachedStatus);
                else if (propName == "outputslotinputbound")
                    propValue = _virtualPad.DescribeSlot(tdevice, OutputSlotInfoKind.InputBound);

                else if (propName == "apprunning")
                    propValue = _lifecycle.Running.ToString(); // Controller idx value is ignored, but it still needs to be in 1..4 range in a cmdline call
            }

            // Write out the property value to MMF result data file and notify a client process that the data is available
            _resultWriter.WriteQueryResult(propValue);
        }
    }
}
