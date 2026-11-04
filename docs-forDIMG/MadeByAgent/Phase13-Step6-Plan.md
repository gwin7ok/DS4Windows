# Phase13-Step6 個別計画書: `Func<ControlService>` の撤廃（9 利用者の置き換え）

作成日: 2026-11-04
状態: **承認済み（2026-11-04。§9 の 1〜7 すべて）。実装中（6-a から）**
親計画: `Phase13-Plan.md` §4 Step6、§9 R3。前提: Step3（`IDeviceRumbleService`）、Step5・5b（`IDeviceSlotState`、`IInputReportProcessor`）の完了（`24f046d1`）。

---

## 1. 目的

1. `Func<ControlService>` を使う 9 クラス（と `ServiceRegistration` の登録 1 件）を、**狭いインターフェースの直接注入**に置き換え、`Func<ControlService>` を 0 件にする。
2. 「`ControlService` 型を引数に取る」構造（約 60 のシグネチャ。ロードマップの R3）を、`ControlService` に依存しない型へ置き換える。
3. 機能・ログ・挙動は変えない（§2.2・§2.3）。

## 2. 現状の調査（2026-11-04 の実測）

### 2.1 9 利用者が `ControlService` から使っているもの

| # | 利用者 | 使っているメンバー | 循環の有無（`ControlService` のコンストラクターの依存グラフ上か） | 置き換えの方向 |
|---|---|---|---|---|
| 1 | `ProfileSettingsService` | `GetController(index)` の 2 か所（`GetRumbleBoost`、`SetRumbleAutostopTime`）のみ | あり（`ControlService` → `IProfileSettingsService`） | `IDeviceStateAccessor`（実体 `DeviceSlotState`。依存なし）を直接注入 |
| 2 | `DefaultControlExtrasPort` | `setRumble(heavy, light, slot)` の 1 か所 | **なし**（プローブ［6-a］の結果。ポートは `Func<IActionBindingBuilder>` の先で、`ControlService` の構築時に作られない）。ただし狭い IF にする（依存の向き） | `IDeviceStateAccessor`＋`IDeviceRumbleService`＋`IEnvironmentService.ControllerSlotLimit`（`setRumble` の 4 行のガードを再現） |
| 3 | `DefaultBatteryCheckPort` | `GetDS4Battery(slot)` の 1 か所（約 25 行の文言の組み立て） | **なし**（同上） | 文言の組み立てを小さなサービスへ移す（§4.2） |
| 4 | `AutoProfileService` | `Control.running`（4 か所）、`DS4Controllers[slot]` の null 判定（接続の既定判定） | なし〜要確認（`ControlService` は `AutoProfileService` に依存しない） | `IServiceLifecycle.Running`＋`IDeviceStateAccessor` を直接注入（`IServiceLifecycle` は既に注入可能） |
| 5 | `ProfileRepository` | `LoadProfile` が `control` を `IProfileXmlStore.LoadProfileXml(…, control, …)` に渡す。`LoadTempProfile(…, ControlService control, …)` も | あり（`ControlService` → `IProfileRepository`） | `IProfileLoadTarget`（§4.1） |
| 6 | `ProfileApplicationService` | `DS4Controllers[slot]`（2 か所）と、`ApplyCore(…, control, …)` 経由で `LoadNormalProfile` が `control.DS4Controllers[device]` | あり（`Mapping` → `IProfileApplicationService`） | `IDeviceStateAccessor`＋`IProfileLoadTarget` |
| 7〜9 | `EditorWindowFactory`／`MainWindowFactory`／`ViewModelFactory` | 画面・ViewModel のコンストラクターに `ControlService` を渡すだけ（UI は `ControlService` を広く使う） | **なし**（`ControlService` はこれらに依存しない）。起動順の注意だけ（K7-1: ホスト構築前に `ControlService` を解決しない。ファクトリーは `ResolvePostHostService` で構築後に解決） | `ControlService` の直接注入（§4.3） |

