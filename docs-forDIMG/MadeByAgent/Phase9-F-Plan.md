# Phase9 9-F（UI のコードビハインド）計画書

作成日: 2026-10-22
状態: 提案（承認待ち）
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

## 3. 進め方（提案）
1. **F-3 の事前調査**（回帰かどうか。実機の比較が要る。利用者の作業）は、後回しのまま。
2. **F-1 → F-2 → F-4 → F-5** の順（推奨）。各 F で、特性化テスト → 切り出し → ビルド・テスト → 実機（画面）確認。
3. 画面の実機確認が必須のため、1 回の変更を小さくする（`Application_Startup` は、手順ごとに段階を分ける）。

## 4. 決めてほしいこと
- Q1: 着手順（F-1 から／F-4 から／F-2 から）。
- Q2: F-3（トレイ）を、今回の 9-F に含めるか（回帰の確認を先に行う）。
- Q3: 9-F の範囲を、`App.xaml.cs` と `MainWindow` までにし、`ProfileEditor`（F-4）は、今回の UI 変更で内容が変わったため見送るか。
