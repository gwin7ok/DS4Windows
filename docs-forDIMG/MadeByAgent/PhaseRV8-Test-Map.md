# PhaseRV8 テストの分類表（案。ユーザーの承認待ち）

作成日: 2026-11-20

文字列でソースを読むテスト 134 ファイルの 1 件ずつの分類の**第 1 案**。分類はファイル名・読む対象・検査の内容からの機械的な推定で、承認後に 1 件ずつ中身を確認してから実施する（置き換え・統合・廃止のたびに、わざと違反を入れて失敗することを確かめる）。

| 分類 | 意味 |
|---|---|
| 残す | XAML・.csproj・.resx・ワークフローなど、型の検査にできない文字列の検査 |
| 型へ | 型・メンバー・参照の検査。ArchUnitNET／リフレクションへ置き換える |
| 表へ | 型・メンバーの不在の検査。`RemovedApiRuleTests` の表へ統合する |
| 要確認 | 中身を読んでから決める |

| テストのファイル | 件数 | 読む対象 | 第 1 案 |
|---|---|---|---|
| ActionAndProfileReferenceIdTests | 16 | App.xaml.cs | 要確認 |
| ActionIdTests | 13 | MacroViewModel.cs | 要確認 |
| AltTabRemovalGuardTests | 3 | Mapping.cs, MacroPlayback.cs, MacroStepExecutor.cs ほか | 残す |
| AppHostNoImplicitHostGuardTests | 3 | AppHostNoImplicitHostGuardTests.cs, AppHost.cs, BackingStore.cs ほか | 型へ |
| AppHostStartupShutdownGuardTests | 3 | App.xaml.cs, AppHost.cs | 型へ |
| AppPostHostContractTests | 7 | ProfileRepository.cs, SpecialActionRepository.cs | 要確認 |
| AppPostHostGlobalReferenceGuardTests | 3 | App.xaml.cs | 表へ |
| ApplyProfileCleanupOrderGuardTests | 1 | ProfileApplicationService.cs | 型へ |
| AutoRevertBatteryTests | 1 | AutoProfileService.cs | 要確認 |
| BatteryPrimingTests | 3 | DualShock4Device.cs, DeviceConnector.cs | 要確認 |
| BatteryRefreshTests | 4 | CompositeDeviceModel.cs | 要確認 |
| BatteryUnificationTests | 5 | DualSenseDevice.cs, DualShock4Device.cs, SwitchProDevice.cs ほか | 要確認 |
| BindingWindowButtonTableTests | 2 | BindingWindow.xaml | 残す |
| BindingWindowMarkupLoadTests | 1 | App.xaml | 残す |
| BindingWindowModeGuardTests | 6 | BindingWindow.xaml.cs, ProfileEditor.xaml.cs, AssignmentEditorSession.cs | 型へ |
| BuildCommitLogGuardTests | 3 | BuildStampReader.cs, DS4WinWPF.csproj | 残す |
| CodeHealthRatchetTests | 11 | (動的) | 型へ |
| CodeHealthScanner | 0 | (動的) | 要確認 |
| ConnectionGuardTests | 6 | DeviceConnectionSetup.cs, DeviceConnector.cs, DeviceRegistryHooks.cs ほか | 型へ |
| ConnectorGuardTests | 4 | InputReportProcessor.cs, DeviceConnector.cs, ServiceLifecycle.cs | 型へ |
| ContractCleanupGuardTests | 4 | IOutputSlotService.cs | 型へ |
| ControlDefaultAndExtrasTests | 17 | AssignmentEditorSession.cs | 要確認 |
| ControlEditorHostGuardTests | 5 | AssignmentEditor.xaml.cs, ProfileEditor.xaml, ProfileEditor.xaml.cs | 残す |
| ControlMacroTabGuardTests | 2 | AssignmentEditor.xaml, AssignmentEditor.xaml.cs | 残す |
| ControlModeGuardTests | 2 | AssignmentWindowFactory.cs, AssignmentEditor.xaml, ProfileEditor.xaml.cs ほか | 残す |
| ControlProfileTests | 15 | ProfileSelectionCoordinator.cs | 要確認 |
| ControlServiceOscSenderRaceTests | 4 | OscService.cs | 要確認 |
| ControllerReadingsNoClippingTests | 2 | ControllerReadingsControl.xaml, DefaultTheme.xaml | 残す |
| ControlsAnalogMouseTests | 8 | ServiceRegistration.cs, Mapping.cs | 要確認 |
| ControlsExtrasTests | 19 | ServiceRegistration.cs, LightbarMacroCreator.xaml, LightbarMacro.cs ほか | 残す |
| ControlsMouseTests | 9 | ServiceRegistration.cs, FrameOutput.cs | 要確認 |
| ControlsPadTests | 11 | ServiceRegistration.cs, TriggerDispatchEngine.cs | 要確認 |
| CustomSteamFolderTests | 9 | MainWindow.xaml.cs, MainWindowsViewModel.cs | 要確認 |
| DefaultUnboundTypeTests | 10 | AssignmentEditor.xaml, AssignmentEditor.xaml.cs, ProfileEditor.xaml.cs ほか | 残す |
| DisconnectCharacterizationTests | 7 | App.xaml.cs, IProfileRepository.cs, AppSettingsXmlSerializer.cs ほか | 要確認 |
| DisconnectClearsTempProfileTests | 3 | DeviceDisconnectHandler.cs, AppliedProfileRecorder.cs, AutoProfileService.cs ほか | 要確認 |
| DisconnectGuardTests | 3 | DeviceConnector.cs, DeviceDisconnectHandler.cs | 型へ |
| DuplicateAssignsNewIdTests | 4 | MainWindowFactory.cs, DupBox.xaml.cs | 要確認 |
| EmptyMacroTests | 5 | MacroPlayback.cs | 要確認 |
| EnvironmentServiceStaticInitGuardTests | 1 | EnvironmentService.cs | 型へ |
| FileNameMatchesTypeGuardTests | 1 | .Designer.cs, .xaml.cs, AssemblyInfo.cs ほか | 型へ |
| FourTwoGlobalReferenceGuardTests | 13 | MainWindow.xaml.cs, OutputSlotManagerControl.xaml.cs, CurrentOutDeviceViewModel.cs | 表へ |
| FuncRatchetGuardTests | 1 | BackingStore.cs, DefaultActionManager.cs, ProfileApplicationService.cs ほか | 型へ |
| GlobalRemovedGuardTests | 10 | App.xaml.cs, Global.cs | 表へ |
| GoldenFile | 0 | (動的) | 要確認 |
| GroupOneGlobalReferenceGuardTests | 3 | StartupEntryService.cs, About.xaml.cs, LanguageSelectDialog.xaml.cs ほか | 残す |
| GroupThreeGlobalReferenceGuardTests | 5 | MainWindow.xaml.cs, ProfileEditor.xaml.cs, ProfileSelectionCoordinator.cs | 表へ |
| GyroCalibrationNullDeviceGuardTests | 2 | ProfileEditor.xaml.cs | 型へ |
| HidHideGuardTests | 3 | RuntimeFeatureSwitch.cs | 型へ |
| HoverSelectDelayTests | 4 | ProfileEditor.xaml.cs | 要確認 |
| InputReportGuardTests | 4 | InputReportProcessor.cs, RuntimeFeatureSwitch.cs | 型へ |
| IpcCommandTests | 7 | IpcCommandProcessor.cs, MainWindow.Ipc.cs, MainWindow.xaml.cs | 要確認 |
| IpcQueryResultGuardTests | 1 | IpcMemoryMap.cs | 型へ |
| K7b1HidOverlappedIoGuardTests | 2 | HidDevice.cs | 型へ |
| KeepsizeTests | 4 | MainWindow.xaml.cs, ProfileEditor.xaml, ProfileEditor.xaml.cs | 残す |
| KeyButtonOutputFoundationTests | 13 | ButtonOutputAction.cs, KeyOutputAction.cs, Mapping.cs | 要確認 |
| LifecycleGuardTests | 8 | ProcessPriorityOptions.cs, BackendFailureHandler.cs, OutputServicesLifecycle.cs ほか | 型へ |
| LocalizationResourceTests | 3 | Resources.ja.resx, Resources.resx, Strings.ja.resx ほか | 残す |
| MacroGuardTests | 4 | Mapping.cs, MacroStepExecutor.cs | 型へ |
| MainWindowUdpOutContTypeGuardTests | 2 | IpcCommandProcessor.cs | 型へ |
| MainWindowsAndTrayViewModelGlobalReferenceGuardTests | 4 | TrayIconViewModel.cs | 表へ |
| MapCustomGuardTests | 6 | Mapping.cs, SyntheticInputCommitter.cs | 型へ |
| MappingCommitAndCalibrationGlobalReferenceGuardTests | 3 | Mapping.cs, SyntheticInputCommitter.cs | 表へ |
| MappingHelperMethodsGlobalReferenceGuardTests | 1 | Mapping.cs, ControlActivityReader.cs | 表へ |
| MappingMacroPathGlobalReferenceGuardTests | 1 | Mapping.cs, MacroStepExecutor.cs | 表へ |
| ModelDiagramGuardTests | 3 | 01-Structure.md, 02-Extension-Guide.md, 03-Lifecycle-and-DI.md ほか | 型へ |
| MouseGuardTests | 5 | Mapping.cs, ButtonMouseMovementCalculator.cs | 型へ |
| MouseReleaseButtonHoldTests | 1 | TouchpadMouse.cs | 要確認 |
| MouseTapClickHoldTests | 2 | TapClickDetector.cs, TouchpadMouse.cs | 要確認 |
| MouseTapToleranceTests | 1 | TapClickDetector.cs | 要確認 |
| NameFunctionsTests | 4 | ProfileButtonControlLoadSection.cs, BackingStore.cs | 要確認 |
| NamespaceFolderGuardTests | 4 | .g.cs, .g.i.cs, LowpassFilter.cs ほか | 残す |
| OrphanMembersGuardTests | 2 | IAppSettingsService.cs, IProfileSettingsService.cs, ISpecialActionRepository.cs ほか | 表へ |
| OscGuardTests | 3 | RuntimeFeatureSwitch.cs | 型へ |
| OutputDataFlagTests | 3 | DualShock4Device.cs, InputDeviceCore.cs, DeviceConnectionSetup.cs ほか | 残す |
| OutputTypeNamingGuardTests | 2 | ControllerLayouts.cs, Ds4OutputCartridge.cs, OutContTypeInfo.cs ほか | 型へ |
| OutputTypeOwnershipGuardTests | 3 | LegacyProfileXmlSerializer.cs, ProfilePresets.cs, BackingStore.cs ほか | 型へ |
| PickerCloseKeepsTypeTests | 2 | BindingWindow.xaml.cs, AssignmentEditorSession.cs, BindingWindowViewModel.cs | 要確認 |
| ProcessingLogGuardRuleTests | 1 | (動的) | 型へ |
| ProfileEditorBackendViewModelTests | 3 | ProfileEditor.xaml.cs, ProfileSettingsViewModel.cs | 要確認 |
| ProfileEditorCallSitesTests | 4 | ProfileEditor.xaml.cs | 要確認 |
| ProfileEditorGlobalReferenceGuardTests | 1 | ProfileEditor.xaml.cs, ProfileSelectionCoordinator.cs | 表へ |
| ProfileEditorHoverIndexTests | 2 | ProfileEditor.HoverTables.cs, ProfileEditor.xaml.cs | 要確認 |
| ProfileEditorLayoutTests | 11 | GyroSettingsPanel.xaml, ProfileEditor.xaml, ProfileEditor.xaml.cs ほか | 残す |
| ProfileEditorMappingDevTypeGuardTests | 1 | ProfileEditor.xaml.cs | 型へ |
| ProfileEditorMarkupLoadTests | 2 | App.xaml | 残す |
| ProfileEditorTargetDeviceGuardTests | 9 | BindingWindow.xaml.cs, MainWindow.xaml.cs, ProfileEditor.xaml ほか | 残す |
| ProfileIdTests | 15 | App.xaml.cs | 要確認 |
| ProfileLegacyXmlRoundTripTests | 5 | LegacyProfileXmlSerializer.cs, BackingStore.cs | 表へ |
| ProfileLoadFoundationTests | 4 | LegacyProfileXmlSerializer.cs | 要確認 |
| ProfileMappingContractTests | 5 | ProfilePresetLoader.cs | 要確認 |
| PublishBindingsTests | 8 | IpcCommandProcessor.cs, ProfileApplicationService.cs | 要確認 |
| R6PageScrollWheelTests | 2 | App.xaml.cs | 要確認 |
| ReapplyConnectedSlotsOnlyTests | 2 | ProfileSelectionCoordinator.cs | 要確認 |
| ReconnectProfilePerControllerTests | 8 | AppliedProfileRecorder.cs, ConnectionProfileSelector.cs | 要確認 |
| RecordBoxExtraButtonsVisibilityGuardTests | 1 | RecordBox.xaml.cs | 型へ |
| RegistrationSource | 0 | ServiceRegistration.cs | 要確認 |
| ReleaseSourceCentralizationGuardTests | 2 | ReleaseSource.cs, ReleaseSource.cs | 型へ |
| RemovedGyroSwipeNoticeWiringTests | 3 | Resources.ja.resx, ProfileEditor.xaml.cs, ProfileSettingsViewModel.cs | 残す |
| RemovedGyroSwipeWiringTests | 12 | BackingStore.cs, Mapping.cs, GyroSettingsViewModel.cs ほか | 表へ |
| RemovedShiftNoticeWiringTests | 3 | Resources.ja.resx, ProfileEditor.xaml.cs, ProfileSettingsViewModel.cs | 残す |
| RemovedShiftWiringTests | 9 | ProfileMiscLoadSection.cs, DS4ControlSettings.cs, Mapping.cs ほか | 残す |
| RenameFollowUpRemovalTests | 6 | MainWindow.xaml.cs, ProfileEditor.xaml.cs, ProfileSelectionCoordinator.cs | 表へ |
| RuntimeKeyRatchetTests | 1 | App.xaml.cs, IProfileRepository.cs, IProfileSettingsService.cs ほか | 型へ |
| RuntimeSwitchTests | 7 | ProfileApplicationService.cs | 要確認 |
| SaExtrasEditorGuardTests | 2 | BindingWindow.xaml.cs, AssignmentEditorSession.cs | 型へ |
| ServiceLocatorGuardTests | 5 | App.xaml.cs, AppLogger.cs, RuntimeBootstrapper.cs ほか | 型へ |
| SettingsSlotWindowUsageTests | 2 | BindingWindow.xaml.cs, ProfileEditor.xaml.cs, CompositeDeviceModel.cs ほか | 要確認 |
| SettingsViewModelGlobalReferenceGuardTests | 2 | SettingsViewModel.cs | 表へ |
| SharedStaThreadRuleTests | 1 | SharedStaThreadRuleTests.cs, WpfPanelTestRunner.cs, WpfUiDispatcherCollectionSyncTests.cs | 型へ |
| SlotLifecycleTests | 7 | Mapping.cs, DeviceDisconnectHandler.cs, ServiceLifecycle.cs ほか | 要確認 |
| SlotLinkedAutoProfileReferenceIdTests | 7 | AppSettingsXmlSerializer.cs, LinkedProfilesXmlSerializer.cs, AutoProfileHolder.cs | 要確認 |
| SmallFixesGuardTests | 2 | GyroOutputProcessor.cs | 型へ |
| SourceFileLocator | 0 | DS4WinWPF.csproj | 残す |
| SourceText | 0 | (動的) | 要確認 |
| Stage3ProfileActionsSnapshotGuardTests | 1 | Mapping.cs | 型へ |
| StartupBootstrapGuardTests | 4 | App.xaml.cs, AppHost.cs | 型へ |
| StartupOrderGuardTests | 4 | App.xaml.cs | 型へ |
| StateHolderInjectionGuardTests | 4 | SpecialActionRepository.cs | 型へ |
| StatusMessageVisibilityTests | 2 | MainWindow.xaml, MainWindow.xaml.cs | 残す |
| SteeringW2ControllerConfigsGuardTests | 2 | ControllerConfigsXmlSerializer.cs | 型へ |
| SteeringW3OutputDeviceGuardTests | 2 | ControllerState.cs, DS4OutDeviceBasic.cs, DS4OutDeviceExt.cs ほか | 型へ |
| StickDeadZoneBindingGuardTests | 1 | AxialStickUserControl.xaml, StickSideSettingsView.xaml | 残す |
| SyntheticInputStateGuardTests | 4 | SpecialActionsXmlSerializer.cs, BackingStore.cs, ActionsLoadedRuntimeReset.cs | 型へ |
| ThemeResourceKeyGuardTests | 2 | DarkTheme.xaml, DefaultTheme.xaml | 残す |
| ThemeTokenRuleTests | 6 | BindingWindow.xaml, DarkTheme.xaml, DefaultTheme.xaml | 残す |
| TriggerPanelGuardTests | 3 | AssignmentEditor.xaml, AssignmentEditor.xaml.cs, AssignmentEditorSession.cs | 残す |
| UdpMotionGuardTests | 2 | RuntimeFeatureSwitch.cs | 型へ |
| UnifiedCodingPracticeRuleTests | 3 | (動的) | 型へ |
| UserLogTextResourceTests | 4 | (動的) | 要確認 |
| ValidationTableTests | 15 | AssignmentEditor.xaml, AssignmentEditorSession.cs | 残す |
| ViewCommandBindingRatchetTests | 2 | (動的) | 型へ |
| ViewModelChangeNotificationTests | 5 | MainWindow.xaml.cs | 要確認 |
| XboxGameDvrRemovalTests | 4 | SpecialAction.cs, Mapping.cs | 表へ |

## 件数

- 型へ: 45
- 残す: 28
- 表へ: 15
- 要確認: 46

## 別に扱うもの
- XML コメントの Phase 名（439 ファイル、1,309 か所）: 目的を述べる部分は残し、Phase 名の参照だけを外す（RV8-1〜4 と同じ道具）。テストのクラス名にある `Phase<数字>`（例: `Phase9EOscServiceDifferentialTests`）は、機能名へ改名し、参照するコメントも同時に直す。
- 改名の対応表は `Phase15-0-Test-Rename-Map.md` の形式で、この文書へ追記する。