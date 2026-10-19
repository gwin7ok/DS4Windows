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

---

## 7.5a Profile（切替のみ。復帰を持たないもの）

計画書: `Phase8-Step7-5-Plan.md`（Q1〜Q6 を推奨どおり承認）。復帰（7.5b）は、7.5a の実機確認の合格後に、個別の計画書を作る。

### 1. 成果

| Wave | 内容 | 主なファイル |
|---|---|---|
| W1 | `SpecialActionTestScope(migrated)` が Profile も移行。切替・復帰の回数、ガード、実行中、AND 条件、遅延、押している最中の適用を旧・新で比較。フレームの打ち切り・キーの解放・連鎖の防止は、旧経路で固定 | `Phase8Step75aW1ProfileSwitchDualPathTests` |
| W2 | `IGatedOutputAction`（立ち上がりの実行の条件の口）を新設。`ProfileSwitchAction`（エンジン用のコンストラクター）に、連鎖の防止・ログ・トリガーのボタンのキーの解放・切替後のフレームの打ち切りを実装。ビルダーは、復帰を持つ Profile の束縛を作らない（`ProfileUntriggerMigrated = false`）。W1 の後半を旧・新の両経路に | `IGatedOutputAction`、`TriggerDispatchEngine`、`ProfileSwitchAction`、`ActionBindingBuilder`、`Phase8Step75aW2ProfileLogTests` |
| W3 | 既定のスイッチに Profile を追加（`ServiceRegistration` の 1 行）。ロールバック、復帰つきが旧経路のままであること、割り当て、性能のテスト | `ServiceRegistration`、`Phase8Step75aW3ProfileSwitchTests` |

- **ロールバック**: `ServiceRegistration` の `migration.MigratedTypes.Add(SpecialAction.ActionTypeId.Profile);` の 1 行を外してビルドし直す。

### 2. 検証結果・判断の記録

1. Q2: 連鎖の防止は、`IGatedOutputAction`＋既存の `IMapping.CaptureProfileUntrigger`・`GetUseTempProfile` で評価（`IMapping` のメンバーは 10 のまま）。
2. Q3: `prevProfileName`／`prevProfileWasTemporary`（書くだけで、読む場所がない）は、新経路で移していない。Step8 で、旧コードと一緒に削除を提案する。
3. Q4: ログ（Trace 4 行、Debug 3 行）、トリガーのボタンのキーの解放、切替後のフレームの打ち切りは、旧経路と同じ条件・順序（W1 の旧・新の比較と、`Phase8Step75aW2ProfileLogTests`）。「Trigger keys」は、旧経路と同じく常に `<none>`。
4. 既存テストの更新（設計どおり）: `Phase8Step5W1BuilderTests` の Profile の検査（復帰つきは束縛を作らない）、ホストの既定の一覧の検査（Button、Key、Macro、Profile、Program）。
5. `ControlServicePr4ShimEquivalenceTests.GetterShims_UsedOnReportPath_DoNotAllocate`: コメントにあるウォームアップが実際になく、初回 JIT の割り当てで偶発的に失敗したため、ウォームアップを足した（判定は 0 バイトのまま）。
6. 割り当て: 立ち上がりを除いて、新経路 ≦ 旧経路（4 KB の誤差まで）。性能: `MapCustom_ProfileMigrated_PerFrameTime_IsReported` の出力を、旧経路と並べて +5% 以内を確認する（測定値: 未記入）。

### 3. 実機確認（ユーザー）

チェックリスト §11 の **U-32**（7 項目。解除トリガー［手動 uTrigger］の Profile は、UI から設定できないため、確認から外した）。確認の前に、起動ログの「built at … (commit …)」で、ビルドを確認する。**結果（2026-10-19）**: 合格。ビルド e0d9fa56 で、ユーザーが主要項目を確認し、問題なし（独自ウィンドウの通知も想定どおり）。ログ: 切替のみは `DispatchTriggerEdge` が出ず新経路、Unload つきは旧経路のまま。最初の確認は古いビルド 8c5ab705 で、無効だった。7.5b（復帰）へ進む（Q5＝A）。

### 4. 次

7.5b 復帰（`Phase8-Step7-5b-Plan.md`。次節）。

## 7.5b Profile の復帰（Unload on trigger release）

### 1. 成果

