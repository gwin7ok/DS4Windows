# Phase8-Step2 設計書（前半: 契約の設計）

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Step2-Plan.md`（Q1〜Q9 を推奨の方向で承認）、`Phase8-Step2-Intent-Decisions.md`（承認済み）  
入力: `Phase8-Step1-Trigger-Spec-Ledger.md`、`Phase8-Step1-Static-Access-Inventory.md`  
状態: **W2（契約の設計）の草案。承認待ち**。後半（W3 状態・スレッド・安全弁、W4 移行計画）は、本書の承認後に追記する  
注意: 本書のコードブロックは**設計のスケッチ**で、コミットされた実装ではない。メンバー名は Step3 の実装時に調整してよい（意味を変える場合は本書を更新して承認を得る）。

---

## 1. 設計の原則

- 構造は系統 A（`IActionBinding` 系）を土台にし、中身は系統 B（`Actions.Action` 系）の稼働実績のあるロジックを使う（D1＝A2）。出力側の `MacroAction`／`ProfileSwitchAction`／`LaunchProcessAction` は、既に `IOutputAction` を実装済みなので、そのまま使う。
- **判定（入力）と実行（出力）を分ける**。判定は読み取り専用で副作用を持たない。ガード・エッジ検出・抑制・復帰は、**エンジンが一元的に担う**（Q3）。
- 毎フレームの割り当て 0（`MappingHotPathAllocationTests`）。ホットパスで `new` しない。判定の結果は値型で返し、入力は再利用するオブジェクトで渡す。
- 挙動を変えない（Q4）。仕様の根拠は `Phase8-Step1-Trigger-Spec-Ledger.md` と `Phase8-Step2-Intent-Decisions.md`。
- 規約: 1 ファイル 1 型、ファイル名＝型名、`Actions` サブシステムは契約も実装も `DS4Windows/Actions/`（R3）、名前空間は `DS4Windows.Actions`。静的な窓口を新設しない（§2.1）。コンストラクター注入（Pure DI）。

## 2. 現行の契約の問題と、改訂の方針

- `IInputAction.Evaluate(DS4State)`: 入力が `DS4State` だけで、タッチ・スワイプ・ジャイロの判定ができない（これらは `DS4StateFieldMapping` と `DS4StateExposed`／`Mouse` を要する）。戻り値 `ITriggerContext` は、呼ぶたびにオブジェクトを作る → ホットパスの割り当て。**改訂: 入力を `ITriggerInput`（再利用）、戻り値を値型にする**。
- `IActionBinding.OnTriggered`／`OnReleased`: ガード・エッジ検出をどこが担うか未定義で、呼び出し元 0 件。**改訂: 削除**（ライフサイクルはエンジンが担う。Q3）。`Input`／`Outputs` は残し、実行方針（`Policy`）などを足す。
- `IActionRegistry.Register`／`Unregister`／`GetBindingsForDevice`: 1 件ずつの登録で、読込時の競合（`Clear()`→`Add()`）を避けられない。**改訂: スロットごとのリストを不変にして、参照ごと差し替える**（Q8）。実装クラスがない → `ActionBindingRegistry` を新設。
- `IOutputContext`（`Device`、`OutputHandler` のみ）: フレームの出力（Button の出力、トリガーのボタンの消去）と、「フレームを打ち切る」要求を表せない。**改訂: 後方互換の拡張**（既存の 3 つの出力アクションは変更不要）。
- `ITriggerContext`／`TriggerContextImpl`（`UseScanCode` なし）: 現行の Key のコントローラー経路（`IActionController`）が使うため**そのまま残す**（本設計の対象外）。

## 3. 入力の契約: `ITriggerInput`（Q1＝A）

`Mapping` が毎フレーム 1 回、デバイスごとに再利用するオブジェクトを更新して渡す（`DS4StateFieldMapping` の再利用と同じ考え方）。

```csharp
// DS4Windows/Actions/ITriggerInput.cs
public interface ITriggerInput
{
    int Device { get; }

    // getBoolSpecialActionMapping と同じ意味の判定
    //  ボタン: そのまま／軸: 128±55 を超えたら／L2・R2: 100 を超えたら／タッチ: ボタン／スワイプ: フラグ／ジャイロ: ジャイロ制御のとき ±67 を超えたら
    bool IsActive(DS4Controls control);

    // 前フレームで押されていたか（MultiAction のエッジ判定用。必要なときだけ前フレームの表を作る＝遅延）
    bool WasActive(DS4Controls control);

