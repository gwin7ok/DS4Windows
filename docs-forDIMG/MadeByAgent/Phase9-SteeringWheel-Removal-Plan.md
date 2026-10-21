# Phase9 提案書: SA ステアリング・ホイール・エミュレーション（ジャイロでハンドル操作）の削除（機能廃止）

作成日: 2026-10-22  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase9-Responsibility-Split-Plan.md` 9-A(2)、`.github/copilot-instructions.md` 2.2（機能廃止はユーザーの明示的な承認が要る）、2.4  
状態: **Q1〜Q4 を推奨どおり承認（2026-10-22）。9-A(2) の A→B→C は完了。W1（割り当て種別の削除）は実機確認も合格（2026-10-22）。W2（本体）・W3（出力と vJoy）・W4（設定・画面・翻訳）は実装済み（2026-10-22。W2〜W4 の実機確認待ち）。残りは W5（文書・Changelog）**。検討の発端: 9-A(2) の設計説明のとき、ユーザーが「ステアリングはどういう機能か。内容によっては機能ごと削除してもよい」と発言（2026-10-22）。エージェントが自らの判断で削除するものではない。

---

## 1. 目的

9-A(2) の対象のうち、**ステアリング（D 区分。約 450〜600 行）が、実機のコントローラー（`DS4Device`）・ライトバーの静的配列・`DateTime.Now`・vJoy に強く絡み、切り出しに実機確認が要る**。使用頻度が低いと思われ、削除すれば構造が単純になるため、削除を提案する。

## 2. 機能の説明

**「SA ステアリング・ホイール・エミュレーション」**（画面の表記は「360 Steering Wheel」）。コントローラーを車のハンドルのように傾け、その角度をアナログ軸の値にする。レースゲーム向け。

| 項目 | 内容 |
|---|---|
| 入力 | コントローラーのジャイロ（加速度の X・Z）から、360° の傾きの角度を計算する |
| 出力先 | 左スティックまたは右スティックの X／Y、L2＋R2 の軸（Xbox 360／DS4 の出力）、vJoy の仮想ジョイスティック（vJoy1／vJoy2 の X・Y・Z）の 11 通り |
| 回転範囲 | 90°〜1440°（360° を超える回転は、回転数を数えて再現する） |
| 較正 | 中心・右 90°・左 90° の 3 点を実機で取る。ライトバーの色（赤→黄→青→緑）で進み具合を示す。結果はコントローラーごとの設定ファイル（`Controllers` の XML）に保存する |
| 補助 | ファズ、デッドゾーン（SX のデッドゾーン）、スムージング（ユーロフィルター） |
| 画面 | プロファイル編集の「360 Steering Wheel」グループ（軸、範囲、Calibrate ボタン、ファズ、スムージング）。Special Action と Controls の種別「Steering Wheel Calibrate」（`SASteeringWheelEmulationCalibrate`。Phase10 で Controls 側にも実装） |

**ジャイロでマウスを動かす機能、ジャイロをスティック・ボタンに割り当てる通常の機能（`GyroOutMode.Controls` の SX／SZ の補正）は、別の機能で、残す**。

## 3. 調査: 削除の対象

### 3.1 本体（`Mapping.cs`）
- `Scale360degreeGyroAxis`（約 250 行）、`SAWheelEmulationCalibration`（約 125 行）、`CalculateControllerAngle`（約 55 行）、`CalcWheelFuzz`、`lastWheelGyroValues`／`LastWheelGyroCoord`、定数（`C_WHEEL_ANGLE_PRECISION`、`calibrationColor_0〜3`）、`LogToGuiSACalibrationDebugMsg`、`MapCustom` の呼び出し（`MappedState.SASteeringWheelEmulationUnit = this.Scale360degreeGyroAxis(...)`）。
- `StickMotionState.WheelFilters`（`OneEuroFilter`）。

### 3.2 コントローラー・状態
- `DS4Device` の車輪の較正用のフィールド一式（`wheelCenterPoint`、`wheel90DegPointLeft／Right`、`wheelCircleCenterPointLeft／Right`、`wheelPrevPhysicalAngle`、`wheelPrevFullAngle`、`wheelFullTurnCount`、`wheelPrevRecalibrateTime`、`WheelRecalibrateActiveState`、`WheelCalibrationPoint`、`wheelCalibratedAxisBitmask`）。
- `DS4State.SASteeringWheelEmulationUnit`（複製の 2 箇所も）。
- コントローラーごとの設定ファイル（`ControllerConfigs.xml`）の**車輪の較正値の部分だけ**（**訂正［2026-10-22。W2 の調査で判明］: このファイルは、車輪の較正値のほかに、コントローラーごとのオプション［`DS4Device.optionsStore`、コントローラー登録オプション画面の設定］も保存している。ファイル・`ControllerConfigsXmlSerializer`・`IProfileXmlStore` の読み書きの口は残し、車輪の部分［`wheelCenterPoint`／`wheel90DegPointLeft`／`wheel90DegPointRight`］だけを除く**）。除くものの一覧: `IProfileXmlStore` の `LoadControllerConfigsForDevice`／`SaveControllerConfigsForDevice`、`ProfileXmlStore`、`PathService`／`IPathService` の該当パス、`ProgramFolderSettingsCleaner` の該当、`ControlService` の呼び出し（`DS4 接続時の読み込み`）、`ControllerRegisterOptionsWindow`／`ControllerRegDeviceOptsViewModel` の該当（要確認）。

### 3.3 出力
- `Xbox360OutDevice`（軸の書き込み 11 分岐）、`DS4OutDeviceBasic`、`DS4OutDeviceExt`、`OutputDevice`、`OutputSlotManager` の `GetSteeringWheelEmulationAxis` の取得口。
- `ControlService` の vJoy への送出（`UseDInputOnly` のときの `VJoyFeeder` 呼び出し）。
- **vJoy 本体**: `DS4Windows/VJoyFeeder/` の 8 ファイル（`vJoyFeeder`、`VJoy`、`HID_USAGES` ほか）。**vJoy は、ステアリングの出力先にしか使われていない**（画面の「About」の vJoy のリンクを除く）。

### 3.4 設定・画面
- `BackingStore` の `sASteeringWheelEmulationAxis`／`Range`、`saWheelFuzzValues`、`wheelSmoothInfo`、`DEFAULT_SA_WHEEL_EMULATION_RANGE`、`Reset` の該当。`SASteeringWheelEmulationAxisType`、`SteeringWheelSmoothingInfo`。
- `IProfileSettingsService`／`ProfileSettingsService` の該当、`ProfileGyroControlsLoadSection`／`ProfileMotionSaveSection`（プロファイル XML の読み書き）、`ProfileSettingsViewModel`（軸・範囲・ファズ・スムージング）、`ProfileEditor.xaml(.cs)` の「360 Steering Wheel」グループ。
- 種別「Steering Wheel Calibrate」: `SteeringWheelCalibrateAction`、`IWheelCalibrationPort`／`DefaultWheelCalibrationPort`、`SpecialActionOutputFactory`／`ActionBindingBuilder`／`ControlsSpecialTypeTable`／`SpecialActionItemMapper`／`ControlSpecialItemXml`／`ActionTypeCatalog`／`MappedControl`／`AssignmentEditor.xaml(.cs)`／`SpecialAction`／`SpecialActionsXmlSerializer`／`ActionsDTO` の該当。
- 翻訳の文字列: `Resources*.resx`、`Strings*.resx`（14 言語）の該当キー。

### 3.5 テスト
関連するテスト（約 30 ファイル。`Phase8Step76dSteeringWheelCalibrateTests`、`RecordingWheelCalibrationPort`、`Phase10*` の種別表のテスト、ガード系のテスト［ラチェットの基準値、`Step12DeadShimRemovalGuardTests` など］の該当行）。削除するテストは、廃止した機能の検査だけに限る（挙動の特性化テストは、残る機能の分を削らない）。

## 4. 挙動の変化（承認の前提）

| 項目 | 変更前 | 変更後 |
|---|---|---|
| ジャイロでハンドル操作（11 の出力先、較正、回転範囲、ファズ、スムージング） | できる | できない（機能の廃止） |
| プロファイル（XML）に `SASteeringWheelEmulationAxis` などの要素がある | 読み込む | **要素を無視する**（他の設定には影響しない。次に保存するとき、要素は書かれない） |
| Special Action／Controls に「Steering Wheel Calibrate」がある | 実行できる | **要確認（Q3）**: 種別が不明な割り当てとして、読み込み時に取り除くか、そのまま保持するか |
| コントローラーごとの設定ファイル（`ControllerConfigs.xml`） | 較正値と、コントローラーごとのオプションを読み書き | **較正値［車輪の 3 要素］だけ**読まない・書かない（オプションの読み書きは、これまでどおり）。古いファイルの車輪の要素は無視され、次の保存で書かれなくなる |
| vJoy への送出 | できる | できない（`UseDInputOnly` のときのステアリング限定の機能だった） |
| 通常のジャイロ機能（マウス、Controls の SX／SZ） | できる | **変わらない** |
| ログ | 較正のときの GUI ログあり | 該当ログも無くなる（機能ごと廃止のため） |

## 5. 作業（承認後。各 Wave の後に Debug ALL）

| Wave | 内容 |
|---|---|
| W1 | 割り当ての種別「Steering Wheel Calibrate」の削除（Controls／SA。画面、表、アクション、ポート）。旧データの扱いは Q3 に従う |
| W2 | 本体の削除（`Mapping.cs` のステアリング一式、`StickMotionState.WheelFilters` と `ControlService` のフィルター作成、`DS4Device` の車輪の状態、`ControllerConfigsXmlSerializer` の車輪の 3 要素、プロファイル編集の Calibrate ボタンとその処理）。`DS4State.SASteeringWheelEmulationUnit` は、出力側が読むため W3 まで残す［W2 の後は、常に 0］ |
| W3 | 出力の削除（`Xbox360OutDevice` ほか、`DS4State.SASteeringWheelEmulationUnit`、`ControlService` の vJoy 送出、`VJoyFeeder`）。vJoy の扱いは Q2 |
| W4 | 設定・画面の削除（`BackingStore`、プロファイル XML の読み書き、`ProfileEditor` の「360 Steering Wheel」グループ、翻訳の文字列）。ラチェットの基準値の更新 |
| W5 | 文書の更新（`Model-Diagram/03`、`Phase9-Responsibility-Split-Plan.md`、`Phase10-*` の該当）と完了報告。`Changelog` の追記 |

## 6. 質問（推奨は A）

- **Q1（削除してよいか）**: A＝削除する（推奨）／B＝残して、独立した Wave で分割する。
- **Q2（vJoy）**: A＝ステアリングと一緒に `VJoyFeeder` も削除する（推奨。ステアリング以外に使われていない）／B＝vJoy は残す（ステアリングの出力先としては使われなくなり、未使用のコードが残る。2.4 の方針に合わない）。
- **Q3（旧データの「Steering Wheel Calibrate」の割り当て）**: A＝読み込み時に取り除く（Actions.xml／プロファイルの該当エントリは、次の保存で消える。ログ［`[DI]` 接頭辞付き］で通知する）（推奨）／B＝そのまま保持し、実行しない（読み込めるが何もしない種別が残る）。
- **Q4（時期）**: A＝9-A(2) の A→B→C（ジャイロ → コントロール、スティックの前処理、フリックスティック）の**後**に実施する（推奨。ステアリングは A〜C と独立で、先に A〜C を済ませたほうが差分が小さい）／B＝9-A(2) の前に実施する。

## 7. リスクと対応

| リスク | 対応 |
|---|---|
| 削除の範囲が広く、取りこぼしや、他の機能の巻き込みが起きる | 上の一覧を W ごとの完了条件にする。`rg`（検索）で、車輪・ステアリング・vJoy の残りが 0 件になることを確認する。通常のジャイロ機能は、ゴールデンと既存のテストで守る |
| 古いプロファイル・Actions.xml の読み込みが壊れる | 読み込みは、不明な要素を無視する作りかを、W1・W4 のテストで確認する（旧形式のファイルを読ませるテスト）。Q3 の扱いも、テストで固定する |
| 使っている人がいる | **提案を承認する前に、ユーザーが利用の有無を確認する**。削除は、ユーザーの承認が前提 |

## 8. 完了条件

- 上の一覧のうち、削除対象のコードが 0 件になる（コメントの履歴を除く）。
- 通常のジャイロ機能、ゴールデン、割り当て 0、ラチェットの一致検査が、変わらず合格する。
- `Debug ALL` が 0 警告・0 エラー・全テスト合格。
- 実機確認: プロファイル編集の「Gyro → Controls」で、「360 Steering Wheel」が無くなり、他の項目が崩れていないこと。古いプロファイルを開けること。

## 9. 次

承認（Q1〜Q4）を得たら、W1 から着手する。承認までは、9-A(2) の A（ジャイロ → コントロール）→ B（スティックの前処理）→ C（フリックスティック）を先に進める。

## 10. 決定事項（2026-10-22、ユーザー承認）

- Q1＝A: ステアリング機能（SA ステアリング・ホイール・エミュレーション）を削除する（機能廃止の明示承認）。
- Q2＝A: vJoy（`VJoyFeeder`）も一緒に削除する。
- Q3＝A: 旧データの「Steering Wheel Calibrate」の割り当ては、読み込み時に取り除く（`[DI]` 接頭辞付きのログで通知）。
- Q4＝A: 9-A(2) の A（ジャイロ → コントロール）→ B（スティックの前処理）→ C（フリックスティック）の**後**に実施する。それまで、ステアリングのコードは変えない。9-A(2) からは D（ステアリング）を外す。

## 11. W1 の結果（2026-10-22。割り当て種別「Steering Wheel Calibrate」の削除）

| 区分 | 内容 |
|---|---|
| 削除 | `Actions/SteeringWheelCalibrateAction.cs`、`Actions/IWheelCalibrationPort.cs`、`Actions/DefaultWheelCalibrationPort.cs`。テスト: `Phase8Step76dSteeringWheelCalibrateTests.cs`、`RecordingWheelCalibrationPort.cs`、`Phase10Step4W4ControlSpecialTests` の「SixaxisWheelCalibration_...」ほか、種別を網羅するテストのステアリングの項目 |
| 種別 | `ActionTypeId` から `SASteeringWheelEmulationCalibrate` を除いた（保存は名前［文字列］のため、数値の並びの変化は保存形式に影響しない）。`SpecialAction` の文字列の分岐、`SpecialActionOutputFactory`（フィールド・引数・case）、`ActionBindingBuilder`（引数）、`ControlsSpecialTypeTable`、`SpecialActionItemMapper`、`ControlSpecialItemXml.AllowedTypes`、`SpecialActionsXmlSerializer`（作成・書き込みの case 8）、`ActionsDTO`（書き込み・読み込み）、`ServiceRegistration`（`IWheelCalibrationPort`）から除いた |
| 画面 | `AssignmentEditor.xaml`: 種別の一覧の「Sixaxis Wheel」の項目と、専用のタブ（`sixaxisWheelCalibrateTab`）を除いた。`AssignmentEditor.xaml.cs`: `saSteeringWheelVM`（生成、辞書、タブの `DataContext`）を除いた。`ActionTypeCatalog`: 添字 8 の項目を除き、Gyro Calibration を 9→8 に詰めた（Default 10→9、Unbound 11→10 は、項目数から自動で詰まる）。`MappedControl`: 表示名の分岐を除いた。`SpecialAction` の保存の「mode」番号（Gyro＝9 など）は、`SpecialActionViewModel` の生成引数と `SpecialActionsXmlSerializer` の case で固定のため、変えていない（8 は欠番） |
| 旧データ（Q3＝A） | **Actions.xml**: `ActionsDTO` の読み込みで、種別 `SASteeringWheelEmulationCalibrate` の項目を一覧に入れず、`[DI] 廃止された割り当て種別 'SASteeringWheelEmulationCalibrate'（Special Action: <名前>）を、読み込み時に取り除きました。` をログに出す。**プロファイルの Controls**: `ControlSpecialItemXml.TryRead` が、その種別を読み込まずに `[DI] …（プロファイルの Controls: <ボタン>）…` をログに出す（そのボタンは既定に戻る）。どちらも、次の保存でファイルから消える |
| テスト（新規） | `Phase9SteeringW1LegacyAssignmentTests`（3 件）: 旧 Actions.xml の項目が取り除かれ、ほかの項目が読み込まれ、ログが出ること／旧プロファイルの Controls の割り当てが取り除かれ、ほかの割り当てが残り、ログが出ること／種別が enum から消え、文字列で作ると None になること |
| 追従したテスト | 種別を網羅するテスト（カタログ、エディターの集合、SA の束縛の表、出力の工場、マッパー、Controls の種別表、監査ほか）から、ステアリングの項目を除いた。添字（Gyro 9→8、Default 10→9、Unbound 11→10、項目数 10→9、12→11）を更新。`PersistenceHarness` のコメント（モード 8）を更新 |

`Debug ALL`: 警告 0・エラー 0・全テスト合格（2,240 件、StandaloneTests 11 件）。

**実機確認（依頼）**: 割り当て編集画面（SA のモード／Controls のモード）で、(1) 種別の一覧に「Sixaxis Wheel」の項目がなく、並びが「種別を選んでください／Macro／Launch Program／Load Profile／Output Key…／Disconnect BT／Check Battery／Multi-action／Calibrate Gyro（／Default／Unbound）」であること。(2) Calibrate Gyro を選んで保存でき、再び開くと同じ種別が選ばれること。(3) 各種別（Default、Unbound を含む）を選んだときの入力欄・タブが、これまでどおりであること。(4) 古い Actions.xml に Steering Wheel Calibrate の項目がある場合、起動時に GUI ログへ `[DI] 廃止された割り当て種別…` が出ること（任意）。なお、プロファイル編集の Calibrate ボタンによる較正は、W4 まで従来どおり動く。

**W1 の実機確認（2026-10-22）: 合格。** ユーザーが、Controls と Special Actions の両方で確認し、問題なし。

## 12. W2 の結果（2026-10-22。本体の削除）

**訂正（W2 の調査で判明）**: 提案書の §3.2 に「`ControllerConfigs.xml` の中身は車輪の較正値だけ」と書いたのは誤りで、このファイルは、コントローラーごとのオプション（`DS4Device.optionsStore`）も保存している。ファイルと読み書きの口は残し、**車輪の 3 要素だけ**を除いた（§3.2・§4・§5 を訂正済み）。

| 区分 | 内容 |
|---|---|
| `Mapping.cs` | 「SA steering wheel emulation mapping」一式を削除: `Scale360degreeGyroAxis`、`SAWheelEmulationCalibration`、`CalculateControllerAngle`、`CalcWheelFuzz`、`CountNumOfSetBits`、`LogToGuiSACalibrationDebugMsg`、`C_WHEEL_ANGLE_PRECISION`、`calibrationColor_0〜3`、`LastWheelGyroCoord`／`lastWheelGyroValues`、`wheelFilters`、`MapCustom` の呼び出し。`Mapping.cs`: 3,072 → 2568 行 |
| 状態 | `StickMotionState.WheelFilters`、`ControlService.SetupInitialHookEvents` のステアリング用フィルターの作成と、スムージング設定との連動 |
| コントローラー | `DS4Device` の車輪の較正の状態一式（`wheelCenterPoint` ほか）と `WheelCalibrationPoint` |
| ファイル | `ControllerConfigsXmlSerializer`: 車輪の 3 要素（`wheelCenterPoint`／`wheel90DegPointLeft`／`wheel90DegPointRight`）の読み書きを削除。Mac・ControllerType の属性と `optionsStore` の読み書きは残した。古いファイルの車輪の要素は無視され、次の保存で書かれなくなる |
| 画面 | プロファイル編集の「Calibrate」ボタンとその処理（`SteeringWheelEmulationCalibrateBtn_Click`） |
| 残したもの | `DS4State.SASteeringWheelEmulationUnit`（出力デバイスが読むため W3 まで。W2 の後は常に 0）、ステアリングの設定（軸、範囲、ファズ、スムージング）とその入力欄（W4 まで。W2〜W3 の間は設定しても何も起きない） |
| テスト | 新規 `Phase9SteeringW2ControllerConfigsGuardTests`（2 件。オプションと属性の読み書きが残り、車輪の 3 要素が読み書きされないこと［ソースの走査。`DS4Device` は実機が要るため挙動のテストは作れない］）。追従: `Phase7Step7StickMotionStateGuardTests`（`WheelFilters`）、`MappingControlPathGlobalReferenceGuardTests`（検査対象から `Scale360degreeGyroAxis` を除く）、`Phase7Step8W2MappingInstanceGuardTests`（static の許可表から `calibrationColor_0〜3` を除く） |

`Debug ALL`: 警告 0・エラー 0・全テスト合格（2,242 件、StandaloneTests 11 件）。ラチェットの一致検査（`Global`／`AppHost` ほか）は、基準値の変更なしで合格。

**実機確認（依頼）**: (1) 起動して、コントローラーを接続できること（`ControllerConfigs.xml` の読み込みが壊れていないこと）。(2) コントローラー登録オプション画面で、オプションを変更して保存し、再起動しても保持されること。(3) プロファイル編集の画面が開き、通常のジャイロ機能（Controls へのジャイロ出力、マウス）が従来どおり動くこと。

## 13. W3 の結果（2026-10-22。出力と vJoy の削除）

| 区分 | 内容 |
|---|---|
| 出力デバイス | `Xbox360OutDevice`／`DS4OutDeviceBasic`／`DS4OutDeviceExt` の `ConvertandSendReport` から、ステアリングの軸の `switch`（LX／LY／RX／RY／L2R2／vJoy の分岐）を除き、通常の軸（左右スティックの X・Y）の出力だけを残した（軸が「None」だった利用者の出力は変わらない）。トリガー（L2／R2）の代入は、元から `switch` の前にあった |
| 取得口 | `OutputDevice.GetSteeringWheelEmulationAxis`／`SteeringWheelAxisProvider`、`OutputSlotManager` のコンストラクター引数（`steeringWheelAxisProvider`）と設定の 1 行、`ControlService` の生成の引数を削除 |
| `ControlService` | 出力デバイスがないとき（`else` 側。`UseDInputOnly`）の vJoy 送出を削除 |
| `DS4State` | `SASteeringWheelEmulationUnit`（宣言、`Reset`、コピー 2 箇所）を削除 |
| vJoy | `DS4Windows/VJoyFeeder/`（8 ファイル、852 行）を削除（Q2＝A） |
| 画面 | About 画面の「vJoyInterfaceCS」の表記と、リンクのクリック処理を削除 |
| 残したもの | ステアリングの設定とその入力欄（軸、範囲、ファズ、スムージング）、`SASteeringWheelEmulationAxisType`、`IProfileSettingsService` の該当（W4 で削除） |
| テスト | 新規 `Phase9SteeringW3OutputDeviceGuardTests`（7 件。出力デバイス 3 つに通常の軸の出力が残り、ステアリング・vJoy の参照がないこと／`ControlService`・`OutputDevice`・`OutputSlotManager`・`DS4State` に vJoy とステアリングの参照がないこと［ソースの走査］）。追従: `OutputSlotManager` の生成（7 箇所）、`Phase7Step10dW3DeviceLayerSettingsGuardTests` の null 検査 |

`Debug ALL`: 警告 0・エラー 0・全テスト合格（2,249 件、StandaloneTests 11 件）。

**実機確認（依頼）**: (1) ゲームパッドとして、スティック・トリガー・ボタンが従来どおり出力されること（Xbox 360 出力、DS4 出力のそれぞれ）。(2) About 画面が開き、表示が崩れていないこと。

## 14. W4 の結果（2026-10-22。設定・画面・翻訳の削除）

| 区分 | 内容 |
|---|---|
| 型・設定 | `SASteeringWheelEmulationAxisType.cs`、`SteeringWheelSmoothingInfo.cs` を削除。`BackingStore`: `sASteeringWheelEmulationAxis`／`Range`、`saWheelFuzzValues`、`wheelSmoothInfo`、`DEFAULT_SA_WHEEL_EMULATION_RANGE`、`Reset` の該当 4 行（計 20 行）を削除。`IProfileSettingsService`／`ProfileSettingsService`: 7 メンバーを削除 |
| プロファイル XML | `ProfileGyroControlsLoadSection`（読み込み）と `ProfileMotionSaveSection`（書き込み）から、`SASteeringWheelEmulationAxis`／`SASteeringWheelEmulationRange`／`SASteeringWheelFuzz`／`SASteeringWheelSmoothingOptions`（`SASteeringWheelUseSmoothing`／`SmoothMinCutoff`／`SmoothBeta`）を削除。書かれなくなった。古いプロファイルに要素が残っていても、無視される。欠けていても `missingSetting` にならない |
| 画面 | `ProfileEditor.xaml`: 「360 Steering Wheel」のグループ（軸・範囲・ファズ・スムージング）を削除。`ProfileSettingsViewModel`: 該当のプロパティ・イベント（約 90 行）と、`CalcProfileFlags` への登録を削除 |
| 翻訳 | `Resources*.resx`（4 言語）の 7 キー、`Strings*.resx`（15 言語）の 2 キー（`SASteeringWheelFuzzTooltip`、`SpecialActionEdit.SixAxisWheelCalib`）、`Resources.Designer.cs`（7 プロパティ）・`Strings.Designer.cs`（2 プロパティ）を削除（エンコード［BOM］と改行は保持。`resx` の差分は削除のみ） |
| 残したもの | `OneEuroFilterPair`／`OneEuroFilter3D` の `DEFAULT_WHEEL_*` の定数（名前は WHEEL だが、ジャイロのフィルターの既定値として他の機能も使う）。旧データの取り除き（`ActionsDTO`、`ControlSpecialItemXml`）とそのログ |
| ゴールデン | 保存形式の意図した変更として更新（`Phase7Step10c/README.txt` が許す、唯一のケース）: `profile-default.xml`、`profile-gap.xml／.state.txt／.resaved.xml`、`profile-legacy.state.txt／.resaved.xml`、`profile-minimal.rewritten.xml`、`profile-varied.xml／.state.txt`。**差分は削除のみ（追加 0 行。削除は、上の 4 要素と、状態の出力の該当フィールド）**を `git diff` で確認してから採用した |
| テスト | 新規 `Phase9SteeringW4LegacyProfileTests`（2 件: 保存した XML にステアリングの要素がないこと／4 要素を含む旧プロファイルが、ほかの設定を壊さずに読まれ、再保存で要素が消えること）。追従: `ProfileLegacyXmlCompletenessTests`（期待する要素の一覧から 4 要素を除く［162→158］）、`Phase8Step4bProfileGoldenGapTests`、`ControlServicePr4ShimEquivalenceTests` |

`Debug ALL`: 警告 0・エラー 0・全テスト合格（2,251 件、StandaloneTests 11 件）。残りの参照は、旧データの取り除き（意図）と、削除を検査するテストだけ。

**実機確認（依頼）**: (1) プロファイル編集の画面が開き、「Gyro → Controls」画面に「360 Steering Wheel」のグループがなく、ほかの項目が崩れていないこと。(2) 既存のプロファイルを開いて保存し、再び開いても問題ないこと。(3) 表示言語を切り替えて（例: 日本語と英語）、画面が開くこと。
