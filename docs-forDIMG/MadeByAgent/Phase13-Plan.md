# Phase13 個別計画書: `ControlService` の整理

作成日: 2026-11-04
状態: **承認済み（2026-11-04）**（論点 Q1〜Q8 の回答を反映済み）
対象ブランチ: `Phase13`（`master` から作成済み。Q4）
上位: `Phase11-and-Beyond-Roadmap.md` §3（Phase13）、§5（再照合）、§8.3（モデル図の更新）
入力: `FurtherImprovements/01`（3.1）、`02`（§3）、`03`（Phase C の C-1〜C-3、§5 品質ゲート）、`Phase9-Deferred-Ledger.md`（F-1c・F-2c・F-5c）、`Phase7-Residual-Ledger.md` §17.3、`Phase11-Completion-Report.md` §6、`Model-Diagram/01〜04`

> この文書は Phase 全体の計画書である。規約 §4 に従い、各 Step は着手前に `Phase13-StepN-Plan.md`（個別計画書）を作り、承認を得てから実装する。

---

## 1. 目的と範囲

`ControlService` が、(a) 多すぎるコンストラクター引数、(b) 循環を避けるための `Func<T>`、(c) `ServiceProviderHolder`（Service Locator）の 3 点で「神クラス」の名残を持っている。これを、機能・ログ・安全弁を変えずに整理する。

範囲（ユーザー指示 + 決定）:

1. `ControlService` のコンストラクター引数の整理（実測 34）
2. `Func<T>` の削減（`ServiceRegistration.cs` と、各サービスの引数）
3. `ServiceProviderHolder` の撤廃
4. F-5c（`running`／`suspending` の転送）、F-1c、F-2c
5. トリガー系 4 クラスの、`DebouncingMs` を使わなくなった未使用コンストラクター引数の整理

範囲外: 名前空間・フォルダの再編（Phase14）、ログの文言・レベルの整理（Phase13b）、`Mapping.MapCustom` や `IProfileXmlStore.LoadProfileXml` が `ControlService` 型の引数を取る構造そのもの（§9 のリスク R3 を参照）。

---

## 2. 現状の再照合（2026-11-04 の実測）

ロードマップ §5 が「着手前に再照合する」とした項目。**数値はすべてソースから数えた。ロードマップ・FI 文書と食い違うものは「差」の列に示す。**

### 2.1 `ControlService` のコンストラクター引数: 34（FI 文書と一致）

`ControlService.cs`（1,077 行）の引数 34 個の内訳（用途別）。

| 区分 | 引数 | 数 |
|---|---|---|
| 基盤・設定 | `cmdParser`、`deviceRegistry`、`profileSettings`、`appSettings`、`environmentService`、`pathService`、`appearanceSettings` | 7 |
| プロファイル | `profileRepository`、`profileXmlStore`、`profileSlotApplier`、`profileActionProvider`、`specialActionRepository` | 5 |
| 入力評価・状態 | `mapping`、`stickMotion`、`syntheticInput`、`deviceStateService` | 4 |
| 出力 | `outputSlotServiceFactory`（`Func`）、`virtualPad`、`virtualKBM`、`kbmLifecycle`、`oscService`、`udpMotion`、`hidHideService` | 7 |
| `On_Report` の部品 | `latencyMonitor`、`touchpadToggle`、`activeInputProbe` | 3 |
| 接続系 | `connectionProfileSelector`、`deviceConnectionSetup`、`deviceModules`、`joyConLinker`、`deviceDisconnect`、`deviceConnector`、`lifecycle` | 7 |
| その他 | `elevatedProcessLauncher` | 1 |

フィールドの使用回数（`ControlService.cs` 内）から分かったこと:

- **保持するだけで一度も使われていない引数が 5 個**: `profileSlotApplier`、`syntheticInput`、`joyConLinker`、`connectionProfileSelector`、`deviceConnector`。いずれも他のサービス（`ConnectionProfileSelector`、`DeviceDisconnectHandler`、`DeviceConnector`、`ServiceLifecycle` ほか）が自分で受けているので、`ControlService` が持つ必要がない（Phase9 の切り出しで残った）。
- `profileXmlStore` は、内部アクセサ `ProfileXmlStore` に公開するためだけに保持している。外部の利用者は検索で 0 件（Step2 の最初に再確認する）。
- 使用 1 回だけ: `deviceStateService`（`RememberProfile`）、`environmentService`（コンストラクターで `ControllerSlotLimit` を読むだけ）、`elevatedProcessLauncher`（昇格要求 1 か所）。
- `cmdParser` は `InitOutputKBMHandler`／`RefreshOutputKBMHandler`（KBM 出力の初期化）のためだけに使う。
- `specialActionRepository`／`profileActionProvider`／`profileSettings` は、`Mapping.cs` へ `ctrl` 経由で渡す internal アクセサ（Phase6-Step3-2 の S3 方針）。

### 2.2 `Func<T>`

| 項目 | ロードマップの記述 | 実測 | 差 |
|---|---|---|---|
| `ServiceRegistration.cs` | 14 件 | `Func<…>` を含む行は 14。うち**登録は 11 件**、使用 2 行（83、90）、コメント 1 行（186） | 14 → **登録 11** |
| `ControlService` 内 | 2 件 | `Func<DI.IOutputSlotService>` の**1 種類**（フィールドと引数の 2 行） | 2 行 = 実質 1 件 |
| そのほか | — | `Func` を引数に取るサービス・クラスは 50 ファイル中、DI で循環を避ける用途のものが下表の 25 クラス前後（それ以外は `Func<DateTime>` など別用途） | — |

