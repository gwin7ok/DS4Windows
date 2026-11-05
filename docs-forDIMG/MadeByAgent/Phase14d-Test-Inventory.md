# Phase14d 棚卸し表: ソースを読むテストと遅いテスト

作成日: 2026-11-05（Step 14d-0）
計画書: `Phase14d-Plan.md`（§3 守るもの、§6 Step）
作り方: 補助スクリプトで、ソースを文字列で読むテストメソッドを洗い出し（DS4Windows/ のパス、`SourceFileLocator`、`RegistrationSource`、`XamlTestHelper`、`*.cs` の走査）、
Guard テストを中心に 1 件ずつ読んで区分を決めた。表にないソースを読むメソッドは K（残す）。

## 1. 区分

| 区分 | 意味 | 件数（メソッド） | 処理する Step |
|---|---|---|---|
| F | 撤去済み API（型・メンバー）がないことの検査。型の検査 1 か所（`RemovedApiRuleTests`）へ畳み、元の検査は消す | 100 | 14d-2 |
| D | 削除（空振り、重複、旧パスの確認）。守る内容がないか、別の検査が同じものを守っている | 26 | 14d-1（空振り）・14d-2 |
| R | 型・コンテナの検査へ置き換え（リフレクション、コンテナの解決、ArchUnitNET） | 29 | 14d-3 |
| K | 残す（うち 16 件は、撤去済みの名前の Assert だけを外す） | 312 | 14d-1 で読み取りの部品だけ差し替え |
| S | 遅い（0.3 秒以上）。§5 の方法で短縮 | 60 | 14d-4 |

ソースを読むメソッドの合計: 467（計画書 §2.3 の 391 は Assert の文字列で数えた概数。ここでは、読み取りの補助メソッド経由と、パスの一覧のフィールド経由を含めた）。
F・D で全メソッドが消えるファイル: 10（14d-2 でファイルごと削除）。

## 2. `RemovedApiRuleTests` に集める内容（F の置き換え先）

| 種類 | 内容 | 由来（F の検査） |
|---|---|---|
| 型がないこと | `Global`、`Program`、`ServiceProviderHolder`、静的 `ActionManager`、`ButtonSpecialActionOutput`、`SyntheticDispatcher`、`ActionRegistry`、`DS4Windows.AppHost`（互換）、`IVirtualPadHost`、`IServiceLifecycleHost`、`IUdpMotionHost`、`IDeviceSlotHost`、`IUdpServerService`、`UdpServerService`、旧 `LightbarMacro` ほか | Global の参照の検査（Phase6-Step2〜Phase7-Step10d、約 20 ファイル）、Phase7-Step9 W1、Phase13-Step3・Step4、Phase9-E3 |
| メンバーがないこと | 移した・撤去したメンバー（`Mapping`・`ControlService`・`BackingStore`・契約・ViewModel・状態の保持クラスの `Shared` ほか）を、型 × メンバー名の表で | 各 Guard の `*_NoLongerContains_*`、`*RemovedMembers*`、`*NoLongerDeclares*` |
| AppHost の利用者 | `AppHost` に依存してよいのは `App` と `DS4Windows.Composition` だけ（ArchUnitNET） | `Step10FourGlobalRatchetGuardTests` の AppHost の列、各 Guard の `DoesNotContain("AppHost")` |

ログの文言の中の `Program.rootHub`（2 件）は、規約 §2.3 によりそのまま残す（検査の対象から外す）。

## 3. D（削除）

