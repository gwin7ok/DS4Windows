# 設計案: 入力機器の「状態の文脈型」（PhaseRV14 の続き）

作成日: 2026-11-21
状態: **設計案（承認待ち。コードは未変更）**
出典: `PhaseRV14-Plan.md`（発見 3）、`InputDeviceCore*.cs`・`SwitchProDevice*.cs`・`DualSenseDevice*.cs`・`DualShock4Device*.cs` の読み取り（2026-11-21）。

## 1. 問題の整理（読んで分かった実態）
切り出しが進まない原因は、型の中の処理が、**持ち主の決まっていない共有フィールド**を直接読み書きしているため。

| 共有状態 | 持ち主の現状 | 触る場所（実数） |
|---|---|---|
| 触覚（振動・ライトバー）の「いま」と「テスト用」: `currentHap`・`testRumble`・`rumbleAutostopTimer`・`previousHapticState` | 基底の `protected` フィールド | `currentHap` 61 行／7 ファイル、`testRumble` 19／5、`rumbleAutostopTimer` 7／4 |
| 出力レポートのバッファ: `outputReport`・`outReportBuffer` | 基底の `protected` フィールド | 127 行／8 ファイル |
| 電池・充電: `battery`・`charging` と、2 つのイベント | 基底の `protected` フィールド＋ `UpdateBattery`・`UpdateCharging` | 機種の読み取りが毎フレーム書く |
| Switch Pro の較正: スティック 4 軸、IMU の 9 配列、`leftStickOffset*` | `SwitchProDevice` の `private` フィールド | `ProcessFrame` が毎フレーム読む。初期化が書く |
| Switch Pro の通信: `hDevice`・`frameCount`・`Subcommand` | `SwitchProDevice` | 初期化・切断・出力・設定変更が使う |

共通する形: **「書く側（初期化・出力の組み立て）」と「読む側（毎フレームの読み取り）」が、同じ型のフィールドを介して結ばれている**。そのため、書く側だけを別の型へ移せない。

## 2. 設計の原則
1. **何でも箱にしない**。文脈型は「1 つのまとまった状態と、それを守る操作」ごとに作る（触覚、較正、通信の 3 種類。全部入りの `DeviceContext` は作らない）。
2. 文脈型は、**状態の持ち主**になる。デバイス（`InputDeviceCore` 系）は、文脈型を `readonly` で 1 つ持ち、必要な部分だけを使う。
3. 書く側の処理（初期化・出力の組み立て）は、文脈型を**引数で受ける**協力クラスへ移す。デバイスのフィールドは直接読ませない（規約 3.1 の Pure DI、層の依存を増やさない）。
4. **同じ層の internal な具象**（規約の「状態を持たない計算の部品は、インターフェースを作らない」に従う。テストで偽物にするものだけがインターフェース）。例外: `IHidTransport`（既存）。
5. 毎フレームの経路（読み取りのループ）で**割り当てを増やさない**（文脈型はコンストラクターで 1 回作る。配列は使い回す）。
6. 挙動・ログ・スレッドの構成は変えない。1 つの文脈型ごとに 1 コミット、そのたびに Debug ALL。

## 3. 提案する文脈型

### 3.1 Switch Pro（効果が最も確実。最初に実施）
| 型 | 持つ状態 | 主な操作 | 使う側 |
|---|---|---|---|
| `SwitchProCalibration` | 左右のスティックの `StickAxisData`（4）、`leftStickCalib`・`rightStickCalib`、`leftStickOffsetX/Y`・`rightStickOffsetX/Y`、IMU の 9 配列（`accelNeutral`・`accelSens`・`accelCoeff`・`gyroBias`・`gyroSens`・`gyroCalibOffsets` ほか） | 読み取り専用のプロパティ（配列は参照を返す）。`SwitchProCalibrationDecoder`（既存）が書く | `ProcessFrame`（読む）、`SwitchProSession`（書く） |
| `SwitchProCommandChannel` | `hDevice`（`IHidTransport`）、`frameCount` | `Subcommand`、`ReloadStickCalib`、`NextFrameCount`、`WriteInterrupt`、`SetInitRumble`、`PrepareRumbleData`。USB の起動手順 `RunUSBSetup` | `SwitchProSession`、出力・切断・設定変更 |
| `SwitchProSession` | なし（引数: 通信路、較正、接続の種類、`enableHomeLED`、スロットのマスク） | `SetOperational`、`CalibrationData`（既存の手順をそのまま移す） | `SwitchProDevice.StartUpdate` |

- 期待: `SwitchProDevice` 1,035 行 → 約 560 行（`.Setup.cs` の 349 行が `SwitchProSession`・`SwitchProCommandChannel` へ）。
- 付随の重複解消: `SwitchProDevice.DisconnectBT` の Bluetooth の OS 呼び出し（約 40 行）は、今回切り出した `BluetoothRadioDisconnect.TrySend` と同じ。差し替える（ログの文言は `TrySend` が出す）。→ 約 520 行。**500 行未満まであと一歩**で、Disconnect の出力の後始末を `SwitchProCommandChannel` へ寄せれば届く見込み。

### 3.2 触覚（基底の出力）
| 型 | 持つ状態 | 主な操作 |
|---|---|---|
| `HapticOutputState` | `currentHap`・`testRumble`・`previousHapticState`、`rumbleAutostopTimer`、自動停止の時間 | `SetRumble`、`MergeStates`、`SetLightbarState`、`RightLightFastRumble` などのアクセサー、`IsDirty`・`ClearDirty` |

