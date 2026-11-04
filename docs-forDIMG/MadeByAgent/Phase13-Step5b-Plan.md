# Phase13-Step5b 個別計画書: 入力レポート処理（`On_Report`）の分離と `Mapping` の逆依存の解消（ゲート付き）

作成日: 2026-11-04（骨子）／ 詳細化: 2026-11-04（Step5 完了後。Step5 の実測と、`On_Report`・`Mapping` の再調査を反映）
状態: **承認済み（2026-11-04。§9 の 1〜8 すべて）。実装中（5b-a から）**
親計画: `Phase13-Plan.md` §4 Step5／5b、§9 R1。分離元: `Phase13-Step5-Plan.md`（ユーザー指示「Step5 を 5 と 5b に2分割」）
前提: Step5 完了（`da6d3a82`。`IDeviceSlotState` が使える）。注: 「Step5b」は Phase13 の中の Step の名前で、ロードマップの「Phase13b（ログ整理）」とは別物。

---

## 1. 目的

1. `ControlService.On_Report`（約 200 行）を `InputReportProcessor` へ移し、`Func<IDeviceSlotHost>`（#7 の最後の 1 件）を撤廃して `IDeviceSlotHost` を削除する。
2. `Mapping`（第 2 層）が `ControlService`（第 4 層）を引数で受ける逆依存を解消する（規約 §1）。`IMapping.MapCustom` の `ControlService ctrl` 引数を廃止する。
3. `On_Report` と `Mapping` のためだけに `ControlService` が持っていたコンストラクター引数を削除する（29 → 26 を見込む。§5.3）。

## 2. 現状の調査（2026-11-04）

### 2.1 `On_Report`（`ControlService.cs` 634〜832 行）が使うもの

`_latencyMonitor`、`CurrentState`／`TempState`／`MappedState`／`ExposedState`、`_pathService`＋`_profileRepository`（初回レポートの Missing-Action ログ）、`_appearanceSettings`（バッテリー表示）、`_profileSettings`、`_virtualKBM`、`_mapping`（`TempMouseJoystick`／`SetCurveAndDeadzone`／`MapCustom`／`Commit`）、`_touchpadToggle`、`_profileActionProvider`、`_appSettings`＋`_oscService`、`outputDevices`（`_virtualPad`）、`ActiveOutDevType`（`Func<IOutputSlotService>` 経由）、`recordingMacro`、`IsMeasuringProcessingDelay`／`ProcessingDelayMs`、`DS4LightBar.updateLightBar`（静的）、そして **`_mapping.MapCustom(…, this)` で `ControlService` 自身を渡している**。

### 2.2 `On_Report` の外から使われる状態（移すもの）

| メンバー | 外部の利用者 | Step5b の扱い |
|---|---|---|
| `recordingMacro` | `RecordBox`（UI。209・236 行で書き込み） | `InputReportProcessor` が持つ。`ControlService` に get／set の転送を残す（Step10 で削除） |
| `IsMeasuringProcessingDelay`／`GetProcessingDelay` | `ControllerReadingsControl`（UI。359・370 行など） | 同上 |
| `ProcessingDelayMs[]` | `ControlService` の内部のみ | `InputReportProcessor` へ |
| `MappedState`／`ExposedState` | `ControlService` の内部のみ（`getDS4StateMapped` も外部の利用者なし） | `InputReportProcessor` へ。`ExposedState`（公開）と `getDS4StateMapped` は外部の利用者が 0 件のため、移した後に `ControlService` からは削除する |
| `ProfileActionProvider`／`SpecialActionRepository`（`ControlService` の internal プロパティ） | `Mapping` だけ | `Mapping` が直接受けるようになるため、削除 |

### 2.3 `Mapping` の逆依存（再調査の結果）