登録 11 件と、利用者・循環の理由・解消の方向:

| # | `Func<T>` | 利用者（引数に取る型） | 循環・遅延の理由 | 解消の方向（担当 Step） |
|---|---|---|---|---|
| 1 | `Func<ControlService>` | `ProfileSettingsService`、`ProfileRepository`、`ProfileApplicationService`、`AutoProfileService`、`EditorWindowFactory`、`MainWindowFactory`、`ViewModelFactory`、`DefaultBatteryCheckPort`、`DefaultControlExtrasPort`（9） | `ControlService` → `IProfileSettingsService` → `ControlService`。さらに、`ControlService` は起動引数（`CreateHost(config, parser)` の後）を保持するため、ホスト構築前に作ってはならない | 用途ごとに狭いポートへ置換（Step6） |
| 2 | `Func<IOutputSlotService>` | `ControlService`、`DeviceConnector`、`DeviceDisconnectHandler`、`VirtualPadService`（4） | `VirtualPadService` ⇄ `OutputSlotService` の相互参照 | 配列（`ActiveOutDevType`）の持ち主を分けて一方向にする（Step7） |
| 3 | `Func<IVirtualPadService>` | `OutputSlotService`（1） | 同上（#2 の対） | Step7 |
| 4 | `Func<IVirtualPadHost>` | `VirtualPadService`（1） | `ControlService` が `IVirtualPadHost` を実装（`SetDevRumble`、`QueueOnEventThread`） | 振動の調整とイベントスレッドを独立のサービスへ（Step3） |
| 5 | `Func<IServiceLifecycleHost>` | `ServiceLifecycle`（1） | `ControlService` が実装（`InvokeOnEventThread`、`InitializeOutputKbmHandler`） | イベントスレッドと KBM 初期化を独立のサービスへ（Step3） |
| 6 | `Func<IUdpMotionHost>` | `UdpMotionService`（1） | `ControlService` が実装（`GetPadDetailForIdx`、`GetCurrentState`、`GetTempState`、`QueueOnEventThread`）。**スロットの配列に依存** | スロット状態の持ち主を新設（Step5） |
| 7 | `Func<IDeviceSlotHost>` | `ServiceLifecycle`、`DeviceConnector`、`DeviceDisconnectHandler`（3） | `ControlService` が配列（`DS4Controllers`／`CurrentState`／`PreviousState`／`touchPad`／`slotManager`）を所有 | Step5 |
| 8 | `Func<IDeviceStateAccessor>` | `VirtualPadService`、`MacroStepExecutor`、`DefaultBatteryCheckPort`、`DefaultControllerDisconnectPort`、`DefaultGyroCalibrationPort`（5） | `IDeviceStateAccessor`（`GetController`）の実体が `ControlService` | Step5（持ち主が新設のスロット状態になる） |
| 9 | `Func<IMapping>` | `ActionBindingBuilder`、`DefaultMacroStringPlayer`、`ProfileSwitchAction`、`SpecialActionOutputFactory`（4） | ホスト構築時に `Mapping` とその依存を作らない（Phase5-Plan §5.4）。`Mapping` ← `IDeviceStateAccessor`（＝ `ControlService`） | Step5 の後に、循環が消えたかを再判定（Step8） |
| 10 | `Func<IAutoProfileService>` | `MainWindowFactory`、`ViewModelFactory`（2） | `AutoProfileService` ← `Func<ControlService>` | Step6 の後に再判定（Step8） |
| 11 | `Func<IActionBindingBuilder>` | `ProfileApplicationService`（1。ほかはテスト） | `ActionBindingBuilder` ← `Func<IMapping>` ほか | Step8 |

補足: `OutputSlotService` のコンストラクター第 1 引数 `() => …VirtualPadService.OutputSlotManager` も、型は `Func<OutputSlotManager>` の遅延（Step7 で一緒に扱う）。`ServiceRegistration.cs` のコメント（185〜186 行）は `DefaultActionManager`／`DefaultMacroPlayer`／`Mapping` が `Func<IMapping>`／`Func<IDeviceStateAccessor>` を受けると書くが、検索では利用者に現れない。Step8 で現状を確認してコメントを直す。

### 2.3 `ServiceProviderHolder`

| 項目 | ロードマップの記述 | 実測 | 差 |
|---|---|---|---|
| 参照ファイル | 6 ファイル・13 件 | 6 ファイルのうち**コードがあるのは 3 つ**。`App.xaml.cs` と `KeyButtonControllerRegistry.cs` はコメントのみ | 13 → **本番 10 + `SetProvider` 2** |
| ラチェットの対象（`.Provider`／`.GetRequiredService`） | — | `ActionManager.cs` 8、`DefaultKeyButtonActionControllerFactory.cs` 2 = **10**（`Phase7Step1ServiceLocatorRatchetGuardTests` の基準値と一致） | — |
| `SetProvider` | — | `AppHost.cs` の 2 か所（`CreateHost()`、`CreateHost(config, parser)`） | — |
| テスト | — | `SetProvider`／`Provider` を使うテストは 5 ファイル（`ToggleAndToggledOnTests`、`ManagedActionManagerRegistrationTests`、ラチェット自身、`StandaloneTests` の 2）。`SetProvider` は 29 か所。加えて **133 ファイルが `[Collection("ServiceProviderHolder")]`**（xUnit の直列化用の名前。型への参照ではない） | — |

10 件の中身（すべて Actions の「静的な橋渡し」）:

