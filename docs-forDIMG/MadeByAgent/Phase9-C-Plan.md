# Phase9-C 計画書: 9-C マクロの再生の切り出し

作成日: 2026-10-22  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase9-Responsibility-Split-Plan.md` 9-C、`Phase9-B-Plan.md`（同じ進め方）  
状態: **承認済み（2026-10-22。§8）。C-1・C-2 を 1 回でまとめて実装済み（ビルド・テストはユーザーの実行待ち）。次は押下・離上の switch の畳み込み（Q4。別コミット）**

---

## 1. 目的と範囲

`Mapping.cs`（2,060 行）に残る、マクロの再生（約 300 行）を、責務ごとの部品へ切り出す。挙動は変えない（実際に出るキー・マウス・ボタン・ライトバー・振動のイベントが一致）。

### 1.1 再計測（2026-10-22）で分かったこと

- 親計画書の `RunLightbarMacro` は、すでに存在しない（ライトバーのマクロは `PlayMacroCodeValue` の中の 1 分岐）。`EndMacroDirect` は空実装（TODO コメントつき）。
- マクロの入口は、すべて `Mapping.PlayMacroDirect`／`PlayMacroString`／`EndMacroDirect`（`IMapping`／`DefaultMacroPlayer`／`DefaultMacroStringPlayer` が委譲）。これらの窓口は変えない。
- **挙動のテストが既にある**: `Phase8Step78cW2ControlsMacroTests`、`Phase10Step3W3EmptyMacroTests`、`Phase8Step77bW2MultiTapTests`、`Phase10Step6W8ControlMultiActionTests` などが `PlayMacroDirect` 経由でキー送出を記録して確かめている。移し先でも、入口が同じなので、そのまま守りになる。ソースを走査する守り（`MappingMacroPathGlobalReferenceGuardTests`、`Phase7Step7MacroRuntimeStateGuardTests`、`Phase10Step3W3EmptyMacroTests` の一部）は、移し先に合わせて直す。

| 対象 | 行 | 規模 | 性質 |
|---|---|---|---|
| `PlayMacroCodeValue` | 1805 | 約 160 | 1 つのマクロ値（キー、マウスボタン、ゲームパッドのボタン、ライトバー、振動、待ち）の実行。キーの押下と離上の 2 つの switch がほぼ同じ形 |
| `PlayMacro`、`PlayMacroTask`、`RunMacroOnce` | 1634〜 | 約 170 | 実行の予約（同期キューと単発）、押している間の繰り返し、1 回分の再生、後始末（押しっぱなしのキーの解放、ライトバーの復帰） |
| `macroControl` の重ね書き（`MapCustom` の中） | 約 1000 | 約 28 | マクロが押しているゲームパッドのボタンを、出力へ重ねる |
| `HandleDeviceDisconnect` の中の、キューのクリア | 約 1279 | 約 10 | 切断時の後始末 |

## 2. 設計

### 2.1 新しい部品（1 ファイル 1 型。契約は `DI/`、実装は `DS4Control/Services/`）

| 部品 | 引き受ける処理 | 依存 |
|---|---|---|
| `IMacroStepExecutor`／`MacroStepExecutor` | `PlayMacroCodeValue`（1 つのマクロ値の実行。待ちが要るかを返す） | `IVirtualKBM`、`IProfileSettingsService`、`MacroRuntimeState`、`Func<IDeviceStateAccessor>`（振動） |
| `IMacroPlayback`／`MacroPlayback` | `PlayMacro`／`PlayMacroTask`／`RunMacroOnce`（予約、同期キュー、繰り返し、後始末）と、切断時のキューのクリア（`ClearDevice`） | `IMacroStepExecutor`、`MacroRuntimeState`（キュー） |

- `Mapping` は、`PlayMacroDirect`／`PlayMacroString`／`EndMacroDirect` を、`IMacroPlayback` への薄い委譲にする。`HandleDeviceDisconnect` は `ClearDevice` を呼ぶ。
- **マクロの重ね書き**（約 28 行）は、新しい型を作らず、状態を持つ `MacroRuntimeState` に `ApplyHeldControls(DS4State mapped)` として置く（状態の持ち主が、自分の状態を出力へ重ねる。割り当て 0）。`MapCustom` は 1 行の呼び出しになる。
- **既存の静的な参照**（`ActionManager.GetStateFor`／`ActionManager.IsBeingTriggered`、`DS4LightBar.forcelight`／`forcedColor`／`forcedFlash`）は、今回は変えずに、そのまま移す。新しい静的な窓口は作らない。移し先のコードに、規約 3.3 原則 4 の TODO を付ける（`IsBeingTriggered` は `IManagedActionManager` の契約にまだない。Actions サブシステムの扱い［規約 R3］とライトバーの `ILightbarService` 化［9-E 付近］で解消する）。
- 名前空間とファイル配置は規約 §3.3・§3.4（R1〜R7）のとおり。

### 2.2 Wave の分け方

| Wave | 内容 | 先に足すテスト |
|---|---|---|
| **C-1** | `PlayMacroCodeValue` → `MacroStepExecutor` | 旧実装の逐語的な写しとの差分テスト: マクロ値の格子（キー 0〜255、マウス 256〜260、ゲームパッド 261〜、待ち、ライトバー、振動）× 押下と離上 × スキャンコードの有無 × キー変換の有無で、記録したイベントの列と状態（`keydown`、`MacroControl`、`MacroCount`）が一致すること |
| **C-2** | `PlayMacro`／`PlayMacroTask`／`RunMacroOnce` → `MacroPlayback`、切断時のクリア、重ね書き → `MacroRuntimeState.ApplyHeldControls` | 既存の挙動テスト（入口の `PlayMacroDirect` が同じ）に加え、重ね書きの値の表（26 フラグ × 出力）、`ClearDevice`、文字列マクロの解析（`"300/261/…"`）、空のマクロの繰り返し間隔 |

各 Wave の終わりに、`Debug ALL`（警告 0・エラー 0・全テスト合格）、割り当て 0（`MappingHotPathAllocationTests`。`ApplyHeldControls` は毎フレーム呼ばれる）、性能の計測（5% 以内）を行う。ソースを走査する既存の守りは、移し先に合わせて直す（Wave ごとに、一覧を計画書へ記録）。

### 2.3 期待する結果

- `Mapping.cs`: 2,060 → 約 1,750 行（約 310 行減）。`PlayMacroCodeValue` 相当は 200 行以下（必要なら、押下と離上の 2 つの switch を表にまとめる畳み込みを、別コミットで行う。Q4）。

## 3. 調査で見つけた、現行の癖（挙動を変えない前提で、記録する）

| # | 内容 | 扱い |
|---|---|---|
| M1 | マクロが押すゲームパッドのボタンのフラグ（`macroControl` の 26 個）と個数（`MacroCount`）は、**全コントローラーで 1 つを共有**している。コントローラー 1 がマクロでボタンを押すと、`MapCustom` が、**全コントローラーの出力に重ねる**（コントローラー 2 にも出る）。**9-B の加速・端数と同じ種類の癖** | Q2 で扱いを決める |
| M2 | `DeviceRuntimeState` に、**使われていない** `MacroControl`／`MacroCount`（デバイスごと）がある。呼出元 0 件。切断時に `deviceRuntime[device] = new DeviceRuntimeState()` で作り直される | 規約 2.4 の手順: 経緯は、M1 を直すためのデバイスごとの状態の置き場として作られたが未接続の可能性が高い（`git log -S` は巨大な一括コミットまでしか遡れない）。Q2 で「デバイスごと」を選べば、これを使う形にできる（使うなら、`MacroRuntimeState` 側に持たせ、`DeviceRuntimeState` の未使用の 2 つは削除する） |
| M3 | 同期キュー（`macroTaskQueue[device]`）への `TryGetValue`／`Add`／代入は、**ロックなし**（複数スレッドから `PlayMacro` が呼ばれると競合しうる） | そのまま移す。是正は別途 |
| M4 | 振動のマクロ値は、`IDeviceStateAccessor.GetController` が null を返すと `d.setRumble` で NullReferenceException になる（呼び出し側の `Task` の中。握りつぶされる） | そのまま移す。差分テストで固定 |
| M5 | マクロの再生は `Task.Delay(...).Wait()` でスレッドプールのスレッドを塞ぐ | そのまま移す（今回の範囲外） |
| M6 | ゲームパッドのマクロ値の範囲は 261〜`MAX_MACRO_VALUE`（288）だが、出力フラグの配列は 26 個（261〜286）。**287・288 は `IndexOutOfRangeException` になる**（`Task` の中で握りつぎれる） | そのまま移す。差分テストで「旧と新が同じ例外」を固定。是正は別途（`MacroControlLength` を 28 にするか、値の範囲を 286 までに絞る） |

## 4. 質問（推奨は A）

- **Q1（Wave の粒度）**: A＝C-1 → C-2 の 2 回（推奨。キー送出の部品［C-1］が、実際のイベントの差分テストで先に固められる）／B＝まとめて 1 回（9-B はこちらで進めた）。
- **Q2（M1: マクロのボタンの共有）**: A＝**今回は、全コントローラーで共有のまま移す**（推奨。挙動を変えない。1 台で使う人には影響がなく、M1 は別の是正として後で判断）／B＝**デバイスごとに分ける**（9-B と同じ考え方。複数台のとき、コントローラー 1 のマクロが、コントローラー 2 の出力にボタンを出さなくなる。**挙動の変更**。`MacroRuntimeState` の `MacroControl`／`MacroCount` をデバイスごとの配列にし、`Phase7Step7MacroRuntimeStateGuardTests` ほかを直す）。
- **Q3（M2: 未使用の `DeviceRuntimeState.MacroControl`／`MacroCount`）**: A＝Q2＝B のときは削除（使うのは `MacroRuntimeState`）、Q2＝A のときは TODO を付けて残す（推奨）／B＝どちらでも削除する。
- **Q4（押下・離上の switch の畳み込み）**: A＝移した後に別コミットで、押下と離上の 2 つの switch（マウスボタン 5 種、キー、スキャンコード）を表にまとめる。差分テストが 1 イベントも変わらないことが条件（推奨）／B＝畳まない（`PlayMacroCodeValue` 相当は約 160 行のまま。200 行以下の目標は満たす）。

## 5. リスクと対応

| リスク | 対応 |
|---|---|
| 実際のキー・マウス送出に関わり、取りこぼしがユーザーに見える | 差分テスト（イベントの列の完全一致）と、既存の挙動テストで守る。実機確認（台帳に追加）を行う |
| 非同期（`Task`）の順序・競合が変わる | 予約の方法（同期キュー、単発）と待ち（`Task.Delay(...).Wait()`）を、コードのまま移す。M3・M5 は変えない |
| 静的な参照（`ActionManager`、`DS4LightBar`）を新しい部品が持つ | 既存の参照をそのまま移すだけで、窓口は増やさない。TODO で記録（規約 3.3 原則 4）。ラチェット（`Global`／`AppHost`／`Program.rootHub`）の件数は変わらない |
| ソースを走査する既存の守りが、移動で壊れる | Wave ごとに一覧にして直す（`MappingMacroPathGlobalReferenceGuardTests`、`Phase7Step7MacroRuntimeStateGuardTests`、`Phase10Step3W3EmptyMacroTests`、`Phase8Step7AltTabRemovalGuardTests`、`Step10FourGlobalRatchetGuardTests` の `Mapping.cs` の行ほか） |

## 6. 実機確認（想定。実装後に依頼）

(1) SA と Controls のマクロ（キー、マウスボタン、ゲームパッドのボタン、ライトバーの色、振動、待ち）が従来どおり動く。(2) 「押している間繰り返す」マクロが、押している間だけ繰り返し、離すと止まる。(3) 〔Q2＝B のとき〕コントローラー 2 台で、一方のマクロのボタンが、もう一方に出ない。

## 7. 次

承認（Q1〜Q4）を得たら、C-1 から着手する。ビルド・テストが必要になった時点で、ユーザーへ依頼する。

---

## 8. 決定事項（2026-10-22、ユーザー承認）

- **Q1＝まとめて 1 回**: C-1・C-2 を 1 回で実装する。
- **Q2＝デバイスごとに分ける**: マクロが押すゲームパッドのボタンのフラグ（`MacroControl`）と個数（`MacroCount`）を、`MacroRuntimeState` にデバイスごとに持つ（`bool[][]`／`uint[]`）。**挙動の変更**（2 台以上のとき、コントローラー 1 のマクロのボタンが、コントローラー 2 の出力に出なくなる。1 台のときは変わらない）。
- **Q3＝Q2 の結果に合わせる**: Q2＝デバイスごとのため、未使用だった `DeviceRuntimeState.MacroControl`／`MacroCount` を削除した（コメントで経緯を残した）。
- **Q4＝別コミットで畳む**: 押下・離上の switch の畳み込みは未実施。**ビルド・テストでそのままの移動が合格してから**行う。

## 9. 実装の結果（2026-10-22。この環境に `dotnet` がないため、ビルド・テストは未実行）

| 区分 | 内容 |
|---|---|
| 新規（契約） | `DI/IMacroStepExecutor.cs`、`DI/IMacroPlayback.cs` |
| 新規（実装） | `DS4Control/Services/MacroStepExecutor.cs`（旧 `PlayMacroCodeValue` を、そのまま移した。約 165 行）、`MacroPlayback.cs`（旧 `PlayMacro`／`PlayMacroTask`／`RunMacroOnce` と定数を移し、切断時のキューのクリア `ClearDevice` を持つ） |
| `MacroRuntimeState` | `MacroControl` を `bool[][]`、`MacroCount` を `uint[]`（デバイスごと）に変更。`ApplyHeldControls(int device, DS4State mapped)` を追加（旧 `MapCustom` の末尾のブロック） |
| `Mapping.cs` | 2,060 → **1,686 行**（374 行減）。マクロの 4 メソッド、`macroControl`／`macroTaskQueue` の複製、`_deviceStateAccessor` を削除。`PlayMacroDirect`／`PlayMacroString` は `IMacroPlayback.Play` へ、`HandleDeviceDisconnect` は `ClearDevice` へ、重ね書きは `ApplyHeldControls` 1 行に。コンストラクターの引数は 22 のまま（`Func<IDeviceStateAccessor>` を `IMacroPlayback` に置き換え） |
| DI | `IMacroStepExecutor`／`IMacroPlayback` を Singleton で登録 |
| テスト（新規） | `Phase9CMacroStepExecutorDifferentialTests`（旧実装との差分。キー、マウス、ゲームパッド、待ち、ライトバーの値 × 押下・離上 × スキャンコードの有無。振動と M6 は「同じ例外」、デバイス独立）、`Phase9CMacroRuntimeStateTests`（26 フラグの出力の表、デバイス独立）、`Phase9CMacroPlaybackTests`（文字列・リスト・配列、終わりの後始末、`ClearDevice`）、`Phase9CMacroGuardTests` |
| テスト（追従） | `TestMappingFactory`、`Phase7Step8W2MappingInstanceGuardTests`、`MappingMacroPathGlobalReferenceGuardTests`（検査の対象を `MacroStepExecutor.cs` に）、`Phase7Step7MacroRuntimeStateGuardTests`、`Phase10Step3W3EmptyMacroTests`（`MacroPlayback.cs` を走査）、`Phase8Step7AltTabRemovalGuardTests`（移し先も走査） |

**実機確認（依頼。実装後）**: 台帳 §32（P9C-1〜4）。