| ファイル | メソッド | 守っていた内容・置き換え先 |
|---|---|---|
| `MappingStep3FinalGlobalReferenceGuardTests` | `KeepItems_TodoCommentsAreResolved` | 解決済みの TODO の文言がないことの確認。守る対象がない |
| `Phase13Step5FuncRemovalGuardTests` | `FuncOfTheSlotHost_NoLongerAppears` | Phase13FuncRatchetGuardTests と重複（IDeviceSlotHost は型ごと撤去） |
| `Phase13Step5FuncRemovalGuardTests` | `FuncsOfTheOutputSlotCycle_NoLongerAppear` | Phase13FuncRatchetGuardTests と重複 |
| `Phase13Step5FuncRemovalGuardTests` | `RemovedFuncs_DoNotAppearInProductionSources` | Phase13FuncRatchetGuardTests（Func<I…> の許可リスト）と重複 |
| `Phase7Step10bW2TrayIconAndColorGuardTests` | `TheSource_HasNoColorOrIconMembersOnGlobalAnymore` | 存在しない Global.cs を読み、何も検査せずに合格している（空振り） |
| `Phase7Step10cW2SerializersGuardTests` | `TheRepositories_UseTheSerializers_NotTheStore` | BackingStore にそのメソッドがない（同じクラスの BackingStore_NoLongerDeclaresTheMovedMethods）ため、呼び出しはコンパイルで止まる |
| `Phase7Step10cW5LegacyProfileSaveGuardTests` | `TheSource_UsesTheSerializerEverywhere` | BackingStore にそのメソッドがないため、呼び出しはコンパイルで止まる |
| `Phase7Step10dW1TestGlobalsGuardTests` | `Tests_DoNotReferenceGlobalDirectly_ForTheFourWindows` | Global 型がないため、テストからも参照できない |
| `Phase7Step10eW5RatchetEqualityGuardTests` | `FinalTotals_AreFixed` | Global・ServiceProviderHolder の 2 つのラチェットの合計の固定。対象のラチェットを F で畳むため |
| `Phase7Step10eW5RatchetEqualityGuardTests` | `GlobalRatchetBaseline_EqualsTheActualCounts` | 対象のラチェット（Step10FourGlobalRatchetGuardTests）を F で畳むため |
| `Phase7Step10eW5RatchetEqualityGuardTests` | `ServiceLocatorRatchetBaseline_EqualsTheActualCounts` | 対象のラチェット（Phase7Step1ServiceLocatorRatchetGuardTests）を F で畳むため |
| `Phase7Step2RemovedContractMembersGuardTests` | `RemovedFiles_DoNotExist` | 旧パス（DS4Control/ ほか。Phase14 で再編）のファイルがないことの確認。同じパスに戻ることはない |
| `Phase7Step3RemovedGlobalForwardersGuardTests` | `RemovedForwarders_DoNotReappearInGlobal` | 存在しない Global.cs を読み、何も検査せずに合格している（空振り） |
| `Phase7Step4AppHostGuardTests` | `GlobalLoad_IsRemoved` | 存在しない Global.cs を読み、何も検査せずに合格している（空振り） |
| `Phase7Step4EnvironmentProbesGuardTests` | `RemovedProbes_DoNotReappearInGlobal` | 存在しない Global.cs を読み、何も検査せずに合格している（空振り） |
| `Phase7Step4PathServiceGuardTests` | `RemovedMembers_DoNotReappearInGlobal` | 存在しない Global.cs を読み、何も検査せずに合格している（空振り） |
| `Phase7Step5RootHubGuardTests` | `GlobalControllerConfigWrappers_AreNotRedeclared` | 存在しない Global.cs を読み、何も検査せずに合格している（空振り） |
| `Phase7Step5ServiceLocatorGuardTests` | `GlobalInstances_AreRemoved_ExceptTheProfileSettingsOne` | 存在しない Global.cs を読み、何も検査せずに合格している（空振り） |
| `Phase7Step8W2MappingInstanceGuardTests` | `CallersUseTheInstance_NotStaticMappingCalls` | Mapping の静的メンバーは、同じクラスの Mapping_ImplementsIMapping_AndHasNoStaticMutableStateOtherThanTheAllowedTables（リフレクション）が守る。読むファイルの一部（ActionManager.cs）は存在しない |
| `Phase7Step8W2MappingInstanceGuardTests` | `OnlyTheDefaultActionManager_ReceivesALazyMapping_TheOthersReceiveTheMappingDirectly` | Phase13FuncRatchetGuardTests（Func<IMapping> は DefaultActionManager だけ）と重複 |
| `Phase7Step8W3OutputKbmOwnershipGuardTests` | `Global_NoLongerOwnsTheOutputKbmHandlerOrMapping` | 存在しない Global.cs を読み、何も検査せずに合格している（空振り） |
| `Phase7Step9W1CompatAppHostRemovedGuardTests` | `TheCompatFile_AndFolder_DoNotExist_AndNoSourceDeclaresIt` | 旧パス（DS4Control/ ほか。Phase14 で再編）のファイルがないことの確認。同じパスに戻ることはない |
| `Phase9EVirtualPadGuardTests` | `OutputSlotService_NoLongerDependsOnControlService` | 第3層 → 第2層の具象の参照は LayerDependencyRuleTests の L4 が禁止している（重複） |
| `Step12DeadShimRemovalGuardTests` | `ProtectedMembers_RemainInGlobal` | 存在しない Global.cs を読み、何も検査せずに合格している（空振り） |
| `Step12DeadShimRemovalGuardTests` | `RemovedFiles_DoNotExist` | 旧パス（DS4Control/ ほか。Phase14 で再編）のファイルがないことの確認。同じパスに戻ることはない |
| `Step12DeadShimRemovalGuardTests` | `RemovedMembers_DoNotReappearInGlobal` | 存在しない Global.cs を読み、何も検査せずに合格している（空振り） |

## 4. R（型・コンテナの検査へ置き換え）

