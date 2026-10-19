# Phase8-Step7-6 計画書: 実機のデバイスを要する種別（GyroCalibrate、DisconnectBT、BatteryCheck、ステアリング較正）

作成日: 2026-10-20  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Step7-Plan.md`（§4 の 7.6、Q8＝A）、`Phase8-Step7-5-Plan.md`（進め方）、`Phase8-Step2-Design.md`（§6、§9、§15、§23）、`Phase8-Step2-Intent-Decisions.md`（項目 5・6）、`Phase8-Step1-Trigger-Spec-Ledger.md`（§3〜§5）  
状態: **Q1〜Q8 を承認（2026-10-20。Q3 と Q8 は B に変更、ほかは推奨 A）。7.6a GyroCalibrate はビルド・テスト成功（実機確認 U-35 は先送り。2026-10-20 ユーザー判断）。7.6b DisconnectBT はビルド・テスト成功（実機確認 U-36 は先送り。2026-10-20 ユーザー判断）。7.6c・7.6d は 7.6b の後**  
調査の方法: 旧ループ（`Mapping.MapCustomAction`）と、実機ログ（2026-10-20）の読解。テストでの確認は未実施

---

## 1. 目的

7.1〜7.5 で移した 5 種別（Button、Key、Macro、Program、Profile）に続き、実機のコントローラー（`DS4Device`）を直接操作する 4 種別を、統一ディスパッチ（エンジン）へ切り替える。運用は 7.1〜7.5 と同じ（`ServiceRegistration` の既定の一覧に種別を 1 行足す。外すと戻る。旧コードは Step8 まで残す）。

| 種別 | 型 ID | 旧経路（`Mapping.cs`）の要点 |
|---|---|---|
| GyroCalibrate | `GyroCalibrate` | 立ち上がりで 1 回: `SixAxis.ResetContinuousCalibration()`（結合デバイスも）。 |
| DisconnectBT | `DisconnectBT` | 押している間、**毎フレーム**実行。BT・同期・非充電なら `DisconnectBT()` → トリガーのボタンのキーを離す → `return`。ドングル（SONYWA）は `pressRelease` を立て、離した後、アイドルになったら `DisconnectDongle()`。 |
| BatteryCheck | `BatteryCheck` | 押している間、毎フレーム: 立ち上がりで通知とライトバーの色の保存、毎フレームで色のフェード。離したら戻す。 |
| ステアリング較正 | `SASteeringWheelEmulationCalibrate` | 押している間、毎フレーム: `WheelRecalibrateActiveState` を 3 秒間隔で 0→1、2→3 に進める。 |

## 2. 現状の調査

### 2.1 共通

- いずれも、**ビルダーは束縛を作らない**（`ActionBindingBuilder` は Button、Key、Macro、Program、Profile だけ）。出力アクション（`IOutputAction`）も未作成。設計書 §6 の「新設する」とされたもの。
- 実機の `DS4Device` が要る（`IDeviceStateAccessor.GetController(deviceIndex)` で取得できる）。**単体テストには実機がない**ため、7.1〜7.5 のように旧・新の実走行を比べにくい。`SpecialActionTestScope` では `ctrl.DS4Controllers[device]` が null で、旧経路の効果の分岐は、そのままでは走らない（旧経路は null 参照で例外となり、外側の `try` が握りつぶして終わる）。
- Fresh-press ガード: DisconnectBT、BatteryCheck、ステアリング較正は、旧経路のブロックの対象（`freshPressBlockedOtherTypes`）。GyroCalibrate は、同じ効果の独自の判定を持つ。いずれも `RespectsFreshPressGuard = true`（既定）で表せる。実行中のブロック（`BlocksWhileExecuting`）は、4 種別とも false。
- 離したときの `Stop` の扱いは、種別ごとに違う（下記）。

### 2.2 種別ごとの旧経路の事実

**GyroCalibrate**（`Mapping.cs` 4946〜4973 行）
- `BeingTriggered` が偽、かつ Fresh-press ガードなしのとき（立ち上がり）だけ: トレースログ → `d.SixAxis.ResetContinuousCalibration()`、結合デバイス（`JointDeviceSlotNumber`）があればそれも → `LogActionDoneCountOnTrigger("GyroCalibrate")` → `BeingTriggered = true`。
- 離したとき: 何もしない（`else` の汎用の処理が `BeingTriggered` を下げる）。

**DisconnectBT**（4860〜4884 行、5052〜5067 行）
- **押している間、毎フレーム** 実行する（`BeingTriggered` の判定がない。成立しても `BeingTriggered` を立てない）。BT・同期・非充電なら: トレースログ → `d.DisconnectBT()` → `ReleaseActionKeys`（トリガーのボタンの通常の割り当てのキー／マクロのキーを離す）→ `return`（フレームを打ち切る）。
- そのため、切断が完了する（約 0.1 秒）までの数フレームで、`DisconnectBT()` とトレースログが **繰り返される**。2026-10-20 の実機ログでも、PS+Down の切断のたびに、`Trigger detected for SA '0002_DS4WDisconnect Controller'` が 2〜5 回ずつ並んでいた（既知の挙動。Profile の 7.5 のときに確認した、同種の繰り返し）。
- ドングル（`SONYWA`）: `action.pressRelease = true`（**`SpecialAction` の `pressRelease` を、「切断待ち」の目印に流用**）。離したフレームから、`isDS4Idle()` になるまで毎フレーム待ち、アイドルになったら `DisconnectDongle()` → `ReleaseActionKeys` → `BeingTriggered` を下げる → `pressRelease = false`。
- 充電中（USB）や未同期のときは、何も起きない（成立するたびに毎フレーム条件を見直す）。

**BatteryCheck**（4885〜4920 行、5035〜5051 行）
- 押している間、毎フレーム（`details` は「|」または「,」区切り。`dets[1]`＝通知、`dets[2]`＝ライトバーの色）:
  - 立ち上がりのとき（`BeingTriggered` が偽）: `dets[1]` が真なら `AppLogger.LogToTray`（電池残量の通知）。`dets[2]` が真なら `lastColor[device]` に現在のライトバーの色を保存し、`DS4LightBar.forcelight[device] = true`。
  - `dets[2]` が真なら毎フレーム: 空・満タンの色から電池残量の色を計算し、`fadetimer[device]`（100 未満）を 2 ずつ増やしながら、`DS4LightBar.forcedColor[device]` を、保存した色から目標の色へ遷移させる。
  - 立ち上がりのとき: トレースログ。`LogActionDoneCountOnTrigger` を 2 回（"BatteryCheck" と、コピーの名残りの "WheelRecalibrate"）。`BeingTriggered = true`。
- 離したとき（`BeingTriggered` が真）: `fadetimer = 0`、`DS4LightBar.forcelight = false`、`BeingTriggered` を下げる。
- 状態は、`Mapping.fadetimer[]`／`lastColor[]` と、**静的な `DS4LightBar`**（`forcelight`／`forcedColor`）。通知は静的な `AppLogger.LogToTray`。

**ステアリング較正**（4921〜4945 行）
- 押している間、毎フレーム: `d.WheelRecalibrateActiveState == 0` かつ前回から 3 秒以上 → トレースログ、`action.firstTap = now`、状態を 1（較正の開始）。状態が 2 かつ 3 秒以上 → トレースログ、`firstTap = now`、状態を 3（較正の確定）。そのあと `BeingTriggered = true`。
- 離したとき: 汎用の処理（`BeingTriggered` を下げる）だけ。

## 3. 方針

### 3.1 分割（Q1）

**4 種別を、別々の PR にする**（影響範囲が小さい順）。それぞれ、W1（特性化）→ W2（実装）→ W3（スイッチ・性能）→ W4（実機確認）。個別の計画書は作らず、本計画書に節を足す（7.6a〜7.6d）。

| PR | 種別 | 実機確認 | 備考 |
|---|---|---|---|
| 7.6a | GyroCalibrate | 較正のログ（`ResetContinuousCalibration`）。効果は目視しにくい | 最も単純。立ち上がりの 1 回だけ |
| 7.6b | DisconnectBT | PS+Down で BT 切断（日常的に使う）。ドングルは機材がなければ、リスク受容 | 毎フレーム実行・`return`・ドングルの切断待ちがある |
| 7.6c | BatteryCheck | ライトバーのフェードと通知 | 静的な `DS4LightBar` を通す |
| 7.6d | ステアリング較正 | ステアリング操作の設定があれば。なければリスク受容 | 時間（3 秒）の状態機械 |

### 3.2 実機のデバイスの扱い（Q2）

出力アクションが `DS4Device` を直接持つと、単体テストで効果を確かめられない。**小さい窓口（ポート）の契約を、種別ごとに新設**し、その実装（アダプター）が `IDeviceStateAccessor` 越しに `DS4Device` を操作する。テストは、記録用の偽のポートで、呼び出しの回数・順序・条件を確かめる。例:
- `IGyroCalibrationPort`: `ResetContinuousCalibration(int deviceIndex)`（結合デバイスも含む）。
- `IControllerDisconnectPort`: 接続の種類・同期・充電・アイドルの取得、`DisconnectBT`、`DisconnectDongle`。
- `ILightBarOverridePort`＋通知: `forcelight`／`forcedColor` の書き込み、電池残量の通知（静的な `DS4LightBar`・`AppLogger.LogToTray` を、この窓口の内側に閉じ込める。Q6）。
- `IWheelCalibrationPort`: `WheelRecalibrateActiveState` の読み書き。

1 ファイル 1 型の原則に従い、契約は `DS4Windows/DI/`（R1）、実装は `DS4Windows/DS4Control/Services/`（R2）、出力アクションは `DS4Windows/Actions/`（R3）。名前空間は過渡期のルール（R6）。

### 3.3 種別ごとの設計の骨子

**GyroCalibrate（7.6a）**: `GyroCalibrateAction : IOutputAction`。エンジンの立ち上がりで `Execute`（`BeingTriggered` と Fresh-press ガードは、エンジンが担う）。ログ（トレース、`LogActionDoneCountOnTrigger`）は、旧経路と同じ文言・順序（7.2〜7.5 のエンジン用コンストラクターの方式）。旧経路は null のデバイスで例外になるが、新経路は、ポートが null を許す（効果は何もしない）。

**DisconnectBT（7.6b）**: 旧経路の「毎フレーム実行」を保つため、`Execute`（立ち上がり）と `IHeldOutputAction.WhileHeld`（保持中）の両方が、同じ処理を行う（Q3＝A）。実行したら `ctx.RequestEndFrame()`（Intent 項目 5。実行しなかった［充電中、未同期、ドングル］ときは呼ばない）。ドングルの切断待ちは、`IFrameOutputAction.ApplyFrame(ctx, established, rising)`（毎フレーム、成立の有無によらず呼ばれる）で、「離した後、アイドルになるまで待つ」を再現する（フラグは、出力アクションのインスタンスに持つ。旧経路が `SpecialAction.pressRelease` を流用していたもの。`ComboTriggerInputAction` の `InvertOnRelease` は、DisconnectBT では使わない）。`ReleaseActionKeys` と、7.5a の `ProfileSwitchAction.ReleaseTheTriggerButtonKeys` は、同じ処理のため、共通の小さい部品に切り出す（Q5）。

**BatteryCheck（7.6c）**: `Execute`（立ち上がり）で通知と色の保存・`forcelight`、`WhileHeld` で毎フレームのフェード、`Stop` で `fadetimer`／`forcelight` を戻す（離したエッジ。`StopOnRelease = true`）。`fadetimer`／`lastColor` は、出力アクションのインスタンスのデバイスごとの値にする（旧 `Mapping.fadetimer[]`／`lastColor[]` は、Step8 で削除）。`LogActionDoneCountOnTrigger` の「WheelRecalibrate」の名前の重複も、旧経路どおり再現する（ログの維持。規約 2.3）。

**ステアリング較正（7.6d）**: 状態機械（0→1、2→3）を 1 つのメソッドにまとめ、`Execute` と `WhileHeld` の両方から呼ぶ。`action.firstTap` は、旧経路と同じく `SpecialAction` の項目を使う（時間の判定は `input.UtcNow`）。

### 3.4 共通のテストの方針

- W1（特性化）: 実機がなくても確かめられる範囲を、旧・新の両経路で比べる。(a) Fresh-press ガード中・押し続けでの実行回数、(b) 立ち上がりのログの文言と順序（NLog のメモリ出力）、(c) フレームの打ち切り（DisconnectBT）、(d) AND 条件。デバイスの効果は、**ポートの偽**で確かめる（旧経路が `ctrl.DS4Controllers` を直接触るため、旧経路の効果の特性化は、デバイスを差し込める場合に限る。W1 の最初に、`DS4Device` を最小限に作る方法があるかを調べる）。
- W3: 割り当て（新 ≦ 旧）、性能の出力、ロールバック（1 行外す）。

## 4. 質問（推奨は A）

- **Q1（分割）**: A（推奨）: 4 種別を別々の PR にし、7.6a → 7.6b → 7.6c → 7.6d の順で進める。B: 4 種別を 1 つの PR にまとめる（実機の切り分けが難しい）。
- **Q2（実機のデバイスの扱い）**: A（推奨）: 種別ごとの小さいポートの契約を新設し、アダプターが `IDeviceStateAccessor` 越しに `DS4Device` を操作する（テストで偽に差し替えられる）。B: 出力アクションが `DS4Device` を直接持つ（ファイルは少ないが、単体テストで効果を確かめられない）。
- **Q3（DisconnectBT の「毎フレーム実行」）**: A（推奨）: 旧経路どおり、押している間、毎フレーム実行する（切断の完了までの数フレームの繰り返しと、トレースログの繰り返しも、旧経路どおり）。B: 立ち上がりの 1 回だけにする（繰り返しのログが減る。切断に失敗したときの再試行がなくなる。挙動の変更のため、規約 2.2 によりユーザーの承認が要る）。
- **Q4（DisconnectBT のドングル）**: A（推奨）: ドングル（SONYWA）の切断待ちも、7.6b で新経路へ移す（`ApplyFrame` で再現）。機材がなければ、ポートの偽でのテストと、実機確認のリスク受容の記録で進める。B: DisconnectBT は Step7 では移さず、Step8 で扱う（旧経路が残るため、旧コードの削除が遅れる）。
- **Q5（キーを離す処理の共通化）**: A（推奨）: DisconnectBT の `ReleaseActionKeys` と `ProfileSwitchAction.ReleaseTheTriggerButtonKeys`（同じ処理）を、共通の小さい部品に切り出す（既存の Profile のテストが守る）。B: 重複して持つ（既存のコードを触らない）。
- **Q6（静的な窓口）**: A（推奨）: BatteryCheck の `DS4LightBar` の静的なフィールドと `AppLogger.LogToTray` は、ポートの内側に閉じ込める（出力アクションからは静的な窓口を直接呼ばない。規約 2.1）。B: 出力アクションから直接呼ぶ（新しい静的な参照が増える）。
- **Q7（実機確認）**: A（推奨）: 種別ごとに、ユーザーが実機で確認し、合格してから次へ進む。機材がない項目（ドングル、ステアリングホイールの設定など）は、リスク受容を記録する（Step9 で、Phase8 の完了をそれらの確認にかけるかを決める）。B: 4 種別の後で、まとめて確認する。
- **Q8（`WheelRecalibrate` のログの名前の重複）**: A（推奨）: BatteryCheck の `LogActionDoneCountOnTrigger("WheelRecalibrate")`（コピーの名残り）も、旧経路どおり再現する（ログの維持。規約 2.3。削除の提案は Step8 で）。B: 7.6c で削除する（ログの変更のため、承認が要る）。

### 承認の記録（2026-10-20）

- Q1＝A、Q2＝A、Q4＝A、Q5＝A、Q6＝A、Q7＝A: 推奨どおり。
- **Q3＝B（変更）**: DisconnectBT は、「毎フレーム実行」をそのまま再現するのではなく、**切断の成功／失敗を戻り値として扱い、失敗の場合は再試行する仕組みを導入する**。
  - 設計（7.6b の W1 で、`DS4Device.DisconnectBT()` の戻り値の有無と失敗の条件を調べて確定する）: ポート `IControllerDisconnectPort` の `DisconnectBT` が成功／失敗（bool）を返す。出力アクションは、立ち上がりで実行し、成功したら `RequestEndFrame` を呼んで終わり。失敗したら、押している間（`WhileHeld`）、次のフレームで再試行する。成功するまで毎フレーム繰り返す旧経路との違い（成功後の繰り返しがなくなり、トレースログの繰り返しが減る）は、承認済みの挙動の変更として、完了報告書に記録する。
  - 旧 `DS4Device.DisconnectBT()` が失敗を返さない（void）場合は、成功の判定（接続が切れたか）の定義を、7.6b の計画書の節で決める。
- **Q8＝B（変更）**: BatteryCheck の `LogActionDoneCountOnTrigger("WheelRecalibrate")`（コピーの名残り）は **削除する**。BatteryCheck が実行されたことが分かるログは、既存の「SpecialAction BatteryCheck: device=…, name=…, index=…, ActionEntries=…」（Debug）と、立ち上がりの Trace 4 行がすでにあるため、追加しない。7.6c で削除し、完了報告書に記録する（ログの変更として承認済み）。

### 7.6a GyroCalibrate の実装（2026-10-20）

- 新設: `IGyroCalibrationPort`（窓口）、`DefaultGyroCalibrationPort`（本番。`Func<IDeviceStateAccessor>` を遅延で受ける。コントローラーがいなければ何もしない。結合デバイスも較正）、`GyroCalibrateAction`（立ち上がりで 1 回: Trace 4 行 → 較正 → Debug の「SpecialAction GyroCalibrate: …」）。配置は、Actions サブシステムのため `DS4Windows/Actions/`（R3）。
- 変更: `ActionBindingBuilder`（`IGyroCalibrationPort` を省略可で受ける。GyroCalibrate の束縛。ポートがなければ作らない）、`ServiceRegistration`（ポートの登録、既定の一覧に GyroCalibrate の 1 行）。ロールバックは、その 1 行を外す。
- テスト: `Phase8Step76aGyroCalibrateTests`、`RecordingGyroCalibrationPort`（記録用）。既存の更新: `SpecialActionTestScope`、`Phase8Step4W1ContractTests`、`Phase8Step7W3ButtonSwitchTests`（既定の一覧に GyroCalibrate）。
- 注意: GyroCalibrate の特殊アクションの型の文字列（`Actions.xml` の `Type`）は `GyroCalibration`（型 ID は `GyroCalibrate`）。割り当てのテストは、同じ件数でトリガー未成立の場合を基準にする（テストの足場が、特殊アクションがあるだけで 1 フレーム約 1 KB 増えるため）。
- 旧経路との比較は、実機がないため、立ち上がりの Trace 4 行の一致だけ。効果は記録用のポートで確認し、実機のログで確認する（U-35）。

### 7.6b DisconnectBT の実装（2026-10-20）

- 調査の事実: `DS4Device.DisconnectBT()` は bool を返すが、Windows の API の結果（`success`）を最後に `success = true;` で上書きしている（コメント: 「戻り値は失敗を示すが、それでも動く？」）。**MAC アドレスがあれば常に true**。ほかの呼び出し元（アイドル切断、`DisconnectWireless` など）がこの戻り値に依存するため、変えない。
- そのため、Q3＝B の「成功／失敗」は、実際に失敗として観測できるのが、コントローラーがいない・MAC がない・例外のときに限られる。それでも、仕組み（失敗なら再試行）は入れた。**実際の Windows の API の失敗を見分けるには、`DS4Device.DisconnectBT()` の戻り値の見直し（ほかの呼び出し元の確認を伴う）が別途要る**。必要なら、別の計画書にする。
- 新設: `DisconnectTargetKind`（None／Bluetooth／Dongle）、`IControllerDisconnectPort`（窓口）、`DefaultControllerDisconnectPort`（本番）、`TriggerButtonKeyReleaser`（キーを離す共通の部品。Q5＝A）、`DisconnectBTAction`（`IHeldOutputAction` と `IFrameOutputAction`）。配置は `DS4Windows/Actions/`（R3）。
- 変更: `ProfileSwitchAction`（キーを離す処理を共通の部品へ。処理は同じ）、`ActionBindingBuilder`（`IControllerDisconnectPort` を省略可で受ける。DisconnectBT の束縛。ポートがなければ作らない）、`ServiceRegistration`（ポートの登録、既定の一覧に DisconnectBT の 1 行）。ロールバックは、その 1 行を外す。
- 動き: Bluetooth（同期済み・非充電）で、トレース 4 行 → 切断の要求 → 成功なら、キーを離し、`RequestEndFrame`。成功のあとは、離すまで再要求しないが、切断の完了までフレームの打ち切りは毎フレーム続ける（旧経路の `return` と同じ）。失敗なら、押している間、次のフレームで再試行（フレームは打ち切らない）。ドングル: 押している間は切断待ちにし、離した後、アイドルになったら切断とキーの解放（`ApplyFrame`）。
- 旧経路との違い（承認済み。完了報告書に記録）: 切断の要求の成功後の繰り返し（`DisconnectBT()` とトレースログが、切断の完了までの数フレーム並んでいた）がなくなる。
- テスト: `Phase8Step76bDisconnectBTTests`、`RecordingControllerDisconnectPort`（記録用）。既存の更新: `SpecialActionTestScope`、`Phase8Step4W1ContractTests`、`Phase8Step7W3ButtonSwitchTests`（既定の一覧に DisconnectBT）。
- 実機確認 U-36。
- **ユーザー決定（2026-10-20）: 「切断の成功／失敗」は、案 A（`DS4Device.DisconnectBT()` が Windows の API の本当の結果を返すようにする）で実装したい。実施の判断は、実機確認 U-36 の後に行う（今は実装しない）。**
  - 案 A の注意: ほかの呼び出し元（アイドル切断［`DS4Device.cs` 1380 行付近］、`DS3Device`／`DualSenseDevice`／`JoyConDevice` の同種の箇所、`DisconnectWireless`）が、成功＝true を前提にしているため、変更の前に、全呼び出し元の影響の確認が要る。別の計画書にする想定。
  - 案 B（新経路で、命令の後、一定時間たっても接続されたままなら再試行）は、今回は採らない。

## 5. リスクと対応

- **実機がない確認**: ポートの偽でのテストで、呼び出しの条件・順序・回数を固定する。ドングルとステアリングは、リスク受容を記録（Q7）。
- **DisconnectBT のフレームの打ち切り**: `RequestEndFrame` の条件を、旧経路の `return`（実行したときだけ）と同じにする。W1 で、旧・新を比べる。
- **ライトバーの色の競合**: `DS4LightBar.forcedColor` を、他の機能（バッテリー低下の点滅など）と共有する。ポートの中で、旧経路と同じ書き込みの順序を保つ。実機確認で、フェードを目視する。
- **ロールバック**: `ServiceRegistration` の該当の 1 行を外す。種別ごとに独立。

## 6. 完了条件（種別ごと）

- [ ] W1〜W3 のビルド・テストが成功（ユーザーが実行）。全テスト・ラチェットの一致検査・1 ファイル 1 型のガードが成功。割り当て（新 ≦ 旧）。
- [ ] 既定の一覧に種別が入り、ロールバックのテストが成功。
- [ ] 実機確認が合格（または、機材がない項目のリスク受容の記録）。
- [ ] `Phase8-Step7-Completion-Report.md`、`Phase8-Status.md`、チェックリスト（U-35 以降）の更新。

## 7. 次

7.6a（GyroCalibrate）の W1 から。その後 7.6b〜7.6d。7.6 の後は、7.7（XboxGameDVR／MultiAction）。
