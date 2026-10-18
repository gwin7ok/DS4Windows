# Phase8-Step2 設計書（前半: 契約の設計）

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Step2-Plan.md`（Q1〜Q9 を推奨の方向で承認）、`Phase8-Step2-Intent-Decisions.md`（承認済み）  
入力: `Phase8-Step1-Trigger-Spec-Ledger.md`、`Phase8-Step1-Static-Access-Inventory.md`  
状態: **承認済み（W2〜W5。2026-10-19）**。W5 のモデル図の更新案は別文書  
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

---

# 設計書（後半: 状態・フレームの手順・スレッド・安全弁）

追記日: 2026-10-19（W3）。前半（§1〜§13）の契約は承認済み（確認したい点 1〜4）。本節は、W3 の調査で**前半の契約に修正が必要と分かった点**（§14）を最初に示し、続けて状態の所有、フレームの手順、スレッド、ガードレールへの適合を設計する。

## 14. W3 で判明した、前半の契約の修正点（承認が要る）

1. **エンジンの入口を 2 つに分ける**（§9 の `Process` を置き換える）。現行の `MapCustom` は、`MapCustomAction`（エッジ型の実行。トリガーのボタンの出力を**消す**）を、通常の割り当て（Controls タブのループ）の**前**に呼び、Stage3（Button の毎フレームの出力）を、通常の割り当ての**後**に呼ぶ。順序を入れ替えると、出力が変わる（Stage3 は、消去の後・通常の割り当ての後に書く）。
   ```csharp
   void ProcessTriggers(int device, ITriggerInput input, IFrameOutput frame);   // 通常の割り当ての前（MapCustomAction の置き換え）
   void ApplyFrameOutputs(int device, ITriggerInput input, IFrameOutput frame); // 通常の割り当ての後（Stage3 の置き換え）
   ```
2. **`delayTime` の基準時刻は、デバイスごとに共有する**（§4 の `ComboTriggerInputAction` の内部状態にしない）。現行の `nowAction[device]`／`oldnowAction[device]` は、そのデバイスのすべての `delayTime` つきアクションで**共有**される（どれか 1 つのトリガーが押されていないと、共有の基準時刻が更新される）。束縛ごとに持つと、`delayTime` つきのアクションが 2 つ以上あるプロファイルで挙動が変わる。
   → `ITriggerInput` に `DelayClock Delay { get; }`（`Now`／`Old` を持つ、デバイスごとの再利用オブジェクト）を足し、`ComboTriggerInputAction` はそれを読み書きする。
3. **押している間の呼び出し `IHeldOutputAction` を足す**（任意の契約）。現行の Macro は、成立中に 2 フレーム目以降も `BeingTriggered` が true のとき `EndMacro(...)` を呼ぶ（`RepeatMacro` でないとき）。マクロが Alt-Tab（`164/9/9/164`）のときだけ効果があり（`AltTabSwappingRelease`）、それ以外は何もしない。この呼び出しを失わないため、`MacroAction` 側に任意の `WhileHeld(ctx)` を持たせる。
   ```csharp
   public interface IHeldOutputAction : IOutputAction { void WhileHeld(IOutputContext ctx); }
   ```
4. **`BindingPolicy` に 2 項目を足す**: `ResetsTriggerOutput`（成立中に、トリガーのボタンの出力を消すか。`typeID != None` の既存の全種別が true）、`StopOnRelease`（離したとき `Stop` を呼ぶか。下の 5）。
5. **離したときの `Stop`**: 現行は、離したエッジで `ActionImpl.OnRelease` → `Stop` が呼ばれる。実際の効果は、種別で異なる。
   - Key（Press）: キーを解放する（意味がある）。Toggle: 無視される（`OnUp` が何もしない。トグル OFF は次の成立で行う）。
   - Macro: `EndMacroDirect` が `control = None` で呼ばれ、Alt-Tab のとき以外は**何もしない**。Program: 何もしない（`Stop` は空）。
   - Profile: `RestoreFromAction`（復帰）。ただし、本番では適用の掃除で `BeingTriggered` が false に戻るため、離したときのエッジは起きない（Intent 項目 11）。
   - → エンジンは、現行と同じく**離したエッジ（`BeingTriggered` が true → false）で `Stop` を呼ぶ**。Profile は、`OnProfileApplied` が `BeingTriggered` を明示的に下げる（§16）ので、離したときの `Stop` は呼ばれない。復帰は復帰のポリシー（§15.2 の `UntriggerPass`）が担う。

## 15. 1 フレームの手順

### 15.1 全体の順序（`Mapping.MapCustom`）
1. `Mapping` が `ITriggerInput.Update(...)` を呼ぶ（`fieldMapping` の展開、前フレームの状態）。
2. `engine.ProcessTriggers(device, input, frame)`（現行の `MapCustomAction` の位置。`GetProfileActionCount > 0 || 一時プロファイル使用中` のとき。束縛が空で、復帰の合図もないときは、すぐ戻る）。
3. 通常の割り当て（Controls タブ。`CheckForSpecialActionSuppression` → `engine.IsSuppressed`）。
4. `engine.ApplyFrameOutputs(device, input, frame)`（現行の Stage3 の位置）。
5. マクロの出力の重ね合わせ、`Commit` など（現行どおり）。

### 15.2 `ProcessTriggers`（擬似コード）
```
bindings = registry.GetBindings(device)              // フレームの先頭で 1 回。途中の差し替えは、次のフレームから
EnsureRuntime(device, bindings)                       // リストの参照が変わったときだけ、実行時の配列を作り直す（フレーム内は割り当て 0）
ReleaseSuppressedButtons(device, input)               // 離されたボタンを、1 つずつ抑制から外す（現行の冒頭）
for each binding b in bindings:
    state  = managed.GetStateFor(b.Source, device)    // 束縛ごとに 1 回（現行は 1 つのアクションで複数回）
    result = b.Input.Evaluate(input)                  // delayTime は input.Delay を使う
    UpdateFreshPressTimer(state, result.IsEstablished, input.TickCount64)    // 全種別。成立中は 0 に戻す。80ms 連続の未成立でガードを解除
    if result.IsEstablished:
        AddToSuppressed(device, b.Input.Triggers)     // 構成するすべてのボタン
        if b.Policy.ResetsTriggerOutput: frame.ResetTriggerControl(each trigger)
        if exclusion group already fired: continue    // シフト（Q6）
        if b.Policy.RespectsFreshPressGuard && state.RequiresFreshPressAfterReset: continue
        if b.Policy.BlocksWhileExecuting && state.IsExecuting: continue
        if !state.BeingTriggered:                      // 立ち上がりのエッジ
            if b.Policy.BlocksWhileExecuting: state.IsExecuting = true
            try:
                state.SetBeingTriggeredInternal(true)
                for o in b.Outputs: o.Execute(ctx)
                if b.Policy.Untrigger != None: untrigger.Set(device, b)    // 復帰の合図（prevProfileName を記録）
            finally: if b.Policy.BlocksWhileExecuting: state.IsExecuting = false
            if ctx.EndFrameRequested: return          // Profile の切替・DisconnectBT
        else:
            for o in b.Outputs: if o is IHeldOutputAction h: h.WhileHeld(ctx)
    else:
        if state.BeingTriggered:                       // 離したエッジ
            state.SetBeingTriggeredInternal(false)
            if b.Policy.StopOnRelease: for o in b.Outputs: o.Stop(ctx)
        // 復帰待ちの間: 切替アクションの BeingTriggered を下げ続ける（Intent 項目 6）