| ファイル | メソッド | 守っていた内容・置き換え先 |
|---|---|---|
| `MappingCommitAndCalibrationGlobalReferenceGuardTests` | `ApplyDrift_TakesProfileSettingsAsParameter` | メソッドの引数の型をリフレクションで確かめる |
| `MappingCommitAndCalibrationGlobalReferenceGuardTests` | `Commit_TakesProfileSettingsAsParameter` | メソッドの引数の型をリフレクションで確かめる |
| `MappingSetCurveAndDeadzoneGlobalReferenceGuardTests` | `SetCurveAndDeadzone_TakesProfileSettingsAsParameter` | メソッドの引数の型をリフレクションで確かめる |
| `Phase13Step3HostRemovalGuardTests` | `ControlService_HasNoKbmInitializationParameters_AndHas17Parameters` | コンストラクターの引数（リフレクション） |
| `Phase13Step5FuncRemovalGuardTests` | `FuncOfControlService_NoLongerAppears` | Phase13FuncRatchetGuardTests の対象を Func<ControlService> にも広げる（I で始まらない型は今の正規表現に掛からない） |
| `Phase7Step10bW3ContractCleanupGuardTests` | `ProfileApplicationService_DoesNotTakeIDeviceStateService` | コンストラクターの引数（リフレクション） |
| `Phase7Step1DeadMethodGuardTests` | `KeptMethods_StillExistInBackingStore` | メソッドがあること（リフレクション） |
| `Phase7Step2RemovedContractMembersGuardTests` | `KeptMembers_StillExistInContracts` | 契約のメンバーがあること（リフレクション） |
| `Phase7Step4AppHostGuardTests` | `AppHost_DoesNotDeclareRemovedOverloads` | CreateHost の多重定義（リフレクション） |
| `Phase7Step4EnvironmentProbesGuardTests` | `EnvironmentService_OwnsTheProbesAndTheirState` | EnvironmentService のメンバーがあること（リフレクション） |
| `Phase7Step5RootHubGuardTests` | `App_HoldsTheControlServiceInAVolatileField` | フィールドが volatile であること（リフレクションの修飾子） |
| `Phase7Step5RootHubGuardTests` | `ServiceRegistration_AlwaysCreatesTheControlService` | コンテナから ControlService を解決し、Singleton であること |
| `Phase7Step5ServiceLocatorGuardTests` | `ControlServiceAndBackingStore_ReceiveTheirCollaborators` | ControlService の引数（リフレクション）と、解決した BackingStore の ProcessInspector が設定済みであること（コンテナ） |
| `Phase7Step5ServiceLocatorGuardTests` | `MainWindowAndProfileEditor_DoNotUseTheLocator_AndAreCreatedThroughFactories` | IMainWindowFactory の解決（コンテナ）。AppHost の部分は AppHost の利用者の規則（App と Composition だけ）と、ServiceProviderHolder・Program の型がないことへ |
| `Phase7Step6OutputTypeOwnershipGuardTests` | `Registration_HandsTheArrayToBackingStore_WithoutResolvingTheServiceFromTheStoreRegistration` | IOutputDeviceTypeState を解決すると、BackingStore.ActiveOutDevType が同じ配列になること（コンテナ） |
| `Phase7Step7MacroRuntimeStateGuardTests` | `Registration_RegistersTheSharedState` | コンテナが同じインスタンスを返すこと |
| `Phase7Step7StickMotionStateGuardTests` | `Mapping_HoldsNoStickMotionStateOfItsOwn_AndNoNestedStateTypes` | Mapping のフィールドの型（リフレクション）。Shared は 撤去したメンバーの一覧（リフレクション）へ |
| `Phase7Step7StickMotionStateGuardTests` | `Registration_RegistersTheSharedState` | コンテナが同じインスタンスを返すこと |
| `Phase7Step7SyntheticInputStateGuardTests` | `Mapping_HoldsNoSyntheticStateOfItsOwn_OnlyForwarders` | Mapping のフィールドの型（リフレクション） |
| `Phase7Step7SyntheticInputStateGuardTests` | `Registration_SubscribesTheResetOnce_AndRegistersTheSharedState` | BackingStore.RaiseActionsLoaded で実行時状態がリセットされること（コンテナ）。差し替えの振る舞いは BackingStoreActionsLoadedHandlerTests |
| `Phase7Step8W1StateHolderInjectionGuardTests` | `MouseAndControlService_ReceiveTheHoldersByConstructor` | コンストラクターの引数（リフレクション） |
| `Phase9EHidHideGuardTests` | `HidHideService_DoesNotTouchTheDriverDirectly` | HidHideAPIDevice への依存は工場だけ（ArchUnitNET） |
| `Phase9EInputReportGuardTests` | `NewComponents_DoNotReferenceStaticEntryPoints_OrControlService` | 新しい部品が ControlService に依存しないこと（ArchUnitNET）。AppHost の部分は AppHost の利用者の規則（App と Composition だけ）と、ServiceProviderHolder・Program の型がないことへ |
| `Phase9EOscGuardTests` | `OscService_AndTheContracts_DoNotUseTheSharpOscTypes` | SharpOSC への依存は SharpOscTransportFactory だけ（ArchUnitNET） |
| `ProfileEditorGlobalReferenceGuardTests` | `ProfileEditor_ReceivesAppSettingsServiceByConstructor_AndMainWindowPassesIt` | コンストラクターの引数（リフレクション）と、null で例外になること（振る舞い） |
| `ProfileSettingsViewModelGlobalReferenceGuardTests` | `Constructors_HaveNoOptionalServiceArguments_AndKeepTargetDevice` | コンストラクターの引数に省略可能なサービスがないこと、targetDevice の既定値（リフレクション） |
| `ProfileSettingsViewModelGlobalReferenceGuardTests` | `ViewModelFactory_HasNoServiceLocatorInConstructor` | コンストラクターの引数（Func<ControlService>・省略可能な引数がない）をリフレクションで。AppHost の部分は AppHost の利用者の規則（App と Composition だけ）と、ServiceProviderHolder・Program の型がないことへ |
| `Step10GroupOneGlobalReferenceGuardTests` | `StartupMethods_TakesPathsAsArguments` | メソッドの引数をリフレクションで |
| `Step12DeadShimRemovalGuardTests` | `Wave5_RemovedApiSurface_DoesNotReappear` | IPathService.AppDataPath に setter がないこと（リフレクション） |

## 5. F（撤去済み API。`RemovedApiRuleTests` へ畳む）

