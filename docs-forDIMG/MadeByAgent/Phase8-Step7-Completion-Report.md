# Phase8-Step7 完了報告書（経過報告）: 種別ごとの切替

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
計画書: `Phase8-Step7-Plan.md`（Q1〜Q8 を推奨どおり承認）  
状態: **7.1 Button は完了（W1〜W3 のビルド・テスト成功。実機確認 U-27 の主要 4 項目が合格）。7.2 以降は未着手**

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
