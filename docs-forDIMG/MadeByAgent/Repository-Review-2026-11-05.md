# リポジトリ全体レビュー（2026-11-05。Phase14 完了・v4.3.1 時点）

作成日: 2026-11-05
対象: `master`（7a477c31）
観点: (1) Pure DI 化、(2) 入力デバイス・仮想出力デバイスのカートリッジ化、(3) 層間の受け渡しと依存関係、(4) 共用部品化、(5) 保守・機能追加の実装コスト
性質: 調査のみ（コードの変更なし）。採用する項目は、個別計画書を作って承認を得てから実装する（規約 §4）。

---

## 0. 要約

- **Pure DI は概ね達成**: `Global`・`Program.rootHub`・`ServiceProviderHolder` は消え、`AppHost.GetService` の実呼び出しは `App.xaml.cs` の 1 か所のみ。残る課題は「静的な可変状態」（`DS4LightBar`、`DS4Devices`、`SyntheticDispatcher`、`ActionRegistry`、`AppLogger`）と、登録ラムダ内の「後付けの配線」。
- **依存の向きは、サービス単位では守られているが、型単位では守られていない**: `Core/` の 51 ファイルが第2層（`DS4Windows.Processing*`）の型を、`Core/` の 4 ファイルと `Input/` の 1 ファイルが UI の型を参照している。原因の大半は「共有のデータ型（`SpecialAction`、`BezierCurve` ほか）が第2層に置かれていること」で、**層の依存を自動で検査するテストが存在しない**ことが是正を難しくしている。モデル図 02 の「依存の向きは実コードと一致」という注記は、型の参照までは成り立っていない。
- **カートリッジ化は入口だけ**: 入力は `IInputDeviceModule`（接続時のフック）までで、新しい機種の追加には 5 層・約 15 ファイルの変更が要る。出力は未着手で、`OutContType` の分岐が 16 ファイルにある。
- **共用部品化の即効候補**: `*DeviceOptions` 4 クラスは既定値 1 つを除いて同一。手書きの `XxxChanged` イベントの定型が `ProfileSettingsViewModel` だけで 83 組。
- **保守コストの主因**: 本番コード中の Phase 履歴コメント 2,143 か所、Phase 名のテスト 299 ファイル（うちソース文字列を読む検査 166 ファイル）、Phase14 の自動生成で入った未使用の `using static DS4Windows.Processing.Mouse` 49 ファイル。

---

## 1. 指摘一覧（優先度順）

優先度: 🔴 高（構造の誤り・潜在不具合・以後の作業を妨げる）／🟡 中（コスト削減の効果が大きい）／🟢 低（整理・好み）
規模: S（1 Step）／M（数 Step）／L（Phase 規模）

| # | 観点 | 指摘 | 優先度 | 規模 | 既存の計画との関係 |
|---|---|---|---|---|---|
| R-01 | 依存 | 層の依存を検査する自動テストがない | 🔴 | S | 新規 |
| R-02 | 依存 | `Core/` → 第2層・UI、`Input/` → UI の逆向きの参照（共有型の置き場所の誤り） | 🔴 | M | 新規（モデル図 02 の注記と食い違う） |
| R-03 | DI | `AppLogger` の暗黙のフォールバックが 2 つ目の `BackingStore` を作る | 🔴 | S | 新規 |
| R-04 | 保守 | 未使用の `using static DS4Windows.Processing.Mouse`（49 ファイル）と、`Mouse` のネスト列挙型 | 🟡 | S | Phase14 の取りこぼし |
| R-05 | DI | `DS4LightBar` の静的配列（`forcelight`／`forcedColor`／`forcedFlash`）が 15 ファイルから読み書きされる | 🟡 | M | 目標像 `ILightbarService`（未実装） |
| R-06 | DI | `SyntheticDispatcher` の静的な橋渡しが残る（「Phase8 で削除」のコメントのまま） | 🟡 | S〜M | 新規 |
| R-07 | カートリッジ | 入力機種の追加に約 15 ファイル・5 層の変更が要る | 🟡 | M | Phase16 の前段にできる |
| R-08 | カートリッジ | 出力（`OutContType`）の分岐が 16 ファイル。`IVirtualPadService` が第3層の具象と第1層の `DS4Device` を公開 | 🟡 | L | Phase16 §8.1 を拡張 |
| R-09 | 依存 | UI 24 ファイルが具象の `ControlService` に依存し、`DS4Controllers` などで第1層の具象へ到達する | 🟡 | M | F-5c の残り（約 50 利用者） |
| R-10 | 依存 | `IProfileSettingsService` が肥大（配列プロパティ 90、スロット引数のメソッド 42、利用 78 ファイル） | 🟡 | L | 9-G（G-1）と関連 |
| R-11 | 共用 | `*DeviceOptions` 4 クラスが同一、`*SupportSettings`／`*ControllerOptionsWrapper` も同型 | 🟡 | S | 新規。R-07 と同時が効率的 |
| R-12 | 共用 | 変更通知の定型（`XxxChanged` イベント）の手書き | 🟡 | M | Phase15・G-1 と同時 |
| R-13 | DI | 登録ラムダ内の後付け配線（`AttachProfileIdentity`、配列の共有、プロパティ注入、静的フィールド `_subscribedReset`） | 🟡 | S〜M | 新規 |
| R-14 | DI | View のコードビハインドで ViewModel を `new`（43 か所）。View がサービスを受けて VM へ渡すだけの中継になっている | 🟢 | M | Phase15 |
| R-15 | 保守 | 本番コードの Phase 履歴コメント 2,143 か所 | 🟢 | 継続 | 新規（運用ルール） |
| R-16 | 保守 | Phase 名のテスト 299 ファイル、ソース文字列を読む検査 166 ファイル | 🟢 | M | R-01 で置き換え可能 |
| R-17 | 依存 | 第1層の `DS4State`／`DS4Controls` が層間のデータ契約を兼ね、第3層 13 ファイルが第1層に依存 | 🟢 | M | Phase16 |
| R-18 | 保守 | `ServiceRegistration.RegisterServices` が 1 メソッド 300 行。層ごとの登録に分かれていない | 🟢 | S | 新規。R-07・R-08 の受け皿 |

