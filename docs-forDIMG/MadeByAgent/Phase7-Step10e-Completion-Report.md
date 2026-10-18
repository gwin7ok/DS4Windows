# Phase7-Step10e 完了報告書: 仕上げと総合検証

作成日: 2026-10-18  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase7-Step10e-Plan.md`（Q1〜Q10 はすべて推奨どおり承認。W4 は §4.1 の判断を承認）。上位: `Phase7-Step10-Plan.md`  
状態: 実装完了。W1〜W5 のビルド・テストビルド・テスト実行は成功（ユーザーが各 Wave で実行）。W6 は文書のみ。総合検証（必須の 8 項目）は **GO**（○ 7、△ 1、× 0）。

---

## 1. 結果の要約

- **静的な `ControlService.CURRENT_DS4_CONTROLLER_LIMIT` を削除した。** スロット数の上限は、注入された `IEnvironmentService.ControllerSlotLimit`（`OutputSlotManager` は整数）で受ける。
- **`AppHost.GetService` の暗黙のホスト構築を廃止した。** ホストが未構築（または Dispose 済み）のとき `null` を返す。K7-1 の残存経路を完全に閉じた。`CreationNote` と起動ログ（`[DI] Host creation path`）は、ユーザー決定により維持した。
- **呼び出し 0 件のメンバーを決着した**（`Phase7-Step10e-Plan.md` §4.1。承認済み）。
- **ラチェットを最終化した**: `Global.*` **2**（ログ文言の文字列のみ）、`AppHost.GetService|Services` **3**、`Program.rootHub` **0**、`ServiceProviderHolder` **17**（`Actions/`。Phase8）。基準値と実数の**一致**を検査するテストを追加した。
- 機能・ログ・起動順序は変えていない。ガードレール 5.1〜5.6 への影響なし。

## 2. Wave ごとの実績

- W1: スロット上限のサービス側（`OutputSlotManager`、`LegacyProfileXmlSerializer`、`ProfileSlotApplier`、`ProfileRepository`、`AutoProfilesDTO`）。`OutputSlotManager` は上限をコンストラクターで受け、`ControlService` が `_controllerSlotLimit` を渡す。`AutoProfilesDTO.MapTo` は `AutoProfileHolder.UsingMaxControllers`（10b の値）を使う。回帰ガード `Phase7Step10eW1SlotLimitServicesGuardTests`。
- W2: 画面・ViewModel 側（`EditorWindowFactory`／`ViewModelFactory` 経由で `IEnvironmentService` を渡す。`BindingWindow`、`ProfileEditor`、`SpecialActionEditor`、`AutoProfiles`、`MainWindow`、各 ViewModel）と、静的シムの削除。回帰ガード `Phase7Step10eW2SlotLimitUiGuardTests`。
- W3: `AppHost` の暗黙構築の廃止。テストの 188 箇所（47 ファイル）を `TestHost`（共有のホストを必要なときに明示的に作る入口）に置き換えた。`StandaloneTests` は明示的に `CreateHost()` を呼ぶ形にした。回帰ガード `Phase7Step10eW3AppHostGuardTests`。
- W4: 孤立コード・0 件メンバーの決着（§3.1）。回帰ガード `Phase7Step10eW4OrphanMembersGuardTests`。
- W5: ラチェットの最終化。`Phase7Step10eW5RatchetEqualityGuardTests`（基準値と実数の一致、最終の合計 0／3／2／17）。静的な状態の目録と、`Mapping`／`MainWindow` の依存の記録を台帳 §17 に作成。
- W6: 文書の最終化（モデル図 01〜04、台帳 §17、チェックリスト §12・U-22、`Phase7-Status.md`、`copilot-instructions.md` §2.1・§3.1）。
- 総合検証: チェックリスト §12（結果は §12.6）。

## 3. 計画との差と、調査で判明した事項

### 3.1 W4 の判断（承認済み）
- 削除: `ColorUtil.applyRatio(Color, Color, uint)`／`HuetoRGB(float, float, float)`（移動前から呼び出し 0 件）、`IAppSettingsService.CheckWhen`（旧 `Global` の概念が撤廃済み）、`IProfileSettingsService.SATriggerCond`（配列。`GetSATriggerCond` は残る）、`IAutoProfileService.ClearState`（追加時から呼び出し 0 件。正規の経路は `IProfileSwitcher.ClearState(int)`）、`IProfileRepository.GetProfilePath`（実装クラスの public メソッドは残る。窓口は `IPathService.GetProfilePath`）。
- 維持して `TODO(Phase8)` を付けた（購読者・読み手が未接続）: `IAppSettingsService.SettingChanged`、`ISpecialActionRepository.ActionsChanged`、`IProfileSettingsService.UseTempProfileArray`。
- 維持（変更なし）: `IDeviceStateService.DeviceSerialChange`（10d で決定済み）、`IUdpServerService.Port`、`IKeyButtonControllerRegistry.GetOrCreate`、`IPathService.GetProfilePath`。

### 3.2 計画との差
- 計画（Q4）では `CreationNote` の値を 2 種類にするとしたが、引数なしの `CreateHost()` の値（`explicit CreateHost() created the host`）を追加した（`CreateHost()` はテストが明示的に呼ぶため）。
- ラチェットの一致検査は、既存のテストを書き換えず、既存の基準値を読む別のテストにした（既存のコメントと履歴を保つため）。

### 3.3 再実行の原因（いずれもテストの問題。本体の回帰ではない）
- W2: `EditorWindowFactory` の生成を文字列で固定していた 2 つのガード（`Step10GroupTwoGlobalReferenceGuardTests`、`ProfileEditorGlobalReferenceGuardTests`）の期待値の更新漏れ。
- W3: 名前空間が `DS4Windows.Actions.Tests` の 1 ファイル（`ToggleAndToggledOnTests`）が `TestHost` を解決できなかった（`using DS4WindowsTests;` を追加）。

### 3.4 総合検証で見つかった事項（Phase7 の退行ではない）
- **L2／R2 のデッドゾーンの仕様変更の要望**（T-4）: 現行は「デッドゾーン以下は 0、超えた点を 0% として残りを引き伸ばす」計算。`Mapping.SetCurveAndDeadzone` の該当部分は Phase6 完了時点・上流（2019 年）と同一（差は `Global.Clamp` → `NumericUtil.Clamp` の置き換えのみで、実装は同一）。要望は「超えたら押し込み量そのままを出力」への変更。機能変更のため、総合検証の後に別の計画書で扱う。アンチデッドゾーンとの関係の整理が要る。
- **既存の不具合: 「Unload on trigger release」で元のプロファイルに戻らない**（T-6。△）: ログで確認した。プロファイル切替（一時プロファイル）の適用処理が `ActionManager.ClearDeviceState` を呼び、`DefaultActionManager.ClearDeviceState` → `Mapping.HandleDeviceDisconnect` が `deviceRuntime[device]` を作り直すため、直前に保存した復帰の合図（`UntriggerAction`）も消える。Phase6 完了時点（`1c97977f`）も同じ構造で、Phase7 の退行ではない（上流の元の実装は `ClearDeviceState` を呼ばず、復帰できていた）。是正案は、適用処理が `ClearDeviceState` を呼ぶ前後で `UntriggerAction`／`UntriggerIndex` を `IMapping` 経由で退避・復元すること。承認を得て、別の計画書で是正する。
- タッチパッドのマウススティックの XML（T-2）: 保存時に 2 つ目の `<OutputStick>` が `<OutputStickAxes>` に変わる。Phase6-Step12 Wave 6 で是正済みの上流の不具合で、読込側に旧形式の互換がある。
- チェックリストの記述の不正確（私の記述の誤り）: 「ターゲットデバイスの一覧」という画面はない（スロット上限に依存するのは、編集中のコントローラーを操作する機能の有効判定）。「Run macro synchronously」というラベルもなく、実際は「Synchronized Run」。

## 4. 起動順序・スレッド・ガードレールへの影響

- 起動順序: 変更なし（`EnvironmentService` → `BackingStore` → `PathService` → …）。ホストは `App` の起動だけが作る。
- 5.1〜5.6: 変更なし。ホットパスに新しい確保はない（W1・W2 は生成時の値の受け渡しのみ。W3 は取得経路のみ）。

## 5. 総合検証の結果

- 実施（必須の 8 項目）: T-1（起動・ログ・設定の復元）○、T-2（設定ファイルの互換）○、T-3（接続・適用・Plug／Unplug）○、T-4（通常入力・割り当て・切断）○、T-6（マクロ・スペシャルアクション）△、T-8（プロファイル編集画面）○、T-9（自動プロファイル画面）○、T-14（短縮版。10 分の連続使用・終了・再起動）○。
- 判定: × が 0 件のため **GO**。
- 先送り（対応する U-xx は「未」のまま）: T-5（ジャイロ・タッチ・フリック）、T-7（出力種別・出力スロットの詳細。Plug／Unplug は T-3 で確認）、T-10（トレイ・通知・ライトバー）、T-11（設定画面・言語・テーマ・更新確認）、T-12（第 1 層の設定）、T-13（起動引数）、T-8 のうち振動テスト・ジャイロ校正・ライトバーの即時反映。

## 6. 未完了・持ち越し

- 別の計画書で扱う（承認が要る。Phase7 の範囲外）: 「Unload on trigger release」の復帰（既存の不具合）、L2／R2 のデッドゾーンの仕様変更。
- 先送りした実機確認（§5）。U-1〜U-7（機材・環境なし）。
- Phase8 へ: `Actions/` の `ServiceProviderHolder` 17 件、第 1 層の静的な状態（`DS4Devices`／`DS4LightBar`／`HidDevices`）、維持して TODO を付けた 3 メンバーの接続、`BackingStore` の分割、`Mapping` の依存 11・`MainWindow` の引数 18 の整理（台帳 §17.3・§17.4）。

## 7. 次の Step

- `Phase7-Completion-Report.md`（Phase7 全体）の作成。その後、上記の不具合の是正と仕様変更の計画書、Phase8 の計画（ユーザーの指示を待つ）。
