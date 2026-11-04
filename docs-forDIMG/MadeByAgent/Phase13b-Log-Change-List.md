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
