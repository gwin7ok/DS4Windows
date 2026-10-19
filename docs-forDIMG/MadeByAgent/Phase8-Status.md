# フェーズ8 進捗管理文書: Controls／SpecialActions の統一トリガーディスパッチ

最終更新日: 2026-10-19  
状態: **Step1〜3 完了（2026-10-19）。Step1〜4・Step4b 完了。Step1〜6 完了。Step7 は 7.1 Button 完了（実機確認 U-27 の主要 4 項目が合格）。7.2 Key は Q1〜Q4 承認済み、7.2 Key 完了（実機確認 U-28 が合格）。7.3 Macro 完了（実機確認 U-30 の主要 4 項目が合格）。7.4 Program 完了（実機確認 U-31 の主要 4 項目が合格）。7.5 Profile は 7.5a（切替のみ）完了（実機確認 U-32 が合格）。7.5b（復帰。自動の復帰のみ）の計画書 `Phase8-Step7-5b-Plan.md` を作成済み（Q1〜Q6 承認済み。W1〜W3 成功。実機確認 U-33 が合格［2026-10-20］。7.5 Profile は完了）。別件: 再接続時のプロファイルのコントローラー単位化 `Reconnect-Profile-PerController-Fix-Plan.md`［Q1〜Q6 承認済み、実装済み、ビルド・テスト成功。実機確認 U-34 が合格（2026-10-20）］。次: 7.6［実機を要する種別。計画書 `Phase8-Step7-6-Plan.md`、Q1〜Q8 承認済み（Q3・Q8 は B）。7.6a GyroCalibrate はビルド・テスト成功、実機確認 U-35 は先送り（2026-10-20）。7.6b DisconnectBT はビルド・テスト成功、実機確認 U-36 は先送り（2026-10-20）。7.6c BatteryCheck はビルド・テスト成功、実機確認 U-37 は先送り（2026-10-20）。7.6d ステアリング較正はビルド・テスト成功、実機確認 U-38 は先送り（2026-10-20）。7.6 は 4 種別とも実装済み。U-35〜U-38 は先送り（Step9 で判断）。次は 7.7［計画書 `Phase8-Step7-7-Plan.md`。ユーザー決定: Q1＝XboxGameDVR は機能削除［7.7a］、Q2＝MultiAction は独立した MultiTapInputAction［7.7b］、他は推奨］。7.7a XboxGameDVR の削除は実装済み、ビルド・テスト結果待ち。U-36 の確認の後に判断［U-36 が先送りのため、確認するまで保留］: 切断の成功／失敗を Windows の本当の結果にする案 A［ユーザー決定。別の計画書で、全呼び出し元の影響を確認してから］。保留: PS+Down の SA［DisconnectBT］で切断した際に、まれに Alt+Tab のプログラム切替表示が出る件は、再現できず［2026-10-20 のログに該当する動きなし。この間に実行された SA は DisconnectBT のみ］、再現条件が分かったときに調査する。7.3 の前に、Alt-Tab の特別扱いと「Cycle Programs」の削除（`Phase8-Step7-3a-AltTab-Removal-Plan.md`。Q1〜Q4 承認済み、W1〜W3 完了。実機確認 U-29 が合格。完了）。次は 7.3 Macro の Q2〜Q4 の承認**。Phase7・7b・7c は完了。実機確認 U-24（Phase7c W1）は確認済み（2026-10-19）  
計画書: `Phase8-Unified-Trigger-Dispatch-Plan.md`（D1〜D5 確定。2026-10-19 追記は事実誤認の訂正）。Step1: `Phase8-Step1-Plan.md`

---

## 1. 全体進捗サマリ

