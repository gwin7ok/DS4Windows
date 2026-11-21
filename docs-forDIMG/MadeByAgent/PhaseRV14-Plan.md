# PhaseRV14 計画書: 入力機器の型の整理（InputDeviceCore と 3 機種）

作成日: 2026-11-21
状態: **計画。主題は (A) 入力機器の型の整理（ユーザー選択済み）。実機確認は最後にまとめて後回し（ユーザー選択済み）。着手の承認待ち**
出典: `02-Scorecard.md`（RV13 まで）、`PhaseRV12-Plan.md`（型の分割の方針 D1〜D3）。
動き: **挙動・ログ・保存の形は変えない**（機能の削減なし）。型の置き場所と責務を整える。

## 目的
1. 型の単位で 500 行以上の 4 件を 500 行未満にする: `InputDeviceCore`（1,187）、`DualSenseDevice`（1,133）、`SwitchProDevice`（1,035）、`DualShock4Device`（835）。
2. 機種の差と、全機種で共通の処理（切断、アイドル判定、電池、出力の書き込み）を、型として分ける。`partial` への分割では指標も構造も動かない（RV11b の結果）ので、**別の型へ切り出す**。

## 前提（調べた事実、2026-11-21）
- `InputDeviceCore` は `partial` 6 ファイル（Basics・Disconnect・Idle・Output・Status と本体）。切断（`DisconnectBT/Dongle/Wireless`）、アイドル判定（`IsDS4Idle`・`EvaluateIdleDisconnect`）、電池（`UpdateBattery`・`UpdateCharging`・`GetBattery`・`DecodeBatteryPercent`）、出力（`WriteOutput`・`SetRumble`・`MergeStates`）、イベントの待ち行列（`QueueEvent`・`HaltReportingRunAction`）が同居している。
- 切り出し済みの先例: パーサー（`Input/Parsers`）、出力レポートの組み立て（`Input/Reports`）、`ProtocolDevice` ＋ `IDeviceProtocol`（Xbox）。
- 3 機種は、読み取りのループを `InputLoopDevice` に任せ、差し込み口（`ReceiveFrame`・`ProcessFrame`・`WriteOutputFrame` ほか）だけを書いている（Phase18）。
- `Switch Pro` は、初期化（`SetOperational`・`RunUSBSetup`・`Subcommand`・`CalibrationData`）が約 350 行で、独立して切り出せる。`DualSense` は、出力レポートの組み立て（`PrepareOutReport` 約 130 行、LED・ミュート・トリガー効果）が `Reports/DualSenseOutputReportBuilder` と重なる部分がある。

## 決定してほしい事項

### D1 切り出しの形
- **(A) 同層の internal な協力クラス（推奨）**: 例 `DeviceIdleMonitor`、`DeviceBatteryTracker`、`DeviceDisconnector`、`DeviceEventQueue` を `InputDeviceCore` が持つ。機種の側は、`SwitchProSetup`（初期化）、`DualSenseOutputPlanner`（出力の組み立て）など。利点: 呼び出し側（`Mapping`・UI・出力層）の API が変わらず、差分が機種の内部に収まる。欠点: 協力クラスが基底のフィールドを参照するため、渡し方（状態の窓口）の設計が要る。
- (B) `IDeviceProtocol`＋`ProtocolDevice` へ全機種を載せ替える（Xbox と同じ形）。利点: 理想の構造に近い。欠点: 3 機種の挙動を全面的に再実装する規模で、実機での差が出やすい。別の Phase 向け。
- (C) 基底だけ分ける（3 機種は触らない）。利点: 小さい。欠点: 500 行以上が 1 件しか減らない。

### D2 範囲
- **(A) `InputDeviceCore` ＋ 3 機種（推奨）**。1,187→約 450、3 機種を各 500 未満へ。
- (B) `InputDeviceCore` ＋ `SwitchProDevice` だけ（初期化の切り出しが最も明確）。効果は 2 件。

### D3 コミットの粒度（過去の RV12 の前例に合わせる）
- **(A) 型ごとに 1 コミット（推奨）**: 壊れたとき、戻しやすい。
- (B) 全体で 1 コミット。

