# Phase8-Step1 W5（D・E）: Actions の静的な窓口の一覧と、Step2 への論点

作成日: 2026-10-19  
対象: `DS4Windows/Actions/`、`DS4Windows/DS4Control/`（`Mapping` ほか）  
上位: `Phase8-Step1-Plan.md` 項目 D・E、`Phase8-Step1-Trigger-Spec-Ledger.md`、`Phase8-Step1-Orphan-Decisions.md`  
性格: 現状の記録（コードは変更していない）。件数は 2026-10-19 の実測（`grep`）。第 1 層（`DS4Devices` ほか）の静的な状態は範囲外（Step1 の Q6＝A）。

---

## 1. `ServiceProviderHolder`（Service Locator）17 件の内訳

ラチェット（`Phase7Step1ServiceLocatorRatchetGuardTests`）の基準値どおり 17 件。**解決先は、DI で登録済みの Singleton**（`IManagedActionManager`、`IMapping`、`IKeyButtonControllerRegistry`、`IActionFactory` ほか）。

| ファイル | 件数 | 関数（静的な窓口） | 呼び出し元（本体） |
|---|---|---|---|
| `ActionManager.cs` | 13 | `GetActionByIndex`、`GetActionByName`、`Actions` | **0 件（削除できる）** |
| 〃 | | `GetStateFor` | 12（`Mapping`、`KeyAction`、`KeyButtonActionController`、`ButtonSpecialActionOutput`） |
| 〃 | | `ClearToggledOnForKey` | 1（`SyntheticInputState`） |
| 〃 | | `ClearAllToggledOn` | 1（`ActionsLoadedRuntimeReset`） |
| 〃 | | `ClearAllEntries` | 5（`ProfileApplicationService`、`SpecialActionRepository`、`SpecialActionsXmlSerializer`） |
| 〃 | | `ClearDeviceState` | 2＋（`Mapping`、`ProfileApplicationService`、`SpecialActionBase`。テスト 14） |
| 〃 | | `SetToggledOn` | 3（`ButtonSpecialActionOutput`、`KeyButtonActionController`、`Mapping`） |
| 〃 | | `DispatchTriggerEstablished`／`DispatchTriggerReleased` | 各 1（`Mapping`）。テスト各 4 |
| 〃 | | `GetOrCreateControllerForAction` | 4（`KeyAction`、`KeyOutputAction`） |
| 〃 | | `PreallocateForProfileApply` | 1（`ProfileApplicationService`） |
| `ActionFactory.cs` | 1 | 静的ファクトリー（DI が無いとき `new KeyActionAdapter` を返す。`TODO(Phase7/Phase8)` 付き） | `ActionEntry`、`ActionManager` |
| `DefaultKeyButtonActionControllerFactory.cs` | 2 | `IKeyButtonActionControllerFactory` の実装内 | DI 経由 |
| `KeyActionAdapter.cs` | 1 | `KeyAction` を作る際のサービス取得 | DI 経由 |

- `ActionManager` の静的 API のうち、**本体の呼び出し元が 0 件のもの**（Step8 の削除候補）: `GetActionByIndex`、`GetActionByName`、`Actions`（いずれも `ServiceProviderHolder` を使う窓口）、`PreallocateOnStartup`、`FireToggledOnChanged`、`DispatchTrigger`。
- `ActionManager` の静的 API にある**フォールバック本体**（DI がないときの `actions` 辞書の直接操作）は、本番では呼ばれない（`IManagedActionManager` の登録により、先に委譲して戻る）。Phase7b W1 の復帰の合図の退避・復元が、`DefaultActionManager.ClearDeviceState` とこのフォールバックの二重になっている。

## 2. その他の静的な窓口（台帳 §17.3 C のとおり。2026-10-19 に追記を確認）

- `ActionManager` の静的な辞書 `actionInstances`／`actions`（`private static readonly` だが中身は可変）。フォールバック用。台帳 §17.3 C に追記済み。
- `ActionRegistry`（静的。`SpecialAction` の一覧。`GetByIndex`／`AllActions`／`Count`／`IsInitialized`）: 利用者は `ActionManager`（4 か所）、`DefaultActionManager`（3 か所）、`Mapping`（`ActionRegistry.IsInitialized`）。**`IActionRegistry` の実装ではない**（別物）。
- `ActionManager.ToggledOnChanged`（静的イベント）: 購読者は `KeyButtonActionController`（3）、`Mapping`（コメント）、テスト。トグル OFF での `impl.ClearAll()` の合図。
- `SyntheticDispatcher`（静的な橋渡し。`bound` に `ISyntheticDispatcher` を保持）: 利用者は `KeyButtonActionController`（`SendPress`／`SendRelease`／`ResolveNativeKey`／`ResetKeyTiming` を約 20 か所）、`KeyAction`（`ResolveNativeKey`）。`TODO(Phase8)` 付き。
- `RepeatHelperToIRepeaterAdapter.RepeaterFactoryOverride`（静的。テスト用のフック）: テストの 2 か所が使う。`KeyButtonActionController` が `new RepeatHelperToIRepeaterAdapter(...)` を 2 か所で直接生成している（DI を通らない。`IRepeater` は DI 登録済み）。
- `KeyboardSettings`（静的。`InitialRepeatDelayMs`／`RepeatIntervalMs`）: OS の設定の読み取り。台帳 §17.3 A のとおり、起動前に必要なため残す。