- `ActionManager`（静的クラス、311 行）の 8 件: `GetStateFor`、`ClearToggledOnForKey`、`ClearAllToggledOn`、`ClearAllEntries`、`ClearDeviceState`、`SetToggledOn`、`GetOrCreateControllerForAction`、`PreallocateForProfileApply`。いずれも `IManagedActionManager`（実体は `DefaultActionManager`）または `IKeyButtonControllerRegistry`／`IProfileActionProvider` に委譲し、無いときは静的な辞書へフォールバックする。
- `DefaultKeyButtonActionControllerFactory` の 2 件: 生成したコントローラーを `IControllerRegistry` へ登録するために取得（コンストラクター注入にできる）。
- 静的 `ActionManager` の呼び出し元は約 12 か所: `KeyAction`、`KeyButtonActionController`（静的イベント `ToggledOnChanged` の購読 2 か所を含む）、`MacroAction`、`ButtonSpecialActionOutput`、`Mapping`（`ToggledOnChanged` の購読）、`MacroPlayback`、`ProfileApplicationService`、`SpecialActionRepository`、`SpecialActionsXmlSerializer`、`ActionsLoadedRuntimeReset`、`SyntheticInputState`。

### 2.4 `running`／`suspending`（F-5c）

- `ControlService.cs` 54〜58 行と 101 行: `running` と `suspending` は `_lifecycle`（`IServiceLifecycle`）への転送。
- `running` の利用者: `AutoProfileService`（4 か所）、`App.xaml.cs`（1）、`MainWindow.xaml.cs`（8）、`MainWindow.Ipc.cs`（4）、`TrayIconViewModel`（1）。`suspending` の利用者: `MainWindow`（2。電源イベント）。台帳の「4 つの利用者」と一致。
- 4 つの利用者はいずれも `Start`／`Stop`／`slotManager` などのためにも `ControlService` を持つ（台帳どおり）。

### 2.5 トリガー系 4 クラス

| クラス | `new` の呼び出し（本番） | テスト | 残っている未使用引数 |
|---|---|---|---|
| `ControlButtonInputAction` | `ControlBindingBuilder` 5、`AssignmentBindingBuilder` 1 | 11 か所・4 ファイル | `profileSettings`（任意引数） |
| `ComboTriggerInputAction` | `AssignmentBindingBuilder` 2、`MultiTapInputAction` 1 | 10 か所・5 ファイル | `profileSettings`（null 検査のみ） |
| `MultiTapInputAction` | `AssignmentBindingBuilder` 1、`ControlMultiTapInputAction` 1 | 1 か所・1 ファイル | `profileSettings`（null 検査のみ） |
| `TwoStageTriggerProcessor` | なし（DI 登録） | 3 か所・2 ファイル | `profileSettings`（null 検査のみ） |

`ControlMultiTapInputAction` も、転送のために `profileSettings` を受けている（Step1 で連動して整理する）。

### 2.6 そのほかの実測

- `ControlService` は 5 つのホスト IF（`IDeviceStateAccessor`、`IUdpMotionHost`、`IVirtualPadHost`、`IDeviceSlotHost`、`IServiceLifecycleHost`）を実装している。**`Func` の大半の根はここにある**（§2.2 の #4〜#8）。
- イベントスレッド（`eventDispatchThread`／`eventDispatcher`。スレッド名 "ControlService Events"、`BelowNormal`、`IsBackground`）は `ControlService` が所有し、`QueueOnEventThread`、`InvokeOnEventThread`、`EventDispatcher`（公開プロパティ）、`ShutDown` の破棄が使う。
- テスト: Phase11 完了時点で Actions 2,837 件 + Standalone 11 件。着手時に再測定する。
- 起動順の注意: `ControlService` は `CreateHost(config, parser)` の後でなければ解決してはならない（`ServiceRegistration.cs` のコメント）。

---

## 3. 設計方針（FI の「3 大コーディネーター」案との比較と提案）

### 3.1 FI 案（02 §3、03 Phase C）

`IInputDeviceCoordinator`／`IVirtualOutputCoordinator`／`IProfileStateCoordinator` を新設し、`ControlService` を 4 引数に集約する。

### 3.2 なぜそのまま採らないか（根拠）

1. **FI は Phase9 より前の診断。** Phase9 で `ControlService` は約 3,400 行から 1,077 行へ分割済みで、FI がコーディネーターに担わせたい責務は既に別のサービスに移っている（接続: `DeviceConnector`／`ConnectionProfileSelector`／`JoyConLinker`／`DeviceConnectionSetup`、切断: `DeviceDisconnectHandler`、開始・停止: `ServiceLifecycle`、出力: `VirtualPadService`／`OutputSlotService`／`UdpMotionService`／`OscService`／`HidHideService`）。コーディネーターを足すと、**既存サービスの上にもう 1 層積む**だけで、型と配線が増える。
2. FI の C-2 が言う「双方向参照のイベント化」は、実測では**イベントでなく、ホスト IF の実体を `ControlService` から外す**ことで解けるものが大半（振動・イベントスレッド・KBM 初期化は状態を持たず、配列の所有だけが本当の状態）。
3. FI の「4 引数」は、薄い転送メンバー（`ChangeUDPStatus` など）を持ち続ける限り達成できない。引数を減らす本筋は、**UI が各サービスを直接使う**こと。

### 3.3 提案（Q1 で承認された「見直し案: 逆依存の根を断つ」）

