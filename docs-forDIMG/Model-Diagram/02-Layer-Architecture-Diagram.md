# レイヤー構成図（スタック図）
**（Layered Stack Architecture Specification）**

本ドキュメントは、巨大ファイル群の1クラス1ファイル解体（パイプライン＆プロセッサ化、通信とパケットパースの分離、UIのMediator化）を反映した、DS4Windows-Vader4Pro の実行時スタック構造、データパイプライン、およびレイヤー間の責務境界を定義する。

---

> **実装状況の注記（2026-10-23 Phase9 9-I）**: 上のスタック図は目標像である。実在する部品と未実装の部品の対応は `01-Component-Dependency-Diagram.md` §2.1、登録されているサービスの一覧は `04-Service-Lifecycle-Spec.md` §2.5 を参照。特に、第4層の `StickSettingsSubVM` などのサブ ViewModel（9-G）と、第1層の通信・解析の分離（9-H）は未実装である。依存の向き（第4層 → 第2層 → { 第1層、第3層、横断基盤 }）は、**サービスの単位では**実コードと一致しているが、**型の参照の単位では違反が残る**（2026-11-05 Phase14b の実測。`LayerDependencyRuleTests` が許可リスト `LayerDependencyBaseline` で凍結し、件数の一致を検査する。内訳は末尾の注釈）。

## 1. 実行時スタック構造図