- 基底は `protected HapticOutputState Haptics { get; }` を 1 つ持つ。機種の `currentHap.…` は `Haptics.Current.…` へ機械的に置換（61＋19＋7 行、7 ファイル）。
- 期待: 基底の約 100 行が移る（1,060→約 960）。これだけでは 500 行にならない（残りはイベント・設定のアクセサー）。

### 3.3 電池・接続状態
| 型 | 持つ状態 | 主な操作 |
|---|---|---|
| `DeviceBatteryState` | `battery`・`charging` と、`BatteryChanged`・`ChargingChanged` | `UpdateBattery`、`UpdateCharging`（値が変わったときだけ通知）。`DecodeBatteryPercent` の寄せ先 |

- 基底の公開 API（`Battery`・`Charging`・`BatteryChanged`…）は、委譲で残す。イベントの `sender` は、基底（`InputDeviceCore`）のまま渡す（受け手は `sender` を使うため。ガード: テストで固定）。
- 期待: 約 60 行。

### 3.4 DualSense・DS4 の出力の組み立て
- 既存の `DualSenseOutputReportBuilder.Fill(buffer, bt, in DualSenseOutputState)` がすでに「状態を値で受ける」形で、方向は正しい。残りの `PrepareOutReport`（約 130 行）は、**変更の検出（`change`）と、待機の時計（`standbySw`）、BT の送出間隔**。これを `DualSenseOutputScheduler`（状態: `outputDirty`・`standbySw`・前回の触覚・BT の送出カウント）へ移す。入力は `HapticOutputState`（3.2）を引数で受ける。
- DS4 は同様に `DualShock4OutputScheduler`（`CheckOutputReportTypes`・`PrepareOutputReportInner`・`SendOutputReport` の約 250 行）。
- 期待: DualSense 1,133→約 800、DS4 835→約 600。**500 未満には届かない**（入力の読み取り `ProcessFrame` 約 100 行、設定・Types の 123 行が残る）。

## 4. 期待値（正直な見積り）
| 型 | 現状 | 3.1〜3.4 の後 | 500 未満か |
|---|---|---|---|
| `SwitchProDevice` | 1,035 | 約 500〜520 | ほぼ（切断の整理次第） |
| `InputDeviceCore` | 約 1,060 | 約 850 | いいえ |
| `DualSenseDevice` | 1,133 | 約 800 | いいえ |
| `DualShock4Device` | 835 | 約 600 | いいえ |

→ 500 行以上の型は、30→29〜30 件（Switch Pro が 500 を切れば 1 件減）。**指標としての効果は小さい**。得られるのは、(a) 初期化・出力の組み立てが単体で試験できる（偽の `IHidTransport` で、サブコマンドの順序・較正の読み取りを検査できる。今は実機が要る）、(b) 新機種が触覚・電池・較正の持ち主を迷わない、という構造上の利点。

## 5. リスク
- **触覚の置換は 61＋19 行、7 ファイル**。機械的だが、DS4 の出力が `currentHap` を毎フレーム読み書きする経路（`SlotStateHotPathAllocationTests`）に触れる。構造体のコピー（`currentHap = testRumble.rumbleState` 等）が、プロパティ越しになると**意図せぬコピー**になる。→ `ref` で返す（`ref DS4HapticState Current`）か、フィールドのまま持たせる。
- イベントの `sender`、スレッド間のタイミング（`lock (outputReport)` が基底と機種で共有）。ロックの対象は `outputReport` のまま変えない。
- 実機でしか確かめられない経路: Switch Pro の起動手順の順序（USB・BT）、較正の読み取り、DualSense の BT の送出。**後回しの実機確認（§9l）が必須**になる。

## 6. 提案する実施順（承認後。ブランチは `PhaseRV14` のまま）
1. RV14-2a: `SwitchProCalibration`（データだけ。`ProcessFrame` の読み取りを置換）
2. RV14-2b: `SwitchProCommandChannel`（通信路。`Subcommand` の特性化テスト：偽の `IHidTransport` で、送ったバイト列と再試行の回数を固定）
3. RV14-2c: `SwitchProSession`（`SetOperational`・`CalibrationData`。サブコマンドの順序の特性化テスト）。`DisconnectBT` の重複を `BluetoothRadioDisconnect` へ
4. RV14-3a: `DeviceBatteryState`
5. RV14-3b: `HapticOutputState`（`ref` で返す。割り当てのテストで確認）
6. RV14-4: `DualSenseOutputScheduler`・`DualShock4OutputScheduler`
7. RV14-5・6: 検査、再採点、文書、マージ

各ステップで Debug ALL（`dotnet build DS4WindowsTests` を忘れず含める）。コミットは 1 ステップにつき 1 つ。

## 7. 決定してほしい事項
- **D4 範囲**: (A) Switch Pro だけ（3.1。効果が確実で、実機の差も限定的）／(B) 3.1＋3.3（電池）／**(C) 3.1〜3.4 すべて（推奨しない。触覚の置換が広く、効果が小さい割にリスクが大きい）**。
- **D5 触覚の持ち方**: (A) `ref` で返す文脈型／(B) フィールドのまま、`HapticOutputState` は作らない（3.2 を見送る）。
- **D6 終了の基準**: 500 行以上の型の件数ではなく、「単体で試験できる部品へ分けた」ことを成果とする（4 章の理由）。同意するか。

推奨: **D4＝(A)、D5＝(B)（3.2 は見送り）、D6＝同意**。理由: 効果と安全のつり合いが最もよい。DualSense・DS4 の出力（3.4）は、触覚の文脈型なしには進めにくいため、見送りとして記録する。