- 範囲: **自動の復帰（`uTrigger` なし）だけ**をエンジンへ移した（Q1＝A）。解除トリガー（手動 uTrigger）を持つ Profile は、UI から作れない（a7277da9 で削除）ため、旧経路のまま（Q2＝A）。Step8 で、廃止（ユーザーの承認が要る）か移行かを決める。
- W1: `Phase8Step75bW1ProfileUntriggerDualPathTests`（復帰の特性化を旧・新の両経路で。ビルド・テスト成功）。
- W2: 復帰の合図をエンジンが持つ（`TriggerDispatchEngine.Pending`。適用で消えず、物理切断で消える）。`ITriggerDispatchEngine.ProcessUntrigger`、`IPendingUntriggerQuery`（連鎖の防止がエンジンの合図も見る）、`ActionBindingBuilder`（復帰つきも束縛を作る。`uTrigger` つきは作らない。復帰つきは `stopOnRelease: false`）、`Mapping`（旧ループの後で `ProcessUntrigger`）。ビルド・テスト成功。
- W3: `Phase8Step75bW3ProfileUntriggerSwitchTests`（ロールバック、割り当て、性能の出力）。既定の一覧は 7.5a のまま（Profile を含む）。

### 2. 検証結果・判断の記録

1. Q3＝A: 合図は切替前の束縛そのものを持つ。復帰の `Stop` はその出力を使う（ロックの外で呼ぶ）。
2. Q4＝A: W1 の報告のみのテストの出力は未確認のまま、二重の復帰を避ける安全側（復帰つきは離したエッジの `Stop` を呼ばない）で実装した。本番では、適用が状態を作り直すため、離したエッジの `Stop` は起きない（設計書 §15.1）。
3. Q5＝A: `IMapping.CaptureProfileUntrigger`／`RestoreProfileUntrigger` は残す（`uTrigger` つきの旧経路が使う）。Step8 で扱う。
4. `prevProfileName`／`prevProfileWasTemporary` は、7.5a のとおり移していない（Step8 で削除を提案）。
5. 性能・割り当て: 出力（`MapCustom_ProfileUntriggerMigrated_PerFrameTime_IsReported` ほか）は、結果の報告のあとに記入する（測定値: 未記入）。

### 3. 実機確認（ユーザー）

チェックリスト §11 の **U-33**（7 項目）。確認の前に、起動ログの「built at … (commit …)」でビルドを確認する。**結果（2026-10-20）**: 合格。ユーザーが最低限の 4 項目［基本の復帰、押し続け、再適用、抜き差し］を実機で確認し、問題なし。7.6 へ進む（Q6＝A）。

### 4. 次

7.6（実機を要する種別。1 種別ずつ）。

## 7.6a GyroCalibrate

### 1. 成果

- 実機のコントローラーを操作する窓口 `IGyroCalibrationPort`（Actions サブシステムのため `Actions/`）と、本番の実装 `DefaultGyroCalibrationPort`（`Func<IDeviceStateAccessor>` を遅延で受ける）、出力 `GyroCalibrateAction` を新設。
- `ActionBindingBuilder` が GyroCalibrate の束縛を作る（ポートがなければ作らない）。`ServiceRegistration` の既定の一覧に GyroCalibrate の 1 行を追加（ロールバックはその 1 行を外す）。
- 旧経路との違い: 旧経路は、コントローラーがいない（null）と例外で止まるが、新経路は何もしない。立ち上がりの 1 回だけ実行する点（`BeingTriggered` と Fresh-press ガード）は同じ。
- ログ: Trace 4 行（旧経路と一致をテストで確認）、Debug「SpecialAction GyroCalibrate: …」。

### 2. 検証結果・判断の記録

1. Q2＝A（窓口を介す）。実機がないテストでは、記録用のポートで、回数・条件（Fresh-press ガード、AND 条件）・デバイスの添字を確認。
2. 割り当て: 押している間（1000 フレーム）、同じ件数でトリガー未成立の基準 ＋ 4 KB 以内。
3. 性能の出力: なし（種別が 1 つだけで、30 件の比較は意味が薄いため）。

### 3. 実機確認（ユーザー）

チェックリスト §11 の **U-35**。確認の前に、起動ログの「built at … (commit …)」でビルドを確認する。

### 4. 次

7.6b DisconnectBT（次節）。7.6a の実機確認 U-35 は先送り（2026-10-20 ユーザー判断。Step9 で、Phase8 の完了をこの確認にかけるかを決める）。

