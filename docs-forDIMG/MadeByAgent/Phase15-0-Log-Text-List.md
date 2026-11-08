# Phase15-0-0: 利用者向けログ文言の一覧（承認用）

作成日: 2026-11-08
状態: **Q1〜Q5 は 2026-11-08 に承認済み（すべて推奨案）。§4 の訳案の承認待ち**（承認後に 15-0-1〜15-0-2 で実装する）
ブランチ: `Phase15-0`
根拠: `Phase15-0-Plan.md` §2 A・§6 15-0-0、`Log-Policy.md` §3・§11、規約 §2.3

## 1. 実測（2026-11-08、Phase15-0 の先頭）

| 区分 | 件数 | 備考 |
|---|---|---|
| `LogOptions.UserFacing` の呼び出し | **137** | grep の一致 143 行から、定義（`LogOptions.cs` 4 行）と `AppLogger.cs` のコメント・判定（2 行）を除いた実数。計画書の 138 は着手前の概数 |
| うち、すでにリソースを使うもの | 20 | §3 の一覧。英語の値と日本語の訳は既にある（`Resources.resx`／`Strings.resx`）。**移す作業は不要** |
| うち、文字列リテラル・補間・変数（移す候補） | 117（うち開発者向けの診断 D が 20、翻訳する対象は 97） | §4〜§5 の一覧 |
| `LogToTray` の呼び出し | **19** | うち 9 件は上の UserFacing と同じ文言を渡す（§6）。残りは独立の文言 |

## 2. 決定事項（Q1〜Q5。2026-11-08 承認、すべて推奨案＝太字）

| # | 論点 | 選択肢（太字＝推奨） |
|---|---|---|
| Q1 | **ファイルのログの言語**。`UserFacing` のログは、画面だけでなく `LoggerHolder` 経由でファイルにも出る。リソース化すると、日本語の表示のとき**ファイルも日本語になる** | **A: ファイルは英語のまま**（リソースは英語・日本語の両方を引く API を用意し、画面は現在の言語、ファイルは英語。不具合報告の読みやすさを保つ）／ B: ファイルも画面と同じ言語／ C: 画面だけ変え、ファイルは今の文字列そのまま（実装が二重になる） |
| Q2 | **開発者向けの診断文**（例外の全文、メソッド名つき）の扱い。§4 の「診断」印（呼び出し箇所で計 20 件）は利用者が読んでも対処できない | **A: 文言は英語のまま据え置き、`[DI]` など接頭辞の統一だけ行う（リソース化は見送り）**／ B: 他と同じくリソース化し訳す／ C: UserFacing の印を外し、ファイルだけにする（利用者の画面から消える。§2.2 の機能削減に当たるため別承認） |
| Q3 | **接頭辞の統一**。今は `WARNING:`／`WARNING.`／`Warning:`／`ERROR:`／`ERROR.`／`ERROR in`／`[DI]` が混在。レベル（Warn／Error）は画面の色・アイコンで既に示される | **A: 文頭の `WARNING:`／`ERROR:` 系の語を外す（レベルで示す）。`[DI]` は据え置き（規約 §2.3 の追跡用）**／ B: `WARNING:`／`ERROR:` に統一する／ C: 変えない |
| Q4 | **日本語の訳の用語** | **A: 既存の `Resources.ja.resx`／`Strings.ja.resx` の用語に合わせる**（コントローラー、プロファイル、排他モード、割り当て、出力スロット）／ B: 別途指示 |
| Q5 | **トレイ通知（§6）の訳** | **A: 画面のログと同じ文言を共有**（同じリソースを使う。重複を作らない）／ B: トレイ用を別に持つ |

以下の一覧の「日本語の訳」は、Q1=A、Q2=A、Q3=A、Q4=A、Q5=A を前提にした案。

## 3. すでにリソースを使っている（20 件。移行不要）

| 場所 | キー | 備考 |
|---|---|---|
| `ControlService.cs:282` | `Strings.ViGEmPluginFailure` | |
| `ControlService.cs:392` | `Resources.CouldNotOpenDS4`＋`QuitOtherPrograms`（`WarnExclusiveModeFailure`） | トレイにも同文 |
| `DS4Devices.cs:322` | `Resources.FoundController`＋MAC・接続種別・名前 | トレイにも同文 |
| `DualSenseDevice.cs:576` | `Strings.CRC32Fail` | |
| `MainWindow.xaml.cs:647` | `Strings.WakeupFromSuspend` | **`Strings.ja.resx` に訳がない**（英語のまま表示）。訳を足す: 「スリープから復帰しました」 |
| `SpecialActionsXmlSerializer.cs:365` | `Resources.XMLActionsCorrupt` | |
| `DeviceDisconnectHandler.cs:116` | `Resources.ControllerWasRemoved`＋`ChargeController` | トレイにも同文 |
| `TouchpadToggleService.cs:41,48` | `Resources.TouchpadMovementOff／On` | トレイにも同文 |
| `ServiceLifecycle.cs:102,122,123,230,290,311` | `Starting`／`SearchingController`／`UsingExclusive`／`UsingShared`／`StoppingX360`／`StoppingDS4`／`StoppedDS4Windows` | |
| `LatencyMonitor.cs:81,93` | `LatencyOverTen`／`LatencyNotOverTen` | |
| `ControlSpecialItemXml.cs:83`、`ActionsDTO.cs:270` | `ObsoleteAssignmentRemoved*` | |
| `SettingsViewModel.cs:356` | `Strings.CustomSteamFolderNotFound` | |

