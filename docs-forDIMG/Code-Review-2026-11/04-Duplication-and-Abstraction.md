# コードレビュー（2026-11）観点 4: 共通部品化が必要十分な範囲か

作成日: 2026-11-11（対象のコミットと読み方は `00-Summary.md`）
範囲の注意: 機種間は、デバイス本体（`Input/Devices`）の読み取り・スレッドの起動と、解析器・`ProcessFrame`（DS4 と DualSense）を読み比べた。工場・Converter・束ね型は 2026-11-11 の追加調査で確かめた（4-7、4-8）。Switch Pro の `ProcessFrame` と、トリガー（L2／R2）の ViewModel は未調査。

## 1. 良い点（維持すべき設計）

- 1 フレームの流れは `InputLoopDevice` に集まり、4 機種（`DualShock4Device`、`DualSenseDevice`、`SwitchProDevice`、`ProtocolDevice`）とも、差し込み口（`ReceiveFrame`／`IsExpectedReport`／`ProcessFrame`／`ProcessIdle`／`WriteOutputFrame`）だけを書いている（`DS4Windows/Input/Devices/InputLoopDevice.cs:102`）。
- 電池の段階 → % は `BatteryScale`、Xbox の接続の判定は `XusbConnectionClassifier` の 1 か所に集まっている。
- 特殊アクションの出力の組み立ては、種別ごとの部品の列（`ISpecialActionOutputBuilder` が 10 個）で、`switch` がない。

## 2. 問題点（重複）

### 4-1（中）「プロファイルに結んだプログラムを起動する」処理が 2 か所にある
- 場所: `DS4Windows/Processing/Services/DeviceConnectionSetup.cs:78`〜（`CheckLauchProfileOption`）と、`DS4Windows/Core/Persistence/ProfileSections/ProfileMiscLoadSection.cs:59`〜`110`
- 事実: どちらも、プロセスを列挙して実行ファイルのパスを比べ、見つからなければ `Task` の中で `Thread.Sleep(5000)` のあとに起動する。永続化の側だけが、先に `IProcessInspector` を試す。
- なぜ問題か: 片方だけ直すと動きがずれる。永続化の層に副作用がある（`02-Layers-and-Placement.md` の 2-1）。
- 追加調査（2026-11-11）: 2 つは場面で分かれており、たどった範囲では二重に起動しない。
  - 接続のとき: 読み込みは「起動しない」で行い（`Processing/Services/ProfileSlotApplier.cs:38` のコメントの引数仕様）、そのあと `DeviceConnector.cs:106` が `ApplyProfileOptions`（`startUp` は既定の偽）を呼んで、第 2 層の側が起動する。
  - 切替のとき（特殊アクションのプロファイル切替 `ProfileApplicationService.cs:124`・`130`、自動プロファイルの一時プロファイル `AutoProfileService.cs:227`、IPC の `LoadTempProfile` `IpcCommandProcessor.cs:177`）: 読み込みを「起動する」で行い、永続化の側が起動する。読み込みの中からの `CheckProfileOptions` は `startUp` が真（`Core/Settings/BackingStore.cs:1171`）なので、第 2 層の側は起動しない。
  - つまり、同じ処理を、場面ごとに別の実装が受け持っている。二重起動の不具合ではないが、片方だけ直すとずれる点は変わらない。
- 修正の方向: 起動の判断と実行を、第 2 層の 1 つの部品へまとめる（`IProcessInspector` と `IProcessLauncher` を使う）。

### 4-2（中）DS4 と DualSense の、読み取り・CRC の検証・停止の手順が重複し、細部がずれている
- 場所: `DS4Windows/Input/Devices/DualShock4Device.cs:539`〜`644` と `DS4Windows/Input/Devices/DualSenseDevice.cs:508`〜`598`
- 事実: 次の 3 つが、ほぼ同じ形で両方にある。
  1. Bluetooth の CRC-32 の検証（受信した値の組み立て、`Crc32Algorithm.CalculateFasterBT78Hash`、失敗の回数の数え上げ）。
  2. 読み取りの失敗のログ（タイムアウトか、Win32 のエラーか）。
  3. 停止の手順（`readWaitEv.Reset` → `StopOutputUpdate` → `isDisconnecting` → `RunRemoval` → `timeoutExecuted`）。
