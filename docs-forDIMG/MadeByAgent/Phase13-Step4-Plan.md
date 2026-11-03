# Phase13-Step4 個別計画書: ServiceProviderHolder の撤廃（静的 ActionManager の廃止と注入）

作成日: 2026-11-04
状態: **実装完了（2026-11-04。§9 の 1〜6 すべて承認済み。Debug ALL 警告 0・エラー 0、全テスト合格。コミットは Step4 全体で 1 つ。コレクション名の改名は別コミット）**
ブランチ: `Phase13`（Step3 は `b8ba2fca` でコミット済み）
上位: `Phase13-Plan.md` §4（Step4）

## 1. 目的

`ServiceProviderHolder`（静的な Service Locator）を完全に撤廃する。唯一の実質的な利用者は静的クラス `ActionManager`（312 行）と `DefaultKeyButtonActionControllerFactory` である。静的 `ActionManager` を廃止し、呼び出し元へ `IManagedActionManager` などをコンストラクターで注入する。完了後の目標値: `ServiceProviderHolder` 0 件、`AppHost.SetProvider` 呼び出し 0 件、`ActionManager`（静的）0 件。

## 2. 現状（2026-11-04 の実測）

### 2.1 `ServiceProviderHolder`（本番）

| 場所 | 件数 | 内容 |
|---|---|---|
| `Actions/ActionManager.cs` | 8（`.Provider`） | 各メソッドが `IManagedActionManager` などを解決して委譲。解決できないとき（プロバイダー無し）は、自前の辞書で動く**静的フォールバック**を持つ（`SetToggledOn` は例外） |
| `Actions/DefaultKeyButtonActionControllerFactory.cs` | 2 | `IControllerRegistry` を解決して `Register` |
| `DI/AppHost.cs` | `SetProvider` 2 | ホスト構築直後に設定 |
| `DI/ServiceProviderHolder.cs` | 定義 | `GetRequiredService` は呼び出し元 0 件（テストのみ） |

### 2.2 静的 `ActionManager` の呼び出し元（本番）

| 呼び出し元 | 使うメンバー | 生成の経路 |
|---|---|---|
| `Actions/KeyAction.cs` | `GetStateFor`、`GetOrCreateControllerForAction`（×2 箇所） | `KeyOutputAction` が `new`（`SpecialActionOutputFactory` が `KeyOutputAction` を作る。同ファクトリーは既に `_manager` を持つ） |
| `Actions/KeyButtonActionController.cs` | `ToggledOnChanged +=`（×2）、`GetStateFor`、`SetToggledOn`（×2） | `DefaultKeyButtonActionControllerFactory`（`new`）。フォールバックの `new` が `KeyButtonControllerRegistry`（2）と `KeyButtonActionControllerAdapter`（1）にある |
| `Actions/MacroAction.cs` | `GetStateFor`、`SetToggledOn`（`_manager` が null のときの静的フォールバック） | `SpecialActionOutputFactory`（`_manager` を渡している） |
| `DS4Control/Services/MacroPlayback.cs` | `GetStateFor`（×3）、`IsBeingTriggered`（×4） | DI の Singleton |
| `DS4Control/Services/ProfileApplicationService.cs` | `ClearAllEntries`、`ClearDeviceState`、`PreallocateForProfileApply` | DI の Singleton |
| `DS4Control/Services/SpecialActionRepository.cs` / `SpecialActionsXmlSerializer.cs` | `ClearAllEntries`（1 / 3） | DI の Singleton |
| `DS4Control/Services/ActionsLoadedRuntimeReset.cs` | `ClearAllToggledOn` | DI で登録 |
| `DS4Control/SyntheticInputState.cs` | `ClearToggledOnForKey`（`ResetRuntimeStateForKey` の中） | `new SyntheticInputState()` がテストに 16 ファイル |
| `DS4Control/Mapping.cs` | `ToggledOnChanged +=`（静的コンストラクター。ログのみ） | DI の Singleton |
| `DS4Control/ButtonSpecialActionOutput.cs` | `GetStateFor`、`SetToggledOn` | **本番の呼び出し元 0 件**（§2.4 参照） |
| `Actions/DefaultActionManager.cs` | `ActionManager.FireToggledOnChanged`（×2） | 自分のイベントに加えて静的イベントも発火している |

### 2.3 重要な事実