---

## 2. 観点別の詳細

### 2.1 Pure DI 化

#### R-03 🔴 `AppLogger` のフォールバックが 2 つ目の `BackingStore` を作る
- 場所: `DS4Windows/Core/Logging/AppLogger.cs:101-105`
  ```csharp
  get => _notificationService ?? (_notificationService = new AppNotificationService(new BackingStore(new EnvironmentService())));
  ```
- 問題: `App` が `NotificationService` を設定する前にログが通知を出すと、**設定の唯一の実体であるはずの `BackingStore` が別に 1 つ作られる**。この `BackingStore` は設定を読み込んでいないため、通知の設定（表示の有無など）が既定値で判定される。また `EnvironmentService` も 2 つ目になり、静的な状態を持つ型（R-05 と同じ類）なので副作用の範囲が読めない。
- 同類: `Core/Settings/BackingStore.cs:755-770` の `ProfileSettings` のフォールバックは、`ProfileSettingsService` と `Processing.Services.DeviceSlotState`（第2層の具象）を `new` する（`Core` → 第2層の具象の参照でもある）。
- 提案: フォールバックを「何もしない実装（`NullNotificationService`）＋ `[DI]` の警告ログ」に替える。`BackingStore.ProfileSettings` のフォールバックは、どの経路で使われているかをログで確かめてから、例外または `Null` 実装に替える。

#### R-05 🟡 `DS4LightBar` の静的な可変配列
- 場所: `DS4Windows/Input/DS4LightBar.cs:45-61`。`forcelight`（26 参照）、`forcedFlash`（22）、`forcedColor`（17）、`shuttingdown`、`updateLightBar`。
- 利用: `Core`（`LegacyProfileXmlSerializer`、`ColorUtil`）、第2層（`InputReportProcessor`、`MacroPlayback`、`MacroStepExecutor`、`ServiceLifecycle`、`LatencyMonitor`、`Default*Port`）、UI（`MainWindow`、`BindingWindowViewModel`、`ProfileSettingsViewModel`、`RecordBoxViewModel`、`CheckBatteryViewModel`）の 15 ファイル。
- 問題: 第1層の静的配列を、全層が直接読み書きしている（実質的なグローバル変数）。ライトバーの色の計算（方針）は第1層の責務でもない。テストでは状態が漏れる。
- 提案: 目標像の `ILightbarService`（モデル図 01・02 にある）を、まず「状態の持ち主」として作る。
  1. `ILightbarOverrideState`（スロットごとの強制色・点滅・強制の有無）を Singleton で登録し、静的配列の参照をこれへ置き換える（機械的。利用者はコンストラクターで受ける）。
  2. `updateLightBar` の色の計算を第2層の `LightbarService` へ移す（第1層は「色を受けてレポートへ書く」だけにする）。

