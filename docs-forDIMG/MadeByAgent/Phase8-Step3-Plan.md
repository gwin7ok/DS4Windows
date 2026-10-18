# Phase8-Step3 計画書: 判定と束縛の型の新設（配線なし）

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Unified-Trigger-Dispatch-Plan.md`（§5 Step3）、`Phase8-Step2-Design.md`（§3〜§8、§14〜§15、§22）、`Phase8-Step2-Completion-Report.md` §4  
状態: **実施中（Q1〜Q7 を推奨どおり承認、2026-10-19）。W1 完了、W2 実装済み［ビルド・テスト待ち］**

---

## 1. 目的と位置づけ

- Step2 で承認された設計のうち、**判定側と束縛の型**を実装する。**本番の挙動は変えない**。新しい型は、`Mapping` にも DI にも**配線しない**（単体テストからのみ使う）。配線は Step4 以降。
- 作るもの: `ITriggerInput`／`TriggerInput`（`DelayClock` を含む）、`TriggerState`、改訂した `IInputAction`、`ComboTriggerInputAction`、`BindingPolicy`／`UntriggerPolicy`、`TriggerBinding`、改訂した `IActionBinding`。書き直すもの: `KeyOutputAction`。削除するもの: `KeyInputAction`、`ITrigger`／`SingleButtonTrigger`。
- 範囲外: エンジン（Step4）、`ButtonOutputAction`／`IFrameOutput`／`IHeldOutputAction`（Step4）、登録簿・ビルダー（Step5）、`Mapping` との接続（Step4 のフックから）、`MultiTapInputAction`（Step7.7）。
- 配置と規約: すべて `DS4Windows/Actions/`（R3）、名前空間 `DS4Windows.Actions`（R6）、1 ファイル 1 型、ファイル名＝型名、コンストラクター注入、静的な窓口を新設しない（§2.1）。ラチェット（`Global.*` ほか）の基準値は変わらない見込み（新設の型は静的参照を持たない）。

## 2. 作業内容

### 2.1 契約と値型（W1）
- `ITriggerInput`（`Device`、`IsActive`、`WasActive`、`Delay`、`UtcNow`、`TickCount64`）、`DelayClock`（`Now`／`Old`。`nowAction`／`oldnowAction` の 1 デバイス分）、`TriggerState`（読み取り専用の構造体。`IsEstablished`、`Gesture`）。
- `IInputAction` の改訂: `Evaluate(ITriggerInput) : TriggerState`、`Triggers`、`Reset()`。現行の `ITriggerContext Evaluate(DS4State)` を置き換える（呼び出し元は 0 件。実装の `KeyInputAction` は同じ Wave で削除）。
- `BindingPolicy`（`RespectsFreshPressGuard`、`BlocksWhileExecuting`、`Untrigger`、`ResetsTriggerOutput`、`StopOnRelease`、`ExclusionGroup`）、`UntriggerPolicy`（`None`／`Automatic`／`Buttons`）、`TriggerBinding`（不変）。
- `IActionBinding` の改訂: `OnTriggered`／`OnReleased` を削除し、`Policy`、`Source`、`Index` を追加。**`KeyActionBinding`（`IActionBinding` の既存の実装）と、テストの 3 ファイル（`ActionControllerTests` ほか。`IActionBinding` を引数に取る）を、新しい契約に合わせる**。

### 2.2 `TriggerInput`（W2）
- `getBoolSpecialActionMapping` と同じ意味の `IsActive`（ボタン／軸 128±55／L2・R2 は 100 超／タッチ／スワイプ／ジャイロ［±67。ジャイロ制御のときだけ］）。ジャイロ制御の判定は、`IProfileSettingsService.GyroOutputMode[device] == GyroOutMode.Controls`（現行の `Mapping.IsUsingGyroForControls`）をコンストラクターで受ける。
- `WasActive`: 前フレームの表を、最初の要求時にだけ作る（遅延）。`UtcNow`／`TickCount64` は差し替え可能にする（時刻の提供元をコンストラクターで受ける。既定は実時間）。
- 更新: `Update(DS4State, DS4StateExposed, Mouse, DS4StateFieldMapping, 前フレームの状態)`。割り当て 0。

### 2.3 `ComboTriggerInputAction`（W3）
- 1 つ以上のボタンの AND 条件。オプション: `DelayTime`（`delayTime`。`input.Delay` の共有の基準時刻を使い、現行の更新の規則を再現する）、`InvertOnRelease`（Macro の `RunOnRelease`。準備→離して成立）。
- 現行の `MapCustomAction`（分岐 A［遅延］と C［標準］）の判定を正とする（`Phase8-Step2-Intent-Decisions.md` 項目 2）。タップ判定（分岐 B）は移植しない（項目 3）。

### 2.4 `KeyOutputAction` の書き直しと削除（W4）
- `KeyAction` へ委譲する形に書き直す（Design §7）。論理キー＝`SpecialAction.details`、ネイティブキー＝現行の `Mapping` の Key 経路と同じ求め方、`useScan`＝`keyType.HasFlag(ScanCode)`（現行どおり下流には渡さない）。
- 削除: `KeyInputAction`、`ITrigger`／`SingleButtonTrigger`、およびそれらへの `TODO(Phase8-Step3)` コメント。`Phase8-Step1-Orphan-Decisions.md` の判断どおり。

### 2.5 性能の基盤とまとめ（W5〜W6）
- 割り当て 0 のテスト（`Evaluate`、`IsActive`、`Update`）と、簡易な処理時間の比較の基盤（固定の入力で `Evaluate` を N 回呼び、1 回あたりの時間と割り当てを出力。Step7 の各 PR の前後で使う）。
- 完了報告書。

## 3. テスト

- `TriggerInput.IsActive`: 全 `DS4Controls` について、現行の `Mapping.getBoolSpecialActionMapping`（private）と**同じ結果**になること（リフレクションでの呼び出し。テスト専用）。境界値（軸 72／73／183／184 付近、L2／R2 の 100／101、ジャイロ ±67、ジャイロ制御の有無）。
- `ComboTriggerInputAction`: AND 条件（一部だけ→不成立）、遅延（`DelayClock` の共有。2 つの遅延つきアクションで基準時刻を共有する現行の挙動）、`InvertOnRelease`（準備→離して成立。`Phase8Step1MacroBranchCharacterizationTests` の期待）、時刻の差し替えで実時間のスリープなし。
- `KeyOutputAction`: 系統 B（`KeyActionAdapter` 経由）と、`MockVirtualKBM` で同じ押下・解放の列になること（Press、Toggle、離した場合）。論理キー・ネイティブキーの求め方が、`Mapping` の Key 経路の値と一致すること。
- `BindingPolicy`／`TriggerBinding`: 不変であること、種別ごとの既定の組み合わせ（Design §6）を作るファクトリーの検査は Step5。
- 既存の全テスト、ラチェットの一致検査、1 ファイル 1 型のガードが成功すること。

## 4. Wave 構成（案）

- W1: 契約と値型（`ITriggerInput`、`DelayClock`、`TriggerState`、`IInputAction` の改訂、`BindingPolicy`、`UntriggerPolicy`、`TriggerBinding`、`IActionBinding` の改訂、`KeyActionBinding` とテストの追従、`KeyInputAction` の削除）。
- W2: `TriggerInput` ＋ テスト（現行の判定との一致）。
- W3: `ComboTriggerInputAction` ＋ テスト。
- W4: `KeyOutputAction` の書き直し ＋ 一致テスト。`ITrigger`／`SingleButtonTrigger` の削除。
- W5: 割り当て 0 のテストと処理時間の比較の基盤。
- W6: 完了報告書、`Phase8-Status.md` の更新。

## 5. 決定してほしい点（質問と推奨）

- **Q1（`TriggerState.Gesture` を Step3 に含めるか）**: A（推奨）: 含める（値は常に 0。`MultiTapInputAction` が Step7.7 で使う）。契約を後から変えないため。B: Step7.7 で足す。
- **Q2（Macro の `RunOnRelease` の「準備」状態の置き場）**: A（推奨）: `ComboTriggerInputAction` の内部状態にする（現行の `SpecialAction.firstTouch` は使わない。`firstTouch` の利用は `Mapping.cs` のみ）。B: `SpecialAction.firstTouch` を引き続き使う。理由: 新旧の並行期間（Step7）は、1 つのアクションが片方の経路でしか処理されないため、状態を分けても食い違わない。`SpecialAction` の状態を契約から切り離せる。
- **Q3（`TriggerInput` の依存）**: A（推奨）: コンストラクターで `IProfileSettingsService` と時刻の提供元（`Func<DateTime>`／`Func<long>`）を受ける。B: ジャイロ制御の判定を `Func<int,bool>` で受ける。理由: A は現行の `IsUsingGyroForControls` と同じ参照先で、テストでは設定を差し替えられる。
- **Q4（`IsActive` の一致検証の方法）**: A（推奨）: `Mapping` の private メソッドをリフレクションで呼んで比較する（テスト専用）。B: 期待値を表で持つ（リフレクションなし）。理由: A は現行の挙動と直接比較でき、`Mapping` が変わっても検出できる。ただし Step8 で `Mapping` の該当メソッドを消す際は、B（期待値の表）に切り替える。
- **Q5（処理時間の比較の扱い）**: A（推奨）: 結果を出力するだけで、テストの失敗条件にしない（環境依存のため）。割り当て 0 は厳密に検査する。B: 閾値を超えたら失敗にする。
- **Q6（命名）**: A（推奨）: Step2 の設計のとおり（`ComboTriggerInputAction`、`TriggerBinding`、`BindingPolicy`）。B: 変更したい名前があれば指定。
- **Q7（`KeyOutputAction` のネイティブキーの求め方）**: A（推奨）: `IProfileSettingsService.OutputKBMMapping.GetRealEventKey(key)`（現行の `Mapping` の Key 経路と同じ）をコンストラクター注入で受ける。B: `ISyntheticDispatcher.ResolveNativeKey`（現行の `KeyAction` のコンストラクターが使う）。W4 のテストで両者が一致するかを確認し、食い違う場合はこの計画書に追記して再相談する。

## 6. リスクと対応

- 判定ロジックの移植で、現行との差が出る: 現行のメソッドとの直接比較（Q4）と、既存の特性化テスト（Phase7c・Step1 の 38 件）で検出する。新しい型は、Step4 まで本番に配線しないため、不一致があっても本番は影響を受けない。
- `IActionBinding` の改訂で、既存のコード・テストが壊れる: 実装は `KeyActionBinding` のみ。利用は `KeyAction`／`KeyOutputAction`／コントローラーのアダプターとテスト 3 ファイルで、いずれも型の引数の受け渡し。W1 で追従させ、ビルド・テストで確認する。
- 時間定数・遅延の挙動の再現: 時刻を注入して、実時間のスリープなしで検証する（Step1 で苦労した不安定さの回避）。
- `KeyOutputAction` の書き直しで、`ScanCode` の扱いが変わる: 現行どおり、ScanCode を下流に渡さない（別件として記録済み）。変えない。

## 7. 完了条件

- [ ] W1〜W5 のビルド・テストが成功する（ユーザーが実行）。ラチェットの一致検査、1 ファイル 1 型のガードも成功する。
- [ ] 新しい型は、`Mapping`・DI のどちらにも配線されていない（本番の挙動が変わっていない）。
- [ ] `KeyInputAction`、`ITrigger`／`SingleButtonTrigger` が削除されている。
- [ ] 完了報告書。

## 8. 次の Step

- Phase8-Step4（エンジンと出力の型、`Mapping` への空配線、抑制の集合のエンジンへの一本化）。
