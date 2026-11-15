# Phase40 計画: 第 3 層が使う入力機種の契約（C1＝A）

作成日: 2026-11-15
状態: **40-0（`ITouchpadBehaviour` の削除）完了。40-1 以降は未着手（承認済みの方針 A。実機確認とあわせて行う）**
決定（2026-11-15。ユーザー）: C1＝A（用途別の小さな契約に分ける）。`ITouchpadBehaviour` は削除（実施済み）。

## 1. 背景
`Output`（第 3 層）の 5 つの部品（`UdpMotionService`、`VirtualPadService`、`DeviceRumbleService`、`HidHideService`、各カートリッジの `AttachFeedback`）が、第 1 層の具象 `InputDeviceCore` を直接使う。使うメンバーは約 20 種で、用途が混ざる。

## 2. 契約の案（置き場所は `Core/Abstractions`。第 1 層が実装し、第 3 層が使うため。R2）
| 契約 | メンバー | 主な使い手 |
|---|---|---|
| `IRumbleTarget` | `setRumble(right, left)` | `DeviceRumbleService`、DS4 カートリッジ |
| `ILightbarTarget` | `LightBarColor`、`GetLightbarStateRef()`、`SetLightbarState(ref)`、`TriggerModeProvider` | DS4 カートリッジ |
| `IMotionSource` | `Report` の購読・解除、`MotionEvent`、`queueEvent` | `UdpMotionService` |
| `IDeviceIdentity` | `DisplayName`、`MacAddress`、`GetDeviceType()`、`DevicePath`（`HidDevice` の代わり） | `VirtualPadService`、`HidHideService` |
| `IDeviceSlotLifecycle` | `ResetState()`、`RemoveFeedbacks()`、`CurrentExclusiveStatus` | `VirtualPadService`、`HidHideService` |

## 3. 手順
1. 40-1: 契約と、必要な値の型（`DS4Color`、`DeviceExclusiveStatus`、`ReportHandler` の置き場所）を `Core` へ置く。`InputDeviceCore` が実装する（動作は不変）。
2. 40-2: `IInputDeviceRegistry.GetControllers()` など、第 2 層の契約の戻り値の型を、必要な契約へ絞る（影響範囲を先に調べる）。
3. 40-3: `Output` の 5 つの部品を契約経由へ切り替える（1 部品 1 コミット）。
4. 40-4: `LayerDependencyRuleTests` に「`Output` は `InputDeviceCore` を参照しない」規則を足し、規則書（1 章、3.4 R2）と図を更新する。
5. 40-5: 実機確認（UDP の動き、DS4 のライトバーと振動、仮想パッドの接続・切断、HidHide）。

## 4. 注意
- `Report` のイベントと `MotionEvent` は、入力のスレッドへ `queueEvent` で渡す仕組みと結びつく。契約にするときは、購読・解除の順序とスレッドを変えない。
- 影響が `Output` 全体と登録簿に及ぶため、実機確認を前提に、独立した作業として行う。
