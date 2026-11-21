# PhaseRV13 計画書: ジャイロ・タッチパッドの状態の絡みを解く

作成日: 2026-11-21
状態: **D1＝(C) タッチパッドと StickMotionState まで、D2＝(B) Y の誤記として直す、D3＝(B) 共通インターフェースも足す、D4＝(B) まとめて 1 コミット、D5＝(A) 実機確認は最後に、で決定（2026-11-21）。実施済み（RV13-7 を除く）**
出典: `PhaseRV13-Analysis-StateEntanglement.md`（読んで分かった絡みの実態と、目指す構造の案）、`PhaseRV12-Plan.md`（見送った `TouchpadMouse`・`GyroOutputProcessor`）。
動き: **保存の形・ログは変えない**。判定の出力の渡し方、入力の渡し方、モードごとの状態の持ち主を直す。**例外（D2＝(B)）: `GyroStickX` の二重代入を、Y の初期化へ直す（MouseJoystick の枝で Y が 128 に戻る。挙動の変更。RV13-0 で現状を固定し、RV13-1 で直して、差を特性化テストに明記する）。**

## 目的
1. `GyroOutputProcessor` の「隠れた出力」（`IsTriggerActive` が書くフィールドを、呼び出し側が読む）と、入力 `s` の差し替えをなくす。
2. ジャイロの 3 つの出力モード（Controls／Mouse／MouseJoystick）を、モードごとの型へ分け、状態の混在をなくす。
3. `TouchpadMouse` の公開の可変フィールド約 30 個と、前回値の手更新を、型で守る形にする。
4. 型の単位で 500 行以上の 2 件（`TouchpadMouse` 740、`GyroOutputProcessor` 573）を 500 行未満にする。

## 前提（調べた事実、2026-11-21）
- 既存のテスト: `GyroOutputProcessorTests`・`TouchpadMouseGyroTriggerTests`・`TapClickDetectorTests`・`TouchButtonDetectorTests`・`TouchMouseTrackballTests`・`TouchStickOutputTests`・`MouseTapClickHoldTests`・`MouseReleaseButtonHoldTests` など。トリガー判定の入口は押さえてあるが、ジャイロ 3 モードの出力（フィルター、トグル、逆ラチェット）と、タッチパッドのスワイプ・スライドの前回値の遷移は薄い。
- `TouchpadMouse` を継承するクラスは本体にもテストにもない（`virtual` は、継承で差し替える用途が実在しない）。
- 不審な 1 行: `GyroOutputProcessor.cs:182` `_stickMotion.GyroStickX[deviceNum] = _stickMotion.GyroStickX[deviceNum] = 128;`（Y が初期化されない書き方）。

## 決定してほしい事項

### D1 範囲
- **(A) ジャイロ（`GyroOutputProcessor`）だけ（推奨）**。判定の値渡し、入力の引数渡し、3 モードの分離。タッチパッドは次の Phase。小さく始め、効果を確かめる。
- (B) ジャイロ＋タッチパッド（`Edge<T>`、公開フィールドの窓口化、持ち物の生成を外へ）。公開フィールドの読み手（`Mapping`・UI）が広く、差分が大きい。
- (C) B＋`StickMotionState` をデバイスごとのオブジェクトへ。`Mapping`・出力側まで影響し、最も大きい。

### D2 不審な 1 行（`GyroStickX` の二重代入）
- **(A) 先に特性化テストで現在の挙動を固定し、直すかを別に判断する（推奨）**。この Phase では挙動を変えず、`PhaseRV13-Analysis` の「決めてほしいこと」に残す。
- (B) Y の初期化の誤記とみなして直す。挙動が変わる（MouseJoystick の枝で、Y が前フレームの値のままになる動きが変わる）。実機確認が要る。
- (C) 現状維持で、コメントに「意図的」と書く。意図が確認できないまま書くのは避けたい。