## 7.6b DisconnectBT

### 1. 成果

- 窓口 `IControllerDisconnectPort`（`Actions/`）と本番の実装 `DefaultControllerDisconnectPort`、`DisconnectTargetKind`、出力 `DisconnectBTAction`、キーを離す共通の部品 `TriggerButtonKeyReleaser`（`ProfileSwitchAction` も使う。Q5＝A）を新設。
- `ActionBindingBuilder` が DisconnectBT の束縛を作る（ポートがなければ作らない）。`ServiceRegistration` の既定の一覧に DisconnectBT の 1 行を追加（ロールバックはその 1 行を外す）。
- **挙動の変更（Q3＝B。承認済み）**: 切断の要求が成功したら、繰り返さない（旧経路は、切断の完了まで毎フレーム、`DisconnectBT()` とトレースログを繰り返した）。失敗したら、押している間、再試行する。成功のあとも、切断の完了までのフレームの打ち切りは続く。
- 注意（調査の事実）: `DS4Device.DisconnectBT()` は、Windows の API の結果を使わず、MAC があれば常に true を返す（既存のコードの `success = true;`）。このため、実際に「失敗」として見えるのは、コントローラーがいない・MAC がない・例外のときに限られる。API の失敗を見分けるには、`DS4Device.DisconnectBT()` の戻り値の見直し（ほかの呼び出し元の確認を伴う）が別途要る。

### 2. 検証結果・判断の記録

1. Q2＝A（窓口を介す）。実機がないテストでは、記録用のポートで、回数・条件・再試行・ドングルの切断待ち・キーの解放を確認。
2. ログ: 試行ごとに Trace 4 行（Debug はなし。旧経路と同じ）。
3. 割り当て: 押している間（1000 フレーム）、同じ件数でトリガー未成立の基準 ＋ 4 KB 以内。

### 3. 実機確認（ユーザー）

チェックリスト §11 の **U-36**。確認の前に、起動ログの「built at … (commit …)」でビルドを確認する。**結果（2026-10-20）**: 先送り（ユーザー判断。Step9 で、Phase8 の完了をこの確認にかけるかを決める）。

### 4. 次

7.6c BatteryCheck（次節）。

## 7.6c BatteryCheck

### 1. 成果

- 窓口 `IBatteryCheckPort`（`Actions/`）と本番の実装 `DefaultBatteryCheckPort`、出力 `BatteryCheckAction` を新設。ライトバーの強制色（静的な `DS4LightBar`）と通知（`AppLogger.LogToTray`）は、窓口の内側に閉じ込めた（Q6＝A）。
- `ActionBindingBuilder` が BatteryCheck の束縛を作る（ポートがなければ作らない）。`ServiceRegistration` の既定の一覧に BatteryCheck の 1 行を追加（ロールバックはその 1 行を外す）。
- **ログの変更（Q8＝B。承認済み）**: BatteryCheck の立ち上がりで出ていた、コピーの名残りのログ「SpecialAction WheelRecalibrate: …」を削除（新経路・旧経路とも）。BatteryCheck が実行されたことは、既存の Debug「SpecialAction BatteryCheck: …」で分かる。
- そのほかの違い: `details` の読み取りは最初の 1 回、遷移の進みと保存した色は出力アクションのインスタンスが持つ（適用で束縛が作り直されると 0 から）、不正な `details` は何もしない。

### 2. 検証結果・判断の記録

1. 実機がないテストでは、記録用のポートで、順序（通知 → 強制色 → 遷移）、遷移の 2 ずつ・100 まで、離したときの戻し、フラグ（通知／ライトバー）、Fresh-press ガード、AND 条件を確認。
2. ログ: 立ち上がりで Trace 4 行 ＋ Debug 1 行。`WheelRecalibrate` は出ない。
3. 割り当て: 押している間（1000 フレーム）、同じ件数でトリガー未成立の基準 ＋ 4 KB 以内。

### 3. 実機確認（ユーザー）

チェックリスト §11 の **U-37**。確認の前に、起動ログの「built at … (commit …)」でビルドを確認する。**結果（2026-10-20）**: 先送り（ユーザー判断。Step9 で、Phase8 の完了をこの確認にかけるかを決める）。

### 4. 次

7.6d ステアリング較正（次節）。