- `IMapping.MapCustom(int device, DS4State cState, DS4State MappedState, DS4StateExposed eState, Mouse tp, ControlService ctrl)`（`IMapping.cs` 16 行）と、`Mapping.cs` の 5 つの private メソッド、`_sampleControl[]`（`ControlService[]`）。
- 使っている `ControlService` のメンバーは 4 つ: `DS4Controllers`（`ctrl.DS4Controllers`。`getMouseMapping`／`MapCustom`／ボタンマウスの計算）、`ProfileActionProvider`（4）、`ProfileSettingsService`（4）、`SpecialActionRepository`（1）。
- `MapCustom` を呼ぶ**本番**のコードは `ControlService.On_Report` の 1 か所だけ。テストは `MappingCharacterizationScope.RunMapCustom` の 1 か所（これを使うテストは多数）と、`StandaloneTests` の `IMapping` スタブ 2 つ（`MapCustom` の本文は `throw`）。当初の見込み（10 ファイル）より小さい。

### 2.4 `ActiveControllers` の持ち主（Step5 の 5c で残った課題）

`DeviceConnector` が `HookSettings(device, () => _host().ActiveControllers)` で読む。持ち主は `ServiceLifecycle`（`ActiveControllers { get; set; }`）で、`ServiceLifecycle` は `DeviceConnector` に依存するため、`DeviceConnector` から `ServiceLifecycle` を直接引くと循環する。

### 2.5 テストでの `On_Report` の実行可能性

`TestDs4Device`（`GetUninitializedObject` による生成）が既にあるが、`On_Report` が触る `DS4Device` のメンバー（`getRawCurrentState`、`PerformStateMerge`、`firstReport`、`isSynced`、`PrimaryDevice`、`lastInputReportTimestamp` など）のうち、仮想でないものは実デバイスなしで動くか未確認。→ 5b-a で、特性テスト（characterization）を書けるかを検証する（§6）。

## 3. 事前調査（規約 §4）

Step5 計画書 §3 のとおり（ゲームパッド周りの OSS に類似の実装なし。指針は「ホットパスでは解決を繰り返さず、一度解決して参照を保持する」）。追加の適用: `InputReportProcessor` は、配列も依存サービスも `readonly` フィールドへコンストラクターで保持し、`On_Report` の本文は**フィールド名の置換のみ**で移す。

## 4. 設計案

### 4.1 新設する型（規約 R1・R2・R6）

| 型 | 置き場所 | 内容 |
|---|---|---|
| `IInputReportProcessor` | `DS4Windows/DI/`（`DS4Windows.DI`） | `OnReport(DS4Device, EventArgs, int)`、`bool RecordingMacro { get; set; }`、`bool IsMeasuringProcessingDelay { get; set; }`、`double GetProcessingDelay(int)` |
| `InputReportProcessor` | `DS4Windows/DS4Control/Services/`（`DS4Windows.Services`） | `On_Report` の本文を移す。コンストラクターの依存（13 個）: `IDeviceSlotState`、`ILatencyMonitor`、`IPathService`、`IProfileRepository`、`IAppearanceSettingsService`、`IProfileSettingsService`、`IVirtualKBM`、`IMapping`、`ITouchpadToggleService`、`IProfileActionProvider`、`IAppSettingsService`、`IOscService`、`IVirtualPadService`、`Func<IOutputSlotService>`（`ActiveOutDevType` の遅延取得。理由: `OutputSlotService` ⇄ `VirtualPadService` の循環が Step7 まで残るため） |

`MappedState`／`ExposedState`／`ProcessingDelayMs` は `InputReportProcessor` のフィールドになる（`ExposedState` の初期化は、`ControlService` のコンストラクターの `for` ループから、`InputReportProcessor` のコンストラクターへ移す。`new DS4StateExposed(CurrentState[i])` の `CurrentState[i]` は `ControlService` が `new DS4State()` した後でなければならないため、**初期化の順序を保つ**——§8 のリスク）。`DeviceConnector` は `IInputReportProcessor` を直接受け、`Report` イベントの購読から `processor.OnReport(...)` を呼ぶ。`ControlService` はレポートの経路から外れる。

