# Phase9 提案書: SA ステアリング・ホイール・エミュレーション（ジャイロでハンドル操作）の削除（機能廃止）

作成日: 2026-10-22  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase9-Responsibility-Split-Plan.md` 9-A(2)、`.github/copilot-instructions.md` 2.2（機能廃止はユーザーの明示的な承認が要る）、2.4  
状態: **Q1〜Q4 を推奨どおり承認（2026-10-22）。実施は 9-A(2) の A→B→C の後（Q4＝A）。それまでコードは変えない**。検討の発端: 9-A(2) の設計説明のとき、ユーザーが「ステアリングはどういう機能か。内容によっては機能ごと削除してもよい」と発言（2026-10-22）。エージェントが自らの判断で削除するものではない。

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
- コントローラーごとの設定ファイル: `ControllerConfigsXmlSerializer`（**中身は車輪の較正値だけ**）、`IProfileXmlStore` の `LoadControllerConfigsForDevice`／`SaveControllerConfigsForDevice`、`ProfileXmlStore`、`PathService`／`IPathService` の該当パス、`ProgramFolderSettingsCleaner` の該当、`ControlService` の呼び出し（`DS4 接続時の読み込み`）、`ControllerRegisterOptionsWindow`／`ControllerRegDeviceOptsViewModel` の該当（要確認）。

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
| コントローラーごとの設定ファイル（`Controllers` の XML） | 較正値を読み書き | 読まない・書かない（ファイルは残る。削除しない） |
| vJoy への送出 | できる | できない（`UseDInputOnly` のときのステアリング限定の機能だった） |
| 通常のジャイロ機能（マウス、Controls の SX／SZ） | できる | **変わらない** |
| ログ | 較正のときの GUI ログあり | 該当ログも無くなる（機能ごと廃止のため） |

## 5. 作業（承認後。各 Wave の後に Debug ALL）

| Wave | 内容 |
|---|---|
| W1 | 割り当ての種別「Steering Wheel Calibrate」の削除（Controls／SA。画面、表、アクション、ポート）。旧データの扱いは Q3 に従う |
| W2 | 本体の削除（`Mapping.cs` のステアリング一式、`StickMotionState.WheelFilters`、`DS4Device` の車輪の状態、`DS4State.SASteeringWheelEmulationUnit`） |
| W3 | 出力の削除（`Xbox360OutDevice` ほか、`ControlService` の vJoy 送出、`VJoyFeeder`、コントローラーごとの設定ファイル）。vJoy の扱いは Q2 |
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
