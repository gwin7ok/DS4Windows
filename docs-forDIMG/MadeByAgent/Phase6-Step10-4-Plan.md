# Phase6-Step10-4 計画書: サービス実装のコンストラクタのフォールバック（F08）と、全域の再走査・分類

作成日: 2026-10-15  
状態: 計画案・承認待ち（実装未着手）  
上位計画書: `Phase6-Step10-Plan.md`（§0A.5、§0A.6 が本 Step の台帳）、前の Step: `Phase6-Step10-3-Plan.md`（完了）  
準拠: `.github/copilot-instructions.md` §2.1〜2.4、§3.1、§3.3、§3.4

---

## 1. 目的
- **F08**: サービス実装・`Actions/*` のコンストラクタに残っている、`AppHost.GetService`／`Program.rootHub`／`Global.*Instance`／`new <具象>` のフォールバックを整理する（Pure DI の完成。copilot-instructions §3.1）。
- **全域の再走査**: Step10-1〜3 の完了後のコードで、`Global.` の直接参照をソリューション全体で再集計し、各項目を「温存（理由付き）」「Step10-4 で置換」「後続（Step12／Phase7）へ送る」に確定する（決定6）。
- 動作は変えない。置換先は既存の契約を優先し、新しい契約は `Global` への薄い委譲にする。

## 2. 全域の再走査の結果（2026-10-15、コメントを除く。`ScpUtil.cs` 自体を除く。71 ファイル、644 件）

### 2.1 温存（定数・不変データ・純粋関数。決定1）
- **定数**: `MAX_DS4_CONTROLLER_COUNT`（`Mapping.cs` 24、`AppSettingsDTO.cs` 14、`DS4LightBar.cs` 7、`AutoProfilesDTO.cs` 4、ほか）、`TEST_PROFILE_INDEX`／`TEST_PROFILE_ITEM_COUNT`、`RESOURCES_PREFIX`／`ASSEMBLY_RESOURCE_PREFIX`（`ProfileEditor.xaml.cs` 34、`TrayIconViewModel.cs` 12、ほか）、`CONFIG_VERSION`／`APP_CONFIG_VERSION`、`LANGUAGE_ASSEMBLY_NAME`／`PROBING_PATH`。
- **不変の値**: `exeversion`（`ProfileDTO`、`AppSettingsDTO`、`ProfileMigration`、`OutputSlotPersist`、`OutputSlotPersistDTO`）、`configFileDecimalCulture`（`ActionsDTO`、種別ごとの ViewModel）。
- **純粋関数・名前表**: `Clamp`（`Mapping.cs` 23、`DS4State` 4、`Mouse`／`MouseCursor` 各 2、`BezierCurve` 1）、`getDS4ControlsByName`／`getX360ControlsByName`／`getX360ControlString`（`ProfileDTO`、`PressKeyViewModel`、`MappingListViewModel`）、`ds4inputNames`／`xboxDefaultNames`／`ds4DefaultNames`（`BindingWindow`）、`macroDS4Values`（`RecordBoxViewModel`）。

### 2.2 温存（Pre-Host。ホスト生成前に動く処理）
- `App.xaml.cs` 16 件（`FindConfigLocation`、`Load`、`RefreshViGEmBusInfo`、`SetCulture`、`UseLang`、`appdatapath`、`LogMaxArchiveFiles`、`LogMinLevel`、`UseCurrentTheme`）。
- `LoggerHolder.cs` 5 件（`appdatapath`、`LogMaxArchiveFiles`、`LogMinLevel`）。
- `WelcomeDialog.xaml.cs` 3 件（`FindConfigLocation`、`Load`。TODO 付き。Step10-1-2 の決定）。
- 文字列リテラルの `Global.UseLang`（`LanguageSelectDialog`、`LanguagePackViewModel` のログ文）は、参照ではない。

### 2.3 温存（§2.1 の薄いシム。サービス実装が `Global` の実体へ委譲する）
- `DS4Control/Services/` の実装クラス: `AppSettingsService` 185、`ProfileRepository` 27、`OutputKBMHandlerAdapter` 21、`EnvironmentService` 20、`ProfileSettingsService` 11、`ProfilePresetLoader` 10、`SpecialActionRepository` 9、`OutputKBMHandlerLifecycle` 8、`PathService` 8、`AppearanceSettingsService` 8、`AppNotificationService` 6、`DeviceStateService` 4、`ProfileXmlStore` 3、`OutputSlotService` 2、`ProfileSlotApplier` 1、`ProfileActionProvider` 1、`Ds4DeviceRegistryAdapter` 1、`DefaultElevatedProcessLauncher` 1（`exelocation`）。
- Step12（旧シムの物理削除）まで残す。