    DateTime UtcNow { get; }     // 時刻の差し替え点（テストで固定できる）
    long TickCount64 { get; }    // Fresh-press ガードの 80ms の判定用
}
```

- 実装 `TriggerInput`（`DS4Windows.Actions`。1 デバイス 1 つ）は、`DS4StateFieldMapping`、`DS4State`、前フレームの状態、ジャイロ制御の有無を持つ。`IsActive` の本体は、現行の `Mapping.getBoolSpecialActionMapping`（約 55 行）を移す。`Mapping` はフレームごとに `Update(cState, eState, tp, fieldMapping, previousState)` を呼ぶだけ。
- 割り当て 0: `IsActive` は表の参照だけ。`WasActive` の前フレームの表は、初回要求時にだけ作り（現行の `previousFieldMapping`）、デバイスごとに再利用する。
- 時刻の注入（`UtcNow`／`TickCount64`）は、Step1 で苦労した実時間のテストを安定させる（仕様台帳 §2・§4）。

## 4. 判定の契約: `IInputAction`（改訂）

```csharp
// DS4Windows/Actions/IInputAction.cs（改訂）
public interface IInputAction
{
    string Name { get; }

    // 構成するボタン。成立中の抑制対象（suppressedTriggerButtons）と、トリガーのボタンの出力の消去に使う
    IReadOnlyList<DS4Controls> Triggers { get; }

    // 副作用なしの判定（delayTime の基準時刻の更新だけは、判定の一部として内部状態を更新する）
    TriggerState Evaluate(ITriggerInput input);

    void Reset();   // プロファイル適用・切断で呼ぶ（delayTime の基準時刻などを初期化）
}

// 値型（割り当て 0）
public readonly struct TriggerState
{
    public bool IsEstablished { get; }   // 成立しているか（ComboTriggerInputAction: AND 条件）
    // MultiTap のように複数の出力を持つ入力アクション用の拡張点（Step7 の最後に設計）
    public int Gesture { get; }          // 0 = なし。1 = タップ、2 = 長押し、3 = ダブルタップ
}
```

- **`ComboTriggerInputAction`**（D2＝B1。Step3 で新設）: 1 つ以上のボタンの AND 条件。Controls タブの 1 ボタンは「要素数 1」の特殊ケース。オプション: `DelayTime`（`delayTime`。基準時刻の更新を含む）、`InvertOnRelease`（Macro の `pressRelease`＋`firstTouch`: 離したときに成立）。時間定数は名前付き定数（Q4 項目 9）。
- **`MultiTapInputAction`**（XboxGameDVR／MultiAction。Step7 の最後）: タップ（150ms＋`DebouncingMs`）・長押し（500ms）・ダブルタップを `Gesture` で返す。状態（`tappedOnce` など）は現状 `SpecialAction` のフィールドにあるため、移植時に入力アクション側へ移すか、`SpecialAction` に残すかを Step7 で決める。
- 現行の判定の複製（`IsSpecialActionTriggered`）は、`ComboTriggerInputAction` に統合される（Intent 項目 2）。到達不能なタップ判定は移植しない（項目 3）。

## 5. 出力の契約: `IOutputAction`／`IOutputContext`（後方互換の拡張）

```csharp
// 既存（変更なし）
public interface IOutputAction
{
    string Id { get; }
    void Execute(IOutputContext ctx);   // 成立の瞬間（エッジ）に 1 回
    void Stop(IOutputContext ctx);      // 離したとき。復帰・停止
}

// 拡張（新規のオプション契約）: 毎フレームの出力（Button の Press／Toggle）
public interface IFrameOutputAction : IOutputAction
{
    // active == 出力を ON にするか。成立している間は毎フレーム呼ばれる（Toggle は内部で反転の状態を持つ）
    void ApplyFrame(IOutputContext ctx, bool active, bool risingEdge);
}

// 既存の IOutputContext を拡張（既存のメンバーは変更しない）
public interface IOutputContext
{
    int Device { get; }
    IVirtualKBM OutputHandler { get; }
    IFrameOutput Frame { get; }          // 新規: このフレームの出力（Button 用。無いときは null）
    void RequestEndFrame();              // 新規: Profile の切替・DisconnectBT の実行後に、フレームの走査を打ち切る（Intent 項目 5）
}