#### R-06 🟡 `SyntheticDispatcher` の静的な橋渡し
- 場所: `DS4Windows/Composition/ServiceRegistration.cs:55-62`（`SyntheticDispatcher.Bind(dispatcher)`。コメントは「Phase8 で削除」）。利用は `KeyButtonActionController.cs` の 21 か所、`RepeatHelper.cs`、`KeyAction.cs`。
- 提案: `IKeyButtonActionControllerFactory`（登録済み）が `ISyntheticDispatcher` を受けて `KeyButtonActionController` のコンストラクターへ渡す。`RepeatHelper`・`KeyAction` も同様。コメントの「Phase8 で削除」は実態と合わないので、着手しない場合も TODO を正しい予定（Step）へ直す（規約 §3.3 原則4）。

#### その他の静的な状態（参考。目録 `Phase7-Residual-Ledger.md` §17.3 と照合のこと）
| 場所 | 内容 |
|---|---|
| `Input/DS4Devices.cs:36-48,111` | 機器の辞書、`RequestElevation` イベント、`PrepareDS4Init`／`PostDS4Init`／`PreparePendingDevice` の静的デリゲート、`isExclusiveMode`、`LayerSettings`（静的な setter による注入） |
| `Processing/Actions/ActionRegistry.cs:8-31` | 静的なアクション一覧（`DefaultActionManager` が読む） |
| `UI/Services/NotificationService.cs` | 静的な `ShowToast`（`Input/DS4Devices.cs:324` が直接呼ぶ。R-02 参照） |
| `Composition/ServiceRegistration.cs:25` | 静的フィールド `_subscribedReset`（コンテナの作り直しへの対処。R-13） |

`DS4Devices` は `Ds4DeviceRegistryAdapter`（20 参照）で包まれているので、アダプターの中身を「インスタンスの `Ds4DeviceRegistry`」へ移し、静的クラスを消すのが自然な次の一手（9-H／Phase16 の前提を軽くする）。

#### R-13 🟡 登録ラムダ内の後付け配線（時間的結合）
- 場所: `ServiceRegistration.cs`
  - `:36-47` `BackingStore` の登録で `ProcessInspector` をプロパティで設定し、`ActionsLoaded` を購読（静的フィールドで前回の購読者を覚える）。
  - `:66-72` `IProfileIdentityService` の生成時に、`BackingStore.AttachProfileIdentity` と、`IProfileSettingsService` を `ProfileSettingsService` へキャストして `AttachProfileIdentity`。
  - `:86-91` `IOutputDeviceTypeState` の生成時に、`BackingStore.ActiveOutDevType` へ配列を渡す。
- 問題: 「どのサービスを最初に解決したか」で配線の有無が変わる（例: `IProfileIdentityService` を誰も解決しないと `BackingStore` に結ばれない）。キャスト（`as ProfileSettingsService`）は、偽の実装に差し替えると黙って配線が抜ける。
- 提案: 配線を 1 か所の明示的な初期化（例: `ICompositionWiring.Wire()` を `CreateHost` の直後に呼ぶ）へ集める。または、`BackingStore` 側が `IProfileIdentityService`／`IOutputDeviceTypeState` をコンストラクターで受ける形へ（ブートストラップの `BackingStore` が先に作られる制約があるため、前者が現実的）。

#### R-14 🟢 View のコードビハインドでの ViewModel の生成
- 例: `UI/AssignmentEditor.xaml.cs:152-167` で 10 個の ViewModel を `new` し、同じ `specialActionRepository` を配る。`UI/BindingWindow.xaml.cs:91` は 5 つのサービスを中継。全体で 43 か所。
- 提案: 画面ごとの小さなファクトリー（`ISpecialActionEditorViewModels` など）を `IViewModelFactory` の隣に置き、View はファクトリー 1 つだけを受ける。Phase15（UI 共通化）で扱う。

#### R-18 🟢 `ServiceRegistration` の分割
- 提案: `AddCoreServices()`／`AddInputLayer()`／`AddProcessingLayer()`／`AddOutputLayer()`／`AddUiLayer()` の拡張メソッドに分け、`RegisterServices` はそれを順に呼ぶだけにする（ファイルは 1 型 1 ファイルの規約に合わせて分ける）。カートリッジ（R-07・R-08）は `AddInputDeviceCartridge<T>()` のような 1 行で足せるようにする。
- ついでの整理: `:148-152` の「修正前／修正後」のコメントと、不要な完全修飾名。

---

