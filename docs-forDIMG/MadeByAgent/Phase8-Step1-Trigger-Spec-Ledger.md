# Phase8-Step1 W1: トリガー判定・実行の仕様台帳（現行の挙動の棚卸し）

作成日: 2026-10-19  
対象: `DS4Windows/DS4Control/Mapping.cs`（7,602 行。2026-10-19 時点）  
上位: `Phase8-Step1-Plan.md` 項目 A  
性格: **現行の挙動の記録**。製品コードは変更していない。「意図は不明」と書いた点は、Phase8 の Step2 で意図を決める。行番号は着手時点の参考値（メソッド名と条件の記述で場所を特定すること）。

---

## 1. フレームの流れ（毎入力レポート）

1. `MapCustom`（`Mapping.cs:2283` 付近）が、`GetProfileActionCount(device) > 0` または一時プロファイル使用中のとき、`MapCustomAction` を呼ぶ（`async void`）。`EnsureActionDoneInitialized` は完了済みの `Task` を返すため、最初の実際の `await` までは同期的に走る。
2. `MapCustomAction`（4223 行付近）の冒頭で、アクション名を `ToArray()` で複製し（Phase7c W1 の是正も同じ守り方）、抑制中のボタンのうち離されたものを解除する（4263〜4281 行）。
3. アクション名ごとに、`GetProfileAction`／`GetProfileActionIndexOf` で定義を引き、次の順に処理する。
   1. 添字が範囲外、または定義が null ならスキップ。
   2. `typeID == None` ではない場合のみ、トリガー成立の判定（§2）。
   3. 解除トリガーの判定（Key のみ。§3）。
   4. Fresh-press ガードの更新（§4。全型、成立／未成立の両方）。
   5. 成立（`triggeractivated`）なら、トリガーのボタンを出力から消し（`ResetToDefaultValue`）、抑制対象に加え、種別ごとの実行（§5）。
   6. 未成立なら、種別ごとの解除（§5）。
4. ループの後で、`deviceRuntime[device].UntriggerAction`（Profile の復帰の合図）を処理する（5316 行付近。§3）。
5. Stage3（`MapCustom` の 2670〜2826 行）は、`MapCustomAction` とは別に、Button 種別だけを毎フレーム走査して出力を作る（§5 の Button）。

## 2. トリガー成立の判定（`triggeractivated`）

判定の分岐は、次の 4 通り（4306〜4397 行）。

- **A. `delayTime > 0`**: 全トリガーが押されている間（`subtriggeractivated`）、`nowAction[device]` を現在時刻にし、`oldnowAction[device] + delayTime` 以上になったら成立。押されていないときは、直近 100ms＋`DebouncingMs` より古ければ `oldnowAction[device]` を現在時刻に更新する。
- **B. タップ判定（`triggerToBeTapped`）**: `typeID == None`、トリガーが 1 つ、かつそのボタンの割り当てが既定（`IsDefault`）のとき。`oldnowKeyAct[device] == MinValue` なら、押された時刻を記録して不成立。記録済みなら、離されたときに、250ms＋`DebouncingMs` 以内ならば成立（成立の前に 3ms の `Task.Delay`）。ただし、この分岐に入る前の条件 `!(typeID == None || index < 0)` により、`typeID == None` のアクションは判定ブロック全体に入らない（タップ判定は `typeID == None` を要するため、実質的に到達しない）。→ **到達不能のコードに見える。Step2 で確認する**。
- **C. 標準**: すべてのトリガーが押されていれば成立（AND）。Macro で `pressRelease && firstTouch` のときは、成立を反転する（離されたときに成立）。
- 押下の判定（`getBoolSpecialActionMapping`、6734 行付近）: ボタンはそのまま、軸は 128±55 を超えたら、トリガー（L2/R2）は 100 を超えたら、タッチはボタン、スワイプはフラグ、ジャイロは `IsUsingGyroForControls` のときに ±67 を超えたら。

補助: `IsSpecialActionTriggered`（6648 行付近）は、Stage3（Button）が使う、同じ判定の複製。違い:
- `delayTime` の分岐で `nowAction`／`oldnowAction` を更新しない（`MapCustomAction` は更新する）。
- Key の `uTrigger` を見る（`MapCustomAction` は Key で `uTriggerCount = 0` にして無視する）。ただし `IsSpecialActionTriggered` を呼ぶのは Button だけなので、現状は影響しない。
- タップ判定の分岐がない。
- → 同じ仕様の複製が 2 つあり、挙動が一致していない。統一ディスパッチでは 1 つにする（D2）。

## 3. 解除（Untrigger）

