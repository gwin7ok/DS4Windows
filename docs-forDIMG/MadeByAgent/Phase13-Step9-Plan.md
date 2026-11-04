# Phase13-Step9 個別計画書: `running`／`suspending` の転送の削除（F-5c）

作成日: 2026-11-04
対象ブランチ: `Phase13`
上位計画: `Phase13-Plan.md` §4 Step9（ロードマップ F-5c、Q7）
状態: **承認待ち**（§9）

---

## 1. 目的

`ControlService.running`／`suspending` は、実体が `IServiceLifecycle`（`Running`／`Suspending`）にあり、`ControlService` は転送しているだけである（`ControlService.cs` 55〜58 行・90 行の F-5c の TODO）。
利用者に `IServiceLifecycle` を直接渡して、転送を削除する。`AutoProfileService` は Step6 6-b で対応済みなので、残る利用者は 3 つである。

## 2. 調査結果（2026-11-04）

| 利用者 | 使い方 | 箇所数 |
|---|---|---|
| `App.xaml.cs` | `CleanShutdown` で `_controlService.running` を見て、動作中なら `Stop`／`ShutDown` | 1（1258 行） |
| `MainWindow.xaml.cs` | `running` の読み取り（開始・停止ボタンの表示、再起動、スロット管理画面の有効化ほか）。`suspending` の書き込み（電源イベントの復帰で `false`、サスペンドで `true`） | `running` 9、`suspending` 2 |
| `MainWindow.Ipc.cs` | IPC の `start`／`stop` の判定と、`running` の問い合わせの応答 | 4 |
| `TrayIconViewModel.cs` | トレイメニューの「開始／停止」の表示 | 1（160 行） |
| テスト | `Phase11W4SaveApplyWiringGuardTests` が `if (!service.running) { return; }` という**ソース文字列**を検査（1 件）。ほかに本物の利用はない | 1 |

- `ControlService` の内部は `_lifecycle` を直接使っており、`running`／`suspending` の転送は**外部のためだけ**に残っている。
- `MainWindow` は現在 19 引数（`ControlService` を含む）、`TrayIconViewModel` は 7 引数。どちらも `ControlService` は **ほかの目的でも使う**（`Start`／`Stop`、`slotManager`、イベントの購読）ため、`ControlService` の引数は残り、`IServiceLifecycle` が**1 つ増える**（Step10 で `Start`／`Stop` などを移して相殺する）。
- `IServiceLifecycle` は既に登録済みの Singleton（Step3 以前から）。新しい型は作らない。
- `Running` は `RunningChanged` イベントを伴うが、本 Step ではイベントの購読先は変えない（`ControlService` の転送イベントは残す。Step10 の対象）。

## 3. 設計案

| 対象 | 変更 |
|---|---|
| `App` | `InitializePostHostServices` で `IServiceLifecycle` を解決して保持。`CleanShutdown` は `_lifecycle.Running` を見る（`_controlService` の null 判定は維持。起動の途中で終了した場合に備える） |
| `MainWindow`／`MainWindow.Ipc` | コンストラクターに `IServiceLifecycle` を追加。`controlService.running`→`lifecycle.Running`、`controlService.suspending = …`→`lifecycle.Suspending = …` |
| `MainWindowFactory` | `IServiceLifecycle` を受けて `MainWindow` へ渡す |
| `TrayIconViewModel` | コンストラクターに `IServiceLifecycle` を追加。`controlService.running`→`lifecycle.Running`。生成は `MainWindow`（引き継ぐ） |
| `ControlService` | `running`／`suspending` を削除。F-5c の TODO を削除 |
| テスト | `Phase11W4SaveApplyWiringGuardTests` の文字列を新しい書き方に更新。`TrayIconViewModel` の引数の検査を更新。ガード（`ControlService` に `running`／`suspending` の公開メンバーが 0 件）を追加 |

## 4. 実装の分割（各サブステップを別コミット。各回 `Debug ALL`）

| サブ | 内容 |
|---|---|
| 9-a | 本計画書 |
| 9-b | `App` と `TrayIconViewModel`（`MainWindow` が生成するため、`MainWindow` に引数を足す必要がある）。※依存の関係で `MainWindow` の引数追加は 9-b に含める |
| 9-c | `MainWindow`／`MainWindow.Ipc` の `running`／`suspending` を切り替え、`MainWindowFactory` を更新 |
| 9-d | `ControlService` の `running`／`suspending` を削除、F-5c の TODO を削除、ガードの追加、結果の記録 |

## 5. 影響範囲

開始・停止ボタン、トレイの表示、電源イベント（サスペンド／復帰）、IPC の `start`／`stop`、終了時の後始末。挙動は変えない（同じ値を読み書きする先が `IServiceLifecycle` に直接なるだけ）。ログは変えない。

## 6. リスクと対策

| リスク | 対策 |
|---|---|
| 終了時（`CleanShutdown`）にホストが未構築・破棄済みで `IServiceLifecycle` が取れない | `_lifecycle` が null のときは従来どおり何もしない（`_controlService` の null 判定と同じ扱い） |
| `suspending` の書き込み先を取り違える | 実体は同じ（`ControlService.suspending` は `_lifecycle.Suspending` への転送）。9-c でこの対応を確認するテストを追加 |
| `MainWindow` の引数が 20 になる | Step10 で相殺する（`Phase13-Plan.md` Step9 の記載どおり） |

## 7. 規約への適合

新しい型は作らない（1 ファイル 1 型、配置 R1〜R7 に変更なし）。ラチェット（`Global`／`AppHost`／`Program.rootHub`）の件数は変えない。

## 8. 検証

`Debug ALL`（警告 0、エラー 0、全テスト合格）。実機確認（最後にまとめて）: **開始・停止**、トレイの「開始／停止」表示、PC のサスペンド・復帰、終了時の停止。

## 9. 決定事項（承認をお願いします）

1. **利用者への渡し方**: `IServiceLifecycle` を直接渡す（案 A。上位計画の Q7 どおり）。
2. **転送の削除範囲**: `ControlService` の `running`／`suspending` を**削除**する（案 A）。`Start`／`Stop` や他のイベントの転送は残す（Step10）。
3. **終了時の扱い**: `CleanShutdown` で `IServiceLifecycle` が取れないときは、従来どおり何もしない。
4. **分割**: 4 コミット（9-a〜9-d）で、以後は自動進行（Step7・8 と同じ）。

---

## 10. 実施結果（2026-11-04）

決定 1〜4 は、すべて推奨案で承認済み（2026-11-04）。

| サブステップ | コミット | 内容 |
|---|---|---|
| 9-a | `a129c9c1` | 本計画書 |
| 9-b／9-c | `f07eea06` | `App`／`MainWindow`／`MainWindow.Ipc`／`TrayIconViewModel` が `IServiceLifecycle` を直接使う。`MainWindowFactory`・`MainWindow`・`TrayIconViewModel` の引数が 1 つ増えた（依存の都合で、9-b と 9-c は 1 コミットにまとめた） |
| 9-d | （本コミット） | `ControlService` の `running`／`suspending` と F-5c の TODO を削除。`Phase13Step9RunningForwardRemovalGuardTests` を新設。`Phase9E6LifecycleGuardTests` の転送の検査を「無いこと」の検査へ更新 |

- `CleanShutdown` は、`_lifecycle` が null のときは従来どおり何もしない（決定 3）。
- `Debug ALL`: 警告 0、エラー 0、3126 件合格。
- 実機確認（最後にまとめて）: 開始・停止、トレイの「開始／停止」表示、PC のサスペンド・復帰、終了時の停止。
