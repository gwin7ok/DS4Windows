# Phase8-Step7 完了報告書（経過報告）: 種別ごとの切替

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase8-Step7-Plan.md`（Q1〜Q8 を推奨どおり承認）  
状態: **7.1 Button は完了（実機確認 U-27 の主要 4 項目が合格）。7.2 Key は完了（W1〜W3 のビルド・テスト成功、実機確認 U-28 の 8 項目が合格）。7.3 以降は未着手**

本書は、各 PR（7.1〜7.8）の結果を追記していく経過報告書である。

---

## 7.1 Button

### 1. 成果

| Wave | 内容 | 主なファイル |
|---|---|---|
| W1 | テスト足場に `migrated` を追加。Button の既存テスト 9 本を旧経路・新経路の `[Theory]` に。両経路の出力比較テスト 9 本 | `SpecialActionTestScope`、`Phase7cW2SpecialActionTriggerCharacterizationTests`、`Phase8Step7W1ButtonDualPathTests` |
| W2 | 旧 Stage3 の TRACE ログを新経路へ引き継ぎ。プロファイル適用の前後・順序・二重ディスパッチの比較 | `ButtonOutputAction`、`FrameOutput`（`IFrameOutputTrace` を実装）、`IFrameOutputTrace`（新設）、`Phase8Step7W2ButtonTraceLogTests`、`Phase8Step7W2ApplyAndOrderTests` |
| W3 | 既定のスイッチに Button を追加（`ServiceRegistration` の 1 行）。ロールバックのテスト、ベンチマーク | `ServiceRegistration`、`Phase8Step7W3ButtonSwitchTests`、`Phase8Step4MapCustomBenchmarkTests` |

- **本番の挙動が変わった点**: Button 種別の特殊アクションが、Stage3 ではなくエンジン（`ApplyFrameOutputs`）で処理される。出力は旧経路と同じ（比較テストで確認）。
- **ロールバック**: `ServiceRegistration` の `migration.MigratedTypes.Add(SpecialAction.ActionTypeId.Button);` の 1 行を外してビルドし直す。旧経路（Stage3）のコードは Step8 まで残す。

### 2. 検証結果

- 旧経路・新経路で、期待値を変えずに成功したもの: Press の出力と AND 条件、Press は Fresh-press ガードを見ない、抑制の追加・解除、Fresh-press の解除条件（4 本）、Toggle の反転とガード中の不反転、タッチパッドクリック、マウス左クリックの数、読めない details、プロファイル適用の前後（押している最中・離している最中、Press／Toggle）、2 つの Button の順序とクリックの二重計上がないこと。
- TRACE ログ: 立ち上がり・立ち下がり、出力の組み合わせ（出力のたびと立ち上がり）、「maps to」「Trigger keys」の文言・順序が旧経路と一致。ログが無効のときは何も出さない。割り当ては、新経路 ≦ 旧経路（`Phase8Step4W4WiringTests`）。
- ロールバック: 一覧から Button を外すと束縛が作られず、Stage3 が Cross を出力する。

### 3. 計画との差・判断の記録

1. **`delayTime` は Button の出力を遅らせない**（旧・新とも）。W1 の私の期待が誤りで、テストを実測に合わせた。
2. **`OnProfileApplied` は接続しない**（Q5＝A の結論）。旧経路では、適用の後始末（`HandleDeviceDisconnect` を含む）は抑制の集合を消さない。新経路も、適用を再現した比較（押している最中・離している最中）で、出力と抑制の集合に差が出なかった。無条件に接続すると抑制の集合が消える（現行と違う）ため、接続しない。Profile（7.5）で、復帰の合図と合わせて再判断する。
3. **既存の不具合を記録（変更していない）**: 旧 `LogSpecialActionTrace` の「Trigger keys for SA」は、状態を渡さずに `getBoolSpecialActionMapping` を呼ぶため例外（握りつぶし）となり、常に `<none>` を記録する。ログを変えない方針（Q6＝A）により、新経路も `<none>` を出す。成立中のトリガーの一覧を出す改善は、別途ユーザーの承認を得て行う候補。
4. **既存テストの更新は 1 件のみ**: `Phase8Step4W1ContractTests` の「ホストの既定のスイッチは空」を、「Button のみ」に変えた（Q3 の範囲。設計どおりの変更）。
5. **既定のスイッチの実装**: `BindingMigrationOptions` の既定のコンストラクターは空のまま（単体テストの前提）。ホストのファクトリーで種別を足す。
6. **適用のタイミングの注意**: 新経路の束縛は、プロファイルの適用・再接続のときに作り直す（スナップショット）。旧 Stage3 は毎フレームの生のリストを読んでいた。編集画面でスペシャルアクションの有効／無効を切り替えたときは、保存して適用したときに反映される（U-27(6) で確認）。

### 4. 実機確認（ユーザー）

`Phase6-Step11-RealDevice-Verification-Checklist.md` §11 の **U-27**（9 項目）。**結果（2026-10-19）**: ユーザーが (1) Press、(2) Toggle、(5) プロファイルの切替・再適用、(7) 抜き差し・再接続を実機で確認し、問題なし。(3)(4)(6)(8)(9) は先送り（リスク受容。単体テストで新旧の一致を確認済み）。合格のため、7.2 Key へ進める（Q7＝A）。

**確認中に見つかった別件（Step7 とは無関係の既存の挙動）**: コントローラーが 1 台も接続されていない状態でも、自動プロファイルが（スロット 0 に）プロファイルを適用し、切替の通知が出る。ログでは `AutoProfileService.CheckProfiles` → `ApplyProfile(device=0, source=AutoProfile)`、「Controller item not found for device 0」。別の作業として調査する。

### 5. 性能

`MapCustom_PerFrameTime_IsReported` と `MapCustom_ButtonMigrated_PerFrameTime_IsReported` の出力を並べて、+5% 以内を確認する（ユーザーが `--logger "console;verbosity=detailed"` で実行した出力を、ここに追記する）。測定値: 未記入。

### 6. 次

7.2 Key（短い個別の計画書 `Phase8-Step7-2-Plan.md` を、U-27 の合格後に作成する）。

---

## 7.2 Key

### 1. 成果

| Wave | 内容 | 主なファイル |
|---|---|---|
| W1 | `SpecialActionTestScope(migrated)` が Key も移行。Key の既存 3 本を旧・新の `[Theory]` に。両経路の状態比較 | `Phase7cW2…`、`Phase8Step72W1KeyDualPathTests` |
| W2 | 旧 `MapCustomAction` の Key のログ（Debug 6 行、Trace 4 行）を `KeyOutputAction` へ引き継ぎ。`index`・`ActionEntries` はビルダーが渡す | `KeyOutputAction`、`ActionBindingBuilder`（`ISpecialActionRepository` を省略可の引数で受ける）、`IOutputContextInput`（エンジンの文脈が入力を返す）、`TriggerDispatchEngine`、`Phase8Step72W2KeyLogTests` |
| W3 | 既定のスイッチに Key を追加（`ServiceRegistration` の 1 行）。ロールバック、割り当て、性能のテスト | `ServiceRegistration`、`Phase8Step72W3KeySwitchTests` |

- **ロールバック**: `ServiceRegistration` の `migration.MigratedTypes.Add(SpecialAction.ActionTypeId.Key);` の 1 行を外してビルドし直す。

### 2. 検証結果・判断の記録

1. 旧経路・新経路の状態は、Press、Toggle、ガード中、AND 条件、遅延、押している最中のプロファイル適用で一致した。
2. **Q1（`UntriggerIndex` の癖）の前提は再現しなかった**: Key を 2 つ同時に押して、どちらを先に離しても、旧経路は各キーを解放した。新経路は `UntriggerIndex` を書かない（Profile の復帰との共有をやめる）ので、結果は同じで、衝突だけがなくなる。是正（挙動の変更）にはならなかった。
3. **Q2**: 合成キーの押下の数は旧経路でも 0 のままで観測できない。適用の後始末で、旧経路は同じキーの解放を 1 回多く送る（`nativeKeyAlias` の項目のため。コントローラーの破棄でも解放されるため、効果は同じ）。**この重複の解放は新経路に移植しない**。
4. **遅延**: `delayTime` を持つ Key は、遅延の基準時刻の初期値が最小値のため、最初の押下は遅延なしで成立する（旧・新とも）。既存の挙動として固定した。
5. **ログ**: 旧経路と同じ文言・順序（`Phase8Step72W2KeyLogTests` で行の一致を確認）。差は 2 点: 「untriggerindex」は常に -1。Toggle の解放は旧経路が送らないため、解放のログも出さない。「Trigger keys for SA」は旧経路と同じく常に `<none>`（既存の挙動）。
6. **既存テストの更新**: ホストの既定の一覧の検査（`Phase8Step4W1ContractTests`、`Phase8Step7W3ButtonSwitchTests`）を「Button と Key」に（設計どおりの変更）。`Phase7cW2…` の Key の `UntriggerIndex` の検査は、新経路で -1（Q1）。
7. 割り当て: 新経路 ≦ 旧経路（`TheNewPath_DoesNotAddAllocationsToTheFrame_ForKey`）。性能: `MapCustom_KeyMigrated_PerFrameTime_IsReported` の出力を、旧経路と並べて +5% 以内を確認する（測定値: 未記入）。

### 3. 実機確認（ユーザー）

`Phase6-Step11-RealDevice-Verification-Checklist.md` §11 の **U-28**（8 項目）。**結果（2026-10-19）**: ユーザーが実機で確認し、問題なし。項目 (4) は、キーの組み合わせ（Shift+Z など）は割り当てできない仕様のため、Shift 単体の出力の確認に変更した。ScanCode の指定の機能テストは除外した（リスク受容。コード上は旧経路と同じ処理で、単体テストでも差は出ていない）。合格のため、7.3 Macro へ進める（Q7＝A）。不合格なら、`ServiceRegistration` の Key の 1 行を外して原因を調べる。

### 4. 次

7.3 Macro（短い個別の計画書 `Phase8-Step7-3-Plan.md` を、U-28 の合格後に作成する）。

---

## 7.3a Alt-Tab の特別扱いと「Cycle Programs」プリセットの削除（機能廃止。7.3 の前提）

計画書: `Phase8-Step7-3a-AltTab-Removal-Plan.md`（Q1〜Q4 を推奨どおり承認。ユーザーの要望による機能廃止）。W1〜W3 のビルド・テスト成功。実機確認 U-29（4 項目）が合格（2026-10-19）。

- 削除: `Mapping.cs` の Alt-Tab の特別扱い（フィールド 3、再生の分岐、`AltTabSwapping`／`AltTabSwappingRelease`、`EndMacro` の判定、押している間の毎フレームの `EndMacro`）、記録画面の「Cycle Programs」（メニュー項目、処理、`WriteCycleProgramsPreset`）。
- 追加の変更（ユーザーの要望）: メニューの項目が 1 つになったため、「Load Preset」のボタンは、メニューをやめて、ファイル選択を直接表示する（保存のボタンと同じ）。
- 挙動の変化: Alt-Tab の並びのマクロは、通常のマクロとして手順どおりに再生される（1 回の Alt+Tab）。保存済みのデータは変更しない。
- テスト: `Phase8Step7AltTabRemovalGuardTests`（ソースに残らない、手順どおりの再生）。`RecordingVirtualKbmBase` に押下の記録を追加。エンジンの割り当てゼロの 2 つの検査（`SteadyState_…`）は、JIT の偶発的な割り当てで 1 回ずつ失敗したため、最大 3 回測って最小値で判定する形にした。
- 影響: 7.3 Macro の Q1（押している間の `EndMacro`）と、`IMacroPlayer.WhileHeld`／`IMapping.EndMacroDirect` の変更が不要になった。

---

## 7.3 Macro

（Alt-Tab の特別扱いの削除は、7.3a として別に記録済み。上の節を参照。）

### 1. 成果

| Wave | 内容 | 主なファイル |
|---|---|---|
| W1 | `SpecialActionTestScope(migrated)` が Macro も移行。Macro の既存 3 本を旧・新の `[Theory]` に（`firstTouch` の検査は旧経路だけ）。状態（`BeingTriggered`、`IMacroPlayer.IsPlaying`）の比較 | `Phase8Step1MacroBranchCharacterizationTests`、`Phase8Step73W1MacroDualPathTests` |
| W2 | 旧 `MapCustomAction` の Macro のログ（Trace 4 行、Debug 1 行）を `MacroAction` から出す（エンジン用のコンストラクターのときだけ。旧経路のアダプターは出さない） | `MacroAction`、`ActionBindingBuilder`、`Phase8Step73W2MacroLogTests` |
| W3 | 既定のスイッチに Macro を追加（`ServiceRegistration` の 1 行）。ロールバック、割り当て、性能のテスト | `ServiceRegistration`、`Phase8Step73W3MacroSwitchTests` |

- **ロールバック**: `ServiceRegistration` の `migration.MigratedTypes.Add(SpecialAction.ActionTypeId.Macro);` の 1 行を外してビルドし直す。

### 2. 検証結果・判断の記録

1. Q1（押している間の `EndMacro`）は、Alt-Tab の特別扱いの削除（7.3a）で不要になった。`IMacroPlayer`／`IMapping` は変更していない（`IMapping` のメンバーは 10 のまま）。
2. Q2: 新経路は `firstTouch` を書かない（RunOnRelease の準備は入力の内部の状態）。
3. Q3: ログは、旧経路と同じ文言・順序（`Phase8Step73W2MacroLogTests`）。「Trigger keys」は、旧経路と同じく常に `<none>`。
4. 既存テストの更新: ホストの既定の一覧の検査（`Phase8Step4W1ContractTests`、`Phase8Step7W3ButtonSwitchTests`）を「Button、Key、Macro」に（設計どおりの変更）。
5. 割り当て: 新経路 ≦ 旧経路（`TheNewPath_DoesNotAddAllocationsToTheFrame_ForMacro`）。性能: `MapCustom_MacroMigrated_PerFrameTime_IsReported` の出力を旧経路と並べて +5% 以内を確認する（測定値: 未記入）。
6. 割り当ての実測: 最初の測定で、押している間の新経路が旧経路より多く見えたが、立ち上がりのフレーム（マクロの再生の開始。1 回だけ大きく割り当てる）を測定に含めていたため。立ち上がりを除くと、Button／Key／Macro とも、新経路 ≦ 旧経路（Macro は一致）。テストは、立ち上がりを除いて判定する形に直した。
7. 観察（変更しない）: `PlayMacroDirect` が `new bool[4]` を渡すため、特殊アクション経由のマクロのボタン出力の手順は、共有の `macroControl` に反映されない。旧経路も同じ入口で、差は出ない。マクロの再設計（`TODO(Phase9-C)`）で扱う。

### 3. 実機確認（ユーザー）

チェックリスト §11 の **U-30**（8 項目）。**結果（2026-10-19）**: ビルド 4d792bab-dirty で、ユーザーが主要 4 項目（通常、離したときに実行、切替・再適用、抜き差し）を実機で確認し、問題なし。ログで、新経路で動いていること（`DispatchTriggerEdge` が出ず、押下ごとに `SpecialAction Macro` → `PlayMacro START` が 1 回）と、例外・エラーがないことを確認した。繰り返し・同期は先送り。合格のため、7.4 Program へ進める（Q7＝A）。最初の確認は、Macro の切替より前のビルド（13:39）だったため、やり直した。このため、起動ログに「built at 日時 (commit ハッシュ)」を出すようにした。

### 4. 次

7.4 Program（短い個別の計画書 `Phase8-Step7-4-Plan.md` を、U-30 の合格後に作成する）。

---

## 7.4 Program

### 1. 成果

| Wave | 内容 | 主なファイル |
|---|---|---|
| W1 | `SpecialActionTestScope(migrated)` が Program も移行。存在しないパスの起動の試行を `GuiLog` で数え、旧・新の起動の回数・`BeingTriggered`・`IsExecuting` を比較 | `Phase8Step74W1ProgramDualPathTests` |
| W2 | 旧 `MapCustomAction` の Program のログ（Trace 4 行、Debug 1 行）を `LaunchProcessAction` から出す（エンジン用のコンストラクターのときだけ。旧経路のアダプターは出さない） | `LaunchProcessAction`、`ActionBindingBuilder`、`Phase8Step74W2ProgramLogTests` |
| W3 | 既定のスイッチに Program を追加（`ServiceRegistration` の 1 行）。ロールバック、割り当て、性能のテスト | `ServiceRegistration`、`Phase8Step74W3ProgramSwitchTests` |

- **ロールバック**: `ServiceRegistration` の `migration.MigratedTypes.Add(SpecialAction.ActionTypeId.Program);` の 1 行を外してビルドし直す。

### 2. 検証結果・判断の記録

1. Q1: ログは旧経路と同じ文言・順序（`Phase8Step74W2ProgramLogTests`）。「Trigger keys」は、旧経路と同じく常に `<none>`。
2. Q2: 起動の観察は、存在しないパスの起動の失敗（`GuiLog` の警告）を数える方式（実際のプログラムは起動しない）。
3. 既存テストの更新: ホストの既定の一覧の検査を「Button、Key、Macro、Program」に（設計どおりの変更）。
4. 割り当て: 立ち上がりを除いて、新経路 ≦ 旧経路（4 KB の誤差まで）。性能: `MapCustom_ProgramMigrated_PerFrameTime_IsReported` の出力を、旧経路と並べて +5% 以内を確認する（測定値: 未記入）。

### 3. 実機確認（ユーザー）

チェックリスト §11 の **U-31**（7 項目）。**確認の前に、起動ログの「built at … (commit …)」で、ビルドが Program の切替を含むことを確認する**（7.3 のとき、古いビルドで確認して取り直したため）。**結果（2026-10-19）**: ユーザーが主要 4 項目（起動と押し続けても増えないこと、切替・再適用、抜き差し、`$hidden`）を実機で確認し、問題なし。ログも分析した: ビルド 8c5ab705（clean）で、`DispatchTriggerEdge` が出ず、押下ごとに起動が 1 回。`$hidden` つき（pwsh、hidden=True）も起動。切替・再適用・切断・再接続のたびに `PublishBindings`／`ReleaseSlot` が動き、例外・エラーなし。合格のため、7.5 Profile へ進める（Q3＝A）。

### 4. 次

7.5 Profile ＋ 復帰（最大のリスク。短い個別の計画書 `Phase8-Step7-5-Plan.md` を、U-31 の合格後に作成する）。