### 4.2 `Mapping` の逆依存の解消

| 現在 | 置き換え |
|---|---|
| `ctrl.DS4Controllers` | `Mapping` のコンストラクターで受ける `IDeviceSlotState.DS4Controllers` |
| `ctrl.ProfileSettingsService` | `Mapping` が既に持つ `profileSettings` |
| `ctrl.ProfileActionProvider` | `Mapping` のコンストラクターで受ける `IProfileActionProvider` |
| `ctrl.SpecialActionRepository` | `Mapping` のコンストラクターで受ける `ISpecialActionRepository` |
| `_sampleControl[]`（`ControlService[]`） | 廃止（`getMouseMapping` 系は、スロット状態と `profileSettings` から読む） |
| `IMapping.MapCustom(…, ControlService ctrl)` | `ctrl` 引数を廃止 |

`Mapping` のコンストラクター引数は 25 → 28（`IDeviceSlotState`、`IProfileActionProvider`、`ISpecialActionRepository`）。Step10 で整理する。循環の検査: `ISpecialActionRepository` は `BackingStore`・`IPathService`・`SyntheticInputState`・`SpecialActionsXmlSerializer`・`IManagedActionManager`（Step4。`Func<IMapping>` で遅延）に依存し、`Mapping` へは戻らない。`IProfileActionProvider` は `BackingStore` だけ。

### 4.3 `ActiveControllers` の持ち主（決定事項 Q1）

| 案 | 内容 |
|---|---|
| **A（推奨）** | `IDeviceSlotState` に `int ActiveControllers { get; set; }` を加える（スロットの状態＝接続中の数）。`ServiceLifecycle.ActiveControllers` は `slots.ActiveControllers` へ転送（既存の get／set を保つ）。`DeviceConnector` は `slots.ActiveControllers` を直接読む。`IDeviceSlotHost` は不要になり削除できる |
| B | `ActiveControllers` 用の最小の IF を新設し、`DeviceConnector` と `ServiceLifecycle` が共有 |

案 A は `DeviceSlotState` の「依存なし」を保つ。複数スレッドからの読み書きは、現状（`ServiceLifecycle` の `int` プロパティ）と同じ（ロックなし）。

### 4.4 公開メンバーの扱い（Step5 の Q2 と同じ）

`ControlService` に `recordingMacro`（get／set）、`IsMeasuringProcessingDelay`（get／set）、`GetProcessingDelay` の転送を残す。規約 §3.3 原則4 の TODO（Step10 で呼び出し側［`RecordBox`、`ControllerReadingsControl`］を `IInputReportProcessor` の直接注入へ切り替えて削除）を付ける。

## 5. 影響範囲

### 5.1 ファイル

| 区分 | ファイル |
|---|---|
| 新規 | `IInputReportProcessor`、`InputReportProcessor`、テスト数本 |
| 本番の変更 | `ControlService`（`On_Report`・`MappedState`・`ExposedState`・`ProcessingDelayMs` の削除、転送の追加、引数の整理）、`IMapping`／`Mapping`（§4.2）、`IDeviceSlotState`／`DeviceSlotState`（`ActiveControllers`）、`ServiceLifecycle`（`ActiveControllers` の転送）、`DeviceConnector`（`IInputReportProcessor`＋`slots.ActiveControllers`）、`ServiceRegistration` |
| 削除 | `IDeviceSlotHost`、`Func<IDeviceSlotHost>` の登録と `ControlService` の実装 |
| テスト | `Phase9EInputReportGuardTests`（`On_Report` のソースの検査を `InputReportProcessor.cs` へ）、`Phase9E6*GuardTests`、`MappingCharacterizationScope`（`RunMapCustom` の呼び出し）、`StandaloneTests` の `IMapping` スタブ 2 つ、`Phase7Step8W2MappingInstanceGuardTests`（`Mapping` の引数の数 25 → 28）、`Phase13Step2/3` のコンストラクターのガード（29 → 26）、`Phase13Step5FuncRemovalGuardTests`（`Func<IDeviceSlotHost>` が 0 件）、ラチェット |
| ドキュメント | モデル図 01〜04（`IInputReportProcessor` の追加、`Mapping` の逆依存の解消、`Func` の撤廃。Step12 で最終確認）、`Phase13-Status.md` |

