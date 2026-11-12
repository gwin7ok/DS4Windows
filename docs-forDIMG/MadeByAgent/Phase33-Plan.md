# Phase33 計画書: 終了の手順の集約と、登録の具象依存の解消（コードレビュー 2026-11 の 1-2・1-3・1-4）

作成日: 2026-11-11
状態: **未着手。承認待ち**（リスクが高い。Phase29〜32 のあとに行う）
出典: `docs-forDIMG/Code-Review-2026-11/01-DI.md`
関連: `Structure-Review-2026-11.md` の C（Phase24 で `App.xaml.cs` を 1,323 → 約 940 行にした続き）、`Phase27-Plan.md`（`RuntimeBootstrapper`）

## 目的
1. アプリの終了の手順（破棄の順序）を、`App` の 1 メソッドの行の並びではなく、1 つの型と、順序を確かめるテストで守る。
2. `App` が個別に解決して持つサービス（約 15 個）を減らす。
3. DI 登録の中の、具象へのキャスト（3 か所）をなくす。

## 現状（調査）
- `App.CleanShutdown`（`App.xaml.cs:920`〜`975`）の並び: サービスの停止（`_lifecycle.Stop(immediateUnplug: true)`）→ `_runtimeBootstrapper.Shutdown()`（仮想パッドの停止、出力スロットの保存、イベントスレッドの停止）→ 設定の保存（`skipSave` でないとき）→ タイマーの解像度を戻す → 単一起動の停止 → IPC の破棄 → ボタンのコントローラーの破棄（全スロット）→ `AppHost.Dispose()` → ログのフラッシュ。
- 登録されたサービスの多くは `IDisposable` を実装しておらず、`AppHost.Dispose()` は実質何も破棄しない。
- `App` のフィールド（`App.xaml.cs:70`〜`88`）と `InitializePostHostServices`（同 `652`〜`672`）: 約 15 個を `AppHost.GetService` で解決する。
- キャスト: `ServiceRegistration.Processing.cs:175`（`(IConfigurableDeviceLookup)`）、`ServiceRegistration.Wiring.cs:50`（`as ProfileSettingsService`）、`AppHost.cs:129`（`as DefaultActionManager`）。
- 起動の順序は `StartupOrderGuardTests`、開始・停止の安全弁は `LifecycleGuardTests` が、ソースの並びで固定している。

### 追加（2026-11-12。外部レビューの照合 `Code-Review-2026-11/07-External-Review-2026-11-12.md` から統合）
- 外部レビューは「`App.xaml.cs` に、起動・単一起動・トレイ・例外処理・サービスの起動が集中している。100〜200 行にできる」と述べる。照合の結果: トレイは `App` にない（`MainWindow` と `TrayAppearanceController`）。単一起動と IPC は Phase24 で部品へ出してある。残るのは、起動の手順、引数の処理、終了、未処理例外。
- `App.xaml.cs`（978 行、メソッド 29 個）の長いメソッド: `CheckOptions` 73 行、`Application_Startup` 72 行、`CleanShutdown` 61 行、`RotateLogsAndLogStartupDiagnostics` 50 行、`ConfigureLoggingAndDiagnostics` 49 行、`AttemptSave` 45 行、`ShowMainWindowAndFinish`・`RunFirstLaunchSetup` 各 40 行。
- **100〜200 行は目標にしない**。`App` は WPF の入口として、未処理例外・セッションの終了・起動の順序の指揮を受け持ち、起動の手順は順序への依存が強い（`StartupOrderGuardTests`）。現実的な目安は、判断と手順を部品へ出して **400 行前後**（33-2・33-3・33-4・33-4b をすべて行った場合）。行数は結果であって、目的にはしない。

## 方針（決定事項の案）
1. **順序は 1 行も変えない**。移すだけにする（ドライバーの破棄順序のガードレール。`Phase5-Plan.md` §5）。
2. 終了の手順は、`Composition/Startup` の部品（仮称 `AppShutdownSequence`）に置き、必要なサービスをコンストラクターで受ける。`App` は、その 1 つを解決して呼ぶ。ホストの構築より前に終了する場合（早期終了）は、今と同じく何もしない分岐を保つ。
3. 順序の検査は、ソースを文字列で読む形ではなく、偽物が呼ばれた順を記録する形で書く（規則 4 章）。既存のソースの並びのガードは、新しい検査が同じものを守れると確かめてから置き換える。
4. サービスに `IDisposable` を付けて、コンテナの破棄の順（生成の逆順）に任せる方式は**採らない**。生成の順は解決の順で決まり、破棄の順を保証しにくい。
5. キャストは、登録の形で解く（具象を 1 つ登録し、2 つの契約がそれを返す。`OutputKBMHandlerAdapter` の前例。`ServiceRegistration.Output.cs:34`〜`36`）。

