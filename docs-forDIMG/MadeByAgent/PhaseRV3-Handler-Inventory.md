# PhaseRV3-0 調査: ハンドラーの分類と、採用の関門の結果

作成日: 2026-11-19（承認待ち）。対象は `MainWindow`・`About`・`AutoProfiles`・`AssignmentEditor` の 4 画面。

## 1. 採用の関門（CommunityToolkit.Mvvm 8.4.0）— 通過

| 確認 | 結果 |
|---|---|
| (i) ソース生成（`[RelayCommand]`・`[ObservableProperty]`）が `_wpftmp` の 1 回目のビルドで動き、Debug ALL が警告 0・エラー 0 | **通過**。プローブ（`partial` クラス＋`[RelayCommand] private async Task DoWorkAsync()`）で `DoWorkCommand` が生成され、警告 0 |
| (ii) `AsyncRelayCommand` は実行中に `CanExecute` が false になる | **通過**（`CanExecuteChanged` も発火） |
| (iii) 例外の既定の扱い | `ICommand.Execute`（XAML のバインディングが呼ぶ形）は、既定では例外を**同期コンテキストへ再スロー**する（`async void` と同じ。WPF ではアプリが落ちる）。`AsyncRelayCommandOptions.FlowExceptionsToTaskScheduler` を付けると、同期コンテキストへは届かない（観測されるだけ）。**規則: すべてのコマンドの本体を、共通の包み（`CommandGuard`）で受けて `AppLogger`・`IDialogService` へ流し、再スローしない**（RV3-1 で作る） |
| (iv) キャンセル | `Cancel()` で実行中のタスクが `IsCanceled` になり、`IsRunning` が false に戻る |
| 追加の観測 | `Execute`／`ExecuteAsync` は、実行中の再入を**拒まない**（2 回目も走る）。拒むのは `CanExecute`（ボタンの無効化）だけ。バインディング以外（ホットキー・トレイ・コード）から呼ぶときは、`CanExecute` を確かめる。共通の包みで、実行中は再入を拒む |
| 追加の観測 | `DS4Windows.UI.ViewModels` に、独自の `ObservableObject` がある。同じ名前空間では、ツールキットの `ObservableObject` は完全修飾か別名（`using ToolkitObservableObject = …`）で書く（RV7 までの共存の約束） |

テスト: `DS4WindowsTests/ToolkitAsyncCommandGateTests.cs`（5 件。RV3-1 で規則のテストへ移す）。

## 2. 分類の基準

- **(a) 業務の呼び出し**: サービスの呼び出し、ファイル・プロセス・ダイアログ、他画面を開く → `ICommand`（ViewModel）へ。
- **(b) 状態の更新**: `IsEnabled`・`Visibility`・`Content`・`ItemsSource`・`SelectedIndex` への代入、選択状態の保存 → バインディング・ViewModel の状態へ。
- **(c) 見た目だけ**（View に残す。**許可リストへ理由つきで載せる**）: ホバー、描画、ウィンドウの位置・大きさ、フォーカス、ドラッグ、列幅、`ContextMenu.IsOpen`、`e.Handled`。

## 3. MainWindow（31 `Click` ＋ その他のイベント）

| ハンドラー | 内容（要約） | 分類 | 移し先 |
|---|---|---|---|
| `AboutBtn_Click` | `About` を `ShowDialog` | a | `ShowAboutCommand`（`IDialogService`／画面の窓口） |
| `StartStopBtn_Click` | `ChangeService`（開始・停止） | a | `ToggleServiceCommand`（非同期。忙しい間は `CanExecute` false） |
| `HideDS4ContCk_Click`・`HideXboxContCk_Click` | 隠す設定を変え、`Task.Run` で Stop→Start、`IsEnabled` を操作 | a＋b | 非同期コマンド＋「忙しい」状態 |
| `DriverSetupBtn_Click` | 停止→ドライバー導入→再開、`IsEnabled` を操作 | a＋b | 同上 |
| `CheckUpdatesBtn_Click` | `Task.Run` で更新を確認し、更新ウィンドウを出す | a | 非同期コマンド |
| `ChecklogViewBtn_Click` | 変更履歴ウィンドウ | a | コマンド |
| `DeviceOptionSettingsBtn_Click` | デバイスオプション画面 | a | コマンド |
| `ProfFolderBtn_Click`・`ControlPanelBtn_Click`・`Html5GameBtn_Click`・`XinputCheckerBtn_Click`・`HidHideBtn_Click`・`FakeExeNameExplainBtn_Click` | プロセスの起動・URL を開く・メッセージ | a | コマンド（`IProcessLauncher`／`IDialogService`） |
| `NewProfListBtn_Click`・`EditProfBtn_Click`・`DeleteProfBtn_Click`・`RenameProfBtn_Click`・`DupProfBtn_Click`・`ImportProfBtn_Click`・`ExportProfBtn_Click` | 選択中のプロファイルの操作 | a | コマンド（`CanExecute`＝選択の有無。`ProfilesListBox_SelectionChanged` の `IsEnabled` の代入を置き換える） |
| `ProfEditSBtn_Click`・`NewProfBtn_Click` | 機器ごとのプロファイル編集・新規（`Tag` に機器の番号） | a | コマンド（`CommandParameter`＝機器の番号） |
| `SwipeTouchCk_Click`・`RunAtStartCk_Click`・`UseOscServerCk_Click`・`UseOscSenderCk_Click`・`UseUdpServerCk_Click` | チェックの状態をサービスへ | a | プロパティの変更時の処理（バインディング）＋コマンド |
| `ExportLogBtn_Click`・`ClearLogBtn_Click` | ファイル保存・ログの消去 | a | コマンド（`IDialogService`） |
| `SelectProfCombo_SelectionChanged`・`LinkedProfCombo_SelectionChanged`・`ProcessPriorityComboBox_OnSelectionChanged`・`ProfilesListBox_SelectionChanged` | 選択の変更を業務へ／ボタンの有効化 | a＋b | バインディング（`SelectedItem`）＋ViewModel のプロパティ変更時の処理 |
| `ProfilesListBox_MouseDoubleClick`・`LogListView_MouseDoubleClick` | 項目の編集・メッセージ表示 | a | 項目の `InputBindings`（`MouseBinding` の `LeftDoubleClick`）→コマンド |
| `MainDS4Window_Closing`・`_Closed`・`_StateChanged`・`_SizeChanged` | 閉じる・最小化・ウィンドウの大きさの保存 | **c**（窓のライフサイクル。中の業務は、ViewModel のメソッドを呼ぶだけにする） | 許可リスト |
| `ControllerLV_PreviewMouseUp`・`LogListView_PreviewMouseUp` | 列幅の保存 | **c**（列幅） | 許可リスト |
| `SelectProfCombo_KeyDown` | `e.Handled = true`（キー入力を捨てる） | **c**（入力の制御） | 許可リスト |
| `LightColorBtn_Click` | ボタンの `ContextMenu` を作って開く | **c**（ポップアップの表示。メニューの項目は、コマンドへ） | 許可リスト |
| `NotifyIcon_TrayMiddleMouseDown`・`_TrayMouseDoubleClick`（コード内の `+=`） | トレイの操作で窓を出す・閉じる | a（D4: トレイのメニューとコマンドを共有） | コマンド |

