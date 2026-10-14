# Phase6-Step9b 完了報告書: PR #1 の Copilot レビュー指摘 14 件の検証と是正

完了日: 2026-10-14  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase6-Step9b-Plan.md`（各指摘の検証結果は §1、決定は §2A、実施記録は §2B）  
参照: PR #1（`gwin7ok/DS4Windows-Vader4Pro`、`For-DI-migration-work` → `master`）への Copilot の自動レビュー（2026-10-13 22:54 JST、指摘 14 件）

---

## 1. 結果の要約

- 指摘 14 件を、現行コードと置換前の実装（`git log -S`、`git show`）を比べて検証し、分類した。自動レビューの指摘をそのまま採用せず、稼働経路か・実害があるかで判断した。
- 分類A（稼働経路で実害。修正）: 4 件。F02、F04、F05、F06。
- 分類B（未配線・呼び出し元なし。TODO または是正のみ）: 2 件。F03、F07。
- 分類C（誤検出）: 1 件。F01。
- テスト品質の是正: 3 件。F10、F11、F14。
- 別 Step へ配分: 4 件。F08・F09（Step10）、F12・F13（Step10b。台帳に既出）。

## 2. 各指摘の結論

### 分類A（修正した）
- **F02 `IManagedActionManager` の未登録**: 2026-09-04（`31dddc43`、自動プロファイル設定コレクションの追加）が、無関係な差分として `IManagedActionManager`・`IControllerRegistry`・`IActionRegistry` の登録を巻き込み削除していた。以降、`ActionManager.SetToggledOn` が例外になり（呼び出し元が握りつぶす）、Toggle 型キーの `IsToggledOn` が true にならなかった。`IManagedActionManager`→`DefaultActionManager`、`IControllerRegistry`→`DefaultControllerRegistry` を再登録した（`IActionRegistry` は実装が静的クラスで呼び出し元もないため対象外）。
  - 再登録で挙動が変わらないよう、`DefaultActionManager.ClearDeviceState` を静的ファサード版に揃えた（`Mapping.HandleDeviceDisconnect`。プロファイル適用時の押しっぱなし合成キーの解放を維持）。
  - 実機ログで、Controls タブ・SpecialActions タブの両方で、トグルのオン／オフ、リピートの開始／停止、トグルオン中のプロファイル切替でのキー解放を確認した。
- **F04 ログ設定の `NullReferenceException`**: `NLog.config` の `async="false"`（2025-12 から）で `logfile` が直接の `FileTarget` になり、ラッパー前提の `UpdateLogSettings`／`RestoreArchiveSetting` が例外になっていた。例外が `Save()` より前に起きるため、設定が保存されず、NLog.config の更新も行われなかった。`FindLogFileTarget`（ラッパー内と直接の両方に対応）に統一した。DI 移行の回帰ではない既存不具合。
- **F05 `DefaultMacroPlayer` の 4 スロット固定**: 配列長と範囲判定を `Global.MAX_DS4_CONTROLLER_COUNT`（8）にした。`Mapping.PlayMacroDirect`／`EndMacroDirect` の `>= 4` も揃えた。既定の 4 スロットでは変化なし。
- **F06 `LaunchProcessAction`**: 置換前（`Mapping.cs` の `specActionLaunchProc`）の仕様へ戻した。`extra` の `$hidden`（大文字小文字不問）のときだけ、その語を取り除き非表示で起動（`.bat`／`.cmd` は `%COMSPEC% /C`）。それ以外は `UseShellExecute=true`、非表示にせず `extra` をそのまま引数に渡す。実機で、引数なし・引数付き・`$hidden`・ショートカットを確認した。

### 分類B（動作は変えない／是正のみ）
- **F03 `MouseOutputAction`**: 未登録・未完成。`Mapping.cs` の `GetService(typeof(MouseOutputAction))` は常に null で、従来の `PerformMouseButtonEvent` が実際の出力経路。登録すると左クリックなどが出なくなるため、クラスと `Mapping.cs` の 2 箇所に「登録してはならない」旨の TODO を付与した（Phase8 で完成させる）。
- **F07 `ServiceProviderHolder.GetRequiredService`**: 呼び出し元 0 件。§2.4 に従い、全体計画書のシム例が使用を想定した契約のため削除せず、未設定・未登録なら `InvalidOperationException` を投げる実装にした。

### 分類C（誤検出）
- **F01 プロファイル切替の 250ms デバウンス**: 250ms のデバウンスは 2026-09-16（`94066f3c`）に消えたが、機能上の安全弁は Fresh-press ガード（`RequiresFreshPressAfterReset`、80ms の未成立継続で解除）・`IsExecuting`・`ProfileActionChainService` の Profile 型チェーンのスキップに置き換わっており、稼働経路では `Mapping.cs` の上流で必ず働く（Phase5-Step14 で実機確認済み）。コード変更なし。`copilot-instructions.md` §2.2 の記述が現状と食い違っていたため、現行の機構へ更新した。残る弱点（`IProfileSwitcher` を直接呼ぶ新しい経路では効かない）は Phase8 計画書へ申し送った。

### テスト品質
- **F10**: `ProfileSettingsServiceSubSettingsTests` の 6 テストで、前提が満たされないと `return` で成功扱いになる条件分岐を `Assert.NotNull` に置き換えた。
- **F11**: `SpecialActionsListViewModelTests` の購読の監査を、発行側（サービス）のデリゲートの購読者を調べる動作による検証に置き換えた。当該 ViewModel は `IDisposable` ではなく、サービスのイベントを購読していない（計画書の「購読と解除の検証」は「購読していないことの検証」に置き換えた）。
- **F14**: `StandaloneTests` の正規化テストを、`Util` と `Global` の独立した 2 実装の比較と期待値の固定に書き直した。

## 3. 付随の修正（実機確認・テスト実行で見つかったもの）
- `ProfileSettingsViewModelGlobalReferenceGuardTests` の `UriFormatException`（`pack://` のスキームが `Application` の生成で登録されるが、このクラスに準備がなく実行順に依存していた既存テストの不具合）: 他の ViewModel テストと同じ準備を静的コンストラクタに追加した。
- `ServiceProviderHolder`（静的）を差し替えるテストが並列に走る問題: `ManagedActionManagerRegistrationTests` と `ToggleAndToggledOnTests` を同じ xUnit コレクション `"ServiceProviderHolder"` で直列化した。
- 「Launch Program」タブのプログラム名ラベルの固定幅 150 を解消した（`SpecialActionEditor.xaml`。親の幅いっぱいを使い、アイコンは右端、長い名前は末尾を省略）。