### 2.2 プロファイル読込の鎖が `ControlService` から使うもの（R3 の中身）

`control` 引数は、`ProfileRepository.LoadProfile` → `IProfileXmlStore.LoadProfileXml` → `LegacyProfileXmlSerializer.LoadProfile` → `ProfileLoadContext` → 各 `Profile*LoadSection` → `BackingStore` の `SetGyro*`／`ResetMouseProperties`／`CheckOldDevicestatus`／`PostLoadSnippet` と流れる。`ProfilePresets`（10 メソッド）→ `IProfilePresetLoader`（10）→ `PresetOption*`（UI 6 クラス）も同じ型を渡す。実際に呼ぶメンバーは次の 8 つだけで、**`PreLoadReset` 以外はすべて、既に独立したサービスに実体がある**:

| `control.` のメンバー | 実体（転送先） |
|---|---|
| `DS4Controllers[i]`、`touchPad[i]`、`outputDevices[i]` | `IDeviceSlotState`、`IVirtualPadService.OutputDevices` |
| `PluginOutDev`／`UnplugOutDev` | `IVirtualPadService`（`ControlService` は転送するだけ） |
| `CheckProfileOptions(ind, device, startUp)` | `IDeviceConnectionSetup.ApplyProfileOptions(ind, device, touchPad[ind], startUp)` |
| `StartTPOff(slot)` | `ITouchpadToggleService.StartTouchpadOff` |
| `PreLoadReset(ind)` | **`ControlService` 内**（`StickMotionState` の FlickStick・DeltaAccel のリセットと `touchPad[ind]?.Reset()`。約 15 行） |

`ProfileSettingsService.SetGyro*`（4）→ `BackingStore.SetGyro*` は `control.touchPad[index]` のみ使う。

### 2.3 依存の向き（循環）の見立て

`ControlService` → { `IProfileSettingsService`、`IProfileRepository`、`IMapping`（→ `IProfileApplicationService`、エンジン → ポート）… } のため、1〜3・5・6 が `ControlService` を直接受けると循環する。一方、`IDeviceSlotState`／`IDeviceStateAccessor` は依存なし、`IDeviceConnectionSetup` は `IProfileSettingsService`・`IUdpMotionService`・`StickMotionState`・`IInputDeviceModuleRegistry` に依存し、`IVirtualPadService` は `IProfileSettingsService`・`Func<IOutputSlotService>`・`IDeviceStateAccessor` ほかに依存する。→ **`BackingStore` のコンストラクターに、これらを足してはならない**（`IProfileSettingsService` → `BackingStore` → `IDeviceConnectionSetup` → `IProfileSettingsService` の循環）。したがって、`control` を**引数のまま、型だけ置き換える**（§4.1）。実装前に、解決グラフのプローブ（§6 6-a）で確かめる。

### 2.4 テスト

`Func<ControlService>` を使うテストは 14 か所、`ControlService` を引数に取る呼び出しは多数（`ControlServiceTestFactory`、`TestRepositoryFactory`、プリセット・ViewModel のテスト）。着手時に再測定する。

## 3. 事前調査（規約 §4）

- 「慣れない機能はゼロから書かず OSS を調べる」: 同種の「循環を避けるための `Func<T>` を、狭い IF へ置き換える」手法は一般的な設計（役割別インターフェース［Role Interface］への分割）で、新しい技術はない。調査は省略（Step5 で調べた結果と同じ）。
- 経緯: `Func<ControlService>` は Phase6-Step10-4（決定 D4-3）で `Program.rootHub`／`AppHost` の参照を廃止するために導入された暫定策（各クラスのコメント）。

## 4. 設計案

### 4.1 `IProfileLoadTarget`（プロファイル読込の鎖のための窓口）

`control` 引数の型を `ControlService` から `IProfileLoadTarget` に置き換える（引数は残す。メソッドの本文は `control.` → `target.` の機械的な置換）。