public interface IFrameOutput
{
    void ResetTriggerControl(DS4Controls control);    // ResetToDefaultValue 相当（成立中のトリガーのボタンの出力を消す）
    void PressX360(X360Controls control);             // Stage3 の Button の出力（ボタン配列／マウスのカウンター／タッチパッドクリック）
}
```

- 既存の `MacroAction`／`ProfileSwitchAction`／`LaunchProcessAction` は**変更しない**。`ProfileSwitchAction`／`DisconnectBT` の出力は、実行したときに `ctx.RequestEndFrame()` を呼ぶ（実行されなかった［ガードでブロック、BT でない等］ときは呼ばない。現行の `return` の位置と同じ）。
- **Button 出力**: 新設 `ButtonOutputAction : IFrameOutputAction`。現行の Stage3 の本体（`X360Controls` の分岐、`ButtonSpecialActionOutput.ResolveOutputActive`）を移す。Press は成立中のみ ON、Toggle は立ち上がりで反転（Fresh-press ガード中は反転しない）。
- **Key 出力**: `KeyOutputAction` を書き直す（§7）。
- **DisconnectBT／BatteryCheck／GyroCalibrate／ステアリング較正**: それぞれ `IOutputAction` を新設する（Step7 の最後。実機の `DS4Device` への参照を要する。`IDeviceStateAccessor` 経由で取得する案）。

## 6. 束縛の契約: `IActionBinding`／`TriggerBinding`

```csharp
// DS4Windows/Actions/IActionBinding.cs（改訂。OnTriggered／OnReleased は削除）
public interface IActionBinding
{
    IInputAction Input { get; }
    IReadOnlyList<IOutputAction> Outputs { get; }
    BindingPolicy Policy { get; }
    SpecialAction Source { get; }        // ActionInstanceState のキー。Controls タブ由来のときは合成の SpecialAction（GetOrCreateSyntheticKeyAction と同じ考え方）
    int Index { get; }                   // BackingStore.actions での添字（UntriggerIndex 用）
}

// 不変の設定（Intent 項目 1・5・6・11）
public sealed class BindingPolicy
{
    public bool RespectsFreshPressGuard { get; }   // 台帳 §4.2: Button の Press のみ false
    public bool BlocksWhileExecuting { get; }      // Program／Profile のみ true（IsExecuting）
    public UntriggerPolicy Untrigger { get; }      // None／Automatic／Buttons（Profile の復帰。Intent 項目 11）
    public IReadOnlyList<DS4Controls> UntriggerButtons { get; }
    public bool SuppressesTriggerButtons { get; }  // 成立中、トリガーのボタンの通常出力を抑制する（既定 true）
    public string ExclusionGroup { get; }          // シフト（Q6）: 同じグループで先に成立した束縛だけが実行される。null = なし
}

public enum UntriggerPolicy { None, Automatic, Buttons }

