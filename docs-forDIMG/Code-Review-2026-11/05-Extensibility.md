# コードレビュー（2026-11）観点 5: 将来の機能・デバイスの追加に対応できる構造か

作成日: 2026-11-11（対象のコミットと読み方は `00-Summary.md`）
方法: `docs-forDIMG/Model-Diagram/02-Extension-Guide.md` の手順を机上で追い、機種の種類（`InputDeviceType`／`InputDeviceFamily`）、機種の具象型、出力の種類（`OutContType`）を本体全体で検索して、手順書にない場所を洗い出した。

## 1. 良い点（維持すべき設計）

- 入力機種の名指しは、カートリッジ・モジュール・設定クラスの外では 5 か所だけだった（下の 5-2、5-3）。
- 機種の登録は 1 行（`ServiceRegistration.Processing.cs:128`〜`131`）、系統の設定は表に 1 行（`DS4Windows/Core/Settings/ControlServiceDeviceOptions.cs:37`〜`40`）。
- HID を持たない Xbox を、`IHidTransport` と `IInputTransportSource` に載せて、同じ流れで扱えている。
- `IProfileSettingsService` は約 54 メンバー（`Structure-Review-2026-11.md` の時点は 158）。領域の窓口（`ForSlot`）への移行が進んでいる。

## 2. 机上で追った結果

### 2.1 入力機種を足す
手順書のとおりに足せる。手順書にない作業は、次の場合にだけ出る。

| 場合 | 触る場所 |
|---|---|
| 電池を、読み取りスレッドの開始前に読みたい | `InputDeviceCore.PrimeBatteryReading`（`DS4Windows/Input/InputDeviceCore.cs:542`）。DS4 かどうかで分岐している |
| 出力を止める処理が要る | `InputDeviceCore.WriteOutputOffOnce`（同 `262`）。DS4 だけが対象 |
| 機種だけの振動の設定を画面から変える | `ControllerSlotCommands.ApplyDualSenseRumbleOptions`（`DS4Windows/Processing/Services/ControllerSlotCommands.cs:52`）のように、機種の名前がついた操作を足すことになる |

### 2.2 出力の種類を足す
手順書の表（カートリッジ、`OutContTypeInfo`、DI 登録、バックエンド）の 4 つでは足りない。5-1 の 8 か所以上を触る。

### 2.3 プロファイルの設定の項目を 1 つ足す
`AntiSnapbackTimeout`（スティックのアンチスナップバックの時間）で実測した。

| # | 触る場所 | ファイル |
|---|---|---|
| 1 | 値の入れ物 | `StickAntiSnapbackInfo`（`Core/Settings`） |
| 2 | 読み込みの節 | `Core/Persistence/ProfileSections/ProfileStickOutputLoadSection.cs` |
| 3 | 保存の節 | `Core/Persistence/ProfileSections/ProfileOutputCurveSaveSection.cs` |
| 4 | ViewModel（左右で 2 か所） | `UI/ViewModels/StickSettingsViewModel.cs:304`、`310` |
| 5 | XAML（左右で 2 か所） | `UI/StickSettingsPanel.xaml` |

既定値・リセット・プリセット（`BackingStore` のリセット、`ProfilePresets`）に入れる場合は、さらに 1〜2 か所。

### 2.4 機種ごとの設定の項目を 1 つ足す
`ControllerSetting` の定義に 1 行と、文言のリソース（`Strings.resx`／`Strings.ja.resx`）。保存・読み込み・画面は定義から作られる。例外は 5-3。

## 3. 問題点

### 5-1（中）出力の種類の分岐が、カートリッジと `OutContTypeInfo` の外に残る
- 場所:

| 場所 | 内容 |
|---|---|
| `DS4Windows/Output/OutputSlotManager.cs:158` | `contType == OutContType.X360` |
| `DS4Windows/Output/Services/OutputSlotView.cs:50`、`55` | Xbox 360 のときだけ XInput の番号を返す（`is Xbox360OutDevice`） |
| `DS4Windows/Output/Services/VirtualPadService.cs:135`〜`154` | 起動時の一時的な Xbox 360 の差し込み |
| `DS4Windows/Processing/Services/InputReportProcessor.cs:170` | DS4 型のときのトリガーのボタン化 |
| `DS4Windows/Processing/Services/IpcCommandProcessor.cs:207`、`209` | 文字列から種類への変換 |
| `DS4Windows/Processing/Macro/MacroParser.cs:101` | マクロの値の表示名 |
| `DS4Windows/Core/Model/X360ControlTables.cs:170`、`174` | ボタン名の表の選択 |
| `DS4Windows/UI/ViewModels/BindingWindowViewModel.cs:84` | `use360Mode`（二択の bool） |
| `DS4Windows/UI/ViewModels/SlotDeviceEntry.cs:128` | 表示の切り替え |
| `DS4Windows/UI/ViewModels/OutputSettingsViewModel.cs:35`、`56` | 既定の種類 |