### 2.4 要個別判断（置換候補。決定が必要）
- **`WindowPlacementHelper.cs` 16 件**（`FormLocationX`／`FormLocationY`／`FormWidth`／`FormHeight`）: 0A.6 の台帳に無かった新規の発見。`IAppSettingsService` に同名の契約が存在する。`MainWindow` だけが使う `internal static` クラス。
- **`MainWindow.xaml.cs` 19 件**: `*Instance` 5（`EnvironmentServiceInstance`、`NotificationServiceInstance`、`OutputSlotServiceInstance`、`PathServiceInstance`、`ProfileRepositoryInstance`、`ProfileSettingsServiceInstance` を含む）、`ApplyProfileToSlot`×2、`ProfilePath`×3、`OlderProfilePath`×1。
- **`AutoProfileService.cs` 7 件**: `AutoProfileRevertDefaultProfile`×2、`autoProfileSwitchNotifyChoice`×2、`ProfilePath`×2、`CreateAutoProfiles`×1。
- **`ProfileApplicationService.cs` 7 件**: `ApplyProfile`×3、`OlderProfilePath`×4。
- **`ControlService.cs` 9 件**（定数除く 7）: `IsRunningSupportedViGEmBus`×2、`vigemInstalled`、`vigembusVersion`×2、`RefreshViGEmBusInfo`、`ProfileSettingsServiceInstance`。
- **`ProfileEntity.cs` 5 件**: `CacheExtraProfileInfo`、`SaveProfile`、`appdatapath`×3。
- **`ProfileList.cs` 1**（`PathServiceInstance`）、**`AutoProfileHolder.cs` 2**（`appdatapath`、定数）、**`OutputSlotPersist.cs` 3**（`appdatapath`×2、`exeversion`）、**`Log.cs` 1**（`NotificationServiceInstance`）、**`OutputSlotManager.cs` 2**（`vigemBusVersionInfo`）。
- **`Actions/ActionManager.cs` 3 件**: `GetProfileAction`、`getProfileActions`、定数。
- **`Mapping.cs` の非定数 3 件**: `ApplyProfile`、`ProfileSettingsServiceInstance`、`outputKBMHandler`。**`SyntheticDispatcher.cs` 4 件**: `outputKBMHandler`×2、`outputKBMMapping`×2。
- **第1層・第3層（`Global` を直接読む下位層）**: `DS4Devices.cs` 6（`CheckIfVirtualDevice`×2、`GetInstanceIdFromDevicePath`×2、`UseMoonlight`、`UseAdvancedMoonlight`）、`DS4Device.cs`（`DebouncingMs`×2、`DebouncingMsChanged`）、`DS4Sixaxis.cs`（`UseDs3PitchRollSim`）、`HidDevices.cs` 2（`DeviceOptions`）、`DS4OutDeviceBasic`／`DS4OutDeviceExt`／`Xbox360OutDevice`（`GetSASteeringWheelEmulationAxis`）、`VirtualKBMFactory.cs`（`fakerInputInstalled`）、`Util.cs`（`IsAdministrator`。Step10-1 で Step12 送りに決定済み）。

### 2.5 F08: コンストラクタのフォールバック（現行）
- **`AppHost.GetService` のフォールバック**: `AutoProfileService`（3）、`ProfileApplicationService`（5）、`OutputSlotService`（`IOutputSlotStore`）、`OutputSlotStore`（`IPathService`）、`ProfileActionChainService`、`ProfileRepository`（`ControlService` の遅延取得）、`DefaultActionManager`、`DefaultMacroPlayer`、`LaunchProcessAction`、`MacroAction`（3）、`MouseOutputAction`、`ProfileSwitchAction`（3）、`AutoProfileChecker`。
- **`new <具象>` のフォールバック**: `OutputSlotService`（`new OutputSlotStore()`）、`OutputSlotStore`（`new PathService()`）、`ProfileActionChainService`（`new MappingActionDispatcher()`）、`DefaultActionManager`（`new DefaultActionFactory()`）、`LaunchProcessAction`／`MacroAction`／`ProfileSwitchAction`（`new Default…()`）。
- **`Global.*Instance` のフォールバック**: `ProfileRepository`（`ProfileSettingsServiceInstance`、`ProfileXmlStoreInstance`、`PathServiceInstance`）、`SpecialActionRepository`（`PathServiceInstance`）。
- **`Program.rootHub` の参照**: `AutoProfileService` 7（`running` の確認と待機）、`ProfileSettingsService` 4（DualSense の判定など）、`OutputSlotService` 1、`DS4Sixaxis` 1。
- **静的 `ServiceProviderHolder.Provider` の利用**: `Actions/ActionManager.cs` 12、`Mapping.cs` 5、`DefaultKeyButtonActionControllerFactory` 2、`KeyActionAdapter` 1、`ActionFactory` 1、`App.xaml.cs` 1。
- **テストでの生成**: `ProfileSettingsService` 76、`SpecialActionRepository` 32、`ProfileRepository` 27、`OutputSlotService` 23、`DefaultActionManager` 7、`ProfileActionChainService` 6、`AutoProfileService` 4、`DefaultMacroPlayer` 3、`LaunchProcessAction` 2、`MouseOutputAction` 1。引数なしの生成が多いため、必須化はテストの更新を伴う。

## 3. 決定が必要な事項

### D4-1: 置換候補（§2.4）の範囲
- 案A（推奨）: **Step10-4 で置換する**: `WindowPlacementHelper`、`AutoProfileService`、`ProfileApplicationService`、`ControlService` の ViGEm 関連、`MainWindow` の `*Instance`、`ProfileEntity`、`ProfileList`、`AutoProfileHolder`、`Log.cs`、`ActionManager` の `GetProfileAction`／`getProfileActions`。**後続へ送る**: `Mapping.cs`／`SyntheticDispatcher.cs`（Phase7 の instance 化。`outputKBMHandler` は TODO 付き）、第1層・第3層（`DS4Devices`、`DS4Device`、`DS4Sixaxis`、`HidDevices`、出力デバイス、`VirtualKBMFactory`。層をまたぐ契約の設計が必要なため、Step12 または Phase7）、`Util.IsAdministrator`（Step12）、`OutputSlotPersist`／`OutputSlotManager`（Step10-5 の出力スロットの接続と一緒に確認）。
- 案B: 置換候補をすべて Step12 へ送り、Step10-4 は F08 だけにする。範囲は小さくなるが、`Global` の残存が増えたまま Step11 へ進む。
- 案C: 第1層・第3層も含めて、すべて Step10-4 で置換する。層をまたぐ契約の新設が必要で、リスクが高い。

### D4-2: F08（コンストラクタのフォールバック）の進め方
- 案A（推奨）: **クラスタ単位で、依存を必須引数にする**（`Program.rootHub`／`AppHost` のフォールバックと `new <具象>` を削除）。3 つのクラスタに分ける: ①プロファイル系（`ProfileRepository`、`SpecialActionRepository`、`ProfileApplicationService`、`AutoProfileService`、`ProfileActionChainService`）、②出力スロット系（`OutputSlotService`、`OutputSlotStore`）、③`Actions/*`（`DefaultActionManager`、`DefaultMacroPlayer`、`LaunchProcessAction`、`MacroAction`、`MouseOutputAction`、`ProfileSwitchAction`、`AutoProfileChecker`）。各クラスタで、ビルド・テスト・実機確認を行う。
- 案B: 引数なしの生成を、テスト専用のファクトリ（テストヘルパー）へ集約してから、一括で必須化する。1 回の変更が大きくなる。
- 案C: テストでの引数なしの生成が多いクラスは、引数なしの生成を温存し、理由を TODO に記載する（`ProfileSettingsService` 76、`SpecialActionRepository` 32 など）。変更は小さいが、フォールバックが残る。

