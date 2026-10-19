# Phase8-Step6 計画書: 切断・接続との連携（エンジンと登録簿の後始末、`ClearState`／`ClearPendingRestore` の扱い）

作成日: 2026-10-19  
対象ブランチ: `For-DI-migration-work`  
上位: `Phase8-Unified-Trigger-Dispatch-Plan.md`（§5 の Step6）、`Phase8-Step2-Design.md` §16・§18（5.6）・§22（Step6）、`Phase8-Step1-Orphan-Decisions.md` §2（`ClearState`／`ClearPendingRestore`）、`Phase8-Step5-Completion-Report.md` §5  
状態: **完了（W1〜W3 成功、2026-10-19）。完了報告書: `Phase8-Step6-Completion-Report.md`**

---

## 1. 目的と位置づけ

- コントローラーの**物理的な切断**のとき、そのスロットのエンジンの状態（抑制の集合、束縛ごとの実行時の状態、復帰の合図）と登録簿の束縛を消す（`ITriggerDispatchEngine.OnDeviceDisconnected`。内部で `IActionRegistry.Clear` も呼ぶ）。
- **接続のとき**、プロファイルを適用し直さない経路（一時プロファイル／AutoProfile の使用中）でも、登録簿に束縛が入っているようにする。
- Phase5-Step3 で契約だけ配備された `IProfileSwitcher.ClearState`／`IProfileApplicationService.ClearPendingRestore`（本番の呼び出し 0 件）について、**意味を調べて、接続するか、別の扱いにするかを決める**（規約 2.4。Phase8-Step1 から持ち越し）。
- 移行対象（`BindingMigrationOptions.MigratedTypes`）は空のままなので、**本番の挙動は変わらない**（切断で消すものは、いまは空の登録簿とエンジンの空の状態だけ）。Step7 の各 PR が動き出したときに、切断・再接続で古い状態が残らない、または束縛が空のままになる、という事故を、先に塞ぐ。
- やらないこと: `OnProfileApplied` の接続（Step7.1 で判断。Step5 計画書 Q4）、種別ごとの切替（Step7）、`Stop()`（サービス停止）の経路の変更（Q5）。

## 2. 現状の調査（この計画書のための事前確認）

### 2.1 切断の経路

- 物理的な切断は `ControlService.On_DS4Removal`: 出力デバイスの取り外し → `_mapping.Commit` → **`_mapping.HandleDeviceDisconnect(ind)`**（保持中の合成キーの解放、カスタムマッピングのキューとマクロのタスクの消去、`deviceRuntime` の作り直し、`KeyButtonActionController` の破棄）→ 通知 → `DS4Controllers[ind] = null` ほか。
- **`Mapping.HandleDeviceDisconnect` はプロファイルの適用のたびにも呼ばれる**（適用の後始末の `ActionManager.ClearDeviceState` が内部で呼ぶ）。そのため、ここにエンジンの `OnDeviceDisconnected` を足すことはできない（適用のたびに登録簿が空になり、復帰の合図［Phase7b W1 の退避・復元の対象］が消える）。**切断専用の呼び出し口が必要**。
- サービス停止（`ControlService.Stop`）は、`Removal` イベントを経由せず、`IsRemoved = true` と `StopUpdate()` で止める経路があり、`HandleDeviceDisconnect` も呼ばれない。現行の後始末の責任範囲に、`Stop` は含まれていない。

### 2.2 接続の経路

- `PrepareConnectedInputController`: 一時プロファイル／AutoProfile を使っていなければ（`!useAutoProfile`）、`IProfileSlotApplier.ApplyToSlot` で適用する。**適用すれば Step5 の `PublishBindings` が走る**ので、束縛は入る。
- **`useAutoProfile` が真のときは適用しない**（メモリ上のプロファイルの状態が残っているため）。この経路では、切断時に登録簿を空にすると、**再接続後に束縛が空のまま**になる（Step7 で種別を切り替えた後に、特殊アクションが効かなくなる）。→ 接続時に差し替える必要がある。
- `IControllerRegistry`（`DefaultControllerRegistry`）は、系統 B の `KeyButtonActionController` の登録簿（`KeyButtonControllerRegistry` が使う）。新経路（エンジン）は、出力を `IOutputContext.OutputHandler` で行い、コントローラーの紐付けを必要としない。系統 B は Step8 で削除する。

### 2.3 `ClearState`／`ClearPendingRestore` の意味

- `ClearPendingRestore(device)` は `OlderProfilePath[device] = ""` にする。`IProfileSwitcher.ClearState` はその委譲。
- `OlderProfilePath` の用途（実測）:
  1. **初回接続の既定**（`PrepareConnectedInputController`: リンクされたプロファイルがないとき `OlderProfilePath[index]` を適用）。アプリ設定（`Profiles.xml` の Controller1〜8）から読み込む。初回接続のフラグは、アプリの起動時（`App.xaml.cs` の `ResetConnectionFlags`）だけで立つ。サービスの停止→開始では立たない。
  2. **画面（スロットの「通常のプロファイル」）**: `CompositeDeviceModel` がプロファイルの選択で更新し、リンクされたプロファイルの解除時に `ProfilePath = OlderProfilePath` で戻す。`MainWindow` がプロファイルの名前変更に追従する。
  3. `RestoreFromAction`（`ProfileSwitchAction.Stop` の経路）。ただし台帳 §8 項目 11 のとおり、本番ではこの経路は通らない（適用の掃除で `BeingTriggered` が戻るため）。
