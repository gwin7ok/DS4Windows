# Phase13b 個別計画書: ログの一括整理（L2）

作成日: 2026-11-04
状態: **草案（承認待ち）**
ブランチ: `Phase13b`（`master` の c8e6fd31 から作成）
根拠: `Log-Policy.md`、規約 `.github/copilot-instructions.md` §2.3（Phase13b に限る例外）、`Phase12-Step1-Log-Inventory.md`、`Phase11-and-Beyond-Roadmap.md` §3 Phase13b

## 1. 目的と範囲

`Log-Policy.md` に沿って、アプリ全体のログ出力（目録 666 か所・101 ファイル）の経路・レベル・文言・ガードを整理する。機能は変えない（規約 §2.2）。`[DI]` 接頭辞は維持する。

## 2. 決定事項（2026-11-04、ユーザー承認済み）

| # | 項目 | 決定 |
|---|---|---|
| D1 | 進め方 | **シム併用で段階移行**。先に `Log(level, message, options)` を新設し、旧 API は新 API へ委譲する薄いシムにする。領域ごとに移行し、最後にシムを削除する（規約 §2.1） |
| D2 | GUI の印 | `options` に **UserFacing（利用者向け）と Temporary（一時表示）**。旧 `LogToGui(warning, temporary)` は対応表（§4）で新 API へ写し、旧実装と同じ出力であることをテストで固定する |
| D3 | `LogToTray` | **通知として別 API のまま残す**（ログではなく利用者への通知）。`[Diag-Toast]` の診断行は削除 |
| D4 | AppLogger 外の出力 | **AppLogger へ集約**。`Console`／`Debug.WriteLine`（29 か所）は移すか削除。Updater の NLog 直接（`UpdaterInstaller` 15、`UpdaterElevatedHelper` 9）も、別プロセス上の事情がなければ AppLogger 経由へ。`LoggerHolder`／`AppLogger` 内部の NLog は基盤なので残す。事情があるものは個別に残し、理由を記録する |

## 3. 現状（2026-11-04 実測。1 行に収まる呼び出しのみ）

| 経路 | 件数 |
|---|---|
| `AppLogger.LogTrace`／`LogDebug` | 199／183（補間文字列は 183／162） |
| `AppLogger.LogToGui` | 114（`warning`: false 57／true 47、`temporary` 付き 14） |
| 各サービスの private `LogDebug`（GUI に出る） | 約 40（定義は 9 ファイルに重複） |
| `LogInfo`／`LogWarn`／`LogError` | 25／18／22 |
| `LogToTray` | 16（13 ファイル） |
| NLog 直接 | 36（`UpdaterInstaller` 15、`UpdaterElevatedHelper` 9、`AppLogger` 9、`LoggerHolder` 3） |
| `Console`／`Debug.WriteLine` | 26／3 |
| ガード（`IsTraceEnabled`／`IsDebugEnabled`） | 74（補間文字列の Trace／Debug 約 345 に対し） |
| ログを参照するテスト | 15 ファイル |

## 4. 旧 `LogToGui` と新 API の対応表（案。13b-1 で確定しテストで固定）

| 旧 | 新（Level／options） | ファイルログ | GUI |
|---|---|---|---|
| `LogToGui(msg, false)` | Info／UserFacing | Info | 通常表示 |
| `LogToGui(msg, true)` | Warn／UserFacing | Warn | 警告表示 |
| `LogToGui(msg, w, true)` | 上記＋Temporary | 出さない | 一時表示 |
| private `LogDebug(msg, warning)` | Info／Warn＋UserFacing（見直し対象。GUI に出ている現状を維持） | 旧と同じ | 旧と同じ |

注: 旧実装は `GuiLog` イベントで `LoggerHolder.WriteToLog` に渡る。実際のファイル出力レベルは 13b-1 で現行コードから確定する。

## 5. 変更の記録方法

変更を 3 種に分けて `Phase13b-Log-Change-List.md` に記録する。

- **レベルのみ**（例: Debug→Info）
- **文言**（接頭辞の統一、リソース化を含む）
- **削除**（`[Diag-Toast]`、重複、不要なもの）

**GUI・トレイに見える変更は、別表にして承認を得てから実施する**（Phase8-Step8-Q7 の前例。13b-2）。

