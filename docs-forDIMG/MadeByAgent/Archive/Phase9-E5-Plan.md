# Phase9 9-E E-5（入力レポートの処理の切り出し）計画書

作成日: 2026-10-22
状態: **承認済み（2026-10-22。§7）。実装済み・ビルド・テスト合格・実機確認も問題なし（台帳 §37。P9E5-5［遅延の警告］は任意のため未実施）**
上位: `Phase9-E-Plan.md`（E-1〜E-4 は完了）、`Phase9-Responsibility-Split-Plan.md`

## 1. 目的と現状

`ControlService` の `On_Report`（約 230 行）は、入力レポートが届くたびに 1 回呼ばれる**高頻度の経路**（ホットパス）。
ここには、本来は別の責務である処理が同居している。目的は、**挙動も呼び出しの順序も変えずに**、独立して扱える部品を切り出し、`On_Report` を「順番に呼ぶだけ」の司令塔にすること。

`ControlService.cs` の現在の行数は約 2,170 行（E-1〜E-4 の後）。

## 2. `On_Report` の中身（1717〜1946 行）

| 区分 | 内容 | 行数の目安 | 状態を持つか |
|---|---|---|---|
| a. エラー・遅延の監視 | `device.error` のログ、`inWarnMonitor`（接続 5 秒後に監視開始）、`lag`、`LagFlashWarning`（ライトバーを赤く点滅） | 約 40 | `lag[]`、`inWarnMonitor[]`、`tempStrings[]` |
| b. 状態の取り出し | `CurrentState` へのコピー、結合デバイスの `MergeStateData` | 約 20 | `CurrentState[]` |
| c. 初回レポートの処理 | プロファイルの不足アクションのログ（一度だけ） | 約 20 | なし |
| d. 結合デバイス（副）の早期処理 | ジャイロの Mouse／MouseJoystick を主デバイスへ反映して return | 約 35 | なし |
| e. タッチパッドの切り替え | `CheckForTouchToggle`（PS＋タッチで ON／OFF） | 約 25（別メソッド） | `touchreleased[]` |
| f. マッピング | `SetCurveAndDeadzone`、`MapCustom`、OSC の前後処理 | 約 35 | なし（ほかの部品が持つ） |
| g. 出力 | DS4 のトリガーモード後処理、`ConvertandSendReport`、処理遅延の計測 | 約 30 | `ProcessingDelayMs[]` |
| h. 後処理 | `Commit`、ライトバー更新、結合状態の保存 | 約 15 | なし |

周辺（別メソッド）: `GetActiveInputControl`（約 75 行。UI のプロファイル編集が使う）、`TouchpadSlide`（約 25 行。メイン画面が使う）、`StartTPOff`。

## 3. 切り出しの候補（案）

### 案 A: 最小（推奨）
`On_Report` 本体は `ControlService` に残し、独立した小さい部品だけを出す。ホットパスの構造は変えない。
1. **`LatencyMonitor`（遅延の監視）**: 区分 a（`lag`、`inWarnMonitor`、`LagFlashWarning`）。状態を持つ Singleton。`LogDebug`／`DS4LightBar` を使う。
2. **`TouchpadToggleService`（タッチパッドの切り替え）**: 区分 e と `TouchpadSlide`、`StartTPOff`（`touchreleased[]`、`touchslid[]` を持つ）。
3. **`ActiveInputProbe`（押されている入力の探索）**: `GetActiveInputControl`（状態なし。純粋な関数）。

ControlService は約 2,170 → 約 1,950 行になる見込み。

### 案 B: 大（On_Report 全体を別クラスへ）
`On_Report` 全体を `InputReportProcessor` へ移し、`CurrentState`／`TempState`／`MappedState`／`ExposedState`／`touchPad`／`outputDevices` を、ホストの窓口（`IInputReportHost`）経由で受ける。
- 長所: ControlService が約 1,900 行へ減り、責務が最も明確になる。
- 短所: ホットパスに窓口経由の呼び出しが入る。高頻度の経路なので、割り当て（GC Alloc）と時間の計測（`ControlServiceHotPathAllocationTests`、`Phase9HotPathTimingBenchmark`）で確かめる必要がある。状態の配列の持ち主を動かすので、影響範囲が広い。