### D3 モードごとの型の形
- **(A) 戦略の型 3 つ（`GyroControlsOutput`・`GyroMouseOutput`・`GyroMouseStickOutput`）を、`GyroOutputProcessor` が持つ（推奨）**。契約（インターフェース）は作らず、同じ層の `internal` の具象（規約 §3.1）。司令塔は、モードを選んで呼ぶだけ。
- (B) 3 つの型に、共通のインターフェース（`IGyroModeOutput`）を足す。差し替えはしやすいが、型が増える。

### D4 コミットの単位
- **(A) Step ごとに 1 コミット。各コミットでビルド警告 0・全テスト（推奨）**。ジャイロは入力の遅延や挙動に影響が出やすく、戻しやすさを取る。
- (B) まとめて 1 コミット。

### D5 実機確認
- **(A) 最後にまとめて、`Device-Check-Checklist.md` に追記する（推奨）**。ジャイロ（Controls／Mouse／MouseJoystick のトリガー、トグル、逆ラチェット、感度）。実機の確認はユーザーが後回しにしている。
- (B) ジャイロの Step が終わるたびに、1 項目ずつ確認する。

## Step（D1＝(A)、D2＝(A)、D3＝(A)、D4＝(A)、D5＝(A) の場合）
| Step | 内容 | 検査 |
|---|---|---|
| RV13-0 | 特性化テストを足す: 3 モードのトリガー判定（AND／OR、トグル、逆ラチェット、空のトリガー）、Controls の `outputGyroControls`、MouseJoystick の初期化（`GyroStickX`／`GyroStickY` の現状の値）。 | Debug ALL（挙動の固定のみ） |
| RV13-1 | `IsTriggerActive` を値を返す形へ（`GyroTriggerResult`）。トリガーの解析・トグルの記憶を `GyroTriggerEvaluator` へ。隠れた出力のフィールドを廃止。 | 同上＋RV13-0 のテスト |
| RV13-2 | 入力 `s` を引数渡しへ。公開の `State` セッターを廃止（呼び出し元を調べて直す）。機器の具象（`InputDeviceCore`）への依存を、狭い契約（現在の状態を返す口）へ。 | 同上 |
| RV13-3 | 3 モードを別の型へ。フィルター（`OneEuroFilterPair`）は MouseJoystick の型へ移す。`GyroOutputProcessor` は司令塔にする。 | 同上＋型の単位で 500 行未満を確認 |
| RV13-4 | `TouchpadMouse` の `virtual` の廃止（継承なしの確認済み）。ガードテストを追加。 | 同上 |
| RV13-5 | 評価の再算定（束ねた計測。`GyroOutputProcessor` の 500 行以上の解消を確認）。`Device-Check-Checklist.md` へ追記。 | `Score-CodeReview.py` |

D1＝(B)・(C) を選んだ場合は、上の Step の後に、`PhaseRV13-Analysis` の B-1・B-2・C を Step として足す（そのときに個別に計画を切る）。

## 守るもの
機能（ジャイロの 3 モード、トグル、逆ラチェット、感度、フィルター）、ログ、`ITouchPadGyroToggles` などの公開の契約、各ラチェット、1 ファイル＝1 型、コンストラクターの引数 12 以下。

## リスク
- ジャイロは入力のスレッドで毎フレーム動くため、割り当て（`new`）を増やさない（`SlotStateHotPathAllocationTests`）。判定の結果は `struct`（`readonly struct`）で返す。
- フィルターの状態（`OneEuroFilterPair`）を移すと、プロファイル切替時の `ReplaceOneEuroFilterPair`・`SetupLateOneEuroFilters` の呼び出し経路が変わる。RV13-0 で呼び出し元を洗い出す。

## 変更履歴
- 2026-11-21: 作成（D1〜D5 の決定待ち）。