## 7.6d ステアリング較正（SASteeringWheelEmulationCalibrate）

### 1. 成果

- 窓口 `IWheelCalibrationPort`（`Actions/`）と本番の実装 `DefaultWheelCalibrationPort`、出力 `SteeringWheelCalibrateAction` を新設。
- `ActionBindingBuilder` が束縛を作る（ポートがなければ作らない）。`ServiceRegistration` の既定の一覧に `SASteeringWheelEmulationCalibrate` の 1 行を追加（ロールバックはその 1 行を外す）。
- 旧経路と同じ: 押している間、毎フレーム（立ち上がりを含む）、較正の状態が 0 なら 1、2 なら 3 に進める（前回から 3 秒の間隔）。状態を変えるときだけ Trace 4 行。
- 違い: コントローラーがいないとき、例外にせず何もしない。

### 2. 検証結果・判断の記録

1. 実機がないテストでは、記録用のポートで、0→1、2→3、3 秒の間隔（`firstTap` の書き換えで再現）、離して押し直し、Fresh-press ガード、AND 条件、ログ（状態を変えたときだけ Trace 4 行）を確認。
2. 割り当て: 押している間（1000 フレーム）、同じ件数でトリガー未成立の基準 ＋ 4 KB 以内。

### 3. 実機確認（ユーザー）

チェックリスト §11 の **U-38**。ジャイロのステアリング操作の設定が必要。**結果（2026-10-20）**: 先送り（ユーザー判断。Step9 で、Phase8 の完了をこの確認にかけるかを決める）。

### 4. 次

Step7 の 7.6 は、これで 4 種別（GyroCalibrate、DisconnectBT、BatteryCheck、ステアリング較正）がそろった。次は 7.7（XboxGameDVR の削除［7.7a］と MultiAction の移行［7.7b］。`Phase8-Step7-7-Plan.md`）。

## 7.7a XboxGameDVR の削除（機能削除）

### 1. 成果

- ユーザーの決定（2026-10-20）: 「この機能自体が不要」として、機能削除を前提に計画（規約 2.2 の、ユーザーの明示的な承認）。
- 削除したもの: `SpecialAction.ActionTypeId.XboxGameDVR`、`SpecialAction` の構築の `XboxGameDVR` の分岐、`Mapping.MapCustomAction` の `XboxGameDVR` の条件と、整数からマクロへの置き換え（3 か所）。
- 削除の根拠（調査の事実）: (1) 構築時に `details` がマクロの文字列に置き換わるのに、旧経路は整数として読み直すため、マクロが実行されなかった。(2) 編集画面の種別の一覧になく、新しく作れなかった。(3) `Actions.xml` への保存で、書き出されなかった。
- 古い `Actions.xml` の `<Type>XboxGameDVR</Type>` の項目は、種別が不明（`None`）の項目になる（何も実行されない。次の保存で書き出されない。今も保存で書き出されないため、実質同じ）。
- 実機確認は不要（動いていない機能の削除のため）。

### 2. 検証

`Phase8Step77aXboxGameDvrRemovalTests`（列挙にない、古い項目が例外にならず `None` になる、MultiAction の `details` が保たれる、ソースに残っていない）。

### 3. 次

7.7b MultiAction（次節）。

## 7.7b MultiAction

### 1. 成果

- 独立した入力アクション `MultiTapInputAction` を新設。トリガーの AND（`ComboTriggerInputAction`）と、`trigger[0]` の押下・解放のエッジ、時刻、`DebouncingMs` から、旧経路と同じ状態機械を動かし、`TriggerState.Gesture`（タップ／長押し／ダブルタップ）を返す。状態は入力アクションのインスタンスが持つ。
- **設計の見直し（W2b。ユーザーの提案、Q8＝A・Q9＝A）**: ジェスチャーから出力への対応を、出力層ではなく、変換層（ビルダーが作る束縛）に置いた。`BindingPolicy.RoutesOutputsByGesture` を追加し、エンジンは、その項目が真の束縛で `Gesture` が 0 でないフレームにだけ `Outputs[Gesture-1]` を実行する。出力は、汎用の `MacroStringAction`（マクロの文字列を再生）。`IGestureOutputAction`／`MultiActionOutput`／`IMultiActionMacroPlayer`／`DefaultMultiActionMacroPlayer` は作らない（W2 の初版を、実装後・コミット前に置き換えた）。窓口は `IMacroStringPlayer`／`DefaultMacroStringPlayer`（`Mapping` が実装。`IMapping` は 10 メンバーのまま）。
- **配線の是正**: `Mapping.MapCustom` が `TriggerInput.Update` に前フレームの状態を `null` で渡していた（Step4）ため、新経路で MultiAction の押下・解放が見えなかった。デバイスの前回のレポート（`getPreviousStateRef`。旧経路と同じ）を渡すようにした。前フレームの表は `WasActive` が最初に呼ばれたときにだけ作る（遅延）ため、MultiAction がなければ費用は増えない。
- `ServiceRegistration` の既定の一覧に MultiAction の 1 行を追加（ロールバックはその 1 行を外す）。

