# Phase8-Step7-2 計画書: Key 種別の切替（Press／Toggle／リピート）

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Step7-Plan.md`（§3 共通の手順、§4 の 7.2）、`Phase8-Step2-Design.md` §6・§7、`Phase8-Step2-Intent-Decisions.md`（項目 4: Key の `uTrigger` は無視）、`Phase8-Step7-Completion-Report.md`（7.1 の結果）  
状態: **Q1〜Q4 を推奨どおり承認（2026-10-19）。7.2-W1・W2 完了（ビルド・テスト成功）。7.2-W3 実装済み（結果待ち）**

---

## 1. 目的

Key 種別の特殊アクション（キーボードのキーの出力。Press／Toggle／リピート）を、旧ループ（`MapCustomAction` の Key の分岐）から、統一ディスパッチ（エンジン）へ切り替える。1 行（`ServiceRegistration` の既定の一覧に `Key` を足す）で切り替わり、外すと戻る（Q2＝A の 7.1 と同じ運用）。旧コードは Step8 まで残す。

## 2. 現状の調査

### 2.1 旧経路（`Mapping.MapCustomAction`）

**判定と共通処理**（Button と同じ共通部）: トリガーの AND 条件、`delayTime`、トリガーのボタンの抑制（`AddSuppressed`）と出力の消去（`ResetToDefaultValue`）、Fresh-press ガード（Key は対象。ガード中は何も実行せず、抑制だけ維持）。

**Key 固有（成立中）**（4787〜4869 行付近）:
1. `uTrigger`（解除用のボタン）は無視される（4421〜4424 行。`uTriggerCount = 0`）。Intent 項目 4 のとおり、新経路も移植しない。
2. `BeingTriggered` が偽（立ち上がり）のときだけ、ログ（Debug: 「SpecialAction KEY entry」「KeyTriggered」「KEY triggered」、`LogActionDoneCountOnTrigger("KeyTriggered")`、`LogSpecialActionTrace` の立ち上がりの TRACE 4 行）。
3. `DispatchOrSetBeingTriggered(action, device, true)`（`prevInputEstablished` による入力のエッジ検出つき）。
4. **`deviceRuntime[device].UntriggerIndex`**: Toggle でなければ `index`、Toggle なら `-1` を書く。
5. `deviceState[device].keyPresses[key]` の項目（と `nativeKeyAlias`）を作り、`kp.current.repeatCount++`（毎フレーム。`repeatCount` を読む場所は、この加算と 0 クリアだけ）。
6. `ActionManager.DispatchTriggerEdge(...)` を毎フレーム呼ぶ（内部で `KeyAction` → `KeyButtonActionController` が、Press／Toggle／リピートの意味を決める。エッジの検出は内部で行う）。

**Key 固有（解放）**（4989〜5043 行付近）: `uTrigger` なし ∧ `BeingTriggered` ∧ `UntriggerIndex == index` のときだけ、`BeingTriggered` を偽にして `UntriggerIndex = -1`、ログ（`KeyReleased` の 2 行）、`TryDispatchSATriggerReleased`（→ `KeyAction.OnRelease` → コントローラーの解放）。

### 2.2 新経路（Step3〜Step6 で用意済み）

- `ActionBindingBuilder`: Key → `TriggerBinding(ComboTriggerInputAction[delayTime 付き], KeyOutputAction, BindingPolicy.Default)`。
- `KeyOutputAction`: `Execute` → `KeyAction.OnTrigger`、`Stop` → `KeyAction.OnRelease`（旧経路と同じ `KeyAction` → `KeyButtonActionController`）。ネイティブキーは `OutputKBMMapping.GetRealEventKey`（旧経路と同じ。Step3 W4）。
- エンジン: 立ち上がりで `Execute` を 1 回、離したときに `Stop`（`StopOnRelease` 既定）。Fresh-press ガードは既定で有効。
- `KeyOutputActionTests` で、系統 B（`KeyActionAdapter`）と同じ押下・解放の列であることは確認済み。**実プロファイルの束縛を、旧ループと並べて比べる検証は、まだない**（本 PR の W1 で行う）。

### 2.3 差になりうる点（W1 の特性化で確定する）

| # | 点 | 旧経路 | 新経路 | 扱い |
|---|---|---|---|---|
| 1 | **`UntriggerIndex` の共有** | Key の Press が `UntriggerIndex` を書く。同じ変数を Profile の復帰（4597、5329〜5395 行）も使う。Key が 2 つ同時に押されると、最後のキーしか解放が送られない（先のキーは `BeingTriggered` が残り、キーが押されたままになりうる） | 束縛ごとに、離したときに `Stop` を呼ぶ。`UntriggerIndex` は書かない | **Q1**（旧経路の癖を再現するか、是正するか） |
| 2 | `keyPresses` の項目と `repeatCount` | 毎フレーム加算。読む場所なし | 作らない | **Q2** |
| 3 | Debug／Trace ログ | 上記 §2.1 の 2 | `KeyOutputAction` は出さない | **Q3**（引き継ぐ） |
| 4 | Toggle の解放 | `UntriggerIndex = -1` のため、解放を送らない | 離したときに `Stop` を呼ぶ（`KeyAction.OnRelease`） | W1 で、コントローラーが Toggle の解放を無視することを確認する（`KeyButtonActionControllerAdapter_Handle_Release_DoesNot_Clear_Toggle` の既存テストあり）。差が出るなら `StopOnRelease=false`（Toggle のとき）で、旧経路と同じにする |
| 5 | ScanCode | 旧経路も、`KeyAction` 側が `keyType` から読む（別件として記録済み） | 同じ | 変更なし |
| 6 | 毎フレームの呼び出し | `DispatchTriggerEdge` を毎フレーム呼ぶ（内部でエッジを検出） | エッジのときだけ呼ぶ | W1 で、キー送出の列（記録用の KBM）が一致することを確認 |

## 3. 作業

### W1: 足場と両経路の特性化（コードは変えない）

**W1 の実装（2026-10-19。ビルド・テスト成功）**

- `SpecialActionTestScope(migrated: true)` が、Button に加えて Key も移行する（本番コードは変更なし）。
- `Phase7cW2…` の Key の 3 本（成立・保持・解放、ガード中は押されない、Toggle は `UntriggerIndex` を登録しない）を `[Theory]`（旧・新）にした。`UntriggerIndex` の検査だけは Q1 により、新経路は -1。
- `Phase8Step72W1KeyDualPathTests`（新設）: 同じ入力の列を両経路に流し、フレームごとの状態（`BeingTriggered`、`IsToggledOn`、合成キーの押下の数、解放の数）が一致することを確認。Press、Toggle、ガード中、AND 条件、長い遅延、押している最中のプロファイル適用。加えて、2 つのキーの同時押し（旧経路は先のキーを解放しない［既存の癖の固定］、新経路は各キーを解放する［Q1＝A］）。
- Q2 の確認（`keyPresses` の項目と `repeatCount`）: 状態の比較に、`keyPresses` の押下の数・解放の数を含めた（`repeatCount` は比較から外した。読む場所がないため）。一致すれば、Q2＝A により移植しない。

- `SpecialActionTestScope`: 移行する種別の集合を指定できるようにする（現在は `migrated` が Button 固定）。`AddKey` で追加したアクションを、本番と同じ経路（ビルダー → 登録簿）で登録する。
- 記録用の KBM（`MappingCharacterizationScope.Recorder`）で、キー送出の列（押下・解放・リピートの有無）を、旧経路と新経路で比べる。
- 既存の Key のテスト（`Phase7cW2…` の `KeyPress_*`、`KeyToggle_*`）を `[Theory]` にし、旧経路・新経路の両方で期待値を変えずに流す（ただし、`UntriggerIndex` の検査は、新経路では書かないため Q1 の結論に従う）。
- 新規: Press の押下・保持・解放、Toggle の ON／OFF、ガード中は押されない、遅延つき、トリガーが AND、2 つの Key の同時押し（§2.3 #1）、プロファイル適用の前後（7.1 の `SimulateProfileApply` を流用）、`keyPresses`／`repeatCount` の有無（§2.3 #2）。

**W1 の結果（実測。予想との差）**:
- 旧経路・新経路の状態は、Press、Toggle、ガード中、AND 条件、遅延、押している最中のプロファイル適用で一致した。
- **Q1 の前提は再現しなかった**: Key を 2 つ同時に押して、どちらを先に離しても、旧経路は各キーを解放した（新経路と一致）。Key の `UntriggerIndex` の癖で「キーが押されたままになる」現象は、この手順では確認できない。新経路は `UntriggerIndex` を書かない（Profile の復帰との共有をやめる）ので、結果は同じで、衝突だけがなくなる。
- **Q2 の結果**: 合成キーの押下の数（`vkCount`）は、旧経路でも 0 のままで、観測できない。適用の後始末（切断と同じ）で、旧経路は同じキーの解放を 1 回多く送る（Mapping が作る `nativeKeyAlias` の項目があるため。コントローラーの破棄でも解放されるため、キーを離す効果は同じ）。**この重複の解放は、新経路に移植しない**（効果が同じため。PR の説明に明記する）。
- **遅延**: `delayTime` を持つ Key は、遅延の基準時刻の初期値が最小値のため、最初の押下は遅延なしで成立する（旧・新とも。既存の挙動の固定）。

### W2: 新経路の不足分

**W2 の実装（2026-10-19。ビルド・テスト成功）**
- `KeyOutputAction` が、旧 `MapCustomAction` の Key の分岐のログ（Debug: KEY entry、KeyTriggered ×2、KEY triggered、KeyReleased ×2。Trace: Trigger detected／maps to／Trigger keys［常に `<none>`］／output combo）を、同じ文言・順序で出す。`index` と `ActionEntries`（`ISpecialActionRepository.ActionList.Count`）は、ビルダーが渡す。差: 「untriggerindex」は常に -1。Toggle の解放は、旧経路が送らないため、ログも出さない。`IOutputContextInput`（エンジンの文脈が入力を返す）を再び使う（解放のログの「released=」のため）。
- テスト: `Phase8Step72W2KeyLogTests`（旧・新の行の一致。Press／Toggle、ログ無効時）。

- Q3 に従い、ログ（Debug 3 行＋`LogActionDoneCountOnTrigger`＋TRACE の立ち上がり＋解放の 2 行）を `KeyOutputAction`（`Execute`／`Stop`）で、同じ文言・レベル・順序で出す。ログが無効なときは文字列を作らない（Debug は、`AppLogger` が文字列を受けるため、判定してから作る）。NLog のメモリ出力で、旧経路との行の一致をテストする（7.1 の `Phase8Step7W2ButtonTraceLogTests` の方式）。
- Q1・Q2 の結論の実装。

### W3: スイッチと性能

**W3 の実装（2026-10-19。ユーザーのビルド・テストの結果待ち）**
- `ServiceRegistration` の既定の一覧に `Key` を 1 行足した。既存テストの更新（設計どおりの変更）: ホストの既定の一覧（`Phase8Step4W1ContractTests`、`Phase8Step7W3ButtonSwitchTests`）を「Button と Key」に。
- `Phase8Step72W3KeySwitchTests`: Key の行だけを外すと束縛が作られず旧経路が処理する（ロールバック）／新経路の割り当て ≦ 旧経路／Key 30 件を移行した状態の `MapCustom` の費用の出力（旧経路と並べて +5% 以内を確認）。
- 実機確認 **U-28**（8 項目）をチェックリストに追加。

- `ServiceRegistration` の既定の一覧に `Key` を足す（1 行）。ロールバックのテスト（7.1 の `Phase8Step7W3ButtonSwitchTests` に追加）。
- ベンチマーク（`Key` を移行した状態）。許容は +5% 以内。割り当ては新経路 ≦ 旧経路。
- ホストの既定の一覧の検査を更新（Button と Key）。

### W4: 実機確認と報告

- チェックリストに U-28（Key）を追加し、ユーザーが確認する。`Phase8-Step7-Completion-Report.md` に 7.2 を追記。

## 4. 質問（推奨は A）

- **Q1（`UntriggerIndex` の癖。§2.3 #1）**: Key の Press が `UntriggerIndex` を書き、Key を 2 つ同時に押すと先のキーの解放が送られない（キーが押されたままになりうる）。Profile の復帰とも同じ変数を共有している。  
  A（推奨）: 新経路は、束縛ごとに離したときに解放を送る（不具合の是正）。`UntriggerIndex` は書かない。Profile の復帰との衝突もなくなる。是正であることを、Intent-Decisions に記録し、PR の説明に明記する。  
  B: 旧経路の癖を再現する（`UntriggerIndex` を書き、1 つしか解放されない）。再現のコードが残り、不具合も残る。
- **Q2（`keyPresses` の項目と `repeatCount`。§2.3 #2）**: A（推奨）: W1 で、観測できる差（キー送出、切断時の解放）が出るかを確認し、出なければ移植しない（読む場所がない加算のため）。出る場合だけ、その分を移す。B: 無条件に同じ項目を作る。
- **Q3（ログ）**: A（推奨）: Debug／Trace のログを、同じ文言・レベル・順序で `KeyOutputAction` から出す（規約 2.3）。B: 省く（規約に反する）。
- **Q4（実機確認）**: A（推奨）: 7.1 と同じく、PR ごとにユーザーが実機で確認し、合格してから 7.3 へ進む。B: Macro・Program と併せて後でまとめて確認する。

## 5. リスクと対応

- **キー押しっぱなし・リピート**: 旧経路は毎フレームの呼び出しで、新経路はエッジだけの呼び出し。記録用 KBM の列の比較（W1）で確認する。長押しのリピートは、`KeyButtonActionController` の側（変更なし）。
- **Toggle の解放**: §2.3 #4。W1 で確定する。
- **適用・再接続**: 7.1 と同じ経路（`PublishBindings`／`PublishSlot`）で束縛が作られる。7.1 の実機確認（U-27）で確認済み。
- **ロールバック**: `ServiceRegistration` の Key の 1 行を外す。

## 6. 完了条件

- [ ] W1〜W4 のビルド・テストが成功（ユーザーが実行）。Key の特性化テストが、旧経路・新経路の両方で、期待値を変えずに（Q1 の是正を除く）成功。
- [ ] 全テスト・ラチェットの一致検査・1 ファイル 1 型のガードが成功。ベンチマークが許容（+5%）と割り当て（≦ 旧経路）を満たす。
- [ ] 既定の一覧に Key が入り、ログの文言・レベルが旧経路と同じ。ロールバックのテストが成功。
- [ ] 実機確認 U-28 が合格（または先送りの記録）。
- [ ] `Phase8-Step7-Completion-Report.md`、`Phase8-Status.md` の更新。

## 7. 次

7.3 Macro（短い個別の計画書を、U-28 の合格後に作成）。
