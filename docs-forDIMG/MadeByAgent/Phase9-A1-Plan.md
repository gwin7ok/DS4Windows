# Phase9-A1 計画書: 9-A(1) スティックとトリガーの補正の切り出し

作成日: 2026-10-22
状態: **(a)・(b)・(c) 完了（2026-10-22）。(d) は、段階ごとに設計を説明し、承認を得てから着手する**
上位: `Phase9-Responsibility-Split-Plan.md` 9-A（Q9: 2 Wave に分ける）、`Phase9-0-Plan.md`（再計測、性能の計測方法と基準値）。

## 1. 目的と範囲

`Mapping.SetCurveAndDeadzone`（1,193 行。左右のスティックにほぼ同じコードが 2 回ある）のうち、**スティックとトリガーの補正**を、責務ごとのクラスへ切り出す。挙動は変えない（入力 → 出力の値、ログ、割り当て 0）。ジャイロ → コントロール、フリックスティック、ステアリング較正は 9-A(2)。

## 2. 段階

| 段階 | 内容 | 守り | 状態 |
|---|---|---|---|
| (a) | `SetCurveAndDeadzone` の値の特性化テスト（コードは変えない） | — | **完了** |
| (b) | 左右のスティックのデッドゾーン（Radial・Axial）を、共通のクラスへ切り出す | (a)、割り当て 0、性能の計測 | **完了**（§4） |
| (c) | 出力カーブ（スティック、トリガー）と、スクエアスティック（左右）を切り出す | 同上 | **完了**（§5） |
| (d) | トリガーの補正（デッドゾーン、感度、2 段階）を切り出す。2 段階トリガー（`ProcessTwoStageTrigger`）の特性化は、この段階の前に足す | 同上 | 未着手 |

各段階の前に、設計を説明し、承認を得る。各段階の終わりで、`Debug ALL`（警告 0・エラー 0・全テスト合格）と、割り当て 0（`MappingHotPathAllocationTests`）、性能の計測（`Phase9HotPathTimingBenchmark`。悪化 5% 以内）を確認する。

## 3. (a) の結果

`DS4WindowsTests/Phase9A1SetCurveAndDeadzoneGoldenTests.cs`（ゴールデン方式。今のコードの出力をそのまま記録）。ゴールデンは `DS4WindowsTests/Golden/Phase7Step10c/` の `phase9a1-*.state.txt`。

| テスト | 内容 | ゴールデン |
|---|---|---|
| スティック | X・Y を 12 点ずつ（144 点）× 場面 28: デッドゾーン（Radial・Axial。デッドゾーン 0／10／25／40、アンチデッドゾーン 0／15／20、最大ゾーン 80／90／100、最大出力 70／85／100、最大出力の強制）、縦の倍率（150・60）、感度（1.5・0.6。Axial では無視される）、スクエアスティック（丸み 5・12）、出力カーブ（1〜5）、回転（30・−45 度）。Axial は、X と Y で軸ごとの値を変えて、入れ替わりを検出する | `phase9a1-sticks.state.txt`（約 4,000 行） |
| トリガー | 10 点 × 場面 14: デッドゾーン・アンチデッドゾーン・最大ゾーン・最大出力、感度、出力カーブ（1〜5）、組み合わせ | `phase9a1-triggers.state.txt` |
| 左右の差 | 左右に同じ設定・入力を与えたときの値の違い。**今は、すべての場面で差なし**（左右のコードは、同じ値を返す） | `phase9a1-left-right.state.txt` |
| ファズ | ファズ 2・6 で、決まった列の出力 | `phase9a1-fuzz.state.txt` |

対象外: アンチスナップバック（時刻に依存するため、時刻を差し替えられるようにしてから扱う。9-A(2) の前に検討）。出力カーブのモード 6（ユーザー定義の曲線）。ジャイロ → コントロール（9-A(2)）。

**意味**: 左右のコードに差がないことが確認できたので、(b) で、左右を 1 つのコードにしても、値が変わらないはず。値が 1 つでも変われば、このテストが失敗する。値を変える意図があるときは、旧実装と比較し、承認を得てから、ゴールデンを更新する。

## 4. (b) の結果（2026-10-22）

左右のスティックのデッドゾーン（Radial、Axial、Radial の外側の出力）を、共通の処理へ切り出した。`Debug ALL` 成功（警告 0・エラー 0、テスト 2249 件と 11 件）。**(a) のゴールデン 4 つは、変更前と一致**（値は変わっていない）。割り当て 0 も成功。

