# コードレビュー（2026-11）観点 4: 共通部品化が必要十分な範囲か

作成日: 2026-11-11（対象のコミットと読み方は `00-Summary.md`）
範囲の注意: 機種間は、デバイス本体（`Input/Devices`）の読み取り・スレッドの起動までを読み比べた。解析器どうし、各機種の `ProcessFrame`、ViewModel の工場どうし、Converter は未調査。

## 1. 良い点（維持すべき設計）

- 1 フレームの流れは `InputLoopDevice` に集まり、4 機種（`DualShock4Device`、`DualSenseDevice`、`SwitchProDevice`、`ProtocolDevice`）とも、差し込み口（`ReceiveFrame`／`IsExpectedReport`／`ProcessFrame`／`ProcessIdle`／`WriteOutputFrame`）だけを書いている（`DS4Windows/Input/Devices/InputLoopDevice.cs:102`）。
- 電池の段階 → % は `BatteryScale`、Xbox の接続の判定は `XusbConnectionClassifier` の 1 か所に集まっている。
- 特殊アクションの出力の組み立ては、種別ごとの部品の列（`ISpecialActionOutputBuilder` が 10 個）で、`switch` がない。

## 2. 問題点（重複）

### 4-1（中）「プロファイルに結んだプログラムを起動する」処理が 2 か所にある
- 場所: `DS4Windows/Processing/Services/DeviceConnectionSetup.cs:78`〜（`CheckLauchProfileOption`）と、`DS4Windows/Core/Persistence/ProfileSections/ProfileMiscLoadSection.cs:59`〜`110`
- 事実: どちらも、プロセスを列挙して実行ファイルのパスを比べ、見つからなければ `Task` の中で `Thread.Sleep(5000)` のあとに起動する。永続化の側だけが、先に `IProcessInspector` を試す。
- なぜ問題か: 片方だけ直すと動きがずれる。永続化の層に副作用がある（`02-Layers-and-Placement.md` の 2-1）。
- 要確認: 同じ場面（接続時のプロファイルの読み込み）で、両方が動いて二重に起動しないか。`DeviceConnectionSetup` は `startUp` が偽のときだけ、永続化の側は `context.LaunchProgram` が真のときだけ動く。
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
