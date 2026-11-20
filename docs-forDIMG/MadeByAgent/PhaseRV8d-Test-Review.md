# PhaseRV8d: ソース文字列を読むテストの 1 件ずつの精査

対象: `SourceFileLocator`・`SourceText`・`File.ReadAllText(*.cs)` を使うテスト（`Measure-CodeReview.py` が数える 129 ファイル）の中の、ソースを読む Fact 207 件。

## 結果

| 区分 | 件数 | 扱い |
|---|---|---|
| C | 37 | 今回、一部を型・リフレクション・表へ置き換えた（残りは下の別の区分） |
| K1 | 52 | 呼び出し順・安全順序の固定。順序は実行時の振る舞いで確かめにくく、ソースの並びが唯一の手がかり（ドライバーの破棄順・切断の後始末・起動の順序など） |
| K2 | 35 | 画面（code-behind／XAML）の配線・見た目の固定。WPF の画面は単体テストで開けず、挙動のテストを足すには画面ごとの土台が要る |
| K3 | 30 | リポジトリ全体の規則の走査（ラチェット・命名・テーマ・文言・XAML の直書きなど）。文字列の走査そのものが規則の実体で、型の検査にならない |
| K4 | 47 | 配線の文字列の固定（「この呼び出しをしている」）。偽物を差した挙動のテストへ書き換えられるが、新しい挙動のテストを書く作業になる（次の Phase の候補） |
| K5 | 6 | 書式・行数・TODO コメントそのものを規約として固定（`copilot-instructions` 2.4・原則4 に基づく） |

合計 207 件。区分 C は、そのファイル内のソースを読む Fact の件数（実際に置き換えた Fact は下記）。

## 今回、置き換えたもの（負の確認済み）

- `RemovedApiRuleTests.RemovedMembers` の表へ統合: `ProfileEditor.GyroOutModeCombo_SelectionChanged`・`FrictionUD_ValueChanged`・`DeviceNum`、`AssignmentEditor` の `ControlTriggerCheckBox_Click`・`ControlUnloadTriggerCheckBox_Click`・`triggerBoxes`・`unloadTriggerBoxes`・`IndexZeroMeansDefault`、`ActionEditorViewModel.IndexZeroMeansDefault`、`MainWindow.MainTabCon_SelectionChanged`、`AppHost._disposed`。元の `DoesNotContain` の行を削除し、`TriggerPanelGuardTests.TheCodeBehind_NoLongerMatchesOrClicksTheCheckBoxes` は表とコンパイル（XAML の `x:Name` のフィールド）で担保されるため削除した。
- `BindingWindowModeGuardTests.TheModes_AreSpecialAction_AndControl_Only`: `Enum.GetNames(typeof(BindingWindow.ExposeMode))` が `SpecialAction`・`Control` の順であることを値で確かめる形へ。
- `BatteryUnificationTests`: 基底 `InputDeviceCore` のイベント `BatteryChanged`・`ChargingChanged` が仮想でなく、派生型が再宣言しないことをリフレクションで確かめる形へ（派生型からの `?.Invoke` はコンパイルが禁じるため、その文字列検査は削除）。
- `LifecycleGuardTests`: プロセス優先度の表 `ProcessPriorityOptions.Classes` の並び（保存済みの添字）を、値で確かめる形へ。
- `ProfileLoadFoundationTests.Sections_AreInternalSealedAndNotRegisteredInTheContainer`: 節の型が `internal sealed` であることをリフレクションで、コンテナへ登録されていないことを `ServiceCollection` で確かめる形へ。
- 負の確認: 上の各違反を本体へ一時的に注入し、`RemovedMembers_AreNotDeclaredAgain`・`TheModes_AreSpecialAction_AndControl_Only`・`TheBase_OwnsTheOnlyPairOfEvents_AndTheWriters`・`TheProcessPriorityList_…`・`Sections_AreInternalSealedAndNotRegisteredInTheContainer` が失敗することを確かめ、元へ戻した。

## 置き換えなかった理由（区分ごとの扱い）

