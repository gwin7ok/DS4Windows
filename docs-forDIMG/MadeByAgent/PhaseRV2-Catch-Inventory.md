# PhaseRV2-0 空の catch の分類表（叩き台）

`scripts/audit/Inventory-EmptyCatch.py` が、キーワードで付けた**叩き台**。承認のとき、人が見て直す。

- 全 388 件。分類: S1 102 / S2 66 / S3 40 / S4 134 / S5 46
- 理由のコメントなし: 366 件、コメントあり: 22 件

## ファイル別の件数（上位）

- `Processing/Actions/KeyButtonActionController.cs` 59
- `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs` 29
- `Processing/Actions/DefaultActionManager.cs` 21
- `Core/Persistence/ProfileSections/ProfileTouchpadMouseLoadSection.cs` 16
- `App.xaml.cs` 14
- `Processing/Services/Conditioning/MacroPlayback.cs` 12
- `Processing/RepeatHelper.cs` 11
- `Processing/Services/Devices/KeyButtonControllerRegistry.cs` 11
- `Processing/Services/Slots/ProfileApplicationService.cs` 11
- `Processing/Actions/KeyButtonActionControllerAdapter.cs` 10
- `Processing/Actions/RepeatHelperToIRepeaterAdapter.cs` 9
- `UI/MainWindow.xaml.cs` 8
- `Core/Utilities/Util.cs` 7
- `Processing/Services/Connection/DeviceInputCleanup.cs` 7
- `Core/Persistence/ProfileSections/ProfileGyroControlsLoadSection.cs` 6

## 一覧

