# PhaseRV2 S3・S4 の処置の提案（承認用）

`scripts/audit/Propose-CatchActions.py` が、規則（D1・D2）から作った**提案**。承認のとき、人が見て直す。

- 対象 166 件（S3・S4。`ProfileSections` は RV5 まで対象外）
- 提案: 型を限定 37 件 / LogDebug を足す 43 件 / そのまま 86 件
- 毎フレームの経路・読み取りスレッド・タイマー・最上位の安全弁（`Input/Devices`、`Processing` の入力の加工・マッピング・アクション、`Output/Udp`・`Output/Devices` など）は、型を絞らず、ログも足さない（D2。Phase29 の教訓）。
- 型を限定すると、想定外の例外が上へ伝わる。限定した箇所は、実機確認の項目（Device-Check-Checklist §9c）へ足す。

## ファイル別

| ファイル | 件数 | 提案 |
|---|---|---|
| `App.xaml.cs` | 10 | LogDebug を足す（ファイルだけ） 3 / 型を限定 7 |
| `Composition/AppHost.cs` | 1 | そのまま（想定する例外が決められない） 1 |
| `Composition/Startup/AppShutdownSequence.cs` | 1 | LogDebug を足す（ファイルだけ） 1 |
| `Composition/Startup/SettingsLocationFallback.cs` | 1 | 型を限定 1 |
| `Core/Logging/AppLogger.cs` | 3 | LogDebug を足す（ファイルだけ） 2 / そのまま（想定する例外が決められない） 1 |
| `Core/Logging/LogRotator.cs` | 3 | LogDebug を足す（ファイルだけ） 1 / 型を限定 2 |
| `Core/Logging/LoggerHolder.cs` | 2 | 型を限定 1 / LogDebug を足す（ファイルだけ） 1 |
| `Core/Platform/DefaultProcessInspector.cs` | 1 | そのまま（想定する例外が決められない） 1 |
| `Core/Platform/EnvironmentService.cs` | 1 | 型を限定 1 |
| `Core/Platform/IpcMemoryMap.cs` | 3 | 型を限定 2 / LogDebug を足す（ファイルだけ） 1 |
| `Core/Platform/KeyboardSettings.cs` | 2 | LogDebug を足す（ファイルだけ） 2 |
| `Core/Platform/PathService.cs` | 1 | 型を限定 1 |
| `Core/Updater/UpdaterElevatedHelper.cs` | 1 | 型を限定 1 |
| `Core/Updater/UpdaterInstaller.cs` | 1 | 型を限定 1 |
| `Core/Utilities/Util.cs` | 7 | 型を限定 7 |
| `Core/Utilities/VersionUtil.cs` | 1 | LogDebug を足す（ファイルだけ） 1 |
| `Input/Devices/DualShock4Device.cs` | 1 | そのまま（毎フレーム・安全弁。型は絞らない） 1 |
| `Input/InputDeviceRegistry.cs` | 1 | そのまま（想定する例外が決められない） 1 |
| `Output/Backend/ViGEmVirtualPadBackend.cs` | 1 | LogDebug を足す（ファイルだけ） 1 |
| `Output/Services/OutputKbmInitializer.cs` | 1 | LogDebug を足す（ファイルだけ） 1 |
| `Output/Services/XboxParentHider.cs` | 2 | 型を限定 2 |
| `Processing/Actions/BatteryCheckAction.cs` | 2 | そのまま（毎フレーム・安全弁。型は絞らない） 2 |
| `Processing/Actions/ControlExtrasOutputAction.cs` | 1 | そのまま（毎フレーム・安全弁。型は絞らない） 1 |
| `Processing/Actions/DefaultActionManager.cs` | 17 | そのまま（毎フレーム・安全弁。型は絞らない） 17 |
| `Processing/Actions/DefaultKeyButtonActionControllerFactory.cs` | 2 | そのまま（毎フレーム・安全弁。型は絞らない） 2 |
| `Processing/Actions/GyroCalibrateAction.cs` | 2 | そのまま（毎フレーム・安全弁。型は絞らない） 2 |
| `Processing/Actions/KeyAction.cs` | 2 | そのまま（毎フレーム・安全弁。型は絞らない） 2 |
| `Processing/Actions/KeyButtonActionController.cs` | 22 | そのまま（毎フレーム・安全弁。型は絞らない） 22 |
| `Processing/Actions/KeyButtonActionControllerAdapter.cs` | 3 | そのまま（毎フレーム・安全弁。型は絞らない） 3 |
| `Processing/Actions/KeyOutputAction.cs` | 3 | そのまま（毎フレーム・安全弁。型は絞らない） 3 |
| `Processing/Actions/LaunchProcessAction.cs` | 1 | そのまま（毎フレーム・安全弁。型は絞らない） 1 |
| `Processing/Actions/MacroAction.cs` | 1 | そのまま（毎フレーム・安全弁。型は絞らない） 1 |
| `Processing/Actions/ProfileSwitchAction.cs` | 1 | そのまま（毎フレーム・安全弁。型は絞らない） 1 |
| `Processing/Actions/RepeatHelperToIRepeaterAdapter.cs` | 2 | そのまま（毎フレーム・安全弁。型は絞らない） 2 |
| `Processing/Actions/TriggerDispatchEngine.cs` | 2 | そのまま（毎フレーム・安全弁。型は絞らない） 2 |
| `Processing/Mapping.cs` | 2 | そのまま（毎フレーム・安全弁。型は絞らない） 2 |
| `Processing/RepeatHelper.cs` | 5 | そのまま（毎フレーム・安全弁。型は絞らない） 5 |
| `Processing/Services/Conditioning/MacroPlayback.cs` | 6 | そのまま（毎フレーム・安全弁。型は絞らない） 6 |
| `Processing/Services/Conditioning/MacroStepExecutor.cs` | 1 | そのまま（毎フレーム・安全弁。型は絞らない） 1 |
| `Processing/Services/Conditioning/SyntheticDispatcherService.cs` | 5 | そのまま（毎フレーム・安全弁。型は絞らない） 5 |
| `Processing/Services/Connection/DeviceDisconnectHandler.cs` | 1 | LogDebug を足す（ファイルだけ） 1 |
| `Processing/Services/Connection/DeviceInputCleanup.cs` | 1 | LogDebug を足す（ファイルだけ） 1 |
| `Processing/Services/Connection/FirstReportActionLogger.cs` | 1 | LogDebug を足す（ファイルだけ） 1 |
| `Processing/Services/Devices/DeviceRegistryHooks.cs` | 1 | LogDebug を足す（ファイルだけ） 1 |
| `Processing/Services/Devices/KeyButtonControllerRegistry.cs` | 3 | LogDebug を足す（ファイルだけ） 3 |
| `Processing/Services/Slots/AppliedProfileRecorder.cs` | 1 | LogDebug を足す（ファイルだけ） 1 |
| `Processing/Services/Slots/ProfileApplicationService.cs` | 7 | LogDebug を足す（ファイルだけ） 7 |
| `Processing/Services/Slots/ProfileProgramLauncher.cs` | 1 | LogDebug を足す（ファイルだけ） 1 |
| `UI/AssignmentEditor.xaml.cs` | 2 | LogDebug を足す（ファイルだけ） 2 |
| `UI/MainWindow.Ipc.cs` | 1 | 型を限定 1 |
| `UI/MainWindow.xaml.cs` | 6 | LogDebug を足す（ファイルだけ） 2 / 型を限定 4 |
| `UI/ProfileEditor.xaml.cs` | 2 | LogDebug を足す（ファイルだけ） 2 |
| `UI/Services/AppNotificationRegistration.cs` | 2 | LogDebug を足す（ファイルだけ） 1 / 型を限定 1 |
| `UI/Services/AppThemeAndCultureService.cs` | 4 | 型を限定 2 / LogDebug を足す（ファイルだけ） 2 |
| `UI/Services/GuardedTimerTick.cs` | 1 | そのまま（毎フレーム・安全弁。型は絞らない） 1 |
| `UI/UpdaterWindow.xaml.cs` | 4 | LogDebug を足す（ファイルだけ） 2 / 型を限定 2 |
| `UI/ViewModels/MainWindowsViewModel.cs` | 1 | 型を限定 1 |
| `UI/WindowPlacementHelper.cs` | 2 | LogDebug を足す（ファイルだけ） 2 |