1. 未使用引数を削除する（34 → 28）。
2. `ControlService` が実装するホスト IF の実体を、状態を持たないものから外す（イベントスレッド、振動の調整、KBM 出力の初期化）。これで `Func<IVirtualPadHost>`／`Func<IServiceLifecycleHost>` が消える。
3. スロットの配列（`IDeviceSlotHost`／`IUdpMotionHost`／`IDeviceStateAccessor`）の持ち主を新設の型に移す。これがホットパス（`On_Report`）に触れるため、**ゲート付きの独立 Step**（Q5）にする。
4. `Func<ControlService>` の 9 利用者を、狭いポートか直接注入へ置き換える。
5. `ServiceProviderHolder` を撤廃する（Q2: 静的 `ActionManager` を廃止して注入へ）。
6. 転送メンバーを呼び出し側から各サービスへ切り替え、引数を努力目標 20 以下にする（Q6）。

### 3.4 訂正（Q5 の回答への補足）

Q5 では「`IUdpMotionHost`／`IVirtualPadHost`／`IServiceLifecycleHost` の 3 つは低リスク」と案内したが、調査の結果、**`IUdpMotionHost` はスロットの配列（`GetCurrentState`／`GetTempState`／`GetPadDetailForIdx`）に依存する**ため、配列の持ち主を変える Step と一緒に扱うのが正しい。Q5 の趣旨（配列に触れる部分を後ろの独立 Step にする）に沿って、**低リスク側は `IVirtualPadHost` と `IServiceLifecycleHost` の 2 つ**とし、`IUdpMotionHost` は配列側（Step5）へ回す。

---

## 4. Step 分割案

依存の順に並べる。各 Step は、個別計画書 → 単体テスト → `Debug ALL`（警告 0・エラー 0）→ ユーザーの承認 → コミット（ユーザーが「コミットして」と言ったときだけ）の順。

| Step | 内容 | 規模 | 実機に触れるか | 備考 |
|---|---|---|---|---|
| **1** | トリガー系 4 クラス（＋`ControlMultiTapInputAction`）の未使用 `profileSettings` 引数の削除。呼び出し側（`ControlBindingBuilder` 5、`AssignmentBindingBuilder` 4、`ControlMultiTapInputAction`）とテスト（25 か所・8 ファイル程度）を同時に更新 | S | いいえ | 純粋な署名変更。DI 登録（`TwoStageTriggerProcessor`）の配線の確認を含む。各クラスの「引数を残した理由」コメントを削除する |
| **2** | `ControlService` の未使用引数 5 個（`profileSlotApplier`、`syntheticInput`、`joyConLinker`、`connectionProfileSelector`、`deviceConnector`）と、`ProfileXmlStore` アクセサのためだけの `profileXmlStore` の削除。34 → 28 | S | いいえ | **解決グラフの検査**を先に行う: 削除した型が、他のサービス経由で従来どおり生成されることをテストで確認する（生成のタイミングが変わる型がないか） |
| **3** | イベントスレッドの独立（案: `IEventThreadDispatcher`）、振動の調整（`SetDevRumble`／`setRumble`。案: `IDeviceRumbleService`）、KBM 出力の初期化（`InitOutputKBMHandler`／`RefreshOutputKBMHandler`。案: `IOutputKbmInitializer`）。`IVirtualPadHost`／`IServiceLifecycleHost` を廃止し、`Func<IVirtualPadHost>`／`Func<IServiceLifecycleHost>` を撤廃 | M | **接続・切断**（イベントスレッドの破棄順、`Stop` の KBM 解放） | スレッド名・優先度・`IsBackground`・`ShutDown` の破棄順を変えない。`ControlService.EventDispatcher`（公開）の利用者を調べ、新サービス経由に切り替える |
| **4** | `ServiceProviderHolder` の撤廃（§5.2）。4a: `DefaultKeyButtonActionControllerFactory` へ `IControllerRegistry` を注入。4b: 静的 `ActionManager` を廃止し、約 12 の呼び出し元へ `IManagedActionManager` を注入（静的イベント `ToggledOnChanged` もインスタンスへ）。4c: `AppHost` の `SetProvider` 削除、`ServiceProviderHolder` の削除、ラチェットの更新、テスト 5 ファイルの更新 | L | **プロファイル適用**（トグル状態のリセット、マクロ、Key／Button の切替） | 静的な辞書のフォールバック（`actions`）を使っているテストがないか、先に洗い出す。4a〜4c を別コミットにできる粒度で |
| **5** | **（ゲート付き。Q5）** スロット状態の持ち主の新設（案: `IDeviceSlotState`／`DeviceSlotState`）。`DS4Controllers`／`CurrentState`／`PreviousState`／`touchPad`／`slotManager`／`ActiveControllers` と `GetController` を `ControlService` から移し、`IDeviceSlotHost`／`IUdpMotionHost`／`IDeviceStateAccessor` の実装者を変更。`Func<IDeviceSlotHost>`／`Func<IUdpMotionHost>`／`Func<IDeviceStateAccessor>` を撤廃 | L | **すべて**（`On_Report` のホットパス、接続・切断、UDP モーション） | **着手前に、Phase9 方式の A/B 計測**（`On_Report` あたりのアロケーション 0、遅延が悪化しない）を設計し、結果を本書へ追記して**再承認**を得る。悪化したら Step5 を取りやめ、§9 の R1 のとおり Func を残す |
| **6** | `Func<ControlService>` の 9 利用者の置き換え。(a) 狭い IF で足りるもの（`ProfileSettingsService`＝`GetController`、`DefaultBatteryCheckPort`＝`GetDS4Battery`、`DefaultControlExtrasPort`＝`setRumble`）→ Step3・5 の新サービスを直接注入、(b) UI ファクトリー 3 つ・`AutoProfileService`・`ProfileApplicationService` → 直接注入が可能かを循環検査で確認、(c) `ProfileRepository` → `LoadProfileXml(…, ControlService control, …)` の `control` の用途を調べて判断 | M | プロファイル適用・自動プロファイル | (c) が解けないときは、その 1 件の `Func<ControlService>` を理由つきで残す（R3） |
| **7** | `Func<IOutputSlotService>`／`Func<IVirtualPadService>`／`OutputSlotService` の `Func<OutputSlotManager>` の相互参照の解消。`ActiveOutDevType` の配列の持ち主を一方向にする | M | **スロット操作**（プラグ・アンプラグ） | `BackingStore.ActiveOutDevType` の配線（`ServiceRegistration.cs` のコメントにある、IOutputSlotStore → IPathService → BackingStore の循環によるスタックオーバーフローの警告）を保つ |
| **8** | 残りの `Func<IMapping>`／`Func<IAutoProfileService>`／`Func<IActionBindingBuilder>` の再判定。循環が消えていれば直接注入へ。起動順（ホスト構築時に作らない）の理由で残すものは、台帳に理由と再検討条件を記録。`ServiceRegistration.cs` の古いコメントの修正 | M | いいえ（配線の変更のみ） | 完了条件は「循環を理由とする `Func` が 0 件」。起動順を理由とするものは残してよい（理由の記録つき） |
| **9** | F-5c: `running`／`suspending` の転送を削除し、4 つの利用者（`AutoProfileService`、`App`、`MainWindow`、`TrayIconViewModel`）へ `IServiceLifecycle` を直接注入（Q7）。`ControlService.cs` の F-5c の TODO を削除 | S〜M | **開始・停止**、トレイの表示、電源イベント | `MainWindow`（18 引数）・`TrayIconViewModel`・`MainWindowFactory`・`AutoProfileService` とテストの引数が増える。Step10 で相殺する |
| **10** | 転送メンバーの削除と引数の整理（努力目標 20 以下。Q6）。対象の候補: `ChangeUDPStatus`／`ChangeMotionEventStatus`（→ `IUdpMotionService`）、`ChangeOSCListenerStatus`／`ChangeOSCSenderStatus`（→ `IOscService`）、`CheckHidHidePresence`／`UpdateHidHiddenAttributes`（→ `IHidHideService`）、`PluginOutDev`／`UnplugOutDev`（→ `IVirtualPadService`）、`TouchPadOn` 系など。呼び出し元（UI、他のサービス）を直接利用へ切り替えられる範囲で、引数を減らす | M〜L | 接続・切断・スロット操作 | 各メンバーの利用者数を測ってから、効果の大きい順に。20 に届かなければ、残る引数の理由を記録（完了条件に含めない） |
| **11** | **（任意。Q3）** F-1c（`Application_Startup` の手順を `AppStartupSequence` へ）と F-2c（IPC の実行を `IpcCommandProcessor`／`IIpcCommandHost` へ）。Step10 までが完了した時点で、続けるか見送るかを**ユーザーが再判断** | L | 起動、IPC コマンド | 台帳の着手条件（起動処理や IPC を増やすとき）を満たさないため任意。見送る場合は、判断と根拠を台帳に記録して閉じる |
| **12** | 完了作業: モデル図 01〜04 の最終確認、`Phase9-Deferred-Ledger.md`・`Phase7-Residual-Ledger.md` の更新、`Phase13-Completion-Report.md`、Release ビルド（2 か所）、実機確認 1 回（§7） | S | 実機確認 | |

