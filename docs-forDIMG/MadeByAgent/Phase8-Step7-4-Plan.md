# Phase8-Step7-4 計画書: Program 種別の切替（プログラムの起動）

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Step7-Plan.md`（§3 共通の手順、§4 の 7.4）、`Phase8-Step7-2-Plan.md`・`Phase8-Step7-3-Plan.md`（進め方）、`Phase8-Step1-Trigger-Spec-Ledger.md`（Program）  
状態: **Q1〜Q3 を推奨どおり承認（2026-10-19）。7.4-W1 完了（ビルド・テスト成功）。7.4-W1・W2 完了、W3 実装済み（結果待ち）**

---

## 1. 目的

Program 種別の特殊アクション（ボタンの組み合わせで、外部のプログラムを起動する）を、旧ループ（`MapCustomAction` の Program の分岐）から、統一ディスパッチ（エンジン）へ切り替える。7.1〜7.3 と同じ運用: `ServiceRegistration` の既定の一覧に `Program` を 1 行足すと切り替わり、外すと戻る。旧コードは Step8 まで残す。起動の処理（`LaunchProcessAction`／`DefaultProcessLauncher`）は変更しない。

## 2. 現状の調査

### 2.1 旧経路（`Mapping.MapCustomAction` の Program の分岐。4515〜4563 行付近）

- 共通部: トリガーの AND 条件、`delayTime`、トリガーのボタンの抑制と出力の消去。
- 起動の条件: `BeingTriggered` が偽、**かつ Fresh-press ガードが解除済み、かつ `IsExecuting` が偽**（Program 自身の判定。Key や Macro と同じ規則だが、共通の「ガード中は何もしない」の一覧には入っておらず、分岐の中で同じ判定をしている）。
- 起動するとき: ログ（Trace の立ち上がり 4 行、Debug 「SpecialAction Program: …ActionEntries=」）→ `IsExecuting = true` → `DispatchInputEdge` → `ActionManager.DispatchTriggerEdge` → `LaunchProcessActionAdapter.OnTrigger` → `LaunchProcessAction.Execute` → `IProcessLauncher.Launch` → `finally` で `IsExecuting = false`。起動は同期の呼び出し（`Process.Start`）で、`IsExecuting` が真なのは、この呼び出しの間だけ。
- 押している間: 何もしない（`BeingTriggered` が真のため、再び起動しない）。離したとき: `BeingTriggered` を偽にして、解放の辺を送る（`LaunchProcessAction.Stop` は何もしない）。
- `LaunchProcessAction.Execute` は、`details`（パス）と `extra`（引数。`$hidden` で非表示。`.bat`／`.cmd` は `%COMSPEC% /C`）を扱い、自前の Trace ログ（`LaunchProcessAction.Execute: id=… target=… hidden=…`）を出す（旧・新で同じ部品）。

### 2.2 新経路（Step3〜Step6 で用意済み）

- `ActionBindingBuilder`: Program → `TriggerBinding(ComboTriggerInputAction, LaunchProcessAction, BindingPolicy(blocksWhileExecuting: true))`。Fresh-press ガードは、既定で有効。
- エンジン: 立ち上がりで、ガードと `IsExecuting` を確認し、`IsExecuting = true` にして `Execute`、`finally` で `false` に戻す（旧経路と同じ）。押している間は何もせず、離したときに `Stop`（何もしない）。
- `LaunchProcessAction` は、既存の単体テスト（`LaunchProcessActionTests`）で、`$hidden`・`.bat`・引数を確認済み。

### 2.3 差になりうる点（W1 の特性化で確定する）

| # | 点 | 旧経路 | 新経路 | 扱い |
|---|---|---|---|---|
| 1 | ログ | 立ち上がりの Trace 4 行、Debug 1 行 | `LaunchProcessAction` は出さない | **Q1** |
| 2 | 起動の回数 | 押下で 1 回。保持中・ガード中は起動しない | エンジンの規則で同じはず | W1 で、起動の回数を新旧で比べる |
| 3 | `IsExecuting` | 起動の呼び出しの間だけ真 | 同じ | W1 で確認 |
| 4 | 起動の失敗 | `DefaultProcessLauncher` が握りつぶしてログ（GUI の警告）を出す | 同じ部品 | 変更なし |

## 3. 作業

### W1: 足場と両経路の特性化（コードは変えない）

**W1 の実装（2026-10-19。ビルド・テスト成功）**: `SpecialActionTestScope(migrated: true)` が Program も移行。`Phase8Step74W1ProgramDualPathTests`（新設）: 存在しないパスの起動の試行を `GuiLog` で数え、旧・新で、累計の起動の回数・`BeingTriggered`・`IsExecuting` を比較。押下ごとに 1 回、`$hidden`、ガード中、`IsExecuting` 中、AND 条件、遅延、押している最中のプロファイル適用。

- `SpecialActionTestScope(migrated: true)` が、Program も移行する。
- **起動の観察の口**: 実際にプログラムを起動しないよう、**存在しない実行ファイルのパス**を使う（既存の `Phase7cW2…` の Program のテストと同じ）。`DefaultProcessLauncher` は、起動に失敗すると `AppLogger.LogToGui("Process launch failed for '<パス>': …")` を出すため、`AppLogger.GuiLog` を購読して、パスごとに起動の試行の回数を数える（旧・新で同じ部品を通るため、回数を比べられる）。
- 既存の Program の特性化（`Phase7cW2…` の `ProfileAndProgram_AreBlockedWhileTheGuardIsOnOrExecuting` の Program の 2 行）を、旧・新の両経路にする。
- 新規: 押下で 1 回だけ起動、保持中は起動しない、離して再び押すと起動、ガード中は起動しない、`IsExecuting` 中は起動しない、AND 条件、遅延、押している最中のプロファイル適用、`$hidden` つきでも同じ。

### W2: 新経路の不足分

**W2 の実装（2026-10-19。ビルド・テスト成功）**: `LaunchProcessAction` に、エンジン用のコンストラクター（添字と `ISpecialActionRepository` つき）を足し、旧経路のログ（Trace 4 行［Trigger keys は常に `<none>`］、Debug の「SpecialAction Program: …ActionEntries=」）を、起動の前に、同じ文言・順序で出す。旧経路のアダプターは、添字なしのままで、ログを出さない（二重にならない）。ビルダーが添字とリポジトリを渡す。テスト: `Phase8Step74W2ProgramLogTests`。

- Q1 の実装: `LaunchProcessAction` に、エンジン用のコンストラクター（添字と `ISpecialActionRepository` つき）を足し、旧経路のログ（Trace 4 行、Debug の「SpecialAction Program: …」）を同じ文言・順序で出す（7.3 の `MacroAction` と同じ方式。旧経路のアダプターは、`Mapping` が同じログを出すため、添字なしのままで出さない）。NLog のメモリ出力で、旧経路との行の一致をテストする。

### W3: スイッチと性能

- `ServiceRegistration` の既定の一覧に `Program` を足す。ロールバックのテスト、割り当て（新 ≦ 旧。立ち上がりを除く）、ホストの既定の一覧の検査を更新。

### W4: 実機確認と報告

- チェックリストに U-31（Program）を追加し、ユーザーが確認する。`Phase8-Step7-Completion-Report.md` に 7.4 を追記。

## 4. 質問（推奨は A）

- **Q1（ログ）**: A（推奨）: 旧経路のログを、同じ文言・レベル・順序で出す（規約 2.3）。B: 省く（規約に反する）。
- **Q2（テストでの起動の観察）**: A（推奨）: 存在しない実行ファイルのパスで、起動の試行を `GuiLog` の警告で数える（実際のプログラムは起動しない。既存のテストと同じ流儀）。B: 起動の部品（`IProcessLauncher`）を、テスト用の偽物へ差し替える（本番のコンテナに、テスト専用の差し替えの口が要る）。
- **Q3（実機確認の時期）**: A（推奨）: 7.1〜7.3 と同じく、PR ごとにユーザーが実機で確認し、合格してから 7.5 へ進む。B: 7.5 と併せて、後でまとめて確認する（7.5 は、最大のリスクを持つ種別のため、不具合の原因が分かりにくくなる）。

## 5. リスクと対応

- **実機での起動**: 実際のプログラムを起動する種別のため、U-31 では、無害なプログラム（メモ帳、電卓）と、`$hidden` つきの `.bat` で確認する。
- **ロールバック**: `ServiceRegistration` の Program の 1 行を外す。
- **7.5（Profile）への影響**: 本 PR は `UntriggerIndex` などの復帰の合図に触れない（Program は復帰を持たない）。

## 6. 完了条件

- [ ] W1〜W4 のビルド・テストが成功（ユーザーが実行）。Program の特性化が、旧経路・新経路の両方で、期待値を変えずに成功。
- [ ] 全テスト・ラチェットの一致検査・1 ファイル 1 型のガードが成功。割り当て（新 ≦ 旧）。
- [ ] 既定の一覧に Program が入り、ログの文言・レベルが旧経路と同じ。ロールバックのテストが成功。
- [ ] 実機確認 U-31 が合格（または先送りの記録）。起動ログの「built at … (commit …)」で、確認したビルドを記録する。
- [ ] `Phase8-Step7-Completion-Report.md`、`Phase8-Status.md` の更新。

## 7. 次

7.5 Profile ＋ 復帰（最大のリスク。短い個別の計画書を、U-31 の合格後に作成。合図の移行と適用との連携を、2 PR に分けるかを、その計画書で決める）。
