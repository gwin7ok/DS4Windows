# Phase13-Step3 個別計画書: 状態を持たないホストの分離（イベントスレッド・振動・KBM 初期化）

作成日: 2026-11-04
状態: **承認済み・実装完了（2026-11-04。結果は Phase13-Status.md）**
ブランチ: `Phase13`（Step2 は `668bdea6` でコミット済み）
上位: `Phase13-Plan.md` §4（Step3）、§3.4（`IUdpMotionHost` の訂正）

## 1. 目的

`ControlService` が実装している `IVirtualPadHost` と `IServiceLifecycleHost` の中身（イベントスレッド、振動の調整、KBM 出力の初期化）は、スロットの配列を使わない。これを独立のサービスに移し、2 つのホスト IF と、それに対する `Func<IVirtualPadHost>`／`Func<IServiceLifecycleHost>` を撤廃する。`IUdpMotionHost` のうち `QueueOnEventThread` も、同じサービスで置き換える（配列を使う残りの 3 メンバーは Step5）。

## 2. 現状（2026-11-04 の実測）

### 2.1 イベントスレッド（`ControlService.cs`）

- 生成: コンストラクターで専用スレッドを開始（名前 "ControlService Events"、`BelowNormal`、`IsBackground`）。スレッド内で `Dispatcher.CurrentDispatcher` を `eventDispatcher` に代入し、`Dispatcher.Run()`。
- 使用: `QueueOnEventThread`（`BeginInvoke`。`IUdpMotionHost`／`IVirtualPadHost`）、`IServiceLifecycleHost.InvokeOnEventThread`（同期の `Invoke`）、`OutputslotMan_ViGEmFailure`（`BeginInvoke`。**ControlService 内部**）、`ShutDown`（`InvokeShutdown` → null 代入 → `Join`）、公開プロパティ `EventDispatcher`。
- 利用者: `UdpMotionService`（2 か所）、`VirtualPadService.QueueOnEventThread`（`OutputSlotService` が 2 か所で使う）、`ServiceLifecycle`（`InvokeOnEventThread` 2 か所）。
- **`EventDispatcher` プロパティの外部の利用者は 0 件**（本番・テスト。コメントの言及のみ）。
- 既知の細かい点: スレッド内で代入するため、コンストラクターの直後に使うと `eventDispatcher` が null の窓がある（現状も同じ）。

### 2.2 振動

- `ControlService.SetDevRumble(device, heavy, light, deviceNum)`: ブースト（`GetRumbleBoost`）と反転（`InverseRumbleMotors`）を適用して `device.setRumble`。`IVirtualPadHost.SetDevRumble` として `VirtualPadService`（161 行）が呼ぶ。
- `ControlService.setRumble(heavy, light, deviceNum)`: スロットの配列からデバイスを引いて `SetDevRumble`。利用者: `DefaultControlExtrasPort`、`ProfileEditor`（2 か所）。**これは配列を使うので `ControlService` に残す**（Step10 で利用者を切り替える）。

### 2.3 KBM 出力の初期化

- `InitOutputKBMHandler`（private）: `IServiceLifecycleHost.InitializeOutputKbmHandler` として `ServiceLifecycle`（96 行）が呼ぶ。`cmdParser.VirtualkbmHandler`、`IVirtualKBM`、`IVirtualKBMLifecycle`、`IProfileSettingsService.OutputKBMMapping` を使う。
- `RefreshOutputKBMHandler`（public）: **呼び出し元が 0 件**。経緯（規約 §2.4）: 追加は初期の実装で、呼び出し元は 38a41ffa「Remove output handler refresh after driver install window」で削除された。残ったのは本体だけ。判断: **使うべきでない孤立実装（廃止済みの残骸）と判断して削除**する（`VirtualKBMLifecycleTests` はコメントでの言及のみ）。
- `cmdParser` は `InitOutputKBMHandler` のためだけに使う。`_kbmLifecycle` は `InitOutputKBMHandler` と `RefreshOutputKBMHandler` のみ。`_virtualKBM` は `Sync()` の 1 か所（829 行）が残る。

### 2.4 ホスト IF の現状

`IVirtualPadHost`（`SetDevRumble`、`QueueOnEventThread`）、`IServiceLifecycleHost`（`InvokeOnEventThread`、`InitializeOutputKbmHandler`）、`IUdpMotionHost`（`GetPadDetailForIdx`、`GetCurrentState`、`GetTempState`、`QueueOnEventThread`）。`ServiceRegistration.cs` に `Func<IVirtualPadHost>`、`Func<IServiceLifecycleHost>` の登録と、3 つのホストの転送登録（312〜315 行付近）がある。

## 3. 設計

### 3.1 新設する型（1 ファイル 1 型。契約は `DS4Windows/DI/`、実装は `DS4Windows/DS4Control/Services/`、名前空間は契約が `DS4Windows.DI`、実装が `DS4Windows.Services`）