Step の順序の理由: Step3（状態を持たないホストの分離）は Step5 の前提を軽くする。Step5 が済むと、Step6 の (a) とStep8 の `Func<IMapping>` が直接注入にできる。Step4（`ServiceProviderHolder`）は他と独立しているので、Step2 の後ならどこでもよい（Step3 のあとに置いたのは、プロファイル適用の変更を接続系の変更と分けて確認しやすくするため）。

### 4.1 新設する型（案。名称は各 Step の個別計画書で確定）

| 型 | 種別 | 置き場所（§3.4 の配置ルール） | 名前空間（R6） | Step |
|---|---|---|---|---|
| `IEventThreadDispatcher` | インターフェース | `DS4Windows/DI/`（R1） | `DS4Windows.DI` | 3 |
| `EventThreadDispatcher` | 実装 | `DS4Windows/DS4Control/Services/`（R2） | `DS4Windows.Services` | 3 |
| `IDeviceRumbleService`／`DeviceRumbleService` | 同上 | 同上 | 同上 | 3 |
| `IOutputKbmInitializer`／`OutputKbmInitializer` | 同上 | 同上 | 同上 | 3 |
| `IDeviceSlotState`／`DeviceSlotState` | 同上 | 同上 | 同上 | 5（ゲート付き） |
| （廃止）`IVirtualPadHost`、`IServiceLifecycleHost` | — | — | — | 3 |
| （廃止）`IDeviceSlotHost`、`IUdpMotionHost`。`IDeviceStateAccessor` は新設の型が実装 | — | — | — | 5 |
| （削除）`ServiceProviderHolder`、静的 `ActionManager` | — | — | — | 4 |

すべて 1 ファイル 1 型（§3.3 原則 1・2）。`DS4Windows/Actions/` の型（`ActionManager` の置き換え）は、R3 により Actions 配下に置く。

---

## 5. ラチェットの基準値の更新計画

### 5.1 検査の関係

- `Step10FourGlobalRatchetGuardTests`: `Global.*`／`AppHost.GetService|Services`／`Program.rootHub` の件数（ファイル別の上限）。
- `Phase7Step1ServiceLocatorRatchetGuardTests`: `ServiceProviderHolder.Provider|GetRequiredService`（ファイル別の上限）。
- `Phase7Step10eW5RatchetEqualityGuardTests`: 上の 2 つの基準値が実数と**一致**することと、最終の合計（`FinalTotals_AreFixed`: 0／3／2／10）を検査する。**件数を減らして基準値を更新し忘れると失敗する**。

