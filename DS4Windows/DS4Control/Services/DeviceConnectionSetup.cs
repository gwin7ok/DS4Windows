using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DS4Windows.DI;

namespace DS4Windows.Services
{
    /// <summary>
    /// Phase9 9-E E-6b（Phase9-E6-Plan.md）: <see cref="IDeviceConnectionSetup"/> の実装（DI の Singleton。状態なし）。
    /// 旧 <c>ControlService</c> の <c>CheckProfileOptions</c>／<c>CheckLauchProfileOption</c>／<c>SetupInitialHookEvents</c>／<c>TouchPadOn</c> を、そのまま移したもの
    /// （<c>touchPad[ind]</c> は引数で受ける。<c>TouchPadOn</c> のイベントは、旧コードと同じく、発火のたびに配列から引き直す）。
    /// DualSense の振動設定の枝は、E-6c で種類ごとの部品（<c>IInputDeviceModule</c>）へ移した。
    /// </summary>
    public sealed class DeviceConnectionSetup : IDeviceConnectionSetup
    {
        private readonly IProfileSettingsService _profileSettings;
        private readonly IUdpMotionService _udpMotion;
        private readonly StickMotionState _stickMotion;
        private readonly IInputDeviceModuleRegistry _deviceModules;

        public event Action<string, bool> DebugMessage;

        public DeviceConnectionSetup(IProfileSettingsService profileSettings, IUdpMotionService udpMotion, StickMotionState stickMotion,
            IInputDeviceModuleRegistry deviceModules)
        {
            _profileSettings = profileSettings ?? throw new ArgumentNullException(nameof(profileSettings));
            _udpMotion = udpMotion ?? throw new ArgumentNullException(nameof(udpMotion));
            _stickMotion = stickMotion ?? throw new ArgumentNullException(nameof(stickMotion));
            _deviceModules = deviceModules ?? throw new ArgumentNullException(nameof(deviceModules));
        }

        private void LogDebug(string data, bool warning = false)
        {
            DebugMessage?.Invoke(data, warning);
        }

        public void ApplyProfileOptions(int ind, DS4Device device, Mouse touchPad, bool startUp = false)
        {
            device.ModifyFeatureSetFlag(VidPidFeatureSet.NoOutputData, !_profileSettings.GetEnableOutputDataToDS4(ind));
            if (!_profileSettings.GetEnableOutputDataToDS4(ind))
                LogDebug("Output data to DS4 disabled. Lightbar and rumble events are not written to DS4 gamepad. If the gamepad is connected over BT then IdleDisconnect option is recommended to let DS4Windows to close the connection after long period of idling.");

            device.setIdleTimeout(_profileSettings.GetIdleDisconnectTimeout(ind));
            device.setBTPollRate(_profileSettings.GetBTPollRate(ind));

            // Phase6-Step12（デバウンス時間の回帰是正）: 接続中のデバイスのデバウンス時間を、適用するプロファイルの値へ更新する。
            // 移行前は、編集画面の保存が Global.DebouncingMsHasChanged() を呼んで更新していたが、9da60b85 で ApplyProfile に置き換わり更新されなくなった。
            device.Debouncer?.SetDuration(TimeSpan.FromMilliseconds(_profileSettings.DebouncingMs[ind]));

            touchPad.ResetTrackAccel(_profileSettings.GetTrackballFriction(ind));
            touchPad.ResetToggleGyroModes();

            //Global.TouchOutMode[ind] = TouchpadOutMode.MouseJoystick;
            touchPad.PostSetup();

            _profileSettings.L2OutputSettings[ind].TrigEffectSettings.maxValue = (byte)(Math.Max(_profileSettings.L2ModInfo[ind].maxOutput, _profileSettings.L2ModInfo[ind].maxZone) / 100.0 * 255);
            _profileSettings.R2OutputSettings[ind].TrigEffectSettings.maxValue = (byte)(Math.Max(_profileSettings.R2ModInfo[ind].maxOutput, _profileSettings.R2ModInfo[ind].maxZone) / 100.0 * 255);

            device.PrepareTriggerEffect(InputDevices.TriggerId.LeftTrigger, _profileSettings.L2OutputSettings[ind].TriggerEffect,
                _profileSettings.L2OutputSettings[ind].TrigEffectSettings);
            device.PrepareTriggerEffect(InputDevices.TriggerId.RightTrigger, _profileSettings.R2OutputSettings[ind].TriggerEffect,
                _profileSettings.R2OutputSettings[ind].TrigEffectSettings);

            device.RumbleAutostopTime = _profileSettings.GetRumbleAutostopTime(ind);
            device.setRumble(0, 0);
            device.LightBarColor = _profileSettings.GetMainColor(ind);

            // 種類ごとの設定（DualSense の振動エミュレーションと触覚の強さ）は、種類ごとの部品（Phase9 9-E E-6c）が反映する
            _deviceModules.Resolve(device.DeviceType).ApplyProfileOptions(device, ind, _profileSettings);

            if (!startUp)
            {
                CheckLauchProfileOption(ind, device);
            }
        }