- K1・K2・K3・K5 は、置き換えると同じ守りにならない、または規則の実体そのものであるため、ソース文字列の検査のまま残す。
- K4 は、置き換えは可能だが、偽物を差す挙動のテストを新しく書く作業になる（例: `RuntimeFeatureSwitch` に偽の `IHidHideService`・`IOscService`・`IUdpMotionService` を渡して、転送を確かめる）。守りの喪失のリスクがあるため、別の Phase で、1 つずつ負の確認をしながら行う。

## ファイル別の区分

| 区分 | ファイル | ソースを読む Fact |
|---|---|---|
| C | `AppHostStartupShutdownGuardTests.cs` | 2 |
| C | `BatteryUnificationTests.cs` | 5 |
| C | `BindingWindowModeGuardTests.cs` | 6 |
| C | `DefaultUnboundTypeTests.cs` | 2 |
| C | `LifecycleGuardTests.cs` | 6 |
| C | `ProfileEditorTargetDeviceGuardTests.cs` | 9 |
| C | `ProfileLoadFoundationTests.cs` | 3 |
| C | `StatusMessageVisibilityTests.cs` | 1 |
| C | `TriggerPanelGuardTests.cs` | 3 |
| K1 | `ActionAndProfileReferenceIdTests.cs` | 1 |
| K1 | `AppPostHostGlobalReferenceGuardTests.cs` | 1 |
| K1 | `ApplyProfileCleanupOrderGuardTests.cs` | 1 |
| K1 | `BatteryPrimingTests.cs` | 2 |
| K1 | `ConnectionGuardTests.cs` | 4 |
| K1 | `ConnectorGuardTests.cs` | 3 |
| K1 | `ControlProfileTests.cs` | 1 |
| K1 | `DisconnectCharacterizationTests.cs` | 4 |
| K1 | `DisconnectClearsTempProfileTests.cs` | 3 |
| K1 | `DisconnectGuardTests.cs` | 2 |
| K1 | `DuplicateAssignsNewIdTests.cs` | 1 |
| K1 | `EmptyMacroTests.cs` | 1 |
| K1 | `GlobalRemovedGuardTests.cs` | 1 |
| K1 | `InputReportGuardTests.cs` | 2 |
| K1 | `MapCustomGuardTests.cs` | 4 |
| K1 | `ProfileEditorMappingDevTypeGuardTests.cs` | 1 |
| K1 | `ProfileIdTests.cs` | 1 |
| K1 | `PublishBindingsTests.cs` | 1 |
| K1 | `R6PageScrollWheelTests.cs` | 1 |
| K1 | `ReconnectProfilePerControllerTests.cs` | 1 |
| K1 | `RemovedGyroSwipeNoticeWiringTests.cs` | 2 |
| K1 | `RemovedShiftNoticeWiringTests.cs` | 2 |
| K1 | `RenameFollowUpRemovalTests.cs` | 1 |
| K1 | `SlotLifecycleTests.cs` | 1 |
| K1 | `StartupBootstrapGuardTests.cs` | 3 |
| K1 | `StartupOrderGuardTests.cs` | 4 |
| K1 | `StateHolderInjectionGuardTests.cs` | 1 |
| K1 | `SyntheticInputStateGuardTests.cs` | 2 |
| K2 | `AltTabRemovalGuardTests.cs` | 1 |
| K2 | `BatteryRefreshTests.cs` | 3 |
| K2 | `ControlDefaultAndExtrasTests.cs` | 1 |
| K2 | `ControlEditorHostGuardTests.cs` | 4 |
| K2 | `ControlMacroTabGuardTests.cs` | 1 |
| K2 | `ControlModeGuardTests.cs` | 2 |
| K2 | `ControllerReadingsNoClippingTests.cs` | 1 |
| K2 | `GyroCalibrationNullDeviceGuardTests.cs` | 1 |
| K2 | `HoverSelectDelayTests.cs` | 3 |
| K2 | `KeepsizeTests.cs` | 2 |
| K2 | `PickerCloseKeepsTypeTests.cs` | 2 |
| K2 | `ProfileEditorBackendViewModelTests.cs` | 2 |
| K2 | `ProfileEditorCallSitesTests.cs` | 4 |
| K2 | `ReapplyConnectedSlotsOnlyTests.cs` | 2 |
| K2 | `RecordBoxExtraButtonsVisibilityGuardTests.cs` | 1 |
| K2 | `SaExtrasEditorGuardTests.cs` | 2 |
| K2 | `StickDeadZoneBindingGuardTests.cs` | 1 |
| K2 | `ValidationTableTests.cs` | 2 |
| K3 | `BindingWindowMarkupLoadTests.cs` | 1 |
| K3 | `CodeHealthRatchetTests.cs` | 1 |
| K3 | `FileNameMatchesTypeGuardTests.cs` | 1 |
| K3 | `FuncRatchetGuardTests.cs` | 1 |
| K3 | `LocalizationResourceTests.cs` | 1 |
| K3 | `ModelDiagramGuardTests.cs` | 3 |
| K3 | `OutputTypeNamingGuardTests.cs` | 2 |
| K3 | `ProcessingLogGuardRuleTests.cs` | 1 |
| K3 | `ProfileEditorMarkupLoadTests.cs` | 2 |
| K3 | `ReleaseSourceCentralizationGuardTests.cs` | 1 |
| K3 | `RemovedGyroSwipeWiringTests.cs` | 1 |
| K3 | `RemovedShiftWiringTests.cs` | 1 |
| K3 | `RuntimeKeyRatchetTests.cs` | 1 |
| K3 | `SettingsSlotWindowUsageTests.cs` | 2 |
| K3 | `SharedStaThreadRuleTests.cs` | 1 |
| K3 | `ThemeResourceKeyGuardTests.cs` | 2 |
| K3 | `ThemeTokenRuleTests.cs` | 2 |
| K3 | `UnifiedCodingPracticeRuleTests.cs` | 3 |
| K3 | `UserLogTextResourceTests.cs` | 1 |
| K3 | `ViewCommandBindingRatchetTests.cs` | 1 |
| K3 | `ViewModelChangeNotificationTests.cs` | 1 |
| K4 | `ActionIdTests.cs` | 1 |
| K4 | `AppHostNoImplicitHostGuardTests.cs` | 2 |
| K4 | `AppPostHostContractTests.cs` | 2 |
| K4 | `AutoRevertBatteryTests.cs` | 1 |
| K4 | `ControlServiceOscSenderRaceTests.cs` | 1 |
| K4 | `ControlsAnalogMouseTests.cs` | 1 |
| K4 | `ControlsExtrasTests.cs` | 1 |
| K4 | `ControlsMouseTests.cs` | 1 |
| K4 | `ControlsPadTests.cs` | 1 |
| K4 | `CustomSteamFolderTests.cs` | 1 |
| K4 | `EnvironmentServiceStaticInitGuardTests.cs` | 1 |
| K4 | `FourTwoGlobalReferenceGuardTests.cs` | 1 |
| K4 | `GroupOneGlobalReferenceGuardTests.cs` | 2 |
| K4 | `GroupThreeGlobalReferenceGuardTests.cs` | 1 |
| K4 | `HidHideGuardTests.cs` | 1 |
| K4 | `IpcCommandTests.cs` | 4 |
| K4 | `IpcQueryResultGuardTests.cs` | 1 |
| K4 | `KeyButtonOutputFoundationTests.cs` | 2 |
| K4 | `MainWindowUdpOutContTypeGuardTests.cs` | 2 |
| K4 | `MainWindowsAndTrayViewModelGlobalReferenceGuardTests.cs` | 1 |
| K4 | `MouseReleaseButtonHoldTests.cs` | 1 |
| K4 | `MouseTapClickHoldTests.cs` | 2 |
| K4 | `OscGuardTests.cs` | 1 |
| K4 | `OutputTypeOwnershipGuardTests.cs` | 2 |
| K4 | `ProfileEditorGlobalReferenceGuardTests.cs` | 1 |
| K4 | `ProfileLegacyXmlRoundTripTests.cs` | 1 |
| K4 | `ProfileMappingContractTests.cs` | 1 |
| K4 | `RuntimeSwitchTests.cs` | 1 |
| K4 | `ServiceLocatorGuardTests.cs` | 4 |
| K4 | `SettingsViewModelGlobalReferenceGuardTests.cs` | 1 |
| K4 | `SlotLinkedAutoProfileReferenceIdTests.cs` | 1 |
| K4 | `SmallFixesGuardTests.cs` | 1 |
| K4 | `Stage3ProfileActionsSnapshotGuardTests.cs` | 1 |
| K4 | `UdpMotionGuardTests.cs` | 1 |
| K5 | `BuildCommitLogGuardTests.cs` | 2 |
| K5 | `ContractCleanupGuardTests.cs` | 1 |
| K5 | `MacroGuardTests.cs` | 1 |
| K5 | `MouseGuardTests.cs` | 1 |
| K5 | `OrphanMembersGuardTests.cs` | 1 |