- ずれ: DS4 は定数 `CRC32_NUM_ATTEMPTS` と `FrameResult.Terminate`、DualSense は直書きの `10` と `exitInputThread = true`＋`FrameResult.Skip`。ログも、DS4 はリソースのキー（`Log_Device_CrcFailedDisconnect`）、DualSense は旧い文言（`Strings.CRC32Fail`）。
- なぜ問題か: 片方だけ直す事故が起きやすい。DualSense は実機がなく、ずれに気づきにくい。
- 修正の方向: CRC の検証と「読み取りの失敗 → 停止」を `InputLoopDevice` の保護メソッドへ出す。ログの文言は機種ごとのまま残す（文言は変えない）。機種ごとの差分テスト（`FakeHidTransport`）で守る。

### 4-3（低）読み取りスレッドの起動と、Bluetooth の監視スレッドが、機種ごとに書かれている
- 場所: `DualShock4Device.cs:229`〜`271`、`DualSenseDevice.cs:430`〜`467`、`SwitchProDevice.cs:310`〜`338`、`ProtocolDevice.cs:101`〜。監視スレッドは `DualShock4Device.cs:169`〜`184` と `DualSenseDevice.cs:469`〜`489`
- 事実: スレッドの作り方（優先度 `AboveNormal`、`IsBackground`、名前）は同じ。監視スレッド（`TimeoutTestThread`）は、機器を起こす 1 行（DS4 は出力レポートの送信、DualSense はシリアルの読み出し）だけが違う。
- 修正の方向: 起動と監視を基底へ出し、「機器を起こす処理」と「スレッドの名前」だけを差し込み口にする。

### 4-4（中）左右スティックの ViewModel と XAML が二重
- 場所: `DS4Windows/UI/ViewModels/StickSettingsViewModel.cs:29`〜`600`（648 行）、`DS4Windows/UI/StickSettingsPanel.xaml`（1,283 行）
- 事実: `LSDeadZone`／`RSDeadZone` のように、左右で同じ形のプロパティが約 31 組（62 個）ある。違いは、読み書きの先（`sticks.LSMod`／`sticks.RSMod` など）だけ。XAML も左右で同じ形を 2 回書いている。
- なぜ問題か: スティックの設定を 1 つ足すと、ViewModel 2 か所と XAML 2 か所を書く。左右で片方だけ直す事故が起きる。
- 修正の方向: 片側ぶんの ViewModel を 1 つ作り、左右で 2 つ生成する。XAML は片側ぶんの部品（UserControl）を 2 回置く。保存する名前・設定の窓口（`StickSlotSettings`）には触れない。
- 参考: 同じ形は、トリガー（`TriggerSettingsViewModel` の L2／R2）にもあると見られる（未調査）。

### 4-7（低）DS4 と DualSense の解析器・タッチの取り込みが、位置だけ違う同じ形（2026-11-11 追加調査）
- 場所: `DS4Windows/Input/Parsers/Ds4ReportParser.cs`（78 行）と `DualSenseReportParser.cs`（95 行）。方向キーの 9 通りの `switch`、ボタンのビットの取り出し、タッチパッドの生値（2 本分、各 5 行）が、バイトの位置（`reportOffset` と固定の差）だけ違う。各機種の `ProcessFrame` のタッチの取り込み（`DualShock4Device.cs:715`〜`739`、`DualSenseDevice.cs:673` の周辺）も同じ形。
- 事実: `ProcessFrame` 全体では、DS4 の 110 行と DualSense の 93 行のうち、空白を除いて違う行が 85 行あり、時刻・電池・同期の扱いは機種ごとに違う。共通なのは、上のビットの取り出しの部分。
- なぜ問題か: 害は小さい。解析器は小さく、旧実装との差分テストで守られている。3 つ目の Sony 系の機種を足すときに、同じ形をもう 1 回書くことになる。
- 修正の方向: 急がない。足すなら、「方向キーの 4 ビット → 4 つの bool」と「タッチ 1 本の 4 バイト → 生値」の 2 つの静的な補助だけを共有する（解析器の全体を 1 つにまとめない。機種ごとの読みやすさを優先する）。