- `IManagedActionManager`（DI の Singleton `DefaultActionManager`）が全メンバーの本体を持つ。静的 `ActionManager` は、ホストがあれば**すべて委譲するだけ**で、本番（ホストあり）の動作は `DefaultActionManager` のとおり。したがって、静的フォールバック（辞書）は本番では使われない。
- ただし静的版にだけある振る舞い:
  1. **`ToggledOnChanged` は静的イベントと、インスタンスのイベントの二重**。`DefaultActionManager` は両方を発火する。購読者は `KeyButtonActionController`（静的に購読）と `Mapping` のログ（静的に購読）。静的 `ActionManager` の静的コンストラクターにも、ログのみの購読がある。
  2. `GetOrCreateControllerForAction` と `PreallocateForProfileApply` は、`IManagedActionManager` に無い（`IKeyButtonControllerRegistry`／`IProfileActionProvider` を使う処理）。
  3. `IsBeingTriggered`／`SetBeingTriggeredFor` は `IManagedActionManager` に無い（`MacroPlayback` の TODO に記載済み）。`SetBeingTriggeredFor`（private）は呼び出し元 0 件。
  4. `PreallocateOnStartup` は呼び出し元 0 件（本番もテストも）。
- 循環の確認: `DefaultActionManager` は `Func<IMapping>` を遅延で受けるだけで、他の依存を持たない。よって、`IManagedActionManager` を `KeyButtonControllerRegistry`／`ProfileApplicationService`／`SpecialActionRepository`／`SpecialActionsXmlSerializer`／`ActionsLoadedRuntimeReset`／`MacroPlayback`／`Mapping`／`DefaultKeyButtonActionControllerFactory`／`SpecialActionOutputFactory` に足しても、生成時の循環は生じない。

### 2.4 未参照コードの判断（規約 §2.4）

| 対象 | 経緯（`git log -S`） | 判断 |
|---|---|---|
| `ActionManager.PreallocateOnStartup` | 初期の静的版の起動時の先割り当て。現在は `DefaultActionManager.PreallocateEntries`（`AppHost` が呼ぶ）が担う | **使うべきでない並行実装＝削除** |
| `ActionManager.SetBeingTriggeredFor`（private） | 同上。呼び出し元 0 件 | **削除** |
| `ButtonSpecialActionOutput`（静的クラス）とそのテスト `ButtonSpecialActionOutputTests` | Phase8-Step8 8b で Stage3 を削除した際の残骸。規則は `ButtonOutputAction`／`FreshPressGuardTable` に移った（コメントに「同じ規則」と記載）。本番の呼び出し元 0 件 | **廃止済み機能の残骸＝削除（要承認。テストの削除を伴う）** |

## 3. 設計

### 3.1 契約の変更

- `IManagedActionManager` に `bool IsBeingTriggered(SpecialAction action, int device)` を追加（`DefaultActionManager` は `GetStateFor(...)?.BeingTriggered ?? false`。静的版と同じ。例外は握りつぶす）。
- `ToggledOnChanged` は、**インスタンスのイベントのみ**にする。静的イベントの発火（`ActionManager.FireToggledOnChanged`）を `DefaultActionManager` から削除する。購読者は注入された `IManagedActionManager` のイベントを購読する。
- 静的版の購読ログ（`ActionManager.ToggledOnChanged: ...`）は、`DefaultActionManager` のコンストラクターで同じ文言のまま購読して維持する。`Mapping: ToggledOnChanged ...` のログは、`Mapping` のコンストラクターで購読して維持する（従来は静的に 1 回。`Mapping` は本番で Singleton のため、本番の購読回数は 1 回で変わらない）。

### 3.2 呼び出し元ごとの置き換え

| 呼び出し元 | 置き換え |
|---|---|
| `DefaultKeyButtonActionControllerFactory` | `IControllerRegistry` と `IManagedActionManager` をコンストラクターで受ける（4a）。`ServiceProviderHolder` 解決を削除 |
| `KeyButtonActionController` | コンストラクターに `IManagedActionManager` を追加（ファクトリー、`KeyButtonControllerRegistry` のフォールバック、`KeyButtonActionControllerAdapter` が渡す）。静的 `ActionManager.*` を `_manager.*` に |
| `KeyButtonControllerRegistry` | `IManagedActionManager` を受けて、フォールバックの `new` に渡す |
| `KeyAction` | `IManagedActionManager` と `IKeyButtonControllerRegistry` を受ける。`GetOrCreateControllerForAction` は `registry.GetOrCreateForAction(device, action)`（静的版と同じ。例外時 null） |
| `KeyOutputAction`／`SpecialActionOutputFactory` | 上の 2 つを `KeyAction` へ渡す（ファクトリーが保持して渡す） |
| `MacroAction` | `_manager` を必須にして、静的フォールバックを削除 |
| `MacroPlayback` | `IManagedActionManager` を受ける。TODO コメント（`MacroPlayback` の技術的負債の記述）を削除 |
| `ProfileApplicationService` | `IManagedActionManager` を受ける。`PreallocateForProfileApply` の本体（`IProfileActionProvider` で名前を列挙 → `GetStateFor`＋`_keyButtonControllers.GetOrCreateForAction`）をこのサービスの private メソッドへ移す。ログ文言は同じ |
| `SpecialActionRepository`／`SpecialActionsXmlSerializer` | `IManagedActionManager` を受けて `ClearAllEntries`（`ServiceRegistration` の lambda の引数も追加） |
| `ActionsLoadedRuntimeReset` | `IManagedActionManager` を受けて `ClearAllToggledOn` |
| `SyntheticInputState` | コンストラクターは変えない（テストで 16 ファイルが `new SyntheticInputState()`）。`ResetRuntimeStateForKey(ushort key, IManagedActionManager actions)` と引数で受ける（呼び出し元は `SpecialActionRepository` の 1 か所。順序・ログは同じ） |
| `Mapping` | `IManagedActionManager` を受ける（静的コンストラクターのログ購読をコンストラクターへ） |
| `AppHost` | `SetProvider` の 2 呼び出しを削除（4c） |