| 型 | 役割 | 主なメンバー |
|---|---|---|
| `IEventThreadDispatcher`／`EventThreadDispatcher` | 専用のイベントスレッドの所有 | `void Queue(Action)`（旧 `BeginInvoke`）、`void Invoke(Action)`（旧同期 `Invoke`）、`void Shutdown()`（旧 `InvokeShutdown` → `Join`） |
| `IDeviceRumbleService`／`DeviceRumbleService` | ブースト・反転つきの振動 | `void SetDevRumble(DS4Device, byte heavy, byte light, int deviceNum)`（本体は `ControlService.SetDevRumble` をそのまま移す。`IProfileSettingsService` を使う） |
| `IOutputKbmInitializer`／`OutputKbmInitializer` | KBM 出力の初期化 | `void Initialize()`（本体は `InitOutputKBMHandler` をそのまま移す） |

要点:
- **イベントスレッド**: スレッド名・優先度・`IsBackground`・破棄の順序（`InvokeShutdown` → `Join`）を変えない。`ControlService.ShutDown` は、スロット管理の停止と出力スロットの保存のあとに `Shutdown()` を呼ぶ（順序は現状のまま）。`Shutdown()` 後の `Queue`／`Invoke` は何もしない（現状は `eventDispatcher = null` による `NullReferenceException`。終了処理中の呼び出しが例外にならない方向の変更で、通常の動作には影響しない）。スレッドの開始直後に使われても null にならないよう、スレッド内の `Dispatcher` の確定を待つ（`ManualResetEventSlim`。現状の「null の窓」を塞ぐだけで、順序は変えない）。
- **`OutputKbmInitializer`**: 起動引数は `IStartupArguments.Parser`（未設定なら `new ArgumentParser()`）を、**`Initialize()` の呼び出し時に読む**。`ServiceRegistration.cs` のコメントにある「`ControlService` はホスト構築前に解決してはならない」制約は、`ControlService` の生成時に値を保持していたことが理由で、`Initialize()` は `Start` の中で呼ばれる（ホスト構築後）ため、この制約はむしろ緩む。
- **`ControlService`**: `IEventThreadDispatcher` と `IDeviceRumbleService` を受ける。`cmdParser` と `kbmLifecycle` の引数は不要になる。**引数の数は 28 のまま**（−2 +2）。Step10 で `setRumble` の利用者を切り替えれば `IDeviceRumbleService` も外れる。
- `ViGEmFailure` の購読は `ControlService` に残し、`BeginInvoke` を `_eventThread.Queue` に替える（`LoopControllers`／`InServiceTask`／`Stop` を使うため）。

### 3.2 廃止・変更

| 対象 | 変更 |
|---|---|
| `IVirtualPadHost`／`IServiceLifecycleHost` | 削除。`ControlService` の実装宣言と、明示的実装を削除 |
| `Func<IVirtualPadHost>`／`Func<IServiceLifecycleHost>`（`ServiceRegistration.cs`）と転送登録 2 件 | 削除 |
| `IUdpMotionHost.QueueOnEventThread` | 削除。`ControlService.QueueOnEventThread`（public）も削除（利用者は `UdpMotionService` のみ） |
| `ControlService.EventDispatcher`（public）、`RefreshOutputKBMHandler`、`InitOutputKBMHandler`、`SetDevRumble`（public） | 削除（利用者 0 件、または移設） |
| `VirtualPadService` | `Func<IVirtualPadHost> host` → `IEventThreadDispatcher` と `IDeviceRumbleService`。`QueueOnEventThread`（`IVirtualPadService` の契約）は `IEventThreadDispatcher.Queue` へ委譲 |
| `UdpMotionService` | `IUdpMotionHost.QueueOnEventThread` の呼び出しを `IEventThreadDispatcher.Queue` へ（`Func<IUdpMotionHost>` は配列用に残る。Step5） |
| `ServiceLifecycle` | `Func<IServiceLifecycleHost>` → `IEventThreadDispatcher`＋`IOutputKbmInitializer`（`Invoke`／`Initialize`） |
| `ControlService.cs` | 上記の削除と注入。ファイル全体は再生成せず、該当箇所だけを置換する（§3.2） |

## 4. 規約への適合

