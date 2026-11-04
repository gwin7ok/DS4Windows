# Phase13-Step7 個別計画書: `Func<IOutputSlotService>`／`Func<IVirtualPadService>`／`Func<OutputSlotManager>` の相互参照の解消

作成日: 2026-11-04
状態: **承認待ち（§9）**
親計画: `Phase13-Plan.md`（ロードマップ #2・#3 ＋ `OutputSlotService` 第 1 引数の `Func<OutputSlotManager>`）
前提: Step6 完了（`4497492a`）。`ControlService` のコンストラクター引数は 26。

---

## 1. 目的

`VirtualPadService` ⇄ `OutputSlotService` の相互参照を一方向にし、次の `Func` を撤廃する。

| `Func` | 利用者 | 数 |
|---|---|---|
| `Func<IOutputSlotService>`（登録: `ServiceRegistration`） | `ControlService`、`DeviceConnector`、`DeviceDisconnectHandler`、`InputReportProcessor`、`VirtualPadService` | 5 |
| `Func<IVirtualPadService>`（登録あり） | `OutputSlotService` | 1 |
| `Func<OutputSlotManager>`（登録なし。ラムダ） | `OutputSlotService` の第 1 引数 | 1 |

## 2. 調査結果（2026-11-04）

### 2.1 相互参照の実体

- `OutputSlotService` → `IVirtualPadService`: 仮想スロットの抜き差し（`PluginSlot`／`UnplugSlot`。`VirtualPad.…`）と、`OutputSlotManager`（`VirtualPadService.OutputSlotManager`）。
- `VirtualPadService` → `IOutputSlotService`: **配列 `ActiveOutDevType` を 1 回取得するためだけ**（`_outputSlotServiceFactory().ActiveOutDevType`）。メソッドは呼んでいない。
- `ActiveOutDevType`（と `OutDevTypeTemp`）の実体は `OutputSlotService` のフィールド（`CreateFilled`。長さ `TestProfileItemCount`）。参照は再代入されない。
- `Func<IOutputSlotService>` の他の利用者の用途:
  - `DeviceConnector`（`ActiveOutDevType[...]` の読み書き 3 か所）、`DeviceDisconnectHandler`（書き込み 2 か所）、`InputReportProcessor`（ホットパスで `ActiveOutDevType[ind] == OutContType.DS4` を読む。初回のみ取得して配列をキャッシュ）: **配列だけ**。
  - `ControlService`: `ActiveOutDevType`（1 か所）と `SaveOutputSlots()`（355 行）の 2 用途。
- `BackingStore.ActiveOutDevType`（公開プロパティ）へは、`ServiceRegistration` の `IOutputSlotService` の登録ラムダが、**`OutputSlotService` の生成時に**同じ配列を渡す。読む側: `LegacyProfileXmlSerializer`（136）、`ProfilePresets`（7 か所）。登録ラムダのコメントに、`BackingStore` の登録側から `IOutputSlotService` を解決すると `IOutputSlotStore → IPathService → BackingStore` の循環でスタックオーバーフローになる、との警告がある。

### 2.2 結論

相互参照は「配列の持ち主が `OutputSlotService` にある」ことから生じている。配列の持ち主を、依存のない小さなホルダーに移せば、`VirtualPadService`（と配列だけの利用者 3 クラス）は `OutputSlotService` を知る必要がなくなり、`OutputSlotService → VirtualPadService` の一方向になる。`Func` はすべて不要になる。

## 3. 設計案

### 3.1 `IOutputTypeState`／`OutputTypeState`（新設）

```
interface IOutputTypeState   // DS4Windows/DI/、namespace DS4Windows.DI
{
    OutContType[] OutDevTypeTemp { get; }   // UI 一時状態（既定 X360）
    OutContType[] ActiveOutDevType { get; } // 実行時接続状態（既定 None）
}
```

