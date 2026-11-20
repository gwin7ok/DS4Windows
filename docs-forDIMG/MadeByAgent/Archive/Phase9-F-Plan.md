# Phase9 9-F（UI のコードビハインド）計画書

作成日: 2026-10-22
状態: **9-F は完了（2026-10-23。F-1a・F-1b・F-2a・F-2b・F-5a・F-5b）。先送りの F-1c・F-2c・F-4・F-5c は `Phase9-Deferred-Ledger.md` に記録**。以下は設計の記録。F-1（起動処理の分割。F-1a・F-1b）は完了（2026-10-23。実機確認 OK。台帳 §48）。F-2 の設計案は §6（承認待ち）。範囲を決定（2026-10-22）。F-3（トレイ）は、利用者が「解決しているようだ」と判断したため、範囲から外す。F-4（`ProfileEditor`）は、今回は先送り。着手は F-1 → F-2 → F-5。F-1 の設計は §5（承認待ち）
上位: `Phase9-Responsibility-Split-Plan.md` §3 の 9-F、§12

## 1. 現状（測定、2026-10-22）
| ファイル | 行数 | 大きいメソッド |
|---|---|---|
| `App.xaml.cs` | 1,189 | `Application_Startup` 401、`CheckOptions` 136、`CleanShutdown` 68、`ApplyLanguageSetting` 49、`AttemptSave` 45 |
| `DS4Forms/MainWindow.xaml.cs` | 2,269 | `WndProc` 267、`MainWindow`（コンストラクター）151、`SyncProfileListAndControllers` 71、`ShowProfileEditor` 67、`SetupEvents` 64、`PowerEventArrive` 63、`LateChecks` 60、`HotkeysTimer_Elapsed` 56 |
| `DS4Forms/ProfileEditor.xaml.cs` | 2,098 | `Reload` 125、`EnsureSpecialActionsHeadersAssigned` 111、`SetAndLogHeaderText` 106、`RumbleTestBtn_Click` 94、`EditActionBtn_Click` 91 |

加えて、E-6 の後に残した課題: トレイのメニューの再クリック（`Phase9-Responsibility-Split-Plan.md` §3）、`ServiceLifecycle` のプロセス優先度の配列の複製（`MainWindow.ProcessPriorityClasses` と同じ順序）、`ControlService` の転送メンバーを UI から外すこと。

## 2. 切り出しの候補
### F-1: 起動の組み立て（`Application_Startup` 401 行）
起動順序（Pre-Host／Post-Host、言語、テーマ、ログ、HidHide、単一起動、設定の読み込み）を、手順ごとの部品（`IStartupStep` 相当）に分ける。順序と、`App.xaml.cs` の `Application_Startup` の副作用（ログの文言、例外の扱い）は変えない。**起動順序の特性化テストを先に足す**。

### F-2: `WndProc`（267 行）
Windows メッセージの種類ごと（デバイスの着脱、電源、IPC、ホットキー、ウィンドウの表示要求）に、ハンドラーへ分ける。ホットプラグ（`InnerHotplug2`）と電源（`PowerEventArrive`）は、安全弁（ロック、遅延）を含む。

### F-3: トレイのメニューの再クリックの課題
開いているときの右クリックの扱い（閉じる、または再表示）を決めて直す。回帰かどうかを、古い版との比較で先に確かめる（利用者が後回しとした項目）。

### F-4: `ProfileEditor` の分割
`Reload`、SA の一覧の見出し・並べ替え（`EnsureSpecialActionsHeadersAssigned`、`SetAndLogHeaderText`、`SortSpecialActionsList`）、振動テスト（`RumbleTestBtn_Click`）を、部品へ。

### F-5: 残った課題の整理
プロセス優先度の型付き化（配列の複製をなくす）、`ControlService` の転送メンバーの削除（UI を抽象経由へ）。