```text
┌──────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                   第4層: UI層 (Presentation Layer)                               │
│  WPF Views (MainWindow, ProfileEditor, BindingWindow, etc.)                                      │
│  ├── Singleton ViewModels: MainWindowsViewModel                                                  │
│  ├── Transient ViewModels: SettingsViewModel / LogViewModel / AboutViewModel                     │
│  └── ProfileEditor 分割ViewModels (Mediatorパターンによる調停):                                   │
│       ProfileSettingsViewModel (親Mediator: 保存・集約・全体設定)                                 │
│       ├── StickSettingsSubVM (LS/RS 感度/デッドゾーン/カーブ)   TriggerSettingsSubVM (L2/R2/モーター)│
│       ├── ButtonMappingSubVM (ボタンリマップ/シフト)          SpecialActionsSubVM (アクション管理) │
│       └── IViewModelFactory (動的パラメータ注入・サブVM群の生成)                                  │
└────────────────────────────────────────────────┬─────────────────────────────────────────────────┘
                                                 │ 依存 (UIバインド・要求発行・イベント購読)
                                                 ▼
┌──────────────────────────────────────────────────────────────────────────────────────────────────┐
│                               第2層: 信号変換層 (Domain & Application Core)                       │
│  【ループ統括】 ControlService (ファサード) ──> IInputLoopCoordinator (高速ループ実行エンジン)       │
│  【状態管理】   MappingPipelineContext Pool (スロット別常駐バッファ: インプレース更新 / Zero-GC)   │
│  【切替制御】   IProfileApplicationService (適用/復帰/Halt) / IAutoProfileService / IProfileSwitcher │
│  【アクション】 ITriggerDispatchEngine (判定→実行の統一) / IActionRegistry (束縛の一覧)  ※Phase8 実装済│
│                 IActionBindingBuilder (束縛の構築) / IManagedActionManager (アクションの状態)    │
│  【マッピング】 IInputMappingPipeline (変換パイプライン親: Context統括)                           │
│                 ├── IButtonProcessor (ボタン変換・シフト)   IStickProcessor (軸/カーブ/AntiSnap)   │
│                 ├── ITriggerProcessor (2段階トリガー)      ITouchGyroProcessor (タッチ/ジャイロ)  │
│                 └── IMouseEngine (Mouse.cs解体先: マウス加速度/OneEuroFilter平滑化/Trackball)     │
└───────────────────────┬──────────────────────────────────────────────────┬───────────────────────┘
                        │                                                  │
          依存 (生データ要求)                                               │ 依存 (変換後データ送出)
                        ▼                                                  ▼
┌───────────────────────────────────────────────┐  ┌───────────────────────────────────────────────┐
│          第1層: 入力監視層 (Ingress)            │  │          第3層: 信号出力層 (Egress)            │
│  IDs4DeviceRegistry (デバイス検出・管理)       │  │  3-a 仮想パッド: IOutputSlotService           │
│  ├─ IDeviceHotplugMonitor (Win32挿抜検知)     │  │  │   └─ ViGEm Client (Xbox360 / DS4 OutDevice) │
│  ├─ IHidTransport (USB / Bluetooth 通信)      │  │  3-b KBM/マクロ: IVirtualKBM (SendInput/Faker)│
│  └─ IInputReportParser / OutputReportEncoder  │  │  │   └─ IMacroPlayer (非同期時系列マクロ再生)  │
│     ├─ DS4ReportParser / Encoder              │  │  3-c 副作用実行:                               │
│     ├─ Vader4ProReportParser / Encoder        │  │      ├─ IProcessLauncher / ElevatedLauncher    │
│     └─ DualSenseReportParser / Encoder        │  │         (IProfileSwitcher は第2層へ移した)│
│                                               │  │      ├─ IUdpMotionService (Cemuhook Motion)    │
│                                               │  │      └─ ILightbarService (LED/発光色計算)      │
└───────────────────────────────────────────────┘  └───────────────────────────────────────────────┘
  ▲                                                  │
  │ 【データパイプライン (Data Pipeline: 250Hz〜1000Hz)】 │
  └───────────── 生HIDパケット ───────────> 変換・補正 ───┴───────────> 仮想ゲームパッド / OS入力
                                              │
                                              ▼
┌──────────────────────────────────────────────────────────────────────────────────────────────────┐
│                          横断基盤・永続化層 (Cross-Cutting Infrastructure)                        │
│  【設定・プロファイル】 IProfileRepository / IProfileXmlStore (Profile.xml CRUD・シリアライズ)    │
│  【アプリ設定】         IAppSettingsService (AppSettings.xml 永続化)                              │
│  【スロット/デバイス】  IOutputSlotStore (OutputSlots.xml) / IDeviceOptionRepository (*OptsDTO)    │
│  【排他制御・OS基盤】   XmlIoLock (プロセス内/スレッド間ロック) / IPathService / IEnvironmentService│
└──────────────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. 採用アーキテクチャ方針の詳細解説

### 方針1：ステートレスSingletonプロセッサ ＋ `MappingPipelineContext` によるZero-GCインプレース更新
* **課題：** スティックのアンチスナップバックや `OneEuroFilter` などの平滑化には「前フレームの座標や時間」という内部状態が必要だが、毎秒1000回のループでインスタンスを `new` するとGCプレッシャーで遅延が発生する。
* **解決策：**
  * 各プロセッサ（`StickProcessor` 等）は**完全ステートレスなSingleton**とする。
  * スロットごとに1つ常駐するバッファオブジェクト `MappingPipelineContext` を事前確保（プール）する。
  * このコンテキスト内に「現在フレームの生入力」「前フレームの状態履歴（HistoryState）」「変換後出力バッファ」を保持し、各プロセッサが順次**インプレース（破壊的書き換え）**で更新していく。これにより、毎フレームのアロケーションを完全にゼロ（Zero-GC）化する。

### 方針2：Layer 1 における通信（Transport）と解析（Parser/Encoder）の分離
* **課題：** `DS4Device.cs` や `Vader4ProDevice.cs` にOS通信ハンドルとバイト解析コードが同居していると、実機をPCに接続しないとテストができない。
* **解決策：**
  * OSとの入出力（`ReadFile`/`WriteFile`）を担当する `IHidTransport` と、バイト列を構造体にパースする `IInputReportParser`、フィードバック用パケットを構築する `IOutputReportEncoder` を分離。
  * パケット解析クラスはOS非依存の純粋なロジックとなるため、Vader 4 Pro の独自ボタン（C/Zボタン、背面M1〜M4）のデコード検証などをテストコードで100%自動検証可能にする。

### 方針3：UI層における Mediator パターンによる親・子ViewModelの調停
* **課題：** `ProfileSettingsViewModel.cs`（158KB）を分割する際、サブVM同士が過度なイベント駆動で通信すると循環参照や更新順序の破綻が起きやすい。
* **解決策：**
  * 親の `ProfileSettingsViewModel` が Mediator となり、各サブVM（`StickSubVM`, `TriggerSubVM` 等）を直接保持・調停する。
  * 「保存」ボタンが押された際は、親VMが各子VMから設定スライスを吸い上げて `ProfileDTO` に統合し、`IProfileXmlStore` に一括保存を要求する。

### 方針4：3-b KBM出力における「送出」と「ライフサイクル」の分離
* **課題：** 現行の `OutputKBMHandlerAdapter` は `Global.outputKBMHandler` への読み取り専用委譲で、出力ハンドラの生成・差し替え・フォールバック切替の口が無く、`ControlService` が `Global` の static フィールドを直接書き換えている。
* **解決策：**
  * 送出（キー・マウス出力）は `IVirtualKBM`、ハンドラの生成・破棄・フォールバック切替・マッピング初期化は `IVirtualKBMLifecycle` に分ける。
  * 上記のスタック図の 3-b `IVirtualKBM` に付随するインターフェースとして扱う（図の枠は変更しない）。ライフサイクル操作を使うのは第2層の `ControlService` のみで、送出側の利用者（Actions・`IMouseEngine`）には公開しない。

---

## 3. 「データフロー」と「依存関係」の比較対比

```text
【データの流れる方向 (Data Flow - 毎秒250〜1000回)】
  [物理コントローラー]
         │ (生HIDパケット)
         ▼
  [1. 入力監視層] (IHidTransport ──> IInputReportParser)
         │ (Parsed RawInputState)
         ▼
  [2. 信号変換層] (IInputMappingPipeline ──[Contextインプレース更新]──> 各Processor)
         │ (Transformed State / SpecialActions)
         ▼
  [3. 信号出力層] (IOutputSlotService / IVirtualKBM)
         │ (仮想パッド信号 / キーマウ)
         ▼
     [PCゲーム / OS]

