# Phase6-Step10-2 計画書: グループ2【プロファイル・マッピング系】の View／ViewModel ペア Pure DI 化

作成日: 2026-10-14  
状態: 計画案・承認待ち（実装未着手）  
上位計画書: `Phase6-Step10-Plan.md`（§0A.3 が本 Step の台帳）、前の Step: `Phase6-Step10-1-Plan.md`（完了）  
準拠: `.github/copilot-instructions.md` §2.1〜2.4、§3.1、§3.3、§3.4

---

## 1. 目的
- プロファイル・マッピング系の小型 View と ViewModel の `Global` 直接参照と、`AppHost.GetService`／`Program.rootHub`／`Global.*Instance` のフォールバックを解消する。
- 決定3（View の依存は親から必須引数）、決定1（不変の静的表と純粋関数は温存）、決定 D8（XAML から生成される部品は `Initialize`）に従う。
- 動作は変えない。新しい契約は `Global` への薄い委譲にし、契約テストで同じ値・同じ動作を固定する。

## 2. 対象（現行コード 2026-10-14 の再確認）

### 2.1 View
- `BindingWindow.xaml.cs`: `defaultButtonMapping`×3、`InverseRumbleMotors`×1（`ds4inputNames`／`xboxDefaultNames`／`ds4DefaultNames` は温存）、`AppHost`（`ControlService`）／`Program.rootHub` のフォールバック。生成箇所は 8 か所（`ProfileEditor.xaml.cs` に 7、`SpecialActionEditor.xaml.cs` に 1）。
- `RecordBox.xaml.cs`: `appdatapath`×2、`AppHost`／`Program.rootHub` のフォールバックと `IViewModelFactory` の取得。生成箇所は `BindingWindow.xaml.cs` と `RecordBoxWindow.xaml.cs`。
- `RecordBoxWindow.xaml.cs`、`RenameProfileWindow.xaml.cs`、`ControllerRegisterOptionsWindow.xaml.cs`、`PresetOptionWindow.xaml.cs`: ViewModel を `new` している（依存の受け渡しが必要）。
- `AutoProfiles.xaml.cs`: `CreateAutoProfiles`、`UseCustomSteamFolder`、`CustomSteamFolder`×2、`PathServiceInstance`／`AppHost` のフォールバック。**`MainWindow.xaml` から XAML で生成される**ため、コンストラクタの処理（保存先のファイルの生成、Steam フォルダの判定、`Holder` の取得）を `Initialize` へ移す必要がある。
- `DupBox.xaml.cs`: `appdatapath`×2。**`MainWindow.xaml` から XAML で生成される**。
- `ControllerReadingsControl.xaml.cs`: `AppHost`／`Program.rootHub` のフォールバック。**`ProfileEditor.xaml` から XAML で生成される**。
- `StickCalibrationWindow.xaml.cs`: `AppHost`／`Program.rootHub` のフォールバック。生成箇所は現行のソースで見つからない（呼出元 0 件の疑い。Step10-2-1 で経緯を調べ、§2.4 に従って判断する）。

### 2.2 ViewModel
- `BindingWindowViewModel`: `outDevTypeTemp`、`RefreshActionAlias`×2。
- `AutoProfilesViewModel`: `?? Global.…` のフォールバック 4 件（`AutoProfileRevertDefaultProfile`、`autoProfileSwitchNotifyChoice`）と `Program.rootHub`、`AppHost`×2。
- `RecordBoxViewModel`: `ProfileSettingsServiceInstance`、`Program.rootHub`、`AppHost`（`macroDS4Values` は温存）。
- `RenameProfileViewModel`: `appdatapath`。`TouchButtonUserControlViewModel`: `TouchpadButtonMode`×2。
- `MappingListViewModel`: `GetDS4CSetting`、`defaultButtonMapping`×2（`getX360ControlString` は温存）。入れ子の `MappedControl` が使う。
- `ControllerListViewModel`: `*Instance` フォールバック 4 件、`AppHost`×5、任意引数（`= null`）。
- `ControllersViewModel`（F09）: 任意引数＋`AppHost`×3。DI に Singleton で登録済み。
- `ControllerRegDeviceOptsViewModel`: `UseMoonlight`×3、`UseAdvancedMoonlight`×3、`SaveControllerConfigs`×1。
- `PresetOption`（`DS4Control/PresetOption.cs`。複数の型が同居。分割は Step10b）: `Load*Profile`×10 と `Program.rootHub` フォールバック。