## 4. 決定（2026-10-14）
決定1＝A（F03 は TODO のみ）、決定2＝A（F02 は再登録）、決定3＝A（F07 は例外を投げる）、決定4＝A（F06 は置換前の仕様）、決定5＝B（PR への返信は内容を提示し、許可を得たものだけ書き込む。Step9b 完了後にまとめて）、決定6＝A（`copilot-instructions.md` §2.2 の更新）。

## 5. 変更したファイル（主なもの）
- 本番コード: `DS4Windows/Core/Logging/LoggerHolder.cs`、`DS4Windows/DI/ServiceRegistration.cs`、`DS4Windows/DI/ServiceProviderHolder.cs`、`DS4Windows/Actions/DefaultActionManager.cs`、`DS4Windows/Actions/DefaultMacroPlayer.cs`、`DS4Windows/Actions/LaunchProcessAction.cs`、`DS4Windows/Actions/MouseOutputAction.cs`（TODO のみ）、`DS4Windows/DS4Control/Mapping.cs`（F05 の範囲判定、F03 の TODO）、`DS4Windows/DS4Forms/SpecialActionEditor.xaml`。
- テスト: `LoggerHolderFindLogFileTargetTests`（新規）、`ManagedActionManagerRegistrationTests`（新規）、`LaunchProcessActionTests`（書き直し）、`DefaultActionManagerTests`（F05 追加）、`ProfileSettingsServiceSubSettingsTests`（F10）、`SpecialActionsListViewModelTests`（F11）、`StandaloneTests/TestSpecialActionUtils`（F14）、`ProfileSettingsViewModelGlobalReferenceGuardTests`、`ToggleAndPressedOnceTests`（コレクション属性）。
- 文書: `Phase6-Step9b-Plan.md`、`Phase6-Status.md`、`Phase6-Plan.md`、`Phase6-Step10-Plan.md`（付随の整理項目）、`Phase6-Step11-Plan.md`（先送り台帳）、`Phase8-Unified-Trigger-Dispatch-Plan.md`（申し送り）、`Phase2-Bugfix-ProfileSwitch-CascadeLoop.md`（その後の経緯）、`DI-App-Wide-Migration-Plan.md`、`.github/copilot-instructions.md` §2.2、`Model-Diagram/04-Service-Lifecycle-Spec.md`。