- **Key の解除トリガー**: `uTrigger` は Key では無視される（`uTriggerCount = 0`、4403〜4406 行。「単一トリガーのトグル」を優先）。ただし、`uTriggerCount > 0` の分岐のコード（5100 行付近）は残っている。→ Key の `uTrigger` が実質デッドコードかどうかは Step2 で確認する。
- **Key の離し（Press）**: 不成立のとき、`uTrigger.Count == 0 && BeingTriggered && UntriggerIndex == index` なら、`DispatchOrSetBeingTriggered(false)`、`UntriggerIndex = -1`、`TryDispatchSATriggerReleased`（4949 行付近）。Toggle は `UntriggerIndex` を登録しない（4801〜4804 行）ため、この分岐に入らない。
- **Profile の復帰**: 実行時に `uTrigger.Count > 0 || automaticUntrigger` なら、`deviceRuntime[device].UntriggerAction`／`UntriggerIndex` を設定し、復帰先 `prevProfileName`／`prevProfileWasTemporary` を記録する（4583〜4592 行）。ループの後（5316 行以降）で、`automaticUntrigger` ならトリガーのどれか 1 つが離された時点、そうでなければ `uTrigger` がすべて押された時点で、復帰する。復帰は、`UntriggerAction`／`UntriggerIndex` を先に消してから `ProfileSwitchAction.Stop` を呼ぶ（Phase7b W1。復帰の適用が `ClearDeviceState` を通り、合図が復元されて毎フレーム復帰が繰り返されるのを防ぐ）。`action.controls == action.ucontrols` のときは、`BeingTriggered` が false のときだけ復帰する。
- **Profile 以外の `UntriggerAction`**: 5392 行の else で、`DispatchOrSetBeingTriggered(false)`。ただし `UntriggerAction` は Profile のときだけ設定されるため、この else は実質的に通らない。→ 要確認。
- **Macro の離し**: `pressRelease` の Macro は、`firstTouch` を使って、離したときに 1 回実行する（4726〜4767 行）。

## 4. 安全弁（Fresh-press ガード、`IsExecuting`）

### 4.1 ガードの更新（全型、`typeID != None` のとき。4423〜4453 行）
- 不成立になった最初のフレームで `UntriggeredTimestampTicks = Environment.TickCount64` を記録し、`FreshPressReleaseHoldMs`（80ms）以上連続で不成立なら `RequiresFreshPressAfterReset = false`。成立している間は `UntriggeredTimestampTicks = 0` に戻す。

### 4.2 ガードを見る型と見ない型
| 型 | Fresh-press ガード | `IsExecuting` |
|---|---|---|
| Macro、Key、DisconnectBT、BatteryCheck、SASteeringWheelEmulationCalibrate | 見る（4494〜4508 行。ブロック中は `actionFound = true` のみで何もしない） | 見ない |
| Button（`MapCustomAction` 側） | 4494〜4508 行の対象。ただし出力は Stage3 | 見ない |
| Button（Stage3） | **Press は見ない**。Toggle の立ち上がりの反転だけが見る（`ButtonSpecialActionOutput.ResolveOutputActive`） | 見ない |
| Program | 見る（4516 行） | 見る（4517 行。実行中は新規実行しない） |
| Profile | 見る（4566 行） | 見る（4567 行） |
| GyroCalibrate | 見る（4942 行付近） | 見ない（瞬時に完了する同期処理のため） |
| XboxGameDVR／MultiAction | 見ない | 見ない |

### 4.3 二重発火の防止（共通）
- `GetBeingTriggered`（`ActionManager.IsBeingTriggered`）が true の間は、新規実行しない（Program、Profile、Macro、GyroCalibrate、Key）。
- `DispatchInputEdge`（648 行付近）が、`(アクション名のハッシュ, device)` ごとの前回の入力状態を `prevInputEstablished` に持ち、状態が変わらなければ `false` を返す（立ち上がり・立ち下がりのエッジ検出）。

## 5. 種別ごとの仕様

### Key
- 成立: ガード通過後、`DispatchOrSetBeingTriggered(true)`、Toggle でなければ `UntriggerIndex = index`、`deviceState[device].keyPresses` の登録、`ActionManager.DispatchTriggerEdge`（`IsEstablished = true`、`OutputHandler = VirtualKBM`）、`repeatCount++`。
- 離し（Press）: §3。Toggle は次の成立で切り替わる（`KeyAction`／`ActionManager.SetToggledOn`）。
- `Mapping` は `IsToggledOn` を変更しない（`KeyAction` が管理）。

### Button
- `MapCustomAction` 側は、`actionFound = true` のみ（出力もディスパッチもしない。Phase6-Step10-3d）。
- Stage3（`MapCustom` 2670〜2826 行）が、毎フレーム、Button 種別のアクションを走査して、`ResolveOutputActive`（Press＝成立中のみ、Toggle＝立ち上がりで反転）が true なら出力する（`details` の `X360Controls` に応じて、タッチパッドクリック、マウスボタン/ホイールのカウンター、ボタン配列）。前回の成立状態は `lastButtonTriggerState`（キー: `device:actionname`）に持つ。
- 注意: Stage3 は、`MapCustomAction` の `ResetToDefaultValue` によるトリガーのボタンの消去の**後**に出力を書く。