## 一覧

| ファイル:行 | 型 | try の本体（先頭） | 分類 | 提案 |
|---|---|---|---|---|
| `App.xaml.cs:178` | (すべて) | _bootstrapPathService.FindConfigLocation(); _bootstrapConfig | S4 | LogDebug を足す（ファイルだけ） |
| `App.xaml.cs:205` | (すべて) | DS4Windows.Core.Logging.LoggerHolder.ApplyBootstrapMinLogLev | S4 | LogDebug を足す（ファイルだけ） |
| `App.xaml.cs:207` | (すべて) | if (!_bootstrapConfigLocationFound) throw new InvalidOperati | S3 | 型を限定: IOException・UnauthorizedAccessException |
| `App.xaml.cs:234` | Exception | AppLogger.LogInfo(StartupDiagnostics.CultureMessage()); AppL | S3 | 型を限定: IOException・UnauthorizedAccessException |
| `App.xaml.cs:250` | (すべて) | NLog.LogManager.Flush(); | S4 | LogDebug を足す（ファイルだけ） |
| `App.xaml.cs:273` | (すべて) | Process.GetCurrentProcess().PriorityClass = ProcessPriorityC | S3 | 型を限定: Win32Exception・InvalidOperationException |
| `App.xaml.cs:313` | (すべて) | DS4Windows.Core.Platform.KeyboardSettings.LoadFromRegistry() | S3 | 型を限定: SecurityException・IOException・UnauthorizedAccessException |
| `App.xaml.cs:410` | (すべて) | if (Directory.Exists(_pathService.RoamingAppDataPath)) { Dir | S3 | 型を限定: IOException・UnauthorizedAccessException |
| `App.xaml.cs:478` | Exception | Directory.CreateDirectory(Path.Combine(_pathService.AppDataP | S3 | 型を限定: IOException・UnauthorizedAccessException |
| `App.xaml.cs:515` | Exception | processExe = Process.GetCurrentProcess().MainModule?.FileNam | S3 | 型を限定: Win32Exception・InvalidOperationException |
| `Composition/AppHost.cs:133` | (すべて) | var manager = _host?.Services.GetService<DS4Windows.Processi | S3 | そのまま（想定する例外が決められない） |
| `Composition/Startup/AppShutdownSequence.cs:84` | (すべて) | _disposeHost(); | S4 | LogDebug を足す（ファイルだけ） |
| `Composition/Startup/SettingsLocationFallback.cs:50` | (すべて) | string roamingAppDataPath = _pathService.RoamingAppDataPath; | S3 | 型を限定: IOException・UnauthorizedAccessException |
| `Core/Logging/AppLogger.cs:51` | Exception | if (options != null && options.IsUserFacing) { GuiLog?.Invok | S4 | LogDebug を足す（ファイルだけ） |
| `Core/Logging/AppLogger.cs:68` | Exception | var manager = DS4Windows.Translations.Strings.ResourceManage | S3 | そのまま（想定する例外が決められない） |
| `Core/Logging/AppLogger.cs:184` | (すべて) | Logger.Debug("LogProfileChanged: Invoking ProfileChanged eve | S4 | LogDebug を足す（ファイルだけ） |
| `Core/Logging/LogRotator.cs:58` | (すべて) | archives[i].Delete(); | S4 | LogDebug を足す（ファイルだけ） |
| `Core/Logging/LogRotator.cs:61` | (すべて) | var archives = Directory.GetFiles(logsDir, "ds4windows_log_* | S3 | 型を限定: IOException・UnauthorizedAccessException |
| `Core/Logging/LogRotator.cs:63` | (すべて) | if (string.IsNullOrEmpty(appDataPath)) return; string logsDi | S3 | 型を限定: IOException・UnauthorizedAccessException・FormatException・OverflowException |
| `Core/Logging/LoggerHolder.cs:100` | (すべて) | var configuration = LogManager.Configuration; if (configurat | S3 | 型を限定: FormatException・OverflowException |
| `Core/Logging/LoggerHolder.cs:131` | (すべて) | fileTarget.KeepFileOpen = false; fileTarget.BufferSize = 0;  | S4 | LogDebug を足す（ファイルだけ） |
| `Core/Platform/DefaultProcessInspector.cs:37` | (すべて) | string temp = localAll[procInd].MainModule!.FileName; if (te | S3 | そのまま（想定する例外が決められない） |
| `Core/Platform/EnvironmentService.cs:381` | Exception | string path = Util.GetHidHideClientPath(); if (!string.IsNul | S3 | 型を限定: IOException・UnauthorizedAccessException |
| `Core/Platform/IpcMemoryMap.cs:78` | Exception | ipcNotifyEvent = EventWaitHandle.OpenExisting(ResultReadyEve | S3 | 型を限定: IOException・UnauthorizedAccessException |
| `Core/Platform/IpcMemoryMap.cs:107` | Exception | byte[] buffer = new byte[ClassNameBufferSize]; mmf = MemoryM | S3 | 型を限定: IOException・UnauthorizedAccessException |
| `Core/Platform/IpcMemoryMap.cs:130` | Exception | resultDataMmf = MemoryMappedFile.CreateNew(ResultDataMapName | S4 | LogDebug を足す（ファイルだけ） |
| `Core/Platform/KeyboardSettings.cs:43` | (すべて) | if (delayVal != null) delay = Convert.ToInt32(delayVal); | S4 | LogDebug を足す（ファイルだけ） |
| `Core/Platform/KeyboardSettings.cs:44` | (すべて) | if (speedVal != null) speed = Convert.ToInt32(speedVal); | S4 | LogDebug を足す（ファイルだけ） |
| `Core/Platform/PathService.cs:128` | (すべて) | Directory.CreateDirectory(path); | S3 | 型を限定: IOException・UnauthorizedAccessException |
| `Core/Updater/UpdaterElevatedHelper.cs:31` | (すべて) | Directory.Delete(sourcePath, true); | S3 | 型を限定: IOException・UnauthorizedAccessException |
| `Core/Updater/UpdaterInstaller.cs:42` | (すべて) | File.Delete(sourcePath); | S3 | 型を限定: IOException・UnauthorizedAccessException |
| `Core/Utilities/Util.cs:251` | (すべて) | using (Process? temp = Process.Start(startInfo)) { } | S3 | 型を限定: Win32Exception・InvalidOperationException |
| `Core/Utilities/Util.cs:286` | (すべて) | using (Process? temp = Process.Start(startInfo)) { } | S3 | 型を限定: Win32Exception・InvalidOperationException |
| `Core/Utilities/Util.cs:480` | (すべて) | string cleaned = uninstallStr.Trim(); if (cleaned.StartsWith | S3 | 型を限定: IOException・UnauthorizedAccessException |
| `Core/Utilities/Util.cs:488` | (すべて) | using (var sub = rk.OpenSubKey(subName)) { if (sub == null)  | S3 | 型を限定: IOException・UnauthorizedAccessException・SecurityException |
| `Core/Utilities/Util.cs:494` | (すべて) | using (var rk = Registry.LocalMachine.OpenSubKey(root)) { if | S3 | 型を限定: IOException・UnauthorizedAccessException・SecurityException |
| `Core/Utilities/Util.cs:533` | (すべて) | if (d.IndexOf("HidHide", StringComparison.OrdinalIgnoreCase) | S3 | 型を限定: IOException・UnauthorizedAccessException |
| `Core/Utilities/Util.cs:540` | (すべて) | string pf = Environment.GetFolderPath(Environment.SpecialFol | S3 | 型を限定: IOException・UnauthorizedAccessException |
| `Core/Utilities/VersionUtil.cs:22` | Exception | Version tmpVersion = new Version(versionStr); result = Compi | S4 | LogDebug を足す（ファイルだけ） |
| `Input/Devices/DualShock4Device.cs:467` | (すべて) | if (!writeOutput()) { if (quitOutputThreadOnError) { int win | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Input/InputDeviceRegistry.cs:259` | Exception | WindowsIdentity identity = WindowsIdentity.GetCurrent(); Win | S3 | そのまま（想定する例外が決められない） |
| `Output/Backend/ViGEmVirtualPadBackend.cs:27` | (すべて) | _client = new ViGEmClient(); | S4 | LogDebug を足す（ファイルだけ） |
| `Output/Services/OutputKbmInitializer.cs:47` | (すべて) | handlerConnected = _virtualKBM.Connect(); | S4 | LogDebug を足す（ファイルだけ） |
| `Output/Services/XboxParentHider.cs:230` | Exception | File.WriteAllLines(RecordPath, _added); | S3 | 型を限定: IOException・UnauthorizedAccessException |
| `Output/Services/XboxParentHider.cs:257` | Exception | if (File.Exists(RecordPath)) { File.Delete(RecordPath); } | S3 | 型を限定: IOException・UnauthorizedAccessException |
| `Processing/Actions/BatteryCheckAction.cs:97` | (すべて) | _port.SetForceLight(dev, false); | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/BatteryCheckAction.cs:152` | (すべて) | count = _actionRepository?.ActionList?.Count ?? 0; | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/ControlExtrasOutputAction.cs:79` | (すべて) | if (!(_values.HeavyRumble == _values.LightRumble && _values. | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/DefaultActionManager.cs:73` | (すべて) | int devices = ControllerLimits.MaxControllerCount; int contr | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/DefaultActionManager.cs:75` | (すべて) | try { int devices = ControllerLimits.MaxControllerCount; int | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/DefaultActionManager.cs:85` | (すべて) | var st = GetStateFor(action, device); if (st != null) return | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/DefaultActionManager.cs:117` | (すべて) | if (ent?.ActionDef == null) continue; if (ushort.TryParse(en | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/DefaultActionManager.cs:121` | (すべて) | lock (actions) { foreach (var ent in actions.Values) { try { | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/DefaultActionManager.cs:137` | (すべて) | if (ent?.States == null) continue; for (int d = 0; d < ent.S | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/DefaultActionManager.cs:141` | (すべて) | lock (actions) { foreach (var ent in actions.Values) { try { | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/DefaultActionManager.cs:176` | (すべて) | ToggledOnChanged?.Invoke(ent.ActionDef, device, old, false); | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/DefaultActionManager.cs:179` | (すべて) | bool old = ent.States[device]?.IsToggledOn ?? false; ent.Sta | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/DefaultActionManager.cs:182` | (すべて) | if (ent?.States == null) continue; if (device >= 0 && device | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/DefaultActionManager.cs:203` | (すべて) | var mapping = _mappingProvider(); var untrigger = mapping.Ca | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/DefaultActionManager.cs:205` | (すべて) | lock (actions) { foreach (var ent in actions.Values) { try { | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/DefaultActionManager.cs:220` | (すべて) | ToggledOnChanged?.Invoke(action, device, old, value); | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/DefaultActionManager.cs:242` | (すべて) | for (int i = 0; i < trace.FrameCount; i++) { var fr = trace. | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/DefaultActionManager.cs:246` | (すべて) | var trace = new StackTrace(1, false); string callerInfo = "( | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/DefaultActionManager.cs:248` | (すべて) | var ent = GetOrCreateEntryInternal(actions, action); if (ent | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/DefaultActionManager.cs:273` | (すべて) | if (buttonStates == null) return; if (device < 0 \|\| device > | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/DefaultKeyButtonActionControllerFactory.cs:36` | (すべて) | var key = device + ":" + (string.IsNullOrEmpty(actionName) ? | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/DefaultKeyButtonActionControllerFactory.cs:49` | (すべて) | var name = actionName ?? (sa?.name ?? "<sa>"); var key = dev | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/GyroCalibrateAction.cs:67` | (すべて) | count = _actionRepository?.ActionList?.Count ?? 0; | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/GyroCalibrateAction.cs:70` | (すべて) | int count = 0; try { count = _actionRepository?.ActionList?. | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyAction.cs:94` | (すべて) | ctrl?.Handle(binding, trigger); | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyAction.cs:120` | (すべて) | ctrl?.Handle(binding, trigger); | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyButtonActionController.cs:75` | (すべて) | _manager.ToggledOnChanged += toggledOnHandler; | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyButtonActionController.cs:98` | (すべて) | isToggle = sa != null && sa.keyType.HasFlag(DS4KeyType.Toggl | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyButtonActionController.cs:103` | (すべて) | if (sa != null && sa.KeyButtonSwitchMode.HasValue) this.mode | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyButtonActionController.cs:122` | (すべて) | _manager.ToggledOnChanged += toggledOnHandler; | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyButtonActionController.cs:190` | (すべて) | _dispatcher.SendPress(device, kvpKey, e.nativeKey, e.useScan | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyButtonActionController.cs:199` | (すべて) | e.repeater.Start(TimeSpan.Zero, TimeSpan.FromMilliseconds(DS | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyButtonActionController.cs:203` | (すべて) | e.repeater.Start(TimeSpan.Zero, TimeSpan.FromMilliseconds(DS | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyButtonActionController.cs:207` | (すべて) | if (assignedActionDef != null) manager.SetToggledOn(assigned | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyButtonActionController.cs:212` | (すべて) | OnToggleOff(kvpKey, nativeKey, useScanCode, handler); | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyButtonActionController.cs:215` | (すべて) | var st = assignedActionDef != null ? manager.GetStateFor(ass | S3 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyButtonActionController.cs:236` | (すべて) | if (assignedActionDef != null) manager.SetToggledOn(assigned | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyButtonActionController.cs:240` | (すべて) | try { if (assignedActionDef != null) manager.SetToggledOn(as | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyButtonActionController.cs:260` | (すべて) | if (e.nativeKey == 0) e.nativeKey = _dispatcher.ResolveNativ | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyButtonActionController.cs:281` | (すべて) | if (e.nativeKey == 0) e.nativeKey = _dispatcher.ResolveNativ | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyButtonActionController.cs:351` | (すべて) | localEntry.repeater.Start(TimeSpan.Zero, TimeSpan.FromMillis | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyButtonActionController.cs:355` | (すべて) | localEntry.repeater.Start(TimeSpan.Zero, TimeSpan.FromMillis | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyButtonActionController.cs:360` | (すべて) |  | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyButtonActionController.cs:382` | (すべて) | _dispatcher.SendRelease(device, kvpKey, e.nativeKey, e.useSc | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyButtonActionController.cs:405` | (すべて) | if (e.isPressed) _dispatcher.SendRelease(device, kvpKey, e.n | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyButtonActionController.cs:422` | (すべて) | if (e.isPressed) _dispatcher.SendRelease(device, k, e.native | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyButtonActionController.cs:441` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"KBC OnSAT | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyButtonActionController.cs:486` | (すべて) | _manager.ToggledOnChanged -= toggledOnHandler; | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyButtonActionControllerAdapter.cs:53` | (すべて) | if (inner == null \|\| trigger == null) return; if (AppLogger. | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyButtonActionControllerAdapter.cs:72` | (すべて) | if (inner == null \|\| trigger == null) return; if (AppLogger. | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyButtonActionControllerAdapter.cs:90` | (すべて) | if (inner == null \|\| trigger == null) return; if (AppLogger. | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyOutputAction.cs:103` | (すべて) | int device = ctx.Device; if (AppLogger.IsDebugEnabled) AppLo | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyOutputAction.cs:159` | (すべて) | int device = ctx.Device; if (AppLogger.IsDebugEnabled) AppLo | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/KeyOutputAction.cs:170` | (すべて) | var mapping = _profileSettings.OutputKBMMapping; if (mapping | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/LaunchProcessAction.cs:58` | (すべて) | count = _actionRepository?.ActionList?.Count ?? 0; | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/MacroAction.cs:109` | (すべて) | count = _actionRepository?.ActionList?.Count ?? 0; | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/ProfileSwitchAction.cs:120` | (すべて) | count = _actionRepository?.ActionList?.Count ?? 0; | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/RepeatHelperToIRepeaterAdapter.cs:52` | (すべて) | helperIRepeater ??= repeaterFactoryOverride(helperFactory);  | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/RepeatHelperToIRepeaterAdapter.cs:62` | (すべて) | if (tickAction != null) { DS4Windows.Core.Logging.AppLogger. | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/TriggerDispatchEngine.cs:639` | (すべて) | if (AppLogger.IsTraceEnabled) { if (AppLogger.IsTraceEnabled | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Actions/TriggerDispatchEngine.cs:653` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"Trigger r | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Mapping.cs:333` | (すべて) | previousState = _slots.Controllers[device]?.getPreviousState | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Mapping.cs:627` | (すべて) | if (deviceRuntime != null && device >= 0 && device < deviceR | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/RepeatHelper.cs:71` | (すべて) | _dispatcher.SendPress(device, kvpKey, nativeKey, useScanCode | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/RepeatHelper.cs:86` | (すべて) | _dispatcher.SendPress(device, kvpKey, nativeKey, useScanCode | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/RepeatHelper.cs:91` | (すべて) | if (DS4Windows.Core.Logging.AppLogger.IsTraceEnabled) DS4Win | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/RepeatHelper.cs:108` | (すべて) | if (DS4Windows.Core.Logging.AppLogger.IsTraceEnabled) DS4Win | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/RepeatHelper.cs:117` | (すべて) | timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeS | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Services/Conditioning/MacroPlayback.cs:82` | (すべて) | bool actionRunning = st != null && st.IsMacroRunning; string | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Services/Conditioning/MacroPlayback.cs:117` | (すべて) | actionDoneState = _manager.GetStateFor(action, device); | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Services/Conditioning/MacroPlayback.cs:169` | (すべて) | string decision = keepRunning ? "RERUN" : "NORUN"; if (AppLo | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Services/Conditioning/MacroPlayback.cs:189` | (すべて) | lock (actionDoneState) { actionDoneState.IsMacroRunning = fa | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Services/Conditioning/MacroPlayback.cs:211` | (すべて) | lock (actionDoneState) { actionDoneState.IsMacroRunning = tr | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Services/Conditioning/MacroPlayback.cs:249` | (すべて) | lock (actionDoneState) { actionDoneState.IsMacroRunning = fa | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Services/Conditioning/MacroStepExecutor.cs:114` | (すべて) | var accessor = _deviceStateAccessor; if (accessor != null) d | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Services/Conditioning/SyntheticDispatcherService.cs:41` | (すべて) | var mapping = _profileSettings.OutputKBMMapping; if (mapping | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Services/Conditioning/SyntheticDispatcherService.cs:76` | (すべて) | ds.nativeKeyAlias[kvpKey] = nativeKey; | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Services/Conditioning/SyntheticDispatcherService.cs:94` | (すべて) | var now = DateTime.UtcNow.Ticks; if (_syntheticInput.DeviceS | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Services/Conditioning/SyntheticDispatcherService.cs:142` | (すべて) | var now = DateTime.UtcNow.Ticks; if (_syntheticInput.DeviceS | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Services/Conditioning/SyntheticDispatcherService.cs:170` | (すべて) | var now = DateTime.UtcNow.Ticks; if (_syntheticInput.DeviceS | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `Processing/Services/Connection/DeviceDisconnectHandler.cs:96` | (すべて) | _mapping.HandleDeviceDisconnect(ind); | S4 | LogDebug を足す（ファイルだけ） |
| `Processing/Services/Connection/DeviceInputCleanup.cs:52` | (すべて) | uint native = 0; ds.nativeKeyAlias?.TryGetValue(k, out nativ | S4 | LogDebug を足す（ファイルだけ） |
| `Processing/Services/Connection/FirstReportActionLogger.cs:34` | (すべて) | _profileRepository.EmitMissingActionLogsForDevice(ind, false | S4 | LogDebug を足す（ファイルだけ） |
| `Processing/Services/Devices/DeviceRegistryHooks.cs:77` | (すべて) | var launcher = _elevatedProcessLauncher; if (launcher != nul | S4 | LogDebug を足す（ファイルだけ） |
| `Processing/Services/Devices/KeyButtonControllerRegistry.cs:63` | (すべて) | temp = _factory.Create(device, mode, actionName ?? "<mapping | S4 | LogDebug を足す（ファイルだけ） |
| `Processing/Services/Devices/KeyButtonControllerRegistry.cs:94` | (すべて) | temp = _factory.Create(device, sa, name); | S4 | LogDebug を足す（ファイルだけ） |
| `Processing/Services/Devices/KeyButtonControllerRegistry.cs:112` | (すべて) | if (inst != null && !string.Equals(inst.AssignedActionName,  | S4 | LogDebug を足す（ファイルだけ） |
| `Processing/Services/Slots/AppliedProfileRecorder.cs:50` | (すべて) | int slot = e.DeviceIndex; if (slot < 0 \|\| slot >= _ds4Contro | S4 | LogDebug を足す（ファイルだけ） |
| `Processing/Services/Slots/ProfileApplicationService.cs:83` | (すべて) | _actionManager.GetStateFor(sa, device); | S4 | LogDebug を足す（ファイルだけ） |
| `Processing/Services/Slots/ProfileApplicationService.cs:84` | (すべて) | _keyButtonControllers.GetOrCreateForAction(device, sa); | S4 | LogDebug を足す（ファイルだけ） |
| `Processing/Services/Slots/ProfileApplicationService.cs:86` | (すべて) | var sa = _profileActionProvider.GetProfileAction(device, act | S4 | LogDebug を足す（ファイルだけ） |
| `Processing/Services/Slots/ProfileApplicationService.cs:91` | (すべて) | var profileActions = _profileActionProvider.GetProfileAction | S4 | LogDebug を足す（ファイルだけ） |
| `Processing/Services/Slots/ProfileApplicationService.cs:360` | (すべて) | PreallocateForProfileApply(device); | S4 | LogDebug を足す（ファイルだけ） |
| `Processing/Services/Slots/ProfileApplicationService.cs:384` | (すべて) | isToggle = sa.keyType.HasFlag(DS4KeyType.Toggle); | S4 | LogDebug を足す（ファイルだけ） |
| `Processing/Services/Slots/ProfileApplicationService.cs:389` | (すべて) | var sa = _profileActionProvider.GetProfileAction(device, act | S4 | LogDebug を足す（ファイルだけ） |
| `Processing/Services/Slots/ProfileProgramLauncher.cs:51` | (すべて) | _launcher.LaunchWithWorkingDirectory(programPath); | S4 | LogDebug を足す（ファイルだけ） |
| `UI/AssignmentEditor.xaml.cs:332` | (すべて) | if (anchor != null) { var anchorPt = anchor.PointToScreen(ne | S4 | LogDebug を足す（ファイルだけ） |
| `UI/AssignmentEditor.xaml.cs:382` | (すべて) | if (anchor != null) { var anchorPt = anchor.PointToScreen(ne | S4 | LogDebug を足す（ファイルだけ） |
| `UI/MainWindow.Ipc.cs:38` | (すべて) | DS4Windows.Core.Platform.COPYDATASTRUCT cds = (DS4Windows.Co | S3 | 型を限定: IOException・UnauthorizedAccessException・FormatException・OverflowException |
| `UI/MainWindow.xaml.cs:199` | Exception | notifyIcon.ForceCreate(enablesEfficiencyMode: false); | S4 | LogDebug を足す（ファイルだけ） |
| `UI/MainWindow.xaml.cs:697` | (すべて) | int devIndex = e.DeviceIndex; string prof = e.ProfileName ?? | S4 | LogDebug を足す（ファイルだけ） |
| `UI/MainWindow.xaml.cs:1275` | (すべて) | using (Process temp = Process.Start(startInfo)) { } | S3 | 型を限定: Win32Exception・InvalidOperationException |
| `UI/MainWindow.xaml.cs:1309` | (すべて) | using (Process temp = Process.Start(startInfo)) { temp.WaitF | S3 | 型を限定: Win32Exception・InvalidOperationException |
| `UI/MainWindow.xaml.cs:1665` | (すべて) | ProcessStartInfo startInfo = new ProcessStartInfo(path); sta | S3 | 型を限定: Win32Exception・InvalidOperationException |
| `UI/MainWindow.xaml.cs:1687` | (すべて) | using (Process proc = Process.Start(path)) { } | S3 | 型を限定: Win32Exception・InvalidOperationException |
| `UI/ProfileEditor.xaml.cs:1192` | (すべて) | backend.RenameActionInEditedProfile(deviceNum, oldName, newN | S4 | LogDebug を足す（ファイルだけ） |
| `UI/ProfileEditor.xaml.cs:1377` | (すべて) | profileSettingsVM.RefreshExtraProfileInfo(); | S4 | LogDebug を足す（ファイルだけ） |
| `UI/Services/AppNotificationRegistration.cs:34` | (すべて) | SetCurrentProcessExplicitAppUserModelID(AppId); RegisterInRe | S4 | LogDebug を足す（ファイルだけ） |
| `UI/Services/AppNotificationRegistration.cs:180` | (すべて) | Assembly sdkAssembly = Assembly.Load("Microsoft.Windows.SDK. | S3 | 型を限定: XmlException・IOException |
| `UI/Services/AppThemeAndCultureService.cs:54` | (すべて) | string baseLogs = Path.Combine(AppContext.BaseDirectory, "Lo | S3 | 型を限定: IOException・UnauthorizedAccessException |
| `UI/Services/AppThemeAndCultureService.cs:70` | (すべて) | CultureInfo.DefaultThreadCurrentCulture = culture; CultureIn | S4 | LogDebug を足す（ファイルだけ） |
| `UI/Services/AppThemeAndCultureService.cs:73` | (すべて) | LocalizeDictionary.Instance.SetCurrentThreadCulture = true; | S4 | LogDebug を足す（ファイルだけ） |
| `UI/Services/AppThemeAndCultureService.cs:78` | (すべて) | try { string baseLogs = Path.Combine(AppContext.BaseDirector | S3 | 型を限定: IOException・UnauthorizedAccessException |
| `UI/Services/GuardedTimerTick.cs:25` | (すべて) | onError(ex); | S4 | そのまま（毎フレーム・安全弁。型は絞らない） |
| `UI/UpdaterWindow.xaml.cs:124` | (すべて) | proc.WaitForExit(); if (proc.ExitCode != 0) { Util.StartProc | S4 | LogDebug を足す（ファイルだけ） |
| `UI/UpdaterWindow.xaml.cs:132` | (すべて) | if (Directory.Exists(tempDir)) Directory.Delete(tempDir, tru | S3 | 型を限定: IOException・UnauthorizedAccessException |
| `UI/UpdaterWindow.xaml.cs:333` | (すべて) | if (Directory.Exists(tempDir)) Directory.Delete(tempDir, tru | S3 | 型を限定: IOException・UnauthorizedAccessException |
| `UI/UpdaterWindow.xaml.cs:339` | (すべて) | Util.StartProcessHelper(DS4Windows.Core.Updater.ReleaseSourc | S4 | LogDebug を足す（ファイルだけ） |
| `UI/ViewModels/MainWindowsViewModel.cs:73` | (すべて) | using (Process temp = Process.Start(startInfo)) { } | S3 | 型を限定: Win32Exception・InvalidOperationException |
| `UI/WindowPlacementHelper.cs:224` | (すべて) | var dpiForWindow = GetDpiForWindow(mainWindowHandle); if (dp | S4 | LogDebug を足す（ファイルだけ） |
| `UI/WindowPlacementHelper.cs:294` | (すべて) | int resWidth = resulting.Right - resulting.Left; int resHeig | S4 | LogDebug を足す（ファイルだけ） |


## 実施結果（2026-11-19 承認・実施）

- 型の限定: 29 件（提案 37 件のうち、外側の広い catch・ログ基盤・IPC の解析など 8 件は、想定する例外を決められないため、そのまま）。例外フィルター `catch (Exception caught) when (caught is …)` を使った。
- LogDebug: 39 件（提案 43 件のうち、`AppLogger`・`LoggerHolder` の中の 3 件は再帰を避けるため、`IpcMemoryMap`（CLI と共有で `AppLogger` を持てない）の 1 件は、そのまま）。Processing では `AppLogger.IsDebugEnabled` で守る。
- 実機確認: Device-Check-Checklist §9c。