## 3. 現行コードの調査結果（設計に影響するもの）
- **ガードテストが生成呼び出しの文字列と件数を固定している**: `ProfileEditorTargetDeviceGuardTests` は `new BindingWindow(`（7）、`new SpecialActionEditor(`（2）、`new RecordBox(`、`CreateRecordBoxViewModel(` 等を検査している。生成方法を変える場合は、このガードも同じ Step で更新する。
- **`IViewModelFactory` は View ではなく ViewModel を生成する契約**: `ViewModelFactory` のコンストラクタの検査（`ProfileSettingsViewModelGlobalReferenceGuardTests`）が、`AppHost` と `= null` が無いこと、`Func<ControlService>` を確認している。引数を足すときは、このガードも確認する。
- **`IAutoProfileService` には `CreateAutoProfiles` に相当するメンバーが無い**（`Holder`、`Running` など）。`Global.CreateAutoProfiles(path)` は、保存先に `Auto Profiles.xml` が無いときの初期化にだけ使われている。
- **`UseMoonlight`／`UseAdvancedMoonlight` の契約が無い**。`DS4Devices`（第1層）も `Global.UseMoonlight` を直接読んでいる。第1層の置換は Step10 の範囲外（Step10-4 の全域再走査で分類する）。本 Step では画面側（`ControllerRegDeviceOptsViewModel`）だけを置換する。
- **`IProfileRepository` には `LoadBlankProfile` しか無い**（`Global.LoadBlankDevProfile` の委譲）。`PresetOption` が使う `Global.LoadBlankDS4Profile` と `LoadDefault*Profile`×8 の契約は無い。
- **プリセット画面の生成経路**: `ProfileEditor` → `PresetOptionWindow`（引数なし）→ `PresetOptionViewModel`（引数なし）→ `GamepadPreset` 等（引数なし）。`ControlService` は各 `PresetOption` が `Program.rootHub` から取得している。

## 4. 決定が必要な事項
- **E1（ダイアログの生成）**: `BindingWindow`（8 か所）、`RecordBox`、`RecordBoxWindow`、`PresetOptionWindow`、`RenameProfileWindow`、`ControllerRegisterOptionsWindow` へ依存を渡す方法。
  - 案A（推奨）= 窓口の契約 `IEditorWindowFactory`（`DI/`。`BindingWindow` などを生成する。実装は `DS4Control/Services/` に置き、必要なサービスを必須引数で保持する）を新設する。`ProfileEditor`／`MainWindow`／`BindingWindow` などは、この窓口を必須引数で受け取り、`new` の代わりに窓口の `Create…` を呼ぶ。生成箇所の変更が小さく、サービスの受け渡しの連鎖（`ProfileEditor` → `BindingWindow` → `RecordBox`）が短くなる。ガードテストの生成呼び出しの検査を、窓口の呼び出しへ更新する。
  - 案B = 各 `new` の呼び出しで、必要なサービスをすべて引数で渡す（`BindingWindow` に約 5 個、`RecordBox` に約 4 個）。窓口の契約を作らない代わりに、生成箇所 15 か所以上のシグネチャが変わり、受け渡しの連鎖が長くなる。
- **E2（プリセットの読み込み）**: `PresetOption` が使う `Load*Profile`×10 の置換先。
  - 案A（推奨）= 小さな契約 `IProfilePresetLoader`（`DI/`。10 メソッド。実装は `Global.Load…Profile` への薄い委譲）を新設する。`PresetOption` は `IProfilePresetLoader` と `ControlService` を必須引数で受け取る。`IProfileRepository` を肥大させず、プリセットの関心事を 1 つの契約にまとめられる。
  - 案B = `IProfileRepository` に 9 メソッドを足す（`LoadBlankProfile` と同じ並び）。契約の数は増えないが、リポジトリの契約が肥大する。
- **E3（`StickCalibrationWindow`）【確認済み】**: 呼出元 0 件ではなかった。`ProfileEditor.xaml.cs:2421` が target-typed new（`new(stick, targetDevice, profileSettingsVM)`）で生成している（従来の grep の対象外だった）。削除せず、E1 の窓口（`IEditorWindowFactory`）で生成し、`ControlService` を必須引数で受け取る形にする。

## 5. 契約の追加（Global への薄い委譲。契約テストで固定）
- `IAppSettingsService`: `UseMoonlight`、`UseAdvancedMoonlight`（変更の通知を含む）。
- `IAutoProfileService`（または `IPathService`）: `CreateAutoProfilesFile(string path)`（`Global.CreateAutoProfiles` と同じ動作）。
- `IProfilePresetLoader`（E2＝A の場合）: `LoadBlank`、`LoadBlankDS4`、`LoadDefaultGamepadGyro`、`LoadDefaultDS4GamepadGyro`、`LoadDefaultMixedControls`、`LoadDefaultDS4MixedControls`、`LoadDefaultMixedGyroMouse`、`LoadDefaultDS4MixedGyroMouse`、`LoadDefaultKBM`、`LoadDefaultKBMGyroMouse`。
- `IEditorWindowFactory`（E1＝A の場合）: 上記のダイアログを生成する窓口。
- 契約にメンバーを足したら、契約を実装するテスト用クラスがないかを確認する。

