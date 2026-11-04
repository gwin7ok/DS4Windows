# Phase13b ログ変更の一覧（承認用。Step 13b-2）

作成日: 2026-11-04
根拠: `Log-Policy.md`、`Phase13b-Plan.md`、規約 §2.3
状態: **GUI・トレイに見える変更（§2）は 2026-11-04 に承認済み**。ファイルのみのログ（§3）は規則で扱い、各 Step の実施時に「レベルのみ／文言／削除」へ追記する。

## 1. 方針の前提（調査で分かったこと）

- GUI に出るログは、`AppLogger.LogToGui`（114）と、サービスの `DebugMessage` イベント → `ControlService.Debug` を経由する private `LogDebug`（約 40）の 2 経路。どちらも購読側（ログタブ、ステータス行、`LoggerHolder`）は同じ。13b-3 で後者を `AppLogger.Log(..., UserFacing)` に統一しても、見え方は変わらない。
- `Temporary`（一時表示）は、画面のステータスにだけ出て、**ファイルには書かれない**。
- `AutoProfileService` の `DEBUG: Auto-Profile…`（8 件）は、利用者が選ぶ `AutoProfileDebugLogLevel` で表示を制御している機能のため、**変更しない**。
- `LogToTray`（16）は通知として**変更しない**（D3）。
- GUI の文言は、**今回は変更しない**（`[DI]`、`WARNING:`、`ERROR` などの既存の語を含む）。リソース化済みの文言も同じ。

## 2. GUI・トレイに見える変更（承認が必要）

### G1: 失敗系の Warn → Error（レベルのみ。GUI の見た目は不変）

`LogToGui(msg, true)` のうち、処理の失敗・例外を表すもの。ファイルのレベルが Warn → Error になる。GUI は、警告色のまま（`Warning` は Warn 以上で真）。

| ファイル | 行 | 内容 |
|---|---|---|
| `Actions/DefaultProcessLauncher.cs` | 41 | Process launch failed |
| `Actions/TriggerDispatchEngine.cs` | 673 | MapCustomAction exception |
| `DS4Control/Mapping.cs` | 591, 963 | Synchronous Button SA handling error／MapCustomAction exception |
| `DS4Control/Services/AppSettingsService.cs` | 34 | Failed to save application settings |
| `DS4Control/Services/ProfileRepository.cs` | 102, 115, 124, 221, 257, 361, 397 | プロファイル保存・更新の失敗 |
| `DS4Control/Services/ProfileXmlStore.cs` | 42, 58, 77, 98 | XML の読み込み・保存の失敗 |
| `DS4Control/Services/SpecialActionRepository.cs` | 304 | Error reloading Actions.xml |
| `DS4Control/SyntheticInputState.cs` | 84 | Failed to reset runtime key state |
| `DS4Control/Util.cs` | 293 | タスクの例外 |
| `DS4Control/WindowPlacementHelper.cs` | 168, 201, 300, 309 | 配置の失敗 |
| `DS4Forms/ProfileEditor.xaml.cs` | 1128 | Failed to save profile |
| `DS4Forms/ViewModels/ControllerListViewModel.cs` | 182, 316, 325 | 例外 |
| `DS4Forms/ViewModels/ProfileSettingsViewModel.cs` | 3445 | Failed to open the web app |
| `DS4Forms/ViewModels/SettingsViewModel.cs` | 466 | Error in TaskService |
| `DS4Library/InputDevices/JoyConDevice.cs`／`SwitchProDevice.cs` | 599／312 | failed to initialize |
| `HidLibrary/HidDevice.cs` | 380 | Failed to generate runtime MAC address |

### G2: 異常なのに Info で出ているもの → Warn（GUI が警告表示になる。**見た目が変わる**）

`LogToGui(msg, false)` のうち、エラー・異常を表すもの。ファイルは Info → Warn、GUI は警告表示になる。

| ファイル | 行 | 内容 |
|---|---|---|
| `DS4Control/BackingStore.cs` | 932 | Profile contains an invalid special action |
| `DS4Control/Services/AppSettingsXmlSerializer.cs` | 467, 542 | Profiles.xml の欠落／IO エラー |
| `DS4Control/Services/ControllerConfigsXmlSerializer.cs` | 65, 94, 158 | Unauthorized Access／can't be found |
| `DS4Control/Services/LinkedProfilesXmlSerializer.cs` | 96, 139, 143, 148, 234 | 保存失敗／読み込み失敗 |
| `DS4Control/Services/SpecialActionRepository.cs` | 283 | Could not count Actions.xml entries |
| `DS4Control/Services/SpecialActionsXmlSerializer.cs` | 558, 563 | Actions.xml の読み込み失敗 |
| `DS4Control/Services/OscService.cs` | 144, 192, 200 | 不正な OSC／Sender 未起動 |
| `DS4Library/DS4Device.cs` | 1080, 1563, 1636 | CRC-32 失敗で切断／バッファ未初期化／書き込み接続の喪失 |
| private `LogDebug`（`VirtualPadService`） | 470 | Failed. No open output slot found |

