# Phase7 残存台帳: Service Locator と静的アクセス

作成日: 2026-10-17（Phase7-Step1 W4）  
基準: 現行コードの走査（コメントを除く）。`Phase6-Step11-Global-Residual-Ledger.md` の続き。  
ガード: `Step10FourGlobalRatchetGuardTests`（`Global.*`／`AppHost`／`Program.rootHub`）、`Phase7Step1ServiceLocatorRatchetGuardTests`（`ServiceProviderHolder`）。減る変更だけ許可。基準値を更新して固定する。

## 1. 集計（2026-10-17、Phase7-Step1 W3 まで）

- `Global.*`: 733（Phase6 完了時 747。W1 の `BackingStore` 孤立メソッド削除で 14 減）。
- `AppHost.GetService|Services`: 40。
- `Program.rootHub`: 22。
- `ServiceProviderHolder.Provider|GetRequiredService`: 23（今回、追跡を開始）。

## 2. Service Locator の分類

### 2.1 AppHost.GetService|Services（40）
- `MainWindow.xaml.cs` 17: コンストラクターで 14 件のサービスを解決。Composition Root 隣接の画面。→ Phase7-Step5
- `Mapping.cs` 6: 静的束縛（`profileSettings`、`profileApplication`、`VirtualKBM`）と 3 件の遅延解決。→ Phase7-Step7〜9（`Mapping` の instance 化）
- `App.xaml.cs` 4（Composition Root。`CreateHost` 自身を除く解決）、`ProfileEditor.xaml.cs` 4、`Global.cs` 4（`*Instance` のフォールバックと `Load`）。→ Phase7-Step4／5
- `DI/AppHost.cs` 2、`DI/Compat/AppHost.cs` 1（`Mapping.cs:86` 用の互換ラッパー）、`BackingStore.cs` 1、`ControlService.cs` 1。→ Step4／Step9／Step3

### 2.2 Program.rootHub（22）
- `App.xaml.cs` 12（代入、`LoggerHolder` の生成、起動時の呼び出し、終了処理）、`Global.cs` 6、`ServiceRegistration.cs` 1、`Mapping.cs` 1、`ProfileEditor.xaml.cs` 1、`DS4SixAxis.cs` 1。→ Phase7-Step5

### 2.3 ServiceProviderHolder（23。新規に追跡）
- `Actions/ActionManager.cs` 13、`ActionFactory.cs` 1、`DefaultKeyButtonActionControllerFactory.cs` 2、`KeyActionAdapter.cs` 1: **Phase8 の持ち越し**（Actions サブシステムは Phase8 で統合ディスパッチへ作り直す）。計 17。
- `Mapping.cs` 5、`App.xaml.cs` 1: → Phase7-Step5（`App` の `CleanShutdown`）と Step7〜9（`Mapping`）。計 6。
- 設定側の `SetProvider`（`DI/AppHost.cs` 3）は Composition Root の正規の使い方で、数えない。

## 3. 参照
- 調査: `Phase7-0-Survey.md` §1.5（F-A）。計画: `Phase7-Step1-Plan.md` W4。

## 4. Phase7-Step2 で追加した台帳（2026-10-17）

- 削除した DI 契約のメンバー: 51（`Phase7-Step2-Plan.md` §2・§8。ガード: `Phase7Step2RemovedContractMembersGuardTests`）。
- 「使うべきだが未接続」として TODO 付きで残した契約: `IEnvironmentService.UsingMaxControllers`（接続予定 Step10）、`IDs4DeviceRegistry.ReEnableDevice`（Step4）、`IOutputSlotService.LoadOutputSlots`／`SaveOutputSlots`（Step5）。温存: `IOutputSlotService.OutputSlotChanged`（Step6 で再判断）。
- 承認待ちの追加候補（Step2 計画書 §8 W6）: `IUdpServerService` 契約全体（未接続。接続を推奨）、`ISpecialActionRepository.GetAction`／`ActionExists`／`SaveActions`、`IPathService.ActionsPath`、`IDs4DeviceRegistry.IsHidHideInstalled`、`IProfileSettingsService.RumbleAutostopTime`、`IVirtualKBM.Version`（削除を推奨）。
- Phase8 へ移管: `Actions/` の未配線の抽象 12 メンバー（`Phase8-Unified-Trigger-Dispatch-Plan.md` に追記済み）。
- Step10 で再判断: `ProfileSubSettingBase.OnSubPropertyChanged`（購読者がいなくなった）、`ProfileApplicationService` の未使用の依存 `IDeviceStateService`。

