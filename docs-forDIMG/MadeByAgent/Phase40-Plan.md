# Phase40 計画: 第 3 層が使う入力機種の契約（C1＝A）

作成日: 2026-11-15
状態: **40-0〜40-4 完了（2026-11-16）。40-5（実機確認）は、最後のまとめの実機確認で行う**
決定（2026-11-15。ユーザー）: Phase34 のあとに続けて行い、実機確認は最後にまとめて行う。C1＝A（用途別の小さな契約に分ける）。`ITouchpadBehaviour` は削除（実施済み）。

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

## 実施の記録
### 2026-11-16（ブランチ `Phase40-1`）
- 40-1: `DS4LightbarState` を `Input` から `Core/Model` へ移した（移動と名前空間は別コミット）。契約は 4 つ（`Core/Abstractions`）: `IRumbleTarget`（`setRumble`）、`IOutputBoundDevice`（振動に加え、`DisplayName`・`MacAddress`・`DevicePath`・`CurrentExclusiveStatus`・ライトバーの色と状態。仮想パッドの接続・取り外し・戻り・HidHide が使う）、`IPadStatusSource`（UDP のパッド情報の読み取り）、`IMotionSource`（`queueEvent` と、フレームごとの呼び出しの付け外し `StartMotionCallback`／`StopMotionCallback`／`PauseMotionCallback`／`ResumeMotionCallback`）。計画の案の `IDeviceSlotLifecycle` は、`ResetState`・`RemoveFeedbacks` が仮想パッド（`OutputDevice`）側のメンバーだったため不要だった。`InputDeviceCore` が実装する。
- 40-2（登録簿の戻り値の型を絞る）: 不要と判断した。`foreach (IMotionSource dev in devices)`・`IPadStatusSource d = _slots.Controllers[i]` のように、使う側の変数の型を契約にすれば、第 2 層の契約（`IInputDeviceRegistry`・`IDeviceSlotState`）の戻り値は `InputDeviceCore` のままで、`Output` が具象の名前を出さずに済む。
- 40-3: `IOutputDeviceCartridge.AttachFeedback`・`IVirtualPadService`・`IDeviceRumbleService`・`IHidHideService`・`IUdpMotionService` の引数と、各実装を契約へ切り替えた。UDP の付け外しの順序とスレッド（`queueEvent` で機器のスレッドへ積む）は変えていない。
- 40-4: `OutputInputDeviceBoundaryTests` が、`Output` が `InputDeviceCore` を参照しないことと、`InputDeviceCore` が 4 契約を実装することを検査する。規則書（copilot-instructions §3.1）へ 1 項目を追記した。
- 40-5（実機確認。最後のまとめで行う）: UDP（Cemuhook）の動き、DS4 のライトバーと振動、仮想パッドの接続・切断、HidHide。