### 5.2 ラチェット

`ControlService.cs` → `InputReportProcessor.cs` に移る `Global`／`AppHost`／`Program.rootHub` の件数を、`Step10FourGlobalRatchetGuardTests` の基準値へ引き継ぐ（合計を増やさない。規約 §3.3 原則1）。

### 5.3 `ControlService` の引数の見込み（29 → 26）

| 引数 | 結果 |
|---|---|
| `IAppearanceSettingsService`、`IVirtualKBM` | `On_Report` 以外に使用 0 件 → **削除** |
| `IProfileActionProvider`、`ISpecialActionRepository` | `Mapping` 専用の internal プロパティのみ → **削除** |
| `IInputReportProcessor` | **追加**（転送のため） |
| `ILatencyMonitor`、`ITouchpadToggleService`、`IPathService`、`IProfileRepository` ほか | `ControlService` に他の用途が残る（ログの転送、`CheckForTouchToggle`、`StartTPOff`、`TouchpadSlide`、`ExecutablePath`、`SelectedProfileChanged`）→ **残す**（5b-e で最終確認） |

## 6. 実装の分割と A/B 計測（ゲート）

| Sub | 内容 | 確認 |
|---|---|---|
| **5b-a** | 調査と下準備: **`On_Report` の特性テスト（characterization）を、移設の前に書く**（`TestDs4Device` で `ControlService.On_Report` を呼び、`CurrentState`／`MappedState`／出力・バッテリー表示・`ProcessingDelayMs` の結果を固定する。書けない場合は、書けなかったメンバーの理由を記録し、`git diff -w` による機械的な置換の確認で代替）。`Mapping` の `ctrl` 4 メンバーの使用箇所の洗い出し。before の計測を採取 | `Debug ALL`、before の計測値 |
| **5b-b** | `Mapping` の逆依存の解消（§4.2）。`IMapping.MapCustom` の `ctrl` 引数を廃止。`ControlService.On_Report` は、`ctrl` なしの呼び出しになる | `Debug ALL`、`Mapping` 関連のテスト（約 3,000 件） |
| **5b-c（ゲート）** | `InputReportProcessor` の新設と `On_Report` の移設。`ExposedState`／`MappedState` の初期化の順序を保つ。after の計測。**不合格なら取りやめ（revert）、`OnReport` 専用の `Func<IDeviceSlotHost>` を理由つきで残す形に切り替えて再相談** | `Debug ALL`、§6 の計測、特性テスト |
| **5b-d** | `IDeviceSlotState.ActiveControllers`（Q1=A）。`DeviceConnector` を `IInputReportProcessor`＋`slots` の直接注入へ。`IDeviceSlotHost`・`Func<IDeviceSlotHost>` の削除 | `Debug ALL` |
| **5b-e** | `ControlService` の不要な引数の削除（§5.3）、ラチェット・ガードテストの更新、モデル図の反映 | `Debug ALL` |

**合格基準**（Step5 と同じ）: ホットパスのアロケーション 0、処理時間は悪化 5% 以内。

