# Phase9 9-E E-6（ライフサイクル：接続・切断・開始・停止、デバイス種類ごとの部品化）計画書

作成日: 2026-10-22（改訂: ユーザーの方針「全部切り出す。実機がなくても。カートリッジのように入れ替えられる形にする」を反映）
状態: **承認済み（2026-10-22。§8）。E-6a 完了（ビルド・テスト・実機確認 OK）。E-6b 以降は段階ごとに進める**
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
| E-6a プロファイル選択（`IConnectionProfileSelector`） | **完了（2026-10-22）**。ビルド・テスト合格、実機確認 OK |
| E-6b 設定の反映（`IDeviceConnectionSetup`） | **完了（2026-10-22）**。ビルド・テスト合格、実機確認 OK（P9E6b-1・6。タッチパッド・DualSense・起動プログラムは機材／設定なしで未実施） |
| E-6c 種類のカートリッジ（`IInputDeviceModule` ×5 と `Registry`） | **完了（2026-10-22）**。ビルド・テスト合格、実機確認 OK（P9E6c-1。DualSense／Edge／Joy-Con／Switch Pro の固有部分は機材なしで未実施、差分テストで担保） |
| E-6d Joy-Con の結合（`IJoyConLinker`、`IJoinableDevice`） | **完了（2026-10-22）**。ビルド・テスト合格（偽物のデバイスの差分テスト約 3.3 万通り含む）。実機はなし（台帳 §41 は Joy-Con が手に入ったとき） |
| E-6e 切断・同期（`IDeviceDisconnectHandler`、`IDeviceSlotHost`） | **実装済み（2026-10-22）**。ビルド・テストはユーザーの実行待ち |
| E-6f 開始・停止・ホットプラグ | 未 |

### E-6a の実装メモ
- 計画（§3.3）では「適用するプロファイル名を返し、`ApplyToSlot` は `ControlService` に残す」としていたが、`MarkConnected`／`SelectedProfile`／`RememberProfile`／Linked の UI 状態／`ApplyToSlot` が**順序つきで連続する一続きの処理**だったため、順序を変えないよう、**`ApplyToSlot` と `PublishSlot` も含めて**まとめて移した。`ControlService` は、適用の成否（`profileLoaded`）を受け取って、仮想パッドの接続以降を続ける。
- `DS4Device` は作れないため、窓口は `isValidSerial` と `macAddress`（有効なシリアルのときだけ `getMacAddress()` の値）をプリミティブで受ける。
- テスト: `Phase9E6ConnectionProfileSelectorTests`（偽物の `IProfileSlotApplier` で、一時プロファイル、初回接続、再接続、シリアルなし、戻り値を固定）、`Phase9E6ConnectionGuardTests`（移した文字列が `ControlService` に無いこと、呼び出し順序、DI 登録）。

### E-6b の実装メモ
- 移したもの: `CheckProfileOptions`（`ApplyProfileOptions`）、`CheckLauchProfileOption`（private）、`SetupInitialHookEvents`（`WireInitialHooks`）、`TouchPadOn`（`AttachTouchpad`）。`ControlService` の `CheckProfileOptions`（BackingStore が使う）と `TouchPadOn` は薄い委譲として残した。
- `touchPad[ind]` は、`ApplyProfileOptions` では呼び出し時の値を引数で渡す（旧コードも同じ時点で配列から読んでいた）。`AttachTouchpad` は、旧 `TouchPadOn` の `PreTouchProcess` のラムダが**発火のたびに配列から引き直していた**ため、配列そのものを渡す（切断後に null になっても、旧コードと同じ挙動）。
- DualSense の振動設定の枝は、そのまま `ApplyProfileOptions` に入っている。E-6c で種類ごとの部品へ移す（TODO をクラスに明記）。
- 既存の `DebouncerSetDurationTests` の走査先を `DeviceConnectionSetup.ApplyProfileOptions` へ更新した。
- テスト: `Phase9E6ConnectionGuardTests` に追加（移した文字列の不在、薄い委譲、呼び出し順序、DI 登録、イベント発火時の引き直し）。`DS4Device`／`Mouse` が作れないため、値の差分テストは書けず、ソースの走査と実機確認（台帳 §39）で担保する。