## 5. Phase7-Step3 で更新した台帳（2026-10-17）

- `Global.*`: 723 → **516**（W1 −8、W2 −105、W3〜W5 −94。基準値は `Step10FourGlobalRatchetGuardTests`）。`AppHost` 40、`Program.rootHub` 22、`ServiceProviderHolder` 23 は変わらない。
- `BackingStore` を DI のシングルトンとして登録（実体は `Global.store` と同一）。`ServiceRegistration`、`ProfileXmlStore`、`ProfileRepository` から `Global.store` の直接参照を除いた（`ServiceRegistration` の登録の 1 件だけが Composition Root として残る）。
- 削除した `Global` の転送メンバー: 76 名（W2 34、W3 21、W4 5、W5 16）。ガード: `Phase7Step3RemovedGlobalForwardersGuardTests`。
- サービス側だけ `BackingStore` に切り替え、`Global` のメンバーは他の呼び出し元のために残したもの（Q5）: `Notifications`（`MainWindow`）、`UseLang`、`LogMinLevel`、`LogMaxArchiveFiles`、`UseCurrentTheme`、`UseMoonlight`、`UseAdvancedMoonlight`、`DeviceOptions`、`getOSCServerPortNum`（`ControlService`）、`LastVersionChecked`（読み取り専用にした。`Changelog` が読む）。
- Step3c（別計画書）へ持ち越し: `ProfilePresetLoader` の 10 名と `ProfileRepository.LoadBlankProfile`（`tempprofileDistance` ほかの状態と絡む）、`ProfilePath`／`OlderProfilePath`／`SelectedProfile`（`Global.ApplyProfile` が内部で使う）、`SelectedProfileChanged` ほかのイベント、`LoadTempProfile`、`ProfileApplicationService`／`ProfileSlotApplier` の `ApplyProfile` 系、`ProfileChangedNotification`（`ProfileSettingsServiceInstance` 経由）。
- Step4 へ: `firstRun`／`runHotPlug`、`PathService`／`EnvironmentService` が参照する環境・パスの実体、`SetCulture`。
- Step5 へ: `ProfileXmlStore.Load/SaveControllerConfigs`（`Program.rootHub` を走査する）、`MainWindow` などの Service Locator。
- Step6 へ: `OutputSlotService` の `outDevTypeTemp`／`activeOutDevType`、`OutputKBMHandlerAdapter` の `outputKBMHandler`。
- 残る Pure DI 違反のフォールバック: `Global.cs` の `new ProfileXmlStore(m_Config)`／`new AppNotificationService(m_Config)`（`*Instance` 系の最後のフォールバック。Step4／Step5 で削除）。

## 6. Phase7-Step3c で更新した台帳（2026-10-17）

- `Global.*`: 516 → **488**（W2 −11、W3 −6、W4 −8、W5 −3。基準値は `Step10FourGlobalRatchetGuardTests`）。`AppHost` 40、`Program.rootHub` 22、`ServiceProviderHolder` 23 は変わらない。Phase7 開始時（733）からの累計は −245。
- 削除した `Global` のメンバー: 24 名（プリセット 10、`loggedInvalidActions`、プロファイル適用 12、`ProfileChangedNotification`）。契約からも、重複していた `IProfileRepository.LoadBlankProfile` を削除。ガード: `Phase7Step3RemovedGlobalForwardersGuardTests`（Step3 と合わせて 100 名）。
- 適用の本体は `ProfileApplicationService`（`ApplyCore`／`LoadNormalProfile`／`CompleteProfileApplication`、入口は `ApplyProfile`／`ApplyFromAction`／`ApplyProfileWithoutHalt`）に移った。`ProfileRepository` が `LoadTempProfile` と `SelectedProfileChanged`（インスタンスイベント）を持つ。
- 静的呼び出しのまま残るもの（適用の本体の中）: `ActionManager.ClearAllEntries`／`ClearDeviceState`／`PreallocateForProfileApply`（→ Phase8）、`Mapping.ClearKeyButtonControllersForDevice`（→ Phase7-Step7〜9）。
- `Global` に残る、プロファイルに関係する窓口: `tempprofileDistance`／`useTempProfile`／`DistanceProfiles`（`DS4LightBar`／`Mapping` が `using static` で参照。→ Step7〜9）、`ProfileSettingsServiceInstance`／`ProfileXmlStoreInstance`（フォールバックの Service Locator。→ Step4／Step5）。
- 実機確認: U-13（未。ユーザー判断で先送り）。