| ファイル | メソッド | 守っていた内容・置き換え先 |
|---|---|---|
| `AppPostHostGlobalReferenceGuardTests` | `GlobalReferences_MatchPlan_PerRegion` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `ControlServiceGlobalReferenceGuardTests` | `ControlService_GlobalReferences_MatchExplicitExclusionList` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `ControlServiceGlobalReferenceGuardTests` | `ControlService_HasNoUsingStaticGlobal` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `EnvironmentServiceStaticInitGuardTests` | `SlotLimitCalculation_DoesNotReferenceNonConstGlobalMembers` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `MainWindowsAndTrayViewModelGlobalReferenceGuardTests` | `MainWindowsViewModel_HasNoGlobalOrServiceLocatorReferences` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ。AppHost の部分は AppHost の利用者の規則（App と Composition だけ）と、ServiceProviderHolder・Program の型がないことへ |
| `MainWindowsAndTrayViewModelGlobalReferenceGuardTests` | `MainWindowsViewModel_RemovedUnreferencedUpdaterMembers_AreNotReintroduced` | 撤去したメンバーの一覧（リフレクション）へ |
| `MainWindowsAndTrayViewModelGlobalReferenceGuardTests` | `TrayIconViewModel_HasOnlyConstGlobalReferences_AndNoServiceLocator` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `MappingCommitAndCalibrationGlobalReferenceGuardTests` | `ApplyDrift_HasNoReferenceToMigratedGlobalMembers` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `MappingCommitAndCalibrationGlobalReferenceGuardTests` | `Commit_HasNoReferenceToMigratedGlobalMembers` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `MappingControlPathGlobalReferenceGuardTests` | `ControlPathMethods_HaveNoReferenceToMigratedGlobalMembers` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `MappingHelperMethodsGlobalReferenceGuardTests` | `HelperMethods_HaveNoReferenceToMigratedGlobalMembers` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `MappingMacroPathGlobalReferenceGuardTests` | `MappingSource_HasNoGlobalReferenceToOutputKbmMapping` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `MappingSetCurveAndDeadzoneGlobalReferenceGuardTests` | `SetCurveAndDeadzone_HasNoReferenceToMigratedGlobalMembers` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `MappingStep3FinalGlobalReferenceGuardTests` | `KeepItems_AreGone_TheServiceBindingsAreConstructorInjected` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `MappingStep3FinalGlobalReferenceGuardTests` | `UsingStaticGlobalDirective_IsStillPresent_ForGetTransitionedColor` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `MouseGlobalReferenceGuardTests` | `MouseFiles_DoNotUseStaticGlobalImport` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `MouseGlobalReferenceGuardTests` | `MouseFiles_HaveNoClampReferenceToGlobalAnymore` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `MouseGlobalReferenceGuardTests` | `MouseFiles_HaveNoGlobalReferenceOtherThanClamp` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `Phase13Step10ForwardRemovalGuardTests` | `ControlService_HasNoneOfTheRemovedForwardingMembers` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase13Step3HostRemovalGuardTests` | `TheRemovedHostInterfaces_DoNotExist_AndControlServiceDoesNotImplementThem` | 撤去した型の一覧へ（リフレクションの検査をまとめる） |
| `Phase13Step4ActionManagerRemovalGuardTests` | `ProductionSources_DoNotReferenceTheRemovedTypes` | 型がないため参照はコンパイルで止まる。撤去した型の一覧へ |
| `Phase13Step4ActionManagerRemovalGuardTests` | `RemovedTypes_DoNotExist` | 撤去した型の一覧へ（リフレクションの検査をまとめる） |
| `Phase13Step9RunningForwardRemovalGuardTests` | `ControlService_NoLongerExposesTheForward` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase7Step10aW2ControllerLimitsGuardTests` | `TheFiles_DoNotReferenceTheGlobalConstants` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `Phase7Step10aW3ResourcePrefixGuardTests` | `TheFormsFiles_DoNotReferenceTheGlobalPrefixes` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `Phase7Step10bW1ControlTablesGuardTests` | `TheSource_NoLongerReferencesTheMovedMembersOnGlobal_AndBackingStoreHasNoCopy` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ。BackingStore の写しは 撤去したメンバーの一覧（リフレクション）へ |
| `Phase7Step10bW4UsingMaxControllersGuardTests` | `NoSource_ReferencesTheRemovedShim` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase7Step10cW2SerializersGuardTests` | `BackingStore_NoLongerDeclaresTheMovedMethods` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase7Step10cW2SerializersGuardTests` | `TheSerializer_DoesNotReferenceGlobal_AndKeepsTheStoreAsTheOnlyState` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `Phase7Step10cW3AppSettingsSerializerGuardTests` | `BackingStore_NoLongerDeclaresLoadSaveAndLoadFakeExeName_ButKeepsPostProcessLoad` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase7Step10cW3AppSettingsSerializerGuardTests` | `TheSerializer_DoesNotReferenceGlobal_AndTheXmlStoreUsesIt` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `Phase7Step10cW4LegacyProfileLoadGuardTests` | `BackingStore_NoLongerDeclaresTheMovedMethods_ButKeepsTheSharedHelpersPublic` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase7Step10cW4LegacyProfileLoadGuardTests` | `TheSerializer_DoesNotReferenceGlobal_AndTheCallersUseIt` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `Phase7Step10cW5LegacyProfileSaveGuardTests` | `BackingStore_NoLongerDeclaresTheMovedMethods_NorTheWorkingXmlDocument` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase7Step10cW6ProfilePresetsGuardTests` | `BackingStore_NoLongerDeclaresThePresetMethods` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase7Step10cW6ProfilePresetsGuardTests` | `TheSource_PresetsDoNotReferenceGlobal_AndTheLoaderDoesNotCallTheStore` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `Phase7Step10dW3DeviceLayerSettingsGuardTests` | `LowerLayerFiles_DoNotReferenceGlobal` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `Phase7Step10dW5GlobalRemovedGuardTests` | `NoSource_UsesTheStaticGlobalDirective_OrAGlobalMemberInCode` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `Phase7Step10dW5GlobalRemovedGuardTests` | `TheGlobalType_AndFile_AreGone` | 撤去した型の一覧へ（この検査を移す） |
| `Phase7Step10eW1SlotLimitServicesGuardTests` | `TheFiles_DoNotUseTheStaticSlotLimit` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase7Step10eW2SlotLimitUiGuardTests` | `TheStaticShim_IsGone_AndNoSourceReferencesIt` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase7Step10eW4OrphanMembersGuardTests` | `RemovedMembers_AreNotDeclaredAgain` | 撤去したメンバーの一覧へ（リフレクションの検査をまとめる） |
| `Phase7Step1DeadMethodGuardTests` | `RemovedMethods_DoNotReappearInBackingStore` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase7Step1ServiceLocatorRatchetGuardTests` | `ServiceProviderHolderReferences_DoNotExceedBaseline` | 基準値の表は空（型は Phase13 で撤廃）。AppHost の利用者の規則（App と Composition だけ）と、ServiceProviderHolder・Program の型がないことへ |
| `Phase7Step2RemovedContractMembersGuardTests` | `RemovedMembers_DoNotReappearInContracts` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase7Step3RemovedGlobalForwardersGuardTests` | `AppSettingsService_DoesNotReferenceRemovedGlobalForwarders` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `Phase7Step4AppHostGuardTests` | `WelcomeDialog_NoLongerLoadsConfigItself` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ。loadConfig は 撤去したメンバーの一覧（リフレクション）へ |
| `Phase7Step4EnvironmentProbesGuardTests` | `NoSourceFile_ReferencesRemovedProbesThroughGlobal` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `Phase7Step4PathServiceGuardTests` | `NoSourceFile_ReferencesRemovedMembersThroughGlobal` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `Phase7Step4PathServiceGuardTests` | `PathService_StaticInitialization_DoesNotReferenceGlobal` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `Phase7Step5RootHubGuardTests` | `NoSourceFile_ReferencesProgramRootHub_AndProgramCsIsGone` | AppHost の利用者の規則（App と Composition だけ）と、ServiceProviderHolder・Program の型がないことへ |
| `Phase7Step6OutputTypeOwnershipGuardTests` | `Global_NoLongerHoldsTheArrays_AndNoOneReadsThemFromGlobal` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `Phase7Step7MacroRuntimeStateGuardTests` | `Mapping_HoldsNoMacroStateOfItsOwn_OnlyForwarders` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase7Step7StickMotionStateGuardTests` | `MouseAndControlService_UseTheHolderAndTheTopLevelTypes` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase7Step7SyntheticInputStateGuardTests` | `SyntheticDispatcherService_UsesTheInjectedHolder_AndTheStaticBridgeHoldsNoState` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase7Step8W1StateHolderInjectionGuardTests` | `Mapping_NoLongerHasTheMovedOrDeadMembers` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase7Step8W2MappingInstanceGuardTests` | `Mapping_HasNoServiceLocatorOrGlobalFallbacks` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ。AppHost の利用者の規則（App と Composition だけ）と、ServiceProviderHolder・Program の型がないことへ |
| `Phase7Step8W3OutputKbmOwnershipGuardTests` | `NoSourceFile_ReferencesTheRemovedGlobalMembers_OrTheStaticResetMethod` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `Phase7Step8W4NoSharedGuardTests` | `NoSourceFile_ReferencesTheRemovedSharedWindows` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase7Step9W1CompatAppHostRemovedGuardTests` | `NoAppHostType_ExistsInTheDS4WindowsNamespace` | 撤去した型の一覧へ（リフレクションの検査をまとめる） |
| `Phase7Step9W2NoGlobalInMappingGuardTests` | `TheFile_DoesNotReferenceGlobal` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `Phase9BMouseGuardTests` | `Mapping_NoLongerContains_TheMovedMembers` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase9BMouseGuardTests` | `Mapping_StatefulMouseFieldsAreGone_ByReflection` | 撤去したメンバーの一覧へ（リフレクションの検査をまとめる） |
| `Phase9BMouseGuardTests` | `NewComponents_DoNotReferenceStaticEntryPoints` | AppHost の利用者の規則（App と Composition だけ）と、ServiceProviderHolder・Program の型がないことへ |
| `Phase9CMacroGuardTests` | `DeviceRuntimeState_NoLongerHasTheUnusedMacroFields` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase9CMacroGuardTests` | `Mapping_NoLongerContains_TheMovedMembers` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase9CMacroGuardTests` | `NewComponents_DoNotReferenceStaticEntryPoints` | AppHost の利用者の規則（App と Composition だけ）と、ServiceProviderHolder・Program の型がないことへ |
| `Phase9E6ConnectionGuardTests` | `ControlService_NoLongerContains_TheJoyConLinking` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase9E6ConnectionGuardTests` | `ControlService_NoLongerContains_TheMovedProfileSelection` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase9E6ConnectionGuardTests` | `ControlService_NoLongerContains_TheMovedSetup` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase9E6ConnectorGuardTests` | `ControlService_NoLongerContains_TheMovedConnection` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase9E6DisconnectGuardTests` | `ControlService_NoLongerContains_TheMovedDisconnectHandling` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase9E6LifecycleGuardTests` | `ControlService_NoLongerContains_TheMovedLifecycle` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase9EHidHideGuardTests` | `ControlService_NoLongerContains_TheMovedMembers` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase9EHidHideGuardTests` | `NewComponents_DoNotReferenceStaticEntryPoints` | AppHost の利用者の規則（App と Composition だけ）と、ServiceProviderHolder・Program の型がないことへ |
| `Phase9EInputReportGuardTests` | `ControlService_NoLongerContains_TheMovedMembers` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase9EOscGuardTests` | `ControlService_NoLongerContains_TheMovedMembers` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase9EOscGuardTests` | `NewComponents_DoNotReferenceStaticEntryPoints` | AppHost の利用者の規則（App と Composition だけ）と、ServiceProviderHolder・Program の型がないことへ |
| `Phase9EUdpMotionGuardTests` | `ControlService_NoLongerContains_TheMovedMembers` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase9EUdpMotionGuardTests` | `NewComponents_DoNotReferenceStaticEntryPoints` | AppHost の利用者の規則（App と Composition だけ）と、ServiceProviderHolder・Program の型がないことへ |
| `Phase9EVirtualPadGuardTests` | `ControlService_NoLongerContains_TheMovedMembers` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase9EVirtualPadGuardTests` | `VirtualPadService_DoesNotReferenceStaticEntryPoints_OrControlService` | AppHost の利用者の規則（App と Composition だけ）と、ServiceProviderHolder・Program の型がないことへ。ControlService の部分は L4 と重複 |
| `Phase9MapCustomGuardTests` | `Mapping_NoLongerContains_TheMovedCommit` | 撤去したメンバーの一覧（リフレクション）へ |
| `Phase9MapCustomGuardTests` | `Mapping_NoLongerContains_TheTwoStageStateMachine` | 撤去したメンバーの一覧（リフレクション）へ |
| `ProfileEditorGlobalReferenceGuardTests` | `ProfileEditor_DoesNotReferenceResolvedGlobalMembers` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `ProfileSettingsViewModelGlobalReferenceGuardTests` | `ProfileSettingsViewModel_HasOnlyConstGlobalReferences_AndNoServiceLocator` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `SettingsViewModelGlobalReferenceGuardTests` | `NoGlobalReferencesRemain` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `SettingsViewModelGlobalReferenceGuardTests` | `RemovedUnreferencedHandler_IsNotReintroduced` | 撤去したメンバーの一覧（リフレクション）へ |
| `SettingsViewModelGlobalReferenceGuardTests` | `ResolvedMembers_AreNotReferencedThroughGlobal` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `Step10FourGlobalRatchetGuardTests` | `DirectReferences_DoNotExceedStep10FourBaseline` | 70 行の基準値のうち 0 でないのは 4 行（AppHost 3、ログの文言の rootHub 2）。AppHost の利用者の規則（App と Composition だけ）と、ServiceProviderHolder・Program の型がないことへ |
| `Step10FourOneGlobalReferenceGuardTests` | `ReplacedFiles_DoNotReferenceGlobalOrServiceLocator` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `Step10FourTwoGlobalReferenceGuardTests` | `MainWindow_And_ControlService_UseOnlyConstGlobalMembers` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `Step10FourTwoGlobalReferenceGuardTests` | `ReplacedFiles_DoNotReferenceGlobalExceptRetainedMembers` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `Step10GroupOneGlobalReferenceGuardTests` | `LanguagePackControl_DoesNotUseServiceLocator` | AppHost の利用者の規則（App と Composition だけ）と、ServiceProviderHolder・Program の型がないことへ |
| `Step10GroupOneGlobalReferenceGuardTests` | `RemovedOrphanMethods_DoNotComeBack` | 撤去したメンバーの一覧（リフレクション）へ |
| `Step10GroupOneGlobalReferenceGuardTests` | `ReplacedFiles_DoNotReferenceGlobalExceptRetainedMembers` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `Step10GroupThreeGlobalReferenceGuardTests` | `ReplacedFiles_DoNotReferenceGlobalExceptRetainedMembers` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `Step10GroupThreeGlobalReferenceGuardTests` | `ReplacedFiles_DoNotUseServiceLocatorOrRootHubFallbacks` | AppHost の利用者の規則（App と Composition だけ）と、ServiceProviderHolder・Program の型がないことへ |
| `Step10GroupTwoGlobalReferenceGuardTests` | `ReplacedFiles_DoNotReferenceGlobalExceptRetainedMembers` | Global 型は Phase7-Step10d で削除済み（型がないため、参照はコンパイルで止まる）。`Global` 型がないことの 1 件へ |
| `Step10GroupTwoGlobalReferenceGuardTests` | `ReplacedFiles_DoNotUseServiceLocatorOrRootHubFallbacks` | AppHost の利用者の規則（App と Composition だけ）と、ServiceProviderHolder・Program の型がないことへ |

## 6. K のうち一部を畳むもの（メソッドは残す）

| ファイル | メソッド | 外す Assert |
|---|---|---|
| `Phase13Step3HostRemovalGuardTests` | `TheNewServices_AreSingletons_AndTheFuncRegistrationsAreGone` | Func<IVirtualPadHost>／Func<IServiceLifecycleHost> の Assert を外す（型ごと撤去） |
| `Phase7Step4AppHostGuardTests` | `DriverInstallBranch_CreatesTheHostExplicitlyBeforeLoadingSettings` | Global.Load の Assert を外す（順序の検査は残す） |
| `Phase7Step4StartupBootstrapGuardTests` | `App_UsesTheBootstrapInstancesInPreHost` | Global.* の Assert を外す |
| `Phase7Step5ServiceLocatorGuardTests` | `App_ResolvesServicesOnlyThroughTheSingleEntryPoint` | ServiceProviderHolder の Assert を外す |
| `Phase9E6ConnectionGuardTests` | `ConnectionProfileSelector_IsRegistered_AndHasNoStaticEntryPoints` | AppHost・ServiceProviderHolder の Assert を外す |
| `Phase9E6ConnectionGuardTests` | `DeviceConnectionSetup_IsRegistered_KeepsTheTouchpadLookupAtEventTime_AndHasNoStaticEntryPoints` | AppHost・ServiceProviderHolder の Assert を外す |
| `Phase9E6ConnectionGuardTests` | `JoyConLinker_IsRegistered_AndHasNoStaticEntryPoints` | AppHost・ServiceProviderHolder の Assert を外す |
| `Phase9E6ConnectionGuardTests` | `TheDeviceModules_AreRegistered_AllFiveOfThem_AndHaveNoStaticEntryPoints` | AppHost・ServiceProviderHolder の Assert を外す |
| `Phase9E6ConnectorGuardTests` | `TheConnector_IsRegistered_AndHasNoStaticEntryPoints` | AppHost・ServiceProviderHolder の Assert を外す |
| `Phase9E6DisconnectGuardTests` | `TheHandler_IsRegistered_AndThereAreNoStaticEntryPoints` | AppHost・ServiceProviderHolder の Assert を外す |
| `Phase9E6LifecycleGuardTests` | `TheLifecycle_IsRegistered_StartsInTheStoppedState_AndHasNoStaticEntryPoints` | AppHost・ServiceProviderHolder の Assert を外す（MainWindow の禁止は L5 と重複のため一緒に外す） |
| `Phase9EUdpMotionGuardTests` | `NewComponents_AreRegistered_AndTheUdpHostInterfaceIsGone` | IUdpMotionHost の Assert を撤去した型の一覧へ |
| `Phase9MapCustomGuardTests` | `TheCommitter_IsRegistered_AndKeepsTheOriginalOrder_AndHasNoStaticEntryPoints` | AppHost・ServiceProviderHolder の Assert を外す |
| `Phase9MapCustomGuardTests` | `TheTwoStageProcessor_IsRegistered_AndHasNoStaticEntryPoints` | AppHost・ServiceProviderHolder の Assert を外す |
| `SettingsViewModelGlobalReferenceGuardTests` | `Constructor_HasNoServiceLocator_AndRequiresServices` | AppHost の Assert を外す（AppHost の利用者の規則（App と Composition だけ）と、ServiceProviderHolder・Program の型がないことへ） |
| `Step10GroupOneGlobalReferenceGuardTests` | `ReplacedFiles_DoNotUseServiceLocatorOrStaticFallbacks` | AppHost・Program.rootHub の Assert を外す。App.Current／Application.Current／logHolder の禁止は残す |

## 7. S（遅いテスト、0.3 秒以上）

| クラス | 件数 | 合計（秒） | 短縮の方法（14d-4 で確かめる） |
|---|---|---|---|
| `Phase9MapCustomCommitterDifferentialTests` | 1 | 5.84 | 差分テストの入力の列を代表的なものに絞る（網羅の観点を記録する） |
| `Phase8Step78cW1ControlsMacroCharacterizationTests` | 5 | 5.38 | テストデータのマクロの待ちを短く（判定の境界は変えない） |
| `Phase8Step77bW1MultiActionDualPathTests` | 6 | 5.21 | テストデータのタップ間隔・マクロの待ちを短く（判定の境界は変えない） |
| `R2W4MacroToggleRepeatTests` | 2 | 4.16 | テストデータの繰り返しの周期の待ちを短く（判定の境界は変えない） |
| `Phase11W5CustomSteamFolderTests` | 9 | 3.30 | ViewModel の生成の費用を調べる（1 件 0.37 秒 × 9） |
| `ControllerReadingsNoClippingTests` | 2 | 2.74 | WPF の配置の計算。残す（短縮の手段がない） |
| `AppLoggerNotificationFallbackTests` | 1 | 2.74 | アセンブリの解析の費用。LayerDependencyRuleTests と解析結果を共有できるか調べる |
| `Phase10Step4W3ControlMacroOptionsTests` | 2 | 2.55 | テストデータのマクロの待ちを短く（判定の境界は変えない） |
| `Phase10Step4W4ControlSpecialTests` | 2 | 2.10 | ジャイロ較正の保持秒をテストデータで短く（判定は変えない） |
| `Phase10Step4W6ControlProfileTests` | 2 | 1.83 | 待ちの内訳を調べて短縮 |
| `Phase8Step4MapCustomBenchmarkTests` | 3 | 1.57 | Category=Benchmark にして通常の実行から外す |
| `PatternAViewModelTests` | 3 | 1.53 | ホストからの ViewModel の生成の費用を調べる |
| `Phase10Step4cW0LogTableTests` | 2 | 1.45 | 待ちの内訳を調べて短縮 |
| `Phase11W6LinkedProfileComboDeleteTests` | 1 | 1.16 | ViewModel の生成の費用を調べる |
| `Phase11W5CheckEveryTests` | 3 | 1.13 | ViewModel の生成の費用を調べる |
| `Phase9E6JoyConLinkerTests` | 2 | 1.08 | 調べる |
| `Phase8Step77bW2MultiTapTests` | 2 | 1.07 | テストデータのタップ間隔を短く（判定の境界は変えない） |
| `Phase10Step4dW5bProfileSlotNamesTests` | 1 | 0.93 | 並行の読み書きの繰り返しの回数を減らす |
| `SettingsViewModelWatchpoint2Tests` | 2 | 0.79 | ViewModel の生成の費用を調べる |
| `Phase7Step10cGoldenPersistenceTests` | 1 | 0.70 | ファイルの読み書き。残す |
| `Phase10Step4dW5aIdentityIndexTests` | 1 | 0.66 | 速さの検査。Benchmark 扱いの候補 |
| `Phase10Step4cW2EdgeLogTests` | 1 | 0.52 | 待ちの内訳を調べて短縮 |
| `Phase8Step1SlotLimitTests` | 1 | 0.45 | 450ms の待ちの短縮を調べる |
| `SettingsViewModelGlobalReferenceGuardTests` | 1 | 0.42 | ViewModel の生成の費用を調べる |
| `Phase13Step5FuncRemovalGuardTests` | 1 | 0.39 | 調べる |
| `Phase9MapCustomTwoStageKeyReleaseTests` | 1 | 0.37 | 調べる |
| `Phase10Step4dW0RuntimeKeyRatchetTests` | 1 | 0.33 | 全ソースの走査。共通の読み取り部品（14d-1）のキャッシュで短縮 |
| `ProcessInspectorTests` | 1 | 0.31 | プロセスの列挙。残す |

メソッド単位（Theory はケースの合計）で 0.3 秒以上のもの。合計 60 件・50.7 秒（2026-11-05 の全件の実行の記録から）。

## 8. K（残す）の内訳

K のメソッドは 312 件、133 ファイル。主な種類: XAML の構造（32 件）、
ログの文言（規約 §2.3）、安全弁・ガードレールの順序（規約 §2.2。例: `Phase9E6LifecycleGuardTests` の Start／Stop の順序、`Phase9F1StartupOrderGuardTests`）、
機能の配線（画面と ViewModel の接続、委譲の形）、行数の上限、TODO コメント（規約 §3.3 原則4）。
14d-1 で、読み取りの部品（自前の `Read`・`StripComments`）だけを共通の `SourceText` に差し替える。

<details><summary>K のファイルの一覧</summary>

- `AppPostHostGlobalReferenceGuardTests`（2）
- `ControlServiceOscSenderRaceTests`（2）
- `ControllerReadingsNoClippingTests`（1）
- `EnvironmentServiceStaticInitGuardTests`（1）
- `GyroCalibrationNullDeviceGuardTests`（1）
- `K7b1HidOverlappedIoGuardTests`（2）
- `MainWindowUdpOutContTypeGuardTests`（2）
- `MainWindowsAndTrayViewModelGlobalReferenceGuardTests`（1）
- `MappingCommitAndCalibrationGlobalReferenceGuardTests`（1）
- `MappingHelperMethodsGlobalReferenceGuardTests`（1）
- `MappingMacroPathGlobalReferenceGuardTests`（1）
- `MouseReleaseButtonHoldTests`（1）
- `MouseTapClickHoldTests`（2）
- `MouseTapToleranceTests`（1）
- `Phase10Step2W2TriggerPanelGuardTests`（3）
- `Phase10Step2W3ControlModeGuardTests`（2）
- `Phase10Step3W0ProfileEditorCallSitesTests`（4）
- `Phase10Step3W1BindingWindowModeGuardTests`（6）
- `Phase10Step3W2ControlDefaultAndExtrasTests`（1）
- `Phase10Step3W3ControlMacroTabGuardTests`（2）
- `Phase10Step3W3EmptyMacroTests`（3）
- `Phase10Step3W4ControlEditorHostGuardTests`（5）
- `Phase10Step3W4DefaultUnboundTypeTests`（3）
- `Phase10Step3bW0ProfileEditorLayoutTests`（11）
- `Phase10Step4W6ControlProfileTests`（2）
- `Phase10Step4cW0ValidationTableTests`（3）
- `Phase10Step4dW0RuntimeKeyRatchetTests`（1）
- `Phase10Step4dW1ProfileIdTests`（2）
- `Phase10Step4dW2ActionIdTests`（2）
- `Phase10Step4dW3ReferenceIdTests`（2）
- `Phase10Step4dW4ReferenceIdTests`（1）
- `Phase10Step4dW5bProfileSlotNamesTests`（2）
- `Phase10Step4dW5cRuntimeSwitchTests`（3）
- `Phase10Step4dW7FollowUpTests`（1）
- `Phase10Step6I5cSaExtrasEditorGuardTests`（2）
- `Phase11BatteryUnificationTests`（5）
- `Phase11W3KeepsizeTests`（3）
- `Phase11W4SaveApplyWiringGuardTests`（4）
- `Phase11W5CheckEveryTests`（2）
- `Phase11W5CustomSteamFolderTests`（2）
- `Phase11W5StatusMessageVisibilityTests`（2）
- `Phase11W6AutoRevertBatteryTests`（1）
- `Phase11W6BatteryPrimingTests`（3）
- `Phase11W6BatteryRefreshTests`（4）
- `Phase11W6DisconnectClearsTempProfileTests`（3）
- `Phase11W6DuplicateAssignsNewIdTests`（2）
- `Phase11W6HoverSelectDelayTests`（4）
- `Phase11W6PickerCloseKeepsTypeTests`（2）
- `Phase11W6ReapplyConnectedSlotsOnlyTests`（2）
- `Phase11W6cOutputDataFlagTests`（4）
- `Phase13FuncRatchetGuardTests`（1）
- `Phase13Step3HostRemovalGuardTests`（1）
- `Phase14NamespaceFolderGuardTests`（4）
- `Phase6Step10GroupTwoContractTests`（2）
- `Phase6Step7ContractExtensionTests`（3）
- `Phase7Step10aW3ResourcePrefixGuardTests`（1）
- `Phase7Step10bW1ControlTablesGuardTests`（2）
- `Phase7Step10bW2TrayIconAndColorGuardTests`（1）
- `Phase7Step10bW3ContractCleanupGuardTests`（2）
- `Phase7Step10bW4UsingMaxControllersGuardTests`（1）
- `Phase7Step10cW2SerializersGuardTests`（1）
- `Phase7Step10cW3AppSettingsSerializerGuardTests`（2）
- `Phase7Step10cW4LegacyProfileLoadGuardTests`（1）
- `Phase7Step10cW5LegacyProfileSaveGuardTests`（1）
- `Phase7Step10cW6ProfilePresetsGuardTests`（1）
- `Phase7Step10dW5GlobalRemovedGuardTests`（2）
- `Phase7Step10eW3AppHostGuardTests`（2）
- `Phase7Step10eW4OrphanMembersGuardTests`（1）
- `Phase7Step4AppHostGuardTests`（3）
- `Phase7Step4PathServiceGuardTests`（1）
- `Phase7Step4StartupBootstrapGuardTests`（3）
- `Phase7Step5Ds3GyroTriggerTests`（4）
- `Phase7Step5ServiceLocatorGuardTests`（3）
- `Phase7Step6OutputTypeOwnershipGuardTests`（2）
- `Phase7Step7MacroRuntimeStateGuardTests`（1）
- `Phase7Step7SyntheticInputStateGuardTests`（2）
- `Phase7Step8W1StateHolderInjectionGuardTests`（1）
- `Phase7Step8W3OutputKbmOwnershipGuardTests`（1）
- `Phase7Step9W2NoGlobalInMappingGuardTests`（2）
- `Phase7bW2W3SmallFixesGuardTests`（1）
- `Phase7cW1Stage3ProfileActionsSnapshotGuardTests`（1）
- `Phase8Step1ApplyProfileCleanupOrderGuardTests`（1）
- `Phase8Step4bW11LengthGuardTests`（3）
- `Phase8Step4bW2LoadFoundationTests`（3）
- `Phase8Step4bW9NameFunctionsTests`（2）
- `Phase8Step5W3PublishBindingsTests`（2）
- `Phase8Step6W1DisconnectCharacterizationTests`（6）
- `Phase8Step6W3SlotLifecycleTests`（2）
- `Phase8Step77aXboxGameDvrRemovalTests`（2）
- `Phase8Step78aFoundationTests`（3）
- `Phase8Step78dW2RemovedShiftWiringTests`（2）
- `Phase8Step78dW3RemovedShiftNoticeWiringTests`（3）
- `Phase8Step78eW2ControlsExtrasTests`（3）
- `Phase8Step78f2W2ControlsMouseTests`（1）
- `Phase8Step78f3W2ControlsAnalogMouseTests`（2）
- `Phase8Step78fW2ControlsPadTests`（2）
- `Phase8Step7AltTabRemovalGuardTests`（2）
- `Phase8Step7BuildCommitLogGuardTests`（2）
- `Phase8Step8W2RemovedGyroSwipeWiringTests`（2）
- `Phase8Step8W3RemovedGyroSwipeNoticeWiringTests`（3）
- `Phase9BMouseGuardTests`（2）
- `Phase9CMacroGuardTests`（1）
- `Phase9E6ConnectionGuardTests`（9）
- `Phase9E6ConnectorGuardTests`（4）
- `Phase9E6DisconnectGuardTests`（3）
- `Phase9E6LifecycleGuardTests`（5）
- `Phase9EHidHideGuardTests`（1）
- `Phase9EInputReportGuardTests`（3）
- `Phase9EOscGuardTests`（1）
- `Phase9EUdpMotionGuardTests`（2）
- `Phase9EVirtualPadGuardTests`（1）
- `Phase9F1StartupOrderGuardTests`（4）
- `Phase9F2IpcCommandTests`（7）
- `Phase9F2IpcQueryResultGuardTests`（1）
- `Phase9MapCustomGuardTests`（4）
- `Phase9SteeringW2ControllerConfigsGuardTests`（2）
- `Phase9SteeringW3OutputDeviceGuardTests`（2）
- `ProfileEditorBackendViewModelTests`（2）
- `ProfileEditorHoverIndexTests`（2）
- `ProfileEditorMappingDevTypeGuardTests`（1）
- `ProfileEditorTargetDeviceGuardTests`（9）
- `ProfileLegacyXmlRoundTripTests`（1）
- `R6PageScrollWheelTests`（2）
- `ReconnectProfilePerControllerTests`（3）
- `RecordBoxExtraButtonsVisibilityGuardTests`（1）
- `ReleaseSourceCentralizationGuardTests`（1）
- `SettingsViewModelGlobalReferenceGuardTests`（1）
- `Step10FourTwoGlobalReferenceGuardTests`（1）
- `Step10GroupOneGlobalReferenceGuardTests`（2）
- `Step10GroupThreeGlobalReferenceGuardTests`（2）
- `StickDeadZoneBindingGuardTests`（1）
- `ThemeResourceKeyGuardTests`（2）
- `XamlTestHelperTests`（6）

</details>
