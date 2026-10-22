# Phase9 9-E E-6（ライフサイクル：接続・切断・開始・停止、デバイス種類ごとの部品化）計画書

作成日: 2026-10-22（改訂: ユーザーの方針「全部切り出す。実機がなくても。カートリッジのように入れ替えられる形にする」を反映）
状態: **承認済み（2026-10-22。§8）。E-6a 実装済み（ビルド・テストはユーザーの実行待ち）。E-6b 以降は段階ごとに進める**
上位: `Phase9-E-Plan.md`（E-1〜E-5 は完了）、`Phase9-Responsibility-Split-Plan.md`

## 1. 目的と現状

`ControlService.cs` は E-5 の後で約 2,015 行。残る塊の大半は、コントローラーの「つなぐ・外す・サービスを始める／止める」の流れ（ライフサイクル）と、**デバイスの種類（DS4／DualSense／Switch Pro／Joy-Con／DS3）ごとの分岐**。

目的は 2 つ。
1. ライフサイクルを `ControlService` から切り出し、`ControlService` を窓口（ファサード）にする。
2. デバイスの種類ごとの処理を、**種類ごとの部品（カートリッジ）**にまとめる。新しい種類の追加・差し替えが、`ControlService` を触らずにできるようにする（Pure DI 化の重要な一歩）。

ここは、ガードレール（`Phase5-Plan.md` §5: ドライバ破棄順序、切断時クリーンアップ、スレッド直列化）に直接かかわる。**1 回で全部は実装せず、段階（E-6a〜E-6f）に分けて、段階ごとにビルド・テスト（と、できる範囲の実機確認）を挟む。**

## 2. 現状の塊（`ControlService.cs`）

| 区分 | メソッド | 行数 | 触る状態 | 備考 |
|---|---|---|---|---|
| d. 接続時のプロファイル選択 | `PrepareConnectedInputControllerSettingEvents` の前半（初回／再接続、Linked、適用） | 約 70 | `IDeviceStateService`、`IProfileRepository`、`IProfileSlotApplier` | 状態なし。テストしやすい |
| e. 接続時の設定の反映 | `CheckProfileOptions`、`CheckLauchProfileOption`、`SetupInitialHookEvents`、`TouchPadOn` | 約 190 | `touchPad[]`、デバイス、プロファイル設定 | 引数で受ければ状態なし |
| **種類ごとの分岐** | `PostDS4DeviceInit`（Joy-Con の結合の初期値）、`PrepareDS4DeviceSettingHooks`（DualSense の LED）、`CheckForSupportedDevice`（種類ごとの有効設定）、`GetKnownExtraButtons`（種類ごとの追加ボタン）、`CheckProfileOptions` の DualSense の振動設定 | 約 120 | `deviceOptions`、デバイス | **カートリッジ化の対象（E-6c）** |
| **Joy-Con の結合** | `Start` と `HotPlug` の中の、L と R を 1 つに結合する手順（2 通り） | 約 100 | `JoyConDevice` の `JointDevice`／`JointState`／`Removal` | **カートリッジ化の対象（E-6d）。実機なし** |
| f. 切断・同期の処理 | `On_DS4Removal`、`On_SyncChange`、`On_SerialChange` | 約 150 | `CurrentState`／`PreviousState`／`DS4Controllers`／`touchPad`／`outputDevices`／`slotManager` | ガードレール（切断時クリーンアップ） |
| a/b/c. 開始・停止・ホットプラグ | `Start`、`Stop`、`HotPlug` | 約 400 | `running`、`inServiceTask`、`loopControllers`、`activeControllers`、`suspending` ほか。イベント `ServiceStarted`／`PreServiceStop`／`ServiceStopped`／`RunningChanged`／`HotplugController` | ガードレール（ViGEm の開始・破棄順序、`inServiceTask` による直列化、`Thread.Sleep`） |

## 3. 設計

### 3.1 デバイス種類のカートリッジ（E-6c・E-6d）
契約 `IInputDeviceModule`（`DS4Windows/DI/`）。種類ごとに 1 つ実装し、すべて Singleton として登録する。`IInputDeviceModuleRegistry`（実装は `InputDeviceModuleRegistry`）が、`InputDeviceType` から部品を引く。

| メンバー | 旧コード | 内容 |
|---|---|---|
| `bool Handles(InputDeviceType type)` | （新設） | この部品が扱う種類 |
| `bool IsEnabled(ControlServiceDeviceOptions options)` | `CheckForSupportedDevice` の switch | 種類ごとの有効設定 |
| `IReadOnlyList<DS4Controls> GetKnownExtraButtons(DS4Device device)` | `GetKnownExtraButtons` の switch | 種類ごとの追加ボタン |
| `void OnDeviceInit(DS4Device device, ControlServiceDeviceOptions options)` | `PostDS4DeviceInit` | Joy-Con の結合の初期値（`PerformStateMerge`、`PrimaryDevice`、`OutputMapGyro`） |
| `void HookSettings(DS4Device device, Func<int> activeControllers)` | `PrepareDS4DeviceSettingHooks` | DualSense の LED モードの変更への反応 |
| `void ApplyProfileOptions(DS4Device device, int ind, IProfileSettingsService settings)` | `CheckProfileOptions` の DualSense の枝 | DualSense の振動エミュレーション、触覚の強さ |

