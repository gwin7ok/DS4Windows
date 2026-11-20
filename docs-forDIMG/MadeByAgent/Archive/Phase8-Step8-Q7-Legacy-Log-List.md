# Phase8 Step8 — Q7 旧経路の診断ログの削除一覧（承認済み［2026-10-20］。8b で実施）

作成日: 2026-10-20
対象: `DS4Windows/DS4Control/Mapping.cs`（8a 完了後の状態。行番号は現在のもの）
根拠: `Phase8-Step8-Plan.md` Q7＝A（旧機構の動作を説明するだけの診断ログは、コードと一緒に削除する。新経路が同じ事象を同じ文言で出すログは変えない）。規約 2.3 により、**この一覧の承認を得てから 8b で消す**。

## 1. 調べ方

`Mapping.cs` の `AppLogger.*` 呼び出しをすべて洗い出し、(a) 8b で削除するコード（SA の旧枝、Stage3、`DispatchOrSetBeingTriggered` 系など）の中にあるか、(b) 新経路（`DS4Windows/Actions/` とエンジン）が同じ文言を出しているか、を確かめた。(b) が「ある」ものは、ユーザーに見えるログの内容は変わらない。「ない」ものは、旧経路だけの診断なので、削除で消える。

## 2. 削除する（旧経路だけ。新経路は出さない）― 承認をお願いします

| ID | 場所（行） | レベル | 文言（要約） | 備考 |
|---|---|---|---|---|
| L1 | 2196 | Debug | `Mapping SUMMARY (5s): device= evaluations= setBeingTriggeredCalls=` | 5 秒ごとの集計。カウンター `mappingEvalSinceLast`／`setBeingTriggeredCallsSinceLast`／`mappingLastSummaryUtcTicks` も削除 |
| L2 | 564 | Trace | `Mapping.SetBeingTriggered (input-edge): index= name= ... dispatched= prevInput=` | `SetBeingTriggered` と一緒に削除 |
| L3 | 633 | Trace | `Mapping.DispatchInputEdge: name= ... dispatched= prevInput=` | `DispatchInputEdge` と一緒に削除 |
| L4 | 3080 | Debug | `SpecialAction KEY synthetic-press delegated to KeyButtonActionController` | `TryDispatchSATriggerEstablished` と一緒に削除 |
| L5 | 3084 | Debug | `SpecialAction KEY/BUTTON press delegated to KeyButtonActionController` | 同上 |
| L6 | 3093 | Trace | `... press dispatch: no KeyButtonActionController available (fallback suppressed)` | 同上 |
| L7 | 3098 | Trace | `... press dispatch failed: {ex}` | 同上 |
| L8 | 3164 | Debug | `SpecialAction KEY synthetic-release delegated to KeyButtonActionController` | `TryDispatchSATriggerReleased` と一緒に削除 |
| L9 | 3168 | Debug | `... release delegated to KeyButtonActionController` | 同上 |
| L10 | 3178 | Trace | `... release dispatch: no KeyButtonActionController available (fallback suppressed)` | 同上 |
| L11 | 3184 | Trace | `... release dispatch failed: {ex}` | 同上 |
| L12 | 4178 | Trace | `Mapping dispatch to ActionManager failed for Key action: {ex}` | Key の旧枝 |
| L13 | 4186 | Debug | `SpecialAction KEY direct PerformKeyPressAlt` | Key の旧枝の直接送信（フォールバック） |
| L14 | 4191 | Debug | `SpecialAction KEY direct PerformKeyPress` | 同上 |
| L15 | 4366 | Trace | `SpecialAction KEY untrigger dispatch failed: {ex}` | Key の旧枝 |
| L16 | 4441 | Trace | `PressActionController.OnTriggerOff failed: {ex}` | Key／Button の旧枝 |
| L17 | 4456, 4462 | Trace | `SpecialAction BUTTON untrigger dispatch failed: {ex}`（2 か所） | Button の旧枝 |
| L18 | 3589 | **GUI** | `BeingTriggered initialization failed after all retries. Skipping Special Actions for safety.` | 画面のログに出る。旧経路の `EnsureActionDoneInitialized`（待ち合わせ）専用。8b で待ち合わせごと不要になるため削除。**GUI に出るログなので、個別に承認をお願いします** |
| L19 | 2644 | GUI | `Synchronous Button SA handling error: {ex}`（Stage3 の catch） | Stage3 と一緒に削除。**同じ文言のもう 1 つ（2649。エンジンの `ApplyFrameOutputs` の catch）は残す** |