        private void CheckLauchProfileOption(int ind, DS4Device device)
        {
            string programPath = _profileSettings.LaunchProgram[ind];
            if (programPath != string.Empty)
            {
                Process[] localAll = Process.GetProcesses();
                bool procFound = false;
                for (int procInd = 0, procsLen = localAll.Length; !procFound && procInd < procsLen; procInd++)
                {
                    try
                    {
                        string temp = localAll[procInd].MainModule.FileName;
                        if (temp == programPath)
                        {
                            procFound = true;
                        }
                    }
                    // Ignore any process for which this information
                    // is not exposed
                    catch { }
                }

                if (!procFound)
                {
                    Task processTask = new Task(() =>
                    {
                        Thread.Sleep(5000);
                        Process tempProcess = new Process();
                        tempProcess.StartInfo.FileName = programPath;
                        tempProcess.StartInfo.WorkingDirectory = new FileInfo(programPath).Directory.ToString();
                        //tempProcess.StartInfo.UseShellExecute = false;
                        try { tempProcess.Start(); }
                        catch { }
                    });

                    processTask.Start();
                }
            }
        }

        public void WireInitialHooks(int ind, DS4Device device)
        {
            _udpMotion.ResetSmoothingFilters(ind);   // Phase9 9-E E-3: 旧 ResetUdpSmoothingFilters

            // Phase9 Steering 削除 W2: 旧「ステアリング用のフィルター（OneEuroFilter）の作成と、設定［スムージング］との連動」は削除した（機能ごと削除）

            FlickStickSettings flickStickSettings = _profileSettings.LSOutputSettings[ind].outputSettings.flickSettings;
            flickStickSettings.RemoveRefreshEvents();
            flickStickSettings.SetRefreshEvents(_stickMotion.FlickMappingData[ind].flickFilter);

            flickStickSettings = _profileSettings.RSOutputSettings[ind].outputSettings.flickSettings;
            flickStickSettings.RemoveRefreshEvents();
            flickStickSettings.SetRefreshEvents(_stickMotion.FlickMappingData[ind].flickFilter);

            int tempIdx = ind;
            _profileSettings.L2OutputSettings[ind].ResetEvents();
            _profileSettings.L2ModInfo[ind].ResetEvents();
            _profileSettings.L2OutputSettings[ind].TriggerEffectChanged += (sender, e) =>
            {
                device.PrepareTriggerEffect(InputDevices.TriggerId.LeftTrigger, _profileSettings.L2OutputSettings[tempIdx].TriggerEffect,
                    _profileSettings.L2OutputSettings[tempIdx].TrigEffectSettings);
            };
            _profileSettings.L2ModInfo[ind].MaxOutputChanged += (sender, e) =>
            {
                TriggerDeadZoneZInfo tempInfo = sender as TriggerDeadZoneZInfo;
                _profileSettings.L2OutputSettings[tempIdx].TrigEffectSettings.maxValue = (byte)(Math.Max(tempInfo.maxOutput, tempInfo.maxZone) / 100.0 * 255.0);

                // Refresh trigger effect
                device.PrepareTriggerEffect(InputDevices.TriggerId.LeftTrigger, _profileSettings.L2OutputSettings[tempIdx].TriggerEffect,
                    _profileSettings.L2OutputSettings[tempIdx].TrigEffectSettings);
            };
            _profileSettings.L2ModInfo[ind].MaxZoneChanged += (sender, e) =>
            {
                TriggerDeadZoneZInfo tempInfo = sender as TriggerDeadZoneZInfo;
                _profileSettings.L2OutputSettings[tempIdx].TrigEffectSettings.maxValue = (byte)(Math.Max(tempInfo.maxOutput, tempInfo.maxZone) / 100.0 * 255.0);

                // Refresh trigger effect
                device.PrepareTriggerEffect(InputDevices.TriggerId.LeftTrigger, _profileSettings.L2OutputSettings[tempIdx].TriggerEffect,
                    _profileSettings.L2OutputSettings[tempIdx].TrigEffectSettings);
            };

            _profileSettings.R2OutputSettings[ind].ResetEvents();
            _profileSettings.R2OutputSettings[ind].TriggerEffectChanged += (sender, e) =>
            {
                device.PrepareTriggerEffect(InputDevices.TriggerId.RightTrigger, _profileSettings.R2OutputSettings[tempIdx].TriggerEffect,
                    _profileSettings.R2OutputSettings[tempIdx].TrigEffectSettings);
            };
            _profileSettings.R2ModInfo[ind].MaxOutputChanged += (sender, e) =>
            {
                TriggerDeadZoneZInfo tempInfo = sender as TriggerDeadZoneZInfo;
                _profileSettings.R2OutputSettings[tempIdx].TrigEffectSettings.maxValue = (byte)(tempInfo.maxOutput / 100.0 * 255.0);

                // Refresh trigger effect
                device.PrepareTriggerEffect(InputDevices.TriggerId.RightTrigger, _profileSettings.R2OutputSettings[tempIdx].TriggerEffect,
                    _profileSettings.R2OutputSettings[tempIdx].TrigEffectSettings);
            };
            _profileSettings.R2ModInfo[ind].MaxZoneChanged += (sender, e) =>
            {
                TriggerDeadZoneZInfo tempInfo = sender as TriggerDeadZoneZInfo;
                _profileSettings.R2OutputSettings[tempIdx].TrigEffectSettings.maxValue = (byte)(tempInfo.maxOutput / 100.0 * 255.0);

                // Refresh trigger effect
                device.PrepareTriggerEffect(InputDevices.TriggerId.RightTrigger, _profileSettings.R2OutputSettings[tempIdx].TriggerEffect,
                    _profileSettings.R2OutputSettings[tempIdx].TrigEffectSettings);
            };
        }