## 7. Phase7-Step4 で更新した台帳（2026-10-17）

- `Global.*`: 488 → **452**（W1 −17、W3 −19。基準値は `Step10FourGlobalRatchetGuardTests`）。`AppHost` 40 → **39**（W2: `Global.Load` の削除）、`Program.rootHub` 22 → **21**（基準値 `Global.cs` の 6 が過大で、実数 5 に直した。コードの変更ではない）、`ServiceProviderHolder` 23 は変わらない。Phase7 開始時（733）からの累計は −281。
- 削除した `Global` のメンバー: 環境の調べ物とその状態 27 名（W1。`EnvironmentService` が処理と状態を持つ）、`Load`（W2）、保存場所・実行ファイルの実体と処理 8 名（W3: `FindConfigLocation`／`SaveWhere`／`SaveDefault`／`firstRun`／`multisavespots`／`appDataPpath`／`runHotPlug`／`fileVersion`）。ガード: `Phase7Step4EnvironmentProbesGuardTests`、`Phase7Step4PathServiceGuardTests`、`Phase7Step4AppHostGuardTests`、`Phase7Step4StartupBootstrapGuardTests`。
- 設計上の逸脱（計画書 §2 との差）: `PathService`／`EnvironmentService` の状態は、インスタンスではなく **static** で持つ。理由は、下位層（`BackingStore`／`LoggerHolder`）、既存テスト（`Global.appdatapath` を書き換えるもの約 46 件）、起動前の `App` が、同じプロセス全体の値を共有するため。ブートストラップのインスタンスは作り、`CreateHost` へ渡して同じインスタンスをコンテナへ登録する（インスタンスはどれも同じ状態を見る）。
- `Global` に残した窓口（読み書き）: `appdatapath`／`exedirpath`／`exelocation`／`exeversion`（`PathService` の状態を指す。`BackingStore`／`LoggerHolder`／`Changelog` ほかが静的に読む。→ Step10 で `BackingStore` の永続化を切り出すときに、`IPathService` の注入に置き換えて廃止）。
- `Global` に残した環境系: `IsAdministrator`／`AdminNeeded`／`GetInstanceIdFromDevicePath`／`CheckIfVirtualDevice`（`BackingStore`／`Util`／`DS4Devices` が呼ぶ。→ Step10）。
- `AppHost`: `CreateHost(string[])` と `CreateHost()` の未使用引数を削除。`Dispose` を `CleanShutdown` で呼ぶ（Dispose の後は暗黙にホストを作り直さない）。`-driverinstall` 分岐は、設定の読込の前にホストを明示的に作る（K7-1 の残存経路の解消）。`CreationNote` は残す。
- `IDs4DeviceRegistry.ReEnableDevice`: 接続した（`-reenableDevice` 分岐が契約経由で呼ぶ）。
- `WelcomeDialog` の `loadConfig` 引数と、中の `FindConfigLocation`／`Load` の重複呼び出しを削除（呼び出し元が直前に同じ処理をしている）。
- 既存の課題（回帰ではない。修正は未承認）: 起動直後のログローテーションとログの最小レベルは、`Profiles.xml` を読む前に参照されるため、`BackingStore` の既定値になる（2025-12-14 の `25d60ecb`／`c1cb918b` の機能から。DI 移行前から）。計画書 Q7＝A により記録のみ。直す場合は、ログ設定だけを先に読む手順を足す（起動順序の変更になる）。
- 持ち越し: `Global.cs` の `*Instance` フォールバック（`new ProfileXmlStore(m_Config)`／`new AppNotificationService(m_Config)` ほか）は Step5 で Service Locator とあわせて削除する（Step4 では触っていない）。`AppHost.Dispose()` は呼ぶようになったが、`Program.rootHub` と Service Locator は Step5。
- 実機確認: U-14（未）。