## K4 の書き換え（RV8e）

K4 の 47 件のうち、次の 10 件を、挙動・リフレクション・表の検査へ書き換えた（いずれも、本体へ違反を一時的に注入して失敗を確かめ、元へ戻した。ただし `SlotLinkedAutoProfileReferenceIdTests` のコンストラクター引数の検査は、注入が大きいため負の確認をしていない）。

- `RuntimeFeatureSwitch` の転送 4 件（`HidHideGuardTests`・`OscGuardTests`・`UdpMotionGuardTests`・`InputReportGuardTests` の `TheOtherFiles_ForTheCallers`）: 新しい `RuntimeFeatureSwitchForwardingTests`（偽の相手 `RecordingProxy` を差し、呼び出しと引数を確かめる）へ。
- `EnvironmentServiceStaticInitGuardTests`: 実行時のスロット上限が 1 以上で上限以内であることの検査へ。
- `CustomSteamFolderTests`: `MainWindowsViewModel.CustomSteamFolderIfValid` の挙動（無効・存在しない・存在する）の検査へ。
- `ControlsMouseTests`・`ControlsPadTests`・`ControlsExtrasTests` の配線の文字列: `FrameOutput` の実装関係の検査、挙動のテストが覆うため削除、撤去した `Mapping` のメンバーを `RemovedMembers` の表へ。
- `SlotLinkedAutoProfileReferenceIdTests`: 3 つのシリアライザーが `IProfileIdentityService` をコンストラクターで受けることの検査へ。

