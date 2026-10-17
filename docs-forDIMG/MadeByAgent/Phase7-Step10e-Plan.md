# Phase7-Step10e 計画書: 仕上げと総合検証

作成日: 2026-10-18  
対象ブランチ: `For-DI-migration-work`  
上位計画: `Phase7-Step10-Plan.md`（Q1〜Q9 は推奨どおり承認）§3「10e」・Q8・Q9、`Phase7-Plan.md` §5（完了条件）、`Phase7-Step10d-Completion-Report.md` §5、`Phase7-Residual-Ledger.md` §13〜§16  
状態: **計画書の作成まで（調査完了）。Q1〜Q10 の承認待ち。実装は未着手。**

---

## 1. 目的と位置づけ

- Phase7 の最後のサブ Step。次の 3 つを行う。
  - 残った静的な窓口（`ControlService.CURRENT_DS4_CONTROLLER_LIMIT`、`AppHost` の暗黙のホスト構築）を片づけ、持ち越した判断（`ColorUtil` の孤立コード、0 件の DI メンバー）を決着させる。
  - 残る静的な状態と、Phase8 へ渡すものを台帳に確定する。ラチェットの最終基準値を確定する。
  - ドキュメント（モデル図 01〜04、`copilot-instructions.md`、台帳）を最終状態にし、**総合検証（実機確認の一括、GO／NO-GO）** と Phase7 完了報告書へ進む。
- 機能・ログ・起動順序は変えない。

## 2. 現状（実測）

- ラチェット: `Global.*` **2**（ログ文言の文字列のみ。`Global.cs` は削除済み）、`AppHost.GetService|Services` **3**（`App.xaml.cs` の窓口 1、`AppHost.cs` のログ 2）、`Program.rootHub` **0**、`ServiceProviderHolder` **17**（`Actions/` と `AppHost.cs`／`ServiceProviderHolder.cs` ほか。Phase8）。ビルド警告 0。複数型ファイル 0。
- `ControlService.CURRENT_DS4_CONTROLLER_LIMIT`（静的シム）: 本体 **16 ファイル、31 箇所**（`EnvironmentService` の注記を除く）、テスト 9 ファイル 14 箇所。内訳:
  - サービス・シリアライザー: `OutputSlotManager` 3、`LegacyProfileXmlSerializer` 2（`IEnvironmentService` を持つ）、`ProfileRepository` 1、`ProfileSlotApplier` 1、`ControlService` 2（`_environmentService` を持つ）、`AutoProfilesDTO` 1。
  - 画面・ViewModel: `ProfileEditor` 5、`CheckBatteryViewModel` 3、`MainWindow` 2、`AutoProfiles` 2、`ProfileSettingsViewModel` 2（コメント 1）、`SpecialActionEditor` 1、`BindingWindowViewModel` 1、`CurrentOutDeviceViewModel` 1、`RecordBoxViewModel` 1。
  - `IEnvironmentService` を既に持たないクラス（コンストラクター変更が要る）: `ProfileRepository`、`ProfileSlotApplier`、`ProfileEditor`、`CheckBatteryViewModel`、`AutoProfiles`、`SpecialActionEditor`、`BindingWindowViewModel`、`CurrentOutDeviceViewModel`、`RecordBoxViewModel`（`ProfileSettingsViewModel` と `MainWindow` は持つ）。`AutoProfilesDTO.MapTo` は、`AutoProfileHolder`（`IEnvironmentService` を持つ。10b）から値を渡す。
