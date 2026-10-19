# フェーズ8 進捗管理文書: Controls／SpecialActions の統一トリガーディスパッチ

最終更新日: 2026-10-19  
状態: **Step1〜3 完了（2026-10-19）。Step1〜4 完了。Step4b 実施中**。Phase7・7b・7c は完了。実機確認 U-24（Phase7c W1）は確認済み（2026-10-19）  
計画書: `Phase8-Unified-Trigger-Dispatch-Plan.md`（D1〜D5 確定。2026-10-19 追記は事実誤認の訂正）。Step1: `Phase8-Step1-Plan.md`

---

## 1. 全体進捗サマリ

| Step | 内容 | 状態 | 記録 |
|---|---|---|---|
| Step1 | 詳細監査（トリガー判定の棚卸し、特性化テストの完成、孤立コードの判断、静的な窓口の洗い出し） | **完了**（2026-10-19。実機確認 W2b-5 は未） | `Phase8-Step1-Plan.md`、`Phase8-Step1-Completion-Report.md` |
| Step2 | 詳細設計（メンバー確定、`KeyOutputAction` の委譲設計、モデル図の更新案） | **完了**（2026-10-19。モデル図の段階 A は反映済み） | `Phase8-Step2-Plan.md`、`Phase8-Step2-Completion-Report.md` |
| Step3 | 判定と束縛の型の新設（`ComboTriggerInputAction` ほか）・単体テスト、`KeyOutputAction` の書き直し（配線なし） | **完了**（2026-10-19。W1〜W5 ビルド・テスト成功） | `Phase8-Step3-Plan.md`、`Phase8-Step3-Completion-Report.md` |
| Step4 | エンジンと出力の型（`TriggerDispatchEngine`、`ButtonOutputAction` ほか）、`Mapping` への空配線 | **完了**（2026-10-19。W1〜W5 ビルド・テスト成功、配線の追加分は 1.15%） | `Phase8-Step4-Plan.md`、`Phase8-Step4-Completion-Report.md` |
| **Step4b（挿入）** | `LoadProfile`/`SaveProfile` の節ごとの分解、機械的な整理 2 件（Step5 の準備。仮称 Phase7d） | **実施中**（Q1〜Q8 を推奨どおり承認。W1 完了［ゴールデン確定、977 件成功］。W2 完了。W3 完了。W4 完了。W5 完了。W6 実装済み［保存側 7 節。ビルド・テスト待ち］。2026-10-19） | `Phase8-Step4b-Plan.md`、`Phase8-Step4b-Audit.md` |
| Step5 | プロファイル読込時の `TriggerBinding` リスト構築 | 未着手 | - |
| Step6 | コントローラー接続時の紐付け | 未着手 | - |
| Step7 | 種別ごとに `Mapping.cs` から新経路へ切替（1 種別＝1PR） | 未着手 | - |
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