### 2.2 入力デバイス・仮想出力デバイスのカートリッジ化

#### R-07 🟡 入力機種の追加コスト
現状、新しい機種（例: 仮に Vader 4 Pro の専用ドライバー）を足すときに触るファイル:

| 層 | ファイル | 内容 |
|---|---|---|
| 第1層 | `Input/Devices/XxxDevice.cs` | デバイス本体（`DS4Device` の派生） |
| 第1層 | `Input/Devices/InputDeviceType.cs` | 列挙値の追加 |
| 第1層 | `Input/Devices/InputDeviceFactory.cs` | `switch` へ追加 |
| 第1層 | `Input/DS4Devices.cs:62-105` | `knownDevices`（VID/PID の静的表）へ追加 |
| 第2層 | `Processing/Services/XxxDeviceModule.cs` | 接続時のフック |
| 4-b | `Composition/ServiceRegistration.cs:227-231` | モジュールの登録 |
| Core | `Core/Settings/XxxDeviceOptions.cs`、`XxxControllerOptions.cs`、`ControlServiceDeviceOptions.cs` | 機種の設定 |
| Core | `Core/Persistence/Dto/XxxSupportSettings.cs`、`AppSettingsDTO.cs`、`AppSettingsXmlSerializer.cs` | 設定の保存 |
| UI | `UI/ViewModels/XxxControllerOptionsWrapper.cs`、`ControllerRegDeviceOptsViewModel.cs`（10 分岐）、`DeviceListItem.cs`、`ControllerRegisterOptionsWindow.xaml` | 設定画面 |

- 問題: 「部品を足すだけ」になっているのは第2層のフック（`IInputDeviceModule`）だけ。機種の識別（VID/PID）、生成、設定、保存、画面がそれぞれ別の `switch`／表に散っている。
- 提案（Phase16 の 9-H［通信／解析の分離］とは独立に、先にできる）: **カートリッジの記述子**を導入する。
  ```csharp
  // Input/Abstractions/IInputDeviceCartridge.cs（第1層の契約）
  public interface IInputDeviceCartridge
  {
      InputDeviceType Type { get; }
      IEnumerable<VidPidInfo> KnownDevices { get; }      // DS4Devices.knownDevices の該当行
      DS4Device Create(HidDevice hid, string displayName, VidPidFeatureSet features); // InputDeviceFactory の該当 case
  }
  ```
  - `DS4Devices`（→ R-05 のインスタンス化後の `Ds4DeviceRegistry`）は `IEnumerable<IInputDeviceCartridge>` から VID/PID 表と生成を組み立てる。`InputDeviceFactory` の `switch` は消える。
  - 設定は、`DeviceOptionsBase<TController>`（R-11）にそろえたうえで、`ControlServiceDeviceOptions` を「種類 → 設定」の辞書にする（保存形式の XML は変えない。読み書きの変換で吸収する）。
  - 画面は、機種ごとの設定 VM をカートリッジが返す（`ControllerRegDeviceOptsViewModel` の分岐を消す）。ここは Phase15 と調整。
  - 効果: 機種の追加が「第1層のデバイス＋カートリッジ 1 つ＋登録 1 行（＋必要なら設定画面の部品）」になる。

#### R-08 🟡 出力（仮想パッド）のカートリッジ化
- `OutContType.X360`／`.DS4` の分岐: `VirtualPadService`（15）、`ProfileSettingsViewModel`（7）、`SlotDeviceEntry`（6）、`ProfileOutputControllerLoadSection`（4）、`ProfilePresets`（4）、`MacroParser`（3）、`OutputSlotManager`（3）、`IpcCommandProcessor`（2）ほか、計 16 ファイル。
- `IVirtualPadService`（第2層のポート、`Processing/Abstractions/IVirtualPadService.cs`）が、第3層の具象（`OutputSlotManager`、`OutputDevice[]`、`OutSlotDevice`）と第1層の具象（`DS4Device`）をそのまま公開している。ポートとしての抽象化になっていない。
- `Processing/ControlService.cs:19,253,272` が `Nefarius.ViGEm.Client` を直接参照し、`OutputSlotManager.ViGEmFailure` を購読している（第2層 → ViGEm の具象）。
- 提案（Phase16 §8.1 の `IVirtualPadBackend` を拡張）:
  1. `IVirtualPadBackend`（ViGEm の包み）に加えて、`IOutputDeviceCartridge`（`OutContType` ごとの「生成」「振動・ライトバーの戻りの接続」「表示名」「保存のキー」）を置き、`switch` をカートリッジの辞書引きへ替える。
  2. `IVirtualPadService` から `OutputSlotManager`／`OutputDevices` の公開をやめ、必要な操作（スロットの一覧の読み取り、差し込み・抜き取り）をメソッドで出す。引数の `DS4Device` は、スロット番号＋振動の受け口（例: `IFeedbackSink`）にする。
  3. `ViGEmFailure` は `IVirtualPadService` のイベント（ViGEm に依存しない名前、例: `BackendFailure`）にして、`ControlService` から `using Nefarius.ViGEm.Client` を外す。