UntriggerPass(device, input)                          // 復帰の判定（現行のループの後）。フレームが打ち切られた場合は行わない
```
- 種別ごとの差は、すべて `BindingPolicy` と出力アクションに押し込める。エンジンの本体に、種別の分岐を持たない。
- **Macro の `RunOnRelease`**: `ComboTriggerInputAction` の `InvertOnRelease`（`firstTouch` を内部状態に持つ。現行は `SpecialAction.firstTouch`）。押したときは「準備」（`IsEstablished = false` を返し、`firstTouch` を立てる）、離したとき `IsEstablished = true`（反転）で 1 回実行する。`Phase8Step1MacroBranchCharacterizationTests` の期待どおり。
- **Key の `UntriggerIndex`**: 現行は、Press の Key が `deviceRuntime.UntriggerIndex` を使って離しを判定する（Profile の復帰と共有の 1 変数）。エンジンは `BeingTriggered` のエッジで離しを判定するため**不要**になる。Profile の `UntriggerIndex` は、復帰の合図（§16）に含める。

### 15.3 `ApplyFrameOutputs`
```
for each binding b in registry.GetBindings(device) that has an IFrameOutputAction:
    result = cached result from ProcessTriggers                 // 同じフレームの評価結果を再利用（遅延の基準時刻を二重に更新しない）
    rising = result.IsEstablished && !runtime[b].PrevEstablished
    foreach o in b.Outputs of IFrameOutputAction: o.ApplyFrame(ctx, active: result.IsEstablished, risingEdge: rising)
    runtime[b].PrevEstablished = result.IsEstablished