- Profile 種別の「離したら元のプロファイルへ戻る」は、`OlderProfilePath` ではなく、**トリガー時に記録する `prevProfileName`／`prevProfileWasTemporary`**（`Mapping` の復帰の合図 `UntriggerAction`）で行う。この合図は、新設計ではエンジンが持ち、`OnDeviceDisconnected` で消える。
- 切断で `ClearPendingRestore` を呼ぶと、そのスロットの「通常のプロファイル」が空になり、再接続後にリンクされたプロファイルを解除したとき `ProfilePath` が空になる、名前変更の追従が効かない、などが起きうる（**リスク**）。一方、ガードレール 6 が防ぎたかった「切断で復帰先が残る」は、復帰の合図の消去（`OnDeviceDisconnected`）が担う。

## 3. 作業内容

### 3.1 6-0 現状の特性化（W1）

- 切断・再接続で何が残り、何が消えるかを、現行の挙動のまま固定するテストを足す: `OlderProfilePath`／`SelectedProfile`／一時プロファイルの状態が切断で変わらないこと、`HandleDeviceDisconnect` が消すもの（合成キー、キュー、`deviceRuntime`）、適用のたびにも同じ後始末が走ること。
- `ControlService.Stop` の経路が `Removal` を経由しないこと、`OlderProfilePath` の使用箇所（読み込み、画面、適用サービス、初回接続）を固定して記録する。
- 結果を、本計画書の Q3 の根拠として追記する（コードは変えない）。

### 3.2 6-1 切断・接続の口（W2）

- `IProfileSlotApplier` に 2 つのメンバーを足す（`ControlService` のコンストラクターを変えずに済む、既存の依存を使う。Q1）:
  - `ReleaseSlot(int slotIndex)`: 物理的な切断の後始末。`ITriggerDispatchEngine.OnDeviceDisconnected` を呼ぶ（登録簿の `Clear`、抑制・実行時の状態・復帰の合図の消去）。範囲外のスロットでは何もしない。例外は握ってログに残す（切断処理を止めない）。
  - `PublishSlot(int slotIndex)`: 接続時に、プロファイルを適用しない経路（`useAutoProfile`）で、`IProfileApplicationService.PublishBindings` を呼ぶ（Q2）。
- `ProfileSlotApplier` に `ITriggerDispatchEngine` を注入する。
- `ControlService.On_DS4Removal` の `_mapping.HandleDeviceDisconnect(ind)` の直後に `ReleaseSlot(ind)`、`PrepareConnectedInputController` の `useAutoProfile` の分岐に `PublishSlot(index)` を足す。
- `ClearState`／`ClearPendingRestore`: Q3 の決定に従う（推奨は A: 接続せず、承認のうえで削除）。

### 3.3 6-2 検証と後片付け（W3）

- 実ホストで、`ReleaseSlot` → 登録簿が空、エンジンの抑制が空、`OlderProfilePath`／`SelectedProfile` は不変。`PublishSlot` → 束縛が入る。範囲外のスロットは無視。例外でも切断処理が続く。
- ソース走査で、`On_DS4Removal` が `HandleDeviceDisconnect` の後に `ReleaseSlot` を呼ぶこと、接続の分岐が `PublishSlot` を呼ぶこと、`HandleDeviceDisconnect` 側（適用でも呼ばれる）にエンジンの切断を足していないことを固定する。
- Q3＝A の場合: `ClearState`／`ClearPendingRestore`（`IProfileSwitcher`、`IProfileApplicationService`、実装、テスト用の偽の実装、テスト）を削除し、`Phase8-Step1-Orphan-Decisions.md` の記録を更新する。

### 3.4 6-3 まとめ（W4）

- 完了報告書、`Phase8-Status.md` の更新、モデル図の更新案の追記。

## 4. テスト方針

- 特性化（W1）: 切断・再接続での `OlderProfilePath`／`SelectedProfile`／一時プロファイルの不変、`HandleDeviceDisconnect` の効果、サービス停止の経路。
- 結合（W3）: 上記 3.3。本番の経路を通す部分は、`ControlService` の接続・切断の本体（ハードウェア依存）を直接動かさず、`ProfileSlotApplier` の口と、ソース走査で固定する。
- 既存の全テストが期待値を変えずに成功すること（`ControlService` のコンストラクターは変えない）。ラチェットの一致検査。

## 5. Wave 構成

- W1: 6-0（特性化テスト、調査の追記）。
- W2: 6-1（`ReleaseSlot`／`PublishSlot`、`ControlService` への接続、DI、偽の実装の追従）。
- W3: 6-2（結合テスト、ソース走査、Q3 に従う削除）。
- W4: まとめ（完了報告書、Status）。

## 6. 質問（推奨は A）

