# Phase13-Step5b 個別計画書: 入力レポート処理（`On_Report`）の分離と `Mapping` の逆依存の解消（ゲート付き）

作成日: 2026-11-04
状態: **骨子（Step5 の完了後に、実測を反映して詳細化し、再承認を得る）**
親計画: `Phase13-Plan.md` §4 Step5、§9 R1。分離元: `Phase13-Step5-Plan.md`（ユーザー指示「Step5 を 5 と 5b に2分割」）
前提: Step5 完了（`IDeviceSlotState` が使える）。注: 「Step5b」は Phase13 の中の Step の名前で、ロードマップの「Phase13b（ログ整理）」とは別物。

---

## 1. 目的

1. `ControlService.On_Report`（約 200 行）を `InputReportProcessor` へ移し、`Func<IDeviceSlotHost>`（#7 の残り）を撤廃して `IDeviceSlotHost` を削除する。
2. `Mapping` が `ControlService`（第 4 層）を引数で受ける逆依存を解消する（規約 §1）。
3. `On_Report` だけが使っていた `ControlService` のコンストラクター引数を削除し、Step10 の目標（20 以下）に近づける。

## 2. 現状の調査（2026-11-04 時点）

### 2.1 `On_Report`（`ControlService.cs` 699〜897 行）が使うもの

`_latencyMonitor`、`CurrentState`／`TempState`／`MappedState`／`ExposedState`、`_pathService`＋`_profileRepository`（初回レポートの Missing-Action ログ）、`_appearanceSettings`（バッテリー表示）、`_profileSettings`、`_virtualKBM`、`_mapping`（`TempMouseJoystick`／`SetCurveAndDeadzone`／`MapCustom`／`Commit`）、`_touchpadToggle`、`_profileActionProvider`、`_appSettings`＋`_oscService`、`outputDevices`（`_virtualPad`）、`ActiveOutDevType`（`Func<IOutputSlotService>` 経由）、`recordingMacro`、`IsMeasuringProcessingDelay`／`ProcessingDelayMs`、`DS4LightBar.updateLightBar`（静的）、そして **`_mapping.MapCustom(…, this)` で `ControlService` 自身を渡している**。

### 2.2 外部から書かれる状態

| メンバー | 書き込み元 |
|---|---|
| `recordingMacro` | `RecordBox`（UI。209・236 行） |
| `IsMeasuringProcessingDelay` | `ControllerReadingsControl`（UI。359・370 行） |
| `ProcessingDelayMs`／`GetProcessingDelay` | 読み取り（UI の遅延表示） |
| `getDS4StateMapped`／`ExposedState` | `ControlService` の内部＋公開 |

### 2.3 `Mapping` の逆依存

`Mapping.MapCustom(…, Mouse tp, ControlService ctrl)` と、そこから呼ぶ 5 つの private メソッド、`_sampleControl[]`（`ControlService[]`）に `ControlService` を保持する（8 か所）。使っているメンバーは 4 つだけ: `ctrl.DS4Controllers`（3）、`ctrl.ProfileActionProvider`（4）、`ctrl.ProfileSettingsService`（4）、`ctrl.SpecialActionRepository`（1）。`MapCustom(` を呼ぶテストは 10 ファイル。

## 3. 設計の骨子

### 3.1 新設する型（規約 R1・R2・R6）

| 型 | 置き場所 | 内容 |
|---|---|---|
| `IInputReportProcessor` | `DS4Windows/DI/`（`DS4Windows.DI`） | `OnReport(DS4Device, EventArgs, int)`、`RecordingMacro`、`IsMeasuringProcessingDelay`、`GetProcessingDelay(int)`、`GetMappedState(int)`、`ExposedStates` |
| `InputReportProcessor` | `DS4Windows/DS4Control/Services/`（`DS4Windows.Services`） | `On_Report` の本文を**フィールド名の置換のみ**で移す。`MappedState`／`ExposedState`／`ProcessingDelayMs`／`recordingMacro` の持ち主になる |

`DeviceConnector` は `IInputReportProcessor` を直接受ける。`ControlService` はレポートの経路から外れる。UI 向けの `recordingMacro` などは `ControlService` に転送プロパティ（get／set）を残し、Step10 で削除する（Step5 の Q2 と同じ）。

### 3.2 `Mapping` の逆依存の解消

| 現在 | 置き換え |
|---|---|
| `ctrl.DS4Controllers` | `IDeviceSlotState.DS4Controllers`（`Mapping` のコンストラクターで受ける） |
| `ctrl.ProfileSettingsService` | `Mapping` が既に持つ `profileSettings` |
| `ctrl.ProfileActionProvider` | `IProfileActionProvider` を `Mapping` のコンストラクターで受ける（循環の有無を調査） |
| `ctrl.SpecialActionRepository` | `ISpecialActionRepository` を同様に受ける（`Func` が必要なら理由を記録） |
| `_sampleControl[]` | 廃止 |

`Mapping` のコンストラクター引数は 25 → 27 前後（Step10 で整理）。

### 3.3 循環の検査（着手時）

`ControlService` → `ServiceLifecycle` → `DeviceConnector` → `InputReportProcessor` → `Mapping` ほか。`InputReportProcessor` は `ControlService` を引数に取らない（`Func<ControlService>` を使う `ProfileSettingsService` などは遅延のまま）。`Mapping` は現在も `ControlService` の引数として先に作られているので、作成順は変わらない。

## 4. 実装の分割（案）

| Sub | 内容 |
|---|---|
| **5b-a** | 調査（循環、`IProfileActionProvider`／`ISpecialActionRepository` の注入可否、`On_Report` が使う依存の一覧）。before の計測（Step5 の after を流用できるなら流用） |
| **5b-b** | `Mapping` の逆依存の解消。`MapCustom` の `ctrl` 引数を廃止し、テスト 10 ファイルを更新 |
| **5b-c（ゲート）** | `InputReportProcessor` の新設と `On_Report` の移設。ガードテスト（`Phase9EInputReportGuardTests`）を新ファイルへ。after の計測。不合格なら取りやめ（revert）、`OnReport` 専用の `Func<IDeviceSlotHost>` を理由つきで残す |
| **5b-d** | `DeviceConnector` を `IInputReportProcessor` の直接注入へ。`IDeviceSlotHost`・`Func<IDeviceSlotHost>` の削除 |
| **5b-e** | `On_Report` だけが使っていた `ControlService` の引数の削除（実測で確定）、ラチェット・ガードテストの更新、モデル図の反映 |

## 5. A/B 計測（ゲート）

合格基準は Step5 と同じ（アロケーション 0、悪化 5% 以内）。`On_Report` の呼び出しの段数は、`IDeviceSlotHost.OnReport`＋`ControlService.On_Report` の 2 段から、`IInputReportProcessor.OnReport` の 1 段に減る。`MapCustom` 以降は不変。実機の `ProcessingDelayMs` の比較（Release ビルド 2 つ）を、Phase13 の実機確認 H 系と同時に 1 回行う。

## 6. リスク（骨子）

- `On_Report` の移設で、呼び出し順・条件が変わる → 本文は機械的な置換のみ。`git diff -w` で、フィールド名以外の変更がないことを確認。
- `Mapping` の逆依存の解消で `MapCustom` の動きが変わる → `ctrl` の 4 つのメンバーを同じ実体へ置換するだけ。`Phase8Step4MapCustomBenchmarkTests` の前後比較。
- UI の書き込み（`recordingMacro` など）が新しい持ち主に届かない → 転送プロパティの単体テスト。
- 規模は L。着手前に Step5 の結果（実測）を反映して、この骨子を詳細化し、再承認を得る。
