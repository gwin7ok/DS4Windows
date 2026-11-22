# PhaseRV6-6 公開の可変フィールドの調査（記録のみ）

作成日: 2026-11-22
対象: `SpecialAction`（27）、`TouchMouseStickInfo`（17）、`GyroMouseStickInfo`（16）の public フィールド。実施（プロパティ化）はしない。

## 調査の結果
- **シリアライズ**: 3 型とも、`XmlSerializer` の型には使っていない（`typeof(...)` で渡しているのは `ActionsDTO`・`AutoProfilesDTO`・`AppSettingsDTO` など別の DTO）。保存は手書きの読み書き（`SpecialActionsXmlSerializer`・`LegacyProfileXmlSerializer`・`BackingStore` の節）なので、プロパティにしても保存の形は変わらない。
- **リフレクション**: 3 型のフィールドをリフレクションで読む箇所はない。
- **`ref`・`out` で渡す箇所**: `TouchMouseStickInfo`・`GyroMouseStickInfo` の `deadZone`（2）、`vertScale`（1）、`inverted`（1）。名前だけの数え方のため、2 型の合算の上限。プロパティにすると、この呼び出しは `ref` を渡せなくなり、一時変数を経由する形への書き換えが要る。`SpecialAction` は 0 件。
- **毎フレームの経路**: `GyroMouseStickInfo`・`TouchMouseStickInfo` の `deadZone`・`maxZone`・`antiDead*`・`inverted` などは、入力の加工（`StickDeadZone` 系、`GyroOutputProcessor`）が毎フレーム読む。自動実装プロパティはインライン化されるため性能は変わらないと見込むが、`Benchmark` の前後比較が要る。

## 参照数（名前だけで数えた上限。他の型の同名メンバーを含む）
- `SpecialAction`: 参照するファイルは約 279。多い順に `name` 320、`keyType` 164、`details` 126、`typeID` 65。使われていない（外から 0 件）のは `tappedOnce`・`secondtouchbegin`・`pastTime`・`firstTap`・`TimeofEnd`・`prevProfileWasTemporary`（未参照の可能性。削除は規則 2.4 に従い別に承認）。
- `TouchMouseStickInfo`・`GyroMouseStickInfo`: 参照するファイルは 3〜4。`deadZone` 99、`maxZone` 69、`maxOutput` 57 以外は 15 以下。`trackballMode`・`trackballFriction` は 0 件（未参照の可能性）。

## プロパティ化できる範囲（提案）
| 型 | 範囲 | 難しさ | 備考 |
|---|---|---|---|
| `GyroMouseStickInfo`・`TouchMouseStickInfo` | 全 33 フィールド | 低〜中 | 参照するファイルが 3〜4。`deadZone`・`vertScale`・`inverted` の `ref` 渡し 4 か所だけ書き換える。毎フレームの経路のため、`Benchmark` の前後比較を付ける。 |
| `SpecialAction` | 全 27 フィールド | 中 | 参照するファイルが約 279 で機械的な置換が大きい。先に、外から 0 件のフィールド 6 つの扱い（未参照の調査）を決める。`List<>` のフィールドは、プロパティにしても中身の変更は防げない（読み取り専用の公開が別に要る）。 |

## 推奨
- 効果が大きいのは `GyroMouseStickInfo`・`TouchMouseStickInfo`（小さく、`ref` が 4 か所だけ）。次の機会に、承認を得て実施する。
- `SpecialAction` は、6 つの未参照フィールドの削除の可否と合わせて別 Phase。

## 実施（2026-11-22、ユーザー承認）
規則 2.4 に従い、経緯（上流 DS4Windows のタップ判定・プロファイル復帰の実行時状態。いまは `TapClickDetector`・`ProfileSwitchAction` が持つ。trackball の設定の正本は `TouchpadSlotSettings`）を確かめ、`SpecialAction` の 8 フィールド（tappedOnce・firstTouch・secondtouchbegin・pastTime・firstTap・TimeofEnd・prevProfileName・prevProfileWasTemporary）と `TouchMouseStickInfo` の trackballMode・trackballFriction（定数 2 つ）を削除。状態の黄金ファイル 8 件から該当行を除き、`RemovedApiRuleTests` の表へ追記。