## 6. 後続への申し送り
- **Step10**: F08（サービス実装のコンストラクタのフォールバック。`DefaultActionManager` のコンストラクタも含む）、F09（`ControllersViewModel`）、`SpecialActionsListViewModel` の未使用フィールド `actionManager`、`ProfileApplicationService` の `deviceIndex >= 4`（K1）。`Phase6-Step10-Plan.md` 冒頭に追記済み。
- **Step10b**: F12（`INotificationService.cs`）、F13（`Trigger.cs`）。台帳に既出。
- **Step11 の先送り台帳**: 8 スロット拡張時のデバイス 4〜7 のマクロ（`DefaultMacroPlayer`。単体テストで固定済み）。
- **Phase8**: `MouseOutputAction` の完成、プロファイル切替ガードの統一経路での維持、`IManagedActionManager`／`IControllerRegistry` の再登録の前提、プログラム起動仕様の維持。`Phase8-Unified-Trigger-Dispatch-Plan.md` 末尾に追記済み。
- **持ち越し（変更なし）**: K7b-1（異常終了。先送り）、K9-3（プリセットメニューのサブメニュー）、K7-1。

## 7. PR #1 のレビューコメントへの返信案（提示のみ。GitHub への書き込みは、ユーザーの許可を得たものだけ行う）
各スレッドの返信案。いずれも、対応したコミットまたはステップを明記する。

- **F01（DefaultProfileSwitcher のデバウンス）**: 「ご指摘の 250ms デバウンスは 2026-09-16 に撤去しています。連鎖発火の防止は、`ActionInstanceState.RequiresFreshPressAfterReset`（トリガーが 80ms 連続して未成立になるまで新規成立とみなさない）と `IsExecuting`、`ProfileActionChainService` の Profile 型チェーンのスキップに置き換えており、稼働経路は `Mapping.cs` の上流でこれらを必ず通ります（実機で確認済み）。コード変更はなく、ドキュメントの記述（`copilot-instructions.md` §2.2）を現行の機構へ更新しました。」
- **F02（IManagedActionManager の未登録）**: 「ご指摘のとおりでした（2026-09-04 の別コミットによる巻き込み削除）。`ServiceRegistration` に `IManagedActionManager`／`IControllerRegistry` を再登録し、`DefaultActionManager.ClearDeviceState` を静的版と同じ動作に揃え、回帰テストと実機で確認しました（Phase6-Step9b-2）。」
- **F03（MouseOutputAction）**: 「`MouseOutputAction` は未登録・未完成で、`GetService` が常に null になるため、従来の `PerformMouseButtonEvent` 経路が動いており実害はありません。登録すると出力が失われるため、登録してはならない旨の TODO を付け、Phase8 で完成させます。」
- **F04（NLog の async=false）**: 「ご指摘のとおりでした。`FindLogFileTarget` で、ラッパー内と直接の `FileTarget` の両方に対応しました。設定が保存されない副作用も含めて解消し、回帰テストを追加しました（Phase6-Step9b-1）。」
- **F05（DefaultMacroPlayer の 4 スロット）**: 「`Global.MAX_DS4_CONTROLLER_COUNT`（8）に合わせました。単体テストで固定済みです。8 スロット構成での実機確認は、機材の都合で Step11 に先送りしています（Phase6-Step9b-3）。」
- **F06（LaunchProcessAction）**: 「置換前の仕様（`$hidden` のときだけ非表示、それ以外は `UseShellExecute=true`）へ戻し、テストを書き直しました。実機でも確認しています（Phase6-Step9b-3）。」
- **F07（GetRequiredService）**: 「呼び出し元は 0 件でしたが、名前どおり、未設定・未登録なら `InvalidOperationException` を投げる実装にしました（Phase6-Step9b-2）。」
- **F08（ProfileActionChainService のフォールバック）／F09（ControllersViewModel）**: 「Pure DI の整理として Phase6-Step10 で対応します（台帳に登録済み）。」
- **F10・F11・F14（テスト）**: 「それぞれ、条件で `return` しない `Assert.NotNull`、発行側の購読者を調べる動作による検証、`Util` と `Global` の独立実装の比較に修正しました（Phase6-Step9b-4）。」
- **F12・F13（1ファイル1型）**: 「Phase6-Step10b（1ファイル1型の全数是正）で一括して分割します（台帳に登録済み）。」
