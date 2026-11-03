# Phase11 電池残量の一本化（案C）個別計画書

作成日: 2026-11-03
状態: **実装済み（2026-11-03）。実機確認待ち**（方針＝案C・DS4 のみ実機確認・4.2.4 に含める、はユーザー決定済み）。
実装結果: Step 1〜4 を実施。Step 0 の診断ログは追加せず、(a) の競合は防御的に解消した（Hotplug 時に前のデバイスの行を置き換え、Removal は新しい行を消さない）。ライトバー等は直読みのまま。`cState.Battery` は各機種のループで従来どおり設定（書き込み 1 か所化は、各機種のループ構造を変えないため見送り）。テスト: `Phase11BatteryUnificationTests`（5 件）。`Debug ALL`: 警告 0・エラー 0、Actions 2,837＋Standalone 11 合格。実機確認項目は台帳 S9-18〜20。
きっかけ: S9-17 の確認で、(a) 切断→再接続後に Controllers 一覧が前の残量のまま、(b) プロファイル適用の独自通知ウィンドウが 0% と表示された（ログは 37% で正しい）。

---

## 1. 調査結果（現状）

### 1.1 書き手（デバイスごとにばらばら）
| 機種 | 更新箇所 | イベント | 備考 |
|---|---|---|---|
| DS4 | `DS4Device` 読み取りループ | `BatteryChanged`（基底の virtual event） | 接続直後は `PrimeBatteryReading()`（イベントなし） |
| DualSense／DS3／Switch Pro／Joy-Con | 各クラスの読み取りループ | **各クラスが `override event` を再宣言**（基底とは別の購読リスト） | Prime なし。`cState.Battery` も各自が設定 |

- いずれも「値が変わったときだけ」通知する。接続直後の最初の値は、通知されない（DS4 以外）か、Prime で通知なしに入る（DS4）。
- `cState.Battery`（`DS4State`）は、読み取りループが別途コピーする。切断時に `DeviceDisconnectHandler`／`ServiceLifecycle` が 0 へ戻す。

### 1.2 読み手（8 経路）
| 読み手 | 場所 | 読み方 |
|---|---|---|
| Controllers 一覧 | `CompositeDeviceModel` | `device.Battery`＋`BatteryChanged`／`ChargingChanged`＋60 秒タイマー |
| プロファイル適用ログ | `ProfileApplicationService`、`AutoProfileService.BatteryText` | `DS4Device.Battery` 直読み |
| **独自通知ウィンドウ** | `MainWindow.OnProfileChanged`（l.797–803） | **一覧の `ControllerDict[slot]` 経由**。なければ `"0"` |
| ホットキー／通知（別経路） | `MainWindow.xaml.cs` l.395、l.726 | 一覧の `item.Device.Battery` 経由 |
| IPC | `MainWindow.Ipc.cs` l.195、227、307 | `devitem.Device.Battery`（一覧経由）と `DS4Controllers[i].Battery`（直）が混在 |
| トレイ | `TrayIconViewModel`、`ControlService` l.836 | デバイスのイベント＋`cState.Battery` |
| ライトバー・低残量 | `DS4LightBar`、`DeviceDisconnectHandler` l.111 | `getBattery()` 直読み |
| Pad メタ・表示文字列 | `ControlService` l.258／727 | `getBattery()` 直読み |
| Readings／OSC／仮想 DS4 出力 | `ControllerReadingsControl`、`OscService`、`DS4OutDeviceExt` | `cState.Battery`（コピー） |

### 1.3 症状の原因
- **(b) 0%（確定）**: 通知ウィンドウだけが UI の一覧モデルを経由して読む。再接続直後はそのスロットのモデルがまだ無く（`Dispatcher.BeginInvoke` で遅れて走るため）、`"0"` になる。
- **(a) 古い値（推定。Step 0 で確認）**: `Service_HotplugController` は `!controllerDict.ContainsKey(index)` のときだけ行を作る。切断側の `Controller_Removal` より先に再接続の通知が来ると、新しい行が作られず、古いデバイスの行が残る。

---

## 2. 目標構造（案C）

> **電池残量の真実は `DS4Device`（基底）の `Battery`／`Charging` の 1 か所。書き手の入口も 1 か所。読み手は全員、スロットから引いたデバイスを直接読む。UI のモデルは経由しない。**

