# Phase8-Step7 計画書: 種別ごとの切替（1 種別＝1 PR）。共通の手順と、最初の PR（7.1 Button）

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Unified-Trigger-Dispatch-Plan.md`（§5 の Step7）、`Phase8-Step2-Design.md` §23〜§28（並行期間のしくみ、PR 一覧、テストの流用、ロールバック）、`Phase8-Step2-Intent-Decisions.md`、`Phase8-Step1-Trigger-Spec-Ledger.md`、`Phase8-Step4/5/6-Completion-Report.md`  
状態: **Q1〜Q8 を推奨どおり承認（2026-10-19）。7.1-W1〜W3 完了（ビルド・テスト成功）。7.1-W4 の実機確認 U-27 の結果待ち。経過報告書: `Phase8-Step7-Completion-Report.md`**

---

## 1. 目的と位置づけ

- Step3〜Step6 で、統一ディスパッチの部品（束縛、エンジン、登録簿、ビルダー、適用・切断・接続の口）を、**本番の挙動を変えずに**用意した。移行対象（`BindingMigrationOptions.MigratedTypes`）が空なので、新経路は動いていない。
- Step7 は、**種別ごとに `MigratedTypes` へ足して、旧経路から新経路（エンジン）へ切り替える**。**本番の挙動に触れる最初の Step** になる。1 種別＝1 PR（Step2-Design §24）。
- 旧コードは **Step8 まで残す**（§26 の修正）。Step7 の各 PR は、スイッチ（`MigratedTypes`）だけで切り替え、ロールバックは一覧から外すだけにする。旧コードの削除は、全種別の実機確認が済んでから Step8 でまとめて行う。
- この計画書は、(1) 全 PR に共通の手順と完了条件、(2) 最初の PR である **7.1 Button** の詳細、を決める。7.2 以降は、各 PR の開始時に、短い個別の計画書（`Phase8-Step7-N-Plan.md`）を作る（Q1）。

## 2. 現状の調査（この計画書のための事前確認）

### 2.1 すでにあるもの（Step3〜Step6）

| 部品 | 状態 |
|---|---|
| `Mapping` の旧ループのフック（`MapCustomAction` の各アクション先頭の `Owns`／`ProcessOwned`、Stage3 の `Owns`）、`MapCustom` 内の `ReleaseSuppressed`、Stage3 直後の `ApplyFrameOutputs` | 配線済み（Step4）。移行対象が空なので素通し |
| `TriggerDispatchEngine`（エッジ検出、Fresh-press ガード、実行中のブロック、排他グループ、抑制の集合、`RequestEndFrame`）、`ButtonOutputAction`、`FrameOutput` | 実装・単体テスト済み（Step4）。偽の束縛では検証済みだが、**実物の束縛を実プロファイルから作って旧経路と比べる検証は、Button の予行［`Phase8Step4W4WiringTests`］だけ** |
| `ActionBindingBuilder`（Key／Button／Macro／Program／Profile の構成）と、適用・切断・接続での登録簿の更新 | 実装・結合テスト済み（Step5・6） |
| `OnProfileApplied`（適用時の初期化の合図） | **未接続**（Step5 Q4、Step6 Q7。Step7.1 で判断） |

### 2.2 Button の旧経路（7.1 の対象）

- **Stage3**（`Mapping` の「Synchronous Button SpecialAction handling」、2684 行付近）が、毎フレーム `IsSpecialActionTriggered` で成立を判定し、`ButtonSpecialActionOutput.ResolveOutputActive`（Press／Toggle）で出力の有無を決め、X360 のボタン、マウス系（`currentClicks`）、タッチパッドクリックを出力する。
- `MapCustomAction` 側の Button の分岐は、トリガーのボタンの抑制や出力の消去の記録だけを行い、出力そのものは Stage3 が担う（Phase6-Step10-3d）。
- 判定が 2 か所にある（台帳 §8 項目 2: `IsSpecialActionTriggered` と `MapCustomAction`）。新経路は 1 回の評価（`ComboTriggerInputAction`）に統一される。`TriggerInput` は `Mapping` の評価と同じ結果を返すことを検証済み（Step3 の `TriggerInputTests`）。
- 現行の Press は Fresh-press ガードを見ない（Toggle の反転だけが見る）。**維持**（Intent 項目 1）。`BindingPolicy.RespectsFreshPressGuard` で表現済み（Press が false／Toggle が true）。
- Stage3 の TRACE ログ（「Trigger detected for Button SA」「Trigger no longer active」、出力の組み合わせ）は、規約 2.3 により、新経路でも同じ文言とレベルで出す必要がある（Step4 完了報告書 §5）。**まだ引き継いでいない**。

### 2.3 切替で本番の挙動が変わりうる点（Button）

| 点 | 現状 | 7.1 での扱い |
|---|---|---|
| 判定の複製 | Stage3 と `MapCustomAction` が別々に判定 | 1 回の評価に統一（結果は同じはず。**特性化テストで確認**） |
| TRACE ログ | Stage3 が出す | 新経路で同じ文言・レベルを出す（Q6） |
| `OnProfileApplied` | 未接続。適用時に抑制の集合を消さない | 差が出るか調べ、出る場合だけ接続（Q5） |
| 実行の順序 | 旧ループの位置でフックし、フレームの打ち切りの順序を保つ | 順序の回帰テストで確認 |
| 性能 | `MapCustom` 30 アクションで 48.76 µs（未操作）／109.05 µs（押下） | 許容 +5% 以内（Button を移行した状態で再計測） |

## 3. 共通の手順（全 PR）

各 PR（7.1〜7.7）は、次の順で進める。完了条件は、Step2-Design §24 の共通項に、本 Step で決めた事項を加えたもの。

1. **特性化テストを両経路で走らせる**: 該当種別の既存の特性化テスト（`Phase7cW2…`、`Phase8Step1…`、`ButtonSpecialActionOutputTests`、`MappingSpecialActionSuppressionTests` ほか）を `[Theory]` にし、旧経路（`migrated: false`）と新経路（`migrated: true`）の両方で流す。**期待値は同じ**。食い違ったら設計か実装を直す。期待値を変えてよいのは、Intent-Decisions で承認した差だけ（Q3）。
2. **足りない部分を埋める**: その種別に特有の、まだ新経路にない処理（ログ、出力の細部）。
3. **スイッチを入れる**: `ServiceRegistration` の `BindingMigrationOptions` の既定の一覧に、種別を 1 行足す（Q2）。
4. **性能の確認**: `Phase8Step4MapCustomBenchmarkTests` にその種別を移行した状態の測定を足し、許容（+5%）と割り当て 0 を確認する。
5. **実機確認**: `Phase6-Step11-RealDevice-Verification-Checklist.md` に項目（U 番号）を足し、**ユーザーが実機で確認する**。合格してから次の PR へ進む（Q7）。
6. **ロールバック**: 一覧から種別を外すだけで旧経路に戻る。PR の説明に明記する。
7. 全テスト・ラチェットの一致検査・1 ファイル 1 型のガードが成功。完了報告書に追記（個別の計画書がある場合は、その完了節）。

## 4. PR 一覧と範囲（Step2-Design §24 を具体化）

| PR | 種別 | 主な内容 | 実機確認 | 計画書 |
|---|---|---|---|---|
| **7.1** | Button（Press／Toggle） | `ButtonOutputAction`＋エンジン。Stage3 と `MapCustomAction` の Button の分岐を新経路へ。TRACE ログの引き継ぎ | ボタンの割り当て（Press、Toggle、マウス系、タッチパッドクリック） | **本計画書 §5** |
| 7.2 | Key（Press／Toggle／リピート） | `KeyOutputAction`。Key の `uTrigger`（解除用ボタン）は現行で無視されるため移植しない（Intent 項目 4） | Press の押下・解放、Toggle、長押しのリピート | 開始時に作成 |
| 7.3 | Macro | `MacroAction`＋`IHeldOutputAction`（Alt-Tab）、RunOnRelease（`InvertOnRelease`）、`synchronized` | 単発、RunOnRelease、Alt-Tab | 開始時に作成 |
| 7.4 | Program | `LaunchProcessAction`、`IsExecuting`（`BlocksWhileExecuting`） | 起動（`$hidden` 含む）、押しっぱなしで繰り返さない | 開始時に作成 |
| 7.5 | Profile ＋ 復帰（最大のリスク） | `ProfileSwitchAction`、復帰の合図を `Mapping.deviceRuntime` からエンジンへ、`OnProfileApplied` の連携、`RequestEndFrame`、`RestoreFromAction` の扱い（Step6 完了報告書 §5）。合図の移行と適用との連携は、必要なら 2 PR に分ける | 切替、Unload on trigger release、解除トリガー、一時プロファイルの復帰、連続切替の安全弁 | 開始時に作成 |
| 7.6 | 実機を要する種別（GyroCalibrate、DisconnectBT、BatteryCheck、ステアリング較正）。1 種別ずつ | 実機確認が必須（機材がなければ、ガードの分岐までの検証とリスク受容の記録） | 開始時に作成 |
| 7.7 | XboxGameDVR／MultiAction | `MultiTapInputAction`（タップ、長押し、ダブルタップ。状態の置き場を決める） | 開始時に作成 |
| 7.8 | **［7.8d は、2026-10-20 のユーザー決定で「シフト機能の廃止」に変更。7.8e は Extras のみ［Lightbar マクロは、保存されない機能のため廃止］。7.8a〜e 完了、7.8f-1［パッド］・7.8f-2［マウス・Toggle］・7.8f-3［マウスの移動・アナログのホイール］完了＝7.8 のすべてが完了］** Controls タブ（単一ボタンの割り当て） | `ProcessControlSettingAction`（約 400 行）を同じエンジンに載せる。**範囲が大きく、別の設計書［`Phase8-Step2b-ControlsTab-Design.md`］を 7.7 の後に作る**（設計書は作成済み［2026-10-20］。Q1〜Q9 を承認済み［2026-10-20。Q4＝B、Q8＝B、Q9＝A、他は推奨］。7.8a〜f に分ける） | 別途 | 別の設計書 |

## 5. 7.1 Button の作業内容

### 5.1 7.1-W1 テストの足場と両経路の特性化

**W1 の実装（2026-10-19。ビルド・テスト成功）**

- `SpecialActionTestScope(bool migrated = false)`: `migrated` が真なら、`BindingMigrationOptions.MigratedTypes` に Button を足し、`Add` のたびに `IActionBindingBuilder.Build` → `IActionRegistry.Replace`（本番の `PublishBindings` と同じ経路）で束縛を登録する。終了時に、移行の一覧と登録簿を元へ戻す。
- `Phase7cW2SpecialActionTriggerCharacterizationTests` の Button 関連 9 本（Press の出力、AND 条件、Press は Fresh-press ガードを見ない、抑制の追加・解除、Fresh-press の解除条件 4 本）を `[Theory]`（`migrated` が偽・真）にした。**期待値は変えていない**。Key／Profile／Program／Stage3 の並行変更のテストは、旧経路のまま。
- `Phase8Step7W1ButtonDualPathTests`（新設。9 本）: 同じ入力の列を両経路に流し、フレームごとの出力（Cross、L1／R1 の素通し、タッチパッドクリック、マウス左クリックの数）が一致することを確かめる。Press、Toggle、Toggle＋ガード中、AND 条件、タッチパッドクリック、マウス、読めない details、長い遅延、スコープの復元。
- `ButtonSpecialActionOutputTests` は、判定関数（`ResolveOutputActive`）と `Mapping` のソースの構造を見るテストで、経路に依存しないため、変更しない。
- 食い違いが出た場合は、本計画書 §2.3 の表に記録し、実装を直す（意図の差ならユーザーに相談）。
- W1 の結果: 失敗は 1 本のみ（私の期待の誤り）。**`delayTime` は Button の出力を遅らせない**（旧・新とも、Press は成立中すぐ出力する）。テストを実測の固定に直した。新旧の出力は、全シナリオで一致した。

- `SpecialActionTestScope` に、種別の集合を指定するパラメーター（`migrated`）を足す。`true` のときは、`BindingMigrationOptions.MigratedTypes` に種別を足し、スコープが登録した特殊アクションから `ActionBindingBuilder.Build` を呼んで登録簿へ `Replace` する（本番の `PublishBindings` と同じ経路）。終了時に元へ戻す。
- Button に関する既存のテスト（`ButtonSpecialActionOutputTests` ほか、Press／Toggle、AND 条件、抑制、Fresh-press、遅延、順序）を `[Theory]` にして、両経路で流す。**期待値は変えない**。
- 食い違いがあれば、本計画書 §2.3 の表に記録し、直すか（実装）、ユーザーに相談する（意図の差）。

### 5.2 7.1-W2 新経路の不足分

**W2 の実装（2026-10-19。ビルド・テスト成功）**

- **TRACE ログの引き継ぎ（Q6＝A）**: `ButtonOutputAction.ApplyFrame` が、旧 Stage3 と `Mapping.LogSpecialActionTrace` の 7 種のログ（立ち上がり／立ち下がり、出力のたびの組み合わせ、立ち上がりの「Trigger detected for SA」「maps to」「Trigger keys」「output combo」）を、同じ文言・レベル・順序で出す。`AppLogger.IsTraceEnabled` が偽のときは文字列を作らない。口として、`IFrameOutputTrace`（`FrameOutput` が出力の組み合わせを返す）を新設。既存のインターフェース（`IFrameOutput`／`IOutputContext`）は変えていない。
  - **既存の不具合の記録**: 旧 `LogSpecialActionTrace` の「Trigger keys for SA」は、状態を渡さずに `getBoolSpecialActionMapping` を呼ぶため例外（握りつぶし）となり、常に `<none>` を記録している。ログの文言を変えない方針により、新経路も同じ `<none>` を出す。成立中のトリガーの一覧を出す改善は、別途ユーザーの承認を得て行う候補。
  - テスト: `Phase8Step7W2ButtonTraceLogTests`（旧経路と新経路の NLog の行を比較。Press、Toggle、マウス・タッチパッド、読めない details、Trace 無効時）。
- **`OnProfileApplied`（Q5＝A）**: 旧経路では、プロファイル適用の後始末（`HandleDeviceDisconnect` を含む）は抑制の集合を消さない。`Phase8Step7W2ApplyAndOrderTests` で、適用を再現して（`SimulateProfileApply`: `ClearDeviceState` → `PreallocateForProfileApply` → `PublishBindings`）、押している最中・離している最中の適用で新旧の出力と抑制の集合を比べる。**差が出なければ、`OnProfileApplied` は接続しない**（結果で確定する。差が出たら、抑制の集合を消さない形で接続する）。
- **順序と二重ディスパッチ**: 2 つの Button の新旧比較（マウスの左クリックのカウンターが 1 フレームにつき 1 回）、移行済みの Button を旧 Stage3 が処理しない（`lastButtonTriggerState` に残らない）ことを確認。

- **TRACE ログの引き継ぎ**（Q6）: 立ち上がり・立ち下がりのログ、出力の組み合わせのログを、`ButtonOutputAction`（または `FrameOutput`）が、旧 Stage3 と同じ文言・レベルで出す。ログが無効のときに文字列を作らない（割り当て 0）。
- **`OnProfileApplied`**（Q5）: プロファイルの適用の前後で、新旧の出力（抑制の集合、Toggle の状態、遅延の基準）を比べる特性化テストを足し、差が出る場合だけ接続する。接続するときは、抑制の集合を消さない形にして、現行の挙動を保つ。
- 順序の回帰テスト（フレームの打ち切りの前後のアクション）と、二重ディスパッチの検出（Step2-Design §23.4）を足す。

### 5.3 7.1-W3 スイッチと性能

**W3 の実装（2026-10-19。ビルド・テスト成功）**

- `ServiceRegistration`: `BindingMigrationOptions` をファクトリー登録にし、既定の一覧に Button を **1 行**足した（`migration.MigratedTypes.Add(SpecialAction.ActionTypeId.Button); // 7.1 Button`）。`BindingMigrationOptions` の既定のコンストラクターは空のまま（単体テストの前提を保つ）。
- 既存テストの更新（Q3 に基づく唯一の変更）: `Phase8Step4W1ContractTests.Host_RegistersTheRegistryAndTheOptions_…` の「ホストの既定は空」の検査を、「既定は Button のみ」に変えた（設計どおりの変更。他の旧経路のテストは `SpecialActionTestScope` が一覧を空にして実行するため、期待値は不変）。
- `Phase8Step7W3ButtonSwitchTests`（3 本）: ホストの既定が Button のみ／ソースに `MigratedTypes.Add` の行が 1 行／その行を外すと束縛が作られず旧経路（Stage3）が Cross を出力する（ロールバック）。
- 性能: `Phase8Step4MapCustomBenchmarkTests.MapCustom_ButtonMigrated_PerFrameTime_IsReported`（Button を移行した状態の MapCustom の費用を出力。旧経路の測定と並べて、+5% 以内を確認する）。割り当ては、`Phase8Step4W4WiringTests.TheNewPath_DoesNotAddAllocationsToTheFrame`（新経路 ≦ 旧経路）が検査する。
- 実機確認: チェックリストに **U-27** を追加（W4 でユーザーが実施）。

