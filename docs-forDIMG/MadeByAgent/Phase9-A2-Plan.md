# Phase9-A2 計画書: 9-A(2) ジャイロ・スティックの前処理・フリックスティックの切り出し

作成日: 2026-10-22  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase9-Responsibility-Split-Plan.md` 9-A（Q9: 2 Wave に分ける）、`Phase9-A1-Plan.md`（9-A(1) は完了）、`Phase9-SteeringWheel-Removal-Plan.md`  
状態: **A・B・C 完了（2026-10-22）。9-A(2) 完了。次はステアリング＋vJoy の削除（Phase9-SteeringWheel-Removal-Plan.md）**

---

## 1. 目的と範囲

`Mapping.SetCurveAndDeadzone` の残り（ジャイロ → コントロール、スティックの前処理）と、`MapCustom` の流れにあるフリックスティックを、責務ごとのクラスへ切り出す。挙動は変えない（入力 → 出力の値、ログ、割り当て 0）。

**ステアリング（旧 D 区分）は、9-A(2) から外した**: SA ステアリング・ホイール・エミュレーションは、機能ごと削除する（ユーザー承認済み。`Phase9-SteeringWheel-Removal-Plan.md`）。実施は A→B→C の後。それまでステアリングのコードは変えない。

## 2. 段階

| 段階 | 内容 | 守り | 状態 |
|---|---|---|---|
| A | ジャイロ → コントロール（`GyroOutMode.Controls` の SX／SZ の補正）を切り出す | 値の特性化（先に追加）、割り当て 0、性能の計測 | **完了**（§3） |
| B | スティックの前処理（アンチスナップバック、ファズ、ドリフト補正［`ApplyStickCalibration`］、回転）を切り出す。アンチスナップバックは時刻（`DateTimeOffset.Now`）に依存するため、時計を差し替えられるようにしてから特性化する | 同上 | **完了**（§4） |
| C | フリックスティック（`ProcessFlickStick`／`HandleFlickStickAngle`／`WarpEaseOut`）を切り出す。`cState.elapsedTime` を引数で渡す形にし、角度の列を与えて変化量を固定する特性化を先に足す | 同上 | **完了**（§5） |

各段階の前に、設計を説明し、承認を得る。各段階の終わりで、`Debug ALL`（警告 0・エラー 0・全テスト合格）と、割り当て 0（`MappingHotPathAllocationTests`）、性能の計測（`Phase9HotPathTimingBenchmark`。基準を `git worktree` で別に建て、交互に 3 回ずつ。悪化 5% 以内）を確認する。

## 3. A の結果（2026-10-22）

**調査**: `SetCurveAndDeadzone` 末尾の `saControls` ブロック（約 160 行）。X（SX）と Z（SZ）で、デッドゾーン部分は名前以外が完全に同じ。出力カーブ部分は同じだが、適用する条件が違う（X: `sxOutCurveMode > 0`、Z: `szOutCurveMode > 0 && outputAccelZ != 0`）。挙動を変えないため、この条件は呼び出し側に残した。

| 区分 | 内容 |
|---|---|
| テスト（置き換えの前に追加） | `DS4WindowsTests/Phase9A2GyroControlsGoldenTests.cs`。ジャイロの X・Z の格子（15 × 15）と、設定の場面（デッドゾーン、最大ゾーン、アンチデッドゾーン、感度、出力カーブ 1〜6［ベジェを含む］、X と Z で設定が違う場面）。ゴールデン `Golden/Phase7Step10c/phase9a2-gyro-controls.state.txt`（約 5,400 行）と、X と Z の差の一覧 `phase9a2-gyro-controls-xz.state.txt`（同じ設定では、差なし） |
| 新規 | `DI/IGyroControlsProcessor.cs`（`Correct(accel, deadZone, maxZone, antiDead, sens)`、`ApplyCurve(mode, bezier, outputAccel)`）、`DS4Control/Services/GyroControlsProcessor.cs`（X のコードをそのまま移し、Z を統合。`[SkipLocalsInit]`） |
| `Mapping.cs` | `saControls` ブロックの約 150 行を、設定の読み取りと 4 つの呼び出しに置き換えた。`Mapping.cs`: 3,444 → 3,317 行 |
| DI | `IGyroControlsProcessor` を Singleton で登録。`Mapping` のコンストラクターの引数を追加（15 → 16）。`TestMappingFactory`、`Phase7Step8W2MappingInstanceGuardTests` を更新 |

置き換え後も、ゴールデンは 1 バイトも変わらなかった。`Debug ALL`: 警告 0・エラー 0・全テスト合格（2,251 件、StandaloneTests 11 件）。

**性能の計測**（Release。基準＝変更前の HEAD［3db35508］を別の `git worktree` に建て、交互に 3 回ずつ。場面ごとの中央値の最小同士。この回も機材の揺れが大きかった）:

| 場面 | 基準（ns／回） | 新（ns／回） | 差 |
|---|---|---|---|
| 既定 | 325.9 | 309.9 | −4.9% |
| 出力カーブあり | 474.0 | 463.2 | −2.3% |
| トリガーの補正 | 458.3 | 408.6 | −10.8% |
| ジャイロ → コントロール | 368.7 | 333.2 | −9.6% |

許容（5% 以内の悪化）に収まった（悪化した場面なし）。

## 4. B の結果（2026-10-22）

**調査**: 回転は、すでに `DS4State.rotateLSCoordinates／rotateRSCoordinates` のため触らない。対象は、アンチスナップバック（`CalcAntiSnapbackStick`。履歴キュー、`DateTimeOffset.Now`）、ファズ（`CalcStickAxisFuzz`。前回の値）、ドリフト補正（`ApplyStickCalibration`。public）。状態の配列 `stickValueHistory`／`lastStickAxisValues` は `Mapping` の public フィールドだったが、`Mapping` 内でしか使われていなかった。

| 区分 | 内容 |
|---|---|
| テスト（置き換えの前に追加） | (1) `Phase9A1SetCurveAndDeadzoneGoldenTests` にドリフト補正の場面を 2 つ追加（左右・X／Y で量と符号を変える。範囲外のクランプ）。ゴールデンは追加のみ。(2) `Phase9A2AntiSnapbackDifferentialTests`（7 件）: アンチスナップバックを、旧実装の逐語的な写し［時刻を引数にしたもの］と突き合わせる差分テスト。固定の列 2 つ、決まった種の疑似乱数の列 5 つ（3,000 フレーム）。出力の値と履歴の中身が、1 フレームごとに一致。ファズは、既存のゴールデンが守る |
| 新規 | `DI/IStickInputConditioner.cs`（`ApplyDrift`、`AntiSnapback`、`Fuzz`）、`DS4Control/Services/StickInputConditioner.cs`（現行のコードをそのまま移した。`out` 引数は `ref` に直した。アンチスナップバックの時刻は引数）、`DS4Control/DS4TimedStickAxisValue.cs`（`Mapping` の入れ子の構造体を独立させた。1 ファイル 1 型） |
| 状態の移動 | `stickValueHistory`／`lastStickAxisValues` を `StickMotionState`（`StickValueHistory`／`LastStickAxisValues`。添字は `TestProfileItemCount`）へ移した。`Mapping` は、他のフィールドと同じく、コンストラクターで参照を受け取る。状態が `Mapping` ごとに分かれるため、テストで状態が混ざらない |
| `Mapping.cs` | `CalcAntiSnapbackStick`／`CalcStickAxisFuzz`／`ApplyStickCalibration` と、状態の宣言を削除し、呼び出しに置き換えた。`Mapping.cs`: 3,317 → 3188 行 |
| DI | `IStickInputConditioner` を Singleton で登録。`Mapping` のコンストラクターの引数を追加（16 → 17）。`TestMappingFactory`、`Phase7Step8W2MappingInstanceGuardTests` を更新 |
| 既存テストの追従 | `ApplyStickCalibration` を直接呼ぶテスト（`MappingArgumentPassThroughTests`、`MappingHotPathAllocationTests`）は、`StickInputConditioner.ApplyDrift` を呼ぶ形に。ソースを読むガード（`MappingCommitAndCalibrationGlobalReferenceGuardTests`、`MappingSetCurveAndDeadzoneGlobalReferenceGuardTests`）は、移動先のファイル・終端の目印に追従 |

`Debug ALL`: 警告 0・エラー 0・全テスト合格（2,258 件、StandaloneTests 11 件）。置き換え後も、ゴールデンは 1 バイトも変わらなかった。

**技術的負債（記録）**: アンチスナップバックの `queue.Any(ラムダ)` は、毎フレーム、クロージャーを割り当てる（旧実装から同じ。有効なときだけ実行される）。挙動を変えないため、そのまま移した。必要なら、別途、割り当て 0 に直す（差分テストで守れる）。

**性能の計測**（Release。基準＝変更前の HEAD［e8ff348e］を別の `git worktree` に建て、交互に 3 回ずつ。場面ごとの中央値の最小同士。この回は機材が静かだった）:

| 場面 | 基準（ns／回） | 新（ns／回） | 差 |
|---|---|---|---|
| 既定 | 310.6 | 307.4 | −1.0% |
| 出力カーブあり | 460.8 | 453.2 | −1.6% |
| トリガーの補正 | 334.0 | 322.5 | −3.4% |
| ジャイロ → コントロール | 300.4 | 300.9 | +0.2% |

許容（5% 以内の悪化）に収まった。

## 5. C の結果（2026-10-22）

**調査**: `Mapping` の private メソッド `ProcessFlickStick`（約 40 行）、`HandleFlickStickAngle`（約 75 行）、`WarpEaseOut`。呼び出しは、`MapCustom` の流れのスティックモード `StickMode.FlickStick` の 2 箇所（左・右）。時刻（`DateTime`）は使わず、フレーム間隔は `cState.elapsedTime`。`ProcessFlickStick` の `device`・`ControlService ctrl` は未使用だった。状態 `FlickStickMappingData` は、すでに独立したクラスで、`StickMotionState` が持っている。

| 区分 | 内容 |
|---|---|
| テスト（置き換えの前に追加） | `DS4WindowsTests/Phase9A2FlickStickGoldenTests.cs`。現行の private メソッド `ProcessFlickStick` を、**リフレクションで呼んで**ゴールデンを作った。スティックの列 3 種（フリック、旋回 2 周＋逆回り、小さな動き）× 設定 6 種（しきい値、フリック時間、補正係数、最小角度、フレーム間隔）。1 フレームごとのマウス X の増分と持ち越しの端数を記録。ゴールデン `Golden/Phase7Step10c/phase9a2-flick-stick.state.txt`（約 2,700 行）。切り出し後は、呼び出し先だけを新しい処理に変えた（ゴールデンは 1 バイトも変わらない） |
| 新規 | `DI/IFlickStickProcessor.cs`（`Process(data, settings, elapsedTime, stickX, stickY, prevStickX, prevStickY, ref mouseDeltaX)`）、`DS4Control/Services/FlickStickProcessor.cs`（3 メソッドをそのまま移した。未使用の引数は外した） |
| `Mapping.cs` | 3 メソッド（約 120 行）を削除し、呼び出し 2 箇所を置き換えた。`Mapping.cs`: 3,188 → 3071 行 |
| DI | `IFlickStickProcessor` を Singleton で登録。`Mapping` のコンストラクターの引数を追加（17 → 18）。`TestMappingFactory`、`Phase7Step8W2MappingInstanceGuardTests` を更新 |

`Debug ALL`: 警告 0・エラー 0・全テスト合格（2,259 件、StandaloneTests 11 件）。

**性能の計測**（Release。基準＝変更前の HEAD［62079ab8］を別の `git worktree` に建て、交互に 3 回ずつ。場面ごとの中央値の最小同士。フリックはベンチマークの場面に入らないため、既存の場面で、ほかへの影響がないことを確認した）:

| 場面 | 基準（ns／回） | 新（ns／回） | 差 |
|---|---|---|---|
| 既定 | 311.2 | 304.0 | −2.3% |
| 出力カーブあり | 448.1 | 444.0 | −0.9% |
| トリガーの補正 | 355.8 | 335.5 | −5.7% |
| ジャイロ → コントロール | 321.9 | 324.9 | +0.9% |

許容（5% 以内の悪化）に収まった。

## 6. 9-A(2) の総括

`Mapping.cs` は 3,444 → 3071 行（9-A(2) で約 370 行減）。9-A 全体では 4,430 → 3071 行（約 1,360 行減）。新しい処理は 6 つ（`IStickDeadZoneProcessor`、`IOutputCurveProcessor`、`ITriggerDeadZoneProcessor`、`IGyroControlsProcessor`、`IStickInputConditioner`、`IFlickStickProcessor`）。モデル図 `Model-Diagram/03-Class-Interface-Diagram.md` に反映した。`SetCurveAndDeadzone` は、回転、前処理の呼び出し、各補正の呼び出しを順に行う司令塔になった。

**残り**: ステアリング（旧 D 区分）は、機能ごと削除する（`Phase9-SteeringWheel-Removal-Plan.md`。承認済み）。2 段階トリガー（`ProcessTwoStageTrigger`）は、`MapCustom` の責務分割の Wave で扱う（時刻の差し替えが要る）。