- **§2.2**: 安全弁を移す対応表。(1) イベントスレッドの破棄順（`ShutDown`: スロット管理 → 出力スロットの保存 → `InvokeShutdown` → `Join`）は同じ順序。(2) `Stop` 内のイベントスレッドでの同期実行（`Invoke`）は同じ。(3) ViGEm 失敗時の `LoopControllers = false` → `InServiceTask` の待ち → ログ → `Stop` は同じ。(4) 振動のブースト・反転の式は文字どおり移す。(5) KBM 初期化の手順（`DetermineHandler` → `Connect` → フォールバック／FakerInput のバージョン → `InitializeMapping` → `PopulateConstants`／`PopulateMappings`）は文字どおり移す。
- **§2.3**: ログの文言・レベルを変えない。移設で変わるのは呼び出し位置のみ（`ViGEmPluginFailure` のログは `ControlService` に残る）。
- **§2.4**: `RefreshOutputKBMHandler` は上記のとおり削除と判断（プラン承認で確定）。`EventDispatcher` は外部の利用者が 0 件の公開プロパティで、新サービスに窓口があるため削除。
- **§3.1**: 新サービスはコンストラクター注入のみ。**§3.3**: 1 ファイル 1 型。名前空間は契約 `DS4Windows.DI`、実装 `DS4Windows.Services`。**§3.4**: R1／R2／R6 のとおり配置。ファイルの移動なし。
- **ラチェット**: `Global`／`AppHost`／`ServiceProviderHolder` を増やさない。基準値の変更なし。
- **OSS の比較（調査の指示）**: 専用スレッドで `Dispatcher.Run` を回し、`BeginInvoke`／`Invoke` で投入し、`InvokeShutdown` → `Join` で止める形は、Microsoft の `Dispatcher` の仕様どおり（停止後の `Dispatcher` は再開不可。停止後の `BeginInvoke` は Aborted）。検索では、この形を包む標準のライブラリは見つからなかった（[Dispatcher Class](https://learn.microsoft.com/dotnet/api/system.windows.threading.dispatcher)）。既存のコードをそのまま移すのが最小で、停止後の呼び出しを無害にする点だけを追加する。

## 5. テスト計画

- 新規:
  - `EventThreadDispatcherTests`: スレッド名・優先度・`IsBackground`、`Queue`／`Invoke` が別スレッドで実行されること、`Shutdown` 後に `Queue`／`Invoke` が例外にならないこと、生成直後の `Queue` が失われないこと。
  - `DeviceRumbleServiceTests`: ブースト 100％、上限 255 でのクリップ、反転あり／なしの `setRumble` の引数（旧 `ControlService.SetDevRumble` との同値）。
  - `OutputKbmInitializerTests`: 既存の `VirtualKBMLifecycleTests` の考え方で、フォールバック、FakerInput のバージョン、マッピングの初期化の順序。
  - `Phase13Step3HostRemovalGuardTests`: `IVirtualPadHost`／`IServiceLifecycleHost` の型が無いこと、`ServiceRegistration.cs` に `Func<IVirtualPadHost>`／`Func<IServiceLifecycleHost>` が無いこと、`ControlService` が 28 引数で `cmdParser`／`IVirtualKBMLifecycle` を含まないこと。
- 更新: `Phase9EVirtualPadGuardTests`（`Func<IVirtualPadHost>` と同一インスタンスの検査を、新サービスの検査へ）、`Phase9EVirtualPadServiceTests`（`RecordingHost` を新サービスの偽実装へ。`new VirtualPadService(` 6 か所）、`Phase9E6LifecycleGuardTests`（`ServiceLifecycle` の生成 1 か所と、`IServiceLifecycleHost` の検査）、`FakeUdpMotion`・`Phase9EUdpMotionServiceTests`・`Phase9EUdpMotionGuardTests`（`IUdpMotionHost` から `QueueOnEventThread` を削除。`new UdpMotionService(` 5 か所）。
- `Debug ALL` で警告 0・エラー 0。

## 6. 実機確認

Phase 末（Q8）。対象項目: H2（切断）、H6（開始・停止・終了時にハングしない）、H8（振動）、H9（UDP モーション・OSC・KBM 出力）。この Step 固有の注意: 終了時（`ShutDown`）にハングしないこと。

## 7. リスク

- 中。R5（イベントスレッドの移動で終了時にハング）: スレッドの生成・破棄の手順を文字どおり移し、停止後の呼び出しを無害にした。テストで停止の順序と、停止後の呼び出しを固定する。
- R6: `OutputKbmInitializer` が起動引数を `Initialize()` 時に読む。`IStartupArguments.Parser` はホスト構築時に設定済みで、`Start` はその後なので、値は同じ。

## 8. 決めてほしいこと

- なし（`RefreshOutputKBMHandler` と `EventDispatcher` の削除、スレッドの開始待ちの追加は、本計画書の承認をもって確定とする）。

## 9. 完了条件

2 つのホスト IF と 2 つの `Func` 登録が無い。`ControlService` の引数は 28 のまま、`cmdParser`／`kbmLifecycle` が無い。全テスト合格、警告 0・エラー 0。モデル図 03（ホスト IF の辺、`Func` の記述）と 04（イベントスレッドの所有者）を更新。`Phase13-Status.md` を更新。