### 3.3 削除

`Actions/ActionManager.cs`（静的クラス全体）、`DI/ServiceProviderHolder.cs`、`DS4Control/ButtonSpecialActionOutput.cs`（承認後）、`ActionManager.FireToggledOnChanged` 呼び出し 2 件。

## 4. Step の分割とコミット

| サブ Step | 内容 | 検証 |
|---|---|---|
| **4a** | `DefaultKeyButtonActionControllerFactory` の注入（`ServiceProviderHolder` 2 件 → 0） | Debug ALL。ラチェット基準値 `Factory` 2 → 0 |
| **4b-1** | `IsBeingTriggered` の追加。`ToggledOnChanged` を注入に。`KeyButtonActionController`／`KeyButtonControllerRegistry`／`KeyAction`／`KeyOutputAction`／`SpecialActionOutputFactory`／`MacroAction`／`MacroPlayback` の置き換え | Debug ALL |
| **4b-2** | `ProfileApplicationService`／`SpecialActionRepository`／`SpecialActionsXmlSerializer`／`ActionsLoadedRuntimeReset`／`SyntheticInputState`／`Mapping` の置き換え。`ActionManager.cs` と `ButtonSpecialActionOutput.cs`（承認後）の削除。静的 `ActionManager` を使うテストの移行 | Debug ALL。`ActionManager` の参照 0 |
| **4c** | `ServiceProviderHolder.cs` と `AppHost.SetProvider` の削除。ラチェット（`Phase7Step1ServiceLocatorRatchetGuardTests`、`Phase7Step10eW5RatchetEqualityGuardTests`）の更新。テストの `[Collection("ServiceProviderHolder")]` 133 ファイルを**別コミット**で改名 | Debug ALL |

4a→4c の順にコミットする（各サブ Step のあとに Debug ALL で警告・エラー 0 を確認。4b-1 までは静的 `ActionManager` が残るため中間も緑）。

## 5. テストの方針

- **新規**: `Phase13Step4ActionManagerRemovalGuardTests`（`ActionManager`／`ServiceProviderHolder` の型と、`DS4Windows/` 配下の参照が 0 であること。`AppHost.SetProvider` が無いこと）、`DefaultActionManager.IsBeingTriggered` のテスト、`ToggledOnChanged` が 1 回だけ発火し、`KeyButtonActionController` が注入されたマネージャーの購読で反応すること。
- **移行（既存）**: 静的 `ActionManager.*` を使う 27 ファイル（72 箇所）は、`TestHost.GetService<IManagedActionManager>()`（または各テストが組み立てる `DefaultActionManager`）へ。`SetProvider` を使う 5 ファイル（`ToggleAndToggledOnTests`、`ManagedActionManagerRegistrationTests`、Standalone の 2 ファイル、…）は、`TestServiceProvider` を介さず、部品を直接渡す形へ。
- **`ManagedActionManagerRegistrationTests`** のうち `ServiceProviderHolder.GetRequiredService` を検査する 4 件は、機能ごと消えるため削除（フォールバックの有無を問う「登録が欠けたときの例外」は、`IManagedActionManager` が必須の引数になるため、コンストラクターの `ArgumentNullException` の検査に置き換える）。
- **Standalone**: `ActionManagerSmokeTests`（静的フォールバックの辞書を検査）は `DefaultActionManager` を直接使う形に書き換える。`MappingControllerDisposeBehaviorTests`／`MappingControllerLifecycleTests` は `ClearDeviceState` を `DefaultActionManager` 経由に。
- 文字列検査のガード（`ActionManager` という文字列を含むもの）は、実装時に洗い出して更新する。