- `AppHost`: 本体の `GetService` の呼び出しは `App.ResolvePostHostService` の 1 件だけ。`CreateHost` は `App` の 2 箇所（通常の起動と `-driverinstall`）。**暗黙のホスト構築**（`GetService` がホスト未構築のときに作る）は、本体では使われず、**テストが使う**（`AppHost.GetService`／`CreateHost` を **55 ファイル、277 箇所**）。`CreationNote`（暗黙構築の呼び出し元スタックを含む診断文字列）は、K7-1 の原因特定用に作った。
- `Mapping` のコンストラクター引数 **11**、`MainWindow` のコンストラクター引数 **18**（いずれも Step10b／Step8 で記録済み。意図した依存で、循環はない）。
- `ColorUtil` の孤立コード: `applyRatio(Color, Color, uint)` と `HuetoRGB(float, float, float)`（移動前から呼び出し元 0 件。TODO 付きで温存中。削除は未承認）。
- DI インターフェースで、本体からの呼び出しが 0 件のメンバー（ヒューリスティックな走査。W4 の冒頭で経緯を確認する）:
  - `IAppSettingsService.SettingChanged`／`CheckWhen`、`IAutoProfileService.ClearState`、`IDeviceStateService.DeviceSerialChange`（10d W4 で `Global` から移した。本体に購読者なし）、`IKeyButtonControllerRegistry.GetOrCreate`、`IPathService.GetProfilePath`／`IProfileRepository.GetProfilePath`、`IProfileSettingsService.UseTempProfileArray`／`SATriggerCond`、`ISpecialActionRepository.ActionsChanged`、`IUdpServerService.Port`。
  - 既に「使うべきだが未接続」として TODO 付きで温存している契約（`Phase7Step2RemovedContractMembersGuardTests` の `Kept`）の現状も、同時に確認する（`UsingMaxControllers`、`ReEnableDevice` は接続済み）。
- 残る静的な状態: `PathService`／`EnvironmentService` の静的な状態（プロセス全体で 1 つ。起動前に必要）、`ServiceRegistration._subscribedReset`、`AppHost`（静的なホスト）、`DS4LightBar` の静的な配列、`DS4Devices.LayerSettings`、`SyntheticDispatcher` の橋渡し（Phase8）。

---

## 3. サブ Wave の構成（案）

- **W1: `CURRENT_DS4_CONTROLLER_LIMIT` のサービス・シリアライザー・DTO の分**（約 9 箇所、本体 6 ファイル）
  - `OutputSlotManager` は上限をコンストラクターで受ける（`ControlService` が `_controllerSlotLimit` を渡す）。`ProfileRepository`／`ProfileSlotApplier` に `IEnvironmentService` を追加。`LegacyProfileXmlSerializer` と `ControlService` は注入済みの値へ。`AutoProfilesDTO.MapTo` は `AutoProfileHolder.UsingMaxControllers`（10b の値）を使う。
- **W2: 画面・ViewModel の分**（約 22 箇所、本体 10 ファイル）と静的シムの削除
  - 各クラスに `IEnvironmentService` を渡す（`ViewModelFactory`／`EditorWindowFactory`／`MainWindowFactory` 経由。画面は既にファクトリー経由）。テスト 14 箇所は `new EnvironmentService().ControllerSlotLimit` ほかへ。静的シム `ControlService.CURRENT_DS4_CONTROLLER_LIMIT` を削除。`EnvironmentService.ProcessControllerSlotLimit`（internal static。純粋な OS 判定の計算）は残す。
- **W3: `AppHost` の暗黙のホスト構築の廃止**（Q3）
  - `GetService` はホスト未構築のとき、作らずに `null` を返す（Dispose 後と同じ扱い）。テストは、共有のホストを作る入口（`TestHost`。`TestGlobals` と同じ場所）を通す。277 箇所を機械的に置き換える。
- **W4: 判断の決着**（Q5・Q6）: `ColorUtil` の孤立コード、0 件の DI メンバー。§2.4 の手順（経緯の調査→判断→記録→承認）。
- **W5: ガード・ラチェットの最終化と台帳**（Q7・Q8・Q9）: 基準値の最終確定、基準値と実数の一致を検査するテスト、静的な状態の目録、`Mapping`／`MainWindow` の引数の記録。
- **W6: 文書の最終化**（Q10）: モデル図 01〜04、`copilot-instructions.md`、台帳、実機確認チェックリストの整理（総合検証の手順）、`Phase7-Status.md`。
- **総合検証（ユーザーが実施）**: チェックリストに従って実機確認を一括して行い、GO／NO-GO を判断する（Q4）。
- **完了**: 結果を受けて Phase7 の完了報告書（`Phase7-Completion-Report.md`）と `Phase7-Step10e-Completion-Report.md` を作成する。

依存関係: W1 → W2（静的シムの削除は W2 の最後）。W3、W4 は独立。W5 は W1〜W4 の後。W6 は W5 の後。総合検証は W6 の後（ビルドが確定してから）。

---

## 4. 決定事項と質問（推奨つき）

### Q1. Wave の構成
- 推奨: **A. 上記の W1〜W6 を、各 Wave の後にユーザーがビルド・テストを実行する形で進める。総合検証は W6 の後**。
- B: W1 と W2 を一つにする。 → 画面の変更（9 クラス）とサービス側が同時になり、失敗の切り分けが難しい。