1. **書き手の一本化**: `DS4Device` 基底に `protected void UpdateBattery(int percent)`／`UpdateCharging(bool)` を置く。値が変わったときだけ、基底の（非 virtual の）`BatteryChanged`／`ChargingChanged` を発火する。各機種は、自分の読み取りの結果をこの 2 つに渡すだけにする。
2. **イベントの一本化**: 各機種の `override event` の再宣言を廃止し、基底の 1 組だけにする（購読リストが別になる構造を解消）。
3. **接続直後の初期値**: `PrimeBatteryReading` を基底の仮想メソッド化（`ReadBatteryOnce()` 相当）し、DS4 は現状どおり、他機種は「未対応。TODO」とする（実機が無いため、動作を変えない）。
4. **読み手の一本化**: 通知ウィンドウ、`MainWindow` l.395／726、IPC を、`controlService.DS4Controllers[slot]`（デバイス）の直読みへ切り替える。一覧モデルからの読み出しをやめる。
5. **一覧の行の作り直し**: 再接続で、古いデバイスの行が残る競合を解消する（Step 0 の結果次第で方法を決める）。
6. **`cState.Battery`**: パイプライン（OSC、仮想 DS4 の出力レポート、Readings）が使うデータなので、**フィールドは残す**。書き込みは、読み取りループの 1 か所（`UpdateBattery` を呼んだ直後に `cState.Battery = (byte)Battery` と書く形）に統一する。廃止は範囲外。

---

## 3. 範囲外・維持するもの
- ライトバーの点滅や色の判定（`getBattery()` 直読みのまま。毎フレーム呼ぶため）。
- ログの文言・レベル（規約 §2.3。Phase13b まで変更しない）。
- DualSense、DS3、Switch Pro、Joy-Con の実機確認（実機が無い。ユニットテストと、コードの形の一致で担保）。
- 60 秒ごとの一覧の読み直し（S9-17。残す。一本化後も安全弁として有効）。

---

## 4. Step 構成

| Step | 内容 | 検証 |
|---|---|---|
| 0 | **原因の確認**: 切断→再接続の順序（`Removal`、`Hotplug`、`Prime`、`OnProfileChanged`）を `[DI]` 付きの Debug ログで確認する。Release の一時ビルドで、(a)(b) を再現する | 実機（DS4）。ログから (a) の推定を確定または訂正 |
| 1 | 基底へ `UpdateBattery`／`UpdateCharging` を追加し、DS4 を切り替える。DS4 の既存の挙動（Prime、99% 固定、イベント）は不変 | ユニット（ソース形状）＋実機 |
| 2 | DualSense／DS3／Switch Pro／Joy-Con を `UpdateBattery`／`UpdateCharging` に切り替え、`override event` の再宣言を廃止 | ユニット（ソース形状）。実機なし（明記） |
| 3 | 読み手の切り替え（通知ウィンドウ、`MainWindow` l.395／726、IPC）。電池文字列の組み立てを 1 つの関数にまとめる（`AutoProfileService.BatteryText` を基にする） | ユニット＋実機（(b) の解消） |
| 4 | 一覧の行の作り直し（(a) の解消）。Step 0 の結果に従う | ユニット＋実機（(a) の解消） |
| 5 | `cState.Battery` の書き込み 1 か所化。文書更新（`Phase11-Completion-Report`、`Phase11-Status`、台帳 S9-17 と新項目、モデル図 03 の電池の記述）、`Debug ALL`、Release ビルド | `Debug ALL` 警告 0・エラー 0 |

### 実機確認項目（台帳 S9 に追加。DS4 のみ）
- S9-18: 切断→再接続後、一覧の残量が、ログの残量と一致する。
- S9-19: 再接続時の独自通知ウィンドウの残量が、ログと一致する（0% にならない）。
- S9-17: 接続したままで、60 秒以内に一覧が実際の残量へ追従する。
- 回帰: USB 接続／Bluetooth 接続で残量と「+」（充電中）が出る。ライトバーの残量連動色・低残量の点滅が従来どおり。

---

## 5. リスクと対策
| リスク | 対策 |
|---|---|
| 他機種の挙動の変化（実機が無い） | 値の計算式は一切変えず、「書き込み＋通知」の呼び出しだけを `UpdateBattery` に置き換える。置換前後のソース形状をテストで固定する |
| イベントの再宣言を廃止したことで、購読者が受け取れなくなる | 基底のイベント 1 組だけにすれば、すべての購読者（一覧、トレイ、`DeviceConnector`）は同じリストに載る。購読箇所は、`BatteryChanged`／`ChargingChanged` の 6 か所のみで、型は `DS4Device` |
| 初回通知の有無の差 | 接続直後は通知しない（従来どおり）。変化があったときだけ通知 |
| 範囲の膨張 | `cState.Battery` の廃止とログの整理は含めない |

## 6. リリースの扱い
4.2.4 に含めるか 4.2.5 に回すかは、ユーザーの判断（未決）。

## 7. 規約との関係
- §2.2（機能維持）: 表示や判定の結果は変えない。変えるのは、読み出しの経路と、通知の取りこぼしの解消のみ。
- §2.3（ログ）: 変更なし。Step 0 の診断ログは `[DI]` 付きの Debug とし、Step 5 で残す／削除するかを決める。
- §3.3（1 ファイル 1 型）: 新しい型は作らない（基底のメソッド追加のみ）。ラチェットの基準値は変えない。