注意: `Resources.*`（`Properties/Resources.resx`）と `Strings.*`（`Translations/Strings.resx`）の 2 系統が混在している。新規は `Strings.*` に集約する（15-0-1 で決定。既存の `Resources.*` 20 件は動かさない）。

## 4. 移す対象の一覧（117 件。D を含む）

列: 印 = **D**（開発者向けの診断。Q2）。引数 = 書式の `{0}`〜の中身。キーは `Strings.resx` の新規名（仮）。

### 4.1 入力デバイス（第1層）

| ID | 場所 | Lv | 今の英語 | 引数 | キー | 日本語の訳（案） |
|---|---|---|---|---|---|---|
| L01 | DS4Device.cs:546 | Info | The gamepad {0} ({1}) uses custom feature set ({2}) | 名前、接続種別、FeatureSet | `Log_Device_CustomFeatureSet` | ゲームパッド {0}（{1}）は独自の機能セット（{2}）を使います |
| L02 | DS4Device.cs:714 / DualSenseDevice.cs:420 | Warn | Gyro Calibration Failed | — | `Log_Device_GyroCalibrationFailed` | ジャイロの較正に失敗しました |
| L03 | DS4Device.cs:727 | Info | Automatically fixed inverted YAW gyro axis in DS4 v.1 BT gamepad ({0}) | MAC | `Log_Device_YawAxisFixed` | DS4 v.1 の Bluetooth ゲームパッド（{0}）で反転していたヨー軸を自動で修正しました |
| L04 | DS4Device.cs:1083 | Warn | {0} failed CRC-32 checks {1} times. Disconnecting | MAC、回数 | `Log_Device_CrcFailedDisconnect` | {0} の CRC-32 検査が {1} 回失敗しました。切断します |
| L05 | DS4Device.cs:1111,1141 / DualSenseDevice.cs:603,632 | Warn | {0} disconnected due to timeout | MAC | `Log_Device_DisconnectedTimeout` | {0} はタイムアウトのため切断されました |
| L06 | DS4Device.cs:1118 / DualSenseDevice.cs:610 | Warn | {0} disconnected due to read failure: {1} | MAC、エラー番号 | `Log_Device_DisconnectedReadFailure` | {0} は読み取りエラーのため切断されました: {1} |
| L07 | DS4Device.cs:1436 / DS3Device.cs:382 / DualSenseDevice.cs:915 / JoyConDevice.cs:1056 / SwitchProDevice.cs:655 | Info | {0} disconnecting due to idle disconnect | MAC | `Log_Device_IdleDisconnect` | {0} を無操作切断の設定により切断します |
| L08 | DS4Device.cs:1565 **D** | Warn | sendOutputReport: output buffers not initialized (outputReport={0}, outReportBuffer={1}) | bool、bool | — | （据え置き） |
| L09 | DS4Device.cs:1638 | Warn | Gamepad data write connection is lost. Disconnecting the gamepad. LastErrorCode={0} | エラー番号 | `Log_Device_WriteConnectionLost` | ゲームパッドへのデータ書き込み接続が失われました。切断します。LastErrorCode={0} |
| L10 | DualSenseDevice.cs:335 | Warn | Feature report read failure | — | `Log_Device_FeatureReportReadFailure` | フィーチャーレポートの読み取りに失敗しました |
| L11 | JoyConDevice.cs:604 / SwitchProDevice.cs:316 | Error | Controller {0} failed to initialize. Closing device | MAC | `Log_Device_InitFailed` | コントローラー {0} の初期化に失敗しました。デバイスを閉じます |
| L12 | HidDevice.cs:343 | Warn | WARNING: Failed to read serial# from a gamepad ({0}/{1}). Generating MAC address from a device path. From now on you should connect this gamepad always into the same USB port or BT pairing host to keep the same device path. | VID、PID | `Log_Hid_SerialReadFailed` | ゲームパッド（{0}/{1}）からシリアル番号を読み取れませんでした。デバイスのパスから MAC アドレスを生成します。以後は、同じパスを保つために、このゲームパッドを常に同じ USB ポートまたは同じ Bluetooth ペアリング先に接続してください |
| L13 | HidDevice.cs:381 | Error | ERROR: Failed to generate runtime MAC address from device path {0}. {1} | パス、例外メッセージ | `Log_Hid_MacGenerationFailed` | デバイスのパス {0} から実行時の MAC アドレスを生成できませんでした。{1} |
| L14 | HidDevices.cs:79 **D** | Info | HID#{0} CONNECTING to {1}  VID={2}  PID={3}  Usage=0x{4}  Version=0x{5}  Path={6} | 7 個 | — | （据え置き。列挙の診断） |
| L15 | HidDevices.cs:83 **D** | Info | HID#{0} Unknown device {1}  VID=…（同上） | 7 個 | — | （据え置き） |
| L16 | HidDevices.cs:91 **D** | Info | `-------------------------`（区切り線） | — | — | （据え置き） |
| L17 | DS4Devices の error 文字列（LatencyMonitor.cs:44 が出力） **D** | Info | `device.error` の内容（`Touchpad: {例外}` など） | 例外 | — | （据え置き。実体は `DS4Device` 側の組み立て。リソース化しない） |