### D4-3: `Program.rootHub` の置換先
- 案A（推奨）: `ControlService` を `Func<ControlService>`（遅延解決）で受け取る。`AutoProfileService`（実行状態の確認）、`ProfileSettingsService`（DualSense の判定）、`OutputSlotService`（`ControlService` の取得）が対象。循環依存を避けるため、遅延解決を使う（Step9-4、Step10-2-3 と同じ形）。
- 案B: 「実行中か」「デバイスの種別」だけを返す小さな契約（`IControllerHub` など）を新設する。契約は増えるが、依存が最小になる。
- 案C: 温存して TODO を付け、Step12 へ送る。

### D4-4: 静的な `ServiceProviderHolder.Provider`（`Actions/ActionManager.cs` ほか 22 件）
- 案A（推奨）: **温存し、TODO を付けて Phase7／Phase8 へ送る**。`ActionManager` は、`Mapping.cs`（static）と一体の静的ファサードで、`Mapping` の instance 化（Phase7）と、Controls／SpecialActions 統一ディスパッチ（Phase8）の際に、構造ごと見直すのが妥当。
- 案B: 静的ファサードを維持したまま、`IManagedActionManager` の取得を 1 か所（初期化時）に集約する。変更は中規模で、効果は限定的。

### D4-5: 実施順（Step10-4 の細分化）
- 案A（推奨）: ①再走査の確定（分類表の確定。コードは変えない）→ ②置換候補のうち小さなもの（`WindowPlacementHelper`、`ProfileList`、`ProfileEntity`、`Log.cs`、`AutoProfileHolder`）→ ③F08 のクラスタ 1（プロファイル系）→ ④クラスタ 2（出力スロット系）→ ⑤クラスタ 3（`Actions/*`）→ ⑥`MainWindow`・`ControlService` の残り → ⑦ガードテストと文書。

## 4. マイクロステップ（案。決定後に確定）
- **Step10-4-0**: 分類表の確定とガードテストの設計（コードの変更なし。D4-1〜D4-5 の決定を反映）。
- **Step10-4-1**: 小さな置換（`WindowPlacementHelper` ほか）。
- **Step10-4-2**: F08 クラスタ 1（プロファイル系）。
- **Step10-4-3**: F08 クラスタ 2（出力スロット系）。
- **Step10-4-4**: F08 クラスタ 3（`Actions/*`）。
- **Step10-4-5**: `MainWindow`・`ControlService` の残りの `Global` 参照の置換。
- **Step10-4-6**: 全域のガードテスト（温存項目の許可リスト方式。ソリューション全体で、許可リスト以外の `Global.`／`AppHost`／`Program.rootHub` が増えないことを検査）と文書（モデル図 03／04、`Phase6-Status.md`、`Phase6-Step10-Plan.md`）。

## 5. 実機確認（案。確認できない項目は `Phase6-Step11-Plan.md` §3.3 へ登録する）
- **ウィンドウの位置・サイズ**（`WindowPlacementHelper`）: 終了時に保存され、起動時に復元されること。最小化起動。
- **自動プロファイル**（`AutoProfileService`）: 対象のプログラムの起動・終了で、プロファイルが切り替わること。サービスの開始・停止との連動。
- **プロファイルの適用**（`ProfileApplicationService`）: プロファイルの切替、一時プロファイルの復帰。
- **出力スロット**: スロットの手動接続・切断、永続化。
- **スペシャルアクション**: マクロ、プログラム起動、プロファイル切替（`Actions/*`）。

## 6. リスク
- **テストの更新量**: 引数なしの生成が、テストに約 180 件ある。クラスタごとに、同じ Step でテストを更新する。
- **循環依存**: `OutputSlotService` ↔ `ControlService` など。遅延解決（`Func<>`）で回避する（既存の方針）。
- **起動順序**: ホスト生成前に解決してはならないサービスがある（K7-1）。`Func<>` の使用と、実機での起動確認で担保する。
- **K1（`ProfileApplicationService` の `deviceIndex >= 4`）**: スロット数 8 への拡張に関わる既知の項目。クラスタ 1 の置換時に、現行コードで再確認する。

## 7. 完了条件
- 許可リスト（§2.1〜2.3、および D4-1 で「温存」「後続へ送る」と決めたもの）以外の `Global.`、`AppHost.GetService`、`Program.rootHub`、`new <具象>` のフォールバックが、`Services/`・`Actions/`・View・ViewModel に残っていない（残るものは、理由付きの TODO がある）。
- 追加した契約の契約テストと、全域のガードテストが通り、`dotnet test` が全件合格する。
- 実機確認の結果が記録され、確認できない項目が先送り台帳へ登録されている。

---

## 8. 決定の記録（2026-10-15、すべて推奨案で承認）
- **D4-1＝A**: 置換候補（§2.4）のうち、`WindowPlacementHelper`、`AutoProfileService`、`ProfileApplicationService`、`ControlService` の ViGEm 関連、`MainWindow` の `*Instance`、`ProfileEntity`、`ProfileList`、`AutoProfileHolder`、`ActionManager` の `GetProfileAction`／`getProfileActions` は Step10-4 で置換する。`Mapping.cs`／`SyntheticDispatcher.cs`（Phase7）、第1層・第3層（Step12 または Phase7）、`Util.IsAdministrator`（Step12）は後続へ送る。
- **D4-2＝A**: F08 は、①プロファイル系、②出力スロット系、③`Actions/*` の 3 クラスタに分け、クラスタごとに必須化とテスト更新、実機確認を行う。
- **D4-3＝A**: `Program.rootHub` は、`Func<ControlService>`（遅延解決）で置き換える。
- **D4-4＝A**: 静的な `ServiceProviderHolder.Provider`（`ActionManager` ほか 22 件）は温存し、TODO を付けて Phase7／Phase8 へ送る。
- **D4-5＝A**: 実施順は §4 のとおり（分類の確定 → 小さな置換 → クラスタ 1 → 2 → 3 → `MainWindow`・`ControlService` → ガードテストと文書）。