### G3: 一時表示の診断（GUI のステータスから消え、ファイルに Debug で残る。**見た目が変わる**）

| ファイル | 行 | 内容 |
|---|---|---|
| `DS4Control/Services/ProfileSettingsService.cs` | 215, 226, 249, 260 | `[DI] …SetXxOutCurveMode: Slot = value` |
| `DS4Control/BackingStore.cs` | 762 | `[Legacy] BackingStore.ProfileSettings: Fallback instance used` |

理由: 開発者向けの状態（Log-Policy §2 の Debug）。現状はファイルに残らない（一時表示のため）。

### G4: トレイ

変更なし。診断行 `[Diag-Toast]`（`AppLogger.LogToTray` 内の 2 か所、`MainWindow` の 5 か所）は、ファイルのみのログで、削除する（Log-Policy §4）。

## 3. ファイルのみのログ（規則。各 Step で記録）

| 規則 | 内容 | 種別 |
|---|---|---|
| F1 | 失敗語（fail／error／exception など）を含むのに Trace／Debug／Info のもの（約 40 か所の候補。`UpdaterInstaller`、`ProfileApplicationService`、`OutputSlotService` など）は、1 件ずつ確認し、失敗なら Warn／Error へ | レベルのみ |
| F2 | 接頭辞を機能名へ統一: `[SortSpecialActionsList]`、`[SpecialActionsHeader_Click]`、`[MacroMove]`、`[MainWindow.EditProfile]`、`[ApplyProfile]`、`[Legacy]` など、メソッド名・診断名のもの | 文言 |
| F3 | `[Diag-Toast]` の 7 か所を削除 | 削除 |
| F4 | `Console`／`Debug.WriteLine`（29 か所）を `AppLogger.LogDebug`（またはレベルを見直し）へ。コメントアウトされた重複や不要なものは削除 | 文言／削除 |
| F5 | Updater の NLog 直接（24 か所）を `AppLogger` 経由へ。別プロセスの事情があるものは残して理由を記録 | 文言 |
| F6 | Trace／Debug の補間文字列で、入力ループなど高頻度の経路にガードを付ける（13b-8） | 性能のみ |
| F7 | 起動時の `LogInfo`（`App.xaml.cs` の環境・カルチャ診断、約 20 か所）は、Info のまま維持（起動の記録として意味がある） | 変更なし |

## 4. 承認の記録

- 2026-11-04: ユーザーが **G1〜G4 をすべて承認**（推奨案のとおり）。以降の Step で実施し、実施内容は各 Step の完了時に本書へ追記する。

## 5. 実施の記録

（各 Step の完了時に追記）

### 13b-3（サービス群の private `LogDebug` の廃止）

- **経路**: 8 サービスの private `LogDebug`＋`DebugMessage` イベント（8 インターフェース）を廃止し、`AppLogger.LogToGui`（利用者向け）を直接呼ぶ形へ。`IServiceDebugRelay`／`ServiceDebugRelay`（Phase13-Step10 10-e で新設）と、`ControlService` の `Debug` イベント・`LogDebug`・`OnDebug` を削除。`ControlService` のコンストラクター引数は 17 → 16。`LoggerHolder`・`LogViewModel` のコンストラクターから `ControlService` 引数を削除（`GuiLog` だけを購読）。画面・ファイルへの出力は不変。
- **G2（実施）**: `VirtualPadService` の「Failed. No open output slot found」を Info → Warn（GUI が警告表示になる。承認済み）。
- 文言の変更・削除: なし。

### 13b-4（`Actions/`・`DS4Control/`）

