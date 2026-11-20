# Phase9-0 計画書: 着手時の再計測と、準備（Phase9 の責務分割）

作成日: 2026-10-22
状態: **承認済み（2026-10-22）。Phase10 の完了後、Phase9 の最初の作業**
上位: `Phase9-Responsibility-Split-Plan.md`（Q1〜Q10 の決定済み）。前提: Phase10（割り当て編集画面の統合）は完了（`Phase10-Status.md`）。

## 1. 目的

Phase9（巨大クラスの責務分割）に、安全に着手するための準備を行う。コードは変えない。(1) 現在の規模の再計測、(2) テストの在庫と不足の確認、(3) 性能の計測方法と基準値、(4) 最初の Wave（9-A(1)）の骨子。

## 2. 再計測の結果（2026-10-22）

| 対象 | 行数 | 大きいメソッド（行数） |
|---|---|---|
| `Mapping.cs` | 4,430 | `SetCurveAndDeadzone` 1,193、`MapCustom` 504、`getMouseMapping` 317、`Mapping` のコンストラクター 277、`Scale360degreeGyroAxis` 252、`ProcessTwoStageTrigger` 195、`Commit` 175、`PlayMacroCodeValue` 163、`SAWheelEmulationCalibration` 123、`GetBoolMappingExternal` 106、`TempMouseJoystick` 76、`PlayMacroTask` 75、`HandleFlickStickAngle` 73 |
| `ControlService.cs` | 3,474 | `On_Report` 254、`EstablishOutFeedback` 227、`CreateOSCCallback` 177、`Start` 173、`PrepareConnectedInputControllerSettingEvents` 161、`PluginOutDev` 157、`HotPlug` 129、`Stop` 119 |
| `App.xaml.cs` | 1,189 | `Application_Startup` 400、`CheckOptions` 135、`CleanShutdown` 67 |
| `MainWindow.xaml.cs` | 2,269 | 9-F で再計測 |
| `ProfileSettingsViewModel.cs` | 3,518 | 9-G で判断 |
| `BackingStore.cs` | 1,525 | Phase9 の対象外 |
| `LegacyProfileXmlSerializer.cs` | 424 | Phase8 で分割済み。Phase9 の対象から外す |

`SetCurveAndDeadzone`（620〜1813 行）の構造: 回転・アンチスナップバック・ファズ → 左スティックのデッドゾーン（Radial 約 140 行、Axial 約 100 行）→ 右スティックのデッドゾーン（同じ形。Radial 約 140 行、Axial 約 100 行）→ L2／R2 のデッドゾーン → 感度（非推奨）→ スクエアスティック → 左右スティックの出力カーブ（各約 130 行）→ L2／R2 の出力カーブ → ジャイロ → コントロール。**左右のスティックで、ほぼ同じコードが 2 回**ある。

## 3. テストの在庫と、性能の計測

**既存のテスト**
- `MappingHotPathAllocationTests`: 毎フレームの割り当て 0（`SetCurveAndDeadzone`、ジャイロ、`ApplyStickCalibration`）。
- `Phase7bTriggerDeadZoneRoundingTests`（3 件）: トリガーのデッドゾーンの丸め。
- `MappingArgumentPassThroughTests`、`MappingSetCurveAndDeadzoneGlobalReferenceGuardTests`、`Phase7Step8W2MappingInstanceGuardTests`: 引数の受け渡しと、静的な参照の禁止。
- `Phase7Step7MapCustomCharacterizationTests`、`Phase7Step7CommitCharacterizationTests`: `MapCustom`・`Commit` の特性化。
- **不足**: `SetCurveAndDeadzone` の**スティックの値**（デッドゾーン［Radial・Axial］、アンチデッドゾーン、最大ゾーン、カーブ、ファズ、アンチスナップバック、回転、スクエアスティック）の入力 → 出力は、固定するテストがほぼない。9-A(1) の最初の作業は、これを足すこと。

**性能の手動計測**: `Phase9HotPathTimingBenchmark`（新規）。環境変数 `DS4W_RUN_BENCH=1` のときだけ計測し、通常のテスト実行では何もせず合格する（処理時間は実行環境で揺れるため、合否にしない）。Release で、`SetCurveAndDeadzone` を 50,000 回 × 7 ラウンド回し、中央値（ns／回）を出す。4 つの場面（既定、出力カーブあり、トリガーの補正、ジャイロ → コントロール）。

```powershell
$env:DS4W_RUN_BENCH='1'; dotnet test ./DS4WindowsTests/DS4Windows.Actions.Tests.csproj -c Release /p:platform=x64 --filter "FullyQualifiedName~Phase9HotPathTimingBenchmark" --logger "console;verbosity=detailed"
```

