# Phase8-Step1 完了報告書: 詳細監査（トリガー判定の棚卸し、特性化テストの完成、孤立コードの判断）

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase8-Step1-Plan.md`（Q1〜Q6 を推奨どおり承認）。進捗: `Phase8-Status.md`  
状態: **完了（ビルド・テストビルド・テスト実行は成功。ユーザーが実行）。実機確認 W2b-5 は未**

---

## 1. 結果の要約

- 製品コードの挙動は、既存の不具合の是正（下記 §3）を除き、変えていない。
- 成果物:
  - 仕様台帳 `Phase8-Step1-Trigger-Spec-Ledger.md`（トリガー判定・実行の現行の挙動。意図が不明な点 11 件）。
  - 特性化テスト・回帰テスト（Phase7c W2 の 16 件に加え、Step1 で 22 件）。
  - 孤立コードの判断書 `Phase8-Step1-Orphan-Decisions.md`（承認済み）と、その実施（削除、TODO コメント）。
  - 静的な窓口の一覧と、Step2 への論点 `Phase8-Step1-Static-Access-Inventory.md`。
  - デバイス上限 4 前提の洗い出しと是正 `Phase8-Step1-SlotLimit-Fix-Plan.md`。
- ラチェットの基準値（Global 2／AppHost 3／rootHub 0／ServiceProviderHolder 17）は変わらない。

## 2. Wave ごとの実績

- **W1（仕様台帳）**: `MapCustomAction`（約 1,180 行）、Stage3、判定の補助（`IsSpecialActionTriggered`、`CheckForSpecialActionSuppression`）を棚卸しし、種別ごとの成立・解除・安全弁（Fresh-press ガード、`IsExecuting`）・出力の経路を文書化した。
- **W2（特性化テスト B-1〜B-4）**:
  - `Phase8Step1ProfileSwitchCharacterizationTests`（4 件）: Profile 種別の成立→切替（`ApplyFromAction` で 1 回、直接の `ApplyProfile` は呼ばれない）、`AutomaticUntrigger` の復帰（離して 1 回だけ。合図が消える）、`uTrigger` あり（解除トリガーがすべて押されたときだけ復帰）、設定なし（合図を設定せず、復帰しない）。
  - `Phase8Step1ShiftTriggerCharacterizationTests`（3 件）、`Phase8Step1ApplyProfileCleanupOrderGuardTests`（1 件。適用後の掃除の順序のソースガード）。
  - 基盤: `RecordingProfileApplicationService`（記録用。本番の適用と同じ掃除を再現する `SimulateStateReset`）、`SpecialActionTestScope.InstallProfileRecorder`。
- **W2b（デバイス上限 4 前提の是正）**: §3 のとおり。回帰テスト `Phase8Step1SlotLimitTests`（11 件）。
- **W3（B-5・B-6）**: `ControlTabAndSpecialActionKeyTests` の Press／Toggle に `MockVirtualKBM` の Assert を補強。`Phase8Step1MacroBranchCharacterizationTests`（3 件）。
- **W4（孤立コードの判断）**: 判断案を提示し、一括で承認を得た。
- **W5（判断の実施、D・E）**: 削除（`IOutputHandler`、`ReleasePolicy`、`ToggleController`、`MacroManager`、`ToggleActionController`、`DateTimeJsonConverter`、`InputMethods`、`Sensitivity`、`FakerInputMapping.ExpandedKeyboardKeys`、`IProfileSwitcher.ApplyManualProfile`）と、TODO コメントの追加（系統 A の骨組み、`ClearState`、`ITouchpadBehaviour`、`SensitivitySettings`、`IMacroPlayer.IsPlaying`、Step3 で削除する `KeyInputAction`／`ITrigger`／`SingleButtonTrigger`）。静的な窓口の一覧と Step2 への論点を文書化した。

## 3. 旧実装との比較と、判明した事項

- **`nowAction`／`oldnowAction` が 4 要素（是正済み）**: 上流の 2015 年（4 台の時代）の初期化子のまま、2020 年の 8 スロット化で更新が漏れた。拡張スロット（デバイス 4〜7）で `delayTime` のアクションが毎フレーム例外になり、そのデバイスの特殊アクションと Stage3（Button）が打ち切られた。**上流の元からの不具合**。`new DateTime[MaxControllerCount]` に是正。
- **出力スロットの復元が 0〜3 に限られていた（是正済み）**: `OutputSlotPersistDTO.MapTo` の `idx <= 3`。上流の `OutputSlotPersist`（出力スロットが 4 つの時代）から。保存側は全スロットを書くため、5〜8 番目の恒久スロットが復元されなかった。**上流の元から**。`< OutputSlots.Length` に是正（Q1＝A）。
- **Profile の「離したら復帰」の暗黙の依存**（台帳 §8 項目 11）: 離したときの `OnRelease` → `RestoreFromAction` が起きないのは、切替の適用が行うアクション状態の掃除（`ClearAllEntries`／`ClearDeviceState`）で `BeingTriggered` が false に戻る副作用による。統一ディスパッチで明示的な仕様にする（Step2）。
- **`ClearState`／`ClearPendingRestore` の未接続**: Phase5-Step3 の「切断時スタック残留防止（§5.6 ガードレール 6）」の完了報告に反し、物理切断の経路から呼ばれていない（契約だけ配備）。意味（`OlderProfilePath` の扱い）を調べて Step6 で接続する。`IProfileSwitcher.ClearState` に TODO を付与した。
- **テストの実行順への依存**（Phase7c で解消）: `pack://` の URI は `Application` の生成後でないと使えない。
- **意図が不明な挙動**（台帳 §8）: Stage3 の Button の Press が Fresh-press ガードを見ない、判定ロジックの複製の不一致、到達不能に見えるタップ判定の分岐、Profile／DisconnectBT の `return` ほか。固定せず、Step2 で意図を決める。