        public void AttachTouchpad(int ind, DS4Device device, Mouse[] touchPads)
        {
            Mouse tPad = touchPads[ind];
            //ITouchpadBehaviour tPad = touchPad[ind];
            device.Touchpad.TouchButtonDown += tPad.touchButtonDown;
            device.Touchpad.TouchButtonUp += tPad.touchButtonUp;
            device.Touchpad.TouchesBegan += tPad.touchesBegan;
            device.Touchpad.TouchesBegan += tPad.TouchStartedOrEnded;
            device.Touchpad.TouchesMoved += tPad.touchesMoved;
            device.Touchpad.TouchesEnded += tPad.touchesEnded;
            device.Touchpad.TouchesEnded += tPad.TouchStartedOrEnded;
            device.Touchpad.TouchUnchanged += tPad.touchUnchanged;
            //device.Touchpad.PreTouchProcess += delegate { touchPads[ind].populatePriorButtonStates(); };
            device.Touchpad.PreTouchProcess += (sender, args) => { touchPads[ind].populatePriorButtonStates(); };
            device.SixAxis.SixAccelMoved += tPad.sixaxisMoved;
            //LogDebug("Touchpad mode for " + device.MacAddress + " is now " + tmode.ToString());
            //Log.LogToTray("Touchpad mode for " + device.MacAddress + " is now " + tmode.ToString());
        }
    }
}
