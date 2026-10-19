# Phase8-Step7-3 計画書: Macro 種別の切替（単発／RunOnRelease／繰り返し／Alt-Tab）

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Step7-Plan.md`（§3 共通の手順、§4 の 7.3）、`Phase8-Step7-2-Plan.md`（7.2 の進め方）、`Phase8-Step2-Design.md` §6・§7、`Phase8-Step1-Trigger-Spec-Ledger.md` §5（Macro）  
状態: **計画書改訂済み（2026-10-19）。Alt-Tab の特別扱いの削除（`Phase8-Step7-3a-AltTab-Removal-Plan.md`。完了、実機確認済み）により、下記の「Q1」と、W2 の `WhileHeld` の実装・`IMacroPlayer`／`IMapping` の変更は不要になった（取り消し線の項目）。Q2〜Q4 の承認待ち**

---

## 1. 目的

Macro 種別の特殊アクションを、旧ループ（`MapCustomAction` の Macro の分岐）から、統一ディスパッチ（エンジン）へ切り替える。7.1・7.2 と同じ運用: `ServiceRegistration` の既定の一覧に `Macro` を 1 行足すと切り替わり、外すと戻る。旧コードは Step8 まで残す。マクロの再生そのもの（`Mapping.PlayMacro`／`PlayMacroTask`）は変更しない。

## 2. 現状の調査

### 2.1 旧経路（`Mapping.MapCustomAction` の Macro の分岐。4690〜4778 行付近）

**共通部**: トリガーの AND 条件、`delayTime`、トリガーのボタンの抑制と出力の消去、Fresh-press ガード（Macro は対象）。**RunOnRelease**（`pressRelease`）のときだけ、`firstTouch` が真の間は成立を反転する（4413 行）。

**通常（`pressRelease` が偽）**:
1. 立ち上がり（`BeingTriggered` が偽）: ログ（Trace の立ち上がり 4 行、Debug 「SpecialAction Macro: …ActionEntries=」）。`DispatchInputEdge` → `ActionManager.DispatchTriggerEdge` → `MacroActionAdapter.OnTrigger` → `MacroAction.Execute` → `IMacroPlayer.Play` → `Mapping.PlayMacroDirect`（再生はバックグラウンドのタスク）。
2. **押している間（`BeingTriggered` が真）: `RepeatMacro` でなければ、毎フレーム `EndMacro(device, macroControl, action.macro, None)` を呼ぶ**。実際の効果は、マクロが Alt-Tab の並び（`164/9/9/164` か `18/9/9/18` で始まる）で、Alt-Tab の途中（`altTabDone` が偽）のときに、Alt を離すこと（`AltTabSwappingRelease`）だけ。
3. 離したとき: `BeingTriggered` を偽にして、解放の辺を送る（`MacroAction.Stop` → `IMacroPlayer.Stop` → `EndMacroDirect(device)`。マクロの文字列が空のため、実際の効果はなく、`DefaultMacroPlayer` の再生中の旗を下げるだけ）。

**RunOnRelease（`pressRelease` が真）**:
- 押したとき: `firstTouch` を真にする（準備。実行しない）。離したとき: `firstTouch` を偽にして 1 回実行（同じ辺の送出）。その次のフレームで、解放の辺。
- `firstTouch` は、`SpecialAction` の公開のフィールドで、Macro と MultiAction だけが使う。

**ログ**: 実行が決まった場所で、Trace（`LogSpecialActionTrace` の立ち上がり）と Debug（`LogActionDoneCountOnTrigger`。文脈は通常が `"Macro"`、RunOnRelease が `"MacroRelease"`）。

### 2.2 新経路（Step3〜Step6 で用意済み）

- `ActionBindingBuilder`: Macro → `TriggerBinding(ComboTriggerInputAction[delayTime、invertOnRelease＝pressRelease], MacroAction, BindingPolicy.Default)`。
- `MacroAction`: `Execute` → `IMacroPlayer.Play`、`Stop` → `IMacroPlayer.Stop`（旧経路のアダプターと同じ呼び出し）。**押している間の処理（§2.1 の 2）は、まだない**（`IHeldOutputAction` を実装していない）。
- `ComboTriggerInputAction` の `invertOnRelease`: 押した時点で準備、離した時点で 1 回成立（準備の状態は、入力の内部。`firstTouch` は使わない）。遅延つきのときは反転を無視する（旧経路の分岐と同じ。単体テストあり）。
- 7.1・7.2 で、適用・切断・再接続（`PublishBindings`／`ReleaseSlot`／`PublishSlot`）、ガード、抑制は確認済み。

### 2.3 差になりうる点（W1 の特性化で確定する）

| # | 点 | 旧経路 | 新経路 | 扱い |
|---|---|---|---|---|
| 1 | ~~押している間の `EndMacro`（Alt-Tab の Alt の解放）~~ | 3a で削除した（効果がなくなったため、呼び出しも削除） | 不要 | ~~Q1~~（取り消し） |
| 2 | RunOnRelease の `firstTouch` | 旧経路が書く | 書かない（入力の内部の状態） | **Q2** |
| 3 | ログ | §2.1 のとおり | `MacroAction` は出さない | **Q3** |
| 4 | 再生中の旗（`IsMacroRunning`、`macroTaskQueue`）と二重実行の防止 | `PlayMacro` の中（変更しない） | 同じ `PlayMacro` を呼ぶ | 変更なし。W1 で、押している間に再実行されないことを確認 |
| 5 | `PlayMacroDirect` の `new bool[4]` | 旧経路もこの入口を通る（フォールバックの `handled` が偽のときだけ、共有の `macroControl`） | 同じ入口 | 変更なし（観察の記録: §5） |

## 3. 作業

### W1: 足場と両経路の特性化（コードは変えない）

**W1 の実装（2026-10-19。ユーザーのビルド・テストの結果待ち）**: `SpecialActionTestScope(migrated: true)` が Macro も移行。`Phase8Step1MacroBranchCharacterizationTests` の 3 本を `[Theory]`（旧・新。`firstTouch` の検査は旧経路だけ）。`Phase8Step73W1MacroDualPathTests`（新設）: 状態（`BeingTriggered`、`IMacroPlayer.IsPlaying`）を旧・新で比較。Press、RunOnRelease、ガード中、AND 条件、遅延、押している最中のプロファイル適用。観察の口は、再生中の旗（`DefaultMacroPlayer.IsPlaying`。`Play` で真、`Stop` で偽）を使い、`IMacroPlayer` の差し替えは不要だった。

- `SpecialActionTestScope(migrated: true)` が、Macro も移行する（Button・Key に加える）。`AddMacro` を用意。
- 観察の口: 再生の回数と引数は、`IMacroPlayer` を記録用に差し替えて数える（`InstallProfileRecorder` と同じ方式で、`MacroActionAdapter` の `macroPlayer` を差し替える。または、記録用の `IMacroPlayer` を `ActionManager` の経路へ注入する）。W1 で、最小の方法を選ぶ。
- 既存の Macro の特性化（`Phase8Step1MacroBranchCharacterizationTests` の 3 本）を `[Theory]`（旧・新）にする。`firstTouch` の検査は旧経路だけ（Q2）。`BeingTriggered` の検査は両方。
- 新規: 再生の回数（押下で 1 回、保持中は増えない、離して解放）、RunOnRelease（押したときは再生しない、離したときに 1 回）、ガード中は再生しない、AND 条件、遅延、押している最中のプロファイル適用（7.1 の `SimulateProfileApply`）、Alt-Tab の並びのマクロの保持中の `EndMacro`（Q1）。

### W2: 新経路の不足分

- ~~Q1 の実装（不要になった）~~: `MacroAction` が `IHeldOutputAction.WhileHeld` を実装し、旧経路と同じ条件（`RepeatMacro` でなく、`pressRelease` が偽）のときだけ、毎フレーム `IMacroPlayer.WhileHeld(device, action)` を呼ぶ。`IMacroPlayer` に、**既定の実装つきのメンバー**として `WhileHeld` を足す（既存の実装・テスト用の偽物は、変更なし）。`DefaultMacroPlayer` は `IMapping.EndMacroDirect(device, action)` を呼ぶ。`IMapping.EndMacroDirect` に、**省略可の引数 `SpecialAction action = null`** を足す（メンバーの数は 10 のまま。`null` のときは、従来どおり）。`action` が指定されたときは、旧経路と同じ `EndMacro(device, macroControl, action.macro, None)` を呼ぶ。
- Q3 の実装: ログを `MacroAction` から、旧経路と同じ文言・順序で出す（`index`・`ActionEntries` はビルダーが渡す。7.2 の Key と同じ方式）。NLog のメモリ出力で、旧経路との行の一致をテストする。

### W3: スイッチと性能

- `ServiceRegistration` の既定の一覧に `Macro` を足す。ロールバックのテスト。割り当て（新 ≦ 旧）。ホストの既定の一覧の検査を更新。

### W4: 実機確認と報告

- チェックリストに U-29（Macro）を追加し、ユーザーが確認する。`Phase8-Step7-Completion-Report.md` に 7.3 を追記。

## 4. 質問（推奨は A）

- ~~**Q1（押している間の `EndMacro`。§2.3 #1）**~~ 【不要: Alt-Tab の特別扱いの削除により、押している間の呼び出しの効果がなくなった】: Alt-Tab のマクロが、押した次のフレームで Alt を離す動作を、新経路でも保つか。  
  A（推奨）: 保つ。`IMacroPlayer.WhileHeld`（既定の実装つき）と、`IMapping.EndMacroDirect` の省略可の引数で実装する（上記 W2）。旧経路と同じ条件・同じ呼び出し。  
  B: 保たない（Alt-Tab のマクロで Alt が押されたままになりうる。機能の低下のため、2.2 により不可）。  
  C: Alt-Tab の並びのマクロだけ、新経路へ移さず旧経路に残す（ビルダーが束縛を作らない。二重の経路が残り、Step8 の削除が複雑になる）。
- **Q2（`firstTouch`）**: A（推奨）: 新経路は `firstTouch` を書かない（RunOnRelease の準備は、入力の内部の状態）。既存の特性化の `firstTouch` の検査は、旧経路だけにする（期待値は変えず、検査の対象を旧経路に限る）。`firstTouch` を読むのは、Macro の旧経路と MultiAction だけ。B: 新経路でも `firstTouch` を書く（入力の内部の状態と二重になる）。
- **Q3（ログ）**: A（推奨）: Trace と Debug のログを、同じ文言・レベル・順序で `MacroAction` から出す（規約 2.3）。B: 省く（規約に反する）。
- **Q4（実機確認）**: A（推奨）: 7.1・7.2 と同じく、PR ごとにユーザーが実機で確認し、合格してから 7.4 へ進む。B: Program と併せて、後でまとめて確認する。

## 5. リスクと対応・観察の記録

- **Alt-Tab**: Q1 のとおり。W1 で、旧・新の `EndMacro` の呼び出しを比べる。
- **二重実行**: 再生の入口（`PlayMacro` の二重実行の防止［K3］）は変更しない。W1 で、押している間に再生が増えないことを確認。
- **観察（変更しない）**: `PlayMacroDirect` が `new bool[4]` を渡すため、特殊アクション経由のマクロのボタン出力の手順（`macroControl` の 4 要素より後ろ）は、共有の `macroControl` に反映されない。旧経路もこの入口を通るため、新経路で差は出ない。マクロの再設計（`TODO(Phase9-C)`）で扱う。
- **ロールバック**: `ServiceRegistration` の Macro の 1 行を外す。

## 6. 完了条件

- [ ] W1〜W4 のビルド・テストが成功（ユーザーが実行）。Macro の特性化テストが、旧経路・新経路の両方で、期待値を変えずに成功（`firstTouch` の検査の対象を旧経路に限る点を除く）。
- [ ] 全テスト・ラチェットの一致検査・1 ファイル 1 型のガードが成功。割り当て（新 ≦ 旧）。`IMapping` のメンバーが 10 のまま。
- [ ] 既定の一覧に Macro が入り、ログの文言・レベルが旧経路と同じ。ロールバックのテストが成功。
- [ ] 実機確認 U-29 が合格（または先送りの記録）。
- [ ] `Phase8-Step7-Completion-Report.md`、`Phase8-Status.md` の更新。

## 7. 次

7.4 Program（短い個別の計画書を、U-29 の合格後に作成）。
