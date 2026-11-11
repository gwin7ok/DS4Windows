# コードレビュー（2026-11）観点 6: パフォーマンスを阻害する処理がないか

作成日: 2026-11-11（対象のコミットと読み方は `00-Summary.md`）
読んだ経路: 入力ループ（`InputLoopDevice`、`XInputTransport`）→ 1 フレームの処理（`InputReportProcessor`、`LatencyMonitor`、`Mapping` の入口と特殊アクション、`StickInputConditioner`、`SyntheticInputCommitter`）→ 出力（`VirtualPadService`、`Xbox360OutDevice`）→ 画面の定期更新（トレイ、`ControllerReadingsControl`、`MainWindow` のタイマー）。
追加調査（2026-11-11）で読んだもの: `TouchpadMouse` のジャイロの処理、`TriggerDispatchEngine`、動き・タッチの取り込み（`DS4SixAxis`、`DS4Touchpad`）、解析器。
読めていないもの: `TouchpadMouse` のタッチの各出力（マウス、スティック、トラックボール）の計算の中身、`MouseCursor`、`OscService`、`UdpMotionService`。計測はしていない（コードの読み取りだけ）。

## 1. 良い点（維持すべき設計）

- `InputLoopDevice.ReadInput` の 1 フレームには、割り当てもロックもない（`DS4Windows/Input/Devices/InputLoopDevice.cs:113`〜`176`）。
- `XInputTransport` は、状態を 4 ms ごとに読むが、電池は間隔をあけ、持ち主の確認も 250 ms ごとにしている（`DS4Windows/Input/Xbox/XInputTransport.cs:24`、`145`）。待ちは、取り消せる待機で行う（同 `216`）。
- 仮想パッドへの送出は、割り当てもロックもない（`DS4Windows/Output/Services/VirtualPadService.cs:70`、`DS4Windows/Output/Devices/Xbox360OutDevice.cs:78`）。
- `Mapping`、`CurveAndDeadzoneStage`、`MouseConversionStage`、`LightbarService` の毎フレームの経路には、6-1 のほかに割り当てが見つからなかった（`DS4Color` は構造体。配列はコンストラクターで 1 度だけ作る）。
- 入力表示（Controller Readings）は 15 fps で、画面が見えないときはタイマーを止める（`DS4Windows/UI/ControllerReadingsControl.xaml.cs:260`、`286`）。
- トレイの電池アイコンは、値が同じなら通知しない（`DS4Windows/UI/ViewModels/TrayIconViewModel.cs:76`）。アイコンの名前は定数の連結で、割り当てがない。
- `AppHost`、`TriggerDispatchEngine` の一部などは、Trace のログを `IsTraceEnabled` で囲んでいる。
- `TriggerDispatchEngine`（2026-11-11 追加調査）: ロックは機器ごと（`Processing/Actions/TriggerDispatchEngine.cs:129`）で、全機器の共通の排他ではない。束縛の一覧は、差し替えのたびに不変の配列を作り（`ActionBindingRegistry.Replace`）、毎フレームは参照の比較だけで済ませる（同 `346`）。割り当ては差し替えの瞬間だけ。6-1 の修正の手本になる。

## 2. 問題点

### 6-1（中）特殊アクションを持つプロファイルで、毎フレーム配列を割り当てる
- 場所: `DS4Windows/Processing/Mapping.cs:692`（呼び出しは同 `349`〜`350`）
- 事実: `_profileActionProvider.GetProfileActionsRaw(device)?.ToArray()`。条件は「プロファイルに特殊アクションが 1 つ以上ある、または一時プロファイルを使っている」で、成り立つ間は毎フレーム（DS4 の USB で 1 秒に約 250 回、機器の数だけ）走る。コメントによれば、UI スレッドが一覧を変えても列挙が壊れないための写し。
- なぜ問題か: `01-Structure.md` §4 の「毎フレームの経路は割り当て 0 を保つ」に反する。`ControlServiceHotPathAllocationTests` は設定の getter だけを検査し、この経路を見ていない。
- 修正の方向: 一覧を「差し替えのたびに新しい不変の配列を作る」形にして、読む側は参照を 1 回読むだけにする。または、スロットごとの使い回しのバッファーへ写す。割り当て 0 のテストを、特殊アクションつきの `MapCustom` へ足す。