- 実装 `OutputTypeState`（`DS4Windows/DS4Control/Services/`、`DS4Windows.Services`）: 依存なし。配列は `OutputSlotService` の `CreateFilled` をそのまま移す（長さ `TestProfileItemCount`、初期値も同じ）。
- 配列の参照は再代入しない。利用者はコンストラクターで `readonly` に保持する（ホットパスは分岐もなくなる）。
- `BackingStore.ActiveOutDevType` への配線は、`IOutputTypeState` の登録ラムダへ移す（`sp.GetRequiredService<BackingStore>().ActiveOutDevType = state.ActiveOutDevType`）。`BackingStore` の登録は `IOutputTypeState` を解決しないため循環しない。

### 3.2 利用者の切り替え

| クラス | 切り替え |
|---|---|
| `VirtualPadService` | `Func<IOutputSlotService>` → `IOutputTypeState`（配列を `readonly` に保持） |
| `DeviceConnector`／`DeviceDisconnectHandler`／`InputReportProcessor` | 同上 |
| `OutputSlotService` | コンストラクターを `(IVirtualPadService virtualPad, IOutputSlotStore store, IOutputTypeState types)` に。`Func<OutputSlotManager>` と `Func<IVirtualPadService>` を撤廃（`SlotManager => _virtualPad.OutputSlotManager`）。`OutDevTypeTemp`／`ActiveOutDevType` は `IOutputSlotService` の公開 API として残し、ホルダーの配列へ転送する（UI の利用者があるため） |
| `ControlService` | `Func<IOutputSlotService>` → `IOutputSlotService`（引数の数は変わらない）。`ActiveOutDevType` は `_outputSlotService.ActiveOutDevType` を初回に取得（現状と同じ形。コンストラクターで保持してもよい） |

循環の検査: `OutputSlotService → IVirtualPadService → { IEnvironmentService、IProfileSettingsService、IDeviceStateAccessor、IEventThreadDispatcher、IDeviceRumbleService、IOutputTypeState }`、`OutputSlotService → IOutputSlotStore → IPathService`。どれも `OutputSlotService`／`ControlService` に届かない。実装前に、6-a と同じ解決グラフのプローブ（`Phase13Step6ResolutionGraphProbeTests` の `Closure`）で確かめる。

### 3.3 `Func` の登録の削除

`ServiceRegistration` の `Func<IOutputSlotService>`（202 行）と `Func<IVirtualPadService>`（203 行）の登録を削除する。`IOutputSlotService` の登録ラムダは、`new OutputSlotService(virtualPad, store, types)` だけにする（`BackingStore` への配線を外す）。

## 4. 実装の分割（各サブステップを別コミット。各回 `Debug ALL`）

| サブ | 内容 |
|---|---|
| **7-a** | 解決グラフのプローブ（`IOutputTypeState` の依存なし、`VirtualPadService` の閉包が `OutputSlotService`／`ControlService` に届かないこと）、特性テスト（`ActiveOutDevType`／`OutDevTypeTemp` の初期値・長さ・同一配列、`BackingStore.ActiveOutDevType` が同じ配列になること、`OutputSlotService` の抜き差しの現状の挙動）。 |
| **7-b** | `IOutputTypeState`／`OutputTypeState` の新設と登録（追加のみ）。`BackingStore` への配線を新しい登録へ移す（`OutputSlotService` の登録ラムダは、一時的に配列をホルダーから受ける）。 |
| **7-c** | 配列だけの利用者 4 クラス（`VirtualPadService`、`DeviceConnector`、`DeviceDisconnectHandler`、`InputReportProcessor`）を `IOutputTypeState` に切り替え。`InputReportProcessor` は 0 アロケーションの特性テストと、`DS4W_RUN_BENCH=1` の計測で悪化がないことを確認。 |
| **7-d** | `OutputSlotService` の直接注入化（`Func<OutputSlotManager>`／`Func<IVirtualPadService>` の撤廃）、`ControlService` の `IOutputSlotService` 直接注入、`ServiceRegistration` の `Func` の登録削除、ガード（`Func<IOutputSlotService>`／`Func<IVirtualPadService>` が本番ソースに 0 件）、台帳・計画書の更新。 |

## 5. 影響範囲