---

### 2.3 層間の受け渡しと依存関係

#### R-01 🔴 層の依存を自動で検査するテストがない
- 現状: `Phase14NamespaceFolderGuardTests` は「名前空間＝フォルダ」だけを検査する。依存の向き（規約 §1、R5「`Core/` から第2層以上を参照しない」）を検査するテストはない。各種のラチェットはソースの文字列検索で、特定の識別子（`Global` など）しか見ない。
- その結果、R-02 の逆向きの参照が 50 ファイル以上あっても検出されていない。
- 提案: アセンブリのリフレクション（または `Mono.Cecil`）で「型 → 参照している型」を取り、名前空間の接頭辞で層を判定する**アーキテクチャテスト 1 本**を作る。例（自作の簡易版でも、NetArchTest.Rules／ArchUnitNET でもよい）:
  - `DS4Windows.Core.*` は `DS4Windows.Processing.*`／`UI.*`／`Composition.*` を参照しない
  - `DS4Windows.Input.*` は `Processing.*`／`UI.*`／`Output.*` を参照しない
  - `DS4Windows.Output.*` は `UI.*` を参照しない（`Processing.Abstractions` は R2 の規約により許可）
  - `DS4Windows.UI.*` は `Input.*`／`Output.*` を参照しない（目標。最初は許可リスト）
  - 既存の違反は**許可リスト（件数のラチェット）**で凍結し、減らしたら基準値も更新する（規約 §3.1 のラチェットの一致検査と同じ運用）。
- 効果: 以後の是正の進み具合が数値で見え、新たな逆向きの参照を作った時点でテストが落ちる。ソース文字列の検査（R-16）よりリファクタリングに強い。

#### R-02 🔴 逆向きの参照（共有型の置き場所の誤り）
実測（`using static DS4Windows.Processing.Mouse` だけのものを除く）:

| 参照元 | 参照先 | ファイル数 | 主な原因 |
|---|---|---|---|
| `Core/` | 第2層（`Processing*`） | 51 | 下表の共有型が第2層にある |
| `Core/` | UI | 4 | `AppLogger.cs:24`（`UI.Services`）、`AppearanceSettingsService.cs:32`（`UI.TrayIconResources`）、`WindowPlacementHelper.cs:135`（`MainWindow` を引数に取る）、`PathService.cs:7`（`using DS4Windows.UI`。未使用の可能性） |
| `Input/` | UI | 1 | `DS4Devices.cs:324` が静的な `UI.Services.NotificationService.ShowToast` を直接呼ぶ |
| `Input/` | 第2層 | 4 | `IDeviceLayerSettings` が `Processing.Abstractions` にある（第1層が使う契約なのに、第2層に置かれている） |

`Core/` が参照する第2層の型の上位（参照回数）:

| 型 | 参照回数 | 性質 | 移し先の案 |
|---|---|---|---|
| `SpecialAction` | 124 | 設定・保存の対象のデータ | `Core/Settings`（実行の部品は第2層に残す） |
| `BezierCurve` | 87 | 出力カーブの設定値と計算 | `Core/Utilities` または `Core/Settings` |
| `IProfileLoadTarget` | 65 | プリセット（`Core/Settings/*Preset.cs`）が使う | プリセットの適用を第2層へ移すか、契約を `Core/Abstractions` へ |
| `Mouse`（ネスト列挙型） | 54 | `TouchButtonActivationMode` | `Core/Settings/TouchButtonActivationMode.cs`（R-04） |
| `MacroExecuteMode(s)` | 30 | 設定値の列挙型 | `Core/Settings` |
| `ProfileActions` | 17 | 設定値 | `Core/Settings` |
| `IAppSettingsService` | 10 | 実装は `Core/Settings/AppSettingsService` なのに、契約が `Processing/Abstractions` にある | `Core/Abstractions`（R2「実装と同じ層」） |
| `WindowFocusResult`／`WindowCandidate`／`WindowTargetMatcher`／`CapturedWindow` | 28 | `Core/Platform` の窓操作が使う | 窓操作の契約を `Core/Abstractions` へ、または実装を第3層へ |