### 6-2（中）アンチスナップバックが有効なとき、毎フレーム割り当てる
- 場所: `DS4Windows/Processing/Services/StickInputConditioner.cs:93`〜`107`
- 事実: `queue.Any(oldValues => …)`。ラムダが 3 つのローカル（`axisXValue`、`axisYValue`、`delta`）を捕捉するので、毎回、捕捉用のオブジェクトとデリゲートを作る。`Queue<T>` を `IEnumerable<T>` として列挙するので、列挙子も箱詰めされる。スティックごとに呼ばれる。
- 修正の方向: `foreach` に書き直す（計算は同じ。値は変わらない）。

### 6-3（低。要確認）機器の誤りが続く間、毎フレーム利用者向けのログを出す
- 場所: `DS4Windows/Processing/Services/LatencyMonitor.cs:41`〜`45`
- 事実: `device.error` が空でなければ、そのフレームで `AppLogger.Log(Info, …, UserFacing)` を呼ぶ。`error` は、フレームの誤り（`frameError`）が空になるまで残る（`InputLoopDevice.cs:169`〜`172`）。
- 追加調査（2026-11-11）: `frameError` を立てるのは、タッチパッドの取り込みで例外が出たときだけ（`DS4Windows/Input/Devices/DualShock4Device.cs:739` と `DualSenseDevice.cs:673` の `catch`。ほかに立てる場所はない）。つまり、ログが続くのは、毎フレーム例外が出ている異常なときに限る。通常の使用では起きない。優先度は低いまま。
- 制約: ログの文言と出力は維持の対象。変えるなら、利用者に見えるログの変更として承認が要る（規則 2.3）。
- 修正の方向: 同じ文言が続く間は、最初の 1 回と、一定の間隔ごとにだけ出す。

### 6-4（低）20 ms のタイマーが、機能が無効でも回り続ける。別スレッドで UI のコレクションを列挙する
- 場所: `DS4Windows/UI/MainWindow.xaml.cs:243`（間隔 20 ms）、`593`〜`615`（`HotkeysTimer_Elapsed`）
- 事実: スワイプでのプロファイルの切替（`SwipeProfiles`）が無効でも、タイマーは止まらず、止めて始めるだけを 1 秒に 50 回くり返す。有効なときは、タイマーのスレッドで `conLvViewModel.ControllerCol`（UI のコレクション）を `foreach` する。
- 要確認: 列挙の最中に、UI スレッドがコレクションを変えると例外になる。実際に重なるかは未確認。
- 修正の方向: 設定が無効なら、タイマーを始めない。列挙は、UI スレッドで作ったスナップショット（スロット番号の配列）に対して行う。

### 6-5（低）Bluetooth の Xbox は、接続直後の 1 回、読み取りスレッドが最大 3 秒待つ
- 場所: `DS4Windows/Input/Xbox/XInputTransport.cs:172`、`188`、`DS4Windows/Input/Xbox/BluetoothBatteryReader.cs:49`〜`53`
- 事実: 最初の電池の読み取りで、GATT の値を最大 3 秒待つ（接続時の電池の表示が 0% になるのを避けるため、とコメントにある）。待つのは読み取りスレッドで、その間は入力が流れない。
- 修正の方向: 待たずに進め、値が届いた時点で反映する。接続時の表示を保ちたいなら、`PrimeBatteryForOtherDevice`（読み取りスレッドの開始前）で待つ。

### 6-6（低）30 秒ごとに、第 0 世代の GC を強制する
- 場所: `DS4Windows/App.xaml.cs:876`、`890`、`901`（`GC.Collect(0, GCCollectionMode.Forced, false)`）
- 事実: 経緯は未調査。毎フレームの割り当てを前提にした対策と見られる。
- 修正の方向: 6-1・6-2 を直したあと、割り当ての量を計測して、要るかどうかを決める。即断で削除しない。