実装は 5 つ（`Ds4DeviceModule`、`DualSenseDeviceModule`、`SwitchProDeviceModule`、`JoyConDeviceModule`、`Ds3DeviceModule`）。**既存の分岐を、部品ごとに逐語で移す**（挙動は変えない）。種類が見つからなければ、旧コードの `default:`（何もしない、`false`）と同じ結果にする。

### 3.2 Joy-Con の結合（E-6d）
`Start` の手順と `HotPlug` の手順は、似ているが違う（`Start` は先に来たものを主にして順に組む。`HotPlug` は主と副を先に探してから組む）。**2 通りの手順をそのまま 1 つのクラス `JoyConLinker` に移し、メソッドを 2 つ（`LinkOnStart`／`LinkOnHotPlug`）にする**（手順は統合しない。挙動を変えないため）。

**実機がなくても検証できるようにする**: 結合が触る最小のメンバーだけの窓口 `IJoinableDevice`（`DeviceType`、`PerformStateMerge`、`PrimaryDevice`、`JointDeviceSlotNumber`、`JointDevice` の設定、`JointState`、`Removal` イベント）を作り、`JoyConDevice` を包むアダプターを使う。テストでは偽物のデバイスで、旧コードの逐語コピーと結果を比べる（差分テスト）。

### 3.3 接続時の準備（E-6a・E-6b）
- E-6a `IConnectionProfileSelector`: 接続時のプロファイル選択（初回接続は Linked か OlderProfilePath、再接続は直前のプロファイル）。「適用するプロファイル名」を返す。`ApplyToSlot` の呼び出しは `ControlService` に残す（順序を変えない）。
- E-6b `IDeviceConnectionSetup`: `CheckProfileOptions`（種類ごとの部分を除く）、`CheckLauchProfileOption`、`SetupInitialHookEvents`、`TouchPadOn`。`touchPad[ind]` は引数で受ける。

### 3.4 切断・同期の処理（E-6e）
`IDeviceDisconnectHandler`（`On_DS4Removal`／`On_SyncChange`／`On_SerialChange`）。複数の配列に触るため、ホストの窓口 `IDeviceSlotHost`（`CurrentState`／`PreviousState`／`DS4Controllers`／`touchPad`／`outputDevices` の読み書き）を `ControlService` が実装し、遅延（`Func`）で受ける（E-3・E-4 と同じ形）。切断の後始末の順序は、逐語で保つ。

### 3.5 開始・停止・ホットプラグ（E-6f）
`IServiceLifecycle`（実装 `ServiceLifecycle`）に、`Start`／`Stop`／`HotPlug` の本体と、状態（`running`、`inServiceTask`、`loopControllers`、`activeControllers`、`suspending`）、イベント（`ServiceStarted`／`PreServiceStop`／`ServiceStopped`／`RunningChanged`／`HotplugController`）を移す。`ControlService` は薄い委譲を残し、状態とイベントは `ControlService` の公開メンバーのまま転送する（外部の呼び出し 19 件を変えない）。
- 守ること: `Start` の `Thread.Sleep(2000)`、UDP の待ちのループ、`Stop` の `ViGEm` の破棄順序（`OutputSlotManager.Stop` → `Thread.Sleep(DELAY_TIME)` → `StopViGEm`）、`inServiceTask` による直列化、イベントの発火の順序。
- 偽物のホスト（`IDeviceSlotHost`、`IVirtualPadService` など）で、`Start`／`Stop` の呼び出しの順序を記録して固定する差分テストを書く（旧コードの手順は、ソースの走査テストで固定する）。

## 4. 段階と順序

依存の順に、小さく安全なものから。**段階ごとに、ビルド・テスト実行（と該当する実機確認）を挟む。**