- 提案: 「依存の向きの是正（型の置き場所）」を 1 つの Phase（例: Phase14b）として、**Phase15 の前に**行う。理由: 名前空間が変わるため、Phase15 で新しく作る UI 部品と XAML を最初から正しい名前空間で書ける（Phase14 を Phase15 の前にした判断と同じ）。手順は Phase14 と同じく `git mv` → 名前空間の書き換え（R7）。R-01 のテストを先に入れて、件数を減らしながら進める。
- モデル図 02 の注記（「依存の向きは、実コードと一致している」）は、型の参照の単位では成り立たないので、R-01 の結果に合わせて書き直す。

#### R-09 🟡 UI → 具象の `ControlService` → 第1層・第3層の具象
- UI の 24 ファイル（＋ `App.xaml.cs`）が `ControlService`（具象。インターフェースなしで登録）に依存。
- `ControlService` の公開メンバーのうち UI が使うもの: `DS4Controllers`（`DS4Device[]`、12 か所）、`slotManager`（5）、`HotplugController`（4）、`touchPad`（`Mouse[]`、3）、`MappingService`（3）、`getDS4State`（2）、`recordingMacro`（2）、`DeviceOptions`（2）、`OutputslotMan`（1）。
- 問題: UI は「第2層だけに依存する」規約だが、`ControlService` が第1層の `DS4Device` と第3層の `OutputSlotManager` への抜け道になっている。UI の 34 ファイルが `using DS4Windows.Input／Output` を持つ。
- 提案: UI 向けの読み取り専用の窓口 `IControllerSlotQuery`（スロットの表示用の情報: 接続の種類、MAC、電池、表示名、接続中か）と、操作の窓口（切断、プロファイルの適用など）を第2層に置き、UI はそれだけを受ける。`ControlService` はインターフェースの背後に置く（`IControlService` を 1 つ作るより、用途別に 2〜3 個に分けるほうがよい）。F-5c の残り（約 50 利用者）と同じ作業になる。

#### R-10 🟡 `IProfileSettingsService` の肥大
- `Core/Abstractions/IProfileSettingsService.cs`（241 行）: スロット番号の配列を返すプロパティ 90 個（`bool[] TouchpadActiveArray` など）と、`int device` を取るメソッド 42 個。利用 78 ファイル。
- 問題: 利用者は全機能の契約に依存し（インターフェース分離の原則に反する）、テストの偽の実装も巨大になる。配列をそのまま返すので、外から要素を書き換えられる。
- 提案: スロット単位の入口 `ProfileSlotSettings ForSlot(int slot)` を足し、その中を領域（`Sticks`、`Triggers`、`Gyro`、`Touchpad`、`Lightbar`、`Output`…）ごとの小さな型に分ける。利用者を領域ごとに移していき、最後に配列のプロパティを消す。9-G（`ProfileSettingsViewModel` のサブ VM 化）の領域分けと一致させると、サブ VM がそのまま対応する領域だけを受けられる。

#### R-17 🟢 層間のデータ契約が第1層にある
- `DS4State`（入力の状態であり、同時にマッピング後の出力の状態）、`DS4Controls`、`DS4Color` が `Input/` にあり、第3層の 13 ファイル（`OutputDevice.ConvertandSendReport(DS4State, int)`、`UdpServer`、`OscService` ほか）が第1層に依存している。
- 規約 §1 の依存の向き（第2層 → {第1層、第3層}）では、第3層 → 第1層は想定されていない。
- 提案: 層をまたぐ値の型（状態・コントロールの名前・色）を `Core/`（例: `Core/Model`）へ移し、第1層と第3層の両方がそれを参照する形にする。Phase16 で第1層を分けるときに合わせて行うのが安全（`DS4State` は毎フレームの経路にあり、移動だけでも影響範囲が広い）。

---

### 2.4 共用部品化