- 本番: `DS4Windows/DI/`（`IOutputTypeState` を新設）、`DS4Windows/DS4Control/Services/`（`OutputTypeState` を新設、`OutputSlotService`、`VirtualPadService`、`DeviceConnector`、`DeviceDisconnectHandler`、`InputReportProcessor`）、`ControlService`（フィールド・引数の型のみ）、`ServiceRegistration`。
- テスト: `OutputSlotService` を生成するテスト（`TestRepositoryFactory.CreateOutputSlotService` ほか 47 か所のうち生成そのものは数か所）、`VirtualPadService`／`DeviceConnector`／`DeviceDisconnectHandler`／`InputReportProcessor` を生成するテスト、ソース文字列を検査するガード。`OutputSlotManager` を差し替えているテスト（`OutputSlotServiceTests`、`OutputSlotServiceSsotTests`）のために、`IVirtualPadService` の小さな偽実装（17 行のインターフェース）を用意する。

## 6. リスクと対策

| # | リスク | 対策 |
|---|---|---|
| R1 | `BackingStore.ActiveOutDevType` の配線のタイミングが変わる（以前は `OutputSlotService` の生成時。今後は `IOutputTypeState` の生成時） | `IOutputTypeState` は `VirtualPadService` などから先に解決されるため、`BackingStore` を読む側より遅れない。7-a で「どの利用者を解決しても、`BackingStore.ActiveOutDevType` が同じ配列になる」テストを書く |
| R2 | 登録ラムダの循環（警告コメント）を再発させる | `BackingStore` の登録は触らない。`IOutputTypeState` は依存なし。プローブで確認 |
| R3 | `InputReportProcessor` のホットパスの劣化 | 配列を `readonly` に保持するだけで、分岐は減る。0 アロケーションの特性テスト＋計測 |
| R4 | `OutputSlotService` を差し替えるテストの書き換えの量 | 偽 `IVirtualPadService`（`OutputSlotManager` だけ使う）で吸収。`TestRepositoryFactory` に集約 |

## 7. 規約への適合

- 1 ファイル 1 型、ファイル名＝型名（`IOutputTypeState.cs`、`OutputTypeState.cs`、偽実装も別ファイル）。
- 配置: R1（インターフェースは `DS4Windows/DI/`）、R2（実装は `DS4Windows/DS4Control/Services/`）。名前空間は新規の契約が `DS4Windows.DI`、実装が `DS4Windows.Services`（R6）。
- ログ文言・レベルは変えない（§2.3）。機能は落とさない（§2.2）。
- 使わなくなる `OutputSlotService` のコメント（`Func` の理由）は削除する。

## 8. 検証

- 各サブステップで `Debug ALL`（警告 0／エラー 0、全テスト合格）。
- 実機の確認（Phase 末の 1 回）: 仮想スロットの抜き差し（出力スロット管理画面）、コントローラーの接続・切断、プロファイル適用時の出力種別の切替（X360⇄DS4）、`ActiveOutDevType` を参照する IPC。

## 9. 決定事項（承認をお願いします）

1. **方針**: 配列の持ち主を新設の `IOutputTypeState`（`OutputTypeState`）へ移し、`Func<IOutputSlotService>`（5 件）・`Func<IVirtualPadService>`（1 件）・`Func<OutputSlotManager>`（1 件）を撤廃する（§3）。名前は `IOutputTypeState`／`OutputTypeState`（別案: `IOutputDeviceTypeState`）。
2. **`IOutputSlotService` の公開 API は変えない**（`OutDevTypeTemp`／`ActiveOutDevType` はホルダーへ転送。UI・IPC の利用者は変更しない）。
3. **`BackingStore.ActiveOutDevType` の配線**は `IOutputTypeState` の登録ラムダへ移す（R1・R2 の検証つき）。
4. **`ControlService`** は `IOutputSlotService` を直接受ける（引数の数は 26 のまま）。
5. **実装の分割**: 7-a〜7-d を別コミットにする（§4）。
6. **A/B 計測**: 0 アロケーションの特性テストと `DS4W_RUN_BENCH=1` の計測を確認として行うが、合否のゲートにはしない（悪化は想定していないため）。
7. **偽 `IVirtualPadService`** をテスト側に用意する（`OutputSlotManager` を差し替えるテストのため）。
