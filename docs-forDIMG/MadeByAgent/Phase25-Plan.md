# Phase25 計画書: 設定を「項目ごとの配列」から「領域ごとの持ち主のオブジェクト」へ

作成日: 2026-11-10
背景: `Structure-Review-2026-11.md` の問題 A。`IProfileSettingsService` が項目の数だけ配列を公開し（開始時 156 メンバー）、`BackingStore` が約 100 本の `X[slot]` の配列を持っていた。利用者は「どの配列の何番目か」を知らなければならず、`Mapping` などが巨大な窓口を受けていた。

## 1. 目的と終わりの状態

- 設定の値の持ち主を、領域（ジャイロ、タッチパッド、トリガー、スティック、ライトバー、出力、一般、6 軸）ごとの **スロットのオブジェクト**（例 `GyroSlotSettings`）にする。`BackingStore` が領域ごとに配列（スロット数分）のオブジェクトを持つ。
- 保存・読み込み（プロファイルの XML）、実行時の入力処理、設定画面の ViewModel は、同じ 1 つのオブジェクトを読み書きする（窓口の二重化をやめる）。
- `IProfileSettingsService` からは、移した領域の配列・`Get…(slot)` を削除し、利用者は `ForSlot(slot).<領域>` を使う。`ForSlot` はスロットごとに 1 度だけ作って使い回す（毎レポートの経路から呼ばれるため）。
- 保存の XML と読み込み後の状態は、変えない（ゴールデン: `GoldenPersistenceTests`／`ProfileGoldenGapTests`）。実行時の処理の順序・ログも変えない。

## 2. 手順（領域ごとに繰り返す）

1. 領域の持ち主 `XxxSlotSettings` を、値そのもの（プロパティ＋既定値）を持つ型にする。実行中のスロットへの反映が要る変更は、反映先を渡す `Set…` のメソッドにする（`IEnvironmentService` はコンストラクターで受ける）。
2. `BackingStore` の該当する配列の宣言を消し、領域の配列（`xxxSlots`）を作る。`BackingStore` 内・永続化（`Core/Persistence`）・テストの `store.name[i]` を、機械的に `store.xxxSlots[i].Prop` へ置換する（`out` 引数は一時変数へ）。
3. `IProfileSettingsService`／`ProfileSettingsService` から、該当する配列と `Get…` を削除し、利用者を `ForSlot(i).Xxx.Prop` へ置換する。
4. ゴールデンの状態ファイルは、名前だけが変わる。旧名へ逆変換して行の集合が一致することを確かめてから、実際の出力で更新する（値は変えない）。
5. `BackingStoreStateFiller`（テストの補助）は、読み取り専用の配列のフィールドの要素も変える。
6. 全テスト（Benchmark を除く）が通ること。コミット（領域ごと）。

## 3. 領域と進捗

| 領域 | 持ち主 | 状態 |
|---|---|---|
| ジャイロ（＋ SA のトリガー条件） | `GyroSlotSettings` | 完了（2026-11-10。パイロット） |
| タッチパッド | `TouchpadSlotSettings` | 完了（2026-11-10） |
| トリガー（L2/R2 のカーブのモードを含む） | `TriggerSlotSettings` | 完了（2026-11-10） |
| スティック（LS/RS のカーブのモードを含む） | `StickSlotSettings` | 完了（2026-11-11） |
| ライトバー | `LightbarSlotSettings` | 完了（2026-11-11） |
| 出力 | `OutputSlotSettings` | 未着手 |
| 一般 | `GeneralSlotSettings` | 未着手 |
| 6 軸 | `SixAxisSlotSettings` | 完了（2026-11-11。SX/SZ のカーブのモードを含む） |
| ボタン割り当て（`ds4controlSettings` など） | — | 領域外。最後に判断 |

## 4. 決定

- 領域ごとの配列（`gyroSlots[i]`）を先に作り、全領域が移ったあとに、1 つの `ProfileSlotSettings[]` に統合する（途中の状態でも、常にビルド・テストが通るようにするため）。
- ViewModel は、自分の領域の持ち主だけを受け取る（`IProfileSettingsService` を受けない。`SettingsSlotWindowUsageTests`）。
- 実行時の反映（トグル、デッドゾーン）は、持ち主のメソッドに置く（旧: `BackingStore.SetGyro…` ＋ サービスの委譲）。

## 5. 進捗

- 2026-11-10: 計画書を作成。ジャイロ領域を完了（19 の配列 → `GyroSlotSettings`。`IProfileSettingsService` から 31 メンバーを削除）。全 3,380 テスト合格。
