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
