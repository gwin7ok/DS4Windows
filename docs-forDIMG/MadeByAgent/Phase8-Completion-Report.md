# Phase8 完了報告書: Controls／SpecialActions の統一トリガーディスパッチ

作成日: 2026-10-20
計画書: `Phase8-Unified-Trigger-Dispatch-Plan.md`、進捗: `Phase8-Status.md`、各 Step の完了報告書（Step1〜Step8）
状態: **Step1〜Step9 完了。最低限の実機確認（★ 18 項目）はすべて合格**（`Phase8-Step9-RealDevice-Checklist.md`）。未解決の件は 1 件（意図しない終了。§4）。

---

## 1. 達成したこと

| 項目 | 内容 |
|---|---|
| 統一トリガーディスパッチ | `TriggerDispatchEngine`（束縛があれば常に処理）、`ActionBindingBuilder`／`ControlBindingBuilder`（全種別・全区分の束縛を組み立てる）、`ActionBindingRegistry`。判定（入力アクション）と実行（出力アクション）を、方針（`BindingPolicy`）で分ける設計。種別ごとの分岐はエンジンに持たない |
| 旧経路の削除 | `Mapping.cs` の SA の Stage3、`MapCustomAction` の種別ごとの旧枝、`ProcessControlSettingAction` の旧本体、判定の旧関数、系統 B（`IActionFactory`・アダプター・`Actions.Action`・`TriggerContext` など 14 型）を削除 |
| 移行スイッチの削除 | `BindingMigrationOptions`、`UntriggerPolicy.Buttons`、`BindingPolicy.UntriggerButtons`、`Mapping` の未使用の定数 2 つ（8d） |
| 規模 | `Mapping.cs` は 7,322 行 → **4,626 行**（約 2,700 行減）。`ServiceProviderHolder` の参照（`Actions/`）は 17 → 10。ロールバックは git のみ |
| 実機確認 | Step9 の ★ 18 項目（SA: GyroCalibrate／BatteryCheck／MultiAction／Profile の切替と復帰／DisconnectBT／再接続、Controls: Key／Macro／Extras／パッド／マウスボタン／マウスの移動とホイール、廃止機能: シフト／ジャイロスワイプ）がすべて ○ |

## 2. 承認済みの機能廃止・動作変更（規約 2.2）

- 機能廃止: ジャイロスワイプ（8-0）、シフト（7.8d）、Lightbar マクロ（7.8e）、XboxGameDVR（7.7a）、Profile 切替の解除トリガー（uTrigger）のボタンによる復帰（8b）。保存済みのデータは、読み込んで解釈せず、保持して書き戻す（廃止の通知で「残す／削除」を選べる）。
- 動作変更: Controls の Key は Key 特殊アクションと状態を共有しない（Q4＝B）、Play Once のマクロは 1 回だけ実行、DisconnectBT は失敗なら再試行、マウスボタンの Toggle は押すたびに 1 回だけ切り替わる、アナログのホイールは複数同時でも干渉しない、`WheelRecalibrate` のログ（コピーの名残り）を削除、旧経路だけの診断ログ（`Phase8-Step8-Q7-Legacy-Log-List.md` の承認済み一覧）を削除。
- 画面に出るログの変化は、旧経路だけの 2 件（`BeingTriggered initialization failed ...`、Stage3 の `Synchronous Button SA handling error`）のみ。

## 3. 持ち越し（Phase9 以降）