### 4.2 出力・サービス（第3層）

| ID | 場所 | Lv | 今の英語 | 引数 | キー | 日本語の訳（案） |
|---|---|---|---|---|---|---|
| L20 | OutSlotDevice.cs:124 | Info | Output slot #{0} has permanent type {1} | 番号、種別 | `Log_Out_SlotPermanentType` | 出力スロット #{0} は種別 {1} に固定されています |
| L21 | OutputSlotManager.cs:198 | Info | Plugging in virtual X360 controller (XInput slot #{0}) in output slot #{1} | XInput 番号（未定は ?）、番号 | `Log_Out_PluginX360` | 仮想 X360 コントローラー（XInput スロット #{0}）を出力スロット #{1} に接続します |
| L22 | OutputSlotManager.cs:202 | Info | Plugging in virtual {0} Controller in output slot #{1} | 種別、番号 | `Log_Out_PluginVirtual` | 仮想 {0} コントローラーを出力スロット #{1} に接続します |
| L23 | OutputSlotManager.cs:248 | Info | Unplugging virtual {0} Controller from output slot #{1} | 種別、番号 | `Log_Out_UnplugVirtual` | 仮想 {0} コントローラーを出力スロット #{1} から取り外します |
| L24 | VirtualPadService.cs:466 | Warn | Failed. No open output slot found | — | `Log_Out_NoOpenSlot` | 失敗しました。空いている出力スロットがありません |
| L25 | VirtualPadService.cs:486 | Info | Associated input controller #{0} ({1}) to virtual {2} Controller in{3} output slot #{4} | 4＋固定語 " permanent" | `Log_Out_Associated`／`…Permanent` | 入力コントローラー #{0}（{1}）を、{3}出力スロット #{4} の仮想 {2} コントローラーに関連付けました（{3} は「固定の」か空） |
| L26 | VirtualPadService.cs:526 | Info | Disassociated virtual {0} Controller in{1} output slot #{2} from input controller #{3} ({4}) | 5 | `Log_Out_Disassociated` | {1}出力スロット #{2} の仮想 {0} コントローラーを、入力コントローラー #{3}（{4}）から切り離しました |
| L27 | HidHideService.cs:40 | Info | HidHide control device found | — | `Log_HidHide_Found` | HidHide の制御デバイスが見つかりました |
| L28 | HidHideService.cs:94 | Info | {0} not found in HidHide whitelist. Adding to list | exe 名 | `Log_HidHide_AddWhitelist` | {0} が HidHide の許可リストにありません。リストに追加します |
| L29 | HidHideService.cs:100 | Info | {0} found in HidHide whitelist. Removing from list | exe 名 | `Log_HidHide_RemoveWhitelist` | {0} が HidHide の許可リストにありました。リストから削除します |
| L30 | UdpMotionService.cs:86,131,218 | Info | UDP server listening on address {0} port {1} | アドレス、ポート | `Log_Udp_Listening` | UDP サーバーがアドレス {0}、ポート {1} で待ち受けています |
| L31 | UdpMotionService.cs:92,137,224（＋トレイ 93,138,225） | Warn | Couldn't start UDP server on address {0}:{1}, outside applications won't be able to access pad data ({2}) | アドレス、ポート、SocketError | `Log_Udp_StartFailed` | UDP サーバーをアドレス {0}:{1} で開始できませんでした。外部のアプリケーションはパッドのデータにアクセスできません（{2}） |
| L32 | UdpMotionService.cs:108 | Info | Closed UDP server | — | `Log_Udp_Closed` | UDP サーバーを閉じました |
| L33 | OscService.cs:53 | Info | OSC LISTENER STARTED AT PORT: {0} | ポート | `Log_Osc_ListenerStarted` | OSC リスナーをポート {0} で開始しました |
| L34 | OscService.cs:59 | Info | OSC LISTENER STOPPED | — | `Log_Osc_ListenerStopped` | OSC リスナーを停止しました |
| L35 | OscService.cs:67 | Info | OSC SENDER STARTED AT IP: {0} PORT: {1} | IP、ポート | `Log_Osc_SenderStarted` | OSC 送信を IP {0}、ポート {1} で開始しました |
| L36 | OscService.cs:72 | Info | OSC SENDER STOPPED | — | `Log_Osc_SenderStopped` | OSC 送信を停止しました |
| L37 | OscService.cs:148 | Warn | Received malformed OSC message: {0} ({1}: {2}) | アドレス、例外型、メッセージ | `Log_Osc_MalformedMessage` | 不正な OSC メッセージを受信しました: {0}（{1}: {2}） |
| L38 | OscService.cs:196 | Warn | Received malformed OSC address: {0} | アドレス | `Log_Osc_MalformedAddress` | 不正な OSC アドレスを受信しました: {0} |
| L39 | OscService.cs:204 | Warn | Battery level requested, but the OSC Sender isn't active. Turn it on in Settings. | — | `Log_Osc_SenderInactive` | バッテリー残量が要求されましたが、OSC 送信が有効ではありません。設定で有効にしてください |

### 4.3 処理（第2層）

| ID | 場所 | Lv | 今の英語 | 引数 | キー | 日本語の訳（案） |
|---|---|---|---|---|---|---|
| L40 | Mapping.cs:588 **D** | Error | Synchronous Button SA handling error: {0} | 例外 | — | （据え置き） |
| L41 | Mapping.cs:960、TriggerDispatchEngine.cs:676 **D** | Error | MapCustomAction exception: {0} | 例外 | — | （据え置き） |
| L42 | SyntheticInputState.cs:82 | Info | Runtime state for key {0} cleared on all devices. | キー | `Log_Synth_StateCleared` | キー {0} の実行時の状態を、すべてのデバイスで解除しました |
| L43 | SyntheticInputState.cs:86 | Error | Failed to reset runtime key state for {0}: {1} | キー、メッセージ | `Log_Synth_StateResetFailed` | キー {0} の実行時の状態を解除できませんでした: {1} |
| L44 | DefaultProcessLauncher.cs:42 | Error | Process launch failed for '{0}': {1} | ファイル、メッセージ | `Log_Process_LaunchFailed` | '{0}' の起動に失敗しました: {1} |
| L45 | DeviceConnectionSetup.cs:44 | Info | Output data to DS4 disabled. Lightbar and rumble events are not written to DS4 gamepad. If the gamepad is connected over BT then IdleDisconnect option is recommended to let DS4Windows to close the connection after long period of idling. | — | `Log_Conn_OutputDisabled` | DS4 への出力は無効です。ライトバーと振動のイベントは DS4 ゲームパッドへ書き込まれません。Bluetooth で接続している場合は、長時間操作がないときに DS4Windows が接続を閉じるよう、無操作切断の設定をおすすめします |
| L46 | ProfileApplicationService.cs:316 | Info | Controller {0} is now using Profile "{1}" (Battery: {2}%) | 番号、名前、電池（N/A あり） | `Log_Profile_NowUsing` | コントローラー {0} はプロファイル「{1}」を使用中です（バッテリー: {2}%） |
| L47 | ActionsLoadedRuntimeReset.cs:32 | Info | Runtime synthetic state reset after Actions load | — | `Log_Actions_RuntimeReset` | Actions の読み込み後に、実行時の疑似入力の状態を初期化しました |
| L48 | ServiceLifecycle.cs:110 | Info | Some applications may block controller inputs. (Windows UAC Conflictions). Please go to {0} for more information and workarounds. | URL | `Log_Svc_UacConflict` | 一部のアプリケーションがコントローラーの入力を妨げることがあります（Windows の UAC との競合）。詳細と回避策は {0} を参照してください |
| L49 | ServiceLifecycle.cs:113 | Info | Using output KB+M handler: {0} | 名前 | `Log_Svc_KbmHandler` | 出力のキーボード・マウスのハンドラー: {0} |
| L50 | ServiceLifecycle.cs:114 | Info | Connection to ViGEmBus {0} established | バージョン | `Log_Svc_VigemConnected` | ViGEmBus {0} に接続しました |
| L51 | ServiceLifecycle.cs:182（＋トレイ 183） | Warn | `e.Message`（例外の文言そのもの） **D** | 例外 | — | （据え置き） |
| L52 | ServiceLifecycle.cs:206（＋トレイ 207） | Info | ViGEmBus is not installed | — | `Log_Svc_VigemNotInstalled` | ViGEmBus がインストールされていません |
| L53 | 同上 | Info | Unsupported ViGEmBus found ({0}). Please install at least ViGEmBus 1.17.333.0 | バージョン | `Log_Svc_VigemUnsupported` | 未対応の ViGEmBus が見つかりました（{0}）。ViGEmBus 1.17.333.0 以上をインストールしてください |
| L54 | 同上 | Info | Could not connect to ViGEmBus. Please check the status of the System device in Device Manager and if Visual C++ 2017 Redistributable is installed. | — | `Log_Svc_VigemConnectFailed` | ViGEmBus に接続できませんでした。デバイス マネージャーのシステム デバイスの状態と、Visual C++ 2017 再頒布可能パッケージのインストールを確認してください |
| L55 | ServiceLifecycle.cs:232 | Info | Closing connection to ViGEmBus | — | `Log_Svc_VigemClosing` | ViGEmBus との接続を閉じます |
| L56 | ServiceLifecycle.cs:327 | Info | Closing connection to output handler {0} | 名前 | `Log_Svc_KbmClosing` | 出力ハンドラー {0} との接続を閉じます |
| L57 | BackingStore.cs:931 | Warn | Profile '{0}' contains an invalid special action '{1}'. | プロファイル、名前 | `Log_Settings_InvalidSpecialAction` | プロファイル '{0}' に無効なスペシャルアクション '{1}' が含まれています |

### 4.4 永続化・設定（Core）

| ID | 場所 | Lv | 今の英語 | 引数 | キー | 日本語の訳（案） |
|---|---|---|---|---|---|---|
| L60 | Util.cs:294 **D** | Error | `t.Exception.ToString()`（例外の全文） | 例外 | — | （据え置き） |
| L61 | SpecialActionsXmlSerializer.cs:554 | Warn | Actions.xml contains invalid data. Could not be read. {0} | メッセージ | `Log_Xml_ActionsInvalidData` | Actions.xml に無効なデータが含まれており、読み込めませんでした。{0} |
| L62 | SpecialActionsXmlSerializer.cs:559 | Warn | Actions.xml could not be read. Invalid XML syntax. {0} | メッセージ | `Log_Xml_ActionsInvalidSyntax` | Actions.xml を読み込めませんでした。XML の構文が正しくありません。{0} |
| L63 | SpecialActionRepository.cs:283 | Warn | Could not count Actions.xml entries: {0} | メッセージ | `Log_Actions_CountFailed` | Actions.xml の項目数を数えられませんでした: {0} |
| L64 | SpecialActionRepository.cs:290 | Warn | Warning: action count mismatch after {0}: memory={1} file={2} | 呼び出し元、数、数 | `Log_Actions_CountMismatch` | {0} の後に、アクション数が一致しません: メモリ={1} ファイル={2} |
| L65 | SpecialActionRepository.cs:294 | Info | Action list reloaded ({0} entries) after {1}. | 数、呼び出し元 | `Log_Actions_Reloaded` | {1} の後に、アクション一覧を再読み込みしました（{0} 件） |
| L66 | SpecialActionRepository.cs:299 | Info | Action list reloaded ({0} entries) after {1}. (file count unknown) | 数、呼び出し元 | `Log_Actions_ReloadedUnknown` | {1} の後に、アクション一覧を再読み込みしました（{0} 件。ファイルの件数は不明） |
| L67 | SpecialActionRepository.cs:304 | Error | Error reloading Actions.xml after {0}: {1} | 呼び出し元、メッセージ | `Log_Actions_ReloadFailed` | {0} の後に、Actions.xml を再読み込みできませんでした: {1} |
| L68 | ProfileXmlStore.cs:45 | Error | Failed to load profile XML: {0} | メッセージ | `Log_Xml_ProfileLoadFailed` | プロファイルの XML を読み込めませんでした: {0} |
| L69 | ProfileXmlStore.cs:61 | Error | Failed to save profile XML: {0} | メッセージ | `Log_Xml_ProfileSaveFailed` | プロファイルの XML を保存できませんでした: {0} |
| L70 | ProfileXmlStore.cs:80 | Error | Failed to load application settings XML: {0} | メッセージ | `Log_Xml_SettingsLoadFailed` | アプリケーション設定の XML を読み込めませんでした: {0} |
| L71 | ProfileXmlStore.cs:101 | Error | Failed to save application settings XML: {0} | メッセージ | `Log_Xml_SettingsSaveFailed` | アプリケーション設定の XML を保存できませんでした: {0} |
| L72 | LinkedProfilesXmlSerializer.cs:91,229、ControllerConfigsXmlSerializer.cs:60,153 | Warn | Unauthorized Access - Save failed to path: {0} | パス | `Log_Xml_UnauthorizedSave` | アクセスが拒否されました。次のパスへの保存に失敗しました: {0} |
| L73 | LinkedProfilesXmlSerializer.cs:134 | Warn | LinkedProfiles.xml contains invalid data. Could not be read. {0} | メッセージ | `Log_Xml_LinkedInvalidData` | LinkedProfiles.xml に無効なデータが含まれており、読み込めませんでした。{0} |
| L74 | LinkedProfilesXmlSerializer.cs:138 | Warn | LinkedProfiles.xml could not be read. Invalid XML syntax. {0} | メッセージ | `Log_Xml_LinkedInvalidSyntax` | LinkedProfiles.xml を読み込めませんでした。XML の構文が正しくありません。{0} |
| L75 | LinkedProfilesXmlSerializer.cs:143 | Warn | LinkedProfiles.xml can't be found. | — | `Log_Xml_LinkedNotFound` | LinkedProfiles.xml が見つかりません |
| L76 | ControllerConfigsXmlSerializer.cs:89 | Warn | ControllerConfigs.xml can't be found. | — | `Log_Xml_ControllerConfigsNotFound` | ControllerConfigs.xml が見つかりません |
| L77 | AppSettingsXmlSerializer.cs:465 | Warn | Profiles.xml is missing some settings; not auto-saving to avoid overwriting existing file. | — | `Log_Xml_ProfilesMissingSettings` | Profiles.xml に不足している設定があるため、既存のファイルの上書きを避けて自動保存しません |
| L78 | AppSettingsXmlSerializer.cs:540 | Warn | Could not save Profiles.xml due to IO error: {0} | メッセージ | `Log_Xml_ProfilesIoError` | 入出力エラーのため Profiles.xml を保存できませんでした: {0} |
| L79 | ProfileRepository.cs:105 | Error | Failed to save profile: invalid path for '{0}' | 名前 | `Log_Repo_SaveInvalidPath` | プロファイルを保存できませんでした: '{0}' のパスが無効です |
| L80 | ProfileRepository.cs:118 | Error | Failed to save profile '{0}' for device {1} | 名前、番号 | `Log_Repo_SaveFailed` | デバイス {1} のプロファイル '{0}' を保存できませんでした |
| L81 | ProfileRepository.cs:127 | Error | Failed to save profile '{0}' for device {1}: {2} | 名前、番号、メッセージ | `Log_Repo_SaveFailedEx` | デバイス {1} のプロファイル '{0}' を保存できませんでした: {2} |
| L82 | ProfileRepository.cs:224 **D** | Error | [DI] ProfileRepository: cannot enumerate profiles for Controls profile switch target update: {0} | メッセージ | — | （接頭辞の整理のみ。§2 Q2） |
| L83 | ProfileRepository.cs:260 | Error | [DI] ProfileRepository: failed to update the Controls profile switch target in '{0}': {1} | ファイル、メッセージ | `Log_Repo_SwitchTargetUpdateFailed` | [DI] Controls のプロファイル切り替え先を '{0}' で更新できませんでした: {1} |
| L84 | ProfileRepository.cs:266 | Info | [DI] Controls profile switch target renamed '{0}' to '{1}' in {2} profile(s): {3} | 旧、新、数、一覧 | `Log_Repo_SwitchTargetRenamed` | [DI] Controls のプロファイル切り替え先 '{0}' を '{1}' に改名しました（{2} 件のプロファイル: {3}） |
| L85 | ProfileRepository.cs:364 **D** | Error | [DI] ProfileRepository: cannot enumerate profiles for action reference update: {0} | メッセージ | — | （接頭辞の整理のみ） |
| L86 | ProfileRepository.cs:400 | Error | [DI] ProfileRepository: failed to update action reference in '{0}': {1} | ファイル、メッセージ | `Log_Repo_ActionRefUpdateFailed` | [DI] '{0}' のアクション参照を更新できませんでした: {1} |
| L87 | ProfileRepository.cs:407 | Info | [DI] Action reference {0} in {1} profile(s): {2} | 動作、数、一覧 | `Log_Repo_ActionRefUpdated` | [DI] アクション参照を {0} しました（{1} 件のプロファイル: {2}） ※{0} は呼び出し元が英語の動詞を渡している。動詞もリソース化が必要 |
| L88 | AppSettingsService.cs:36 | Error | Failed to save application settings | — | `Log_Settings_SaveFailed` | アプリケーション設定を保存できませんでした |
| L89 | BezierCurve.cs:156 | Warn | WARNING. Invalid custom bezier curve "{0}, {1}, {2}, {3}" in {4} axis. x1 and x2 should be in 0..1 range. Using linear curve. | 4 値、軸 | `Log_Bezier_Invalid` | {4} 軸のカスタムベジェ曲線 "{0}, {1}, {2}, {3}" が無効です。x1 と x2 は 0〜1 の範囲にしてください。直線の曲線を使います |

### 4.5 UI・アプリ

| ID | 場所 | Lv | 今の英語 | 引数 | キー | 日本語の訳（案） |
|---|---|---|---|---|---|---|
| L90 | App.xaml.cs:697 | Info | Running as {0}（Admin／User） | 語 | `Log_App_RunningAs` | {0} として実行中です（管理者／ユーザー） ※語は別キー |
| L91 | MainWindow.xaml.cs:542 | Warn | Could not connect to Windows Management Instrumentation service.⏎Suspend support not enabled. | — | `Log_Ui_WmiFailed` | Windows Management Instrumentation サービスに接続できませんでした。サスペンドへの対応は有効になりません |
| L92 | MainWindow.Ipc.cs:91 **D** | Info | IPC コマンドのデバッグ文言（呼び出し元が渡す） | — | — | （据え置き。`LogDebug` の実装で、呼び出し元の文言をそのまま通す） |
| L93 | WindowPlacementHelper.cs:167 **D** | Error | ApplyPlacement: Failed while enumerating screens for debug: {0} | メッセージ | — | （据え置き） |
| L94 | WindowPlacementHelper.cs:194 **D** | Info | ApplyPlacement: Using Primary Monitor DPI Scale = {0} | 数値 | — | （据え置き） |
| L95 | WindowPlacementHelper.cs:200 **D** | Error | ApplyPlacement: Failed to get target monitor DPI: {0} | メッセージ | — | （据え置き） |
| L96 | WindowPlacementHelper.cs:299 **D** | Error | ApplyPlacement: SetWindowPlacement returned false, Win32Error={0} | 番号 | — | （据え置き） |
| L97 | WindowPlacementHelper.cs:308 | Error | Failed to apply window placement: {0} | メッセージ | `Log_Ui_PlacementFailed` | ウィンドウの位置を適用できませんでした: {0} |
| L98 | ProfileEditor.xaml.cs:1123 | Info | [DI] Profile '{0}' saved successfully. | 名前 | `Log_Editor_ProfileSaved` | [DI] プロファイル '{0}' を保存しました |
| L99 | ProfileEditor.xaml.cs:1135 | Error | [DI] Failed to save profile '{0}'. | 名前 | `Log_Editor_ProfileSaveFailed` | [DI] プロファイル '{0}' を保存できませんでした |
| L100 | ProfileEditor.xaml.cs:1894 | Info | [DI] Controls tab: reset all assignments to Default | — | `Log_Editor_ControlsReset` | [DI] Controls タブ: すべての割り当てを既定に戻しました |
| L101 | ControllerListViewModel.cs:186 **D** | Error | ControllerListViewModel.Dispose: ClearControllerList failed: {0} | 例外 | — | （据え置き） |
| L102 | ControllerListViewModel.cs:320 **D** | Error | ERROR in Global_SelectedProfileChanged Dispatcher callback: {0} | メッセージ | — | （据え置き。`ERROR in` は Q3 で整理） |
| L103 | ControllerListViewModel.cs:329 **D** | Error | ERROR in Global_SelectedProfileChanged: {0} | メッセージ | — | （据え置き。同上） |
| L104 | ProfileSettingsViewModel.cs:3453 | Error | ERROR. Failed to open {0}\BezierCurveEditor\index.html web app. Check that the web file exits or launch it outside of DS4Windows application. {1} | パス、メッセージ | `Log_Ui_BezierEditorOpenFailed` | {0}\BezierCurveEditor\index.html を開けませんでした。ファイルが存在するか確認するか、DS4Windows の外から起動してください。{1} |
| L105 | SettingsViewModel.cs:470 | Error | Error in TaskService. Check WinOS TaskScheduler service functionality. {0} | メッセージ | `Log_Ui_TaskServiceError` | タスク サービスでエラーが発生しました。Windows のタスク スケジューラーの動作を確認してください。{0} |

**内訳**: 翻訳してリソースへ移す呼び出し箇所は 97 件（同じ文言の重複は 1 キーにまとめるので、新設キー数はこれより少ない。実数は 15-0-1 で確定）。据え置き（D の印）は 20 件。

## 5. 接頭辞の整理（Q3=A の場合）

| 場所 | 今 | 整理後 |
|---|---|---|
| HidDevice.cs:343 | `WARNING: Failed to read…` | `Failed to read…`（Warn のレベルで示す） |
| HidDevice.cs:381 | `ERROR: Failed to generate…` | `Failed to generate…` |
| BezierCurve.cs:156 | `WARNING. Invalid custom…` | `Invalid custom…` |
| ProfileSettingsViewModel.cs:3453 | `ERROR. Failed to open…` | `Failed to open…` |
| ControllerListViewModel.cs:320,329 | `ERROR in Global_SelectedProfileChanged…` | `Global_SelectedProfileChanged failed…`（据え置きの診断文。語だけ置換） |
| SpecialActionRepository.cs:290 | `Warning: action count mismatch…` | `Action count mismatch…` |
| `[DI]` | 6 件（L82〜L87、L98〜L100 の 9 件） | 据え置き（規約 §2.3 の追跡用） |

## 6. トレイ通知 `LogToTray`（19 件）

| ID | 場所 | 文言の出どころ | 方針（Q5=A） |
|---|---|---|---|
| T01 | ControlService.cs:393 | 3 章 R 群と同じ（`CouldNotOpenDS4`＋`QuitOtherPrograms`） | 変更なし（リソース済み） |
| T02 | DS4Devices.cs:325 | `FoundController`＋… | 変更なし |
| T03 | UdpMotionService.cs:93,138,225 | L31 と同じ | L31 のキーを共有 |
| T04 | ServiceLifecycle.cs:183 | L51（例外メッセージ） | 据え置き |
| T05 | ServiceLifecycle.cs:207 | L52〜L54 と同じ | 同じキーを共有 |
| T06 | DeviceDisconnectHandler.cs:117 | `ControllerWasRemoved` | 変更なし |
| T07 | TouchpadToggleService.cs:42,49 | `TouchpadMovementOff／On` | 変更なし |
| T08 | UpdaterWindow.xaml.cs:100,241,291 | `Strings.InstallSuccess_Notification` | 変更なし（ja の訳あり） |
| T09 | MainWindow.xaml.cs:772 | ホットキー通知の呼び出し元の文言（`ShowHotkeyNotification(message)`） | 呼び出し元の文言を確認して別表で扱う（15-0-1 の冒頭で追加の一覧を出す） |
| T10 | DefaultBatteryCheckPort.cs:42 | `"Controller " + (番号) + ": " + 残量テキスト` | `Controller {0}: {1}` を `Log_Battery_Notify` へ。ja: `コントローラー {0}: {1}`（残量テキスト自体は別所で生成） |
| T11 | AppLogger.cs:106（定義）、NotificationService.cs:13（転送） | — | 対象外（文言を持たない） |

## 7. 実装方針（承認後）

- 新規の文言は `Strings.resx`（英語）と `Strings.ja.resx`（日本語）に足す。キー名は `Log_<領域>_<内容>`。
- Q1=A のため、画面に出す文字列は現在の言語、ファイルへ出す文字列は英語（`Strings.ResourceManager.GetString(key, CultureInfo.InvariantCulture)`）。`AppLogger` に、キーと引数を受ける経路を足すか、呼び出し側で 2 つの文字列を作るかを 15-0-1 で決める（`AppLogger.Log` の署名は変えない方針）。
- 書式の引数の数は、英語・日本語の両方を `{n}` の最大番号で照合するテストを足す（計画書 §7）。
- 既存の `Resources.*` 20 件は動かさない。`Strings.ja.resx` に `WakeupFromSuspend` の訳だけ足す（§3）。
- ログの文言を文字列で照合している既存のテストは、15-0-1 で洗い出して直す（調査は 15-0-1 の冒頭）。

## 8. 承認してほしいもの

1. 質問 Q1〜Q5 の選択
2. §4 の日本語の訳（直したい行があれば ID で指定）
3. 据え置き（D）20 件の扱い（Q2）

## 9. 実施結果（Step 15-0-1。2026-11-08）

- §4 の訳案は承認済み。`Strings.resx`／`Strings.ja.resx` に `Log_*` キー 87 件（Log_Repo_ActionRefRemoved／Renamed、Log_Out_Associated／AssociatedPermanent などの分岐用を含む）を追加し、`WakeupFromSuspend` の訳も足した。
- 呼び出し箇所 85 件を `AppLogger.LogUser(level, key, args…)` へ置き換えた。画面は現在の表示言語、ファイルは英語（`DebugEventArgs.FileData`、`LoggerHolder` が `FileData ?? Data` を書く）。トレイ・変数経由の文言は `AppLogger.UserText(key, args…)`（UDP の起動失敗、ViGEmBus の 3 文言、プロファイル適用の案内、電池通知）。
- 接頭辞（Q3）: `WARNING:`／`ERROR:`／`Warning:` を外した 6 件と、`ControllerListViewModel` の `ERROR in …` 2 件（診断文）を整理。`[DI]` は据え置き。
- 据え置き（D）20 件と、すでにリソースだった 20 件は変更なし。
- テスト: `UserLogTextResourceTests`（日本語の訳の有無・書式の引数の一致・使うキーと定義の一致・画面／ファイルの出し分け）を追加。英語の文言を前提にしていた 4 テストは、ファイルの文字列（`FileData ?? Data`）で照合する形に直した（期待値は変えていない）。
- 検証: ビルド 0 警告 0 エラー、全件（Benchmark を除く）3,152 件合格。
- 持ち越し: `DS4Device.error` など呼び出し元が組み立てる英語の文言は未リソース化（診断文、Q2）。