- **Q1（切断の口の置き場所）**: A（推奨）: `IProfileSlotApplier` に `ReleaseSlot` を足す（`ControlService` が既に持つ依存。コンストラクターを変えない）。B: `ControlService` に `ITriggerDispatchEngine` を直接注入する（コンストラクターの引数が増え、テストの構築が広く変わる）。C: `Mapping.HandleDeviceDisconnect` に足す（**不可**: 適用のたびにも呼ばれ、登録簿と復帰の合図が消える）。
- **Q2（一時プロファイル／AutoProfile を使う再接続）**: A（推奨）: 接続時に `PublishSlot` で束縛を差し替える。切断で登録簿を空にしても、再接続後に束縛が戻る。B: 切断では登録簿を空にせず、エンジンの状態だけを消す（設計書の `registry.Clear` を外す。古い束縛が残るが、入力が来ないので害はない。Step7 の事故の防止は A より弱い）。C: 何もせず、Step7.1 で対応する（忘れる危険）。
- **Q3（`ClearState`／`ClearPendingRestore`）**: A（推奨）: **切断には接続せず、承認を得て削除する**。切断で `OlderProfilePath` を空にすると、そのスロットの「通常のプロファイル」が空になり、画面の動作（リンクの解除など）に影響しうる（§2.3）。ガードレール 6 の目的（切断で復帰先が残らない）は、エンジンの `OnDeviceDisconnected` による復帰の合図の消去が担う。B: 設計書どおり、切断で `ClearPendingRestore` を呼ぶ（リスクあり。呼ぶなら `OlderProfilePath` の初回接続の既定の用途を先に分ける必要がある）。C: 残して、TODO の接続予定を Step7.5（Profile の切替）へ送る（判断の先送り）。
- **Q4（`IControllerRegistry` による接続時の紐付け）**: A（推奨）: 新経路では不要と確定する（出力は `IOutputContext.OutputHandler`）。系統 B の `KeyButtonActionController` 用の登録簿は、Step8 の系統 B の削除まで、そのまま使う。B: 接続時に新経路の側でもコントローラーを紐付ける（利用者がなく、未使用のコードが増える）。
- **Q5（切断の後始末を付ける経路）**: A（推奨）: 物理的な切断（`On_DS4Removal`）だけに付ける。サービスの停止（`Stop`）は、現行の `HandleDeviceDisconnect` と同じく対象外（再開始の接続で、束縛は適用か `PublishSlot` により作り直される）。B: `Stop` の経路にも付ける（現行の後始末の範囲を超え、停止処理の順序に触れる）。
- **Q6（特性化テストを先に）**: A（推奨）: W1 で現行の切断・再接続の挙動を固定してから、口を足す。B: 省く（Q3 の根拠が弱くなる）。
- **Q7（`OnProfileApplied`）**: A（推奨）: Step6 でも接続しない。適用時に抑制の集合を消す挙動の変化は、最初の種別の切替（Step7.1）で、特性化テストとあわせて決める（Step5 の Q4 の継続）。B: Step6 で接続する。

## 7. リスクと対応

- **切断と適用の競合**: `ReleaseSlot` は切断のスレッド、`PublishBindings` は適用のスレッドから呼ばれる。登録簿は参照の差し替えでアトミック、エンジンの状態はデバイスごとのロック（Step4 Q8）の下で扱う。切断の直後に適用が走っても、最後に差し替えた側が残る（適用が後なら束縛が入る。物理的に切断されたスロットで適用が走るのは、再接続のときだけ）。
- **切断処理の停止**: `ReleaseSlot` の例外は握ってログに残し、`On_DS4Removal` の後続の処理（`DS4Controllers[ind] = null` ほか）を止めない。
- **`OlderProfilePath` を壊す**: Q3＝A なら切断では触らない。W1 の特性化で、切断で不変であることと、使用箇所の一覧を固定する。
- **未使用コードの扱い（規約 2.4）**: Q3＝A の削除は、規約 2.2 に従い、ユーザーの承認を得てから行う（削除の対象と理由を W3 の前に再掲する）。

## 8. 完了条件

- [x] W1〜W4 のビルド・テストが成功する（ユーザーが実行）。既存の全テストが期待値を変えずに成功。ラチェットの一致検査、1 ファイル 1 型のガードも成功。
- [x] 切断のスロットで、登録簿とエンジンの状態が消え、`OlderProfilePath`／`SelectedProfile` は不変。プロファイルを適用しない再接続でも、束縛が差し替わる。
- [x] 適用のたびに走る `HandleDeviceDisconnect` に、エンジンの切断を足していない。
- [x] `ClearState`／`ClearPendingRestore` の扱いが決まり、TODO が解消されている（`Phase8-Step1-Orphan-Decisions.md` を更新）。
- [x] 本番の挙動が変わらない（`MigratedTypes` が空）。ベンチマークの割り当て 0 が不変。
- [x] 完了報告書、`Phase8-Status.md` の更新。

## 9. 次の Step

- Phase8-Step7（アクション種別ごとの切替。Button から。1 種別＝1 PR）。計画書は、Step6 の完了後に作成する。