### Macro
- 通常（`!pressRelease`）: `BeingTriggered` が false のとき、`DispatchInputEdge`（`MacroAction`／`IMacroPlayer`）。未処理（`handled == false`）なら `SetBeingTriggeredIf(true)` と `PlayMacro` の直接呼び出し（フォールバック）。`BeingTriggered` が true で、`RepeatMacro` でなければ `EndMacro`。
- `pressRelease`: 離したときに 1 回、同様に実行。
- K3: `PlayMacro` の二重実行防止は `MacroRuntimeState.TryBeginDispatch`／`EndDispatch`（Phase7-Step9）。

### Program
- ガード通過、`BeingTriggered` が false、`IsExecuting` が false のとき、`IsExecuting = true` にして、`DispatchInputEdge`（`LaunchProcessAction`）。未処理なら `LaunchProcessAction` の直接呼び出し。`finally` で `IsExecuting = false`。
- 仕様: `extra` の `$hidden` のときだけ非表示、それ以外は `UseShellExecute = true`（Phase6-Step9b F06 で是正済み）。

### Profile
- ガード通過、`BeingTriggered` が false、`IsExecuting` が false、かつ「一時プロファイル使用中でない、または `UntriggerAction` が Profile でない」とき。`IsExecuting = true`、`UntriggerAction` の設定（§3）、トリガーのボタンの割り当てのキー/マクロの解放、`DispatchInputEdge`（`ProfileSwitchAction`／`IProfileSwitcher`）。未処理なら `Task.Run` の中で `HaltReportingRunAction` を使って `ApplyProfile`（`MappingAction`）し、`uTrigger.Count == 0 && !automaticUntrigger` なら、同じ `controls` を持つ次のアクションを `DispatchOrSetBeingTriggered(true)`（チェーン）。
- **`return` がある**: Profile の実行後（4672 行）、`MapCustomAction` 全体から戻り、そのフレームの残りのアクションと復帰の判定（5316 行以降）は処理されない。→ 意図は不明（Step2 で確認）。
- 安全弁: Fresh-press ガード、`IsExecuting`（§4.2）。旧 250ms のデバウンスは、2026-09-16 に Fresh-press ガードと `IsExecuting` に置き換え済み。

### DisconnectBT
- ガード通過後、BT 接続で同期・非充電なら `DisconnectBT()`、`ReleaseActionKeys`、そして `return`（Profile と同様に、フレームの残りを処理しない）。SONYWA は `pressRelease = true` にして、離したときの処理で切断する。

### BatteryCheck、SASteeringWheelEmulationCalibrate、GyroCalibrate
- BatteryCheck: 初回に通知、ライトバーの強制色とフェード。離したら `fadetimer`/`forcelight` を戻す。
- ステアリング較正: `WheelRecalibrateActiveState` を 0 → 1 → （2 のとき）3 に進める（3 秒の間隔）。
- GyroCalibrate: `SixAxis.ResetContinuousCalibration()`。
- いずれも `DispatchOrSetBeingTriggered(true)`。実機の `DS4Device` を要する。

### XboxGameDVR／MultiAction
- ガードを見ない。トリガー 1 つ目の押下／解放のエッジ（前フレームの `getPreviousStateRef`）から、タップ（150ms＋`DebouncingMs`）、長押し（500ms＋`DebouncingMs`）、ダブルタップを判定し、`PlayMacro` を直接呼ぶ（`ActionManager` を通らない）。`BeingTriggered` を設定しない。

### その他（else）
- `DispatchOrSetBeingTriggered(false)`。

## 6. 抑制（`suppressedTriggerButtons`）

- 追加: トリガー成立のたびに、構成するすべてのトリガーのボタンを追加（4461〜4472 行）。
- 解除: フレームの冒頭で、離されたボタンだけを個別に外す（4263〜4281 行。ボタン単位。トリガー全体の成立・解除とは独立。Phase5-Step14 Issue8-1(3)）。
- 利用: `CheckForSpecialActionSuppression`（6720 行付近）が、通常の割り当て（`MapCustom`）で、そのボタンを抑制するかを返す。

## 7. 出力・状態の経路