## 8. Phase7-Step5 で更新した台帳（2026-10-17）

- `Program.rootHub`: 21 → **0**（`Program` クラスと `Program.cs` を削除）。`AppHost.GetService|Services`: 39 → **11**。`ServiceProviderHolder`: 23 → **22**。`Global.*`: 452 → **446**。Phase7 開始時（733）からの累計は −287。基準値は `Step10FourGlobalRatchetGuardTests`（合計 0／11／446）と `Phase7Step1ServiceLocatorRatchetGuardTests`。
- 残る `AppHost.GetService`（11）: `Mapping.cs` 6（静的なフィールド初期化子ほか。→ Step7〜9）、`DI/Compat/AppHost.cs` 1（互換ラッパー。→ Step9）、`DI/AppHost.cs` 2（自身のログ文）、`Global.cs` 1（`ProfileSettingsServiceInstance`。→ Step7〜9／Step10）、`App.xaml.cs` 1（`ResolvePostHostService`。Composition Root の解決の窓口）。
- 残る `ServiceProviderHolder`（22）: `Actions/` 17（→ Phase8）、`Mapping.cs` 5（→ Step7〜9）。
- 新設: `IMainWindowFactory`／`MainWindowFactory`（`MainWindow` の 17 件の Locator を引数化）、`IEditorWindowFactory.CreateProfileEditor`（`ProfileEditor` の 4 件を引数化）、`AppLogger.NotificationService`（静的ロガーへ通知サービスを設定）、`BackingStore.ProcessInspector`（プロパティ注入）、`ControlService` のコンストラクター引数 `IElevatedProcessLauncher`、`DS4SixAxis.GyroControlsTriggerActive`。
- 削除: `Program.rootHub`、`Global.SaveControllerConfigs`／`LoadControllerConfigs`、`Global.ProfileXmlStoreInstance`／`NotificationServiceInstance`、`ControlService.LoadPermanentSlotsConfig`。接続: `IOutputSlotService.LoadOutputSlots`／`SaveOutputSlots`（Step2 の「接続」の判断を完了）。
- 不具合の是正: DS3 コントローラーのジャイロトリガー経路（`DS4SixAxis._controlService` が常に null。コミット `78ae8687` からの DI 移行による回帰）。移行前と同じ結果を返す形に直した（`Phase7-Step5-Plan.md` §1.6、Q3＝A）。DS3 の機材での実機確認は難しい可能性がある（U-15 の注記）。
- 持ち越し: `Global.ProfileSettingsServiceInstance` とそのフォールバック（→ Step7〜9／Step10）。`AppHost.CreationNote` と暗黙構築の経路（→ Step9 で互換 `AppHost` と同時に判断）。`MainWindow` のコンストラクター引数は 18 個と多い（→ Step10 で、画面ごとの責務の整理とあわせて見直す）。
- 実機確認: U-15（未）。

## 9. Phase7-Step6 で更新した台帳（2026-10-17）

- `Global.*`: 446 → **438**（`BackingStore.cs` 166 → 159、`OutputSlotService.cs` 2 → 1）。`AppHost.GetService|Services` 11、`Program.rootHub` 0、`ServiceProviderHolder` 22 は変更なし。Phase7 開始時（733）からの累計は −295。基準値は `Step10FourGlobalRatchetGuardTests`（合計 0／11／438）。
- 移管: `Global.outDevTypeTemp`（編集画面の未確定の出力種別）と `Global.activeOutDevType`（スロットごとの接続中の種別）を削除し、実体を `OutputSlotService` の readonly フィールドへ移した（Q1＝A、Q3＝A）。`BackingStore.ActiveOutDevType`（プロパティ。既定は全スロット None）が、`IOutputSlotService` の登録時に同じ配列を受け取る（Q2＝A）。`BackingStore` の読み取り 8 件を置き換えた。
- 注意（循環）: `BackingStore` の登録の中で `IOutputSlotService` を解決してはならない（`IOutputSlotStore` → `IPathService` → `BackingStore` と循環し、実行時にスタックオーバーフローになる）。W1 で一度発生し、受け渡しを `IOutputSlotService` の登録側へ移して解消した。`Phase7Step6OutputTypeOwnershipGuardTests` が固定している。
- 持ち越し: `Global.outputKBMHandler`／`outputKBMMapping`（`OutputKBMHandlerAdapter`、`OutputKBMHandlerLifecycle`、`SyntheticDispatcher`、`Mapping` のフォールバック）は、Q4＝A により Step7〜9（`Mapping` の instance 化）で扱う。`IOutputSlotService.OutputSlotChanged`（購読者 0 件）は、Q5＝A により温存し、TODO の再判断先を Step10 へ更新した。`BackingStore.ActiveOutDevType` のコンストラクター注入は Step10。
- 実機確認: U-16（未）。

