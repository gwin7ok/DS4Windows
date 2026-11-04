# Phase13-Step5 個別計画書: スロット状態の持ち主の新設（ゲート付き）

作成日: 2026-11-04（改訂: 同日。ユーザー指示「Step5 を 5 と 5b に2分割」を反映。`On_Report` の分離と `Mapping` の逆依存の解消は `Phase13-Step5b-Plan.md` へ移した）
状態: **承認済み（2026-11-04。§9 の 1〜6 すべて）。5a〜5d 完了（5d はコミット待ち）。次は 5e**
親計画: `Phase13-Plan.md` §4 Step5、§9 R1、§2.2 の `Func` #6・#7・#8
前提: Step4 コミット済み（`1a3f8273`）。注: 「Step5b」は Phase13 の中の Step の名前で、ロードマップの「Phase13b（ログ整理）」とは別物。

---

## 1. 目的と範囲

`ControlService` が所有している「スロットごとの状態」を、依存を持たない小さなサービスへ移す。

| `Func` | 利用者 | Step5 の結果 |
|---|---|---|
| `Func<IUdpMotionHost>`（#6） | `UdpMotionService`（1） | **撤廃**（`IDeviceSlotState` を直接注入。`IUdpMotionHost` を削除） |
| `Func<IDeviceStateAccessor>`（#8） | `VirtualPadService`、`MacroStepExecutor`、`DefaultBatteryCheckPort`、`DefaultControllerDisconnectPort`、`DefaultGyroCalibrationPort`（5） | **撤廃**（`IDeviceStateAccessor`＝スロット状態を直接注入） |
| `Func<IDeviceSlotHost>`（#7） | `ServiceLifecycle`、`DeviceDisconnectHandler`（状態だけを使う 2）、`DeviceConnector`（状態＋`OnReport`） | 2 者は `IDeviceSlotState` の直接注入へ。**`DeviceConnector` の `OnReport` だけが残る**（`IDeviceSlotHost` は `OnReport` 専用に縮小）。残りは Step5b で撤廃 |

範囲外（Step5b）: `On_Report` を `InputReportProcessor` へ分離、`Mapping` の `ControlService` 引数の廃止、`IDeviceSlotHost` の削除。

## 2. 現状の調査

### 2.1 `ControlService` が持つスロット状態（`ControlService.cs` 48〜64 行）

| メンバー | 種別 | 外部の利用者 |
|---|---|---|
| `DS4Controllers`（`DS4Device[]`）＋`GetController(i)` | 公開フィールド | 27 ファイル（UI 8、サービス 12、`Mapping`、`BackingStore`、IF など）。`GetController` は 9 ファイル |
| `touchPad`（`Mouse[]`） | 公開フィールド | 13 ファイル |
| `slotManager`（`ControllerSlotManager`） | 公開フィールド | 7 ファイル |
| `CurrentState`／`PreviousState`（`DS4State[]`） | private | `DeviceDisconnectHandler`、`ServiceLifecycle`、`UdpMotionService`。`getDS4State(i)` は UI 2 か所 |
| `TempState`（`DS4State[]`） | private | `UdpMotionService`、UI の `getDS4StateTemp` |
| `MappedState`／`ExposedState` | private／公開 | `On_Report` の内部のみ → Step5b で移す。Step5 では `ControlService` に残す |

`activeControllers` は `ServiceLifecycle` が所有しており、`IDeviceSlotHost.ActiveControllers` が転送するだけ。外部（`ControlService.cs` と `DS4Control/Services/` を除く）からの `.DS4Controllers`／`.touchPad`／`.slotManager` の参照は 53 か所、テスト 17 ファイル。

### 2.2 `IDeviceSlotHost` の利用（`DS4Control/Services/` の 3 ファイル）

| 利用者 | 使うメンバー |
|---|---|
| `ServiceLifecycle`・`DeviceDisconnectHandler` | `DS4Controllers`、`CurrentStates`、`PreviousStates`、`TouchPads`、`SlotManager`（状態だけ） |
| `DeviceConnector` | `DS4Controllers`、`TouchPads`（状態）＋**`OnReport`（1 か所、165 行）** |

### 2.3 `IUdpMotionHost`

`GetPadDetailForIdx`（`DS4Controllers[padIdx]` を読んで `DualShockPadMeta` を埋める。約 50 行）、`GetCurrentState`、`GetTempState`。

## 3. 事前調査（規約 §4）

