# Phase13-Step11 個別計画書（任意）: 起動手順（F-1c）と IPC コマンドの実行（F-2c）

作成日: 2026-11-04
対象ブランチ: `Phase13`
上位計画: `Phase13-Plan.md` §4 Step11（Q3）
状態: **承認待ち**（§9）

---

## 1. 目的と前提

`Phase9-Deferred-Ledger.md` の F-1c・F-2c を、実施するか、見送って台帳に記録して閉じるかを決める。
どちらも台帳の着手条件（起動処理を頻繁に変更する／IPC の機能を増やす／UI なしでコマンドを単体テストしたい）を**現時点では満たさない**。ユーザーの指示（2026-11-04）で、検討を行う。

## 2. 現状（2026-11-04、ソースの調査）

| 項目 | 現状 |
|---|---|
| 起動手順 | `App.xaml.cs`（1320 行）の `Application_Startup`（約 70 行）が、15 前後のメソッド（`RunPreHostBootstrap`〜`ShowMainWindowAndFinish`）を順に呼ぶだけ。順序は `Phase9F1StartupOrderGuardTests` が固定済み。状態は `App` のフィールド `runShutdown`／`skipSave`／`exitApp`（3 つ）と、ホスト構築後の約 10 個のサービスの参照 |
| IPC の実行 | `MainWindow.Ipc.cs`（327 行）に、`HandleCopyData`（受け付け）と 9 個の `Ipc*`。解析は `IpcCommandParser`（独立済み）。実行は UI に依存: `conLvViewModel`（コントローラー一覧）、`profileListHolder`、`MainDS4Window_Closed`（終了）、`lifecycle`、`controlService`、`outputSlotService`、`profileRepo`、`loadTarget` |

## 3. 案

### F-1c（起動手順を `AppStartupSequence` へ）
`runShutdown`／`skipSave`／`exitApp` を持つクラスを新設し、手順を移す。ただし手順の中身は、ウィンドウの表示（`ShowMainWindowAndFinish`）、ダイアログ（初回の言語選択、保存先の選択）、`Application.Current` の操作など **WPF の UI に密着**している。移せるのは「順序を呼ぶ部分」のみで、実体は `App` に残るため、窓口（`IAppStartupHost`）に十数個のメソッドが必要になる。見込み: 新設 3〜4 型、`App.xaml.cs` の減少は 100 行程度、リスクは**起動の失敗（実機でしか確認できない）**。利点: 起動順を UI なしでテストできる（既に文字列ベースのガードで固定済み）。

### F-2c（IPC の実行を `IpcCommandProcessor` へ）
9 個のコマンドを、窓口 `IIpcCommandHost`（実行中か／開始／停止／再起動／終了／コントローラーの一覧と切断・LED 色・プロファイルの選択・出力スロット・問い合わせ）を通す `IpcCommandProcessor` へ移す。見込み: 新設 2〜3 型、窓口のメンバー約 12、`MainWindow.Ipc.cs` が 100 行程度に縮む。利点: **UI なしでコマンドを単体テストできる**（現状は不可）。リスク: 中（IPC は CLI からの操作のみ。実機で `start`／`stop`／`query` などを確認）。

### 見送り
台帳に「Phase13 で再判断し、見送り。理由と再開条件」を記録して閉じる。コード変更なし。

## 4. 推奨

**F-2c のみ実施、F-1c は見送り**。
理由: F-2c は単体テストを新たに可能にする実利があり、窓口の形も明確。F-1c は UI 密着で窓口が太く、実利（既にガードあり）が小さい一方、失敗すると起動できなくなる。

## 5. F-2c の実装の分割（F-2c を選んだ場合。各サブで `Debug ALL`）

| サブ | 内容 |
|---|---|
| 11-a | 本計画書 |
| 11-b | `IIpcCommandHost`（`DS4Windows/DI/`）と `IpcCommandProcessor`（`DS4Windows/DS4Control/Services/`）の新設。9 コマンドを、受け付け順・条件・例外の握りつぶしを変えずに移す。`MainWindow` が窓口を実装（partial の `MainWindow.IpcHost.cs`。1 ファイル 1 型の例外ではなく、`partial` の分割） |
| 11-c | `MainWindow.Ipc.cs` の `HandleCopyData` を `IpcCommandProcessor` 呼び出しに替え、旧 `Ipc*` を削除。単体テスト（コマンドごとの正常系・範囲外・不正入力・例外の握りつぶし）を追加 |
| 11-d | 台帳（F-1c は見送り、F-2c は実施）、計画書 §10、`Phase13-Status.md` の更新 |

## 6. 規約・ログ

ログの文言・レベルは変えない。新しい静的な窓口は作らない。配置は R1・R2。`MainWindow` の引数は増やさない（`IpcCommandProcessor` は `MainWindow` が生成するか、`MainWindowFactory` 経由で 1 つ受ける。後者だと +1。**前者を推奨**）。

## 7. 検証

`Debug ALL`。実機確認（最後にまとめて）: CLI からの `start`／`stop`／`cycle`／`disconnect`／`changeledcolor`／`loadprofile`／`outputslot`／`query`。

## 8. リスク

窓口の取り違え（`controlService` の状態と `lifecycle` の状態の混同）、`shutdown` コマンドが `MainDS4Window_Closed` を直接呼ぶ点（UI の終了処理に依存するため、窓口の `RequestShutdown` として `MainWindow` に残す）。

## 9. 決定事項（承認をお願いします）

1. **範囲**: F-2c のみ実施（推奨）／F-1c と F-2c の両方／両方とも見送り。
2. **F-1c を見送る場合**: 台帳に理由と再開条件を記録して閉じる（推奨）。
3. **進め方**: 4 コミット（11-a〜11-d）で以後は自動進行。