残り 37 件の K4 は、挙動のテストにするには、対象の準備が大きい（`MainWindow`・`ProfileEditor` などの画面、`App.xaml.cs` の起動、`Mapping`・`AutoProfileService` などの組み立て）。1 件ずつ負の確認をしながら、別の Phase で進める。

## K4 の書き換え（RV8f）

残り 37 件の K4 のうち、次を書き換えた（すべて負の確認済み。注入 → 失敗を確認 → 復元）。

- `ActionIdTests`: 8 つのエディター VM を、記録する偽のリポジトリで作り、保存の引数の ID が `action.id` であることを確かめる。
- `OutputTypeOwnershipGuardTests`・`FourTwoGlobalReferenceGuardTests`: 挙動・反射の検査へ。
- `AutoRevertBatteryTests`: 実際に動かして、通知のバッテリー表示（`N/A%` と数値）を確かめる（`TestDs4Device.SetBattery` を追加）。
- `SettingsViewModelGlobalReferenceGuardTests`: 文字列 1 行の検査を撤去（型の検査が覆う）。
- `AppPostHostContractTests`: `LoadActions` の「ファイルがなければ既定のアクションを作る」を挙動の検査にした（`File.Exists` の正規表現を撤去）。

`CreateStandardActions` は、実行すると利用者の Profiles フォルダーを書き換えるため、委譲の検査のまま残した（OS の状態に触れない規則）。

結果: ソースの文字列を読むテストは 121 → 119 ファイル。全 3,664 件合格。残りの K4 は、画面（`MainWindow`・`ProfileEditor`）・`App.xaml.cs` の起動・`Mapping` などの組み立てが必要で、準備が大きいか、実プロセスの OS 状態に触れるため、書き換えていない。
