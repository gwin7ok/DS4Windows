# Phase13-Step10 個別計画書: 転送メンバーの削除と引数の整理

作成日: 2026-11-04
対象ブランチ: `Phase13`
上位計画: `Phase13-Plan.md` §4 Step10（Q6: 努力目標 `ControlService` の引数 20 以下）
状態: **承認待ち**（§9）

---

## 1. 目的

`ControlService` に残る「転送だけのメンバー」を、利用者が本物のサービスを直接使う形に替えて削除し、`ControlService` のコンストラクター引数（現在 26）を減らす。
`MainWindow`（現在 20 引数）は、これ以上増やさない。

## 2. 調査結果（2026-11-04。ソースの検索による。テストを除く）

### 2.1 利用者が 0 件の転送（削除してよいもの）

`BackingStore` と `LegacyProfileXmlSerializer` が呼ぶ `control.PluginOutDev(device, tempDev)` などは、引数の並びから `IProfileLoadTarget`（`ProfileLoadTarget`）の呼び出しであり、`ControlService` のメンバーではない。

| メンバー | 外部の利用者 | 備考 |
|---|---|---|
| `PluginOutDev`／`UnplugOutDev` | 0（`IProfileLoadTarget`・`IVirtualPadService` が別に持つ） | テストのみ |
| `CheckProfileOptions`／`TouchPadOn`／`StartTPOff` | 0（同上） | テストのみ |
| `CheckForTouchToggle`（protected） | 0 | |

### 2.2 利用者が少数の転送（利用者を直接利用へ切り替えるもの）

| メンバー | 実体 | 利用者 |
|---|---|---|
| `ChangeOSCListenerStatus`／`ChangeOSCSenderStatus` | `IOscService` | `MainWindow` 2 |
| `ChangeUDPStatus`／`ChangeMotionEventStatus` | `IUdpMotionService` | `MainWindow` 4 |
| `CheckHidHidePresence`／`UpdateHidHiddenAttributes` | `IHidHideService` | `App` 1、`MainWindow` 1、`AutoProfilesViewModel` 1 |
| `TouchpadSlide` | `ITouchpadToggleService.Slide`（状態配列を渡す） | `MainWindow` 1 |
| `GetActiveInputControl` | `IActiveInputProbe.GetActive`（状態配列を渡す） | `ProfileEditor` 1 |
| `setRumble` | `IDeviceRumbleService.SetDevRumble`（スロット上限と有無の検査つき） | `ProfileEditor` 2 |

### 2.3 残す転送（本 Step の対象外）

`Start`／`Stop`／`HotPlug`／`PrepareAbort`（利用者が `MainWindow` 約 12、`App` 数か所と多く、`ControlService` の公開イベントの送り主を保つ必要がある）、`GetController`、`OutputslotMan`、`DS4Controllers` などの状態、`LogDebug`、`getDS4State`／`getDS4StateTemp`、`MappingService`。

### 2.4 コンストラクター引数の内訳（26）

- 2.1・2.2 の移行で**使い道がなくなる**: `oscService`、`activeInputProbe`、`rumble`、`environmentService`（`_controllerSlotLimit` は `setRumble` だけが使う）。
- 移行後は **`DebugMessage` を `ControlService.Debug` へ中継するためだけ**に残る: `hidHideService`、`udpMotionService`、`touchpadToggle`、`deviceConnectionSetup`、`deviceDisconnect`、`latencyMonitor`（6 つ）。
- よって、転送の削除だけで 26 → 22。20 以下にするには、中継の 6 つを 1 か所にまとめる必要がある（§3 の案 C）。

## 3. 設計案

| 案 | 内容 | 引数の見込み |
|---|---|---|
| A | 2.1 の削除のみ（利用者 0 件） | 26（変わらない） |
| B | A ＋ 2.2 の切り替え | 22 |
| **C** | B ＋ ログの中継を `IServiceDebugRelay`（新設）にまとめる。中継の 6 つを `ControlService` ではなくこのサービスが受け、`ControlService.Debug` へ流す（`LogDebug`／`GUI` への経路・文言は変えない） | **17〜18** |

`MainWindow` の引数: 2.2 を直接受けると +4（`IOscService`、`IUdpMotionService`、`IHidHideService`、`ITouchpadToggleService`）で 24 になる。
そこで UI 側の窓口 `IRuntimeFeatureSwitch`（新設。OSC／UDP／HidHide／タッチパッドの切り替えを 1 か所に集める。実体は既存のサービスへ委譲するだけ）にまとめれば +1 で 21（20 に届かない分は、記録して閉じる）。

## 4. 実装の分割（各サブステップを別コミット。各回 `Debug ALL`）

| サブ | 内容 |
|---|---|
| 10-a | 本計画書 |
| 10-b | 2.1 の削除（`PluginOutDev`／`UnplugOutDev`／`CheckProfileOptions`／`TouchPadOn`／`StartTPOff`／`CheckForTouchToggle`）。テストの呼び出しは、実体のサービスを直接呼ぶ形に替える |
| 10-c | HidHide・OSC・UDP・タッチパッドの切り替えの利用者（`App`、`MainWindow`、`AutoProfilesViewModel`）を切り替え、`ControlService` の転送を削除 |
| 10-d | `GetActiveInputControl`／`setRumble` の利用者（`ProfileEditor`）を切り替え、転送と `activeInputProbe`／`rumble`／`environmentService` の引数を削除 |
| 10-e | （案 C の場合）`IServiceDebugRelay` の新設と、6 つの引数の置き換え。ログの経路が変わらないことを、ログの出力順と文言を固定するテストで確認 |
| 10-f | ガード（`ControlService` の公開メンバーに該当の転送が 0 件、引数の数の上限）、結果の記録 |

## 5. 影響範囲

HidHide の登録、OSC／UDP モーションの切り替え、タッチパッドの表示、プロファイル編集の「入力の取得」と振動の停止。ログの文言・レベルは変えない。

## 6. リスクと対策

| リスク | 対策 |
|---|---|
| ログの中継を替えると、GUI・ログファイルの出力が変わる | 案 C は 10-e に分け、各サービスの `DebugMessage` が従来どおり `ControlService.Debug` に届くことをテストで固定。`LogDebug` の本体は変えない |
| `ProfileEditor` が受ける状態（`CurrentState`／`touchPad`）の渡し方 | 既存の `IDeviceSlotState` を使う。`ProfileEditor` の引数が増える場合は、そこも記録 |
| テスト（`ControlServiceTestFactory` ほか）の大量変更 | 生成は 1 か所に集まっているため、そこで吸収 |

## 7. 規約への適合

新設は最大 2 型（`IServiceDebugRelay`／`ServiceDebugRelay`、`IRuntimeFeatureSwitch`／`RuntimeFeatureSwitch`）。1 ファイル 1 型、配置は R1・R2（インターフェースは `DS4Windows/DI/`、実装は `DS4Windows/DS4Control/Services/`）。ラチェットの件数は変えない。

## 8. 検証

`Debug ALL`（警告 0、エラー 0、全テスト合格）。実機確認（最後にまとめて）: OSC／UDP の切り替え、HidHide の登録、タッチパッドの表示、プロファイル編集の入力取得と振動の停止、ログの表示。

## 9. 決定事項（承認をお願いします）

1. **範囲**: 案 A／B／C のどれにするか（推奨: C）。
2. **`MainWindow` の渡し方**: 4 つを直接受ける（24 引数）か、`IRuntimeFeatureSwitch` にまとめる（21 引数）か（推奨: まとめる）。
3. **分割**: 6 コミット（10-a〜10-f）で、以後は自動進行。
