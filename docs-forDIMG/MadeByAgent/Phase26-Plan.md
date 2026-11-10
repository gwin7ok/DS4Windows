# Phase26 計画書: 画面のロジックを code-behind から出す（Structure-Review の B）

作成日: 2026-11-11

## 目的
`.xaml.cs` 合計 約 10,500 行（`MainWindow` 1,858、`ProfileEditor` 1,802、`BindingWindow` 961、`RecordBox` 900、`AssignmentEditor` 698）の、画面に依存しない判断・計算を、テストできる部品（`UI/Services`・`UI/ViewModels`）へ出す。code-behind は描画とイベントの橋渡しだけにする。

## 進め方
- 画面ごと・メソッドごとに小さく進め、各 Step で単体テストを足す（部品は WPF なしでテストできる形にする）。
- 動作は変えない。UI の実機確認が要る変更は、`Known-Issues-Ledger.md` の実機確認の項目へ足す。
- 大きなメソッドの候補（行数の多い順）:
  - `MainWindow`: `OnSourceInitialized`(71)、`SetupEvents`(61)、`PowerEventArrive`(61)、`LateChecks`(60)、`ShowProfileEditor`(56)、`HotkeysTimer_Elapsed`、`Check_Version`(54)、`OnProfileChanged`(42)
  - `ProfileEditor`: `Reload`(115)、`SortSpecialActionsList`(103)、`RumbleTestBtn_Click`(92)、`EditActionBtn_Click`(91)、`InputDS4`(73)
- タイマー・電源イベントなど実行時の調整役は Phase27（E）と重なるため、そちらで扱う。

## Step 一覧
| Step | 内容 | 状態 |
|---|---|---|
| 26-1 | `MainWindow` のスワイプ切替の規則を `TouchpadSwipeProfileRule` へ出す（タイマーとディスパッチは窓に残す） | 完了（2026-11-11） |
| 26-2 | `ProfileEditor.RumbleTestBtn_Click`（92 行）の判断を `RumbleTestController` へ出す（`SortSpecialActionsList` は既に薄く、対象から外した） | 完了（2026-11-11） |
| 26-3 | `ProfileEditor.Reload` の、画面に依存しない初期化を ViewModel へ出す | 未着手 |
| 26-4 | `MainWindow.Check_Version`／`LateChecks` の判断を部品へ出す | 未着手 |
| 26-5 | `BindingWindow`・`RecordBox`・`AssignmentEditor` の同様の整理 | 未着手 |

## 履歴
- 2026-11-11: 計画書を作成。26-1 を完了（`TouchpadSwipeProfileRule` と 17 件のテストを追加。全 3,399 テスト合格）。スワイプの文字列 "left"／"right" は文字 t を含むため通知も出る、という元の挙動をテストで固定した。
- 2026-11-11: 26-2 を完了（`RumbleTestController`・`RumbleTestOutcome` とテスト 5 件。全 3,404 テスト合格）。押したボタンの文字は、反転設定でも常に「押したボタン」が変わる（元の分岐を整理しても結果は同じ）。
