# PhaseRV9 ガード台帳（動きを変えうる修正）

注釈（`?`）だけでは済まず、`null` の流れを変えた箇所を 1 件ずつ書く。

| # | ファイル:行 | 以前の動き | 新しい動き | 根拠のテスト |
|---|---|---|---|---|
| G1 | `Core/Utilities/Util.cs` `GetOSProductName`・`GetOSReleaseId`・`SystemAppsUsingDarkTheme` | レジストリの値がない（キーなし）とき `NullReferenceException` | `?.ToString() ?? string.Empty`（OS 名・リリース ID は空文字、ダークテーマの判定は偽）。呼ぶのは診断ログだけ（`SystemAppsUsingDarkTheme` は呼び出し 0） | 既存テストで他の動きに変化なし。キーなしの経路は実行環境に依存（テストなし） |
| G2 | `Core/Persistence` の `e.InnerException` を使うログ 4 か所 | 内側の例外がないとき `NullReferenceException` | 内側の例外がなければ `e.Message` をログに出す | ログのみ（テストなし） |
| G3 | `UI/ViewModels/AutoProfilesViewModel` | 連携先のプロファイル名が null のとき、そのまま入る（または例外） | `AutoProfileEntity.NONE_STRING`（「(none)」）にする。削除で割り当て先が null なら何もしない | 既存テスト |
| G4 | `UI/ViewModels/ControllerListViewModel.Snapshot()` | 一覧が null のとき例外 | 空の配列として扱う | 既存テスト |
| G5 | `UI/ViewModels/OutBinding`・`ControlSettingsEditTarget` | マクロの配列が null のまま保存される | `new int[1]` を入れる | 既存テスト |
| G6 | `UI/MainWindow.xaml.cs` のホットキーのタイマー・`ProfileEditor_Closed`・トレイのプロファイル選択 | タイマー／編集画面／プロファイル名が null のとき `NullReferenceException` | タイマーは何もしない。編集画面がなければサイズを戻す側へ。プロファイル名が null なら適用しない | `KeepsizeTests`（文字列） |
| G7 | `UI/MainWindow.xaml.cs` と `UI/ProfileEditor.xaml.cs` の `sender as …`（状態アイコン、ID のツールチップ、ライトバー色、振動テスト、ホバー） | 型が違う `sender` で `NullReferenceException` | 何もせず戻る（通常の操作では起きない） | なし |
| G8 | `UI/ProfileEditor.xaml.cs` の右クリックのメニュー・`Reload` のプロファイル名 | メニューのリソースがなければ例外／名前が null なら例外 | 何もしない／空の名前として扱う | `RemovedShiftNoticeWiringTests`・`RemovedGyroSwipeNoticeWiringTests`（文字列） |
| G9 | `UI/RecordBox.xaml.cs` | 行の容器が取れないとき例外 | 何もしない／確定を飛ばす | なし |
| G10 | `UI/UpdaterWindow.xaml.cs`・`UI/WelcomeDialog.xaml.cs`・`UI/LanguageSelectDialog.xaml.cs`・`App.xaml.cs` | メインモジュール名・タイマーの `sender`・言語コードが null のとき例外 | 空文字／何もしない／言語の適用を飛ばす | なし |
| G11 | `UI/OutputSlotManagerControl.xaml.cs` | 一覧の表示が `GridView` でないとき例外 | 列幅は空の配列／何もしない | なし |
| G12 | `UI/DupBox.xaml.cs`・`UI/AutoProfiles.xaml.cs`・`UI/LanguagePackControl.xaml.cs` ほか、後から渡されるフィールド | 渡される前の呼び出しで `NullReferenceException` | 同じ（理由コメント付きの `!`。動きは変えない） | `DuplicateAssignsNewIdTests`（文字列） |
| G13 | `UI/MainWindow.Ipc.cs` | `Marshal.PtrToStructure` が null のとき例外 | ジェネリック版へ（構造体の既定値） | なし |

## A 組の覚え書き（2026-11-19。RV9-2〜6）
- 動きを変えた箇所は G1 だけ。そのほかの `!` は、null のときは従来どおり `NullReferenceException` になるまま、理由コメントを付けた（挙動は変えない）。
- 設定が後から入るフィールドは `= null!;`（理由コメント付き）。対象は `App.xaml.cs` の起動で設定するもの、`DS4SixAxis` などのコンストラクターで設定するもの、`Core/Platform/EnvironmentService` の列挙で設定する値。
- 空きスロットを表す `Controllers[i] = null`・`TouchPads[i] = null` は、配列の要素の型を `?` にする作業を別に残し、`null!` と理由コメントで保った（`DeviceDisconnectHandler`・`ServiceLifecycle`）。要素の型を `?` へ変えるのは、B 組のあとの整理（RV9-13 の前）で検討する。
- `DS4WindowsCli` は、`CodeHealthScanner` の対象外（本体の `DS4Windows/` だけを数える）。指令とビルドの警告 0 は確認済み。
- 文字列でソースを検査するガードテスト 4 件（`DisconnectGuardTests`・`ConnectionGuardTests`・`StartupBootstrapGuardTests`・`BatteryUnificationTests`）は、`!`・`?` の分だけ期待値を更新した（検査の意図は同じ）。