【プログラムの依存方向 (Dependency Flow - DIP原則)】
  [4. UI層 (Views / ViewModels)]
         │
         │ (利用)
         ▼
  [2. 信号変換層] ────────────┐
   │            │             │
   │ (利用)     │ (利用)      │ (利用)
   ▼            ▼             ▼
  [1. 入力監視層] [3. 信号出力層] [横断基盤・永続化層]
```

---

%% 注釈補強（2026-09-19 Phase6-Step2 実地確認・決定D1〜D3反映）
%% - 方針4（3-b の送出とライフサイクルの分離、`IVirtualKBMLifecycle`）を追加（決定D3）。スタック図・依存方向図の構造は変更なし。
%% - [Phase13 で解消] 依存方向（上位→下位）に反する過渡期の逆依存（`OutputSlotService` → `ControlService` 等）は、Phase13（Step5〜7）で `IDeviceSlotState`／`IProfileLoadTarget`／`IOutputDeviceTypeState` を介する形に置き換え、なくなった。経緯は 04-Service-Lifecycle-Spec.md §4。

%% 注釈補強（2026-09-18監査結果反映、構造変更なし）
%% - データパイプライン（生HID→変換→仮想パッド/OS入力）は現状のホットパス（ControlService入力ループ、Mouse系加速度処理）と一致。Zero-GCインプレース更新（MappingPipelineContext）の原則を維持。
%% - 依存方向（UI→変換→入力/出力→横断基盤）はDIP原則に従い、現状の監査結果（Global型503メンバの各層分散、ScpUtilの横断基盤層分解、MappingのPipeline+Processor分解）と完全一致。
%% - ScpUtil.cs（255件静的宣言）の横断基盤層への分解は理想構造と一致するが、参照と宣言の区別を維持（§2抽出問題記録済み）。
%% - Phase5証跡（Step14/15未完了）の前提条件を維持（証跡存在確認済み、内容検証別途必要：Phase5-Evidence-Check.md 参照）。
%% - c除外（≈304件）の改修対象外原則を維持（Classification-Metrics.md, C-Classification-Count.md 参照）。
%% 注釈補強（2026-10-13 Phase6-Step8 反映、構造変更なし）
%% - `ProfileEditor`（View）の Global 直接参照を解消し、保存・ブランク読込・キャッシュ更新を親 `ProfileSettingsViewModel`（Mediator）へ移した。親が保存を調停し、コントローラーへの再適用は保存の通知を受けた `MainWindow` が行う（Step7b 決定8）。View が設定サービス（`IAppSettingsService`／`IProfileSettingsService`）を直接受け取る辺があるが、これは画面のレイアウトと編集スロットの補正値の参照で、依存方向（UI→下位層）に反しない。図の構造は変更しない。
%% 注釈補強（2026-10-17 Phase7-Step8 反映、構造の目標は変更なし）
%% - 現行の Mapping.cs（理想構造では IInputMappingPipeline 以下へ分解される予定の対象）は、DI の Singleton（IMapping）の instance になった。状態は 3 つの保持クラス（合成入力・マクロ・スティック／ジャイロ／トリガー）に分かれ、いずれもコンテナの Singleton。サービスへの静的な束縛（AppHost／ServiceProviderHolder／Global のフォールバック）はない。
%% - 出力 KBM ハンドラの実体は OutputKBMHandlerAdapter（第3層）が持つ。第2層（Mapping）→ 第3層（IVirtualKBM）の依存方向は、インターフェース経由のままで変わらない。
%% 注釈補強（2026-10-18 Phase7-Step10e 反映、構造の目標は変更なし）
%% - 依存方向（上位 → 下位、またはインターフェース経由）に反する静的な窓口は、Phase7 で解消した: 第 1 層の設定は IDeviceLayerSettings（10d）、スロット数の上限は IEnvironmentService.ControllerSlotLimit（10e）。
%% - 第 4-b 層（Composition Root）の AppHost は、暗黙にホストを作らない。起動（App.xaml.cs）が明示的に作る。
%% - 持ち越し: 第 1 層（DS4Devices／DS4LightBar／HidDevices）の静的な状態。（Actions の ServiceProviderHolder は Phase13-Step4 で撤廃済み）
%% 注釈補強（2026-10-18 Phase7 完了時点のまとめ。構造の目標は変更なし。各 Step の詳細は 03・04 の注釈と Phase7-Completion-Report.md）
%% - 第 4-b 層（Composition Root）: App の起動が EnvironmentService → BackingStore → PathService の順にブートストラップのインスタンスを作り、CreateHost(config, parser) でコンテナに渡して同じインスタンスを登録する。AppHost は暗黙にホストを作らず、終了時に Dispose する。
%% - 第 4-c 層（サービス群）: BackingStore（設定の唯一の実体）と、永続化の型、プロファイル適用（ProfileApplicationService）、出力デバイス種別の所有（OutputSlotService）。
%% - 第 2 層: Mapping が DI の Singleton の instance になり、第 3 層（IVirtualKBM）への依存はインターフェース経由のまま。
%% - 依存方向に反する静的な窓口は、起動前に必要なもの（PathService／EnvironmentService の状態）を除いて解消した。残りは第 1 層の静的な状態のみ（Actions の ServiceProviderHolder は Phase13-Step4 で撤廃）。
%% 注釈補強（2026-10-19 依存図の検証［Phase8 前レビュー］。依存の向きの是正のみ）
%% - IProfileSwitcher を、第3層（3-c）から第2層（【切替制御】）へ移した。プロファイルの切替はアプリ内部の状態遷移で、IProfileApplicationService（第2層）に依存するため、第3層に置くと第3層 → 第2層の上向きの辺と循環ができる。詳細は 01 の注釈。
%% - 依存の規則: UI層(4) → 信号変換層(2) → { 入力監視層(1)、信号出力層(3)、横断基盤 }。第1層・第3層・横断基盤から第2層への参照は禁止（`.github/copilot-instructions.md` §1 も同様に書き直した）。
%% 注釈補強（2026-10-19 Phase8-Step2 W5［段階 A］: 統一トリガーディスパッチの目標像を追加。構造の目標は変更しない）
%% - 第2層の【アクション】の行を、目標像（ITriggerDispatchEngine／IActionRegistry／IActionBindingBuilder／IManagedActionManager［状態］）に書き換えた。特殊アクションと Controls タブの「トリガー成立→処理実行」を、第2層の統一エンジンに集約する（Phase8）。未実装。Step7 の各 PR で種別ごとに実装済みになる。
%% - 入力の 1 フレームは 2 段階: 通常の割り当ての前（トリガーの実行・ボタンの出力の消去）と、後（Button の毎フレームの出力）。依存方向（UI → 変換 → 入力／出力／基盤）は変更なし。

%% 注釈補強（2026-11-04 モデル図の現状合わせ）
%% - Phase8 の統一トリガーディスパッチは実装済み（スタック図の「※Phase8 目標像」を「※Phase8 実装済」に改めた）。
%% - Phase12: 第1層に IMouseInputHook（マクロ記録用の画面全体のマウス監視）、第3層に IWindowFocuser／IWindowSystem（ウィンドウの前面化・カーソル移動）が加わった。依存の向き（第4層 → 第2層 → { 第1層、第3層、横断基盤 }）は変えていない。ただし RecordBox は IMouseInputHook／IWindowSystem を直接受けており、是正候補（01 の凡例を参照）。

%% ---- Phase13 完了（2026-11-04）----
%% - ControlService は 17 引数の統括役に縮小した（Phase13 開始時は 34 引数）。第 2 層から第 1 層・第 3 層への依存は、IDeviceSlotState／IInputReportProcessor／IOutputDeviceTypeState などのポート経由。
%% - 依存方向に反する逆依存と、Host 窓口 4 本（IVirtualPadHost／IServiceLifecycleHost／IUdpMotionHost／IDeviceSlotHost）はなくなった。残る Func による遅延取得は、本物の循環の 2 件のみ。

%% ---- Phase14 完了（2026-11-04。名前空間・フォルダの再編）----
%% - フォルダ＝名前空間（DS4Windows.<フォルダ>）に統一した。層とフォルダの対応は次のとおり（Phase14NamespaceFolderGuardTests が検査）。
%%     第1層  Input/（Devices、Hid）                     … DS4Device、InputDevices/*、HidLibrary
%%     第2層  Processing/（Abstractions、Actions、Services）… Mapping、ControlService、SyntheticDispatcher、Actions サブシステム、入力処理サービス、第2層が使うポート（IWindowSystem 等の利用側の契約）
%%     第3層  Output/（Devices、Kbm、Services）           … DS4OutDevice／Xbox360OutDevice、OutputKBM、OutputSlotManager、仮想パッド・HidHide・UDP・OSC のサービス
%%     第4-a  UI/（ViewModels、Converters、Themes、Services）… DS4Forms 一式、通知、ウィンドウ／ViewModel のファクトリ
%%     第4-b  Composition/（Startup）                     … AppHost、ServiceRegistration、起動手順（Startup）。App.xaml(.cs)・StartupMethods.cs はアプリの起点としてルートに残した（DI/ フォルダは廃止）
%%     横断基盤・永続化  Core/（Abstractions、Logging、Utilities、Platform、Settings、Persistence〔Dto、ProfileSections〕、Profiles、Updater）
%% - ルート名前空間は DS4Windows に統一した（DS4WinWPF は廃止。csproj の RootNamespace／StartupObject も DS4Windows）。
%% - インターフェースの置き場所: 実装と同じ層の Abstractions/。第2層が第1・3層・UI の実装を使うポートは、使う側（Processing/Abstractions）に置いた（依存の向きを保つため）。

%% ---- Phase14b-Step1〜3（2026-11-05。層の依存の検査と、最初の是正）----
%% - 層の依存を、アセンブリの実際の型参照（メソッド本体を含む）で検査するテスト LayerDependencyRuleTests（ArchUnitNET）を加えた。規則は L1〜L7（Phase14b-Plan.md §4）。
%%     禁止（許可リストで凍結・件数の一致を検査）: L1 Core → 第2層・UI・起点、L3 第1層 → 上位、L4 第3層 → 第2層の具象・UI、L5 第2層 → UI・起点、L6 UI → 起点（App／StartupMethods）
%%     記録のみ: L2 Core → 第1層・第3層、L7 UI → 第1層・第3層
%% - 実測: 禁止 108 件（L1 81、L3 5、L4 1、L5 15、L6 6）→ Phase14b-Step3 の時点で 102 件。記録のみ L2 70、L7 80。
%% - 主な原因: 共有のデータ型（SpecialAction、BezierCurve、MacroExecuteMode ほか）と、Core が実装する契約（IAppSettingsService、IProfileLoadTarget）が第2層にある。
%%   UI のファクトリーの契約（IEditorWindowFactory／IMainWindowFactory／IViewModelFactory）が Processing/Abstractions にあり、UI の型を参照している（L5 の 15 件のすべて）。
%% - 是正済み: Mouse のネスト型 TouchButtonActivationMode → Core/Settings、INotificationService／NotificationEventArgs → Core/Abstractions、AppLogger のフォールバック（2 つ目の BackingStore）→ NullNotificationService。
%% ---- Phase14b-Step5〜9（2026-11-05。R-02 本体）----
%% - フォルダの追加: Input/Abstractions（第1層が使い、第2層が実装する契約。IDeviceLayerSettings）、UI/Abstractions（UI のファクトリーの契約。IEditorWindowFactory／IMainWindowFactory／IViewModelFactory）。
%% - Core へ移した型: SpecialAction、MacroExecuteMode(s)、ProfileActions、ControlActionData（Core/Settings）、BezierCurve（Core/Utilities）、WindowTargetMatcher（Core/Platform）、
%%   SelectedProfileChangedEventArgs、WindowCandidate、WindowFocusResult、CapturedWindow、IAppSettingsService、IProfileLoadTarget、ITouchPadGyroToggles（新設。Mouse が実装）（Core/Abstractions）。
%% - 第1層 DS4Devices の UI 参照（静的な ShowToast）は AppLogger.LogToTray の直接呼び出しへ。BackingStore.ProfileSettings のフォールバック（第2層の具象を new）は撤去（未登録なら例外）。
%% - 禁止の規則の違反: 108 → 18 件（L1 11、L4 1、L6 6。L3・L5 は 0）。残りは Core → Actions の IManagedActionManager／SyntheticInputState、AppLogger → ProfileChangedEventArgs ほか、Core/UI → App（起点）など。
%% ---- Phase14b-2（2026-11-05。残りの片付け）----
%% - 禁止の規則の違反は 18 → 0 件（LayerDependencyBaseline は空）。記録のみ: L2（Core → 第1層・第3層）75 件、L7（UI → 第1層・第3層）80 件。
%% - 小さな契約・型の Core への移動（IDeviceStateAccessor、IRemoved*Prompt、IInstanceIdentifiable、ProfileChangedEventArgs／ProfileChangeSource）、WindowPlacementHelper → UI。
%% - Core が第2層・UI を使っていた箇所: 実行時状態のリセットを ISpecialActionRuntimeReset（Core の契約・第2層の実装）に、トレイアイコンの画像パスは UI が直接引く形に。
%% - UI が起点の App を使っていた箇所: IUiThemeAndLanguage・IIpcResultWriter（UI/Abstractions）と、App へ委譲する Composition のアダプター。COPYDATASTRUCT は Core/Platform へ。
%% - 静的な窓口の撤廃: App.requestClient → DI の HttpClient と IChangelogService（Changelog をインスタンス化）、StartupMethods → IStartupEntryService（Core/Platform/StartupEntryService）。

%% ---- Phase14c（2026-11-05）----
%% - フォルダの追加: Core/Model（層をまたいで共有する値の型と状態の持ち主）。DS4Controls・DS4Color・DS4ControlNames・InputDeviceType・TriggerEffects・OutContType・X360Controls・X360ControlTables（旧 第1層・第2層）と、
%%   LightbarOverrides（新設。旧 DS4LightBar の静的な配列）。上の Phase14 の対応表の「横断基盤・永続化 Core/」に Model が加わる。
%% - 第2層の SyntheticDispatcher（静的な橋渡し）は撤廃した（上の対応表の第2層の行は Phase14 時点の記録）。合成キーの送出は ISyntheticDispatcher（SyntheticDispatcherService）だけ。
%% - 記録のみの規則は件数のラチェット（LayerDependencyRuleTests.RecordOnlyLayerDependencies_MatchTheCountRatchet）: L2 29 件、L7 30 件。値の型を Core/Model へ移したことで、Core・UI からの第1層・第3層への参照の大半が消えた。
%%   残りの L2 は Core（永続化・設定）→ 機器と出力の型（DS4Device、DualSenseDevice、OutputDevice、OutputSlotManager ほか。Phase16）、L7 は UI → 機器・状態・出力スロットの型（DS4Device、DS4State、OutputSlotManager ほか。Phase15 の R-09、Phase16 の R-17）。

%% ---- Phase15-1 完了（2026-11-08）----
%% - 第 4 層 UI から第 1 層・第 3 層の具象型（DS4Device、OutSlotDevice、Xbox360OutDevice など）と ControlService への参照をなくした。UI が見る面は第 2 層の契約（IControllerSlotView／Query／Commands、IControllerReadingsSource、IMacroRecordingState、IOutputSlotView）で、通知の送り主はその面自身。
%% - 層の依存の記録のみの規則 L7（UI → 第 1 層・第 3 層）の基準値は 5。