## 9. 分類表（Step10-4-0 の成果。コードの変更なし）

### 9.1 Step10-4 で置換する（置換先と、実施するマイクロステップ）
- **Step10-4-1（小さな置換）**
  - `WindowPlacementHelper.cs`（16 件）: `internal static` のため、各メソッドが `IAppSettingsService`（`FormLocationX`／`FormLocationY`／`FormWidth`／`FormHeight`）を引数で受け取る形にする。呼び出し元は `MainWindow`（5 か所）。
  - `ProfileList.cs`（1 件）: `IPathService` を必須引数にし、`Global.PathServiceInstance` のフォールバックを削除する。生成箇所は `MainWindow`（1）とテスト（3）。
  - `ProfileEntity.cs`（5 件。`appdatapath`×3、`SaveProfile`、`CacheExtraProfileInfo`）: `IPathService` と `IProfileRepository`（`SaveProfile`、`CacheExtraProfileInfo`）を、`ProfileList` から渡す（`ProfileEntity` は `ProfileList` だけが生成する）。
  - `AutoProfileHolder.cs`（`appdatapath` 1 件）: `IPathService` を必須引数にする。生成箇所は DI の登録、`AutoProfileService` のフォールバック、テスト（7）。
- **Step10-4-2（クラスタ 1: プロファイル系）**
  - `ProfileRepository`（`Global.*Instance`×3、`AppHost`×1 のフォールバック）、`SpecialActionRepository`（`PathServiceInstance`）、`ProfileApplicationService`（`AppHost`×5、`ApplyProfile`×3、`OlderProfilePath`×4。K1 の `deviceIndex >= 4` を現行コードで再確認）、`AutoProfileService`（`AppHost`×3、`Program.rootHub`×7、`new AutoProfileHolder()`、`new EnvironmentService()`、`AutoProfileRevertDefaultProfile`×2、`autoProfileSwitchNotifyChoice`×2、`ProfilePath`×2、`CreateAutoProfiles`）、`ProfileActionChainService`（`AppHost`、`new MappingActionDispatcher()`）、`ProfileSettingsService`（`Program.rootHub`×4、`new EnvironmentService`）。
- **Step10-4-3（クラスタ 2: 出力スロット系）**: `OutputSlotService`（`Program.rootHub`、`AppHost`、`new OutputSlotStore()`、`new OutputSlotManager`）、`OutputSlotStore`（`AppHost`、`new PathService()`）、`OutputSlotPersist`／`OutputSlotManager`（`appdatapath`、`vigemBusVersionInfo`。Step10-5 の接続の前提として確認）。
- **Step10-4-4（クラスタ 3: `Actions/*`）**: `DefaultActionManager`、`DefaultMacroPlayer`、`LaunchProcessAction`、`MacroAction`（3）、`MouseOutputAction`、`ProfileSwitchAction`（3）、`AutoProfileChecker`。`Actions/*Action` は、利用側が `new` する箇所と、DI の外で生成される経路（`DefaultActionFactory` など）を突き合わせてから、必須化するか、理由を書いて温存するかを判断する。
- **Step10-4-5（`MainWindow`・`ControlService`）**: `MainWindow`（`*Instance`×6、`ApplyProfileToSlot`×2、`ProfilePath`×3、`OlderProfilePath`×1。Step9 のガードテストの許容範囲を確認）、`ControlService`（`IsRunningSupportedViGEmBus`×2、`vigemInstalled`、`vigembusVersion`×2、`RefreshViGEmBusInfo`、`ProfileSettingsServiceInstance`。`IEnvironmentService` に既存の契約があるものを使う）、`ActionManager` の `GetProfileAction`／`getProfileActions`（`IProfileActionProvider`）。

### 9.2 温存する（理由付き。許可リストへ登録）
- §2.1（定数・不変データ・純粋関数）、§2.2（Pre-Host）、§2.3（薄いシム）。
- **`Log.cs`（`NotificationServiceInstance`）**: 静的ロガー（`AppLogger`）で、コンストラクター注入ができない。D4-4 と同様に、静的ファサードとして温存し、TODO を付けて Step12 へ送る（当初の「置換」から、本分類で変更）。
- **`Mapping.cs`（`ApplyProfile`、`ProfileSettingsServiceInstance`、`outputKBMHandler`）、`SyntheticDispatcher.cs`（`outputKBMHandler`、`outputKBMMapping`）**: Phase7（`Mapping` の instance 化）へ送る（TODO 付き）。
- **静的な `ServiceProviderHolder.Provider`**（`Actions/ActionManager.cs` 12 ほか 22 件）: D4-4 により、Phase7／Phase8 へ送る。

### 9.3 後続へ送る（Step12 または Phase7。理由付きで台帳へ）
- 第1層（`DS4Devices.cs`: `CheckIfVirtualDevice`×2、`GetInstanceIdFromDevicePath`×2、`UseMoonlight`、`UseAdvancedMoonlight`。`DS4Device.cs`: `DebouncingMs`×2、`DebouncingMsChanged`。`DS4Sixaxis.cs`: `UseDs3PitchRollSim`、`Program.rootHub`。`HidDevices.cs`: `DeviceOptions`×2）。
- 第3層（`DS4OutDeviceBasic`／`DS4OutDeviceExt`／`Xbox360OutDevice`: `GetSASteeringWheelEmulationAxis`。`VirtualKBMFactory.cs`: `fakerInputInstalled`）。
- `Util.cs`（`IsAdministrator`）。
- 下位層から上位層の設定を読んでいるため、層をまたぐ契約の設計（例: 下位層が購読するイベントや、生成時に値を渡す形）が必要。`Phase6-Step12-Plan.md`（旧シムの物理削除）または Phase7 で、層の依存方向とあわせて扱う。