## 6. Step 構成（各サブステップは Debug ALL が緑になってコミット）

| Step | 内容 | 承認 |
|---|---|---|
| 13b-1 | `LogOptions`（1 ファイル 1 型）と `AppLogger.Log(level, msg, options)` を新設。旧 `LogToGui`／`LogInfo`／`LogWarn`／`LogError`／`LogDebug`／`LogTrace` を新 API へ委譲するシムにする。対応表のテスト。出力は不変 | 不要（出力不変） |
| 13b-2 | 全件の分類一覧（レベルのみ／文言／削除）を作成。GUI・トレイに見える変更を別表にして提示 | **必須（一覧で承認）** |
| 13b-3 | サービス群の private `LogDebug`（9 ファイル）を新 API へ移行し、定義を削除。`IIpcCommandHost.LogDebug` は契約を維持して実装側を新 API へ（CLI 件は 13b-9） | 13b-2 の範囲内 |
| 13b-4 | `DS4Control/` 配下の残り（Profile／XML 系、`SyntheticInputCommitter` ほか）を移行 | 同上 |
| 13b-5 | UI（`DS4Forms`、`App.xaml.cs`）を移行。`[Diag-Toast]` 削除、`LogToTray` 周辺の整理 | 同上 |
| 13b-6 | 入力層・HID（`DS4Device`、`InputDevices/*`、`HidLibrary`）の `Console`／`Debug.WriteLine` を AppLogger へ移行または削除 | 同上 |
| 13b-7 | Updater の NLog 直接を AppLogger 経由へ（別プロセス事情の確認を先に行う） | 同上 |
| 13b-8 | §6 のガード不足（高頻度経路の補間文字列 Trace／Debug）を解消 | 不要（性能面のみ） |
| 13b-9 | シム削除、`AppLogger.TrayIconLog` 削除、ラチェット基準値の更新。**CLI の `ChangeLedColor`／`LoadProfile` の原因調査**（`master` 旧ビルドとの比較）を、`IpcCommandProcessor` 周辺のログ変更の前に実施 | 調査結果を報告 |
| 13b-10 | 台帳・ロードマップ §7・モデル図（構造に関わる変更があれば）・`Log-Policy.md` の更新 | 不要 |
| 13b-11 | Release ビルド（`bin\x64\Release\` と `bin\x64\DS4Windows\` の 2 か所）→ 実機確認（1 回。持ち越し項目を含む）→ `Phase13b-Completion-Report.md` | 実機確認 |

別コミットの小作業: テストの `[Collection("ServiceProviderHolder")]` の改名（13b とは独立）。

## 7. テスト方針

- ログを期待値にするテスト（15 ファイル）は、変更と同じコミットでレベルと主要語（`key=value`）の検査へ直す（Log-Policy §8）。
- 新 API とシムの対応表（§4）は、旧実装との同値性を直接検査するテストで固定する。
- ラチェット系（`Step10FourGlobalRatchetGuardTests` ほか）は、件数が変わったら同じコミットで基準値を更新する。

## 8. リスクと対策

| リスク | 対策 |
|---|---|
| GUI ログの見え方が意図せず変わる | 13b-1 で出力不変をテストで固定。変更は 13b-2 の一覧で承認 |
| 入力ループ（毎フレーム）の性能悪化 | 補間文字列にガード（13b-8）。差分が大きい箇所は A/B 計測（Phase9 の前例） |
| Updater の別プロセスでの動作 | 13b-7 の前に `UpdaterElevatedHelper` の実行形態を確認。事情があるものは残して理由を記録 |
| 巨大ファイル編集 | 対象行のみ置換（規約 §3.2） |
| CLI 件との混同 | 13b-9 で、ログ変更の前に旧ビルド比較を実施 |

## 9. 規約の順守

- 1 ファイル 1 型、ファイル名＝型名、配置ルール R1〜R7（新規 `LogOptions`／新 API の置き場所は 13b-1 で決める。実装は `DS4Control/`〔AppLogger と同じ〕を想定）。
- `[DI]` 接頭辞の維持。新しいコードは最初から Log-Policy に沿う。
- 実機確認は Phase の最後に 1 回だけ。