| 項目 | 内容 |
|---|---|
| 新規 | `DI/IStickDeadZoneProcessor.cs`（`Process(StickDeadZoneInfo mod, byte rawX, byte rawY, ref byte x, ref byte y, ref byte outer)`）、`DS4Control/Services/StickDeadZoneProcessor.cs`（左スティックのコードをそのまま移したもの。右は、名前と数値の書き方［`128f`／`128.0`］以外は同じだったため統合） |
| `Mapping.cs` | `SetCurveAndDeadzone` の 466 行（左右の Radial・Axial）を、2 回の呼び出しに置き換えた。`Mapping.cs`: 4,430 → 3,972 行 |
| DI | `IStickDeadZoneProcessor` を Singleton で登録。`Mapping` のコンストラクターに引数を追加（12 → 13）。`TestMappingFactory`、`Phase7Step8W2MappingInstanceGuardTests` を更新 |
| 性能 | `[SkipLocalsInit]`（ローカルの 0 初期化を省く。C# の確定代入で、読む前に必ず代入されるため安全）を付けた。付けないと、既定の場面で +7%、ジャイロの場面で +10% だった |

**性能の計測（Release。基準［変更前のコミット b7a0db7 を、別の `git worktree` に建てたもの］と、新を、交互に 3 回ずつ実行。場面ごとの中央値の最小同士）**

| 場面 | 基準（ns／回） | 新（ns／回） | 差 |
|---|---|---|---|
| 既定 | 326.4 | 324.7 | −0.5% |
| 出力カーブあり | 432.7 | 434.9 | +0.5% |
| トリガーの補正 | 398.8 | 359.8 | −9.8%（揺れ。基準側の実行に外れ値あり） |
| ジャイロ → コントロール | 344.5 | 356.0 | +3.3% |

許容（5% 以内）に収まった。**計測の注意**: この機材は、時間帯や直前の負荷で、同じコードでも 10〜30% 揺れる（基準を単独で 3 回測ると 327〜360）。そのため、前後は**同じ条件で交互に実行し、最小同士で比べる**（基準は、変更前のコミットを `git worktree add ../ds4w-baseline <コミット>` で別に建て、ベンチマークのファイルをコピーしてビルドする。終わったら `git worktree remove`）。`Phase9-0-Plan.md` §3 の判定の規則に、この方法を足した。

## 5. (c) の結果（2026-10-22）

設計: スクエアスティック（ユーザー指示で範囲に含めた）と、スティック・トリガーの出力カーブを、状態のない処理として切り出した。LS と RS、L2 と R2 のコードは、名前以外は同じだった（差は、設定の取得先［`GetLsOutCurveMode`／`GetRsOutCurveMode` など］だけ）。

| 区分 | 内容 |
|---|---|
| 新規 | `DI/IOutputCurveProcessor.cs`（`ProcessSquareStick(roundness, ref x, ref y)`、`ProcessStickCurve(mode, deadzoneType, bezier, ref x, ref y)`、`ProcessTriggerCurve(mode, bezier, ref value)`）、`DS4Control/Services/OutputCurveProcessor.cs`（`[SkipLocalsInit]`。LS／L2 のコードを移し、左右を統合） |
| `Mapping.cs` | `SetCurveAndDeadzone` の約 430 行を、5 つの呼び出し（条件の判定は呼び出し側に残す）に置き換えた。旧 `DS4SquareStick`／`DS4Vector2`（スクエアスティックの作業用オブジェクト）と配列 `outSqrStk` を削除し、計算はローカル変数で行う（割り当て 0。結果は同じ）。`Mapping.cs`: 3,972 → 3,532 行 |
| DI | `IOutputCurveProcessor` を Singleton で登録。`Mapping` のコンストラクターの引数を追加（13 → 14）。`TestMappingFactory`、`Phase7Step8W2MappingInstanceGuardTests` を更新 |
| テスト | 置き換えの**前**に、ゴールデンへ追加（既存の記録は 1 行も変わらず、追加のみ）: Axial のデッドゾーンでの出力カーブ 1〜5、ベジェ曲線（モード 6）のスティック［Radial・Axial］とトリガー、スクエアスティックの片側のみ。置き換え後も、ゴールデンは 1 バイトも変わらなかった |

**性能の計測**（Release。基準＝変更前の HEAD［02072faf］を別の `git worktree` に建て、交互に 3 回ずつ。場面ごとの中央値の最小同士）:

| 場面 | 基準（ns／回） | 新（ns／回） | 差 |
|---|---|---|---|
| 既定 | 327.9 | 308.1 | −6.0% |
| 出力カーブあり | 430.6 | 448.4 | +4.1%（新の 3 回: 543／509／448。揺れが大きい） |
| トリガーの補正 | 350.4 | 332.2 | −5.2% |
| ジャイロ → コントロール | 341.6 | 322.1 | −5.7% |

許容（5% 以内）に収まった。`Debug ALL`: 警告 0・エラー 0・全テスト合格（2,249 件、StandaloneTests 11 件）。次は (d)（トリガーの補正。2 段階の特性化を先に足す）。