#### R-11 🟡 機種ごとの設定クラスの重複
- `Core/Settings/DS3DeviceOptions.cs`、`DS4DeviceOptions.cs`、`DualSenseDeviceOptions.cs`、`SwitchProDeviceOptions.cs`（各 48 行）は、型名を除くと**同一**（`DS4DeviceOptions` だけ `DEFAULT_ENABLE = false`）。
- 同型の並び: `Core/Persistence/Dto/*SupportSettings.cs`（4 つ）、`UI/ViewModels/*ControllerOptionsWrapper.cs`（4 つ）、`Core/Settings/*ControllerOptions.cs`（4 つ）。
- 提案: `DeviceOptionsBase`（`Enabled` と変更通知）を作り、機種ごとの差分（既定値、`JoyConDeviceOptions` の追加項目）だけを派生型に残す。DTO は `DeviceSupportSettings` の基底に。R-07 のカートリッジと同時に行うと、機種の追加で書くコードがさらに減る。

#### R-12 🟡 変更通知の定型
- `ProfileSettingsViewModel` だけで `public event EventHandler XxxChanged` が 83 個。各プロパティが「旧値の計算 → 比較 → 代入 → イベント発火」を手で書いている（例: `UI/ViewModels/ProfileSettingsViewModel.cs:968-979` の `LSDeadZone`）。`*ControllerOptions` も同じ形。
- 提案: 共通の基底（`ObservableObject` の `SetProperty(ref field, value)`、または CommunityToolkit.Mvvm の `[ObservableProperty]` のソース生成）にそろえる。設定サービスへの読み書きを伴うプロパティは、`SetProperty(getter, setter, value)` の形のヘルパーを 1 つ用意する。
- 注意: WPF の Binding は `XxxChanged` イベントのパターンでも動いており、`INotifyPropertyChanged` へ替えても Binding は動くが、コードビハインドで `XxxChanged` を購読している箇所は書き換えが要る。Binding の誤りはビルドで検出できないため、9-G（G-1）・F-4 と同じ判断（UI の仕様が固まってから、領域を 1 つ切り出して確認）に乗せるのが安全。

---

### 2.5 保守・機能追加の実装コスト

#### R-04 🟡 未使用の `using static DS4Windows.Processing.Mouse`（49 ファイル）
- Phase14-Step7-12（29256a75）の `using` の自動生成で、54 ファイルに `using static DS4Windows.Processing.Mouse;` が入った。`Mouse` のうち静的に参照される公開メンバーはネスト列挙型 `TouchButtonActivationMode`（`Processing/Mouse.cs:94`）だけで、それを使うのは 6 ファイル。**49 ファイルは未使用**。
- 問題: 規約 §3.3 原則3（`using` は使う名前空間だけ）に反し、`Core/`・`Input/`・`Output/` に見かけ上の第2層への依存を作っている（R-02 の集計を汚す）。
- 提案（S。1 Step）: `TouchButtonActivationMode` をトップレベルの列挙型として `Core/Settings/` へ移し（設定値の型。R5）、`using static` を 54 ファイルすべてから削除する。列挙型のシリアライズ名は変わらない（ネスト型でも XML には値名だけが出る）ことを、保存・読込のテストで確認する。

#### R-15 🟢 本番コードの Phase 履歴コメント（2,143 か所）
- 例: `ServiceRegistration.cs` のほぼ全行、`ControlService.cs:101,122,285,292,395` など。「Phase13-Step7 7-d: 旧: 遅延取得デリゲート…」のような経緯の記述が、現在の意図（なぜそうなっているか）より多い。
- 問題: 読むときの雑音になり、機能追加のたびに履歴コメントを足す習慣がさらに増やす。経緯は git の履歴と `docs-forDIMG/` に既にある。
- 提案（運用ルール）: 「新しく書くコメントは意図（なぜ）を書き、Phase 名と旧実装の説明はコミットメッセージと計画書へ」とし、ファイルを触ったついでに古い経緯コメントを縮める。規約 §3.3 原則4 の「意図的に残すコードの TODO」は対象外（残す）。規約への追記が要るので、承認事項。

#### R-16 🟢 テストの構成
- `DS4WindowsTests` の 485 ファイルのうち、Phase 名のテストが 299、`*Guard*` が 110、ソースファイルを文字列として読む検査が 166。
- 問題: ファイルの移動や名前の変更で、仕様と関係なく落ちる（Phase14 では Step5b でテスト中のパス文字列の書き換えが必要になった）。Phase 名では、どの機能のテストかを探しにくい。
- 提案: (1) R-01 のリフレクションの検査で置き換えられる文字列検査（層・名前空間・静的参照のラチェット）を統合する。(2) 対象が恒久的になくなった検査（例: `Global` の件数 0 の確認）は、R-01 の規則（「`Global` という型が存在しない」など）1 行に畳む。(3) 新しいテストは機能名で命名する。いずれも、検査を消す前に 1 件ずつ「何を守っていたか」を確認する（規約 §2.2 の安全弁を守る）。

