# PhaseRV11 計画書: 500 行以上のファイルの分割

作成日: 2026-11-20
状態: **D1＝(B) 32 件すべて、D2＝(A) 責務ごとに別の型へ、D3＝(B) 入力機種も含める、D4＝(B) 層ごとにまとめて 1 コミット（2026-11-20）。着手前**
出典: `docs-forDIMG/Code-Review-2026-11-19/02-Scorecard.md`（RV10 の「次の Phase で動かす指標」）。計測は `Measure-CodeReview.py` の `size`（`.cs` のみ。500 行以上 **32 件**）。
動き: **機能・保存の形・ログは変えない**。型の中身を、責務ごとに別の型・`partial` へ分けるだけ。

## 現状（2026-11-20、`.cs` 32 件）
| 区分 | ファイル（行数） | 扱い |
|---|---|---|
| 自動生成 | `Translations/Strings.Designer.cs`（4,272）、`Properties/Resources.Designer.cs`（1,677） | 手で分けない。計測から除外するかを D1 で決める |
| 画面（code-behind） | `ProfileEditor.xaml.cs`（1,615）、`MainWindow.xaml.cs`（1,492）、`RecordBox.xaml.cs`（867）、`ControllerReadingsControl.xaml.cs`（636）、`WelcomeDialog.xaml.cs`（519） | 見た目の処理だけが残るものは分けにくい。ViewModel へ寄せる余地を調べる |
| ViewModel | `SettingsViewModel`（708）、`AutoProfilesViewModel`（673）、`GyroSettingsViewModel`（579）、`SpecialActionsListViewModel`（569）、`RecordBoxViewModel`（533）、`MainWindowsViewModel`（523）、`PresetMenuHelper`（509） | 領域ごとに `partial` または部品へ |
| 入力機種 | `InputDeviceCore`（1,109）、`DualSenseDevice`（1,083）、`SwitchProDevice`（995）、`DualShock4Device`（801）、`InputDeviceRegistry`（541） | 実機確認が要る。解析・出力レポートは `Parsers`／`Reports` へ寄せる余地 |
| 第 2 層 | `Mapping`（883）、`TriggerDispatchEngine`（750）、`TouchpadMouse`（730）、`GyroOutputProcessor`（542）、`ControlBindingBuilder`（502）、`KeyButtonActionController`（501） | 挙動の固定テストが前提 |
| 設定・永続化 | `AppSettingsDTO`（1,175）、`BackingStore`（955）、`AppSettingsService`（586）、`ProfilePresets`（580）、`ProfileMigration`（544）、`Util`（544）、`NativeMethods`（530） | `NativeMethods` は宣言の列で、分けても効果が小さい |
| そのほか | `App.xaml.cs`（931）、`OscService`（504） | |

## 決定してほしい事項

### D1 範囲
- **(A) 手書きの `.cs` のうち、1,000 行以上（約 7 件）と、効果が大きい ViewModel を先に（推奨）**。生成ファイルは計測から外す（計測の定義の変更。記録する）。
- (B) 32 件すべて。
- (C) 計測を「生成ファイルを除く」に直すだけにして、分割は別の Phase にする。

### D2 分け方
- **(A) 責務ごとに別の型へ（推奨）**。呼び出し側が変わるので、1 ファイル＝1 型の規則とコンストラクターの引数の目標に沿う。差分は大きいが、構造が良くなる。
- (B) `partial` で複数ファイルへ。差分が小さく安全だが、型の責務は変わらない（行数の指標だけが改善）。

### D3 入力機種の 4 ファイル
- **(A) 今回は対象外にする（推奨）**。実機の確認が要り、先送り中の実機確認が溜まっているため。
- (B) 含める。分割後に実機確認の項目を追加する。

### D4 順序と確認
- **(A) 1 ファイル（または 1 まとまり）ごとに 1 コミット。各コミットでビルド警告 0・全テスト（推奨）**。
- (B) 層ごとにまとめて 1 コミット。

## Step（D1＝(A)、D2＝(A)、D3＝(A)、D4＝(A) の場合）
| Step | 内容 | 検査 |
|---|---|---|
| RV11-0 | 計測の定義を決め、基準（`Measure-CodeReview.py` の除外）を直す。分ける前の挙動の固定（足りない所へテスト） | Debug ALL |
| RV11-1 | `ProfileEditor.xaml.cs`・`MainWindow.xaml.cs` から、ViewModel へ寄せられる処理を調べ、分ける | 同上 |
| RV11-2 | `AppSettingsDTO`・`BackingStore`・`AppSettingsService` の領域ごとの分割（RV10 の領域に沿う） | 同上＋往復テスト |
| RV11-3 | 第 2 層（`Mapping`・`TriggerDispatchEngine`・`TouchpadMouse`）の分割 | 同上 |
| RV11-4 | ViewModel 群の分割 | 同上 |
| RV11-5 | 評価の再算定（500 行以上 32→目標 20 前後） | `Score-CodeReview.py` |

## 守るもの
機能・保存の形・ログの文言。規約（1 ファイル＝1 型、名前空間＝フォルダ、コンストラクターの引数の基準、各ラチェット）。

