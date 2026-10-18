# Phase8-Step2 計画書: 詳細設計（統一トリガーディスパッチの契約・状態・移行方法の確定）

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Unified-Trigger-Dispatch-Plan.md`（D1〜D5 確定。2026-10-19 の 2 つの追記を含む）、`Phase8-Step1-Completion-Report.md`  
入力: `Phase8-Step1-Trigger-Spec-Ledger.md`（現行の挙動）、`Phase8-Step1-Static-Access-Inventory.md` §3（Step2 への論点）、`Phase8-Step1-Orphan-Decisions.md`  
状態: **計画案（承認待ち）。実装は未着手**

---

## 1. 目的と位置づけ

- Step2 は**設計の確定**であり、製品コードは変更しない（成果物は文書。必要なら TODO コメントのみ）。Step3 以降（`ComboTriggerInputAction` の新設、`KeyOutputAction` の書き直しなど）の実装が、迷わず進められる状態にする。
- 確定する内容:
  1. 統一抽象化の**契約**（`IActionBinding`／`IInputAction`／`IOutputAction`／`IActionRegistry` の具体的なメンバー、評価の入力、`TriggerBinding` の構造）。
  2. **状態の所有**（`ActionInstanceState`、復帰の合図、抑制、エッジ検出）と、**安全弁の置き場**（Fresh-press ガード、`IsExecuting`）。
  3. **仕様台帳の「意図が不明な挙動」11 件の意図決定**（維持するか、揃えるか。挙動を変える場合は承認が要る）。
  4. **移行方法**（種別ごとの切替の順序、新旧の並行期間の二重ディスパッチ防止、`Mapping` との境界）。
  5. **モデル図 01〜04 の更新案**（**図の変更はユーザーの指示を得てから行う**）。
- 範囲外: 実装、`LoadProfile` の分解（Step4b）、第 1 層、`Mapping` の補正・マウス変換などの分割（Phase9）。

## 2. 現状の整理（設計の出発点）

- 系統 A（未配線）の契約は、そのままでは統一ディスパッチを表現できない。
  - `IInputAction.Evaluate(DS4State)` は、**入力が `DS4State` だけ**。実際のトリガー判定（`getBoolSpecialActionMapping`）は、`DS4StateFieldMapping`（ボタン・軸・トリガー・タッチ・スワイプ・ジャイロを 1 つの表に展開したもの）と `DS4StateExposed`／`Mouse` を要する（タッチ・ジャイロ・スワイプの判定に必要）。
  - `ITriggerContext` は `UseScanCode` を持たない（現行の `TriggerContext` は持つ）。
  - `IActionBinding.OnTriggered`／`OnReleased` は、エッジ検出・ガード・抑制をどこが担うかを決めていない。
  - `IActionRegistry` は実装クラスがない。
- 系統 B（稼働中）は、`ActionManager`（`DefaultActionManager`）が `ActionInstanceState`（`BeingTriggered`、`IsToggledOn`、`RequiresFreshPressAfterReset`、`IsExecuting` ほか）を持ち、`Mapping.MapCustomAction` が判定・ガード・抑制・復帰を手続きで担う。
- 稼働中の出力側（`MacroAction`／`ProfileSwitchAction`／`LaunchProcessAction`）は既に `IOutputAction` を実装済み。`KeyOutputAction` だけが未検証の別実装（D1 の注記）。

## 3. 設計の論点と、決定してほしい点（質問と推奨）

Step2 の本体は、次の論点を、選択肢・根拠・推奨とあわせて設計書にし、ユーザーの承認を得ること。**決定は、設計書（W2〜W4）を確認した後に確定する。ここでは推奨の方向を示す。**

- **Q1: トリガー評価の入力**
  - 案 A（推奨）: `Mapping` が毎フレーム 1 回だけ作る読み取り専用の入力スナップショット（`DS4StateFieldMapping` ＋ `DS4State`／`DS4StateExposed`／前フレームの状態）を、`IInputAction.Evaluate` に渡す。割り当て 0（既存の `fieldMapping` の再利用）。
  - 案 B: `Evaluate` に `cState`、`eState`、`tp`、`fieldMapping` をそのまま渡す。
  - 理由: タッチ・ジャイロ・スワイプの判定は `DS4State` だけでは足りない。ホットパスの割り当て 0 が必須（`MappingHotPathAllocationTests`）。
- **Q2: `TriggerBinding` の構造と状態の所有**
  - 案 A（推奨）: 不変の `TriggerBinding : IActionBinding`（トリガー条件［`IInputAction`］＋ 出力［`IOutputAction` のリスト］＋ 復帰のポリシー ＋ 種別ごとの安全弁の設定）。実行時の状態は、**既存の `ActionInstanceState`**（`IManagedActionManager`）に残す。
  - 案 B: 状態も `TriggerBinding` に持たせる。
  - 理由: Phase8 では状態の置き場を動かさない（`ClearAllEntries`／`ClearDeviceState` が状態を作り直す暗黙の依存が、Profile の復帰の仕様と絡むため。台帳 §8 項目 11）。置き場の統合は Step8 以降。
- **Q3: エッジ検出・ガード・抑制の置き場**
  - 案 A（推奨）: 1 つの**ディスパッチエンジン**（仮称 `ITriggerDispatchEngine`。`Mapping` から毎フレーム呼ばれる）が、デバイスごとの `TriggerBinding` のリストを走査し、エッジ検出・Fresh-press ガード・`IsExecuting`・抑制（`suppressedTriggerButtons`）・復帰の合図を一元的に担う。`TriggerBinding`／`IInputAction`／`IOutputAction` は、判定と出力に専念する。
  - 案 B: 各 `TriggerBinding` が自分でガードを持つ。
  - 理由: 安全弁の維持（規約 §2.2）を 1 か所で保証でき、特性化テストを 1 つのエンジンに向けて書ける。種別ごとのガードの差（台帳 §4.2）は、`TriggerBinding` の設定（ガードを見るか、`IsExecuting` を見るか）で表現する。
- **Q4: 仕様台帳の「意図が不明な挙動」11 件の扱い**
  - 方針（推奨）: **Phase8 では挙動を変えない**。各件を「維持（現状の仕様として明文化）」「統一経路で揃える（挙動が変わる。承認が要る）」「不具合として別途是正」に分類し、旧実装（上流）との比較（`git log`）を添えて提示する。
  - 特に、Stage3 の Button の Press が Fresh-press ガードを見ない件は「維持」を推奨（挙動を変えるなら別途承認）。`IsSpecialActionTriggered` と `MapCustomAction` の判定の複製の不一致は、どちらを正とするかを旧実装との比較で決める。タップ判定（分岐 B）の到達不能は、到達不能を確認できたら移植しない（削除の承認が要る）。
- **Q5: Profile の「離したら復帰」の仕様化**
  - 案 A（推奨）: 復帰のポリシーを `TriggerBinding` の明示的な設定にする（`None`／`Automatic`［`AutomaticUntrigger`］／`Buttons`［`uTrigger`］）。離したときの `OnRelease` → `RestoreFromAction` に頼らない（現行の本番の挙動と同じ結果を、明示的な仕様にする）。復帰の合図（`UntriggerAction`／`UntriggerIndex`）は、`Mapping` の `deviceRuntime` から、エンジンの管理へ移す際に Phase7b の退避・復元と同等の保護を維持する。
- **Q6: `shiftTrigger`（Controls タブのシフト）のモデル**
  - 案 A（推奨）: シフト用の割り当てを、**別の `TriggerBinding`** として持ち、同じボタンの通常の割り当ての `TriggerBinding` と**排他グループ**（シフトボタンを押している間は、シフト側が優先され、通常側は抑制）にする。
  - 案 B: 条件クラスに「修飾ボタン」を持たせる。
  - 理由: AND 条件のモデルを壊さず、挙動（`Phase8Step1ShiftTriggerCharacterizationTests` で固定済み）を再現できる。
- **Q7: 移行の順序と、新旧の並行期間（D3＝C1）**
  - 方針（推奨）: 種別ごとに 1 PR。順序は、Button → Key → Macro → Program → Profile → その他（GyroCalibrate、DisconnectBT、BatteryCheck、ステアリング較正、XboxGameDVR／MultiAction は最後。実機の `DS4Device` を要する）。切り替えた種別は、`MapCustomAction` の該当の分岐を削除し、新エンジンが担う。**同じアクションを新旧の両方が処理しない**ことを、種別単位のスイッチ（`TriggerBinding` に登録された種別かどうか）で保証する。
  - Controls タブ（単一ボタン）は、特殊アクションの切替（Step7）が済んだ後に、同じエンジンへ載せる（D2＝B1）。
- **Q8: `IActionRegistry` の実装と、プロファイル読込・接続との接続**
  - 案 A（推奨）: DI の Singleton の実装（仮称 `ActionBindingRegistry`）。スロットごとに、不変の `IReadOnlyList<TriggerBinding>` を持ち、**参照の差し替えで更新**する（Phase7c W1 で保留した、読込側のリスト差し替えの本格案。`Clear()`→`Add()` の競合を構造的に排除）。構築は Step5（プロファイル読込時）、紐付けは Step6（コントローラー接続時）。
  - 注: `ClearState`／`ClearPendingRestore` の切断時の接続（Step6 に TODO）も、ここで意味（`OlderProfilePath` の扱い）を確定する。
- **Q9: `Mapping` との境界**
  - 案 A（推奨）: `Mapping` に `ITriggerDispatchEngine` をコンストラクター注入（`Func<>` の遅延が要るかは、循環［`IMapping` の Singleton との依存］を確認して決める）。`MapCustom` からの 1 呼び出しが、現行の `MapCustomAction` の呼び出し（`async void`）を置き換える。**エンジンは同期的に走る**（非同期の部分［プロファイル適用の `Task.Run`、マクロ］は出力側が担う）。割り当て 0 を守る。

## 4. Wave 構成（案）

- **W1: 意図の決定資料**: 台帳 §8 の 11 件ごとに、旧実装（上流）との比較、挙動を維持した場合・揃えた場合の影響、推奨を整理（`Phase8-Step2-Intent-Decisions.md`）。
- **W2: 契約の設計**: `IInputAction`（評価の入力、`Reset`）、`IOutputAction`（`Execute`／`Stop` の呼び出し規約、`IOutputContext` の過不足）、`TriggerBinding`、`IActionRegistry`、エンジンの契約（`Phase8-Step2-Design.md` の前半）。系統 A と系統 B の対応表、`KeyOutputAction` の `KeyAction` への委譲の設計（D1 の注記）。
- **W3: 状態・スレッド・安全弁の設計**: 状態の所有（`ActionInstanceState` との関係）、エンジンの 1 フレームの流れ（疑似コード）、6 大ガードレール（同一 XML 排他、適用時 Halt、スレッド直列化、On-Demand パス評価、ドライバ破棄順序、切断時クリーンアップ）への適合、ホットパス（割り当て 0）、プロファイル適用中の競合（`TriggerBinding` のリスト差し替えと `Halt` の関係）、`Mapping` の復帰の合図の移し方。
- **W4: 移行計画**: 種別ごとの切替（Step7）の PR 単位、新旧の二重ディスパッチ防止、各 PR の特性化テスト（Step1 のテストを新エンジンに向けて流用する方法）、旧コード（系統 B）の削除のタイミング（Step8）。
- **W5: モデル図の更新案**: 01〜04 の変更案（`Phase8-Step2-ModelDiagram-Update-Proposal.md`）。**図の変更は、ユーザーの指示を得てから行う**。
- **W6: レビューと承認**: 設計書全体の確認、決定事項（Q1〜Q9）の確定、Step3 の計画書の作成方針、完了報告書。

## 5. リスクと対応

- 設計が現行の挙動を再現できない: 仕様台帳と特性化テスト（Step1・Phase7c の 38 件）を、設計の検証基準にする。設計書の各要素に、対応するテストを紐付ける。
- 統一エンジンがホットパスの性能を悪化させる: 割り当て 0 の制約、`fieldMapping` の再利用、仮想呼び出しの回数を設計時に見積もる。Step3 でベンチマーク（簡易な処理時間の比較）を用意する。
- 循環依存（`Mapping` ↔ エンジン ↔ `IManagedActionManager`）: 依存の向きを設計書に明記する（第 2 層の内部。規約 §1）。`Func<>` の遅延が要るかを確認する。
- 意図決定が多く、承認が滞る: Q4 は「維持」を既定にし、挙動を変える項目だけを承認の対象に絞る。

## 6. 完了条件

- [ ] `Phase8-Step2-Design.md`（契約、状態、エンジンの流れ、移行計画）が完成し、ユーザーの承認を得ている。
- [ ] `Phase8-Step2-Intent-Decisions.md` の 11 件の意図決定が確定している。
- [ ] モデル図の更新案が提示されている（変更は指示を得てから）。
- [ ] Step3 の計画書の方針が決まっている（`ComboTriggerInputAction`、`KeyOutputAction` の書き直し、単体テスト）。
- [ ] 完了報告書。

## 7. 次の Step

- Phase8-Step3（トリガー条件クラス［`ComboTriggerInputAction`］の新設・単体テスト、`KeyOutputAction` の `KeyAction` への委譲への書き直し）。