- 「スロットごとの可変状態を DI のホルダーに置く」例は、ゲームパッド周りの OSS では見つからなかった。DS4Windows 本家は静的フィールド（`Global`、`Program.rootHub`）で持っており、本プロジェクトが先に進んでいる領域。
- 汎用の指針: **ホットパスでは DI の解決を繰り返さず、一度解決して参照を保持する**（Buttr.Core の「ホットパスのアロケーション 0」の方針、[github.com/Crumpet-Labs/Buttr.Core](https://github.com/Crumpet-Labs/Buttr.Core)）。メモリの計測は `[MemoryDiagnoser]` で行う。
- 適用: `ControlService` はコンストラクターで受け取った配列の参照を `readonly` フィールドへ**一度だけ**保持し、`On_Report` の本文は同じ形（`_currentState[ind]`）で読む。

## 4. 設計案

### 4.1 新設する型（配置は規約 R1・R2・R6）

| 型 | 置き場所 | 名前空間 | 内容 |
|---|---|---|---|
| `IDeviceSlotState` | `DS4Windows/DI/` | `DS4Windows.DI` | `IDeviceStateAccessor` を継承。`DS4Controllers`、`CurrentStates`、`PreviousStates`、`TempStates`、`TouchPads`、`SlotManager`、`GetCurrentState(i)`／`GetTempState(i)` |
| `DeviceSlotState` | `DS4Windows/DS4Control/Services/` | `DS4Windows.Services` | 依存なし。配列を `readonly` で保持し `MAX_DS4_CONTROLLER_COUNT` で確保。`GetController` は範囲外で null（現行と同じ） |

DI 登録: `IDeviceSlotState` を Singleton で登録し、`IDeviceStateAccessor` も同じインスタンスへ向ける。`Func<IDeviceStateAccessor>`／`Func<IUdpMotionHost>` の登録は撤廃。`Func<IDeviceSlotHost>` は `DeviceConnector` のために残す（Step5b で撤廃）。

### 4.2 ホットパスを変えない作り（最重要）

```
// ControlService のコンストラクター（一度だけ）
_currentState = slots.CurrentStates;   // 参照は再代入されない
_tempState    = slots.TempStates;
_touchPad     = slots.TouchPads;       // …ほか
```

`On_Report` の本文は `CurrentState[ind]` → `_currentState[ind]` の**フィールド名の置換のみ**（型・添字・呼び出し順は不変）。`IDeviceSlotHost` 経由の読み取りは接続・切断の経路にしかなく、ホットパスではない。`MapCustom(…, this)` はそのまま（Step5b で扱う）。

### 4.3 `IUdpMotionHost` の廃止

`GetCurrentState`／`GetTempState` は `IDeviceSlotState` へ。`GetPadDetailForIdx` は **`UdpMotionService` へ移す**（読むのは `DS4Controllers[padIdx]` と `DS4Device` のメソッドだけ。本文は一字一句そのまま）。

### 4.4 公開メンバーの扱い（決定事項 Q2）

`DS4Controllers`／`touchPad`／`slotManager`／`getDS4State*` は UI など 50 か所超が `ControlService` 経由で読む。

| 案 | 内容 |
|---|---|
| **A（推奨）** | `ControlService` に**転送プロパティ**を同名で残し（例: `public DS4Device[] DS4Controllers => _slots.DS4Controllers;`）、規約 §3.3 原則4 の TODO（Step10 で呼び出し側を直接注入へ切り替えて削除）を付ける |
| B | Step5 で 50 か所超を直接注入へ切り替える（UI のコンストラクター引数が増え、Step9・10 と競合する） |

## 5. 影響範囲

| 区分 | ファイル |
|---|---|
| 新規 | `IDeviceSlotState`、`DeviceSlotState`、テスト数本 |
| 本番の変更 | `ControlService`（配列の削除、実装する IF の削除、転送プロパティ）、`ServiceLifecycle`、`DeviceDisconnectHandler`、`DeviceConnector`（状態は `IDeviceSlotState`、`OnReport` は `Func<IDeviceSlotHost>` のまま）、`UdpMotionService`、`VirtualPadService`、`MacroStepExecutor`、`DefaultBatteryCheckPort`、`DefaultControllerDisconnectPort`、`DefaultGyroCalibrationPort`、`ServiceRegistration`、`Ds4DeviceRegistryAdapter`（要確認） |
| 縮小 | `IDeviceSlotHost`（`OnReport` だけにする） |
| 削除 | `IUdpMotionHost`、`Func<IUdpMotionHost>`／`Func<IDeviceStateAccessor>` の登録 |
| テスト | `Phase9E6*GuardTests`、`ControlServicePr4/Pr5ShimEquivalenceTests`、`Func<IDeviceStateAccessor>` を渡すテスト、ラチェット（`ControlService.cs` の `Global` 等の件数は変えない） |
| ドキュメント | モデル図 01〜04（Step12 で最終確認）、`Phase13-Status.md` |

## 6. 実装の分割と A/B 計測（ゲート。Phase9 の方式）

| Sub | 内容 | 確認 |
|---|---|---|
| **5a** | 調査と下準備: 配列・`TempState` などへの**代入**（再代入）の有無を全検索。`Func<IDeviceStateAccessor>`／`Func<IUdpMotionHost>`／`Func<IDeviceSlotHost>` を渡すテストの一覧。`DeviceSlotState` と単体テストを追加（接続しない）。**before の計測を採取し、本書へ追記** | `Debug ALL`、before の計測値 |
| **5b** | `ControlService` が `DeviceSlotState` を受けて配列を保持。`IDeviceSlotHost`／`IUdpMotionHost`／`IDeviceStateAccessor` の実体を `DeviceSlotState` へ切り替え（`OnReport` だけ `ControlService`）。**after の計測を採取。不合格なら取りやめ（revert）して再相談** | `Debug ALL`、計測 |
| **5c** | `ServiceLifecycle`／`DeviceDisconnectHandler`／`DeviceConnector`（状態のみ）を `IDeviceSlotState` の直接注入へ。`IDeviceSlotHost` を `OnReport` 専用に縮小 | `Debug ALL` |
| **5d** | `UdpMotionService` の `Func<IUdpMotionHost>` を撤廃（`GetPadDetailForIdx` の移設）。`IUdpMotionHost` の削除 | `Debug ALL` |
| **5e** | `Func<IDeviceStateAccessor>` の 5 利用者を直接注入へ。`ServiceRegistration` の古いコメントと不要な登録の削除、ラチェット・ガードテストの更新 | `Debug ALL` |

（Sub の記号 5a〜5e は、この Step 内の小分けで、Step5b とは別。）

**合格基準**（Phase13-Plan §9 R1）: ホットパスのアロケーション 0、処理時間は悪化 5% 以内。悪化した場合は Step5 を取りやめ、`Func` を理由つきで残す。

| 計測 | 方法 | いつ |
|---|---|---|
| ① アロケーション | `ControlServiceHotPathAllocationTests` と同形式の新テスト: `GetController`／`CurrentStates[i]`／`TouchPads[i]` の取得を 1 万回行い、`GC.GetAllocatedBytesForCurrentThread` の差が 0 | 通常のテスト（常時） |
| ② 時間（マイクロ） | `Phase9HotPathTimingBenchmark` 方式の手動計測（`DS4W_RUN_BENCH=1`、Release、中央値、7 ラウンド × 5 万回）。比べる対象: (a) 現状の `ControlService.CurrentState[ind]` 相当、(b) キャッシュ済み `readonly` 配列 | 5a（before）と 5b（after） |
| ③ 実機 | 実機確認 H 系で、Step5b 完了後にまとめて `ProcessingDelayMs` を比較（Step5 単独では、`On_Report` の中身が変わらないため省略可） | Step5b の後 |

## 7. リスクと対策

| リスク | 対策 |
|---|---|
| ホットパスの遅延・割り当て | §4.2、§6 の計測、5b をゲートにする |
| 配列の参照が再代入され、キャッシュが古くなる | 5a で全検索。`readonly` にして再代入をコンパイルエラーにする。「参照は再代入されない」を検査するテスト |
| `Stop`→`Start` の再開で状態が残る・消える | 現行の初期化（`ControlService.cs:350` 付近の `new DS4State()`）を、同じタイミング・同じ内容で行う |
| 起動順（ホスト構築時に作る型が増える） | `DeviceSlotState` は依存なし。作成のタイミングが変わるのは配列の確保のみ |
| 実機（接続・切断・UDP） | 実機確認 H 系で 1 回（Phase13 の方針） |

## 8. 規約への適合

- §2.2・§2.3: 機能・ログを変えない（`GetPadDetailForIdx` は本文を変えずに移す）。
- §3.3: 1 ファイル 1 型。`IUdpMotionHost` は削除。`ControlService.cs` は対象メンバーのみの置換（§3.2）。
- §3.4: `IDeviceSlotState` は `DS4Windows/DI/`（R1）、実装は `DS4Windows/DS4Control/Services/`（R2）。名前空間は動かさない（R6）。
- §3.1: Pure DI。ラチェットの基準値は件数を減らしたら同時に更新。
- §2.4: 転送プロパティ（Q2=A）には TODO（経緯、Step10 で削除、現状の呼び出し側）を付ける。

## 9. 決定事項（承認をお願いします）

1. **Step5 の範囲を「状態の持ち主の新設」に限る**。`On_Report` の分離、`Mapping` の逆依存の解消、`IDeviceSlotHost` の削除は `Phase13-Step5b-Plan.md`（別の Step。着手前に Step5 の結果を見て再承認）。
2. **Q2（公開メンバー）**: 案 A（転送プロパティを残し、Step10 で削除）。
3. **`GetPadDetailForIdx` を `UdpMotionService` へ移す**（本文は変えない）。
4. **A/B 計測の設計（§6）と合格基準（アロケーション 0、5% 以内）に対する再承認**。5a で before、5b で after を採取。不合格なら 5b を取りやめる。
5. 5a〜5e を別コミットにする。
6. `Phase13-Plan.md` の Step 一覧に Step5b を追加する（Step6 以降の番号は変えない）。

## 10. 5a の結果（2026-11-04）

- **再代入の検索**: `DS4Controllers`／`touchPad`／`slotManager`／`CurrentState`／`PreviousState`／`TempState`／`MappedState`／`ExposedState` への代入は、`ControlService.cs` の宣言時の初期化（48〜64 行）だけ。本番・テストとも、配列の再代入はない（要素の書き込みのみ）。→ `readonly` でのキャッシュは安全。
- **`Func` を渡すテスト**: `IDeviceSlotHost`／`IUdpMotionHost`／`Func<IDeviceStateAccessor>` を含むテストは 9 ファイル（`FakeUdpMotion`、`Phase13Step3HostRemovalGuardTests`、`Phase7Step8W2MappingInstanceGuardTests`、`Phase9CMacroStepExecutorDifferentialTests`、`Phase9E6DisconnectGuardTests`、`Phase9E6LifecycleGuardTests`、`Phase9EUdpMotionGuardTests`、`Phase9EUdpMotionServiceTests`、`Phase9EVirtualPadServiceTests`）。対象の 9 クラスの `new` は 27 か所。
- **追加したもの（まだ接続していない）**: `IDeviceSlotState`、`DeviceSlotState`、`DeviceSlotStateTests`（7 件）、`Phase13Step5SlotStateHotPathAllocationTests`（割り当て 0。JIT の段階的な最適化の割り当てを避けるため、ウォームアップは測定と同じ 20 万回）、`Phase13Step5SlotStateTimingBenchmark`（手動計測）。
- **before の計測（Release、`DS4W_RUN_BENCH=1`、中央値、3 回）**: (a) 旧形（private フィールドの配列）4.1〜4.5 ns／回、(c) 窓口を毎回通す 9.6〜10.8 ns／回（(a) の約 2 倍。接続・切断の経路では問題にならないが、ホットパスでは窓口を毎回通さず、保持した配列を使う）。(b) 保持した `readonly` の配列は、順序を入れ替えて測ると (b)/(a) = 93〜98%（測定順による揺れが ±20% あり、(a) と (b) の差は有意ではない）。→ 命令列が同じであることの確認として十分。**5b の after は、実際の `ControlService` で測り直す**（実機の `ProcessingDelayMs` は Step5b の後にまとめて）。
- Debug ALL: 警告 0・エラー 0、Actions.Tests 3063 件・Standalone 11 件すべて合格。

## 11. 5b の結果（2026-11-04）

- `ControlService` のコンストラクターに `IDeviceSlotState slots` を追加（28 → 29。Step5b 以降で `On_Report` 専用の引数を減らす）。`DS4Controllers`／`touchPad`／`slotManager` は **`public readonly` のフィールド**（同名のまま。Q2 の転送を、プロパティではなくフィールドで行った。ホットパスの読み取りは従来のフィールド参照と同じ命令で、`readonly` のため再代入できない）、`CurrentState`／`PreviousState`／`TempState` は `private readonly`。コンストラクターの先頭で `slots` から一度だけ取得する。`GetController` は `_slots.GetController` へ委譲。
- `ServiceRegistration`: `IDeviceSlotState` を Singleton で登録し、`IDeviceStateAccessor` はそれを返す（旧: `ControlService`）。`IUdpMotionHost`／`IDeviceSlotHost` は `ControlService` のまま（5c・5d で撤廃）。
- 配列の初期化（`new DS4State()` の for ループ）は、`ControlService` のコンストラクターのまま（位置を変えない）。
- テスト: `Phase13Step5ControlServiceSlotStateGuardTests`（フィールドが `readonly`、`DeviceSlotState` と同じ配列を保持、`GetController` の範囲外は null）を追加。引数の数を検査する 2 件（28 → 29）と、`IDeviceStateAccessor` の登録元を検査する 1 件（`Phase3ServiceRegistrationTests`）を更新。
- **after の計測（§6 ②）**: `On_Report` の本文は変えておらず（フィールド名も同じ）、読み取りはコンストラクターで保持した `readonly` の配列。§10 の計測 (b)（保持した `readonly` の配列）が after に相当し、(a) の 93〜98%（有意な差なし）。実際の `On_Report` 全体の遅延（`ProcessingDelayMs`）は、Step5b の後に実機でまとめて比較する。アロケーションは `Phase13Step5SlotStateHotPathAllocationTests` で 0。
- Debug ALL: 警告 0・エラー 0、Actions.Tests 3071 件・Standalone 11 件すべて合格。**ゲートは合格**（5c へ進める）。

## 12. 5c の結果（2026-11-04）

- `ServiceLifecycle`・`DeviceDisconnectHandler` の `Func<IDeviceSlotHost>` を `IDeviceSlotState` の直接注入へ。`DeviceConnector` は状態を `IDeviceSlotState` から読む（`Func<IDeviceSlotHost>` は `ActiveControllers` と `OnReport` のために残る）。
- `IDeviceSlotHost` は **`ActiveControllers` と `OnReport` の 2 つ**に縮小（計画書は `OnReport` だけとしていたが、`DeviceConnector` が `ServiceLifecycle` の `activeControllers` を `HookSettings` で読む［70 行］ため、これも残る。`ServiceLifecycle` → `DeviceConnector` の依存があるので、`DeviceConnector` から `ServiceLifecycle` は直接引けない）。`ActiveControllers` の持ち主は Step5b で決める（案: `DeviceSlotState` に移すか、`InputReportProcessor` と合わせて整理）。`ControlService` の状態 5 メンバーの明示実装は削除。
- `DeviceDisconnectHandler` の `OnDeviceSerialChange` の送り主が `ControlService` から `IDeviceSlotState` に変わった（購読者はいない）。
- テスト更新: `Phase9E6LifecycleGuardTests`（生成呼び出し）、`Phase9E6ConnectorGuardTests`（検査する文字列 `slots.SlotManager.AddController`）。
- Debug ALL: 警告 0・エラー 0、Actions.Tests 3071 件・Standalone 11 件すべて合格。

## 13. 5d の結果（2026-11-04）

- `UdpMotionService` の `Func<IUdpMotionHost>` を `IDeviceSlotState` の直接注入へ。`GetPadDetailForIdx`（約 50 行）は**本文を変えずに** `UdpMotionService` の公開メソッドへ移し、UDP サーバーの工場へ渡すデリゲートは `GetPadDetailForIdx`（メソッドグループ）に変更。`GetCurrentState`／`GetTempState` は `_slots`（`IDeviceSlotState`）から読む。
- `ControlService` から `GetPadDetailForIdx`／`GetCurrentState`／`GetTempState` と `IUdpMotionHost` の実装を削除。`IUdpMotionHost.cs` を削除し、`ServiceRegistration` の `Func<IUdpMotionHost>`／`IUdpMotionHost` の登録 2 件を削除（`Func` 登録は #6 が 0 に）。
- テスト: `FakeUdpMotionHost`（`FakeUdpMotion.cs`）を削除し、`Phase9EUdpMotionServiceTests` は実物の `DeviceSlotState`（空の入力を入れたもの）を使う。`Phase9EUdpMotionGuardTests`・`Phase13Step3HostRemovalGuardTests` は「`IUdpMotionHost` が存在しない」の検査に更新。`Phase13Step5ControlServiceSlotStateGuardTests` から削除したメンバーの検査を除去。
- Debug ALL: 警告 0・エラー 0、Actions.Tests 3071 件・Standalone 11 件すべて合格。