```
- Button の Press: `active = IsEstablished`（Fresh-press ガードを見ない）。Toggle: `ButtonOutputAction` が `risingEdge && !state.RequiresFreshPressAfterReset` で反転し（`ActionManager.SetToggledOn` 相当）、`state.IsToggledOn` を出力する。現行の `ButtonSpecialActionOutput.ResolveOutputActive` と同じ。
- 現行の Stage3 は、`GetProfileActionsRaw` の複製を列挙している（Phase7c W1）。新設計では、不変のリストを使うため、複製が不要。例外はフレーム単位で記録する（現行の `catch` のログ文言を維持）。

## 16. 状態の所有と、後始末

- **`ActionInstanceState`（`IManagedActionManager`）に残すもの**: `BeingTriggered`、`IsToggledOn`、`RequiresFreshPressAfterReset`、`UntriggeredTimestampTicks`、`IsExecuting`、`FirstTouch`（Q2＝A）。
- **エンジンが新しく持つもの（デバイスごと）**:
  - 抑制の集合（現行の `Mapping.suppressedTriggerButtons`）→ `IsSuppressed` の本体。
  - 復帰の合図 `UntriggerSignal { SpecialAction Action; int Index; }`（現行の `deviceRuntime[device].UntriggerAction`／`UntriggerIndex`）。
  - 束縛ごとの実行時の配列 `runtime`（`PrevEstablished`。リストの参照が変わったときだけ作り直す）。
- **`OnProfileApplied(device)`**（現行の `ProfileApplicationService.CompleteProfileApplication` の掃除に相当）:
  1. `managed.ClearAllEntries()`／`managed.ClearDeviceState(device)`（既存。状態を作り直す。`BeingTriggered` が false、`RequiresFreshPressAfterReset` が true になる）。
  2. エンジンの `runtime` を初期化し、抑制の集合を空にし、`input.Delay` を初期化する。**復帰の合図は残す**（Phase7b W1 の退避・復元を、エンジン内で明示的に行う。`Mapping.HandleDeviceDisconnect` が合図を消す問題は、合図がエンジンに移ることで構造的に起きなくなる）。
  3. 新しいプロファイルの束縛を `registry.Replace(device, builder.Build(device))`。
  - 順序は現行の掃除の順序（`ClearAllEntries` → `ClearForDevice` → `ClearDeviceState` → `PreallocateForProfileApply`。`Phase8Step1ApplyProfileCleanupOrderGuardTests` で固定）を維持し、その後にエンジンの初期化と束縛の差し替えを足す。
- **`OnDeviceDisconnected(device)`**: 復帰の合図も含めてすべて消す（物理切断）。`registry.Clear(device)`。ガードレール 5.6（切断時の一時プロファイル残留防止）の本実装に、`ClearState`／`ClearPendingRestore` の接続（Step6）が含まれる。

## 17. スレッドと同時実行

- **呼び出し元のスレッド**: `engine.ProcessTriggers`／`ApplyFrameOutputs` は、`Mapping.MapCustom` を呼ぶ**そのデバイスの入力スレッド**（`ControlService.On_Report`）だけ。デバイスごとの状態（抑制、復帰の合図、`runtime`、`input`）は、そのスレッドだけが読み書きする。デバイス間で共有する可変の状態はない。
- **差し替えの競合**: `registry.Replace` は、プロファイル適用のスレッド（UI、`Task.Run`、AutoProfile のタイマー）から呼ばれる。参照の差し替えはアトミックで、入力スレッドは、フレームの先頭で 1 回だけ参照を取る。Halt（ガードレール 5.2）の下で行う現行の運用は維持し、さらに、Halt を取りこぼしても（500ms のタイムアウトで適用が飛ぶ現行の挙動を含め）一覧が壊れない。
- **`OnProfileApplied` の呼び出し**: 適用のスレッドから呼ばれる。エンジンの状態（抑制の集合、`runtime`）は入力スレッドのものなので、適用スレッドは**直接書かず**、「次のフレームの先頭でリセットする」要求のフラグ（volatile）を立てる。フレームの先頭でそのフラグを見て初期化する（`Halt` が成功した場合は入力スレッドが止まっているため即時でも安全だが、Halt を取りこぼした場合も安全にするため）。`ActionInstanceState` の作り直し（`ClearAllEntries`）は、現行どおり適用スレッドで行う。
- **`async void` の廃止**: 現行の `MapCustomAction` は `async void`（例外が握りつぶされるか、プロセスを落とす）。エンジンは同期で動き、例外は束縛ごとに `try/catch` して記録する（現行の `MapCustomAction exception` のログ文言を維持）。非同期の処理（プロファイル適用の `Task.Run`、マクロ）は、出力アクションの内側に閉じる。
- **ロック**: `ActionInstanceState` への書き込みは、現行どおり `lock (st)` の下で（エッジ検出の `SetBeingTriggeredInternal`）。エンジン自身の状態にロックは不要。
- **非同期実行中の再入**: `IsExecuting`（Program・Profile）と K3 の二重実行防止（`MacroRuntimeState`）を維持。

## 18. 6 大ガードレール（`Phase5-Plan.md` §5）への適合

- 5.1 同一 XML の排他: 影響なし（束縛の構築は読込の完了後。`Build` は XML を読まず、メモリ上の設定から作る）。
- 5.2 適用時 Halt: `registry.Replace`／`OnProfileApplied` を、Halt の区間内で行う（現行の `ApplyFromAction`／`ApplyProfile` の位置）。Halt を取りこぼしても安全（§17）。
- 5.3 AutoProfile のタイマー vs UI の競合: 束縛の読み出しは不変リストの参照で、競合しない（現行の `Clear()`→`Add()` の問題の解消）。
- 5.4 On-Demand のパス評価: 影響なし。
- 5.5 ドライバ破棄順序: 影響なし（出力デバイスは触らない）。
- 5.6 切断時クリーンアップ: `OnDeviceDisconnected` で束縛・合図・抑制を消す。`ClearState`／`ClearPendingRestore` の接続は Step6（意味の調査を含む）。

## 19. ホットパス（割り当て 0・呼び出し回数）の見積もり

- 割り当て 0 の確認点: `Evaluate` は値型を返す、`ITriggerInput` はデバイスごとに再利用、束縛のリストは不変で反復は `for` ループ（`foreach` の列挙子を作らない）、`runtime` の配列はリストの差し替え時だけ作る、ログ文字列は `IsTraceEnabled`／`IsDebugEnabled` のガードの内側で作る。
- 呼び出し回数の見積もり: 現行は、1 つのアクションあたり `GetStateFor`（ロックと辞書の検索）が複数回、`GetProfileAction`／`GetProfileActionIndexOf`（辞書の検索）がアクション数分（実測で最大約 40）。新設計は、束縛あたり `GetStateFor` が 1 回、辞書の検索はなし。仮想呼び出しは、束縛あたり `Evaluate`＋出力が数回。**現行より軽くなる見込み**（特性化テストと割り当て 0 のテスト［`MappingHotPathAllocationTests`］で確認し、簡易な処理時間の比較を Step3 で用意する）。

## 20. 設計上の注意（Step3 以降の確認事項）

- **`prevInputEstablished` のキャッシュ**（Intent 項目 8）: 新エンジンはこのキャッシュを使わない。プロファイル適用の後に、「押しっぱなしのトリガーを離して再度押す」と、現行と同じく 1 回だけ実行されること（および、`BeingTriggered` が作り直された後にフォールバックで余分に実行されないこと）を、Step3 の単体テストで確認する。
- **`ClearAllEntries` が呼ばれるタイミング**: `ActionEntry` の再作成で、系統 B の `ActionImpl`（`KeyActionAdapter` など）も作り直される。新エンジンの出力アクションは束縛が保持するため、作り直されない。系統 B の出力と並行する期間（Step7）の、`IsToggledOn` などの状態の持ち方は、W4 で設計する。
- **Controls タブ**: `ProcessControlSettingAction` は対象外（特殊アクションの切替の後。Phase8 の後半）。

## 21. 確認したい点（W3）

1. §14 の契約の修正 5 項目（エンジンの入口を 2 つに分ける、`DelayClock` をデバイスごとに共有、`IHeldOutputAction`、`BindingPolicy` の 2 項目、離したときの `Stop`）。
2. 復帰の合図・抑制の集合をエンジンに移す（Step7 の Profile の切替で、`Mapping.deviceRuntime` から移す）。
3. `OnProfileApplied` を、適用のスレッドから「次のフレームの先頭でリセット」の要求にする（§17）。

---

# 設計書（移行計画: W4）

追記日: 2026-10-19（W4）。W3 の設計（§14〜§21）は承認済み。本節は、Step3〜Step9 の範囲、新旧の並行期間のしくみ、Step7 の PR の割り方、テストの流用、ロールバックを定める。

## 22. Step3〜Step9 の範囲（`Phase8-Unified-Trigger-Dispatch-Plan.md` §5 の具体化）

各 Step は、個別の計画書を作成し、承認を得てから進める。**Step3〜Step6 では、本番の挙動は変わらない**（新しい型を足し、配線するが、移行対象の種別が空）。挙動が新経路に移るのは Step7 の種別ごとの PR から。

- **Step3: 判定と束縛の型（配線なし）**
  - 新設: `ITriggerInput`／`TriggerInput`（`DelayClock` を含む）、`TriggerState`、`IInputAction` の改訂、`ComboTriggerInputAction`（AND 条件、`delayTime`、`InvertOnRelease`）、`BindingPolicy`／`UntriggerPolicy`、`TriggerBinding`、`IActionBinding` の改訂（`OnTriggered`／`OnReleased` の削除）。
  - 書き直し: `KeyOutputAction`（`KeyAction` への委譲。§7）。削除: `KeyInputAction`、`ITrigger`／`SingleButtonTrigger`（Orphan-Decisions のとおり）。
  - テスト: `ComboTriggerInputAction` の単体テスト（AND、遅延、反転、時刻の差し替え）、`KeyOutputAction` と系統 B（`KeyActionAdapter` 経由）の挙動一致、処理時間の簡易な比較の用意（割り当て 0 のテストを含む）。
- **Step4: エンジンと出力の型（`Mapping` への配線は空の状態で）**
  - 新設: `IFrameOutput` の実装（`outputfieldMapping` に書くアダプター）、`ButtonOutputAction`、`IHeldOutputAction`、`TriggerDispatchEngine`（`ProcessTriggers`／`ApplyFrameOutputs`／`IsSuppressed`／`OnProfileApplied`／`OnDeviceDisconnected`）。排他グループ（シフト）の解決を含む。
  - 配線: `Mapping` に `ITriggerDispatchEngine` を注入し、**抑制の集合の所有を `Mapping` からエンジンへ移す**（§23.3）。旧ループに「所有しているアクションなら、エンジンに任せる」フックを入れる（§23.1）。移行対象が空なので、挙動は変わらない。
  - テスト: エンジンを偽の束縛・出力で検証する単体テスト（エッジ検出、ガード、抑制、フレームの打ち切り、排他グループ、`OnProfileApplied` の後始末）。既存の全テストが変わらず成功すること。
- **Step4b（挿入）**: `LoadProfile`／`SaveProfile` の分解ほか（`Phase8-Unified-Trigger-Dispatch-Plan.md` 2026-10-19 追記）。
- **Step5: 登録簿とビルダー（空の移行対象で）**
  - 新設: `ActionBindingRegistry`、`ActionBindingBuilder`、移行対象を持つ設定オブジェクト（§23.2）。プロファイルの読込・適用の完了後に `Replace(device, Build(device))` を呼ぶ（移行対象が空なら、空のリスト）。
  - テスト: 各種別の `SpecialAction` から作る束縛の構成（ポリシーの値）、リスト差し替えのアトミック性、読込中の並行呼び出し。
- **Step6: 接続・切断との連携**
  - 切断時に `registry.Clear`／`engine.OnDeviceDisconnected`。`ClearState`／`ClearPendingRestore` の意味（`OlderProfilePath` の扱い）を調べて接続（Phase8-Step1 の発見）。接続時の紐付け（`IControllerRegistry` の利用の要否を、ここで確定）。
  - テスト: 切断で束縛・合図・抑制が消えること、再接続後の復元（`ControlService` の接続処理）が従来どおりであること。
- **Step7: 種別ごとの切替**（§24。1 種別 ＝ 1 PR）。
- **Step8: 旧コードの削除**: `MapCustomAction`、Stage3、`TryDispatchSATrigger*`、`prevInputEstablished`、系統 B（`Actions.Action`、`*ActionAdapter`、`IActionFactory`／`DefaultActionFactory`／`ActionFactory`、`ActionEntry`）、`ActionManager` の静的 API（呼び出し元 0 件の 6 件を含む）、`IMapping` の `CaptureProfileUntrigger`／`RestoreProfileUntrigger`、`ServiceProviderHolder` の 17 件の注入化、旧フック。ラチェットの基準値を更新する。
- **Step9: 総合検証**: 実機確認（全種別）、性能の比較、モデル図 01〜04 の更新、完了報告書。

## 23. 新旧の並行期間のしくみ

### 23.1 二重処理の防止: 「所有しているアクションはエンジンに任せる」フック
- 旧ループ（`MapCustomAction`）の各アクションの先頭に、次のフックを入れる（Step4）。
  ```csharp
  if (_engine.Owns(device, action))              // 移行済みの種別で、束縛が登録されている
  {
      if (_engine.ProcessOwned(device, action, input, frame) == FrameResult.EndFrame) return;
      continue;                                   // 旧経路の処理は一切しない
  }
  ```
- **順序を保つ**: エンジンの処理を旧ループの中の**そのアクションの位置**で行うので、プロファイル内のアクションの順序（フレームの打ち切りで、後ろのアクションの処理が飛ぶ順序を含む）は、現行と変わらない。最終形（Step8）では、`ProcessTriggers` が束縛を順に処理する（このフックは消える）。
- Stage3（Button）にも同じフック（`Owns` なら飛ばし、毎フレームの出力は `ApplyFrameOutputs` が担う）。
- `ProcessOwned` は、単一の束縛の処理（§15.2 のループの本体）。`ProcessTriggers` は、これを順に呼ぶだけ。

### 23.2 切り替えるスイッチ: 設定オブジェクト `BindingMigrationOptions`
- ビルダーは、`BindingMigrationOptions.MigratedTypes`（`ISet<ActionTypeId>`）に含まれる種別の束縛だけを作る。**静的な窓口にしない**: DI の Singleton の設定オブジェクトを注入する。既定値は、コード中の 1 か所の一覧（Step7 の PR ごとに種別を足す）。
- テストは、このオブジェクトを書き換えて、**同じ特性化テストを旧経路と新経路の両方で走らせる**（§25）。
- ロールバックは、一覧から種別を外すだけ（§26）。

### 23.3 共有する状態
- `ActionInstanceState`（`IManagedActionManager`）は、新旧で**同じオブジェクト**を使う。`IsToggledOn`、`BeingTriggered`、`RequiresFreshPressAfterReset`、`IsExecuting` は、旧経路で処理するアクションと新経路で処理するアクションが、混在しても食い違わない。
- **抑制の集合**（`suppressedTriggerButtons`）は、Step4 で**エンジンの所有に一本化**する。旧ループは、`engine.AddSuppressed`／`engine.ReleaseSuppressed`／`IsSuppressed` を呼ぶ（挙動は同じ）。新旧が別々の集合を持つと、通常の割り当ての抑制が食い違うため。
- **復帰の合図**（`UntriggerAction`／`UntriggerIndex`）は、`Mapping.deviceRuntime` に残し、**Profile の PR（Step7）で一度に**エンジンへ移す（Profile だけが設定し、判定するため）。それまで、Key（Press）が共有の `UntriggerIndex` を設定する現行の挙動は、そのまま。
  - 注意: Key を先に移行すると、Key が `UntriggerIndex` を上書きしなくなる。現行では、Key の Press が動いたとき、Profile の復帰の合図の `UntriggerIndex` を上書きしうる（偶発的な競合）。移行で**挙動が改善する方向の差**になるため、Key の PR の説明に明記する。
- 時刻・遅延の基準（`nowAction`／`oldnowAction`）は、`TriggerInput.Delay` へ。新旧のどちらかの経路だけが `delayTime` つきのアクションを処理する種別ごとの移行では、旧配列と `Delay` の二重管理が起きる。→ Step4 で、旧ループも `input.Delay`（`Mapping` が持つ `TriggerInput`）を使うように揃える（旧配列を `Delay` の実体にする）。

### 23.4 二重ディスパッチの検出
- テスト用の検査: エンジンが処理したアクションの集合と、旧ループが処理したアクションの集合が、重ならないことを確認する（`Owns` が true のアクションは、旧ループの本体を通らない）。`Phase8Step1` までの特性化テストに、「旧経路の通過回数」を数える仕掛けを足す（テスト用のフックのみ。製品コードにカウンターは置かない）。

## 24. Step7 の PR 一覧（案。順序は変更可）

各 PR の共通の完了条件: ① 該当種別の特性化テストが、旧経路・新経路の両方で成功する（§25）。② 全テスト・ラチェットの一致検査が成功する。③ 処理時間の比較が許容範囲（現行の +5% 以内）。④ 実機確認の項目が合格（または先送りの記録）。⑤ 切り替えた種別の旧コードの分岐を削除する（Strangler Fig）。

- **7.1 Button**（Press／Toggle）: `ButtonOutputAction`、Stage3 とバイパスの `MapCustomAction` の Button の分岐を削除。実機確認: ボタンの割り当て（Press、Toggle、マウス系、タッチパッドクリック）。
- **7.2 Key**（Press／Toggle／リピート）: `KeyOutputAction`、`TryDispatchSATrigger*` の Key 用の経路、Key の分岐（解除トリガーを使う分岐は移植しない）を削除。実機確認: Press の押下・解放、Toggle の ON／OFF、長押しのリピート。
- **7.3 Macro**: `MacroAction`＋`IHeldOutputAction`（Alt-Tab）、`RunOnRelease`（`InvertOnRelease`）。実機確認: マクロの単発、`RunOnRelease`、Alt-Tab マクロ。
- **7.4 Program**: `LaunchProcessAction`、`IsExecuting`。実機確認: プログラムの起動（`$hidden` を含む）、押しっぱなしで繰り返し起動しないこと。
- **7.5 Profile ＋ 復帰**（最大のリスク）: `ProfileSwitchAction`、復帰の合図を `Mapping.deviceRuntime` からエンジンへ、`OnProfileApplied` と `ProfileApplicationService` の連携、`IMapping` の `CaptureProfileUntrigger`／`RestoreProfileUntrigger` の削除、フレームの打ち切り（`RequestEndFrame`）。実機確認: プロファイルの切替、「Unload on trigger release」、解除トリガー、一時プロファイルの復帰、連続切替の安全弁。
- **7.6 実機のデバイスを要する種別**（1 種別ずつ PR）: GyroCalibrate、DisconnectBT、BatteryCheck、ステアリング較正。実機確認が必須（機材がなければ、ガードの分岐までの検証と、リスク受容の記録）。
- **7.7 XboxGameDVR／MultiAction**: `MultiTapInputAction`（タップ・長押し・ダブルタップ。状態の置き場を決める）。
- **7.8 Controls タブ**（単一ボタンの割り当て）: `ProcessControlSettingAction`（約 400 行。Key・Button・Macro・マウス・シフトほか）を、同じエンジンに載せる。**範囲が大きく、追加の設計が要る**（Step2b として、7.7 の後に、別の設計書［`Phase8-Step2b-ControlsTab-Design.md`］を作る）。D2＝B1 の「Controls タブとスペシャルアクションの統一」の最後の部分。

## 25. テストの流用と、性能・実機の検証

- **同じ特性化テストを、両方の経路で**: `SpecialActionTestScope` に、`BindingMigrationOptions` を設定するパラメーター（`migrated: bool` または種別の集合）を足し、Phase7c・Step1 の特性化テスト（`Phase7cW2...`、`Phase8Step1...`）を `[Theory]` にして、旧経路（`migrated: false`）と新経路（`migrated: true`）の両方で流す。期待値は同じ。**新経路で食い違ったら、設計か実装を直す**（期待値を変えるのは、Intent-Decisions で承認した差だけ）。Step8 で旧経路を消した後は、新経路のみになる。
- **新経路で足す回帰テスト**: Intent 項目 8（適用後の再成立が 1 回だけ実行される）、`DelayClock` の共有（`delayTime` つきのアクションが 2 つ）、排他グループ（シフト）、`OnProfileApplied` の後始末、`RequestEndFrame` でその後ろのアクションが処理されないこと。
- **性能**: 簡易なベンチマーク（固定の入力で `MapCustom` を N 回呼び、1 回あたりの時間と割り当てを測る）を Step3 で用意し、各 PR の前後で比較する（許容: 現行の +5% 以内）。`MappingHotPathAllocationTests` を維持。
- **実機確認**: `Phase6-Step11-RealDevice-Verification-Checklist.md` §11 に、PR ごとの項目を追加する（Step7 の各 PR で、U 番号を割り振る）。

## 26. ロールバック

- 種別の PR が問題を起こしたら、`MigratedTypes` から該当の種別を外せば、旧経路に戻る（旧コードは Step8 まで残してあるため）。→ **旧コードの削除（Step8）は、全種別の実機確認が済むまで行わない**。
- 各 PR で旧コードの分岐を削除する運用（上の完了条件⑤）と、このロールバックは両立しない。→ **修正**: Step7 の各 PR では、旧コードの分岐を**残したまま**、スイッチ（`MigratedTypes`）だけで切り替える。旧コードは Step8 でまとめて削除する。ただし、Stage3 のような別経路（Button）の旧コードも同様に残す。

  上の完了条件⑤は「旧コードの削除は Step8」に読み替える（D5 の「種別ごとの切替が完了するたびに削除対象として扱う」は、「削除可能になる」の意味とする）。

## 27. リスクと対応（移行固有）

- 旧ループのフックの挿入で、旧経路の挙動が変わる: フックは `Owns` が false のとき何もしない（Step4 のテストで、既存の全テストが変わらず成功することを確認）。
- 順序が変わる（フレームの打ち切り）: フックで旧ループ内の位置を維持（§23.1）。順序の回帰テスト（打ち切りの前後のアクション）を足す。
- 新旧で `ActionInstanceState` を共有することによる食い違い: 新経路は旧経路の語彙（`BeingTriggered` のエッジ）を使う。`Owns` で分離されたアクションは、片方の経路しか触らない。
- Profile の PR（7.5）の規模: 復帰の合図の移行、適用との連携、Phase7b の退避・復元の撤去を含む。分割できる場合は（合図の移行と、`OnProfileApplied` の連携）2 PR にする。
- 実機がなければ確認できない種別（7.6）: ガードの分岐までの検証と、リスク受容の記録。Phase8 の完了を、これらの確認の完了にかけるかは、Step9 で決める。

## 28. 確認したい点（W4）

1. §22 の Step の割り方（Step3〜Step6 は本番の挙動を変えない）。
2. §23 の並行期間のしくみ（旧ループ内のフック、`BindingMigrationOptions` のスイッチ、抑制の集合をエンジンへ一本化、`DelayClock` を旧ループも使う）。
3. §26 の修正: **旧コードは Step8 まで残し、Step7 はスイッチだけで切り替える**（D5 の運用を「削除可能になる」の意味に読み替える）。
4. §24 の PR の割り方（7.1 Button → 7.2 Key → 7.3 Macro → 7.4 Program → 7.5 Profile ＋ 復帰 → 7.6 デバイスを要する種別 → 7.7 MultiAction → 7.8 Controls タブ［別の設計書］）。