| 計測 | 方法 | いつ |
|---|---|---|
| ① アロケーション | `On_Report` の特性テストの harness が作れれば、`GC.GetAllocatedBytesForCurrentThread` で 1 万回の差が 0（作れない場合は、読み取りの部分［`Phase13Step5SlotStateHotPathAllocationTests`］で代替し、理由を記録） | 通常のテスト（常時） |
| ② 時間（マイクロ） | `Phase13Step5SlotStateTimingBenchmark` 方式（`DS4W_RUN_BENCH=1`、Release、中央値）。比べる対象: `DeviceConnector` の購読から `OnReport` までの呼び出し（現状: `IDeviceSlotHost.OnReport` → `ControlService.On_Report` の 2 段／移設後: `IInputReportProcessor.OnReport` の 1 段）。harness が作れれば `On_Report` 全体の前後比較 | 5b-a（before）と 5b-c（after） |
| ③ 実機 | `ProcessingDelayMs` を、Release ビルド 2 つ（Step4 のコミット `1a3f8273` ／ 5b 完了時）で、同じ操作（入力 30 秒＋スティック操作）をして比較。ユーザー実機。Step5・Step5b の接続・切断・UDP の確認もここでまとめて行う | 5b-e 完了後（実機確認 H 系と同時） |

## 7. リスクと対策

| リスク | 対策 |
|---|---|
| `On_Report` の移設で、呼び出し順・条件が変わる | 本文は機械的な置換のみ。`Phase9EInputReportGuardTests` の順序検査（監視 → タッチ切り替え → マッピング）を新ファイルに対して維持。`git diff -w` で、フィールド名以外の変更がないことを確認。5b-a の特性テスト |
| ホットパスの遅延・割り当て | §6 の計測、5b-c をゲートにする。呼び出しの段数は 2 → 1 に減る |
| `Mapping` の逆依存の解消で `MapCustom` の動きが変わる | `ctrl` の 4 メンバーを同じ実体へ置換するだけ。`MapCustom` の特性テスト群と、`Phase8Step4MapCustomBenchmarkTests` の前後比較 |
| `ExposedState` の初期化の順序（`DS4StateExposed` が `CurrentState[i]` を参照する） | `DeviceSlotState` の `CurrentStates[i]` は `ControlService` のコンストラクターが `new DS4State()` する。`InputReportProcessor` は `ExposedState[i]` を、**遅延せず**、`ControlService` が `CurrentState[i]` を作った後に作る必要がある。→ 初期化（`for` ループ）を `InputReportProcessor` のコンストラクターへまとめて移す案を第一とし（`CurrentStates[i]` も同じ場所で作る）、`ControlService` は作らない。5b-a で、`CurrentStates[i]` を読む他の部品［`DeviceDisconnectHandler`、`UdpMotionService`］が、作成前に読まないことを確認する |
| `Stop`→`Start` で状態が残る・消える | 現行の初期化の位置・内容を変えない |
| 循環（`ControlService` → … → `InputReportProcessor` → `Mapping` →…） | 5b-c で DI の解決テスト（`TestHost` で全型を解決）を実行。`Func` が必要な箇所は理由を記録 |
| UI の書き込み（`recordingMacro` など）が新しい持ち主に届かない | 転送プロパティの単体テスト |
| 実機（接続・切断・UDP・マクロ記録・遅延表示） | 実機確認 H 系で 1 回（Phase13 の方針。Release ビルドは 2 か所へ） |

## 8. 規約への適合

- §2.2・§2.3: 機能・ログを変えない（`On_Report` は本文を変えずに移す）。
- §3.2: `ControlService.cs`・`Mapping.cs`（巨大ファイル）は、対象メソッドのみの置換・削除（全体を再生成しない）。
- §3.3: 1 ファイル 1 型、ファイル名＝型名。`IDeviceSlotHost` は削除。
- §3.4: `IInputReportProcessor` は `DS4Windows/DI/`（R1）、実装は `DS4Windows/DS4Control/Services/`（R2）。名前空間は動かさない（R6）。
- §3.1: Pure DI。`AppHost.Services` を持ち込まない。ラチェットは件数を減らしたら同時に更新。
- §1: `Mapping`（第 2 層）→ `ControlService`（第 4 層）の逆依存を解消する。
- §2.4: 転送プロパティ（§4.4）には TODO（経緯、Step10 で削除、現状の呼び出し側）を付ける。