## 10. 進捗（2026-10-15）
- **Step10-4-0（分類表の確定）**: 完了（本書 §8〜9。コードの変更なし）。次: Step10-4-1（小さな置換）の計画と実装。

---

## 11. Step10-4-1 の計画（小さな置換）。2026-10-15

### 11.1 対象の再確認（現行コード）
- **`WindowPlacementHelper.cs`**: `Global.FormLocationX`／`FormLocationY`／`FormWidth`／`FormHeight` を使っているのは、`ApplyPlacement(MainWindow, bool)` だけ（16 件）。`GetPlacement`／`GetLogicalPlacement` は `Global` を使わない。呼び出し元は `MainWindow` の `ApplyPlacement` 1 か所（1197 行）。
- **`ProfileList.cs`**: `IPathService` を任意引数で受け取り、省略時は `Global.PathServiceInstance`。生成箇所は `MainWindow`（フィールド初期化子）、テスト 2 件（`AutoProfilesViewModelTests`、`PatternCViewModelTests`）。`ProfileEntity` は `ProfileList.AddProfileSort` の 2 か所だけが生成する。
- **`ProfileEntity.cs`**: `DeleteFile`（`appdatapath`）、`RenameProfile`（`appdatapath`×2）、`SaveProfile`（`Global.SaveProfile`／`Global.CacheExtraProfileInfo`）。呼び出し元: `DeleteFile` は `MainWindow` 1 件、`RenameProfile` は `MainWindow` 1 件、**`SaveProfile` と `FireSaved` は 0 件**（`ProfileSaved` イベントの購読も 0 件。`ProfileDeleted` は `ControllerListViewModel` が購読）。
- **`AutoProfileHolder.cs`**: `appdatapath`×1。コンストラクターで `Load()` を実行する。生成箇所は、DI の登録、`AutoProfileService` の `new AutoProfileHolder()` のフォールバック、テスト 7 件。

### 11.2 計画の調整（§9.1 からの変更）
- **`AutoProfileHolder` は、10-4-1 から 10-4-2（クラスタ 1）へ移す**。理由: `AutoProfileService` の `new AutoProfileHolder()` のフォールバック（F08）と同時に直さないと、一時的に別の `Global`／`AppHost` を持ち込むことになるため。10-4-2 で、`IPathService` を必須引数にし、フォールバックと同時に整理する。

### 11.3 変更の内容
1. **`WindowPlacementHelper.ApplyPlacement`**: `IAppSettingsService` を引数に追加する（`ApplyPlacement(MainWindow, IAppSettingsService, bool)`）。`Global.Form*` を、引数の `FormLocationX` など（`IAppSettingsService` の既存の契約）へ置換する。`MainWindow` が、保持している `appSettingsService` を渡す。
2. **`ProfileList`**: `IPathService` を必須引数にし、`Global.PathServiceInstance` のフォールバックを削除する（null は拒否）。`MainWindow` のフィールド初期化子（`new ProfileList()`）は、`pathService` を解決した後（コンストラクター内）で生成するように移す。テスト 2 件は `new PathService()` を渡す。
3. **`ProfileEntity`**: `IPathService` を必須引数にする（`ProfileList` が、自身の `pathService` を渡す）。`DeleteFile`／`RenameProfile` の `Global.appdatapath` を、`pathService.AppDataPath` へ置換する。
4. **`ProfileEntity.SaveProfile`／`FireSaved`／`ProfileSaved`（呼び出し元 0 件）**: §2.4 に従って判断する（11.4 の決定 E1）。

### 11.4 決定が必要な事項
- **E1（呼び出し元 0 件の `ProfileEntity.SaveProfile`／`FireSaved`／`ProfileSaved`）**
  - 経緯: 上流の `DS4Windows` から引き継いだメンバー。現在のプロファイルの保存は、`ProfileEditor` → `ProfileSettingsViewModel.SaveProfile` → `IProfileRepository`（Step8・Step9）に一本化されており、このメンバーは、それと重複する並行実装（`Global.SaveProfile`／`Global.CacheExtraProfileInfo` を直接呼ぶ）で、どこからも使われていない。「本来使うべき API」には当たらない（正規の経路は別にある）。
  - 案A（推奨）: **削除する**（§2.4 の「使うべきでないもの」）。`Global` への直接参照が 2 件減り、`ProfileEntity` が、パスの操作だけに単純化される。機能廃止（§2.2）には当たらない（呼び出し元が無く、画面の機能ではない）。
  - 案B: 温存し、TODO（経緯と、Step12 で削除する予定）を付ける。`Global` への参照が残る。
- **E2（`AutoProfileHolder` を 10-4-2 へ移す）**: 11.2 のとおり。案A（推奨）＝移す。案B＝10-4-1 で、`IPathService` を任意引数（`AppHost` へのフォールバック付き）として足す（フォールバックが増えるため非推奨）。

### 11.5 テスト
- `Step10FourOneGlobalReferenceGuardTests`（新規）: `WindowPlacementHelper.cs`、`ProfileList.cs`、`ProfileEntity.cs` に、温存項目以外の `Global.`／`AppHost`／`Program.rootHub` が無いこと（`ApplyPlacement` が `IAppSettingsService` を受け取ること）、`ProfileList`／`ProfileEntity` の必須引数の null 拒否。
- 動作の検査: 一時ディレクトリで、`ProfileList.Refresh`（プロファイルの追加・削除の反映）、`ProfileEntity.DeleteFile`／`RenameProfile` が、注入した `IPathService` の保存先を使うこと。
- 既存のテスト（`ProfileList`／`AutoProfilesViewModel`／`PatternC`）を、新しいコンストラクターに合わせる。

### 11.6 実機確認
1. **ウィンドウの位置・サイズ**: ウィンドウを移動・リサイズして終了し、起動し直したときに復元されること。最小化起動（`-m`）でも崩れないこと。
2. **プロファイル一覧**: メイン画面の「プロファイル」タブの一覧が表示され、新規作成・複製・名前変更・削除が反映されること（名前変更・削除は `ProfileEntity` を通る）。
3. **コントローラー一覧のプロファイル選択**: 削除したプロファイルが、各コントローラーの選択肢から消えること。