- `ActionManager`（静的）／`DefaultActionManager` の `DispatchTriggerEdge`／`DispatchTriggerEstablished`／`DispatchTriggerReleased` → `Actions.Action.OnTrigger`／`OnRelease`（系統 B）。
- Key／Button の `TryDispatchSATriggerEstablished`（3228 行付近）: `DispatchInputEdge` → 未処理なら `KeyButtonControllerRegistry.GetOrCreateForAction` と再度 `DispatchInputEdge` → 未処理なら `ActionManager.DispatchTriggerEstablished`。「フォールバックは抑制する」とコメントされた経路（コントローラーがなければ `false`）。
- `BeingTriggered`、`IsToggledOn`、`RequiresFreshPressAfterReset`、`UntriggeredTimestampTicks`、`IsExecuting` は `ActionInstanceState`（`ActionManager.GetStateFor(action, device)`）にある。

## 8. Step2 で意図を決める点（現行の挙動に疑問があるもの）

1. Stage3 の Button の Press が Fresh-press ガードを見ない（Toggle だけが見る）。
2. `IsSpecialActionTriggered` と `MapCustomAction` の判定の複製と、その不一致（§2）。
3. タップ判定の分岐（B）が、条件上、到達不能に見える。
4. Key の `uTrigger` を無視する（`uTriggerCount = 0`）一方で、その分岐のコードが残っている。
5. Profile／DisconnectBT の `return` が、フレームの残りの処理を飛ばす。
6. 5392 行の else（`UntriggerAction` が Profile 以外のとき）が実質的に通らない。
7. XboxGameDVR／MultiAction が、統一経路（`ActionManager`）を通らず、`PlayMacro` を直接呼ぶ。
8. Macro のフォールバック（`handled == false` のとき `PlayMacro` を直接呼ぶ）の有無（DI 未登録時のためのもの。現行は登録済み）。
9. `FreshPressReleaseHoldMs`（80ms）とタップ判定（250ms）、`MultiAction`（150ms/500ms）の時間定数が別々。
10. **【2026-10-19 是正済み: Phase8-Step1 W2b。`new DateTime[MaxControllerCount]`、テスト `Phase8Step1SlotLimitTests`】`nowAction`／`oldnowAction` が 4 要素の配列**（`Mapping.cs:695-696`）。`delayTime > 0` の特殊アクションが、デバイス 4〜7 で `IndexOutOfRangeException` になる（`MapCustomAction` の外側の `catch` に捕まり、`MapCustomAction exception` が記録されてそのフレームの特殊アクションが打ち切られる）。`oldnowKeyAct` や `deviceRuntime` は 8 要素（`MaxControllerCount`）。拡張スロット（5 台目以降）の実装時の取りこぼしとみられる（Phase6-Step9b F05 の 4 スロット固定と同種）。上流の元の実装との比較と是正は、承認を得て行う。特性化テストでは固定しない（不具合を固定しないため）。
11. **Profile 種別の「離したときの OnRelease → 復帰」が、プロファイル適用による状態の掃除に依存している**（Phase8-Step1 W2 のテストで判明、2026-10-19）。トリガーを離すと、`DispatchOrSetBeingTriggered(false)` → `ActionManager`（`DefaultActionManager`）の `DispatchTriggerReleased` → `ProfileSwitchActionAdapter.OnRelease` → `ProfileSwitchAction.Stop` → `RestoreFromAction`（`OlderProfilePath` へ復帰）という経路がある。ただし本番では、切替の適用（`CompleteProfileApplication`）が `ActionManager.ClearAllEntries`／`ClearDeviceState` でアクションの状態を作り直し、`BeingTriggered` が false に戻るため、離したときのエッジが発火せず、この経路は通らない（Phase7b で「Unload on trigger release」の復帰が `Mapping` の復帰処理［5316 行付近］の 1 回だけになる理由の 1 つ）。→ **統一ディスパッチは、この暗黙の依存を明示的な仕様にする必要がある**（Profile の「離したら復帰」は、`AutomaticUntrigger`／`uTrigger` の指定があるときだけ。指定がないときは離しても復帰しない）。特性化テストの `RecordingProfileApplicationService` は、本番と同じ掃除を再現する（`SimulateStateReset`、既定 true）。

## 9. 現行のテストとの対応（2026-10-19、Phase7c W2 後）

- 固定済み（`Phase7cW2SpecialActionTriggerCharacterizationTests`）: Button の Press、AND 条件、抑制の追加と解除、Fresh-press の更新、Profile／Program のガード中・実行中のブロック、Key の Press／Toggle の状態遷移、Stage3 の並行変更。
- 未固定（`Phase8-Step1-Plan.md` 項目 B で書き足す）: `uTrigger`／`delayTime`／`pressRelease` と復帰の通し、チェーンの条件、`shiftTrigger` と `MapCustom` の Key 出力、`ApplyProfile` 後の掃除の順序、Macro 分岐、XboxGameDVR／MultiAction（Q1 の範囲外にする案を検討）、Profile／DisconnectBT の `return`。