// DS4Windows/Actions/TriggerBinding.cs
public sealed class TriggerBinding : IActionBinding { /* 上記を保持するだけの不変クラス */ }
```

- **種別ごとの構成**（`IActionBindingBuilder` が組み立てる。§8）:
  - Button: 入力＝`ComboTriggerInputAction`、出力＝`ButtonOutputAction`、`RespectsFreshPressGuard` は Press が false／Toggle が true。
  - Key: 出力＝`KeyOutputAction`、ガード＝true。
  - Macro: 出力＝`MacroAction`、`InvertOnRelease`（RunOnRelease）、ガード＝true。
  - Program: 出力＝`LaunchProcessAction`、`BlocksWhileExecuting`＝true、ガード＝true。
  - Profile: 出力＝`ProfileSwitchAction`、`BlocksWhileExecuting`＝true、`Untrigger`＝Automatic／Buttons／None、ガード＝true。
- **シフト（Q6）**: Controls タブの 1 ボタンに、通常用とシフト用の 2 束縛を作り、`ExclusionGroup` を同じ値にする。シフトのトリガーは、`shiftTrigger` の副ボタンを Combo の条件に含める（シフト側の優先は、グループ内の宣言順で表現する）。通常側は、シフトが成立している間は実行しない。
- 状態は束縛に持たない（Q2＝A）。実行時の状態は、既存の `ActionInstanceState`（`IManagedActionManager.GetStateFor(Source, device)`）に残す。

## 7. `KeyOutputAction` の書き直し（D1 の注記）

現行（未検証の独自実装）は、`KeyAction` に委譲せず、`TriggerContextImpl`・`KeyActionBinding` を組み立てて `ActionManager.GetOrCreateControllerForAction(...).Handle(...)` を呼ぶ。`KeyAction.OnTrigger`／`OnRelease` と、ほぼ同じ処理の重複で、論理キー・ネイティブキーの求め方だけが違う。

```csharp
// 書き直し後（スケッチ）
public sealed class KeyOutputAction : IOutputAction
{
    public KeyOutputAction(SpecialAction sa, KeyAction keyAction, ISyntheticDispatcher dispatcher, ...);
    public string Id => sa.name;
    public void Execute(IOutputContext ctx) => keyAction.OnTrigger(ctx.Device, logical, native, useScan, ctx.OutputHandler);
    public void Stop(IOutputContext ctx)    => keyAction.OnRelease(ctx.Device, logical, native, useScan, ctx.OutputHandler);
}
```

- `logical`＝`SpecialAction.details`（ushort）、`native`＝現行の `Mapping` の Key 経路と同じく `outputKBMMapping.GetRealEventKey(key)`（`deviceState[device].nativeKeyAlias[key]`）、`useScan`＝`keyType.HasFlag(ScanCode)`。**値の求め方を、現行の `Mapping`（Key 経路）と一致させる**（`KeyAction` のコンストラクターが求める値［`SyntheticDispatcher.ResolveNativeKey`］と一致するか、Step3 の単体テストで確認）。
- 注意（現行の挙動。変更しない）: `KeyActionAdapter` と `KeyButtonActionControllerAdapter` は、`useScanCode` を下流に渡さず `false` 固定。ScanCode 指定が実際には効いていない可能性がある。Step3 で現行どおり再現し、別件として記録する（是正には承認が要る）。
- Step7 の Key の切替までは、系統 B（`KeyActionAdapter` 経由）と並行して存在する。二重ディスパッチを防ぐ仕組みは、W4（移行計画）で設計する。

## 8. 登録簿とビルダー: `IActionRegistry`（改訂）、`IActionBindingBuilder`（新設）

```csharp
// DS4Windows/Actions/IActionRegistry.cs（改訂。Register／Unregister／GetBindingsForDevice は削除）
public interface IActionRegistry
{
    IReadOnlyList<IActionBinding> GetBindings(int device);   // 不変のスナップショット。null は返さない（空のリスト）
    void Replace(int device, IReadOnlyList<IActionBinding> bindings);   // 参照の差し替え（アトミック）
    void Clear(int device);
}

// DS4Windows/Actions/ActionBindingRegistry.cs（DI の Singleton）: volatile な参照の配列（要素数＝MaxControllerCount）

// DS4Windows/Actions/IActionBindingBuilder.cs
public interface IActionBindingBuilder
{
    // プロファイルのスペシャルアクション（IProfileActionProvider）と、Controls タブの割り当て（IProfileSettingsService）から、
    // デバイスの束縛を組み立てる（Step5。プロファイル読込の完了後に呼ぶ）
    IReadOnlyList<IActionBinding> Build(int device);
}
```

- 呼び出しの位置（Step5〜6）: プロファイルの読込・適用の完了後（`ProfileApplicationService.CompleteProfileApplication` の掃除の後）に `registry.Replace(device, builder.Build(device))`。切断（`ControlService.On_DS4Removal`）で `registry.Clear(device)`。これで、`Clear()`→`Add()` の競合は構造的に起きない（Phase7c W1 で保留した本格案）。
- 読み出し側（エンジン）は、フレームの先頭で `GetBindings(device)` を 1 回だけ取る。差し替えが途中で起きても、そのフレームは古い一覧で最後まで処理する（一貫性）。

## 9. エンジンの契約: `ITriggerDispatchEngine`（Q3・Q9）

```csharp
// DS4Windows/Actions/ITriggerDispatchEngine.cs
public interface ITriggerDispatchEngine
{
    // 現行の MapCustomAction（と Stage3）の置き換え。同期的に走る（async void ではない）
    void Process(int device, ITriggerInput input, IFrameOutput frame);

    // 現行の CheckForSpecialActionSuppression。通常の割り当てがボタンを抑制するかの照会（Mapping.MapCustom が使う）
    bool IsSuppressed(int device, DS4Controls control);