### E-6c の実装メモ
- 契約 `IInputDeviceModule`（`Handles`／`IsEnabled`／`GetKnownExtraButtons`／`OnDeviceInit`／`HookSettings`／`ApplyProfileOptions`）と `IInputDeviceModuleRegistry.Resolve(type)`。実装は `Ds4DeviceModule`／`DualSenseDeviceModule`／`SwitchProDeviceModule`／`JoyConDeviceModule`／`Ds3DeviceModule`、共通の土台 `InputDeviceModuleBase`、扱う部品がない種類のための `NullInputDeviceModule`（旧コードの `default:` と同じ）、引き当ての `InputDeviceModuleRegistry`。新しい種類は、`IInputDeviceModule` を実装して `ServiceRegistration` へ 1 行足すだけ。
- `ControlService` は、`PostDS4DeviceInit`／`PrepareDS4DeviceSettingHooks`／`CheckForSupportedDevice`／`GetKnownExtraButtons` を 1 行の委譲にした。`DeviceConnectionSetup.ApplyProfileOptions` の DualSense の枝は、`_deviceModules.Resolve(device.DeviceType).ApplyProfileOptions(...)` に置き換えた（位置は元のまま）。
- `activeControllers` は、旧コードと同じく LED モード変更の発火のたびに読む（`Func<int>`）。
- Joy-Con の結合の初期値は、デバイスなしで確かめられる純粋な関数 `JoyConDeviceModule.DecideJoin` に分けた。`Phase9E6DeviceModuleTests` が、旧 `PostDS4DeviceInit` の逐語コピーと、全ての種類×結合モード×ジャイロ提供側で突き合わせる。有効設定も、旧 `CheckForSupportedDevice` の逐語コピーと、全ての種類×フラグの組み合わせで突き合わせる（実機のない種類［Joy-Con、Switch Pro、DS3］もここで担保）。
- `Start`／`HotPlug` の中の Joy-Con の結合の手順（`InputDeviceType.JoyConL`／`JoyConR` の判定）は、E-6d で `JoyConLinker` へ移す。ガードテストはその 2 つだけを例外として許している。

### E-6d の実装メモ
- 契約: `IJoinableDevice`（結合が触る最小のメンバー: 種類、`PerformStateMerge`、`PrimaryDevice`、`JointDeviceSlotNumber`、`SetJointDevice`、`CopyJointStateFrom`、`OnRemoval`）と `IJoyConLinker`（`LinkOnStart`／`FindPendingForHotPlug`／`LinkOnHotPlug`）。実装は `JoyConLinker`（状態なし）と、実機の `DS4Device` を包む `JoinableDeviceAdapter`。
- `Start` の手順と `HotPlug` の手順は、統合せずに別のメソッドにした（挙動を変えないため）。3 か所で同じだった「相互に結合相手を設定 → `JointState` を相手のものに → 取り外しで相手の結合を解除する処理を両方に足す」は、private の `Join` にまとめた（順序はそのまま）。保留中の主／副は、これまでどおり呼び出し側のループの局所変数（`ref`／戻り値）で持つ。
- 旧コードの `device as JoyConDevice` の型の絞り込みは、アダプターの `SetJointDevice` の中に移した。Joy-Con の種類（L／R）のデバイスは必ず `JoyConDevice` なので、結果は同じ。
- テスト: `Phase9E6JoyConLinkerTests` が、旧 `Start`／`HotPlug` の逐語コピーと、**偽物のデバイスで** 全ての小さな組み合わせ（種類 4 × 結合の可否 × 主／副 × 結合済みか＝32 通りのデバイスを 1〜3 台、2 つの結合モード）を突き合わせる。結合後の相手・状態と、各デバイスを取り外した後の状態まで同じことを確かめる（実機の Joy-Con はなくても担保）。ガードテストで、`ControlService` に `JoyConDevice`／`.JointDevice`／`.JointState` が残っていないこと、呼び出しの順序（準備 → 結合 → 割り当て）を検査する。

### E-6e の実装メモ
- 契約: `IDeviceDisconnectHandler`（`OnRemoval`／`OnSyncChange`／`OnSerialChange`／`DebugMessage`）と、スロットごとの状態への窓口 `IDeviceSlotHost`（`DS4Controllers`／`CurrentStates`／`PreviousStates`／`TouchPads`／`SlotManager`）。実装は `DeviceDisconnectHandler`（Singleton、状態なし）。`ControlService` が `IDeviceSlotHost` を明示実装し（配列の所有者のまま）、`Func<IDeviceSlotHost>` で遅延して渡す（E-3・E-4 と同じ形。構築の循環なし）。
- 旧 3 メソッドを逐語で移した。配列の参照は `host.XXX` に、`outputDevices`／`UnplugOutDev`／`PluginOutDev` は `IVirtualPadService` 経由に、`ActiveOutDevType` は `IOutputSlotService` の同じ配列に置き換えただけ。`OnDeviceSerialChange(this, ...)` の送り主は、同じ `ControlService` のインスタンス（`IDeviceSlotHost` の実体）を渡す。
- `ControlService` の `On_DS4Removal`／`On_SyncChange`／`On_SerialChange`（protected）は、イベントの購読先として、1 行の委譲で残した。購読の登録順は変えていない。
- 既存テスト `Phase8Step6W1DisconnectCharacterizationTests`／`Phase8Step6W3SlotLifecycleTests` の走査先を `DeviceDisconnectHandler.OnRemoval` へ更新した。
- テスト: `Phase9E6DisconnectGuardTests`（移した呼び出しの不在、薄い委譲、購読の登録順、**切断の後始末の順序**［仮想パッドの取り外し → コミット → エンジンの切断 → スロットの解放 → 通知 → 状態の初期化 → スロット管理の解除 → OSC → 遅延監視のリセット］、DI 登録とホスト）。`DS4Device` が作れないため、値の差分テストは書けず、順序のソース走査と実機確認（台帳 §42）で担保する。