## 6. マイクロステップ（案）
各ステップのあと、ビルド・テスト・実機確認の結果を待ってから次へ進む。

### Step10-2-1: 契約の追加と契約テスト
- 5 章の契約を追加し、`Global` と同じ値・動作であることを契約テストで固定する。
- `StickCalibrationWindow` の経緯調査（E3）。

### Step10-2-2: 設定・コントローラー一覧系（ViewModel 中心）
- `ControllersViewModel`（F09）、`ControllerListViewModel`、`ControllerRegDeviceOptsViewModel`（`ControllerRegisterOptionsWindow` と同時）: 任意引数＋フォールバックを必須引数へ。`MainWindow` の生成呼び出しを更新。
- 実機確認: コントローラー一覧の表示と操作、Moonlight 設定の変更と保存。

### Step10-2-3: 自動プロファイル・複製・名前変更
- `AutoProfiles`／`AutoProfilesViewModel`（`Initialize` 化。`ViewModelFactory` へ必須引数を追加）、`DupBox`（`Initialize` 化）、`RenameProfileWindow`／`RenameProfileViewModel`。`MainWindow` のコンストラクタと生成呼び出しを更新。
- 実機確認: 自動プロファイルの追加・削除・プロファイルの紐付け、プロファイルの複製・名前変更。

### Step10-2-4: ダイアログの生成窓口と BindingWindow／RecordBox
- E1 の結果に従って、`BindingWindow`／`BindingWindowViewModel`、`RecordBox`／`RecordBoxViewModel`／`RecordBoxWindow` を置換。`ProfileEditor`／`SpecialActionEditor` の生成呼び出しとガードテスト（`ProfileEditorTargetDeviceGuardTests`）を更新。
- 実機確認: ボタン割り当て画面の表示、マクロの記録、キー割り当ての保存。

### Step10-2-5: マッピング一覧・タッチボタン・プリセット
- `MappingListViewModel`（入れ子の `MappedControl` を含む）、`TouchButtonUserControlViewModel`、`PresetOption`（E2 の結果に従う）、`PresetOptionWindow`／`PresetOptionViewModel`、`ControllerReadingsControl`（`Initialize` 化）。
- 実機確認: プロファイル編集画面のマッピング一覧、タッチパッドのボタンモード、プリセットの適用。

### Step10-2-6: ガードテストと文書
- `Step10GroupTwoGlobalReferenceGuardTests`（新規）: 置換済みのファイルに、温存項目以外の `Global.`、`AppHost`、`Program.rootHub` が無いこと。
- `Phase6-Status.md`、`Phase6-Step10-Plan.md`、モデル図（03 のクラス図、04 の §2）を更新する。

## 7. 実機確認（案。確認できない項目は `Phase6-Step11-Plan.md` §3.3 へ登録する）
- 上記の各ステップの項目に加え、全体の確認として、プロファイルの新規作成（プリセットの適用）→ ボタンの割り当て → 保存 → 自動プロファイルへの紐付け → 複製 → 名前変更 → 削除。

## 8. リスク
- **XAML から生成される部品の初期化の順序**: `AutoProfiles` は `MainWindow` が直後に `AutoProfileHolder` を読む。`Initialize` を、読み取りより前に呼ぶこと。
- **ガードテストの文字列依存**: `new BindingWindow(` の件数など、生成方法の変更でガードが落ちる。同じ Step で更新する。
- **`PresetOption` の `ControlService` の取得元**: `Program.rootHub` から、`PresetOptionWindow` を生成する `ProfileEditor` が保持する `ControlService` へ変わる。同じインスタンスであることをテストで確認する。
- **`AutoProfiles` の初期化中の副作用**: 保存先に `Auto Profiles.xml` が無いと、画面の生成時にファイルを作る。この動作と順序を維持する。

## 9. 完了条件
- 対象ファイルに、温存項目（定数、不変の静的表、純粋関数）を除く `Global.`、`AppHost.GetService`、`Program.rootHub` が無い。
- 追加した契約の契約テストとガードテストが通り、`dotnet test` が全件合格する。
- 実機確認の結果が記録され、確認できない項目が先送り台帳へ登録されている。