### 5.2 更新の計画

| Step | 変更 | 更新する基準値 |
|---|---|---|
| 1〜3、5〜10 | `Global`／`AppHost`／`rootHub` を増やさない。新設の型・ファイルは基準に載せない（基準に無いファイルは 0 として扱われる） | `Step10Four…`: 変更なし（`ControlService.cs` は既に (0,0,0)）。新ファイルが 1 件でも参照を持つと失敗するので、持たせない |
| 4a | `DefaultKeyButtonActionControllerFactory.cs` の 2 → 0 | `Phase7Step1…` の `Baseline` から該当エントリを削除 |
| 4b | `ActionManager.cs` の 8 → 0（ファイル自体を削除） | 該当エントリを削除。同じコミットで `FinalTotals_AreFixed` の `locator` を 10 → 0 |
| 4c | `ServiceProviderHolder.cs` を削除。`LocatorPattern` の対象が無くなる | `Phase7Step1…` の `Baseline` は空になる。**検査自体は残し、「新規の参照が 0 件」の禁止ガードとして維持**する（クラスを再導入できないようにする）。`Phase7Step10eW5…` の `LocatorPattern` は 0 件の確認として残す |

各 Step で、基準値を変えるコミットには必ず `Phase7Step10eW5RatchetEqualityGuardTests` の実行を含める。件数を減らしたら基準値も同時に更新する（規約 §3.1）。

追加の検査（案）: Step8 の完了時に、`ServiceRegistration.cs` の `Func<` の件数を固定する新しい検査（例: `Phase13FuncRatchetGuardTests`。残す `Func` を理由つきの許可リストで固定し、増えたら失敗させる）。

---

## 6. 規約への適合

| 規約 | 適合の方法 |
|---|---|
| §2.1 シム維持 | 静的な窓口を**新設しない**。`ControlService` の転送メンバーは「呼び出し元の切り替えが済むまで残す」（Step10 で、利用者が 0 件になったものだけ削除。台帳 F-5b の前例） |
| §2.2 機能維持 | 機能を削らない。安全弁（Fresh-press ガード `RequiresFreshPressAfterReset`／`IsExecuting`、一時プロファイル復帰スタック、排他制御、Phase5-Plan §5 の 6 大ガードレール［同一 XML の排他ロック、適用時 Halt 停止、スレッド直列化、On-Demand パス評価、ドライバ破棄順序、切断時クリーンアップ］）は、移動する処理とともに**そのまま移す**。特に Step3（イベントスレッドの破棄順）、Step4（トグル状態のリセット）、Step7（ドライバ破棄順序）は個別計画書に「安全弁の対応表」を入れる |
| §2.3 ログ | **文言・レベルを変えない。** `DebugMessage` を `ControlService.LogDebug` へ転送する配線は、移した先でも同じ送り主・同じ経路を保つ。ログの整理は 13b。移動に伴い呼び出し位置が変わるだけのログは、検査テスト（ログを見る 14 テストファイル）の期待値を変えない。新規コードは `Log-Policy.md` に最初から沿う |
| §2.4 未参照コード | 各 Step で呼び出し元が 0 件になった型・メンバーは、経緯を調べてから判断する（削除してよいものは本書の対象の廃止 IF など、置き換え済みのもの。**使うべきものは削除しない**）。判断は各 Step の計画書に記録する |
| §3.1 Pure DI | 新サービスはコンストラクター注入のみ。`AppHost.GetService`／`ServiceProviderHolder` を新たに使わない。`ControlService` の生成は `ServiceRegistration.cs` のファクトリーに集約（現状どおり） |
| §3.2 巨大ファイル | `ControlService.cs`（1,077 行）、`ServiceRegistration.cs`（320 行）は**全体を再生成せず**、対象メソッドをピンポイントで置換する |
| §3.3 原則 1・2 | 1 ファイル 1 型、ファイル名＝型名。新設の型はすべて単独のファイル |
| §3.3 原則 3 | 既存クラスの名前空間は変えない。新設の実装は `DS4Windows.Services`、契約は `DS4Windows.DI` |
| §3.3 原則 4 | 残す型・メンバーに TODO コメント（理由・経緯・予定 Phase）。既存のコメント（設計意図・TODO）は、置換範囲を限定して消さない。解消した TODO（例: `ControlService.cs` の F-5c、`_outputSlotServiceFactory` の D1 の TODO、トリガー系 4 クラスの「引数を残した理由」）は、**解消した Step で削除**する |
| §3.4 配置ルール | 契約は `DI/`（R1）、実装は `DS4Control/Services/`（R2）、Actions は `Actions/`（R3）。**ファイルの移動は行わない**（R7 は不要）。型の削除のみ |
| §4 プロセス | Step ごとに個別計画書・単体テスト・`dotnet test` の常時グリーン。ビルドは "Debug ALL"（メモリの手順）で警告 0・エラー 0 になるまで繰り返す |
| 調査の指示 | 慣れない機能をゼロから書かない。Step5（スロット状態）と Step3（イベントスレッド）の着手前に、OSS の類似実装（例: スレッド専用ディスパッチャー、状態ホルダーの DI の例）を検索して比較し、結果を個別計画書に書く |

---

## 7. 実機確認の計画（DS4 実機のみ。Q8: Phase 末に 1 回）

**確認の前提（メモの手順）**: ユーザーに実機確認を依頼する前に、Release ビルドを次の 2 か所へ出す。

