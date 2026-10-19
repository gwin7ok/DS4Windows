# Phase8-Step7-5 計画書: Profile 種別の切替（プロファイルの切替と復帰）。最大のリスク

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Step7-Plan.md`（§4 の 7.5）、`Phase8-Step7-4-Plan.md`（進め方）、`Phase8-Step2-Design.md`（§6・§9・§17）、`Phase8-Step2-Intent-Decisions.md`（項目 5: 切替の後はフレームを打ち切る）、`Phase8-Step1-Trigger-Spec-Ledger.md`（Profile）、`Phase8-Step6-Completion-Report.md` §5、`Phase8-Step7-Completion-Report.md`  
状態: **Q1〜Q6 を推奨どおり承認（2026-10-19）。7.5a-W1・W2 完了（ビルド・テスト成功）、W3 完了（実機確認 U-32 が合格。2026-10-19）**

---

## 1. 目的と、なぜ最大のリスクか

Profile 種別の特殊アクション（ボタンの組み合わせで、プロファイルを切り替え、離したとき（または解除トリガーで）元へ戻す）を、旧ループから、統一ディスパッチ（エンジン）へ切り替える。7.1〜7.4 と同じ運用（`ServiceRegistration` の既定の一覧に `Profile` を足す。外すと戻る。旧コードは Step8 まで残す）。

**7.1〜7.4 と違う点（リスク）**:
1. 切替は、**自分自身の束縛を差し替える**（プロファイルを適用すると、登録簿が新しいプロファイルの束縛に置き換わる）。
2. 復帰の合図（`UntriggerAction`）は、**切替の前のプロファイルの特殊アクションを指したまま、適用をまたいで残る**。復帰は、その合図が毎フレーム見張る。
3. 旧ループは、切替のあと、**フレームの残りを打ち切る**（`return`）。
4. 切替の直前に、トリガーのボタンの通常の割り当て（キー、マクロ）のキーを離す副作用がある。
5. 一時プロファイルの間は、復帰の合図が残っている限り、別の Profile 切替を始めない（連鎖の防止）。

## 2. 現状の調査

### 2.1 旧経路

**切替（`MapCustomAction` の Profile の分岐。4564〜4684 行付近）**。成立中で、ガード・実行中・連鎖の条件を満たすとき:
- 条件: `BeingTriggered` が偽、**かつ** Fresh-press ガードが解除済み、**かつ** `IsExecuting` が偽、**かつ**（一時プロファイルでない、または復帰の合図がない、または合図が Profile でない）。
- ログ: Trace の立ち上がり 4 行、Debug 「SpecialAction PROFILE: Triggered …」「SpecialAction PROFILE: beingTriggered=…, useTempProfile=…」、`LogActionDoneCountOnTrigger("Profile")`。
- `IsExecuting = true`。**復帰の合図の設定**（`uTrigger` がある、または `automaticUntrigger` のとき）: `UntriggerAction = action`、`UntriggerIndex = index`、前のプロファイル名の記録（`prevProfileName`／`prevProfileWasTemporary`。**書くだけで、読む場所はない**）。
- トリガーのボタンの通常の割り当てがキーまたはマクロなら、そのキーを離す（`VirtualKBM.PerformKeyRelease`）。
- `DispatchInputEdge` → `ActionManager` → `ProfileSwitchActionAdapter` → `IProfileSwitcher.SwitchProfile` → `IProfileApplicationService.ApplyFromAction`（`Task.Run` で、HaltReporting の内側で適用）。
- **`return`**（`MapCustomAction` を終わる。フレームの残りの特殊アクションと、後述の復帰の確認は、そのフレームでは行わない）。`finally` で `IsExecuting = false`。

**復帰（`MapCustomAction` の後段。5316〜5398 行付近）**: `UntriggerAction` があるとき、毎フレーム:
- 自動（`automaticUntrigger`）: トリガーのうち**どれか 1 つでも離れた**ら成立。手動（`uTrigger`）: 解除トリガーが**すべて押された**ら成立。
- 成立のとき、（トリガーと解除トリガーが同じなら `BeingTriggered` が偽であることも条件で）: 解除トリガーごとに `LogActionDoneCountOnTrigger("UntriggerProfile")`、`DispatchOrSetBeingTriggered(true)`、解除トリガーの通常の割り当てのキーを離す。**合図を消し**（適用が `ClearDeviceState` を通り、合図を退避・復元するため。Phase7b）、`ProfileSwitchAction.Stop` → `IProfileSwitcher.RestoreProfile` → `RestoreFromAction`（**`OlderProfilePath`＝スロットの通常のプロファイル**へ適用）。
- 成立しないフレームは、`DispatchOrSetBeingTriggered(false)`。

**合図の保存**: `Mapping.deviceRuntime[device].UntriggerAction`／`UntriggerIndex`。プロファイルの適用（`ClearDeviceState` → `HandleDeviceDisconnect`）で消えないよう、`CaptureProfileUntrigger`／`RestoreProfileUntrigger`（`IMapping`）で退避・復元する。物理的な切断（`Mapping.HandleDeviceDisconnect` を `ActionManager` を通さずに呼ぶ経路）では消える。

### 2.2 新経路（Step3〜Step6 で用意済み）

- `ActionBindingBuilder`: Profile → `TriggerBinding(ComboTriggerInputAction, ProfileSwitchAction, BindingPolicy(blocksWhileExecuting: true, untrigger: Automatic／Buttons／None, untriggerButtons))`。
- `ProfileSwitchAction`: `Execute` → `IProfileSwitcher.SwitchProfile`、`Stop` → `RestoreProfile`。**`RequestEndFrame` を呼ばない。ログ、キーを離す副作用、連鎖の条件、復帰の合図の設定は、まだない。**
- エンジン: **復帰（`UntriggerPolicy`）の処理は、まだない**（エンジンのコメント: Step7.5 で足す）。`OnDeviceDisconnected` が合図を消す設計（Step6）。登録簿の差し替え（適用）は、エンジンの状態（抑制の集合など）を消さない。

## 3. 方針（分割）

**Profile を 2 つの PR に分ける**（Q1）。復帰は、切替より大きく、先に切替だけを確かめる方が安全なため。

| PR | 内容 | 新経路で処理する範囲 |
|---|---|---|
| **7.5a 切替** | 切替の経路（条件、ログ、キーの解放、フレームの打ち切り、連鎖の防止）をエンジンへ | **復帰を持たない** Profile（`uTrigger` なし、`automaticUntrigger` なし）だけ。復帰つきの Profile は、ビルダーが束縛を作らず、旧経路のまま |
| **7.5b 復帰** | 復帰の合図と毎フレームの確認をエンジンへ | 復帰つきの Profile も新経路。旧経路の復帰の確認は、合図が空のため素通し |

7.5a の時点で、旧経路が復帰つきの Profile を処理し続けるため、旧経路の復帰の動作は変わらない。

### 7.5a の作業

- **W1 の実装（2026-10-19。ビルド・テスト成功）**: `SpecialActionTestScope(migrated: true)` が Profile も移行。`Phase8Step75aW1ProfileSwitchDualPathTests`（新設）。前半（旧・新の両経路）: 切替・復帰の回数、ガード中、実行中、AND 条件、遅延、押している最中の適用。後半（**旧経路だけ**。新経路に、まだ無い項目の固定。W2 で、新経路の検査を足す）: 切替のあとフレームの残りが処理されない、トリガーのボタンのキーの解放、連鎖の防止。
- **W1 特性化**: `Phase8Step1ProfileSwitchCharacterizationTests` の切替の検査（成立で `ApplyFromAction` が 1 回、直接の `ApplyProfile` は呼ばれない、復帰なしでは合図を設定しない）を、旧・新の両経路にする。新規: 保持中は再切替しない、ガード中・実行中・連鎖（一時プロファイル＋復帰の合図が Profile）で切り替えない、切替のあとでフレームが打ち切られる（同じフレームの後ろの特殊アクションが処理されない）、トリガーのボタンの通常の割り当て（キー、マクロ）のキーが離される、押している最中のプロファイル適用。観察は、`RecordingProfileApplicationService`（`InstallProfileRecorder`）と、`PerformKeyRelease` の記録。
- **W2 の実装（2026-10-19。ビルド・テスト成功）**: `IGatedOutputAction`（新設。立ち上がりの実行の直前の条件の口。偽なら、エンジンは状態を変えず素通し）、`TriggerDispatchEngine`（立ち上がりで条件を確認）、`ProfileSwitchAction`（エンジン用のコンストラクター: 連鎖の防止、ログ［Trace 4 行、Debug 3 行］、トリガーのボタンのキーの解放、`RequestEndFrame`）、`ActionBindingBuilder`（復帰を持つ Profile は束縛を作らない。`ProfileUntriggerMigrated = false`。`Func<IMapping>` を省略可で受ける）。テスト: `Phase8Step75aW1…` の後半を旧・新の両経路に、`Phase8Step75aW2ProfileLogTests`、`Phase8Step5W1BuilderTests` の Profile の検査を更新（復帰つきは束縛なし）。
- **W2 新経路の不足分**: `ProfileSwitchAction` に、エンジン用のコンストラクターで、(1) ログ（Trace 4 行、Debug 3 行）、(2) トリガーのボタンのキーの解放（`IProfileSettingsService` と出力の KBM［`ctx.OutputHandler`］）、(3) `ctx.RequestEndFrame()`（Intent 5）を足す。連鎖の防止は、**任意の実行条件の口**（`IGatedOutputAction`。`CanExecute(ctx)` が偽なら、エンジンは、`IsExecuting`・`BeingTriggered` を変えずに素通しする）として足し、`ProfileSwitchAction` が、`GetUseTempProfile` と `IMapping.CaptureProfileUntrigger(device).Action` で、旧経路と同じ条件を評価する（Q2）。ビルダーは、復帰なしの Profile だけ束縛を作る。
- **W3**: `ServiceRegistration` の既定の一覧に `Profile` を足す（7.5a。復帰つきは旧経路のまま）。ロールバック、割り当て、性能。
- **W4**: 実機確認 U-32（切替）。報告書。

### 補足（2026-10-19）: 手動の解除トリガー（uTrigger）は、UI から作れない

- 2025-12-15 のコミット a7277da9（Key の Toggle 不具合の修正）で、Profile 用の「Set Unload Trigger」ボタンも削除された（Profile 側の理由の記録なし。巻き添えの可能性が高い）。実行側（`Mapping` の uTrigger）と Actions.xml の読み書き（UnloadTrigger）は残っている。ユーザーに確認し、「別ボタンで元へ戻す」使い方は使っていない。
- 影響: 実機確認 U-32 から解除トリガーの手順を外した（7 項目）。手動 uTrigger の Profile は UI から新規作成できないため、**7.5b の対象は事実上 AutomaticUntrigger（Unload on trigger release）のみ**。
- 今は UI を戻さず、実行側の uTrigger も触らない（UI の復活は別件として記録）。
- **7.5b の計画書で選ぶ事項（手動 uTrigger を持つ Profile の扱い）**: (案1・推奨) 旧経路のまま残し、Step8 の旧コード削除時に改めて扱いを決める。(案2) エンジンへ移す。(案3) 廃止する。ただし機能廃止（規約 2.2）に当たるため、ユーザーの明示的な承認が必要。

### 7.5b の作業（7.5a の実機確認の合格後に、個別の計画書を作る。骨子）

- 復帰の合図の置き場所は、**エンジン**（Q6＝A。適用で消えず、切断で消える）。束縛が差し替わっても、合図は、切替前の束縛（その `SpecialAction` と方針）を指したまま残す。
- 復帰の確認の入口（`ProcessUntrigger`）を、`Mapping` の復帰の確認と同じ位置（`MapCustomAction` の後段）から呼ぶ。切替のフレームでは、旧経路と同じく、確認が飛ぶ（`return`）。
- 復帰の実行は、`RestoreProfile` を呼ぶ前に合図を消す（旧経路と同じ順序）。`OlderProfilePath` は変更しない。
- `Phase7bW1ProfileUntriggerPreservationTests` と同じ観点（適用をまたぐ、切断で消える）を、エンジンの合図でテストする。

## 4. 質問（推奨は A）

- **Q1（分割）**: A（推奨）: 7.5a（切替。復帰を持たない Profile だけ）と 7.5b（復帰）の 2 つの PR に分け、それぞれ実機確認をする。B: 1 つの PR で、切替と復帰を一度に移す（最大のリスクを一度に負う。不具合の原因の切り分けが難しい）。
- **Q2（連鎖の防止）**: 一時プロファイルの間、復帰の合図が Profile なら、別の Profile 切替を始めない条件。A（推奨）: `IGatedOutputAction`（任意の実行条件の口）を足し、`ProfileSwitchAction` が `GetUseTempProfile` と `IMapping.CaptureProfileUntrigger`（既存のメンバー）で評価する（`IMapping` のメンバー数は 10 のまま）。B: 条件を省く（連鎖の防止が消える。機能の低下のため、規約 2.2 により不可）。
- **Q3（書くだけのフィールド）**: A（推奨）: `prevProfileName`／`prevProfileWasTemporary`（書くだけで、読む場所がない）は、新経路で移さない（旧経路はそのまま残す。Step8 で、旧コードと一緒に削除を提案する）。B: 新経路でも書く（読まれない書き込みが残る）。
- **Q4（ログと副作用）**: A（推奨）: Trace・Debug のログと、トリガーのボタンの通常の割り当てのキーの解放を、旧経路と同じ文言・順序・条件で、新経路でも行う（規約 2.2・2.3）。B: 省く（規約に反する）。
- **Q5（実機確認）**: A（推奨）: 7.5a と 7.5b のそれぞれで、ユーザーが実機で確認し、合格してから次へ進む。確認の前に、起動ログの「built at … (commit …)」で、ビルドを確認する。B: 7.5b のあとで、まとめて確認する。
- **Q6（復帰の合図の置き場所。7.5b）**: A（推奨）: エンジンが持つ（Step6 の設計どおり。適用で消えず、切断で消える）。B: `Mapping` に残し、`IMapping` に合図を設定するメンバーを足す（`IMapping` のメンバー数が増え、ガードテストと設計の更新が要る。層の方向も不自然）。

## 5. リスクと対応

- **切替の最中の登録簿の差し替え**: 切替が、非同期（`Task.Run`）で、適用を行い、登録簿を差し替える。エンジンは、デバイスごとのロックの下で、次の処理の先頭で、登録簿のスナップショットの差し替えを検出して、実行時の配列を作り直す（7.1〜7.4 の適用・再接続の実機確認で、確認済みの経路）。
- **フレームの打ち切り**: `RequestEndFrame` → `ProcessOwned` が `EndFrame` を返し、`Mapping` が `return`（Step4 で配線・テスト済み）。W1 で、「後ろの特殊アクションが、そのフレームで処理されない」ことを、旧・新で比べる。
- **ロールバック**: `ServiceRegistration` の Profile の 1 行を外す（7.5a・7.5b とも）。
- **Phase7b の復帰の不具合（U-23）**: 復帰の合図が適用で消える問題は、Phase7b で是正済み。7.5b で、エンジンの合図でも、同じ観点をテストする。

## 6. 完了条件（7.5a）

- [ ] W1〜W4 のビルド・テストが成功（ユーザーが実行）。切替の特性化が、旧経路・新経路の両方で、期待値を変えずに成功。
- [ ] 全テスト・ラチェットの一致検査・1 ファイル 1 型のガードが成功。割り当て（新 ≦ 旧）。`IMapping` のメンバーが 10 のまま。
- [ ] 既定の一覧に Profile が入り、復帰つきの Profile は旧経路のまま。ロールバックのテストが成功。
- [ ] 実機確認 U-32 が合格（または先送りの記録）。
- [ ] `Phase8-Step7-Completion-Report.md`、`Phase8-Status.md` の更新。

## 7. 次

7.5b 復帰（7.5a の合格後に、個別の計画書 `Phase8-Step7-5b-Plan.md` を作成）。その後、7.6（実機を要する種別）。
