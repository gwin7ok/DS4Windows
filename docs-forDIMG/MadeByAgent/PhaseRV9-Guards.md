# PhaseRV9 ガード台帳（動きを変えうる修正）

注釈（`?`）だけでは済まず、`null` の流れを変えた箇所を 1 件ずつ書く。

| # | ファイル:行 | 以前の動き | 新しい動き | 根拠のテスト |
|---|---|---|---|---|
| G1 | `Core/Utilities/Util.cs` `GetOSProductName`・`GetOSReleaseId`・`SystemAppsUsingDarkTheme` | レジストリの値がない（キーなし）とき `NullReferenceException` | `?.ToString() ?? string.Empty`（OS 名・リリース ID は空文字、ダークテーマの判定は偽）。呼ぶのは診断ログだけ（`SystemAppsUsingDarkTheme` は呼び出し 0） | 既存テストで他の動きに変化なし。キーなしの経路は実行環境に依存（テストなし） |

## A 組の覚え書き（2026-11-19。RV9-2〜6）
- 動きを変えた箇所は G1 だけ。そのほかの `!` は、null のときは従来どおり `NullReferenceException` になるまま、理由コメントを付けた（挙動は変えない）。
- 設定が後から入るフィールドは `= null!;`（理由コメント付き）。対象は `App.xaml.cs` の起動で設定するもの、`DS4SixAxis` などのコンストラクターで設定するもの、`Core/Platform/EnvironmentService` の列挙で設定する値。
- 空きスロットを表す `Controllers[i] = null`・`TouchPads[i] = null` は、配列の要素の型を `?` にする作業を別に残し、`null!` と理由コメントで保った（`DeviceDisconnectHandler`・`ServiceLifecycle`）。要素の型を `?` へ変えるのは、B 組のあとの整理（RV9-13 の前）で検討する。
- `DS4WindowsCli` は、`CodeHealthScanner` の対象外（本体の `DS4Windows/` だけを数える）。指令とビルドの警告 0 は確認済み。
- 文字列でソースを検査するガードテスト 4 件（`DisconnectGuardTests`・`ConnectionGuardTests`・`StartupBootstrapGuardTests`・`BatteryUnificationTests`）は、`!`・`?` の分だけ期待値を更新した（検査の意図は同じ）。