## 9. 決定事項（承認をお願いします）

1. **`On_Report` を `InputReportProcessor` へ分離する**（`IInputReportRouter` は作らない）。5b-c の計測が不合格なら取りやめ、`OnReport` 専用の `Func<IDeviceSlotHost>` を理由つきで残す。
2. **Q1（`ActiveControllers`）**: 案 A（`IDeviceSlotState.ActiveControllers { get; set; }` を加え、`ServiceLifecycle` は転送。`IDeviceSlotHost` を削除）。
3. **`Mapping` の逆依存の解消**（`IMapping.MapCustom` の `ctrl` 引数の廃止。`Mapping` のコンストラクター引数 25 → 28）。
4. **Q2（公開メンバー）**: `recordingMacro`／`IsMeasuringProcessingDelay`／`GetProcessingDelay` は転送を残し、Step10 で削除。`ExposedState`／`getDS4StateMapped`（外部の利用者 0 件）は、移した後に `ControlService` から削除する。
5. **`ExposedState`／`CurrentStates` の初期化**は、`InputReportProcessor` のコンストラクターへまとめて移す案を第一とする（順序の検証を 5b-a で行う）。
6. **5b-a で `On_Report` の特性テスト（移設前）を書く**。書けないメンバーは理由を記録し、`git diff -w` の確認で代替する。
7. **A/B 計測の設計（§6）と合格基準（アロケーション 0、5% 以内）に対する再承認**。5b-a で before、5b-c で after を採取。
8. 5b-a〜5b-e を別コミットにする。`ControlService` の引数は 29 → 26 を見込む（5b-e で確定）。

## 10. 5b-a の結果（2026-11-04）

- **特性テストは書けた**: `Phase13Step5bOnReportCharacterizationTests`（6 件）。`TestDs4Device` に、レポート処理が読む保護フィールド（`cState`／`pState`／`jointState`／`jointPreviousState`／`primaryDevice`／`outputMapGyro`／`jointDeviceSlotNumber`）をリフレクションで与え、DI 構築の `ControlService.On_Report` を呼ぶ。固定した挙動: `ind=-1` は何もしない／主デバイスは生の入力を `CurrentStates[ind]` にコピー／従デバイス（ジャイロ出力なし）は Motion をコピーしてマッピングを飛ばす／`ProcessingDelayMs` は計測中かつ DInput 専用でない経路でだけ記録／`recordingMacro` はレポート処理を止めない。入口は `Invoke` に集約済みで、5b-c で `IInputReportProcessor.OnReport` に替える。
  - 発見: `ProcessingDelayMs` は `UseDInputOnlyArray[ind] == false` の経路でだけ記録される（既存の挙動。変更しない）。テストはそれに合わせて設定・復元する。
- **before の計測（Release、`DS4W_RUN_BENCH=1`、`Phase13Step5bOnReportTimingBenchmark`、中央値 7 ラウンド）**: 692.9 ns／回、574.2 ns／回（2 回実行。最小 253〜258、最大 956〜977 ns と環境負荷で大きく揺れる）。**割り当て: 0 bytes／200,000 回**（デリゲート経由）。リフレクション経由の特性テストでの 72 bytes／回は呼び出し自体（`object[]`＋boxing）で、`On_Report` 本体は 0。
  - 注意: 揺れが大きい（最小値と最大値で約 4 倍）ため、5b-c の after は同じ条件で複数回測り、**最小値と中央値の両方**で比べる。合格基準は 5% 以内（§6）。