```
interface IProfileLoadTarget   // DS4Windows/DI/
{
    DS4Device GetController(int ind);          // = IDeviceSlotState.GetController
    Mouse GetTouchPad(int ind);                // = slots.TouchPads[ind]
    OutputDevice GetOutputDevice(int ind);     // = virtualPad.OutputDevices[ind]
    void PluginOutDev(int index, DS4Device device);
    void UnplugOutDev(int index, DS4Device device, bool immediate = false, bool force = false);
    void CheckProfileOptions(int ind, DS4Device device, bool startUp = false);
    void StartTPOff(int deviceID);
    void PreLoadReset(int ind);                // ControlService から移す
}
```

- 実装 `ProfileLoadTarget`（`DS4Windows/DS4Control/Services/`、`DS4Windows.Services`）の依存: `IDeviceSlotState`、`IVirtualPadService`、`IDeviceConnectionSetup`、`ITouchpadToggleService`、`StickMotionState`。いずれも `ProfileRepository`／`ProfileApplicationService` を通らず、循環しない（6-a のプローブで確認）。
- `ControlService.PreLoadReset` は `IProfileLoadTarget` へ転送する（§3.3 原則 4 の TODO つき。呼び出し元を切り替えた後、Step10 で削除）。`CheckProfileOptions`／`StartTPOff`／`PluginOutDev`／`UnplugOutDev` の `ControlService` 側は既に転送だけで、Step10 の対象。
- 配列を `GetTouchPad(int)` などのメソッドにする理由: 呼び出しは設定の読込時（接続・プロファイル適用）で、ホットパスではない。範囲外の添字の扱いは、元の `control.touchPad[index]`（例外）から変えないよう、実装は配列を直接添字参照する。
- UI 側（`PresetOption*` ほか）は `ControlService` を持つため、型の置き換え後は `IProfileLoadTarget` を注入で受けるか、`ControlService` から取得する。→ §9 の決定事項 2。

### 4.2 `DefaultBatteryCheckPort` の電池残量の文言

`ControlService.GetDS4Battery`（約 25 行）の本体を、新しい `IDeviceBatteryText`／`DeviceBatteryText`（依存: `IDeviceStateAccessor`）へ移し、`ControlService.GetDS4Battery` は転送する（TODO つき。UI の利用者がいるため温存、Step10 で整理）。本文は変えない。既存コードの `battery` が未割り当てになりうる箇所（`IsAlive()` が false のとき）は、**そのまま移す**（挙動を維持。改善は別件）。

### 4.3 UI ファクトリー 3 つの直接注入

`EditorWindowFactory`／`MainWindowFactory`／`ViewModelFactory` は `Func<ControlService>` をやめ、`ControlService` を直接受ける。登録済みの `ControlService`（Singleton）はファクトリーの生成時（`ResolvePostHostService`、構築後）に解決されるため、K7-1 を侵さない。`ViewModelFactory` が構築時に `ControlService` を作ると、その時点でホストが構築済みであることが条件になるため、**6-a のプローブで、ファクトリーの生成が構築後に限られること**（テストの `TestHost` を含む）を確かめる。懸念があれば、この 3 件だけ `Func` を理由つきで残す（R3 の予備案）。

### 4.4 `AutoProfileService`

`Control.running` → `IServiceLifecycle.Running`、`DS4Controllers[slot]` の null 判定 → `IDeviceStateAccessor.GetController`。`Control != null` の分岐（`ControlService` が未解決のとき）は、直接注入では常に真になるため削除する（起動前の判定は `IServiceLifecycle.Running == false` が同じ結果を返す）。これは Step9（F-5c）の「4 つの利用者」の 1 つ（`AutoProfileService`）を前倒しで済ませる。Step9 の対象から外す（`Phase13-Plan.md` を更新）。

## 5. 影響範囲