## 10. Phase7-Step7 で更新した台帳（2026-10-17）

- `Global.*`: 438 → **432**（`Mapping.cs` 52 → 43、新ファイル `SyntheticInputState.cs`／`MacroRuntimeState.cs`／`StickMotionState.cs` が各 1）。`AppHost.GetService|Services` 11、`Program.rootHub` 0、`ServiceProviderHolder` 22 は変更なし。Phase7 開始時（733）からの累計は −301。基準値は `Step10FourGlobalRatchetGuardTests`（合計 0／11／432）。
- 新設（状態の保持クラス。いずれも `Shared`＝暫定の静的な窓口＋DI の Singleton 登録。Step8 で `Shared` を削除）: `SyntheticInputState`（`globalState`／`deviceState`／`syncStateLock`）、`MacroRuntimeState`（`macrodone`／`macroDispatchInFlight`／`macroDispatchLock`／`macroControl`／`macroCount`／`macroTaskQueue`）、`StickMotionState`（`gyroStickX`／`gyroStickY`／`mapStickActionData`／`flickMappingData`／`l2/r2TwoStageMappingData`／`wheelFilters`／`deltaAccelProcessors`）。`Mapping` の同名メンバーは、保持クラスへの転送（状態は持たない。TODO は Step8）。
- トップレベルへ出した型（Q5＝A。中身は変えていない）: `SyntheticState`、`PostMapStickData`、`FlickStickMappingData`、`TwoStageTriggerMappingData`、`DeltaSettingsProcessor`、`DeltaSettingsProcessorGroup`、`Click`。
- 逆参照の解消（Q4＝A）: `BackingStore` から `Mapping` への参照を 0 にした。`BackingStore.ActionsLoaded` イベントを `LoadActions` の末尾で発行し、`ServiceRegistration` が購読者 `ActionsLoadedRuntimeReset.Reset`（順序とログは従来どおり）を登録する。
- 特性化テスト（Q1＝A）: `MapCustom`（素通し、ボタン→ボタン、軸の向き→ボタン、トリガー→ボタン、ボタン→マウスボタン）、`Commit`（左・右・中ボタンの押下／保持／解放、Sync）、`MapClick`、`getBool*Mapping`、`HandleDeviceDisconnect`。範囲外: ボタン→キー（`ActionManager` 経由。Phase8）、ホイール（実時間 150ms の連続送出）、マクロ、ジャイロ、フリックスティック、タッチ。
- 計画との差: `ControlService` と `Mouse` は、保持クラスを注入ではなく `Shared` から得る（`ControlService` のコンストラクター変更によるテストの改修を避けるため。Step8 で `Mapping` の instance 化と同時に注入へ）。`stickValueHistory`／`lastStickAxisValues`／`outSqrStk`／`lastWheelGyroValues` など `Mapping` 内部だけで使う状態は、Step8 まで `Mapping` に残した。
- 持ち越し: `Mapping` の `profileSettings`／`profileApplication`／`VirtualKBM` の束縛、`AppHost` 6・`ServiceProviderHolder` 5、`Global.outputKBMHandler`／`outputKBMMapping`（Q6＝A）、`Shared` の削除（Step8）。互換 `AppHost`、`Global` 定数参照 43、K3（`macroDispatchInFlight` の再設計）（Step9）。`Actions/` の `ServiceProviderHolder` 17（Phase8）。
- 実機確認: U-17（未）。