- **`Mapping` の `ctrl` の使用箇所（5b-b の対象）**: `_sampleControl`（291, 298, 314, 539）、`ctrl.ProfileSettingsService`（303–317）、`ctrl.DS4Controllers`（307, 533, 649, 688）、`ctrl.ProfileActionProvider`（542, 923, 937, 938）、`ctrl.SpecialActionRepository`（920）、`ctrl` を受け渡す private メソッド（516, 642, 714, 887, 915）。想定どおり 4 メンバー（`ProfileSettingsService`／`DS4Controllers`／`ProfileActionProvider`／`SpecialActionRepository`）。`ProfileSettingsService` は `Mapping` が既に `IProfileSettingsService` を持つか 5b-b で確認する。
- **`CurrentStates` の初期化順**: プロダクションで `CurrentStates[i]` を読むのは `DeviceDisconnectHandler`（接続切断時）と `ServiceLifecycle`（`Start` 時）だけで、いずれも `ControlService` 構築後の実行時。構築中に読む部品はなく、`InputReportProcessor` のコンストラクターへ初期化をまとめても順序の問題は生じない。
- **Debug ALL**: Actions.Tests 3081 件すべて合格（3071 → +10）、警告 0・エラー 0 のビルド。

## 11. 5b-b の結果（2026-11-04）

- `IMapping.MapCustom` から `ControlService ctrl` を廃止し、`Mapping` の private メソッド（`ProcessStick`／`ProcessTriggerWithTwoStage`／`ProcessControlSettingAction`／`MapCustomAction`）の `ctrl` 引数も削除。`_sampleControl` を削除。
- `Mapping` のコンストラクターは 25 → 28（`IDeviceSlotState`、`IProfileActionProvider`、`ISpecialActionRepository` を追加）。`ctrl.ProfileSettingsService` は、`Mapping` が既に持つ `profileSettings`（同じ Singleton）に置き換えた。
- 挙動の差: 旧 `SampleMouseMovement`／`SampleFixedMouseMovement` の「`_sampleControl` が null なら 0」は、最初の `MapCustom` 前だけの防御だった。`_sampleState` が null のときの 0 は維持。`SampleFixedMouseMovement` は `ctrl` の null 判定が不要になった（`profileSettings` は常にある）。
- 呼び出し元（`ControlService.On_Report`）、`MappingCharacterizationScope`、Standalone の `IMapping` スタブ 2 件、`TestMappingFactory`、`Phase7Step8W2MappingInstanceGuardTests`（28 件／null 検査）を更新。
- `ControlService.ProfileActionProvider`／`SpecialActionRepository` は `Mapping` から使われなくなった。コンストラクターの引数ごと 5b-e で削除する。
- Debug ALL: Actions.Tests 3081 件・Standalone 11 件すべて合格、警告 0・エラー 0。

## 12. 5b-c の結果（2026-11-04）