#### 巨大ファイル（参考。既存の計画どおり）
| ファイル | 行数 | 既存の計画 |
|---|---|---|
| `UI/ViewModels/ProfileSettingsViewModel.cs` | 3,472 | G-1（9-G）。R-10・R-12 と同時に |
| `UI/ProfileEditor.xaml.cs` | 2,229 | F-4 |
| `UI/MainWindow.xaml.cs` | 2,089 | — |
| `Input/DS4Device.cs` | 2,069 | 9-H（Phase16） |
| `Input/Devices/JoyConDevice.cs` | 1,935 | 9-H |
| `Processing/Mouse.cs` | 1,809 | `IMouseEngine`（未着手） |
| `Core/Settings/BackingStore.cs` | 1,499 | — |
| `App.xaml.cs` | 1,328 | F-1c（見送り） |

---

## 3. 良い点（維持すべきもの）

- `Global`・`Program.rootHub`・`ServiceProviderHolder` の撤廃と、`AppHost` の暗黙のホスト構築の禁止。`Func<T>` の遅延取得が本物の循環 2 件だけに絞られ、許可リストのテスト（`Phase13FuncRatchetGuardTests`）で固定されている。
- 第2層の処理が状態なしの部品（`StickDeadZoneProcessor` など 13 個）に分かれ、状態は保持クラス（`StickMotionState` ほか）に集まっている（目標像の方針1に沿う）。
- 統一トリガーディスパッチ（`ITriggerDispatchEngine`／`IActionRegistry`／`IActionBindingBuilder`）で、特殊アクションと Controls タブの経路が 1 本になった。
- 第2層から外への操作が、用途別のポート（`IGyroCalibrationPort`、`IBatteryCheckPort`、`IControlExtrasPort` ほか）として切り出されている。
- フォルダ＝名前空間＝層の対応が、テストで固定されている。

---

## 4. 進め方の提案

既存のロードマップ（Phase15: UI 共通化 → Phase16: Layer 1 のカートリッジ化）を崩さず、前に小さな是正を挟む案。

| 順 | 内容 | 指摘 | 規模 | 理由 |
|---|---|---|---|---|
| 1 | 即効の整理 | R-04、R-03、R-18 の「修正前／修正後」コメント | S | 影響が小さく、R-02 の集計を正しくする |
| 2 | 層の依存のアーキテクチャテスト（現状の違反を許可リストで凍結） | R-01 | S | 以後の是正の物差し。新しい逆向き参照を止める |
| 3 | 依存の向きの是正（共有型を `Core/` へ、`IAppSettingsService`／`IDeviceLayerSettings` の置き場所、`Input` → UI の通知を第2層のイベントへ） | R-02 | M | Phase15 の前に名前空間を確定させる |
| 4 | 静的な状態の撤廃（`SyntheticDispatcher`、`DS4LightBar` の状態、`DS4Devices` のインスタンス化） | R-05、R-06、R-13 | M | Phase16 の前提を軽くする |
| 5 | Phase15（UI 共通化）に、UI 向けの窓口・ViewModel のファクトリー・変更通知の基底を含める | R-09、R-12、R-14、（R-10 の一部） | L | 既存の Phase15・G-1・F-4 と同じ画面を触る |
| 6 | Phase16 の最初に入力カートリッジの記述子と設定の共通化、最後に出力カートリッジ | R-07、R-11、R-08、R-17 | L | 9-H の前に「機種の追加コスト」を先に下げられる |

順 1・2（R-01・R-03・R-04）の個別計画書: `Phase14b-Plan.md`（2026-11-05 作成、承認待ち）。

運用ルールの追加（R-15、R-16 の命名）は、規約 `.github/copilot-instructions.md` の改定として別途承認を得る。

---

## 5. 調査の方法と限界

- 静的な検索（`grep`）と、主要ファイルの読解による。ビルド・テストの実行はしていない（コードは変更していない）。
- 件数は `obj/`・`bin/` を除いた `DS4Windows/` 配下の `.cs` を対象にした実測（2026-11-05）。参照回数はコメント中の出現も含むため、概数として扱う（R-02 の「ファイル数」は `using static ... Mouse` の行だけのものを除外済み）。
- `PathService.cs` の `using DS4Windows.UI` が実際に使われているかは未確認。
- 実行時の挙動（R-03 のフォールバックが実際に通る経路があるか）は、ログで確かめていない。着手時に `[DI]` ログで確認する。
