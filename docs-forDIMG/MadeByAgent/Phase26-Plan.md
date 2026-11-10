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
| 26-3 | `ProfileEditor.Reload` の、入力の表示へ渡すデッドゾーン・ドリフトの値の集め方を `ReadingsDeadZoneSnapshot` へ出す（残りの `Reload` は、画面部品の初期化の順序が中心で窓に残す） | 完了（2026-11-11） |
| 26-4 | `MainWindow.Check_Version` の判断を `UpdateCheckPolicy` へ出す（`LateChecks` は起動の順序が中心で、Phase27 で扱う） | 完了（2026-11-11） |
| 26-5 | `BindingWindow`・`RecordBox`・`AssignmentEditor` の同様の整理 | 一部完了（2026-11-11。`BindingWindow.TestRumbleBtn_Click` を `ActionRumbleTestController` へ。ほかは続く） |

## 履歴
- 2026-11-11: 計画書を作成。26-1 を完了（`TouchpadSwipeProfileRule` と 17 件のテストを追加。全 3,399 テスト合格）。スワイプの文字列 "left"／"right" は文字 t を含むため通知も出る、という元の挙動をテストで固定した。
- 2026-11-11: 26-2 を完了（`RumbleTestController`・`RumbleTestOutcome` とテスト 5 件。全 3,404 テスト合格）。押したボタンの文字は、反転設定でも常に「押したボタン」が変わる（元の分岐を整理しても結果は同じ）。
- 2026-11-11: 26-1 の通知の扱いを是正。`TouchpadSlide` の返り値は "right"／"left"／""／"none" だけで、"t" を含むのは左右のスワイプのみ（タップではない）。スワイプの切替は適用処理が通知を出すため、`MainWindow` のタイマー側の通知（`ShowHotkeyNotification`）と `WantsNotification` を削除した（通知の二重を解消する挙動の変更。実機の確認は台帳へ）。
- 2026-11-11: 26-3 を完了（`ReadingsDeadZoneSnapshot` とテスト 3 件。全テスト合格）。
- 2026-11-11: 26-4 を完了（`UpdateCheckPolicy`・`UpdateCheckOutcome` とテスト 7 件）。版の記録が読めないときは何も知らせない、という元の挙動を固定した。共有ストアの振動の強さを書き換えるテストが、別のテストを時々落とした（並列実行の競合）ため、同じ xunit コレクションに入れて同時に走らせないようにした。
- 2026-11-11: 26-5 の 1 件目として、出力ピッカーの振動テストを `ActionRumbleTestController` へ出した（テスト 4 件）。