| Step | 内容 | 状態 | 記録 |
|---|---|---|---|
| Step1 | 詳細監査（トリガー判定の棚卸し、特性化テストの完成、孤立コードの判断、静的な窓口の洗い出し） | **完了**（2026-10-19。実機確認 W2b-5 は未） | `Phase8-Step1-Plan.md`、`Phase8-Step1-Completion-Report.md` |
| Step2 | 詳細設計（メンバー確定、`KeyOutputAction` の委譲設計、モデル図の更新案） | **完了**（2026-10-19。モデル図の段階 A は反映済み） | `Phase8-Step2-Plan.md`、`Phase8-Step2-Completion-Report.md` |
| Step3 | 判定と束縛の型の新設（`ComboTriggerInputAction` ほか）・単体テスト、`KeyOutputAction` の書き直し（配線なし） | **完了**（2026-10-19。W1〜W5 ビルド・テスト成功） | `Phase8-Step3-Plan.md`、`Phase8-Step3-Completion-Report.md` |
| Step4 | エンジンと出力の型（`TriggerDispatchEngine`、`ButtonOutputAction` ほか）、`Mapping` への空配線 | **完了**（2026-10-19。W1〜W5 ビルド・テスト成功、配線の追加分は 1.15%） | `Phase8-Step4-Plan.md`、`Phase8-Step4-Completion-Report.md` |
| **Step4b（挿入）** | `LoadProfile`/`SaveProfile` の節ごとの分解、機械的な整理 2 件（Step5 の準備。仮称 Phase7d） | **完了**（W1〜W11 成功。2026-10-19） | `Phase8-Step4b-Plan.md`、`Phase8-Step4b-Audit.md`、`Phase8-Step4b-Completion-Report.md` |
| Step5 | 束縛のビルダー（`IActionBindingBuilder`）と、登録簿への `Replace`（移行対象が空のまま） | **完了**（W1〜W3 成功。2026-10-19） | `Phase8-Step5-Plan.md`、`Phase8-Step5-Completion-Report.md` |
| Step6 | 切断・接続との連携（エンジンと登録簿の後始末、`ClearState`／`ClearPendingRestore` の削除） | **完了**（W1〜W3 成功。2026-10-19） | `Phase8-Step6-Plan.md`、`Phase8-Step6-Completion-Report.md` |
| Step7 | 種別ごとの切替（1 種別＝1PR。7.1 Button → 7.2 Key → 7.3 Macro → 7.4 Program → 7.5 Profile → 7.6 実機を要する種別 → 7.7 MultiAction → 7.8 Controls タブ） | **Q1〜Q8 承認済み（推奨どおり。2026-10-19）。7.1-W1 完了**（テスト足場 `migrated`、Button テストの新旧 Theory 化、`Phase8Step7W1ButtonDualPathTests`）。**W2 完了**（TRACE ログの引き継ぎ、`OnProfileApplied` は新旧差が出ないため未接続のまま、順序・二重ディスパッチ）。**W3 完了**（既定のスイッチに Button を追加［`ServiceRegistration` の 1 行］、ロールバックのテスト、ベンチマーク）。**W4**: 経過報告書 `Phase8-Step7-Completion-Report.md` を作成。実機確認 U-27 の主要 4 項目が合格（2026-10-19）。**7.1 完了**。**7.2 Key**: 計画書作成済み（Q1〜Q4 の承認待ち） | `Phase8-Step7-Plan.md`、`Phase8-Step7-2-Plan.md` |
| Step8 | 旧ディスパッチと系統 B の削除 | 未着手 | - |
| Step9 | 総合検証・実機確認 | 未着手 | - |

## 2. Step1 の Wave

- W1: トリガー判定・実行の仕様台帳 — **完了**（`Phase8-Step1-Trigger-Spec-Ledger.md`）。
- W2: 特性化テスト B-1〜B-4 — **完了（ビルド・テスト成功。2026-10-19）**。`Phase8Step1ProfileSwitchCharacterizationTests`（成立→切替、AutomaticUntrigger の復帰、uTrigger の復帰、チェーン経路が使われないこと）、`Phase8Step1ShiftTriggerCharacterizationTests`、`Phase8Step1ApplyProfileCleanupOrderGuardTests`（ソース順序のガード）。`delayTime` は、配列が 4 要素の不具合（台帳 §8 項目 10）のため固定しない。
- **W2b: 「デバイス上限が 4 前提」の箇所の是正**— **完了（ビルド・テスト成功。2026-10-19）。実機確認 W2b-5 は未**。A-1（`nowAction`／`oldnowAction`）と B-1（出力スロットの復元）を是正。`Phase8-Step1-SlotLimit-Fix-Plan.md`。
- W3: B-5・B-6 — **完了（ビルド・テスト成功。2026-10-19）**。`ControlTabAndSpecialActionKeyTests` の Press／Toggle に Assert を補強（Press: 押下で 1 回押し、離して 1 回解放。Toggle: トグル OFF で解放）、`Phase8Step1MacroBranchCharacterizationTests`（ガード中は不実行、成立・保持・解除、RunOnRelease の準備→離して実行）。
- W4: 孤立コードの調査と判断案（`Phase8-Step1-Orphan-Decisions.md`）— **判断案を提示済み（承認待ち。2026-10-19）**。重要な発見: `IProfileSwitcher.ClearState`／`ClearPendingRestore`（Phase5-Step3 の「切断時スタック残留防止」）は、契約だけ配備され、本番の呼び出しが 0 件。
- W5: 判断の実施（承認された分）— **実施済み（ビルド・テスト待ち。2026-10-19）**。削除: `IOutputHandler`、`ReleasePolicy`、`ToggleController`、`MacroManager`、`ToggleActionController`、`DateTimeJsonConverter`、`InputMethods`、`Sensitivity`、`FakerInputMapping.ExpandedKeyboardKeys`、`IProfileSwitcher.ApplyManualProfile`（実装、モック 2 か所を含む）。TODO 追加: 系統 A の骨組み、`ClearState`、`ITouchpadBehaviour`、`SensitivitySettings`、`IMacroPlayer.IsPlaying`、`KeyInputAction`／`ITrigger`／`SingleButtonTrigger`（Step3 で削除）。静的な窓口の一覧と Step2 への論点は `Phase8-Step1-Static-Access-Inventory.md`（2026-10-19）。
- W6: 完了報告書 — **作成済み（`Phase8-Step1-Completion-Report.md`、2026-10-19）**。