### 11.7 リスク
- **`MainWindow` の初期化順序**: `profileListHolder` を使う箇所（153 行付近）は、`pathService` の解決より後。生成位置の移動に伴い、他のフィールドの初期化子から参照していないことを確認する。
- **`WindowPlacementHelper` の起動時の呼び出し**: ウィンドウの表示前（`SourceInitialized` など）に呼ばれる。`appSettingsService` は、コンストラクターで解決済みのため、順序の問題は無い見込み。

## 12. Step10-4-1 の実装記録。2026-10-15
- **決定**: E1＝A（`ProfileEntity.SaveProfile`／`FireSaved`／`ProfileSaved` を削除。呼出元 0 件、正規の保存経路と重複する並行実装）、E2＝A（`AutoProfileHolder` は 10-4-2 へ移す）。
- **変更**
  - `WindowPlacementHelper.ApplyPlacement` に `IAppSettingsService` を引数追加し、`Global.Form*` 16 件を置換。`MainWindow` は `appSettingsService` を渡す。
  - `ProfileList` は `IPathService` を必須引数にし、`Global.PathServiceInstance` のフォールバックを削除。`MainWindow` の生成位置を、`pathService` 解決後（コンストラクター内）へ移動。
  - `ProfileEntity` は `IPathService` を必須引数にし、`Global.appdatapath` を置換。未使用の `SaveProfile`／`FireSaved`／`ProfileSaved` を削除。
- **テスト**: 新規 `Step10FourOneGlobalReferenceGuardTests`（Global 参照の不在、未使用メンバー削除、必須引数、注入パスでの Refresh／Rename／Delete）。`AutoProfilesViewModelTests`／`PatternCViewModelTests` の `new ProfileList()` を更新。
- **状態**: ビルド・テスト・実機確認（§11.6）の結果待ち。

## 13. Step10-4-1 の完了と Step10-4-2 の計画（クラスタ 1: プロファイル系）。2026-10-15
- **Step10-4-1**: ビルド・テストビルド・テスト実行成功、実機確認（ウィンドウ位置・サイズ、プロファイル一覧、コントローラー選択肢）問題なし。

### 13.1 現行コードの再確認（クラスタ 1 の 6 クラス）
- **ProfileRepository**: 引数 3 つ（`IProfileSettingsService`、`IProfileXmlStore`、`IPathService`）が全て省略可。省略時は `Global.*Instance` へフォールバック。さらに `LoadProfile` 内に `AppHost.GetService<ControlService>()` が 1 件。`Global.ProfilePath` などの薄い委譲（約 25 件）は、契約（`IProfileRepository`）の実体なので温存対象。
- **SpecialActionRepository**: `BackingStore config = null`（省略時 `Global.store`）、`IPathService`（省略時 `Global.PathServiceInstance`）。
- **ProfileApplicationService**: 引数 5 つ全て省略可。省略時は `AppHost` の 5 件。`ControlService` を直接保持。`Global.ApplyProfile`×3、`Global.OlderProfilePath`×4（`IProfileRepository` に同名の契約がある）。`deviceIndex >= 4` の上限が 4 か所（K1）。
- **AutoProfileService**: `new AutoProfileHolder()`、`new EnvironmentService()`、`AppHost`×3、`Program.rootHub`×7、`Global.AutoProfileRevertDefaultProfile`×2（`IAppSettingsService` に契約あり）、`Global.ProfilePath`×2（`IProfileRepository`）、`autoProfileSwitchNotifyChoice`／`CreateAutoProfiles`（`IAutoProfileService` の薄い委譲で温存）。
- **ProfileActionChainService**: `AppHost` と `new MappingActionDispatcher()` のフォールバック。
- **ProfileSettingsService**: `Program.rootHub`×4（`DualSenseDevice` 判定・デバイス取得）、`new EnvironmentService()`、`Global.store` のフォールバック。薄い委譲（`RefreshActionAlias` ほか）は温存。
- **生成箇所**: DI 登録、`ScpUtil.cs` の Global 用の静的フォールバック 3 件（714・779・808 行）、`AutoProfileChecker`（フォールバック）、テスト多数（`ProfileSettingsService` 約 80、`ProfileRepository` 約 25、`SpecialActionRepository` 約 30）。
- **発見（§2.4 の対象）**: `AutoProfileChecker` は `new` する呼出元が 0 件。経緯の調査後、削除可否を 10-4-4 で判断する。

### 13.2 方針（案）
- **P1**: 省略可の引数を、必須引数（null は `ArgumentNullException`）にする。DI の登録は、すでに全依存を解決できるため、変更不要。
- **P2**: `Program.rootHub`／`AppHost` の `ControlService` は、`Func<ControlService>`（D4-3）で受け取る。`ProfileSettingsService` は `ControlService` を持たず、`DualSenseDevice` 判定のためだけに使うため、`Func<ControlService>` を使う（`ControlService` → `ProfileSettingsService` の依存循環を避けるため遅延解決）。
- **P3**: `Global` 用の静的フォールバック（`ScpUtil.cs` 714・779・808 行）は、シム側の実装であり、必須引数に合わせて明示的に引数を渡す形に変える。静的初期化順序のハザード（`Global.store` の初期化前の参照）を、契約テストで確認する。
- **P4**: `new EnvironmentService()`／`new MappingActionDispatcher()`／`new AutoProfileHolder()` は、必須引数にして削除する。
- **P5**: `AutoProfileHolder` は `IPathService` を必須引数にする（10-4-1 から移した分）。

### 13.3 テストへの影響
- テストの生成箇所を、必須引数に合わせて更新する（約 135 件。テスト用の共通ヘルパーを `DS4WindowsTests` に 1 つ用意して、置換を機械化する）。
- 新規ガード `Step10FourTwoGlobalReferenceGuardTests`: 6 ファイルに温存項目以外の `Global`／`AppHost`／`Program.rootHub` が無いこと、必須引数の null 拒否。

