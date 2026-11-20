# Phase8-Step1 計画書: 詳細監査（トリガー判定の棚卸し、特性化テストの完成、孤立コードの判断）

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Unified-Trigger-Dispatch-Plan.md`（D1〜D5 は確定。2026-10-19 追記［Phase7c W3］が事実誤認の訂正）、`Phase7c-Completion-Report.md` §5  
状態: **実施中（Q1〜Q6 を推奨どおり承認、2026-10-19）。W1 完了。進捗は `Phase8-Status.md`**

---

## 1. 目的と位置づけ

- Phase8 本体（Step2 以降の設計・実装）に入る前に、**現行の挙動を文書とテストで固定し、使うか捨てるかの判断を記録する**。Step1 では製品コードの挙動を変えない（テストと文書、必要なら TODO コメントのみ）。
- Phase8 計画書 §5 の Step1 の定義: 「`MapCustomAction` 内のトリガー判定ロジックの完全な棚卸し（デバウンス・抑制・トグル等の仕様を 1 件ずつ文書化）。系統 A の現在の呼び出し元が本当にゼロのままか再確認する」。これに、Phase7c で移した項目を加える。
- 前提（完了済み）: Phase7・7b・7c の完了。実機確認 U-24（Stage3 の競合の是正）は未（Step1 と並行して実施してよい）。

## 2. 作業項目

### A. トリガー判定の仕様の棚卸し（文書）
- 対象（行番号は着手時に再確認）: `Mapping.cs` の `MapCustomAction`（4223 行付近から約 1,100 行）、Stage3（2670〜2826 行）、`TryDispatchSATriggerEstablished`（3228 行付近）、`IsSpecialActionTriggered`／`CheckForSpecialActionSuppression`（6641〜6711 行付近）、`DispatchInputEdge`／`DispatchOrSetBeingTriggered`（618〜700 行付近）。
- 種別（Key／Button／Macro／Program／Profile／DisconnectBT／BatteryCheck／GyroCalibrate／SASteeringWheelEmulationCalibrate／XboxGameDVR／MultiAction ほか）ごとに、次を 1 件ずつ表にする。
  - トリガー成立の条件（AND、`delayTime`、タップ判定 `triggerToBeTapped`、`pressRelease`＋`firstTouch`）。
  - 解除（`uTrigger`、`automaticUntrigger`、`UntriggerAction`／`UntriggerIndex`）。
  - 安全弁（Fresh-press ガードを見る型・見ない型、`IsExecuting` を見る型）。
  - 抑制（`suppressedTriggerButtons` の追加・解除）。
  - 出力の経路（Stage3、`ActionManager.DispatchTriggerEdge`、`PlayMacro`、`ApplyProfile` ほか）。
- 注意点の記録: Stage3 の Button の Press は Fresh-press ガードを見ない（Toggle の反転だけが見る）。統一経路で意図して維持するか揃えるかは Step2 で決める。
- 成果物: `Phase8-Step1-Trigger-Spec-Ledger.md`（見出しと箇条書き、種別ごとの小表）。

### B. 特性化テストの完成（Phase7c W2 から移した分）
1. `uTrigger`／`delayTime`／`pressRelease` と「Unload on trigger release」の通し復帰（`Mapping.cs:5361` 付近、`ClearDeviceState` をまたぐ）。
2. 一時プロファイルのチェーンの条件（`uTrigger.Count == 0 && !automaticUntrigger`、`controls` 一致）。
3. Controls タブの `shiftTrigger`（`Mapping.cs:2216`、`3759`、`3776` 付近）と `MapCustom` の Key 出力（Press／Toggle）。
4. `ApplyProfile` 後の掃除の順序（`ClearAllEntries` → `ClearForDevice` → `ClearDeviceState` → `PreallocateForProfileApply`）。
5. `ControlTabAndSpecialActionKeyTests.cs` の 2 本の Assert の補強（`MockVirtualKBM` の押下・解放の回数）。
6. Macro 分岐（`pressRelease` の反転、K3 の二重実行防止）の `MapCustomAction` 経由の確認。
- 既存のハーネス `SpecialActionTestScope`（Phase7c）を拡張する。プロファイル切替を通す項目は、`IProfileSwitcher`／`IProfileApplicationService` のテスト用の差し替えが要る（実際のプロファイルの読込を走らせない）。差し替えの方法は Q2。
- 置き場所: `DS4WindowsTests`（xunit）。1 ファイル 1 型。

### C. 系統 A の呼び出し元の再確認と、孤立コードの §2.4 判断記録
- 再確認: `IActionRegistry.Register`／`GetBindingsForDevice`、`IActionBinding`、`IInputAction.Evaluate`、`IOutputHandler` ほかの呼び出し元が、今もゼロか（`git grep` で実測）。`Outputs` など同名の別物は、個別に確認する。
- 判断の対象（`Phase8-Unified-Trigger-Dispatch-Plan.md` 2026-10-19 追記 §8）:
  - 旧系統の孤立ヘルパー: `ReleasePolicy`、`ToggleController`、`MacroManager`、`SingleButtonTrigger`／`ITrigger`、`KeyInputAction`、`IActionRegistry`、`IOutputHandler`。
  - 上流由来の死にコード: `DateTimeJsonConverter`、`InputMethods`、`FakerInputMapping` の `ExpandedKeyboardKeys`、`ToggleActionController`（`[Obsolete]` の殻）。
  - `SensitivitySettings`（製品コードの参照 0 件。テストのみ）。
- 手順（規約 §2.4）: ① `git log -S` で導入コミットを特定し、当時の計画書・報告書で経緯を確認する。② 「本来その API を使うべきか」を、Pure DI 化と 4 層モデルに照らして判断する。③ 使うべきものは、TODO コメント（§3.3 原則 4）を付け、接続作業を Step の計画書へ追加する。使うべきでないものは削除候補とする（機能の削除に当たるものは承認が要る）。④ 判断と根拠を記録する。
- 成果物: `Phase8-Step1-Orphan-Decisions.md`。**削除・TODO コメントの追加は、判断を承認してから実施する**（Step1 内の別 Wave）。

### D. 静的な窓口の洗い出し（Step2 の設計入力）
- `Actions/` の `ServiceProviderHolder` 17 件（`ActionManager` 13／`ActionFactory` 1／`DefaultKeyButtonActionControllerFactory` 2／`KeyActionAdapter` 1）と、`ActionManager.actionInstances`／`actions`（静的な可変辞書）、`ActionRegistry`、`SyntheticDispatcher.bound`、`RepeatHelperToIRepeaterAdapter.RepeaterFactoryOverride` について、呼び出し元と、コンストラクター注入へ置き換える際の障害（静的 API を呼ぶ側の数、テストの依存）を一覧にする。
- 静的 `ActionManager.ClearDeviceState` のフォールバック本体（本番では呼ばれない。Phase7b の退避・復元が `DefaultActionManager` と二重）の扱いを記録する。
- `KeyButtonActionController.cs:169`、`319` の `new RepeatHelperToIRepeaterAdapter` を、注入の対象として記録する。
- 成果物: Trigger-Spec-Ledger の付録、または `Phase8-Step1-Static-Access-Inventory.md`。

### E. Step2 への入力（設計の論点の整理）
- Phase8 計画書 §6 のリスクのうち、`shiftTrigger` が AND 条件のモデルに収まるか、Press と Toggle の Fresh-press ガードの差、`IProfileSwitcher` を直接呼ぶ経路のガード（Phase8 計画書 2026-10-14 追記の申し送り 2）、`MouseOutputAction` の完成（同追記 1）を、A・B の結果をもとに整理する。
- モデル図 01〜04 の更新案（`TriggerBinding`、`IActionBinding` 系）は、Step2 で作る。**モデル図の変更はユーザーの指示を得てから行う。**

## 3. Wave 構成（案）

- W1: A（トリガー判定の棚卸し）。文書のみ。
- W2: B-1〜B-4（特性化テスト。ハーネスの拡張を含む）。
- W3: B-5・B-6（Assert の補強、Macro 分岐）。
- W4: C の調査（呼び出し元の再確認、導入コミットの調査）と、判断の案の提示。
- W5: C の実施（承認された削除・TODO コメントの追加）と、D・E の文書。
- W6: 完了報告書、`Phase7-Status.md` に準じる進捗文書（`Phase8-Status.md` を新設）の更新。

## 4. 決定してほしい点（質問と推奨）

- **Q1（特性化テストの深さ）**: 種別ごとに、成立・保持・解除・ガードの 4 点を固定する（推奨）か、Phase8 で直接書き換える種別（Key、Button、Macro、Profile、Program）に限定するか。推奨は前者。ただし GyroCalibrate、DisconnectBT、BatteryCheck、ステアリング較正は、実機の `DS4Device` に依存するため、ガードの分岐のみ（実行前にブロックされること）に限る。
- **Q2（プロファイル切替を通すテストの差し替え）**: `IProfileApplicationService` を、記録するだけの実装にして、`Mapping` へ注入する（推奨）か、実物のまま、存在しないプロファイル名で呼ぶか。推奨は前者（`TestMappingFactory.Create` で `Mapping` を作る。実際の読込と `Task.Run` の副作用を避ける）。`Task.Run` の完了待ちは、記録側の `ManualResetEventSlim` で同期する。
- **Q3（孤立コードの判断の進め方）**: C の判断は一括の文書で提示して承認を得る（推奨）か、1 件ずつ確認するか。推奨は一括。削除は機能の削除に当たるか（例: `FakerInputMapping` の `ExpandedKeyboardKeys`）を、各件で明記する。
- **Q4（実機確認 U-24 の扱い）**: Step1 の前に行う（推奨。Stage3 の修正を実機で確定させておく）か、Step1 と並行するか。
- **Q5（Phase8 の進捗文書）**: `Phase8-Status.md` を新設する（推奨。Phase6・7 と同じ運用）か、`Phase8-Unified-Trigger-Dispatch-Plan.md` に追記するか。
- **Q6（Step1 の範囲外にするもの）**: 第 1 層（`DS4Devices`／`DS4LightBar`／`HidDevices`）の静的な状態は、Phase8 の統合ディスパッチと直接の関係がないため、Step1 では扱わない（推奨）。

## 5. リスクと対応

- 行番号が変わる（`Mapping.cs` は今後も変わる）: 棚卸しは、メソッド名と周辺の条件の記述で場所を特定し、行番号は着手時点の参考値とする。
- 実機の `DS4Device` を要する分岐（DisconnectBT、BatteryCheck ほか）をテストで走らせられない: Q1 のとおり、ガードの分岐までに限り、残りは Phase8 の実機確認（Step9）の項目として記録する。
- 特性化テストが、現行の「意図しない挙動」まで固定してしまう: 固定する挙動のうち、意図が不明なもの（例: Button の Press がガードを見ない）は、棚卸しの文書に「現状の挙動。Step2 で意図を決める」と明記する。
- テストの実行順への依存（Phase7c で `Application` の初期化で経験）: 新しいテストは、他のテストクラスの副作用に頼らない。WPF の初期化が要るものは、ファクトリーの静的初期化に置く。

## 6. 完了条件

- [ ] A: Trigger-Spec-Ledger が、種別ごとの 4 点（成立・解除・安全弁・出力）を網羅している。
- [ ] B: 特性化テスト 6 項目が成功する（ユーザーが実行）。ラチェットの一致検査・複数型ファイル 0 のガードも成功する。
- [ ] C: 孤立コードの判断が記録され、承認された分の TODO コメントの追加・削除が済む。
- [ ] D・E: 静的な窓口の一覧と、Step2 への論点の整理が済む。
- [ ] ビルド警告 0、全テスト成功（ユーザーが実行）。製品コードの挙動は変えない。
- [ ] 完了報告書。

## 7. 次の Step

- Phase8-Step2（詳細設計: `IActionBinding`／`IInputAction`／`IOutputAction`／`IActionRegistry` の具体的なメンバー確定、`KeyOutputAction` の `KeyAction` への委譲設計、モデル図 01〜04 の更新案）。