| ファイル:行 | 型 | try の本体（先頭） | コメント | 分類 | 推奨の処置 |
|---|---|---|---|---|---|
| `App.xaml.cs:178` | (すべて) | _bootstrapPathService.FindConfigLocation(); _bootstrapConfigLocationFo | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `App.xaml.cs:205` | (すべて) | DS4Windows.Core.Logging.LoggerHolder.ApplyBootstrapMinLogLevel(_bootst | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `App.xaml.cs:207` | (すべて) | if (!_bootstrapConfigLocationFound) throw new InvalidOperationExceptio | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `App.xaml.cs:221` | (すべて) | AppLogger.LogInfo(StartupDiagnostics.ArgsMessage(e.Args)); | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `App.xaml.cs:234` | Exception | AppLogger.LogInfo(StartupDiagnostics.CultureMessage()); AppLogger.LogI | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `App.xaml.cs:250` | (すべて) | NLog.LogManager.Flush(); | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `App.xaml.cs:273` | (すべて) | Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High; | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `App.xaml.cs:313` | (すべて) | DS4Windows.Core.Platform.KeyboardSettings.LoadFromRegistry(); | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `App.xaml.cs:410` | (すべて) | if (Directory.Exists(_pathService.RoamingAppDataPath)) { Directory.Del | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `App.xaml.cs:478` | Exception | Directory.CreateDirectory(Path.Combine(_pathService.AppDataPath, "Macr | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `App.xaml.cs:512` | (すべて) | AppLogger.LogInfo($"Process exe: {Process.GetCurrentProcess().MainModu | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `App.xaml.cs:513` | (すべて) | AppLogger.LogInfo($"AppDomain BaseDirectory: {AppContext.BaseDirectory | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `App.xaml.cs:514` | (すべて) | AppLogger.LogInfo($"Current working directory: {Environment.CurrentDir | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `App.xaml.cs:535` | (すべて) | AppLogger.LogInfo("Build summary not found in the executable"); | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Composition/AppHost.cs:133` | (すべて) | var manager = _host?.Services.GetService<DS4Windows.Processing.Actions | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `Composition/AppHost.cs:200` | (すべて) | _host.Dispose(); if (AppLogger.IsTraceEnabled) AppLogger.LogTrace("[DI | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Composition/Startup/AppShutdownSequence.cs:77` | (すべて) | _registry.ClearControllersForDevice(d); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Composition/Startup/AppShutdownSequence.cs:81` | (すべて) | if (_registry != null) { for (int d = 0; d < _controllerSlotCount; d++ | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Composition/Startup/AppShutdownSequence.cs:84` | (すべて) | _disposeHost(); | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Composition/Startup/SettingsLocationFallback.cs:50` | (すべて) | string roamingAppDataPath = _pathService.RoamingAppDataPath; string ex | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `Composition/Startup/SingleInstanceGuard.cs:37` | System.UnauthorizedAccessException | if (EventWaitHandleAcl.TryOpenExisting(SingleAppComEventName, System.S | あり | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Core/Logging/AppLogger.cs:169` | (すべて) | Logger.Debug("LogProfileChanged: Invoking ProfileChanged event"); Prof | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Core/Logging/LogRotator.cs:58` | (すべて) | archives[i].Delete(); | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Core/Logging/LogRotator.cs:61` | (すべて) | var archives = Directory.GetFiles(logsDir, "ds4windows_log_*.txt") .Wh | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `Core/Logging/LogRotator.cs:63` | (すべて) | if (string.IsNullOrEmpty(appDataPath)) return; string logsDir = Path.C | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `Core/Logging/LogWriter.cs:54` | (すべて) | StreamWriter stream = new StreamWriter(filename); foreach(string line  | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Core/Logging/LoggerHolder.cs:100` | (すべて) | var configuration = LogManager.Configuration; if (configuration == nul | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `Core/Logging/LoggerHolder.cs:131` | (すべて) | fileTarget.KeepFileOpen = false; fileTarget.BufferSize = 0; fileTarget | あり | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Core/Persistence/AppSettingsXmlSerializer.cs:93` | (すべて) | AppSettingsDTO dto = null; try { var xr = new XmlRootAttribute("Profil | あり | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/AppSettingsXmlSerializer.cs:386` | (すべて) | setting.ParseOrKeep(xmlSupport.SelectSingleNode(setting.XmlName)?.Inne | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/AppSettingsXmlSerializer.cs:404` | (すべて) | if (File.Exists(store.m_Profile)) { string fileXml = File.ReadAllText( | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileIdFile.cs:41` | (すべて) | if (string.IsNullOrEmpty(path) \|\| !File.Exists(path)) return null; var | あり | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileMigration.cs:74` | XmlException | profileReader = XmlReader.Create(new StringReader(currentMigrationText | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileGyroControlsLoadSection.cs:64` | (すべて) | Item = xmlGyroControlsElement.SelectSingleNode("Triggers"); if (Item ! | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileGyroControlsLoadSection.cs:74` | (すべて) | Item = xmlGyroControlsElement.SelectSingleNode("TriggerCond"); if (Ite | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileGyroControlsLoadSection.cs:84` | (すべて) | Item = xmlGyroControlsElement.SelectSingleNode("TriggerTurns"); if (bo | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileGyroControlsLoadSection.cs:94` | (すべて) | Item = xmlGyroControlsElement.SelectSingleNode("Toggle"); if (bool.Try | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileGyroControlsLoadSection.cs:131` | (すべて) | Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickOutput | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileGyroControlsLoadSection.cs:141` | (すべて) | Item = m_Xdoc.SelectSingleNode("/" + rootname + "/GyroMouseStickOutput | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:36` | (すべて) | Item = lsAxialDeadElement.SelectSingleNode("DeadZoneX"); int.TryParse( | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:45` | (すべて) | Item = lsAxialDeadElement.SelectSingleNode("DeadZoneY"); int.TryParse( | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:53` | (すべて) | Item = lsAxialDeadElement.SelectSingleNode("MaxZoneX"); int.TryParse(I | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:61` | (すべて) | Item = lsAxialDeadElement.SelectSingleNode("MaxZoneY"); int.TryParse(I | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:69` | (すべて) | Item = lsAxialDeadElement.SelectSingleNode("AntiDeadZoneX"); int.TryPa | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:77` | (すべて) | Item = lsAxialDeadElement.SelectSingleNode("AntiDeadZoneY"); int.TryPa | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:85` | (すべて) | Item = lsAxialDeadElement.SelectSingleNode("MaxOutputX"); double.TryPa | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:93` | (すべて) | Item = lsAxialDeadElement.SelectSingleNode("MaxOutputY"); double.TryPa | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:109` | (すべて) | Item = lsDeltaAccelElement.SelectSingleNode("Enabled"); if (bool.TryPa | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:120` | (すべて) | Item = lsDeltaAccelElement.SelectSingleNode("Multiplier"); if (double. | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:131` | (すべて) | Item = lsDeltaAccelElement.SelectSingleNode("MaxTravel"); if (double.T | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:142` | (すべて) | Item = lsDeltaAccelElement.SelectSingleNode("MinTravel"); if (double.T | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:153` | (すべて) | Item = lsDeltaAccelElement.SelectSingleNode("EasingDuration"); if (dou | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:164` | (すべて) | Item = lsDeltaAccelElement.SelectSingleNode("MinFactor"); if (double.T | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:175` | (すべて) | Item = m_Xdoc.SelectSingleNode("/" + rootname + "/RSDeadZoneType"); if | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:191` | (すべて) | Item = rsAxialDeadElement.SelectSingleNode("DeadZoneX"); int.TryParse( | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:200` | (すべて) | Item = rsAxialDeadElement.SelectSingleNode("DeadZoneY"); int.TryParse( | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:208` | (すべて) | Item = rsAxialDeadElement.SelectSingleNode("MaxZoneX"); int.TryParse(I | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:216` | (すべて) | Item = rsAxialDeadElement.SelectSingleNode("MaxZoneY"); int.TryParse(I | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:224` | (すべて) | Item = rsAxialDeadElement.SelectSingleNode("AntiDeadZoneX"); int.TryPa | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:232` | (すべて) | Item = rsAxialDeadElement.SelectSingleNode("AntiDeadZoneY"); int.TryPa | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:240` | (すべて) | Item = rsAxialDeadElement.SelectSingleNode("MaxOutputX"); double.TryPa | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:248` | (すべて) | Item = rsAxialDeadElement.SelectSingleNode("MaxOutputY"); double.TryPa | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:264` | (すべて) | Item = rsDeltaAccelElement.SelectSingleNode("Enabled"); if (bool.TryPa | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:275` | (すべて) | Item = rsDeltaAccelElement.SelectSingleNode("Multiplier"); if (double. | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:286` | (すべて) | Item = rsDeltaAccelElement.SelectSingleNode("MaxTravel"); if (double.T | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:297` | (すべて) | Item = rsDeltaAccelElement.SelectSingleNode("MinTravel"); if (double.T | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:308` | (すべて) | Item = rsDeltaAccelElement.SelectSingleNode("EasingDuration"); if (dou | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileStickAxialLoadSection.cs:319` | (すべて) | Item = rsDeltaAccelElement.SelectSingleNode("MinFactor"); if (double.T | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileTouchpadMouseLoadSection.cs:62` | (すべて) |  | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileTouchpadMouseLoadSection.cs:72` | (すべて) | Item = xmlTouchMStickSmoothingElement.SelectSingleNode("DeadZone"); if | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileTouchpadMouseLoadSection.cs:82` | (すべて) | Item = xmlTouchMStickSmoothingElement.SelectSingleNode("MaxZone"); if  | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileTouchpadMouseLoadSection.cs:93` | (すべて) | Item = xmlTouchMStickSmoothingElement.SelectSingleNode("OutputStick"); | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileTouchpadMouseLoadSection.cs:111` | (すべて) | Item = xmlTouchMStickSmoothingElement.SelectSingleNode("OutputStickAxe | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileTouchpadMouseLoadSection.cs:121` | (すべて) | Item = xmlTouchMStickSmoothingElement.SelectSingleNode("AntiDeadX"); i | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileTouchpadMouseLoadSection.cs:131` | (すべて) | Item = xmlTouchMStickSmoothingElement.SelectSingleNode("AntiDeadY"); i | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileTouchpadMouseLoadSection.cs:142` | (すべて) | Item = xmlTouchMStickSmoothingElement.SelectSingleNode("Invert"); if ( | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileTouchpadMouseLoadSection.cs:152` | (すべて) | Item = xmlTouchMStickSmoothingElement.SelectSingleNode("MaxOutput"); i | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileTouchpadMouseLoadSection.cs:162` | (すべて) | Item = xmlTouchMStickSmoothingElement.SelectSingleNode("MaxOutputEnabl | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileTouchpadMouseLoadSection.cs:172` | (すべて) | Item = xmlTouchMStickSmoothingElement.SelectSingleNode("VerticalScale" | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileTouchpadMouseLoadSection.cs:182` | (すべて) | Item = xmlTouchMStickSmoothingElement.SelectSingleNode("OutputCurve"); | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileTouchpadMouseLoadSection.cs:193` | (すべて) | Item = xmlTouchMStickSmoothingElement.SelectSingleNode("Rotation"); if | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileTouchpadMouseLoadSection.cs:220` | (すべて) | Item = xmlTouchMStickSmoothingElement.SelectSingleNode("SmoothingMetho | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileTouchpadMouseLoadSection.cs:230` | (すべて) | Item = xmlTouchMStickSmoothingElement.SelectSingleNode("SmoothingMinCu | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileTouchpadMouseLoadSection.cs:240` | (すべて) | Item = xmlTouchMStickSmoothingElement.SelectSingleNode("SmoothingBeta" | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileTriggerCurveLoadSection.cs:91` | (すべて) | Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2TwoStageMode"); if | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileTriggerCurveLoadSection.cs:102` | (すべて) | Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2HipFireTime"); if  | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileTriggerCurveLoadSection.cs:112` | (すべて) | Item = m_Xdoc.SelectSingleNode("/" + rootname + "/L2TriggerEffect"); i | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileTriggerCurveLoadSection.cs:127` | (すべて) | Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2TwoStageMode"); if | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileTriggerCurveLoadSection.cs:137` | (すべて) | Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2HipFireTime"); if  | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileTriggerCurveLoadSection.cs:147` | (すべて) | Item = m_Xdoc.SelectSingleNode("/" + rootname + "/R2TriggerEffect"); i | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/ProfileSections/ProfileTriggerStickBasicLoadSection.cs:280` | (すべて) | Item = m_Xdoc.SelectSingleNode("/" + rootname + "/LSDeadZoneType"); if | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/SpecialActionRepository.cs:147` | (すべて) | if (mode == 4 && ushort.TryParse(details.Split(' ')[0], out ushort sav | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Core/Persistence/SpecialActionRepository.cs:235` | (すべて) | _runtimeReset.ClearAllActionStates(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Core/Persistence/SpecialActionRepository.cs:237` | (すべて) | try { _runtimeReset.ClearAllActionStates(); } catch { } | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Core/Persistence/SpecialActionRepository.cs:344` | (すべて) | string[] profiles = Directory.GetFiles(_pathService.AppDataPath + @"\P | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Persistence/SpecialActionsXmlSerializer.cs:376` | (すべて) | _runtimeReset.ClearAllActionStates(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Core/Persistence/SpecialActionsXmlSerializer.cs:389` | (すべて) | _runtimeReset.ClearAllActionStates(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Core/Persistence/SpecialActionsXmlSerializer.cs:394` | (すべて) | try { _runtimeReset.ClearAllActionStates(); } catch { } store.RaiseAct | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Core/Persistence/SpecialActionsXmlSerializer.cs:437` | (すべて) | _runtimeReset.ClearAllActionStates(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Core/Persistence/SpecialActionsXmlSerializer.cs:440` | (すべて) | try { _runtimeReset.ClearAllActionStates(); } catch { } store.RaiseAct | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Core/Platform/DefaultProcessInspector.cs:37` | (すべて) | string temp = localAll[procInd].MainModule!.FileName; if (temp == exeP | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `Core/Platform/EnvironmentService.cs:381` | Exception | string path = Util.GetHidHideClientPath(); if (!string.IsNullOrEmpty(p | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `Core/Platform/IpcMemoryMap.cs:52` | Exception | StringBuilder wndClassNameStr = new StringBuilder(ClassNameBufferSize) | あり | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Core/Platform/IpcMemoryMap.cs:78` | Exception | ipcNotifyEvent = EventWaitHandle.OpenExisting(ResultReadyEventName); b | あり | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `Core/Platform/IpcMemoryMap.cs:107` | Exception | byte[] buffer = new byte[ClassNameBufferSize]; mmf = MemoryMappedFile. | あり | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `Core/Platform/IpcMemoryMap.cs:130` | Exception | resultDataMmf = MemoryMappedFile.CreateNew(ResultDataMapName, IpcResul | あり | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Core/Platform/IpcMemoryMap.cs:156` | Exception | if (ipcNotifyEvent == null \|\| ipcNotifyEvent.WaitOne(10000)) { byte[]  | あり | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Core/Platform/KeyboardSettings.cs:34` | (すべて) | AppLogger.LogInfo("KeyboardSettings.LoadFromRegistry: registry key not | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Core/Platform/KeyboardSettings.cs:43` | (すべて) | if (delayVal != null) delay = Convert.ToInt32(delayVal); | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Core/Platform/KeyboardSettings.cs:44` | (すべて) | if (speedVal != null) speed = Convert.ToInt32(speedVal); | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Core/Platform/KeyboardSettings.cs:57` | (すべて) | AppLogger.LogInfo($"Keyboard settings loaded: KeyboardDelay[{DelayInde | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Core/Platform/KeyboardSettings.cs:62` | (すべて) | AppLogger.LogError($"KeyboardSettings.LoadFromRegistry failed: {ex}"); | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Core/Platform/PathService.cs:128` | (すべて) | Directory.CreateDirectory(path); | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `Core/Platform/Win32WindowSystem.cs:60` | (すべて) | if (!IsWindowVisible(hWnd)) return true; if (GetWindowTextLength(hWnd) | あり | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Core/Profiles/AutoProfileHolder.cs:86` | InvalidOperationException | AutoProfilesDTO dto = (serializer.Deserialize(sr) as AutoProfilesDTO)! | なし | S1 | 型は限定済み。理由のコメントがあればよい |
| `Core/Profiles/AutoProfileHolder.cs:87` | XmlException |  | なし | S1 | 型は限定済み。理由のコメントがあればよい |
| `Core/Settings/BackingStore.cs:571` | (すべて) | loggedInvalidActions.Add(actionname); | なし | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Settings/UiCulture.cs:22` | (すべて) | CultureInfo ci = CultureInfo.GetCultureInfo(culture); Thread.CurrentTh | あり | S2 | 保存ファイル・設定の読み込み。欠けた要素は既定値のまま（RV5 で畳む対象は触らない） |
| `Core/Updater/UpdaterElevatedHelper.cs:31` | (すべて) | Directory.Delete(sourcePath, true); | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `Core/Updater/UpdaterInstaller.cs:42` | (すべて) | File.Delete(sourcePath); | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `Core/Utilities/Util.cs:251` | (すべて) | using (Process? temp = Process.Start(startInfo)) { } | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `Core/Utilities/Util.cs:286` | (すべて) | using (Process? temp = Process.Start(startInfo)) { } | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `Core/Utilities/Util.cs:480` | (すべて) | string cleaned = uninstallStr.Trim(); if (cleaned.StartsWith("\"") &&  | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `Core/Utilities/Util.cs:488` | (すべて) | using (var sub = rk.OpenSubKey(subName)) { if (sub == null) continue;  | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `Core/Utilities/Util.cs:494` | (すべて) | using (var rk = Registry.LocalMachine.OpenSubKey(root)) { if (rk == nu | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `Core/Utilities/Util.cs:533` | (すべて) | if (d.IndexOf("HidHide", StringComparison.OrdinalIgnoreCase) >= 0) { v | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `Core/Utilities/Util.cs:540` | (すべて) | string pf = Environment.GetFolderPath(Environment.SpecialFolder.Progra | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `Core/Utilities/VersionUtil.cs:22` | Exception | Version tmpVersion = new Version(versionStr); result = CompileVersionN | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Input/Devices/DualShock4Device.cs:467` | (すべて) | if (!writeOutput()) { if (quitOutputThreadOnError) { int winError = Ma | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Input/Devices/DualShock4Device.cs:525` | ThreadInterruptedException | while (!exitOutputThread) { if (outReportBuffer == null \|\| outputRepor | なし | S1 | 型は限定済み。理由のコメントがあればよい |
| `Input/InputDeviceRegistry.cs:259` | Exception | WindowsIdentity identity = WindowsIdentity.GetCurrent(); WindowsPrinci | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `Input/InputDeviceRegistry.cs:452` | (すべて) | tempDev.CloseDevice(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Input/Xbox/BluetoothBatteryReader.cs:57` | AggregateException | entry.Pending?.Wait(maxWait); | あり | S1 | 型は限定済み。理由のコメントがあればよい |
| `Output/Backend/ViGEmVirtualPadBackend.cs:27` | (すべて) | _client = new ViGEmClient(); | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Output/Kbm/FakerInputHandler.cs:75` | SEHException | fakerInput!.Disconnect(); | あり | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Output/Services/OutputKBMHandlerLifecycle.cs:52` | Exception | AppLogger.LogDebug($"Output KBM handler fakeKeyRepeat={handler.fakeKey | あり | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Output/Services/OutputKbmInitializer.cs:47` | (すべて) | handlerConnected = _virtualKBM.Connect(); | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Output/Services/XboxParentHider.cs:230` | Exception | File.WriteAllLines(RecordPath, _added); | あり | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `Output/Services/XboxParentHider.cs:257` | Exception | if (File.Exists(RecordPath)) { File.Delete(RecordPath); } | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `Processing/Actions/ActionEntry.cs:22` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"ActionEntry created | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/Actions/BatteryCheckAction.cs:72` | (すべて) | AppLogger.LogWarn($"BatteryCheckAction.Execute failed: {ex}"); | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/Actions/BatteryCheckAction.cs:88` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"BatteryCheckAction. | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/Actions/BatteryCheckAction.cs:97` | (すべて) | _port.SetForceLight(dev, false); | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/BatteryCheckAction.cs:152` | (すべて) | count = _actionRepository?.ActionList?.Count ?? 0; | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/BatteryCheckAction.cs:155` | (すべて) | if (AppLogger.IsTraceEnabled) { if (AppLogger.IsTraceEnabled) AppLogge | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/ButtonOutputAction.cs:112` | (すべて) | var parts = new List<string>(); (ctx.Frame as IFrameOutputTrace)?.Appe | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/ButtonOutputAction.cs:114` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"Trigger detected fo | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/ControlExtrasOutputAction.cs:79` | (すべて) | if (!(_values.HeavyRumble == _values.LightRumble && _values.LightRumbl | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/DefaultActionManager.cs:33` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"ActionManager.Toggl | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/Actions/DefaultActionManager.cs:39` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"Mapping: ToggledOnC | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/Actions/DefaultActionManager.cs:73` | (すべて) | int devices = ControllerLimits.MaxControllerCount; int controls = (int | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/DefaultActionManager.cs:75` | (すべて) | try { int devices = ControllerLimits.MaxControllerCount; int controls  | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/DefaultActionManager.cs:85` | (すべて) | var st = GetStateFor(action, device); if (st != null) return st.BeingT | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/DefaultActionManager.cs:117` | (すべて) | if (ent?.ActionDef == null) continue; if (ushort.TryParse(ent.ActionDe | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/DefaultActionManager.cs:121` | (すべて) | lock (actions) { foreach (var ent in actions.Values) { try { if (ent?. | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/DefaultActionManager.cs:137` | (すべて) | if (ent?.States == null) continue; for (int d = 0; d < ent.States.Leng | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/DefaultActionManager.cs:141` | (すべて) | lock (actions) { foreach (var ent in actions.Values) { try { if (ent?. | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/DefaultActionManager.cs:153` | (すべて) | lock (actions) { actions.Clear(); } | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/DefaultActionManager.cs:173` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"DefaultActionManage | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/Actions/DefaultActionManager.cs:176` | (すべて) | ToggledOnChanged?.Invoke(ent.ActionDef, device, old, false); | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/DefaultActionManager.cs:179` | (すべて) | bool old = ent.States[device]?.IsToggledOn ?? false; ent.States[device | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/DefaultActionManager.cs:182` | (すべて) | if (ent?.States == null) continue; if (device >= 0 && device < ent.Sta | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/DefaultActionManager.cs:203` | (すべて) | var mapping = _mappingProvider(); var untrigger = mapping.CaptureProfi | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/DefaultActionManager.cs:205` | (すべて) | lock (actions) { foreach (var ent in actions.Values) { try { if (ent?. | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/DefaultActionManager.cs:220` | (すべて) | ToggledOnChanged?.Invoke(action, device, old, value); | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/DefaultActionManager.cs:242` | (すべて) | for (int i = 0; i < trace.FrameCount; i++) { var fr = trace.GetFrame(i | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/DefaultActionManager.cs:246` | (すべて) | var trace = new StackTrace(1, false); string callerInfo = "(unknown)"; | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/DefaultActionManager.cs:248` | (すべて) | var ent = GetOrCreateEntryInternal(actions, action); if (ent == null)  | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/DefaultActionManager.cs:273` | (すべて) | if (buttonStates == null) return; if (device < 0 \|\| device >= buttonSt | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/DefaultControllerRegistry.cs:37` | (すべて) | controllers[k]?.Dispose(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/DefaultKeyButtonActionControllerFactory.cs:36` | (すべて) | var key = device + ":" + (string.IsNullOrEmpty(actionName) ? "<default | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/DefaultKeyButtonActionControllerFactory.cs:49` | (すべて) | var name = actionName ?? (sa?.name ?? "<sa>"); var key = device + ":"  | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/DisconnectBTAction.cs:74` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"DisconnectBTAction. | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/Actions/DisconnectBTAction.cs:102` | (すべて) | AppLogger.LogWarn($"DisconnectBTAction.Execute failed: {ex}"); | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/Actions/DisconnectBTAction.cs:122` | (すべて) | if (!AppLogger.IsTraceEnabled) return; AppLogger.LogTrace($"Trigger de | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/GyroCalibrateAction.cs:53` | (すべて) | if (AppLogger.IsTraceEnabled) { if (AppLogger.IsTraceEnabled) AppLogge | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/GyroCalibrateAction.cs:61` | (すべて) | AppLogger.LogWarn($"GyroCalibrateAction.Execute failed: {ex}"); | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/Actions/GyroCalibrateAction.cs:67` | (すべて) | count = _actionRepository?.ActionList?.Count ?? 0; | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/GyroCalibrateAction.cs:70` | (すべて) | int count = 0; try { count = _actionRepository?.ActionList?.Count ?? 0 | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyAction.cs:77` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"KeyAction.OnTrigger | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyAction.cs:79` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"KeyAction.OnTrigger | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/Actions/KeyAction.cs:94` | (すべて) | ctrl?.Handle(binding, trigger); | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyAction.cs:120` | (すべて) | ctrl?.Handle(binding, trigger); | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyButtonActionController.cs:54` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"KeyButtonActionCont | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/Actions/KeyButtonActionController.cs:70` | (すべて) | impl.ClearAll(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:73` | (すべて) | if (sa == this.assignedActionDef && dev == this.device && oldv == true | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:75` | (すべて) | _manager.ToggledOnChanged += toggledOnHandler; | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyButtonActionController.cs:98` | (すべて) | isToggle = sa != null && sa.keyType.HasFlag(DS4KeyType.Toggle); | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyButtonActionController.cs:103` | (すべて) | if (sa != null && sa.KeyButtonSwitchMode.HasValue) this.mode = sa.KeyB | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyButtonActionController.cs:117` | (すべて) | impl.ClearAll(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:120` | (すべて) | if (sa == this.assignedActionDef && dev == this.device && oldv == true | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:122` | (すべて) | _manager.ToggledOnChanged += toggledOnHandler; | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyButtonActionController.cs:190` | (すべて) | _dispatcher.SendPress(device, kvpKey, e.nativeKey, e.useScanCode, e.ha | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyButtonActionController.cs:198` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"ToggleImpl.OnDown:  | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/Actions/KeyButtonActionController.cs:199` | (すべて) | e.repeater.Start(TimeSpan.Zero, TimeSpan.FromMilliseconds(DS4Windows.C | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyButtonActionController.cs:203` | (すべて) | e.repeater.Start(TimeSpan.Zero, TimeSpan.FromMilliseconds(DS4Windows.C | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyButtonActionController.cs:207` | (すべて) | if (assignedActionDef != null) manager.SetToggledOn(assignedActionDef, | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyButtonActionController.cs:212` | (すべて) | OnToggleOff(kvpKey, nativeKey, useScanCode, handler); | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyButtonActionController.cs:215` | (すべて) | var st = assignedActionDef != null ? manager.GetStateFor(assignedActio | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `Processing/Actions/KeyButtonActionController.cs:222` | (すべて) | _dispatcher.ResetKeyTiming(device, kvpKey); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:231` | (すべて) | e.repeater.Stop(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:232` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"ToggleImpl.OnToggle | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/Actions/KeyButtonActionController.cs:236` | (すべて) | if (assignedActionDef != null) manager.SetToggledOn(assignedActionDef, | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyButtonActionController.cs:240` | (すべて) | try { if (assignedActionDef != null) manager.SetToggledOn(assignedActi | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyButtonActionController.cs:242` | (すべて) | try { e.repeater.Stop(); } catch { } try { if (AppLogger.IsTraceEnable | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:246` | (すべて) | _dispatcher.ResetKeyTiming(0, kvpKey); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:254` | (すべて) | e.repeater?.Stop(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:260` | (すべて) | if (e.nativeKey == 0) e.nativeKey = _dispatcher.ResolveNativeKey(kvpKe | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyButtonActionController.cs:263` | (すべて) | _dispatcher.ResetKeyTiming(0, kvpKey); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:281` | (すべて) | if (e.nativeKey == 0) e.nativeKey = _dispatcher.ResolveNativeKey(k); _ | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyButtonActionController.cs:282` | (すべて) | e.repeater?.Dispose(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:284` | (すべて) | _dispatcher.ResetKeyTiming(0, k); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:286` | (すべて) | var e = entries[k]; try { if (e.nativeKey == 0) e.nativeKey = _dispatc | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:289` | (すべて) | var keys = new List<ushort>(entries.Keys); foreach (var k in keys) { t | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:351` | (すべて) | localEntry.repeater.Start(TimeSpan.Zero, TimeSpan.FromMilliseconds(DS4 | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyButtonActionController.cs:355` | (すべて) | localEntry.repeater.Start(TimeSpan.Zero, TimeSpan.FromMilliseconds(DS4 | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyButtonActionController.cs:359` | OperationCanceledException | await Task.Delay(TimeSpan.FromMilliseconds(DS4Windows.Core.Platform.Ke | なし | S1 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `Processing/Actions/KeyButtonActionController.cs:360` | (すべて) |  | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyButtonActionController.cs:363` | (すべて) | e.delayCts = new CancellationTokenSource(); var localEntry = e; Task.R | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:375` | (すべて) | e.delayCts?.Cancel(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:379` | (すべて) | e.repeater.Stop(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:382` | (すべて) | _dispatcher.SendRelease(device, kvpKey, e.nativeKey, e.useScanCode, e. | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyButtonActionController.cs:385` | (すべて) | try { e.delayCts?.Cancel(); } catch { } if (e.repeater != null) { try  | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:389` | (すべて) | _dispatcher.ResetKeyTiming(device, kvpKey); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:396` | (すべて) | Clear(kvpKey); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:403` | (すべて) | e.delayCts?.Cancel(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:404` | (すべて) | e.repeater?.Stop(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:405` | (すべて) | if (e.isPressed) _dispatcher.SendRelease(device, kvpKey, e.nativeKey,  | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyButtonActionController.cs:409` | (すべて) | _dispatcher.ResetKeyTiming(0, kvpKey); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:421` | (すべて) | e.delayCts?.Cancel(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:422` | (すべて) | if (e.isPressed) _dispatcher.SendRelease(device, k, e.nativeKey, e.use | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyButtonActionController.cs:423` | (すべて) | e.repeater?.Dispose(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:425` | (すべて) | _dispatcher.ResetKeyTiming(0, k); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:427` | (すべて) | var e = entries[k]; try { e.delayCts?.Cancel(); } catch { } try { if ( | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:430` | (すべて) | var keys = new List<ushort>(entries.Keys); foreach (var k in keys) { t | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:441` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"KBC OnSATriggerEsta | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyButtonActionController.cs:451` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"KBC OnSATriggerRele | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/Actions/KeyButtonActionController.cs:463` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"KBC OnSATriggerTogg | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/Actions/KeyButtonActionController.cs:484` | (すべて) | impl.ClearAll(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionController.cs:486` | (すべて) | _manager.ToggledOnChanged -= toggledOnHandler; | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyButtonActionController.cs:487` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"KeyButtonActionCont | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/Actions/KeyButtonActionController.cs:492` | (すべて) | Dispose(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionControllerAdapter.cs:32` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"KBC Adapter ctor(de | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/Actions/KeyButtonActionControllerAdapter.cs:41` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"KBC Adapter wrap ct | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/Actions/KeyButtonActionControllerAdapter.cs:49` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"KBC Adapter Start:  | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyButtonActionControllerAdapter.cs:53` | (すべて) | if (inner == null \|\| trigger == null) return; try { if (AppLogger.IsTr | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyButtonActionControllerAdapter.cs:61` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"KBC Adapter Handle: | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyButtonActionControllerAdapter.cs:72` | (すべて) | if (inner == null \|\| trigger == null) return; try { if (AppLogger.IsTr | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyButtonActionControllerAdapter.cs:80` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"KBC Adapter Stop: C | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyButtonActionControllerAdapter.cs:90` | (すべて) | if (inner == null \|\| trigger == null) return; try { if (AppLogger.IsTr | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyButtonActionControllerAdapter.cs:95` | (すべて) | inner?.ClearKeyEntries(0); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyButtonActionControllerAdapter.cs:100` | (すべて) | inner?.Dispose(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyOutputAction.cs:103` | (すべて) | int device = ctx.Device; if (AppLogger.IsDebugEnabled) AppLogger.LogDe | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyOutputAction.cs:121` | (すべて) | var parts = new List<string>(); (ctx.Frame as IFrameOutputTrace)?.Appe | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyOutputAction.cs:123` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"Trigger detected fo | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/KeyOutputAction.cs:159` | (すべて) | int device = ctx.Device; if (AppLogger.IsDebugEnabled) AppLogger.LogDe | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/KeyOutputAction.cs:170` | (すべて) | var mapping = _profileSettings.OutputKBMMapping; if (mapping != null)  | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/LaunchProcessAction.cs:58` | (すべて) | count = _actionRepository?.ActionList?.Count ?? 0; | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/LaunchProcessAction.cs:61` | (すべて) | if (AppLogger.IsTraceEnabled) { if (AppLogger.IsTraceEnabled) AppLogge | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/LaunchProcessAction.cs:102` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"LaunchProcessAction | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/Actions/LaunchProcessAction.cs:106` | (すべて) | AppLogger.LogWarn($"LaunchProcessAction.Execute failed: {ex}"); | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/Actions/MacroAction.cs:109` | (すべて) | count = _actionRepository?.ActionList?.Count ?? 0; | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/MacroAction.cs:113` | (すべて) | if (AppLogger.IsTraceEnabled) { if (AppLogger.IsTraceEnabled) AppLogge | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/MacroStringAction.cs:61` | (すべて) | if (!AppLogger.IsTraceEnabled) return; AppLogger.LogTrace($"Trigger de | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/ProfileSwitchAction.cs:120` | (すべて) | count = _actionRepository?.ActionList?.Count ?? 0; | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/ProfileSwitchAction.cs:123` | (すべて) | if (AppLogger.IsTraceEnabled) { if (AppLogger.IsTraceEnabled) AppLogge | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/RepeatHelperToIRepeaterAdapter.cs:40` | (すべて) | DS4Windows.Core.Logging.AppLogger.LogTrace("RepeatHelperToIRepeaterAda | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/Actions/RepeatHelperToIRepeaterAdapter.cs:52` | (すべて) | helperIRepeater ??= repeaterFactoryOverride(helperFactory); helperIRep | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/RepeatHelperToIRepeaterAdapter.cs:62` | (すべて) | if (tickAction != null) { try { DS4Windows.Core.Logging.AppLogger.LogT | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/RepeatHelperToIRepeaterAdapter.cs:69` | (すべて) | helperIRepeater.Stop(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/RepeatHelperToIRepeaterAdapter.cs:70` | (すべて) | helper?.Stop(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/RepeatHelperToIRepeaterAdapter.cs:72` | (すべて) | if (helperIRepeater != null) try { helperIRepeater.Stop(); } catch { } | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/RepeatHelperToIRepeaterAdapter.cs:79` | (すべて) | helperIRepeater?.Dispose(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/RepeatHelperToIRepeaterAdapter.cs:80` | (すべて) | helper?.Dispose(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/RepeatHelperToIRepeaterAdapter.cs:84` | (すべて) | try { helperIRepeater?.Dispose(); } catch { } try { helper?.Dispose(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/TriggerDispatchEngine.cs:303` | (すべて) | rt.ReleaseInput = input; rt.Suppressed.RemoveWhere(rt.ReleasePredicate | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Actions/TriggerDispatchEngine.cs:639` | (すべて) | if (AppLogger.IsTraceEnabled) { if (AppLogger.IsTraceEnabled) AppLogge | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/TriggerDispatchEngine.cs:653` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"Trigger released fo | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Actions/TriggerDispatchEngine.cs:678` | (すべて) | AppLogger.Log(AppLogLevel.Error, $"MapCustomAction exception: {ex}", L | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/Mapping.cs:333` | (すべて) | previousState = _slots.Controllers[device]?.getPreviousStateRef(); | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Mapping.cs:627` | (すべて) | if (deviceRuntime != null && device >= 0 && device < deviceRuntime.Len | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Mapping.cs:629` | (すべて) | _inputCleanup.Clear(device); try { if (deviceRuntime != null && device | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Mapping.cs:691` | ArgumentOutOfRangeException | for (; copied < count; copied++) buffer[copied] = names[copied]; | なし | S1 | 型は限定済み。理由のコメントがあればよい |
| `Processing/Mapping.cs:716` | (すべて) | _engine.ReleaseSuppressed(device, _triggerInputs[device]); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Mapping.cs:744` | (すべて) | AppLogger.Log(AppLogLevel.Error, $"MapCustomAction exception: {ex}", L | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/RepeatHelper.cs:52` | (すべて) | if (DS4Windows.Core.Logging.AppLogger.IsTraceEnabled) DS4Windows.Core. | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/RepeatHelper.cs:71` | (すべて) | _dispatcher.SendPress(device, kvpKey, nativeKey, useScanCode, handler) | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/RepeatHelper.cs:83` | (すべて) | if (DS4Windows.Core.Logging.AppLogger.IsTraceEnabled) DS4Windows.Core. | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/RepeatHelper.cs:86` | (すべて) | _dispatcher.SendPress(device, kvpKey, nativeKey, useScanCode, handler) | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/RepeatHelper.cs:91` | (すべて) | try { if (DS4Windows.Core.Logging.AppLogger.IsTraceEnabled) DS4Windows | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/RepeatHelper.cs:104` | (すべて) | if (DS4Windows.Core.Logging.AppLogger.IsTraceEnabled) DS4Windows.Core. | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/RepeatHelper.cs:108` | (すべて) | try { if (DS4Windows.Core.Logging.AppLogger.IsTraceEnabled) DS4Windows | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/RepeatHelper.cs:116` | (すべて) | if (DS4Windows.Core.Logging.AppLogger.IsTraceEnabled) DS4Windows.Core. | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/RepeatHelper.cs:117` | (すべて) | timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan); | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/RepeatHelper.cs:118` | (すべて) | timer.Dispose(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/RepeatHelper.cs:120` | (すべて) | try { if (DS4Windows.Core.Logging.AppLogger.IsTraceEnabled) DS4Windows | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Services/Conditioning/MacroPlayback.cs:52` | (すべて) | dict.Clear(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Services/Conditioning/MacroPlayback.cs:56` | (すべて) | var queues = _macroRuntime.TaskQueue; if (queues != null && device >=  | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Services/Conditioning/MacroPlayback.cs:82` | (すべて) | bool actionRunning = st != null && st.IsMacroRunning; string decision  | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Conditioning/MacroPlayback.cs:117` | (すべて) | actionDoneState = _manager.GetStateFor(action, device); | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Conditioning/MacroPlayback.cs:123` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"PlayMacroTask SKIP: | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/Services/Conditioning/MacroPlayback.cs:141` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"PlayMacroTask START | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Conditioning/MacroPlayback.cs:169` | (すべて) | string decision = keepRunning ? "RERUN" : "NORUN"; if (AppLogger.IsTra | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Conditioning/MacroPlayback.cs:189` | (すべて) | lock (actionDoneState) { actionDoneState.IsMacroRunning = false; } | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Conditioning/MacroPlayback.cs:192` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"PlayMacroTask END:  | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Conditioning/MacroPlayback.cs:211` | (すべて) | lock (actionDoneState) { actionDoneState.IsMacroRunning = true; } | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Conditioning/MacroPlayback.cs:249` | (すべて) | lock (actionDoneState) { actionDoneState.IsMacroRunning = false; } | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Conditioning/MacroPlayback.cs:251` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"PlayMacroTask MACRO | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Conditioning/MacroStepExecutor.cs:114` | (すべて) | var accessor = _deviceStateAccessor; if (accessor != null) d = accesso | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Conditioning/SyntheticDispatcherService.cs:41` | (すべて) | var mapping = _profileSettings.OutputKBMMapping; if (mapping != null)  | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Conditioning/SyntheticDispatcherService.cs:76` | (すべて) | ds.nativeKeyAlias[kvpKey] = nativeKey; | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Conditioning/SyntheticDispatcherService.cs:94` | (すべて) | var now = DateTime.UtcNow.Ticks; if (_syntheticInput.DeviceState != nu | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Conditioning/SyntheticDispatcherService.cs:142` | (すべて) | var now = DateTime.UtcNow.Ticks; if (_syntheticInput.DeviceState != nu | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Conditioning/SyntheticDispatcherService.cs:170` | (すべて) | var now = DateTime.UtcNow.Ticks; if (_syntheticInput.DeviceState != nu | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Connection/DeviceDisconnectHandler.cs:96` | (すべて) | _mapping.HandleDeviceDisconnect(ind); | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Connection/DeviceInputCleanup.cs:52` | (すべて) | uint native = 0; ds.nativeKeyAlias?.TryGetValue(k, out native); _synth | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Connection/DeviceInputCleanup.cs:54` | (すべて) | ds.keyPresses.Clear(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Services/Connection/DeviceInputCleanup.cs:55` | (すべて) | ds.nativeKeyAlias!.Clear(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Services/Connection/DeviceInputCleanup.cs:59` | (すべて) | if (deviceState != null && device >= 0 && device < deviceState.Length) | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Services/Connection/DeviceInputCleanup.cs:63` | (すべて) | _macroPlayback.ClearDevice(device); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Services/Connection/DeviceInputCleanup.cs:66` | (すべて) | _keyButtonControllers.ClearForDevice(device); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Services/Connection/DeviceInputCleanup.cs:68` | (すべて) | try { if (deviceState != null && device >= 0 && device < deviceState.L | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Services/Connection/FirstReportActionLogger.cs:34` | (すべて) | _profileRepository.EmitMissingActionLogsForDevice(ind, false); | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Devices/DeviceRegistryHooks.cs:77` | (すべて) | var launcher = _elevatedProcessLauncher; if (launcher != null) { int?  | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Devices/DeviceRegistryHooks.cs:101` | (すべて) | Process? child = Process.Start(startInfo); if (!child!.WaitForExit(300 | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Services/Devices/KeyButtonControllerRegistry.cs:63` | (すべて) | temp = _factory.Create(device, mode, actionName ?? "<mapping>"); | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Devices/KeyButtonControllerRegistry.cs:67` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"KBC DIAGNOSTIC: Cre | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/Services/Devices/KeyButtonControllerRegistry.cs:71` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"KBC DIAGNOSTIC: Reu | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/Services/Devices/KeyButtonControllerRegistry.cs:94` | (すべて) | temp = _factory.Create(device, sa, name); | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Devices/KeyButtonControllerRegistry.cs:98` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"KBC DIAGNOSTIC: Cre | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/Services/Devices/KeyButtonControllerRegistry.cs:102` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"KBC DIAGNOSTIC: Reu | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/Services/Devices/KeyButtonControllerRegistry.cs:112` | (すべて) | if (inst != null && !string.Equals(inst.AssignedActionName, name, Stri | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Devices/KeyButtonControllerRegistry.cs:143` | (すべて) | inst?.Dispose(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Services/Devices/KeyButtonControllerRegistry.cs:144` | (すべて) | _controllerRegistry.Unregister(k); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Services/Devices/KeyButtonControllerRegistry.cs:147` | (すべて) | var inst = _controllers[k]; try { inst?.Dispose(); } catch { } try { _ | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Services/Devices/KeyButtonControllerRegistry.cs:152` | (すべて) | lock (_lock) { var prefix = device + ":"; var keys = new List<string>( | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Services/Slots/ActionsLoadedRuntimeReset.cs:33` | (すべて) | _actionManager.ClearAllToggledOn(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Services/Slots/ActionsLoadedRuntimeReset.cs:37` | (すべて) | _syntheticInput.ResetAll(); try { _actionManager.ClearAllToggledOn();  | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Services/Slots/AppliedProfileRecorder.cs:50` | (すべて) | int slot = e.DeviceIndex; if (slot < 0 \|\| slot >= _ds4Controllers.Leng | あり | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Slots/ProfileApplicationService.cs:83` | (すべて) | _actionManager.GetStateFor(sa, device); | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Slots/ProfileApplicationService.cs:84` | (すべて) | _keyButtonControllers.GetOrCreateForAction(device, sa); | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Slots/ProfileApplicationService.cs:86` | (すべて) | var sa = _profileActionProvider.GetProfileAction(device, actionName);  | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Slots/ProfileApplicationService.cs:89` | (すべて) | if (AppLogger.IsTraceEnabled) AppLogger.LogTrace($"ActionManager.Preal | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `Processing/Services/Slots/ProfileApplicationService.cs:91` | (すべて) | var profileActions = _profileActionProvider.GetProfileActionNames(devi | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Slots/ProfileApplicationService.cs:341` | (すべて) | _actionManager.ClearAllEntries(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Services/Slots/ProfileApplicationService.cs:352` | (すべて) | _actionManager.ClearDeviceState(device); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Services/Slots/ProfileApplicationService.cs:360` | (すべて) | PreallocateForProfileApply(device); | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Slots/ProfileApplicationService.cs:384` | (すべて) | isToggle = sa.keyType.HasFlag(DS4KeyType.Toggle); | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Slots/ProfileApplicationService.cs:389` | (すべて) | var sa = _profileActionProvider.GetProfileAction(device, actionName);  | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Services/Slots/ProfileApplicationService.cs:421` | (すべて) | _actionRegistry.Clear(deviceIndex); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `Processing/Services/Slots/ProfileProgramLauncher.cs:51` | (すべて) | _launcher.LaunchWithWorkingDirectory(programPath); | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `Processing/Synthetic/SyntheticInputState.cs:83` | (すべて) | actionManager?.ClearToggledOnForKey(key); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `UI/AssignmentEditor.xaml.cs:332` | (すべて) | if (anchor != null) { var anchorPt = anchor.PointToScreen(new Point(0, | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `UI/AssignmentEditor.xaml.cs:382` | (すべて) | if (anchor != null) { var anchorPt = anchor.PointToScreen(new Point(0, | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `UI/AssignmentEditor.xaml.cs:410` | (すべて) | ownerWindow.Close(); | なし | S1 | 後始末・解放。型は絞らず、理由のコメント（または IOException など限定） |
| `UI/MainWindow.Ipc.cs:38` | (すべて) | DS4Windows.Core.Platform.COPYDATASTRUCT cds = (DS4Windows.Core.Platfor | あり | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `UI/MainWindow.xaml.cs:199` | Exception | notifyIcon.ForceCreate(enablesEfficiencyMode: false); | あり | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `UI/MainWindow.xaml.cs:421` | COMException | scope.Connect(); | なし | S1 | 型は限定済み。理由のコメントがあればよい |
| `UI/MainWindow.xaml.cs:422` | ManagementException |  | なし | S1 | 型は限定済み。理由のコメントがあればよい |
| `UI/MainWindow.xaml.cs:699` | (すべて) | int devIndex = e.DeviceIndex; string prof = e.ProfileName ?? string.Em | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `UI/MainWindow.xaml.cs:1277` | (すべて) | using (Process temp = Process.Start(startInfo)) { } | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `UI/MainWindow.xaml.cs:1311` | (すべて) | using (Process temp = Process.Start(startInfo)) { temp.WaitForExit();  | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `UI/MainWindow.xaml.cs:1667` | (すべて) | ProcessStartInfo startInfo = new ProcessStartInfo(path); startInfo.Use | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `UI/MainWindow.xaml.cs:1689` | (すべて) | using (Process proc = Process.Start(path)) { } | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `UI/ProfileEditor.xaml.cs:1192` | (すべて) | backend.RenameActionInEditedProfile(deviceNum, oldName, newName); prof | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `UI/ProfileEditor.xaml.cs:1377` | (すべて) | profileSettingsVM.RefreshExtraProfileInfo(); | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `UI/Services/AppNotificationRegistration.cs:34` | (すべて) | SetCurrentProcessExplicitAppUserModelID(AppId); RegisterInRegistry(); | あり | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `UI/Services/AppNotificationRegistration.cs:157` | (すべて) | AppLogger.LogTrace($"[ModernToast] Dispatch Stacked Toast: ToastID(Tag | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `UI/Services/AppNotificationRegistration.cs:180` | (すべて) | Assembly sdkAssembly = Assembly.Load("Microsoft.Windows.SDK.NET"); Typ | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `UI/Services/AppThemeAndCultureService.cs:54` | (すべて) | string baseLogs = Path.Combine(AppContext.BaseDirectory, "Logs"); if ( | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `UI/Services/AppThemeAndCultureService.cs:70` | (すべて) | CultureInfo.DefaultThreadCurrentCulture = culture; CultureInfo.Default | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `UI/Services/AppThemeAndCultureService.cs:73` | (すべて) | LocalizeDictionary.Instance.SetCurrentThreadCulture = true; | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `UI/Services/AppThemeAndCultureService.cs:78` | (すべて) | try { string baseLogs = Path.Combine(AppContext.BaseDirectory, "Logs") | あり | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `UI/Services/AppThemeAndCultureService.cs:119` | CultureNotFoundException | AppLogger.LogDebug($"SetUICulture requested: {culture}"); CultureInfo  | あり | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `UI/Services/GuardedTimerTick.cs:25` | (すべて) | onError(ex); | あり | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `UI/Services/NotificationService.cs:13` | (すべて) | DS4Windows.Core.Logging.AppLogger.LogToTray(message); | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `UI/UpdaterWindow.xaml.cs:124` | (すべて) | proc.WaitForExit(); if (proc.ExitCode != 0) { Util.StartProcessHelper( | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `UI/UpdaterWindow.xaml.cs:132` | (すべて) | if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `UI/UpdaterWindow.xaml.cs:333` | (すべて) | if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `UI/UpdaterWindow.xaml.cs:339` | (すべて) | Util.StartProcessHelper(DS4Windows.Core.Updater.ReleaseSource.LatestRe | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `UI/ViewModels/MainWindowsViewModel.cs:73` | (すべて) | using (Process temp = Process.Start(startInfo)) { } | なし | S3 | ファイル I/O・プロセス・レジストリ。想定する例外へ型を絞る（D2） |
| `UI/ViewModels/TrayIconViewModel.cs:67` | InvalidOperationException | TooltipTextChanged?.Invoke(this, EventArgs.Empty); | なし | S1 | 型は限定済み。理由のコメントがあればよい |
| `UI/WindowPlacementHelper.cs:224` | (すべて) | var dpiForWindow = GetDpiForWindow(mainWindowHandle); if (dpiForWindow | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `UI/WindowPlacementHelper.cs:231` | (すべて) | AppLogger.LogTrace($"ApplyPlacement: Target monitor scale = {targetSca | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `UI/WindowPlacementHelper.cs:261` | (すべて) | var dpiB = GetDpiForWindow(mainWindowHandle); AppLogger.LogTrace($"App | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `UI/WindowPlacementHelper.cs:274` | (すべて) | var dpiA = GetDpiForWindow(mainWindowHandle); AppLogger.LogTrace($"App | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
| `UI/WindowPlacementHelper.cs:294` | (すべて) | int resWidth = resulting.Right - resulting.Left; int resHeight = resul | なし | S4 | 原因が追えない。Debug／Trace のファイルのログを足す（D1）か、理由のコメント |
| `UI/WindowPlacementHelper.cs:352` | (すべて) | AppLogger.LogTrace($"GetLogicalPlacement: PhysicalRect={physicalRect}  | なし | S5 | ログ呼び出しを包むだけ。RV2-2 で try ごと消す（AppLogger の入口で吸収） |
