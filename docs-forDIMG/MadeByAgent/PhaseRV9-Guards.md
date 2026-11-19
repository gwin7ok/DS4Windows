# PhaseRV9 ガード台帳（動きを変えうる修正）

注釈（`?`）だけでは済まず、`null` の流れを変えた箇所を 1 件ずつ書く。

| # | ファイル:行 | 以前の動き | 新しい動き | 根拠のテスト |
|---|---|---|---|---|
| G1 | `Core/Utilities/Util.cs` `GetOSProductName`・`GetOSReleaseId`・`SystemAppsUsingDarkTheme` | レジストリの値がない（キーなし）とき `NullReferenceException` | `?.ToString() ?? string.Empty`（OS 名・リリース ID は空文字、ダークテーマの判定は偽）。呼ぶのは診断ログだけ（`SystemAppsUsingDarkTheme` は呼び出し 0） | 既存テストで他の動きに変化なし。キーなしの経路は実行環境に依存（テストなし） |
