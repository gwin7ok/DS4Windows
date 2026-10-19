# Phase8-Step7-5b 計画書: Profile 種別の復帰（Unload on trigger release）をエンジンへ

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Step7-5-Plan.md`（§3 の 7.5b、Q5・Q6）、`Phase8-Step2-Design.md`（§15.2 の `UntriggerPass`、§16）、`Phase8-Step2-Intent-Decisions.md`（項目 5・6・11・12）、`Phase7b` の W1（復帰の合図の退避・復元）、`Phase8-Step7-Completion-Report.md`（7.5a）  
状態: **Q1〜Q6 を推奨（A）どおり承認（2026-10-19）。W1 成功（ビルド・テスト）。W2 実装済み（結果待ち）**

---

## 1. 目的

7.5a で、復帰を持たない「切替のみ」の Profile は新経路になった（実機確認 U-32 が合格）。7.5b では、**復帰を持つ Profile（「Unload on trigger release」）** も新経路へ移す。移すのは次の 3 つ。

1. 復帰の合図（旧 `Mapping.deviceRuntime[device].UntriggerAction`／`UntriggerIndex`）の置き場所を、**エンジン**にする（Q6＝A、承認済み）。
2. 復帰の確認（旧 `MapCustomAction` の後段の `UNTRIGGER check`）を、エンジンの `ProcessUntrigger` にする。
3. 連鎖の防止（7.5a で `IMapping.CaptureProfileUntrigger` を参照）が、エンジンの合図も見るようにする。

**範囲の訂正（7.5a の補足を受けて）**: 手動の解除トリガー（`uTrigger`）は UI から作れない（a7277da9 で「Set Unload Trigger」ボタンが削除された）。7.5b の対象は **自動の復帰（`automaticUntrigger`）で、`uTrigger` を持たない Profile だけ**。`uTrigger` を持つ Profile は、旧経路のまま残す（Q2）。

## 2. 現状の調査

### 2.1 旧経路の復帰（`Mapping.cs` 5316〜5398 行。7.5-Plan §2.1 のとおり）

毎フレーム、`MapCustomAction` のループが終わった**後**に、合図があれば次を行う。
- **成立の判定（自動）**: `action.trigger` のうち、**どれか 1 つでも押されていない**（`getBoolSpecialActionMapping`）。
- **成立しないフレーム**: `DispatchOrSetBeingTriggered(action, device, false)`（切替アクションの `BeingTriggered` を下げ続ける。Intent 項目 6）。
- **成立したフレーム**: 解除トリガー（`uTrigger`）ごとの処理（ログ、`BeingTriggered` を上げる、通常の割り当てのキーを離す）は、自動で `uTrigger` が空なら**何も起きない**。そのあと、**合図を消し**、`ProfileSwitchAction.Stop` → `RestoreProfile` → `RestoreFromAction`（`OlderProfilePath` へ適用）。
- ループの途中で `return` した（Profile の切替のフレーム、例外）ときは、この確認は**行われない**。
- `controls == ucontrols` の条件（同じボタンでの再成立の待ち）は、自動では `ucontrols = "AutomaticUntrigger"`（`SpecialAction.cs`）で `controls` と一致しないため、常に真。

合図の設定（4591 行）は、切替が成立したとき（ガード・実行中・連鎖の条件を満たしたとき）に、`Dispatch` の**前**に行う。`prevProfileName`／`prevProfileWasTemporary` は書くだけで読まれない（7.5a の Q3＝A のとおり、移さない）。

### 2.2 新経路で足りないもの

- エンジンに合図も復帰の確認もない（`TriggerDispatchEngine` のコメント: Step7.5 で足す）。
- `ActionBindingBuilder` は、復帰を持つ Profile の束縛を作らない（`ProfileUntriggerMigrated = false`）。
- `ProfileSwitchAction.CanExecute`（連鎖の防止）は、`IMapping.CaptureProfileUntrigger` の合図だけを見る。
- 離したエッジの `Stop`: `BindingPolicy.StopOnRelease` は既定で true。Profile は、適用が状態を作り直すため、離したエッジは通常起きない（設計書 §15.1）が、**適用が終わる前に離すと**、`Stop`（＝復帰）が呼ばれうる。旧経路にも同じ可能性（`ProfileSwitchActionAdapter.OnRelease`）がある。W1 で旧・新を比べて確かめる（Q4）。

## 3. 方針と作業

### 設計

- **合図の型**: エンジン内部の `UntriggerSignal`（切替前の束縛 `IActionBinding` を 1 つ持つ。束縛から `Source`・`Index`・出力を引ける）。デバイスごとに 1 つ。
- **設定**: `ProcessBinding` の立ち上がり、`Execute` の**前**（旧経路と同じ順）に、`binding.Policy.Untrigger != None` なら設定する。種別の分岐は持たない（`BindingPolicy` で表す）。
- **復帰の確認**: `ITriggerDispatchEngine.ProcessUntrigger(device, input)`（新設）。`Mapping` は、旧ループの後（旧経路の復帰の確認と同じ位置）で呼ぶ。合図が空なら即時に戻る（ロックも割り当てもなし）。
  - 自動: `Source.trigger` のどれかが `input.IsActive == false` → 成立。成立しなければ、`Source` の状態の `BeingTriggered` を下げる。
  - 成立したら、**合図を消してから**、束縛の出力の `Stop(ctx)`（`ProfileSwitchAction.Stop` → `RestoreProfile`）。
- **残す・消す**: 適用（`OnProfileApplied`）では合図を消さない（現状は未接続で、状態のリセット要求も立たない）。物理切断（`OnDeviceDisconnected`）では消す。これで Phase7b の `CaptureProfileUntrigger`／`RestoreProfileUntrigger` の退避・復元が、移行済みの Profile には不要になる（`IMapping` の 2 メンバーは、旧経路［`uTrigger` つき］のために残る）。
- **連鎖の防止**: `EngineContext` が `IPendingUntriggerQuery`（新設。`SpecialAction PendingUntriggerAction { get; }`）を実装し、`ProfileSwitchAction.CanExecute` が、(エンジンの合図) と (`IMapping` の合図) の**どちらか**が Profile なら、切り替えない。`IOutputContext`・`IMapping` のメンバーは増やさない。
- **ビルダー**: `ProfileUntriggerMigrated` を削除する。`uTrigger` を持つ Profile は束縛を作らない（旧経路。自動と併用でも旧経路。旧経路の自動の分岐に、`uTrigger` ごとの処理が付くため）。それ以外（切替のみ、自動の復帰）は束縛を作る。

### W1: 特性化（コードは変えない）

**実装（2026-10-19）**: `Phase8Step75bW1ProfileUntriggerDualPathTests`（新設。復帰の回数を旧・新の両経路で比べる: 離して 1 回、AND 条件は 1 つ離れた時点、切り替えなかったときは復帰なし、適用をまたぐ、物理切断で消える）。旧経路だけ: 合図の設定の条件、復帰の前に合図が消える。報告のみ: 適用が終わる前に離したとき（Q4 の材料。判定なし）。W1 の時点では新経路の指定でもビルダーが束縛を作らないため、旧経路が処理する。

`SpecialActionTestScope(migrated)` と `Phase8Step75a…` の方式で、**旧経路だけ**で期待値を固定する（新経路はまだ復帰を持たないため）。
- 自動の復帰: トリガーを離すと `RestoreFromAction` が 1 回。保持中は呼ばれない。AND 条件（`L1/R1`）は、どれか 1 つが離れた時点で成立。
- 合図が立つ時点: 切替が成立したフレーム。ガード中・実行中・連鎖で切り替えないときは、立たない。
- 切替のフレームでは、復帰の確認が行われない（`return`）。
- 復帰の前に合図が消える（復帰の後に残らない）。
- 適用をまたいで合図が残る（Phase7b W1 の観点）。切断で消える。
- 復帰待ちの間、切替アクションの `BeingTriggered` が下がり続ける（Intent 項目 6）。
- 離したエッジ（適用が終わる前に離す）の挙動（Q4 の確認）。
- 既存の `Phase8Step1ProfileSwitchCharacterizationTests`・`Phase7bW1ProfileUntriggerPreservationTests` を、旧・新の両経路にする準備をする。

### W2: 新経路の実装

**実装（2026-10-19）**: `IPendingUntriggerQuery`（新設）、`ITriggerDispatchEngine.ProcessUntrigger`（追加）、`TriggerDispatchEngine`（合図 `Pending`［束縛］を、立ち上がりの `Execute` の前に設定、`ProcessUntrigger`［自動の判定、成立しない間は `BeingTriggered` を下げる、成立したら合図を消してからロックの外で `Stop`］、`OnDeviceDisconnected` で消す）、`ProfileSwitchAction.CanExecute`（エンジンの合図と `IMapping` の合図の両方を見る）、`ActionBindingBuilder`（`ProfileUntriggerMigrated` を削除、`uTrigger` つきは束縛なし、復帰つきは `stopOnRelease: false`）、`Mapping`（旧ループの後で `ProcessUntrigger`）。テスト: `Phase8Step75bW2ProfileUntriggerEngineTests`（新設）、`Phase8Step5W1BuilderTests`・`Phase8Step75aW3ProfileSwitchTests`・`Phase8Step6W3SlotLifecycleTests` の更新。
**Q4 の決定**: W1 の報告のみのテスト（`ReleaseBeforeTheApplyCompletes_IsReported`）の出力は未確認のまま、二重の復帰を避ける安全側（`stopOnRelease: false`）で実装した。本番では、適用が状態を作り直すため、離したエッジの `Stop` は起きない（設計書 §15.1）。W2 の結果と合わせて確認する。

`UntriggerSignal`、`ProcessUntrigger`（`ITriggerDispatchEngine` に追加。実装を持つ他のクラス［テストの偽物］の更新を含む）、`IPendingUntriggerQuery`、`ProfileSwitchAction.CanExecute` の更新、`ActionBindingBuilder` の更新（`ProfileUntriggerMigrated` の削除、`uTrigger` つきは束縛なし）、`Mapping` の呼び出し（旧ループの後）、`OnDeviceDisconnected` で合図を消す。`Phase8Step5W1BuilderTests` の Automatic の検査を、束縛ありに戻す。W1 の旧経路の検査を、旧・新の両経路にする。ログ: 復帰のログは、旧経路が `UNTRIGGER check` の Debug を毎フレーム出す（旧経路と同じ文言・レベルを、合図があるときだけ出す。割り当てを増やさない）。

### W3: スイッチ（実質は変更なし）と性能

Profile は 7.5a で既定の一覧に入っているため、一覧は変えない。変わるのは、復帰つきの Profile の束縛が作られること。ロールバックは、`ServiceRegistration` の Profile の 1 行を外す（7.5a と同じ。切替も旧経路に戻る）。割り当て（合図なし・あり）と性能を、旧・新で比べる。

### W4: 実機確認 U-33 と報告書

## 4. 質問（推奨は A）

- **Q1（範囲）**: A（推奨）: 自動の復帰（`uTrigger` なし）だけをエンジンへ移す。B: `uTrigger` つきも移す（UI から作れず、実機で確認できない経路のために、`controls == ucontrols` の待ちなどを移すことになる）。
- **Q2（手動の解除トリガー）**: A（推奨）: `uTrigger` を持つ Profile は、旧経路のまま残す。Step8 の旧コード削除のときに、廃止（機能廃止のため、ユーザーの明示的な承認が必要）か、エンジンへの移行かを、改めて決める。UI の復活は別件。B: 今回、エンジンへ移す（Q1＝B と同じ）。C: 今、廃止する（規約 2.2 により、明示的な承認が必要。実行側と Actions.xml の読み書きの削除を伴う）。
- **Q3（合図の持ち方）**: A（推奨）: 切替前の束縛（`IActionBinding`）を持つ。復帰の `Stop` は、その束縛の出力を使う（適用で登録簿が差し替わっても、`ProfileSwitchAction` の参照は残る）。B: `SpecialAction` と添字だけを持ち、復帰時に新しい `ProfileSwitchAction` を作る（旧経路と同じ。復帰のたびに割り当てが出る）。
- **Q4（離したエッジの `Stop`）**: A（推奨）: W1 の特性化の結果を見て決める。旧経路で適用が終わる前に離しても復帰しないなら、`Untrigger != None` の Profile は `stopOnRelease: false`（復帰は `ProcessUntrigger` だけが担う）。旧経路が復帰するなら、同じにする。B: 何もしない（`StopOnRelease` が既定の true のまま）。
- **Q5（`IMapping` の合図のメンバー）**: A（推奨）: `CaptureProfileUntrigger`／`RestoreProfileUntrigger` は残す（`uTrigger` つきの旧経路が使う）。Step8 で、旧経路の削除と一緒に扱う。B: 今、削除する（旧経路の復帰が動かなくなる）。
- **Q6（実機確認）**: A（推奨）: U-33 をユーザーが実機で確認し、合格してから 7.6 へ進む。確認の前に、起動ログの「built at … (commit …)」でビルドを確認する。B: 7.6 のあとでまとめて確認する。

## 5. リスクと対応

- **合図が適用で消える／残り続ける**: 消えると復帰しない（Phase7b W1 の不具合）。残り続けると毎フレーム復帰を繰り返す。対応: 合図を消してから `Stop` を呼ぶ（旧経路と同じ順）。適用で消えない・切断で消えるテスト。復帰を 1 回しか呼ばないテスト。
- **復帰の前に別の切替**: 連鎖の防止（エンジンの合図も見る）。W1 で旧・新を比べる。
- **一時プロファイルの残留**: 切断で合図が消え、一時プロファイルが残る問題。切断の後始末（`ProfileSlotApplier.ReleaseSlot` → `OnDeviceDisconnected`）で合図を消す。実機確認（抜き差し）に含める。
- **フレームの打ち切り**: 切替のフレームで復帰の確認を飛ばす（`Mapping` が `EndFrame` で `return`）。W1・W2 で、旧・新を比べる。
- **ロールバック**: `ServiceRegistration` の Profile の 1 行を外す。復帰も切替も、旧経路に戻る。

## 6. 完了条件

- [ ] W1〜W3 のビルド・テストが成功（ユーザーが実行）。復帰の特性化が、旧・新の両経路で、期待値を変えずに成功。
- [ ] 全テスト・ラチェットの一致検査・1 ファイル 1 型のガードが成功。割り当て（新 ≦ 旧）。`IMapping`・`IOutputContext` のメンバーが増えていない。
- [ ] `uTrigger` つきの Profile が束縛にならず、旧経路が復帰すること。ロールバックのテストが成功。
- [ ] 実機確認 U-33 が合格（または先送りの記録）。
- [ ] `Phase8-Step7-Completion-Report.md`、`Phase8-Status.md`、チェックリストの更新。

## 7. 次

7.6（実機を要する種別。GyroCalibrate、DisconnectBT、BatteryCheck、ステアリングの校正。1 種別ずつ）。