件数: a・b＝**約 41**、c＝**8**（Closing／Closed／StateChanged／SizeChanged／列幅 2／KeyDown／LightColorBtn）。

## 4. About（13 `Click`）

すべて `Util.StartProcessHelper(URL)` の 1 行 → **a**。`OpenLinkCommand`（`CommandParameter`＝URL。1 つのコマンドを 12 のリンクが共有）。`Hyperlink` は `Command`・`CommandParameter` に対応する。許可リスト: なし。

## 5. AutoProfiles（11 `Click`）

| ハンドラー | 分類 | 備考 |
|---|---|---|
| `RemoveAutoBtn_Click`・`SaveAutoBtn_Click`・`HideUncheckedBtn_Click`・`MoveUpDownAutoBtn_Click`・`ShowAutoDebugCk_Click`・`SteamMenuItem_Click`・`StartMenuItem_Click`・`BrowseProgsMenuItem_Click`・`BrowseAddProgMenuItem_Click` | a＋b | コマンド。`this.IsEnabled = false`・`Visibility`・`ItemsSource = null` は「忙しい」状態のバインディングへ。`SearchFinished` の `+=` は、コマンドの完了（`await`）に置き換える |
| `AddProgramsBtn_Click` | **c** | ボタンの `ContextMenu.IsOpen = true`（ポップアップの表示） |
| `ProgramListLV_PreviewMouseUp` | **c** | 列幅の保存 |

## 6. AssignmentEditor（10 `Click`）

| ハンドラー | 分類 | 備考 |
|---|---|---|
| `SaveBtn_Click`・`CancelBtn_Click`・`LaunchProgBrowseBtn_Click`・`PressKeySelectBtn_Click` | a | コマンド（`session` を持つ ViewModel へ） |
| `BatteryEmptyColorBtn_Click`・`BatteryFullColorBtn_Click` | a | 色の選択ダイアログ → `IDialogService`／色の選択の窓口 |
| `RecordMacroBtn_Click`・`MultiTapTrigBtn_Click`・`MultiHoldTapTrigBtn_Click`・`MultiDoubleTapTrigBtn_Click` | 要調査 | ハンドラーが `AssignmentEditor.xaml.cs` に見つからない（部分クラスの別ファイルか、別の型）。RV3-5 の最初に探す |

## 7. 承認を求める許可リスト（(c) の候補。理由つき）

| # | 画面 | ハンドラー | 理由 |
|---|---|---|---|
| 1〜4 | MainWindow | `MainDS4Window_Closing`／`_Closed`／`_StateChanged`／`_SizeChanged` | ウィンドウのライフサイクル（キャンセルの `e.Cancel`、最小化の見た目、サイズの記録）。中の業務は ViewModel のメソッドへ |
| 5〜6 | MainWindow | `ControllerLV_PreviewMouseUp`／`LogListView_PreviewMouseUp` | 列幅の保存（表示の状態） |
| 7 | MainWindow | `SelectProfCombo_KeyDown` | キー入力を捨てる（`e.Handled`） |
| 8 | MainWindow | `LightColorBtn_Click` | ボタンの `ContextMenu` を作って開く（ポップアップの表示） |
| 9 | AutoProfiles | `AddProgramsBtn_Click` | `ContextMenu.IsOpen = true` |
| 10 | AutoProfiles | `ProgramListLV_PreviewMouseUp` | 列幅の保存 |

D2＝(B) の目標は、(a)(b) の 100%。残るのは上の 10 件だけ。

## 8. 比較の記録（規約 §4。コマンドの基盤）

| 案 | 評価 |
|---|---|
| 独自の `RelayCommand` の拡張 | 依存なし。ただし非同期・実行中の `CanExecute`・キャンセルを自作・保守する |
| **CommunityToolkit.Mvvm（採用）** | 標準的。`AsyncRelayCommand`・ソース生成・`ObservableObject`。関門を通過（§1）。例外の既定の扱いは、共通の包みで吸収する |
| Prism・ReactiveUI | 機能が大きすぎる（ナビゲーション、リアクティブ）。この規模には過剰 |