    // プロファイル適用・切断の後始末（エッジ検出の前回状態、復帰の合図、抑制の集合を初期化。Phase7b W1 の退避・復元と同等の保護を維持）
    void OnProfileApplied(int device);
    void OnDeviceDisconnected(int device);
}
```

- 実装 `TriggerDispatchEngine`（DI の Singleton）は、`IActionRegistry`、`IManagedActionManager`（状態）を受け取る。**1 フレームの処理手順、ガード、抑制、復帰の詳細は W3 で設計する**。
- `Mapping` は `ITriggerDispatchEngine` を 1 つ受け取り、`MapCustom` からの呼び出しで `MapCustomAction` と Stage3 を置き換える（Step7 で種別ごとに）。`Mapping` の復帰の合図（`deviceRuntime[device].UntriggerAction`／`UntriggerIndex`）は、エンジンの内部状態へ移す（W3）。
- 循環依存の確認: `Mapping` → `ITriggerDispatchEngine` → `IManagedActionManager` → `Func<IMapping>`（遅延）。エンジンは `IMapping` に依存しない。出力アクション（`MacroAction` → `IMacroPlayer` → `Func<IMapping>`）も遅延なので、循環しない。

## 10. 系統 A と系統 B の対応

- `IActionBinding`（Input＋Outputs＋Policy） ↔ `Actions.Action`（`OnTrigger`／`OnRelease`）＋ `ActionEntry`（`ActionDef`＋`ActionImpl`＋`States`）。
- `IOutputAction.Execute`／`Stop` ↔ `Actions.Action.OnTrigger`／`OnRelease`（`*ActionAdapter` が `IOutputAction` を内包）。
- `IActionFactory.CreateFrom(SpecialAction, index)` ↔ `IActionBindingBuilder.Build(device)`（`DefaultActionFactory` が Key／Program／Macro／Profile だけを作る。Button 等は `null` を返して Stage3 や `Mapping` 内の分岐が担う）。
- `IManagedActionManager.DispatchTriggerEdge`／`DispatchTriggerEstablished`／`DispatchTriggerReleased` ↔ エンジン（エッジ検出）＋出力アクションの `Execute`／`Stop`。
- `ActionInstanceState`（`BeingTriggered`、`IsToggledOn`、`RequiresFreshPressAfterReset`、`IsExecuting`、`UntriggeredTimestampTicks`）: **そのまま使う**（Q2＝A）。
- 系統 B は、種別の切替ごとに該当の `*ActionAdapter` と `Mapping` の分岐を削除し、全種別の切替後（Step8）に `IActionFactory` 本体と `ActionManager` の静的 API を削除する（D5）。

## 11. DI 登録と配置（規約 §3.1・§3.4）

- 登録（`ServiceRegistration`）: `ITriggerInput` は登録しない（`Mapping` が内部で保持）、`IActionRegistry`→`ActionBindingRegistry`（Singleton）、`IActionBindingBuilder`→`ActionBindingBuilder`（Singleton）、`ITriggerDispatchEngine`→`TriggerDispatchEngine`（Singleton）。
- 配置: `Actions` サブシステムなので、契約も実装も `DS4Windows/Actions/`（R3）。名前空間は `DS4Windows.Actions`（R6）。1 ファイル 1 型（§3.3 原則 1）。`BindingPolicy`／`TriggerState`／`UntriggerPolicy`／`IFrameOutput` もそれぞれ別ファイル。
- `Mapping` のコンストラクター引数は 11 → 12（`ITriggerDispatchEngine`）。台帳 §17.4 の「依存 11 の整理」は Phase9 で扱う。

## 12. 未決（W3 以降）

- 1 フレームの処理手順（束縛の走査順、排他グループの解決、抑制の集合の更新、復帰の判定の位置）。
- 状態の所有の詳細（`prevEstablished` の置き場、`ActionInstanceState` の作り直しとの関係、`OnProfileApplied` の順序）、スレッドの扱い（適用時 Halt、`MapCustomAction` の `async void` の廃止による、同時実行の変化）。
- ホットパスの割り当て 0 の見積もり、性能の測り方。
- 移行の順序・並行期間の二重ディスパッチ防止・各 PR のテストの流用方法（W4）。
- `MultiTapInputAction` の状態の置き場（Step7 の最後）。
- Controls タブ（`ProcessControlSettingAction`）の取り込み範囲と時期（特殊アクションの切替の後。Intent 項目の範囲外）。

## 13. 確認したい点

1. `IActionBinding` から `OnTriggered`／`OnReleased` を削除する（ライフサイクルはエンジンが担う）。
2. `IActionRegistry` の `Register`／`Unregister`／`GetBindingsForDevice` を、`GetBindings`／`Replace`／`Clear` に置き換える（呼び出し元は 0 件なので影響なし）。
3. `IOutputContext` の拡張（`Frame`、`RequestEndFrame`）は、既存の実装（`OutputContextImpl` ほか）の追加のみで、既存の 3 つの出力アクションは変更しない。
4. 時刻の注入（`ITriggerInput.UtcNow`／`TickCount64`）を契約に含める。