- 本番: `DS4Windows/DI/`（`IProfileLoadTarget`、`IDeviceBatteryText` を新設）、`DS4Windows/DS4Control/Services/`（`ProfileLoadTarget`、`DeviceBatteryText` を新設）、`ProfileSettingsService`、`DefaultControlExtrasPort`、`DefaultBatteryCheckPort`、`AutoProfileService`、`ProfileRepository`、`ProfileApplicationService`、`EditorWindowFactory`、`MainWindowFactory`、`ViewModelFactory`、`BackingStore`・`LegacyProfileXmlSerializer`・`ProfileXmlStore`・`ProfileLoadContext`・`Profile*LoadSection`・`ProfilePresets`・`ProfilePresetLoader`（シグネチャの型のみ）、`ServiceRegistration`（`Func<ControlService>` の登録削除、新規 2 件の登録）。
- UI: `PresetOption*`（6 クラス）・`PresetOptionWindow`・`PresetOptionViewModel` ほか、`ControlService` を `IProfilePresetLoader` へ渡す箇所（§9 の 2 次第）。
- テスト: `Func<ControlService>` 14 か所、ctor の null 検査、`IProfileLoadTarget` の特性テスト（`ProfileLoadTarget` が従来の `ControlService` 経由と同じ結果になること）、`Phase13Step5FuncRemovalGuardTests` に `Func<ControlService>` 0 件の検査を追加。

## 6. 実装の分割（各サブステップを別コミット。各回 `Debug ALL`）

| サブ | 内容 | ゲート |
|---|---|---|
| **6-a** | 調査と下準備: 解決グラフのプローブ（`ControlService` を起点に、各クラスが `ControlService` を直接受けた場合の循環の有無をコンテナで実際に確かめるテスト）、`IProfileLoadTarget` の使用メンバーの確定（§2.2 の再確認）、特性テスト（`ProfileSettingsService.GetRumbleBoost`／`SetRumbleAutostopTime`、`DefaultControlExtrasPort`、`DefaultBatteryCheckPort`、`AutoProfileService` の接続判定、`ProfileRepository.LoadProfile`） | `Debug ALL` |
| **6-b** | 狭い IF で足りるもの: `ProfileSettingsService`（`IDeviceStateAccessor`）、`DefaultControlExtrasPort`、`DefaultBatteryCheckPort`（`IDeviceBatteryText` 新設）。`AutoProfileService`（`IServiceLifecycle`＋`IDeviceStateAccessor`） | `Debug ALL` |
| **6-c** | UI ファクトリー 3 つの直接注入（`Func<ControlService>` の 3 件） | `Debug ALL` |
| **6-d** | `IProfileLoadTarget`／`ProfileLoadTarget` の新設と登録、`ControlService.PreLoadReset` の転送化。まだ `control` 引数の型は変えない（追加のみ） | `Debug ALL` |
| **6-e** | 鎖の型の置き換え: `IProfileXmlStore`／`LegacyProfileXmlSerializer`／`ProfileLoadContext`／`BackingStore`／`IProfilePresetLoader` ほか（約 60 シグネチャ）の `ControlService control` → `IProfileLoadTarget target`（本文は機械的な置換）。`diff -w` で置換以外の差がないことを確認 | `Debug ALL` |
| **6-f** | `ProfileRepository`／`ProfileApplicationService` の `Func<ControlService>` を直接注入（`IProfileLoadTarget`、`IDeviceStateAccessor`）に。UI の呼び出し元の切り替え。`ServiceRegistration` の `Func<ControlService>` の登録を削除。ガード（`Func<ControlService>` 0 件）とラチェットの更新、記録 | `Debug ALL` |

6-e は機械的だが行数が多いため、`BackingStore`（巨大ファイル）はピンポイント置換（規約 §3.2）。6-f の後に実機で確認する項目は、Phase 末の 1 回にまとめる（Q8）。

## 7. リスクと対策

