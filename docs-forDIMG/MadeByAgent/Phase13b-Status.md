# Phase13b 進捗（ログの一括整理 L2）

計画: `Phase13b-Plan.md`（承認済み 2026-11-04）。変更の記録: `Phase13b-Log-Change-List.md`。ブランチ: `Phase13b`（`master` の c8e6fd31 から）。

| Step | 内容 | 状態 |
|---|---|---|
| 13b-1 | `AppLogger.Log(AppLogLevel, msg, LogOptions)` を新設。旧 API はそこへ委譲。出力は不変 | 完了（`e3b74f63`） |
| 13b-2 | 変更の一覧。GUI・トレイの変更 G1〜G4 を承認 | 完了（`3423a0f0`） |
| 13b-3 | サービスの private `LogDebug`／`DebugMessage`／`ServiceDebugRelay`／`ControlService.Debug` を廃止 | 完了（`39c1ab05`） |
| 13b-4 | `Actions`・`DS4Control` のレベル整理（G1 23／G2 17／G3 5／F1 26） | 完了（`2883c354`） |
| 13b-5 | UI・入力層・Updater のレベル整理、`[Diag-Toast]` の削除 | 完了（`8d669cae`） |
| 13b-6 | `Console`／`Debug.WriteLine` の移行 17・削除 7 | 完了（`25182c1a`） |
| 13b-7 | Updater の NLog 直接を AppLogger へ（重複 15 組を統合） | 完了（`5ec596b4`） |
| 13b-8 | 接頭辞の統一、毎フレーム経路のガード | 完了（`592dc658`） |
| 13b-9 | `LogToGui`・`TrayIconLog` の削除（61 か所の書き換え） | 完了（`2cde72f3`） |
| 13b-10 | モデル図 03・04・01、`Log-Policy.md`、ロードマップ §7、先送り台帳 | 完了（コミット待ち→本コミット） |
| 13b-11 | Release ビルド 2 か所 → 実機確認（1 回）→ 完了報告書 | Release ビルドの後、実機確認待ち |

検証（各 Step）: Debug ALL で警告 0・エラー 0。13b-9 時点で Actions 3,150 件、Standalone 11 件が合格（13b-3 で `ServiceDebugRelay` のテストを削除、13b-9 で `LogToGui` の対応表のテスト 4 ケースを新 API のテストに置換）。ラチェットの基準値の変更なし。

実機確認で見る点: ログタブとステータス行の表示（G1〜G3 の変化）、起動・停止・接続・切断のログ、`Logs\ds4windows_log.txt` の Warn／Error、Updater の昇格インストールのログ、持ち越しの項目（CLI `ChangeLedColor`／`LoadProfile`、スリープ復帰、OSC／UDP、タッチパッド、振動、エディターの入力取得、`LoadTempProfile`）。