## 3. Step2（詳細設計）への論点

### 3.1 統一ディスパッチの設計で、決めること
1. **`IActionRegistry` の実装の形**: 実装クラスが存在しない（Phase8 計画書 2026-10-19 追記 §3）。DI 登録するか、`TriggerBinding` のリストをスロットごとに持つ別の型にするか（Step5〜6）。
2. **`ComboTriggerInputAction` のモデルに収まらない仕様**（仕様台帳 §8 の意図不明 9 件のうち、設計に影響するもの）:
   - Stage3 の Button の Press が Fresh-press ガードを見ない（Toggle だけが見る）。統一経路で揃えるか、維持するか。
   - 判定ロジックの複製の不一致（`IsSpecialActionTriggered` と `MapCustomAction`）。1 つにする際、どちらを正とするか（`delayTime` の `nowAction` 更新の有無、Key の `uTrigger` の扱い）。
   - 到達不能に見えるタップ判定の分岐（分岐 B）を移植するか、捨てるか。
   - Profile／DisconnectBT の `return` がフレームの残りを飛ばす挙動の意図。
   - XboxGameDVR／MultiAction が `ActionManager` を通らず `PlayMacro` を直接呼ぶ。統一経路へ寄せるか。
3. **Profile の「離したら復帰」の暗黙の依存**（台帳 §8 項目 11）: 本番では、切替の適用が行うアクション状態の掃除により、離したときの `OnRelease` → `RestoreFromAction` が起きない。統一ディスパッチでは、これを明示的な仕様（`AutomaticUntrigger`／`uTrigger` の指定があるときだけ復帰する）にする。
4. **`shiftTrigger`**: `ComboTriggerInputAction`（AND 条件）に収まらない可能性（`Phase8-Step1ShiftTriggerCharacterizationTests` で挙動は固定済み）。副ボタンによる割り当て切替を、`TriggerBinding` の別の束縛として持つか、条件の拡張として持つか。
5. **`IProfileSwitcher` を `Mapping` の上流を通らずに直接呼ぶ経路**（Phase8 計画書 2026-10-14 追記の申し送り 2）: 統一経路の側に、Fresh-press ガード・成立エッジのみ・実行中の再実行防止と同等のガードを設ける。
6. **`MouseOutputAction` の完成**（同追記 1）: 現状は未登録・未完成。マウスのボタン・ホイール・移動を統一経路へ移すときに、`PerformMouseButtonEvent` などの呼び出しを移植して完成させる。登録すると `mouseHandled=true` で左クリックなどが出なくなるため、完成するまで登録しない。
7. **Key の統合**（同 2026-10-15 追記）: ゲームパッド出力（Stage3）と仮想キーボード出力（`Commit` の `fakeKeyRepeat`、`SyntheticDispatcher`）を、どの層で統合するか。

### 3.2 静的な窓口の置き換え方針（案）
- 先に、呼び出し元 0 件の静的 API を削除する（Step8 の候補: §1 の 6 件）。件数のラチェットの基準値は、削除と同時に更新する（`Phase7Step10eW5RatchetEqualityGuardTests`）。
- `ActionManager` の静的 API は、`IManagedActionManager` を使うコンストラクター注入へ（利用者: `Mapping`［`IMapping` は Singleton。循環に注意。`Func<IManagedActionManager>` の遅延が必要か］、`KeyAction`／`KeyButtonActionController`／`ButtonSpecialActionOutput`［状態の取得のみ。`ActionInstanceState` への参照を渡す形も検討］、`ProfileApplicationService`、`SpecialActionRepository`、`SpecialActionsXmlSerializer`、`SyntheticInputState`、`ActionsLoadedRuntimeReset`）。
- `SyntheticDispatcher`（静的な橋渡し）は、`KeyButtonActionController` が `ISyntheticDispatcher` を受け取る形へ。`RepeatHelperToIRepeaterAdapter` の直接生成は、`IRepeater` の工場を注入する。
- 注意: テストが静的 API を多数使う（`ClearDeviceState` 14、`GetStateFor` 18、`IsBeingTriggered` 12 など）。置き換え時はテストの書き換えが大きくなるため、1 API ごとに 1 PR とする（D3＝C1）。

### 3.3 Step2 で更新するモデル図（ユーザーの指示を得てから）
- 01: 第 2 層の「アクション実行管理」に `TriggerBinding`／`IActionBinding`／`IInputAction`／`IOutputAction` を追加し、実在／未実装の区別を反映。`IProfileSwitcher` の配置（第 2 層。2026-10-19 に反映済み）。
- 02: スタック図の【アクション】の記述の更新。
- 03: クラス図に `IActionBinding` 系と `ComboTriggerInputAction`、`KeyOutputAction` の `KeyAction` への委譲構造。
- 04: `TriggerBinding` のライフサイクル（プロファイル読込時に構築、接続時に紐付け、切断・プロファイル適用で破棄）。