## Step
| Step | 内容 | 検査 |
|---|---|---|
| RV14-0 | 特性化テストの確認。`InputDeviceCore` の切断・アイドル・電池・出力の書き込みに、既存のテストが足りない分を足す（偽の `IHidTransport` ＋ `TimeProvider`）。3 機種はパーサー・レポートの差分テストがあるので、足りない分岐だけ足す | 追加テストが今のコードで合格する（変更前の挙動の固定） |
| RV14-1 | `InputDeviceCore` の切り出し: アイドル判定 → `DeviceIdleMonitor`、電池 → `DeviceBatteryTracker`、切断 → `DeviceDisconnector`、イベントの待ち行列 → `DeviceEventQueue`。公開メソッドは基底に薄い委譲を残す（呼び出し側は変えない）。規約の「新しい静的な窓口を作らない」に従い、状態は協力クラスが持つ | Debug ALL、`ConstructorArityRuleTests`、`SlotStateHotPathAllocationTests` |
| RV14-2 | `SwitchProDevice`: 初期化・サブコマンド・校正を `SwitchProSetup` へ。`SwitchProDevice.Setup.cs` の `partial` を廃止 | 同上＋`SwitchProReportParser` の差分テスト |
| RV14-3 | `DualSenseDevice`: 出力の組み立て（LED・ミュート・スロット番号・トリガー効果）を `DualSenseOutputPlanner` へ。`Reports/DualSenseOutputReportBuilder` と重なる処理は寄せる | 同上＋`DualSenseOutputReportBuilder` のテスト |
| RV14-4 | `DualShock4Device`: 出力レポートの種類の判定と組み立てを `DualShock4OutputPlanner` へ | 同上 |
| RV14-5 | 検査: `DeviceLoopStructureTests`・`LayerDependencyRuleTests` の範囲確認。協力クラスが基底へ逆参照しないこと（型の検査）。ラチェット（`ConstructorArityBaseline`、`CodeHealthBaseline`）を実数へ | 全テスト、わざと違反を入れる負の確認 |
| RV14-6 | 文書: `01-Structure.md`・`02-Extension-Guide.md`（新機種の足し方）の更新。`02-Scorecard.md` に RV14 の採点（`Score-CodeReview.py`、`scorecard/RV14.json`）。実機確認 §9l を `Device-Check-Checklist.md` へ追記 | `ModelDiagramGuardTests` |

実施順は RV14-0 → 1 → 2 → 3 → 4 → 5 → 6。各 Step の終わりに Debug ALL（警告 0・全テスト合格）。ブランチは `PhaseRV14`。

## 守るもの（ガード）
- 読み取りループのスレッドの構成、切断時のクリーンアップの順序、`HaltReportingRunAction` による直列化（ガードレール 3・6）。
- ログの文言とレベル（`[DI]` 接頭辞を含む）。
- 割り当てゼロの経路（`SlotStateHotPathAllocationTests`）。協力クラスの呼び出しで、1 フレームごとの割り当てを増やさない。

## 実機確認（最後にまとめて。D は後回しで承認済み）
`Device-Check-Checklist.md` §9l へ追記する項目: DS4・DualSense・Switch Pro の USB／Bluetooth での接続、電池表示、アイドル切断、Bluetooth の切断（スタートメニュー・トレイ）、ライトバー・ランブル・DualSense のトリガー効果・プレイヤー LED。

## 予想（03-Rubric.md §5）
- 500 行以上の型 30→26。オブジェクト指向 87.7%→約 89%（等級は A− のまま）、メンテナンス性 96.7%→約 97%。
- 計測に現れにくい改善: 基底クラスの責務が、切断・アイドル・電池・出力に分かれ、機種の追加で触る範囲が小さくなる。

## リスク
- 基底のフィールド（`conType`、`inputReport`、状態の配列）を協力クラスへ渡す形が、使いにくい窓口（何でも箱）にならないよう、必要な値だけを引数で受ける。
- 実機を要する経路（Bluetooth の切断、トリガー効果）は、単体テストでは検出できない。実機確認が後回しなので、差分は小さくし、型ごとのコミットで戻せるようにする。

## 変更履歴への追記
- 2026-11-21: 作成。主題 (A)、実機確認の後回しをユーザーが選択。D1〜D3 は着手の承認時に決める（推奨は D1＝(A)、D2＝(A)、D3＝(A)）。