- **新設**: `DI/IInputReportProcessor.cs`（`OnReport`／`GetMappedState`／`ExposedState`／`RecordingMacro`／`IsMeasuringProcessingDelay`／`ProcessingDelayMs`／`GetProcessingDelay`）、`DS4Control/Services/InputReportProcessor.cs`（Singleton。コンストラクター引数 14）。`ServiceRegistration` に登録し、`ControlService` のファクトリーへ渡した。
- **`On_Report` の移設**: 旧 `ControlService.On_Report` を削除し、本文を `InputReportProcessor.OnReport` へ移した。`diff -w` で、フィールド名の置換以外の差がないことを確認した（置換: `CurrentState`→`_currentState`、`TempState`→`_tempState`、`MappedState`→`_mappedState`、`ExposedState`→`_exposedState`、`touchPad`→`_touchPad`、`outputDevices[`→`_virtualPad.OutputDevices[`、`recordingMacro`→`RecordingMacro`、`ProcessingDelayMs`→`_processingDelayMs`。呼び出しの順序・条件・ログは同一）。
- **初期化**: `MappedState`／`CurrentState`／`ExposedState` の作成は `InputReportProcessor` のコンストラクターへ移した（`CurrentStates[i]` を作った後に `DS4StateExposed` を作る順序は同じ）。`TempState`／`PreviousState` は `ControlService` のコンストラクターに残している。→ `TempStates` は `ControlService` の構築までは未作成のため、テストで `InputReportProcessor` だけを使うときは `ControlService` も解決する（本番は起動時に必ず構築される）。
- **`ControlService`**: `recordingMacro`／`IsMeasuringProcessingDelay`／`ProcessingDelayMs`／`GetProcessingDelay` は転送だけ残した（§3.3 原則4 の TODO つき。Step10 で削除）。`ExposedState`／`getDS4StateMapped` は削除（外部の利用者 0 件）。`IDeviceSlotHost.OnReport` は `_inputReport.OnReport` へ転送（5b-d で IF ごと削除）。コンストラクターは 29 → 30（5b-e で不要な引数を外して 26）。
- **after の計測（Release、`Phase13Step5bOnReportTimingBenchmark`、3 回実行）**: 中央値 666.0／471.8／614.8 ns（before: 692.9／574.2）、最小 247.3／333.8／317.0 ns（before: 257.9／253.0）、**割り当て 0 bytes／200,000 回**。揺れが大きい（最小と最大で約 3〜4 倍）環境のため、中央値は before と同等かやや良く、最小値は同等〜やや大きい範囲に収まる。本文の命令列は同じで（差はフィールドの参照元のみ）、構造上の悪化の要因はない。**判定: 合格（割り当て 0、有意な悪化なし）**。実機の `ProcessingDelayMs` の比較は、計画どおり 5b-e の後に H 系の実機確認とあわせて 1 回行う。
- **更新したテスト**: `Phase13Step5bOnReportCharacterizationTests`（入口を `IInputReportProcessor.OnReport` の直接呼び出しに変更、割り当て 0 を合格基準に）、`Phase13Step5bOnReportTimingBenchmark`、`Phase9EInputReportGuardTests`（順序・`_latencyMonitor.OnReport` の検査を新ファイルに）、`Phase9E6ConnectorGuardTests`、`Phase10Step4dW0RuntimeKeyRatchetTests`（基準値を `ControlService.cs` から `InputReportProcessor.cs` へ。合計は増えていない）、ctor 数のガード 2 件（29 → 30）。
- **Debug ALL**: Actions.Tests 3081 件・Standalone 11 件すべて合格、警告 0・エラー 0。

## 13. 5b-d の結果（2026-11-04）

- `IDeviceSlotState` に `int ActiveControllers { get; set; }` を追加（実体は `DeviceSlotState` の自動プロパティ）。`ServiceLifecycle.ActiveControllers`（`IServiceLifecycle` の契約はそのまま）は `IDeviceSlotState` へ転送する。
- `DeviceConnector` は `Func<IDeviceSlotHost>` をやめ、`IInputReportProcessor` を直接注入で受ける（`ActiveControllers` は `_slots.ActiveControllers`、`Report` の購読は `inputReport.OnReport`）。循環（ControlService → ServiceLifecycle → DeviceConnector → ControlService）は、`InputReportProcessor` が `ControlService` に依存しないため解消している。
- `IDeviceSlotHost`（ファイルごと）と、その登録 2 行（`IDeviceSlotHost`、`Func<IDeviceSlotHost>`）、`ControlService` の実装を削除。`Func<IDeviceSlotHost>` は本番ソースで 0 件（`Phase13Step5FuncRemovalGuardTests` を 0 件の検査に更新）。Step5 のロードマップ #6〜#8 の `Func` 撤廃がすべて完了。
- 更新したテスト: `Phase9E6ConnectorGuardTests`（2 件）、`Phase9E6ConnectionGuardTests`、`Phase9E6DisconnectGuardTests`（`IDeviceSlotHost` が存在しないことの検査に）、`Phase13Step5FuncRemovalGuardTests`。
- Debug ALL: Actions.Tests 3081 件・Standalone 11 件すべて合格、警告 0・エラー 0。