## 追加の Step（D1＝(C) による。D3＝(B) のインターフェースは `IGyroModeOutput`）
| Step | 内容 | 検査 |
|---|---|---|
| RV13-6 | `Edge<T>`（現在・前回の組）と `TouchpadGestureState`（スワイプ・スライドの読み取りの窓口）。公開の可変フィールド約 30 個を窓口へ。読み手（`Mapping`・UI・テスト）を直す。 | Debug ALL＋RV13-0 に足すタッチパッドの特性化テスト |
| RV13-7 | `TouchpadMouse` の持ち物（`MouseCursor`・`MouseWheel`・`GyroOutputProcessor`・`TouchStickOutput`）の生成を、コンストラクター注入／ファクトリーへ。引数は 12 以下（束ねるだけの箱にしない）。 | 同上＋`ConstructorArityRuleTests` |
| RV13-8 | `StickMotionState` を、デバイスごとのオブジェクト（`DeviceStickMotion`）の配列へ。`Mapping`（`mapStickActionData` など）・ジャイロ・タッチパッド・出力側の読み手を直す。 | 同上＋`SlotStateHotPathAllocationTests` |

実施順は RV13-0 → 1 → 2 → 3 → 4 → 6 → 7 → 8 → 5（再採点は最後）。D4＝(B) により、全 Step を 1 コミットにまとめる（途中の確認は、Step ごとに Debug ALL。失敗時は、直前の Step の状態へ戻して原因を直す）。

## 実機確認（D5＝(A)。最後にまとめて）
`Device-Check-Checklist.md` へ追記する項目: ジャイロ（Controls・Mouse・MouseJoystick のトリガー、トグル、逆ラチェット、感度）、**MouseJoystick の Y の初期化（D2＝(B) の挙動変更）**、タッチパッド（スワイプ、スライド、タップ、クリック領域、トラックボール、タッチスティック）。

## 変更履歴への追記
- 2026-11-21: D1＝(C)、D2＝(B)、D3＝(B)、D4＝(B)、D5＝(A) を、ユーザーが選択。D1・D2・D3・D4 はいずれも推奨と異なる。D1＝(C) は変更が最も大きく、`StickMotionState` の読み手が `Mapping`・出力側にも及ぶ。D4＝(B) は、途中で壊れたときに戻しにくいため、Step ごとの Debug ALL を必須とする。
- 2026-11-21: 実施（`PhaseRV13` ブランチ）。RV13-0（特性化テスト 5 件）、RV13-1（`GyroTriggerEvaluator`・`GyroTriggerResult`。`GyroStickX` の二重代入を `GyroStickY` へ直した＝D2 の挙動の変更）、RV13-2（`s` と公開の `State` を廃止。判定は状態を引数で受ける）、RV13-3（`IGyroModeOutput` と 3 モードの型。`GyroOutputProcessor` は 135 行）、RV13-4（`virtual`・`protected` と、死んだフィールド `pushed`・`clicked`・`secondTouch` を削除）、RV13-6（`TouchpadGestureState`。公開フィールド約 30 個を窓口へ）、RV13-8（`DeviceStickMotion`。`StickMotionState` の 7 本の並列配列を、デバイスごとの型の配列へ）。全 3,668 テスト合格・警告 0。**未実施: RV13-7**（`TouchpadMouse` の持ち物の生成を外へ。反射で private フィールドを検査するテスト約 10 箇所の書き換えが大きく、挙動の改善がないため）。D4＝(B)（まとめて 1 コミット）で、途中は Step ごとに Debug ALL。実機確認は `Device-Check-Checklist.md` §9k（先頭が Y の初期化の挙動の変更）。
- 2026-11-21: RV13-7 を実施（`PhaseRV13b` ブランチ）。`TouchpadMouse` のコンストラクターは持ち物（タップ・カーソル・ホイール・ジャイロ・タッチスティック）を受け取る 11 引数になり、生成は `TouchpadMouseFactory` へ移した。`MouseWheel` は public にした。テストの生成箇所はファクトリー経由へ。全 3,668 テスト合格・警告 0。