1. `DS4Windows\bin\x64\Release\`: `dotnet build DS4WindowsWPF.sln -c Release /p:platform=x64`（PowerShell。`DS4Windows.exe`／`DS4Windows.dll` の更新時刻を確認）
2. `DS4Windows\bin\x64\DS4Windows\`: `scripts\publish-win-x64.ps1` ＋ `scripts\post-build.ps1 <root>/DS4Windows/bin/x64/Release <root>/DS4Windows`（`CopyDS4WindowsToJK.bat` が取り込む実行物。管理者権限が必要な最後のコピーはユーザーが行う）

ビルドの途中で DLL が 1 バイトに見えることがあるので、終了後に再確認する。実機確認の依頼文には、この 2 か所を更新したことを明記する。

**確認項目**（台帳 §52 の S 系の再実行を基にする。Phase13 で触れた経路を重点的に）:

| # | 項目 | 対応する Step |
|---|---|---|
| H1 | USB／Bluetooth で DS4 を接続する（起動時に接続済み、起動後に接続の両方）。入力が反映され、スロットが埋まる | 2、3、5、9 |
| H2 | 切断（PS+↓、ケーブル抜き）。スロットが空き、仮想パッドが外れ、再接続で復帰する。ログの文言が従来どおり | 3、5、7 |
| H3 | プロファイルの適用・切替（通常、トリガーによる切替、一時プロファイル、元に戻す）。直後の押しっぱなしで連鎖発火しない（Fresh-press ガード） | 4、6 |
| H4 | 割り当て（Key のトグル、Macro、Button、Program、Profile）の動作 | 1、4 |
| H5 | スロット操作（出力スロットの Plug／Unplug、永続設定）。ViGEm の Xbox360／DS4 出力 | 5、7 |
| H6 | サービスの開始・停止・ホットプラグ。トレイの表示（Start／Stop）、電源イベント（スリープ復帰）。ウィンドウ終了時にハングしない | 3、9 |
| H7 | 自動プロファイル（プログラム検知による切替） | 6、9 |
| H8 | 振動（RumbleTest、ゲーム側の振動、ブースト設定・反転） | 3 |
| H9 | UDP モーション（Cemuhook）・OSC の有効／無効。KBM 出力（FakerInput／SendInput のフォールバック） | 3、5、10 |
| H10 | IPC コマンド（`start`／`stop`／`cycle`／`query`／`loadprofile`） | 9、10（Step11 を行う場合は必須） |
| H11 | Step11 を行う場合: 起動ログの順序、初回起動の流れ（別の設定フォルダで） | 11 |

Q8 の回答は「Phase 末に 1 回だけ」。そのため、**不具合が出たときに原因の Step を特定できるよう、Step ごとのコミット（ユーザーが「コミットして」と言ったとき）を細かく分け、各 Step のユニットテストと特性化テストを厚くする**（§9 の R2）。

---

## 8. 論点の一覧

### 8.1 決定済み（AskUserQuestion の回答）

| # | 論点 | 決定 |
|---|---|---|
| Q1 | 分割方針 | **見直し案: 逆依存の根を断つ。** FI の 3 大コーディネーターは採らない（§3.2） |
| Q2 | `ServiceProviderHolder` の撤廃方法 | **静的 `ActionManager` を廃止して注入へ**（Bind 方式の橋渡しにはしない） |
| Q3 | F-1c・F-2c | **最後の任意 Step（Step11）として計画に含める**。Step10 の後に続けるか再判断 |
| Q4 | ブランチ | **`Phase13` を切る**（承認後、master から作成） |
| Q5 | ホスト IF の進め方 | **低リスクを先に、配列は別 Step で判断**（§3.4 の訂正: `IUdpMotionHost` は配列側へ） |
| Q6 | 引数の目標 | **努力目標 20 以下。未達なら理由を記録**。完了条件は「未使用 0・循環を理由とする `Func` 0・`ServiceProviderHolder` 0」 |
| Q7 | F-5c の方式 | **`IServiceLifecycle` を直接注入** |
| Q8 | 実機確認 | **Phase 末に 1 回だけ** |

### 8.2 既定案で進める軽微な論点（承認時に異議があれば変更する）

| # | 論点 | 既定案 |
|---|---|---|
| M1 | xUnit の `[Collection("ServiceProviderHolder")]`（133 ファイル）の名前 | Step4c で、型の削除と**別コミット**にして、名前を実態に合わせて変更する（例: `"SharedHost"`）。機械的な置換で、挙動は変わらない。**変更しないなら、クラス削除後も古い名前が残る** |
| M2 | Step3 の新サービスの名前（`IEventThreadDispatcher`／`IDeviceRumbleService`／`IOutputKbmInitializer`） | 案のまま、Step3 の個別計画書で確定 |
| M3 | `ControlService.EventDispatcher`（公開プロパティ）の扱い | 利用者を調べ、新サービスの窓口へ切り替えてから削除（§2.6） |
| M4 | 新しい検査 `Phase13FuncRatchetGuardTests`（残す `Func` を許可リストで固定） | Step8 で追加（§5.2） |
| M5 | 各 Step の個別計画書の置き場所と名前 | `docs-forDIMG/MadeByAgent/Phase13-StepN-Plan.md`。進捗は `Phase13-Status.md`（Phase12 と同じ形式） |
| M6 | `ProfileXmlStore` の internal アクセサ（外部利用 0 件の再確認後に削除） | Step2 の最初に確認し、使われていれば引数を残す |

---

## 9. リスクと対策

| # | リスク | 対策 |
|---|---|---|
| R1 | Step5（スロット状態）が `On_Report` のホットパスのアロケーション・遅延を悪化させる | 着手前に A/B 計測を設計し、結果を本書へ追記して再承認（Phase9 の方式）。悪化したら取りやめ、`Func<IDeviceSlotHost>` 等を理由つきで残す（§8 の完了条件を満たす範囲に調整） |
| R2 | 実機確認が Phase 末の 1 回だけのため、不具合の原因の Step が特定しにくい | Step ごとの小さなコミット、特性化テスト（接続・切断・プロファイル適用・スロット操作）を各 Step で先に足す。`git bisect` が使える粒度を保つ |
| R3 | `IProfileXmlStore.LoadProfileXml(…, ControlService control, …)` と `Mapping.MapCustom(…, ControlService ctrl)` が `ControlService` 型を引数に取る構造のため、`ProfileRepository` の `Func<ControlService>` が解けない可能性 | Step6 (c) で `control` の用途を調べる。解けなければ、その 1 件を理由つきで残し、Phase16 以降の課題として台帳に記録（範囲外としていた構造の整理が必要） |
| R4 | 静的 `ActionManager` の廃止（Step4b）が、静的辞書へのフォールバックに依存するテストを壊す | 着手前に、フォールバック経路（`mgr == null`）を通るテストを洗い出す。呼び出し元ごとに、特性化テストを先に足す |
| R5 | イベントスレッドの移動（Step3）で、破棄順（`InvokeShutdown`→`Join`）や起動順が変わり、終了時にハングする | スレッド名・優先度・破棄順を維持。`Stop`／`ShutDown` の順序テストを追加。実機の H6 で確認 |
| R6 | 未使用引数の削除（Step2）で、型の生成タイミングが変わり、イベント購読の配線が抜ける | 解決グラフの検査（どのサービスがどの経路で生成されるか）を先にテストにする。生成時に副作用（購読）を持つ型は、削除せず別の窓口へ |
| R7 | 引数を増やす Step（Step9 の `IServiceLifecycle` 注入）が `MainWindow`（18 引数）のテストを大きく書き換える | Step10 で相殺する計画。テストの生成を `TestHost` 経由に寄せられるものは寄せる |

---

## 10. モデル図 01〜04 の更新項目（Phase13 の完了条件）

各 Step の完了時に、該当する図を同じコミットで更新する。Phase13 の完了条件は「下表がすべて反映済みで、図と実コードの食い違いが無い」こと。

| 図 | 更新項目 | Step |
|---|---|---|
| 01 | 「2. 信号変換層」の `ControlService` の箱と説明（63〜69 行付近、238・251 行の表: 行数、コンストラクター引数 34→実数、`Func<>` 2 件→0）。新設サービスの追加。`ServiceProviderHolder` の持ち越し記述（279・284 行付近: 17 件 → 0）と、`Actions/` の静的な橋渡しの削除 | 2〜4、10 |
| 02 | スタック図の `ControlService`（28 行）と、依存方向の注記（132・148 行付近）。過渡期の逆依存（`OutputSlotService`→`ControlService` 等）の記述を、解消した分だけ更新 | 3〜7 |
| 03 | `ControlService` のクラス（112 行付近）。ホスト IF のクラス（`IVirtualPadHost`／`IDeviceSlotHost`／`IServiceLifecycleHost`／`IUdpMotionHost`）の廃止と、関係の辺（497〜547 行の「`Func` による遅延取得」→ コンストラクター注入）。UI ファクトリーの `Func~ControlService~`（696・723 行）。新設の型の追加。`ActionManager`／`IManagedActionManager` の関係 | 3〜9 |
| 04 | サービスの一覧（30〜90 行）の追加・削除。注記※1（`Func<IOutputSlotService>`）の更新。窓口（ホスト）の表（143 行）。**イベントスレッドの所有者、生成順、破棄順（Dispose の順序）** | 3、5、7 |
| 共通 | 冒頭の「現状」の記述と、行数・件数の更新 | 12 |

---

## 11. 完了条件

1. 未使用のコンストラクター引数が 0 件（`ControlService` とトリガー系 4 クラス）。
2. 循環を理由とする `Func<T>` が 0 件。起動順を理由として残すものは、台帳に理由と再検討条件を記録済み。
3. `ServiceProviderHolder`（クラス、`SetProvider`、静的 `ActionManager`）が 0 件。ラチェットは禁止ガードとして維持。
4. F-5c が完了（`ControlService` の `running`／`suspending` の転送が 0 件）。
5. F-1c・F-2c: 実施、または見送りの判断と根拠を台帳に記録して閉じる。
6. `ControlService` の引数が 20 以下。未達の場合は、残る引数の理由を完了報告書に記録。
7. `Debug ALL`: 警告 0・エラー 0・全テスト合格。ラチェット 3 本（一致検査を含む）がグリーン。
8. モデル図 01〜04 が §10 のとおり反映済み。
9. Release ビルドを 2 か所に出し、実機確認（§7）が完了。
10. `Phase13-Completion-Report.md`、`Phase9-Deferred-Ledger.md`（F-1c・F-2c・F-5c）、`Phase7-Residual-Ledger.md`（§17.3）、`Phase11-and-Beyond-Roadmap.md`（§7 次の作業）を更新。

---

## 12. 次の作業

1. 本書のレビューと承認（承認の対象は本書全体。特に §3.4 の訂正と §8.2 の軽微な論点）。
2. 承認後、`master` から `Phase13` ブランチを作成する（Q4）。
3. Step1 の個別計画書（`Phase13-Step1-Plan.md`）を作り、承認を得て着手する。