### 13.4 分割の提案
- 影響が大きいため、10-4-2 を次の 3 つに分けて、それぞれでビルド・テスト・実機確認を行う。
  - **10-4-2a**: `ProfileActionChainService`、`ProfileApplicationService`、`SpecialActionRepository`（依存が少なく、テストの更新も少ない）。
  - **10-4-2b**: `ProfileRepository`、`AutoProfileService`＋`AutoProfileHolder`。
  - **10-4-2c**: `ProfileSettingsService`（テストの更新が約 80 件。`Func<ControlService>` の導入）。

## 14. 決定と Step10-4-2a の実装記録。2026-10-15
- **決定**: G1＝A（10-4-2 を 2a／2b／2c の 3 分割）、G2＝A（テスト用の共通ヘルパー）、G3＝A（`AutoProfileChecker` は 10-4-4 で判断）。
- **10-4-2a の変更**
  - `ProfileActionChainService`: `IProfileActionProvider`／`IMappingActionDispatcher` を必須化。`AppHost` と `new MappingActionDispatcher()` のフォールバックを削除。
  - `ProfileApplicationService`: 5 引数を全て必須化。`ControlService` は `Func<ControlService>`（遅延解決。D4-3）。`AppHost` の 5 件を削除。`Global.OlderProfilePath`×4 は `IProfileRepository.OlderProfilePath`（同じ実体）へ置換。`Global.ApplyProfile`×3 は実装本体が `Global` にあるため温存し、TODO（Phase7）を付与。
  - `SpecialActionRepository`: `BackingStore`／`IPathService` を必須化。`Global.store`／`PathServiceInstance`／`appdatapath` のフォールバックを削除。`BackingStore` は DI 未登録のため、`ServiceRegistration` で `Global.store` を渡すファクトリー登録に変更。`Global` 側の静的フォールバックも、明示的に引数を渡す形へ。薄い委譲（`SaveAction` など 4 件）は温存。
- **テスト**: 新規 `Step10FourTwoGlobalReferenceGuardTests`、共通ヘルパー `TestRepositoryFactory`（約 30 件の生成箇所を置換）。
- **新たな発見（10-4-2b へ追加）**: `ProfileActionProvider` が `BackingStore config = null`（省略時 `Global.store`）。クラスタ 1 の仲間として、10-4-2b で必須化する。
- **実機確認項目**: スペシャルアクションの一覧表示・追加・削除・保存、プロファイル切替アクション（ボタン割り当てからの切替と、元のプロファイルへの復帰）、アクション連鎖。

## 15. Step10-4-2a の完了と、Step10-4-2b の計画。2026-10-15
- **Step10-4-2a**: ビルド・テストビルド・テスト実行成功。実機確認（スペシャルアクションの一覧・追加・削除・保存、プロファイル切替と元への復帰）問題なし。アクション連鎖の確認は、下記の決定により省略。
- **決定（アクション連鎖の廃止＝案A）**: 「プロファイル切替アクションの後に、新プロファイルの同じトリガーのアクションを続けて発火させる」連鎖（`ProfileActionChainService`。移行前の `Mapping.cs` の処理を `b8c914c2` で切り出したもの）は、トリガーを再成立させない限り追加のアクションを実行しない仕様へ変更する。実施は 10-4-2 の完了後に、別の計画（Step10-4-2d として提案）で行う。2a は挙動を維持した。
- **K1 の再確認（現行コード）**: `ProfileApplicationService`（4 か所）と `ProfileActionChainService`（1 か所）が `deviceIndex >= 4` で弾いている。一方、スロットの上限は `IEnvironmentService.ControllerSlotLimit`（最大 8）。移行前の `Mapping.cs` も `device >= 4` であり、**移行前から存在する上限**（回帰ではない）。5〜8 番目のコントローラーでは、プロファイル切替アクションと復帰が動かない可能性がある。是正の可否はユーザー判断（10-4-2b の決定 H3）。

### 15.1 Step10-4-2b の対象と方針（案）
- **ProfileRepository**: 引数 3 つを必須化し、`Func<ControlService>` を追加（`LoadProfile` 内の `AppHost` を置換）。`Global.ProfilePath` は自身の `ProfilePath`（同じ実体）へ置換。`Global.loggedInvalidActions.Clear()` は温存（TODO）。他の薄い委譲は温存。
- **ProfileActionProvider**: `BackingStore` を必須化（`ServiceRegistration` で `Global.store` を渡すファクトリー登録）。
- **AutoProfileHolder**: `IPathService` を必須化（`Global.appdatapath` の置換）。
- **AutoProfileService**: 全引数を必須化し、`new AutoProfileHolder()`／`new EnvironmentService()`／`AppHost` を削除。`Program.rootHub`×7 は `Func<ControlService>`、`Global.AutoProfileRevertDefaultProfile`×2 は `IAppSettingsService`、`Global.ProfilePath`×2 は `IProfileRepository` へ。`autoProfileSwitchNotifyChoice`／`CreateAutoProfiles` は薄い委譲として温存。
- **`Global` 側の静的フォールバック**（`fallbackProfileRepository`）: 必須引数に合わせ、遅延生成（初回アクセス時）に変更する。静的初期化順序のハザードを避けるため。
- **AutoProfileChecker（決定 H1）**: `AutoProfileService` の必須化に合わせ、`AutoProfileService` を `new` する分岐（呼出元 0 件）が、そのままではコンパイルできない。案A（推奨）: 型は温存し、`IAutoProfileService` が解決できない場合は例外にする（削除の判断は 10-4-4）。案B: 本ステップで削除する。
- **テスト**: `TestRepositoryFactory` に `CreateProfileRepository` ほかを追加（約 25＋10 件）。`Step10FourTwoGlobalReferenceGuardTests` に対象を追加。
- **実機確認項目**: プロファイルの選択・保存・読み込み、自動プロファイル（Auto Profiles 画面での編集と、対象プロセス起動時の切替、終了時の復帰、「サービスを一時停止」の動作）。