| リスク | 対策 |
|---|---|
| 直接注入で循環が生じ、起動時に `InvalidOperationException`（循環依存）になる | 6-a のプローブで、実装前に確かめる。生じた 1 件は、理由つきで `Func` を残し、台帳に記録する（R3） |
| `ProfileLoadTarget` の配列アクセスが従来と変わり、範囲外の扱いが変わる | 配列を直接添字参照する（例外の挙動を維持）。特性テストで固定 |
| `AutoProfileService` の `Control != null` の分岐の削除で、起動前の挙動が変わる | `IServiceLifecycle.Running` の初期値が false であることを確認し、特性テストで固定 |
| UI ファクトリーが構築前に解決され、K7-1（ホスト構築前の `ControlService` 解決）を侵す | 6-a のプローブで確認。懸念が残る場合は 3 件だけ `Func` を残す |
| `BackingStore` の巨大ファイルの誤編集 | ピンポイント置換のみ。全体を再生成しない。`diff -w` で置換以外の差を確認 |
| 実機の不具合の原因が Step ごとに特定しにくい | サブステップごとに別コミット（`git bisect` 可） |

## 8. 規約への適合

- §1 依存の向き: 第 4 層の UI／サービスが、第 2 層の狭い IF（`IProfileLoadTarget` など）に依存する形へ寄せる。
- §2.1: 古い静的シムは新設しない。`ControlService` の転送メンバーは TODO つきで温存（Step10 で削除）。§2.2・§2.3: 機能・ログ・文言は変えない。
- §3.1: Pure DI。`AppHost.Services` は持ち込まない。登録は `ServiceRegistration.cs` に集約。
- §3.3: 1 ファイル 1 型（`IProfileLoadTarget`、`ProfileLoadTarget`、`IDeviceBatteryText`、`DeviceBatteryText` は別ファイル）。名前空間は `DS4Windows.DI`／`DS4Windows.Services`。原則 4: 転送メンバーに TODO を付ける。
- §3.4: インターフェースは `DS4Windows/DI/`（R1）、実装は `DS4Windows/DS4Control/Services/`（R2）。
- §4: 各サブステップでテストを作成・実行し、`Debug ALL` を維持。

## 9. 決定事項（承認をお願いします）

1. **方針**: `Func<ControlService>` の 9 件を、§2.1 の置き換え方向（狭い IF の直接注入）で撤廃する。1 件でも循環で解けなければ、その 1 件だけ理由つきで残す（R3）。
2. **`IProfileLoadTarget` の採用**: `control` 引数の型だけを `ControlService` → `IProfileLoadTarget` に置き換える（引数は残す。`BackingStore` に新しい依存を足さない）。UI 側（`PresetOption*` ほか）は、`IProfileLoadTarget` を注入で受ける（推奨）か、当面 `ControlService` から取得する（案 B。UI の変更が少ない）か。→ **案 A（注入）を推奨**。
3. **`DefaultBatteryCheckPort`**: 文言の組み立てを `IDeviceBatteryText`／`DeviceBatteryText` に移す（`ControlService.GetDS4Battery` は転送。Step10 で整理）。
4. **`AutoProfileService` の前倒し**: Step9（F-5c）の対象から `AutoProfileService` を外し、Step6 の 6-b で `IServiceLifecycle` を直接注入にする。
5. **UI ファクトリー 3 つ**: `ControlService` の直接注入にする（6-a のプローブで構築後の解決を確認する条件つき）。
6. **実装の分割**: 6-a〜6-f を別コミットにする（6-a から着手）。`PreLoadReset` は `IProfileLoadTarget` へ移し、`ControlService` は転送（TODO）。
7. **A/B 計測は不要**（プロファイル適用・設定の読込でホットパスではない）。実機の確認は Phase 末の 1 回にまとめる（Q8）。確認項目: プロファイルの適用・一時プロファイル・自動プロファイル、振動（アクションの Rumble）、電池残量の表示。

## 10. 6-a の結果（2026-11-04）

