# Phase13-Step1 個別計画書: トリガー系 4 クラスの未使用コンストラクター引数の整理

作成日: 2026-11-04
状態: **承認待ち**
ブランチ: `Phase13`
上位: `Phase13-Plan.md` §4（Step1）、`Phase11-Completion-Report.md` §6

## 1. 目的

Phase11 の `DebouncingMs` 廃止（K-W6-10）で、トリガー系クラスは `IProfileSettingsService profileSettings` を使わなくなった。呼び出し側とテストの変更を避けるため引数だけが残っている。これを削除し、呼び出し側とテストを同時に更新する。**振る舞いは変えない。**

## 2. 現状（2026-11-04 の実測）

| クラス | 該当の引数 | 現状の使われ方 |
|---|---|---|
| `ControlButtonInputAction` | `IProfileSettingsService profileSettings = null`（末尾の任意引数） | `delaySeconds > 0 && profileSettings == null` のとき `ArgumentNullException`。値は使わない |
| `ComboTriggerInputAction` | 第 3 引数 `profileSettings`（必須） | null 検査のみ |
| `MultiTapInputAction` | 第 3 引数 `profileSettings`（必須） | null 検査 + `ComboTriggerInputAction` へ転送 |
| `TwoStageTriggerProcessor` | 第 1 引数 `profileSettings`（必須） | null 検査のみ |
| `ControlMultiTapInputAction`（計画書に追加） | 第 3 引数 `profileSettings`（必須） | `MultiTapInputAction` へ転送のみ |

呼び出し側（本番）:
- `ControlBindingBuilder`: `ControlButtonInputAction` 5 か所（`profileSettings` は渡していない。変更不要）
- `AssignmentBindingBuilder`: `ComboTriggerInputAction` 2（53、94 行）、`MultiTapInputAction` 1（93）、`ControlMultiTapInputAction` 1（79）、`ControlButtonInputAction` 1（87。`profileSettings: _profileSettings` を渡している）
- `ServiceRegistration.cs` 227 行: `TwoStageTriggerProcessor` は DI 登録（`Func<DateTime>` は任意引数で、コンテナからは解決されない形。登録の変更は不要の見込み。型が解決できることをテストで確認する）

テスト側: `new` は `ControlButtonInputAction` 4 ファイル、`ComboTriggerInputAction` 5 ファイル（`ComboTriggerInputActionTests`、`Phase10Step4W4ControlSpecialTests`、`Phase8Step3PerformanceTests`、`Phase8Step4W4WiringTests`、`R5SaDigitalJudgementTests`）、`MultiTapInputAction` 1 ファイル、`TwoStageTriggerProcessor` 2 ファイル（`Phase9MapCustomTwoStageDifferentialTests`、`TestMappingFactory`）。`ControlMultiTapInputAction` を直接 `new` するテストは無い。

## 3. 変更内容

1. 上記 5 クラスから `profileSettings` 引数を削除する。`ControlButtonInputAction` の null 検査（遅延が 0 より大きいとき）も削除する（検査対象の引数が無くなるため。有効な呼び出しの動作は同じ）。
2. 各クラスの「引数を残した理由」のコメントと `<param name="profileSettings">` の説明を削除する（解消した TODO の削除。規約 §3.3 原則 4）。
3. 呼び出し側（`AssignmentBindingBuilder` の 5 か所）と、テストの `new`（約 25 か所・12 ファイル）を更新する。
4. `TwoStageTriggerProcessor` の `Func<DateTime> now` は、先頭引数になる。DI の解決（`ITwoStageTriggerProcessor`）が通ることを確認する。
5. `AssignmentBindingBuilder` の `_profileSettings`: 上の変更後に他で使われていなければ、フィールドと引数も削除する（呼び出し 2 か所［`ActionBindingBuilder`、`ControlBindingBuilder`］とテスト 3 か所が連動）。**他で使われていれば残す**（実装時に確認し、結果を記録）。

## 4. 規約への適合

- 機能維持（§2.2）: 判定・遅延・反転の処理は変えない。引数の削除のみ。
- ログ（§2.3）: 変更なし（これらのクラスにログの変更は無い）。
- 1 ファイル 1 型、配置、名前空間: 新規ファイルなし。移動なし。
- ラチェット: `Global`／`AppHost`／`ServiceProviderHolder` の参照は増減なし。基準値の変更は不要（`Phase7Step10eW5RatchetEqualityGuardTests` で確認）。

## 5. テスト計画

- 既存テストを更新して、`dotnet test` で件数が減らないこと（引数を直しただけで、検証内容は同じ）。
- 追加（小）: 引数削除後も次が成り立つ特性化テスト。(a) `ComboTriggerInputAction`／`MultiTapInputAction` が `profileSettings` なしで同じ判定をする（既存の `ComboTriggerInputActionTests`、`Phase8Step77bW2MultiTapTests` が担う。足りない分だけ追加）。(b) `TestHost.GetService<ITwoStageTriggerProcessor>()` が非 null。
- `Debug ALL`（`Remove-bin` → Build Debug → Build Test → Execute Test）で警告 0・エラー 0。

## 6. 実機確認

不要（純粋な署名変更）。Phase 末の実機確認 H4（割り当ての動作）で担保する。

## 7. リスク

- 低。コンパイルエラーが取りこぼしを教えてくれる。`ControlButtonInputAction` の任意引数は名前つきで渡されている箇所があるため、検索で漏れを確認する。

## 8. 決めてほしいこと

- なし（§3 の 5 は既定案で進める。`AssignmentBindingBuilder` の連動削除は、実装時に不要と確認できた場合のみ行う）。

## 9. 完了条件

`profileSettings` 引数が 5 クラスから無くなり、コメントも整理済み。`Debug ALL` が警告 0・エラー 0・全テスト合格。`Phase13-Status.md` に Step1 の結果を記録。