### 6-7（中。事故のときは高）合成入力の確定のロックが、`try`／`finally` で守られていない
- 場所: `DS4Windows/Processing/Services/SyntheticInputCommitter.cs:43`（`EnterWriteLock`）、`205`（`ExitWriteLock`）。呼び出しは `InputReportProcessor.cs:221`（毎フレーム、機器ごと）
- 事実: 取得と解放の間（約 160 行）に、キーボード・マウスの送出（`_virtualKbm.PerformMouseButtonEvent` など）がある。`try`／`finally` はない。このロックを取る場所は、本体でここだけ（読み取りロックを取る場所もない）。
- なぜ問題か:
  1. 間の処理が例外を投げると、ロックが解放されない。次のフレームから、全コントローラーの入力スレッドが `EnterWriteLock` で止まる（同じスレッドなら再入の例外、別のスレッドなら待ち続ける）。
  2. 取る場所が 1 つだけなので、読み書きロックの意味がなく、全コントローラーを 1 本に直列化する排他になっている。送出の間も握り続ける。
- 修正の方向: まず `try`／`finally` で囲む（動作は変えない）。次に、排他の範囲を、共有の集計（`GlobalState`）の更新だけに絞れるかを調べる。

### 6-8（中）トグルが切り替わるたびに、レベルを見ずにスタックトレースを取る
- 場所: `DS4Windows/Processing/Actions/DefaultActionManager.cs:222`〜`242`（`SetToggledOn`）。呼び出しは `ButtonOutputAction.cs:207`、`KeyButtonActionController.cs:198`、`227`、`MacroAction.cs:80`
- 事実: 値が変わるたびに `new StackTrace(1, false)` を作り、フレームをたどり、`trace.ToString()` で文字列にする。Trace のログのためだが、`IsTraceEnabled` を見ていない。入力スレッドで動く。
- なぜ問題か: 切り替えの瞬間だけだが、スタックトレースの取得は 1 回が重い（割り当ても多い）。押した瞬間の 1 フレームが遅れる。
- 修正の方向: `IsTraceEnabled` の判定で囲む（文言は変わらない）。

### 6-9（低）Trace／Debug のログの文字列を、レベルの判定なしで組み立てる
- 場所: `DS4Windows/Processing/Services/SyntheticDispatcherService.cs:57`、`58`、`107`、`108`、`SyntheticInputCommitter.cs`（34 か所）、`Processing/Actions/KeyButtonActionController.cs`（9 か所）、`Processing/Touchpad/TouchpadMouse.cs:1114`〜`1167`（4 か所）
- 事実: 押し離し・クリック・タッチの始まりと終わりのたびで、毎フレームではない。キーのリピート中は、1 秒に約 30 回（`KeyboardSettings.RepeatCharsPerSec`）になる。
- 修正の方向: `if (AppLogger.IsTraceEnabled)`／`IsDebugEnabled` で囲む。文言は変えない。

### 6-10（中）ジャイロのトリガーの判定が、毎フレーム文字列を分割する（2026-11-11 追加調査）
- 場所: `DS4Windows/Processing/Touchpad/TouchpadMouse.cs:284`〜`340`（`IsGyroTriggerActive`）。呼び出しは同 `233`〜`281`（`sixaxisMoved`）、購読は `DS4Windows/Processing/Services/DeviceConnectionSetup.cs:199`
- 事実: 動きのフレームのたびに、トリガーの設定（例: `"-1"`、`"4,5"`）を `Split(',')` で配列にし、要素を `int.TryParse` で数にする。ジャイロの出力が Controls・Mouse・MouseJoystick のどれでも通る。既定は Controls（`Core/Settings/BackingStore.cs:121`）なので、ジャイロを持つ機種では、既定の設定で毎フレーム配列を割り当てる。
- 修正の方向: 設定の入れ物（`GyroControlsInfo.triggers` の setter、`Gyro.MouseTriggers`、`Gyro.MouseStickTriggers`）が、値を入れたときに数の配列へ直して持つ。毎フレームは配列を読むだけにする。保存する文字列は変えない。

