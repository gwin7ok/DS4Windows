# Phase8-Step7-8c 計画書: Controls タブの Macro の新経路への移行（SA タブの Macro の仕様に合わせる）

作成日: 2026-10-20（同日、方針を改訂）  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Step2b-ControlsTab-Design.md`（§3.3 の E、§10。Q8＝B［実機確認は Step9］）、`Phase8-Step7-8a-Plan.md`、`Phase8-Step7-8b-Plan.md`（Controls の Key。同じ仕組みを使う）、`Phase8-Step7-3a-AltTab-Removal-Plan.md`（SA の Macro）  
状態: **方針の改訂を反映済み（2026-10-20 ユーザー指示: 最終的に Controls の Macro も SA の Macro の仕様［呼び出し、押したかの判定、状態の持ち方］に合わせる。今回から合わせる）。W1（旧経路の特性化）に着手。動作が変わる点は、W1 の実測のあと、一覧にしてユーザーが承認する（規約 2.2）**

---

## 0. 方針の改訂（初版からの変更）

初版は、動作を変えないために、Controls 専用の出力（`ControlMacroAction`）、再生窓口（`IControlMacroPlayer`）、3 種類目の押下判定（`GetBoolMapping`）を新設する案だった。ユーザーから「最終的には Controls の Macro も SA の Macro の仕様に合わせてよい」との指示があり、**捨てることになる中継の部品を作らず、最初から SA の Macro の仕組みに載せる**ことにした。

- 出力: 既存の `MacroAction`（SA の Macro と同じ）。
- 押下の判定: 既存の `ITriggerInput.IsActive`（SA のトリガーの判定）。
- 状態: 識別用の `SpecialAction` の `ActionInstanceState`（`IsMacroRunning` ほか）。ボタン単位の `macrodone`／`TryBeginDispatch` は使わない。
- 呼び出し: 立ち上がりで 1 回（`Execute`）。離したら `Stop`。繰り返し（押している間）は、`RepeatMacro` のフラグを持つマクロが、`PlayMacroTask` の内部で行う（SA と同じ）。

解釈（マクロの手順、`keyType`）は変換層（`ControlBindingBuilder`）が行い、識別用 SA の項目に入れて渡す（Q9＝A の考え方。`MacroAction` 自体は手順を解釈せず、再生窓口が SA の項目を読むため、`MacroAction` の値渡し化は、SA の Macro のリファクタとして別途 TODO とする）。

## 1. 目的と範囲

Controls タブで「ボタン → マクロ」を割り当てた dcs（`DS4ControlSettings`）を、旧経路から統一エンジンへ切り替える。運用は 7.8b と同じ: `MigratedControlKinds` に `Macro` を足す 1 行で有効化し、外すと旧経路に戻る。旧コードは Step8 まで残す。

**範囲（ボタン単位の所有。移行中の仕組み。7.8d〜f で区分を増やし、最終的にすべて新経路になる）**: 次のすべてを満たす dcs だけを新経路が処理する。それ以外は旧経路が全体を処理する。

- 通常の割り当てが Macro（`actionType == Macro`）で、手順（`action.actionMacro`）が空でない。
- シフトなし（`shiftActionType == Default`）、Extras なし（通常・シフトとも）、Lightbar マクロが有効でない。
- ジャイロスワイプの割り当ては対象外（7.8b と同じ）。

**やらないこと**: シフト（7.8d）、Extras（7.8e）、Button／マウス／ホイール（7.8f）、`PlayMacro`／`PlayMacroTask` 本体の変更、旧コードの削除（Step8）。

## 2. 現状の調査（2026-10-20）

### 2.1 旧経路の Macro（`Mapping.cs` 3925〜3938 行）

dcs 1 つにつき**毎フレーム**: 押下 ＝ `GetBoolMapping(control)` なら `PlayMacro(device, macroControl, "", null, action.actionMacro, control, keyType)`、押していなければ `EndMacro(…, control)`。そのあと毎フレーム `ResetToDefaultValue(control)`。

`PlayMacro`: ボタン単位の二重実行防止（`MacroRuntimeState.TryBeginDispatch`）→ 背景のタスク（`PlayMacroTask`）。タスクは `macrodone[control]` が偽のときだけ再生し、`HoldMacro` なら 50ms 後に `macrodone` を戻す。終了時の `EndMacro` も `macrodone[control] = false` にする。→ **押し続けたときの再実行の有無は、読むだけでは断定できない**。W1 で実測する。

### 2.2 SA タブの Macro（7.3 で新経路化済み）との違い

| 項目 | SA の Macro（合わせる先） | 旧 Controls の Macro |
|---|---|---|
| 呼び出し | 立ち上がり 1 回（`Execute`）、離して `Stop` | 押している間、毎フレーム |
| 押下の判定 | `getBoolSpecialActionMapping`（スティック ±55、L2／R2 が 100 超、ジャイロ ±67） | `GetBoolMapping`（ジャイロだけ 0 超／0 未満） |
| 状態 | `ActionInstanceState`（`IsMacroRunning`） | ボタン単位の `macrodone`／`TryBeginDispatch` |
| 繰り返し | `RepeatMacro` ＋ 同期キュー（`PlayMacroTask` の内部ループ。`BeingTriggered` を見る） | `HoldMacro`（50ms 後に `macrodone` を戻し、次のフレームで再実行） |
| 安全弁 | Fresh-press ガード | なし |

### 2.3 編集画面のフラグは共通

SA の Macro の編集（`MacroViewModel`）も、Controls の記録画面（`RecordBoxViewModel`）も、「押している間繰り返す」を **`RepeatMacro` ＋ `HoldMacro` の両方**として `keyType` に保存する。したがって、識別用 SA の `keyType` に dcs の `keyType` をそのままコピーすれば、「繰り返す」は SA 側の `RepeatMacro`（内部ループ）で表される。`PlayMacroDirect` は `macroControl`（フィールド）を使い、引数の `new bool[4]` は使われないため、ゲームパッドのボタンのコード（261 以上）を含むマクロも SA の経路で動く。

## 3. 設計

### 3.1 層の割り振り

| 層 | 担当 |
|---|---|
| 入力監視層 | `TriggerInput`（既存の `IsActive`） |
| 信号変換層 | `ControlBindingBuilder`（所有する dcs の選別、手順と `keyType` の解釈、識別用 SA の項目への設定）、`ControlButtonInputAction`（判定の種類を持つ）、エンジン |
| 出力層 | `MacroAction`（既存。手順の再生は `IMacroPlayer` → `Mapping.PlayMacro`） |

### 3.2 追加・変更するもの

- **`ControlJudgement`（新設。列挙）**: `Assignment`（`IsControlActive`。7.8b の Key）、`Trigger`（`IsActive`。SA のトリガーの判定。Macro）。`ControlButtonInputAction` のコンストラクターに、判定の種類と所属の区分（`ControlBindingKind`）を足す（既定は Assignment／Key）。
- **エンジン**: `IsOwnedBinding` が、`ControlButtonInputAction` の持つ区分で `IsControlMigrated(kind)` を調べる（7.8b は Key に固定していた）。
- **`ControlBindingBuilder`**: Macro の条件を満たす dcs ごとに束縛を作る。識別用 SA は、名前 `Synthetic_Macro_{device}_{control}`、種別 Macro、`details` ＝ 手順を「/」でつないだ文字列、`macro` ＝ 手順のリスト、`keyType` ＝ dcs の `keyType`（`Macro`／`HoldMacro`／`RepeatMacro`／`ScanCode`）、`pressRelease`／`synchronized`／`keepKeyState` は偽。出力は `new MacroAction(identity, device, macroPlayer)`（エンジン用のログつきのコンストラクターは使わない: 旧経路の Controls の Macro に SA 形式のログはないため）。方針は `respectsFreshPressGuard: true`（SA と同じ）、`suppressesTriggerButtons: false`、`resetsTriggerOutput: false`、`erasesOutputEveryFrame: true`（割り当てたボタンの通常の出力を消す規則は Controls のもの）、離したとき `Stop`。
- **依存の追加**: `ControlBindingBuilder` が `IMacroPlayer` を受け取る。
- **`ServiceRegistration`**: `MigratedControlKinds` に `Macro` の 1 行。

## 4. 動作が変わる点（W1 の実測のあと、確定して承認を得る）

初版の見立て（W1 で確定する）:

1. **呼び出し**: 毎フレームの呼び出し → 立ち上がりで 1 回。旧経路で「押し続けると再実行される」なら、`RepeatMacro` を持たないマクロは、再実行されなくなる。
2. **押下の判定**: ジャイロだけ、閾値が 0 から ±67 に変わる（スティックと L2／R2 は同じ）。
3. **Fresh-press ガード**: プロファイル適用直後に押しっぱなしのボタンは、一度離すまでマクロが動かない（SA と同じ安全弁）。
4. **状態**: ボタン単位の `macrodone`／二重実行防止 → `IsMacroRunning`。同じボタンの再押下の扱いが変わる可能性がある。
5. **`HoldMacro` だけを持つ手編集のプロファイル**（`RepeatMacro` なし）: 旧経路は `macrodone` の戻しで繰り返す可能性。新経路では繰り返さない。編集画面は両方を保存するため、通常は影響しない。

## 5. 作業（Wave）

- **W1（特性化。旧・新の両経路の Theory。W1 の時点では新経路の指定でも旧経路が処理する）**: `ControlsKeyTestScope` に `MapMacro(control, steps, keyType)` を足す。`Phase8Step78cW1ControlsMacroCharacterizationTests`: 押下で手順どおりにキーが入る、**押し続けたときの再実行の有無（回数を測り、`ITestOutputHelper` に出力して期待値を確定）**、`RepeatMacro|HoldMacro` のとき、離したあとの状態、スティックの判定（±55）、L2 の 100／101、割り当てたボタンの通常の出力が消える、シフト付きは旧経路、押したままの適用・切断。
- **W2（実装）**: §3.2。W1 のテストが新経路を走らせる。期待値の変更点は §4 のとおり、承認を得てから切り替える。
- **W3**: 割り当て、ロールバック、ソースのガード。
- **W4**: 完了報告書、`Phase8-Status.md`、チェックリスト（U-42）。**実機確認は作らない（Q8＝B）**。

## 6. 質問と決定

- **方針（ユーザーの指示）**: SA の Macro の仕様に合わせる（§0）。動作が変わる点は W1 のあとに一覧で承認する。
- **Q1（所有の範囲）**: §1 のとおり（Macro、手順あり、シフトなし、Extras なし、Lightbar マクロなし）。7.8b と同じ考え方。【推奨 A。ユーザーの方針に沿って進める】
- **Q2（`MacroAction` の値渡し化）**: SA の Macro のリファクタとして別途 TODO（`MacroAction` は手順を解釈せず、再生窓口が SA の項目を読むため、今回の範囲外）。【推奨 A】
- **Q3（ログ）**: Controls の Macro は、SA タブ形式のログを出さない（旧経路にない）。`PlayMacro` の START GUARD の Trace は `Mapping` から従来どおり出る。【推奨 A】

## 7. 完了条件

- [ ] W1 の実測値をこの計画書に追記し、§4 の変更点をユーザーが承認。
- [ ] ビルド・テストが成功（ユーザーが実行）。割り当て（新 ≦ 旧）、ロールバック、全ガード。
- [ ] 実機確認は Step9（U-42）。
- [ ] `Phase8-Step7-Completion-Report.md`、`Phase8-Status.md` の更新。

## 8. 次

7.8d（シフト）。事前に、通常／シフトの組み合わせの特性化テストを追加する（Step2b Q6＝A）。