### 2. 検証結果・判断の記録

1. W1（旧経路の特性化。テスト用のデバイス `TestDs4Device` を使用）: タップ、長押し、ダブルタップ、空のマクロ、`DebouncingMs`、Fresh-press ガードを見ない、トリガーの出力の消去、前の特殊アクションのフレームの打ち切りでの見逃し。旧・新の両経路で成功。
2. W2: 偽の入力（時刻と押下を差し込める）での状態機械、出力、ビルダー、ロールバック、窓口、旧・新のログの一致（マクロの直前の Trace 4 行）、割り当て。成功。
3. 旧経路との違いなし（旧ループの位置で動くため、見逃しも同じ）。

### 3. 実機確認（ユーザー）

チェックリスト §11 の **U-39**。確認の前に、起動ログの「built at … (commit …)」でビルドを確認する。**結果（2026-10-20）**: 先送り（ユーザー判断。Step9 で、Phase8 の完了をこの確認にかけるかを決める）。

### 4. 次

7.8（Controls タブ。別の設計書を先に作る）。その後 Step8（旧コードの削除）。

## 7.8a Controls タブの土台と、Key／Button の出力の値渡し化（2026-10-20）

計画書: `Phase8-Step7-8a-Plan.md`（Q1〜Q4 承認済み。推奨どおり）。動作・ログは変えない（実機確認は作らない。Q8＝B、Step9 でまとめて行う）。

- **値渡し化**: Key／Button の出力が `SpecialAction` から設定を解釈していたのを、変換層（`ActionBindingBuilder.InterpretKey`／`InterpretButton`）に移し、出力は `KeyBindingValues`／`ButtonBindingValues` を受け取るだけにした。`SpecialAction` は、状態の鍵・ログの名前・系統 B への入力として残る（識別用。中立な型への置き換えは Step8 以降）。従来のコンストラクターは互換として残した（TODO: Step8）。Macro は 7.8c に回した（Q3＝A）。
- **土台**: `IControlBindingBuilder`／`ControlBindingBuilder`（空）、`BindingMigrationOptions.MigratedControlKinds`（既定は空）、`ITriggerDispatchEngine.OwnsControl`（常に偽）、`Mapping.ProcessControlSettingAction` の先頭に素通りの 1 行。
- **テスト**: `Phase8Step78aFoundationTests`（新規）。ビルド・テスト成功（2026-10-20。ユーザーが実行）。次は 7.8b（Controls タブの Key。Q4＝B の動作変更を含む）。

## 7.8b Controls タブの Key の新経路への移行（2026-10-20）

計画書: `Phase8-Step7-8b-Plan.md`（Q1〜Q5 承認済み。推奨どおり）。運用は 7.1〜7.7 と同じ（`ServiceRegistration` の既定の `MigratedControlKinds` に Key の 1 行。外すと旧経路）。旧コードは Step8 まで残す。