- **解決グラフのプローブ**（`Phase13Step6ResolutionGraphProbeTests`。`ServiceRegistration` の登録とコンストラクターの引数を静的にたどり、`Func<T>` は辺にしない。ラムダ登録の型は実体の型を与える）。`ControlService` の推移的な依存は 75 型。9 利用者の判定:
  - **グラフの内側（`ControlService` を直接受けると循環）**: `ProfileSettingsService`、`ProfileRepository`、`ProfileApplicationService`（予想どおり）。
  - **グラフの外側**: `AutoProfileService`、`EditorWindowFactory`、`MainWindowFactory`、`ViewModelFactory`（予想どおり）、**`DefaultControlExtrasPort`、`DefaultBatteryCheckPort`（予想と違い外側）**。2 つのポートは束縛のビルダー（`Func<IActionBindingBuilder>` で遅延）の先にあり、`ControlService` の構築時には作られない。→ 直接 `ControlService` を受けても循環はしないが、計画どおり狭い IF にする（§1 の依存の向き）。§2.1 の表を実測に合わせて修正した。
  - 追加の検査: `IProfileLoadTarget` の依存先（`IDeviceSlotState`、`IVirtualPadService`、`IDeviceConnectionSetup`、`ITouchpadToggleService`、`StickMotionState`）は、`ProfileRepository`／`ProfileApplicationService`／`ControlService` に到達しない（→ `ProfileLoadTarget` を作ってもそれらが受けて循環しない）。`IDeviceStateAccessor`（`DeviceSlotState`）は依存なし。UI ファクトリー 3 つと `AutoProfileService` は、`ProfileSettingsService`／`ProfileRepository`／`ProfileApplicationService`／`Mapping` のどのグラフにも現れない（構築前に解決される経路がない）。
- **特性テスト**（`Phase13Step6CharacterizationTests`）: 電池残量の文言（デバイスなし → `NA`、充電なし → `55%`、充電中 → `55%+`、100% で充電中 → `Full`）、`GetRumbleBoost`（DualSense で汎用の再スケールなし → 100 固定、ほかは設定値）。入口は置き換え前の経路（`ControlService.GetDS4Battery`、`ProfileSettingsService.GetRumbleBoost`）。
  - **書けなかったもの**: `setRumble`・`RumbleAutostopTime` の実デバイスへの反映は、`DS4Device` の内部（`testRumble`、タイマー）が未初期化のテスト用デバイスでは動かないため。6-b では `IDeviceRumbleService` の差し替え（フェイク）で、スロットの範囲検査と null 検査を固定する。`AutoProfileService` の接続判定と `ProfileRepository.LoadProfile` は、既存のテスト（`AutoProfileService*Tests`、`ProfileRepository*Tests`）が固定しているため、新規には足さない（6-b／6-f で入口を替えて再実行）。
- **Debug ALL**: Actions.Tests 3095 件（3075 → +20）・Standalone 11 件すべて合格、警告 0・エラー 0。

## 11. 6-b の結果（2026-11-04）

**変更**
- `IDeviceBatteryText`（`DS4Windows/DI/`）／`DeviceBatteryText`（`DS4Windows/DS4Control/Services/`）を新設。旧 `ControlService.GetDS4Battery` の本体をそのまま移した（文言の組み立ては不変）。`GetDS4Battery` は削除（呼び出し元は `DefaultBatteryCheckPort` だけだった）。
- `DefaultBatteryCheckPort`: `Func<ControlService>` → `IDeviceBatteryText`。
- `DefaultControlExtrasPort`: `Func<ControlService>` → `IDeviceStateAccessor`＋`IDeviceRumbleService`＋`IEnvironmentService`。`SetRumble` は旧 `ControlService.setRumble` と同じ検査（スロット上限 → コントローラーの有無）を経て `SetDevRumble` を呼ぶ。
- `ProfileSettingsService`: `Func<ControlService>` → `IDeviceStateAccessor`（使うのは `GetController` だけ）。`BackingStore` 内の暫定生成は `new DeviceSlotState()`（旧 `() => null` と同じ「コントローラーなし」）。
- `AutoProfileService`: `Func<ControlService>` → `IServiceLifecycle`＋`IDeviceStateAccessor`。`Control` プロパティを削除。旧「ControlService が未解決なら接続とみなす」は、直接注入で常に解決済みのため不要になった（本番の挙動は不変）。
- テスト: `TestRepositoryFactory.CreateAutoProfileService` は既定で `SlotConnectedProbe = _ => true`（旧テストの前提「ControlService 未解決＝全スロット接続」を保つ）。`Phase13Step6ControlExtrasPortRumbleTests`（4 件）を追加。