### Q2. `CURRENT_DS4_CONTROLLER_LIMIT` の置き換え方
- 推奨: **A. 注入された `IEnvironmentService.ControllerSlotLimit` に替え、静的シムを削除する**。上限は設定画面などで実行中に変わらない（プロセス全体で一定）ため、起動時の値を渡す。`OutputSlotManager` は整数で受ける。
- B: シムを残す。 → 静的な窓口が残り、Phase7 の終了時に `ControlService` の静的状態が 1 つ残る。

### Q3. `AppHost` の暗黙のホスト構築
- 推奨: **A. 廃止する（`GetService` は作らない）**。本体は使っておらず、K7-1 の残存経路を完全に閉じられる。テストの 277 箇所は、機械的に共有のホスト取得の入口（`TestHost`）へ置き換える（W1 と同様のスクリプト）。ホストが未構築のまま `GetService` が呼ばれたときは、ログを出して `null` を返す（Dispose 後と同じ）。
- B: 維持し、理由を記録する。 → テストの改修は不要だが、暗黙構築が将来また混入するリスクが残る（現状も `CreationNote` で検出する運用）。

### Q4. `CreationNote` と `[DI] Host creation path` のログ
- 推奨: **A. 残す（ユーザー決定の維持。copilot-instructions §2.3）**。ただし暗黙構築がなくなるため、`CreationNote` は「explicit CreateHost(config, parser)」または「host already existed」だけになる。値の組み立て（呼び出し元スタック）は削除する。
- B: プロパティとログを削除する。 → ログの削除はユーザーの承認が要る。診断の価値は下がるが、保持してもコストはない。

### Q5. `ColorUtil` の孤立コード（`applyRatio(Color…)`、`HuetoRGB`）
- 推奨: **A. 削除する**。移動前から呼び出し元 0 件で、DI の契約でもなく、正規の経路と重複する WPF 色のブレンドの残骸（§2.4 の「使うべきでないもの」）。ただし機能の削除に当たりうるため、**あなたの承認が要る**（§2.2）。削除後、`ColorUtil` から `System.Windows.Media` への依存が不要になるなら、それも整理する。
- B: 残す（TODO を維持）。 → 孤立コードが Phase7 の後も残る。

### Q6. 呼び出し 0 件の DI メンバーの決着
- 推奨: **A. W4 の冒頭に、各メンバーの経緯（`git log -S`、当時の計画書）を調べ、§2.4 の基準で「削除」「接続（TODO 付き）」「Phase8 へ移管」を判断し、台帳と本計画書の W4 の節に記録して、承認を得てから実装する**。見込み: `IDeviceStateService.DeviceSerialChange`（購読者なし。旧 `Global` のイベントも同様）と `IProfileSettingsService.SATriggerCond` などは削除候補、`ISpecialActionRepository.ActionsChanged` と `IAppSettingsService.SettingChanged` は画面への通知の契約として接続候補。結論は調査後。
- B: すべて Phase8 へ移管する。 → Phase7 の完了条件（0 件メンバーの判断済み）を満たさない。

### Q7. `Mapping` の依存 11、`MainWindow` の引数 18
- 推奨: **A. 変更しない。台帳に「意図した依存（Phase8 の Actions 統合・ウィンドウ分割の候補）」として記録する**。循環はなく、各依存は使われている。
- B: 束ねる型を作る（Facade）。 → 効果に対して差分が大きく、Phase8 の再設計と衝突する。

### Q8. 静的な状態の目録
- 推奨: **A. 台帳に目録を確定する（§2 の「残る静的な状態」）。各項目に、残す理由と、解消する Phase を付ける**。コードは変えない。
- B: 一部を DI の Singleton に替える。 → `PathService`／`EnvironmentService` の静的な状態は起動前（コンテナ構築前）に必要で、替えられない。

### Q9. ラチェットの最終基準値
- 推奨: **A. 最終基準値を確定する（`Global.*` 2、`AppHost` 3→W3 後の実数、`Program.rootHub` 0、`ServiceProviderHolder` 17）。さらに、基準値と実数の一致（超過だけでなく、余りも検出）を検査するテストを追加し、更新漏れを防ぐ**（10c の教訓: 基準値の取りこぼしは「超えない」検査では見つからない）。Phase8 で `ServiceProviderHolder` を減らすときも、同じ一致検査を使う。
- B: 現状の「超えない」検査のままにする。 → 余りが残る。