- **範囲**: Controls タブで「ボタン → キー」を割り当てた dcs のうち、シフトなし、Extras なし、Lightbar マクロなし、`actionKey` が範囲内のものだけを、新経路が処理する（ボタン単位の所有）。他は旧経路が全体を処理する。ジャイロスワイプは対象外。これは移行中の仕組みで、7.8c〜f で区分を増やし、最終的にすべて新経路になる。
- **新設・変更**: `IControlTriggerInput`／`TriggerInput.IsControlActive`（現行の `GetBoolActionMapping` の移植。スティックは角度の扇形、L2／R2 は 0 より大きい）、`ControlButtonInputAction`、`BindingPolicy.ErasesOutputEveryFrame`（割り当てのあるボタンの通常の出力を、毎フレーム消す）、`KeyBindingValues`（別名キー、SA 形式ログの抑制）、`ControlBindingBuilder`（実装）、`ActionBindingBuilder`（Controls の束縛を SA の束縛の後ろに連結）、`TriggerDispatchEngine`（`OwnsControl`／`ProcessControl`）、`Mapping`（所有なら `ProcessControl` を呼んで戻る）。
- **動作の変更（承認済み。Q4＝B）**: Controls タブの Key は、同じ論理キーの Key 特殊アクションと状態を共有しない。
- **ログ**: 旧経路の Controls タブの Key が出していた行（`KeyAction` 以降）は同じ。旧ディスパッチャ内部の Trace（`DispatchInputEdge`、`DispatchTriggerEdge: firing established` など）は、7.1〜7.7 と同様、新経路では出ない。
- **テスト**: `Phase8Step78bW1ControlsKeyCharacterizationTests`（旧・新）、`Phase8Step78bW2ControlsKeyTests`。ビルド・テスト成功。実機確認は Step9（U-41）。

## 7.8c Controls タブの Macro の新経路への移行（2026-10-20。ビルド・テスト成功）

計画書: `Phase8-Step7-8c-Plan.md`（ユーザー指示: Controls の Macro も SA の Macro の仕様に合わせる）。運用は 7.8b と同じ（`MigratedControlKinds` に Macro の 1 行。外すと旧経路）。

- **範囲**: Macro、手順あり、シフトなし、Extras なし、Lightbar マクロなしの割り当てだけ（ボタン単位の所有。移行中の仕組み）。
- **SA に合わせた点**: 出力は既存の `MacroAction`、押下の判定は SA のトリガー（`IsActive`）、状態は `ActionInstanceState`、呼び出しは立ち上がりの 1 回（繰り返しは `RepeatMacro` の内部ループ）、Fresh-press ガードを見る。
- **動作の変更（承認済み）**: (1) **Play Once の連続再実行を、不具合として 1 回だけに直した**（旧経路は、フラグなしでも押している間ほぼ毎フレーム再実行されていた。W1 の実測: 900ms で 58 回）。(2) ジャイロの閾値が 0 から ±67 に。(3) プロファイル適用直後の押しっぱなしは、一度離すまで動かない（Fresh-press ガード）。(4) 状態の持ち方が、ボタン単位の `macrodone` から `ActionInstanceState` に。
- **後続**: Phase10（割り当ての UI の統一）で、Controls と SA のマクロの編集画面を共通化する（`Phase10-Assignment-UI-Unification-Plan.md`）。

## 7.8d Controls タブの「シフト」機能の廃止（2026-10-20。W2 までの実装。ビルド・テストの結果待ち）

計画書: `Phase8-Step7-8d-Plan.md`（ユーザーの決定: SA の組み合わせトリガーで代用できるため、シフト機能を廃止する。規約 2.2 の機能廃止の承認）。**当初の 7.8d（シフトの新経路への移行）は、この廃止に置き換えた。**

- **廃止したもの**: Controls タブの割り当ての「シフト」（副ボタンを押している間だけ別の割り当てに切り替わる）。データ（`DS4ControlSettings` のシフトの欄）、実行（`Mapping` のシフトの処理、副ボタンの表）、画面（割り当て画面のシフトの選択）、旧形式の `<ShiftModifier>` の読込。代用できない使い方（アナログの割り当て、Extras）は、利用者が承知のうえで失う。
- **プロファイルに残るシフトの設定**: 読込のとき、`<ShiftControl>` の内容を解釈せず、そのまま保持する（`RemovedShiftSettings`）。動作には使わない。保存では、保持した内容をそのまま書き出す（編集画面で削除を選べば、空の要素になる）。**編集画面を開いたときの警告（削除か残すかの選択。残せば毎回警告）は W3**。
- **移行処理**: SA への変換は作らない（ユーザー指示）。
- **影響した API**: `UpdateSettings`／`UpdateDS4CSetting`／`UpdateDS4CExtra`／`UpdateDS4CKeyType`／`RefreshActionAlias`／`RecordBox`／`RecordBoxViewModel`／`IEditorWindowFactory.CreateRecordBox`／`IViewModelFactory.CreateRecordBoxViewModel` から、シフトの引数を削除した。