**結果**: `Debug ALL` 警告 0／エラー 0。Actions.Tests 3,099 件、Standalone 11 件、すべて合格。
**残る `Func<ControlService>`**: `ProfileRepository`／`ProfileApplicationService`／UI ファクトリー 3 件（6-c〜6-f）。

## 12. 6-c の結果（2026-11-04）

**変更**: `EditorWindowFactory`／`MainWindowFactory`／`ViewModelFactory` の `Func<ControlService>` を `ControlService` の直接注入へ。3 クラスは `ControlService` の依存グラフの外（6-a のプローブで確認）のため循環しない。K7-1（ホストの暗黙構築）は 2026-10-17 に解消済みで、`MainWindowFactory` の解決は `App.xaml.cs` のホスト構築後（`ResolvePostHostService`）。
**テスト**: `Step10GroupTwoGlobalReferenceGuardTests.EditorWindowFactory_RequiresAllServices` の引数を未初期化の `ControlService` に、`ProfileSettingsViewModelGlobalReferenceGuardTests.ViewModelFactory_HasNoServiceLocatorInConstructor` の期待を「`Func<ControlService>` なし・`ControlService controlService` あり」に更新。
**結果**: `Debug ALL` 警告 0／エラー 0。Actions.Tests 3,099 件、Standalone 11 件、すべて合格。
**実機で見る点（Phase 末の 1 回）**: 起動直後のメイン画面の表示と、エディター・補助ウィンドウが開くこと（`ControlService` の生成時期が「生成のたび」から「ファクトリーの解決時」へ早まったため）。

## 13. 6-d の結果（2026-11-04）

**変更（追加のみ）**: `IProfileLoadTarget`（`DS4Windows/DI/`）と `ProfileLoadTarget`（`DS4Windows/DS4Control/Services/`）を新設し、`ServiceRegistration` に Singleton で登録。依存は `IDeviceSlotState`／`IVirtualPadService`／`IDeviceConnectionSetup`／`ITouchpadToggleService`／`StickMotionState`（6-a のプローブで、`ProfileRepository`／`ProfileApplicationService`／`ControlService` に届かないことを確認済み）。`PreLoadReset` の本体は `ProfileLoadTarget` へ複製した。
**計画からの変更**: 計画（§4.1）は `ControlService.PreLoadReset` を転送にするとしていたが、`ControlService` のコンストラクター引数（26）を増やさないため、**6-d では元の本体のまま残し**（TODO［Phase13-Step6 6-e］つき）、呼び出し元（`LegacyProfileXmlSerializer.LoadProfile` のみ）を切り替える 6-e で削除する（呼び出し元 0 件。規約 §2.4: 使うべき API は `IProfileLoadTarget` 側）。
**テスト**: `Phase13Step6ProfileLoadTargetTests`（6 件）。配列が `ControlService` と同一、範囲外の挙動（`GetController` は null、配列は例外）、`PreLoadReset` が旧経路と同じ結果（テストプロファイル添字では何もしない）、コンストラクターの null 検査。
**結果**: `Debug ALL` 警告 0／エラー 0。Actions.Tests 3,105 件、Standalone 11 件、すべて合格。

## 14. 6-e／6-f の結果（2026-11-04。6-e と 6-f の中核を 1 コミットにまとめた）