### 6-11（中）動き・タッチのイベント引数を、毎フレーム割り当てる（2026-11-11 追加調査）
- 場所: `DS4Windows/Input/DS4SixAxis.cs:198`（`new SixAxisEventArgs`）、`DS4Windows/Input/Devices/SwitchProDevice.cs:525`（同）、`DS4Windows/Input/DS4Touchpad.cs:105`〜`240`（`new TouchpadEventArgs` が 14 か所）、`DS4Windows/Input/TouchpadEventArgs.cs:42`・`48`（`new Touch[2]`／`new Touch[1]`）
- 事実: 加速度が 0 でないフレーム（重力があるので、実質は毎フレーム）で、購読者がいれば（タッチパッドの処理が必ず購読する）、引数のオブジェクトを 1 つ作る。指が触れている間は、タッチの引数と配列も毎フレーム作る。どちらもクラス。
- なぜ問題か: 6-1・6-2・6-10 と合わせて、機器 1 台あたり毎フレーム 2〜5 個の小さな割り当てになる。30 秒ごとの GC の強制（6-6）は、この対策と見られる。上流（元の DS4Windows）から続く形。
- 修正の方向: 機器ごとに引数のオブジェクトを 1 つ持ち、中身を書き換えて使い回す。購読者が引数を保持しないことを先に確かめる（`TouchpadMouse` は `firstTouch = arg.Touches[0]` のように、タッチのオブジェクトを保持する箇所がある）。保持する側は、値を写して持つ形に直す。

### 6-12（中）自分で始め直すタイマーが、例外 1 つで止まったままになる（2026-11-11 追加調査。6-4 の続き）
- 場所: `DS4Windows/UI/MainWindow.xaml.cs:593`〜`615`（`HotkeysTimer_Elapsed`）、同 `754`〜`764`（`AutoProfilesTimer_Elapsed`）。タイマーは `System.Timers.Timer`（同 `35`）で、`AutoReset = false`
- 事実: どちらも、冒頭で `Stop()`、末尾で `Start()` を呼ぶ。間が `try`／`finally` で囲まれていない。`System.Timers.Timer` は、`Elapsed` の中の例外を握りつぶす（ランタイムの挙動。要確認）。したがって、間で例外が 1 回出ると、`Start()` に届かず、タイマーは止まったままになる。ログも出ない。
- 例外が出る経路: `HotkeysTimer_Elapsed` は、`conLvViewModel.ControllerCol` を、ロックを取らずに列挙する（同 `599`）。このコレクションは、ほかのスレッドが書き込みロック（`UI/ViewModels/ControllerListViewModel.cs:41` の `_colListLocker`）の下で変える（同 `130`〜`146`、`220`〜`226`）。着脱と列挙が重なると、列挙が例外になる。`AutoProfilesTimer_Elapsed` は、`CheckProfiles()` が例外を投げた場合。
- 結果: スワイプでのプロファイルの切替、または自動プロファイルが、アプリを再起動するまで黙って動かなくなる。
- 修正の方向: `try`／`finally` で囲み、`finally` で始め直す。例外は Error のログに出す（新しいログ。規則 2.3 に沿って書く）。列挙は、読み取りロックの下で取ったスナップショットに対して行う。

## 3. UI スレッドを塞ぐ処理

- 開始の手順に `Thread.Sleep(2000)` がある（`DS4Windows/Processing/Services/OutputServicesLifecycle.cs:54`）。安全弁として `LifecycleGuardTests` が固定している。呼ぶスレッドが UI スレッドでないことは、未確認（要確認）。
- 終了の手順は `Task.Run(…).Wait()` で、UI スレッドが待つ（`DS4Windows/App.xaml.cs:924`〜`933`）。終了時だけなので、問題にしない。