- なぜ問題か: 規則 3.1（種類ごとの `if`／`switch` を増やさない）の対象が、新しく書く分だけで、既存の分が残っている。3 つ目の種類を足すと、手順書にない場所を全部触る。`use360Mode` は、種類が 2 つである前提の型になっている。
- 修正の方向: 種類ごとの差を、`OutContTypeInfo` の項目（ボタン名の表、IPC の名前、既定かどうか）と、カートリッジの操作（送出の直前の後処理、表示用の番号）にする。

### 5-2（中）全機種の基底・第 2 層・Core に、機種の名指しが残る
- 場所:
  - `DS4Windows/Input/InputDeviceCore.cs:262`（`deviceType != InputDeviceType.DS4`）、`542`（同）
  - `DS4Windows/Processing/Services/ControllerSlotCommands.cs:52`（`is DualSenseDevice`）
  - `DS4Windows/Core/Settings/ProfileSettingsService.cs:256`（`DeviceType == InputDeviceType.DualSense`）
- なぜ問題か: 基底が派生の種類を知っている。第 2 層は、第 1 層をインターフェース経由で使う規則（1 章）なのに、具象の型で分岐している。`Core` の設定が、機種の振動の事情を知っている。
- 修正の方向: 基底の 2 つは仮想メソッドにし、DS4 が中身を持つ。DualSense の振動の設定は、`IInputDeviceModule` の差し込み口（`ApplyProfileOptions` の仲間）へ寄せる。`GetRumbleBoost` の特例は、機種が「振動の強さの調整を受けるか」を返す形にする。

### 5-3（低）Xbox の設定 1 つだけ、画面が手書き
- 場所: `DS4Windows/UI/ViewModels/SettingsViewModel.cs:55`〜`58`（`XboxDeviceOptions.HideUsbParent`）
- 事実: ほかの機種の設定は `ControllerSetting` の定義から画面が作られるが、これだけ ViewModel が系統と型を名指しする。
- 要確認: HidHide が未導入のときの赤文字の案内のために、手書きにした可能性がある。
- 修正の方向: 定義（`WithUi`）で表せるなら寄せる。表せないなら、理由をコメントに書く。

### 5-4（低）手順書・モデル図が古い
- 場所と内容:
  - `docs-forDIMG/Model-Diagram/02-Extension-Guide.md:9`「1 系統あたり約 28 ファイル」（Phase17 の前の実測）。同 `:3` の「最終形は Phase17-4 で確定する」。
  - `02-Extension-Guide.md` §2 の表に、5-1 の場所がない。
  - `01-Structure.md:57`〜`62` のフォルダーの表に、`Input/Xbox`、`Processing/Touchpad`・`Gyro`・`Macro`・`Synthetic`、`Processing/Actions/OutputBuilders` がない。
  - 引数の基準が「15 以下」（`01-Structure.md:134`、`02-Extension-Guide.md:48`、`03-Lifecycle-and-DI.md:38`）。規則は 12。
- 修正の方向: 文書だけの更新。5-1・5-2 を直したあとに、手順を書き直すのがよい。

### 5-5（低）読み込みと保存の節の分け方が、対になっていない
- 事実: 読み込みは 15 の節、保存は 7 の節で（`Core/Persistence/ProfileSections`）、同じ項目が別の名前の節にある（2.3 の表の 2 と 3）。
- なぜ問題か: 項目を足す場所を探しにくい。片方だけ足す事故のもとになる（保存の黄金テストが守ってはいる）。
- 修正の方向: 急がない。`Structure-Review-2026-11.md` の A（設定のオブジェクト化）の続きで、領域ごとに読み書きを 1 つの型へ寄せるときに揃える。