## 6. 規約への適合

- **§2.2**: 安全弁の対応表。(1) トグル状態の解除（`ClearAllToggledOn`／`ClearToggledOnForKey`／`ClearDeviceState`）は、静的版が委譲していた `DefaultActionManager` の本体をそのまま使う（本番の動作は変わらない）。(2) Fresh-press ガードの `RequiresFreshPressAfterReset` は `ActionInstanceState` の中にあり、`ClearDeviceState` が新しい状態を作ることで維持される（`DefaultActionManager` の本体）。(3) プロファイル適用の順序（`ClearAllEntries` → `ClearDeviceState` → `PreallocateForProfileApply` → 束縛の登録）は `ProfileApplicationService` の中で同じ。(4) 切断時の `Mapping.HandleDeviceDisconnect`（復帰の合図の退避・復元つき）は `DefaultActionManager.ClearDeviceState` が持つ。
- **§2.3**: ログの文言・レベルは変えない。`ActionManager.ToggledOnChanged: ...` と `Mapping: ToggledOnChanged ...` の購読ログは維持する。静的版だけにあった `ActionManager.ClearDeviceState: reset ActionInstanceState ...`（フォールバックの辞書用）は本番では出ない経路のため、廃止に伴い消える（`DefaultActionManager.ClearDeviceState` の同種のログが出る）。
- **§3.1**: `AppHost.SetProvider` を削除する。ラチェットは、減らす分を基準値と同時に更新する。
- **§3.3 原則1・2**: 新規ファイルは 1 型 1 ファイル。ファイルの削除は `ActionManager.cs`、`ServiceProviderHolder.cs`、`ButtonSpecialActionOutput.cs`。
- **§4 検索**: 本 Step は新機能ではなく、既存の静的ファサードを注入に替える作業のため、外部の OSS の比較は不要。

## 7. リスクと対策

| # | リスク | 対策 |
|---|---|---|
| R1 | `KeyButtonActionController` が静的イベントを購読していたため、注入したマネージャーを取り違えると購読が外れる（トグルを OFF にしても連打が止まらない） | 購読と解除（`Dispose` での `-=`）の両方を、注入したインスタンスに対して行う。テストで「OFF→連打停止」を検証。実機 H（トグルのキー割り当て） |
| R2 | `Mapping` の購読がインスタンスごとになり、テストで複数の `Mapping` を作るとログが重複する | ログのみ。本番は Singleton で 1 回 |
| R3 | `ProfileApplicationService` に `IManagedActionManager` を足したことによる循環 | §2.3 のとおり `DefaultActionManager` の依存は `Func<IMapping>` のみで、循環しない。起動のテスト（`TestHost` の全サービスの解決）で確認 |
| R4 | テスト 27＋133 ファイルの移行の量 | 4b-2 の移行は機械的な置換（ヘルパーを 1 つ追加）。コレクション名の改名は別コミット |
| R5 | 静的フォールバックを頼りに動いていたテストが緑のまま空振りする | 移行後、`ActionManager`／`ServiceProviderHolder` の参照 0 をガードテストで保証。移行した各テストは、元の主張（状態が変わる等）を保つ |

## 8. 実機確認（Phase 末にまとめて。`Phase13-Plan.md` の H 項目に追加）

- トグルのキー割り当て: 押すたびに ON/OFF が切り替わり、OFF で連打が止まる。
- キー・ボタン・マクロの特殊アクション（Press／Toggle）。マクロの繰り返し。
- プロファイル切り替え後、押しっぱなしでも連鎖して発火しない。
- コントローラーの切断で、保持中のキーが解放される。

## 9. 決定事項（承認をお願いします）

1. 静的 `ActionManager` を完全に廃止し、静的フォールバックの辞書も削除する（Phase13-Plan で承認済みの方針の具体化）。
2. `ToggledOnChanged` は、静的イベントを廃止してインスタンスのイベントだけにする。
3. `ActionManager.PreallocateOnStartup`／`SetBeingTriggeredFor` を削除する（§2.4。呼び出し元 0 件の並行実装）。
4. `ButtonSpecialActionOutput` と `ButtonSpecialActionOutputTests` を削除する（§2.4。廃止済みの残骸。テストの削除を伴う）。
5. サブ Step 4a／4b-1／4b-2／4c の順に実装し、コミットは Step 全体で 1 つ（または、ご希望ならサブ Step ごと）。
6. `[Collection("ServiceProviderHolder")]` の改名は、4c の別コミットで行う（名前の候補: `SharedHost`）。