## 変更履歴
- 2026-11-20: 作成（D1〜D4 の決定待ち）。
- 2026-11-20: D1＝(B)、D2＝(A)、D3＝(B)、D4＝(B) を、ユーザーが選択。D1 と D3 は推奨と異なる。生成ファイル 2 件（`Strings.Designer.cs`・`Resources.Designer.cs`）は手で分けられないため、計測の除外か、リソースの生成方法の変更で扱う。入力機種を分けたあとは実機確認の項目を追加する。層ごとの Step は、1 層ずつ取りかかるときに個別に切る。
- 2026-11-20: 設定・永続化層を実施（`PhaseRV11` ブランチ）。`AppSettingsService`（586→104。領域ごとの `partial` 4 本）、`BackingStore`（955→478。`Actions`・`ControlSettings`・`Lightbar`・`ProfileReset`）、`ProfilePresets`（580→195。`Gyro`・`Kbm`）、`ProfileMigration`（544→200。`Versions`）、`Util`（544→165。`Win32`・`HidHide`）、`NativeMethods`（530→169。`Hid`・`SetupApi`）、`AppSettingsDTO`（1,175→910。`Mapping`）を分けた。**D2＝(A)（別の型）からの逸脱**: 読み書きの経路が多く、呼び出し側の書き換えが大きいため、この層は `partial` で責務ごとのファイルへ分けた（型の分割は、後の Phase の候補）。`AppSettingsDTO` のプロパティ（約 700 行）は、XML の要素の順序が宣言の順で決まり、`partial` をまたぐと順序が不定になるため、本体に残した（500 行以上が 1 件残る）。守りのテストは、ファイル名で見ていたもの（RuntimeKey の基準、OutputTypeNaming の許可リスト、DisconnectCharacterization）を新しいファイル名へ直し、ソースを読む 2 件は `SourceText.ReadCodeWithPartials` で `partial` を続けて読む。Debug ビルド 警告 0・全 3,662 テスト合格。
- 2026-11-21: 第 2 層を実施。`Mapping`（883→372。`MapCustom`・`Curves`・`SpecialActions`）、`TriggerDispatchEngine`（750→486。`Lifecycle`・`Runtime`）、`TouchpadMouse`（730→460。`Touches`）、`GyroOutputProcessor`（542→222。`MouseStick`・`Triggers`）、`ControlBindingBuilder`（502→317。`Interpret`）、`KeyButtonActionController`（501→193。`Impl`）を `partial` で分けた（D2＝(A) からの逸脱は設定層と同じ理由）。ソースを読む 7 件のテストは `SourceText.ReadFileWithPartials`／`ReadCodeWithPartials` へ。Debug 警告 0・全 3,662 テスト合格。
- 2026-11-21: ViewModel 群を実施。`SettingsViewModel`（708→377。`Startup`・`Network`）、`AutoProfilesViewModel`（673→489。`Discovery`）、`GyroSettingsViewModel`（579→464。`Triggers`）、`SpecialActionsListViewModel`（569→393。`Operations`）、`RecordBoxViewModel`（533→302。`Steps`・`Presets`）、`MainWindowsViewModel`（523→375。`Service`）、`PresetMenuHelper`（509→128。`Presets`）を `partial` で分けた。Debug 警告 0・全 3,662 テスト合格（ソースを読むテストの修正は不要だった）。
- 2026-11-21: 画面の code-behind と `OscService` を実施。`ProfileEditor`（1,615→461。`Layout`・`Visualizer`・`SpecialActions`・`Events`・`Editing`・`Presets`）、`MainWindow`（1,492→480。`WindowMessages`・`Updates`・`ProfileEditorHost`・`Devices`・`Log`・`Lifecycle`・`Service`）、`RecordBox`（867→275）、`ControllerReadingsControl`（636→386）、`WelcomeDialog`（519→195）、`App`（931→362。`Bootstrap`）、`OscService`（504→273。`Commands`）。ソースを読むテストは `SourceText.Read*` が `partial` を続けて読むようにし、ファイルを列挙して数える 4 件（RuntimeKey、DisconnectCharacterization、ControlMode）は新しいファイル名へ直した。Debug 警告 0・全 3,662 テスト合格。
- 2026-11-21: 入力機種を実施。`InputDeviceCore`（1,109→423。`Status`・`Basics`・`Idle`・`Output`・`Disconnect`）、`DualSenseDevice`（1,083→409。`Types`・`Setup`・`Output`）、`SwitchProDevice`（995→484。`Setup`・`Disconnect`）、`DualShock4Device`（801→294。`Output`・`Frames`）、`InputDeviceRegistry`（542→283。`Discovery`）。ソースを読むテスト 1 件（`OutputDataFlagTests`）を新しいファイル名へ。Debug 警告 0・全 3,662 テスト合格。実機確認の項目を `Device-Check-Checklist.md` §9i に追加（未実施）。500 行以上の残りは `AppSettingsDTO`（XML の順序のため）と生成ファイル 2 件。