## 3. 削除する（新経路が同じ文言で出している。画面に出るログの内容は変わらない）

| ID | 旧経路の場所（行） | 新経路での出力 |
|---|---|---|
| M1 | `LogSpecialActionTrace`（183〜243。`Trigger detected for SA`／`SA maps to`／`Trigger keys for`／`output combo`）と、その呼び出し（Stage3 の 2630、各枝の 3864〜4579） | `KeyOutputAction`、`MacroAction`、`ProfileSwitchAction`、`LaunchProcessAction`、`ButtonOutputAction`、`FrameOutput` など（7.x で同じ文言に揃えた。W2 の「ログの比べ」のテストで確認済み） |
| M2 | 2522〜2527（Stage3 の `Trigger detected for Button SA`／`Trigger no longer active for Button SA`）と 2588〜2621（出力の組み合わせ） | `ButtonOutputAction`（7.1） |
| M3 | `LogActionDoneCountOnTrigger`（3551。`SpecialAction {context}: ... ActionEntries=`）と、その呼び出し | 各アクション（`Key`／`Macro`／`Program`／`Profile`／`BatteryCheck`／`GyroCalibrate` ほか）が、同じ行を出す。テスト `MappingLogActionDoneCountOnTriggerTests` は、メソッドと一緒に削除 |
| M4 | 3919、3920（`SpecialAction PROFILE: Triggered ...`／`beingTriggered=..., useTempProfile=...`） | `ProfileSwitchAction`（106、107） |
| M5 | 4118、4131、4147（Key の `entry`／`KeyTriggered`／`KEY triggered`）、4352（`KeyReleased`） | `KeyOutputAction`（90〜96、129、152） |
| M6 | 4617（`SpecialAction UNTRIGGER check`） | `TriggerDispatchEngine`（205） |
| M7 | 4230（BatteryCheck の `LogToTray("Controller N: 残量", true)`。トレイの通知） | `DefaultBatteryCheckPort`（34）。通知の文言・条件は同じ（7.6c で確認済み） |

## 4. 残す（新経路も使うもの。変更しない）

| 場所（行） | 内容 | 理由 |
|---|---|---|
| 115 | `Mapping: ToggledOnChanged ...`（Trace。`ActionManager` のイベントの購読） | Toggle の状態は新経路も `ActionManager` で持つ |
| 755〜881 | `SYNTHETIC TRACE`／`EVENT SENT [SYNTHETIC]`（`Commit` のマウス・ホイールの送信） | `Commit` は新経路の出力（クリックの数）も送る |
| 4795〜5143 | `PlayMacro`／`PlayMacroTask`／`EVENT SENT [INLINE]`／`INLINE RELEASE TRACE` | マクロの実行は新経路（`PlayMacroDirect`／`PlayMacroString`）も使う |
| 4601 | `MapCustomAction exception: {ex}`（GUI） | エンジンにも同じ文言がある（`TriggerDispatchEngine` 620）。8b で `MapCustomAction` の形が決まったあと、残るならそのまま、メソッドごと消えるなら、エンジン側が代わる |
| 2649 | `Synchronous Button SA handling error: {ex}`（`ApplyFrameOutputs` の catch） | 新経路の呼び出し |
| 5922、6009、6031、6044、6211、6229 | ステアリングホイールの較正の GUI ログ・デバッグログ | 新経路（`SteeringWheelCalibrateAction`）も使う |
| コメントアウトされた `Console.WriteLine` | — | ログではない（変更しない） |

## 5. 影響するテスト

- 削除: `MappingLogActionDoneCountOnTriggerTests`（M3）。
- 8b で、旧経路を走らせる SA のテスト（`migrated: false`）は削除、または新経路だけにする。ログを比べるテスト（`Phase8Step72W2KeyLogTests` など）は、旧経路との比べを外し、新経路の行の並びを期待値として固定する（期待値は今のまま）。
- ユーザーに見えるログ（GUI・トレイ）の変更は、**L18（BeingTriggered の初期化失敗）と L19（Stage3 の例外）の 2 件だけ**。どちらも旧経路が消えれば起きない状況のログ。

## 6. 承認の結果（2026-10-20）

全項目を承認（L18・L19 を含む）。4601 は、`MapCustomAction` が残るため残した（8b で実施済み）。

（以下は承認前の依頼文）

### 承認をお願いしたい点

1. L1〜L17、M1〜M7 の削除（デバッグ・トレース・新経路が同じ文言で出すもの）。
2. L18、L19（GUI に出るログ）の削除。
3. 4601 は 8b で形を見て判断する、で良いか。
