# Phase13b 完了報告書（ログの一括整理 L2）

作成日: 2026-11-04
ブランチ: `Phase13b`（`master` の c8e6fd31 から）。計画: `Phase13b-Plan.md`、変更の記録: `Phase13b-Log-Change-List.md`、進捗: `Phase13b-Status.md`

## 1. 結果

`Log-Policy.md` に沿って、ログの入口を `AppLogger.Log(AppLogLevel, string, LogOptions)` に一本化し、レベル・経路・接頭辞を整理した。機能は変えていない。

| 項目 | 内容 |
|---|---|
| API | `Log()` を新設。`LogTrace`／`LogDebug`／`LogInfo`／`LogWarn`／`LogError` は短縮形として残す（承認済み）。`LogToGui`・`AppLogger.TrayIconLog`・各サービスの private `LogDebug`（`DebugMessage` ごと）・`IServiceDebugRelay`／`ServiceDebugRelay`・`ControlService.Debug` を廃止 |
| GUI・トレイの変更（承認済み G1〜G4） | G1 失敗系 Warn→Error 32 件、G2 Info→Warn 20 件、G3 一時表示の診断→ファイル Debug 5 件、G4 トレイは変更なし |
| ファイルのみ | F1 握りつぶした例外を Warn へ（26 件）、F2 接頭辞統一、F3 `[Diag-Toast]` 削除、F4 `Console`／`Debug.WriteLine` を移行 17・削除 7、F5 Updater の NLog 直接を AppLogger へ（重複 15 組を統合）、F6 毎フレーム経路のガード 1 件 |
| 構造 | `ControlService` のコンストラクター 17 → 16 引数。`LoggerHolder`／`LogViewModel` から `ControlService` 引数を削除 |

## 2. 検証

- Debug ALL: 警告 0・エラー 0。Actions 3,150 件、Standalone 11 件が合格（ラチェットの基準値の変更なし）。
- Release ビルド 2 か所（v4.3.0）: `DS4Windows\bin\x64\Release\`、`DS4Windows\bin\x64\DS4Windows\`。

## 3. 実機確認（2026-11-04。1 回）

| # | 項目 | 結果 |
|---|---|---|
| 1 | 起動・接続・切断・Stop／Start とログタブ | 正常 |
| 2 | 警告表示（G2） | 再現できず見送り |
| 3 | ログファイル（WARN／ERROR の妥当性） | 妥当 |
| 4 | トレイ通知・プロファイル操作 | 正常 |
| 5 | CLI `ChangeLedColor`／`LoadProfile` | **やはり反応しない**（未解決のまま） |
| 6 | スリープ復帰 | 見送り |
| 7 | OSC／UDP／HidHide | 見送り |
| 8 | タッチパッド・振動・エディターの入力取得・`LoadTempProfile` | 見送り |

## 4. 未解決・持ち越し

- **CLI の `ChangeLedColor`／`LoadProfile`**: 今回の変更では、`IpcCommandProcessor` のロジックには触れていない（`LogDebug` の出し先だけ `AppLogger.Log` に変えた）。回帰かどうかは、旧版（`master` のビルド）との比較で判定する（先送り台帳 Phase13b-1）。
- 見送った実機確認（G2 の警告表示、スリープ復帰、OSC／UDP／HidHide、タッチパッド、振動、エディターの入力取得、`LoadTempProfile`）は、次の実機確認の機会にまとめて行う。
- 利用者向けログ文言のリソース化（先送り台帳 Phase13b-2）、`[Collection("ServiceProviderHolder")]` の改名（同 Phase13b-3）。

## 5. 次の作業

Phase14（名前空間・フォルダの再編）。詳細は `Phase11-and-Beyond-Roadmap.md` §7。