## 決めること（着手前にユーザーへ確認）
| # | 決めること | 選択肢（先頭が推奨） |
|---|---|---|
| D1 | 範囲 | (A) 終了の手順（33-1〜33-3）とキャスト（33-5）だけ。起動後の処理と引数の処理の移動（33-4・33-4b）は見送る／(B) すべて行う（`App.xaml.cs` は 400 行前後になる見込み） |
| D2 | 終了の部品の置き場所 | (A) `Composition/Startup`（組み立ての層。全層の具象を知ってよい）／(B) 既存の `RuntimeBootstrapper`（第 2 層）へ足す。IPC・単一起動・ログは第 2 層が知るべきでないため、(A) を勧める |
| D3 | 未処理例外のときの終了（`CurrentDomain_UnhandledException`）も同じ部品を通すか | (A) 通す（今も `CleanShutdown` を呼んでいる）／(B) 触らない |

## Step 一覧
| Step | 内容 | テスト | 状態 |
|---|---|---|---|
| 33-0 | 調査: `CleanShutdown` が呼ばれる経路（通常の終了、サインアウト、未処理例外、早期終了、`-driverinstall`）と、そのときのフィールドの状態（`null` かどうか）を表にする。`skipSave`・`runShutdown` の立つ条件を確かめる | — | 未着手 |
| 33-1 | 順序の検査を先に足す: 偽の `IServiceLifecycle`・`IRuntimeBootstrapper`・`IAppSettingsService`・`IControllerRegistry` などで、呼ばれた順を記録して、今の順序を固定する（`CleanShutdown` の中身を、テストできる静的でないメソッドへ出すところまで） | 新規（終了の順序） | 未着手 |
| 33-2 | 終了の手順の部品を作り、`CleanShutdown` の中身を移す（順序は同じ）。`App` は部品を呼ぶ | 33-1 が不変 | 未着手 |
| 33-3 | `App` のフィールドのうち、終了のためだけに持っていたもの（`_controllerRegistry`、`_ipc` など）を消す | `RemovedApiRuleTests` の表へ足す | 未着手 |
| 33-4 | （D1 が B のとき）起動後の処理（ドライバーの有無のログ、ログの設定の反映、通知サービスの取り付け）を `Composition/Startup` の部品へ出し、`InitializePostHostServices` を短くする | `StartupOrderGuardTests` が不変 | 未着手 |
| 33-4b | （D1 が B のとき。2026-11-12 追加）引数の処理（`CheckOptions` 73 行。`App.xaml.cs:789`〜）と、設定の保存の試行（`AttemptSave` 45 行）、ログの準備（`RotateLogsAndLogStartupDiagnostics` 50 行、`ConfigureLoggingAndDiagnostics` 49 行）、はじめの起動の設定（`RunFirstLaunchSetup` 40 行）の、判断の部分を `Composition/Startup` の部品へ出す。`Application_Startup` は、手順を順に呼ぶだけの形を保つ（順序は変えない） | 引数ごとの判断（`-driverinstall`、`-command`、`-m` など）の単体テスト。`StartupOrderGuardTests` が不変 | 未着手 |
| 33-5 | キャスト 3 か所を、登録の形で解く。`DeviceSlotState` を具象で 1 つ登録し、`IDeviceSlotState`・`IDeviceStateAccessor`・`IConfigurableDeviceLookup` がそれを返す。`ProfileSettingsService`・`DefaultActionManager` も同様 | 3 つの契約が同じインスタンスを返すこと。偽物に差し替えたとき、配線が例外で知らせること | 未着手 |
| 33-6 | 文書: `03-Lifecycle-and-DI.md` §4（終了の順序を、部品の名前つきで書く）、`01-Structure.md` の型の表。`Code-Review-2026-11/00-Summary.md` の該当行を「是正済み」へ | `ModelDiagramGuardTests` | 未着手 |

## 確認の方法
- 実機: トレイからの終了、ウィンドウを閉じての終了、サインアウト、コントローラーを接続したままの終了で、(1) 仮想パッドが消える、(2) 出力スロットとアプリの設定が保存される、(3) プロセスが残らない、(4) 次の起動が正常。最小化起動（`-m`）と、2 つ目の起動（単一起動）も確かめる。
- `Known-Issues-Ledger.md` §3 の Phase27 の項目（終了）と合わせて確認する。

## リスク
- 高。終了の順序を誤ると、仮想パッドが残る・設定が保存されない・プロセスが残る。33-1（順序の固定）を必ず先に行い、33-2 は移動だけにする。
- 未処理例外の経路は、テストで再現しにくい。D3 を B にすれば、触らずに済む。

## ほかの計画との関係（2026-11-12 追加）
- `Phase35-Plan.md`（画面の MVVM 化）の 35-3 は `MainWindow` に触れ、起動・終了・電源の通知と隣り合う。本 Phase と同時には進めない。
- `PhaseX2-Custom-Exe-Name-Plan.md` の案 C（起動役の方式）は、起動の入口（`App.xaml.cs:113`〜`128`）に触れる。実施する場合は、本 Phase の 33-4b（引数の処理の切り出し）のあとに行うと、起動役の判断を置く場所が決まる。

## 履歴
- 2026-11-11: 計画書を作成。コードは未着手。
- 2026-11-12: 外部レビューの照合の結果を統合。`App.xaml.cs` の現状と目安（400 行前後）、33-4b（引数の処理ほかの切り出し）、ほかの計画との関係を足した。
