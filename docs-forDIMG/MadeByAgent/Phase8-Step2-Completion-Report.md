# Phase8-Step2 完了報告書: 詳細設計（統一トリガーディスパッチの契約・状態・移行方法の確定）

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase8-Step2-Plan.md`。設計書: `Phase8-Step2-Design.md`（§1〜§13 契約、§14〜§21 状態・手順・スレッド、§22〜§28 移行計画）。意図決定: `Phase8-Step2-Intent-Decisions.md`。モデル図の更新案: `Phase8-Step2-ModelDiagram-Update-Proposal.md`  
状態: **完了（設計の承認済み。2026-10-19）**。製品コードは変更していない（成果物は文書のみ）。モデル図の段階 A は反映済み（2026-10-19）

---

## 1. 結果の要約

- 統一ディスパッチの**契約、状態の所有、1 フレームの手順、スレッドの扱い、6 大ガードレールへの適合、移行の方法**を確定した。
- 仕様台帳の「意図が不明な挙動」は、**Phase8 では挙動を変えない**方針で分類し、承認を得た（挙動を変える項目なし）。
- W3 の調査で、W2 の契約に **5 つの修正**が必要と判明し、承認を得て反映した（エンジンの入口を 2 つに分ける、`DelayClock` のデバイス共有、`IHeldOutputAction`、`BindingPolicy` の 2 項目、離したときの `Stop` の扱い）。
- 移行は、**Step3〜Step6 では本番の挙動を変えず**、Step7 で種別ごとにスイッチ（`BindingMigrationOptions`）で切り替える。**旧コードは Step8 まで残し、ロールバックはスイッチだけで行う**（D5 の「種別ごとの削除」を「Step8 でまとめて削除」に読み替え）。

## 2. 決定事項（Q1〜Q9 と W2〜W5 の承認）

- **Q1＝A**: トリガー評価の入力は、`Mapping` が毎フレーム更新する読み取り専用の `ITriggerInput`（割り当て 0。時刻の注入を含む）。
- **Q2＝A**: `TriggerBinding` は不変の設定。実行時の状態は既存の `ActionInstanceState` に残す。
- **Q3＝A**: エッジ検出・ガード・抑制・復帰は、1 つのエンジン（`ITriggerDispatchEngine`）が一元的に担う。
- **Q4**: Phase8 では挙動を変えない。意図の決定（12 件）は承認済み。UI との不整合（Key の Toggle で解除トリガーを必須とする検証）は別件として記録。
- **Q5＝A**: 復帰のポリシー（なし／自動／解除ボタン）を `TriggerBinding` の明示的な設定にする。
- **Q6＝A**: シフトは別の `TriggerBinding` ＋ 排他グループ。
- **Q7**: Step7 は 1 種別 ＝ 1 PR。順序は Button → Key → Macro → Program → Profile ＋ 復帰 → デバイスを要する種別 → MultiAction → Controls タブ（別の設計書）。
- **Q8＝A**: `IActionRegistry` は DI の Singleton。スロットごとの不変リストを、参照ごと差し替える。
- **Q9＝A**: `Mapping` にエンジンを注入。エンジンは同期で動く（`async void` の廃止）。
- **W5（モデル図）**: 適用は 3 段階（A: 目標像を点線で追加、B: Step7 の PR ごとに実線化、C: Step8〜9 で旧記述を削除）。01 は第 2 層にサブグラフを追加（層をまたぐ辺は 16 本のまま）、03 は約 20 クラスのクラス図を追加、04 は 4 行を追加（移行スイッチも載せる）。

## 3. 設計で判明した、現行の仕様に関する事項（記録）

- Profile の「離したら復帰」は、プロファイル適用の掃除（状態の作り直し）に暗黙に依存している（Intent 項目 11）。明示的な仕様にする。
- 復帰待ちの間の `else` は、毎フレーム通る（台帳の記述を訂正済み）。
- `prevInputEstablished`（前回の入力状態のキャッシュ）が、プロファイル適用後に残り、`handled == false` のフォールバックで余分に実行される可能性（推定）。新エンジンはこのキャッシュに頼らない。Step3 のテストで確認する。
- `delayTime` の基準時刻は、デバイスのすべての遅延つきアクションで共有されている。新設計でも共有を維持する。
- `KeyActionAdapter`／`KeyButtonActionControllerAdapter` が `useScanCode` を下流に渡さず `false` 固定（ScanCode 指定が効いていない可能性）。現状どおり再現し、別件として記録する。

## 4. Step3 の計画書の方針

`Phase8-Step3-Plan.md` は、次の範囲で作成する（Step3 の着手前に、個別の計画書で承認を得る）。

- **範囲（本番の挙動は変えない。配線なし）**: `ITriggerInput`／`TriggerInput`（`DelayClock` を含む）、`TriggerState`、`IInputAction` の改訂、`ComboTriggerInputAction`、`BindingPolicy`／`UntriggerPolicy`、`TriggerBinding`、`IActionBinding` の改訂（`OnTriggered`／`OnReleased` の削除）、`KeyOutputAction` の書き直し、`KeyInputAction`／`ITrigger`／`SingleButtonTrigger` の削除。
- **テスト**: `ComboTriggerInputAction` の単体テスト（AND、遅延、反転、時刻の差し替え）、`KeyOutputAction` と系統 B の挙動一致（論理キー・ネイティブキーの求め方の一致を含む）、割り当て 0、簡易な処理時間の比較の用意。
- **Wave の目安**: W1 契約の型の追加（インターフェース・値型・ポリシー）、W2 `ComboTriggerInputAction`、W3 `KeyOutputAction` の書き直しと一致テスト、W4 不要な型の削除、W5 処理時間の比較の基盤、W6 完了報告書。
- **確認する質問（Step3 の計画書で提示）**: `TriggerState.Gesture` を Step3 に含めるか（`MultiTapInputAction` は Step7 の最後）、`ITriggerInput` の `WasActive` の実装（前フレームの表の遅延生成）の範囲、命名（`ComboTriggerInputAction` の名称を維持するか）。

## 5. 未完了・持ち越し

- **モデル図の変更**: 段階 A［目標像の点線での追加］は 01〜04 に反映済み（2026-10-19）。段階 B（Step7 の PR ごとに実線化）・段階 C（Step8〜9 で旧記述を削除）は今後。
- Controls タブ（`ProcessControlSettingAction`）の統合の設計（Step7.8）: 7.7 の後に、別の設計書（`Phase8-Step2b-ControlsTab-Design.md`）を作る。
- `MultiTapInputAction` の状態の置き場（Step7.7 で決める）。
- 設計の検証: 設計書の各要素に対応するテストの紐付けは、Step3 以降の計画書で行う。
- 実機確認 W2b-5、K7b-1、K9-3、UI の不整合（Key の Toggle の解除トリガー）は、これまでどおり持ち越し。

## 6. 次の Step

- **Phase8-Step3**（判定と束縛の型の新設。配線なし）。着手前に `Phase8-Step3-Plan.md` を作成して承認を得る。
- その後の順序: Step4（エンジンと出力の型、`Mapping` への空配線）→ Step4b（`LoadProfile`／`SaveProfile` の分解）→ Step5（登録簿とビルダー）→ Step6（接続・切断との連携）→ Step7（種別ごとの切替）→ Step8（旧コードの削除）→ Step9（総合検証）→ Phase9。