- `ServiceRegistration` の既定の一覧に `Button` を足す（1 行）。
- ベンチマークに、Button を移行した状態の測定を足す。許容は +5% 以内。割り当て 0。
- 既存の全テストが、新経路の既定で成功すること。

### 5.4 7.1-W4 実機確認と報告

- チェックリストに U 番号の項目を足す。ユーザーが実機で確認する。
- 完了報告書（Step7 の経過報告として、各 PR を追記していく `Phase8-Step7-Completion-Report.md`）を作る。モデル図の更新案は、Step9 でまとめる。

## 6. 質問（推奨は A）

- **Q1（計画書の粒度）**: A（推奨）: 本計画書は共通の手順と 7.1 の詳細を決め、7.2 以降は各 PR の開始時に短い個別の計画書を作る（各種別の旧コードを、その時点で読んでから書く。Profile のような大きな PR の詳細を、今決めても外れやすい）。B: 7.1〜7.7 すべての詳細を、今書く。
- **Q2（スイッチの置き場所と、実機での切替）**: A（推奨）: `ServiceRegistration` の既定の一覧に、PR ごとに 1 行足す。ロールバックは 1 行を外す（ユーザーのビルドで切り替わる）。B: 加えて、隠しの設定（設定ファイルやコマンドライン引数）で、実行時に切り替えられるようにする（実機で新旧を比べやすいが、保存形式・引数の仕様が増え、削除（Step8）の対象も増える）。
- **Q3（既存テストの期待値）**: A（推奨）: 既存の特性化テストの期待値は、変えない。新経路で食い違ったら、実装か設計を直す。変えてよいのは、Intent-Decisions で承認した差だけで、変えるときは PR の説明に明記して、ユーザーの承認を得る。B: 新経路の挙動を正として、期待値を新経路に合わせて更新する（不具合を取り込む危険）。
- **Q4（旧コードの扱い）**: A（推奨）: Step8 まで残す（§26 の修正）。Step7 の各 PR は、スイッチだけで切り替える。ロールバックが 1 行で済む。B: PR ごとに旧コードを削除する（Strangler Fig の運用だが、ロールバックにコードの復元が要る）。
- **Q5（`OnProfileApplied` の接続）**: A（推奨）: 7.1 の W2 で、プロファイル適用の前後の新旧の差を特性化テストで調べ、差が出る場合だけ接続する。接続するときも、抑制の集合は消さない（現行の挙動を保つ）。B: 7.1 で無条件に接続し、抑制の集合の消去を含めて受け入れる（適用の瞬間にボタンを押していた場合に、挙動が変わりうる）。C: 7.5（Profile）まで接続しない（Profile の復帰と一緒に設計するが、それまで Button の適用時の初期化が `Replace` だけに頼る）。
- **Q6（Stage3 の TRACE ログ）**: A（推奨）: 新経路でも、同じ文言・レベルで出す（規約 2.3）。ログが無効のときは文字列を作らない。B: 新経路では省く（ログの追跡性が下がる。規約に反する）。
- **Q7（実機確認の時期）**: A（推奨）: PR ごとに、ユーザーが実機で確認し、合格してから次の PR に進む。不合格なら、スイッチを外して原因を調べる。B: 全種別を切り替えた後に、まとめて確認する（問題の帰属が分かりにくい。Phase7c の方針に反する）。
- **Q8（7.6 と 7.8 の扱い）**: A（推奨）: 7.6 は実機が要る種別を 1 種別ずつ別の PR とし、機材がなければ、ガードの分岐までの検証とリスク受容の記録で進める。7.8（Controls タブ）は範囲が大きいため、7.7 の後に別の設計書（Step2b）を作ってから進める。B: 7.6 をまとめて 1 PR にする。C: 7.8 を Step7 に含めず、Phase8 の範囲外として Phase9 以降にする（D2＝B1 の「Controls タブとスペシャルアクションの統一」が未完になる）。