- **G1（実施・レベルのみ）**: 失敗系 `LogToGui(…, true)` を `Log(Error, …, UserFacing)` へ — `DefaultProcessLauncher` 41、`TriggerDispatchEngine` 673、`Mapping` 591／963、`AppSettingsService` 34、`ProfileRepository` 7 か所、`ProfileXmlStore` 4 か所、`SpecialActionRepository` 304、`SyntheticInputState` 84、`Util` 293、`WindowPlacementHelper` 4 か所（計 23）。GUI の見た目は不変。
- **G2（実施・GUI が警告表示になる）**: `LogToGui(…, false)` → `Log(Warn, …, UserFacing)` — `BackingStore` 932、`AppSettingsXmlSerializer` 2、`ControllerConfigsXmlSerializer` 3、`LinkedProfilesXmlSerializer` 5、`SpecialActionRepository` 283、`SpecialActionsXmlSerializer` 2、`OscService` 144／192／200（計 17）。
- **G3（実施・GUI の一時表示が消え、ファイルに Debug）**: `ProfileSettingsService` 4 か所、`BackingStore` 762（計 5）。
- **F1（レベルのみ・ファイル）**: 握りつぶした例外を表す Trace／Debug を Warn へ — `BatteryCheckAction.Execute`、`ButtonOutputAction`、`DisconnectBTAction.Execute`、`GyroCalibrateAction`、`KeyAction` 2、`KeyButtonActionController`、`LaunchProcessAction`、`Changelog`、`OutputSlotService` 4、`OutputSlotStore` 2、`ProfileApplicationService` 4（計 19）。
  GUI に Error を出す処理の詳細（スタック）を持つ Trace は Debug へ — `ProfileRepository` 2、`ProfileXmlStore` 2、`AppSettingsService` 1、`SyntheticDispatcherService` 2（計 7）。
  **毎フレームの経路は Trace のまま**（ログの洪水を避けるため）: `BatteryCheckAction.WhileHeld`、`DisconnectBTAction.ApplyFrame`。

### 13b-5（UI・入力層・Updater の G1／G2／F1／F3）

- **F3（削除）**: `[Diag-Toast]` の診断行 7 か所のうち、`MainWindow` の呼び出し記録・分岐記録 4 か所と `AppLogger.LogToTray` の呼び出し記録 1 か所を削除。例外を記録する 2 か所（`MainWindow` の `ShowModernToast`、`AppLogger.LogToTray` の `SendNotification`）は、診断用の接頭辞を `[Toast]` に改め、Debug → Warn にした（文言の変更＋レベル）。
- **G1（実施・レベルのみ）**: `ProfileEditor` 1128、`ControllerListViewModel` 3 か所、`ProfileSettingsViewModel` 3445、`SettingsViewModel` 466、`JoyConDevice` 599、`SwitchProDevice` 312、`HidDevice` 380（計 9）。
- **G2（実施・GUI が警告表示になる）**: `DS4Device` の「CRC-32 失敗で切断」「バッファ未初期化」「書き込み接続の喪失」（計 3）。
- **F1（レベルのみ・ファイル）**: Debug／Trace → Warn — `ProfileEditor` 291、`CompositeDeviceModel` 278、`App` 598、`UpdaterInstaller` 81／175、`DS4Device` 263／786（計 7）。Trace → Debug — `MainWindow` の PostApply の計測の失敗 1 か所。

### 13b-6（F4: `Console`／`Debug.WriteLine` の整理）

- **文言（`AppLogger.LogDebug` へ移行、ファイルの Debug に出るようになる）**: `DS4Device` 830（Thread already running）／852／875（例外。接頭辞 `[DS4Device] {Mac}` を付与）／1113／1143（read failure）／1724／1759（BT 切断）、`DS4SixAxis` 383／435（ジャイロの平均）、`DS3Device` 96、`DualSenseDevice` 471／605／634、`JoyConDevice` 1687、`SwitchProDevice` 1241、`HidDevice` 108（例外の記録）、`Util` 201／221（例外）、`WelcomeDialog` 148、`LoggerHolder` 149（NLog の `logger?.Debug` へ。ログ基盤の内部）。
- **削除**: 開発中の残骸で、情報のない出力 — 空の `Console.WriteLine()` 3 か所（`DS4Device`、`DS3Device`、`DualSenseDevice`）、`"CURRENT: …"`（`DualSenseDevice`）、`"RESET NFC/IR MCU"`・`"FINISHED"`（`JoyConDevice`）、`"FINISHED"`（`SwitchProDevice`）の計 7 か所。WPF アプリでは `Console` の出力は見えないため、利用者への影響はなし。
- **維持**: `App.xaml.cs` 962 の `Console.WriteLine(WaitAndReadIPCResultDataMMF(...))` は、CLI の `-command` の結果を標準出力へ返す機能の出力であり、ログではない。
- ブロックコメント内（`/* … */`）の `Console.WriteLine` は対象外。