**まとめた理由**: `ProfileRepository`／`ProfileApplicationService` は、鎖の `control` 引数の型を `IProfileLoadTarget` に替えると、`Func<ControlService>` から得た `ControlService` を渡せなくなる。そのため、型置換（6-e）と、この 2 クラスの直接注入・UI 呼び出し元の切り替え・`Func<ControlService>` の登録削除（6-f）を同時に行った（分けるとビルドが通らない）。
**型の置換（機械的）**: `ControlService control` → `IProfileLoadTarget control`（引数名 `control` は変えず、差分を最小にした。名前の整理は Step10 以降）。対象 15 ファイル: `IProfilePresetLoader`／`ProfilePresetLoader`／`ProfilePresets`／`BackingStore`（`SetGyro*`・`ResetMouseProperties`・`CheckOldDevicestatus`・`PostLoadSnippet`。ピンポイント置換）／`IProfileSettingsService`／`ProfileSettingsService`／`IProfileXmlStore`／`ProfileXmlStore`／`IProfileRepository`／`LegacyProfileXmlSerializer`／`ProfileLoadContext`／`Profile*LoadSection` 4 件。本文は `control.touchPad[i]`→`GetTouchPad(i)`、`control.DS4Controllers[i]`→`GetController(i)`、`control.outputDevices[i]`→`GetOutputDevice(i)` のみ（他のメンバー呼び出しは同名）。
**挙動の保持**: `ProfileLoadTarget.GetController` は配列を直接参照する（範囲外は例外のまま）。`PostLoadSnippet` などが範囲の検査をしないため。`GetTouchPad`／`GetOutputDevice` も元と同じ。
**直接注入**: `ProfileRepository`／`ProfileApplicationService` の `Func<ControlService>` → `IProfileLoadTarget`。`ProfileRepository.LoadProfile` の `control == null` の分岐（遅延解決が null を返したとき）は、直接注入では起きないため削除した。`ProfileApplicationService` の `_control?.DS4Controllers?[i]` → `_control.GetController(i)`。
**UI（案 A）**: `PresetOption` ほか 6 クラス・`PresetOptionViewModel`・`PresetOptionWindow` は `IProfileLoadTarget` を受ける。`ProfileSettingsViewModel` は `ControlService` の代わりに `IProfileLoadTarget` を受け、`LoadBlankProfile()` は呼び出し元から渡さず自分の窓口を使う（`ProfileEditor` の呼び出しも更新）。`EditorWindowFactory`／`MainWindowFactory`／`ViewModelFactory` は `IProfileLoadTarget` を追加で注入。`MainWindow` は IPC の `LoadTempProfile` に `loadTarget` を渡す。
**削除**: `ControlService.PreLoadReset`（呼び出し元 0 件）、`ServiceRegistration` の `Func<ControlService>` の登録。
**ガード**: `Phase13Step5FuncRemovalGuardTests.FuncOfControlService_NoLongerAppears`（本番ソースに `Func<ControlService>` が 0 件）。
**テスト側**: 遅延（`Func`）の意味を保つ `LazyProfileLoadTarget`（初回使用時にホストから解決）を新設し、`TestRepositoryFactory` などの既定値にした。`PersistenceGoldenContext.LoadTarget` を追加。ソース文字列を検査するガード 3 件（`Phase8Step5W3PublishBindingsTests`、`ProfileEditorBackendViewModelTests`、`Phase7Step7StickMotionStateGuardTests`）の期待を更新。
**結果**: `Debug ALL` 警告 0／エラー 0。Actions.Tests 3,106 件、Standalone 11 件、すべて合格。`Func<ControlService>` は本番ソースで 0 件。
**実機で見る点（Phase 末の 1 回）**: (1) 起動直後のプロファイル読込（`ControlService` の生成より前に `ProfileRepository.LoadProfile` が呼ばれても、`touchPad` が null のときは `SetGyro*` が何もしない）、(2) プロファイルの適用・一時プロファイル・自動プロファイル・IPC（`LoadTempProfile`）、(3) 新規プロファイルのプリセット選択（キャンセルで空プロファイル）、(4) ジャイロ設定のトグル（`SetGyro*`）、(5) 振動（アクションの Rumble）、電池残量の表示。