| 段階 | 内容 | 見込み | 実機確認 |
|---|---|---|---|
| E-6a | プロファイル選択（`IConnectionProfileSelector`） | 約 70 行減 | 初回／再接続のプロファイル |
| E-6b | 接続時の設定の反映（`IDeviceConnectionSetup`） | 約 190 行減 | 接続時の設定の反映 |
| E-6c | 種類のカートリッジ（`IInputDeviceModule` ×5 と `Registry`） | 約 120 行減 | DS4／DualSense（手元の種類）で、追加ボタン、LED、振動設定 |
| E-6d | Joy-Con の結合（`JoyConLinker`、`IJoinableDevice`） | 約 100 行減 | **実機なし。偽物のデバイスの差分テストで担保** |
| E-6e | 切断・同期の処理（`IDeviceDisconnectHandler`、`IDeviceSlotHost`） | 約 150 行減 | 切断・再接続を繰り返す |
| E-6f | 開始・停止・ホットプラグ（`IServiceLifecycle`） | 約 400 行減 | サービスの停止／開始の繰り返し、アプリの終了 |

合計で約 1,030 行減（2,015 → 約 985 行）の見込み。

## 5. 守ること（共通）

- 呼び出しの順序を変えない。特に `Start`／`Stop`／`PrepareConnectedInputControllerSettingEvents` の中。
- ログの文言とレベルを変えない（`AppLogger.LogDebug`、`LogDebug`、`LogToTray`。`[DI]` の接頭辞付きの既存ログもそのまま）。
- イベントの購読の登録順（`Removal`、`SyncChange`、`Report`）を変えない。
- 薄い委譲メソッドとイベント、公開フィールドは `ControlService` に残す（呼び出し側は 9-F まで変えない）。
- 逐語で移し、リファクタリング（手順の統合など）は同じ変更に混ぜない。

## 6. リスクと対策

| リスク | 対策 |
|---|---|
| Joy-Con 等、実機のない種類が壊れても気づけない | 窓口（`IJoinableDevice`）と偽物で差分テスト。旧コードの逐語コピーとの比較。ユーザーが実機を用意できたときの確認項目を台帳（§38 以降）に先に書いておく |
| `Start`／`Stop` の順序の変更 | 順序を記録する差分テストと、ソースの走査テスト（旧コードのメソッド名と呼び出し順） |
| 高頻度の経路（`On_Report`）への影響 | E-6 では `On_Report` を触らない。E-5 の部品はそのまま |
| 一度に大きく変えて原因が分からなくなる | 段階ごとにビルド・テスト・コミット。失敗は、その段階だけを直す |

## 7. 決めてほしいこと

- **Q1（全体の設計）**: §3 の「種類ごとのカートリッジ（`IInputDeviceModule` ＋ Registry）」と、E-6a〜E-6f の 6 段階で進めてよいか。
- **Q2（Joy-Con の検証）**: 実機がないため、`IJoinableDevice` の窓口と偽物による差分テストで担保する案（§3.2）でよいか。
- **Q3（`Start`／`Stop` の移し方）**: 本体と状態・イベントを `ServiceLifecycle` へ移し、`ControlService` は転送で残す案（§3.5）でよいか。
- **Q4（進め方）**: 段階ごとにビルド・テストを挟む（推奨）／E-6a〜E-6c をまとめてから挟む。

## 8. 決定事項（2026-10-22 承認）と進捗

- **Q1＝この設計で進める**（種類のカートリッジ化を含む 6 段階）。**Q2＝窓口＋偽物の差分テストで担保**。**Q3＝`ServiceLifecycle` へ移し、`ControlService` は転送で残す**。**Q4＝段階ごとにビルド・テストを挟む**。
- ユーザーの方針: 「全部切り出す。実機がなくても。カートリッジのように入れ替えられるようにしたい」。

| 段階 | 状態 |
|---|---|
| E-6a プロファイル選択（`IConnectionProfileSelector`） | **実装済み（2026-10-22）**。ビルド・テストはユーザーの実行待ち |
| E-6b 設定の反映 | 未 |
| E-6c 種類のカートリッジ | 未 |
| E-6d Joy-Con の結合 | 未 |
| E-6e 切断・同期 | 未 |
| E-6f 開始・停止・ホットプラグ | 未 |

### E-6a の実装メモ
- 計画（§3.3）では「適用するプロファイル名を返し、`ApplyToSlot` は `ControlService` に残す」としていたが、`MarkConnected`／`SelectedProfile`／`RememberProfile`／Linked の UI 状態／`ApplyToSlot` が**順序つきで連続する一続きの処理**だったため、順序を変えないよう、**`ApplyToSlot` と `PublishSlot` も含めて**まとめて移した。`ControlService` は、適用の成否（`profileLoaded`）を受け取って、仮想パッドの接続以降を続ける。
- `DS4Device` は作れないため、窓口は `isValidSerial` と `macAddress`（有効なシリアルのときだけ `getMacAddress()` の値）をプリミティブで受ける。
- テスト: `Phase9E6ConnectionProfileSelectorTests`（偽物の `IProfileSlotApplier` で、一時プロファイル、初回接続、再接続、シリアルなし、戻り値を固定）、`Phase9E6ConnectionGuardTests`（移した文字列が `ControlService` に無いこと、呼び出し順序、DI 登録）。
