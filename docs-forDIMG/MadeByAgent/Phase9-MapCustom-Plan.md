# Phase9 `MapCustom` の分割（旧 9-D の吸収先）計画書

作成日: 2026-10-22
状態: **提案（承認待ち。§7 の質問に答えてから実装する）**
上位: `Phase9-Responsibility-Split-Plan.md`（§12.3 の最優先の残り）

## 1. 目的と現状

`Mapping.cs`（1,686 行）に残る最大の塊は、毎フレーム（毎秒 250〜1000 回）呼ばれる高頻度の経路にある次の 3 つ。完了条件（§6: 巨大メソッドが 200 行以下）を満たすには、`MapCustom` が 473 行で未達。

| メソッド | 行数 | 中身 |
|---|---|---|
| `MapCustom` | 473（うち**コメントアウトされた旧コードが 154 行**） | 1 フレームの入力 → 出力の流れの司令塔。左右スティック、L2／R2（2 段階トリガー）、通常ボタン、Extras、新経路のフレーム出力、マクロの重ね合わせ、スティックの後処理、マウス移動の確定 |
| `ProcessTwoStageTrigger` | 195 | 2 段階トリガー（Normal／ExclusiveButtons／HairTrigger／HipFire／HipFireExclusiveButtons）の状態機械 |
| `Commit` | 174 | 合成入力（マウスのクリック・ホイール）の確定と、ホイールの繰り返し。実際の出力（`IVirtualKBM`）はここ |

## 2. 調べて分かったこと

1. **`MapCustom` の L2 のブロックと R2 のブロックは、ほぼ同一のコードの複製**（各約 70 行。違いは、L2／R2 の定数［`DS4Controls.L2`／`R2`、`L2FullPull`／`R2FullPull`］、`cState.L2`／`R2` とその `Raw`、設定の配列、状態の配列だけ）。左右のスティック（LS／RS）のブロックも似ている（RS にだけ、デルタ加速の処理がある）。
2. **コメントアウトされた旧コードが 154 行**（ジャイロ→マウススティック、タッチ→マウススティックの旧実装。`TempMouseJoystick` と `GyroMouseStickBlender`［9-B］に置き換わった後の残骸）。コメントには設計の意図のメモがなく、旧コードそのもの。
3. **`ProcessTwoStageTrigger` は純粋に近い**: 入力は引数、状態は `TwoStageTriggerMappingData`（`StickMotionState` が保持済み）、依存は `profileSettings.DebouncingMs[device]` と `DateTime.Now`（HipFire の待ち時間）だけ。切り出しやすい。
4. **`Commit` には既存の特性化テストがある**（`Phase7Step7CommitCharacterizationTests`。`RecordingVirtualKbm` で出力を記録）。切り出しの前に固定済み。
5. **`Commit` の状態 3 つ（`oldnow`、`wheel`、`pressagain`）は、全デバイスで共有**（デバイスごとではない）。マウスホイールの繰り返しの基準時刻とホイールの方向が、複数のコントローラーの間で混ざる（9-B・9-C でユーザーが「デバイスごと」に直した問題と同じ種類）。
6. **`pressagain` は、どこにも `true` が代入されない**（宣言と `!pressagain` の判定だけ。テストの `AnalogMouseTestHarness` が `false` を代入するのみ）。常に `false` の、上流 DS4Windows 由来の残骸（規約 §2.4: 呼出元 0 件のコードは、経緯を調べて判断する）。
7. **2 段階トリガーの動作を固定する既存テストがない**（`TwoStage` を含むテストは、保存形式の golden のみ）。`MapCustom` を触る前に、値の特性化テストが必要。
8. **`MapCustom` は、具象の `ControlService ctrl` を引数に受ける**（`ctrl.ProfileActionProvider`、`ctrl.DS4Controllers`、`ctrl.ProfileSettingsService`）。第 2 層から上位の具象への依存。今回の範囲外（§6 の後続）だが、分割の過程で見えるようにしておく。

## 3. 切り出しの案（Wave）

### M1: `Commit` → `ISyntheticInputCommitter`（約 170 行減）
- 合成入力の確定（クリックの集計、押下・離上の出力、ホイールの出力と繰り返し）を、`SyntheticInputCommitter`（DI の Singleton）へ移す。`Mapping.Commit` は薄い委譲として残す（呼び出し側 2 件と多数のテストは変えない）。
- 状態（`oldnow`、`wheel`）は committer が持つ。`Mapping.oldnow`（テストの `AnalogMouseTestHarness` が代入）は、窓口を保つか、ハーネスを更新する。
- 検証: 既存の `Phase7Step7CommitCharacterizationTests` に加え、旧 `Commit` の逐語コピーとの差分テスト（`RecordingVirtualKbm`）。