- **F-5a（完了。2026-10-23）**: 優先度の表を `ProcessPriorityOptions`（`DS4Windows/DS4Control/Services/`）へ 1 か所にまとめた。`MainWindow` と `ServiceLifecycle` の複製を削除。並び順（Normal／AboveNormal／High／RealTime。設定は添字で保存）は、ガードテストで固定。
- **F-5b（完了。2026-10-23）**: 外部の参照が 0 件だった転送（`activeControllers`／`loopControllers`／`inServiceTask`）を `ControlService` から削除し、内部は `_lifecycle` を直接使う。
- **F-5c（先送り）**: `running`（外部の利用者は `AutoProfileService`／`App`／`MainWindow`／`TrayIconViewModel` の 4 つ）と `suspending`（`MainWindow` のみ）の転送は残す。理由: 4 つの利用者はいずれも `ControlService` を、`Start`／`Stop`／`slotManager` などのためにも直接持っており、`running` だけを抽象へ替えると、コンストラクターの引数（`MainWindow`・`TrayIconViewModel`・`MainWindowFactory`・`AutoProfileService` と、そのテスト）が増えて割に合わない。`ControlService` の他のメンバーの切り離しと同時に行う（`ControlService.cs` に TODO を記載）。

## 3. 進め方（提案）
1. **F-3 の事前調査**（回帰かどうか。実機の比較が要る。利用者の作業）は、後回しのまま。
2. **F-1 → F-2 → F-4 → F-5** の順（推奨）。各 F で、特性化テスト → 切り出し → ビルド・テスト → 実機（画面）確認。
3. 画面の実機確認が必須のため、1 回の変更を小さくする（`Application_Startup` は、手順ごとに段階を分ける）。

## 4. 決めてほしいこと
- Q1: 着手順（F-1 から／F-4 から／F-2 から）。
- Q2: F-3（トレイ）を、今回の 9-F に含めるか（回帰の確認を先に行う）。
- Q3: 9-F の範囲を、`App.xaml.cs` と `MainWindow` までにし、`ProfileEditor`（F-4）は、今回の UI 変更で内容が変わったため見送るか。


## 5. F-1（`Application_Startup`）の設計案（2026-10-22）
`Application_Startup`（401 行）の流れ（順序は変えない）:
1. AUMID とレジストリの初期化、ブートストラップのサービス、設定の場所、ログのローテーション（Pre-Host）
2. 起動時の診断ログ（文化、引数、言語フォルダ）
3. `--complete-install` の引数の処理（昇格した更新の完了。処理して終了）
4. `CheckOptions`、優先度の設定、単一起動の確認（既存のインスタンスへ通知して終了）
5. タイマー精度、キーボードのリピートの設定、ViGEmBus の情報、IPC のイベント、一時ワーカー
6. ホスト構築、`CreateControlService`、`InitializePostHostServices`
7. 初回起動の流れ（言語選択、保存先、設定フォルダの骨格）
8. `Macros` フォルダ、設定の読み込み、言語の再適用
9. ログの設定（`LoggerHolder`）、例外ハンドラー、環境のログ、ビルドの日時のログ
10. 初回のデバイス設定、既定の設定の作成
11. 接続フラグのリセット、プロファイル ID、SA の読み込み、言語・テーマ
12. メイン画面の生成と表示、IPC、`LateChecks`

### 分け方の案
- **F-1a（純粋な部分を、部品へ）**: ① `--complete-install` の引数の読み取り（`CompleteInstallArguments`）、② ビルドの日時のログの文言づくり（`BuildStampReader`。`build_timestamp.txt` の文字列 → ログの文言）、③ 環境の診断ログ（`StartupDiagnostics`）。いずれも状態を持たず、単体テストで値を固定できる。
- **F-1b（手順ごとのメソッドへ）**: 上の 12 の区分を、`App` の private メソッド（`RunPreHostBootstrap`、`HandleCompleteInstall`、`EnsureSingleInstance`、`BuildHostAndServices`、`RunFirstRunFlow`、`LoadSettingsAndLogging`、`PrepareProfilesAndActions`、`ShowMainWindow` ほか）に分ける。`Application_Startup` は、それらを順に呼ぶだけにする。早期 return（終了、他のインスタンスへの通知）は、各手順が「続けるか」を返す。呼び出しの順序は、ソースの順序のガードテストで固定する。
- **F-1c（任意）**: 手順を `App` の外（`AppStartupSequence`）へ移す。`runShutdown`／`skipSave`／`exitApp` などの状態の持ち主を決める必要があり、F-1b の後に判断する。