### 4-8（調査の結果、問題なし）工場・Converter・束ね型（2026-11-11 追加調査）
- ViewModel と窓の工場（`UI/Services/*Factory*.cs` の 9 つ）は、どれも 40〜102 行で、作る物が違う。重複として直す所は見つからなかった。
- Converter は 4 つだけ（`UI/Converters` の 3 つと、`UI/IntToBoolConverter.cs`）。重複はない。`IntToBoolConverter` だけ `UI/` の直下にあり、置き場所が規則 R1（`UI/Converters`）と合わない（低。`Phase34-Plan.md` の A 部へ）。
- 依存を束ねただけの型（何でも箱）は、名前（`*Context`、`*Dependencies`、`*Services`、`*Bundle` など）で全体を検索しても見つからなかった。`ProfileLoadContext`／`ProfileSaveContext` は、読み込み・保存 1 回ぶんの値の入れ物で、DI の依存を隠す箱ではない（ただし `BackingStore` の全体を節へ渡している。`Structure-Review-2026-11.md` の A の範囲）。

## 3. 問題点（過剰な抽象）

### 4-5（低）実装が 1 つで、テストの偽物もないインターフェースが多い
- 事実（機械集計。数は要確認）: 本体のインターフェース 176 個のうち 82 個は、実装が 1 つで、テスト（`DS4WindowsTests`。モックのライブラリは使わず、偽物は手書き）に実装がない。
- 2 種類に分かれる。
  - **必要なもの**: 層をまたぐポート（第 2 層が第 3 層・UI を使う契約。例: `IOscService`、`IUdpMotionService`、`IOutputSlotService`、`IMainWindowFactory`）。偽物がなくても、依存の向きのために要る。
  - **差し替える場面がないもの**: 状態を持たない計算の部品。`IStickDeadZoneProcessor`、`IOutputCurveProcessor`、`ITriggerDeadZoneProcessor`、`IGyroControlsProcessor`、`IFlickStickProcessor`、`IGyroMouseStickBlender`、`IMouseMotionQuantizer`、`IButtonMouseMovementCalculator`、`ITwoStageTriggerProcessor`、`IControlActivityReader`（すべて `Processing/Abstractions`）。同じ層の中で使い、テストは実物を直接使う。毎フレーム、インターフェース越しの呼び出しになる。
- なぜ問題か: 読む人が「実装が複数ある」と期待して探す。型を足すたびに、契約のファイルも足す。
- 修正の方向: 規則に 1 行足す（同じ層の中だけで使う、状態を持たない計算の部品は、具象のまま注入してよい）。既存の撤去は急がない。

### 4-6（低）使われていない契約の候補
- 場所: `DS4Windows/Processing/Actions/IGatedOutputAction.cs`、`IResettableOutputAction.cs`、`IPendingUntriggerQuery.cs`、`IToggleFrameOutput.cs`、`Processing/Touchpad/ITouchpadBehaviour.cs`
- 事実: 本体での参照が 2〜3 か所で、実装は 1 つ。型の検査（`is`）のための印として使っているものもある。
- 修正の方向: 規則 2.4 の手順（経緯の調査 → 使うべきかの判断）に従って、1 つずつ判断する。即断で削除しない。