## 4. 起動順序・スレッド・ガードレールへの影響

- 起動順序、適用時 Halt、安全弁（Fresh-press ガード、`IsExecuting`、一時プロファイル復帰、排他）: 変更なし。
- ログの出力・レベル: 変更なし。

## 5. 未完了・持ち越し

- **実機確認 W2b-5**: 5 台以上のコントローラー（スロット 5〜8）での `delayTime` のアクション、5〜8 番目の出力スロットの恒久の保存・復元。機材がなければ、リスク受容として記録する。
- **Step6 へ**: `ClearState`／`ClearPendingRestore` の接続（意味の調査を含む）。
- **Step3 で実施**: `KeyInputAction`、`ITrigger`／`SingleButtonTrigger` の削除（`ComboTriggerInputAction` の導入と同時）。
- **Step2 へ**: `Phase8-Step1-Static-Access-Inventory.md` §3 の論点（`IActionRegistry` の実装、`shiftTrigger` のモデル、Press のガードの差、判定ロジックの一本化、`MouseOutputAction`、モデル図 01〜04 の更新案）。
- **Step8 へ**: 呼び出し元 0 件の `ActionManager` の静的 API（`GetActionByIndex`、`GetActionByName`、`Actions`、`PreallocateOnStartup`、`FireToggledOnChanged`、`DispatchTrigger`）の削除と、`ServiceProviderHolder` 17 件のコンストラクター注入化。
- **別 Phase へ**: `ITouchpadBehaviour`（Phase9-B）、`SensitivitySettings`（Phase9-G）、`IMacroPlayer.IsPlaying`（Phase9-C）。
- 陳腐化したテスト `AppPostHostGlobalReferenceGuardTests.RetainedGlobalReferences_HaveTodoComments`（存在しない `Global` の TODO を要求する）の整理。
- K7b-1（異常終了）、K9-3（プリセットのサブメニュー）: 先送りのまま。

## 6. 次の Step

- **Phase8-Step2（詳細設計）**: `IActionBinding`／`IInputAction`／`IOutputAction`／`IActionRegistry` の具体的なメンバー確定、`KeyOutputAction` の `KeyAction` への委譲設計、モデル図 01〜04 の更新案。着手前に、`Phase8-Step2-Plan.md` を作成して承認を得る。
- その後の順序: Step3〜Step4 → Step4b（`LoadProfile`／`SaveProfile` の分解）→ Step5〜Step9 → Phase9（`Phase8-Unified-Trigger-Dispatch-Plan.md` 2026-10-19 追記［実行順序の改訂］）。