### 検証
- 起動のログの順序（`ds4windows_log` の先頭の行）が、変更の前後で同じであること（実機）。
- 初回起動の流れ（言語選択 → 保存先 → デバイス設定）は、実機でしか確認できない。設定フォルダのない環境での確認が要る（既存の設定を、別の場所へ退避して行う）。

### 決めてほしいこと
- Q1: F-1a（純粋な部分の部品化）と F-1b（手順ごとのメソッド）を、この順で進める（推奨）／F-1b だけ。
- Q2: F-1c は、F-1b の後に判断する（推奨）／今回は行わない。


## 6. F-2（`MainWindow.WndProc`、267 行）の設計案（2026-10-23）
`WndProc` は、2 種類のメッセージを扱う。
- `WM_DEVICECHANGE`（約 25 行）: ホットプラグ。`RunHotPlug` の設定が有効で、デバイスの着脱（`DBT_DEVICEARRIVAL`／`DBT_DEVICEREMOVECOMPLETE`）のとき、カウンターを増やし、実行中でなければ `InnerHotplug2` をタスクで起動する。**ロック（`hotplugCounterLock`）と、実行中のフラグ（`inHotPlug`）が安全弁**。
- `WM_COPYDATA`（約 230 行）: IPC のコマンド（コマンドラインからの `start`／`stop`／`cycle`／`shutdown`／`disconnect`／`changeledcolor`／`loadprofile`／`loadtempprofile`／`outputslot`／`query`）。文字列を `.` で区切り、1 つの巨大な `if` の連なりで処理する。例外は、すべて握りつぶす（致命的でないため）。

### 分け方の案
- **F-2a（純粋な部分）**: ① コマンドの読み取り（`COPYDATASTRUCT` のサイズ検査 4〜256、ASCII 変換、`.` で分割、先頭を小文字へ）を `IpcCommandParser` に。② `query` の「プロパティ名 → 値」の対応表のうち、デバイスの有無に依らず引ける部分。状態を持たず、単体テストで値を固定できる。
- **F-2b（コマンドごとのメソッドへ）**: `WndProc` を、`HandleDeviceChange`／`HandleCopyData` に分け、`HandleCopyData` は、コマンドごとの private メソッド（`IpcStart`、`IpcStop`、`IpcCycle`、`IpcShutdown`、`IpcDisconnect`、`IpcChangeLedColor`、`IpcLoadProfile`、`IpcOutputSlot`、`IpcQuery`）を呼ぶ形にする。MainWindow が持つ UI の部品（`ChangeService`、`conLvViewModel`、閉じる処理）に触れるため、**クラスの外へは出さず、`MainWindow` の partial（`MainWindow.Ipc.cs`、`MainWindow.HotPlug.cs`）へ移す**。コマンドの受け付け順、分岐の順序（`if`／`else if` の順）、例外の握りつぶし、`query` の結果の書き込み（`WriteIPCResultDataMMF`）は、変えない。
- **F-2c（任意）**: コマンドの実行を、UI から独立した `IpcCommandProcessor`（窓口 `IIpcCommandHost`）へ。窓口のメンバーが多く（サービス状態、デバイス一覧、プロファイル、出力スロット）、効果に比べて変更が大きいため、F-2b の後に判断する。

### 検証
- 特性化テスト: コマンドの読み取り（F-2a）、`WndProc` の分岐の順序とコマンド名の集合（ソースの照合。F-1b と同じ方式）。
- 実機: コマンドラインから `-command start`／`stop`／`cycle`／`disconnect`／`loadprofile`／`outputslot`／`query` などを送って、従来どおりに動く（既存の Phase5/Phase8 の IPC の確認手順があれば、それを流用）。ホットプラグ（USB の抜き差し）が動く。

### 決めてほしいこと
- Q1: F-2a → F-2b の順で進める（推奨）／F-2b だけ。
- Q2: F-2c は、F-2b の後に判断する（推奨）／今回は行わない。
- Q3: 実機の IPC の確認は、どのコマンドを必須にするか（推奨: `start`／`stop`／`cycle`／`query`／`loadprofile` の 5 つ。`shutdown` と `outputslot`、`changeledcolor`、`disconnect` は、特性化テストと目視でよいか）。