## 7. リスクと対応

- **本番の挙動が変わる最初の Step**: 7.1 は影響範囲が小さい（Button は非同期・外部の副作用なし）種別から始める。ロールバックは 1 行。実機確認を PR ごとに入れる（Q7）。
- **二重ディスパッチ・順序の崩れ**: `Owns` が真のアクションは旧ループの本体を通らない（Step4 で検証済み）。順序・二重ディスパッチの検出テストを足す（5.2）。
- **新旧の状態の食い違い**: `ActionInstanceState` は新旧で同じオブジェクトを使う（Step2-Design §23.3）。種別ごとに片方の経路しか触らない。
- **性能**: Button を移行した状態で再計測する。許容を超えたら、評価の重複排除などを検討するまで、スイッチを入れない。
- **ログの欠落**: 規約 2.3。5.2 で引き継ぐ。
- **Profile（7.5）の規模**: 開始時に、合図の移行と適用との連携を 2 PR に分けるかを、個別の計画書で決める。
- **実機がない種別（7.6）**: Phase8 の完了を、これらの確認の完了にかけるかは、Step9 で決める。

## 8. 完了条件（7.1）

- [ ] 7.1-W1〜W4 のビルド・テストが成功する（ユーザーが実行）。Button の特性化テストが、旧経路・新経路の両方で、期待値を変えずに成功する。
- [ ] 既存の全テスト・ラチェットの一致検査・1 ファイル 1 型のガードが成功。ベンチマークが許容（+5%）と割り当て 0 を満たす。
- [ ] `MigratedTypes` の既定に `Button` が入り、新経路で Button が動く。ログの文言・レベルが、旧 Stage3 と同じ。
- [ ] 実機確認の項目が合格する（または先送りの記録）。
- [ ] ロールバック（一覧から外す）で旧経路に戻ることを、テストで確認している。
- [ ] 経過報告書（`Phase8-Step7-Completion-Report.md`）、`Phase8-Status.md` の更新。

## 9. 次の Step

- 7.2 Key（短い個別の計画書を、7.1 の実機確認の合格後に作成）。7.1〜7.8 の完了後に、Phase8-Step8（旧コードの削除）。
