# Phase7 完了報告書: `Global`／`BackingStore` の解体、起動の近代化、`Mapping` の instance 化

作成日: 2026-10-18  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase7-Plan.md`。調査: `Phase7-0-Survey.md`。進捗: `Phase7-Status.md`。残存台帳: `Phase7-Residual-Ledger.md`  
状態: **完了（総合検証 GO。2026-10-18 宣言、ユーザー承認）**

---

## 1. 目的と結論

- 目的: 静的クラス `Global` と、その中の `BackingStore` を DI のサービスへ移し、静的アクセスと Service Locator をコンストラクター注入に置き換える。`Mapping` を instance 化する。機能・ログ・安全弁は維持する（`.github/copilot-instructions.md` §2）。
- 結論: **目的を達成した。** `Global` の型は存在しない。`Program.rootHub` は 0。`AppHost.GetService` は `App` の窓口 1 件（と `AppHost` 自身のログ文言 2 件）。`Mapping` は DI の Singleton の instance。互換 `AppHost` は削除済み。Service Locator（`ServiceProviderHolder`）は Actions サブシステムの 17 件だけが残り、Phase8 の対象。
- 完了条件（`Phase7-Plan.md` §5）の確認:
  - ラチェットの基準値が目標値に達した（§3）。
  - `Mapping` の instance 化と、互換 `AppHost` の削除。済み（Step8・Step9）。
  - DI インターフェースの 0 件メンバーの判断。済み（Step2、Step10e）。
  - ビルド警告 0、全テスト成功（ユーザーが各 Wave で実行）。
  - 実施可能な実機確認の合格（総合検証 GO。先送りした項目と残課題は §4・§5）。
  - モデル図の更新、本報告書。済み。

## 2. Step ごとの成果

- Phase7-0: 調査と Step 分割案（D1〜D6 承認）。
- Step1: 整理と台帳の確定。孤立メソッド 10 名（396 行）の削除、K2 の是正、不具合 B（アプリ設定の DTO 読込の後処理の欠落）の是正、`ServiceProviderHolder` の追跡。
- Step2: DI インターフェースの 0 件メンバー（49）の個別判断（削除 45／接続 3／温存 1。Phase8 へ 12）。
- Step3・Step3c: `BackingStore` の DI 登録、`Global` の転送の削除（`AppSettingsService`、`ProfileRepository`、外観、自動プロファイルほか）。プロファイル適用・プリセット・イベントの移管（`ProfileApplicationService`、`ProfilePresetLoader`、`ProfileSlotApplier`）。
- Step4: 起動・終了シーケンスの近代化。ViGEmBus／HidHide／FakerInput／OS の調べ物と状態を `EnvironmentService` へ、実行ファイル情報・保存場所を `PathService` へ。ブートストラップの `PathService`／`EnvironmentService`、`-driverinstall` の明示的なホスト構築（K7-1 の残存経路）、終了時の `AppHost.Dispose()`。
- Step5: `Program.rootHub` の削除、Service Locator の削減（`IMainWindowFactory`、`IEditorWindowFactory.CreateProfileEditor`、`IElevatedProcessLauncher` の注入）。DS3 のジャイロトリガーの不具合（DI 移行による回帰）の修正。
- Step6: 出力デバイス種別の所有権を `OutputSlotService` へ。
- Step7・Step8・Step9: `Mapping` の instance 化。特性化テスト、状態の切り出し（`SyntheticInputState`／`MacroRuntimeState`／`StickMotionState`）、`IMapping`／`Mapping`（Singleton）、`KeyButtonControllerRegistry`、出力 KBM ハンドラの実体を `OutputKBMHandlerAdapter` へ、互換 `AppHost` の削除、`ControllerLimits`／`NumericUtil`、K3（マクロの二重実行防止）を `MacroRuntimeState` に包む。
- Step10a〜10e: 仕上げ。
  - 10a: 定数・資源パス・`Clamp` の再配置。
  - 10b: 表と変換、アイコンの表・色の関数、契約の整理、`UsingMaxControllers` の接続。
  - 10c: `BackingStore` の永続化を 6 つの型へ切り出し、ゴールデンテスト 11 ファイル（`BackingStore.cs` 6,522 → 1,660 行）。
  - 10d: `Global` の削除、`BackingStore(IEnvironmentService)` のコンストラクター注入、ブートストラップの共有、第 1 層の設定（`IDeviceLayerSettings`）。
  - 10e: `CURRENT_DS4_CONTROLLER_LIMIT` の削除、`AppHost` の暗黙構築の廃止、0 件メンバーの決着、ラチェットの最終化（一致検査）、総合検証。
- 各 Step の詳細は、`Phase7-Step*-Completion-Report.md` を参照。

## 3. 数値の推移（全域の直接参照）

`Global.*`／`AppHost.GetService|Services`／`Program.rootHub`／`ServiceProviderHolder`:
- Phase6 完了時（2026-10-17）: 747／40／22／（未追跡）
- Phase7-Step1 W4 後: 733／40／22／23
- Step3 W5 後: 516／40／22／23
- Step4 W5 後: 452／39／21／23
- Step5 W6 後: 446／11／0／22
- Step8 W5 後: 399／5／0／17
- Step9 W4 後: 355／4／0／17
- Step10a W5 後: 107／4／0／17
- Step10c W7 後: 69／4／0／17
- Step10d W7 後: 2／3／0／17
- **Step10e W5 後（最終）: 2／3／0／17**
- 残る `Global.*` の 2 件は、ログ文言の文字列（`"Global.UseLang …"`）で、コードの参照ではない。`AppHost` の 3 件は、`App` の窓口 1 件と `AppHost` 自身のログ文言 2 件。
- ガード: `Step10FourGlobalRatchetGuardTests`、`Phase7Step1ServiceLocatorRatchetGuardTests`、基準値と実数の一致を検査する `Phase7Step10eW5RatchetEqualityGuardTests`。複数型ファイルは 0。

## 4. 実機確認の結果

- 総合検証（`Phase6-Step11-RealDevice-Verification-Checklist.md` §12。結果は §12.6）: 必須の 8 項目を実施。○ 7、△ 1、× 0。判定 **GO**。
  - ○: 起動・ログ・設定の復元（T-1）、設定ファイルの互換（T-2）、接続・適用・Plug／Unplug（T-3）、通常入力・割り当て・切断（T-4）、プロファイル編集画面（T-8）、自動プロファイル画面（T-9）、10 分の連続使用・終了・再起動（T-14）。
  - △（T-6）: マクロ、通常のプロファイル切替、プログラム起動、キー、ボタン、Actions.xml の再読み込みは ○。「Unload on trigger release」で元のプロファイルに戻らない（既存の不具合。§5.1）。
- 先送り（ユーザー判断）: T-5（ジャイロ・タッチ・フリック）、T-7（出力種別・出力スロットの詳細）、T-10（トレイ・通知・ライトバー）、T-11（設定画面・言語・テーマ・更新確認）、T-12（第 1 層の設定）、T-13（起動引数）、T-8 のうち振動テスト・ジャイロ校正・ライトバーの即時反映。対応する U-11〜U-22 のうち、総合検証で直接確認していない部分は「未」のまま残す（リスク受容。特性化テスト・ゴールデンテスト・ガードで構造は固定している）。
- U-1〜U-7: 機材・環境が揃うまで未確認（Phase6 から継続のリスク受容）。U-8〜U-10: 確認済み（Phase6〜Step1）。

## 5. 残課題と引き継ぎ

### 5.1 別の計画書で扱う（Phase7 の範囲外。承認が要る）
- **既存の不具合: 「Unload on trigger release」で元のプロファイルに戻らない。** プロファイル切替の適用処理が `ActionManager.ClearDeviceState` を呼び、`Mapping.HandleDeviceDisconnect` が `deviceRuntime[device]` を作り直すため、復帰の合図（`UntriggerAction`）も消える。Phase6 完了時点も同じ構造（Phase7 の退行ではない）。上流の元の実装は復帰できていた。是正案は、`ClearDeviceState` の前後で `UntriggerAction`／`UntriggerIndex` を `IMapping` 経由で退避・復元すること。詳細は `Phase7-Step10e-Completion-Report.md` §3.4。
- **L2／R2 のデッドゾーンの仕様変更の要望**: 現行は「超えた点を 0% として引き伸ばす」（Phase6 完了時点・上流と同一）。要望は「超えたら押し込み量そのまま」。アンチデッドゾーンとの関係の整理が要る。

### 5.2 Phase8 へ
- `Actions/` の `ServiceProviderHolder` 17 件、`Actions/` の未配線の抽象 11 メンバー（`IActionBinding`、`IKeyController`、`IActionRegistry`、`IControllerRegistry`、`IInputAction`、`IMacroPlayer`、`ITrigger`）、`Controls`／`SpecialActions` の統合ディスパッチ。
- 第 1 層（`DS4Devices`／`DS4LightBar`／`HidDevices`）の静的な状態。静的な状態の目録は台帳 §17.3（起動前に必要な `PathService`／`EnvironmentService` の状態などは残す）。
- 維持して `TODO(Phase8)` を付けたメンバー（`IAppSettingsService.SettingChanged`、`ISpecialActionRepository.ActionsChanged`、`IProfileSettingsService.UseTempProfileArray`）の接続。
- `BackingStore` のフィールド群の分解、`Mapping` の依存 11・`MainWindow` の引数 18 の整理（台帳 §17.4）、K3（マクロの二重ガード）の恒久的な再設計。
- 起動直後のログローテーションとログの最小レベルが `BackingStore` の既定値になる課題（DI 移行前からの動作。記録のみ。直す場合は承認が要る）。

### 5.3 規約・文書
- `.github/copilot-instructions.md`: §2.1（シム維持は過渡期の記述に。静的な窓口を新設しない）、§3.1（暗黙のホスト構築の禁止、ラチェットの一致検査）を更新済み。
- モデル図 01〜04: Phase7-Step10e までの注釈を反映済み。

## 6. 最終確認

- Step1〜Step10e の各 Wave のビルド・テストビルド・テスト実行: すべて成功（ユーザーが実行）。
- 実機確認: 総合検証（必須の 8 項目）は GO。先送りと U-1〜U-7 はリスク受容として §4 に記録。
- 判定: **Phase7 の完了条件を満たした。** 宣言日: 2026-10-18（ユーザー承認）。