## 3. 決定事項（Step1。2026-10-19）

- Q1＝A: 全種別で成立・保持・解除・ガードの 4 点を固定。実機の `DS4Device` を要する種別（DisconnectBT、BatteryCheck、GyroCalibrate、ステアリング較正）は、ガードの分岐までに限る。
- Q2＝A: プロファイル切替を通すテストは、`IProfileApplicationService` を記録用の実装に差し替える。
- Q3＝A: 孤立コードの判断は、調査結果を 1 つの文書にして、一括で承認を得る。
- Q4＝A: U-24 の実機確認を Step1 の前に行う（W1 は文書のみなので、着手済み）。
- Q5＝A: 本書（`Phase8-Status.md`）を新設して進捗を管理する。
- Q6＝A: 第 1 層（`DS4Devices`／`DS4LightBar`／`HidDevices`）の静的な状態は Step1 で扱わない。

## 4. 実機確認の台帳

- U-24（Phase7c W1 後。Stage3 の競合の是正）: **○（2026-10-19。ユーザーが確認）**。確認内容:手動のプロファイル切替（メイン画面）を繰り返し、`Collection was modified` が出ないこと、Button／Key／マクロ／プロファイル切替と復帰が従来どおり動くこと（`Phase6-Step11-RealDevice-Verification-Checklist.md` §11）。

## 5. 持ち越し・保留事項

- **Phase5-Step3 の完了報告と実コードの不一致**（W4 で発見）: 「切断時スタック残留防止」の `IProfileSwitcher.ClearState`／`IProfileApplicationService.ClearPendingRestore` は契約だけ配備され、物理切断の経路から呼ばれていない。意味（`OlderProfilePath` の扱い）を調べて、Phase8 の Step6 で接続する（`IProfileSwitcher.ClearState` に TODO を付与済み）。

- K7b-1（異常終了）: 先送り。切り分け手順は `Phase7c-Completion-Report.md` §4。
- K9-3（プリセットのサブメニュー）: 先送り。
- `RetainedGlobalReferences_HaveTodoComments`（`AppPostHostGlobalReferenceGuardTests`）: 存在しない `Global` の TODO を要求する陳腐化したテスト。整理を判断する。
- `Phase7-Status.md` §1 の Step6・Step7 行の記録列が空欄。
- 台帳 §17.3 の B（第 1 層の静的な状態）: Phase8 の範囲外（Q6）。

## 6. 直近の次アクション

1. U-24 の実機確認: 済み（2026-10-19）。
2. W2（特性化テスト B-1〜B-4）。`Phase8-Step1-Trigger-Spec-Ledger.md` §8・§9 の「意図が不明な挙動」を、固定する／固定しないを決める。

## 7. 実行順序（2026-10-19 改訂。ユーザー承認）

U-24 → Step1〜Step4 → **Step4b（`LoadProfile`／`SaveProfile` の分解ほか）** → Step5〜Step9 → **Phase9（巨大クラスの責務分割。`Phase9-Responsibility-Split-Plan.md`）**。`Mapping` の補正・マウス変換・マクロなどの分割は、Phase8 が `Mapping.cs` の旧ディスパッチを削除する前には行わない（二度手間と、実機確認の帰属の混乱を避けるため）。

## 8. 経過メモ（2026-10-19）

- W2・W2b の特性化テストと回帰テストは成功（ユーザーが実行）。W2 の途中で、本番では「プロファイル適用が行うアクション状態の掃除［`ClearAllEntries`／`ClearDeviceState`］により、離したときの `OnRelease` → `RestoreFromAction` が起きない」という暗黙の依存が判明（台帳 §8 項目 11）。Phase8 の統一ディスパッチで明示的な仕様にする。
- 上流の元からの不具合 2 件を是正: `nowAction`／`oldnowAction` が 4 要素（デバイス 4〜7 で `delayTime` が例外。台帳 §8 項目 10）、出力スロットの復元が 0〜3 に限られていた（`OutputSlotPersistDTO.MapTo`）。
- 実機確認の追加候補: 5 台以上のコントローラー（スロット 5〜8）での `delayTime` のアクション、5〜8 番目の出力スロットの恒久の保存・復元。機材がなければリスク受容。
- 次: W3（B-5 Assert の補強、B-6 Macro 分岐）。
