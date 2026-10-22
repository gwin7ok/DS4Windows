# Phase9 9-E E-6（ライフサイクル：接続・切断・開始・停止）計画書

作成日: 2026-10-22
状態: **提案（承認待ち。§6 の質問に答えてから実装する）**
上位: `Phase9-E-Plan.md`（E-1〜E-5 は完了）、`Phase9-Responsibility-Split-Plan.md`

## 1. 目的と現状

`ControlService.cs` は E-5 の後で約 2,015 行。残る塊の大半は、コントローラーの「つなぐ・外す・サービスを始める／止める」の流れ（ライフサイクル）。
ここは、**ガードレール（`Phase5-Plan.md` §5）に直接かかわる**部分が多い（ドライバ破棄順序、切断時クリーンアップ、スレッド直列化）。E-1〜E-5 より慎重に進める。

## 2. 現状の塊（`ControlService.cs`）

| 区分 | メソッド | 行数の目安 | 触る状態 | 性質 |
|---|---|---|---|---|
| a. 開始 | `Start` | 約 155 | `running`、`loopControllers`、`inServiceTask`、`DS4Controllers` ほか | ガードレール（ViGEm の開始順、`Thread.Sleep(2000)`、UDP の待ち）。イベント `ServiceStarted`／`RunningChanged` |
| b. 停止 | `Stop` | 約 120 | 同上＋`outputDevices`、`slotManager` | ガードレール（**ドライバ破棄順序**、切断時クリーンアップ）。イベント `PreServiceStop`／`ServiceStopped` |
| c. ホットプラグ | `HotPlug` | 約 130 | `DS4Controllers`、Joy-Con の結合 | スレッド直列化（`inServiceTask`）。Joy-Con の結合ロジック約 50 行を含む |
| d. 接続時のプロファイル選択 | `PrepareConnectedInputControllerSettingEvents` のうち、プロファイルの選択と適用（初回／再接続） | 約 70 | `IDeviceStateService`、`IProfileRepository`、`IProfileSlotApplier` | 状態を持たない。**テストしやすい** |
| e. 接続時の設定の反映 | `CheckProfileOptions`、`CheckLauchProfileOption`、`SetupInitialHookEvents`、`TouchPadOn`、`GetKnownExtraButtons` | 約 220 | `touchPad[]`、デバイス、プロファイル設定 | ほぼ「デバイスと設定を引数に受けて反映する」だけ |
| f. 切断・同期の処理 | `On_DS4Removal`、`On_SyncChange`、`On_SerialChange` | 約 150 | `CurrentState`／`PreviousState`／`DS4Controllers`／`touchPad`／`outputDevices`／`slotManager` | ガードレール（切断時クリーンアップ）。複数の配列に触る |

## 3. 切り出しの候補

### 部品 1（推奨）: 接続時の準備（d＋e）
`IDeviceConnectionSetup` ／ `DeviceConnectionSetup`（Singleton、状態なし）に、次を移す。
- 接続時のプロファイル選択（初回接続は Linked プロファイルか OlderProfilePath、再接続は直前のプロファイル）。結果として「適用するプロファイル名」を返す部分を切り出し、`ApplyToSlot` の呼び出しは `ControlService` に残す（呼び出しの順序を変えない）。
- `CheckProfileOptions`、`CheckLauchProfileOption`、`SetupInitialHookEvents`、`GetKnownExtraButtons`、`TouchPadOn`。`touchPad[ind]` は引数で受ける。
- `ControlService` の同名の公開メソッド（`CheckProfileOptions`、`TouchPadOn`、`PreLoadReset` など、外部が使うもの）は薄い委譲として残す。
- 見込み: 約 290 行減（2,015 → 約 1,725 行）。
- テスト: プロファイル選択の分岐（初回／再接続／Linked の有無）を差分テストで固める（`IDeviceStateService` などは偽物で差し替え可）。デバイスを使う反映は、`DS4Device` が作れないため、ソースの走査と実機で担保する。

### 部品 2（任意）: 切断・同期の処理（f）
`IDeviceDisconnectHandler`。複数の配列（`CurrentState`／`PreviousState`／`DS4Controllers`／`touchPad`／`outputDevices`／`slotManager`）へ触るため、ホストの窓口（`IDeviceSlotHost`）を作って受ける必要がある。ガードレール（切断時クリーンアップ）に直結するので、実機で切断・再接続を繰り返して確かめる必要がある。見込み: 約 150 行減。

### 部品 3（今回は見送りを推奨）: Joy-Con の結合ロジック（c の一部と a の一部）
`Start` と `HotPlug` に、似ているが違う結合の手順がある。**Joy-Con の実機がない**ため、実機確認ができない。差分テストも `JoyConDevice` を作れず難しい。実機を用意できるときまで残す。

### 部品 4（今回は見送りを推奨）: `Start`／`Stop`／`HotPlug` 本体
ドライバ破棄順序、スレッドの直列化、イベントの発火の順序など、安全弁が集中している。分割の見返り（行数）に対して危険が大きい。`ControlService` は、サービスの開始・停止の窓口（ファサード）として残す。E-6 の後で、まだ必要なら別途検討する。

## 4. 守ること（共通）

- 呼び出しの順序を変えない（特に `Start`／`Stop` の中の順序、`PrepareConnectedInputControllerSettingEvents` の中の順序）。
- ログの文言とレベルを変えない（`AppLogger.LogDebug`、`LogDebug`、`LogToTray`）。
- `[DI]` の接頭辞が付いた既存のログは、そのまま維持する。
- 一部のイベント（`Removal`／`SyncChange`／`Report` の購読）の登録順を変えない。
- 薄い委譲メソッドは `ControlService` に残す（呼び出し側は 9-F まで変えない）。

## 5. 実機確認が要るもの（部品 1 の場合）

- 初回接続と再接続で、適用されるプロファイルが従来どおりか（Linked プロファイルあり／なし、再接続で直前のプロファイルに戻る）。
- 接続時に、ライトバー、トリガーの効果、振動の自動停止、アイドル切断、ポーリング間隔が、プロファイルの設定どおりに反映される。
- 「プロファイルでアプリを起動」（`LaunchProgram`）が動く。
- DualSense の振動エミュレーションの設定が反映される（DualSense がある場合）。

## 6. 決めてほしいこと

- **Q1（範囲）**: 部品 1 のみ（推奨）／部品 1＋2／部品 1＋2＋3／切り出さない。
- **Q2（進め方）**: 部品 1 を 1 回で実装（推奨）／d と e を分けて 2 回に分ける。
- **Q3（見送りの記録）**: 部品 3・4 は、見送りとして `Phase9-Responsibility-Split-Plan.md` に記録する（推奨）／今回の範囲に含める。