### 案 C: 切り出さない
`On_Report` は ControlService のまま。E-6（ライフサイクル）に進む。

## 4. 守ること（案 A・B 共通）

- 呼び出しの順序を変えない（a → b → c → d → e → デバウンス → f → g → h）。
- ホットパスで新しい割り当てをしない（ゼロアロケーション方針）。`Func`／ラムダの新規生成、LINQ、文字列の組み立てを、毎レポートの経路に入れない。
- ログの文言とレベルを変えない（`LogDebug`、`AppLogger.LogToTray`）。
- ライトバーの点滅の条件（`FlashWhenLate`、`FlashWhenLateAt`）を変えない。
- 薄い委譲メソッド（`CheckForTouchToggle`、`TouchpadSlide`、`GetActiveInputControl`、`StartTPOff`）は ControlService に残す（画面と既存テストが使う）。
- 差分テスト: 旧コードの逐語コピーと、新しい部品の出力を、同じ入力で比べる（Phase9-B／9-C／9-E と同じ方式）。

## 5. 実機確認が要るもの

- 遅延の警告（ライトバーが赤く点滅）は、実機が遅れないと出にくい。差分テストで担保し、実機は「通常の使用で、警告が出ないこと」だけを見る。
- タッチパッドの切り替え（PS ボタンを押しながらタッチ）は、DS4 など、タッチパッドのある実機が必要。

## 6. 決めてほしいこと

- **Q1（範囲）**: 案 A（最小）／案 B（大）／案 C（切り出さない）。推奨は案 A。
- **Q2（ActiveInputProbe）**: `GetActiveInputControl` を純粋な関数として出す（推奨）／ControlService に残す。
- **Q3（進め方）**: 案 A なら、3 部品を 1 回でまとめて実装する（推奨。どれも小さい）／部品ごとに分けて、ビルド・テストを挟む。

## 7. 決定事項（2026-10-22 承認）

- **Q1＝案 A（最小）**: `On_Report` 本体は `ControlService` に残し、遅延の監視、タッチパッドの切り替え、押されている入力の探索の 3 部品だけを出す。高頻度の経路の構造は変えない。
- **Q2＝純粋な関数として出す**: `GetActiveInputControl` の本体は `ActiveInputProbe`（状態なし）へ。`ControlService` に薄い委譲を残す。
- **Q3＝3 部品を 1 回で**。

## 8. 実装（2026-10-22）

| 部品 | 契約（`DS4Windows/DI/`） | 実装（`DS4Windows/DS4Control/Services/`） | 移したもの |
|---|---|---|---|
| 遅延の監視 | `ILatencyMonitor` | `LatencyMonitor` | `On_Report` 先頭のエラーのログと遅延の監視、`LagFlashWarning`、状態 `lag`／`inWarnMonitor`／`tempStrings` |
| タッチパッドの切り替え | `ITouchpadToggleService` | `TouchpadToggleService` | `CheckForTouchToggle`／`StartTPOff`／`TouchpadSlide`、状態 `touchreleased`／`touchslid` |
| 押されている入力の探索 | `IActiveInputProbe` | `ActiveInputProbe` | `GetActiveInputControl` の本体（状態なし） |

- `ControlService` の `lag`／`inWarnMonitor`（public の配列）は、外部の参照が 0 件だったため、部品の private に移して公開をやめた。
- `ControlService` の `CheckForTouchToggle`／`StartTPOff`／`TouchpadSlide`／`GetActiveInputControl` は、薄い委譲として残した（画面とプロファイル読み込みが使う）。
- ログは、各部品の `DebugMessage` イベントを `ControlService` が購読して、これまでどおり Debug イベントへ流す（文言とレベルは同じ）。
- 切断・停止時の `lag[i] = false; inWarnMonitor[i] = false;` は `_latencyMonitor.Reset(i)` に置き換えた（呼び出す位置は同じ）。
- テスト: `Phase9EInputReportPartsTests`（値の特性化）、`Phase9EInputReportGuardTests`（移したメンバーの不在、薄い委譲の存在、`On_Report` の呼び出し順序、DI 登録、静的な窓口の不参照）。`DS4Device`／`Mouse` は作れないため、それらを読まない経路を調べ、遅延の監視の本体は実機で担保する（台帳 §37）。