**基準値（Phase9 着手前。2026-10-22。この開発機の Release。2 回の実行）**

| 場面 | 1 回目（ns／回） | 2 回目（ns／回） |
|---|---|---|
| 既定 | 327.2 | 338.8 |
| 出力カーブあり（LS／RS／L2／R2） | 436.8 | 456.3 |
| トリガーの補正（L2／R2） | 341.9 | 346.5 |
| ジャイロ → コントロール | 329.0 | 329.7 |

**判定の規則**: 実行ごとの揺れが約 4% あるため、各 Wave の前後で、**3 回実行し、場面ごとの中央値の最小同士**を比べる。悪化が **5% 以内**（承認済みの初期値）なら合格。超えたときは、再計測して、原因を調べる。基準値は、機材やビルドが変わったら取り直す。割り当て 0 は、既存の `MappingHotPathAllocationTests` が担う。

## 4. 9-A(1)（スティックとトリガーの補正）の骨子

Q9 のとおり、9-A は 2 つの Wave に分ける: **9-A(1) スティック・トリガー補正**、**9-A(2) ジャイロ・フリックスティック・ステアリング較正**。9-A(1) の個別計画書は、着手前に作り、承認を得る。内訳（案）:

| 段階 | 内容 | 守り |
|---|---|---|
| (a) | `SetCurveAndDeadzone` の値の特性化テストを足す（コードは変えない）: デッドゾーン（Radial・Axial、左右）、アンチデッドゾーン、最大ゾーン、最大出力、ファズ、アンチスナップバック、回転、スクエアスティック、出力カーブ（スティック、トリガー）、トリガーの補正（2 段階を含む）の境界値。左右で同じ設定なら、同じ結果になること | 新規テスト |
| (b) | 左右のスティックのデッドゾーンを、共通のクラスへ切り出す（左右を 1 つのコードにする） | (a)、割り当て 0、性能の計測 |
| (c) | 出力カーブを切り出す | 同上 |
| (d) | トリガーの補正（2 段階を含む）を切り出す | 同上 |

注意: 左右のコードに、細かい差（引数の違い、符号、丸め）があるかもしれない。共通化の前に、(a) の特性化テストで、左右それぞれの値を固定する。差があれば、現行のとおりに保つ（不具合と思われるものは、旧実装と比較し、承認を得てから別途是正する）。

## 5. 決定事項（2026-10-22、ユーザー承認）

- 性能の許容は、5% 以内（手動計測。通常のテストとは別）。
- 最初の作業は、9-A(1) の (a)（値の特性化テストの追加。コードは変えない）。

## 6. 成果物

- 本書。`DS4WindowsTests/Phase9HotPathTimingBenchmark.cs`（手動計測）。
- 次: 9-A(1) の個別計画書は `Phase9-A1-Plan.md`。(a)（値の特性化テスト）は 2026-10-22 に完了。(b) 以降は、段階ごとに承認を得る。

## 7. テストの整理（2026-10-22、ユーザー判断）

テストが増えた（346 ファイル、約 47,900 行、1,816 メソッド［展開すると 2,245 件］。うち、ソースの文字列を読む検査が 120 ファイル・632 メソッド）ため、不要なテストの整理を検討した。**Phase9 を先に進め、整理は Phase9 の後にする**（ユーザー決定）。理由: 動作の特性化テストが Phase9 の安全網であり、構造が落ち着いてから、重複や不要な検査を点検するほうが安全なため。整理の進め方（案）は、(1) 点検の一覧を作る（コードは変えない）、(2) 区分ごとに承認、(3) 小さく削除し、そのつど `Debug ALL` を確認、(4) 結果を記録。Phase9 の間は、動作の特性化テスト・ゴールデン・割り当て 0・ラチェットの一致検査を、削らない。

## 8. 性能の比較の方法（2026-10-22 追記。9-A(1) (b) で得た知見）

この機材は、時間帯や直前の負荷で、同じコードでも 10〜30% 揺れる（基準を単独で 3 回測ると 327〜360 ns）。§3 の「前後を各 3 回」では、揺れで判定を誤る。**前後を、同じ条件で交互に実行する**: 変更前のコミットを `git worktree add ..\ds4w-baseline <コミット>` で別に建て、`Phase9HotPathTimingBenchmark.cs` をコピーして Release でビルドし、基準と新を交互に 3 回ずつ実行し、場面ごとの中央値の最小同士を比べる（5% 以内）。終わったら `git worktree remove --force ..\ds4w-baseline`。