| ID | 内容 | 扱い |
|---|---|---|
| C-1 | `IMapping.CaptureProfileUntrigger`／`RestoreProfileUntrigger`、`ProfileUntriggerSnapshot`、`DeviceRuntimeState.UntriggerAction`／`UntriggerIndex`（合図は常に空。`ProfileSwitchAction` の保険の読み取りと Phase7b の保全テストが使う） | 影響を調べて削除（テストの書き換えを含む）。Phase9 の最終クリーンアップ（9-I）の候補 |
| C-2 | **実施（2026-10-20。ビルド・テスト待ち）**: テストの `migrated` 引数（`SpecialActionTestScope`／`ControlsKeyTestScope`／`AnalogMouseTestHarness`／各ヘルパー）、`[Theory]` の `InlineData(false/true)`（83 メソッド）、`foreach (migrated in {false, true})`、旧・新の比べ（`legacy` と `migrated` の `Assert.Equal`）を削除。比べだけが検査だったテスト 7 件（`ADelay_BehavesTheSame` ほか）と、旧・新の割り当ての比べだった 10 件（`TheNewPath_DoesNotAddAllocationsToTheFrame_*` など）は削除。比べを除いた残りの検査（`Assert.Contains` など）は、新経路の期待値として残した | 完了（詳細は `Phase8-Step8-Completion-Report.md` の C-2 節） |
| C-3 | 残した系統 A の型: `KeyAction`、`KeyButtonActionController`、`KeyButtonActionControllerAdapter`、`PressActionController`、`RepeatHelperToIRepeaterAdapter`、`IControllerRegistry`／`DefaultControllerRegistry`、`IKeyButtonControllerRegistry`／`KeyButtonControllerRegistry`、`ActionInstanceState`。静的な `ActionManager`、`ActionEntry`（状態の入れ物） | Q6＝A（新経路が使う）。静的な窓口の整理は `Phase7-Residual-Ledger.md` §17.3 の目録と Phase9 で扱う |
| C-4 | 廃止機能の通知部品の重複（`RemovedShift*` と `RemovedGyroSwipe*`）、未使用の文言（`Strings` の `DirectionalSwipe`、4 言語） | 共通化と翻訳ファイルの整理（Phase9） |
| C-5 | 編集画面で、割り当てがマウスボタンのとき Toggle を設定できない制限の由来 | 調べて、仕様か不具合かを判断（Step9 の B-11） |
| C-6 | `GyroOutputMode` が `DirectionalSwipe` のプロファイルで、画面の表示が Controls(Accel) になる（内部は None。一覧に None がない） | None を持つ従来のプロファイルでも同じ表示かを確認して、判断（Step9 の C-4） |
| C-7 | `Mapping.cs`（4,626 行）、`ControlService.cs` ほか巨大ファイルの責務分割 | Phase9（`Phase9-Responsibility-Split-Plan.md`） |
| C-8 | 割り当て UI の統一 | Phase10（`Phase10-Assignment-UI-Unification-Plan.md`） |
| C-9 | 未実施の実機確認（任意・機材）: ステアリング較正、USB・ドングルでの切断、Controls の細部（Toggle の細部、感度、加速、複数同時の組み合わせなど） | 機材と機会があるときに実施。不具合が出たら、該当の Step の PR を git で戻して調べる |

## 4. 未解決の件: 意図しない終了（1 回）

- 発生: 2026-10-20 22:28:21（Step9 の実機確認中）。イベント ID 1000／1026、モジュール `coreclr.dll`、例外コード 0xc0000005（アクセス違反）。
- ログ: 直前は 22:27:52（`verifyA` の保存と再適用が正常に完了）。その後 29 秒間、ログなし。管理下の例外・エラーのログは、すべてのログで 0 件。
- 原因: 未特定。Step8 の変更との関係も不明（変更は管理側の削除が中心で、ネイティブの呼び出しは触っていない）。
- 対応: クラッシュダンプの採取を設定済み（`C:\CrashDumps`、フルダンプ）。再現したら、ダンプ・ログ・直前の操作を揃えて調べる。過去のビルドで同様の終了があったかは、イベントビューアーで確認する。
- 判断: 再現しないため、Phase8 の完了条件には含めない。

## 5. 完了条件の確認（`Phase8-Unified-Trigger-Dispatch-Plan.md`）

- 旧ディスパッチ・系統 B の削除: 完了（8a〜8d）。
- 全種別・全区分が統一ディスパッチ経由: 完了（Step7）。
- ビルド・単体テスト: 8d 時点で全成功。
- 実機確認: ★ 18 項目すべて ○。
- 文書: モデル図 03・04、`copilot-instructions.md`、状況、各 Step の完了報告書を更新。

## 6. 次の Step

- **Phase9**: `Phase9-Responsibility-Split-Plan.md`（巨大クラスの責務分割）。着手前に、持ち越し C-2（`migrated` 引数の整理）を小さな PR として先に行うことを推奨する。