## 16. Step10-4-2b の実装記録。2026-10-15
- **決定**: H1＝A（`AutoProfileChecker` は型を温存し、`IAutoProfileService` が未解決なら例外。削除の判断は 10-4-4）、H3＝A（K1 は 10-4-2 では触れず、別の小ステップで提案）。
- **変更**
  - `ProfileRepository`: 4 引数（`IProfileSettingsService`、`IProfileXmlStore`、`IPathService`、`Func<ControlService>`）を必須化。`AppHost`／`Global.*Instance` のフォールバックを削除。`Global.ProfilePath`／`ProfileActions` の内部参照は自身のプロパティへ。薄い委譲は温存。
  - `ProfileActionProvider`: `BackingStore` を必須化（`ServiceRegistration` で `Global.store` を渡す）。
  - `AutoProfileHolder`: `IPathService` を必須化（`Global.appdatapath` を置換）。
  - `AutoProfileService`: 8 引数を全て必須化。`new`／`AppHost` を削除。`Program.rootHub`×7 → `Func<ControlService>`、`Global.AutoProfileRevertDefaultProfile`×2 → `IAppSettingsService`、`Global.ProfilePath`×2 → `IProfileRepository`。
  - `AutoProfileChecker`: `new AutoProfileService` の分岐を例外に変更（TODO 付き）。
  - `ProfileEditor`: `?? new ProfileRepository()` のフォールバックを例外に変更（DI 登録済みのため挙動は同じ）。
  - `Global` の `fallbackProfileRepository`: 初回アクセス時の生成に変更。
- **注意（挙動の差）**: `AutoProfileService` が `ControlService` を `Func`（DI の解決）経由で取得するため、`Program.rootHub` が未設定の時点で呼ばれると、DI が `ControlService` を生成する可能性がある。実機では、起動後のタイマーから呼ばれるため `rootHub` は設定済みの見込み。実機確認で、自動プロファイルの動作を確認する。
- **テスト**: `TestRepositoryFactory` に `CreateProfileRepository` ほか 3 つを追加し、生成箇所を置換。`Step10FourTwoGlobalReferenceGuardTests` に 4 ファイルの `Global`／`AppHost` 不在、必須引数、注入パスでの `AutoProfileHolder` の読み込みを追加。

## 17. Step10-4-2b の完了と、Step10-4-2c の計画（ProfileSettingsService）。2026-10-15
- **Step10-4-2b**: ビルド・テストビルド・テスト実行成功、実機確認（プロファイル、自動プロファイル、サービスの一時停止）問題なし。

### 17.1 現行コードの再確認
- **ProfileSettingsService**: 引数 `BackingStore backingStore = null`（省略時 `Global.store`）、`IEnvironmentService environmentService = null`（省略時 `new EnvironmentService()`）。`SafeConfig => _config ?? Global.store`。
- **`Program.rootHub`×4**（2 メソッド）: `GetRumbleBoost`（DualSense の判定。レポートごとの経路）と `SetRumbleAutostopTime`（接続中のデバイスへの反映）。いずれも、スロット番号のデバイスを取るだけ。`ControlService.GetController(int)` が同じ値を、範囲外安全に返す。
- **`Global.ProfileActions`**: `ProfileActions` プロパティの取得のフォールバックと、設定時の同期ループ。`SafeConfig` が `Global.store` と同一なら、同期ループは同じ配列への代入で、何も起きない。
- **生成箇所**: DI 登録（`BackingStore` が未登録）、`ScpUtil.cs` 714 行の静的フォールバック、テスト約 80 件（引数なし約 60、`BackingStore` 指定約 15、環境サービス指定数件）。
- 薄い委譲（`RefreshActionAlias`、`RefreshExtrasButtons`、`defaultButtonMapping`、`reverseX360ButtonMapping`）は温存。

### 17.2 方針（案）
- **I1**: `BackingStore`／`IEnvironmentService` を必須化。DI は `ServiceRegistration` で `Global.store` を渡すファクトリー登録。`Global` の静的フォールバックは、明示的に渡す。
- **I2**: `Program.rootHub` の 4 件は、`Func<ControlService>`（D4-3）で受け取り、`GetController(index)` で取得する。`ControlService` の生成が `IProfileSettingsService` に依存するため、遅延解決が必須。レポートごとの経路（`GetRumbleBoost`）では、1 回の `Func` 呼び出し（DI の Singleton の取得）が増える。割り当てテスト（`MappingHotPathAllocationTests` など）で確認する。
- **I3**: `Global.ProfileActions` のフォールバックと同期ループは、削除する（`SafeConfig` が非 null になるため。本番では同じ実体で、挙動は変わらない）。
- **テスト**: `TestRepositoryFactory.CreateProfileSettingsService(store = null, env = null, control = null)`（既定: `Global.store`、`new EnvironmentService()`、`() => null`）で、約 80 件の生成箇所を置換。ガードテストに対象を追加。
- **実機確認項目**: プロファイルの読み込み・保存、振動（DualSense を含む。DualSense の振動強度の補正）、振動の自動停止時間の設定変更が、接続中のコントローラーに反映されること。

## 18. Step10-4-2c の実装記録。2026-10-15
- **決定**: I1（必須化）、I2＝A（`Func<ControlService>`）、I3＝A（`Global.ProfileActions` の同期ループとフォールバックを削除）。
- **変更**: `ProfileSettingsService` は、`BackingStore`／`IEnvironmentService`／`Func<ControlService>` を必須化。`Program.rootHub`×4 を `GetController(index)` へ、`Global.store`／`new EnvironmentService()` のフォールバックを削除。`ServiceRegistration` はファクトリー登録（`Global.store` を渡す）。`Global` の静的フォールバックは、明示的に引数を渡す形へ。
- **テスト**: `TestRepositoryFactory.CreateProfileSettingsService`（79 件の生成箇所を置換）、`Step10FourTwoGlobalReferenceGuardTests` に対象を追加。
- **実機確認項目**: プロファイルの読み込み・保存、振動（DualSense の補正を含む）、振動の自動停止時間の変更の反映。
- **後続**: クラスタ 1 は完了。続けて、アクション連鎖の廃止（10-4-2d）と K1（`>= 4` の上限）の小ステップを提案し、その後 10-4-3（出力スロット系）へ進む。
