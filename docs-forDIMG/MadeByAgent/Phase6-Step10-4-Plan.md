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