### Q10. ドキュメントの最終化と `copilot-instructions.md` の更新
- 推奨: **A. W6 で次を行う。承認後に実施する**。
  - モデル図 01〜04: Phase7 完了時点の構造へ最終更新（`Global`／`ScpUtil` の注記を、削除済みの事実に直し、新設した型［永続化 6 型、`ControllerLimits`、`IDeviceLayerSettings` ほか］を反映）。
  - `copilot-instructions.md`: §2.1（`Global` の薄いシムの記述を、Phase7 で削除した旨に更新。今後の静的経路の扱い）、§3.1（`BackingStore` のコンストラクター注入、ブートストラップの方式）、§3.3 原則 4（ラチェットの一致検査）、最終更新日。変更はプロジェクトの規約文書のため、**あなたの承認が要る**。
  - 台帳（§17）、`Phase7-Status.md`、実機確認チェックリスト（§12 に総合検証の手順を追加）。
- B: 文書は Phase8 の開始時に更新する。 → 構造と文書がずれたまま Phase7 を閉じることになる。

---

## 5. 総合検証（実機確認の一括）

- 範囲: `Phase6-Step11-RealDevice-Verification-Checklist.md` §11 の U-11〜U-21（U-1〜U-7 は機材・環境がなく確認不能のままリスク受容。U-8〜U-10 は確認済み）。U-13 は先送りしたままだが、総合検証で実施するか、リスク受容とするかを、検証時にあなたが決める。
- 追加項目（10a・10b の記録から）: トレイアイコンの切り替え、プロファイル編集の画像の表示、自動プロファイル画面（8 台表示）、ライトバーの色の遷移、自動プロファイルの保存。10d の U-21（起動直後のログ設定、言語・テーマ、バッテリー表示、Moonlight、デバウンス、ライトバー、`-driverinstall`）。
- 進め方: W6 で、重複する項目を整理した「総合検証の手順」（所要時間の目安つき）をチェックリストの §12 に作る。あなたが実施し、結果（○／×／不能）を報告する。× があれば原因を調べて是正し、再検証する。30 分の連続操作と終了処理（ガードレール 5.5）を含める。
- 判定: ○ の割合と × の有無で GO／NO-GO（Phase6-Step11 の前例）。GO で Phase7 完了報告書を作成する。

## 6. リスクと対策

- W2（画面・ViewModel）: コンストラクターの変更が 9 クラスと、ファクトリー・テストに波及する。対策: 既存のガード（`Constructor_Requires…` 系）を更新し、`TestRepositoryFactory`／`ViewModelFactory` に集約する。上限の値は `EnvironmentService` が静的に計算した同じ値（`ProcessControllerSlotLimit`）なので、挙動は変わらない。
- W3（暗黙構築の廃止）: テストの 277 箇所の置き換え。対策: `TestGlobals` と同じ方式で、コード部分だけをスクリプトで置換。ホストの Dispose 後の取得は、最後の実体を保持する（10d と同じ）。本体の影響はない（`App.ResolvePostHostService` はホスト構築後にしか呼ばれない）。
- W4（削除の判断）: 機能の削除に当たりうる。対策: すべてあなたの承認の後に実装する。
- 起動順序・ガードレール 5.1〜5.6 への影響なし（コンストラクターの値の受け渡しのみ）。

## 7. 完了条件

- `ControlService.CURRENT_DS4_CONTROLLER_LIMIT` が存在しない。`AppHost` の暗黙構築がない（Q3＝A の場合）。
- `ColorUtil` の孤立コード、0 件の DI メンバーの全件に判断が記録されている。
- ラチェットの最終基準値が確定し、一致検査が成功している。ビルド警告 0、全テスト成功。
- モデル図 01〜04、`copilot-instructions.md`、台帳、チェックリストが最終状態。
- 総合検証が実施され、GO／NO-GO の判定が出ている。Phase7 完了報告書がある。

## 8. 見込みの数値

- `Global.*` 2（変更なし）、`AppHost.GetService|Services` 3（W3 の後に 3 以下）、`Program.rootHub` 0、`ServiceProviderHolder` 17（Phase8）。
- 本体の `ControlService.CURRENT_DS4_CONTROLLER_LIMIT` 31 箇所 → 0。