### M2: `ProcessTwoStageTrigger` → `ITwoStageTriggerProcessor`（約 195 行減）
- 状態機械を `TwoStageTriggerProcessor`（状態なしの Singleton。状態は `TwoStageTriggerMappingData`）へ移す。
- 検証: 5 つのモード × 入力列の差分テスト（旧コードの逐語コピーとの比較）。HipFire の時間は、`TwoStageTriggerMappingData.checkTime` を設定して決定的にする。

### M3: `MapCustom` の整理（約 270〜320 行減。`MapCustom` は約 150〜200 行に）
- **M3a**: コメントアウトされた旧コード 154 行の削除（Q2）。
- **M3b**: L2／R2 の複製を 1 つの private メソッド（`ProcessTriggerWithTwoStage`）にまとめる（約 70 行減。引数で L2／R2 の違いを渡す）。挙動は同じ。
- **M3c**: LS／RS の複製を 1 つの private メソッドにまとめる（約 50 行減。RS のデルタ加速は引数で有効化）。
- 検証: **M3 の前に**、`MappingCharacterizationScope` を使った `MapCustom` の値の特性化テストを足す（2 段階トリガーの全モード、フリックスティック、デルタ加速）。M3 の前後で出力が一致することを確かめる。ホットパスの割り当て 0（`MappingHotPathAllocationTests`）と処理時間（`Phase9HotPathTimingBenchmark`）も確認する。

### M4（任意・今回は見送りを推奨）: `ControlService ctrl` への依存の解消
- `MapCustom`／`SampleMouseMovement`／`SampleFixedMouseMovement` が使う `ctrl` を、`IDeviceStateAccessor` と `IProfileSettingsService` に置き換える。テストが `ControlService` を構築しなくても `MapCustom` を呼べるようになる。範囲が `MapCustom` の呼び出し元（`ControlService.On_Report` など）に及ぶため、別の計画書にする。

## 4. 見込み

| 段階 | `Mapping.cs` |
|---|---|
| 現在 | 1,686 行 |
| M1 | 約 1,520 行 |
| M2 | 約 1,330 行 |
| M3 | 約 1,030 行（`MapCustom` 約 150〜200 行） |

## 5. 守ること

- 毎フレームの割り当て 0。新しい `new`、ラムダ、LINQ を高頻度の経路に入れない。
- 呼び出しの順序とログを変えない。`MapCustom` の中の順序（左スティック → 右スティック → L2 → R2 → 通常ボタン → Extras → フレーム出力 → マクロ → スティックの後処理 → マウスの確定）を、ソース走査のテストで固定する。
- 薄い委譲（`Mapping.Commit` など）を残す。
- 1 ファイル 1 型、ラチェットの基準値の引き継ぎ（規約 §3.3）。

## 6. 実機確認が要るもの

- マウスのクリックとホイール（Controls／SA で割り当てた）。ホイールの押しっぱなしの繰り返し。
- 2 段階トリガー（L2／R2 のソフトプル／フルプル、各モード）。DualSense／DS4 の実機。
- フリックスティック、デルタ加速。

## 7. 決めてほしいこと

- **Q1（着手順と範囲）**: M1 → M2 → M3 の順で進める（推奨）／M3 から（複製の整理を先に）／M1・M2 だけ（`MapCustom` の整理は見送り）。
- **Q2（コメントアウトされた旧コードの削除）**: 154 行を削除する（推奨。経緯はコミットと計画書にある）／規約 §3.3 原則 4 に従い、理由つきで残す。
- **Q3（`Commit` の状態の分離・調査項目 5）**: ホイールの繰り返しの基準時刻と方向を**デバイスごと**にする（挙動の変更。9-B・9-C と同じ扱い）／全デバイス共有のまま（現状維持）。
- **Q4（`pressagain`・調査項目 6）**: 常に `false` の変数と、その判定を削除する（推奨。上流由来の残骸）／残す。
- **Q5（進め方）**: 段階（M1／M2／M3）ごとに、ビルド・テスト・実機確認を挟む（推奨）。
