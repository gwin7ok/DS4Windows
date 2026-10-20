# Phase8-Step8 計画書: 旧ディスパッチと系統 B の削除

作成日: 2026-10-20  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Unified-Trigger-Dispatch-Plan.md`（§5 の Step8、D3＝C1、D5）、`Phase8-Step7-Plan.md`（Q4＝A: 旧コードは Step8 まで残す）、`Phase8-Step2b-ControlsTab-Design.md`、`Phase8-Step7-Completion-Report.md`  
状態: **計画書の作成（2026-10-20）。質問 Q1〜Q8 の承認待ち。着手前に W0（監査）を行う。**

---

## 1. 目的と位置づけ

- Step7（7.1〜7.8f）で、特殊アクション（SA）のすべての種別と Controls タブの割り当て（Key／Macro／Extras／Button のパッド・マウス・ホイール・移動・Toggle）を、新経路（`TriggerDispatchEngine`）へ切り替えた。旧経路のコードは、ロールバックを `ServiceRegistration` の 1 行で済ませるために、Step8 まで残してある。
- Step8 では、新経路が処理するようになった旧コードと、役目を終えた**系統 B**（`Actions.Action` 系の `IActionFactory`、各 Adapter など）、および切替のスイッチ（`BindingMigrationOptions`）を削除する。`Mapping.cs`（現在 7,401 行）から、約 2,000 行を減らす見込み（Phase8 全体計画 §26 の見積もり）。
- **Step8 の後は、ロールバックが「1 行を外す」ではなく「PR を git で戻す」になる。** そのため、削除を小さな PR に分け、各 PR でテスト（旧・新の比べで固定した期待値）を通す。実機確認（Step9。U-35〜U-47）を削除の前に行うかは Q1。
- やらないこと: `Mapping` の責務分割（Phase9）、挙動・ログ・保存形式の変更（規約 2.2／2.3。削除する診断ログは Q7 で承認を得る）、ジャイロスワイプ以外の新機能。

## 2. 削除の対象（調査の見立て。W0 で確定する）

行番号は 2026-10-20 時点の `Mapping.cs`。

### 2.1 `Mapping.cs` の旧経路

| 区分 | 場所（概算） | 内容 |
|---|---|---|
| A. SA の Stage3（Button の同期処理）と出力の組み合わせ | 2,650〜2,840 行付近（`lastButtonTriggerState` ほか） | 旧 Button の毎フレームの出力。新経路は `ApplyFrameOutputs`（`_engine.Owns` で素通しにしている） |
| B. `customMapQueue` とその処理 | 2,629〜2,650 行、3,183 行、`ControlToXInput` | 旧パッド出力のキュー（7.8f-1 で新経路は `ControlPadOutputAction`） |
| C. `ProcessControlSettingAction` の Controls タブの旧枝 | 3,738〜4,090 行 | Key／Macro／Button（パッド・マウス・ホイール・移動・Toggle）／Default の素通し（`_engine.OwnsControl` で素通し） |
| D. `MapCustomAction` の SA の種別ごとの旧枝 | 4,166〜5,250 行（約 1,000 行） | Macro、Button、Key、Program、Profile、DisconnectBT、BatteryCheck、ステアリング較正、GyroCalibrate、MultiAction と、押し続けの解除・uTrigger の処理 |
| E. 判定の旧関数 | `GetBoolMapping`、`getBoolSpecialActionMapping`、`GetBoolActionMapping`、`GetXYAxisMapping`、`GetByteMapping`、`GetMouseWheelMapping`、`getBool*Mapping`（静的）ほか | 新経路は `TriggerInput` に移植済み。**`getMouseMapping` は、新経路が借りている（`MouseSampler`）ため残す**（Q5） |
| F. 旧経路だけの状態 | `lastButtonTriggerState`、`stickWheelRemainder`／`stickWheelDownDir`／`stickWheel`、`pressagain`、`_syntheticActionCache`（`GetOrCreateSyntheticKeyAction`）、`FindSpecialActionForLogicalKey`、`DispatchOrSetBeingTriggered`、`GetBeingTriggered`／`SetBeingTriggered` ほか | `oldnow`は`Commit`のデジタルのホイールの繰り返しが使うため残す。他は呼び出し元が消えたら削除（W0 で確定） |
| G. 旧経路の診断ログ | `Mapping SUMMARY (5s)`、`SpecialAction BUTTON/KEY press delegated…` ほか | 旧機構の説明用。削除は Q7 で承認を得る |

### 2.2 系統 B と切替のスイッチ

| 区分 | 内容 |
|---|---|
| H. 系統 B | `IActionFactory`／`ActionFactory`／`DefaultActionFactory`、`*Adapter`（`KeyActionAdapter`、`MacroActionAdapter`、`ProfileSwitchActionAdapter`、`LaunchProcessActionAdapter`、`KeyButtonActionControllerAdapter`）、`IActionController`／`KeyButtonActionController` 系ほか。**`KeyAction` は `KeyOutputAction` が委譲しているため残る**（D1 の注記）。W0 で、各型の呼び出し元を確かめて確定する |
| I. 切替のスイッチ | `BindingMigrationOptions`（`MigratedTypes`／`MigratedControlKinds`）、`ControlBindingKind` の「有効な区分」の判定、エンジンの `_options.IsMigrated(...)`。全種別が移行済みなので、常に有効にする（Q3） |
| J. テスト | 旧・新の両経路の Theory（`InlineData(false)`: 15 ファイル、`migrated` を取るテスト: 36 ファイル）、旧関数を reflection で呼ぶ比べ（`TriggerInputTests`、`Phase8Step78bW2ControlsKeyTests`、`Phase8Step78fW2ControlsPadTests`）、`MigratedTypes` を触るテスト（28 ファイル） |

### 2.3 新経路に入っていない割り当て（旧経路が処理している残り）

Step8 で旧経路を消すと、処理する者がいなくなるもの。**W0 で全件を洗い出し、Q2 で扱いを決める。**

- ジャイロスワイプの割り当て（`GyroSwipe*`。7.8b から対象外。Q7 で「入れられるものは入れる」と承認済みだが、入力の作り方を確かめていない）
- Extras の値が読めない割り当て（9 個未満など。旧経路でも途中までしか動かない）
- SA の「詳細が読めない」もの（旧経路でも何も出力しないことを 7.1 などで固定済み）
- 本番では起きないが、テスト用の構築（ポートなし）で束縛が作られない場合

## 3. 方針

1. **削除の前に、旧関数の期待値を固定する（W1）。** 旧関数と新経路を reflection で比べているテスト（アナログ値、判定、`IsActive` など）は、旧関数を消すと成り立たない。旧関数が返す値を、全コントロール・乱数の入力で表にして**テストデータとして保存**し、削除後は表と比べる。
2. **削除は小さな PR に分ける（Q1）。** 依存の少ない方から:
   - **8a**: C（Controls タブの旧枝）、B（`customMapQueue`）、F のうち Controls 専用のもの、判定の旧関数のうち Controls 専用のもの。
   - **8b**: A（Stage3）と D（SA の種別ごとの旧枝）、F の残り、E の残り、G。
   - **8c**: 系統 B（H）。
   - **8d**: スイッチの削除（I）、テストの整理（J）、モデル図・文書の更新。
3. 各 PR で、(a) 旧・新の比べで固定した期待値のテスト、(b) 割り当て・ロールバックの代わりの「削除後も同じ出力」の検査、(c) ラチェット（`Global.*` ほか）の一致検査、(d) `Mapping.cs` の行数の減少の確認、を通す。ビルド・テストはユーザーが実行する。
4. 規約: 1 ファイル＝1 型、削除したクラスのテストも削除、コメント（設計意図・TODO）を誤って消さない。TODO（Step8 で削除と書いたもの）は、削除と同時に消す。

## 4. 作業（Wave）

- **W0（監査）**: 2.1〜2.3 の確定。① 各区分の呼び出し元の洗い出し（削除してよいもの／残すもの）。② 2.3 の全件。③ 旧経路が行う副作用で、新経路が引き継いでいないもの（ログ、静的な状態）。④ 旧関数を使うテストの一覧。⑤ 削除する診断ログの一覧（Q7）。結果は本書 §5 に追記する。
- **W1（期待値の固定）**: 旧関数の値の表（J の reflection の比べの置き換え）。W0 で挙がった「新経路に入っていない割り当て」の特性化テスト。
- **W2（8a）→ W3（8b）→ W4（8c）→ W5（8d）**: 実装。各 PR のあとにユーザーがビルド・テスト・コミット。
- **W6**: 文書（完了報告書、`Phase8-Status.md`、モデル図 01〜04、`copilot-instructions.md` の過渡期の記述）。

## 5. W0 の結果

（W0 の完了後に追記する。）

## 6. 質問（承認待ち）

- **Q1（実機確認との順序）**: A（推奨）: 削除を 8a〜8d の小さな PR に分けて先に行い、実機確認（Step9。U-35〜U-47 をまとめて）を最後に行う。不具合が見つかったら、PR 単位で git で戻す。Step7 の PR は、すべてビルド・テスト成功済みで、旧・新の比べで同じ出力を確かめている。B: 削除の前に、実機確認の主要項目（U-27〜U-47 のうち、押す・離す・切断・プロファイル切替を含む短い版）を行い、合格してから削除する。ロールバックの 1 行が使えるうちに実機で確かめられるため安全だが、実機確認を 2 回行うことになる。
- **Q2（新経路に入っていない割り当て。§2.3）**: A（推奨）: W0 で全件を洗い出し、(1) ジャイロスワイプは、入力の作り方を調べて新経路に入れる（Q7［7.8f］の承認どおり。小さな 7.8g として 8a の前に行う）、(2) 値が読めないなど、旧経路でも実質何もしないものは、「束縛を作らない＝何もしない」を仕様として固定する（テストで確かめる）。B: ジャイロスワイプは旧経路のまま残し、`Mapping` に最小限の旧経路を残す（Step8 の完了が不完全になる）。C: ジャイロスワイプの割り当てを廃止する（機能廃止。規約 2.2 のため別途承認が要る）。
- **Q3（スイッチの扱い）**: A（推奨）: 8d で `BindingMigrationOptions` を削除し、エンジンは束縛があれば常に処理する。ビルダーも常に全種別を組み立てる。ロールバックの手段は git のみ。B: スイッチを残す（常に全部有効の既定で。テスト用の差し込み口として）。コードが減らず、「旧経路が消えたのにスイッチがある」状態になる。
- **Q4（旧・新の両経路のテスト）**: A（推奨）: 両経路を走らせる Theory は、新経路だけにして（`migrated` の引数を消す）、期待値は旧経路で固定したものをそのまま使う。旧関数を reflection で比べるテストは、W1 の表との比べに置き換える。B: 旧・新の比べのテストは、削除と同時にすべて削除する（期待値の固定が失われる）。
- **Q5（`getMouseMapping`）**: A（推奨）: `Mapping` に残す（新経路が `MouseSampler` で借りている。マウス変換は Phase9 で `Mapping` から分ける）。B: 8d で `TriggerInput` 側（または新しいクラス）へ移す。感度・加速・デッドゾーンの計算を動かすため、特性化テストがまだ薄い現状ではリスクが高い。
- **Q6（系統 B の範囲）**: A（推奨）: W0 で呼び出し元がなくなった型だけを削除し、残るもの（`KeyAction`、`KeyButtonActionController` が新経路から使われていれば）は残して、残す理由を TODO ではなく通常のコメントにする。B: 系統 B を残す方針で、削除は `IActionFactory` 系のみに限る。
- **Q7（旧経路の診断ログの削除。規約 2.3）**: A（推奨）: 旧機構の動作を説明するだけの診断ログ（`Mapping SUMMARY (5s)`、`setBeingTriggeredCalls`、「press delegated to KeyButtonActionController」など）は、コードと一緒に削除する。新経路が同じ事象を出すログ（Key の押す・離す、SA の立ち上がり・立ち下がりなど）は 7.x で同じ文言に揃えてあり、変えない。削除する一覧は W0 で作り、承認を得てから消す。B: すべてのログを新経路に移す（旧機構にしか意味のないログも）。
- **Q8（`Mapping.cs` の減少の確認）**: A（推奨）: 各 PR で行数の変化を記録し、全体で約 2,000 行の減少を目標にする（目標未満でも、残す理由を記録する）。B: 行数は目標にしない。

## 7. 完了条件

- [ ] Q1〜Q8 の承認、W0 の結果の追記（§5）。
- [ ] 8a〜8d のすべてでビルド・テストが成功し、固定した期待値のテストが通る。
- [ ] `Mapping.cs` に旧ディスパッチ（§2.1 の A〜D）が残っていない。系統 B の削除対象が残っていない（残すものは理由を記録）。
- [ ] スイッチの扱いが Q3 のとおり。ラチェットの基準値が実数と一致。
- [ ] 完了報告書、`Phase8-Status.md`、モデル図、規約の記述を更新。
- [ ] 実機確認は Step9（Q1 のとおり）。

## 8. 次

- ユーザーが Q1〜Q8 を承認 → W0（監査）→ 結果を §5 に追記して確認 → W1 → 8a〜8d。
