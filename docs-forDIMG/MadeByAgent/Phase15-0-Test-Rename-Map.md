`# Phase15-0-5: テストの改名の対応表（旧名 → 新名）
`
`作成日: 2026-11-08
`根拠: `Phase15-0-Plan.md` 15-0-5（レビュー R-16）、規約 §4「テストの命名」
`
`過去の計画書・完了報告がテストのクラス名を根拠に引いているため、旧名から新名を引けるようにする。
`ファイル名（\`git mv\` だけのコミット）→ クラス名（識別子の書き換えのコミット）の順で変えた（1 ファイル = 1 型）。中身（入力・期待値）は変えていない。
`
`**命名の規則**: 旧名の先頭の Phase／Step／W／I の記号（例: `Phase10Step4dW3`）を除いた、機能の名前を新名とする。除いた結果が重複する、または機能を表さないもの（`CharacterizationTests`、`ContractExtensionTests`、`Tests` など）だけ、テストの冒頭の説明から機能名を付けた（下の表で `*` を付けた行）。
`
`| 旧名（クラス＝ファイル） | 新名 |
`|---|---|
`| `Phase10ControlsSaParityAuditTests` | `ControlsSaParityAuditTests` |
`| `Phase10Step1W1MacroPlaybackOptionsTests` | `MacroPlaybackOptionsTests` |
`| `Phase10Step1W2BindingWindowMacroScanCodeTests` | `BindingWindowMacroScanCodeTests` |
`| `Phase10Step1W2RecordBoxPlaybackOptionsTests` | `RecordBoxPlaybackOptionsTests` |
`| `Phase10Step1W3MacroViewModelPlaybackOptionsTests` | `MacroViewModelPlaybackOptionsTests` |
`| `Phase10Step1W4KeyActionScanCodePersistenceTests` | `KeyActionScanCodePersistenceTests` |
`| `Phase10Step1W4PressKeyScanCodeTests` | `PressKeyScanCodeTests` |
`| `Phase10Step2W0ButtonToggleReproTests` | `ButtonToggleReproTests` |
`| `Phase10Step2W0PriorityAndSuppressionCharacterizationTests` | `PriorityAndSuppressionCharacterizationTests` |
`| `Phase10Step2W1ActionTypeCatalogTests` | `ActionTypeCatalogTests` |
`| `Phase10Step2W1ActionTypeEditorSetTests` | `ActionTypeEditorSetTests` |
`| `Phase10Step2W1SpecialActEditorViewModelTests` | `SpecialActEditorViewModelTests` |
`| `Phase10Step2W1SpecialActionTriggerSourceTests` | `SpecialActionTriggerSourceTests` |
`| `Phase10Step2W2TriggerCatalogTests` | `TriggerCatalogTests` |
`| `Phase10Step2W2TriggerOptionsTests` | `TriggerOptionsTests` |
`| `Phase10Step2W2TriggerPanelGuardTests` | `TriggerPanelGuardTests` |
`| `Phase10Step2W3ControlActionEditorViewModelTests` | `ControlActionEditorViewModelTests` |
`| `Phase10Step2W3ControlEditTargetTests` | `ControlEditTargetTests` |
`| `Phase10Step2W3ControlModeGuardTests` | `ControlModeGuardTests` |
`| `Phase10Step2W3ControlTriggerSourceTests` | `ControlTriggerSourceTests` |
`| `Phase10Step2W4ControlSettingsEditTargetTests` | `ControlSettingsEditTargetTests` |
`| `Phase10Step3W0BindingWindowWriteTests` | `BindingWindowWriteTests` |
`| `Phase10Step3W0OutBindingExtrasTests` | `OutBindingExtrasTests` |
`| `Phase10Step3W0ProfileEditorCallSitesTests` | `ProfileEditorCallSitesTests` |
`| `Phase10Step3W1BindingWindowModeGuardTests` | `BindingWindowModeGuardTests` |
`| `Phase10Step3W2ControlDefaultAndExtrasTests` | `ControlDefaultAndExtrasTests` |
`| `Phase10Step3W3ControlMacroTabGuardTests` | `ControlMacroTabGuardTests` |
`| `Phase10Step3W3EmptyMacroTests` | `EmptyMacroTests` |
`| `Phase10Step3W4ControlEditorHostGuardTests` | `ControlEditorHostGuardTests` |
`| `Phase10Step3W4DefaultUnboundTypeTests` | `DefaultUnboundTypeTests` |
`| `Phase10Step3bW0ProfileEditorLayoutTests` | `ProfileEditorLayoutTests` |
`| `Phase10Step4W0ControlProfileXmlFormatTests` | `ControlProfileXmlFormatTests` |
`| `Phase10Step4W0SaBindingTableTests` | `SaBindingTableTests` |
`| `Phase10Step4W1ControlSpecialItemTests` | `ControlSpecialItemTests` |
`| `Phase10Step4W2SpecialActionOutputFactoryTests` | `SpecialActionOutputFactoryTests` |
`| `Phase10Step4W3ControlMacroOptionsTests` | `ControlMacroOptionsTests` |
`| `Phase10Step4W4ControlSpecialTests` | `ControlSpecialTests` |
`| `Phase10Step4W5ControlProgramTests` | `ControlProgramTests` |
`| `Phase10Step4W6ControlProfileTests` | `ControlProfileTests` |
`| `Phase10Step4cW0InputSetTests` | `TriggerInputSetDifferenceTests` * |
`| `Phase10Step4cW0JudgementTableTests` | `JudgementTableTests` |
`| `Phase10Step4cW0LogTableTests` | `LogTableTests` |
`| `Phase10Step4cW0PolicyTableTests` | `PolicyTableTests` |
`| `Phase10Step4cW0ValidationTableTests` | `ValidationTableTests` |
`| `Phase10Step4cW1FreshPressGuardTableTests` | `FreshPressGuardTableTests` |
`| `Phase10Step4cW2EdgeLogTests` | `EdgeLogTests` |
`| `Phase10Step4cW4JudgementTests` | `JudgementTests` |
`| `Phase10Step4cW5InputSetTests` | `TouchStartedEndedInputTests` * |
`| `Phase10Step4dW0NameKeyTests` | `NameKeyTests` |
`| `Phase10Step4dW0RuntimeKeyRatchetTests` | `RuntimeKeyRatchetTests` |
`| `Phase10Step4dW1ProfileIdTests` | `ProfileIdTests` |
`| `Phase10Step4dW2ActionIdTests` | `ActionIdTests` |
`| `Phase10Step4dW3ReferenceIdTests` | `ActionAndProfileReferenceIdTests` * |
`| `Phase10Step4dW4ReferenceIdTests` | `SlotLinkedAutoProfileReferenceIdTests` * |
`| `Phase10Step4dW5aIdentityIndexTests` | `IdentityIndexTests` |
`| `Phase10Step4dW5bProfileSlotNamesTests` | `ProfileSlotNamesTests` |
`| `Phase10Step4dW5cRuntimeSwitchTests` | `RuntimeSwitchTests` |
`| `Phase10Step4dW5dTests` | `AutoAndLinkedProfileRuntimeIdTests` * |
`| `Phase10Step4dW7FollowUpTests` | `RenameFollowUpRemovalTests` * |
`| `Phase10Step5SaAnalogOutputTests` | `SaAnalogOutputTests` |
`| `Phase10Step5SaAnalogStrengthStorageTests` | `SaAnalogStrengthStorageTests` |
`| `Phase10Step6I1AssignmentDefinitionTests` | `AssignmentDefinitionTests` |
`| `Phase10Step6I2SpecialActionItemMapperTests` | `SpecialActionItemMapperTests` |
`| `Phase10Step6I3AssignmentReaderTests` | `AssignmentReaderTests` |
`| `Phase10Step6I4aOutputNatureTableTests` | `OutputNatureTableTests` |
`| `Phase10Step6I4bControlsKeyMacroDefinitionTests` | `ControlsKeyMacroDefinitionTests` |
`| `Phase10Step6I4cAssignmentBindingBuilderTests` | `AssignmentBindingBuilderTests` |
`| `Phase10Step6I5SaExtrasEngineTests` | `SaExtrasEngineTests` |
`| `Phase10Step6I5aSaExtrasStorageTests` | `SaExtrasStorageTests` |
`| `Phase10Step6I5bSaExtrasBindingTests` | `SaExtrasBindingTests` |
`| `Phase10Step6I5cSaExtrasEditorGuardTests` | `SaExtrasEditorGuardTests` |
`| `Phase10Step6I6bControlsSpecialTypeTableTests` | `ControlsSpecialTypeTableTests` |
`| `Phase10Step6W7ControlBatteryTests` | `ControlBatteryTests` |
`| `Phase10Step6W8ControlMultiActionTests` | `ControlMultiActionTests` |
`| `Phase11BatteryUnificationTests` | `BatteryUnificationTests` |
`| `Phase11W3KeepsizeTests` | `KeepsizeTests` |
`| `Phase11W4Ds4OutputCorrectionTests` | `Ds4OutputCorrectionTests` |
`| `Phase11W4SaveApplyWiringGuardTests` | `SaveApplyWiringGuardTests` |
`| `Phase11W5CheckEveryTests` | `CheckEveryTests` |
`| `Phase11W5CustomSteamFolderTests` | `CustomSteamFolderTests` |
`| `Phase11W5StatusMessageVisibilityTests` | `StatusMessageVisibilityTests` |
`| `Phase11W6AutoRevertBatteryTests` | `AutoRevertBatteryTests` |
`| `Phase11W6BatteryPrimingTests` | `BatteryPrimingTests` |
`| `Phase11W6BatteryRefreshTests` | `BatteryRefreshTests` |
`| `Phase11W6DisconnectClearsTempProfileTests` | `DisconnectClearsTempProfileTests` |
`| `Phase11W6DuplicateAssignsNewIdTests` | `DuplicateAssignsNewIdTests` |
`| `Phase11W6HoverSelectDelayTests` | `HoverSelectDelayTests` |
`| `Phase11W6LinkedProfileComboDeleteTests` | `LinkedProfileComboDeleteTests` |
`| `Phase11W6LoadProfileNoTargetTests` | `LoadProfileNoTargetTests` |
`| `Phase11W6MacroButtonNamesTests` | `MacroButtonNamesTests` |
`| `Phase11W6OutDevTypeTempFollowsEditorTests` | `OutDevTypeTempFollowsEditorTests` |
`| `Phase11W6PickerCloseKeepsTypeTests` | `PickerCloseKeepsTypeTests` |
`| `Phase11W6ReapplyConnectedSlotsOnlyTests` | `ReapplyConnectedSlotsOnlyTests` |
`| `Phase11W6cOutputDataFlagTests` | `OutputDataFlagTests` |
`| `Phase12MacroRecordListEditTests` | `MacroRecordListEditTests` |
`| `Phase12Step2W2b1WindowTargetMatcherDifferentialTests` | `WindowTargetMatcherDifferentialTests` |
`| `Phase12Step2W2b2WindowFocuserTests` | `WindowFocuserTests` |
`| `Phase12Step2W2cFocusWindowPersistenceTests` | `FocusWindowPersistenceTests` |
`| `Phase12Step2W2dFocusWindowActionTests` | `FocusWindowActionTests` |
`| `Phase12Step2W2eFocusWindowEditorTests` | `FocusWindowEditorTests` |
`| `Phase12Step2W2hWindowTitleMatchModeTests` | `WindowTitleMatchModeTests` |
`| `Phase12Step2W2iWindowTargetPickerTests` | `WindowTargetPickerTests` |
`| `Phase12Step2W2jCenterCursorTests` | `CenterCursorTests` |
`| `Phase12Step3W3bMacroPointerMoveTests` | `MacroPointerMoveTests` |
`| `Phase12Step3W3cMacroPointerMoveExecutionTests` | `MacroPointerMoveExecutionTests` |
`| `Phase12Step3W3dMacroPointerMoveEditTests` | `MacroPointerMoveEditTests` |
`| `Phase12Step3W3eMacroPointerRecorderTests` | `MacroPointerRecorderTests` |
`| `Phase13FuncRatchetGuardTests` | `FuncRatchetGuardTests` |
`| `Phase13Step10ForwardRemovalGuardTests` | `ControlServiceForwardRemovalGuardTests` * |
`| `Phase13Step11IpcCommandProcessorTests` | `IpcCommandProcessorTests` |
`| `Phase13Step2ControlServiceConstructorGuardTests` | `ControlServiceConstructorGuardTests` |
`| `Phase13Step3HostRemovalGuardTests` | `HostRemovalGuardTests` |
`| `Phase13Step4ActionManagerRemovalGuardTests` | `ActionManagerRemovalGuardTests` |
`| `Phase13Step5ControlServiceSlotStateGuardTests` | `ControlServiceSlotStateGuardTests` |
`| `Phase13Step5SlotStateHotPathAllocationTests` | `SlotStateHotPathAllocationTests` |
`| `Phase13Step5SlotStateTimingBenchmark` | `SlotStateTimingBenchmark` |
`| `Phase13Step5bOnReportCharacterizationTests` | `OnReportCharacterizationTests` |
`| `Phase13Step5bOnReportTimingBenchmark` | `OnReportTimingBenchmark` |
`| `Phase13Step6CharacterizationTests` | `LazyControlServiceReplacementCharacterizationTests` * |
`| `Phase13Step6ControlExtrasPortRumbleTests` | `ControlExtrasPortRumbleTests` |
`| `Phase13Step6ProfileLoadTargetTests` | `ProfileLoadTargetTests` |
`| `Phase13Step6ResolutionGraphProbeTests` | `ControlServiceResolutionGraphProbeTests` * |
`| `Phase13Step7CharacterizationTests` | `OutputDeviceTypeStateCharacterizationTests` * |
`| `Phase13Step8ResolutionGraphProbeTests` | `LazyDependencyResolutionGraphProbeTests` * |
`| `Phase13Step9RunningForwardRemovalGuardTests` | `ServiceLifecycleRunningForwardRemovalGuardTests` * |
`| `Phase13bStep1LogApiTests` | `LogApiTests` |
`| `Phase14NamespaceFolderGuardTests` | `NamespaceFolderGuardTests` |
`| `Phase3ServiceRegistrationTests` | `ServiceRegistrationTests` |
`| `Phase6Step10GroupOneContractTests` | `StartupEnvironmentContractTests` * |
`| `Phase6Step10GroupOneViewModelTests` | `AboutUpdaterLogViewModelTests` * |
`| `Phase6Step10GroupThreeBContractTests` | `ActionReferenceReplaceContractTests` * |
`| `Phase6Step10GroupThreeContractTests` | `SpecialActionContractTests` * |
`| `Phase6Step10GroupTwoContractTests` | `ProfileMappingContractTests` * |
`| `Phase6Step3ContractExtensionTests` | `MappingArgumentContractTests` * |
`| `Phase6Step7ContractExtensionTests` | `AppPostHostContractTests` * |
`| `Phase6Step8ContractExtensionTests` | `ProfileEditorContractTests` * |
`| `Phase6Step9ContractExtensionTests` | `MainViewModelContractTests` * |
`| `Phase7Step10aW2ControllerLimitsGuardTests` | `ControllerLimitsGuardTests` |
`| `Phase7Step10aW3ResourcePrefixGuardTests` | `ResourcePrefixGuardTests` |
`| `Phase7Step10bW1ControlTablesGuardTests` | `ControlTablesGuardTests` |
`| `Phase7Step10bW2TrayIconAndColorGuardTests` | `TrayIconAndColorGuardTests` |
`| `Phase7Step10bW3ContractCleanupGuardTests` | `ContractCleanupGuardTests` |
`| `Phase7Step10bW4UsingMaxControllersGuardTests` | `UsingMaxControllersGuardTests` |
`| `Phase7Step10cGoldenPersistenceTests` | `GoldenPersistenceTests` |
`| `Phase7Step10cW2SerializersGuardTests` | `SerializersGuardTests` |
`| `Phase7Step10cW3AppSettingsSerializerGuardTests` | `AppSettingsSerializerGuardTests` |
`| `Phase7Step10cW4LegacyProfileLoadGuardTests` | `LegacyProfileLoadGuardTests` |
`| `Phase7Step10cW5LegacyProfileSaveGuardTests` | `LegacyProfileSaveGuardTests` |
`| `Phase7Step10cW6ProfilePresetsGuardTests` | `ProfilePresetsGuardTests` |
`| `Phase7Step10dW2StaticWindowsGuardTests` | `StaticWindowsGuardTests` |
`| `Phase7Step10dW3DeviceLayerSettingsGuardTests` | `DeviceLayerSettingsGuardTests` |
`| `Phase7Step10dW4AppLayerStaticsGuardTests` | `AppLayerStaticsGuardTests` |
`| `Phase7Step10dW5GlobalRemovedGuardTests` | `GlobalRemovedGuardTests` |
`| `Phase7Step10eW1SlotLimitServicesGuardTests` | `SlotLimitServicesGuardTests` |
`| `Phase7Step10eW2SlotLimitUiGuardTests` | `SlotLimitUiGuardTests` |
`| `Phase7Step10eW3AppHostGuardTests` | `AppHostNoImplicitHostGuardTests` * |
`| `Phase7Step10eW4OrphanMembersGuardTests` | `OrphanMembersGuardTests` |
`| `Phase7Step1DeadMethodGuardTests` | `DeadMethodGuardTests` |
`| `Phase7Step2RemovedContractMembersGuardTests` | `RemovedContractMembersGuardTests` |
`| `Phase7Step3cApplyProfileCharacterizationTests` | `ApplyProfileCharacterizationTests` |
`| `Phase7Step4AppHostGuardTests` | `AppHostStartupShutdownGuardTests` * |
`| `Phase7Step4EnvironmentProbesGuardTests` | `EnvironmentProbesGuardTests` |
`| `Phase7Step4PathServiceGuardTests` | `PathServiceGuardTests` |
`| `Phase7Step4StartupBootstrapGuardTests` | `StartupBootstrapGuardTests` |
`| `Phase7Step5Ds3GyroTriggerTests` | `Ds3GyroTriggerTests` |
`| `Phase7Step5RootHubGuardTests` | `RootHubGuardTests` |
`| `Phase7Step5ServiceLocatorGuardTests` | `ServiceLocatorGuardTests` |
`| `Phase7Step6OutputTypeOwnershipGuardTests` | `OutputTypeOwnershipGuardTests` |
`| `Phase7Step7CommitCharacterizationTests` | `CommitCharacterizationTests` |
`| `Phase7Step7MacroRuntimeStateGuardTests` | `MacroRuntimeStateGuardTests` |
`| `Phase7Step7MapCustomCharacterizationTests` | `MapCustomCharacterizationTests` |
`| `Phase7Step7StickMotionStateGuardTests` | `StickMotionStateGuardTests` |
`| `Phase7Step7SyntheticInputStateGuardTests` | `SyntheticInputStateGuardTests` |
`| `Phase7Step8W1StateHolderInjectionGuardTests` | `StateHolderInjectionGuardTests` |
`| `Phase7Step8W2MappingInstanceGuardTests` | `MappingInstanceGuardTests` |
`| `Phase7Step8W3OutputKbmOwnershipGuardTests` | `OutputKbmOwnershipGuardTests` |
`| `Phase7Step8W4NoSharedGuardTests` | `NoSharedGuardTests` |
`| `Phase7Step9W1CompatAppHostRemovedGuardTests` | `CompatAppHostRemovedGuardTests` |
`| `Phase7Step9W2NoGlobalInMappingGuardTests` | `NoGlobalInMappingGuardTests` |
`| `Phase7bTriggerDeadZoneRoundingTests` | `TriggerDeadZoneRoundingTests` |
`| `Phase7bW1ProfileUntriggerPreservationTests` | `ProfileUntriggerPreservationTests` |
`| `Phase7bW2W3SmallFixesGuardTests` | `SmallFixesGuardTests` |
`| `Phase7cW1Stage3ProfileActionsSnapshotGuardTests` | `Stage3ProfileActionsSnapshotGuardTests` |
`| `Phase7cW2SpecialActionTriggerCharacterizationTests` | `SpecialActionTriggerCharacterizationTests` |
`| `Phase8FixMacroRepeatUnifyPersistenceTests` | `MacroRepeatUnifyPersistenceTests` |
`| `Phase8Step1ApplyProfileCleanupOrderGuardTests` | `ApplyProfileCleanupOrderGuardTests` |
`| `Phase8Step1MacroBranchCharacterizationTests` | `MacroBranchCharacterizationTests` |
`| `Phase8Step1ProfileSwitchCharacterizationTests` | `ProfileSwitchCharacterizationTests` |
`| `Phase8Step1SlotLimitTests` | `SlotLimitTests` |
`| `Phase8Step3ContractTests` | `TriggerBindingContractTests` * |
`| `Phase8Step3PerformanceTests` | `TriggerBindingPerformanceTests` * |
`| `Phase8Step4MapCustomBenchmarkTests` | `MapCustomBenchmarkTests` |
`| `Phase8Step4W1ContractTests` | `TriggerRegistryContractTests` * |
`| `Phase8Step4W2FrameOutputTests` | `FrameOutputTests` * |
`| `Phase8Step4W3EngineTests` | `TriggerEngineTests` * |
`| `Phase8Step4W4WiringTests` | `TriggerEngineWiringTests` * |
`| `Phase8Step4bProfileGoldenGapTests` | `ProfileGoldenGapTests` |
`| `Phase8Step4bW11LengthGuardTests` | `ProfileLoadSaveLengthGuardTests` * |
`| `Phase8Step4bW2LoadFoundationTests` | `ProfileLoadFoundationTests` * |
`| `Phase8Step4bW9NameFunctionsTests` | `NameFunctionsTests` |
`| `Phase8Step5W1BuilderTests` | `ActionBindingBuilderTests` * |
`| `Phase8Step5W3PublishBindingsTests` | `PublishBindingsTests` |
`| `Phase8Step6W1DisconnectCharacterizationTests` | `DisconnectCharacterizationTests` |
`| `Phase8Step6W3SlotLifecycleTests` | `SlotLifecycleTests` |
`| `Phase8Step72W1KeyDualPathTests` | `KeyDualPathTests` |
`| `Phase8Step72W2KeyLogTests` | `KeyLogTests` |
`| `Phase8Step72W3KeySwitchTests` | `KeySwitchTests` |
`| `Phase8Step73W1MacroDualPathTests` | `MacroDualPathTests` |
`| `Phase8Step73W2MacroLogTests` | `MacroLogTests` |
`| `Phase8Step73W3MacroSwitchTests` | `MacroSwitchTests` |
`| `Phase8Step74W1ProgramDualPathTests` | `ProgramDualPathTests` |
`| `Phase8Step74W2ProgramLogTests` | `ProgramLogTests` |
`| `Phase8Step74W3ProgramSwitchTests` | `ProgramSwitchTests` |
`| `Phase8Step75aW1ProfileSwitchDualPathTests` | `ProfileSwitchDualPathTests` |
`| `Phase8Step75aW2ProfileLogTests` | `ProfileLogTests` |
`| `Phase8Step75aW3ProfileSwitchTests` | `ProfileSwitchTests` |
`| `Phase8Step75bW1ProfileUntriggerDualPathTests` | `ProfileUntriggerDualPathTests` |
`| `Phase8Step75bW2ProfileUntriggerEngineTests` | `ProfileUntriggerEngineTests` |
`| `Phase8Step75bW3ProfileUntriggerSwitchTests` | `ProfileUntriggerSwitchTests` |
`| `Phase8Step76aGyroCalibrateTests` | `GyroCalibrateTests` |
`| `Phase8Step76bDisconnectBTTests` | `DisconnectBTTests` |
`| `Phase8Step76cBatteryCheckTests` | `BatteryCheckTests` |
`| `Phase8Step77aXboxGameDvrRemovalTests` | `XboxGameDvrRemovalTests` |
`| `Phase8Step77bW1MultiActionDualPathTests` | `MultiActionDualPathTests` |
`| `Phase8Step77bW2MultiTapTests` | `MultiTapTests` |
`| `Phase8Step78aFoundationTests` | `KeyButtonOutputFoundationTests` * |
`| `Phase8Step78bW1ControlsKeyCharacterizationTests` | `ControlsKeyCharacterizationTests` |
`| `Phase8Step78bW2ControlsKeyTests` | `ControlsKeyTests` |
`| `Phase8Step78cW1ControlsMacroCharacterizationTests` | `ControlsMacroCharacterizationTests` |
`| `Phase8Step78cW2ControlsMacroTests` | `ControlsMacroTests` |
`| `Phase8Step78dW1RemovedShiftSpecTests` | `RemovedShiftSpecTests` |
`| `Phase8Step78dW2RemovedShiftWiringTests` | `RemovedShiftWiringTests` |
`| `Phase8Step78dW3RemovedShiftNoticeWiringTests` | `RemovedShiftNoticeWiringTests` |
`| `Phase8Step78eW1ControlsExtrasCharacterizationTests` | `ControlsExtrasCharacterizationTests` |
`| `Phase8Step78eW2ControlsExtrasTests` | `ControlsExtrasTests` |
`| `Phase8Step78f2W1ControlsMouseCharacterizationTests` | `ControlsMouseCharacterizationTests` |
`| `Phase8Step78f2W2ControlsMouseTests` | `ControlsMouseTests` |
`| `Phase8Step78f3W1ControlsAnalogMouseCharacterizationTests` | `ControlsAnalogMouseCharacterizationTests` |
`| `Phase8Step78f3W2ControlsAnalogMouseTests` | `ControlsAnalogMouseTests` |
`| `Phase8Step78fW1ControlsPadCharacterizationTests` | `ControlsPadCharacterizationTests` |
`| `Phase8Step78fW2ControlsPadTests` | `ControlsPadTests` |
`| `Phase8Step7AltTabRemovalGuardTests` | `AltTabRemovalGuardTests` |
`| `Phase8Step7BuildCommitLogGuardTests` | `BuildCommitLogGuardTests` |
`| `Phase8Step7W1ButtonDualPathTests` | `ButtonDualPathTests` |
`| `Phase8Step7W2ApplyAndOrderTests` | `ApplyAndOrderTests` |
`| `Phase8Step7W2ButtonTraceLogTests` | `ButtonTraceLogTests` |
`| `Phase8Step8W1LegacyFunctionGoldenTests` | `LegacyFunctionGoldenTests` |
`| `Phase8Step8W1RemovedGyroSwipeSpecTests` | `RemovedGyroSwipeSpecTests` |
`| `Phase8Step8W2RemovedGyroSwipeWiringTests` | `RemovedGyroSwipeWiringTests` |
`| `Phase8Step8W3RemovedGyroSwipeNoticeWiringTests` | `RemovedGyroSwipeNoticeWiringTests` |
`| `Phase9A1SetCurveAndDeadzoneGoldenTests` | `SetCurveAndDeadzoneGoldenTests` |
`| `Phase9A2AntiSnapbackDifferentialTests` | `AntiSnapbackDifferentialTests` |
`| `Phase9A2FlickStickGoldenTests` | `FlickStickGoldenTests` |
`| `Phase9A2GyroControlsGoldenTests` | `GyroControlsGoldenTests` |
`| `Phase9AssignmentDescriberTests` | `AssignmentDescriberTests` |
`| `Phase9BButtonMouseMovementDifferentialTests` | `ButtonMouseMovementDifferentialTests` |
`| `Phase9BControlActivityReaderTests` | `ControlActivityReaderTests` |
`| `Phase9BGyroMouseStickBlenderTests` | `GyroMouseStickBlenderTests` |
`| `Phase9BMouseGuardTests` | `MouseGuardTests` |
`| `Phase9BMouseMotionQuantizerTests` | `MouseMotionQuantizerTests` |
`| `Phase9CMacroGuardTests` | `MacroGuardTests` |
`| `Phase9CMacroPlaybackTests` | `MacroPlaybackTests` |
`| `Phase9CMacroRuntimeStateTests` | `MacroRuntimeStateTests` |
`| `Phase9CMacroStepExecutorDifferentialTests` | `MacroStepExecutorDifferentialTests` |
`| `Phase9ControlsPadToggleTests` | `ControlsPadToggleTests` |
`| `Phase9E6ConnectionGuardTests` | `ConnectionGuardTests` |
`| `Phase9E6ConnectionProfileSelectorTests` | `ConnectionProfileSelectorTests` |
`| `Phase9E6ConnectorGuardTests` | `ConnectorGuardTests` |
`| `Phase9E6DeviceModuleTests` | `DeviceModuleTests` |
`| `Phase9E6DisconnectGuardTests` | `DisconnectGuardTests` |
`| `Phase9E6JoyConLinkerTests` | `JoyConLinkerTests` |
`| `Phase9E6LifecycleGuardTests` | `LifecycleGuardTests` |
`| `Phase9EHidHideGuardTests` | `HidHideGuardTests` |
`| `Phase9EHidHideServiceDifferentialTests` | `HidHideServiceDifferentialTests` |
`| `Phase9EInputReportGuardTests` | `InputReportGuardTests` |
`| `Phase9EInputReportPartsTests` | `InputReportPartsTests` |
`| `Phase9EOscGuardTests` | `OscGuardTests` |
`| `Phase9EOscServiceDifferentialTests` | `OscServiceDifferentialTests` |
`| `Phase9EUdpMotionGuardTests` | `UdpMotionGuardTests` |
`| `Phase9EUdpMotionServiceTests` | `UdpMotionServiceTests` |
`| `Phase9EVirtualPadGuardTests` | `VirtualPadGuardTests` |
`| `Phase9EVirtualPadServiceTests` | `VirtualPadServiceTests` |
`| `Phase9F1StartupOrderGuardTests` | `StartupOrderGuardTests` |
`| `Phase9F1StartupPartsTests` | `StartupPartsTests` |
`| `Phase9F2IpcCommandTests` | `IpcCommandTests` |
`| `Phase9F2IpcQueryResultGuardTests` | `IpcQueryResultGuardTests` |
`| `Phase9HotPathTimingBenchmark` | `HotPathTimingBenchmark` |
`| `Phase9MapCustomCommitterDifferentialTests` | `MapCustomCommitterDifferentialTests` |
`| `Phase9MapCustomGuardTests` | `MapCustomGuardTests` |
`| `Phase9MapCustomTwoStageCharacterizationTests` | `MapCustomTwoStageCharacterizationTests` |
`| `Phase9MapCustomTwoStageDifferentialTests` | `MapCustomTwoStageDifferentialTests` |
`| `Phase9MapCustomTwoStageKeyReleaseTests` | `MapCustomTwoStageKeyReleaseTests` |
`| `Phase9MappingListDisplayOrderTests` | `MappingListDisplayOrderTests` |
`| `Phase9SteeringW1LegacyAssignmentTests` | `SteeringW1LegacyAssignmentTests` |
`| `Phase9SteeringW2ControllerConfigsGuardTests` | `SteeringW2ControllerConfigsGuardTests` |
`| `Phase9SteeringW3OutputDeviceGuardTests` | `SteeringW3OutputDeviceGuardTests` |
`| `Phase9SteeringW4LegacyProfileTests` | `SteeringW4LegacyProfileTests` |
`| `Step10FourOneGlobalReferenceGuardTests` | `FourOneGlobalReferenceGuardTests` |
`| `Step10FourTwoGlobalReferenceGuardTests` | `FourTwoGlobalReferenceGuardTests` |
`| `Step10GroupOneGlobalReferenceGuardTests` | `GroupOneGlobalReferenceGuardTests` |
`| `Step10GroupThreeGlobalReferenceGuardTests` | `GroupThreeGlobalReferenceGuardTests` |
`| `Step10GroupTwoGlobalReferenceGuardTests` | `GroupTwoGlobalReferenceGuardTests` |
`| `Step12DeadShimRemovalGuardTests` | `DeadShimRemovalGuardTests` |
`
`合計 303 件（`*` は手で名前を付けた 33 件）。
